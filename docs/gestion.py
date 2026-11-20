"""Datos de gestión del proyecto (compartidos por el documento y la hoja de cálculo)."""

D = "Documentación y planeación"
B = "Programación multiagente (back-end)"
U = "Programación en Unity (front-end)"
MIEMBROS = ["Alberto López", "Gerardo Islas", "Gilberto Olavarrieta", "Juan Esteban Jaramillo", "Franco Salazar"]

# Calendario propuesto (viernes de cierre). Confirmar contra Canvas.
SPRINTS = [
    ("Sprint 1", "lun 16 nov 2026", "vie 20 nov 2026", "Primer incremento: correcciones, servicio de agentes, escena 3D preliminar"),
    ("Sprint 2", "lun 23 nov 2026", "vie 27 nov 2026", "Integración Unity–servicio, prompts, modelos 3D definitivos, prueba ciega"),
    ("Sprint 3", "lun 30 nov 2026", "vie 4 dic 2026", "Evaluación final (15 sesiones y 5 usuarios), resultados y entrega final"),
]

# id, historia, usuario, prioridad, puntos, sprint, estado
HISTORIAS = [
    ("HU-01", "Como emprendedor, quiero capturar mi pitch en un formulario con campos guiados, para que el panel lo evalúe con datos completos.", "Emprendedor", "Alta", 5, "Sprint 1", "Hecha (preliminar)"),
    ("HU-02", "Como emprendedor, quiero ver qué tiburones me recomienda la app y por qué, y poder cambiarlos, para elegir un panel útil para mi negocio.", "Emprendedor", "Alta", 5, "Sprint 1", "Hecha (preliminar)"),
    ("HU-03", "Como emprendedor, quiero ver en el estudio quién habla, con subtítulos, cámara y reflector, para seguir la conversación.", "Emprendedor", "Alta", 5, "Sprint 1", "Hecha (preliminar)"),
    ("HU-04", "Como emprendedor, quiero responder por escrito las preguntas de cada ronda, para defender mi idea con mis propios datos.", "Emprendedor", "Alta", 5, "Sprint 2", "En curso"),
    ("HU-05", "Como espectador, quiero ver a los tiburones debatir entre ellos, para entender por qué coinciden o discrepan.", "Espectador", "Media", 3, "Sprint 1", "Hecha (preliminar)"),
    ("HU-06", "Como emprendedor, quiero recibir la decisión (trato o sin trato) y poder negociar si solo hay un voto a favor, para mejorar mi oferta.", "Emprendedor", "Alta", 8, "Sprint 2", "En curso"),
    ("HU-07", "Como emprendedor, quiero un reporte de retroalimentación por tiburón con los criterios que no se evaluaron, para saber qué mejorar en mi pitch.", "Emprendedor", "Alta", 5, "Sprint 2", "En curso"),
    ("HU-08", "Como usuario, quiero pausar, cambiar la velocidad, abrir el historial y repetir una sesión (o usar una de ejemplo sin conexión), para revisar la conversación a mi ritmo.", "Usuario", "Media", 5, "Sprint 1", "Hecha (preliminar)"),
    ("HU-09", "Como profesor evaluador, quiero ver las métricas de una sesión (llamadas al modelo, rondas, duración, votos), para juzgar su costo y su coherencia.", "Evaluador", "Media", 3, "Sprint 3", "Pendiente"),
]

