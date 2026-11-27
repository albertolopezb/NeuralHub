import json
from pathlib import Path

import pytest

from neuralhub.grafo import NIVELES, sistema_tiburon
from neuralhub.sesiones import Gestor

EJ = json.loads((Path(__file__).parent.parent / "pruebas" / "pitches_ejemplo.json").read_text(encoding="utf-8"))
PANEL = ["tiburon_financiero", "tiburon_estratega", "tiburon_tecnologico"]


def test_medio_no_cambia_el_prompt_base():
    assert NIVELES["medio"] == ""
    assert sistema_tiburon("tiburon_financiero") == sistema_tiburon("tiburon_financiero", "medio")


def test_cada_nivel_cambia_la_exigencia():
    assert "FÁCIL" in sistema_tiburon("tiburon_mentor", "facil")
    assert "DIFÍCIL" in sistema_tiburon("tiburon_mentor", "dificil")


def test_hay_un_ejemplo_por_nivel():
    assert sorted(e["nivel"] for e in EJ) == ["dificil", "facil", "medio"]


def test_la_sesion_guarda_el_nivel_y_rechaza_otros():
    g = Gestor(mock=True)
    s = g.crear(EJ[0]["pitch"], PANEL, "autopiloto", nivel="dificil")
    s.iniciar(esperar=True)
    assert s.nivel == "dificil" and s.estado == "terminada"
    assert json.loads(s.guardar().read_text(encoding="utf-8"))["nivel"] == "dificil"
    with pytest.raises(ValueError):
        g.crear(EJ[0]["pitch"], PANEL, "autopiloto", nivel="imposible")
