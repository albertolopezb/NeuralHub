"""Implementación preliminar del modelo (NeuralHub, TC2008B).

Un tiburón resuelve el problema: escucha el pitch del agente emprendedor,
le hace preguntas desde su criterio, evalúa las respuestas y emite un voto
justificado. Los dos agentes se comunican con los mensajes del modelo de
negociación (PITCH, PREGUNTA, RESPUESTA, VOTO, DECISION) y cada mensaje se
guarda como un evento con el mismo formato que después leerá Unity.

Uso:
    python prototipo_un_tiburon.py                       # idea ecotrack, tiburón financiero
    python prototipo_un_tiburon.py --idea greenplate --tiburon tiburona_consumo
    python prototipo_un_tiburon.py --mock                # sin LLM, para probar el flujo
"""

import argparse
import json
import operator
import os
import time
from pathlib import Path
from typing import Annotated, Literal, Optional, TypedDict

from langgraph.graph import END, START, StateGraph
from pydantic import BaseModel, Field

from tiburones import TIBURONES

BASE = Path(__file__).parent
MAX_RONDAS = 3


# ---------- Salidas estructuradas que pedimos al modelo ----------

class Pregunta(BaseModel):
    pregunta: str = Field(description="Una sola pregunta corta al emprendedor, desde tu criterio.")
    tengo_suficiente: bool = Field(
        description="True si ya tienes información suficiente para votar y no necesitas preguntar."
    )


class Voto(BaseModel):
    decision: Literal["invertir", "contraoferta", "salir"]
    justificacion: str = Field(description="Una o dos oraciones que mencionen tu criterio principal.")
    contraoferta: Optional[str] = Field(
        default=None, description="Solo si decision es contraoferta: monto y porcentaje que propones."
    )


# ---------- Estado compartido del grafo ----------

class Estado(TypedDict):
    idea: dict
    tiburon_id: str
    ronda: int
    eventos: Annotated[list, operator.add]   # cada nodo agrega sus eventos
    suficiente: bool
    voto: Optional[dict]
    llamadas: int


def evento(estado, tipo, emisor, destino, texto, extra=None):
    """Crea un evento con el formato que consume Unity."""
    e = {
        "n": len(estado["eventos"]) + 1,
        "ronda": estado["ronda"],
        "tipo": tipo,
        "emisor": emisor,
        "destino": destino,
        "texto": texto,
    }
    if extra:
        e.update(extra)
    return e


def historial(estado):
    return "\n".join(f"{e['emisor']} ({e['tipo']}): {e['texto']}" for e in estado["eventos"])


def ficha(idea):
    return (
        f"Nombre: {idea['nombre']}\nDescripción: {idea['descripcion']}\nMercado: {idea['mercado']}\n"
        f"Modelo de ingresos: {idea['modelo_ingresos']}\nTracción: {idea['traccion']}\n"
        f"Pide: {idea['monto']} por el {idea['porcentaje']} de la empresa\nUso de fondos: {idea['uso_fondos']}"
    )


# ---------- Modelo de lenguaje ----------

def crear_llm(mock: bool):
    if mock:
        from mock_llm import MockLLM
        return MockLLM()
    from dotenv import load_dotenv
    from langchain_openai import AzureChatOpenAI
    load_dotenv(BASE / ".env")
    return AzureChatOpenAI(
        azure_endpoint=os.environ["AZURE_ENDPOINT"],
        api_key=os.environ["AZURE_API_KEY"],
        azure_deployment=os.getenv("AZURE_DEPLOYMENT", "gpt-4.1-mini"),
        api_version=os.getenv("AZURE_API_VERSION", "2024-12-01-preview"),
        temperature=0.7,
    )


