"""Parche: tema de pecera, bitácora del anteproyecto repartida y tabla de assets (con autores y fuentes)."""
import re
from pathlib import Path

B = Path(__file__).parent

# ---------- gestion.py ----------
g = (B / "gestion.py").read_text(encoding="utf8")
i = g.index("# Bitácora del anteproyecto"); j = g.index("BITACORA_E2")
nuevo = '''# Bitácora del anteproyecto (entrega 1). Reparto acordado por el equipo (líder del proyecto) de forma equitativa y acorde a los roles.
# Verificado en el historial de Drive: creación (6 nov, 13:39, cuenta de Juan Esteban) y última edición (6 nov, 19:20, cuenta de Alberto).
BITACORA_E1 = [
    ("Investigar antecedentes (debate entre modelos, TradingAgents, LangGraph)", "Alberto López", "3 nov 2026", "Asignación del equipo."),
    ("Definir contexto y problema", "Gerardo Islas", "3 nov 2026", "Asignación del equipo."),
    ("Redactar objetivos y restricciones", "Gilberto Olavarrieta", "4 nov 2026", "Asignación del equipo."),
    ("Diseñar perfiles de los tiburones y modelo BDI", "Gerardo Islas", "4 nov 2026", "Asignación del equipo."),
    ("Clasificar el entorno (Russell y Norvig)", "Franco Salazar", "4 nov 2026", "Asignación del equipo."),
    ("Definir mensajes y regla de decisión", "Alberto López", "5 nov 2026", "Asignación del equipo."),
    ("Diagrama de actividades (Figura 1)", "Gilberto Olavarrieta", "5 nov 2026", "Asignación del equipo."),
    ("Diagrama de estados (Figura 2)", "Franco Salazar", "5 nov 2026", "Asignación del equipo."),
    ("Diagrama de comunicaciones (Figura 3)", "Gerardo Islas", "5 nov 2026", "Asignación del equipo."),
    ("Boceto de la escena (Figura 4)", "Juan Esteban Jaramillo", "5 nov 2026", "Asignación del equipo."),
    ("Diagrama de clases (Figura 5)", "Gilberto Olavarrieta", "5 nov 2026", "Asignación del equipo."),
    ("Portada, identidad visual y formato", "Juan Esteban Jaramillo", "5 nov 2026", "Asignación del equipo."),
    ("Crear el documento en Google Drive", "Juan Esteban Jaramillo", "6 nov 2026, 13:39", "Verificado en el historial de Drive."),
    ("Revisión final del formato", "Franco Salazar", "6 nov 2026", "Asignación del equipo. La última edición en Drive (19:20) la hizo la cuenta de Alberto."),
    ("Entrega del anteproyecto", "Alberto López", "6 nov 2026", "Fecha de la portada."),
]
'''
g = g[:i] + nuevo + g[j:]
g = g.replace('"Modelos 3D definitivos de tiburones y del estudio"', '"Modelos 3D definitivos del ambiente de pecera (tiburones empresarios, pez payaso, coral y algas); evaluar el Fish Pack de Quaternius"')
(B / "gestion.py").write_text(g, encoding="utf8")

# ---------- construir_docx.py ----------
s = (B / "construir_docx.py").read_text(encoding="utf8")


def rep(a, b, count=1):
    global s
    assert a in s, a[:60]
    s = s.replace(a, b, count)


rep("Todo se representa como un estudio de televisión en Unity.", "Todo se representa en Unity como un estudio de televisión submarino: una pecera con tiburones empresarios, el «Tanque de tiburones».")
rep("la escena 3D del estudio", "la escena 3D de la pecera") if "la escena 3D del estudio" in s else None
rep('"**Una sola escena:** un estudio de televisión con una cámara principal que cambia de plano y 3 sillones fijos;', '"**Una sola escena:** un estudio de televisión submarino (pecera) con una cámara principal que cambia de plano y 3 sillones concha fijos;')
rep('("Estudio en Unity: plano general con el panel de 3 tiburones.")' if False else '"Estudio en Unity: plano general con el panel de 3 tiburones."', '"Estudio-pecera en Unity: plano general con el panel de 3 tiburones empresarios."')

# 8.1: descripción coloreada
i = s.index('color_par("Se ve un {n|estudio de televisión}'); j = s.index('p("Siguiendo el método de diseño del curso')
s = s[:i] + '''color_par("Se ve un {n|estudio de televisión} {a|submarino}, una {n|pecera} {a|profunda} con {n|arena}, {n|rocas}, {n|corales}, {n|algas} que se mecen, {n|peces}, {n|medusas} y {n|burbujas}. "
          "Al fondo, una {a|gran} {n|pantalla} {v|muestra} el nombre del {n|negocio}, el {n|monto} {a|solicitado} y el {n|porcentaje} {a|ofrecido}. "
          "Al frente, el {n|emprendedor}, un {n|pez payaso} {a|naranja} que es el avatar del {n|usuario}, {a|de pie} junto al {n|podio} de coral sobre un {n|círculo} {a|luminoso}, {v|presenta} su idea, {v|responde} las preguntas y {v|negocia}. "
          "Detrás, {a|tres} {n|tiburones} {a|empresarios} con {n|traje} y {n|corbata}, {a|sentados} en {n|sillones} {n|concha} {a|fijos} en semicírculo, cada uno con su {n|escritorio} y un {n|letrero} con su nombre, {v|escuchan}, {v|preguntan}, {v|replican} a otro tiburón, {v|votan} y {v|reaccionan}: el personaje de cada sillón {v|cambia} según el panel elegido. "
          "Una {n|cámara} {a|principal} {v|cambia} de plano para {v|enfocar} a quien habla y un {n|reflector} {v|ilumina} al agente en turno. En la parte inferior se ve un {n|cuadro de diálogo} con el texto de cada turno, un {n|indicador de ronda} y un {n|marcador} con los votos; "
          "al final aparece un {n|letrero de resultado}: trato o sin trato.")
''' + s[j:]

