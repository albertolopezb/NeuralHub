"""Recomendación del panel (regla en Python, 0 llamadas al modelo).

Para cada tiburón: puntaje = 2 × (fracción de sus campos que son huecos) + afinidad,
donde afinidad = 0.5 si el sector del pitch está en sus sectores + 0.5 si la etapa está en sus etapas.
Se recomiendan los 3 con mayor puntaje; el empate lo gana el que va primero en el catálogo.
"""

from .pitch import ETIQUETAS, Pitch, es_hueco
from .tiburones import IDS, TIBURONES

TAM_PANEL = 3


def evaluar(pitch: Pitch) -> list[dict]:
    filas = []
    for idx, tid in enumerate(IDS):
        t = TIBURONES[tid]
        huecos = [c for c in t["campos"] if es_hueco(pitch, c)]
        fraccion = len(huecos) / len(t["campos"])
        afin_sector = pitch.sector in t["sectores"]
        afin_etapa = pitch.etapa in t["etapas"]
        puntaje = 2 * fraccion + 0.5 * afin_sector + 0.5 * afin_etapa
        partes = []
        if huecos:
            partes.append("te faltan datos de " + ", ".join(ETIQUETAS[c] for c in huecos))
        if afin_sector:
            partes.append("su perfil encaja con tu sector")
        if afin_etapa:
            partes.append("evalúa bien negocios en tu etapa")
        filas.append({
            "tiburon_id": tid, "puntaje": round(puntaje, 3), "huecos": huecos,
            "fraccion_huecos": round(fraccion, 3), "afinidad_sector": afin_sector,
            "afinidad_etapa": afin_etapa, "indice": idx,
            "motivo": "; ".join(partes) if partes else "no hay datos que lo hagan imprescindible",
        })
    return filas


def recomendar(pitch: Pitch) -> list[dict]:
    """Devuelve los 6 tiburones ordenados; los primeros 3 llevan recomendado=True."""
    filas = sorted(evaluar(pitch), key=lambda f: (-f["puntaje"], f["indice"]))
    for i, f in enumerate(filas):
        f["recomendado"] = i < TAM_PANEL
    return filas


def orden_de_turnos(pitch: Pitch, panel: list[str]) -> list[str]:
    """Orden en que preguntan en la ronda 1: primero el que tiene más huecos en su criterio.
    En las rondas siguientes el orden rota una posición."""
    puntajes = {f["tiburon_id"]: f for f in evaluar(pitch)}
    return sorted(panel, key=lambda t: (-puntajes[t]["fraccion_huecos"], puntajes[t]["indice"]))


def criterios_no_evaluados(panel: list[str]) -> list[dict]:
    return [{"tiburon_id": t, "nombre": TIBURONES[t]["nombre"], "criterio": TIBURONES[t]["criterio"]}
            for t in IDS if t not in panel]
