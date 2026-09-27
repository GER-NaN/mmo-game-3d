# Art pipeline

How a zone goes from voxel art to something the game runs. Three stages, and the game
reads only the last artifact.

```
Voxel pack  -->  Scene (Voxel Scene Maker)  -->  scene.json  -->  the game
(models)         (placements, layers,            (what the game
                  markers, properties)             reads)
```

The pipeline is built end to end: every scene is a zone of the same name, and both
server and client read the compiled scene for the bounds, the spawn and everything
standing in it. `game-data/world.json` names the zone new players start in. The marker kinds and rules below the
stages are still **planned**; names marked *(planned)* do not exist yet.

Layout is done in Voxel Scene Maker, a stand-alone general tool at
`C:\Users\geral\src\voxel-scene-maker`, not part of this repo. MagicaVoxel is for
making models only. An earlier plan laid zones out in MagicaVoxel's World editor and
compiled them; that is dropped.

## What belongs where

Four tools, each with one question it answers. When a feature is being placed, ask
which question it is part of, and it goes in that tool.

**MagicaVoxel answers: what is this thing?** Shape, colours, materials (which colours
glow), and, for anything that moves, which voxels are which part and where the joints
are, as named objects. Its output is the `.vox` file, and everything in it is true of
the model wherever it stands: a lamp glows in every scene, a dog's legs are its legs.
It composes nothing: a street is not a model, it is placements. A multi-object Magica
"scene" saved as one file is one rigid thing to the game, with one footprint.

**Voxel Scene Maker answers: where does it stand, and what is it for here?** Position,
rotation, layer, a name, and free properties whose meaning the tool does not know:
`sway = 0.3`, `effect = fire`, `light`, `blocks`, `door.library`. Its output is the
`.scene.json`, and everything in it is true of one placement in one scene. It edits no
voxels and knows no game words; the words come from the props in
`art/mmo-game.project.json`, which are the game's vocabulary, edited in the tool. It is generic on purpose, so it can be used for
another game without a change.

**The compiler answers: what does the game need to know about this?** It reads the
project, the scenes and the models, and derives: footprints from the models, checks
against the game's conventions, the set of models actually used, a version stamp. Its
output is the package, and the rule is that it derives what can be derived so that no
artist and no scene has to say what a program can measure. Anything that is a check
("this zone has no spawn", "this model has no head part") belongs here, where it is
reported once at compile time rather than discovered in the game.

**The game answers: what does that mean on screen and in play?** The client reads the
package and decides how `sway` moves, what `fire` looks like, how a body walks. The
server reads the same package and decides what blocks and where you appear. Neither
reads the art or the scenes. The numbers behind a word (`fire` is 24 embers a second
that live half a second) are the game's, in its code or its own data files, never in
the scene, because the scene says what a thing is and the game says what that means.

Two places this is not yet true, both in the TODO: the client still decides glow and
lights by model name and colour brightness rather than by material and property, and
the compiler still measures a footprint as the model's whole box rather than what
touches the ground.

## The stages

| Stage | You do | Tool | Produces | Status |
| --- | --- | --- | --- | --- |
| 1. Get voxel assets | Buy or make models. One file per model. Check the licence allows edits. | MagicaVoxel, packs | `art/models/**/*.vox` | built |
| 2. Lay out a scene | Drag models onto the ground. Name what the rules must point at. Set properties. | Voxel Scene Maker, project at `art/mmo-game.project.json` | `art/scenes/<name>.scene.json` | built |
| 3. Compile it | Every build runs the compiler (`build/MapPackage.targets`): it checks every scene against the game's rules, measures the models it draws, and writes the package, stamped with the compile time. Errors name the scene and the placement. Then rebuild the server and the client: both bake the package in, and the server refuses a client whose stamp is not its own. | `Compiler` | `Package/package.json`, `Package/zones/<n>.zone.json` and `Package/models/*.vox` | built |
| 4. Load it | Every compiled scene is a zone named after it; `world.json` names the start. `ZoneLoader` reads the compiled scene for the bounds, the player spawn and the dressing; the server enforces the footprints, the client draws the placements from the package's models. Neither reads the art or the scenes. | `src/Core/Zones/ZoneLoader.cs` | a zone the server runs and the client draws | built |

