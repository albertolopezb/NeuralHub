import subprocess, sys
DOT = r"C:\Program Files\Graphviz\bin\dot.exe"
N, V, A, C, L = "#00165C", "#4F1FD0", "#1060D0", "#13B8F0", "#E8F0FF"
HEAD = f'bgcolor=white; fontname="Helvetica"; node[fontname="Helvetica",fontsize=13,color="{N}",fontcolor="{N}",style="rounded,filled",fillcolor="{L}",shape=box]; edge[fontname="Helvetica",fontsize=11,color="{V}",fontcolor="{N}"];'


def render(nombre, src):
    open(f"{nombre}.dot", "w", encoding="utf8").write(src)
    subprocess.check_call([DOT, "-Tpng", "-Gdpi=170", f"{nombre}.dot", "-o", f"{nombre}.png"])


def rec(nombre, attrs, metodos, color=L):
    a = "\\l".join(attrs) + "\\l" if attrs else ""
    m = "\\l".join(metodos) + "\\l" if metodos else ""
    return f'{nombre}[shape=record,style=filled,fillcolor="{color}",label="{{{nombre.replace("_", " ")}|{a}|{m}}}",fontsize=11];'


py = "#F1ECFF"
un = L
cl = [
    "digraph G{ " + HEAD + " rankdir=TB; nodesep=.25; ranksep=.5; splines=true;",
    f'subgraph cluster_py{{label="Servicio de agentes (Python: LangGraph + FastAPI)";fontcolor="{V}";color="{V}";style="rounded,dashed";fontname="Helvetica";',
    rec("Pitch", ["nombre, sector, etapa", "problema, solucion", "equipo_*, monto_mxn", "porcentaje, ventas_12m_mxn"], ["valuacion_mxn()", "es_hueco(campo)"], py),
    rec("Panel", ["tamaño = 3"], ["recomendar(pitch)", "orden_de_turnos()"], py),
    rec("AgenteTiburon", ["id, nombre, perfil", "criterio, campos"], ["preguntar()", "replicar()", "votar()", "revotar()"], py),
    rec("AgenteEmprendedor", ["modo: humano | autopiloto"], ["responder(preguntas)", "negociar()"], py),
    rec("Moderador", [], ["fija_orden()", "asigna_replica()"], py),
    rec("NodoVotacion", [], ["primera_votacion()", "puede_mantener_original()", "segunda_votacion()"], py),
    rec("GrafoSesion", ["estado: Estado", "checkpointer"], ["presentar()", "preguntar()", "responder()", "replicar()", "votar()", "decidir()", "negociar()", "reporte()"], py),
    rec("Sesion_FastAPI", ["eventos[], estado", "pendiente"], ["emit(evento)", "iniciar()", "reanudar()", "metricas()"], py),
    "}",
    f'subgraph cluster_un{{label="Unity (C#)";fontcolor="{A}";color="{A}";style="rounded,dashed";fontname="Helvetica";',
    rec("AppNeuralHub", ["vista"], ["Ir(vista)", "IniciarSesion()"]),
    rec("ClienteServicio", ["urlBase, usarMock", "recibidos: Evento[]"], ["CrearSesion()", "Sondear() cada 1 s", "EnviarRespuestas()", "EnviarNegociacion()"]),
    rec("ReproductorSesion", ["pausado, velocidad", "historial"], ["ReproducirSiguiente()", "AlternarPausa()", "SaltarAnimacion()"]),
    rec("Estudio", ["sillones[3]"], ["IniciarSesion()", "Aplicar(evento)"]),
    rec("Tiburon_Unity", ["perfilId, estado"], ["Escuchar()", "Preguntar()", "Replicar()", "Votar()", "Reaccionar()"]),
    rec("Emprendedor", ["estado"], ["Presentar()", "Responder()", "Negociar()"]),
    rec("CamaraEstudio", ["plano"], ["Cambiar(plano)"]),
    rec("Reflector", [], ["Iluminar(punto)"]),
    rec("Pantalla", [], ["MostrarOferta()"]),
    rec("Evento", ["n, ronda, tipo", "emisor, destino", "texto, estado, decision"], []),
    rec("InterfazDebate", [], ["Dibujar(): dialogo, ronda, votos"]),
    rec("MenuInicio", [], ["Dibujar()"]),
    rec("FormularioPitch", ["pitch"], ["Json()", "Completo()"]),
    rec("EleccionPanel", ["elegidos (3)"], ["PrecargarRecomendados()"]),
    rec("ControlReproduccion", [], ["pausa, 1x/2x, saltar"]),
    rec("PanelHistorial", [], ["Dibujar()"]),
    rec("PanelRespuestas", [], ["Dibujar()"]),
    rec("PanelNegociacion", [], ["Dibujar()"]),
    rec("PantallaResultado", [], ["Dibujar()"]),
    "}",
    "Pitch->Panel; Panel->GrafoSesion; GrafoSesion->AgenteTiburon[label=\"3\"]; GrafoSesion->AgenteEmprendedor; GrafoSesion->Moderador; GrafoSesion->NodoVotacion; Sesion_FastAPI->GrafoSesion;",
    f'ClienteServicio->Sesion_FastAPI[label="HTTP",color="{C}",penwidth=2]; AppNeuralHub->ClienteServicio; AppNeuralHub->ReproductorSesion; ReproductorSesion->Estudio; ReproductorSesion->Evento;',
    'Estudio->Tiburon_Unity[label="3"]; Estudio->Emprendedor; Estudio->CamaraEstudio; Estudio->Reflector; Estudio->Pantalla;',
    "AppNeuralHub->MenuInicio; AppNeuralHub->FormularioPitch; AppNeuralHub->EleccionPanel; AppNeuralHub->ControlReproduccion; AppNeuralHub->PanelHistorial; AppNeuralHub->PanelRespuestas; AppNeuralHub->PanelNegociacion; AppNeuralHub->PantallaResultado; AppNeuralHub->InterfazDebate;",
    "}",
]