def construir_grafo(llm):
    def sistema_emprendedor(idea):
        return (
            "Eres el fundador de una startup en un programa tipo Shark Tank. Responde en español, "
            "con seguridad pero sin inventar datos que no estén en tu ficha. Máximo 90 palabras.\n"
            + ficha(idea)
        )

    def sistema_tiburon(t):
        return (
            f"Eres el {t['nombre']} de un panel de inversionistas. {t['perfil']} "
            f"Tu criterio principal es: {t['criterio']}. Hablas en español, eres directo y breve."
        )

    # Nodo 1: el emprendedor presenta su pitch
    def presentar_pitch(estado):
        r = llm.invoke([
            ("system", sistema_emprendedor(estado["idea"])),
            ("user", "Presenta tu pitch a los tiburones y termina pidiendo el monto y el porcentaje."),
        ])
        return {
            "eventos": [evento(estado, "PITCH", "emprendedor", "tiburones", r.content)],
            "llamadas": estado["llamadas"] + 1,
        }

    # Nodo 2: el tiburón pregunta (o decide que ya tiene suficiente)
    def preguntar(estado):
        t = TIBURONES[estado["tiburon_id"]]
        r = llm.with_structured_output(Pregunta).invoke([
            ("system", sistema_tiburon(t)),
            ("user", f"Conversación hasta ahora:\n{historial(estado)}\n\n"
                     f"Vas en la ronda {estado['ronda'] + 1} de {MAX_RONDAS}. Haz UNA pregunta sobre "
                     "el punto más débil del negocio según tu criterio, o indica que ya tienes suficiente."),
        ])
        cambios = {"llamadas": estado["llamadas"] + 1, "suficiente": r.tengo_suficiente}
        if not r.tengo_suficiente:
            cambios["eventos"] = [evento(estado, "PREGUNTA", estado["tiburon_id"], "emprendedor", r.pregunta)]
        return cambios

    # Nodo 3: el emprendedor responde
    def responder(estado):
        r = llm.invoke([
            ("system", sistema_emprendedor(estado["idea"])),
            ("user", f"Conversación:\n{historial(estado)}\n\nResponde la última pregunta del tiburón."),
        ])
        return {
            "eventos": [evento(estado, "RESPUESTA", "emprendedor", estado["tiburon_id"], r.content)],
            "ronda": estado["ronda"] + 1,
            "llamadas": estado["llamadas"] + 1,
        }

    # Nodo 4: el tiburón vota con justificación
    def votar(estado):
        t = TIBURONES[estado["tiburon_id"]]
        r = llm.with_structured_output(Voto).invoke([
            ("system", sistema_tiburon(t)),
            ("user", f"Conversación completa:\n{historial(estado)}\n\n"
                     "Emite tu voto: invertir, contraoferta o salir, con una justificación breve "
                     "que mencione tu criterio principal."),
        ])
        voto = r.model_dump()
        texto = voto["justificacion"] + (f" Contraoferta: {voto['contraoferta']}" if voto.get("contraoferta") else "")
        return {
            "voto": voto,
            "eventos": [evento(estado, "VOTO", estado["tiburon_id"], "nodo_votacion", texto,
                               {"decision": voto["decision"]})],
            "llamadas": estado["llamadas"] + 1,
        }

    # Nodo 5: regla de decisión (determinista, sin LLM)
    def decidir(estado):
        d = estado["voto"]["decision"]
        resultado = {"invertir": "TRATO", "contraoferta": "TRATO CON CONTRAOFERTA", "salir": "SIN TRATO"}[d]
        return {"eventos": [evento(estado, "DECISION", "nodo_votacion", "todos", resultado)]}

    # Aristas condicionales
    def despues_de_preguntar(estado):
        return "votar" if estado["suficiente"] else "responder"

    def despues_de_responder(estado):
        return "votar" if estado["ronda"] >= MAX_RONDAS else "preguntar"

    g = StateGraph(Estado)
    g.add_node("presentar_pitch", presentar_pitch)
    g.add_node("preguntar", preguntar)
    g.add_node("responder", responder)
    g.add_node("votar", votar)
    g.add_node("decidir", decidir)
    g.add_edge(START, "presentar_pitch")
    g.add_edge("presentar_pitch", "preguntar")
    g.add_conditional_edges("preguntar", despues_de_preguntar, ["responder", "votar"])
    g.add_conditional_edges("responder", despues_de_responder, ["preguntar", "votar"])
    g.add_edge("votar", "decidir")
    g.add_edge("decidir", END)
    return g.compile()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--idea", default="ecotrack")
    ap.add_argument("--tiburon", default="tiburon_financiero", choices=list(TIBURONES))
    ap.add_argument("--mock", action="store_true", help="Usa respuestas fijas en lugar del LLM")
    args = ap.parse_args()

    ideas = {i["id"]: i for i in json.loads((BASE / "ideas.json").read_text(encoding="utf-8"))}
    grafo = construir_grafo(crear_llm(args.mock))

    inicio = time.time()
    final = grafo.invoke({
        "idea": ideas[args.idea], "tiburon_id": args.tiburon, "ronda": 0,
        "eventos": [], "suficiente": False, "voto": None, "llamadas": 0,
    })
    segundos = time.time() - inicio

    for e in final["eventos"]:
        print(f"\n[{e['n']}] ronda {e['ronda']} | {e['tipo']} | {e['emisor']} -> {e['destino']}\n{e['texto']}")

    criterio = TIBURONES[args.tiburon]["criterio"]
    palabras = [p.strip() for p in criterio.replace(" y ", ",").split(",") if p.strip()]
    menciona = any(p.split()[0].lower() in final["voto"]["justificacion"].lower() for p in palabras)
    print("\n--- Métricas de la sesión ---")
    print(f"Llamadas al modelo: {final['llamadas']}")
    print(f"Rondas de preguntas: {final['ronda']}")
    print(f"Tiempo total: {segundos:.1f} s")
    print(f"La justificación menciona el criterio principal: {'sí' if menciona else 'no'}")

    carpeta = BASE / "sesiones"
    carpeta.mkdir(exist_ok=True)
    salida = carpeta / f"sesion_{args.idea}_{args.tiburon}_{int(inicio)}.json"
    salida.write_text(json.dumps({"eventos": final["eventos"], "llamadas": final["llamadas"],
                                  "segundos": round(segundos, 1)}, ensure_ascii=False, indent=2),
                      encoding="utf-8")
    print(f"Eventos guardados en {salida}")
    print("\nDiagrama del grafo (Mermaid):\n" + grafo.get_graph().draw_mermaid())


if __name__ == "__main__":
    main()
