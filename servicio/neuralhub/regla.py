"""Regla de decisión (nodo de votación determinista, sin modelo de lenguaje)."""

A_FAVOR = ("invertir", "contraoferta")


def a_favor(votos: dict) -> int:
    """Cuenta votos a favor: invertir y contraoferta."""
    return sum(1 for v in votos.values() if v in A_FAVOR)


def primera_votacion(votos: dict) -> str:
    """2 o 3 a favor -> TRATO; 1 a favor -> NEGOCIACION; 0 -> SIN_TRATO."""
    n = a_favor(votos)
    if n >= 2:
        return "TRATO"
    if n == 1:
        return "NEGOCIACION"
    return "SIN_TRATO"


def segunda_votacion(votos: dict) -> str:
    """Tras la negociación se vota una sola vez: 2 o más a favor -> TRATO; si no, SIN_TRATO."""
    return "TRATO" if a_favor(votos) >= 2 else "SIN_TRATO"


def elegir_contraoferta_autopiloto(contraofertas: list[dict], original: dict) -> dict:
    """El emprendedor en autopiloto busca ceder lo menos posible: elige la condición con mayor valuación
    implícita (monto / porcentaje). Si hay empate, la primera de la lista."""
    opciones = [dict(original, origen="original")] + contraofertas
    return max(opciones, key=lambda o: o["monto_mxn"] / o["porcentaje"])
