"""Voz: texto a voz con la voz de Windows (SAPI, es-MX) y voz a texto con Whisper local (faster-whisper).

Ambos corren en la computadora del servicio, sin costo y sin enviar audio a terceros.
"""

import hashlib
import math
import os
import re
import threading
from pathlib import Path

BASE = Path(__file__).resolve().parent.parent
CACHE = BASE / "audio_cache"

# Voz de cada personaje: (voz de Windows es-MX, velocidad -10..10, timbre, graves, eco).
# «timbre» cambia a la vez tono y formantes (como un tamaño distinto de personaje): < 1 más grande y grave, > 1 más
# joven y agudo; la velocidad se compensa para que nadie hable más rápido o lento por eso. «graves» refuerza los
# bajos y «eco» agrega una reverberación corta. Se usan las dos voces en español de México que trae Windows.
PERFIL_VOZ = {
    "tiburon_financiero": ("Raul", 0, 0.93, 0.25, 0.00),     # sobrio y seco
    "tiburon_estratega": ("Sabina", -1, 0.92, 0.20, 0.05),   # serena, más grave
    "tiburon_tecnologico": ("Raul", 2, 1.13, 0.00, 0.00),    # joven y acelerado
    "tiburona_consumo": ("Sabina", 1, 1.09, 0.00, 0.00),     # alegre y ágil
    "tiburon_consumo": ("Sabina", 1, 1.09, 0.00, 0.00),
    "tiburon_mentor": ("Raul", -2, 0.86, 0.45, 0.12),        # mayor, pausado y cálido
    "tiburon_negociador": ("Raul", 0, 0.81, 0.60, 0.07),     # grave e imponente
    "emprendedor": ("Raul", 1, 1.05, 0.00, 0.00),
    "moderador": ("Sabina", 0, 1.00, 0.10, 0.10),
    "nodo_votacion": ("Sabina", 0, 1.00, 0.10, 0.10),
    "servicio": ("Sabina", 0, 1.00, 0.00, 0.00),
}
MAX_CHARS_VOZ = 3000
# Las voces «OneCore» de Windows (entre ellas Raúl) están en una categoría aparte de las clásicas de SAPI
CATEGORIAS = (r"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Voices",
              r"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech\Voices")


def _elegir_voz(comtypes_client, nombre: str):
    """Busca la voz pedida; si no está instalada, cualquier voz en español de México."""
    respaldo = None
    for ruta in CATEGORIAS:
        try:
            categoria = comtypes_client.CreateObject("SAPI.SpObjectTokenCategory")
            categoria.SetId(ruta, False)
            fichas = categoria.EnumerateTokens()
            for i in range(fichas.Count):
                v = fichas.Item(i)
                descripcion = v.GetDescription()
                if nombre in descripcion:
                    return v
                if respaldo is None and ("Sabina" in descripcion or "Mexico" in descripcion or "México" in descripcion):
                    respaldo = v
        except OSError:
            continue
    return respaldo


_lock_tts = threading.Lock()
_lock_stt = threading.Lock()
_modelo = None


def _limpiar(texto: str) -> str:
    t = re.sub(r"[<>&]", " ", texto)
    return t[:MAX_CHARS_VOZ]


