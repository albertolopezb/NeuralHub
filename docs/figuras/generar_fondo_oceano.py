"""Fondo pintado del océano (detrás del ventanal del estudio) y textura de burbuja. Original, hecho con Pillow."""
import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

RES = Path(__file__).resolve().parents[2] / "unity" / "NeuralHubUnity" / "Assets" / "Resources" / "Escenario"
W, H = 3072, 1024
random.seed(8)


def mezcla(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def fondo():
    paradas = [(0.0, (120, 224, 236)), (0.28, (46, 168, 208)), (0.62, (18, 96, 150)), (1.0, (8, 42, 84))]
    col = Image.new("RGB", (1, H))
    for y in range(H):
        t = y / (H - 1)
        for (a, ca), (b, cb) in zip(paradas, paradas[1:]):
            if a <= t <= b:
                col.putpixel((0, y), mezcla(ca, cb, (t - a) / (b - a)))
    im = col.resize((W, H)).convert("RGBA")

    rayos = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(rayos)
    for _ in range(26):
        x = random.randint(-200, W)
        ancho = random.randint(30, 130)
        caida = random.randint(120, 380)
        d.polygon([(x, -20), (x + ancho, -20), (x + ancho * 2 + caida, H * 0.86), (x + caida, H * 0.86)],
                  fill=(210, 255, 250, random.randint(14, 36)))
    im.alpha_composite(rayos.filter(ImageFilter.GaussianBlur(18)))

    def capa(color, base, alto, desenfoque, corales):
        c = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        d = ImageDraw.Draw(c)
        pts = [(0, H)]
        fase = random.random() * 6
        for x in range(0, W + 40, 40):
            pts.append((x, base - alto * (0.55 * math.sin(x / 310 + fase) + 0.45 * math.sin(x / 127 + fase * 2))))
        pts.append((W, H))
        d.polygon(pts, fill=color)

        def rama(x, y, ang, largo, g, nivel):
            x2, y2 = x + largo * math.sin(ang), y - largo * math.cos(ang)
            d.line([(x, y), (x2, y2)], fill=color, width=g)
            d.ellipse([x2 - g / 2, y2 - g / 2, x2 + g / 2, y2 + g / 2], fill=color)
            if nivel > 0:
                for da in (-0.55, 0.5):
                    rama(x2, y2, ang + da * random.uniform(0.6, 1.2), largo * 0.72, max(5, int(g * 0.72)), nivel - 1)

        for _ in range(corales):
            x = random.randint(40, W - 40)
            rama(x, base + 24, random.uniform(-0.2, 0.2), random.randint(45, 90), random.randint(12, 22), 3)
        im.alpha_composite(c.filter(ImageFilter.GaussianBlur(desenfoque)))

    capa((26, 108, 150, 150), 800, 55, 9, 14)
    capa((14, 72, 116, 215), 880, 45, 5, 16)
    capa((7, 40, 76, 255), 960, 34, 2, 12)

    burbujas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(burbujas)
    for _ in range(190):
        x, y, r = random.randint(0, W), random.randint(0, H - 120), random.choice([2, 3, 3, 4, 6, 9])
        d.ellipse([x - r, y - r, x + r, y + r], outline=(220, 250, 255, random.randint(50, 130)), width=2)
    im.alpha_composite(burbujas)
    im.convert("RGB").save(RES / "oceano.png")


def burbuja():
    n = 256
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse([12, 12, n - 12, n - 12], fill=(200, 240, 255, 38), outline=(235, 252, 255, 200), width=10)
    d.ellipse([62, 52, 108, 96], fill=(255, 255, 255, 215))
    d.arc([50, 50, n - 44, n - 44], 20, 75, fill=(255, 255, 255, 150), width=10)
    im.resize((64, 64), Image.LANCZOS).save(RES / "burbuja.png")


if __name__ == "__main__":
    RES.mkdir(parents=True, exist_ok=True)
    fondo()
    burbuja()
    print("ok")
