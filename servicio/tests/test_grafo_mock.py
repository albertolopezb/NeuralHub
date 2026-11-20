"""Pruebas del grafo completo con el modelo simulado (no gastan tokens)."""

import json
import time
from pathlib import Path

import pytest

from neuralhub.llm import LLM
from neuralhub.sesiones import Sesion

EJ = json.loads((Path(__file__).parent.parent / "pruebas" / "pitches_ejemplo.json").read_text(encoding="utf-8"))
PITCH = EJ[0]["pitch"]


def guion_para(panel):
    return {"respuestas": {str(r): {t: f"Respuesta {r} a {t}" for t in panel} for r in (1, 2, 3)}}


def correr(panel, modo="guion", votos=None, votos2=None):
    llm = LLM(mock=True, mock_votos=votos, mock_votos2=votos2)
    s = Sesion(PITCH, panel, modo, llm, guion_para(panel) if modo == "guion" else None)
    s.iniciar(esperar=True)
    return s


def tipos(s):
    return [e["tipo"] for e in s.eventos]


def test_trato_directo_16_llamadas():
    s = correr(["tiburon_financiero", "tiburon_estratega", "tiburon_mentor"])
    m = s.metricas()
    assert s.estado == "terminada", s.error
    assert m["resultado"] == "TRATO" and m["llamadas_modelo"] == 16 and m["rondas_preguntas"] == 3
    assert m["preguntas"] == 9 and m["replicas"] == 3 and m["votos"] == 3
    assert not m["hubo_negociacion"] and m["eventos_en_orden"] and m["votos_publicados_juntos"]
    assert s.reporte()["criterios_no_evaluados"]


def test_hay_replica_de_tiburon_a_tiburon_en_cada_ronda():
    s = correr(["tiburon_financiero", "tiburon_estratega", "tiburon_mentor"])
    reps = [e for e in s.eventos if e["tipo"] == "REPLICA"]
    assert [e["ronda"] for e in reps] == [1, 2, 3]
    assert all(e["emisor"] in s.panel and e["destino"] in s.panel and e["emisor"] != e["destino"] for e in reps)


def test_sin_trato():
    s = correr(["tiburon_financiero", "tiburon_estratega", "tiburon_mentor"],
               votos={"tiburon_financiero": "salir", "tiburon_estratega": "salir", "tiburon_mentor": "salir"})
    assert s.metricas()["resultado"] == "SIN_TRATO" and s.metricas()["llamadas_modelo"] == 16


def test_un_voto_a_favor_abre_negociacion_y_19_llamadas():
    s = correr(["tiburon_estratega", "tiburon_tecnologico", "tiburon_mentor"],
               votos={"tiburon_estratega": "salir", "tiburon_mentor": "salir", "tiburon_tecnologico": "invertir"},
               votos2={"tiburon_estratega": "invertir"})
    m = s.metricas()
    assert m["hubo_negociacion"] and m["llamadas_modelo"] == 19 and m["resultado"] == "TRATO"
    assert "NEGOCIACION" in tipos(s) and "OFERTA" in tipos(s)


def test_negociacion_fallida_sin_trato():
    s = correr(["tiburon_estratega", "tiburon_tecnologico", "tiburon_mentor"],
               votos={"tiburon_estratega": "salir", "tiburon_mentor": "salir", "tiburon_tecnologico": "invertir"},
               votos2={"tiburon_estratega": "salir", "tiburon_mentor": "salir"})
    assert s.metricas()["resultado"] == "SIN_TRATO" and s.metricas()["llamadas_modelo"] == 19


def test_autopiloto_suma_una_llamada_por_ronda():
    s = correr(["tiburon_financiero", "tiburon_estratega", "tiburon_mentor"], modo="autopiloto")
    assert s.metricas()["llamadas_modelo"] == 19 and s.estado == "terminada"


def test_contraofertas_en_trato_las_elige_el_autopiloto():
    s = correr(["tiburon_financiero", "tiburon_negociador", "tiburon_mentor"])
    dec = [e for e in s.eventos if e["tipo"] == "DECISION"][-1]
    assert dec["resultado"] == "TRATO" and dec["condiciones"]["origen"] in ("original", "tiburon_financiero", "tiburon_negociador")


