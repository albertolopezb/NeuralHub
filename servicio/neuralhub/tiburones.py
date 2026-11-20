"""Catálogo de los seis tiburones (arquetipos inspirados en Shark Tank México, no personas reales)."""

# Orden fijo: se usa como desempate en la recomendación del panel.
TIBURONES = {
    "tiburon_financiero": {
        "nombre": "Tiburón financiero",
        "corto": "Financiero",
        "perfil": "Escéptico; solo invierte si los números cuadran.",
        "criterio": "márgenes, costo de adquisición de clientes y valuación",
        "palabras_clave": ["margen", "costo de adquisici", "cac", "valuaci", "ventas", "gasto", "flujo", "rentab"],
        "campos": ["monto_mxn", "porcentaje", "ventas_12m_mxn", "margen_bruto_pct", "gasto_mensual_mxn"],
        "sectores": ["servicios_profesionales", "sustentabilidad_y_energia", "salud"],
        "etapas": ["primeras_ventas", "crecimiento"],
        "color": "#1060D0",
    },
    "tiburon_estratega": {
        "nombre": "Tiburón estratega",
        "corto": "Estratega",
        "perfil": "Piensa en el crecimiento a largo plazo.",
        "criterio": "tamaño de mercado, competencia y ventaja defendible",
        "palabras_clave": ["mercado", "competen", "ventaja", "diferenci", "defend", "escal", "crecimiento"],
        "campos": ["mercado_objetivo", "competencia", "diferenciador"],
        "sectores": ["tecnologia", "educacion", "servicios_profesionales"],
        "etapas": ["prototipo", "primeras_ventas"],
        "color": "#4F1FD0",
    },
    "tiburon_tecnologico": {
        "nombre": "Tiburón tecnológico",
        "corto": "Tecnológico",
        "perfil": "Le atraen las ideas disruptivas aunque tengan riesgo.",
        "criterio": "innovación, tecnología y propiedad intelectual",
        "palabras_clave": ["innov", "tecnolog", "patente", "propiedad intelectual", "disrupt", "prototipo", "software", "marca registrada"],
        "campos": ["solucion", "propiedad_intelectual"],
        "sectores": ["tecnologia", "salud", "sustentabilidad_y_energia"],
        "etapas": ["idea", "prototipo"],
        "color": "#13B8F0",
    },
    "tiburona_consumo": {
        "nombre": "Tiburona de consumo",
        "corto": "Consumo",
        "perfil": "Piensa en el cliente final.",
        "criterio": "marca, canales de venta y demanda del producto",
        "palabras_clave": ["marca", "canal", "demanda", "cliente", "consumidor", "distribu", "venta"],
        "campos": ["mercado_objetivo", "modelo_ingresos", "validacion"],
        "sectores": ["alimentos_y_bebidas", "consumo_y_moda", "salud"],
        "etapas": ["primeras_ventas", "crecimiento"],
        "color": "#E8590C",
    },
    "tiburon_mentor": {
        "nombre": "Tiburón mentor",
        "corto": "Mentor",
        "perfil": "Invierte en personas más que en cifras.",
        "criterio": "equipo, compromiso y honestidad del fundador",
        "palabras_clave": ["equipo", "fundador", "compromiso", "honestidad", "dedicaci", "experiencia", "socio"],
        "campos": ["equipo_fundadores", "equipo_roles", "equipo_dedicacion", "equipo_experiencia"],
        "sectores": ["educacion", "salud", "servicios_profesionales"],
        "etapas": ["idea", "prototipo"],
        "color": "#2F9E44",
    },
    "tiburon_negociador": {
        "nombre": "Tiburón negociador",
        "corto": "Negociador",
        "perfil": "Duro con las condiciones.",
        "criterio": "porcentaje de capital, valuación y contraofertas",
        "palabras_clave": ["porcentaje", "valuaci", "capital", "condicion", "contraoferta", "oferta", "%", "monto"],
        "campos": ["monto_mxn", "porcentaje", "uso_fondos"],
        "sectores": ["alimentos_y_bebidas", "consumo_y_moda", "servicios_profesionales"],
        "etapas": ["primeras_ventas", "crecimiento"],
        "color": "#C92A2A",
    },
}

IDS = list(TIBURONES)


def menciona_criterio(texto: str, tiburon_id: str) -> bool:
    """Métrica automática: ¿el texto menciona alguna palabra clave del criterio principal del tiburón?"""
    t = texto.lower()
    return any(p in t for p in TIBURONES[tiburon_id]["palabras_clave"])
