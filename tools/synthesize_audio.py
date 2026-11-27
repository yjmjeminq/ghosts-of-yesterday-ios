"""Render original loopable score cues and a balcony impact to local PCM."""
import math
import random
import struct
import wave
from pathlib import Path

AUDIO = Path(__file__).resolve().parents[1] / "Assets/Game/Resources/Audio"
RATE = 22050
CHORDS = {
    "city": [(45, 57, 60, 64), (41, 53, 57, 60), (48, 55, 59, 64), (43, 55, 58, 62)],
    "past": [(40, 52, 55, 59), (40, 52, 54, 57), (43, 55, 58, 62), (42, 54, 57, 60)],
    "day": [(48, 60, 64, 67), (45, 57, 60, 64), (41, 53, 57, 64), (43, 55, 59, 62)],
    "rain": [(45, 57, 60, 67), (43, 55, 59, 62), (41, 53, 57, 64), (40, 52, 55, 62)],
}


def write(name, frames):
    with wave.open(str(AUDIO / f"{name}.wav"), "wb") as stream:
        stream.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        stream.writeframes(b"".join(struct.pack("<h", round(sample * 32767)) for sample in frames))


def tone(note):
    return 2 * math.pi * 440 * 2 ** ((note - 69) / 12)


def loop(chords):
    for chord in chords:
        freqs = [tone(note) for note in chord]
        for i in range(RATE * 4):
            t = i / RATE
            veil = min(1, t * 12, (4 - t) * 3)
            bass = math.sin(freqs[0] * t) * .089
            pads = sum(math.sin(f * t) + .18 * math.sin(2 * f * t) for f in freqs[1:]) * .032
            pulse = t % 1
            bell = math.sin(freqs[1 + int(t) % 3] * 2 * pulse) * min(1, pulse * 80) * math.exp(-4 * pulse) * .05
            yield (bass + pads + bell) * veil


def impact():
    rng = random.Random(6502)
    for i in range(RATE // 2):
        t = i / RATE
        yield (math.sin(tone(33) * t) * .4 + rng.uniform(-.17, .17)) * math.exp(-19 * t) * min(1, t * 220)


if __name__ == "__main__":
    for name, chords in CHORDS.items():
        write(name, loop(chords))
    write("impact", impact())
