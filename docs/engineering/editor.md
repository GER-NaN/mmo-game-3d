# Working in the Godot editor

What to know before editing the game by hand in the editor. The code rules for the same
things are in making-changes.md.

## Opening the project

1. Use the Godot 4.7.2 **.NET** build (the one `scripts/server-up.ps1` names). The plain
   build cannot run C# scripts.
2. Open `project.godot` at the repo root.
3. Click **Build** (the hammer, top right) first, and again after any C# change. Scenes
   run the built assembly in `.godot/mono`; without a build they show script errors.

The main scene is `game/Main.tscn`. It is nearly empty on purpose: the code builds the
server or the client under it at start.

## Running from the editor

- **F5 runs one client**, which cannot play without a server. Start the server first
  with `scripts/server-up.ps1`, then press F5.
- Or run both from the editor: **Debug > Customize Run Instances**, two instances, and
  give one the arguments `--headless -- --server`.
- The server reads the zone scenes when it starts. **After editing a zone, restart the
  server** (`scripts/server-stop.ps1`, then `server-up.ps1`), or the server's collision
  and doors are the old ones while the client draws the new ones.
- While the game runs, the **Remote** tab of the Scene dock shows the live tree. A zone
  is at `World/<zone id>/Zone`; players are under its `Players`, named by peer id.

## Zones

Each zone is one scene: `game/zones/<id>/<id>.tscn`. `greenhouse.tscn` is the smallest
complete one; open it to see the nodes every zone needs (`Interactables`, `Doors`,
`Arrivals`, `Spawn`, `Players` + `PlayerSpawner`, `Items` + `ItemSpawner`). Renaming or
removing those breaks the zone. The root's settings (footstep surface, map size,
ground items) are in the Inspector when the root is selected.

A new zone also needs a line of code (`src/Rules/World/ZoneIds.cs`); see
making-changes.md.

## Placing things

- **Drag prop scenes from `game/props/<pack>/`**, not models from `assets/`. A prop
  scene carries the model at the game's scale and its collision. A raw `.gltf` has no
  collision: players walk through it, and the server does not know it is there.
- A prop that is missing is made with `tools/kaykit-props` (its README), not by hand.
- Something you build yourself that should block players (a wall, a box) needs a
  `StaticBody3D` with a collision shape, on collision layer 1 (World). Tick layer 3 as
  well if it should also stop the camera (walls, buildings). The layer numbers are in
  `game/PhysicsLayers.cs`.

## Placing things that work

A thing the player uses with F (a terminal, a shop counter, a bench that breaks) is a
scene with a script. The game knows it by two things only:

- **Where it sits:** under the zone's `Interactables` node. The server finds every
  thing there and routes a use by the thing's class (`ServerInteractions.Use`). The
  client offers the nearest one in reach.
- **Its node name:** the server and the client refer to it by name, so each needs its
  own name in the zone (`LibraryTerminal`, `StreetKiosk`, `Bench1`).

So to place one: drag its scene (below) onto `Interactables`, move it, give it a name,
and set its fields in the Inspector. No code. A kind of thing that does not exist yet is
code: see example-tree-chopping.md.

