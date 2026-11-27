"""La voz se pide por adelantado y a la vez desde el juego: nadie debe recibir un archivo a medio escribir."""

import uuid
import wave
from concurrent.futures import ThreadPoolExecutor

import pytest

from neuralhub import voz


def test_pedir_la_misma_voz_a_la_vez_entrega_siempre_el_audio_completo():
    pytest.importorskip("comtypes")
    texto = "Prueba simultánea de voz " + uuid.uuid4().hex + ". " + "Este audio es largo a propósito. " * 8

    def pedir(_):
        return voz.sintetizar(texto, "emprendedor").read_bytes()

    with ThreadPoolExecutor(max_workers=6) as ex:
        audios = list(ex.map(pedir, range(6)))
    assert len({len(a) for a in audios}) == 1 and len(audios[0]) > 50_000
    ruta = voz.sintetizar(texto, "emprendedor")
    with wave.open(str(ruta)) as w:
        assert w.getnframes() * 2 + 44 == ruta.stat().st_size
    assert not list(voz.CACHE.glob("*.parcial.wav"))
    ruta.unlink()
