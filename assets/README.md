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
| `kaykit/rpg_tools_bits/` | `rpg_tools_bits/` (tools held in the hand while working) |
| `kaykit/character_animations/rig_medium/` | `character_animations/animations/rig_medium/` |
| `kaykit/characters/` | `mystery_monthly_series_5/10_protagonists/characters/*.glb`, and the townspeople: `mystery_monthly_series_5/11_hiker/characters/Hiker.glb`, `mystery_monthly_series_6/12_farmers/characters/Farmer_A.glb`, `mystery_monthly_series_4/02_driver/characters/Driver.glb` |

From Tiny Treats Collection 1 (CC0, Isa Lousberg, www.isalousberg.com), at
`C:\game-art\3d\Tiny_Treats_Collection_1_1.0`:

| Here | From the collection |
| --- | --- |
| `tinytreats/License.txt` | `License.txt` |
| `tinytreats/house_plants/` | `House Plants/Assets/gltf/` (every file) |

Sounds, from the sound library at `C:\game-sound`, go in `audio/` with the same paths.
Only the files the game uses are copied, by `tools/audio-subset/copy.py`, which reads
the catalog (`game/audio/sounds.json`) and brings each pack's licence along. The packs
and what their licences ask:

| Pack | Licence | Credit |
| --- | --- | --- |
| `music/abstraction-troubadeck-loops` | CC0 | "Abstraction" (appreciated, not required) |
| `ambience/helton-yan-surreal-drones` | CC BY 4.0 | Helton Yan (required) |
| `ui/nathan-gibson-universal-ui` | CC BY 4.0 | Nathan Gibson (required) |
| `ui/bleeoop-interface-bleeps` | Bleeoop EULA | royalty free in games; the raw files must not be passed on |
| `sfx/jdwasabi-8bit-16bit` | free for games | jdwasabi (asked for) |
| `sfx/nox-sound-essentials` | CC0 | Nox Sound (appreciated) |
| `voice/super-dialogue-audio-pack` | CC BY 4.0 | Dillon Becker (required) |

The CC BY credits are shown in the game: Credits, on the main menu.

After copying, open the project in the Godot editor once (or run Godot with
`--headless --import`) so it imports them.

## Scale

The city pieces are miniatures (a building is 2 units wide, a car under 1), so the
prop scenes in `game/props/` scale them up; see `tools/kaykit-props/`. The scale is a
placeholder until it is looked at in the game.
