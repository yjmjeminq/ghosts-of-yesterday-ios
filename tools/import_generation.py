"""Prepare selected generated CG and sprite PNGs for offline Unity import."""
import argparse
from pathlib import Path

from PIL import Image, ImageOps

RESOURCES = Path(__file__).resolve().parents[1] / "Assets/Game/Resources"


def import_asset(source, asset, sprite):
    with Image.open(source) as loaded:
        if sprite:
            image = loaded.convert("RGBA")
            alpha = image.getchannel("A")
            if alpha.getextrema()[0] != 0 or alpha.getextrema()[1] < 250:
                raise ValueError(f"{source} is missing a usable transparent alpha channel")
            bounds = alpha.point(lambda value: 255 if value >= 40 else 0).getbbox()
            if bounds is None:
                raise ValueError(f"{source} contains no sprite")
            image = image.crop(bounds)
            image.thumbnail((1400, 1400), Image.Resampling.LANCZOS)
            target = RESOURCES / "Sprites" / f"{asset}.png"
            target.parent.mkdir(exist_ok=True)
            image.save(target, optimize=True)
        else:
            image = ImageOps.fit(loaded.convert("RGB"), (1600, 900), Image.Resampling.LANCZOS)
            target = RESOURCES / "Art" / f"{asset}.jpg"
            image.save(target, quality=94, subsampling=0, optimize=True)
    print(f"{target.name}: {image.width} x {image.height}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("asset")
    parser.add_argument("--sprite", action="store_true")
    args = parser.parse_args()
    import_asset(args.source, args.asset, args.sprite)
