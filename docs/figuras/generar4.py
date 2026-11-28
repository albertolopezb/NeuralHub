"""Diagramas de clases del incremento 2, separados en servicio (Python) y Unity (C#) para que sean legibles."""
import subprocess

DOT = r"C:\Program Files\Graphviz\bin\dot.exe"
N, V, A, C, L = "#00165C", "#4F1FD0", "#1060D0", "#13B8F0", "#E8F0FF"
HEAD = f'bgcolor=white; fontname="Helvetica"; node[fontname="Helvetica",fontsize=12,color="{N}",fontcolor="{N}",shape=record,style=filled]; edge[fontname="Helvetica",fontsize=10,color="{V}",fontcolor="{N}"];'


def rec(nombre, attrs, metodos, color=L, tipo=""):
    titulo = f"«{tipo}»\\n{nombre}" if tipo else nombre
    a = "\\l".join(attrs) + "\\l" if attrs else ""
    m = "\\l".join(metodos) + "\\l" if metodos else ""
    return f'{nombre}[fillcolor="{color}",label="{{{titulo}|{a}|{m}}}"];'


def render(nombre, cuerpo, rankdir="TB"):
    src = "digraph G{ " + HEAD + f" rankdir={rankdir}; nodesep=.3; ranksep=.45;\n" + "\n".join(cuerpo) + "\n}"
    open(f"{nombre}.dot", "w", encoding="utf8").write(src)
    subprocess.check_call([DOT, "-Tpng", "-Gdpi=170", f"{nombre}.dot", "-o", f"{nombre}.png"])


