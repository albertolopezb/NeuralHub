import io
import zipfile

import pytest
from fastapi.testclient import TestClient

from neuralhub import api, extraer


def docx(texto):
    b = io.BytesIO()
    with zipfile.ZipFile(b, "w") as z:
        z.writestr("word/document.xml", "".join(f"<w:p><w:t>{l}</w:t></w:p>" for l in texto.split("\n")))
    return b.getvalue()


LARGO = "ParkVision &amp; amigos\n" + "Detectamos cajones libres con las cámaras que la plaza ya tiene. " * 3


def test_lee_docx_y_texto():
    t = extraer.texto_de_archivo(docx(LARGO), "pitch.docx")
    assert t.startswith("ParkVision & amigos") and "cajones" in t
    assert "cajones" in extraer.texto_de_archivo(LARGO.encode("utf8"), "pitch.txt")


@pytest.mark.parametrize("datos,nombre", [(b"", "a.txt"), (b"corto", "a.txt"), (b"x" * 200, "a.exe"), (b"no soy zip" * 20, "a.docx")])
def test_rechaza_lo_que_no_sirve(datos, nombre):
    with pytest.raises(ValueError):
        extraer.texto_de_archivo(datos, nombre)


def test_limpiar_descarta_valores_invalidos_y_avisa_lo_que_falta():
    r = extraer.resultado({"nombre": "X", "sector": "otro", "porcentaje": 150, "monto_mxn": 1000, "problema": "palabra " * 90})
    assert "sector" not in r["pitch"] and "porcentaje" not in r["pitch"] and len(r["pitch"]["problema"]) <= 300
    assert "porcentaje ofrecido" in r["faltan"] and "competencia" in r["recomendados"]


def test_api_extraer(monkeypatch):
    monkeypatch.setattr(api.gestor, "mock", True)
    r = TestClient(api.app).post("/pitch/extraer?nombre=pitch.docx", content=docx(LARGO))
    assert r.status_code == 200 and r.json()["pitch"]["nombre"] and r.json()["faltan"]
