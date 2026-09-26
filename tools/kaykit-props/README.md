# kaykit-props

Turns KayKit models into game-ready prop scenes in `game/props/<pack>/`: the model at
the game's scale, with a collision box on a `StaticBody3D`, sized from the model's own
bounds. Zone scenes place props, never raw models, so collision and scale are decided
once, here.

```
python tools/kaykit-props/make_props.py
```

Run it from the repo root, with the art in `assets/kaykit/` (see `assets/README.md`).

- To add a model, add a line to `PROPS` in the script and run it again.
- Collision kinds: `box` (the model's bounds), `block` (a box that also stops the chase
  camera, for buildings), `trunk` (a narrow post, for trees and
  lights, so a player walks under the branches), `flat` (roads and paving: no collision,
  and scaled only sideways, so they stay a thin surface), `none` (bushes).
- `CITY_SCALE` and `FURNITURE_SCALE` are placeholders until seen in the game. Changing
  one and re-running rescales every prop, and every zone with it.

The prop scenes are committed; the art they point at is not.
