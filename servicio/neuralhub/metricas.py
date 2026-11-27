"""Métricas automáticas de una sesión (las mismas que usan los objetivos del documento)."""

import re

from .tiburones import menciona_criterio

TIPO_POR_ID = {"PREGUNTA": "preguntas", "REPLICA": "replicas", "VOTO": "votos"}


def tiene_dato_concreto(texto: str) -> bool:
    """Heurística: la justificación cita un número (cifra, porcentaje o monto) o un término del pitch entre comillas."""
    return bool(re.search(r"\d", texto)) or '"' in texto or "«" in texto


def panel_votos(votos, k):
    return [e for e in votos if e.get("votacion") == k]


FALTA_DATO = re.compile(r"falta|no (me |nos )?(dices|das|mencionas|indicas|explicas|has dicho|veo|queda claro)|no (est[aá]|aparece|incluyes|tienes|hay)", re.I)


def cita_dato_o_falta(texto: str) -> bool:
    """Segunda parte del objetivo 3: la pregunta cita un dato (cifra o comillas) o señala que falta."""
    return tiene_dato_concreto(texto) or bool(FALTA_DATO.search(texto))


def calcular(ses) -> dict:
    ev = ses.eventos
    preguntas = [e for e in ev if e["tipo"] == "PREGUNTA"]
    replicas = [e for e in ev if e["tipo"] == "REPLICA"]
    votos = [e for e in ev if e["tipo"] == "VOTO"]
    decision = next((e for e in reversed(ev) if e["tipo"] == "DECISION"), None)
    rondas = max([e["ronda"] for e in ev if e["tipo"] == "PREGUNTA"] or [0])
    n_ok = [e["n"] for e in ev]
    return {
        "llamadas_modelo": ses.llm.llamadas,
        "segundos_modelo": round(ses.llm.segundos, 1),
        "segundos_total": round(ses.segundos, 1),
        "rondas_preguntas": rondas,
        "n_eventos": len(ev),
        "eventos_en_orden": n_ok == list(range(1, len(ev) + 1)),
        "preguntas": len(preguntas),
        "replicas": len(replicas),
        "replicas_con_postura": sum(1 for e in replicas if e.get("postura") in ("acuerdo", "desacuerdo")),
        "replicas_mencionan_otro_tiburon": sum(
            1 for e in replicas if e["destino"] != e["emisor"] and e["destino"] in ses.panel),
        "preguntas_mencionan_criterio": sum(1 for e in preguntas if menciona_criterio(e["texto"], e["emisor"])),
        "preguntas_citan_dato": sum(1 for e in preguntas if cita_dato_o_falta(e["texto"])),
        "votos": len(votos),
        "justificaciones_mencionan_criterio": sum(1 for e in votos if menciona_criterio(e["texto"], e["emisor"])),
        "justificaciones_con_dato": sum(1 for e in votos if tiene_dato_concreto(e["texto"])),
        # Secreto: los votos de cada votación se publican juntos (n consecutivos), después de todo el debate.
        "votos_publicados_juntos": all(
            [e["n"] for e in votos if e.get("votacion") == k] ==
            list(range(min(e["n"] for e in votos if e.get("votacion") == k),
                       min(e["n"] for e in votos if e.get("votacion") == k) + len(panel_votos(votos, k))))
            for k in {e.get("votacion") for e in votos}) and
            all(e["n"] < min(v["n"] for v in votos) for e in ev if e["tipo"] in ("PREGUNTA", "RESPUESTA", "REPLICA"))
            if votos else True,   # a media sesión todavía no hay votos
        "hubo_negociacion": any(e["tipo"] == "NEGOCIACION" for e in ev),
        "modo": ses.modo,
        "ayuda": getattr(ses, "ayuda", False),
        "sugerencias": getattr(ses, "sugerencias", 0),
        "resultado": decision.get("resultado") if decision else None,
        "terminada": ses.estado == "terminada",
    }
