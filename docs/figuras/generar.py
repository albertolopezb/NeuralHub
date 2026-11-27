"""Genera las figuras del documento (Graphviz + matplotlib) con la identidad NeuralHub."""
import subprocess, sys, os
DOT = r"C:\Program Files\Graphviz\bin\dot.exe"
NEATO = r"C:\Program Files\Graphviz\bin\neato.exe"
N, V, A, C, L = "#00165C", "#4F1FD0", "#1060D0", "#13B8F0", "#E8F0FF"
HEAD = f'bgcolor=white; fontname="Helvetica"; node[fontname="Helvetica",fontsize=13,color="{N}",fontcolor="{N}",style="rounded,filled",fillcolor="{L}",shape=box]; edge[fontname="Helvetica",fontsize=11,color="{V}",fontcolor="{N}"];'

def render(nombre, src, motor=DOT, extra=()):
    p = f"{nombre}.dot"
    open(p, "w", encoding="utf8").write(src)
    subprocess.check_call([motor, "-Tpng", "-Gdpi=170", *extra, p, "-o", f"{nombre}.png"])

# ---- F1 actividades
render("f01_actividades", f'''digraph G{{ {HEAD} rankdir=TB; nodesep=.35; ranksep=.32;
ini[shape=circle,label="",width=.25,fillcolor="{N}"]; fin[shape=doublecircle,label="",width=.25,fillcolor="{N}"];
a1[label="Usuario llena el formulario del pitch\n(o lo autocompleta con un documento o texto)"]; a2[label="Python calcula la valuación\n(monto / porcentaje)"];
a3[label="La app recomienda un panel de 3\n(regla en Python, 0 llamadas)"]; a4[label="Usuario acepta o cambia el panel\ny pulsa «Iniciar sesión»"];
a5[label="Intro del episodio y pitch hablado\n(la petición va al final)"];
subgraph cluster_r{{label="Ronda k (k = 1, 2, 3)";fontname="Helvetica";fontcolor="{V}";color="{V}";style="rounded,dashed";
 r1[label="El siguiente tiburón pregunta\n(orden del moderador, 1 a la vez)"]; r2[label="Usuario responde esa pregunta\n(el grafo se pausa)",fillcolor="#FFF3BF"];
 r3[label="Un tiburón replica a otro\n(RÉPLICA)"]; r4[shape=diamond,label="¿k < 3?",fillcolor="white"]; r1->r2; r2->r1[label="faltan tiburones",constraint=false]; r2->r3->r4;}}
v[label="Voto secreto y simultáneo\nde los 3 tiburones"]; d[shape=diamond,label="¿Cuántos votos\na favor?",fillcolor="white"];
c1[label="Condiciones: oferta original solo si 2 o más\nvotaron «invertir»; si no, una contraoferta"]; n1[label="Negociación: usuario ajusta\nla oferta o acepta una contraoferta"]; n2[label="Segunda votación (3 tiburones)"];
d2[shape=diamond,label="¿2 o más\na favor?",fillcolor="white"];
t[label="DECISIÓN: TRATO",fillcolor="#D3F9D8"]; s[label="DECISIÓN: SIN TRATO",fillcolor="#FFE3E3"]; rep[label="Reporte de retroalimentación\ny cierre del episodio en Unity"];
ini->a1->a2->a3->a4->a5->r1; r4->r1[label="sí"]; r4->v[label="no"]; v->d;
d->c1[label="2 o 3"]; d->n1[label="1"]; d->s[label="0"]; c1->t; c1->s[label="rechaza"]; n1->n2->d2; d2->t[label="sí"]; d2->s[label="no"]; t->rep; s->rep; rep->fin;}}''')

# ---- F2 estados del tiburón
render("f02_estados_tiburon", f'''digraph G{{ {HEAD} rankdir=TB; nodesep=.6; ranksep=.5;
s[shape=circle,label="",width=.25,fillcolor="{N}"];
Fuera[label="Fuera del panel"]; Esp[label="Esperando"]; Esc[label="Escuchando"]; Pre[label="Preguntando"]; Eva[label="Evaluando"]; Rep[label="Replicando",fillcolor="#E5DBFF"]; Vot[label="Votando"]; Rea[label="Reaccionando"];
s->Fuera; Fuera->Esp[label="elegido en\nel panel"]; Esp->Esc[label="pitch"]; Esc->Pre[label="su turno\nde preguntar"]; Pre->Eva[label="el usuario\nresponde"];
Eva->Esc[label="pregunta\nde otro"]; Eva->Rep[label="se le asigna\nreplicar"]; Rep->Eva[label="réplica\nenviada"]; Eva->Pre[label="siguiente\nronda"]; Eva->Vot[label="terminan\nlas 3 rondas"];
Vot->Eva[label="negociación"]; Vot->Rea[label="DECISIÓN"]; Rea->Fuera[label="sesión cerrada"];}}''')

