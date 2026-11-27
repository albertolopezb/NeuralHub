"""Sesiones: ejecutan el grafo en un hilo, guardan los eventos y esperan al usuario en los interrupt."""

import json
import threading
import time
import uuid
from pathlib import Path

from langgraph.checkpoint.memory import MemorySaver
from langgraph.types import Command

from . import metricas
from .grafo import NIVELES, construir_grafo, sugerir_respuesta
from .llm import LLM
from .pitch import Pitch, faltan_obligatorios

BASE = Path(__file__).resolve().parent.parent
CARPETA = BASE / "sesiones"

# Estado de animación del emisor, que Unity usa para elegir la animación del personaje.
ESTADO_EMISOR = {"PITCH": "Presentando", "PREGUNTA": "Preguntando", "RESPUESTA": "Respondiendo",
                 "REPLICA": "Replicando", "VOTO": "Votando", "OFERTA": "Negociando", "DECISION": "Reaccionando"}


class Sesion:
    def __init__(self, pitch: dict, panel: list, modo: str, llm: LLM, guion: dict | None = None, id_: str | None = None,
                 ayuda: bool = False, nivel: str = "medio"):
        self.id = id_ or uuid.uuid4().hex[:8]
        self.pitch, self.panel, self.modo, self.llm, self.guion = pitch, panel, modo, llm, guion
        self.ayuda = bool(ayuda) and modo == "humano"   # el agente empresario solo sugiere en el modo en vivo
        self.sugerencias = 0
        self.nivel = nivel
        self.eventos: list[dict] = []
        self.estado = "creada"        # creada | corriendo | esperando | terminada | error
        self.pendiente: dict | None = None
        self.error: str | None = None
        self.creada = time.time()
        self.segundos = 0.0
        self._lock = threading.Lock()
        self._hilo: threading.Thread | None = None
        self.grafo = construir_grafo(MemorySaver())
        self.config = {"recursion_limit": 80, "configurable": {"thread_id": self.id, "sesion": self, "llm": llm, "guion": guion}}

    # --- eventos ---
    def emit(self, tipo, emisor, destino, texto, ronda, **extra):
        with self._lock:
            e = {"n": len(self.eventos) + 1, "ronda": ronda, "tipo": tipo, "emisor": emisor, "destino": destino,
                 "texto": texto, "estado": ESTADO_EMISOR.get(tipo, "")}
            e.update(extra)
            self.eventos.append(e)
            return e

    # --- ejecución ---
    def _avanzar(self, entrada):
        t0 = time.time()
        try:
            self.estado = "corriendo"
            self.pendiente = None
            res = self.grafo.invoke(entrada, self.config)
            intr = res.get("__interrupt__") if isinstance(res, dict) else None
            if intr:
                self.pendiente = intr[0].value
                self.estado = "esperando"
            else:
                self.estado = "terminada"
                self.guardar()
        except Exception as ex:  # noqa: BLE001
            self.error = f"{type(ex).__name__}: {ex}"
            self.estado = "error"
        finally:
            self.segundos += time.time() - t0

    def iniciar(self, esperar: bool = False):
        entrada = {"pitch": self.pitch, "panel": self.panel, "modo": self.modo}
        self._hilo = threading.Thread(target=self._avanzar, args=(entrada,), daemon=True)
        self._hilo.start()
        if esperar:
            self._hilo.join()

    def reanudar(self, valor: dict, esperar: bool = False):
        if self.estado != "esperando":
            raise ValueError("La sesión no está esperando una respuesta del usuario.")
        self.estado = "corriendo"
        self._hilo = threading.Thread(target=self._avanzar, args=(Command(resume=valor),), daemon=True)
        self._hilo.start()
        if esperar:
            self._hilo.join()

    def esperar(self, timeout=300):
        if self._hilo:
            self._hilo.join(timeout)

    # --- ayuda del agente empresario (modo en vivo) ---
    def sugerir(self, tiburon_id: str) -> str:
        if not self.ayuda:
            raise PermissionError("La ayuda del agente empresario no está activa en esta sesión.")
        if tiburon_id not in self.panel:
            raise ValueError("Ese tiburón no está en el panel de esta sesión.")
        with self._lock:
            eventos = list(self.eventos)
        texto = sugerir_respuesta(self.llm, self.pitch, eventos, tiburon_id)
        with self._lock:
            self.sugerencias += 1
        return texto

    # --- salida ---
    def estado_publico(self, desde: int = 0) -> dict:
        with self._lock:
            evs = self.eventos[desde:]
        return {"sesion_id": self.id, "estado": self.estado, "pendiente": self.pendiente,
                "eventos": evs, "total_eventos": len(self.eventos), "error": self.error}

    def reporte(self):
        for e in reversed(self.eventos):
            if e["tipo"] == "REPORTE":
                return e["reporte"]
        return None

    def metricas(self) -> dict:
        return metricas.calcular(self)

    def guardar(self):
        CARPETA.mkdir(exist_ok=True)
        ruta = CARPETA / f"sesion_{self.id}.json"
        ruta.write_text(json.dumps({"id": self.id, "modo": self.modo, "ayuda": self.ayuda, "nivel": self.nivel, "panel": self.panel, "pitch": self.pitch,
                                    "eventos": self.eventos, "metricas": self.metricas()},
                                   ensure_ascii=False, indent=2), encoding="utf-8")
        return ruta


class Gestor:
    def __init__(self, mock: bool = False):
        self.mock = mock
        self.sesiones: dict[str, Sesion] = {}
        self._llm = None

    def llm(self):
        if self._llm is None:
            self._llm = LLM(mock=self.mock)
        return self._llm

    def crear(self, pitch: dict, panel: list, modo: str = "humano", guion: dict | None = None, ayuda: bool = False,
              nivel: str = "medio") -> Sesion:
        falt = faltan_obligatorios(pitch)
        if falt:
            raise ValueError(f"Faltan campos obligatorios: {', '.join(falt)}")
        Pitch(**pitch)
        if len(panel) != 3 or len(set(panel)) != 3:
            raise ValueError("El panel debe tener exactamente 3 tiburones distintos.")
        # Cada sesión tiene su propio contador de llamadas
        if modo not in ("humano", "autopiloto"):
            raise ValueError("El modo debe ser «humano» (en vivo) o «autopiloto» (agente empresario).")
        if nivel not in NIVELES:
            raise ValueError("El nivel debe ser «facil», «medio» o «dificil».")
        s = Sesion(pitch, panel, modo, LLM(mock=self.mock), guion, ayuda=ayuda, nivel=nivel)
        self.sesiones[s.id] = s
        return s
