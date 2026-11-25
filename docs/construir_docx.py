"""Construye NeuralHub_Primer_Incremento.docx con la identidad del equipo."""
import json
import re
import sys
from pathlib import Path

from docx import Document
from docx.enum.section import WD_ORIENT
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Inches, Pt, RGBColor

from gestion import *

BASE = Path(__file__).parent
IMG = BASE / "img"
SERV = BASE.parent / "servicio"
NAVY, VIOLET, BLUE, CYAN, GREEN = (RGBColor(0x00, 0x16, 0x5C), RGBColor(0x4F, 0x1F, 0xD0), RGBColor(0x10, 0x60, 0xD0),
                                   RGBColor(0x13, 0xB8, 0xF0), RGBColor(0x2F, 0x9E, 0x44))
GRAY = RGBColor(0x5A, 0x64, 0x80)
FECHA = "27 de noviembre de 2026"
REPO = "https://github.com/albertolopezb/NeuralHub"
VIDEO = "https://youtu.be/STD4X71H2I0"
VIDEO2 = "https://github.com/albertolopezb/NeuralHub/blob/main/docs/video/NeuralHub_en_vivo.mp4"
SHEET = "https://docs.google.com/spreadsheets/d/1l3FO2RC_p4CcSI41rOY3-Zu0JECe1XgxQEAlMEQZM0k/edit"

doc = Document()
sec = doc.sections[0]
sec.page_width, sec.page_height = Cm(21.59), Cm(27.94)
sec.left_margin = sec.right_margin = Cm(2.3)
sec.top_margin = sec.bottom_margin = Cm(2.2)

# ---------- estilos ----------
st = doc.styles["Normal"]
st.font.name = "Calibri"; st.font.size = Pt(11)
st.element.rPr.rFonts.set(qn("w:eastAsia"), "Calibri")
st.paragraph_format.space_after = Pt(6); st.paragraph_format.line_spacing = 1.25
for nombre, tam, color in (("Heading 1", 16, NAVY), ("Heading 2", 13, VIOLET), ("Heading 3", 11.5, BLUE)):
    s = doc.styles[nombre]
    s.font.name = "Calibri"; s.font.size = Pt(tam); s.font.bold = True; s.font.color.rgb = color
    s.element.rPr.rFonts.set(qn("w:eastAsia"), "Calibri"); s.element.rPr.rFonts.set(qn("w:ascii"), "Calibri"); s.element.rPr.rFonts.set(qn("w:hAnsi"), "Calibri")
    s.paragraph_format.space_before = Pt(14 if nombre == "Heading 1" else 10); s.paragraph_format.space_after = Pt(4)
    s.paragraph_format.keep_with_next = True

FIGS = ["act", "clases", "agente_t", "agente_e", "est_t", "est_es", "vot", "com", "sec", "grafo", "vivo", "arq", "boceto", "general", "arriba",
        "s_inicio", "s_form", "s_panel", "s_pregunta", "s_replica", "s_historial", "s_votos", "s_decision", "s_resultado"]
NUM = {k: i + 1 for i, k in enumerate(FIGS)}
usadas = []


def F(k):
    return f"la Figura {NUM[k]}"


def shade(cell, hexcolor):
    tcPr = cell._tc.get_or_add_tcPr()
    sh = OxmlElement("w:shd"); sh.set(qn("w:val"), "clear"); sh.set(qn("w:color"), "auto"); sh.set(qn("w:fill"), hexcolor)
    tcPr.append(sh)


def p(texto="", bold=False, italic=False, align=None, size=None, color=None, after=None, keep=False, style=None):
    par = doc.add_paragraph(style=style)
    if align == "j": par.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    elif align == "c": par.alignment = WD_ALIGN_PARAGRAPH.CENTER
    rich(par, texto, bold, italic, size, color)
    if after is not None: par.paragraph_format.space_after = Pt(after)
    if keep: par.paragraph_format.keep_with_next = True
    return par


def rich(par, texto, bold=False, italic=False, size=None, color=None):
    """**negrita** y [[PENDIENTE: ...]] resaltado en amarillo."""
    partes = re.split(r"(\*\*[^*]+\*\*|\[\[[^\]]+\]\])", texto)
    for t in partes:
        if not t: continue
        if t.startswith("**"):
            r = par.add_run(t[2:-2]); r.bold = True
        elif t.startswith("[["):
            r = par.add_run("[" + t[2:-2] + "]"); r.bold = True
            rPr = r._r.get_or_add_rPr(); hl = OxmlElement("w:highlight"); hl.set(qn("w:val"), "yellow"); rPr.append(hl)
        else:
            r = par.add_run(t); r.bold = bold
        r.italic = italic
        if size: r.font.size = Pt(size)
        if color is not None: r.font.color.rgb = color


def h1(t): return doc.add_heading(t, 1)
def h2(t): return doc.add_heading(t, 2)
def h3(t): return doc.add_heading(t, 3)


def bullets(items, style="List Bullet"):
    for i in items:
        par = doc.add_paragraph(style=style)
        rich(par, i); par.paragraph_format.space_after = Pt(2)


def tabla(cols, filas, anchos=None, size=9.5, header="00165C"):
    t = doc.add_table(rows=1, cols=len(cols)); t.style = "Table Grid"; t.alignment = WD_TABLE_ALIGNMENT.CENTER
    for i, c in enumerate(cols):
        cell = t.rows[0].cells[i]; cell.text = ""; shade(cell, header)
        r = cell.paragraphs[0].add_run(c); r.bold = True; r.font.size = Pt(size); r.font.color.rgb = RGBColor(255, 255, 255)
    # repetir encabezado
    trPr = t.rows[0]._tr.get_or_add_trPr(); th = OxmlElement("w:tblHeader"); th.set(qn("w:val"), "true"); trPr.append(th)
    for f in filas:
        row = t.add_row()
        trPr = row._tr.get_or_add_trPr(); cs = OxmlElement("w:cantSplit"); cs.set(qn("w:val"), "true"); trPr.append(cs)
        for i, v in enumerate(f):
            cell = row.cells[i]; cell.text = ""
            par = cell.paragraphs[0]; par.paragraph_format.space_after = Pt(0); par.paragraph_format.line_spacing = 1.0
            rich(par, str(v), size=size)
    if anchos:
        for row in t.rows:
            for i, w in enumerate(anchos): row.cells[i].width = Cm(w)
    doc.add_paragraph().paragraph_format.space_after = Pt(2)
    return t


def ficha_tabla(filas, imagen=None, ancho_img=7.0):
    """Tabla vertical de 2 columnas (template 8.2–8.4)."""
    t = doc.add_table(rows=0, cols=2); t.style = "Table Grid"; t.alignment = WD_TABLE_ALIGNMENT.CENTER
    for k, v in filas:
        row = t.add_row()
        trPr = row._tr.get_or_add_trPr(); cs = OxmlElement("w:cantSplit"); cs.set(qn("w:val"), "true"); trPr.append(cs)
        c0, c1 = row.cells; c0.width = Cm(3.2); c1.width = Cm(13.8); shade(c0, "00165C")
        c0.text = ""; r = c0.paragraphs[0].add_run(k); r.bold = True; r.font.size = Pt(9.5); r.font.color.rgb = RGBColor(255, 255, 255)
        c1.text = ""
        par = c1.paragraphs[0]; par.paragraph_format.space_after = Pt(0); par.paragraph_format.line_spacing = 1.0
        if k == "Imagen" and imagen:
            par.add_run().add_picture(str(IMG / f"{imagen}.jpg"), width=Cm(ancho_img))
        else:
            rich(par, v, size=9.5)
    doc.add_paragraph().paragraph_format.space_after = Pt(2)


def figura(k, ancho_cm, leyenda):
    usadas.append(k)
    assert FIGS[len(usadas) - 1] == k, f"orden de figuras: se esperaba {FIGS[len(usadas) - 1]} y llegó {k}"
    par = doc.add_paragraph(); par.alignment = WD_ALIGN_PARAGRAPH.CENTER; par.paragraph_format.keep_with_next = True
    NOMBRES = {"act": "f01_actividades", "clases": "f05_clases", "agente_t": "f09a_agente_tiburon", "agente_e": "f09b_agente_emprendedor",
                                         "est_t": "f02_estados_tiburon", "est_es": "f08_estados_emprendedor_sesion", "vot": "f10_votacion", "com": "f03_comunicaciones",
                                         "sec": "f07_secuencia_ronda", "grafo": "f06_grafo_langgraph", "vivo": "vivo_replica", "arq": "f11_arquitectura_comunicacion", "boceto": "f04_boceto_escena", "general": "estudio_general",
                                         "arriba": "vista_arriba", "s_inicio": "01_inicio", "s_form": "02_formulario", "s_panel": "03_eleccion_panel",
                                         "s_pregunta": "05_pregunta", "s_replica": "06_replica", "s_historial": "07_historial", "s_votos": "08_votos",
                                         "s_decision": "09_decision", "s_resultado": "10_resultado"}
    par.add_run().add_picture(str(IMG / (NOMBRES[k] + ".jpg")), width=Cm(ancho_cm))
    c = doc.add_paragraph(); c.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = c.add_run(f"Figura {NUM[k]}. {leyenda}"); r.italic = True; r.font.size = Pt(10); r.font.color.rgb = GRAY


# ---------- datos medidos ----------
res = json.load(open(SERV / "resultados_prueba.json", encoding="utf8"))
NS = len(res)
llam_max = max(x["llamadas_modelo"] for x in res)
seg_prom = round(sum(x["segundos_total"] for x in res) / NS, 1); seg_max = max(x["segundos_total"] for x in res)
n16 = sum(1 for x in res if x["llamadas_modelo"] == 16); n19 = sum(1 for x in res if x["llamadas_modelo"] == 19)
v_tot = sum(x["votos"] for x in res); v_crit = sum(x["justificaciones_mencionan_criterio"] for x in res)
p_tot = sum(x["preguntas"] for x in res); p_crit = sum(x["preguntas_mencionan_criterio"] for x in res)
r_tot = sum(x["replicas"] for x in res); r_pos = sum(x["replicas_con_postura"] for x in res); r_otro = sum(x["replicas_mencionan_otro_tiburon"] for x in res)
ordenok = sum(1 for x in res if x["eventos_en_orden"]); juntos = sum(1 for x in res if x["votos_publicados_juntos"])
terminadas = sum(1 for x in res if x["terminada"] and x["resultado"] in ("TRATO", "SIN_TRATO"))
solidos = [x for x in res if x["tipo"] == "solido"]; trato_sol = sum(1 for x in solidos if x["resultado"] == "TRATO")
debiles = [x for x in res if x["criterio_debil"]]
dueno_sale = sum(1 for x in debiles if x["votos_primera"].get(x["criterio_debil"]) in ("salir", "contraoferta"))
# justificaciones con criterio y dato (heurística)
sys.path.insert(0, str(SERV))
from neuralhub.metricas import tiene_dato_concreto
from neuralhub.tiburones import menciona_criterio
cd_ok = cd_tot = 0
for f in sorted((SERV / "sesiones" / "prueba").glob("*.json")):
    d = json.load(open(f, encoding="utf8"))
    for e in d["eventos"]:
        if e["tipo"] == "VOTO":
            cd_tot += 1; cd_ok += menciona_criterio(e["texto"], e["emisor"]) and tiene_dato_concreto(e["texto"])
pct = lambda a, b: f"{100 * a / b:.1f} %".replace(".0 %", " %")

