"""Grafo de LangGraph de una sesión de NeuralHub.

presentar -> preguntar -> responder -> replicar -> (3 rondas) -> votar -> decidir
   decidir -> condiciones (trato) | negociar -> revotar (1 voto a favor) | reporte (sin trato)
   condiciones / revotar -> reporte -> FIN

Presupuesto de llamadas al modelo (ver docs): 9 preguntas + 3 réplicas + 3 votos + 1 reporte = 16;
+3 si hay segunda votación = 19 como máximo. El emprendedor en autopiloto suma 1 llamada por ronda (3).
"""

import operator
from concurrent.futures import ThreadPoolExecutor
from typing import Annotated, TypedDict

from langgraph.graph import END, START, StateGraph
from langgraph.types import interrupt

from . import esquemas as E
from . import panel as P
from . import regla
from .pitch import Pitch, ficha, mxn, valuacion_mxn
from .tiburones import TIBURONES

MAX_RONDAS = 3
RONDA_NEGOCIACION = MAX_RONDAS + 1
MAX_CHARS_RESPUESTA = 600
REGLA_JUSTIFICACION = (
    "Tu justificación (máximo 45 palabras) debe nombrar tu criterio principal y citar AL MENOS UNA CIFRA escrita con número "
    "que aparezca en el pitch o en la conversación (por ejemplo el monto, el porcentaje, la valuación, el margen, las ventas o "
    "el número de clientes). Si el dato de tu criterio no existe, dilo y cita de todos modos una cifra del pitch.")


class Estado(TypedDict, total=False):
    pitch: dict
    panel: list
    modo: str                       # humano | autopiloto | guion
    ronda: int                      # rondas de preguntas completadas (0..3)
    orden: list                     # orden base de turnos (lo fija el moderador)
    historial: Annotated[list, operator.add]   # solo mensajes públicos
    preguntas: dict                 # preguntas de la ronda en curso {tiburon_id: texto}
    votos1: dict                    # {tiburon_id: decision}
    votos1_det: dict
    oferta: dict                    # oferta vigente {monto_mxn, porcentaje}
    ruta: str
    condiciones: dict
    resultado: str
    reporte: dict


# ---------- utilidades ----------

def nombre(emisor: str) -> str:
    if emisor in TIBURONES:
        return TIBURONES[emisor]["nombre"]
    return {"emprendedor": "Emprendedor", "moderador": "Moderador", "nodo_votacion": "Nodo de votación"}.get(emisor, emisor)


def hist_texto(estado) -> str:
    return "\n".join(f"[R{h['ronda']}] {nombre(h['emisor'])} ({h['tipo']}): {h['texto']}" for h in estado.get("historial", [])) \
        or "(todavía no hay conversación)"


def h(ronda, tipo, emisor, texto):
    return {"ronda": ronda, "tipo": tipo, "emisor": emisor, "texto": texto}


def rotar(orden: list, r: int) -> list:
    k = r % len(orden)
    return orden[k:] + orden[:k]


def sistema_tiburon(tid: str) -> str:
    t = TIBURONES[tid]
    return (
        f"[id:{tid}]\nEres el {t['nombre']} de un panel de inversionistas tipo Shark Tank México. {t['perfil']} "
        f"Tu criterio principal es: {t['criterio']}. Tu estilo: {t['estilo']}\nReglas: hablas en español de México, directo y breve (máximo 60 palabras); "
        "los montos van en pesos mexicanos; solo usas datos de la ficha y de la conversación: si falta un dato, dilo "
        "(\"no me dices X\") en lugar de inventarlo; usa las cifras calculadas tal cual, no hagas cuentas propias; "
        "si algo contradice lo dicho antes, señálalo."
    )


def sistema_emprendedor() -> str:
    return ("Eres el fundador que presenta su negocio ante un panel de inversionistas. Respondes en español de México, "
            "con seguridad. NUNCA inventes cifras, fechas ni hechos: usa solo lo que está en tu ficha. Si el dato no está en tu ficha, "
            "responde que aún no lo sabes y qué harías para averiguarlo. Máximo 70 palabras por respuesta.")


TIPOS_CONVERSACION = ("PITCH", "PREGUNTA", "RESPUESTA", "REPLICA", "OFERTA")


