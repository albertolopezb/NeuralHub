"""API HTTP (FastAPI) que consume Unity. Arranque: uvicorn neuralhub.api:app --port 8000  (NEURALHUB_MOCK=1 para no usar el modelo)."""

import json
import os
from pathlib import Path
from typing import Optional

import tempfile

from fastapi import FastAPI, HTTPException, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse
from pydantic import BaseModel, ValidationError

from . import panel as P
from .pitch import ETAPAS, SECTORES, Pitch
from .sesiones import Gestor
from .tiburones import TIBURONES

BASE = Path(__file__).resolve().parent.parent
app = FastAPI(title="NeuralHub - servicio de agentes")
app.add_middleware(CORSMiddleware, allow_origins=["*"], allow_methods=["*"], allow_headers=["*"])
gestor = Gestor(mock=os.getenv("NEURALHUB_MOCK") == "1")


class NuevaSesion(BaseModel):
    pitch: dict
    panel: list[str]
    modo: str = "humano"          # humano (en vivo: el usuario responde) | autopiloto (responde el agente empresario)
    ayuda: bool = False           # en vivo: el agente empresario puede sugerir respuestas
    nivel: str = "medio"          # facil | medio | dificil: exigencia de los tiburones


class Respuestas(BaseModel):
    respuestas: dict[str, str]    # {tiburon_id: texto}


class Negociacion(BaseModel):
    accion: str                   # ajustar | aceptar_contraoferta | mantener
    monto_mxn: Optional[float] = None
    porcentaje: Optional[float] = None
    tiburon_id: Optional[str] = None


class PedirSugerencia(BaseModel):
    tiburon_id: str               # tiburón cuya última pregunta se quiere responder


class Condicion(BaseModel):
    opcion: str = "original"      # "original" o el id de un tiburón con contraoferta


def _ses(id_):
    s = gestor.sesiones.get(id_)
    if not s:
        raise HTTPException(404, "Sesión no encontrada")
    return s


@app.get("/salud")
def salud():
    return {"ok": True, "mock": gestor.mock}


@app.get("/tiburones")
def tiburones():
    return [{"id": k, **{c: v[c] for c in ("nombre", "perfil", "criterio", "color", "campos")}} for k, v in TIBURONES.items()]


@app.get("/catalogos")
def catalogos():
    return {"sectores": SECTORES, "etapas": ETAPAS}


@app.get("/ejemplos")
def ejemplos():
    """Pitches de ejemplo para el botón «Cargar ejemplo» del formulario."""
    return json.loads((BASE / "pruebas" / "pitches_ejemplo.json").read_text(encoding="utf-8"))


@app.post("/panel/recomendacion")
def recomendacion(pitch: dict):
    try:
        p = Pitch(**pitch)
    except ValidationError as e:
        raise HTTPException(422, e.errors()) from e
    return {"valuacion_mxn": round(p.monto_mxn / (p.porcentaje / 100), 2),
            "ranking": P.recomendar(p)}


@app.post("/sesiones")
def crear_sesion(body: NuevaSesion):
    try:
        s = gestor.crear(body.pitch, body.panel, body.modo, ayuda=body.ayuda, nivel=body.nivel)
    except (ValueError, ValidationError) as e:
        raise HTTPException(422, str(e)) from e
    s.iniciar()
    return {"sesion_id": s.id}


CARPETA_SESIONES = BASE / "sesiones"


def _guardadas() -> dict:
    """Sesiones terminadas que se guardaron en disco (persisten aunque se reinicie el servicio)."""
    out = {}
    for f in sorted(CARPETA_SESIONES.glob("sesion_*.json")):
        try:
            d = json.loads(f.read_text(encoding="utf-8"))
            out[d["id"]] = d
        except (ValueError, KeyError):
            continue
    return out


@app.get("/sesiones")
def listar():
    vivas = {s.id: {"sesion_id": s.id, "negocio": s.pitch.get("nombre"), "panel": s.panel, "estado": s.estado, "modo": s.modo, "nivel": s.nivel,
                    "resultado": s.metricas()["resultado"], "creada": s.creada} for s in gestor.sesiones.values()}
    for id_, d in _guardadas().items():
        if id_ not in vivas:
            vivas[id_] = {"sesion_id": id_, "negocio": d.get("pitch", {}).get("nombre"), "panel": d.get("panel"),
                          "estado": "terminada", "modo": d.get("modo"), "nivel": d.get("nivel", "medio"), "resultado": d.get("metricas", {}).get("resultado"),
                          "creada": 0}
    return sorted(vivas.values(), key=lambda x: -x["creada"])


