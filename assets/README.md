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
| `kaykit/character_animations/rig_large/` | `character_animations/animations/rig_large/`: `Rig_Large_General.glb`, `Rig_Large_MovementBasic.glb`, `Rig_Large_Simulation.glb` |
| `kaykit/characters/` | every character model in the collection, the players' bases (`src/Rules/Players/Looks.cs`): each `*/characters/*.glb` that is not a `Rig_*` animation file, 64 in all, flat in one folder. From Git Bash in the collection's folder: `find . -path "*/characters/*.glb" ! -name "Rig_*" -exec cp {} <this repo>/assets/kaykit/characters/ \;` |

From Tiny Treats Collection 1 (CC0, Isa Lousberg, www.isalousberg.com), at
`C:\game-art\3d\Tiny_Treats_Collection_1_1.0`:

| Here | From the collection |
| --- | --- |
| `tinytreats/License.txt` | `License.txt` |
| `tinytreats/house_plants/` | `House Plants/Assets/gltf/` (every file) |

From Ozea Studio's Ultimate Sci-Fi Asset Library (use in games is allowed; the
files may not be shared or resold, so they stay out of git), at
`C:\game-art\3d\Ozea_Studio_Ultimate_SciFi_Asset_Library`:

| Here | From the library |
| --- | --- |
| `ozea/License.txt` | `Pack_SciFi_K_001_V1.0/04_DOCS/LICENSE.txt` |
| `ozea/drones/` | `Pack_SciFi_K_001_V1.0/02_EXPORT/FBX/` (every file: six drones; the town's drone is `SM_Drone_Basic`) |

Ground textures for Terrain3D, from ambientCG (CC0), copied from the Terrain3D v1.0.2
demo (`demo/assets/textures/`):

| Here | From |
| --- | --- |
| `terrain/asset_licenses.txt` | the demo's licence note |
| `terrain/ground037_alb_ht.png`, `ground037_nrm_rgh.png` | Ground037 (grass) |
| `terrain/rock023_alb_ht.png`, `rock023_nrm_rgh.png` | Rock023 (cliff) |

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
