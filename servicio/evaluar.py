"""Evalúa el set de prueba fijo: 5 pitches × 3 sesiones = 15 sesiones (respuestas del emprendedor desde un guion fijo).

    python evaluar.py                  # real (Azure)
    python evaluar.py --mock           # sin gastar tokens
    python evaluar.py --sesiones 1     # una sesión por pitch (prueba rápida)
Guarda sesiones/prueba/*.json y resultados_prueba.json.
"""

import argparse
import json
import time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

from neuralhub import panel as P
from neuralhub.llm import LLM
from neuralhub.pitch import Pitch
from neuralhub.sesiones import Sesion

BASE = Path(__file__).parent


def guion_de(p):
    return {"respuestas": {str(r + 1): {t: textos[r] for t, textos in p["guion"].items()} for r in range(3)}}


def una(p, k, mock):
    ses = Sesion(p["pitch"], p["panel"], "guion", LLM(mock=mock), guion_de(p), id_=f"{p['id']}_s{k}")
    ses.iniciar(esperar=True)
    m = ses.metricas()
    rec = [f["tiburon_id"] for f in P.recomendar(Pitch(**p["pitch"])) if f["recomendado"]]
    carp = BASE / "sesiones" / "prueba"
    carp.mkdir(parents=True, exist_ok=True)
    (carp / f"{ses.id}.json").write_text(json.dumps(
        {"id": ses.id, "pitch_id": p["id"], "panel": p["panel"], "eventos": ses.eventos, "metricas": m,
         "error": ses.error}, ensure_ascii=False, indent=2), encoding="utf-8")
    votos = {e["emisor"]: e["decision"] for e in ses.eventos if e["tipo"] == "VOTO" and e.get("votacion") == 1}
    return {"pitch_id": p["id"], "tipo": p["tipo"], "sesion": k, "panel": p["panel"], "criterio_debil": p["criterio_debil"],
            "panel_recomendado": rec, "votos_primera": votos, "error": ses.error, **m}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--mock", action="store_true")
    ap.add_argument("--sesiones", type=int, default=3)
    ap.add_argument("--hilos", type=int, default=3)
    a = ap.parse_args()
    pitches = json.loads((BASE / "pruebas" / "pitches_prueba.json").read_text(encoding="utf-8"))
    tareas = [(p, k) for p in pitches for k in range(1, a.sesiones + 1)]
    t0 = time.time()
    with ThreadPoolExecutor(max_workers=a.hilos) as ex:
        res = list(ex.map(lambda x: una(x[0], x[1], a.mock), tareas))
    (BASE / "resultados_prueba.json").write_text(json.dumps(res, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"{len(res)} sesiones en {time.time() - t0:.0f} s")
    for r in res:
        print(f"{r['pitch_id']}.{r['sesion']} {r['resultado']:<11} llamadas={r['llamadas_modelo']:>2} "
              f"seg={r['segundos_total']:>5} neg={'sí' if r['hubo_negociacion'] else 'no'} "
              f"crit_voto={r['justificaciones_mencionan_criterio']}/3 err={r['error']}")


if __name__ == "__main__":
    main()
