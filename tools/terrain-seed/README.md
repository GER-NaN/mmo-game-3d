# terrain-seed

Fills a zone's Terrain3D data (`game/zones/<zone>/terrain/`) with rolling hills from a
seed, flat around the entry, as a start to sculpt from in the editor. Running it again
**replaces** the zone's terrain, sculpting and painting included: use it to start a
zone, not on one that has been worked on by hand.

It also writes the shared ground resources in `game/terrain/` (the material and the two
textures: rock on slopes, grass on the flat) if they are missing.

```
godot --headless --path . -s tools/terrain-seed/seed.gd -- --zone meadows --size 3000 --seed 7 --height 40 --frequency 0.0015 --flat -1470,0
```

(`godot` is the .NET console build that `scripts/server-up.ps1` names.) It takes a few
seconds for a 3 km zone and quits by itself.

| Option | Default | What |
| --- | --- | --- |
| `--zone` | (required) | the zone id; the data goes in `game/zones/<zone>/terrain/` |
| `--size` | 1000 | metres across; rounded up to whole 256 m regions, centred on the origin |
| `--seed` | 1 | the noise seed: the same seed gives the same hills |
| `--height` | 30 | the highest the hills go, in metres |
| `--frequency` | 0.002 | noise features per metre: smaller is broader hills |
| `--octaves` | 4 | noise detail layers |
| `--flat` | 0,0 | x,z of the flat area round the entry (height 0) |
| `--flat-radius` | 30 | metres of flat ground round it |
| `--flat-blend` | 60 | metres over which it rises into the hills |
| `--terraces` | 0 | above 0: the height snaps to this many levels with steep steps between (cliffs) |
| `--step` | 0.15 | the width of each step, as a share of a level |

Two things Terrain3D does that the tool handles:

- An import is snapped to Terrain3D's 256 m region grid, so the heights are made to
  start on a region edge; otherwise the whole terrain lands shifted (it once put the
  flat entry 36 m from its marker, and players arrived under the ground).
- The auto shader (grass on the flat, rock on slopes) only applies where the control
  map's "auto" bit is set, so the tool sets it everywhere. Painting by hand clears it
  where painted.
