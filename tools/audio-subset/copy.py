"""Copies the sounds the game uses from the sound library into assets/audio/.

The catalog, game/audio/sounds.json, names every file the game plays by its path in the
library. This copies exactly those, keeping the paths, plus each pack's licence and
readme files, so the credits travel with the sounds. assets/ is not in git.

    python tools/audio-subset/copy.py                      library at C:/game-sound
    python tools/audio-subset/copy.py --library D:/sounds

Then let Godot import them once:

    <godot console exe> --headless --path . --import
"""

import argparse
import json
import os
import shutil

CATALOG = "game/audio/sounds.json"
OUT = "assets/audio"

# The licence and readme files a pack keeps beside its sounds.
PACK_NOTES = ("LICENSE", "LICENSE.txt", "License.txt", "README", "README.txt", "README.md")


def pack_of(path):
    parts = path.split("/")
    return "/".join(parts[:2])


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--library", default="C:/game-sound")
    args = parser.parse_args()

    with open(CATALOG, encoding="utf-8") as f:
        sounds = json.load(f)["sounds"]

    files = set()

    for sound in sounds.values():
        files.update(sound["files"])

    missing = 0
    packs = set()

    for path in sorted(files):
        source = os.path.join(args.library, path)

        if not os.path.exists(source):
            print("missing:", source)
            missing += 1
            continue

        target = os.path.join(OUT, path)
        os.makedirs(os.path.dirname(target), exist_ok=True)
        shutil.copy2(source, target)
        packs.add(pack_of(path))

    for pack in sorted(packs):
        for name in PACK_NOTES:
            source = os.path.join(args.library, pack, name)

            if os.path.exists(source):
                shutil.copy2(source, os.path.join(OUT, pack, name))

    print("copied", len(files) - missing, "sounds from", len(packs), "packs to", OUT, "; missing", missing)


if __name__ == "__main__":
    main()
