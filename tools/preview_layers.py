"""Build an offline proof sheet using the Unity layout coordinates. Does not start Unity."""
import json
from pathlib import Path

from PIL import Image

from compile_story import BOOK, RESOURCES, ROOT

W, H = 1440, 810
SCENES = ("ep002-fall", "ep002-distance", "ep005-pizza")


def composition(scene):
    source = RESOURCES / "Art" / (scene["art"] + ".jpg")
    with Image.open(source) as background:
        panel = background.resize((W, H), Image.Resampling.LANCZOS).convert("RGBA")
    for layer in scene["layers"]:
        path = RESOURCES / "Sprites" / (layer["art"] + ".png")
        with Image.open(path) as art:
            height = layer["height"]
            width = round(art.width / art.height * height)
            x = round(layer["x"] - width / 2)
            y = layer["y"] - height
            if not (0 <= x <= W - width and 0 <= y <= H - height):
                raise ValueError(f"Clipped sprite in {scene['id']}")
            resized = art.resize((width, height), Image.Resampling.LANCZOS).convert("RGBA")
            if scene["art"] in ("balcony", "roof"):
                r, g, b, a = resized.split()
                r = r.point(lambda v: int(v * 0.78))
                g = g.point(lambda v: int(v * 0.80))
                b = b.point(lambda v: int(v * 0.90))
                resized = Image.merge("RGBA", (r, g, b, a))
            panel.alpha_composite(resized, (x, y))
    return panel


def main():
    scenes = {s["id"]: s for ep in json.loads(BOOK.read_text(encoding="utf8"))["episodes"]
              for s in ep["scenes"]}
    result = Image.new("RGB", (480 * len(SCENES), 270))
    for index, key in enumerate(SCENES):
        result.paste(composition(scenes[key]).resize((480, 270), Image.Resampling.LANCZOS).convert("RGB"),
                     (480 * index, 0))
    path = ROOT / "qa/RUNS/balcony-suit-composite.jpg"
    path.parent.mkdir(parents=True, exist_ok=True)
    result.save(path, quality=94)
    print(path)


if __name__ == "__main__":
    main()