res1 = json.load(open(SERV / "resultados_prueba_inc1.json", encoding="utf8"))
def _cd(carpeta):
    ok = tot = 0
    for f in sorted((SERV / "sesiones" / carpeta).glob("*.json")):
        for e in json.load(open(f, encoding="utf8"))["eventos"]:
            if e["tipo"] == "VOTO":
                tot += 1; ok += menciona_criterio(e["texto"], e["emisor"]) and tiene_dato_concreto(e["texto"])
    return ok, tot
cd1_ok, cd1_tot = _cd("prueba_inc1")
ciega = json.load(open(SERV / "resultados_prueba_ciega.json", encoding="utf8"))["resumen"]
ciega1 = {"porcentaje": 56.7, "por_tiburon": {"tiburon_financiero": "10/10", "tiburon_estratega": "7/10", "tiburon_tecnologico": "7/10", "tiburona_consumo": "3/10", "tiburon_mentor": "6/10", "tiburon_negociador": "1/10"}}
vivo = json.load(open(SERV / "resultados_vivo.json", encoding="utf8"))
lat = [x["segundos"] for x in vivo["latencias"]]
lat_ok = sum(1 for x in lat if x < 10)
seg_prom1 = round(sum(x["segundos_total"] for x in res1) / len(res1), 1)

# ---------- portada ----------
par = doc.add_paragraph(); par.alignment = WD_ALIGN_PARAGRAPH.CENTER
par.add_run().add_picture(str(IMG / "logo.jpg"), width=Cm(8.2))
p("Equipo NeuralHub", bold=True, align="c", size=24, color=NAVY, after=2)
p("Incremento 2", bold=True, align="c", size=20, color=VIOLET, after=14)
p("**Integrantes**", align="c", size=12, after=2)
for n, m in (("Alberto López Barragán", "A00841178"), ("Gerardo Islas Gómez", "A01739896"), ("Gilberto Olavarrieta Sánchez", "A01286889"),
             ("Juan Esteban Jaramillo", "A00841074"), ("Franco Salazar Guerrero", "A01178655")):
    p(f"{n} - {m}", align="c", size=11.5, after=1)
p("", after=8)
p(FECHA, align="c", size=12, after=2); p("Campus MTY", align="c", size=12, after=2)
p("Modelación de sistemas multiagentes con gráficas computacionales (Gpo 305)", align="c", size=12, after=2)
p("Profesores: Dra. Lorena B. Martínez Elizalde y Dr. Carlos Astengo Noguez", align="c", size=12, after=6)
p("Repositorio: " + REPO, align="c", size=10, color=GRAY, after=0)
p("Gestión del proyecto: " + SHEET, align="c", size=10, color=GRAY, after=0)
doc.add_page_break()

# ---------- correcciones atendidas ----------
h1("Correcciones atendidas")
p("El primer incremento obtuvo 100/100. Sus dos observaciones (que no restaron puntos) y dónde se atendieron:", align="j", keep=True)
tabla(["Observación del primer incremento", "Cómo se atendió", "Sección"], [
    ("HU-08 dice solo «usuario»; especifiquen de qué tipo es.",
     "HU-08 ahora es «Como emprendedor o espectador…». Se revisaron todas las historias: ninguna usa un usuario genérico, y se agregaron HU-10 (emprendedor, voz) y HU-11 (espectador, voces y pantallas).", "6"),
    ("Siguen clasificando el entorno como estático; con la negociación y la segunda votación, revisen si eso sigue aplicando.",
     "Se reclasificó como dinámico: la oferta cambia entre la primera y la segunda votación, el usuario actúa en tiempo real (escribe o dicta mientras el servicio espera) y los tres tiburones deliberan en paralelo. Se explica la razón en la tabla del entorno.", "7.2"),
], anchos=[5.6, 9.2, 2.2])
h2("Correcciones del anteproyecto (atendidas en el primer incremento)")
p("Se conservan para dar trazabilidad. Cada comentario de la retroalimentación del anteproyecto (80/100) y la sección donde se atendió:", align="j", keep=True)
tabla(["Comentario de la profesora", "Cómo se atendió", "Sección"], [
    ("El problema es genérico; falta fundamentar con datos cómo deciden los inversionistas reales.",
     "Se reescribió con datos verificados: encuesta a 885 inversionistas de riesgo, cifras de evaluación y cierre de tratos, fracaso de startups y supervivencia de negocios en México.", "3"),
    ("Los objetivos específicos no tienen métrica.",
     "Nueve objetivos SMART con sprint, métrica con umbral, conjunto de prueba fijo y estado medido (primer incremento).", "4"),
    ("Contradicción: en las creencias el tiburón conoce los votos emitidos, pero en el entorno no.",
     "La votación es secreta y simultánea. Las creencias ya no incluyen votos ajenos; solo en la negociación se conoce el resultado agregado que publica el nodo de votación.", "7.1, 7.2"),
    ("Dicen que los tiburones debaten, pero no hay mensajes de tiburón a tiburón.",
     "Se creó el mensaje RÉPLICA (tiburón → tiburón), una por ronda, presente en el grafo, la tabla de mensajes, la figura de comunicaciones, el código y la escena.", "7.3, 7.5, 8.1"),
    ("Seis jueces son muchos (≈40 llamadas): ¿se van a seleccionar entre ellos?",
     "Hay 6 tiburones y en cada sesión participan 3, recomendados por una regla en Python (0 llamadas) y elegidos por el usuario. Presupuesto: máximo 19 llamadas con usuario humano; medido: máximo %d." % llam_max, "7, 7.3"),
    ("Escena: no se describen los controles del usuario.",
     "Se describen y se construyeron en Unity: formulario, elección de panel, respuestas, negociación, pausa, velocidad, saltar, historial y repetir sesión.", "8.1"),
    ("Portada, créditos e identidad (10/10) y roles por integrante.",
     "Se conservan el logo, los colores y los roles de cada integrante; se agregó un encabezado con el nombre del equipo en todas las páginas.", "Portada, 2"),
    ("Restricciones (10/10): concretas y cuantificadas.",
     "Se mantienen y se actualizan las cifras (6 tiburones con 3 activos, máximo de llamadas, controles del usuario, un solo escenario).", "5"),
    ("Gestión: sin lista de actividades, responsables, fechas ni herramienta.",
     "Hoja de gestión con product backlog, sprint backlog, calendario, bitácoras y matriz de participación, y resumen en este documento.", "9"),
], anchos=[5.6, 9.2, 2.2])

# ---------- 1 introducción ----------
h1("1. Introducción")
p("Cuando una persona emprendedora presenta su idea a inversionistas, rara vez sabe qué criterio pesó en su decisión ni cuánto habría cambiado el resultado con otros datos. "
  "NeuralHub es un simulador de un panel de inversionistas al estilo de Shark Tank: el usuario captura su propio pitch, elige a tres tiburones de un catálogo de seis "
  "(cada uno con un criterio distinto), responde sus preguntas en un máximo de tres rondas, ve cómo debaten entre ellos, y recibe una decisión de inversión justificada "
  "y un reporte de retroalimentación. Todo se representa en Unity como un estudio de televisión submarino: una pecera con tiburones empresarios, el «Tanque de tiburones».", align="j")
p("**Estrategia de solución.** Un servicio en Python ejecuta los agentes con LangGraph sobre un modelo de lenguaje de Azure OpenAI y expone sus eventos por HTTP. "
  "Unity no ejecuta a los agentes: pide los eventos y los reproduce en la escena. Las cuentas (valuación, recomendación del panel, regla de votación) se hacen en código determinista, "
  "no en el modelo, para controlar el costo y evitar cifras inventadas.", align="j")
p("**Alcance de este incremento.** Múltiples agentes funcionando en vivo: la app de Unity conversa con el servicio real (el usuario escribe o dicta sus respuestas con la voz y los tiburones hablan con voz propia), "
  "se documenta cómo se comunican y deciden los agentes, se mejoraron los prompts (estilo distintivo por tiburón y justificaciones con cifras), se repitió la evaluación de 15 sesiones y la escena ganó iluminación submarina, "
  "pantallas con datos, sonido y navegación por la conversación.", align="j")

# ---------- 2 créditos ----------
h1("2. Créditos")
p("Equipo NeuralHub, Campus Monterrey. Integrantes y su rol en el proyecto:", keep=True)
tabla(["Nombre", "Matrícula", "Rol"], [
    ("Alberto López Barragán", "A00841178", "Líder de proyecto y programador de agentes (LangGraph)"),
    ("Gerardo Islas Gómez", "A01739896", "Programador de agentes y diseño de prompts"),
    ("Gilberto Olavarrieta Sánchez", "A01286889", "Programador de integración (servicio de agentes con Unity)"),
    ("Juan Esteban Jaramillo", "A00841074", "Especialista en gráficos y diseño 3D"),
    ("Franco Salazar Guerrero", "A01178655", "Programador de Unity (lógica de la escena y animación)"),
], anchos=[5.6, 2.8, 8.6], size=10)

# ---------- 3 contexto y problema ----------
h1("3. Contexto y problema")
h2("Contexto")
p("En México, la esperanza de vida promedio de un establecimiento es de 7.8 años y entre 2014 y 2019 cerraron casi 4 de cada 10 [8]. En startups, un análisis de 101 autopsias escritas por fundadores "
  "encontró que las causas más citadas son la falta de necesidad en el mercado (42 %), la falta de capital suficiente (29 %) y haber armado el equipo equivocado (23 %) [9]. "
  "Estas fallas son, en buena medida, detectables antes de invertir si alguien hace las preguntas correctas.", align="j")
p("Los inversionistas profesionales hacen esas preguntas, pero a muy pocas empresas. En una encuesta a 885 inversionistas de capital de riesgo de 681 firmas [6], "
  "uno de sus autores resume que el inversionista promedio evalúa 200 empresas al año e invierte en cuatro, con 118 horas de revisión previa y 83 días para cerrar cada trato, "
  "y que el 95 % considera al equipo un factor importante y el 47 % el más importante [7]. Es decir, cada decisión combina criterios distintos (equipo, negocio, mercado, números) "
  "y se toma con información incompleta.", align="j")
p("Shark Tank condensa esa evaluación en minutos y con inversionistas de perfiles distintos. Estudios sobre el programa, como el de Jetter y Stockley con 4,893 interacciones entre "
  "emprendedores e inversionistas [10], lo usan precisamente porque permite observar ambos lados de la decisión, incluidos los pitches que no reciben oferta.", align="j")
h2("Problema")
p("Quien empieza un negocio no tiene acceso a un panel que lo evalúe desde varios criterios a la vez (números, mercado, tecnología, cliente, equipo y condiciones del trato) "
  "ni recibe una explicación de por qué sí o por qué no le invertirían. Observar o ensayar un panel real es difícil y costoso; la herramienta que nos compartió el socio formador "
  "simula el panel como un chat abierto, sin rondas, sin votación y sin retroalimentación estructurada.", align="j")
p("El reto es construir un sistema multiagente en el que el usuario elige 3 tiburones de un catálogo de 6, con criterios distintos; los tiburones lo interrogan sobre su propio pitch, "
  "debaten entre ellos y emiten una decisión justificada que el emprendedor puede usar para mejorar, bajo un presupuesto de llamadas al modelo acotado y representado en una escena de Unity.", align="j")
