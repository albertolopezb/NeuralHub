"""Une los cuadros grabados por Unity con el audio (voces, efectos y ambiente) según audio_log.txt.

    python montar_video.py <carpeta_cuadros> <salida.mp4> [--mock]
"""
import json
import subprocess
import sys
import wave
from pathlib import Path

import imageio_ffmpeg
import numpy as np

sys.path.insert(0, r"D:\TEC\TC2008B\Reto\NeuralHub\servicio")
from neuralhub.voz import sintetizar  # noqa: E402

FPS, SR = 12, 44100
U = Path(r"D:\TEC\TC2008B\Reto\NeuralHub\unity\NeuralHubUnity\Assets")
SERV = Path(r"D:\TEC\TC2008B\Reto\NeuralHub\servicio")


def leer(ruta):
    with wave.open(str(ruta), "rb") as w:
        sr, ch = w.getframerate(), w.getnchannels()
        x = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).astype(np.float32) / 32768
    if ch > 1:
        x = x.reshape(-1, ch).mean(axis=1)
    if sr != SR:
        n = int(len(x) * SR / sr)
        x = np.interp(np.linspace(0, len(x) - 1, n), np.arange(len(x)), x)
    return x


def main():
    carpeta, salida = Path(sys.argv[1]), Path(sys.argv[2])
    mock = "--mock" in sys.argv
    total = int((carpeta / "listo.txt").read_text().strip())
    pista = np.zeros(int(total / FPS * SR) + SR * 8, dtype=np.float32)
    amb = leer(U / "Resources" / "Sonidos" / "ambiente.wav") * 0.25
    for i in range(0, len(pista), len(amb)):
        pista[i:i + len(amb)] += amb[: len(pista) - i]
    if mock:
        eventos = {e["n"]: e for e in json.loads((U / "StreamingAssets" / "mock_sesion.json").read_text(encoding="utf-8"))["eventos"]}
    else:
        sid = (carpeta / "sesion.txt").read_text().strip()
        eventos = {e["n"]: e for e in json.loads((SERV / "sesiones" / f"sesion_{sid}.json").read_text(encoding="utf-8"))["eventos"]}
    log = (carpeta / "audio_log.txt").read_text(encoding="utf-8").splitlines() if (carpeta / "audio_log.txt").exists() else []
    voces = []
    for linea in log:
        f, tipo, dato = linea.split("|")
        ini = int(int(f) / FPS * SR)
        if tipo == "efecto":
            x = leer(U / "Resources" / "Sonidos" / f"{dato}.wav") * 0.6
        else:
            e = eventos[int(dato)]
            x = leer(U / "StreamingAssets" / "audio" / f"{dato}.wav") if mock else leer(sintetizar(e["texto"], e["emisor"]))
            voces.append((ini, len(x)))
        fin = min(len(pista), ini + len(x))
        pista[ini:fin] += x[: fin - ini]
    pista = np.clip(pista / max(1.0, np.abs(pista).max() * 1.05), -1, 1)
    wav = carpeta / "pista.wav"
    with wave.open(str(wav), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((pista * 32767).astype(np.int16).tobytes())
    ff = imageio_ffmpeg.get_ffmpeg_exe()
    subprocess.check_call([ff, "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", str(carpeta / "f%05d.png"), "-i", str(wav),
                           "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "26", "-c:a", "aac", "-b:a", "128k", "-shortest",
                           "-movflags", "+faststart", str(salida)])
    print(f"{salida} listo: {total / FPS:.0f} s, {len(voces)} voces")


if __name__ == "__main__":
    main()