CRITERIOS = {
    "HU-01": ["Los campos obligatorios están marcados con * y «Siguiente» solo se activa cuando están completos.", "La valuación implícita (monto ÷ porcentaje) se muestra al momento.", "Los campos opcionales pueden quedar vacíos («Aún no lo sé»).", "Hay un botón «Cargar ejemplo» con 3 pitches."],
    "HU-02": ["La app marca exactamente 3 tiburones recomendados, cada uno con su motivo.", "El usuario puede cambiar la selección, pero el botón «Iniciar sesión» solo se activa con exactamente 3.", "La recomendación no usa el modelo de lenguaje (0 llamadas)."],
    "HU-03": ["Cada evento muestra emisor, destino, tipo y texto en el cuadro de diálogo.", "La cámara enfoca al emisor y el reflector lo ilumina.", "El indicador de ronda muestra «Ronda k de 3» o «Negociación»."],
    "HU-04": ["Al terminar las 3 preguntas aparecen 3 campos de respuesta (máximo 600 caracteres).", "«Enviar respuestas» solo se activa con las 3 respuestas escritas.", "El servicio reanuda la sesión al recibirlas."],
    "HU-05": ["En cada ronda hay una RÉPLICA de un tiburón dirigida a otro tiburón del panel.", "La réplica indica acuerdo o desacuerdo y se ve en el historial."],
    "HU-06": ["Con 2 o 3 votos a favor hay trato y el usuario elige entre su oferta y las contraofertas.", "Con 1 voto a favor se abre una negociación y los 3 votan una sola vez más.", "Con 0 votos a favor la decisión es «Sin trato»."],
    "HU-07": ["El reporte muestra voto, calificación de 1 a 5, frase clave y qué necesitaría cada tiburón.", "Incluye 3 fortalezas, 3 alertas, preguntas sin responder y 3 siguientes pasos.", "Lista los criterios de los tiburones que no estuvieron en el panel y el aviso de simulación educativa."],
    "HU-08": ["Los botones Pausa/Reanudar, 1x/2x y Saltar actúan sobre la reproducción.", "El botón «Historial» abre la lista de turnos (ronda, quién, tipo, texto) en cualquier momento.", "El interruptor «Usar sesión de ejemplo (sin conexión)» reproduce una sesión real guardada."],
    "HU-09": ["Se muestran llamadas al modelo, rondas, segundos y resultado de la sesión.", "Las métricas coinciden con las que calcula el servicio."],
}

