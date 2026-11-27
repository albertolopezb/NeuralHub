"""Construye NeuralHub_Primer_Incremento.docx con la identidad del equipo."""
import glob
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
SHEET = "https://docs.google.com/spreadsheets/d/1Lq0ONh_gI4ibv0SCWzO404WvHAJkELCffuVmeLKgAsw/edit"

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

FIGS = ["act", "clases_s", "clases_u", "agente_t", "agente_e", "est_t", "est_es", "vot", "com", "sec", "grafo", "vivo", "arq", "boceto", "general", "arriba",
        "s_inicio", "s_form", "s_panel", "s_conocer", "s_intro", "s_pitch", "s_pregunta", "s_replica", "s_historial", "s_votos", "s_decision", "s_sin", "s_cierre", "s_resultado"]
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
    NOMBRES = {"act": "f01_actividades", "clases_s": "f05a_clases_servicio", "clases_u": "f05b_clases_unity", "agente_t": "f09a_agente_tiburon", "agente_e": "f09b_agente_emprendedor",
                                         "est_t": "f02_estados_tiburon", "est_es": "f08_estados_emprendedor_sesion", "vot": "f10_votacion", "com": "f03_comunicaciones",
                                         "sec": "f07_secuencia_ronda", "grafo": "f06_grafo_langgraph", "vivo": "vivo_replica", "arq": "f11_arquitectura_comunicacion", "boceto": "f04_boceto_escena", "general": "estudio_general",
                                         "arriba": "vista_arriba", "s_inicio": "01_inicio", "s_form": "02_formulario", "s_panel": "03_eleccion_panel",
                                         "s_pregunta": "05_pregunta", "s_replica": "06_replica", "s_historial": "07_historial", "s_votos": "08_votos",
                                         "s_decision": "09_decision", "s_resultado": "10_resultado", "s_conocer": "12_conocer", "s_intro": "14_intro_episodio", "s_pitch": "15_pitch", "s_sin": "17_sin_trato", "s_cierre": "16_cierre"}
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
p_tot = sum(x["preguntas"] for x in res); p_crit = sum(x["preguntas_mencionan_criterio"] for x in res); p_dato = sum(x["preguntas_citan_dato"] for x in res)
votos_inv = sum(1 for x in res for v in x["votos_primera"].values() if v == "invertir")
niveles = json.load(open(SERV / "resultados_niveles.json", encoding="utf8"))
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
ciega_prev = json.load(open(SERV / "sesiones" / "anterior_regla" / "resultados_prueba_ciega.json", encoding="utf8"))["resumen"]
ciega_ant = json.load(open(SERV / "sesiones" / "anterior_inc2" / "resultados_prueba_ciega.json", encoding="utf8"))["resumen"]


def _condiciones_debiles():
    """Sesiones débiles (P3 y P4) que cerraron trato, y cuántas con una contraoferta (leído de cada sesión)."""
    trato = contra = 0
    for f in sorted(glob.glob(str(SERV / "sesiones" / "prueba" / "P[34]_s*.json"))):
        d = [e for e in json.load(open(f, encoding="utf8"))["eventos"] if e["tipo"] == "DECISION"][-1]
        if d["resultado"] == "TRATO":
            trato += 1
            contra += d["condiciones"]["origen"] != "original"
    return trato, contra
