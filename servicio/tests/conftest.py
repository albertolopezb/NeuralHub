import pytest

from neuralhub import sesiones


@pytest.fixture(autouse=True)
def carpeta_temporal(tmp_path, monkeypatch):
    """Las sesiones de prueba se guardan en una carpeta temporal, no en el historial real."""
    monkeypatch.setattr(sesiones, "CARPETA", tmp_path)
