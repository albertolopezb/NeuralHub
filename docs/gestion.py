"""Datos de gestión del proyecto (compartidos por el documento y la hoja de cálculo)."""

D = "Documentación y planeación"
B = "Programación multiagente (back-end)"
U = "Programación en Unity (front-end)"
MIEMBROS = ["Alberto López", "Gerardo Islas", "Gilberto Olavarrieta", "Juan Esteban Jaramillo", "Franco Salazar"]

# Calendario propuesto (viernes de cierre). Confirmar contra Canvas.
SPRINTS = [
    ("Sprint 1", "lun 16 nov 2026", "vie 20 nov 2026", "Primer incremento: correcciones, servicio de agentes, escena 3D preliminar"),
    ("Sprint 2", "lun 23 nov 2026", "vie 27 nov 2026", "Integración Unity–servicio, prompts, escena 3D con assets, niveles y programa"),
    ("Sprint 3", "lun 30 nov 2026", "vie 4 dic 2026", "Prueba ciega con persona, métricas en la app, prueba con 5 usuarios y entrega final"),
]

# id, historia, usuario, prioridad, puntos, sprint, estado
HISTORIAS = [
    ("HU-01", "Como emprendedor, quiero capturar mi pitch en un formulario con campos guiados, para que el panel lo evalúe con datos completos.", "Emprendedor", "Alta", 5, "Sprint 1", "Hecha"),
    ("HU-02", "Como emprendedor, quiero ver qué tiburones me recomienda la app y por qué, y poder cambiarlos, para elegir un panel útil para mi negocio.", "Emprendedor", "Alta", 5, "Sprint 1", "Hecha"),
    ("HU-03", "Como emprendedor, quiero ver en el estudio quién habla, con subtítulos, cámara y reflector, para seguir la conversación.", "Emprendedor", "Alta", 5, "Sprint 1", "Hecha"),
    ("HU-04", "Como emprendedor, quiero responder por escrito las preguntas de cada ronda, para defender mi idea con mis propios datos.", "Emprendedor", "Alta", 5, "Sprint 2", "Hecha"),
    ("HU-05", "Como espectador, quiero ver a los tiburones debatir entre ellos, para entender por qué coinciden o discrepan.", "Espectador", "Media", 3, "Sprint 1", "Hecha"),
    ("HU-06", "Como emprendedor, quiero recibir la decisión (trato o sin trato) y poder negociar si solo hay un voto a favor, para mejorar mi oferta.", "Emprendedor", "Alta", 8, "Sprint 2", "Hecha"),
    ("HU-07", "Como emprendedor, quiero un reporte de retroalimentación por tiburón con los criterios que no se evaluaron, para saber qué mejorar en mi pitch.", "Emprendedor", "Alta", 5, "Sprint 2", "Hecha"),
    ("HU-08", "Como emprendedor o espectador, quiero pausar, cambiar la velocidad, navegar a cualquier turno de la conversación y repetir una sesión (o usar una de ejemplo sin conexión), para revisarla a mi ritmo.", "Emprendedor / espectador", "Media", 5, "Sprint 2", "Hecha"),
    ("HU-10", "Como emprendedor, quiero dictar mis respuestas con mi voz, para responder con naturalidad como en un pitch real.", "Emprendedor", "Media", 5, "Sprint 2", "Hecha (falta validar con una persona)"),
    ("HU-11", "Como espectador, quiero escuchar a cada tiburón con su propia voz y ver en las pantallas del estudio los datos de la oferta y los votos, para seguir la sesión sin leer todo.", "Espectador", "Media", 5, "Sprint 2", "Hecha"),
    ("HU-12", "Como emprendedor, quiero elegir un nivel de dificultad, para practicar de menos a más exigente.", "Emprendedor", "Media", 3, "Sprint 2", "Hecha"),
    ("HU-13", "Como emprendedor, quiero conocer a cada tiburón y, si respondo yo, pedir una sugerencia al agente empresario, para elegir bien mi panel y aprender a contestar.", "Emprendedor", "Media", 5, "Sprint 2", "Hecha"),
    ("HU-14", "Como emprendedor, quiero llenar el formulario subiendo un documento o pegando mi pitch como texto, para no capturar todo a mano.", "Emprendedor", "Media", 5, "Sprint 2", "Hecha"),
    ("HU-09", "Como evaluador (profesor o socio formador), quiero ver las métricas de una sesión (llamadas al modelo, rondas, duración, votos), para juzgar su costo y su coherencia.", "Evaluador", "Media", 3, "Sprint 3", "Pendiente"),
]

