from neuralhub.sesiones import a_su_manera, sin_etiquetas


def test_teo_dice_startup_y_los_demas_no_cambian():
    texto = "Empresa joven: ¿qué porcentaje de la empresa ofreces a estas empresas? Es empresarial."
    assert a_su_manera(texto, "tiburon_tecnologico") == "Startup joven: ¿qué porcentaje de la startup ofreces a estas startups? Es empresarial."
    assert a_su_manera(texto, "tiburon_financiero") == texto


def test_quita_marcas_internas():
    assert sin_etiquetas("En el pitch (R0) dices 38 % [R1].") == "En el pitch dices 38 %."
    assert sin_etiquetas("Tienes IA (PITCH) y patente (pitch, R2)?") == "Tienes IA y patente?"


def test_respeta_parentesis_normales():
    texto = "Vendes el medidor ($1,200) y la suscripción (premium)."
    assert sin_etiquetas(texto) == texto