The package is what the game reads, and it is committed to git too, so the server's
container and the client build from the repository alone. A zone file is the scene in
the scene's own words, with editor-only flags dropped and one thing added: the width,
depth and height of every model it draws, because the server has no models to
measure. A compiled scene is named after its scene file, and that name is the zone's:
one scene, one zone. Only the start zone must have a player spawn; the compiler fails
the build when it does not.

The scene file is committed to git. It is the tool's format, documented in the tool's
README: placements with a model name, position, quarter-turn rotation, layer, name and
free properties. The game's reader decides what `door.library` means; the tool does not
know.

What the game asks of a scene it uses as a zone, today: a layer named `spawns` that is
not drawable, with exactly one placement named `player` on it. Its position is the
spawn and its rotation the facing. The compiler warns when these are missing, because
a scene need not be a zone; the loader refuses the zone, because a zone needs them.
Both read the words from `src/Core/Zones/Scenes/ZoneConventions.cs`. Everything drawn
blocks when it is at least 0.6 m tall, whatever it is called: a placed person is in the
way like a post (the `chr_*` exception went on 2026-09-24).

A free property `sway`, a number of metres, makes a model move in the wind: its top
swings that far and its base stays put. Set it once on the layer the trees and bushes
share (0.3 for a tree, 0.15 for a bush is a fair start), or on one placement to
override. It is a client effect only; footprints do not move.

A door to another zone is a placement with a `name` and two free properties,
`transition.scene` and `transition.name`, on any layer, drawn or not. Their presence is
the flag. The far door is the one it names in the scene it names, and the player
arrives on it; a door is two-way when each end names the other. Give the drawn marker
`effect=threshold` and it shimmers, brighter as a player nears. A door never blocks.
The compiler checks that every door leads to a scene the project has and a name that
scene has, and warns when one does not lead back. Built 2026-09-22; `zones.md` has
the crossing.

A placement with `interaction=chest` or `interaction=vendor` needs a `name` of its
own: the server keys what the chest rolls and what the vendor sells by it. The
compiler refuses one without a name or with a shared name. Built 2026-09-22.

## Rule one: what needs a name

**Anything the rules must point at needs a name the game's reader understands.** Two
flavours:

| Flavour | What it is | Where it sits | Named how | Example |
| --- | --- | --- | --- | --- |
| Marker | A point or an area. Invisible in the game. | a placement of a small marker model on a `markers` layer | `kind.name` | `door.library`, `spawn.player`, `terminal.gamingrig` |
| Named object | A visible part with behaviour. Real art that draws. | its own model, placed on a normal layer | `kind.name` | `sign.library`, `window.library.front` |

The ground is never drawn in the scene. The client draws it from `ground.*` placements
and the zone's default surface. The tool snaps every placement to the ground. The one
exception is `terrain.*`: sculpted height is art, and the height field comes from it.
The game is flat today, so a `terrain.*` object draws nothing and walks as flat ground
until the height field is built. Floors above floors (bridges, balconies) are out of
scope; upper storeys are separate zones like any interior.

Everything else is decoration: unnamed, or named anything that is not a known kind.
The reader leaves it alone. A whole street can be decoration. One sign in it can be
a named object. One doorway in it can be a marker.

The test when placing something: *could the game ever need to ask about this one
thing?* If yes, name it. If no, do not.

## Marker kinds *(planned)*