CRITERIOS = {
    "HU-01": ["Los campos obligatorios están marcados con * y «Siguiente» solo se activa cuando están completos.", "La valuación implícita (monto ÷ porcentaje) se muestra al momento.", "Los campos opcionales pueden quedar vacíos («Aún no lo sé»).", "El inicio ofrece 3 emprendedores de ejemplo (Fácil, Medio y Difícil) y hay «Cargar ejemplo» en el formulario."],
    "HU-02": ["La app marca exactamente 3 tiburones recomendados, cada uno con su motivo.", "El usuario puede cambiar la selección, y «Siguiente» solo se activa con exactamente 3.", "La recomendación no usa el modelo de lenguaje (0 llamadas)."],
    "HU-03": ["El cuadro de diálogo muestra quién habla, el tipo de intervención y el texto; el destino de una réplica aparece bajo el título de la ronda.", "La cámara enfoca al emisor y el reflector lo ilumina.", "La barra de etapas muestra Pitch, Ronda 1 a 3, Veredicto y Negociación, con un letrero al empezar cada una."],
    "HU-04": ["Cada pregunta abre un campo de respuesta (máximo 600 caracteres) antes de que pregunte el siguiente tiburón.", "El botón de envío solo se activa con una respuesta escrita.", "El servicio reanuda la sesión al recibirla."],
    "HU-05": ["En cada ronda hay una RÉPLICA de un tiburón dirigida a otro tiburón del panel.", "La réplica indica acuerdo o desacuerdo y se ve en el historial."],
    "HU-06": ["Con 2 o 3 votos a favor se eligen condiciones: la oferta original solo si 2 o más votaron «invertir»; si no, una contraoferta o rechazarlas (sin trato).", "Con 1 voto a favor se abre una negociación y los 3 votan una sola vez más.", "Con 0 votos a favor la decisión es «Sin trato»."],
    "HU-07": ["El reporte muestra voto, calificación de 1 a 5, frase clave y qué necesitaría cada tiburón.", "Incluye 3 fortalezas, 3 alertas, preguntas sin responder y 3 siguientes pasos.", "Lista los criterios de los tiburones que no estuvieron en el panel y el aviso de simulación educativa."],
    "HU-08": ["Los botones Anterior, Pausa/Reanudar, Siguiente y 1x/2x actúan sobre la reproducción y se muestra «Turno k/N».", "El panel «Conversación» lista todos los turnos y el botón «Ir» reconstruye la escena desde ese turno.", "El interruptor «Usar sesión de ejemplo (sin conexión)» reproduce una sesión real guardada, y el historial lista también las sesiones guardadas en disco."],
    "HU-10": ["Cada pregunta tiene un botón «Dictar con voz» que graba con el micrófono y muestra los segundos.", "La grabación admite hasta 2 minutos y avisa si casi no se oyó o no se entendió. Al detener, el texto transcrito (Whisper local, español) se agrega al campo y el usuario puede corregirlo antes de enviar.", "Si no hay micrófono, se muestra «Sin micrófono para dictar» y se puede seguir escribiendo."],
    "HU-11": ["El pitch, las preguntas, las réplicas y los votos se escuchan con la voz del personaje (timbre, ritmo y graves propios) y el siguiente turno espera a que terminen; en vivo, la respuesta del usuario también se narra con la voz del emprendedor. El texto aparece cuando su audio está listo.", "La pantalla izquierda muestra monto, porcentaje, valuación, ventas, margen y el ícono del sector; la derecha, el voto y la calificación de cada tiburón.", "Los botones «Voz» y «Silencio» apagan la voz o todo el audio."],
    "HU-12": ["El inicio muestra las tarjetas Fácil, Medio y Difícil con su emprendedor.", "El nivel se puede cambiar en el formulario y se ve durante la sesión y en el resultado.", "En Fácil se les pide no exigir cifras ni usar tecnicismos (el modelo no siempre lo cumple); en Difícil exigen la cifra exacta."],
    "HU-13": ["«Conoce a los tiburones» muestra a cada uno en el estudio, con su presentación hablada y cuándo elegirlo.", "Las tarjetas del panel muestran la foto de cada tiburón.", "En vivo con ayuda, el agente propone una respuesta o mejora la que el usuario ya escribió; se puede editar (una ayuda por pregunta)."],
    "HU-14": ["Acepta .docx, .pdf, .txt, .md o texto pegado.", "Avisa que puede que no todo se transfiera bien y lista los datos obligatorios que faltan y los recomendados que no venían.", "Se le pide no inventar: lo que el documento no dice queda vacío o sin marcar para que el usuario lo revise y complete."],
    "HU-09": ["Se muestran llamadas al modelo, rondas, segundos y resultado de la sesión.", "Las métricas coinciden con las que calcula el servicio."],
}