# ---- F6 grafo de LangGraph exportado del código real
sys.path.insert(0, r"D:\TEC\TC2008B\Reto\NeuralHub\servicio")
from neuralhub.grafo import construir_grafo

g = construir_grafo().get_graph()
lines = ["digraph G{ " + HEAD + " rankdir=TB; nodesep=.4; ranksep=.4;"]
for n in g.nodes:
    if n == "__start__":
        lines.append(f'{n}[shape=circle,label="",width=.25,fillcolor="{N}"];')
    elif n == "__end__":
        lines.append(f'{n}[shape=doublecircle,label="",width=.25,fillcolor="{N}"];')
    else:
        lines.append(f'{n}[label="{n}"];')
for e in g.edges:
    lines.append(f'{e.source}->{e.target}[style={"dashed" if e.conditional else "solid"}];')
lines.append("}")
render("f06_grafo_langgraph", "\n".join(lines))

# ---- F8 estados emprendedor y sesión
render("f08_estados_emprendedor_sesion", f'''digraph G{{ {HEAD} rankdir=TB; nodesep=.3; ranksep=.45;
subgraph cluster_e{{label="Emprendedor (usuario o agente empresario)";fontcolor="{V}";color="{V}";style="rounded,dashed";fontname="Helvetica";
 e0[shape=circle,label="",width=.2,fillcolor="{N}"]; Pres[label="Presentando"]; EspE[label="Esperando"]; Resp[label="Respondiendo"]; Neg[label="Negociando"]; Rec[label="Recibiendo\\nresultado"];
 e0->Pres; Pres->EspE[label="pitch enviado"]; EspE->Resp[label="llega una\\npregunta"]; Resp->EspE[label="respuesta\\nenviada"]; EspE->Neg[label="1 voto a favor"]; Elig[label="Eligiendo\\ncondiciones"]; EspE->Elig[label="contraofertas"]; Elig->Rec[label="elige o rechaza"]; Neg->EspE[label="oferta enviada"]; EspE->Rec[label="DECISIÓN"];}}
subgraph cluster_s{{label="Sesión (moderador y nodo de votación)";fontcolor="{A}";color="{A}";style="rounded,dashed";fontname="Helvetica";
 s0[shape=circle,label="",width=.2,fillcolor="{N}"]; Cre[label="Creada"]; Pan[label="Panel de 3\\nlisto"]; Ron[label="Ronda k\\n(k = 1..3)"]; Vot[label="Votación"]; Ngc[label="Negociación"]; Cer[label="Cerrada"];
 s0->Cre; Cre->Pan[label="usuario inicia"]; Pan->Ron[label="pitch"]; Ron->Ron[label="k < 3"]; Ron->Vot[label="k = 3"]; Vot->Ngc[label="1 a favor"]; Ngc->Vot[label="2.ª votación"]; Eli[label="Eligiendo\\ncondiciones"]; Vot->Eli[label="2 o 3 a favor\\ncon contraofertas"]; Eli->Cer[label="trato, o sin trato\\nsi las rechaza"]; Vot->Cer[label="trato / sin trato\\n+ reporte"];}}
}}''')


# ---- F9 modelo del agente
def agente(titulo, percep, creen, deseos, planes, accion, color):
    return f'''digraph G{{ {HEAD} rankdir=TB; nodesep=.35; ranksep=.4;
subgraph cluster_a{{label="{titulo}";fontcolor="{color}";color="{color}";style="rounded";fontname="Helvetica";fontsize=14;
 P[label="Percepciones\\n{percep}"]; B[label="Creencias\\n{creen}"]; D[label="Deseos / metas\\n{deseos}"]; I[label="Planes / intenciones\\n{planes}"]; Ac[label="Acciones\\n{accion}",fillcolor="#D3F9D8"]; P->B->D->I->Ac;
 B->I[style=dashed,label="LLM (salida estructurada)"];}}
Ent[label="Entorno\\n(historial público de la sesión,\\ngrafo de LangGraph)",shape=box3d,fillcolor="#FFF3BF"];
Ent->P[label="eventos"]; Ac->Ent[label="mensajes"];}}'''