def test_modo_humano_pausa_y_reanuda():
    panel = ["tiburon_financiero", "tiburon_estratega", "tiburon_mentor"]
    s = Sesion(PITCH, panel, "humano", LLM(mock=True))
    s.iniciar(esperar=True)
    for r in (1, 2, 3):
        assert s.estado == "esperando" and s.pendiente["tipo"] == "RESPUESTAS" and s.pendiente["ronda"] == r
        assert len(s.pendiente["preguntas"]) == 3
        s.reanudar({p["tiburon_id"]: f"mi respuesta {r}" for p in s.pendiente["preguntas"]}, esperar=True)
    # Votos 3 a favor con contraofertas -> pide elegir condiciones
    assert s.estado == "esperando" and s.pendiente["tipo"] == "CONDICIONES"
    s.reanudar({"opcion": "tiburon_financiero"}, esperar=True)
    assert s.estado == "terminada", s.error
    dec = [e for e in s.eventos if e["tipo"] == "DECISION"][-1]
    assert dec["condiciones"]["origen"] == "tiburon_financiero"
    assert s.metricas()["llamadas_modelo"] == 16
    resp = [e for e in s.eventos if e["tipo"] == "RESPUESTA"]
    assert len(resp) == 9 and all(e["emisor"] == "emprendedor" for e in resp)


def test_modo_humano_negociacion():
    panel = ["tiburon_estratega", "tiburon_tecnologico", "tiburon_mentor"]
    s = Sesion(PITCH, panel, "humano", LLM(mock=True, mock_votos={"tiburon_estratega": "salir", "tiburon_mentor": "salir", "tiburon_tecnologico": "invertir"},
                                          mock_votos2={"tiburon_estratega": "invertir"}))
    s.iniciar(esperar=True)
    for _ in range(3):
        s.reanudar({p["tiburon_id"]: "ok" for p in s.pendiente["preguntas"]}, esperar=True)
    assert s.pendiente["tipo"] == "NEGOCIACION"
    s.reanudar({"accion": "ajustar", "monto_mxn": 300000, "porcentaje": 14}, esperar=True)
    assert s.estado == "terminada", s.error
    assert s.metricas()["resultado"] == "TRATO" and s.metricas()["llamadas_modelo"] == 19
    dec = [e for e in s.eventos if e["tipo"] == "DECISION"][-1]
    assert dec["condiciones"]["porcentaje"] == 14


def test_no_se_ven_votos_antes_de_terminar_el_debate():
    s = correr(["tiburon_financiero", "tiburon_estratega", "tiburon_mentor"])
    primer_voto = min(e["n"] for e in s.eventos if e["tipo"] == "VOTO")
    assert all(e["n"] < primer_voto for e in s.eventos if e["tipo"] in ("PREGUNTA", "RESPUESTA", "REPLICA"))


def test_api_flujo_completo():
    from fastapi.testclient import TestClient
    import os
    os.environ["NEURALHUB_MOCK"] = "1"
    from neuralhub import api
    api.gestor.mock = True
    c = TestClient(api.app)
    assert c.get("/tiburones").json().__len__() == 6
    rec = c.post("/panel/recomendacion", json=PITCH).json()
    panel = [f["tiburon_id"] for f in rec["ranking"] if f["recomendado"]]
    assert len(panel) == 3
    sid = c.post("/sesiones", json={"pitch": PITCH, "panel": panel, "modo": "humano"}).json()["sesion_id"]
    for _ in range(3):
        for _ in range(100):
            est = c.get(f"/sesiones/{sid}/eventos").json()
            if est["estado"] == "esperando":
                break
            time.sleep(0.05)
        pend = est["pendiente"]
        assert pend["tipo"] == "RESPUESTAS"
        assert c.post(f"/sesiones/{sid}/respuestas", json={"respuestas": {p["tiburon_id"]: "x" for p in pend["preguntas"]}}).status_code == 200
    for _ in range(100):
        est = c.get(f"/sesiones/{sid}/eventos").json()
        if est["estado"] in ("terminada", "esperando"):
            break
        time.sleep(0.05)
    if est["estado"] == "esperando":
        assert c.post(f"/sesiones/{sid}/condiciones", json={"opcion": "original"}).status_code == 200
        for _ in range(100):
            est = c.get(f"/sesiones/{sid}/eventos").json()
            if est["estado"] == "terminada":
                break
            time.sleep(0.05)
    assert est["estado"] == "terminada", est.get("error")
    assert c.get(f"/sesiones/{sid}/reporte").status_code == 200
    # polling incremental
    k = est["total_eventos"]
    assert c.get(f"/sesiones/{sid}/eventos", params={"desde": k}).json()["eventos"] == []
    assert c.post("/sesiones", json={"pitch": PITCH, "panel": panel[:2]}).status_code == 422