h2("Antecedentes y trabajos relacionados")
p("El debate entre varios modelos de lenguaje mejora el razonamiento y la veracidad de las respuestas frente a un solo modelo [1]. Nuestro panel aplica esa idea con roles distintos: "
  "el emprendedor defiende y los tiburones cuestionan y se contestan entre ellos. TradingAgents asigna roles de analistas e inversionistas a agentes de lenguaje que debaten antes de decidir [2]; "
  "su código usa LangGraph [3], [4], la misma librería que elegimos. Nuestro caso cambia el dominio a un panel de inversión con votación secreta y negociación, y lo representa visualmente en Unity.", align="j")

# ---------- 4 objetivos ----------
h1("4. Objetivos")
h2("Objetivo general")
p("Desarrollar, antes de la entrega final del reto (viernes 4 de diciembre de 2026), un sistema multiagente con inteligencia artificial generativa que simule un panel de seis "
  "inversionistas de Shark Tank del que el usuario elige tres por sesión: el usuario emprendedor captura su pitch, responde las preguntas en un máximo de tres rondas más una de negociación, "
  "y recibe una decisión de inversión justificada y un reporte de retroalimentación, todo representado en una escena de Unity.", align="j")
h2("Objetivos específicos (SMART)")
p("**Conjunto de prueba.** 5 pitches escritos por el equipo (2 sólidos, 2 débiles en un criterio conocido y 1 incompleto), 3 sesiones por pitch: 15 sesiones. Cada pitch usa un panel fijo de 3 tiburones "
  "(en los débiles incluye al dueño del criterio débil) y las respuestas del emprendedor salen de un guion fijo. Las métricas automáticas buscan palabras clave del criterio de cada tiburón y cifras en el texto; "
  "son un indicador preliminar y se complementan con la rúbrica manual descrita en el objetivo 2.", align="j")
tabla(["#", "Objetivo (específico, medible, alcanzable, relevante y con fecha)", "Estado medido (26 nov)"], [
    ("1", "**Recomendación del panel.** Para el final del Sprint 2 (27 nov), en los 3 pitches débiles o incompletos del conjunto de prueba, el panel recomendado incluye al tiburón dueño del criterio débil, sin usar el modelo (0 llamadas).",
     "3 de 3 pitches (100 %), 0 llamadas. Cumplido en prueba preliminar."),
    ("2", "**Personalidades distinguibles.** Para el final del Sprint 2, en una prueba ciega con 60 mensajes sin nombre (10 por tiburón), un integrante que no escribió los prompts identifica al tiburón autor en al menos el 80 % de los casos.",
     f"Juez automático (LLM): {ciega['aciertos']} de {ciega['mensajes']} ({ciega['porcentaje']} %); en el incremento 1 era {ciega1['porcentaje']} %. La prueba con una persona usa la planilla pruebas/prueba_ciega_humana.csv (tarea T23)."),
    ("3", "**Análisis por criterio.** Para el final del Sprint 2, en las 15 sesiones, al menos el 85 % de las preguntas aborda el criterio principal del tiburón que pregunta y cita un dato del pitch o señala que falta.",
     f"{p_crit} de {p_tot} preguntas ({pct(p_crit, p_tot)}) mencionan su criterio (indicador automático)."),
    ("4", "**Debate entre tiburones.** Para el final del Sprint 2, el 100 % de las rondas incluye una RÉPLICA de un tiburón dirigida a otro tiburón del panel, con acuerdo o desacuerdo explícito.",
     f"{r_tot} réplicas en {r_tot} rondas ({r_otro} dirigidas a otro tiburón; {r_pos} con postura explícita). Cumplido."),
    ("5", "**Decisión que siempre termina.** Para el final del Sprint 2, el 100 % de las sesiones termina con trato o sin trato en un máximo de 3 rondas más 1 de negociación, y la regla de votación pasa las 27 combinaciones posibles de votos.",
     f"{terminadas} de {NS} sesiones terminadas; 27 de 27 combinaciones. Cumplido."),
    ("6", "**Costo controlado.** Para el final del Sprint 3 (4 dic), ninguna sesión con usuario humano usa más de 19 llamadas al modelo (22 con el emprendedor en autopiloto) y el modelo responde en menos de 40 s por sesión en total.",
     f"Máximo {llam_max} llamadas ({n16} sesiones con 16 y {n19} con 19); {seg_prom} s en promedio y {seg_max} s como máximo. Cumplido en la prueba."),
    ("7", "**Justificación con evidencia.** Para el final del Sprint 3, al menos el 85 % de las justificaciones de voto menciona el criterio principal del tiburón y al menos un dato concreto de la conversación.",
     f"{cd_ok} de {cd_tot} ({pct(cd_ok, cd_tot)}); en el incremento 1 era {pct(cd1_ok, cd1_tot)}. Cumplido tras ajustar los prompts (T20)."),
    ("8", "**Coherencia con el perfil.** Para el final del Sprint 3, en los pitches débiles el tiburón dueño del criterio débil vota «salir» o «contraoferta» en al menos el 80 % de las sesiones; en los pitches sólidos hay trato en al menos 4 de 6 sesiones.",
     f"{dueno_sale} de {len(debiles)} sesiones débiles ({pct(dueno_sale, len(debiles))}); trato en {trato_sol} de {len(solidos)} sesiones sólidas. Cumplido."),
    ("9", "**Integración con Unity.** Para el final del Sprint 3, la escena reproduce en orden el 100 % de los eventos de las 15 sesiones, y en el 90 % de los turnos la siguiente intervención aparece en menos de 10 s tras enviar la respuesta.",
     f"Eventos en orden: {ordenok} de {NS} sesiones y la sesión en vivo completa ({vivo['eventos']} eventos). Latencia tras enviar la respuesta: {lat_ok} de {len(lat)} turnos bajo 10 s (máximo {max(lat):.1f} s). Cumplido en la prueba en vivo."),
    ("10", "**Retroalimentación útil.** Para la entrega final, al menos 5 usuarios ajenos al equipo usan la app con su propia idea y al menos el 80 % califica el reporte con 4 o 5 de 5 en utilidad.",
     "Pendiente (tarea T31)."),
], anchos=[0.8, 10.4, 5.8], size=9)

# ---------- 5 restricciones ----------
h1("5. Restricciones")
bullets([
    "**Agentes:** catálogo de seis tiburones con perfil fijo definido por el equipo; en cada sesión participan exactamente 3. El emprendedor es el usuario humano; existe un agente emprendedor «autopiloto» solo para pruebas automáticas y para la sesión de ejemplo. El usuario no puede crear ni editar tiburones. El moderador y el nodo de votación son nodos deterministas del grafo, no agentes.",
    "**Debate acotado:** máximo 3 rondas; en cada una los 3 tiburones preguntan, el usuario responde y un tiburón replica a otro. Hay una ronda extra de negociación solo si hay un voto a favor.",
    "**Pitch del usuario:** el usuario escribe su pitch en un formulario con campos obligatorios y un límite de 300 caracteres por campo; las respuestas tienen máximo 600 caracteres.",
    "**Costo y latencia:** máximo 19 llamadas al modelo por sesión con usuario humano (16 sin negociación) y 22 en autopiloto. La recomendación del panel, la valuación y la regla de votación no usan el modelo.",
    "**Idioma y moneda:** todo el diálogo es en español y los montos están en pesos mexicanos.",
    "**Una sola escena:** un estudio de televisión submarino (pecera) con una cámara principal que cambia de plano y 3 sillones concha fijos; el personaje de cada sillón cambia según el panel elegido.",
    "**Comunicación solo por HTTP:** Unity y el servicio se comunican únicamente por HTTP; los agentes no corren dentro de Unity.",
    "**Interfaz:** el usuario captura su pitch, elige su panel, inicia la sesión, responde por escrito, decide en la negociación, y cuenta con pausa, velocidad 1x/2x, saltar animación e historial. No controla a los tiburones ni su orden de turnos.",
    "**Aviso:** es una simulación educativa; no constituye asesoría financiera.",
    "**Tiempo y recursos:** el equipo aprende LangGraph durante el proyecto; la entrega final es el viernes 4 de diciembre de 2026.",
])

# ---------- 6 historias de usuario ----------
h1("6. Historias de usuario")
p("Los usuarios son las personas que usan la simulación; ni el sistema ni los agentes son usuarios. Se definen tres tipos: el **emprendedor** (persona con una idea que quiere practicar su pitch), "
  "el **espectador** (quien observa cómo se decide un panel) y el **evaluador** (profesor o socio formador que revisa la calidad y el costo de una sesión). Formato de Atlassian: «Como [usuario], quiero [acción], para [beneficio]». "
  "Estas historias son el product backlog (sección 9).", align="j")
for hid, hist, usr, pri, pts, sp, est in HISTORIAS:
    t = doc.add_table(rows=2, cols=1); t.style = "Table Grid"; t.alignment = WD_TABLE_ALIGNMENT.CENTER
    c = t.rows[0].cells[0]; shade(c, "E8F0FF"); c.text = ""
    par = c.paragraphs[0]; par.paragraph_format.space_after = Pt(0); par.paragraph_format.keep_with_next = True
    r = par.add_run(f"{hid}  "); r.bold = True; r.font.color.rgb = VIOLET; r.font.size = Pt(10)
    rich(par, hist, size=10)
    c2 = t.rows[1].cells[0]; c2.text = ""
    par = c2.paragraphs[0]; par.paragraph_format.space_after = Pt(2)
    rich(par, f"**Prioridad:** {pri}   **Estimación:** {pts} puntos   **Sprint:** {sp}   **Estado:** {est}", size=9)
    for cr in CRITERIOS[hid]:
        q = c2.add_paragraph(); q.paragraph_format.space_after = Pt(0); q.paragraph_format.left_indent = Cm(0.4); q.paragraph_format.line_spacing = 1.0
        rich(q, "☐ " + cr, size=9)
    for row in t.rows:
        trPr = row._tr.get_or_add_trPr(); cs = OxmlElement("w:cantSplit"); cs.set(qn("w:val"), "true"); trPr.append(cs)
    sp_ = doc.add_paragraph(); sp_.paragraph_format.space_after = Pt(3)

# ---------- 7 sistema multiagente ----------
h1("7. Descripción del sistema multiagente")
p("El sistema simula un panel de seis tiburones del que el usuario elige tres por sesión. Tiene dos partes: un servicio en Python que ejecuta los agentes con LangGraph sobre Azure OpenAI (modelo gpt-4.1-mini) "
  f"y una aplicación en Unity que muestra la escena. {F('clases').capitalize()} es el diagrama de clases con las clases de ambos lados.", align="j")
h2("Idea general")
p("LangGraph modela la sesión como un grafo: cada paso es un nodo, las rondas son un ciclo y la votación es un nodo determinista [4]. Se eligió porque admite ciclos y pausas para esperar al usuario (interrupt) y porque nosotros fijamos el orden de turnos, "
  "el máximo de rondas y la regla de decisión. Una sesión sigue estos pasos (Figura 1):", align="j")