| Kind | Marks | Position means | Facing means | Rules file adds |
| --- | --- | --- | --- | --- |
| `spawn.player` | where a new player appears | the point | which way they face | which spawn is default, if several |
| `door.<name>` | *(replaced)* a zone transition is a named placement with `transition.scene` and `transition.name`; see above | | | |
| `terminal.<name>` | a fixed terminal | the point | which way to face it | enabled, kind of terminal |
| `place.<name>` | a named spot for NPC routines | the point | - | nothing; NPC routines use the name |
| `spawn.items` | an area where items scatter | the box the object covers | - | how many, which items |
| `ground.<surface>` | a surface area: road, grass, stone | the box the object covers | - | the zone's default surface, for what is not covered |
| `light.<name>` | a point light | the point | - | colour, on/off rule |
| `terrain.<name>` | sculpted ground with height: a hill, a slope, a raised plaza | the voxels themselves, drawn | - | nothing; the reader samples the top surface into a height field |

A marker's position is the centre of its object. Facing comes from the object's
rotation, so it is one of four directions.

## What the game's reader checks *(planned)*

- Exactly one default player spawn, inside the bounds, not inside a footprint.
- *(built)* Every transition leads to a scene the project has and a transition that
  scene has; `src/Compiler/README.md` has the codes.
- Every name the rules file uses exists in the scene.
- A `kind.name` with an unknown kind is reported, not silently treated as decoration.

A failure names the placement by id and name. The tool may grow a per-project schema that checks the same things while you edit.

## What the game takes from a scene *(planned)*

One scene file per zone. The reader turns placements, layers, names and properties
into these.

| Section | Holds | Server reads | Client reads |
| --- | --- | --- | --- |
| `id`, `kind`, `bounds` | exterior or interior, the playable box | yes | yes |
| `placements` | model, position, rotation, blocks | footprints only | draws them |
| `markers` | kind, name, position, facing | spawn, doors, terminals, places | door and terminal prompts |
| `regions` | kind, name, box | item scatter | - |
| `links` | door to zone and arrival marker | zone transitions | - |
| `npcs` | routines and lines | ticks them | names, models, text |
| `rules` | per-zone properties | as needed | lighting profile |

## Glow

Glow is art. Paint the voxels with a colour whose material is **Emit** and it glows.
No marker, no rule, no compile. The reader takes the flag from the file, the mesher
bakes it, the shader blooms it.

Glow with a rule splits in two:

- The model still says what *can* glow.
- A rule says *whether* it glows now. Time of day: the client decides alone. Server
  state such as a terminal being enabled or a district being powered: the server sends
  it, every light with that rule follows.

For a rule to point at it, the glowing part must be its own named object. A sign that
must go dark on its own is `sign.library`, not part of the building.

One palette per `.vox` file. A colour that glows, glows everywhere that file uses it.
Give a glowing part its own palette slot.

## Where the models come from

| Option | Scene object | Game placement | Palette | Use for |
| --- | --- | --- | --- | --- |
| Prefab by name | named after a pack model, e.g. `obj_house1` | `model: "house1"`, position, rotation | the model's own | everything placed from a pack |
| Baked | any other object with voxels | a zone-local model exported from the scene | the scene's, 255 colours for the whole zone | terrain, one-off custom pieces |

**Open decision:** which of these the reader does first, or both. Prefab by name keeps
the scene a layout tool, the palette per model, and edits in the model's own file. Baked
gives full freedom to sculpt a zone as one piece at the cost of one palette per zone.

## What the game's .vox reader supports today

`src/Voxels/VoxFile.cs` reads `SIZE`, `XYZI`, `RGBA` and `MATL`. It does **not** read the
scene graph (`nTRN`, `nGRP`, `nSHP`) or layers (`LAYR`). A multi-object scene loads as a
flat list of models with no positions, names or layers. That no longer matters for
layout: the scene file carries positions and names, and the game only meshes single
models.

## Scale and axes

- 8 voxels per metre, one density for the whole game.
- A MagicaVoxel object is at most 256 voxels per axis, so 32 metres. A zone is many
  objects.
- MagicaVoxel is z-up with y as depth. The mesher maps world X = x, world Y = z, and
  world Z runs the other way along y. A model's front faces world +Z. Whether the pack
  models were drawn front-to-that-side is unverified.
