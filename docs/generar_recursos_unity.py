"""Genera recursos propios para Unity (sin licencias de terceros):
sonidos (síntesis con numpy), íconos por sector (PIL), textura de cáusticas y voces de la sesión de ejemplo."""
import json
import math
import sys
import wave
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

U = Path(r"D:\TEC\TC2008B\Reto\NeuralHub\unity\NeuralHubUnity\Assets")
SR = 44100
rng = np.random.default_rng(7)


def guardar_wav(ruta, x):
    x = np.clip(x, -1, 1)
    ruta.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(ruta), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((x * 32767).astype(np.int16).tobytes())


def env(n, a=0.01, r=0.3):
    t = np.arange(n) / SR
    e = np.minimum(1, t / max(a, 1e-4)) * np.exp(-t / r)
    return e


def burbuja(dur=0.18, f0=600, f1=1500):
    n = int(SR * dur); t = np.arange(n) / SR
    f = f0 + (f1 - f0) * (t / dur) ** 0.6
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * env(n, 0.005, 0.06)


S = U / "Resources" / "Sonidos"
# 1) ambiente submarino: ruido grave filtrado + burbujas aleatorias (loop de 20 s)
n = SR * 20
ruido = rng.normal(0, 1, n)
k = np.ones(400) / 400
grave = np.convolve(ruido, k, mode="same") * 6
lento = 0.6 + 0.4 * np.sin(2 * np.pi * np.arange(n) / SR / 7)
amb = grave * lento * 0.25
for _ in range(45):
    p = rng.integers(0, n - SR)
    b = burbuja(rng.uniform(0.08, 0.25), rng.uniform(300, 700), rng.uniform(900, 2200)) * rng.uniform(0.05, 0.18)
    amb[p:p + len(b)] += b
fade = int(SR * 0.5)
amb[:fade] *= np.linspace(0, 1, fade); amb[-fade:] *= np.linspace(1, 0, fade)
guardar_wav(S / "ambiente.wav", amb)
# 2) clic de interfaz
guardar_wav(S / "clic.wav", burbuja(0.07, 900, 1800) * 0.5)
# 3) campanita de voto
def tono(f, dur, amp=0.4):
    nn = int(SR * dur); t = np.arange(nn) / SR
    return (np.sin(2 * np.pi * f * t) + 0.3 * np.sin(2 * np.pi * 2 * f * t)) * env(nn, 0.005, dur / 3) * amp
guardar_wav(S / "voto.wav", np.concatenate([tono(880, 0.12), tono(1320, 0.25)]))
# 4) trato (arpegio ascendente) y 5) sin trato (descendente)
guardar_wav(S / "trato.wav", np.concatenate([tono(f, 0.16) for f in (523, 659, 784, 1047)] + [tono(1047, 0.6, 0.35)]))
guardar_wav(S / "sin_trato.wav", np.concatenate([tono(f, 0.22, 0.35) for f in (392, 349, 311)] + [tono(262, 0.7, 0.3)]))
# 6) aviso de turno (pregunta)
guardar_wav(S / "turno.wav", tono(740, 0.18, 0.3))
# 7) burbujas sueltas
guardar_wav(S / "burbujas.wav", np.concatenate([burbuja(rng.uniform(0.08, 0.2)) * 0.3 for _ in range(5)]))

# ---- íconos por sector (blancos sobre transparente, 256 px)
I = U / "Resources" / "Iconos"
I.mkdir(parents=True, exist_ok=True)
W = 256
def lienzo():
    im = Image.new("RGBA", (W, W), (0, 0, 0, 0)); return im, ImageDraw.Draw(im)
B = (255, 255, 255, 255)
iconos = {}
im, d = lienzo(); d.polygon([(140, 20), (60, 140), (120, 140), (100, 236), (196, 100), (136, 100), (160, 20)], fill=B); iconos["sustentabilidad_y_energia"] = im
im, d = lienzo(); d.rounded_rectangle([60, 60, 196, 196], 18, outline=B, width=14); d.rectangle([100, 100, 156, 156], fill=B)
for i in range(4):
    for s in (0, 1):
        x = 84 + i * 30
        d.rectangle([x, 30 if s == 0 else 196, x + 10, 60 if s == 0 else 226], fill=B)
        d.rectangle([30 if s == 0 else 196, x, 60 if s == 0 else 226, x + 10], fill=B)
iconos["tecnologia"] = im
im, d = lienzo(); d.ellipse([40, 70, 216, 230], outline=B, width=14); d.rectangle([70, 40, 82, 120], fill=B); d.rectangle([174, 40, 186, 120], fill=B); d.line([(128, 30), (128, 120)], fill=B, width=14); iconos["alimentos_y_bebidas"] = im
im, d = lienzo(); d.rectangle([104, 40, 152, 216], fill=B); d.rectangle([40, 104, 216, 152], fill=B); iconos["salud"] = im
im, d = lienzo(); d.polygon([(128, 50), (240, 100), (128, 150), (16, 100)], fill=B); d.rectangle([70, 120, 186, 190], fill=B); d.line([(230, 100), (230, 180)], fill=B, width=8); iconos["educacion"] = im
im, d = lienzo(); d.polygon([(80, 40), (128, 70), (176, 40), (236, 90), (200, 130), (186, 116), (186, 226), (70, 226), (70, 116), (56, 130), (20, 90)], fill=B); iconos["consumo_y_moda"] = im
im, d = lienzo(); d.rounded_rectangle([30, 80, 226, 216], 16, fill=B); d.rounded_rectangle([90, 44, 166, 90], 12, outline=B, width=14); d.rectangle([30, 130, 226, 142], fill=(0, 0, 0, 0)); iconos["servicios_profesionales"] = im
for k, im in iconos.items():
    im.save(I / f"{k}.png")

# ---- textura de cáusticas (cookie) 256 px, mosaico
X, Y = np.meshgrid(np.linspace(0, 2 * np.pi, 256), np.linspace(0, 2 * np.pi, 256))
c = np.zeros_like(X)
for a, b, ph in ((3, 2, 0.3), (2, 5, 1.7), (5, 3, 2.9), (4, 4, 0.9)):
    c += np.sin(a * X + b * Y + ph)
c = np.abs(np.sin(c * 1.3))
c = (1 - c) ** 3
c = (c - c.min()) / (c.max() - c.min())
img = Image.fromarray((180 + 75 * c).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(1.2))
(U / "Resources").mkdir(exist_ok=True)
img.save(U / "Resources" / "causticas.png")

# ---- voces de la sesión de ejemplo (para el modo sin conexión)
sys.path.insert(0, r"D:\TEC\TC2008B\Reto\NeuralHub\servicio")
from neuralhub.voz import sintetizar
ses = json.loads((U / "StreamingAssets" / "mock_sesion.json").read_text(encoding="utf-8"))
A = U / "StreamingAssets" / "audio"
A.mkdir(exist_ok=True)
for e in ses["eventos"]:
    if e["tipo"] == "REPORTE":
        continue
    src = sintetizar(e["texto"], e["emisor"])
    (A / f"{e['n']}.wav").write_bytes(src.read_bytes())
print("recursos ok:", len(list(S.glob('*.wav'))), "sonidos,", len(iconos), "íconos,", len(list(A.glob('*.wav'))), "voces")