def sugerir_respuesta(llm, pitch: dict, eventos: list, tiburon_id: str) -> str:
    """Modo en vivo con ayuda: el agente empresario propone una respuesta a la última pregunta de `tiburon_id`.

    Usa los eventos públicos (no el estado del grafo) porque las preguntas de la ronda se emiten antes de que
    el nodo «preguntar» termine. Es 1 llamada al modelo por sugerencia; el usuario decide si la usa o la edita.
    """
    pregunta = next((e for e in reversed(eventos) if e["tipo"] == "PREGUNTA" and e["emisor"] == tiburon_id), None)
    if pregunta is None:
        raise ValueError(f"{nombre(tiburon_id)} todavía no ha hecho una pregunta.")
    p = Pitch(**pitch)
    conversacion = "\n".join(f"[R{e['ronda']}] {nombre(e['emisor'])} ({e['tipo']}): {e['texto']}"
                              for e in eventos if e["tipo"] in TIPOS_CONVERSACION) or "(todavía no hay conversación)"
    salida = llm.estructurado(E.Sugerencia, sistema_emprendedor(), (
        f"TU FICHA:\n{ficha(p)}\n\nCONVERSACIÓN:\n{conversacion}\n\n"
        f"El {nombre(tiburon_id)} te acaba de preguntar: \"{pregunta['texto']}\"\n"
        "Sugiere UNA respuesta breve y convincente que el emprendedor pueda decir tal cual o ajustar. "
        "Usa solo los datos de tu ficha; si el dato no está, reconócelo y di cómo lo obtendrías."))
    return salida.texto.strip()[:MAX_CHARS_RESPUESTA]


def _cfg(config):
    c = config["configurable"]
    return c["sesion"], c["llm"], c.get("guion")


# ---------- construcción ----------