bullets([
    "El usuario llena el formulario del pitch en Unity. Python calcula la valuación implícita (monto ÷ porcentaje).",
    "La app recomienda 3 tiburones con una regla en Python (sin modelo), según los datos que le faltan al pitch y la afinidad de cada tiburón con el sector y la etapa. El usuario acepta o cambia el panel e inicia la sesión.",
    "La escena muestra el pitch en la pantalla del estudio. El moderador (nodo determinista) fija el orden de turnos: pregunta primero el tiburón con más datos faltantes en su criterio.",
    "En cada ronda (3 en total) los 3 tiburones preguntan, el usuario responde por escrito las 3 preguntas (el grafo se pausa) y un tiburón replica a otro (RÉPLICA); el moderador rota quién replica.",
    "Los 3 tiburones votan en secreto y en paralelo: invertir, contraoferta o salir, con una justificación breve.",
    "El nodo de votación revela los votos y aplica la regla: con 2 o 3 a favor hay trato (el usuario elige su oferta o una contraoferta); con 1 a favor hay una negociación y una segunda votación; con 0 no hay trato.",
    "Un último paso genera el reporte de retroalimentación y el servicio entrega los eventos a Unity, que los reproduce.",
], style="List Number")
figura("act", 9.8, "Diagrama de actividades de una sesión.")
figura("clases", 17.0, "Diagrama de clases del sistema (servicio de agentes y Unity).")

h2("7.1 Modelo de los agentes")
p("El sistema tiene dos tipos de agente con modelo de lenguaje y dos nodos deterministas. Los perfiles de los tiburones son arquetipos inspirados en el estilo de los inversionistas de Shark Tank México, aunque no son copias.", align="j", keep=True)
tabla(["Agente", "Perfil", "Criterio principal"], [
    ("**Emprendedor** (usuario humano; agente «autopiloto» en pruebas)", "Defiende su idea y busca el trato cediendo el menor porcentaje", "Responde con los datos de su ficha; si falta un dato, lo dice"),
    ("**Tiburón financiero**", "Escéptico; solo invierte si los números cuadran", "Márgenes, costo de adquisición de clientes, valuación"),
    ("**Tiburón estratega**", "Piensa en el crecimiento a largo plazo", "Tamaño de mercado, competencia, ventaja defendible"),
    ("**Tiburón tecnológico**", "Le atraen las ideas disruptivas aunque tengan riesgo", "Innovación, tecnología, propiedad intelectual"),
    ("**Tiburona de consumo**", "Piensa en el cliente final", "Marca, canales de venta, demanda del producto"),
    ("**Tiburón mentor**", "Invierte en personas más que en cifras", "Equipo, compromiso y honestidad del fundador"),
    ("**Tiburón negociador**", "Duro con las condiciones", "Porcentaje de capital, valuación, contraofertas"),
    ("Moderador (nodo, sin modelo)", "Fija el orden de turnos y asigna quién replica a quién", "Primero pregunta quien tiene más huecos en su criterio"),
    ("Nodo de votación (nodo, sin modelo)", "Revela los votos y aplica la regla de decisión", "2 o 3 a favor: trato; 1: negociación; 0: sin trato"),
], anchos=[4.6, 6.0, 6.4], size=9.5)
p("En total son 7 agentes con modelo de lenguaje (6 tiburones, de los cuales 3 participan por sesión, y el emprendedor en autopiloto) más 2 nodos deterministas. "
  "Cada tiburón se modela con cuatro elementos:", align="j")
bullets([
    "**Creencias:** su perfil y criterio, el pitch con la valuación calculada y el historial público de la sesión (preguntas, respuestas y réplicas). No conoce el razonamiento interno ni los votos de los demás mientras vota. Solo en la negociación conoce el resultado agregado de la primera votación y las contraofertas, porque el nodo de votación las publica.",
    "**Planes:** preguntar sobre el dato más débil de su criterio, evaluar la respuesta, replicar cuando el moderador se lo asigna, votar con una justificación y, si aplica, hacer una contraoferta.",
    "**Cooperación:** comparten el historial público. Coinciden o discrepan de forma explícita con el mensaje RÉPLICA y pueden coincidir en una misma contraoferta.",
    "**Aprendizaje:** no hay entrenamiento. Se adaptan con el contexto acumulado de la sesión; el estado de LangGraph guarda el historial y permite repetirla.",
])
p("El agente emprendedor en autopiloto tiene como creencias su ficha del pitch y el historial público; planea responder con los datos de su ficha (sin inventar cifras) y elegir la condición de mayor valuación implícita; no aprende. "
  f"{F('agente_t').capitalize()} y {F('agente_e')} muestran el modelo de cada agente (percepciones, creencias, metas, planes y acciones) y su entorno.", align="j")
figura("agente_t", 16.5, "Modelo del agente tiburón.")
figura("agente_e", 16.5, "Modelo del agente emprendedor (autopiloto).")
p(f"El diagrama de estados del tiburón ({F('est_t')}) agrega el estado «Replicando» y el estado «Fuera del panel» para los tiburones no elegidos; Unity usa el estado para elegir la animación. "
  f"{F('est_es').capitalize()} muestra los estados del emprendedor y de la sesión (moderador).", align="j")
figura("est_t", 16.5, "Diagrama de estados del agente tiburón.")
figura("est_es", 16.5, "Diagramas de estados del emprendedor y de la sesión.")
p(f"El nodo de votación y el moderador no usan el modelo de lenguaje; {F('vot')} muestra el diagrama de flujo del nodo de votación, que implementa la regla de decisión de la sección 7.3.", align="j", keep=True)
figura("vot", 8.8, "Diagrama de flujo del nodo de votación.")
p("**Acciones de los agentes**", keep=True)
tabla(["Agente", "Acción", "Precondición", "Efecto", "Mensaje", "Modelo"], [
    ("Usuario/Emprendedor", "Presentar pitch", "Formulario completo", "Se calcula la valuación y se muestra en pantalla", "PITCH", "No"),
    ("Moderador", "Fijar panel y orden", "3 tiburones elegidos", "Orden de turnos de la ronda", "PANEL", "No"),
    ("Tiburón", "Preguntar", "Es su turno en la ronda k ≤ 3", "Una pregunta desde su criterio", "PREGUNTA", "Sí"),
    ("Emprendedor", "Responder", "Llegaron las 3 preguntas", "3 respuestas al historial", "RESPUESTA", "No (usuario) / Sí (autopiloto)"),
    ("Tiburón", "Replicar", "Se le asignó replicar", "Acuerdo o desacuerdo hacia otro tiburón", "RÉPLICA", "Sí"),
    ("Tiburón", "Votar", "Terminó la ronda 3", "Voto secreto con justificación", "VOTO", "Sí"),
    ("Nodo de votación", "Revelar y decidir", "Hay 3 votos", "Trato, negociación o sin trato", "DECISIÓN / NEGOCIACIÓN", "No"),
    ("Emprendedor", "Elegir condiciones u ofertar", "Trato con contraofertas, o 1 voto a favor", "Condiciones finales o nueva oferta", "OFERTA", "No"),
    ("Tiburón", "Votar de nuevo", "Hubo negociación", "Segundo voto (invertir o salir)", "VOTO", "Sí"),
    ("Servicio", "Generar reporte", "Hay decisión", "Retroalimentación por tiburón", "REPORTE", "Sí"),
], anchos=[2.6, 2.6, 3.0, 3.6, 2.2, 3.0], size=8.5)

h2("7.2 Modelo del entorno")
p("Clasificamos el entorno con las seis características de Russell y Norvig [5]:", keep=True)
tabla(["Característica", "Clasificación", "Razón"], [
    ("**Observabilidad**", "Parcialmente observable", "Todos ven el pitch y el historial público, pero no el razonamiento interno de los demás. La votación es secreta y simultánea: nadie ve otros votos hasta que el nodo de votación los publica."),
    ("**Determinismo**", "Estocástico", "El modelo de lenguaje puede responder distinto con la misma entrada; el orden de turnos, la recomendación del panel y la regla de decisión son deterministas."),
    ("**Episodios**", "Secuencial", "Cada pregunta, respuesta y réplica depende de lo anterior, y el voto depende de todo el debate."),
    ("**Cambio**", "Dinámico", "El entorno cambia por acciones que no controla el tiburón que delibera: el usuario escribe o dicta en tiempo real mientras el servicio espera, la oferta cambia entre la primera y la segunda votación y los tres tiburones deliberan en paralelo sin ver los votos de los demás. Se revisó tras la observación del primer incremento."),
    ("**Valores**", "Discreto", "Hay turnos y rondas contables y un conjunto finito de acciones."),
    ("**Agentes**", "Multiagente", "Tres tiburones por sesión, elegidos de un catálogo de seis, que cooperan y compiten, y un emprendedor con objetivo opuesto (obtener el trato cediendo lo menos posible)."),
], anchos=[3.2, 3.4, 10.4], size=9.5)
p("El tiempo avanza por pasos: un paso es un turno (o una votación) y una ronda es la secuencia de preguntas, respuestas y réplica. En el servicio no hay espacio físico: el espacio es una red de información, el grafo de LangGraph, "
  "donde cada arista indica quién puede responder a quién. La posición física solo existe en Unity, donde cada tiburón ocupa un sillón fijo.", align="j")

h2("7.3 Modelo de la negociación")
p("Los agentes no hacen subastas. Negocian con un debate por rondas, una votación secreta y, cuando hace falta, una negociación con segunda votación. Mensajes que se intercambian:", align="j", keep=True)
tabla(["Mensaje", "Emisor → destino", "Contenido"], [
    ("PITCH", "Emprendedor → tiburones", "Datos del formulario y valuación calculada"),
    ("PANEL", "Moderador → todos", "Tiburones del panel y el motivo de cada uno"),
    ("PREGUNTA", "Tiburón → emprendedor", "Duda desde su criterio"),
    ("RESPUESTA", "Emprendedor → tiburón", "Texto escrito por el usuario (o el autopiloto)"),
    ("RÉPLICA", "Tiburón → tiburón", "Acuerdo o desacuerdo con la pregunta de otro, visible para todos"),
    ("VOTO", "Tiburón → nodo de votación", "Invertir, contraoferta o salir, con justificación; secreto hasta que se publica"),
    ("CONTRAOFERTA", "Tiburón → emprendedor", "Monto y porcentaje propuestos; viaja dentro del VOTO"),
    ("NEGOCIACIÓN", "Nodo de votación → emprendedor", "Aviso de que solo hubo 1 voto a favor"),
    ("OFERTA", "Emprendedor → tiburones", "Condición elegida o nueva oferta"),
    ("DECISIÓN", "Nodo de votación → todos", "Trato o sin trato, con las condiciones finales"),
    ("REPORTE", "Servicio → emprendedor", "Retroalimentación por tiburón y resumen"),
], anchos=[3.0, 5.0, 9.0], size=9.5)
p("**Regla de decisión.** «Invertir» y «contraoferta» cuentan como votos a favor. Con 2 o 3 a favor hay trato; si hubo contraofertas, el usuario elige una o mantiene su oferta original, y esas son las condiciones finales. "
  "Con 1 a favor hay una ronda de negociación: el usuario ajusta su oferta o acepta una contraoferta y los 3 tiburones votan una sola vez más (invertir o salir); con 2 o más a favor hay trato. Con 0 a favor no hay trato.", align="j")
