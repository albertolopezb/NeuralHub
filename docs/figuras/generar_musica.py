"""Música original del programa, sintetizada con numpy: tema de fondo en bucle y cortinilla de entrada.

Salida: unity/NeuralHubUnity/Assets/Resources/Sonidos/musica.wav y cortinilla.wav
"""
import wave
from pathlib import Path

import numpy as np

SR = 32000
SALIDA = Path(__file__).resolve().parents[2] / "unity" / "NeuralHubUnity" / "Assets" / "Resources" / "Sonidos"
rng = np.random.default_rng(4)


def hz(nota):      # número MIDI -> Hz
    return 440.0 * 2 ** ((nota - 69) / 12)


def tono(f, dur, ataque=0.01, caida=2.5, armonicos=((1, 1.0), (2, 0.28), (3, 0.1))):
    t = np.arange(int(dur * SR)) / SR
    x = sum(a * np.sin(2 * np.pi * f * k * t) for k, a in armonicos)
    env = np.minimum(1, t / ataque) * np.exp(-caida * t) * np.minimum(1, (dur - t) / 0.03)
    return x * env


def poner(pista, inicio, sonido, vol):
    i = int(inicio * SR)
    n = len(pista)
    idx = (i + np.arange(len(sonido))) % n      # lo que sobra vuelve al principio: bucle sin cortes
    np.add.at(pista, idx, sonido * vol)


def golpe(dur=0.22):
    t = np.arange(int(dur * SR)) / SR
    return np.sin(2 * np.pi * (95 * np.exp(-14 * t) + 45) * t) * np.exp(-16 * t)


def sacudida(dur=0.07):
    t = np.arange(int(dur * SR)) / SR
    r = rng.standard_normal(len(t))
    return np.diff(r, prepend=0) * np.exp(-55 * t)


def guardar(nombre, x, pico=0.8):
    x = x / (np.max(np.abs(x)) + 1e-9) * pico
    with wave.open(str(SALIDA / f"{nombre}.wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((x * 32767).astype(np.int16).tobytes())


def tema():
    bpm = 96
    b = 60 / bpm
    acordes = [[60, 64, 67, 71], [57, 60, 64, 67], [53, 57, 60, 64], [55, 59, 62, 65],
               [60, 64, 67, 71], [52, 55, 59, 62], [53, 57, 60, 64], [55, 59, 62, 69]]
    compases = len(acordes) * 2
    pista = np.zeros(int(compases * 4 * b * SR))
    melodia = [76, None, 79, 76, 74, None, 72, None, 71, None, 74, 76, 72, None, None, None]
    for c in range(compases):
        ac = acordes[c % len(acordes)]
        t0 = c * 4 * b
        for n in ac:                                    # colchón suave
            poner(pista, t0, tono(hz(n), 4 * b, 0.35, 0.35, ((1, 1.0), (2, 0.12))), 0.075)
        for k in range(8):                              # arpegio tipo marimba
            n = ac[[0, 2, 1, 3, 2, 1, 3, 2][k]] + 12
            poner(pista, t0 + k * b / 2, tono(hz(n), 0.5, 0.004, 9, ((1, 1.0), (4, 0.18))), 0.12 if k % 2 == 0 else 0.08)
        for k, paso in enumerate([0, 1.5, 2, 3]):       # bajo
            poner(pista, t0 + paso * b, tono(hz(ac[0] - 24 + (7 if k == 3 else 0)), b * 0.9, 0.008, 2.4, ((1, 1.0), (2, 0.25))), 0.24)
        for k in range(4):
            if k in (0, 2):
                poner(pista, t0 + k * b, golpe(), 0.22)
            poner(pista, t0 + (k + 0.5) * b, sacudida(), 0.05)
        if c >= compases // 2:                          # segunda mitad: aparece la melodía
            for k in range(4):
                n = melodia[((c % 4) * 4 + k)]
                if n:
                    poner(pista, t0 + k * b, tono(hz(n), b * 1.3, 0.02, 2.8, ((1, 1.0), (2, 0.2), (3, 0.05))), 0.11)
        if c % 4 == 3:                                  # burbuja
            f = np.linspace(500, 1400, int(0.14 * SR))
            s = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-np.linspace(0, 5, len(f)))
            poner(pista, t0 + 3.5 * b, s, 0.06)
    guardar("musica", pista, 0.7)


def cortinilla():
    dur = 7.5
    pista = np.zeros(int((dur + 1.5) * SR))
    for k, n in enumerate([60, 64, 67, 72, 76, 79, 84]):       # subida
        poner(pista, 0.25 + k * 0.17, tono(hz(n), 0.7, 0.005, 5, ((1, 1.0), (2, 0.35), (4, 0.15))), 0.22)
    metal = ((1, 1.0), (2, 0.55), (3, 0.36), (4, 0.22), (5, 0.12))
    for t0, acorde, largo in [(1.6, [48, 60, 64, 67, 72], 0.45), (2.25, [48, 60, 64, 67, 72], 0.3),
                              (2.7, [53, 65, 69, 72, 77], 0.9), (3.9, [55, 67, 71, 74, 79], 0.9), (5.1, [48, 60, 67, 76, 84], 2.3)]:
        for n in acorde:
            poner(pista, t0, tono(hz(n), largo, 0.025, 1.1, metal), 0.09)
        poner(pista, t0, golpe(0.4), 0.5)
    t = np.arange(int(1.6 * SR)) / SR
    platillo = np.diff(rng.standard_normal(len(t)), prepend=0) * np.exp(-2.4 * t)
    poner(pista, 5.1, platillo, 0.09)
    sube = np.diff(rng.standard_normal(int(1.2 * SR)), prepend=0) * np.linspace(0, 1, int(1.2 * SR)) ** 3
    poner(pista, 0.4, sube, 0.06)
    guardar("cortinilla", pista[: int(dur * SR)] * np.minimum(1, (dur - np.arange(int(dur * SR)) / SR) / 0.6), 0.85)


if __name__ == "__main__":
    SALIDA.mkdir(parents=True, exist_ok=True)
    tema()
    cortinilla()
    print("ok")
