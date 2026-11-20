"""Corre una sesión completa en consola con el emprendedor en autopiloto.

    python ejecutar_sesion.py --ejemplo 0                       # panel recomendado por la regla
    python ejecutar_sesion.py --ejemplo 1 --panel tiburon_financiero tiburon_mentor tiburona_consumo
    python ejecutar_sesion.py --mock                            # sin gastar tokens
"""

import argparse
import json
from pathlib import Path

from neuralhub import panel as P
from neuralhub.llm import LLM
from neuralhub.pitch import Pitch
from neuralhub.sesiones import Sesion
from neuralhub.tiburones import TIBURONES

BASE = Path(__file__).parent


def imprimir(ses):
    for e in ses.eventos:
        print(f"\n[{e['n']}] R{e['ronda']} {e['tipo']}  {e['emisor']} -> {e['destino']}"
              + (f"  ({e['decision']})" if "decision" in e else ""))
        print(e["texto"])


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--ejemplo", type=int, default=0)
    ap.add_argument("--panel", nargs=3, choices=list(TIBURONES))
    ap.add_argument("--mock", action="store_true")
    ap.add_argument("--guardar-como", default=None)
    a = ap.parse_args()
    ej = json.loads((BASE / "pruebas" / "pitches_ejemplo.json").read_text(encoding="utf-8"))[a.ejemplo]
    pitch = ej["pitch"]
    panel = a.panel or [f["tiburon_id"] for f in P.recomendar(Pitch(**pitch)) if f["recomendado"]]
    print(f"Pitch: {ej['etiqueta']}\nPanel: {panel}")
    ses = Sesion(pitch, panel, "autopiloto", LLM(mock=a.mock))
    ses.iniciar(esperar=True)
    if ses.estado == "error":
        raise SystemExit(ses.error)
    imprimir(ses)
    m = ses.metricas()
    print("\n--- Métricas ---\n" + json.dumps(m, ensure_ascii=False, indent=2))
    ruta = ses.guardar()
    if a.guardar_como:
        (BASE / "sesiones" / a.guardar_como).write_bytes(ruta.read_bytes())
    print(f"Guardada en {ruta}")


if __name__ == "__main__":
    main()