# ---- F3 comunicaciones
render("f03_comunicaciones", f'''digraph G{{ {HEAD} rankdir=LR; nodesep=.8; ranksep=1.3; compound=true;
E[label="Emprendedor\n(usuario humano o\nagente empresario)",fillcolor="{C}",fontcolor=white];
M[label="Moderador\n(nodo determinista)"]; NV[label="Nodo de votación\n(determinista)"]; S[label="Servicio → Unity\n(eventos HTTP)",fillcolor="#FFF3BF"];
subgraph cluster_t{{label="Panel: 3 tiburones elegidos de 6";fontname="Helvetica";fontcolor="{V}";color="{V}";style="rounded,dashed";
 T1[label="Tiburón 1"]; T2[label="Tiburón 2"]; T3[label="Tiburón 3"];}}
E->T2[label="PITCH, RESPUESTA,\nOFERTA",lhead=cluster_t]; T2->E[label="PREGUNTA",ltail=cluster_t];
T1->T2[label="RÉPLICA",color="{V}",penwidth=2.5,fontcolor="{V}",constraint=false]; T2->T3[label="RÉPLICA",color="{V}",penwidth=2.5,fontcolor="{V}",constraint=false]; T3->T1[label="RÉPLICA",color="{V}",penwidth=2.5,fontcolor="{V}",constraint=false];
M->T2[label="PANEL / turnos",lhead=cluster_t]; T2->NV[label="VOTO secreto de cada uno\n(la CONTRAOFERTA viaja dentro)",ltail=cluster_t]; NV->E[label="DECISIÓN, NEGOCIACIÓN"]; NV->S[label="DECISIÓN"]; S->E[label="REPORTE"];}}''')

# ---- F4 boceto (vista superior, posiciones fijas)
render("f04_boceto_escena", f'''graph G{{ bgcolor=white; node[fontname="Helvetica",fontsize=18,color="{N}",fontcolor="{N}",style="filled",fillcolor="{L}",shape=box]; edge[style=invis];
panta[label="PANTALLA PRINCIPAL\nlámina del tema de cada pregunta",pos="0,9!",width=6,height=.6,fillcolor="{N}",fontcolor=white];
s1[label="Podio 1\ntiburón + letrero",pos="-4.2,5.2!",width=2.7,height=.9,fillcolor="#E5DBFF"];
s2[label="Podio 2\ntiburón + letrero",pos="0,6.4!",width=2.7,height=.9,fillcolor="#E5DBFF"];
s3[label="Podio 3\ntiburón + letrero",pos="4.2,5.2!",width=2.7,height=.9,fillcolor="#E5DBFF"];
emp[label="Emprendedor\n(avatar del usuario)",pos="0,1.5!",shape=ellipse,width=3.0,height=1.0,fillcolor="{C}",fontcolor=white];
pod[label="Set del emprendedor\n(letrero del programa)",pos="0,0.0!",width=3.1,height=.55];
ofe[label="Pantalla\nde la oferta",pos="-5.6,8.2!",width=2.1,fillcolor="{L}"];
vot[label="Pantalla\nde votos",pos="5.6,8.2!",width=2.1,fillcolor="{L}"];
ven[label="Ventanal curvo al océano (peces y tiburón que nadan detrás)",pos="0,10.3!",width=9.5,height=.4,fillcolor="#D0F0FF"];
cam[label="Cámara principal\n(cambia de plano)",pos="-5.2,-1.5!",shape=component,width=2.9,fillcolor="#FFF3BF"];
ref[label="Reflector\n(sigue al turno)",pos="5.2,1.5!",shape=house,width=2.3,fillcolor="#FFF3BF"];
ui[label="Interfaz: barra de rondas · tarjeta del tema · subtítulos · controles",pos="0,-1.6!",width=6,height=.5,fillcolor="{L}",style="dashed,filled"];
}}''', motor=NEATO)
