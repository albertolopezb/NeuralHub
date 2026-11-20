"""LLM falso para probar el flujo del grafo sin gastar tokens (opción --mock)."""

from types import SimpleNamespace


class MockLLM:
    def __init__(self):
        self.preguntas = 0

    def invoke(self, mensajes):
        ultimo = mensajes[-1][1]
        if "Presenta tu pitch" in ultimo:
            return SimpleNamespace(content="Hola, tiburones. Les presento mi negocio y busco su inversión.")
        return SimpleNamespace(content="Respondo con los datos de mi ficha: vamos creciendo cada mes.")

    def with_structured_output(self, esquema):
        llm = self

        class _Salida:
            def invoke(self, mensajes):
                if esquema.__name__ == "Pregunta":
                    llm.preguntas += 1
                    return esquema(pregunta=f"Pregunta {llm.preguntas}: ¿cuál es tu margen por cliente?",
                                   tengo_suficiente=llm.preguntas > 2)
                return esquema(decision="contraoferta",
                               justificacion="Los márgenes aún no están probados, pero la tracción es real.",
                               contraoferta="300,000 USD por el 20 %")
        return _Salida()