def sintetizar(texto: str, emisor: str) -> Path:
    """Genera (o reutiliza) un WAV con la línea del personaje."""
    CACHE.mkdir(exist_ok=True)
    nombre, rate, timbre, graves, eco = PERFIL_VOZ.get(emisor, ("Sabina", 0, 1.0, 0.0, 0.0))
    # en SAPI, +10 de velocidad es hablar 3 veces más rápido: se compensa lo que el timbre acelera o frena
    rate_sapi = max(-10, min(10, round(rate - 10 * math.log(timbre) / math.log(3))))
    clave = hashlib.sha1(f"{emisor}|{nombre}|{rate}|{timbre}|{graves}|{eco}|{texto}".encode("utf8")).hexdigest()[:20]
    ruta = CACHE / f"{clave}.wav"
    if ruta.exists():          # solo existe cuando ya está completo (se publica con un cambio de nombre)
        return ruta
    with _lock_tts:
        if ruta.exists():      # otro hilo lo terminó mientras esperábamos
            return ruta
        # Se escribe en un archivo temporal: si se entregara a medio escribir, Unity recibe el audio cortado.
        parcial = ruta.with_name(ruta.stem + ".parcial.wav")
        import comtypes
        import comtypes.client
        comtypes.CoInitialize()
        try:
            voz = comtypes.client.CreateObject("SAPI.SpVoice")
            elegida = _elegir_voz(comtypes.client, nombre)
            if elegida is not None:
                voz.Voice = elegida
            stream = comtypes.client.CreateObject("SAPI.SpFileStream")
            fmt = comtypes.client.CreateObject("SAPI.SpAudioFormat")
            fmt.Type = 22  # SAFT22kHz16BitMono
            stream.Format = fmt
            stream.Open(str(parcial), 3)  # SSFMCreateForWrite
            voz.AudioOutputStream = stream
            voz.Rate = rate_sapi
            voz.Speak(_limpiar(texto), 0)
            stream.Close()
        finally:
            comtypes.CoUninitialize()
        _personalizar(parcial, timbre, graves, eco)
        os.replace(parcial, ruta)
    return ruta


def _personalizar(ruta: Path, timbre: float, graves: float, eco: float):
    """Aplica el timbre, los graves y el eco del personaje al WAV generado."""
    import wave

    import numpy as np
    with wave.open(str(ruta), "rb") as w:
        sr = w.getframerate()
        x = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).astype(np.float32)
    if len(x) < 2:
        return
    if timbre != 1.0:
        x = np.interp(np.arange(0, len(x) - 1, timbre), np.arange(len(x)), x).astype(np.float32)
    if graves > 0:
        ancho = int(sr / 320)                       # pasa-bajas sencillo (media móvil)
        bajos = np.convolve(x, np.ones(ancho) / ancho, mode="same")
        x = x + graves * bajos
    if eco > 0:
        salida = np.concatenate([x, np.zeros(int(sr * 0.25), dtype=np.float32)])
        for retardo, fuerza in ((0.047, 1.0), (0.083, 0.6), (0.131, 0.35)):
            d = int(sr * retardo)
            salida[d:d + len(x)] += eco * fuerza * x
        x = salida
    x = x * min(1.0, 31000.0 / (np.max(np.abs(x)) + 1e-9))
    with wave.open(str(ruta), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(x.astype(np.int16).tobytes())


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


def cargar_modelo():
    """Carga Whisper una sola vez (al arrancar el servicio, para que el primer dictado no tarde de más)."""
    global _modelo
    with _lock_stt:
        if _modelo is None:
            from faster_whisper import WhisperModel
            _modelo = WhisperModel("base", device="cpu", compute_type="int8")
    return _modelo


def _normalizar(x):
    """Sube el volumen de una grabación baja: sin esto, hablar suave hacía que se descartara como silencio."""
    import numpy as np
    if len(x) == 0:
        return x
    pico = float(np.percentile(np.abs(x), 99.5))
    return x if pico < 0.002 else np.clip(x * (0.7 / pico), -1.0, 1.0).astype(np.float32)


def transcribir(ruta_wav: Path) -> dict:
    """Voz a texto en español con Whisper (modelo 'base', CPU)."""
    modelo = cargar_modelo()
    audio = _normalizar(_leer_wav_16k(ruta_wav))
    with _lock_stt:
        def pasada(vad):
            segmentos, info = modelo.transcribe(audio, language="es", beam_size=2, condition_on_previous_text=False,
                                                temperature=[0.0, 0.4],   # pocos reintentos: un audio difícil no debe tardar minutos
                                                vad_filter=vad, vad_parameters={"threshold": 0.25, "min_silence_duration_ms": 900} if vad else None)
            return " ".join(s.text.strip() for s in segmentos).strip(), info

        texto, info = pasada(True)
        if not texto:            # el detector de voz descartó todo: se intenta sin él
            texto, info = pasada(False)
    return {"texto": texto, "duracion_s": round(info.duration, 1)}
