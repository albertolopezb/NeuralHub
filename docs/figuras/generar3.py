"""Figura del incremento 2: arquitectura de comunicación entre agentes y con Unity."""
import subprocess

DOT = r"C:\Program Files\Graphviz\bin\dot.exe"
N, V, A, C, L = "#00165C", "#4F1FD0", "#1060D0", "#13B8F0", "#E8F0FF"
HEAD = f'bgcolor=white; fontname="Helvetica"; node[fontname="Helvetica",fontsize=12,color="{N}",fontcolor="{N}",style="rounded,filled",fillcolor="{L}",shape=box]; edge[fontname="Helvetica",fontsize=10,color="{V}",fontcolor="{N}"];'
src = f'''digraph G{{ {HEAD} rankdir=LR; nodesep=.35; ranksep=.7; compound=true;
subgraph cluster_u{{label="Unity (cliente)";fontcolor="{A}";color="{A}";style="rounded,dashed";
 UI[label="Interfaz del usuario\\n(formulario, respuestas,\\nvoz, negociación)"]; CS[label="ClienteServicio\\nUnityWebRequest\\nsondeo cada 1 s"]; RP[label="ReproductorSesion\\ncola de eventos,\\nvoz y navegación"]; UI->CS; CS->RP[label="eventos JSON"];}}
subgraph cluster_s{{label="Servicio de agentes (FastAPI + LangGraph)";fontcolor="{V}";color="{V}";style="rounded,dashed";
 API[label="API REST\\n/sesiones, /respuestas,\\n/negociacion, /audio, /voz"];
 subgraph cluster_g{{label="Grafo de LangGraph: estado compartido (pizarra) + checkpointer";fontcolor="{V}";color="{V}";style="rounded";
  ST[label="Estado de la sesión\\nhistorial público, ronda,\\noferta, votos secretos",shape=cylinder,fillcolor="#FFF3BF"];
  MOD[label="Moderador\\n(determinista)"]; T1[label="Tiburón 1"]; T2[label="Tiburón 2"]; T3[label="Tiburón 3"]; VOT[label="Nodo de votación\\n(regla 2 de 3)"]; EMP[label="Emprendedor\\n(usuario vía interrupt\\no agente empresario)"];
  MOD->ST[label="turnos"]; T1->ST[label="REQUEST / INFORM"]; T2->ST; T3->ST[label="PROPOSE"]; ST->VOT[label="votos secretos"]; VOT->ST[label="DECISIÓN"]; EMP->ST[label="INFORM / ACCEPT"];
 }}
 LLM[label="Azure OpenAI\\ngpt-4.1-mini\\nsalida estructurada (Pydantic)",shape=component,fillcolor="#E5DBFF"];
 VOZ[label="Voz local\\nWindows SAPI (es-MX)\\nWhisper (faster-whisper)",shape=component,fillcolor="#E5DBFF"];
 API->ST[label="invoke / Command(resume)",lhead=cluster_g]; T2->LLM[label="HTTPS",style=dashed]; API->VOZ[style=dashed];
}}
CS->API[label="HTTP + JSON",color="{C}",penwidth=2]; API->CS[label="eventos {{n, tipo, emisor,\\ndestino, texto}}",color="{C}",penwidth=2];
}}'''
open("f11_arquitectura_comunicacion.dot", "w", encoding="utf8").write(src)
subprocess.check_call([DOT, "-Tpng", "-Gdpi=170", "f11_arquitectura_comunicacion.dot", "-o", "f11_arquitectura_comunicacion.png"])
print("ok")
