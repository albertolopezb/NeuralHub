"""Prueba ciega de personalidades (objetivo 2), versión automatizada con un juez LLM.

Toma 10 mensajes por tiburón (preguntas, réplicas y votos de las sesiones de prueba), quita los nombres
y le pide a un juez que identifique qué perfil lo escribió. Es un indicador preliminar: el objetivo
original pide que lo haga una persona que no escribió los prompts (planilla en pruebas/prueba_ciega_humana.csv).
"""
import csv
import json
import random
import re
from collections import Counter
from pathlib import Path
from typing import Literal

from pydantic import BaseModel

from neuralhub.llm import LLM
from neuralhub.tiburones import IDS, TIBURONES

BASE = Path(__file__).parent
random.seed(11)


class Atribucion(BaseModel):
    tiburon_id: Literal["tiburon_financiero", "tiburon_estratega", "tiburon_tecnologico", "tiburona_consumo", "tiburon_mentor", "tiburon_negociador"]


def anonimizar(t: str) -> str:
    for tid, d in TIBURONES.items():
        t = re.sub(d["nombre"], "otro tiburón", t, flags=re.I)
        t = re.sub(r"tibur[oó]na? " + d["corto"], "otro tiburón", t, flags=re.I)
    return t


def main():
    por = {t: [] for t in IDS}
    for f in sorted((BASE / "sesiones" / "prueba").glob("*.json")):
        for e in json.loads(f.read_text(encoding="utf-8"))["eventos"]:
            if e["tipo"] in ("PREGUNTA", "REPLICA", "VOTO") and e["emisor"] in por:
                por[e["emisor"]].append(anonimizar(e["texto"]))
    muestra = []
    for t, msgs in por.items():
        random.shuffle(msgs)
        muestra += [(t, m) for m in msgs[:10]]
    random.shuffle(muestra)
    catalogo = "\n".join(f"- {t}: {d['perfil']} Criterio: {d['criterio']}." for t, d in TIBURONES.items())
    llm = LLM()
    filas, aciertos = [], 0
    for real, msg in muestra:
        r = llm.estructurado(Atribucion, "Eres un evaluador. Lee un mensaje de un panel de inversionistas y decide qué perfil lo escribió.",
                             f"PERFILES:\n{catalogo}\n\nMENSAJE:\n{msg}\n\nResponde con el tiburon_id del autor más probable.")
        ok = r.tiburon_id == real
        aciertos += ok
        filas.append({"real": real, "juez": r.tiburon_id, "acierto": ok, "mensaje": msg})
    por_tib = Counter(f["real"] for f in filas if f["acierto"])
    res = {"mensajes": len(filas), "aciertos": aciertos, "porcentaje": round(100 * aciertos / len(filas), 1),
           "por_tiburon": {t: f"{por_tib.get(t, 0)}/{sum(1 for f in filas if f['real'] == t)}" for t in IDS}, "llamadas": llm.llamadas}
    (BASE / "resultados_prueba_ciega.json").write_text(json.dumps({"resumen": res, "detalle": filas}, ensure_ascii=False, indent=2), encoding="utf-8")
    # planilla para la prueba con una persona (sin la respuesta)
    with open(BASE / "pruebas" / "prueba_ciega_humana.csv", "w", newline="", encoding="utf-8-sig") as fh:
        w = csv.writer(fh); w.writerow(["#", "mensaje", "tu respuesta (financiero/estratega/tecnologico/consumo/mentor/negociador)"])
        for i, f in enumerate(filas, 1):
            w.writerow([i, f["mensaje"], ""])
    print(json.dumps(res, ensure_ascii=False, indent=1))


if __name__ == "__main__":
    main()
