"""API HTTP (FastAPI) que consume Unity. Arranque: uvicorn neuralhub.api:app --port 8000  (NEURALHUB_MOCK=1 para no usar el modelo)."""

import json
import os
from pathlib import Path
from typing import Optional

from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
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
    modo: str = "humano"          # humano | autopiloto


class Respuestas(BaseModel):
    respuestas: dict[str, str]    # {tiburon_id: texto}


class Negociacion(BaseModel):
    accion: str                   # ajustar | aceptar_contraoferta | mantener
    monto_mxn: Optional[float] = None
    porcentaje: Optional[float] = None
    tiburon_id: Optional[str] = None


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
        s = gestor.crear(body.pitch, body.panel, body.modo)
    except (ValueError, ValidationError) as e:
        raise HTTPException(422, str(e)) from e
    s.iniciar()
    return {"sesion_id": s.id}


@app.get("/sesiones")
def listar():
    return [{"sesion_id": s.id, "negocio": s.pitch.get("nombre"), "panel": s.panel, "estado": s.estado,
             "resultado": s.metricas()["resultado"], "creada": s.creada} for s in gestor.sesiones.values()]


@app.get("/sesiones/{id_}/eventos")
def eventos(id_: str, desde: int = 0):
    return _ses(id_).estado_publico(desde)


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
