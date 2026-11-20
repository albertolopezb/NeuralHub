import json
from pathlib import Path

from neuralhub import panel as P
from neuralhub.pitch import Pitch, valuacion_mxn

EJ = json.loads((Path(__file__).parent.parent / "pruebas" / "pitches_ejemplo.json").read_text(encoding="utf-8"))


def pitch(i=0, **cambios):
    d = dict(EJ[i]["pitch"], **cambios)
    return Pitch(**d)


def test_valuacion():
    assert valuacion_mxn(pitch(0)) == 3_000_000


def test_recomienda_exactamente_tres():
    r = P.recomendar(pitch(0))
    assert sum(f["recomendado"] for f in r) == 3 and len(r) == 6


def test_huecos_financieros_incluyen_al_financiero():
    p = pitch(0, ventas_12m_mxn=None, margen_bruto_pct=None, gasto_mensual_mxn=None)
    ids = [f["tiburon_id"] for f in P.recomendar(p) if f["recomendado"]]
    assert "tiburon_financiero" in ids


def test_equipo_vacio_incluye_al_mentor():
    p = pitch(0, equipo_roles=None, equipo_dedicacion=None, equipo_experiencia="Aún no lo sé")
    ids = [f["tiburon_id"] for f in P.recomendar(p) if f["recomendado"]]
    assert "tiburon_mentor" in ids


def test_empate_gana_el_primero_del_catalogo():
    r = P.recomendar(pitch(0))
    puntajes = [(f["puntaje"], f["indice"]) for f in r]
    assert puntajes == sorted(puntajes, key=lambda x: (-x[0], x[1]))


def test_orden_de_turnos_primero_el_de_mas_huecos():
    p = pitch(0, equipo_roles=None, equipo_dedicacion=None, equipo_experiencia=None)
    orden = P.orden_de_turnos(p, ["tiburon_financiero", "tiburon_mentor", "tiburon_estratega"])
    assert orden[0] == "tiburon_mentor"


def test_criterios_no_evaluados():
    out = P.criterios_no_evaluados(["tiburon_financiero", "tiburon_mentor", "tiburon_estratega"])
    assert {o["tiburon_id"] for o in out} == {"tiburon_tecnologico", "tiburona_consumo", "tiburon_negociador"}
