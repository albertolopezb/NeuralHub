"""Convierte las figuras PNG indicadas a docs/img/*.jpg."""
import sys
from pathlib import Path
from PIL import Image

AQUI = Path(__file__).resolve().parent
for nombre in sys.argv[1:]:
    im = Image.open(AQUI / f"{nombre}.png").convert("RGB")
    im.thumbnail((2200, 2200))
    im.save(AQUI.parent / "img" / f"{nombre}.jpg", quality=90)