# id, tarea, categoria, responsable, inicio, fin, estado, historia
TAREAS = [
    ("T01", "Revisar la retroalimentación del anteproyecto y proponer correcciones y mejoras de la app", D, "Gerardo Islas", "17 nov", "18 nov", "Hecha", "—"),
    ("T02", "Contexto y problema con datos y fuentes verificadas", D, "Gerardo Islas", "17 nov", "19 nov", "Hecha", "—"),
    ("T03", "Objetivos SMART y restricciones actualizadas", D, "Gilberto Olavarrieta", "17 nov", "18 nov", "Hecha", "—"),
    ("T04", "Historias de usuario, criterios de aceptación y backlog", D, "Franco Salazar", "18 nov", "19 nov", "Hecha", "Todas"),
    ("T05", "Grafo de LangGraph: panel de 3, réplica, voto secreto, negociación y reporte", B, "Alberto López", "17 nov", "19 nov", "Hecha", "HU-03, HU-05, HU-06"),
    ("T06", "Regla de votación y prueba de las 27 combinaciones de votos", B, "Franco Salazar", "18 nov", "19 nov", "Hecha", "HU-06"),
    ("T07", "Recomendación del panel (regla en Python)", B, "Gerardo Islas", "17 nov", "18 nov", "Hecha", "HU-02"),
    ("T08", "API HTTP con FastAPI (rutas de tiburones, sesiones, respuestas, negociación, reporte)", B, "Gilberto Olavarrieta", "17 nov", "19 nov", "Hecha", "HU-04, HU-06"),
    ("T09", "Set de prueba de 5 pitches con guion y evaluador de 15 sesiones", B, "Gerardo Islas", "18 nov", "19 nov", "Hecha", "HU-09"),
    ("T10", "Prototipo de un agente (3 ideas × 2 tiburones) contra Azure OpenAI", B, "Alberto López", "16 nov", "17 nov", "Hecha", "—"),
    ("T11", "Proyecto Unity 6, estudio 3D con 3 sillones y prefabs", U, "Juan Esteban Jaramillo", "17 nov", "19 nov", "Hecha", "HU-03"),
    ("T12", "Pantallas y controles: formulario, panel, respuestas, negociación, historial y resultado", U, "Franco Salazar", "17 nov", "19 nov", "Hecha", "HU-01, HU-02, HU-08"),
    ("T13", "ClienteServicio (HTTP, polling de 1 s) y sesión de ejemplo sin conexión", U, "Gilberto Olavarrieta", "18 nov", "19 nov", "Hecha", "HU-08"),
    ("T14", "Figuras 1 a 9 y modelo de los agentes", D, "Juan Esteban Jaramillo", "18 nov", "19 nov", "Hecha", "—"),
    ("T15", "Revisar la sección de interacción (7.4) con las pruebas reales de integración", D, "Gilberto Olavarrieta", "23 nov", "25 nov", "Hecha", "HU-04"),
    ("T16", "Documentar componentes gráficos y sus fuentes (8.2) con las imágenes definitivas", D, "Juan Esteban Jaramillo", "23 nov", "26 nov", "Hecha", "HU-03"),
    ("T17", "Documentar prefabs y scripts (8.3 y 8.4) tras las animaciones", D, "Franco Salazar", "24 nov", "27 nov", "Hecha", "HU-03"),
    ("T18", "Redactar los resultados parciales y mantener la bitácora", D, "Gerardo Islas", "25 nov", "27 nov", "Hecha", "HU-09"),
    ("T19", "Mantener el backlog y el calendario (hoja de gestión)", D, "Alberto López", "23 nov", "27 nov", "Hecha", "Todas"),
    ("T20", "Ajustar los prompts para que cada justificación cite un dato concreto (meta ≥ 85 %)", B, "Gerardo Islas", "23 nov", "26 nov", "Hecha", "HU-06"),
    ("T21", "Pruebas de integración API ↔ Unity con el servicio en vivo", B, "Gilberto Olavarrieta", "23 nov", "26 nov", "Hecha", "HU-04, HU-06"),
    ("T22", "Ampliar pruebas unitarias del panel recomendado y de la negociación", B, "Franco Salazar", "24 nov", "26 nov", "Hecha", "HU-02, HU-06"),
    ("T23", "Prueba ciega con persona: identificar al tiburón autor de 60 mensajes sin nombre", B, "Juan Esteban Jaramillo", "30 nov", "3 dic", "Pendiente", "HU-05"),
    ("T24", "Historial persistente: cada sesión terminada se guarda en JSON (checkpointer en memoria durante la sesión)", B, "Alberto López", "24 nov", "27 nov", "Hecha", "HU-08"),
    ("T25", "Modelos 3D del estudio: integrar paquetes CC0 de Quaternius (tiburones, emprendedor y peces)", U, "Juan Esteban Jaramillo", "23 nov", "26 nov", "Hecha", "HU-03"),
    ("T26", "Animaciones por estado y ajuste de cámara", U, "Franco Salazar", "23 nov", "27 nov", "Hecha", "HU-03, HU-05"),
    ("T27", "ClienteServicio contra el servicio real: errores y reconexión", U, "Gilberto Olavarrieta", "23 nov", "26 nov", "Hecha", "HU-04"),
    ("T28", "Pantalla de métricas de la sesión (HU-09)", U, "Gerardo Islas", "30 nov", "2 dic", "Pendiente", "HU-08, HU-09"),
    ("T29", "Flujo completo con usuario humano y dictado en Unity, documentado como evidencia", U, "Alberto López", "24 nov", "3 dic", "En curso", "HU-04, HU-06, HU-07"),
    ("T33", "Voz: texto a voz (Windows SAPI, es-MX) y voz a texto (Whisper local) en el servicio", B, "Gilberto Olavarrieta", "25 nov", "25 nov", "Hecha", "HU-10, HU-11"),
    ("T51", "Mejoras tras la prueba del 27 nov: autocompletar el pitch, mejorar respuestas, dictado largo, voz sincronizada, preguntas por ángulos, personalidades y apariencia", B, "Alberto López", "27 nov", "27 nov", "Hecha", "HU-10, HU-13, HU-14"),
    ("T34", "Dictado por micrófono y reproducción de voces en Unity", U, "Franco Salazar", "25 nov", "26 nov", "Hecha", "HU-10, HU-11"),
    ("T35", "Navegación por la conversación (anterior, siguiente, ir a un turno)", U, "Franco Salazar", "25 nov", "26 nov", "Hecha", "HU-08"),
    ("T36", "Iluminación submarina (cáusticas, rayos de luz) y pantallas laterales con datos e íconos", U, "Juan Esteban Jaramillo", "25 nov", "26 nov", "Hecha", "HU-11"),
    ("T37", "Efectos de sonido y ambiente submarino (síntesis propia)", U, "Juan Esteban Jaramillo", "25 nov", "26 nov", "Hecha", "HU-11"),
    ("T38", "Estilo distintivo por tiburón en los prompts y prueba ciega automatizada", B, "Gerardo Islas", "25 nov", "26 nov", "Hecha", "HU-05"),
    ("T39", "Sección de comunicación entre agentes y diagrama de arquitectura", D, "Gilberto Olavarrieta", "26 nov", "26 nov", "Hecha", "—"),
    ("T40", "Prueba en vivo Unity–servicio, medición de latencia y video", B, "Alberto López", "25 nov", "26 nov", "Hecha", "HU-04, HU-06"),
    ("T41", "Correcciones del primer incremento (HU-08 y clasificación del entorno)", D, "Gerardo Islas", "26 nov", "26 nov", "Hecha", "HU-08"),
    ("T42", "Pitch hablado como en televisión y preguntas una por una (grafo, API y pruebas)", B, "Alberto López", "26 nov", "26 nov", "Hecha", "HU-04"),
    ("T43", "Niveles de dificultad y comparación con el modelo real", B, "Gerardo Islas", "26 nov", "26 nov", "Hecha", "HU-12"),
    ("T44", "Modo en vivo con sugerencia del agente empresario (servicio y pruebas)", B, "Gerardo Islas", "26 nov", "26 nov", "Hecha", "HU-13"),
    ("T45", "Interfaz del modo en vivo: selección de modo y burbuja de respuesta", U, "Gerardo Islas", "26 nov", "26 nov", "Hecha", "HU-13"),
    ("T46", "Escenario caricatura, ventanal con fauna y personalidad de cada tiburón", U, "Juan Esteban Jaramillo", "26 nov", "26 nov", "Hecha", "HU-11"),
    ("T47", "Intro del episodio, barra de rondas, finales y cierre", U, "Franco Salazar", "26 nov", "26 nov", "Hecha", "HU-03, HU-08"),
    ("T48", "«Conoce a los tiburones», niveles en el menú y fotos de perfil", U, "Gilberto Olavarrieta", "26 nov", "26 nov", "Hecha", "HU-12, HU-13"),
    ("T49", "Voces por personaje, turnos que esperan a la voz y música original", B, "Franco Salazar", "26 nov", "26 nov", "Hecha", "HU-11"),
    ("T50", "Actualizar el documento, figuras y video del incremento 2", D, "Alberto López", "26 nov", "27 nov", "Hecha", "—"),
    ("T30", "Evaluación final de las 15 sesiones y resultados", B, "Alberto López", "30 nov", "2 dic", "Pendiente", "HU-09"),
    ("T31", "Prueba con 5 usuarios externos (encuesta de utilidad)", D, "Gerardo Islas", "30 nov", "3 dic", "Pendiente", "HU-07"),
    ("T32", "Resultados, discusión y revisión final del documento", D, "Gilberto Olavarrieta", "1 dic", "4 dic", "Pendiente", "—"),
]