deb_trato, deb_contra = _condiciones_debiles()
vivo = json.load(open(SERV / "resultados_vivo.json", encoding="utf8"))
lat = [x["segundos"] for s_ in vivo["sesiones"] for x in s_["latencias"]]
lat_ok = sum(1 for x in lat if x < 10)
ev_vivo = sum(s_["eventos"] for s_ in vivo["sesiones"])
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
     "Se reclasificó como dinámico: en la votación los tres tiburones deliberan al mismo tiempo, así que los otros actúan mientras uno decide (sus votos se publican juntos al terminar los tres), y entre la primera y la segunda votación la oferta vigente cambia por una acción del usuario que el tiburón no controla. La razón se explica en la tabla del entorno.", "7.2"),
], anchos=[5.6, 9.2, 2.2])
h2("Correcciones del anteproyecto (atendidas en el primer incremento)")
p("Se conservan para dar trazabilidad. Cada comentario de la retroalimentación del anteproyecto (80/100) y la sección donde se atendió:", align="j", keep=True)
tabla(["Comentario de la profesora", "Cómo se atendió", "Sección"], [
    ("El problema es genérico; falta fundamentar con datos cómo deciden los inversionistas reales.",
     "Se reescribió con datos verificados: encuesta a 885 inversionistas de riesgo, cifras de evaluación y cierre de tratos, fracaso de startups y supervivencia de negocios en México.", "3"),
    ("Los objetivos específicos no tienen métrica.",
     "Diez objetivos SMART (desde el primer incremento) con sprint, métrica con umbral, conjunto de prueba fijo y estado medido.", "4"),
    ("Contradicción: en las creencias el tiburón conoce los votos emitidos, pero en el entorno no.",
     "La votación es secreta y simultánea. Las creencias ya no incluyen votos ajenos; solo después de publicarse la primera votación (en la negociación) conoce quién votó qué y por qué.", "7.1, 7.2"),
    ("Dicen que los tiburones debaten, pero no hay mensajes de tiburón a tiburón.",
     "Se creó el mensaje RÉPLICA (tiburón → tiburón), una por ronda, presente en el grafo, la tabla de mensajes, la figura de comunicaciones, el código y la escena.", "7.3, 7.5, 8.1"),
    ("Seis jueces son muchos (≈40 llamadas): ¿se van a seleccionar entre ellos?",
     "Hay 6 tiburones y en cada sesión participan 3, recomendados por una regla en Python (0 llamadas) y elegidos por el usuario. Presupuesto: máximo 19 llamadas con usuario humano; medido: máximo %d." % llam_max, "7, 7.3"),
    ("Escena: no se describen los controles del usuario.",
     "Se describen y se construyeron en Unity: formulario, elección de panel, respuestas, negociación, pausa, velocidad, anterior, siguiente e ir a un turno, voz y silencio, historial y repetir sesión.", "8.1"),
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
p("**Alcance de este incremento.** Múltiples agentes funcionando en vivo: la app de Unity conversa con el servicio real y se documenta cómo se comunican y deciden los agentes. La sesión ahora funciona como un programa de televisión, «Aguas Profundas»: "
  "intro del episodio, el emprendedor presenta su pitch hablado y deja la petición para el final, los tiburones preguntan uno por uno y cada respuesta llega antes de la siguiente pregunta. Hay tres niveles de dificultad (Fácil, Medio y Difícil), "
  "cada tiburón tiene nombre, voz y apariencia propios, la pantalla del estudio muestra el tema de cada pregunta y el episodio termina con celebración o hundimiento y un cierre. El usuario escribe sus respuestas; también puede dictarlas con la voz (implementado; se probará con usuarios en el Sprint 3).", align="j")

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
  "son un indicador preliminar y se complementan con la prueba ciega con una persona del objetivo 2.", align="j")
tabla(["#", "Objetivo (específico, medible, alcanzable, relevante y con fecha)", "Estado medido (26 nov)"], [
    ("1", "**Recomendación del panel.** Para el final del Sprint 2 (27 nov), en los 3 pitches débiles o incompletos del conjunto de prueba, el panel recomendado incluye al tiburón dueño del criterio débil, sin usar el modelo (0 llamadas).",
     "3 de 3 pitches (100 %), 0 llamadas. Cumplido en prueba preliminar."),
    ("2", "**Personalidades distinguibles.** Para el final del Sprint 3 (4 dic), en una prueba ciega con 60 mensajes sin nombre (10 por tiburón), un integrante que no escribió los prompts identifica al tiburón autor en al menos el 80 % de los casos.",
     f"En curso. Indicador previo con juez automático: {ciega['aciertos']} de {ciega['mensajes']} ({ciega['porcentaje']} %) en la última corrida y {ciega_prev['porcentaje']} % en la anterior, con las mismas instrucciones de los tiburones: oscila alrededor de la meta y no es concluyente. Falta la prueba con una persona, que es la que mide el objetivo (servicio/pruebas/prueba_ciega_humana.csv, tarea T23)."),
    ("3", "**Análisis por criterio.** Para el final del Sprint 2, en las 15 sesiones, al menos el 85 % de las preguntas aborda el criterio principal del tiburón que pregunta y cita un dato del pitch o señala que falta.",
     f"{p_crit} de {p_tot} ({pct(p_crit, p_tot)}) abordan su criterio. La cita de un dato quedó en {p_dato} de {p_tot} ({pct(p_dato, p_tot)}) según el indicador automático, debajo del 85 %. El indicador solo reconoce cifras, comillas o frases como «no me dices»; no cuenta datos citados con palabras (por ejemplo, «son dos fundadoras»). Cumplido en parte; falta revisar las preguntas a mano."),
    ("4", "**Debate entre tiburones.** Para el final del Sprint 2, el 100 % de las rondas incluye una RÉPLICA de un tiburón dirigida a otro tiburón del panel, con acuerdo o desacuerdo explícito.",
     f"{r_tot} réplicas en {r_tot} rondas ({r_otro} dirigidas a otro tiburón; {r_pos} con postura explícita). Cumplido."),
    ("5", "**Decisión que siempre termina.** Para el final del Sprint 2, el 100 % de las sesiones termina con trato o sin trato en un máximo de 3 rondas más 1 de negociación, y la regla de votación pasa las 27 combinaciones posibles de votos.",
     f"{terminadas} de {NS} sesiones terminadas; 27 de 27 combinaciones. Cumplido."),
    ("6", "**Costo controlado.** Para el final del Sprint 3 (4 dic), ninguna sesión en la que responde el usuario sin ayuda usa más de 19 llamadas al modelo (28 como máximo con ayuda o si responde el agente empresario: hasta 9 respuestas o sugerencias, una por pregunta) y el servicio la completa en menos de 40 s sin contar las esperas del usuario.",
     f"Máximo {llam_max} llamadas en las {NS} sesiones ({n16} con 16 y {n19} con 19); tiempo total {seg_prom} s en promedio y {seg_max} s como máximo. En vivo con negociación, la sesión usó 28: 19 propias más 9 sugerencias pedidas al agente empresario. Cumplido en la prueba."),
    ("7", "**Justificación con evidencia.** Para el final del Sprint 3, al menos el 85 % de las justificaciones de voto menciona el criterio principal del tiburón y al menos un dato concreto de la conversación.",
     f"{cd_ok} de {cd_tot} ({pct(cd_ok, cd_tot)}); en el incremento 1 era {pct(cd1_ok, cd1_tot)}. Cumplido tras ajustar los prompts (T20)."),
    ("8", "**Coherencia con el perfil.** Para el final del Sprint 3, en los pitches débiles o incompletos el tiburón dueño del criterio débil vota «salir» o «contraoferta» en al menos el 80 % de las sesiones; en los pitches sólidos hay trato en al menos 4 de 6 sesiones.",
     f"{dueno_sale} de {len(debiles)} sesiones débiles o incompletas ({pct(dueno_sale, len(debiles))}); trato en {trato_sol} de {len(solidos)} sólidas. Cumplido según la métrica, pero con un límite: en esta corrida hubo {votos_inv} votos «invertir», así que el dueño del criterio solo podía contraofertar o salir. En P3 y P4 (débiles) también hubo trato en {deb_trato} de 6 sesiones; todas por contraoferta, porque sin votos «invertir» la regla ya no permite conservar la oferta original (el agente empresario siempre toma la mejor; una persona puede rechazarlas). El panel castiga con condiciones, no siempre con un rechazo."),
    ("9", "**Integración con Unity.** Para el final del Sprint 3, el servicio emite en orden el 100 % de los eventos de las 15 sesiones, Unity completa sesiones en vivo, y en el 90 % de los envíos del usuario la siguiente intervención llega en menos de 10 s.",
     f"Eventos en orden en {ordenok} de {NS} sesiones; 3 sesiones en vivo en Unity ({ev_vivo} eventos) con {lat_ok} de {len(lat)} envíos bajo 10 s (máximo {max(lat):.1f} s, promedio {vivo['promedio_s']:.1f} s), con un usuario automatizado que contesta con la sugerencia del agente empresario (grabadas antes del ajuste de la regla de contraofertas). Cumplido en la prueba."),
    ("10", "**Retroalimentación útil.** Para la entrega final, al menos 5 usuarios ajenos al equipo usan la app con su propia idea y al menos el 80 % califica el reporte con 4 o 5 de 5 en utilidad.",
     "Pendiente (tarea T31)."),
], anchos=[0.8, 10.4, 5.8], size=9)

# ---------- 5 restricciones ----------
h1("5. Restricciones")
bullets([
    "**Agentes:** catálogo de seis tiburones con perfil fijo definido por el equipo; en cada sesión participan exactamente 3. Responde el usuario emprendedor (modo en vivo) o su agente empresario (modo observador y pruebas automáticas). El usuario no puede crear ni editar tiburones. El moderador y la votación son lógica determinista del grafo, no agentes.",
    "**Debate acotado:** máximo 3 rondas; en cada una los 3 tiburones preguntan uno por uno, cada pregunta se responde antes de la siguiente y un tiburón replica a otro. Hay una ronda extra de negociación solo si hay un voto a favor.",
    "**Niveles:** Fácil, Medio y Difícil cambian cómo preguntan, replican y votan los tiburones; las reglas de la sesión y la regla de votación no cambian.",
    "**Pitch del usuario:** el usuario escribe su pitch en un formulario con campos obligatorios y un límite de 300 caracteres por campo; las respuestas tienen máximo 600 caracteres.",
    "**Costo y latencia:** máximo 19 llamadas al modelo por sesión si responde el usuario sin ayuda (16 sin negociación) y 28 con ayuda o si responde el agente empresario (una sugerencia o respuesta por pregunta). El pitch hablado, la recomendación del panel, la valuación y la regla de votación no usan el modelo.",
    "**Idioma y moneda:** todo el diálogo es en español y los montos están en pesos mexicanos.",
    "**Una sola escena:** un estudio de televisión submarino con 3 podios fijos y una cámara que cambia de plano; el tiburón de cada podio cambia según el panel elegido.",
    "**Comunicación solo por HTTP:** Unity y el servicio se comunican únicamente por HTTP; los agentes no corren dentro de Unity.",
    "**Interfaz:** el usuario elige nivel, captura su pitch, elige panel y modo, responde por escrito o con dictado, decide en la negociación y controla la reproducción (anterior, pausa, siguiente, ir a un turno, 1x/2x, voz, silencio, historial). No controla a los tiburones ni su orden de turnos.",
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
  f"y una aplicación en Unity que muestra la escena. {F('clases_s').capitalize()} y {F('clases_u')} son los diagramas de clases del servicio y de Unity.", align="j")
h2("Idea general")
p("LangGraph modela la sesión como un grafo: cada paso es un nodo, las rondas son un ciclo y la votación es un nodo determinista [4]. Se eligió porque admite ciclos y pausas para esperar al usuario (interrupt) y porque nosotros fijamos el orden de turnos, "
  "el máximo de rondas y la regla de decisión. Una sesión sigue estos pasos (Figura 1):", align="j")
bullets([
    "El usuario elige un nivel (cada uno trae un emprendedor de ejemplo) o escribe su propio pitch en Unity. Python calcula la valuación implícita (monto ÷ porcentaje).",
    "La app recomienda 3 tiburones con una regla en Python (sin modelo), según los datos que le faltan al pitch y la afinidad con el sector y la etapa. El usuario acepta o cambia el panel (puede conocer a cada tiburón) y elige quién responde: él o su agente empresario.",
    "Empieza el episodio: la intro presenta la empresa y el panel, y el emprendedor dice su pitch como en televisión (saludo, problema, solución, avance y, al final, cuánto pide).",
    "El moderador (lógica determinista) fija el orden: empieza el tiburón con más datos faltantes en su criterio.",
    "En cada ronda (3 en total) los tiburones preguntan uno por uno; el grafo se pausa hasta que llega la respuesta de esa pregunta. Al final de la ronda un tiburón replica a otro (RÉPLICA) y el moderador rota quién replica.",
    "Los 3 tiburones votan en secreto y en paralelo: invertir, contraoferta o salir, con una justificación breve.",
    "El nodo de votación revela los votos y aplica la regla: con 2 o 3 a favor se eligen condiciones (la oferta original solo si 2 o más votaron «invertir»; si no, una contraoferta, o el usuario las rechaza y no hay trato); con 1 a favor hay una negociación y una segunda votación; con 0 no hay trato.",
    "Un último paso genera el reporte; Unity muestra la celebración o el hundimiento, el cierre del episodio y la retroalimentación.",
], style="List Number")
figura("act", 7.8, "Diagrama de actividades de una sesión.")
figura("clases_s", 17.0, "Diagrama de clases del servicio de agentes (Python).")
figura("clases_u", 16.5, "Diagrama de clases de la aplicación de Unity (C#).")

h2("7.1 Modelo de los agentes")
p("El sistema tiene dos tipos de agente con modelo de lenguaje (tiburones y agente empresario), dos funciones deterministas (moderador y votación) y un paso de reporte que usa el modelo una vez. Corre sobre Azure OpenAI (gpt-4.1-mini) [11]. Los perfiles de los tiburones son arquetipos inspirados en el estilo de los inversionistas de Shark Tank México, aunque no son copias.", align="j", keep=True)
tabla(["Agente", "Perfil", "Criterio principal"], [
    ("**Emprendedor** (usuario humano o su agente empresario)", "Defiende su idea y busca el trato cediendo el menor porcentaje", "Responde con los datos de su ficha; si falta un dato, lo dice"),
    ("**Tiburón financiero**", "Escéptico; solo invierte si los números cuadran", "Márgenes, costo de adquisición de clientes, valuación"),
    ("**Tiburón estratega**", "Piensa en el crecimiento a largo plazo", "Tamaño de mercado, competencia, ventaja defendible"),
    ("**Tiburón tecnológico**", "Le atraen las ideas disruptivas aunque tengan riesgo", "Innovación, tecnología, propiedad intelectual"),
    ("**Tiburona de consumo**", "Piensa en el cliente final", "Marca, canales de venta, demanda del producto"),
    ("**Tiburón mentor**", "Invierte en personas más que en cifras", "Equipo, compromiso y honestidad del fundador"),
    ("**Tiburón negociador**", "Duro con las condiciones", "Porcentaje de capital, valuación, contraofertas"),
    ("Moderador (nodo, sin modelo)", "Fija el orden de turnos y asigna quién replica a quién", "Primero pregunta quien tiene más huecos en su criterio"),
    ("Nodo de votación (nodo, sin modelo)", "Revela los votos y aplica la regla de decisión", "2 o 3 a favor: se eligen condiciones (trato, o sin trato si se rechazan las contraofertas); 1: negociación; 0: sin trato"),
], anchos=[4.6, 6.0, 6.4], size=9.5)
p("En total son 7 agentes con modelo de lenguaje (6 tiburones, de los cuales 3 participan por sesión, y el agente empresario) más el moderador y la votación, que son deterministas. En la app cada tiburón tiene un nombre y una personalidad visible: Ramiro (financiero), Estela (estratega), Teo (tecnológico), Coral (consumo), Aurelio (mentor) y Bruno (negociador). "
  "Cada tiburón se modela con estos elementos:", align="j")
bullets([
    "**Creencias:** su perfil y criterio, el pitch con la valuación calculada y el historial público de la sesión (preguntas, respuestas y réplicas). No conoce el razonamiento interno ni los votos de los demás mientras vota. En la negociación conoce los votos ya publicados de la primera votación (quién votó qué y por qué) y las contraofertas.",
    "**Nivel:** modifica sus planes: en Fácil pregunta por la historia y no pide cifras; en Medio hace preguntas prácticas desde su criterio; en Difícil exige la cifra exacta y es más estricto al votar.",
    "**Planes:** preguntar sobre el dato más débil de su criterio, evaluar la respuesta, replicar cuando el moderador se lo asigna, votar con una justificación y, si aplica, hacer una contraoferta.",
    "**Cooperación:** comparten el historial público. Coinciden o discrepan de forma explícita con el mensaje RÉPLICA y pueden coincidir en una misma contraoferta.",
    "**Aprendizaje:** no hay entrenamiento. Se adaptan con el contexto acumulado de la sesión; el estado de LangGraph guarda el historial y permite repetirla.",
])
p("El agente empresario tiene como creencias su ficha del pitch y el historial público; planea responder con los datos de su ficha (sin inventar cifras) y elegir la condición de mayor valuación implícita; no aprende. "
  f"{F('agente_t').capitalize()} y {F('agente_e')} muestran el modelo de cada agente (percepciones, creencias, metas, planes y acciones) y su entorno.", align="j")
figura("agente_t", 10.4, "Modelo del agente tiburón.")
figura("agente_e", 10.4, "Modelo del agente empresario.")
p(f"El diagrama de estados del tiburón ({F('est_t')}) agrega el estado «Replicando» y el estado «Fuera del panel» para los tiburones no elegidos; En Unity, el tipo de cada evento cambia el estado del personaje y el estado elige la animación (saludar al pedir la palabra, asentir o negar al replicar y votar, celebrar o encogerse). "
  f"{F('est_es').capitalize()} muestra los estados del emprendedor y de la sesión (moderador).", align="j")
figura("est_t", 12.4, "Diagrama de estados del agente tiburón.")
figura("est_es", 16.5, "Diagramas de estados del emprendedor y de la sesión.")
p(f"El nodo de votación y el moderador no usan el modelo de lenguaje; {F('vot')} muestra el diagrama de flujo del nodo de votación, que implementa la regla de decisión de la sección 7.3.", align="j", keep=True)
figura("vot", 8.8, "Diagrama de flujo del nodo de votación.")
p("**Acciones de los agentes**", keep=True)
tabla(["Agente", "Acción", "Precondición", "Efecto", "Mensaje", "Modelo"], [
    ("Usuario/Emprendedor", "Presentar pitch", "Formulario completo", "Se calcula la valuación y se muestra en pantalla", "PITCH", "No"),
    ("Moderador", "Fijar panel y orden", "3 tiburones elegidos", "Orden de turnos de la ronda", "PANEL", "No"),
    ("Tiburón", "Preguntar", "Es su turno en la ronda k ≤ 3", "Una pregunta desde su criterio", "PREGUNTA", "Sí"),
    ("Emprendedor", "Responder", "Llegó una pregunta", "La respuesta entra al historial", "RESPUESTA", "No (usuario) / Sí (agente)"),
    ("Tiburón", "Replicar", "Se le asignó replicar", "Acuerdo o desacuerdo hacia otro tiburón", "RÉPLICA", "Sí"),
    ("Tiburón", "Votar", "Terminó la ronda 3", "Voto secreto con justificación", "VOTO", "Sí"),
    ("Nodo de votación", "Revelar y decidir", "Hay 3 votos", "Trato, negociación o sin trato", "DECISIÓN / NEGOCIACIÓN", "No"),
    ("Emprendedor", "Elegir condiciones u ofertar", "2 o 3 a favor con contraofertas, o 1 voto a favor", "Condiciones finales, rechazo (sin trato) o nueva oferta", "OFERTA", "No"),
    ("Tiburón", "Votar de nuevo", "Hubo negociación", "Segundo voto (invertir o salir)", "VOTO", "Sí"),
    ("Servicio", "Generar reporte", "Hay decisión", "Retroalimentación por tiburón", "REPORTE", "Sí"),
], anchos=[2.6, 2.6, 3.0, 3.6, 2.2, 3.0], size=8.5)

h2("7.2 Modelo del entorno")
p("Clasificamos el entorno con las seis características de Russell y Norvig [5]:", keep=True)
tabla(["Característica", "Clasificación", "Razón"], [
    ("**Observabilidad**", "Parcialmente observable", "Todos ven el pitch y el historial público, pero no el razonamiento interno de los demás. La votación es secreta y simultánea: nadie ve otros votos hasta que el nodo de votación los publica."),
    ("**Determinismo**", "Estocástico", "El modelo de lenguaje puede responder distinto con la misma entrada; el orden de turnos, la recomendación del panel y la regla de decisión son deterministas."),
    ("**Episodios**", "Secuencial", "Cada pregunta, respuesta y réplica depende de lo anterior, y el voto depende de todo el debate."),
    ("**Cambio**", "Dinámico", "El entorno puede cambiar mientras un tiburón delibera: en la votación los otros dos actúan al mismo tiempo (los tres votan en paralelo y sus votos se publican juntos, sin verse antes), y en la negociación la oferta vigente cambia por una acción del usuario que el tiburón no controla. Fuera de la votación el grafo avanza por turnos y se pausa mientras el usuario responde. Se revisó tras la observación del primer incremento."),
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
    ("RESPUESTA", "Emprendedor → tiburón", "Respuesta del usuario (o de su agente) a esa pregunta"),
    ("RÉPLICA", "Tiburón → tiburón", "Acuerdo o desacuerdo con la pregunta de otro, visible para todos"),
    ("VOTO", "Tiburón → nodo de votación", "Invertir, contraoferta o salir, con justificación; secreto hasta que se publica"),
    ("CONTRAOFERTA", "Tiburón → emprendedor", "Monto y porcentaje propuestos; viaja dentro del VOTO"),
    ("NEGOCIACIÓN", "Nodo de votación → emprendedor", "Aviso de que solo hubo 1 voto a favor"),
    ("OFERTA", "Emprendedor → tiburones", "Nueva oferta en la negociación; con 2 o 3 a favor, la condición elegida o el rechazo (CONDICIONES)"),
    ("DECISIÓN", "Nodo de votación → todos", "Trato o sin trato, con las condiciones finales"),
    ("REPORTE", "Servicio → emprendedor", "Retroalimentación por tiburón y resumen"),
], anchos=[3.0, 5.0, 9.0], size=9.5)
p("**Regla de decisión.** «Invertir» y «contraoferta» cuentan como votos a favor y deciden la ruta. Con 2 o 3 a favor se pasa a elegir condiciones. Una contraoferta no es aceptar la oferta original: "
  "el emprendedor solo puede mantenerla si 2 o más votaron «invertir»; si no, acepta una de las contraofertas o las rechaza y termina sin trato. Si elige una contraoferta, esas son las condiciones finales. "
  "Este ajuste es de este incremento: antes se podía conservar la oferta original aunque nadie la hubiera aceptado. "
  "Con 1 a favor hay una ronda de negociación: el usuario ajusta su oferta o acepta una contraoferta y los 3 tiburones votan una sola vez más (invertir o salir); con 2 o más a favor hay trato. Con 0 a favor no hay trato.", align="j")
p("**Presupuesto de llamadas al modelo.** Antes eran unas 40 porque el emprendedor también era un agente y los seis tiburones preguntaban en cada ronda.", align="j", keep=True)
tabla(["Paso", "Llamadas"], [
    ("Pitch, valuación, recomendación del panel y orden de turnos (formulario y Python)", "0"),
    ("Preguntas: 3 rondas × 3 tiburones", "9"),
    ("Réplicas: 1 por ronda", "3"),
    ("Respuestas del usuario (si responde el agente empresario: 1 llamada por pregunta)", "0 (9)"),
    ("Votación (3 en paralelo)", "3"),
    ("Reporte final", "1"),
    ("**Sesión sin negociación**", "**16** (25 con agente)"),
    ("Segunda votación (solo con 1 voto a favor)", "+3"),
    ("**Máximo por sesión**", "**19** (28 con agente, o con ayuda usando una sugerencia por pregunta)"),
], anchos=[12.5, 4.5], size=9.5)
p(f"En las {NS} sesiones de prueba (emprendedor con guion fijo) se midieron {n16} sesiones con 16 llamadas y {n19} con 19, sin excepciones.", align="j")
p(f"El diagrama de comunicaciones ({F('com')}) incluye la flecha de tiburón a tiburón (RÉPLICA) y {F('sec')} muestra la secuencia de una ronda.", align="j")
figura("com", 16.0, "Diagrama de comunicaciones entre los agentes.")
figura("sec", 16.0, "Diagrama de secuencia de una ronda.")

h2("7.4 Modelo de la interacción")
p("Unity no ejecuta a los agentes. El servicio expone una API REST con JSON (FastAPI) [13]; guarda cada turno como un evento y Unity los pide por HTTP (UnityWebRequest) aproximadamente cada segundo, los guarda en una cola y los reproduce de uno en uno. Formato del evento:", align="j", keep=True)
p('{"n": 10, "ronda": 2, "tipo": "PREGUNTA", "emisor": "tiburon_financiero", "destino": "emprendedor", "texto": "…", "estado": "Preguntando"}', size=9, color=GRAY)
p("Unity usa los campos así: **emisor** decide qué personaje habla, a dónde apunta la cámara y qué reflector se enciende; **tipo** elige la animación (el campo **estado** acompaña al diagrama de estados); **texto** es el subtítulo; **ronda** y **decision** actualizan el indicador de ronda y el marcador de votos; "
  "y **n** garantiza el orden (los eventos tienen numeración continua). Cuando el servicio espera al usuario, la respuesta de sondeo trae el campo **pendiente** (RESPUESTAS, NEGOCIACIÓN o CONDICIONES) y Unity muestra el panel correspondiente. En el código los tipos van sin acento (REPLICA, NEGOCIACION, DECISION).", align="j")
tabla(["Ruta", "Para qué"], [
    ("GET /tiburones", "Catálogo de los 6 tiburones con su perfil"),
    ("GET /ejemplos", "Pitches de ejemplo para «Cargar ejemplo»"),
    ("POST /panel/recomendacion", "Recibe el formulario y devuelve la valuación y el ranking de tiburones con el motivo"),
    ("POST /sesiones", "Recibe pitch, 3 tiburones, modo, ayuda y nivel; devuelve el id de la sesión"),
    ("GET /sesiones/{id}/eventos?desde=n", "Eventos nuevos, estado de la sesión y qué espera del usuario"),
    ("POST /sesiones/{id}/respuestas", "Envía la respuesta de la pregunta en turno y reanuda el grafo"),
    ("POST /sesiones/{id}/sugerencia", "Modo en vivo con ayuda: el agente empresario propone una respuesta"),
    ("GET /sesiones/{id}/audio/{n}", "Audio (WAV) con la voz del turno n"),
    ("POST /voz/transcribir", "Recibe un WAV y devuelve el texto (Whisper local)"),
    ("GET /catalogos, GET /salud", "Listas de sectores y etapas; prueba de conexión"),
    ("POST /sesiones/{id}/negociacion", "Envía el ajuste de oferta o la contraoferta aceptada"),
    ("POST /sesiones/{id}/condiciones", "Elige la oferta original (si tiene mayoría) o una contraoferta, o las rechaza"),
    ("GET /sesiones/{id}/reporte, /metricas", "Reporte final y métricas de la sesión"),
    ("GET /sesiones", "Sesiones pasadas para el historial"),
], anchos=[7.0, 10.0], size=9.5)

h2("7.5 Implementación preliminar del modelo")
p("Se implementaron dos niveles. El primero es **un agente resolviendo el problema**: un tiburón y el agente emprendedor se comunican con los mensajes del modelo (PITCH, PREGUNTA, RESPUESTA, VOTO, DECISIÓN) en un grafo de 5 nodos sobre Azure OpenAI. "
  "El segundo es el **sistema completo** de este documento (3 tiburones, réplica, voto secreto, negociación, reporte y API). Video del primer incremento (corrida completa, 2 min): " + VIDEO + ". Código y evidencia ejecutable: " + REPO + " (carpetas prototipo_un_agente y servicio; las transcripciones completas están en las carpetas sesiones y los comandos para repetir cada corrida en el README).", align="j")
p(f"**Grafo.** {F('grafo').capitalize()} es el grafo exportado del código. Los nodos son presentar, preguntar, responder, replicar, votar, decidir, condiciones, negociar, revotar y reporte. preguntar y responder se repiten hasta que los 3 tiburones preguntaron; la lógica del moderador vive en presentar, preguntar y replicar, y la votación en decidir y revotar. "
  "Las aristas punteadas son condicionales (¿faltan tiburones?, ¿k < 3?, resultado de la votación). El estado de LangGraph guarda el pitch, el panel, la ronda, el historial público, los votos y la oferta vigente; "
  "el nodo «responder» usa interrupt para pausar el grafo hasta que el usuario responde.", align="j")
figura("grafo", 5.8, "Grafo de LangGraph exportado del código (aristas punteadas: condicionales).")

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
p("**Transcripción real (recortada)** de una sesión completa con el panel financiero, estratega y tecnológico (pitch DataSense AI, responde el agente empresario). "
  f"La sesión usó {demo['metricas']['llamadas_modelo']} llamadas en {demo['metricas']['segundos_total']} s y terminó en «{demo['metricas']['resultado'].replace('_', ' ')}»" + (" después de negociar." if demo["metricas"]["hubo_negociacion"] else " sin negociación."), align="j", keep=True)
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
p("**Evaluación repetida en el incremento 2** (mismo conjunto de 15 sesiones, nivel Medio; cambios: estilo por tiburón, cifra obligatoria en cada voto, preguntas una por una y niveles):", keep=True)
tabla(["Métrica", "Incremento 1", "Incremento 2"], [
    ("Justificaciones con criterio y cifra concreta (objetivo 7)", f"{cd1_ok} de {cd1_tot} ({pct(cd1_ok, cd1_tot)})", f"{cd_ok} de {cd_tot} ({pct(cd_ok, cd_tot)})"),
    ("Preguntas que mencionan el criterio (objetivo 3)", f"{sum(x['preguntas_mencionan_criterio'] for x in res1)} de {sum(x['preguntas'] for x in res1)}", f"{p_crit} de {p_tot}"),
    ("Prueba ciega con juez automático (objetivo 2)", "Sin corrida guardada", f"{ciega['porcentaje']} %"),
    ("Trato en pitches sólidos (objetivo 8)", f"{sum(1 for x in res1 if x['tipo'] == 'solido' and x['resultado'] == 'TRATO')} de 6", f"{trato_sol} de {len(solidos)}"),
    ("Máximo de llamadas por sesión (objetivo 6)", str(max(x['llamadas_modelo'] for x in res1)), str(llam_max)),
    ("Preguntas que citan un dato o dicen que falta (objetivo 3)", "No se medía", f"{p_dato} de {p_tot}"),
    ("Tiempo total promedio por sesión", f"{seg_prom1} s", f"{seg_prom} s"),
], anchos=[8.4, 4.3, 4.3], size=9)
p("**Prueba ciega por tiburón** (10 mensajes sin nombre por perfil; un juez LLM elige el autor entre los 6 perfiles):", keep=True)
tabla(["Tiburón", "25 nov (flujo anterior)", "26 nov, corrida 1", "26 nov, corrida 2"], [(TIBURONES[t]["nombre"], ciega_ant["por_tiburon"][t], ciega_prev["por_tiburon"][t], ciega["por_tiburon"][t]) for t in TIBURONES] + [("**Total**", f"{ciega_ant['porcentaje']} %", f"{ciega_prev['porcentaje']} %", f"{ciega['porcentaje']} %")], anchos=[5.0, 4.0, 4.0, 4.0], size=9)
h3("Múltiples agentes funcionando en vivo")
p("Se probó el sistema completo de punta a punta: el ejecutable de Unity contra el servicio real con Azure OpenAI, con un usuario automatizado: en cada pregunta pide la sugerencia del agente empresario y la envía desde el mismo campo que usa una persona (cada sugerencia suma 1 llamada). "
  "En las tres sesiones los tiburones preguntaron uno por uno, replicaron entre ellos y votaron en secreto; el pitch, las preguntas, las réplicas y los votos se escucharon con su voz (las respuestas del usuario no se narran). La del nivel Difícil pasó por negociación y terminó sin trato. "
  "Video de una sesión en vivo grabada desde el ejecutable (enlace en el Anexo A). Estas sesiones y las capturas se grabaron antes de tres ajustes hechos al revisarlas: la regla de contraofertas (en la sesión de EcoTrack se conservó la oferta original con tres contraofertas, lo que ya no es posible), el texto completo de la pregunta mientras se responde y el retiro de marcas internas como «(R0)» del texto.", align="j", keep=True)
tabla(["Sesión", "Negocio", "Nivel", "Eventos", "Llamadas", "Resultado", "Envíos bajo 10 s (máximo)"], [
    (s_["sesion"], s_["negocio"], {"facil": "Fácil", "medio": "Medio", "dificil": "Difícil"}[s_["nivel"]], s_["eventos"], s_["llamadas"], s_["resultado"].replace("_", " ").title(),
     f"{sum(1 for x in s_['latencias'] if x['segundos'] < 10)} de {len(s_['latencias'])} ({max(x['segundos'] for x in s_['latencias']):.1f} s)") for s_ in vivo["sesiones"]],
    anchos=[2.0, 2.8, 1.6, 1.5, 1.6, 2.4, 5.1], size=9)
figura("vivo", 13.5, "Sesión en vivo: la pregunta del tiburón y la respuesta antes de enviarse, con «Recomendación del agente empresario» y «Dictar con voz».")
p("**Los niveles se sienten distintos.** Mismo negocio (EcoTrack) y mismo panel en los tres niveles, 2 sesiones por nivel con el modelo real (servicio/resultados_niveles.json):", align="j", keep=True)
fil_niv = []
for nv, nom in (("facil", "Fácil"), ("medio", "Medio"), ("dificil", "Difícil")):
    xs = [x for x in niveles if x["nivel"] == nv]
    votos_n = [v for x in xs for _, v in x["votos"][:3]]
    fil_niv.append((nom, f"{sum(x['palabras_por_pregunta'] for x in xs) / len(xs):.0f}", f"{sum(x['preguntas_con_tecnicismos'] for x in xs)} de {9 * len(xs)}",
                    ", ".join(f"{votos_n.count(v)} {v}" for v in ("invertir", "contraoferta", "salir")), ", ".join(x["resultado"].replace("_", " ").title() for x in xs)))
tabla(["Nivel", "Palabras por pregunta", "Preguntas con tecnicismos", "Votos de la primera votación", "Resultados"], fil_niv, anchos=[2.0, 2.4, 2.8, 5.6, 4.2], size=9)
p("Hallazgos del proceso: (a) el agente empresario inventaba cifras; se corrigió el prompt para que responda «aún no lo sé» y diga qué haría para averiguarlo; "
  "(b) una réplica se dirigía a un tiburón distinto del indicado; ahora el moderador asigna el destino; (c) el mapeo de respuestas por id fallaba en una ronda; ahora tiene un respaldo por posición.", align="j")

h2("7.6 Comunicación entre agentes y toma de decisiones")
p("**¿Toman decisiones?** Sí, en dos niveles. Cada tiburón decide de forma individual qué preguntar, si está de acuerdo o no con otro tiburón (RÉPLICA) y su voto (invertir, contraoferta o salir, con justificación y contraoferta numérica). "
  "La decisión colectiva la toma el nodo de votación con una regla explícita: mayoría de 2 de 3 a favor (y 2 «invertir» para conservar la oferta original); con un solo voto a favor se abre una negociación y una segunda votación. El emprendedor decide qué responder y qué condiciones acepta. "
  "El moderador no decide el resultado: solo fija el orden de turnos y a quién se dirige cada réplica.", align="j")
p(f"**¿Qué usan para hablar entre ellos?** No hay sockets entre agentes: se comunican por **memoria compartida tipo pizarra**, el estado del grafo de LangGraph. Cada mensaje es un evento tipado {{n, ronda, tipo, emisor, destino, texto}} que se agrega al historial público; "
  "los votos se guardan en una parte privada del estado y solo el nodo de votación los publica. Cada agente produce su mensaje con **salida estructurada** (esquemas Pydantic) validada antes de entrar al estado, así que el contenido siempre tiene la forma esperada. "
  f"Hacia fuera hay tres canales: HTTPS con Azure OpenAI para razonar [11], una **API REST con JSON** (FastAPI) [13] que Unity consulta cada segundo, y la voz local expuesta por esa misma API (texto a voz con Windows SAPI [19] y voz a texto con Whisper [17] mediante faster-whisper [18]). Cuando el grafo necesita al usuario se pausa con interrupt y se reanuda con Command(resume). {F('arq').capitalize()} resume la arquitectura.", align="j")
figura("arq", 16.5, "Arquitectura de comunicación: pizarra compartida de LangGraph, salida estructurada, API REST y voz local.")
p("**Equivalencia con FIPA-ACL.** Aunque no se usa una plataforma FIPA, cada tipo de mensaje corresponde a un acto comunicativo estándar [16]:", keep=True)
tabla(["Mensaje NeuralHub", "Acto FIPA-ACL", "Emisor → receptor", "Efecto en el estado"], [
    ("PITCH", "inform", "Emprendedor → tiburones", "Se agrega al historial público y a la pantalla"),
    ("PANEL / turnos", "inform", "Moderador → todos", "Fija el orden de preguntas y quién replica"),
    ("PREGUNTA", "query-ref / request", "Tiburón → emprendedor", "El grafo espera la respuesta (interrupt)"),
    ("RESPUESTA", "inform", "Emprendedor → tiburón", "Se agrega al historial; el grafo se reanuda"),
    ("RÉPLICA", "agree / refuse (inform)", "Tiburón → tiburón", "Acuerdo o desacuerdo visible para todos"),
    ("VOTO", "propose / reject-proposal", "Tiburón → nodo de votación", "Se guarda en privado hasta la decisión"),
    ("CONTRAOFERTA", "propose", "Tiburón → emprendedor", "Viaja dentro del voto"),
    ("NEGOCIACIÓN", "inform", "Nodo de votación → emprendedor", "Abre la negociación (interrupt)"),
    ("OFERTA", "accept-proposal / propose", "Emprendedor → tiburones", "Cambia la oferta vigente o fija las condiciones"),
    ("DECISIÓN", "inform", "Nodo de votación → todos", "Cierra la votación y dispara el reporte"),
    ("REPORTE", "inform", "Servicio → emprendedor", "Cierra la sesión"),
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
color_par("Se ve un {n|estudio de televisión} {a|submarino} en estilo de {n|caricatura}: una {n|plataforma} con {n|aros} de luz y un {n|ventanal} {a|curvo} al océano, detrás del cual {v|nadan} {n|peces}, una {n|mantarraya}, un {n|delfín} y un {n|tiburón}, entre {n|rocas}, {n|algas} que se {v|mecen} y {n|burbujas}. "
          "Al centro, una {a|gran} {n|pantalla} {v|muestra} una {n|lámina} del {n|tema} de cada {n|pregunta} con su {n|ilustración}; a los lados, dos {n|pantallas} con la {n|oferta} y los {n|votos}. "
          "Al frente, el {n|emprendedor} {a|de traje} {v|presenta}, {v|responde} y {v|negocia} sobre su {n|círculo} {a|luminoso}; cuando la cámara lo enfoca aparece su {n|escenografía} con el {n|nombre} del {n|programa}, el {n|logotipo} del sector y el {n|nombre} de su {n|empresa}. "
          "Enfrente, {a|tres} {n|tiburones} {a|inversionistas} con {n|corbata} del {n|color} de su perfil, cada uno detrás de su {n|podio} con {n|letrero}, un {n|accesorio} (sombrero, gorra, audífonos, flor, birrete), un {n|objeto} de su especialidad y un {n|emblema} {a|flotante}: {v|escuchan}, {v|preguntan}, {v|replican}, {v|votan} con una {n|paleta} y {v|reaccionan}. "
          "Una {n|cámara} {v|cambia} de plano para {v|enfocar} a quien habla y un {n|reflector} lo {v|ilumina}. La interfaz tiene {n|barra} de {n|rondas}, {n|tarjeta} del {n|tema}, {n|subtítulos} y {n|controles}.")
p("**El programa «Aguas Profundas».** El episodio abre con una cortinilla y «En este episodio presentamos…» (empresa y panel); termina con celebración (confeti, fanfarria y posibles socios) o hundimiento (la pantalla se oscurece y suben burbujas), seguido del cierre. "
  "Cada personaje tiene voz propia (dos voces es-MX de Windows con timbre, ritmo y graves distintos); hay música original de fondo y ningún turno avanza mientras alguien habla.", align="j")
p("Siguiendo el método de diseño del curso, marcamos en **violeta** los sustantivos (clases), en **azul** los verbos (métodos) y en **verde** los adjetivos (atributos). Con esta lista se armó el diagrama de clases.", align="j")
p("**Controles del usuario.** El usuario controla su pitch, la elección del panel, sus respuestas y su decisión final; nunca a los tiburones.", align="j", keep=True)
tabla(["Pantalla", "Qué ve", "Controles"], [
    ("Inicio", "Nombre del programa, 3 emprendedores por nivel y conexión con el servicio", "Fácil, Medio o Difícil; Escribir mi propio pitch; Conoce a los tiburones; Historial; «Usar sesión de ejemplo (sin conexión)»"),
    ("Formulario del pitch", "Campos del pitch, nivel y valuación calculada al momento", "Campos, nivel, «Cargar ejemplo», «Siguiente» (activo solo con los obligatorios)"),
    ("Elección del panel", "Tarjetas con foto, nombre y especialidad; 3 recomendados con motivo", "Agregar o quitar (exactamente 3), «Conocer a los tiburones», «Siguiente»"),
    ("Conoce a los tiburones", "El tiburón en el estudio, su presentación, cuándo elegirlo y en qué se fija", "Elegir tiburón, «Escuchar su presentación», «Volver»"),
    ("Modo", "Responde el agente empresario o el usuario (con o sin ayuda)", "Elegir modo y ayuda; «Iniciar sesión»"),
    ("Sesión en el estudio", "Intro, barra de rondas con letrero de etapa, subtítulos por frases sincronizados con la voz, tarjeta del tema y marcador", "Saltar intro, Anterior, Pausa, Siguiente, 1x/2x, Voz, Silencio, Historial, Tu panel, Salir; en vivo: responder, «Dictar con voz», «Recomendación del agente empresario»"),
    ("Negociación", "Contraofertas y condiciones", "Aceptar contraoferta, ajustar monto y porcentaje, mantener mi oferta; elegir condiciones finales (oferta original solo con mayoría) o rechazar las contraofertas"),
    ("Final y reporte", "Celebración o hundimiento, cierre, voto y justificación de cada tiburón, reporte", "Continuar, Ver historial, Repetir con este pitch, Nueva sesión"),
    ("Conversación e historial", "Todos los turnos (futuros atenuados) y sesiones guardadas", "«Ir» a cualquier turno; «Repetir sesión»"),
], anchos=[3.0, 6.4, 7.6], size=9)
p(f"**Expectativa y realidad.** {F('boceto').capitalize()} es el boceto visto desde arriba; {F('general')} y {F('arriba')} son el resultado en Unity. "
  "Las capturas de las pantallas (Figuras " + f"{NUM['s_inicio']} a {NUM['s_resultado']}) son del ejecutable compilado, con la sesión de ejemplo.", align="j")
figura("boceto", 15.5, "Boceto de la escena vista desde arriba (3 podios).")
figura("general", 14.5, "Estudio submarino en Unity: plano general con el panel de 3 tiburones.")
figura("arriba", 11.5, "Estudio en Unity visto desde arriba, para compararlo con el boceto.")
figura("s_inicio", 11.5, "Inicio: el programa y los tres niveles de emprendedor.")
figura("s_form", 11.5, "Formulario del pitch con el nivel de exigencia y los campos guiados.")
figura("s_panel", 11.5, "Elección del panel con foto de cada tiburón; 3 recomendados con su motivo.")
figura("s_conocer", 11.5, "Conoce a los tiburones: presentación y cuándo elegirlo.")
figura("s_intro", 11.5, "Intro del episodio: la empresa y el panel.")
figura("s_pitch", 11.5, "El emprendedor dice su pitch frente a la escenografía del programa.")
figura("s_pregunta", 11.5, "Una pregunta en curso: subtítulo, cámara sobre el tiburón y reflector.")
figura("s_replica", 11.5, "Una RÉPLICA de un tiburón a otro.")
figura("s_historial", 11.5, "Historial abierto durante la sesión.")
figura("s_votos", 11.5, "Marcador con los votos revelados.")
figura("s_decision", 11.5, "Final con trato: celebración y posibles socios.")
figura("s_sin", 11.5, "Final sin trato: el estudio se hunde.")
figura("s_cierre", 11.5, "Cierre del episodio.")
figura("s_resultado", 11.5, "Pantalla de resultado con votos y retroalimentación.")

h2("8.2 Componentes gráficos")
p("La escena se construye en Unity 6 [12] con un script de editor reproducible (menú NeuralHub → Construir estudio) y posprocesado (bloom, corrección de color, oclusión ambiental y viñeta) [15]. "
  "Los modelos de personajes y animales son de Quaternius, con licencia CC0 [14]; el escenario, los accesorios, las ilustraciones, el fondo, la música y la escenografía los hizo el equipo por código. La interfaz usa las fuentes Inter y Space Grotesk [20].", align="j")
comp = [
    ("Estudio submarino", "Plataforma con aros de luz, ventanal curvo con marcos, fondo oceánico pintado, rocas, algas que se mecen y burbujas.", "estudio_general", "Juan Esteban Jaramillo; mallas generadas por código (Editor/Mallas.cs) y fondo propio."),
    ("Tiburones inversionistas", "Dos modelos animados (Sharky y Mako) con corbata del color del perfil; saludan, asienten, niegan, saltan o se encogen según su estado.", "tiburon_2", "Quaternius, Pirate Kit (CC0); se quitaron las armas y se tiñó el pantalón."),
    ("Identidad de cada tiburón", "Accesorio (sombrero de copa, gorra de capitana, audífonos, flor y perlas, birrete, fedora y cadena), objeto en el podio (monedas, pieza de ajedrez, laptop, bolsa, libros, maletín) y emblema flotante.", "tiburon_1", "Juan Esteban Jaramillo; creación propia por código."),
    ("Podios y paletas de voto", "Podio con aro del color del perfil y letrero; paleta con DENTRO (invertir), OFERTA (contraoferta) o FUERA (salir).", "paleta", "Equipo; creación propia."),
    ("Emprendedor y su escenografía", "Personaje animado (saluda, explica, celebra o se lamenta) y escenografía con el nombre del programa, el logotipo del sector y la empresa.", "emprendedor", "Quaternius, Ultimate Modular Men (CC0); escenografía propia."),
    ("Fauna del ventanal", "Peces, mantarraya, delfín y tiburón animados que nadan detrás del cristal (en la imagen, acercamiento a dos de los peces).", "fauna", "Quaternius, Animated Fish Pack y Pirate Kit (CC0)."),
    ("Pantallas", "Principal: lámina del tema de cada pregunta (15 ilustraciones propias por sector y tema). Laterales: oferta y votos con barras.", "estudio_general", "Equipo; ilustraciones propias (docs/figuras/generar_ilustraciones.py)."),
    ("Sonido", "Música de fondo, cortinilla, fanfarria, hundimiento y cierre originales; efectos de turno y voto; voces es-MX por personaje.", "estudio_general", "Equipo; música sintetizada (docs/figuras/generar_musica.py) y Windows SAPI [19]."),
]
for n, d, im, fu in comp:
    ficha_tabla([("Nombre", n), ("Descripción", d), ("Imagen", ""), ("Fuente", fu)], imagen=im, ancho_img=6.5)
p("**Assets externos usados.** Todos están integrados en la escena de este incremento:", align="j", keep=True)
tabla(["Asset", "Uso", "Autor", "Fuente", "Licencia"], [
    ("Pirate Kit (Sharky, Mako, Shark)", "Tiburones inversionistas y tiburón que nada", "Quaternius", "https://quaternius.com/packs/piratekit.html", "CC0"),
    ("Animated Fish Pack", "Peces, mantarraya y delfín", "Quaternius", "https://quaternius.com/packs/animatedfish.html", "CC0"),
    ("Ultimate Modular Men", "Emprendedor animado", "Quaternius", "https://quaternius.com/packs/ultimatemodularcharacters.html", "CC0"),
    ("Post Processing Stack v2", "Bloom, color, oclusión y viñeta", "Unity Technologies", "Package Manager de Unity", "Paquete oficial de Unity"),
    ("Inter y Space Grotesk", "Tipografías de la interfaz y las pantallas", "Sus autores", "https://fonts.google.com", "SIL OFL 1.1"),
], anchos=[4.0, 4.2, 2.4, 4.8, 1.6], size=8.5)

h2("8.3 Prefabs")
pref = [
    ("Tiburon", "Personaje del panel con los dos modelos, el juego de accesorios de cada perfil, aro, letrero y paleta de voto. Se instancia 3 veces al iniciar la sesión.", "tiburon_1", "Tiburon.cs"),
    ("Sillon (estación)", "Tarima y podio redondeado. Hay 3 en semicírculo mirando al emprendedor.", "tiburon_2", "(solo geometría)"),
    ("Emprendedor", "Personaje animado del usuario.", "emprendedor", "Emprendedor.cs"),
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
    ("Emprendedor.cs", "Estados del emprendedor (Esperando, Presentando, Escuchando, Respondiendo, Negociando, Recibiendo resultado) y su animación.", "Recibe órdenes de Estudio."),
    ("Reflector.cs / Pantalla.cs", "Spotlight que sigue al turno; pantalla grande que muestra la lámina del tema sin recortar el texto.", "Los llama Estudio."),
    ("AppNeuralHub.cs", "Controlador de la aplicación: navega entre pantallas y conecta servicio, reproductor y estudio; incluye el modo de capturas automáticas.", "Usa ClienteServicio, ReproductorSesion y las pantallas."),
    ("Pantallas.cs", "Clases MenuInicio, FormularioPitch, EleccionPanel, SeleccionModo, BurbujaRespuesta, ControlReproduccion, PanelHistorial, InterfazDebate, PanelRespuestas, PanelNegociacion, PantallaResultado y ListaSesiones.", "Las dibuja AppNeuralHub; llaman a ClienteServicio."),
    ("Datos.cs / Estilos.cs", "Clases de datos (Evento, PitchDto, Pendiente…) y estilos con los colores del equipo; paneles y botones redondeados y fuentes Inter y Space Grotesk.", "Compartidos por todos los scripts."),
    ("Sonidos.cs", "Ambiente, música de fondo que baja cuando alguien habla, efectos, piezas del programa y voces con pausa; registra el audio para montar los videos.", "Lo usan ReproductorSesion, la intro y los finales."),
    ("Programa.cs / FinalPrograma.cs", "Intro del episodio, «Conoce a los tiburones», celebración, hundimiento y cierre.", "Los dibuja AppNeuralHub; mueven CamaraEstudio y usan Sonidos."),
    ("BarraRondas.cs / TemasPitch.cs", "Barra de etapas con letrero al iniciar cada ronda; detecta el tema de cada pregunta y arma la lámina de la pantalla.", "Los usan la interfaz y Estudio."),
    ("Niveles.cs / Personas.cs", "Niveles de dificultad y la identidad de cada tiburón (nombre, presentación, cuándo elegirlo).", "Los usan el menú, el panel y la intro."),
    ("SetEmprendedor.cs / Reloj.cs / TextoNitido.cs", "Escenografía del emprendedor; reloj para grabar video; texto 3D que respeta la profundidad.", "Auxiliares de la escena."),
    ("GrabadorVoz.cs", "Graba la voz del usuario con el micrófono (16 kHz) y la convierte a WAV.", "Lo usa PanelRespuestas; el WAV se envía a ClienteServicio.Transcribir."),
    ("PantallasDatos.cs", "Pantallas laterales: datos de la oferta con ícono del sector y barras de voto y calificación por tiburón.", "La actualiza ReproductorSesion con PITCH, PANEL, VOTO y REPORTE."),
    ("Caustica.cs / Ondular.cs / Nadador.cs", "Movimiento de la luz de cáusticas, de las algas y de los peces.", "Los agrega el constructor de la escena."),
    ("Editor: ConstruirEstudio, ConstruirPersonajes, ConstruirCaracter, Mallas", "Construyen el escenario, los personajes, los accesorios, los retratos y las mallas; compilan el ejecutable.", "Se ejecutan en el editor de Unity."),
]
for n, d, i in scripts:
    ficha_tabla([("Nombre", n), ("Descripción", d), ("Interacciones", i)])

# ---------- 9 gestión ----------
h1("9. Gestión de proyecto")
p("Herramienta: **Google Sheets**, hoja «NeuralHub – Gestión» con las pestañas Product backlog, Sprint backlog, Calendario, Bitácoras de cada entrega y Participación.", align="j")
p("Enlace de la hoja: " + SHEET, align="j")
p("Repositorio de código: " + REPO, align="j")
h2("Calendario de sprints")
p("Cada sprint cierra en viernes, alineado con las entregas: Incremento 2 el 27 de noviembre y entrega final el 4 de diciembre de 2026.", align="j", keep=True)
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
p(f"**Incremento 2.** En las {NS} sesiones de prueba contra Azure OpenAI todas terminaron con decisión, con un máximo de {llam_max} llamadas y {seg_prom} s en promedio. "
  f"El {pct(p_crit, p_tot)} de las preguntas aborda el criterio de quien pregunta y el {pct(p_dato, p_tot)} cita una cifra, comillas o dice que falta (indicador estrecho, meta 85 %); las justificaciones con criterio y cifra pasaron de {pct(cd1_ok, cd1_tot)} a {pct(cd_ok, cd_tot)}; hubo réplica en {r_tot} de {r_tot} rondas y el dueño del criterio débil votó «salir» o «contraoferta» en {dueno_sale} de {len(debiles)} sesiones. "
  f"En vivo, Unity y el servicio completaron 3 sesiones ({ev_vivo} eventos) con {lat_ok} de {len(lat)} envíos bajo 10 s (máximo {max(lat):.1f} s). Al comparar los niveles con el mismo negocio, Fácil hizo preguntas cortas sobre la historia y Difícil preguntas largas que exigen cifras.", align="j")
p(f"**Lo que falta.** El objetivo 2 requiere la prueba con una persona; el juez automático dio {ciega_prev['porcentaje']} % y {ciega['porcentaje']} % en dos corridas (meta 80 %). "
  f"En el objetivo 3, la cita de un dato quedó en {pct(p_dato, p_tot)} con el indicador automático. Al revisar este incremento vimos que una cifra más alta de ese indicador se debía a que contaba como dato el número de marcas internas como «(R0)»; esas marcas ya se retiran del texto y la cifra reportada es sin ellas. "
  "En la evaluación fija ningún tiburón votó «invertir» en la primera votación, así que los tratos salen de contraofertas y los pitches débiles P3 y P4 también cierran trato en peores condiciones; en el nivel Fácil sí hubo votos «invertir». "
  "Las pruebas en vivo usaron un usuario automatizado que contesta con las sugerencias del agente empresario; en una respuesta calculó mal el costo de adquisición (dividió el gasto mensual entre los usuarios de prueba) y los tiburones lo retomaron. Se corrigió su instrucción, pero no se han repetido las sesiones en vivo. "
  "Faltan la prueba con una persona que dicte sus respuestas y la de 5 usuarios externos (objetivo 10), planeadas para el Sprint 3.", align="j")

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
    "Quaternius, «Pirate Kit», «Animated Fish Pack» y «Ultimate Modular Men», modelos 3D con licencia CC0. [En línea]. Disponible en: https://quaternius.com",
    "Unity Technologies, «Post Processing Stack v2», documentación del paquete com.unity.postprocessing. [En línea]. Disponible en: https://docs.unity3d.com/Packages/com.unity.postprocessing@3.4/manual/index.html",
    "Foundation for Intelligent Physical Agents, «FIPA ACL Message Structure Specification», SC00061G, 2002. [En línea]. Disponible en: http://www.fipa.org/specs/fipa00061/",
    "A. Radford, J. W. Kim, T. Xu, G. Brockman, C. McLeavey e I. Sutskever, «Robust speech recognition via large-scale weak supervision», arXiv:2212.04356, 2022.",
    "SYSTRAN, «faster-whisper», repositorio de GitHub. [En línea]. Disponible en: https://github.com/SYSTRAN/faster-whisper",
    "Microsoft, «Speech API (SAPI 5.4) overview». [En línea]. Disponible en: https://learn.microsoft.com/previous-versions/windows/desktop/ee125663(v=vs.85)",
    "R. Andersson, «Inter», y F. Karsten, «Space Grotesk», tipografías con SIL Open Font License 1.1. [En línea]. Disponible en: https://fonts.google.com",
]
for i, r in enumerate(refs, 1):
    par = doc.add_paragraph(); par.paragraph_format.left_indent = Cm(0.9); par.paragraph_format.first_line_indent = Cm(-0.9)
    rich(par, f"[{i}]  {r}", size=10)

# ---------- 12 anexos ----------
h1("12. Anexos")
h2("Anexo A. Evidencias y código")
bullets([
    "Repositorio: " + REPO + " (carpetas servicio, prototipo_un_agente, unity y docs).",
    "Video del primer incremento (2 min): " + VIDEO,
    "Transcripciones completas y métricas: servicio/sesiones/ y servicio/resultados_prueba.json (15 sesiones); prototipo_un_agente/sesiones/ (6 corridas). Corridas anteriores citadas en la comparación: servicio/sesiones/anterior_inc2 (flujo anterior) y servicio/sesiones/anterior_regla (antes del ajuste de contraofertas).",
    "Pruebas automáticas: servicio/tests (42 pruebas con el modelo simulado; una recorre las 27 combinaciones de votos y la de voz usa SAPI y Whisper locales). Dependencias en servicio/requirements.txt.",
    "Video de la sesión en vivo (Unity + servicio + voz): " + VIDEO2,
    "Evaluación del incremento 2: servicio/resultados_prueba.json (15 sesiones), resultados_prueba_ciega.json, pruebas/prueba_ciega_humana.csv (planilla para la prueba con una persona), resultados_niveles.json (niveles) y resultados_vivo.json (3 sesiones en vivo).",
    "Assets y licencias: unity/paquetes_terceros.md.",
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
    "«Hazlo como un programa real: pitch hablado, preguntas una por una, niveles que se sientan distintos, intro, finales y tiburones con personalidad».",
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
def fijar_anchos(tabla_):
    """Google Docs ignora el ancho de las celdas y reparte columnas iguales si la rejilla de la tabla no los declara."""
    celdas = tabla_.rows[0].cells
    anchos_ = [c.width for c in celdas]
    columnas = tabla_._tbl.tblGrid.findall(qn("w:gridCol"))
    if any(w is None for w in anchos_) or len(columnas) != len(anchos_):
        return
    for col, w in zip(columnas, anchos_):
        col.set(qn("w:w"), str(int(w / 635)))          # EMU -> vigésimos de punto
    tblPr = tabla_._tbl.tblPr
    for nombre, atributos in (("w:tblW", {"w:w": str(int(sum(anchos_) / 635)), "w:type": "dxa"}), ("w:tblLayout", {"w:type": "fixed"})):
        el = tblPr.find(qn(nombre))
        if el is None:
            el = OxmlElement(nombre); tblPr.append(el)
        for k, v in atributos.items():
            el.set(qn(k), v)


for t_ in doc.tables:
    fijar_anchos(t_)

doc.save(out)
print("guardado", out, out.stat().st_size // 1024, "KB;", len(usadas), "figuras")
