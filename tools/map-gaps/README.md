# map-gaps

Finds narrow gaps between the box colliders in a zone scene: gaps a player can walk
into and get wedged in for good. A player's capsule is 1 m wide, so a gap about that
wide holds them; the bots found eight such gaps in Old Town (see
`docs/engineering/bot-testing-findings.md`, "Players wedge into the gaps").

```
python tools/map-gaps/map_gaps.py game/zones/town/town.tscn
python tools/map-gaps/map_gaps.py "game/zones/*/*.tscn"
python tools/map-gaps/map_gaps.py game/zones/town/town.tscn --max 2.5
```

It reads the scene file, not the running game. It counts:

- instanced scenes whose root is a `StaticBody3D`, with their box shapes (the KayKit
  buildings and props); a door's `Area3D` stops no one and is left out;
- `StaticBody3D` nodes in the zone scene itself with a box shape (such as the
  `GapFills` in `town.tscn`), which also count as filling a gap.

It reports each gap between two boxes that is at least `--min` wide (0.9 m; narrower,
no one gets in), under `--max` (2 m), and at least `--depth` long (1 m), with the
boxes' names and where it is, and exits 1 when there is one. Boxes lower than
`--min-height` (2.5 m) are left out, since a player steps or jumps over them. Rotations
other than quarter turns are treated roughly (the box's footprint on the axes), and
shapes other than boxes are not counted.

The fix for a gap is to close it (move the buildings together, or fill it with an
invisible box) or to widen it past 2 m, so a player can turn in it.
