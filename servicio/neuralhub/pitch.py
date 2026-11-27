"""Formulario del pitch y cálculos numéricos (en Python, nunca en el modelo)."""

from typing import Optional

from pydantic import BaseModel, Field

SECTORES = [
    "tecnologia", "alimentos_y_bebidas", "consumo_y_moda", "salud", "educacion",
    "sustentabilidad_y_energia", "servicios_profesionales",
]
ETAPAS = ["idea", "prototipo", "primeras_ventas", "crecimiento"]

# Etiquetas legibles (para motivos y para el reporte).
ETIQUETAS = {
    "nombre": "nombre del negocio", "sector": "sector", "etapa": "etapa",
    "problema": "problema", "solucion": "solución", "mercado_objetivo": "mercado objetivo",
    "modelo_ingresos": "modelo de ingresos", "competencia": "competencia",
    "diferenciador": "diferenciador", "validacion": "validación con clientes",
    "propiedad_intelectual": "patente o marca registrada", "equipo_fundadores": "número de fundadores",
    "equipo_roles": "roles del equipo", "equipo_dedicacion": "dedicación del equipo",
    "equipo_experiencia": "experiencia del equipo", "monto_mxn": "monto solicitado",
    "porcentaje": "porcentaje ofrecido", "uso_fondos": "uso de los fondos",
    "ventas_12m_mxn": "ventas de los últimos 12 meses", "margen_bruto_pct": "margen bruto",
    "gasto_mensual_mxn": "gasto mensual",
}

NO_SE = {"", "aún no lo sé", "aun no lo se", "no lo sé", "no lo se"}
MIN_CHARS = 15  # un texto más corto que esto se considera vacío en la recomendación


class Pitch(BaseModel):
    """Campos del formulario. Los opcionales aceptan None (= «Aún no lo sé»)."""

    # obligatorios
    nombre: str
    sector: str
    etapa: str
    problema: str
    solucion: str
    mercado_objetivo: str
    modelo_ingresos: str
    monto_mxn: float = Field(gt=0)
    porcentaje: float = Field(gt=0, le=100)
    uso_fondos: str
    # equipo (obligatorio el número de fundadores; el resto puede quedar vacío)
    equipo_fundadores: Optional[int] = None
    equipo_roles: Optional[str] = None
    equipo_dedicacion: Optional[str] = None  # "tiempo completo" | "parcial"
    equipo_experiencia: Optional[str] = None
    # opcionales
    competencia: Optional[str] = None
    diferenciador: Optional[str] = None
    validacion: Optional[str] = None
    propiedad_intelectual: Optional[str] = None  # patente | marca | ninguna
    ventas_12m_mxn: Optional[float] = None
    margen_bruto_pct: Optional[float] = None
    gasto_mensual_mxn: Optional[float] = None
    # cómo lo cuenta en voz alta (opcional; si falta se arma con los campos de arriba)
    discurso: Optional[str] = None


OBLIGATORIOS = ["nombre", "sector", "etapa", "problema", "solucion", "mercado_objetivo",
                "modelo_ingresos", "monto_mxn", "porcentaje", "uso_fondos", "equipo_fundadores"]


def es_hueco(pitch: Pitch, campo: str) -> bool:
    """Un campo es un hueco si está vacío, dice «Aún no lo sé» o es demasiado corto para evaluarse."""
    v = getattr(pitch, campo)
    if v is None:
        return True
    if isinstance(v, str):
        t = v.strip().lower()
        if t in NO_SE:
            return True
        if campo in ("nombre", "sector", "etapa", "propiedad_intelectual", "equipo_dedicacion"):
            return False
        return len(t) < MIN_CHARS
    return False


def faltan_obligatorios(pitch_dict: dict) -> list[str]:
    return [c for c in OBLIGATORIOS if pitch_dict.get(c) in (None, "")]


def valuacion_mxn(pitch: Pitch) -> float:
    """Valuación implícita = monto entre porcentaje (post-money)."""
    return round(pitch.monto_mxn / (pitch.porcentaje / 100.0), 2)


def mxn(x: float) -> str:
    return f"${x:,.0f} MXN"


def _frase(texto: str) -> str:
    t = (texto or "").strip()
    return t if not t or t[-1] in ".!?" else t + "."


def discurso(pitch: Pitch) -> str:
    """El pitch contado como en un programa de inversionistas: saludo, problema, solución, a quién le vende,
    cómo gana dinero, avance y, al final, la petición. Sin modelo: solo usa lo que escribió el emprendedor."""
    if pitch.discurso and pitch.discurso.strip():
        return pitch.discurso.strip()
    partes = [f"¡Hola, tiburones! Hoy les quiero presentar {pitch.nombre}.",
              "Todo empezó con un problema.", _frase(pitch.problema),
              "Por eso creamos nuestra solución.", _frase(pitch.solucion),
              "Está pensada para este mercado:", _frase(pitch.mercado_objetivo),
              "Así ganamos dinero:", _frase(pitch.modelo_ingresos)]
    if pitch.validacion and not es_hueco(pitch, "validacion"):
        partes += ["Y no es solo una idea:", _frase(pitch.validacion)]
    partes += [f"Por eso hoy venimos a pedirles {mxn(pitch.monto_mxn)} a cambio del {pitch.porcentaje:g} % de nuestra empresa, "
               "para lo siguiente:", _frase(pitch.uso_fondos), "¿Quién quiere nadar con nosotros?"]
    return " ".join(partes)


def ficha(pitch: Pitch) -> str:
    """Texto del pitch que ven los tiburones. Incluye los cálculos hechos en Python."""

    def v(campo):
        val = getattr(pitch, campo)
        return "no proporcionado" if val is None or (isinstance(val, str) and not val.strip()) else val

    ventas = f"{mxn(pitch.ventas_12m_mxn)}" if pitch.ventas_12m_mxn is not None else "no proporcionado"
    gasto = f"{mxn(pitch.gasto_mensual_mxn)}" if pitch.gasto_mensual_mxn is not None else "no proporcionado"
    margen = f"{pitch.margen_bruto_pct} %" if pitch.margen_bruto_pct is not None else "no proporcionado"
    return "\n".join([
        f"Negocio: {pitch.nombre} (sector: {pitch.sector}; etapa: {pitch.etapa})",
        f"Problema: {pitch.problema}",
        f"Solución: {pitch.solucion}",
        f"Mercado objetivo: {pitch.mercado_objetivo}",
        f"Modelo de ingresos: {pitch.modelo_ingresos}",
        f"Competencia: {v('competencia')}",
        f"Diferenciador: {v('diferenciador')}",
        f"Validación con clientes: {v('validacion')}",
        f"Patente o marca registrada: {v('propiedad_intelectual')}",
        f"Equipo: {v('equipo_fundadores')} fundador(es); roles: {v('equipo_roles')}; "
        f"dedicación: {v('equipo_dedicacion')}; experiencia: {v('equipo_experiencia')}",
        f"Ventas últimos 12 meses: {ventas}",
        f"Margen bruto: {margen}",
        f"Gasto mensual: {gasto}",
        f"Pide: {mxn(pitch.monto_mxn)} por el {pitch.porcentaje:g} % de la empresa",
        f"Valuación implícita (calculada en Python): {mxn(valuacion_mxn(pitch))}",
        f"Uso de los fondos: {pitch.uso_fondos}",
    ])