p("**Presupuesto de llamadas al modelo.** Antes eran unas 40 porque el emprendedor también era un agente y los seis tiburones preguntaban en cada ronda.", align="j", keep=True)
tabla(["Paso", "Llamadas"], [
    ("Pitch, valuación, recomendación del panel y orden de turnos (formulario y Python)", "0"),
    ("Preguntas: 3 rondas × 3 tiburones", "9"),
    ("Réplicas: 1 por ronda", "3"),
    ("Respuestas del usuario (con el emprendedor en autopiloto: 1 llamada por ronda)", "0 (3)"),
    ("Votación (3 en paralelo)", "3"),
    ("Reporte final", "1"),
    ("**Sesión sin negociación**", "**16** (19 con autopiloto)"),
    ("Segunda votación (solo con 1 voto a favor)", "+3"),
    ("**Máximo por sesión**", "**19** (22 con autopiloto)"),
], anchos=[12.5, 4.5], size=9.5)
p(f"En las {NS} sesiones de prueba (emprendedor con guion fijo) se midieron {n16} sesiones con 16 llamadas y {n19} con 19, sin excepciones.", align="j")
p(f"El diagrama de comunicaciones ({F('com')}) incluye la flecha de tiburón a tiburón (RÉPLICA) y {F('sec')} muestra la secuencia de una ronda.", align="j")
figura("com", 16.0, "Diagrama de comunicaciones entre los agentes.")
figura("sec", 16.0, "Diagrama de secuencia de una ronda.")

h2("7.4 Modelo de la interacción")
p("Unity no ejecuta a los agentes. El servicio guarda cada turno como un evento y Unity los pide por HTTP (UnityWebRequest) aproximadamente cada segundo, los guarda en una cola y los reproduce de uno en uno. Formato del evento:", align="j", keep=True)
p('{"n": 7, "ronda": 2, "tipo": "PREGUNTA", "emisor": "tiburon_financiero", "destino": "emprendedor", "texto": "…", "estado": "Preguntando"}', size=9, color=GRAY)
p("Unity usa los campos así: **emisor** decide qué personaje habla, a dónde apunta la cámara y qué reflector se enciende; **tipo** y **estado** eligen la animación; **texto** es el subtítulo; **ronda** y **decision** actualizan el indicador de ronda y el marcador de votos; "
  "y **n** garantiza el orden (los eventos tienen numeración continua). Cuando el servicio espera al usuario, la respuesta de sondeo trae el campo **pendiente** (RESPUESTAS, NEGOCIACIÓN o CONDICIONES) y Unity muestra el panel correspondiente.", align="j")
tabla(["Ruta", "Para qué"], [
    ("GET /tiburones", "Catálogo de los 6 tiburones con su perfil"),
    ("GET /ejemplos", "Pitches de ejemplo para «Cargar ejemplo»"),
    ("POST /panel/recomendacion", "Recibe el formulario y devuelve la valuación y el ranking de tiburones con el motivo"),
    ("POST /sesiones", "Recibe el pitch y los 3 tiburones elegidos; devuelve el id de la sesión"),
    ("GET /sesiones/{id}/eventos?desde=n", "Eventos nuevos, estado de la sesión y qué espera del usuario"),
    ("POST /sesiones/{id}/respuestas", "Envía las 3 respuestas de la ronda y reanuda el grafo"),
    ("POST /sesiones/{id}/negociacion", "Envía el ajuste de oferta o la contraoferta aceptada"),
    ("POST /sesiones/{id}/condiciones", "Elige oferta original o contraoferta cuando hay trato"),
    ("GET /sesiones/{id}/reporte, /metricas", "Reporte final y métricas de la sesión"),
    ("GET /sesiones", "Sesiones pasadas para el historial"),
], anchos=[7.0, 10.0], size=9.5)

h2("7.5 Implementación preliminar del modelo")
p("Se implementaron dos niveles. El primero es **un agente resolviendo el problema**: un tiburón y el agente emprendedor se comunican con los mensajes del modelo (PITCH, PREGUNTA, RESPUESTA, VOTO, DECISIÓN) en un grafo de 5 nodos sobre Azure OpenAI. "
  "El segundo es el **sistema completo** de este documento (3 tiburones, réplica, voto secreto, negociación, reporte y API). Video de una corrida completa (2 min): " + VIDEO + ". Código y evidencia ejecutable: " + REPO + " (carpetas prototipo_un_agente y servicio; las transcripciones completas están en las carpetas sesiones y los comandos para repetir cada corrida en el README).", align="j")
p(f"**Grafo.** {F('grafo').capitalize()} es el grafo exportado del código. Los nodos son presentar, preguntar, responder, replicar, votar, decidir, condiciones, negociar, revotar y reporte; "
  "las aristas punteadas son condicionales (¿k < 3?, resultado de la votación). El estado de LangGraph guarda el pitch, el panel, la ronda, el historial público, los votos y la oferta vigente; "
  "el nodo «responder» usa interrupt para pausar el grafo hasta que el usuario responde.", align="j")
figura("grafo", 7.2, "Grafo de LangGraph exportado del código (aristas punteadas: condicionales).")

# tabla del prototipo de un agente
rows = []
for f in sorted((SERV.parent / "prototipo_un_agente" / "sesiones").glob("*.json")):
    d = json.load(open(f, encoding="utf8")); ev = d["eventos"]
    idea, tib = f.stem.replace("sesion_", "").rsplit("_", 1)[0].split("_", 1)[0], None
    nombre = f.stem.replace("sesion_", "")
    idea = nombre.split("_")[0]; tib = nombre[len(idea) + 1:].rsplit("_", 1)[0]
    voto = [e for e in ev if e["tipo"] == "VOTO"][-1]
    dec = [e for e in ev if e["tipo"] == "DECISION"][-1]["texto"]
    rondas = max(e["ronda"] for e in ev)
    from neuralhub.tiburones import TIBURONES
    crit = "sí" if menciona_criterio(voto["texto"], tib) else "no"
    rows.append((idea, TIBURONES[tib]["nombre"], d["llamadas"], rondas, d["segundos"], dec.title(), crit))
p("**Salida estructurada.** Cada agente devuelve su resultado con un esquema validado (Pydantic) y no como texto libre: la pregunta (Pregunta), la réplica (Replica con postura de acuerdo o desacuerdo), el voto (Voto con decisión, justificación y contraoferta opcional) y el reporte (Reporte con calificación de 1 a 5 por tiburón). Ejemplo de un evento real de voto producido por una corrida:", align="j", keep=True)
ev_demo = next(e for e in json.load(open(SERV / "sesiones" / "demo_ejemplo2.json", encoding="utf8"))["eventos"] if e["tipo"] == "VOTO" and e.get("votacion") == 1)
ev_demo = {k: ev_demo[k] for k in ("n", "ronda", "tipo", "emisor", "destino", "decision", "votacion", "texto")}
p(json.dumps(ev_demo, ensure_ascii=False), size=8.5, color=GRAY)
p("**Un agente resolviendo el problema.** Se corrió contra Azure OpenAI con 3 ideas y 2 tiburones distintos:", keep=True)
tabla(["Idea", "Tiburón", "Llamadas", "Rondas", "Segundos", "Decisión", "Menciona su criterio"], rows, anchos=[2.6, 3.6, 1.8, 1.6, 1.8, 3.6, 2.0], size=9)
p("Cada corrida usó 8 llamadas (pitch, 3 preguntas, 3 respuestas y 1 voto). Una ficha de las ideas (EcoTrack, GreenPlate, DataSense AI) viene en el código.", align="j")

# transcripción de una sesión real
demo = json.load(open(SERV / "sesiones" / "demo_ejemplo2.json", encoding="utf8"))
p("**Transcripción real (recortada)** de una sesión completa con el panel financiero, estratega y tecnológico (pitch DataSense AI, emprendedor en autopiloto). "
  f"La sesión usó {demo['metricas']['llamadas_modelo']} llamadas en {demo['metricas']['segundos_total']} s y terminó con negociación y «{demo['metricas']['resultado'].replace('_', ' ')}».", align="j", keep=True)
sel = []
vistos = set()
for e in demo["eventos"]:
    clave = (e["tipo"], e["emisor"]) if e["tipo"] in ("PREGUNTA",) else e["tipo"]
    if e["tipo"] in ("PREGUNTA", "RESPUESTA", "REPLICA", "VOTO", "NEGOCIACION", "OFERTA", "DECISION") and e["ronda"] in (0, 1, 3, 4) and (e["tipo"], e["ronda"]) not in vistos:
        vistos.add((e["tipo"], e["ronda"])); sel.append(e)
filas_t = []
for e in sel[:14]:
    t = e["texto"]; t = t if len(t) < 230 else t[:227].rsplit(" ", 1)[0] + "…"
    filas_t.append((e["n"], f"R{e['ronda']}", e["tipo"], f"{e['emisor'].replace('tiburon_', '').replace('tiburona_', '')} → {e['destino'].replace('tiburon_', '').replace('tiburona_', '')}", t))
tabla(["n", "Ronda", "Tipo", "Emisor → destino", "Texto"], filas_t, anchos=[0.9, 1.2, 2.4, 3.4, 9.1], size=8.5)
p(f"**Resultados de las {NS} sesiones de prueba** (5 pitches × 3 sesiones, guion fijo, Azure OpenAI gpt-4.1-mini):", keep=True)
fil_res = []
for pid in ("P1", "P2", "P3", "P4", "P5"):
    xs = [x for x in res if x["pitch_id"] == pid]
    fil_res.append((pid, xs[0]["tipo"].replace("_", " "), ", ".join(t[len("tiburon_" if t.startswith("tiburon_") else "tiburona_"):].capitalize() for t in xs[0]["panel"]),
                    "/".join(str(x["llamadas_modelo"]) for x in xs), f"{sum(x['segundos_total'] for x in xs) / 3:.1f}",
                    ", ".join(x["resultado"].replace("_", " ").title() for x in xs), "sí" if any(x["hubo_negociacion"] for x in xs) else "no"))
tabla(["Pitch", "Tipo", "Panel fijo", "Llamadas (3 ses.)", "Seg. prom.", "Resultados", "Negoc."], fil_res, anchos=[1.2, 2.6, 4.2, 2.4, 1.6, 3.6, 1.4], size=8.5)
p("**Evaluación repetida en el incremento 2** (mismo conjunto de 15 sesiones; cambios: estilo distintivo por tiburón y regla de citar una cifra en cada justificación):", keep=True)
tabla(["Métrica", "Incremento 1", "Incremento 2"], [
    ("Justificaciones con criterio y cifra concreta (objetivo 7)", f"{cd1_ok} de {cd1_tot} ({pct(cd1_ok, cd1_tot)})", f"{cd_ok} de {cd_tot} ({pct(cd_ok, cd_tot)})"),
    ("Preguntas que mencionan el criterio (objetivo 3)", f"{sum(x['preguntas_mencionan_criterio'] for x in res1)} de {sum(x['preguntas'] for x in res1)}", f"{p_crit} de {p_tot}"),
    ("Prueba ciega con juez automático (objetivo 2)", f"{ciega1['porcentaje']} %", f"{ciega['porcentaje']} %"),
    ("Trato en pitches sólidos (objetivo 8)", f"{sum(1 for x in res1 if x['tipo'] == 'solido' and x['resultado'] == 'TRATO')} de 6", f"{trato_sol} de {len(solidos)}"),
    ("Máximo de llamadas por sesión (objetivo 6)", str(max(x['llamadas_modelo'] for x in res1)), str(llam_max)),
    ("Tiempo de modelo promedio por sesión", f"{seg_prom1} s", f"{seg_prom} s"),
], anchos=[8.4, 4.3, 4.3], size=9)
p("**Prueba ciega por tiburón** (10 mensajes sin nombre por perfil; un juez LLM elige el autor entre los 6 perfiles):", keep=True)
tabla(["Tiburón", "Incremento 1", "Incremento 2"], [(TIBURONES[t]["nombre"], ciega1["por_tiburon"][t], ciega["por_tiburon"][t]) for t in TIBURONES], anchos=[6.0, 5.5, 5.5], size=9)
h3("Múltiples agentes funcionando en vivo")
p(f"Se probó el sistema completo de punta a punta: Unity (ejecutable compilado) contra el servicio real con Azure OpenAI, con un usuario automatizado que escribe las respuestas en los mismos campos que usa una persona. "
  f"La sesión {vivo['sesion']} tuvo {vivo['eventos']} eventos, {vivo['llamadas']} llamadas al modelo y terminó con «{vivo['resultado'].replace('_', ' ')}»; los tres tiburones preguntaron, replicaron entre ellos y votaron en secreto, y cada intervención se escuchó con su voz. "
  f"Video de la sesión en vivo: " + VIDEO2 + ".", align="j", keep=True)