| Thing | Scene | Set in the Inspector |
| --- | --- | --- |
| Terminal | `game/terminals/Terminal.tscn` | `TypeId` (0 public; the rest in `src/Rules/Terminals/TerminalType.cs`) |
| Shop counter | `game/vendors/Vendor.tscn` | `ShopId` (a shop in `src/Rules/Shops/`), `KeeperName` |
| Workbench | `game/workbenches/Workbench.tscn` | nothing |
| Recycler | `game/town/Recycler.tscn` | nothing |
| Chest | `game/chests/Chest.tscn` | `ChestName` |
| Taxi stand | `game/taxis/TaxiStand.tscn` | nothing |
| Junction box (the street lights' repair) | `game/town/JunctionBox.tscn` | nothing |
| Tag wall | `game/subway/SubwayWall.tscn` | nothing |
| Potting table | `game/gardening/PottingTable.tscn` | nothing |
| Someone at the college | `game/college/CollegePerson.tscn` | `Role`, `PersonName`, `ModelPath` |
| Townsperson | `game/town/Townsperson.tscn` | `Route` (a `Path3D` loop in the zone), `StartAt`, `PersonName`, `ModelPath` |

**Something that breaks and is repaired** (a bench, a dumpster, a hydrant): place
`game/town/Fixable.tscn`, set `FixableName` (what the prompt calls it), and tick
`Electrical` if it is wired (fixing it is then Electrical repair, not Field repair).
Then drag the prop that is its look (from `game/props/`) onto it and name that child
`Prop`. Any prop becomes breakable this way.

**A security camera** is a fixable with `Electrical` ticked and the camera prop as its
`Prop`, plus one more child: a `Node3D` named `Lens`, at the lens, with the script
`game/town/CameraMount.cs`, and `Target` set to the point it looks at (zone
coordinates). The town cameras app shows every `CameraMount` in the zone; see `CameraNE`
in `town.tscn`.

**A street lamp** is not an interactable: place `game/zones/props/StreetLamp.tscn`
anywhere (the town keeps them under `Lamps`). It lights when it is dark and the town has
power. Lamps do not break one by one: the town's lights go out together, and the repair
job (the junction box) brings them back. A lamp that breaks on its own would be a
fixable with the lamp as its `Prop`, and going dark by itself would be new code.

## Sculpting ground (Terrain3D)

Outdoor ground is a **Terrain3D** node named `Terrain` in the zone (the add-on is in
`addons/terrain_3d`, on in Project Settings > Plugins). `game/zones/meadows/meadows.tscn`
is the example.

1. Open the zone and select the `Terrain` node. The Terrain3D toolbar appears on the
   left of the viewport, and its settings along the bottom.
2. Sculpt: raise, lower, smooth, flatten, with brush size and strength at the bottom.
   Paint: pick a texture (rock is 0, grass is 1) and paint; unpainted ground keeps the
   automatic grass-on-flat, rock-on-slopes.
3. Save the scene. The heights live in `game/zones/<id>/terrain/` (one file per 256 m
   region), not in the `.tscn`: commit those files too.
4. Restart the server: it reads the terrain at start.

Keep the ground under doors and arrival markers where they are: the server puts an
arriving player on the ground under the marker, but a door's trigger floating in the air
or sunk in a hill will not be walked into. Players cannot climb slopes steeper than
about 45 degrees.

To start a new terrain zone from generated hills rather than flat ground, run
`tools/terrain-seed` once (its README), then sculpt. Running it again replaces the
sculpting.

The official Terrain3D documentation (terrain3d.readthedocs.io) has the full tool list.

## Doors between zones

1. In the zone you leave from, add an instance of `game/zones/Door.tscn` where the way
   out is. Stand it in front of walls, not inside them.
2. In the Inspector set `TargetZone` (the other zone's id, such as `greenhouse`) and
   `TargetArrival` (a marker name there).
3. In the other zone, add a `Marker3D` with that name under `Arrivals`, a few metres
   inside, facing the way players should face when they arrive.
4. For a way back, do the same in the other direction.

A door that names a missing zone or marker does nothing when walked into, and the
server's window prints an error naming it.

## What the editor does not show

Everything made while the game runs: players, ground items, drones, taxi cars, garden
plants, and the whole terminal and phone UI. To see those, run the game, or use the
`--screenshot` options in running.md.

## Files the editor makes

- `.uid` files beside new scripts and resources: commit them.
- `.import` files beside new art under `game/`: commit them. Art under `assets/` is not
  in git at all (assets/README.md says where it comes from). `.godot/` is not in git.
- Saving a scene can reorder or reformat lines in its `.tscn`. That diff is normal.
