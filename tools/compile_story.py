"""Compile the authored Markdown scripts into the runtime's local JSON book."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RESOURCES = ROOT / "Assets/Game/Resources"
BOOK = RESOURCES / "Story/book.json"


def need_asset(directory, key):
    suffixes = {"Art": (".jpg", ".png"), "Sprites": (".png",), "Audio": (".wav",)}[directory]
    if not re.fullmatch(r"[a-z][a-z0-9_]*", key) or not any(
        (RESOURCES / directory / (key + suffix)).is_file() for suffix in suffixes
    ):
        raise ValueError(f"Unresolved {directory} asset {key!r}")


def parse_layers(source):
    layers = []
    for declaration in source.split(";"):
        key, coordinates = declaration.split("@")
        x, y, height = map(int, coordinates.split(","))
        need_asset("Sprites", key)
        if not (0 < x < 1440 and 0 < height <= y <= 810):
            raise ValueError("Character layer uses an invalid foot anchor or height")
        layers.append(dict(art=key, x=x, y=y, height=height))
    return layers


def read_episode(path):
    episode_id = path.stem
    episode = None
    scene = None
    keys = set()
    cut_ids = []
    for number, raw in enumerate(path.read_text(encoding="utf8").splitlines(), 1):
        line = raw.strip()
        cut = re.fullmatch(r"<!-- cut: ([0-9]{3}) -->", line)
        if not line or (line.startswith("<!--") and not cut):
            continue
        try:
            if cut:
                key = f"{scene['id']}-{cut.group(1)}"
                if key in keys:
                    raise ValueError("Duplicate cut line")
                keys.add(key)
                cut_ids.append(key)
            elif line.startswith("# "):
                if episode or not line[2:].strip():
                    raise ValueError("Expected one episode title")
                episode = dict(id=episode_id, title=line[2:], scenes=[])
            elif line.startswith("## "):
                suffix, place, time, pov, art, music, gallery, *layers = map(str.strip, line[3:].split("|"))
                if len(layers) > 1:
                    raise ValueError("Expected at most one character layer field")
                if not re.fullmatch(r"[a-z0-9_]+", suffix) or not (place and time and pov):
                    raise ValueError("Invalid scene metadata")
                need_asset("Art", art)
                need_asset("Audio", music)
                scene = dict(id=f"{episode_id}-{suffix}", place=place, time=time, pov=pov,
                             art=art, music=music, gallery=gallery, lines=[],
                             layers=parse_layers(layers[0]) if layers and layers[0] else [])
                episode["scenes"].append(scene)
                if scene["id"] in keys:
                    raise ValueError("Duplicate scene")
                keys.add(scene["id"])
            else:
                suffix, speaker, text, *effect = map(str.strip, line.split("|"))
                if not re.fullmatch(r"[0-9]{3}", suffix) or not text or len(text) > 85 or len(effect) > 1:
                    raise ValueError("Invalid beat ID, empty caption or caption exceeds 85 characters")
                sfx = effect[0] if effect else ""
                if sfx:
                    need_asset("Audio", sfx)
                key = f"{scene['id']}-{suffix}"
                if key in keys:
                    raise ValueError("Duplicate line")
                keys.add(key)
                beat = dict(id=key, speaker=speaker, text=text, sfx=sfx)
                if cut_ids:
                    beat["aliases"] = cut_ids
                    cut_ids = []
                scene["lines"].append(beat)
        except (ValueError, TypeError, KeyError, IndexError) as error:
            raise ValueError(f"{path.name}:{number}: {error}") from error
    if not episode or not episode["scenes"] or any(not s["lines"] for s in episode["scenes"]):
        raise ValueError(f"{path.name} has an empty or absent scene")
    if cut_ids:
        raise ValueError(f"{path.name} has deleted lines after the final playable beat")
    return episode


def main():
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true")
    check = parser.parse_args().check
    paths = sorted((ROOT / "story/episodes").glob("ep*.md"))
    if not paths or any(p.stem != f"ep{i:03}" for i, p in enumerate(paths, 1)):
        raise ValueError("Episode numbering must be contiguous from ep001")
    episodes = [read_episode(path) for path in paths]
    scenes = [scene for episode in episodes for scene in episode["scenes"]]
    galleries = {}
    for scene in scenes:
        name = scene["gallery"]
        if name in galleries:
            raise ValueError(f"Repeated gallery title {name!r}")
        if name:
            galleries[name] = scene["id"]
    data = json.dumps({"episodes": episodes}, ensure_ascii=False, indent=2) + "\n"
    if check:
        if BOOK.read_text(encoding="utf8") != data:
            raise ValueError("Runtime book differs from the authored scripts")
    else:
        BOOK.write_text(data, encoding="utf8", newline="\n")
    print(f"{len(episodes)} episodes, {len(scenes)} scenes, {sum(len(s['lines']) for s in scenes)} beats")


if __name__ == "__main__":
    main()
