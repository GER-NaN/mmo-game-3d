# audio-subset

Copies the sounds the game uses from the sound library (`C:\game-sound` on the author's
machine) into `assets/audio/`, which is not in git. The list is the catalog,
`game/audio/sounds.json`: every file a sound id names, with each pack's licence and
readme beside it.

```
python tools/audio-subset/copy.py
python tools/audio-subset/copy.py --library D:/sounds
```

Run it from the repo root, then let Godot import the new files once
(`<godot console exe> --headless --path . --import`).

To add a sound: add its id and file to the catalog, run this again, and play the id
from the game (`AudioDirector.Play`). Levels in the catalog are placeholders until heard
in the game.
