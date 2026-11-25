"""Diagramas de clases del incremento 2, separados en servicio (Python) y Unity (C#) para que sean legibles."""
import subprocess

DOT = r"C:\Program Files\Graphviz\bin\dot.exe"
N, V, A, C, L = "#00165C", "#4F1FD0", "#1060D0", "#13B8F0", "#E8F0FF"
HEAD = f'bgcolor=white; fontname="Helvetica"; node[fontname="Helvetica",fontsize=12,color="{N}",fontcolor="{N}",shape=record,style=filled]; edge[fontname="Helvetica",fontsize=10,color="{V}",fontcolor="{N}"];'


def rec(nombre, attrs, metodos, color=L):
    a = "\\l".join(attrs) + "\\l" if attrs else ""
    m = "\\l".join(metodos) + "\\l" if metodos else ""
    return f'{nombre}[fillcolor="{color}",label="{{{nombre}|{a}|{m}}}"];'


def render(nombre, cuerpo, rankdir="TB"):
    src = "digraph G{ " + HEAD + f" rankdir={rankdir}; nodesep=.3; ranksep=.45;\n" + "\n".join(cuerpo) + "\n}"
    open(f"{nombre}.dot", "w", encoding="utf8").write(src)
    subprocess.check_call([DOT, "-Tpng", "-Gdpi=170", f"{nombre}.dot", "-o", f"{nombre}.png"])


py = "#F1ECFF"
render("f05a_clases_servicio", [
    rec("Pitch", ["nombre, sector, etapa", "problema, solucion", "equipo_*", "monto_mxn, porcentaje", "ventas_12m_mxn, margen_bruto_pct"], ["valuacion_mxn()", "es_hueco(campo)"], py),
    rec("Panel", ["TAM_PANEL = 3"], ["recomendar(pitch)", "orden_de_turnos()", "criterios_no_evaluados()"], py),
    rec("AgenteTiburon", ["id, perfil, criterio, estilo", "campos, palabras_clave"], ["preguntar()", "replicar()", "votar()", "revotar()"], py),
    rec("AgenteEmprendedor", ["modo: humano | autopiloto | guion"], ["responder(preguntas)", "negociar()", "elegir_condiciones()"], py),
    rec("Moderador", ["(lógica dentro de presentar,", "preguntar y replicar)"], ["orden_de_turnos()", "asignar_replica()"], py),
    rec("NodoVotacion", ["(nodos decidir y revotar)"], ["primera_votacion()", "segunda_votacion()"], py),
    rec("GrafoSesion", ["Estado: pitch, panel, ronda,", "historial, votos, oferta", "checkpointer MemorySaver"], ["presentar()", "preguntar()", "responder() [interrupt]", "replicar()", "votar()", "decidir()", "condiciones()", "negociar()", "revotar()", "reporte()"], py),
    rec("LLM", ["llamadas, segundos", "Azure OpenAI gpt-4.1-mini"], ["texto()", "estructurado(esquema)"], py),
    rec("Sesion", ["eventos[], estado, pendiente"], ["emit(evento)", "iniciar()", "reanudar(valor)", "metricas()", "guardar()"], py),
    rec("API_FastAPI", ["/tiburones, /ejemplos, /panel", "/sesiones, /eventos", "/respuestas, /negociacion", "/condiciones, /reporte", "/audio, /voz/transcribir"], [], py),
    rec("Voz", ["SAPI es-MX (texto a voz)", "Whisper base (voz a texto)"], ["sintetizar(texto, emisor)", "transcribir(wav)"], py),
    'Pitch->Panel; Panel->GrafoSesion; GrafoSesion->AgenteTiburon[label="3"]; GrafoSesion->AgenteEmprendedor; GrafoSesion->Moderador; GrafoSesion->NodoVotacion;',
    'AgenteTiburon->LLM; AgenteEmprendedor->LLM[style=dashed,label="autopiloto"]; Sesion->GrafoSesion; Sesion->LLM; API_FastAPI->Sesion; API_FastAPI->Voz;',
])
render("f05b_clases_unity", [
    rec("AppNeuralHub", ["vista, mostrarHistorial"], ["Ir(vista)", "IniciarSesion()", "RepetirSesion()", "RespondedorAutomatico()"]),
    rec("ClienteServicio", ["urlBase, usarMock", "recibidos: Evento[]", "pendiente"], ["CrearSesion()", "Sondear() cada 1 s", "EnviarRespuestas()", "EnviarNegociacion()", "ObtenerAudio(n)", "Transcribir(wav)"]),
    rec("ReproductorSesion", ["pausado, velocidad", "historial, votos"], ["ReproducirSiguiente()", "IrA(turno)", "Anterior()", "Siguiente()", "AlternarPausa()"]),
    rec("Estudio", ["sillones[3]", "panel: Tiburon[3]"], ["IniciarSesion()", "Aplicar(evento)", "Preparar()"]),
    rec("Tiburon", ["perfilId, estado"], ["Escuchar()", "Preguntar()", "Replicar()", "Votar()", "Reaccionar()"]),
    rec("Emprendedor", ["estado"], ["Presentar()", "Responder()", "Negociar()"]),
    rec("CamaraEstudio", ["plano"], ["Cambiar(plano)"]),
    rec("Reflector", [], ["Iluminar(punto)"]),
    rec("Pantalla", ["titulo, detalle, icono"], ["MostrarOferta()", "MostrarIcono(sector)"]),
    rec("PantallasDatos", ["barras[3], icono"], ["MostrarOferta()", "FijarPanel()", "Voto()", "Calificaciones()"]),
    rec("Sonidos", ["vozActiva, silencio"], ["Efecto(nombre)", "Voz(clip)", "FijarSilencio()"]),
    rec("GrabadorVoz", ["(estática)"], ["Iniciar()", "Detener() : WAV"]),
    rec("Ambiente", ["Caustica, Ondular, Nadador"], ["Update()"]),
    rec("Evento", ["n, ronda, tipo", "emisor, destino, texto", "estado, decision, postura"], []),
    rec("Pantallas_IMGUI", ["MenuInicio, FormularioPitch,", "EleccionPanel, ControlReproduccion,", "PanelHistorial, InterfazDebate,", "PanelRespuestas, PanelNegociacion,", "PantallaResultado, ListaSesiones"], ["Dibujar()"]),
    'AppNeuralHub->ClienteServicio; AppNeuralHub->ReproductorSesion; AppNeuralHub->Pantallas_IMGUI; AppNeuralHub->Sonidos;',
    'ReproductorSesion->Estudio; ReproductorSesion->PantallasDatos; ReproductorSesion->Sonidos; ReproductorSesion->Evento; ReproductorSesion->ClienteServicio[label="audio"];',
    'Estudio->Tiburon[label="3"]; Estudio->Emprendedor; Estudio->CamaraEstudio; Estudio->Reflector; Estudio->Pantalla; Pantallas_IMGUI->GrabadorVoz; Pantallas_IMGUI->ClienteServicio;',
])
print("ok")
