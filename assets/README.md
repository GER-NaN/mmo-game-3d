# Art assets

Everything in this folder except this file is kept out of git (see `.gitignore`).
On a new machine, copy the art in by hand before opening the project; the zone and
player scenes reference these files and fail to load without them.

## What goes here

From the KayKit Complete collection (CC0, Kay Lousberg, www.kaylousberg.com), the
Godot edition at `C:\game-art\3d\kaykit-godot`:

| Here | From the collection |
| --- | --- |
| `kaykit/License.txt` | `License.txt` |
| `kaykit/city_builder_bits/` | `city_builder_bits/` |
| `kaykit/furniture_bits/` | `furniture_bits/` |
| `kaykit/resource_bits/` | `resource_bits/` |
| `kaykit/character_animations/rig_medium/` | `character_animations/animations/rig_medium/` |
| `kaykit/characters/` | `mystery_monthly_series_5/10_protagonists/characters/*.glb`, and the townspeople: `mystery_monthly_series_5/11_hiker/characters/Hiker.glb`, `mystery_monthly_series_6/12_farmers/characters/Farmer_A.glb`, `mystery_monthly_series_4/02_driver/characters/Driver.glb` |

After copying, open the project in the Godot editor once (or run Godot with
`--headless --import`) so it imports them.

## Scale

The city pieces are miniatures (a building is 2 units wide, a car under 1), so the
prop scenes in `game/props/` scale them up; see `tools/kaykit-props/`. The scale is a
placeholder until it is looked at in the game.