tabla(["Momento", "Espera hasta la siguiente intervención de un agente"], [(x["tipo"].title(), f"{x['segundos']:.1f} s") for x in vivo["latencias"]], anchos=[6.0, 11.0], size=9)
figura("vivo", 13.0, "Sesión en vivo: un tiburón replica a otro mientras las pantallas muestran la oferta y el panel.")
p("Hallazgos del proceso: (a) el emprendedor en autopiloto inventaba cifras; se corrigió el prompt para que responda «aún no lo sé» y diga qué haría para averiguarlo; "
  "(b) una réplica se dirigía a un tiburón distinto del indicado; ahora el moderador asigna el destino; (c) el mapeo de respuestas por id fallaba en una ronda; ahora tiene un respaldo por posición.", align="j")

h2("7.6 Comunicación entre agentes y toma de decisiones")
p("**¿Toman decisiones?** Sí, en dos niveles. Cada tiburón decide de forma individual qué preguntar, si está de acuerdo o no con otro tiburón (RÉPLICA) y su voto (invertir, contraoferta o salir, con justificación y contraoferta numérica). "
  "La decisión colectiva la toma el nodo de votación con una regla explícita: mayoría de 2 de 3 a favor; con un solo voto a favor se abre una negociación y una segunda votación. El emprendedor decide qué responder y qué condiciones acepta. "
  "El moderador no decide el resultado: solo fija el orden de turnos y a quién se dirige cada réplica.", align="j")
p(f"**¿Qué usan para hablar entre ellos?** No hay sockets entre agentes: se comunican por **memoria compartida tipo pizarra**, el estado del grafo de LangGraph. Cada mensaje es un evento tipado {{n, ronda, tipo, emisor, destino, texto}} que se agrega al historial público; "
  "los votos se guardan en una parte privada del estado y solo el nodo de votación los publica. Cada agente produce su mensaje con **salida estructurada** (esquemas Pydantic) validada antes de entrar al estado, así que el contenido siempre tiene la forma esperada. "
  f"Hacia fuera hay dos protocolos: HTTPS con Azure OpenAI para razonar y una **API REST con JSON** (FastAPI) que Unity consulta cada segundo; cuando el grafo necesita al usuario se pausa con interrupt y se reanuda con Command(resume). {F('arq').capitalize()} resume la arquitectura.", align="j")
figura("arq", 16.5, "Arquitectura de comunicación: pizarra compartida de LangGraph, salida estructurada, API REST y voz local.")
p("**Equivalencia con FIPA-ACL.** Aunque no se usa una plataforma FIPA, cada tipo de mensaje corresponde a un acto comunicativo estándar [17]:", keep=True)
tabla(["Mensaje NeuralHub", "Acto FIPA-ACL", "Emisor → receptor", "Efecto en el estado"], [
    ("PITCH", "inform", "Emprendedor → tiburones", "Se agrega al historial público y a la pantalla"),
    ("PANEL / turnos", "inform", "Moderador → todos", "Fija el orden de preguntas y quién replica"),
    ("PREGUNTA", "query-ref / request", "Tiburón → emprendedor", "El grafo espera la respuesta (interrupt)"),
    ("RESPUESTA", "inform", "Emprendedor → tiburón", "Se agrega al historial; el grafo se reanuda"),
    ("RÉPLICA", "agree / refuse (inform)", "Tiburón → tiburón", "Acuerdo o desacuerdo visible para todos"),
    ("VOTO", "propose / reject-proposal", "Tiburón → nodo de votación", "Se guarda en privado hasta la decisión"),
    ("CONTRAOFERTA", "propose", "Tiburón → emprendedor", "Viaja dentro del voto"),
    ("OFERTA / CONDICIONES", "accept-proposal / propose", "Emprendedor → tiburones", "Cambia la oferta vigente"),
    ("DECISIÓN", "inform", "Nodo de votación → todos", "Cierra la sesión y dispara el reporte"),
], anchos=[3.4, 3.6, 4.4, 5.6], size=9)
p("**Por qué así.** Una pizarra compartida con turnos fijos garantiza el orden (los eventos tienen n consecutivo), permite repetir y auditar cada sesión y controla el costo (cada mensaje es una llamada conocida). "
  "Los votos privados evitan que un tiburón se deje llevar por otro, y la regla determinista hace que la decisión final sea explicable.", align="j")

# ---------- 8 modelado gráfico ----------
h1("8. Descripción del modelado gráfico")
h2("8.1 Escena a modelar")
def color_par(texto):
    par = doc.add_paragraph(); par.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    for t in re.split(r"(\{[nva]\|[^}]+\})", texto):
        if not t: continue
        if t.startswith("{"):
            tipo, txt = t[1], t[3:-1]; r = par.add_run(txt); r.bold = True
            r.font.color.rgb = {"n": VIOLET, "v": BLUE, "a": GREEN}[tipo]
        else:
            par.add_run(t)
    return par
color_par("Se ve un {n|estudio de televisión} {a|submarino}, una {n|pecera} {a|profunda} con {n|arena}, {n|rocas}, {n|corales}, {n|algas} que se mecen, {n|peces}, {n|medusas} y {n|burbujas}. "
          "Al fondo, una {a|gran} {n|pantalla} {v|muestra} el nombre del {n|negocio}, el {n|monto} {a|solicitado} y el {n|porcentaje} {a|ofrecido}. "
          "Al frente, el {n|emprendedor}, un {n|pez payaso} {a|naranja} que es el avatar del {n|usuario}, {a|de pie} junto al {n|podio} de coral sobre un {n|círculo} {a|luminoso}, {v|presenta} su idea, {v|responde} las preguntas y {v|negocia}. "
          "Detrás, {a|tres} {n|tiburones} {a|empresarios} con {n|traje} y {n|corbata}, {a|sentados} en {n|sillones} {n|concha} {a|fijos} en semicírculo, cada uno con su {n|escritorio} y un {n|letrero} con su nombre, {v|escuchan}, {v|preguntan}, {v|replican} a otro tiburón, {v|votan} y {v|reaccionan}: el personaje de cada sillón {v|cambia} según el panel elegido. "
          "Una {n|cámara} {a|principal} {v|cambia} de plano para {v|enfocar} a quien habla y un {n|reflector} {v|ilumina} al agente en turno. En la parte inferior se ve un {n|cuadro de diálogo} con el texto de cada turno, un {n|indicador de ronda} y un {n|marcador} con los votos; "
          "al final aparece un {n|letrero de resultado}: trato o sin trato.")
p("**Novedades del incremento 2 en la escena.** Iluminación submarina con cáusticas que se mueven en el piso y rayos de luz; dos pantallas laterales (datos de la oferta con el ícono del sector, y voto y calificación de cada tiburón); "
  "voz propia para cada personaje sincronizada con el subtítulo; ambiente y efectos de sonido; navegación por la conversación (anterior, siguiente, ir a un turno); y dictado por voz de las respuestas.", align="j")
p("Siguiendo el método de diseño del curso, marcamos en **violeta** los sustantivos (clases), en **azul** los verbos (métodos) y en **verde** los adjetivos (atributos). Con esta lista se armó el diagrama de clases.", align="j")
p("**Controles del usuario.** El usuario controla su pitch, la elección del panel, sus respuestas y su decisión final; nunca a los tiburones.", align="j", keep=True)
tabla(["Pantalla", "Qué ve", "Controles"], [
    ("Inicio", "Logo, estado de conexión con el servicio", "Nueva sesión, Historial de sesiones, interruptor «Usar sesión de ejemplo (sin conexión)»"),
    ("Formulario del pitch", "Campos del pitch y la valuación calculada al momento", "Campos de texto y numéricos, «Cargar ejemplo», «Siguiente» (activo solo con los obligatorios)"),
    ("Elección del panel", "Tarjetas de los 6 tiburones con perfil y criterio; los 3 recomendados con el motivo", "Seleccionar o quitar tarjetas (exactamente 3) e «Iniciar sesión»"),
    ("Sesión en el estudio", "Cámara sobre quien habla, subtítulos con voz, indicador de ronda, «Turno k/N», marcador de votos y pantallas con datos", "Anterior, Pausa/Reanudar, Siguiente, 1x/2x, Voz, Silencio, Historial, Salir; campos de respuesta con «Dictar con voz» y «Enviar respuestas»"),
    ("Negociación", "Contraofertas y resultado de la primera votación", "Aceptar contraoferta, ajustar monto y porcentaje, mantener mi oferta; elegir condiciones finales"),
    ("Resultado y reporte", "Trato o sin trato, voto y justificación de cada tiburón, reporte y criterios no evaluados", "Ver historial completo, Repetir con este pitch, Nueva sesión"),
    ("Conversación e historial", "Todos los turnos de la sesión (los futuros atenuados) y la lista de sesiones guardadas", "«Ir» a cualquier turno para volver a verlo; «Repetir sesión»"),
], anchos=[3.2, 6.8, 7.0], size=9)
p(f"**Expectativa y realidad.** {F('boceto').capitalize()} es el boceto inicial visto desde arriba (ahora con 3 sillones); {F('general')} y {F('arriba')} son el resultado en Unity. "
  "Las capturas de las pantallas (Figuras " + f"{NUM['s_inicio']} a {NUM['s_resultado']}) son del ejecutable compilado, con la sesión de ejemplo.", align="j")
figura("boceto", 11.5, "Boceto de la escena vista desde arriba (3 sillones).")
figura("general", 13.5, "Estudio-pecera en Unity: plano general con el panel de 3 tiburones empresarios.")
figura("arriba", 11.5, "Estudio en Unity visto desde arriba, para compararlo con el boceto.")
figura("s_inicio", 11.5, "Pantalla de inicio con la sesión de ejemplo sin conexión.")
figura("s_form", 11.5, "Formulario del pitch con la valuación calculada.")
figura("s_panel", 11.5, "Elección del panel: 3 recomendados con su motivo.")
figura("s_pregunta", 11.5, "Una pregunta en curso: subtítulo, cámara sobre el tiburón y reflector.")
figura("s_replica", 11.5, "Una RÉPLICA de un tiburón a otro.")
figura("s_historial", 11.5, "Historial abierto durante la sesión.")
figura("s_votos", 11.5, "Marcador con los votos revelados.")
figura("s_decision", 11.5, "Decisión del panel.")
figura("s_resultado", 11.5, "Pantalla de resultado con votos y retroalimentación.")