@app.get("/sesiones/{id_}/eventos")
def eventos(id_: str, desde: int = 0):
    s = gestor.sesiones.get(id_)
    if s:
        return s.estado_publico(desde)
    d = _guardadas().get(id_)
    if not d:
        raise HTTPException(404, "Sesión no encontrada")
    evs = d["eventos"]
    return {"sesion_id": id_, "estado": "terminada", "pendiente": None, "eventos": evs[desde:], "total_eventos": len(evs), "error": None}


def _evento(id_: str, n: int) -> dict:
    s = gestor.sesiones.get(id_)
    evs = s.eventos if s else (_guardadas().get(id_) or {}).get("eventos", [])
    if not 1 <= n <= len(evs):
        raise HTTPException(404, "Evento no encontrado")
    return evs[n - 1]


@app.get("/sesiones/{id_}/audio/{n}")
def audio(id_: str, n: int):
    """Voz del personaje que emite el evento n (WAV, voz es-MX de Windows)."""
    from .voz import sintetizar
    e = _evento(id_, n)
    if e["tipo"] in ("REPORTE",):
        raise HTTPException(404, "Este evento no tiene voz")
    return FileResponse(sintetizar(e["texto"], e["emisor"]), media_type="audio/wav")


@app.post("/voz/transcribir")
async def transcribir_voz(request: Request):
    """Recibe un WAV (cuerpo de la petición) y devuelve el texto en español (Whisper local)."""
    from .voz import transcribir
    datos = await request.body()
    if len(datos) < 1000:
        raise HTTPException(422, "Audio vacío")
    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as f:
        f.write(datos)
        ruta = Path(f.name)
    try:
        return transcribir(ruta)
    except ValueError as e:
        raise HTTPException(422, str(e)) from e
    finally:
        ruta.unlink(missing_ok=True)


@app.post("/sesiones/{id_}/respuestas")
def responder(id_: str, body: Respuestas):
    s = _ses(id_)
    if not s.pendiente or s.pendiente.get("tipo") != "RESPUESTAS":
        raise HTTPException(409, "No se esperan respuestas ahora")
    esperadas = {p["tiburon_id"] for p in s.pendiente["preguntas"]}
    if set(body.respuestas) != esperadas:
        raise HTTPException(422, "Faltan respuestas para: " + ", ".join(esperadas - set(body.respuestas)))
    s.reanudar(body.respuestas)
    return {"ok": True}


@app.post("/sesiones/{id_}/sugerencia")
def sugerencia(id_: str, body: PedirSugerencia):
    """Modo en vivo con ayuda: el agente empresario sugiere una respuesta que el usuario puede editar (1 llamada al modelo)."""
    s = _ses(id_)
    try:
        texto = s.sugerir(body.tiburon_id)
    except PermissionError as e:
        raise HTTPException(403, str(e)) from e
    except ValueError as e:
        raise HTTPException(409, str(e)) from e
    return {"tiburon_id": body.tiburon_id, "texto": texto}


@app.post("/sesiones/{id_}/negociacion")
def negociar(id_: str, body: Negociacion):
    s = _ses(id_)
    if not s.pendiente or s.pendiente.get("tipo") != "NEGOCIACION":
        raise HTTPException(409, "No se espera una negociación ahora")
    s.reanudar(body.model_dump())
    return {"ok": True}


@app.post("/sesiones/{id_}/condiciones")
def condiciones(id_: str, body: Condicion):
    s = _ses(id_)
    if not s.pendiente or s.pendiente.get("tipo") != "CONDICIONES":
        raise HTTPException(409, "No se espera elegir condiciones ahora")
    s.reanudar(body.model_dump())
    return {"ok": True}


@app.get("/sesiones/{id_}/reporte")
def reporte(id_: str):
    r = _ses(id_).reporte()
    if r is None:
        raise HTTPException(404, "El reporte aún no está listo")
    return r


@app.get("/sesiones/{id_}/metricas")
def metricas(id_: str):
    return _ses(id_).metricas()
