"""Imágenes de componentes: recortes de capturas de la escena y forma de onda de la música propia."""
import wave
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
from PIL import Image, ImageEnhance

AQUI = Path(__file__).resolve().parent
IMG = AQUI.parent / "img"
SONIDOS = AQUI.parents[1] / "unity" / "NeuralHubUnity" / "Assets" / "Resources" / "Sonidos"


def recorte(origen, caja, destino, brillo=1.0):
    im = Image.open(IMG / f"{origen}.jpg").convert("RGB").crop(caja)
    if brillo != 1.0:
        im = ImageEnhance.Brightness(im).enhance(brillo)
    im.save(IMG / f"{destino}.jpg", quality=92)


recorte("tiburon_2", (0, 0, 1600, 330), "comp_pantallas")
recorte("tiburon_2", (620, 575, 1100, 880), "comp_podio")
recorte("emprendedor", (560, 340, 1060, 860), "comp_emprendedor", 1.25)

nombres = ["musica", "cortinilla", "fanfarria", "hundimiento", "cierre"]
fig, ejes = plt.subplots(len(nombres), 1, figsize=(9, 4.6))
for eje, nombre in zip(ejes, nombres):
    with wave.open(str(SONIDOS / f"{nombre}.wav")) as w:
        datos = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16)[:: w.getnchannels()]
        dur = w.getnframes() / w.getframerate()
    eje.plot(np.linspace(0, dur, len(datos))[::20], datos[::20], lw=0.5, color="#4F1FD0")
    eje.set_yticks([])
    eje.set_ylabel(nombre, rotation=0, ha="right", va="center", color="#00165C", fontsize=11)
    eje.tick_params(labelsize=8)
    for lado in ("top", "right", "left"):
        eje.spines[lado].set_visible(False)
ejes[-1].set_xlabel("segundos", fontsize=9)
fig.tight_layout()
fig.savefig(IMG / "comp_sonido.jpg", dpi=170)
