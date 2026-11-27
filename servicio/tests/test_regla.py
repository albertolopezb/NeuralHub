import itertools

from neuralhub import regla

OPC = ["invertir", "contraoferta", "salir"]


def test_27_combinaciones_primera_votacion():
    """Las 27 combinaciones de 3 opciones para 3 tiburones dan el resultado esperado."""
    total = 0
    for combo in itertools.product(OPC, repeat=3):
        votos = dict(zip(["a", "b", "c"], combo))
        favor = sum(v != "salir" for v in combo)
        esperado = "TRATO" if favor >= 2 else "NEGOCIACION" if favor == 1 else "SIN_TRATO"
        assert regla.primera_votacion(votos) == esperado, combo
        total += 1
    assert total == 27


def test_27_combinaciones_segunda_votacion():
    for combo in itertools.product(OPC, repeat=3):
        votos = dict(zip(["a", "b", "c"], combo))
        esperado = "TRATO" if sum(v != "salir" for v in combo) >= 2 else "SIN_TRATO"
        assert regla.segunda_votacion(votos) == esperado


def test_original_solo_con_dos_invertir_en_las_27_combinaciones():
    for combo in itertools.product(OPC, repeat=3):
        votos = dict(zip(["a", "b", "c"], combo))
        assert regla.puede_mantener_original(votos) == (combo.count("invertir") >= 2), combo


def test_autopiloto_sin_mayoria_no_puede_quedarse_con_su_oferta():
    orig = {"monto_mxn": 300000, "porcentaje": 10}
    contras = [{"tiburon_id": "x", "monto_mxn": 300000, "porcentaje": 20},
               {"tiburon_id": "y", "monto_mxn": 300000, "porcentaje": 12}]
    assert regla.elegir_contraoferta_autopiloto(contras, orig, False)["tiburon_id"] == "y"


def test_autopiloto_elige_mayor_valuacion():
    orig = {"monto_mxn": 300000, "porcentaje": 10}                # valuación 3,000,000
    contras = [{"tiburon_id": "x", "monto_mxn": 300000, "porcentaje": 20},   # 1,500,000
               {"tiburon_id": "y", "monto_mxn": 300000, "porcentaje": 12}]   # 2,500,000
    assert regla.elegir_contraoferta_autopiloto(contras, orig)["origen"] == "original"
    contras_mejor = [{"tiburon_id": "z", "monto_mxn": 400000, "porcentaje": 10}]  # 4,000,000
    assert regla.elegir_contraoferta_autopiloto(contras_mejor, orig)["tiburon_id"] == "z"