# 8.2: texto y componentes
i = s.index('p("El estudio se construyó con primitivas de Unity'); j = s.index('h2("8.3 Prefabs")')
s = s[:i] + '''p("El ambiente se construyó con primitivas de Unity (cubos, esferas, cápsulas y cilindros) mediante un script de editor reproducible (menú NeuralHub → Construir estudio), sin assets externos en este incremento. "
  "Los modelos y animaciones definitivos están planeados en la tarea T25; la tabla de assets tentativos al final de esta sección indica autor, fuente y licencia de cada candidato.", align="j")
comp = [
    ("Estudio-pecera (piso, muros y niebla)", "Piso de arena, muros de agua profunda, niebla azul-verde y franjas de luz; define el ambiente submarino.", "estudio_general", "Alberto López; primitivas de Unity (Assets/Editor/ConstruirEstudio.cs)."),
    ("Pantalla grande", "Marco, fondo y texto 3D con el negocio, el monto, el porcentaje y la valuación; muestra TRATO o SIN TRATO al final.", "estudio_general", "Alberto López; fuente de texto integrada de Unity (LegacyRuntime)."),
    ("Sillón concha y escritorio", "Sillón con respaldo en forma de concha marina y escritorio frontal; 3 fijos en semicírculo.", "tiburon_2", "Alberto López; primitivas de Unity."),
    ("Tiburón empresario", "Cuerpo con traje, camisa y corbata del color del perfil, cabeza con hocico, ojos y dientes, aleta dorsal y cola.", "tiburon_1", "Alberto López; primitivas de Unity (a reemplazar o complementar con los modelos de Juan Esteban)."),
    ("Pez payaso emprendedor y podio de coral", "Avatar del usuario: pez payaso naranja con franjas blancas junto a un podio de coral.", "emprendedor", "Alberto López; primitivas de Unity."),
    ("Algas, coral, rocas, peces y medusas", "Algas y medusas que se mecen (script Ondular), peces que nadan en círculos (script Nadador) y burbujas (sistema de partículas).", "tiburon_3", "Alberto López; primitivas y sistema de partículas de Unity."),
]
for n, d, im, fu in comp:
    ficha_tabla([("Nombre", n), ("Descripción", d), ("Imagen", ""), ("Fuente", fu)], imagen=im, ancho_img=6.5)
p("**Assets tentativos para las siguientes entregas.** Ninguno se ha descargado ni usado todavía; la licencia se confirmará en la página de descarga antes de incluirlo en el proyecto y se citará en las referencias.", align="j", keep=True)
tabla(["Asset", "Uso previsto", "Autor", "Fuente", "Licencia", "Entrega"], [
    ("Fish Pack Animated (modelos 3D animados: peces, tiburón, ballena, mantarraya, delfín)", "Peces y fauna animada de fondo; referencia para el tiburón", "Quaternius", "https://quaternius.itch.io/lowpoly-animated-fish", "CC0 (indicada en la página de descarga)", "2 (27 nov)"),
    ("Paquetes de modelos 3D de Kenney (mobiliario y props)", "Mobiliario y decorado del estudio", "Kenney", "https://kenney.nl (apartado Assets)", "CC0 según la declaración general de Kenney; por confirmar en cada paquete", "2 (27 nov)"),
    ("Animaciones de personaje (sentarse, gesticular, celebrar)", "Animar a los tiburones y al pez payaso por estado", "Adobe Mixamo", "https://www.mixamo.com", "Términos de Adobe; por confirmar antes de usar", "3 (4 dic)"),
    ("Modelos propios de tiburones empresarios y pez payaso", "Sustituir las primitivas por modelos con rig", "Juan Esteban Jaramillo (equipo)", "Blender; se subirán al repositorio", "Propia del equipo", "2–3"),
    ("Efectos de agua: partículas, rayos de luz y cáusticas", "Mejorar el ambiente submarino", "Por definir (Unity Asset Store o creación propia)", "Por definir", "Por confirmar", "3 (4 dic)"),
], anchos=[4.2, 3.4, 2.4, 3.6, 2.4, 1.2], size=8)
''' + s[j:]

# referencias nuevas
rep('    "S. Ramírez, «FastAPI». [En línea]. Disponible en: https://fastapi.tiangolo.com/",',
    '    "S. Ramírez, «FastAPI». [En línea]. Disponible en: https://fastapi.tiangolo.com/",\n    "Quaternius, «Fish Pack Animated» (asset candidato, licencia CC0). [En línea]. Disponible en: https://quaternius.itch.io/lowpoly-animated-fish",\n    "Kenney, «Assets» y declaración de licencia CC0. [En línea]. Disponible en: https://kenney.nl/support",')
# portadas/anexos
rep("Las tablas del anteproyecto", "Las tablas del anteproyecto") if "Las tablas del anteproyecto" in s else None
(B / "construir_docx.py").write_text(s, encoding="utf8")
print("parche ok")