# id, tarea, categoria, responsable, inicio, fin, estado, historia
TAREAS = [
    ("T01", "Revisar la retroalimentación del anteproyecto y proponer correcciones y mejoras de la app", D, "Gerardo Islas", "17 nov", "18 nov", "Hecha", "—"),
    ("T02", "Contexto y problema con datos y fuentes verificadas", D, "Gerardo Islas", "17 nov", "19 nov", "Hecha", "—"),
    ("T03", "Objetivos SMART y restricciones actualizadas", D, "Gilberto Olavarrieta", "17 nov", "18 nov", "Hecha", "—"),
    ("T04", "Historias de usuario, criterios de aceptación y backlog", D, "Franco Salazar", "18 nov", "19 nov", "Hecha", "Todas"),
    ("T05", "Grafo de LangGraph: panel de 3, réplica, voto secreto, negociación y reporte", B, "Alberto López", "17 nov", "19 nov", "Hecha", "HU-03, HU-05, HU-06"),
    ("T06", "Regla de votación y 27 pruebas unitarias", B, "Franco Salazar", "18 nov", "19 nov", "Hecha", "HU-06"),
    ("T07", "Recomendación del panel (regla en Python)", B, "Gerardo Islas", "17 nov", "18 nov", "Hecha", "HU-02"),
    ("T08", "API HTTP con FastAPI (rutas de tiburones, sesiones, respuestas, negociación, reporte)", B, "Gilberto Olavarrieta", "17 nov", "19 nov", "Hecha", "HU-04, HU-06"),
    ("T09", "Set de prueba de 5 pitches con guion y evaluador de 15 sesiones", B, "Gerardo Islas", "18 nov", "19 nov", "Hecha", "HU-09"),
    ("T10", "Prototipo de un agente (3 ideas × 2 tiburones) contra Azure OpenAI", B, "Alberto López", "16 nov", "17 nov", "Hecha", "—"),
    ("T11", "Proyecto Unity 6, estudio 3D con 3 sillones y prefabs", U, "Juan Esteban Jaramillo", "17 nov", "19 nov", "Hecha", "HU-03"),
    ("T12", "Pantallas y controles: formulario, panel, respuestas, negociación, historial y resultado", U, "Franco Salazar", "17 nov", "19 nov", "Hecha", "HU-01, HU-02, HU-08"),
    ("T13", "ClienteServicio (HTTP, polling de 1 s) y sesión de ejemplo sin conexión", U, "Gilberto Olavarrieta", "18 nov", "19 nov", "Hecha", "HU-08"),
    ("T14", "Figuras 1 a 9 y modelo de los agentes", D, "Juan Esteban Jaramillo", "18 nov", "19 nov", "Hecha", "—"),
    ("T15", "Revisar la sección de interacción (7.4) con las pruebas reales de integración", D, "Gilberto Olavarrieta", "23 nov", "25 nov", "Pendiente", "HU-04"),
    ("T16", "Documentar componentes gráficos y sus fuentes (8.2) con las imágenes definitivas", D, "Juan Esteban Jaramillo", "23 nov", "26 nov", "Pendiente", "HU-03"),
    ("T17", "Documentar prefabs y scripts (8.3 y 8.4) tras las animaciones", D, "Franco Salazar", "24 nov", "27 nov", "Pendiente", "HU-03"),
    ("T18", "Redactar los resultados parciales y mantener la bitácora", D, "Gerardo Islas", "25 nov", "27 nov", "Pendiente", "HU-09"),
    ("T19", "Mantener el backlog y el calendario (hoja de gestión)", D, "Alberto López", "23 nov", "27 nov", "En curso", "Todas"),
    ("T20", "Ajustar los prompts para que cada justificación cite un dato concreto (meta ≥ 85 %)", B, "Gerardo Islas", "23 nov", "26 nov", "Pendiente", "HU-06"),
    ("T21", "Pruebas de integración API ↔ Unity con el servicio en vivo", B, "Gilberto Olavarrieta", "23 nov", "26 nov", "Pendiente", "HU-04, HU-06"),
    ("T22", "Ampliar pruebas unitarias del panel recomendado y de la negociación", B, "Franco Salazar", "24 nov", "26 nov", "Pendiente", "HU-02, HU-06"),
    ("T23", "Prueba ciega: calificar 60 mensajes con la rúbrica y revisar transcripciones", B, "Juan Esteban Jaramillo", "24 nov", "27 nov", "Pendiente", "HU-05"),
    ("T24", "Guardar sesiones de forma persistente (checkpointer) para el historial", B, "Alberto López", "24 nov", "27 nov", "Pendiente", "HU-08"),
    ("T25", "Modelos 3D definitivos del ambiente de pecera (tiburones empresarios, pez payaso, coral y algas); evaluar el Fish Pack de Quaternius", U, "Juan Esteban Jaramillo", "23 nov", "27 nov", "Pendiente", "HU-03"),
    ("T26", "Animaciones por estado y ajuste de cámara", U, "Franco Salazar", "23 nov", "27 nov", "Pendiente", "HU-03, HU-05"),
    ("T27", "ClienteServicio contra el servicio real: errores y reconexión", U, "Gilberto Olavarrieta", "23 nov", "26 nov", "Pendiente", "HU-04"),
    ("T28", "Pulir historial y pantalla de métricas (HU-09)", U, "Gerardo Islas", "25 nov", "27 nov", "Pendiente", "HU-08, HU-09"),
    ("T29", "Flujo completo con usuario humano en Unity (de extremo a extremo)", U, "Alberto López", "24 nov", "27 nov", "Pendiente", "HU-04, HU-06, HU-07"),
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
BITACORA_E2 = [
    ("Revisión de la retroalimentación y propuesta de correcciones (documento «Correcciones al anteproyecto»)", "Gerardo Islas", "17–18 nov 2026"),
    ("Contexto y problema con fuentes verificadas; recomendación del panel; set de prueba de 5 pitches", "Gerardo Islas", "17–19 nov 2026"),
    ("Objetivos SMART, restricciones, API HTTP y cliente de Unity (ClienteServicio)", "Gilberto Olavarrieta", "17–19 nov 2026"),
    ("Historias de usuario, regla de votación con 27 pruebas y pantallas de la interfaz en Unity", "Franco Salazar", "17–19 nov 2026"),
    ("Grafo de LangGraph (réplica, voto secreto, negociación, reporte) y prototipo de un agente", "Alberto López", "16–19 nov 2026"),
    ("Estudio 3D de pecera (prefabs y ambiente), figuras y modelo de los agentes", "Juan Esteban Jaramillo", "17–19 nov 2026"),
]
