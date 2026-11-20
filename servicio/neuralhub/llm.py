"""Envoltura del modelo de lenguaje: cuenta las llamadas y el tiempo, y permite un modo simulado (mock)."""

import os
import re
import threading
import time
from pathlib import Path

from . import esquemas as E

BASE = Path(__file__).resolve().parent.parent


class LLM:
    """Cada llamada al modelo (texto o estructurada) suma 1 a `llamadas`. Es seguro entre hilos."""

    def __init__(self, mock: bool = False, mock_votos: dict | None = None, mock_votos2: dict | None = None):
        self.mock = mock
        self.llamadas = 0
        self.segundos = 0.0
        self._lock = threading.Lock()
        if mock:
            self._cliente = MockLLM(mock_votos, mock_votos2)
        else:
            from dotenv import load_dotenv
            from langchain_openai import AzureChatOpenAI
            load_dotenv(BASE / ".env")
            self._cliente = AzureChatOpenAI(
                azure_endpoint=os.environ["AZURE_ENDPOINT"],
                api_key=os.environ["AZURE_API_KEY"],
                azure_deployment=os.getenv("AZURE_DEPLOYMENT", "gpt-4.1-mini"),
                api_version=os.getenv("AZURE_API_VERSION", "2024-12-01-preview"),
                temperature=0.7,
                timeout=60,
                max_retries=2,
            )

    def _contar(self, t0):
        with self._lock:
            self.llamadas += 1
            self.segundos += time.time() - t0

    def texto(self, system: str, user: str) -> str:
        t0 = time.time()
        try:
            if self.mock:
                return self._cliente.texto(system, user)
            return self._cliente.invoke([("system", system), ("user", user)]).content
        finally:
            self._contar(t0)

    def estructurado(self, esquema, system: str, user: str):
        t0 = time.time()
        try:
            if self.mock:
                return self._cliente.estructurado(esquema, system, user)
            return self._cliente.with_structured_output(esquema).invoke([("system", system), ("user", user)])
        finally:
            self._contar(t0)


class MockLLM:
    """Respuestas fijas pero coherentes con el formato de los prompts. No gasta tokens."""

    VOTOS = {
        "tiburon_financiero": "contraoferta", "tiburon_estratega": "invertir", "tiburon_tecnologico": "salir",
        "tiburona_consumo": "invertir", "tiburon_mentor": "invertir", "tiburon_negociador": "contraoferta",
    }

    def __init__(self, votos=None, votos2=None):
        self.votos = dict(self.VOTOS, **(votos or {}))
        self.votos2 = votos2 or {}
        self.n = 0

    @staticmethod
    def _id(system):
        m = re.search(r"\[id:(\w+)\]", system)
        return m.group(1) if m else "tiburon_financiero"

    def texto(self, system, user):
        return "Texto simulado."

    def estructurado(self, esquema, system, user):
        tid = self._id(system)
        panel = re.search(r"PANEL: ([\w, ]+)", user)
        panel = [p.strip() for p in panel.group(1).split(",")] if panel else [tid]
        self.n += 1
        if esquema is E.Pregunta:
            return E.Pregunta(pregunta=f"¿Cuál es tu dato más sólido sobre mi criterio, pregunta {self.n}?")
        if esquema is E.Respuestas:
            ids = re.findall(r"^\[(\w+)\]", user, flags=re.M)
            return E.Respuestas(respuestas=[E.RespuestaItem(tiburon_id=i, texto="Respondo con los datos de mi ficha.")
                                            for i in ids])
        if esquema is E.Replica:
            return E.Replica(postura="desacuerdo" if self.n % 2 else "acuerdo",
                             texto=f"Sobre su pregunta: {'no coincido' if self.n % 2 else 'coincido'} con su duda.")
        if esquema is E.Voto:
            d = self.votos[tid]
            m = re.search(r"OFERTA ACTUAL: monto=([\d.]+) porcentaje=([\d.]+)", user)
            monto, pct = (float(m.group(1)), float(m.group(2))) if m else (300000.0, 10.0)
            return E.Voto(decision=d, justificacion="Mi criterio principal (margen y valuación) justifica este voto.",
                          contraoferta_monto_mxn=monto if d == "contraoferta" else None,
                          contraoferta_porcentaje=pct + 5 if d == "contraoferta" else None)
        if esquema is E.VotoFinal:
            d = self.votos2.get(tid, "invertir" if self.votos[tid] != "salir" else "salir")
            return E.VotoFinal(decision=d, justificacion="Con la nueva oferta, mi criterio de valuación se cumple." if d == "invertir"
                               else "Sigue sin cumplirse mi criterio principal de valuación.")
        if esquema is E.Reporte:
            return E.Reporte(
                por_tiburon=[E.CalificacionTiburon(tiburon_id=p, calificacion=3, frase_clave="frase", necesitaria="más datos")
                             for p in panel],
                fortalezas=["a", "b", "c"], alertas=["a", "b", "c"], preguntas_sin_responder=[],
                siguientes_pasos=["a", "b", "c"])
        raise ValueError(esquema)