# Bitácora del anteproyecto (entrega 1). Reparto acordado por el equipo (líder del proyecto) de forma equitativa y acorde a los roles.
# Verificado en el historial de Drive: creación (6 nov, 13:39, cuenta de Juan Esteban) y última edición (6 nov, 19:20, cuenta de Alberto).
BITACORA_E1 = [
    ("Investigar antecedentes (debate entre modelos, TradingAgents, LangGraph)", "Alberto López", "3 nov 2026", "Asignación del equipo."),
    ("Definir contexto y problema", "Gerardo Islas", "3 nov 2026", "Asignación del equipo."),
    ("Redactar objetivos y restricciones", "Gilberto Olavarrieta", "4 nov 2026", "Asignación del equipo."),
    ("Diseñar perfiles de los tiburones y modelo BDI", "Gerardo Islas", "4 nov 2026", "Asignación del equipo."),
    ("Clasificar el entorno (Russell y Norvig)", "Franco Salazar", "4 nov 2026", "Asignación del equipo."),
    ("Definir mensajes y regla de decisión", "Alberto López", "5 nov 2026", "Asignación del equipo."),
    ("Diagrama de actividades", "Gilberto Olavarrieta", "5 nov 2026", "Asignación del equipo."),
    ("Diagrama de estados", "Franco Salazar", "5 nov 2026", "Asignación del equipo."),
    ("Diagrama de comunicaciones", "Gerardo Islas", "5 nov 2026", "Asignación del equipo."),
    ("Boceto de la escena", "Juan Esteban Jaramillo", "5 nov 2026", "Asignación del equipo."),
    ("Diagrama de clases", "Gilberto Olavarrieta", "5 nov 2026", "Asignación del equipo."),
    ("Portada, identidad visual y formato", "Juan Esteban Jaramillo", "5 nov 2026", "Asignación del equipo."),
    ("Crear el documento en Google Drive", "Juan Esteban Jaramillo", "6 nov 2026, 13:39", "Verificado en el historial de Drive."),
    ("Revisión final del formato", "Franco Salazar", "6 nov 2026", "Asignación del equipo. La última edición en Drive (19:20) la hizo la cuenta de Alberto."),
    ("Entrega del anteproyecto", "Alberto López", "6 nov 2026", "Fecha de la portada."),
]
BITACORA_E3 = [
    ("Correcciones del primer incremento (HU-08 con tipo de usuario; entorno reclasificado como dinámico)", "Gerardo Islas", "26 nov 2026"),
    ("Estilo distintivo por tiburón, prueba ciega automatizada y nueva evaluación de 15 sesiones", "Gerardo Islas", "25–26 nov 2026"),
    ("Servicio de voz (texto a voz y voz a texto)", "Gilberto Olavarrieta", "25 nov 2026"),
    ("Sección de comunicación entre agentes y diagrama de arquitectura", "Gilberto Olavarrieta", "26 nov 2026"),
    ("Dictado por micrófono, voces y navegación por la conversación en Unity", "Franco Salazar", "25–26 nov 2026"),
    ("Iluminación submarina, pantallas con datos, íconos por sector y sonidos", "Juan Esteban Jaramillo", "25–26 nov 2026"),
    ("Prueba en vivo Unity–servicio con medición de latencia", "Alberto López", "25–26 nov 2026"),
    ("Pitch hablado y preguntas una por una", "Alberto López", "26 nov 2026"),
    ("Niveles de dificultad y modo en vivo con sugerencia del agente", "Gerardo Islas", "26 nov 2026"),
    ("Escenario caricatura con modelos CC0 y personalidad de cada tiburón", "Juan Esteban Jaramillo", "26 nov 2026"),
    ("Intro, barra de rondas, finales, voces y música", "Franco Salazar", "26 nov 2026"),
    ("«Conoce a los tiburones» y niveles en el menú", "Gilberto Olavarrieta", "26 nov 2026"),
    ("Mejoras tras la prueba del juego (autocompletar, dictado, voz, preguntas, personalidades y apariencia)", "Alberto López", "27 nov 2026"),
    ("Documento, video y gestión del proyecto", "Alberto López", "23–27 nov 2026"),
]

BITACORA_E2 = [
    ("Revisión de la retroalimentación y propuesta de correcciones (documento «Correcciones al anteproyecto»)", "Gerardo Islas", "17–18 nov 2026"),
    ("Contexto y problema con fuentes verificadas; recomendación del panel; set de prueba de 5 pitches", "Gerardo Islas", "17–19 nov 2026"),
    ("Objetivos SMART, restricciones, API HTTP y cliente de Unity (ClienteServicio)", "Gilberto Olavarrieta", "17–19 nov 2026"),
    ("Historias de usuario, regla de votación con 27 pruebas y pantallas de la interfaz en Unity", "Franco Salazar", "17–19 nov 2026"),
    ("Grafo de LangGraph (réplica, voto secreto, negociación, reporte) y prototipo de un agente", "Alberto López", "16–19 nov 2026"),
    ("Estudio 3D de pecera (prefabs y ambiente), figuras y modelo de los agentes", "Juan Esteban Jaramillo", "17–19 nov 2026"),
]

HISTORIAS.sort(key=lambda h: int(h[0][3:]))