py = "#F1ECFF"
render("f05a_clases_servicio", [
    rec("Pitch", ["nombre, sector, etapa", "problema, solucion", "equipo_*, discurso", "monto_mxn, porcentaje", "ventas_12m_mxn, margen_bruto_pct"], ["valuacion_mxn()", "es_hueco(campo)", "discurso()"], py),
    rec("Panel", ["TAM_PANEL = 3"], ["recomendar(pitch)", "orden_de_turnos()", "criterios_no_evaluados()"], py, "módulo"),
    rec("AgenteTiburon", ["id, perfil, criterio, estilo", "persona, personalidad", "campos, palabras_clave"], ["preguntar()", "replicar()", "votar()", "revotar()", "(nivel: facil, medio, dificil)"], py, "módulo y nodos"),
    rec("AgenteEmprendedor", ["modo: humano, autopiloto, guion"], ["responder(pregunta)", "sugerir(tiburon, borrador)", "negociar()", "elegir_condiciones()"], py, "nodos del grafo"),
    rec("Moderador", ["(lógica dentro de presentar,", "preguntar y replicar)"], ["orden_de_turnos()"], py, "nodos del grafo"),
    rec("NodoVotacion", ["(nodos decidir y revotar)"], ["primera_votacion()", "puede_mantener_original()", "segunda_votacion()"], py, "nodos del grafo"),
    rec("GrafoSesion", ["Estado: pitch, panel, ronda,", "historial, votos, oferta", "checkpointer MemorySaver"], ["presentar()", "preguntar()", "responder() [interrupt]", "replicar()", "votar()", "decidir()", "condiciones()", "negociar()", "revotar()", "reporte()"], py, "grafo"),
    rec("LLM", ["llamadas, segundos", "Azure OpenAI gpt-4.1-mini"], ["texto()", "estructurado(esquema)"], py),
    rec("Sesion", ["eventos[], estado, pendiente"], ["emit(evento)", "iniciar()", "reanudar(valor)", "metricas()", "guardar()"], py),
    rec("API_FastAPI", ["/tiburones, /ejemplos, /panel", "/sesiones, /eventos", "/respuestas, /negociacion", "/condiciones, /reporte", "/sugerencia, /pitch/extraer", "/audio, /voz/transcribir"], [], py, "módulo"),
    rec("Voz", ["SAPI es-MX (texto a voz)", "Whisper base (voz a texto)"], ["cargar_modelo()", "sintetizar(texto, emisor)", "transcribir(wav)"], py, "módulo"),
    rec("Extraer", ["PitchExtraido (esquema)", "docx, pdf, txt o texto"], ["texto_de_archivo()", "extraer(llm, texto)"], py, "módulo"),
    'Pitch->Panel; Panel->GrafoSesion; GrafoSesion->AgenteTiburon[label="3"]; GrafoSesion->AgenteEmprendedor; GrafoSesion->Moderador; GrafoSesion->NodoVotacion;',
    'AgenteTiburon->LLM; AgenteEmprendedor->LLM[style=dashed,label="agente empresario"]; Sesion->GrafoSesion; Sesion->LLM; API_FastAPI->Sesion; API_FastAPI->Voz; API_FastAPI->Extraer; Extraer->LLM;',
])
render("f05b_clases_unity", [
    rec("AppNeuralHub", ["vista, nivel, modoEnVivo"], ["Ir(vista)", "ElegirNivel(n)", "IniciarSesion()", "EscucharPresentacion(id)"], tipo="MonoBehaviour"),
    rec("ClienteServicio", ["urlBase, usarMock", "recibidos: Evento[]", "pendiente"], ["CrearSesion(nivel)", "Sondear() cada 1 s", "EnviarRespuestas()", "PedirSugerencia(borrador)", "ExtraerPitch(archivo)", "ObtenerAudio(n)", "Transcribir(wav)"], tipo="MonoBehaviour"),
    rec("ReproductorSesion", ["pausado, velocidad", "turnoUsuario, Progreso"], ["ReproducirSiguiente()", "PedirVoz(evento)", "Responder(texto)", "IrA(turno)", "AlternarPausa()"], tipo="MonoBehaviour"),
    rec("Estudio", ["sillones[3]", "panel: Tiburon[3]"], ["IniciarSesion()", "Aplicar(evento)", "RetirarPanel()"], tipo="MonoBehaviour"),
    rec("Tiburon", ["perfilId, estado", "modelos[2], juegos[6]"], ["Asignar(id)", "Preguntar()", "Replicar(postura)", "Votar()", "Reaccionar()"], tipo="MonoBehaviour"),
    rec("Emprendedor", ["estado, animador"], ["Presentar()", "Responder()", "Negociar()", "RecibirResultado()"], tipo="MonoBehaviour"),
    rec("CamaraEstudio", ["planoActual"], ["Cambiar(plano)", "Vuelo(avance)"], tipo="MonoBehaviour"),
    rec("Reflector", [], ["Iluminar(punto)"], tipo="MonoBehaviour"),
    rec("Pantalla", ["lámina actual"], ["MostrarLamina()"], tipo="MonoBehaviour"),
    rec("PantallasDatos", ["barras[3], icono"], ["MostrarOferta()", "Voto()", "Calificaciones()"], tipo="MonoBehaviour"),
    rec("SetEmprendedor", ["empresa, logo"], ["Vestir(nombre, sector)"], tipo="MonoBehaviour"),
    rec("Sonidos", ["vozActiva, musicaActiva"], ["Efecto()", "Voz(clip)", "Musical()", "PausarVoz()"], tipo="MonoBehaviour"),
    rec("TemasPitch", ["(estática)"], ["Detectar(pregunta)", "Mostrar(pantalla)"]),
    rec("Niveles_Personas", ["(datos estáticos)"], ["Ejemplo(nivel)", "De(id)"]),
    rec("DialogoArchivo", ["(estática)"], ["Elegir() : ruta"]),
    rec("GrabadorVoz", ["(estática)"], ["Iniciar()", "Detener() : WAV"]),
    rec("Evento", ["n, ronda, tipo", "emisor, destino, texto", "estado, decision, postura"], []),
    rec("Programa", ["IntroPrograma", "ConocerTiburones", "FinalPrograma", "BarraRondas"], ["Dibujar()"]),
    rec("Ambiente", ["Caustica, Ondular, Nadador,", "TextoNitido (MonoBehaviour)", "Reloj (estática)"], ["Update()"]),
    rec("Pantallas_IMGUI", ["MenuInicio, FormularioPitch,", "EleccionPanel, SeleccionModo,", "BurbujaRespuesta, InterfazDebate,", "PanelNegociacion, PantallaResultado,", "ControlReproduccion, PanelHistorial,", "PanelRespuestas, ListaSesiones"], ["Dibujar()"]),
    'AppNeuralHub->ClienteServicio; AppNeuralHub->ReproductorSesion; AppNeuralHub->Pantallas_IMGUI; AppNeuralHub->Programa; AppNeuralHub->Sonidos;',
    'ReproductorSesion->Estudio; ReproductorSesion->PantallasDatos; ReproductorSesion->Sonidos; ReproductorSesion->Evento; ReproductorSesion->ClienteServicio[label="audio"];',
    'Estudio->Ambiente[style=dashed];', 
    'Estudio->Tiburon[label="3"]; Estudio->Emprendedor; Estudio->CamaraEstudio; Estudio->Reflector[minlen=2]; Estudio->Pantalla[minlen=2]; Estudio->SetEmprendedor[minlen=2]; Estudio->TemasPitch[minlen=3];',
    'Pantallas_IMGUI->DialogoArchivo; ClienteServicio->PantallasDatos[style=invis]; Evento->GrabadorVoz[style=invis]; GrabadorVoz->Niveles_Personas[style=invis];',
    'Pantallas_IMGUI->GrabadorVoz; Pantallas_IMGUI->Niveles_Personas[constraint=false]; Programa->CamaraEstudio[constraint=false];',
])
print("ok")
