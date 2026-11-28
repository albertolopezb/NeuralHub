"""Sesiones: ejecutan el grafo en un hilo, guardan los eventos y esperan al usuario en los interrupt."""

import json
import queue
import re
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

# Marcas internas del historial («[R1]», «PITCH») que el modelo a veces copia: no deben verse ni escucharse.
_ETIQUETA = r"(?:R\d+|PITCH|pitch)"
ETIQUETAS = re.compile(rf"\s*[\(\[]\s*{_ETIQUETA}(?:\s*(?:,|/|y)\s*{_ETIQUETA})*\s*[\)\]]")


_cola_voz: "queue.Queue" = queue.Queue()


def _preparar_voces():
    from .voz import sintetizar
    while True:
        texto, emisor = _cola_voz.get()
        try:
            sintetizar(texto, emisor)
        except Exception:  # noqa: BLE001  (si falla, se vuelve a intentar cuando Unity pida el audio)
            pass


threading.Thread(target=_preparar_voces, daemon=True).start()


def a_su_manera(texto: str, emisor: str) -> str:
    """Teo dice «startup» donde otros dirían «empresa» (ambas son femeninas, así que la frase sigue concordando)."""
    if emisor != "tiburon_tecnologico":
        return texto
    return re.sub(r"\b([Ee])mpresa(s?)\b", lambda m: ("S" if m.group(1) == "E" else "s") + "tartup" + m.group(2), texto)


def sin_etiquetas(texto: str) -> str:
    return ETIQUETAS.sub("", texto).strip()


class Sesion:
    def __init__(self, pitch: dict, panel: list, modo: str, llm: LLM, guion: dict | None = None, id_: str | None = None,
                 ayuda: bool = False, nivel: str = "medio"):
        self.id = id_ or uuid.uuid4().hex[:8]
        self.pitch, self.panel, self.modo, self.llm, self.guion = pitch, panel, modo, llm, guion
        self.ayuda = bool(ayuda) and modo == "humano"   # el agente empresario solo sugiere en el modo en vivo
        self.sugerencias = 0
        self.preparar_voz = False
        # por pregunta: una recomendación y una mejora; repetir el mismo botón no gasta otra llamada
        self._sugeridas: dict[tuple, tuple] = {}
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
        texto = a_su_manera(sin_etiquetas(texto), emisor)
        with self._lock:
            e = {"n": len(self.eventos) + 1, "ronda": ronda, "tipo": tipo, "emisor": emisor, "destino": destino,
                 "texto": texto, "estado": ESTADO_EMISOR.get(tipo, "")}
            e.update(extra)
            self.eventos.append(e)
        if self.preparar_voz and tipo not in ("REPORTE", "PANEL"):
            _cola_voz.put((texto, emisor))      # se sintetiza ya, para que suene a la vez que aparece el texto
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
    def sugerir(self, tiburon_id: str, borrador: str = "") -> str:
        if not self.ayuda:
            raise PermissionError("La ayuda del agente empresario no está activa en esta sesión.")
        if tiburon_id not in self.panel:
            raise ValueError("Ese tiburón no está en el panel de esta sesión.")
        with self._lock:
            eventos = list(self.eventos)
        # solo la pregunta en curso: la última que se hizo, si todavía no tiene respuesta
        ultima = next((e for e in reversed(eventos) if e["tipo"] in ("PREGUNTA", "RESPUESTA")), None)
        if not ultima or ultima["tipo"] != "PREGUNTA" or ultima["emisor"] != tiburon_id:
            raise ValueError("Ese tiburón no tiene una pregunta esperando respuesta.")
        n = ultima["n"]
        borrador = (borrador or "").strip()
        clave = (n, "mejorar" if borrador else "recomendar")
        if clave in self._sugeridas:
            origen, listo = self._sugeridas[clave]
            if borrador and borrador not in (origen, listo):
                raise ValueError("Ya usaste «Mejorar» en esta pregunta.")
            return listo
        texto = sin_etiquetas(sugerir_respuesta(self.llm, self.pitch, eventos, tiburon_id, borrador))
        with self._lock:
            self.sugerencias += 1
            self._sugeridas[clave] = (borrador, texto)
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
        s.preparar_voz = not self.mock
        self.sesiones[s.id] = s
        return s