h2("8.2 Componentes gráficos")
p("El ambiente se construye con un script de editor reproducible (menú NeuralHub → Construir estudio). En este incremento se agregaron recursos propios (sonidos sintetizados, íconos por sector y textura de cáusticas, generados con docs/generar_recursos_unity.py) "
  "y una fuente tipográfica con licencia abierta. Los modelos del Asset Store y los definitivos del equipo se integran en la tarea T25; la tabla al final indica autor, fuente, licencia y estado de cada uno.", align="j")
comp = [
    ("Estudio-pecera (piso, muros y niebla)", "Piso de arena, muros de agua profunda, niebla azul-verde y franjas de luz; define el ambiente submarino.", "estudio_general", "Alberto López; primitivas de Unity (Assets/Editor/ConstruirEstudio.cs)."),
    ("Pantalla grande", "Marco, fondo y texto 3D con el negocio, el monto, el porcentaje y la valuación; muestra TRATO o SIN TRATO al final.", "estudio_general", "Alberto López; fuente de texto integrada de Unity (LegacyRuntime)."),
    ("Sillón concha y escritorio", "Sillón con respaldo en forma de concha marina y escritorio frontal; 3 fijos en semicírculo.", "tiburon_2", "Alberto López; primitivas de Unity."),
    ("Tiburón empresario", "Cuerpo con traje, camisa y corbata del color del perfil, cabeza con hocico, ojos y dientes, aleta dorsal y cola.", "tiburon_1", "Alberto López; primitivas de Unity (a reemplazar o complementar con los modelos de Juan Esteban)."),
    ("Pez payaso emprendedor y podio de coral", "Avatar del usuario: pez payaso naranja con franjas blancas junto a un podio de coral.", "emprendedor", "Alberto López; primitivas de Unity."),
    ("Algas, coral, rocas, peces y medusas", "Algas y medusas que se mecen (script Ondular), peces que nadan en círculos (script Nadador) y burbujas (sistema de partículas).", "tiburon_3", "Alberto López; primitivas y sistema de partículas de Unity."),
    ("Pantallas laterales con datos", "Izquierda: monto, porcentaje, valuación, ventas, margen e ícono del sector. Derecha: voto (color) y calificación (barra) de cada tiburón.", "inc2_general", "Juan Esteban Jaramillo; primitivas y TextMesh de Unity."),
    ("Iluminación submarina", "Luz direccional con textura de cáusticas que se mueve (script Caustica), rayos de luz aditivos y luz ambiental de tres tonos.", "inc2_general", "Juan Esteban Jaramillo; textura de cáusticas propia (generada con numpy)."),
    ("Íconos por sector y sonidos", "7 íconos (energía, tecnología, alimentos, salud, educación, moda, servicios) y 7 sonidos (ambiente, clic, turno, voto, trato, sin trato, burbujas).", "inc2_general", "Equipo; creación propia con PIL y síntesis con numpy (sin licencias de terceros)."),
]
for n, d, im, fu in comp:
    ficha_tabla([("Nombre", n), ("Descripción", d), ("Imagen", ""), ("Fuente", fu)], imagen=im, ancho_img=6.5)
p("**Assets externos y su estado.** Solo la fuente Poppins está integrada; los paquetes del Asset Store se agregaron a la cuenta del equipo y se importan en la tarea T25. La licencia de cada uno se confirma en su página antes de usarlo.", align="j", keep=True)
tabla(["Asset", "Uso previsto", "Autor", "Fuente", "Licencia", "Entrega"], [
    ("Poppins (Regular y SemiBold)", "Tipografía de la interfaz", "Indian Type Foundry / The Poppins Project Authors", "https://github.com/google/fonts/tree/main/ofl/poppins", "SIL Open Font License 1.1 (archivo OFL.txt incluido)", "Integrado (inc. 2)"),
    ("3D Shark – Character Free", "Modelo de tiburón para los personajes", "Publicador del Asset Store", "https://assetstore.unity.com/packages/3d/characters/animals/fish/3d-shark-character-free-190081", "Licencia estándar del Asset Store", "T25"),
    ("Low Poly Office Pack: Characters & Props", "Trajes y utilería de oficina", "Publicador del Asset Store", "https://assetstore.unity.com/packages/3d/characters/low-poly-office-pack-characters-props-119386", "Licencia estándar del Asset Store", "T25"),
    ("Low Poly Simple Furniture FREE; Chair y Table pack (FastMesh)", "Escritorios y sillas del panel", "Publicadores del Asset Store", "https://assetstore.unity.com/packages/3d/props/furniture/low-poly-simple-furniture-free-240197", "Licencia estándar del Asset Store", "T25"),
    ("Fish Pack Animated (modelos 3D animados: peces, tiburón, ballena, mantarraya, delfín)", "Peces y fauna animada de fondo", "Quaternius", "https://quaternius.itch.io/lowpoly-animated-fish", "CC0 (indicada en la página de descarga)", "T25"),
    ("Paquetes de modelos 3D de Kenney (mobiliario y props)", "Mobiliario y decorado del estudio", "Kenney", "https://kenney.nl (apartado Assets)", "CC0 según la declaración general de Kenney; por confirmar en cada paquete", "2 (27 nov)"),
    ("Animaciones de personaje (sentarse, gesticular, celebrar)", "Animar a los tiburones y al pez payaso por estado", "Adobe Mixamo", "https://www.mixamo.com", "Términos de Adobe; por confirmar antes de usar", "3 (4 dic)"),
    ("Modelos propios de tiburones empresarios y pez payaso", "Sustituir las primitivas por modelos con rig", "Juan Esteban Jaramillo (equipo)", "Blender; se subirán al repositorio", "Propia del equipo", "2–3"),

], anchos=[4.2, 3.4, 2.4, 3.6, 2.4, 1.2], size=8)
h2("8.3 Prefabs")
pref = [
    ("Tiburon", "Personaje del panel. Se instancia 3 veces al iniciar la sesión, en los sillones; cambia su color, nombre y estado según el perfil elegido.", "tiburon_1", "Tiburon.cs"),
    ("Sillon", "Sillón con escritorio. Se coloca 3 veces en semicírculo mirando hacia el emprendedor.", "tiburon_2", "(ninguno; solo geometría)"),
    ("Emprendedor", "Avatar del usuario con el podio; cambia de animación según su estado.", "emprendedor", "Emprendedor.cs"),
]
for n, d, im, sc in pref:
    ficha_tabla([("Nombre", n), ("Descripción", d), ("Imagen", ""), ("Scripts", sc)], imagen=im, ancho_img=6.0)

h2("8.4 Scripts")
p("Código propio del equipo; no se reutilizó código externo. Las pantallas de la interfaz se dibujan con IMGUI, sin paquetes adicionales.", align="j")
scripts = [
    ("ClienteServicio.cs", "Habla con el servicio por HTTP (UnityWebRequest), sondea los eventos cada segundo, descarga la voz de cada turno y envía la voz del usuario para transcribirla; en modo sin conexión lee StreamingAssets.", "Lo usan AppNeuralHub y ReproductorSesion; envía respuestas, negociación y condiciones al servicio."),
    ("ReproductorSesion.cs", "Reproduce los eventos de uno en uno con pausa, velocidad, voz y navegación (anterior, siguiente, ir a un turno, que reconstruye la escena hasta ese punto).", "Lee la cola de ClienteServicio, pide el audio de cada turno y llama a Estudio, PantallasDatos y Sonidos."),
    ("Estudio.cs", "Coloca los 3 tiburones en los sillones y aplica cada evento a la escena (animación, cámara, reflector, pantalla).", "Controla Tiburon, Emprendedor, CamaraEstudio, Reflector y Pantalla."),
    ("CamaraEstudio.cs", "Cámara principal que cambia de plano (general, emprendedor, tiburón, arriba) con movimiento suave.", "La llama Estudio."),
    ("Tiburon.cs", "Máquina de estados del tiburón (Fuera del panel, Esperando, Escuchando, Preguntando, Evaluando, Replicando, Votando, Reaccionando) y su animación.", "Recibe órdenes de Estudio."),
    ("Emprendedor.cs", "Estados del emprendedor (Presentando, Esperando, Respondiendo, Negociando, Recibiendo resultado).", "Recibe órdenes de Estudio."),
    ("Reflector.cs / Pantalla.cs", "Spotlight que sigue al turno; pantalla grande que muestra la oferta y el resultado.", "Los llama Estudio."),
    ("AppNeuralHub.cs", "Controlador de la aplicación: navega entre pantallas y conecta servicio, reproductor y estudio; incluye el modo de capturas automáticas.", "Usa ClienteServicio, ReproductorSesion y las pantallas."),
    ("Pantallas.cs", "Clases MenuInicio, FormularioPitch, EleccionPanel, ControlReproduccion, PanelHistorial, InterfazDebate, PanelRespuestas, PanelNegociacion, PantallaResultado y ListaSesiones.", "Las dibuja AppNeuralHub; llaman a ClienteServicio."),
    ("Datos.cs / Estilos.cs", "Clases de datos (Evento, PitchDto, Pendiente…) y estilos con los colores del equipo; paneles y botones redondeados y fuente Poppins.", "Compartidos por todos los scripts."),
    ("Sonidos.cs", "Ambiente submarino en bucle, efectos (clic, turno, voto, trato, sin trato) y la voz de cada personaje; registra el audio para montar los videos.", "Lo usan ReproductorSesion y AppNeuralHub; los botones Voz y Silencio lo controlan."),
    ("GrabadorVoz.cs", "Graba la voz del usuario con el micrófono (16 kHz) y la convierte a WAV.", "Lo usa PanelRespuestas; el WAV se envía a ClienteServicio.Transcribir."),
    ("PantallasDatos.cs", "Pantallas laterales: datos de la oferta con ícono del sector y barras de voto y calificación por tiburón.", "La actualiza ReproductorSesion con PITCH, PANEL, VOTO y REPORTE."),
    ("Caustica.cs / Ondular.cs / Nadador.cs", "Movimiento de la luz de cáusticas, de algas y medusas, y de los peces de fondo.", "Independientes; los agrega el constructor de la escena."),
]
for n, d, i in scripts:
    ficha_tabla([("Nombre", n), ("Descripción", d), ("Interacciones", i)])

# ---------- 9 gestión ----------
h1("9. Gestión de proyecto")
p("Herramienta: **Google Sheets**, hoja «NeuralHub – Gestión» con las pestañas Product backlog, Sprint backlog, Calendario, Bitácoras de cada entrega y Participación.", align="j")
p("Enlace de la hoja: " + SHEET, align="j")
p("Repositorio de código: " + REPO, align="j")
h2("Calendario de sprints")
p("Calendario tentativo: las fechas oficiales de las entregas todavía no se han comunicado, así que se planeó con los viernes como cierre de cada sprint y se ajustará cuando se conozcan.", align="j", keep=True)
tabla(["Sprint", "Inicio", "Cierre", "Enfoque"], SPRINTS, anchos=[2.0, 3.2, 3.2, 8.6], size=9.5)
h2("Actividades de la entrega del anteproyecto (6 de noviembre de 2026)")
p("Las actividades se repartieron entre los cinco integrantes de forma equitativa (3 por persona) y de acuerdo con su rol. La creación del documento en Drive (6 nov, 13:39) está verificada en el historial de versiones; la última edición registrada (6 nov, 19:20) fue de la cuenta de Alberto.", align="j", keep=True)
tabla(["Actividad", "Responsable", "Fecha"], [(a, r, f) for a, r, f, _ in BITACORA_E1], anchos=[9.4, 4.4, 3.2], size=9)
h2("Actividades del primer incremento")
tabla(["Actividad", "Responsable", "Fecha"], BITACORA_E2, anchos=[9.4, 4.4, 3.2], size=9)
h2("Actividades del incremento 2")
tabla(["Actividad", "Responsable", "Fecha"], BITACORA_E3, anchos=[9.4, 4.4, 3.2], size=9)
h2("Sprint backlog y participación")
p("Cada integrante tiene al menos una tarea en cada una de las tres categorías (documentación y planeación, programación multiagente y programación en Unity). Tareas hechas y planeadas:", align="j", keep=True)
filas = []
for m in MIEMBROS:
    n = [sum(1 for t in TAREAS if t[3] == m and t[2] == c) for c in (D, B, U)]
    filas.append((m, n[0], n[1], n[2], sum(n)))
