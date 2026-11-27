"""Graba la sesión de ejemplo que Unity reproduce sin conexión (con el modelo real) y sus audios.

Uso: python generar_mock.py [indice_de_ejemplo] [nivel]   |   python generar_mock.py --solo-audio
Escribe StreamingAssets/mock_sesion.json y StreamingAssets/audio/<n>.wav
"""

import json
import shutil
import sys
from pathlib import Path

from neuralhub import sesiones, voz
from neuralhub.llm import LLM
from neuralhub.panel import recomendar
from neuralhub.pitch import Pitch

BASE = Path(__file__).parent
STREAMING = BASE.parent / "unity" / "NeuralHubUnity" / "Assets" / "StreamingAssets"


def grabar_audios(eventos):
    audio = STREAMING / "audio"
    audio.mkdir(exist_ok=True)
    nuevos = set()
    for e in eventos:
        if e["tipo"] == "REPORTE" or not e["texto"]:
            continue
        shutil.copy(voz.sintetizar(e["texto"], e["emisor"]), audio / f"{e['n']}.wav")
        nuevos.add(f"{e['n']}.wav")
    for p in [p for p in audio.glob("*.wav") if p.name not in nuevos]:
        p.unlink()
        Path(str(p) + ".meta").unlink(missing_ok=True)
    return len(nuevos)


def main():
    if sys.argv[1:] == ["--solo-audio"]:
        guardada = json.loads((STREAMING / "mock_sesion.json").read_text(encoding="utf-8"))
        print(grabar_audios(guardada["eventos"]), "audios")
        return
    ejemplos = json.loads((BASE / "pruebas" / "pitches_ejemplo.json").read_text(encoding="utf-8"))
    ej = ejemplos[int(sys.argv[1]) if len(sys.argv) > 1 else 2]
    nivel = sys.argv[2] if len(sys.argv) > 2 else ej["nivel"]
    panel = [f["tiburon_id"] for f in recomendar(Pitch(**ej["pitch"])) if f["recomendado"]]
    sesiones.CARPETA = BASE / "sesiones" / "mock"
    s = sesiones.Sesion(ej["pitch"], panel, "autopiloto", LLM(mock=False), id_="mock", nivel=nivel)
    s.iniciar(esperar=True)
    if s.estado != "terminada":
        sys.exit(f"La sesión no terminó: {s.estado} {s.error}")
    shutil.copy(s.guardar(), STREAMING / "mock_sesion.json")

    n_audios = grabar_audios(s.eventos)
    m = s.metricas()
    print(f"{ej['pitch']['nombre']} nivel {nivel}: {m['resultado']}, {len(s.eventos)} eventos, {n_audios} audios, "
          f"negociación={m['hubo_negociacion']}")


if __name__ == "__main__":
    main()