def construir_grafo(checkpointer=None):
    # 1. Presentación del pitch y del panel (sin modelo)
    def presentar(estado, config):
        ses, _, _ = _cfg(config)
        pitch = Pitch(**estado["pitch"])
        val = valuacion_mxn(pitch)
        resumen = (f"{pitch.nombre}: pide {mxn(pitch.monto_mxn)} por el {pitch.porcentaje:g} % "
                   f"(valuación implícita {mxn(val)}). Problema: {pitch.problema} Solución: {pitch.solucion}")
        evs = [h(0, "PITCH", "emprendedor", resumen)]
        ses.emit("PITCH", "emprendedor", "tiburones", resumen, 0, pitch=estado["pitch"], valuacion_mxn=val)
        motivos = {f["tiburon_id"]: f["motivo"] for f in P.evaluar(pitch)}
        panel = estado["panel"]
        txt = "Panel de esta sesión: " + ", ".join(TIBURONES[t]["nombre"] for t in panel) + "."
        ses.emit("PANEL", "moderador", "todos", txt, 0,
                 panel=[{"tiburon_id": t, "nombre": TIBURONES[t]["nombre"], "motivo": motivos[t]} for t in panel])
        return {"orden": P.orden_de_turnos(pitch, panel), "ronda": 0, "oferta": {"monto_mxn": pitch.monto_mxn, "porcentaje": pitch.porcentaje},
                "historial": evs}

    # 2. Cada tiburón del panel hace una pregunta (3 llamadas)
    def preguntar(estado, config):
        ses, llm, _ = _cfg(config)
        r = estado["ronda"] + 1
        pitch = Pitch(**estado["pitch"])
        hechas = {}
        nuevos = []
        for tid in rotar(estado["orden"], estado["ronda"]):
            ya = "\n".join(f"- {nombre(k)}: {v}" for k, v in hechas.items()) or "(ninguna)"
            salida = llm.estructurado(E.Pregunta, sistema_tiburon(tid), (
                f"PITCH:\n{ficha(pitch)}\n\nCONVERSACIÓN:\n{hist_texto({'historial': estado['historial'] + nuevos})}\n\n"
                f"Vas en la ronda {r} de {MAX_RONDAS}. Preguntas ya hechas en esta ronda por otros tiburones:\n{ya}\n\n"
                "Haz UNA pregunta nueva, sobre el punto más débil de tu criterio. No repitas una pregunta que el emprendedor ya respondió. "
                "Cita un dato del pitch o señala que ese dato falta."))
            hechas[tid] = salida.pregunta.strip()
            ses.emit("PREGUNTA", tid, "emprendedor", hechas[tid], r)
            nuevos.append(h(r, "PREGUNTA", tid, hechas[tid]))
        return {"preguntas": hechas, "historial": nuevos}

    # 3. El emprendedor responde las 3 preguntas de la ronda (usuario: interrupt; autopiloto: 1 llamada; guion: 0)
    def responder(estado, config):
        ses, llm, guion = _cfg(config)
        r = estado["ronda"] + 1
        preguntas = estado["preguntas"]
        modo = estado["modo"]
        if modo == "humano":
            recibidas = interrupt({"tipo": "RESPUESTAS", "ronda": r,
                                   "preguntas": [{"tiburon_id": t, "texto": q} for t, q in preguntas.items()]})
        elif modo == "guion":
            recibidas = guion["respuestas"][str(r)]
        else:
            pitch = Pitch(**estado["pitch"])
            listado = "\n".join(f"[{t}] {q}" for t, q in preguntas.items())
            salida = llm.estructurado(E.Respuestas, sistema_emprendedor(), (
                f"TU FICHA:\n{ficha(pitch)}\n\nCONVERSACIÓN:\n{hist_texto(estado)}\n\n"
                f"Responde cada pregunta con una respuesta propia En tiburon_id copia EXACTAMENTE el id que está entre corchetes:\n{listado}"))
            por_id = {x.tiburon_id: x.texto for x in salida.respuestas}
            # Respaldo: si el modelo cambió los ids, se asignan por posición.
            if set(por_id) != set(preguntas) and len(salida.respuestas) == len(preguntas):
                por_id = {t: x.texto for t, x in zip(preguntas, salida.respuestas)}
            recibidas = por_id
        nuevos = []
        for t in preguntas:
            txt = str(recibidas.get(t) or "(sin respuesta)").strip()[:MAX_CHARS_RESPUESTA]
            ses.emit("RESPUESTA", "emprendedor", t, txt, r)
            nuevos.append(h(r, "RESPUESTA", "emprendedor", f"(a {nombre(t)}) {txt}"))
        return {"historial": nuevos}

    # 4. Un tiburón replica a otro (1 llamada). El moderador (nodo determinista) asigna quién replica y a quién:
    #    replica el último que preguntó en la ronda y el destino es el primero (el orden rota cada ronda).
    def replicar(estado, config):
        ses, llm, _ = _cfg(config)
        r = estado["ronda"] + 1
        rot = rotar(estado["orden"], estado["ronda"])
        yo, a = rot[-1], rot[0]
        pitch = Pitch(**estado["pitch"])
        pregunta_a = estado["preguntas"][a]
        salida = llm.estructurado(E.Replica, sistema_tiburon(yo), (
            f"PANEL: {', '.join(estado['panel'])}\nPITCH:\n{ficha(pitch)}\n\nCONVERSACIÓN:\n{hist_texto(estado)}\n\n"
            f"Te toca replicar. El moderador te asignó al {nombre(a)}, que preguntó en esta ronda: \"{pregunta_a}\". "
            f"Respóndele a él directamente (tú eres el {nombre(yo)}): di si estás de acuerdo o en desacuerdo con su pregunta o "
            "su preocupación, a la luz de la respuesta del emprendedor, y explica por qué desde tu criterio."))
        ses.emit("REPLICA", yo, a, salida.texto.strip(), r, postura=salida.postura)
        return {"ronda": r, "historial": [h(r, "REPLICA", yo, f"(a {nombre(a)}, {salida.postura}) {salida.texto.strip()}")]}

    def tras_replica(estado):
        return "votar" if estado["ronda"] >= MAX_RONDAS else "preguntar"

    # 5. Voto secreto y simultáneo (3 llamadas en paralelo). Nadie ve los votos de otros hasta decidir.
    def _normaliza_contra(v: dict, oferta: dict) -> dict:
        if v["decision"] != "contraoferta":
            v["contraoferta_monto_mxn"] = v["contraoferta_porcentaje"] = None
            return v
        m, p = v.get("contraoferta_monto_mxn"), v.get("contraoferta_porcentaje")
        if not (m and p and m > 0 and 0 < p <= 100):
            m, p = oferta["monto_mxn"], min(100.0, oferta["porcentaje"] + 5)
        v["contraoferta_monto_mxn"], v["contraoferta_porcentaje"] = float(m), float(p)
        return v

    def votar(estado, config):
        _, llm, _ = _cfg(config)
        pitch = Pitch(**estado["pitch"])
        of = estado["oferta"]
        base = (f"PANEL: {', '.join(estado['panel'])}\nPITCH:\n{ficha(pitch)}\n\nCONVERSACIÓN COMPLETA:\n{hist_texto(estado)}\n\n"
                f"OFERTA ACTUAL: monto={of['monto_mxn']} porcentaje={of['porcentaje']}\n\n"
                "Emite tu voto en secreto: invertir, contraoferta o salir. Calibra tu voto: invertir si los datos cumplen tu criterio; contraoferta si el negocio te interesa pero las condiciones o algún dato no te convencen; salir solo si tu criterio falla claramente o faltan los datos clave. " + REGLA_JUSTIFICACION +
                " Si haces contraoferta, da monto en MXN y porcentaje. No sabes cómo votan los demás.")

        def uno(tid):
            return tid, llm.estructurado(E.Voto, sistema_tiburon(tid), base).model_dump()

        with ThreadPoolExecutor(max_workers=3) as ex:
            det = dict(ex.map(uno, estado["panel"]))
        det = {t: _normaliza_contra(v, of) for t, v in det.items()}
        return {"votos1_det": det, "votos1": {t: v["decision"] for t, v in det.items()}}

    # 6. Nodo de votación (determinista): revela los votos y aplica la regla
    def decidir(estado, config):
        ses, _, _ = _cfg(config)
        det, votos = estado["votos1_det"], estado["votos1"]
        nuevos = []
        for t in estado["panel"]:
            v = det[t]
            txt = v["justificacion"].strip()
            if v["decision"] == "contraoferta":
                txt += f" Contraoferta: {mxn(v['contraoferta_monto_mxn'])} por el {v['contraoferta_porcentaje']:g} %."
            ses.emit("VOTO", t, "nodo_votacion", txt, MAX_RONDAS, decision=v["decision"], votacion=1,
                     contraoferta_monto_mxn=v["contraoferta_monto_mxn"], contraoferta_porcentaje=v["contraoferta_porcentaje"])
            nuevos.append(h(MAX_RONDAS, "VOTO", t, f"{v['decision']}. {txt}"))
        ruta = regla.primera_votacion(votos)
        cambios = {"ruta": ruta, "historial": nuevos}
        if ruta == "SIN_TRATO":
            ses.emit("DECISION", "nodo_votacion", "todos", "SIN TRATO", MAX_RONDAS, resultado="SIN_TRATO", condiciones=None)
            cambios["resultado"] = "SIN_TRATO"
        elif ruta == "NEGOCIACION":
            ses.emit("NEGOCIACION", "nodo_votacion", "emprendedor",
                     "Solo 1 tiburón votó a favor. Hay una ronda de negociación: ajusta tu oferta o acepta una contraoferta.",
                     RONDA_NEGOCIACION)
        return cambios

    def tras_decidir(estado):
        return {"TRATO": "condiciones", "NEGOCIACION": "negociar", "SIN_TRATO": "reporte"}[estado["ruta"]]

    def _contraofertas(estado):
        return [{"tiburon_id": t, "monto_mxn": v["contraoferta_monto_mxn"], "porcentaje": v["contraoferta_porcentaje"]}
                for t, v in estado["votos1_det"].items() if v["decision"] == "contraoferta"]

    # 7a. Trato en la primera votación: el emprendedor fija las condiciones finales
    def condiciones(estado, config):
        ses, _, _ = _cfg(config)
        orig = dict(estado["oferta"])
        contras = _contraofertas(estado)
        elegida = dict(orig, origen="original")
        if contras:
            if estado["modo"] == "humano":
                resp = interrupt({"tipo": "CONDICIONES", "original": orig, "contraofertas": contras})
                sel = resp.get("opcion", "original")
                elegida = next((dict(c, origen=c["tiburon_id"]) for c in contras if c["tiburon_id"] == sel), elegida)
            else:
                elegida = regla.elegir_contraoferta_autopiloto(contras, orig)
                if "tiburon_id" in elegida:
                    elegida["origen"] = elegida["tiburon_id"]
            texto = (f"Elijo la condición de {nombre(elegida['origen'])}." if elegida["origen"] != "original"
                     else "Mantengo mi oferta original.") + f" {mxn(elegida['monto_mxn'])} por el {elegida['porcentaje']:g} %."
            ses.emit("OFERTA", "emprendedor", "tiburones", texto, MAX_RONDAS)
        cond = {"monto_mxn": elegida["monto_mxn"], "porcentaje": elegida["porcentaje"], "origen": elegida["origen"]}
        ses.emit("DECISION", "nodo_votacion", "todos",
                 f"TRATO: {mxn(cond['monto_mxn'])} por el {cond['porcentaje']:g} % de la empresa.", MAX_RONDAS,
                 resultado="TRATO", condiciones=cond)
        return {"resultado": "TRATO", "condiciones": cond}

    # 7b. Un solo voto a favor: negociación (el usuario ajusta la oferta o acepta una contraoferta)
    def negociar(estado, config):
        ses, _, guion = _cfg(config)
        orig = dict(estado["oferta"])
        contras = _contraofertas(estado)
        if estado["modo"] == "humano":
            resp = interrupt({"tipo": "NEGOCIACION", "oferta_actual": orig, "contraofertas": contras,
                              "votos": estado["votos1"]})
        elif guion and guion.get("negociacion"):
            resp = guion["negociacion"]
        elif contras:
            mejor = regla.elegir_contraoferta_autopiloto(contras, {"monto_mxn": 0.0, "porcentaje": 1.0})
            resp = {"accion": "aceptar_contraoferta", "tiburon_id": mejor["tiburon_id"]}
        else:
            resp = {"accion": "ajustar", "monto_mxn": orig["monto_mxn"], "porcentaje": min(100.0, orig["porcentaje"] + 5)}
        nueva, texto = orig, "Mantengo mi oferta."
        accion = resp.get("accion")
        if accion == "aceptar_contraoferta":
            c = next((c for c in contras if c["tiburon_id"] == resp.get("tiburon_id")), None)
            if c:
                nueva = {"monto_mxn": c["monto_mxn"], "porcentaje": c["porcentaje"]}
                texto = f"Acepto la contraoferta de {nombre(c['tiburon_id'])}."
        elif accion == "ajustar":
            m, p = resp.get("monto_mxn"), resp.get("porcentaje")
            if m and p and m > 0 and 0 < p <= 100:
                nueva = {"monto_mxn": float(m), "porcentaje": float(p)}
                texto = "Ajusto mi oferta."
        texto += f" Nueva oferta: {mxn(nueva['monto_mxn'])} por el {nueva['porcentaje']:g} %."
        ses.emit("OFERTA", "emprendedor", "tiburones", texto, RONDA_NEGOCIACION, oferta=nueva)
        return {"oferta": nueva, "historial": [h(RONDA_NEGOCIACION, "OFERTA", "emprendedor", texto)]}

    # 7c. Segunda y última votación (3 llamadas en paralelo, solo invertir o salir)
    def revotar(estado, config):
        ses, llm, _ = _cfg(config)
        pitch = Pitch(**estado["pitch"])
        of = estado["oferta"]
        n_inv = list(estado["votos1"].values()).count("invertir")
        n_con = list(estado["votos1"].values()).count("contraoferta")
        n_sal = list(estado["votos1"].values()).count("salir")
        resumen = (f"Primera votación: {n_inv} invertir, {n_con} contraoferta, {n_sal} salir. Contraofertas: " +
                   (", ".join(f"{nombre(c['tiburon_id'])}: {mxn(c['monto_mxn'])} por {c['porcentaje']:g} %" for c in _contraofertas(estado)) or "ninguna"))
        base = (f"PANEL: {', '.join(estado['panel'])}\nPITCH:\n{ficha(pitch)}\n\nCONVERSACIÓN COMPLETA:\n{hist_texto(estado)}\n\n"
                f"{resumen}\nOFERTA ACTUAL: monto={of['monto_mxn']} porcentaje={of['porcentaje']}\n\n"
                "Esta es la votación final. Vota solo invertir o salir con la oferta actual. " + REGLA_JUSTIFICACION)

        def uno(tid):
            return tid, llm.estructurado(E.VotoFinal, sistema_tiburon(tid), base).model_dump()

        with ThreadPoolExecutor(max_workers=3) as ex:
            det = dict(ex.map(uno, estado["panel"]))
        votos = {t: v["decision"] for t, v in det.items()}
        for t in estado["panel"]:
            ses.emit("VOTO", t, "nodo_votacion", det[t]["justificacion"].strip(), RONDA_NEGOCIACION,
                     decision=det[t]["decision"], votacion=2)
        res = regla.segunda_votacion(votos)
        if res == "TRATO":
            ses.emit("DECISION", "nodo_votacion", "todos",
                     f"TRATO: {mxn(of['monto_mxn'])} por el {of['porcentaje']:g} % de la empresa.", RONDA_NEGOCIACION,
                     resultado="TRATO", condiciones=dict(of, origen="negociacion"))
        else:
            ses.emit("DECISION", "nodo_votacion", "todos", "SIN TRATO", RONDA_NEGOCIACION, resultado="SIN_TRATO", condiciones=None)
        return {"resultado": res, "condiciones": dict(of, origen="negociacion") if res == "TRATO" else None,
                "historial": [h(RONDA_NEGOCIACION, "VOTO", t, f"{det[t]['decision']}. {det[t]['justificacion']}") for t in estado["panel"]]}

    # 8. Reporte de retroalimentación (1 llamada)
    def reporte(estado, config):
        ses, llm, _ = _cfg(config)
        pitch = Pitch(**estado["pitch"])
        panel_ids = estado["panel"]
        votos = {t: estado["votos1"][t] for t in panel_ids}
        base = (f"PANEL: {', '.join(panel_ids)}\nPITCH:\n{ficha(pitch)}\n\nCONVERSACIÓN COMPLETA:\n{hist_texto(estado)}\n\n"
                f"RESULTADO: {estado['resultado']}; condiciones: {estado.get('condiciones')}\n\n"
                "Redacta la retroalimentación para el emprendedor: por cada tiburón del panel (tiburon_id exacto) una calificación de 1 a 5 "
                "en su criterio, la frase de la conversación que la justifica y qué necesitaría ver para invertir; además 3 fortalezas, "
                "3 alertas, las preguntas que el emprendedor no supo responder y 3 siguientes pasos concretos. Español de México.")
        sis = ("Eres un mentor de emprendedores que resume lo que pasó en un panel de inversionistas simulado. "
               "Eres concreto y honesto; no inventas datos que no aparecen en la conversación.")
        rep = llm.estructurado(E.Reporte, sis, base).model_dump()
        for x in rep["por_tiburon"]:
            x["calificacion"] = max(1, min(5, int(x["calificacion"])))
        rep["resultado"] = estado["resultado"]
        rep["condiciones"] = estado.get("condiciones")
        rep["valuacion_mxn"] = valuacion_mxn(pitch)
        rep["votos_primera_votacion"] = votos
        rep["criterios_no_evaluados"] = P.criterios_no_evaluados(panel_ids)
        rep["aviso"] = "Simulación educativa; no es asesoría financiera."
        resumen = ("Fortalezas: " + "; ".join(rep["fortalezas"]) + ". Alertas: " + "; ".join(rep["alertas"]) +
                   ". Siguientes pasos: " + "; ".join(rep["siguientes_pasos"]) + ".")
        ronda_final = RONDA_NEGOCIACION if estado.get("ruta") == "NEGOCIACION" else MAX_RONDAS
        ses.emit("REPORTE", "servicio", "emprendedor", resumen, ronda_final, reporte=rep)
        return {"reporte": rep}

    g = StateGraph(Estado)
    for nom, fn in [("presentar", presentar), ("preguntar", preguntar), ("responder", responder), ("replicar", replicar),
                    ("votar", votar), ("decidir", decidir), ("condiciones", condiciones), ("negociar", negociar),
                    ("revotar", revotar), ("reporte", reporte)]:
        g.add_node(nom, fn)
    g.add_edge(START, "presentar")
    g.add_edge("presentar", "preguntar")
    g.add_edge("preguntar", "responder")
    g.add_edge("responder", "replicar")
    g.add_conditional_edges("replicar", tras_replica, ["preguntar", "votar"])
    g.add_edge("votar", "decidir")
    g.add_conditional_edges("decidir", tras_decidir, ["condiciones", "negociar", "reporte"])
    g.add_edge("condiciones", "reporte")
    g.add_edge("negociar", "revotar")
    g.add_edge("revotar", "reporte")
    g.add_edge("reporte", END)
    return g.compile(checkpointer=checkpointer)
