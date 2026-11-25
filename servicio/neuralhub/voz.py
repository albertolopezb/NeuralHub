"""Voz: texto a voz con la voz de Windows (SAPI, es-MX) y voz a texto con Whisper local (faster-whisper).

Ambos corren en la computadora del servicio, sin costo y sin enviar audio a terceros.
"""

import hashlib
import re
import threading
from pathlib import Path

BASE = Path(__file__).resolve().parent.parent
CACHE = BASE / "audio_cache"

# Cada personaje con su velocidad (-10..10) y tono (-10..10) para distinguirlos con la misma voz.
PERFIL_VOZ = {
    "tiburon_financiero": (0, -6),
    "tiburon_estratega": (-1, -3),
    "tiburon_tecnologico": (2, 2),
    "tiburona_consumo": (1, 6),
    "tiburon_mentor": (-2, -1),
    "tiburon_negociador": (1, -8),
    "emprendedor": (1, 4),
    "moderador": (0, 0),
    "nodo_votacion": (0, 0),
    "servicio": (0, 0),
}

_lock_tts = threading.Lock()
_lock_stt = threading.Lock()
_modelo = None


def _limpiar(texto: str) -> str:
    t = re.sub(r"[<>&]", " ", texto)
    return t[:900]


def sintetizar(texto: str, emisor: str) -> Path:
    """Genera (o reutiliza) un WAV con la línea del personaje."""
    CACHE.mkdir(exist_ok=True)
    rate, pitch = PERFIL_VOZ.get(emisor, (0, 0))
    clave = hashlib.sha1(f"{emisor}|{rate}|{pitch}|{texto}".encode("utf8")).hexdigest()[:20]
    ruta = CACHE / f"{clave}.wav"
    if ruta.exists():
        return ruta
    with _lock_tts:
        import comtypes
        import comtypes.client
        comtypes.CoInitialize()
        try:
            voz = comtypes.client.CreateObject("SAPI.SpVoice")
            for v in voz.GetVoices():
                if "es-MX" in v.GetAttribute("Language") or "080A" in v.GetAttribute("Language") or "Sabina" in v.GetDescription():
                    voz.Voice = v
                    break
            stream = comtypes.client.CreateObject("SAPI.SpFileStream")
            fmt = comtypes.client.CreateObject("SAPI.SpAudioFormat")
            fmt.Type = 22  # SAFT22kHz16BitMono
            stream.Format = fmt
            stream.Open(str(ruta), 3)  # SSFMCreateForWrite
            voz.AudioOutputStream = stream
            voz.Rate = rate
            voz.Speak(f'<pitch absmiddle="{pitch}">{_limpiar(texto)}</pitch>', 8)  # SVSFIsXML
            stream.Close()
        finally:
            comtypes.CoUninitialize()
    return ruta


def _leer_wav_16k(ruta):
    """Lee un WAV PCM de 16 bits y lo convierte a mono, 16 kHz, float32 (lo que espera Whisper)."""
    import wave

    import numpy as np
    with wave.open(str(ruta), "rb") as w:
        sr, nch, ancho = w.getframerate(), w.getnchannels(), w.getsampwidth()
        datos = w.readframes(w.getnframes())
    if ancho != 2:
        raise ValueError("Se espera WAV PCM de 16 bits")
    x = np.frombuffer(datos, dtype=np.int16).astype(np.float32) / 32768.0
    if nch > 1:
        x = x.reshape(-1, nch).mean(axis=1)
    if sr != 16000:
        n = int(len(x) * 16000 / sr)
        x = np.interp(np.linspace(0, len(x) - 1, n), np.arange(len(x)), x).astype(np.float32)
    return x


def transcribir(ruta_wav: Path) -> dict:
    """Voz a texto en español con Whisper (modelo 'base', CPU)."""
    global _modelo
    with _lock_stt:
        if _modelo is None:
            from faster_whisper import WhisperModel
            _modelo = WhisperModel("base", device="cpu", compute_type="int8")
        segmentos, info = _modelo.transcribe(_leer_wav_16k(ruta_wav), language="es", vad_filter=True)
        texto = " ".join(s.text.strip() for s in segmentos).strip()
    return {"texto": texto, "duracion_s": round(info.duration, 1)}
