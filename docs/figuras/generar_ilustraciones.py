"""Ilustraciones planas originales para la pantalla del estudio: una por sector del pitch y una por tema de pregunta.

Se dibujan con Pillow a 3x y se reducen (antialias). Salida: unity/NeuralHubUnity/Assets/Resources/Laminas/*.png
"""
import math
from pathlib import Path

from PIL import Image, ImageDraw

SALIDA = Path(__file__).resolve().parents[2] / "unity" / "NeuralHubUnity" / "Assets" / "Resources" / "Laminas"
S = 3
W, H = 800, 800

NAVY, AZUL, CIAN = "#0B2447", "#2D6CDF", "#35C6F4"
CORAL, SOL, MENTA = "#FF7A6B", "#FFC857", "#3DDC97"
BLANCO, GRIS, VIOLETA, ARENA = "#FFFFFF", "#C9D6E8", "#7C5CFC", "#F4D8A8"


class Lienzo:
    def __init__(self, fondo):
        self.im = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.im)
        self.circulo(400, 400, 380, fondo)

    def _p(self, pts):
        return [(x * S, y * S) for x, y in pts]

    def circulo(self, x, y, r, c, borde=None, g=0):
        self.d.ellipse([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S], fill=c, outline=borde, width=g * S)

    def elipse(self, x0, y0, x1, y1, c):
        self.d.ellipse([x0 * S, y0 * S, x1 * S, y1 * S], fill=c)

    def caja(self, x0, y0, x1, y1, c, r=18, borde=None, g=0):
        self.d.rounded_rectangle([x0 * S, y0 * S, x1 * S, y1 * S], radius=r * S, fill=c, outline=borde, width=g * S)

    def poli(self, pts, c):
        self.d.polygon(self._p(pts), fill=c)

    def linea(self, pts, c, g=12):
        self.d.line(self._p(pts), fill=c, width=g * S, joint="curve")
        for x, y in (pts[0], pts[-1]):
            self.circulo(x, y, g / 2, c)

    def arco(self, x0, y0, x1, y1, a, b, c, g=14):
        self.d.arc([x0 * S, y0 * S, x1 * S, y1 * S], a, b, fill=c, width=g * S)

    def sector(self, x, y, r, a, b, c):
        self.d.pieslice([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S], a, b, fill=c)

    def estrella(self, x, y, r, c, puntas=5):
        pts = []
        for i in range(puntas * 2):
            a = -math.pi / 2 + i * math.pi / puntas
            k = r if i % 2 == 0 else r * 0.45
            pts.append((x + k * math.cos(a), y + k * math.sin(a)))
        self.poli(pts, c)

    def moneda(self, x, y, r):
        self.circulo(x, y, r, "#E5A82E")
        self.circulo(x, y - 4, r - 8, SOL)
        self.linea([(x, y - r * 0.45), (x, y + r * 0.35)], "#B97F12", max(6, r // 6))

    def persona(self, x, y, e, cuerpo, piel=ARENA, cabello=NAVY):
        self.caja(x - 62 * e, y + 20 * e, x + 62 * e, y + 150 * e, cuerpo, r=int(48 * e))
        self.circulo(x, y - 38 * e, 48 * e, piel)
        self.sector(x, y - 44 * e, 52 * e, 180, 360, cabello)

    def hoja(self, x, y, e, c):
        pts = [(x + e * 90 * math.sin(t) * 0.55, y - e * 90 * math.cos(t)) for t in [i * math.pi / 20 for i in range(21)]]
        pts += [(2 * x - px, py) for px, py in reversed(pts)]
        self.poli(pts, c)
        self.linea([(x, y - 70 * e), (x, y + 105 * e)], "#1E8C5E", int(9 * e))

    def guardar(self, nombre):
        SALIDA.mkdir(parents=True, exist_ok=True)
        self.im.resize((W, H), Image.LANCZOS).save(SALIDA / f"{nombre}.png")


# ---------- sectores (el producto) ----------
def sustentabilidad_y_energia():
    L = Lienzo("#12456B")
    L.circulo(590, 190, 70, SOL)
    for i in range(8):
        a = i * math.pi / 4
        L.linea([(590 + 90 * math.cos(a), 190 + 90 * math.sin(a)), (590 + 124 * math.cos(a), 190 + 124 * math.sin(a))], SOL, 14)
    L.caja(200, 230, 520, 650, BLANCO, 48)
    L.caja(238, 272, 482, 452, NAVY, 24)
    L.arco(270, 308, 450, 488, 180, 360, "#2A4A78", 22)
    L.arco(270, 308, 450, 488, 180, 292, MENTA, 22)
    L.linea([(360, 398), (408, 346)], BLANCO, 10)
    L.circulo(360, 398, 14, BLANCO)
    L.poli([(378, 480), (322, 560), (362, 560), (342, 622), (410, 530), (368, 530), (392, 480)], SOL)
    L.hoja(580, 560, 1.0, MENTA)
    L.guardar("sector_sustentabilidad_y_energia")


def alimentos_y_bebidas():
    L = Lienzo("#14564E")
    L.hoja(560, 270, 1.35, MENTA)
    L.hoja(250, 300, 0.9, "#6FE3B0")
    L.poli([(190, 420), (610, 420), (566, 630), (234, 630)], ARENA)
    L.caja(165, 385, 635, 432, "#E3B873", 22)
    L.elipse(250, 300, 390, 400, CORAL)
    L.elipse(360, 288, 500, 400, "#8FD14F")
    L.elipse(455, 318, 570, 398, SOL)
    L.caja(165, 385, 635, 432, "#E3B873", 22)
    L.circulo(400, 525, 62, "#1E8C5E")
    L.hoja(400, 520, 0.45, BLANCO)
    L.guardar("sector_alimentos_y_bebidas")


def tecnologia():
    L = Lienzo("#1B2F72")
    L.caja(150, 210, 650, 540, GRIS, 28)
    L.caja(176, 236, 624, 514, NAVY, 14)
    for i, h in enumerate([80, 130, 108, 175]):
        L.caja(214 + i * 62, 480 - h, 256 + i * 62, 480, [CIAN, AZUL, VIOLETA, MENTA][i], 10)
    L.linea([(220, 350), (288, 318), (350, 336), (470, 268)], SOL, 12)
    L.circulo(470, 268, 16, SOL)
    L.circulo(540, 400, 54, VIOLETA)
    L.sector(540, 400, 54, -90, 160, CIAN)
    L.circulo(540, 400, 24, NAVY)
    L.poli([(110, 548), (690, 548), (646, 600), (154, 600)], BLANCO)
    L.estrella(640, 170, 48, SOL, 4)
    L.estrella(170, 160, 30, CIAN, 4)
    L.guardar("sector_tecnologia")


def consumo_y_moda():
    L = Lienzo("#5B2A68")
    L.arco(300, 170, 500, 390, 180, 360, BLANCO, 24)
    L.poli([(225, 285), (575, 285), (610, 640), (190, 640)], CORAL)
    L.poli([(330, 370), (368, 350), (432, 350), (470, 370), (515, 430), (468, 462), (452, 440), (452, 560), (348, 560), (348, 440), (332, 462), (285, 430)], BLANCO)
    L.arco(368, 322, 432, 378, 0, 180, CORAL, 12)
    L.estrella(620, 220, 52, SOL, 4)
    L.estrella(185, 235, 34, CIAN, 4)
    L.guardar("sector_consumo_y_moda")


def salud():
    L = Lienzo("#0F5470")
    L.circulo(305, 320, 112, CORAL)
    L.circulo(495, 320, 112, CORAL)
    L.poli([(204, 370), (596, 370), (400, 620)], CORAL)
    L.linea([(150, 420), (290, 420), (335, 330), (395, 515), (445, 385), (478, 420), (650, 420)], BLANCO, 20)
    L.caja(580, 150, 626, 270, MENTA, 10)
    L.caja(543, 187, 663, 233, MENTA, 10)
    L.guardar("sector_salud")


def educacion():
    L = Lienzo("#243B80")
    L.poli([(400, 470), (160, 400), (160, 610), (400, 680)], BLANCO)
    L.poli([(400, 470), (640, 400), (640, 610), (400, 680)], GRIS)
    for k in range(3):
        L.linea([(200, 468 + 46 * k), (358, 514 + 46 * k)], AZUL, 10)
        L.linea([(442, 514 + 46 * k), (600, 468 + 46 * k)], AZUL, 10)
    L.poli([(400, 140), (640, 238), (400, 336), (160, 238)], SOL)
    L.poli([(272, 296), (400, 348), (528, 296), (528, 372), (400, 420), (272, 372)], "#E5A82E")
    L.linea([(610, 250), (610, 366)], CORAL, 12)
    L.circulo(610, 380, 22, CORAL)
    L.guardar("sector_educacion")


def servicios_profesionales():
    L = Lienzo("#1F3D5C")
    L.caja(318, 205, 482, 330, None, 30, borde=GRIS, g=26)
    L.caja(175, 290, 625, 610, "#C77B3E", 40)
    L.caja(175, 290, 625, 440, "#E0954F", 40)
    L.caja(362, 408, 438, 476, SOL, 14)
    L.circulo(590, 240, 62, MENTA)
    L.linea([(560, 242), (584, 266), (624, 218)], BLANCO, 16)
    L.guardar("sector_servicios_profesionales")


# ---------- temas de pregunta ----------
def mercado():
    L = Lienzo("#123C69")
    L.circulo(430, 330, 215, BLANCO)
    L.circulo(430, 330, 160, CORAL)
    L.circulo(430, 330, 105, BLANCO)
    L.circulo(430, 330, 52, CORAL)
    L.linea([(430, 330), (610, 170)], NAVY, 18)
    L.poli([(582, 158), (655, 135), (632, 208)], SOL)
    for x, c in [(220, CIAN), (370, MENTA), (520, VIOLETA)]:
        L.persona(x, 575, 0.8, c)
    L.guardar("tema_mercado")


def numeros():
    L = Lienzo("#10464F")
    for i, h in enumerate([140, 215, 300, 400]):
        L.caja(190 + i * 112, 620 - h, 274 + i * 112, 620, [CIAN, AZUL, VIOLETA, MENTA][i], 16)
    L.linea([(200, 390), (330, 320), (445, 262), (575, 150)], SOL, 18)
    L.poli([(540, 140), (620, 118), (598, 198)], SOL)
    for k in range(3):
        L.moneda(190, 250 - k * 28, 56)
    L.linea([(150, 640), (660, 640)], BLANCO, 14)
    L.guardar("tema_numeros")


def equipo():
    L = Lienzo("#35336F")
    L.persona(215, 385, 1.15, CIAN, cabello="#5A3A22")
    L.persona(585, 385, 1.15, CORAL, piel="#C99266")
    L.persona(400, 355, 1.45, MENTA)
    L.estrella(400, 150, 54, SOL)
    L.estrella(255, 190, 30, SOL)
    L.estrella(545, 190, 30, SOL)
    L.caja(150, 590, 650, 616, BLANCO, 13)
    L.guardar("tema_equipo")


def ventaja():
    L = Lienzo("#173F5F")
    L.poli([(400, 140), (610, 220), (588, 465), (400, 660), (212, 465), (190, 220)], AZUL)
    L.poli([(400, 192), (562, 254), (545, 445), (400, 595), (255, 445), (238, 254)], CIAN)
    L.estrella(400, 370, 122, SOL)
    L.estrella(640, 150, 34, BLANCO, 4)
    L.estrella(150, 560, 28, BLANCO, 4)
    L.guardar("tema_ventaja")


def oferta():
    L = Lienzo("#3B2F63")
    L.circulo(360, 370, 205, BLANCO)
    L.sector(360, 370, 180, -90, 234, AZUL)
    L.sector(386, 350, 190, 234, 270, SOL)
    L.circulo(360, 370, 78, "#3B2F63")
    for k in range(5):
        L.moneda(590, 610 - k * 34, 62)
    for k in range(3):
        L.moneda(470, 622 - k * 34, 52)
    L.guardar("tema_oferta")


def votacion():
    L = Lienzo("#14385C")
    L.caja(225, 400, 575, 650, AZUL, 28)
    L.caja(200, 380, 600, 440, CIAN, 20)
    L.caja(300, 398, 500, 420, NAVY, 10)
    L.caja(290, 170, 510, 408, BLANCO, 18)
    L.linea([(340, 285), (385, 332), (466, 232)], MENTA, 28)
    L.caja(300, 418, 500, 440, CIAN, 6)
    L.guardar("tema_votacion")


def trato():
    L = Lienzo("#0E5A47")
    for i in range(28):
        a, r = i * 2.39996, 190 + (i * 53) % 170
        x, y = 400 + r * math.cos(a), 400 + r * math.sin(a)
        L.caja(x - 11, y - 11, x + 11, y + 11, [SOL, CORAL, CIAN, BLANCO][i % 4], 5)
    L.circulo(400, 400, 195, BLANCO)
    L.circulo(400, 400, 165, MENTA)
    L.linea([(312, 405), (376, 470), (500, 332)], BLANCO, 46)
    L.guardar("tema_trato")


def sin_trato():
    L = Lienzo("#4A2438")
    L.circulo(400, 400, 195, BLANCO)
    L.circulo(400, 400, 165, CORAL)
    L.linea([(325, 325), (475, 475)], BLANCO, 46)
    L.linea([(475, 325), (325, 475)], BLANCO, 46)
    L.guardar("tema_sin_trato")


if __name__ == "__main__":
    todas = [sustentabilidad_y_energia, alimentos_y_bebidas, tecnologia, consumo_y_moda, salud, educacion,
             servicios_profesionales, mercado, numeros, equipo, ventaja, oferta, votacion, trato, sin_trato]
    for f in todas:
        f()
    hojas = sorted(SALIDA.glob("*.png"))
    mosaico = Image.new("RGB", (5 * 320, 3 * 320), "#05070B")
    for i, p in enumerate(hojas):
        m = Image.open(p).resize((320, 320), Image.LANCZOS)
        mosaico.paste(m, ((i % 5) * 320, (i // 5) * 320), m)
    mosaico.save(Path(__file__).parent / "img" / "laminas_mosaico.png")
    print(len(hojas), "láminas")
