"""Corre el mismo pitch en los tres niveles con el modelo real y compara preguntas, votos y resultado.

Uso: python comparar_niveles.py [indice_de_ejemplo] [repeticiones]
Guarda resultados_niveles.json
"""

import json
import sys
from pathlib import Path

from neuralhub import sesiones
from neuralhub.llm import LLM
from neuralhub.panel import recomendar
from neuralhub.pitch import Pitch

BASE = Path(__file__).parent
TECNICOS = ("cac", "tam", "margen", "valuaci", "churn", "escalab", "propiedad intelectual", "métrica", "costo de adquisi")


def main():
    ej = json.loads((BASE / "pruebas" / "pitches_ejemplo.json").read_text(encoding="utf-8"))
    idx = int(sys.argv[1]) if len(sys.argv) > 1 else 0
    veces = int(sys.argv[2]) if len(sys.argv) > 2 else 1
    pitch = ej[idx]["pitch"]
    panel = [f["tiburon_id"] for f in recomendar(Pitch(**pitch)) if f["recomendado"]]
    sesiones.CARPETA = BASE / "sesiones" / "niveles"
    salida = []
    for nivel in ("facil", "medio", "dificil"):
        for k in range(veces):
            s = sesiones.Sesion(pitch, panel, "autopiloto", LLM(mock=False), id_=f"nivel_{nivel}_{k}", nivel=nivel)
            s.iniciar(esperar=True)
            preguntas = [e["texto"] for e in s.eventos if e["tipo"] == "PREGUNTA"]
            votos = [(e["emisor"].replace("tiburon_", ""), e.get("decision")) for e in s.eventos if e["tipo"] == "VOTO"]
            fila = {
                "negocio": pitch["nombre"], "nivel": nivel, "estado": s.estado, "error": s.error,
                "resultado": s.metricas()["resultado"], "votos": votos,
                "palabras_por_pregunta": round(sum(len(p.split()) for p in preguntas) / max(1, len(preguntas)), 1),
                "preguntas_con_tecnicismos": sum(any(t in p.lower() for t in TECNICOS) for p in preguntas),
                "preguntas": preguntas,
            }
            salida.append(fila)
            print(f"\n== {nivel.upper()} ({s.estado}) -> {fila['resultado']} | {fila['palabras_por_pregunta']} palabras/pregunta | "
                  f"{fila['preguntas_con_tecnicismos']}/9 con tecnicismos")
            print("   votos:", votos)
            for p in preguntas[:3]:
                print("   -", p)
            if s.error:
                print("   ERROR:", s.error)
    (BASE / "resultados_niveles.json").write_text(json.dumps(salida, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
