from neuralhub.sesiones import sin_etiquetas


def test_quita_marcas_internas():
    assert sin_etiquetas("En el pitch (R0) dices 38 % [R1].") == "En el pitch dices 38 %."
    assert sin_etiquetas("Tienes IA (PITCH) y patente (pitch, R2)?") == "Tienes IA y patente?"


def test_respeta_parentesis_normales():
    texto = "Vendes el medidor ($1,200) y la suscripción (premium)."
    assert sin_etiquetas(texto) == texto
