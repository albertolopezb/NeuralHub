"""Graba la presentación hablada de cada tiburón («Conoce a los tiburones»). El texto vive en Scripts/Personas.cs.

Uso: python generar_presentaciones.py
"""

import re
import shutil
from pathlib import Path

from neuralhub import voz

BASE = Path(__file__).parent
UNITY = BASE.parent / "unity" / "NeuralHubUnity" / "Assets"


def main():
    codigo = (UNITY / "Scripts" / "Personas.cs").read_text(encoding="utf-8")
    pares = re.findall(r'id = "(tibur\w+)".*?presentacion = "([^"]+)"', codigo, re.S)
    destino = UNITY / "StreamingAssets" / "audio" / "presentaciones"
    destino.mkdir(parents=True, exist_ok=True)
    for tid, texto in pares:
        shutil.copy(voz.sintetizar(texto, tid), destino / f"{tid}.wav")
    print(len(pares), "presentaciones")


if __name__ == "__main__":
    main()
