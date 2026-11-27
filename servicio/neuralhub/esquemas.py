"""Salidas estructuradas que se piden al modelo de lenguaje."""

from typing import Literal, Optional

from pydantic import BaseModel, Field


class Pregunta(BaseModel):
    pregunta: str = Field(description="Una sola pregunta corta al emprendedor, desde tu criterio principal.")


class RespuestaItem(BaseModel):
    tiburon_id: str = Field(description="Id del tiburón al que se responde.")
    texto: str = Field(description="Respuesta del emprendedor a ese tiburón.")


class Respuestas(BaseModel):
    respuestas: list[RespuestaItem]


class Replica(BaseModel):
    postura: Literal["acuerdo", "desacuerdo"]
    texto: str = Field(description="Una o dos frases dirigidas al tiburón indicado: menciona su pregunta y di por qué coincides o no.")


class Voto(BaseModel):
    decision: Literal["invertir", "contraoferta", "salir"]
    justificacion: str = Field(description="Una o dos frases que mencionen tu criterio principal y un dato concreto.")
    contraoferta_monto_mxn: Optional[float] = Field(default=None, description="Solo si decision es contraoferta.")
    contraoferta_porcentaje: Optional[float] = Field(default=None, description="Solo si decision es contraoferta.")


class VotoFinal(BaseModel):
    decision: Literal["invertir", "salir"]
    justificacion: str = Field(description="Una o dos frases que mencionen tu criterio principal y un dato concreto.")


class CalificacionTiburon(BaseModel):
    tiburon_id: str
    calificacion: int = Field(description="De 1 a 5 en el criterio de ese tiburón.")
    frase_clave: str = Field(description="Frase de la conversación que justifica la calificación.")
    necesitaria: str = Field(description="Qué necesitaría ver para invertir.")


class Reporte(BaseModel):
    por_tiburon: list[CalificacionTiburon]
    fortalezas: list[str] = Field(description="Exactamente 3.")
    alertas: list[str] = Field(description="Exactamente 3.")
    preguntas_sin_responder: list[str]
    siguientes_pasos: list[str] = Field(description="Exactamente 3, concretos.")


class Sugerencia(BaseModel):
    texto: str = Field(description="Respuesta sugerida, en primera persona, que el emprendedor puede decir tal cual o ajustar.")