render("f09a_agente_tiburon", agente(
    "Agente tiburón (6 perfiles; 3 activos por sesión)",
    "pitch y cifras calculadas\\lpreguntas y respuestas\\lréplicas de otros\\lresultado de la 1.ª votación\\l(solo en negociación)\\l",
    "su perfil y criterio\\lpitch + valuación\\lhistorial público\\l(NO los votos de otros)\\l",
    "obtener datos de su criterio\\ldecidir con justificación\\l",
    "preguntar sobre su criterio\\levaluar respuestas\\lreplicar si se le asigna\\lvotar / contraofertar\\l",
    "PREGUNTA · RÉPLICA\\lVOTO (secreto)\\lCONTRAOFERTA\\l", V))
render("f09b_agente_emprendedor", agente(
    "Agente empresario (el usuario humano ocupa su lugar en vivo)",
    "una pregunta a la vez\\lcontraofertas\\lDECISIÓN\\l",
    "su ficha del pitch\\lhistorial público\\l(no inventa datos)\\l",
    "obtener trato cediendo\\lel menor porcentaje\\l",
    "responder con su ficha\\lsugerir o mejorar respuesta\\lelegir la condición de\\lmayor valuación\\l",
    "RESPUESTA · OFERTA\\lSUGERENCIA (al usuario)\\l", A))

# ---- F7 diagrama de secuencia de una ronda (matplotlib)
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

act = ["Usuario\n(emprendedor)", "Moderador", "Tiburón 1", "Tiburón 2", "Tiburón 3", "Servicio\n→ Unity"]
x = {a: i * 2.2 for i, a in enumerate(act)}
fig, ax = plt.subplots(figsize=(13, 8.2))
ax.set_xlim(-1.2, 2.2 * 5 + 1.2); ax.set_ylim(-1, 15.6); ax.axis("off")
for a in act:
    ax.add_patch(plt.Rectangle((x[a] - 0.95, 14.6), 1.9, 0.95, color=N if a != act[0] else C, zorder=3))
    ax.text(x[a], 15.07, a, ha="center", va="center", color="white", fontsize=10, fontweight="bold", zorder=4)
    ax.plot([x[a], x[a]], [14.6, -0.5], color="#9AA5C7", lw=1, ls="--", zorder=1)
pasos = [
    (act[1], act[2], "orden de turnos (más huecos primero)", 13.6, V, False),
    (act[2], act[0], "PREGUNTA (Tiburón 1)", 12.7, A, False),
    (act[0], act[0], "responde (el grafo se pausa)", 12.0, "#E67700", False),
    (act[0], act[2], "RESPUESTA", 11.1, A, False),
    (act[3], act[0], "PREGUNTA (Tiburón 2)", 10.2, A, False),
    (act[0], act[3], "RESPUESTA", 9.3, A, False),
    (act[4], act[0], "PREGUNTA (Tiburón 3)", 8.4, A, False),
    (act[0], act[4], "RESPUESTA", 7.5, A, False),
    (act[1], act[4], "asigna réplica al último que preguntó", 6.4, V, False),
    (act[4], act[2], "RÉPLICA (acuerdo/desacuerdo)", 5.4, V, False),
    (act[4], act[5], "cada turno se emite como evento al momento", 4.5, "#868E96", True),
    (act[5], act[0], "Unity lo reproduce con voz y animación", 3.5, "#868E96", True),
    (act[1], act[1], "k < 3 → siguiente ronda; si k = 3 → voto secreto", 2.2, V, False),
]
for o, d, t, y, c, dash in pasos:
    if o == d:
        ax.annotate("", xy=(x[o] + 0.9, y - 0.35), xytext=(x[o] + 0.9, y + 0.0), arrowprops=dict(arrowstyle="->", color=c, lw=1.6, connectionstyle="arc3,rad=-0.6"))
        ax.text(x[o] + 1.1, y - 0.15, t, ha="left", va="center", fontsize=9, color=N)
    else:
        ax.annotate("", xy=(x[d], y), xytext=(x[o], y), arrowprops=dict(arrowstyle="->", color=c, lw=1.8, ls="--" if dash else "-"))
        ax.text((x[o] + x[d]) / 2, y + 0.18, t, ha="center", va="bottom", fontsize=9, color=N)
fig.savefig("f07_secuencia_ronda.png", dpi=170, bbox_inches="tight", facecolor="white")
print("ok")