tabla(["Integrante", "Documentación y planeación", "Back-end (multiagente)", "Front-end (Unity)", "Total"], filas, anchos=[5.0, 3.6, 3.4, 3.2, 1.8], size=9.5)
p("Las tareas se repartieron entre los cinco integrantes según su rol. El código se integró y se subió al repositorio desde la cuenta del líder del proyecto. Lista de tareas:", align="j", keep=True)
tabla(["ID", "Tarea", "Categoría", "Responsable", "Fechas", "Estado"], [
    (t[0], t[1], {D: "Documentación", B: "Back-end", U: "Unity"}[t[2]], t[3], f"{t[4]} – {t[5]}", t[6]) for t in TAREAS],
    anchos=[1.0, 7.0, 2.4, 2.8, 2.2, 1.6], size=8.5)

# ---------- 10 resultados ----------
h1("10. Resultados")
p(f"**Incremento 2 (26 nov).** En las {NS} sesiones de prueba contra Azure OpenAI todas terminaron con decisión, con un máximo de {llam_max} llamadas por sesión (límite 19) y {seg_prom} s de modelo en promedio. "
  f"Las justificaciones con criterio y cifra subieron de {pct(cd1_ok, cd1_tot)} a {pct(cd_ok, cd_tot)} (objetivo 7 cumplido), hubo réplica en {r_tot} de {r_tot} rondas y el dueño del criterio débil votó «salir» o «contraoferta» en {dueno_sale} de {len(debiles)} sesiones. "
  f"En vivo, Unity y el servicio completaron una sesión de {vivo['eventos']} eventos con {lat_ok} de {len(lat)} esperas bajo 10 s (objetivo 9).", align="j")
p(f"**Lo que falta.** La prueba ciega con juez automático subió de {ciega1['porcentaje']} % a {ciega['porcentaje']} %; la versión con una persona (objetivo 2) y la prueba con 5 usuarios externos (objetivo 10) se hacen en el Sprint 3. "
  "Las métricas de criterio son indicadores automáticos por palabras clave. Los pitches de prueba y de ejemplo son ficticios, escritos por el equipo.", align="j")

# ---------- 11 referencias ----------
h1("11. Referencias")
refs = [
    "Y. Du, S. Li, A. Torralba, J. B. Tenenbaum y I. Mordatch, «Improving factuality and reasoning in language models through multiagent debate», en Proc. 41st Int. Conf. Machine Learning (ICML), PMLR, vol. 235, 2024, pp. 11733-11763.",
    "Y. Xiao, E. Sun, D. Luo y W. Wang, «TradingAgents: Multi-agents LLM financial trading framework», arXiv:2412.20138, 2024. [En línea]. Disponible en: https://arxiv.org/abs/2412.20138",
    "Tauric Research, «TradingAgents», repositorio de GitHub. [En línea]. Disponible en: https://github.com/TauricResearch/TradingAgents",
    "LangChain, «LangGraph documentation». [En línea]. Disponible en: https://langchain-ai.github.io/langgraph/",
    "S. Russell y P. Norvig, Artificial Intelligence: A Modern Approach, 4.ª ed. Hoboken, NJ, EE. UU.: Pearson, 2021.",
    "P. Gompers, W. Gornall, S. N. Kaplan e I. A. Strebulaev, «How do venture capitalists make decisions?», Journal of Financial Economics, vol. 135, n.º 1, pp. 169-190, 2020, doi: 10.1016/j.jfineco.2019.06.011. Versión previa: NBER Working Paper 22587, sep. 2016. [En línea]. Disponible en: https://www.nber.org/papers/w22587",
    "I. A. Strebulaev, «Jockeys trump horses in race to secure VC funding», VentureBeat, 2016. [En línea]. Disponible en: https://www.venturebeat.com/2016/05/12/jockeys-trump-horses-in-race-to-secure-vc-funding/",
    "INEGI, «Características de los negocios. Censos Económicos 2019», folleto, 2020. [En línea]. Disponible en: https://en.www.inegi.org.mx/contenidos/programas/ce/2019/doc/frrdf_ce19.pdf",
    "E. Griffith, «Why startups fail, according to their founders», Fortune, 25 sep. 2014 (datos de CB Insights). [En línea]. Disponible en: https://fortune.com/2014/09/25/why-startups-fail-according-to-their-founders/",
    "M. Jetter y K. Stockley, «Gender match and the gender gap in venture capital financing: Evidence from Shark Tank», IZA Discussion Paper n.º 14069, ene. 2021. [En línea]. Disponible en: https://docs.iza.org/dp14069.pdf",
    "Microsoft, «Azure OpenAI Service». [En línea]. Disponible en: https://learn.microsoft.com/azure/ai-services/openai/",
    "Unity Technologies, «Unity 6 (6000.3) Manual». [En línea]. Disponible en: https://docs.unity3d.com/6000.3/Documentation/Manual/",
    "S. Ramírez, «FastAPI». [En línea]. Disponible en: https://fastapi.tiangolo.com/",
    "Quaternius, «Fish Pack Animated» (asset candidato, licencia CC0). [En línea]. Disponible en: https://quaternius.itch.io/lowpoly-animated-fish",
    "Kenney, «Assets» y declaración de licencia CC0. [En línea]. Disponible en: https://kenney.nl/support",
    "Foundation for Intelligent Physical Agents, «FIPA ACL Message Structure Specification», SC00061G, 2002. [En línea]. Disponible en: http://www.fipa.org/specs/fipa00061/",
    "A. Radford, J. W. Kim, T. Xu, G. Brockman, C. McLeavey e I. Sutskever, «Robust speech recognition via large-scale weak supervision», arXiv:2212.04356, 2022.",
    "SYSTRAN, «faster-whisper», repositorio de GitHub. [En línea]. Disponible en: https://github.com/SYSTRAN/faster-whisper",
    "Microsoft, «Speech API (SAPI 5.4) overview». [En línea]. Disponible en: https://learn.microsoft.com/previous-versions/windows/desktop/ee125663(v=vs.85)",
    "The Poppins Project Authors, «Poppins», SIL Open Font License 1.1. [En línea]. Disponible en: https://github.com/google/fonts/tree/main/ofl/poppins",
]
for i, r in enumerate(refs, 1):
    par = doc.add_paragraph(); par.paragraph_format.left_indent = Cm(0.9); par.paragraph_format.first_line_indent = Cm(-0.9)
    rich(par, f"[{i}]  {r}", size=10)

# ---------- 12 anexos ----------
h1("12. Anexos")
h2("Anexo A. Evidencias y código")
bullets([
    "Repositorio: " + REPO + " (carpetas servicio, prototipo_un_agente, unity y docs).",
    "Video de la corrida (2 min): " + VIDEO,
    "Transcripciones completas y métricas: servicio/sesiones/ y servicio/resultados_prueba.json (15 sesiones); prototipo_un_agente/sesiones/ (6 corridas).",
    "Pruebas automáticas: servicio/tests (22 pruebas con el modelo simulado, incluidas las 27 combinaciones de votos y los endpoints de voz).",
    "Video de la sesión en vivo (Unity + servicio + voz): " + VIDEO2,
    "Evaluación del incremento 2: servicio/resultados_prueba.json (15 sesiones), servicio/resultados_prueba_ciega.json y pruebas/prueba_ciega_humana.csv (planilla para la prueba con una persona), servicio/resultados_vivo.json (latencias).",
    "Set de prueba: servicio/pruebas/pitches_prueba.json (5 pitches, paneles y guiones). Los pitches son ficticios.",
    "Hoja de gestión: " + SHEET,
    "Las llaves de Azure OpenAI se guardan solo en archivos .env ignorados por git; no aparecen en el repositorio ni en este documento.",
])
h2("Anexo B. Uso de IA generativa")
p("El equipo usó un asistente de IA generativa (Claude) como apoyo para programar el servicio y la escena, correr las pruebas, generar las figuras y redactar este documento; "
  "las fuentes se abrieron y verificaron, y los datos no confirmados se omitieron. Prompts principales:", align="j")
bullets([
    "«Corrijamos todo lo anterior y agreguemos lo nuevo, verifica todo con el perfil de mi maestra a partir de sus correcciones».",
    "«Cada pregunta/respuesta/voto debe cumplir los objetivos SMART; mide las llamadas al modelo y reporta lo que no se cumple».",
    "«Construye la escena de Unity con 3 sillones, el formulario, la elección del panel, la pausa, la velocidad y el historial».",
    "«Verifica en la fuente cada cifra de Contexto y problema antes de citarla».",
    "«Mejora la escena: mejor iluminación, pantallas con información, sonidos, voz y navegación por la conversación; luego evalúa el documento con varios revisores distintos y corrige».",
    "Instrucciones del sistema para los agentes: se encuentran en servicio/neuralhub/grafo.py (funciones sistema_tiburon y sistema_emprendedor y los prompts de cada nodo).",
])
# número de página en el pie
def pie():
    f = sec.footer.paragraphs[0]; f.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = f.add_run("NeuralHub · Incremento 2 · página "); r.font.size = Pt(9); r.font.color.rgb = GRAY
    for tag, txt in (("begin", None), (None, "PAGE"), ("end", None)):
        run = f.add_run(); run.font.size = Pt(9); run.font.color.rgb = GRAY
        if tag:
            e = OxmlElement("w:fldChar"); e.set(qn("w:fldCharType"), tag); run._r.append(e)
        else:
            e = OxmlElement("w:instrText"); e.set(qn("xml:space"), "preserve"); e.text = txt; run._r.append(e)
def encabezado():
    h = sec.header.paragraphs[0]; h.alignment = WD_ALIGN_PARAGRAPH.CENTER
    h.add_run().add_picture(str(IMG / "logo.jpg"), height=Cm(0.75))
    for txt, col in (("  Neural", NAVY), ("Hub", VIOLET), ("  ·  Equipo NeuralHub  ·  Incremento 2  ·  TC2008B", BLUE)):
        r = h.add_run(txt); r.bold = txt.strip() in ("Neural", "Hub"); r.font.size = Pt(9.5); r.font.color.rgb = col
    pPr = h._p.get_or_add_pPr(); b = OxmlElement("w:pBdr"); bot = OxmlElement("w:bottom")
    bot.set(qn("w:val"), "single"); bot.set(qn("w:sz"), "8"); bot.set(qn("w:space"), "4"); bot.set(qn("w:color"), "13B8F0"); b.append(bot); pPr.append(b)
encabezado()
pie()
assert usadas == FIGS, usadas
out = BASE / "NeuralHub_Incremento_2.docx"
doc.save(out)
print("guardado", out, out.stat().st_size // 1024, "KB;", len(usadas), "figuras")
