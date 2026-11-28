"""Modo en vivo: el agente empresario sugiere respuestas solo si la ayuda está activa (modelo simulado)."""

import json
import time
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from neuralhub import api
from neuralhub.llm import LLM
from neuralhub.sesiones import Sesion

EJ = json.loads((Path(__file__).parent.parent / "pruebas" / "pitches_ejemplo.json").read_text(encoding="utf-8"))
PITCH = EJ[0]["pitch"]
PANEL = ["tiburon_financiero", "tiburon_estratega", "tiburon_mentor"]


def en_vivo(ayuda):
    s = Sesion(PITCH, PANEL, "humano", LLM(mock=True), ayuda=ayuda)
    s.iniciar(esperar=True)
    assert s.estado == "esperando" and s.pendiente["tipo"] == "RESPUESTAS"
    return s


def test_sugerencia_con_ayuda_cuenta_una_llamada():
    s = en_vivo(True)
    antes = s.llm.llamadas
    texto = s.sugerir("tiburon_financiero")
    assert texto and len(texto) <= 600
    assert s.sugerencias == 1 and s.llm.llamadas == antes + 1
    assert s.metricas()["sugerencias"] == 1


def test_repetir_la_sugerencia_no_gasta_otra_llamada():
    s = en_vivo(True)
    primera = s.sugerir("tiburon_financiero")
    antes = s.llm.llamadas
    assert s.sugerir("tiburon_financiero") == primera
    assert s.sugerencias == 1 and s.llm.llamadas == antes


def test_no_hay_sugerencia_para_quien_aun_no_pregunta():
    s = en_vivo(True)
    en_turno = s.pendiente["preguntas"][0]["tiburon_id"]
    otro = next(t for t in PANEL if t != en_turno)
    antes = s.llm.llamadas
    with pytest.raises(ValueError):
        s.sugerir(otro)
    assert s.llm.llamadas == antes


def test_recomendar_y_mejorar_son_independientes_y_tienen_tope():
    """Si mejoras, borras todo y pides recomendación, debe llegar una recomendación y no tu texto mejorado."""
    s = en_vivo(True)
    turno = s.pendiente["preguntas"][0]["tiburon_id"]
    llm = s.llm
    textos = iter(["TEXTO MEJORADO", "RECOMENDACION NUEVA"])
    original = llm.estructurado

    def falso(esquema, sis, usr):
        r = original(esquema, sis, usr)
        r.texto = next(textos)
        return r

    llm.estructurado = falso
    assert s.sugerir(turno, "mi borrador") == "TEXTO MEJORADO"
    assert s.sugerir(turno, "") == "RECOMENDACION NUEVA"
    antes = llm.llamadas
    assert s.sugerir(turno, "") == "RECOMENDACION NUEVA" and s.sugerir(turno, "mi borrador") == "TEXTO MEJORADO"
    with pytest.raises(ValueError):
        s.sugerir(turno, "otro borrador distinto")
    assert llm.llamadas == antes and s.sugerencias == 2


def test_sin_ayuda_no_hay_sugerencia():
    s = en_vivo(False)
    with pytest.raises(PermissionError):
        s.sugerir("tiburon_financiero")


def test_sugerencia_de_tiburon_fuera_del_panel():
    s = en_vivo(True)
    with pytest.raises(ValueError):
        s.sugerir("tiburon_negociador")


def test_autopiloto_ignora_la_ayuda():
    s = Sesion(PITCH, PANEL, "autopiloto", LLM(mock=True), ayuda=True)
    assert s.ayuda is False


def test_api_sugerencia(monkeypatch):
    monkeypatch.setattr(api.gestor, "mock", True)
    c = TestClient(api.app)
    sid = c.post("/sesiones", json={"pitch": PITCH, "panel": PANEL, "modo": "humano", "ayuda": True}).json()["sesion_id"]
    for _ in range(100):
        if c.get(f"/sesiones/{sid}/eventos").json()["estado"] == "esperando":
            break
        time.sleep(0.05)
    r = c.post(f"/sesiones/{sid}/sugerencia", json={"tiburon_id": PANEL[0]})
    assert r.status_code == 200 and r.json()["texto"]
    sid2 = c.post("/sesiones", json={"pitch": PITCH, "panel": PANEL, "modo": "humano"}).json()["sesion_id"]
    assert c.post(f"/sesiones/{sid2}/sugerencia", json={"tiburon_id": PANEL[0]}).status_code == 403


def test_mejorar_solo_ve_la_pregunta_y_el_borrador(monkeypatch):
    """Si el editor recibiera la ficha, cambiaría la respuesta del usuario por otra armada con el pitch."""
    s = en_vivo(True)
    turno = s.pendiente["preguntas"][0]["tiburon_id"]
    vistos = []
    original = s.llm.estructurado
    monkeypatch.setattr(s.llm, "estructurado", lambda e, sis, usr: vistos.append((sis, usr)) or original(e, sis, usr))
    s.sugerir(turno, "mi texto propio con una idea mia")
    sistema, usuario = vistos[-1]
    assert "editor de estilo" in sistema and "mi texto propio con una idea mia" in usuario
    assert "TU FICHA" not in usuario and PITCH["problema"] not in usuario
