# A 3D world to walk around in

**Date:** 2026-09-24
**Status:** All decisions answered 2026-09-25. Phases 1, 2 and 3 built the same day;
Phase 4 in part (spawn and door heights); Phase 5 not started. Parked
2026-09-25 morning for gameplay on metro-minis, taken up again the same day.
**Sources read:** docs/engineering/art-pipeline.md (the ground, `terrain.*`, floors
above floors, the marker kinds); docs/first-playable.md; docs/backlog.md;
docs/features/second-zone.md (F9, and the interiors decision it lists);
Core/Zones/Dressing; Core/Simulation/State/Player.cs; Core/Simulation/View/PlayerView.cs;
Client/Rendering/MotionSmoother.cs; Voxels.Rendering/DressingPlacements.cs;
Voxels.Rendering/FacetGround.cs; Voxels.Rendering/LightSettings.cs; the editor's
Scene.cs and TODO.md.
**Build from:** the phases below once the open decisions are answered. The
`/feature-design` session turns this plan into the two Outcome sections.

Everything in the game has a height: the player, what is placed, the ground. Planned
now because laying out the starter zone with real art ran into the flat world at every
turn on 2026-09-24: a grass tile covered the player's feet, a fire pit sank into it,
the stairs on a house could not be climbed, and the whole house blocked as one box.

The author's words are quoted as typed. Model additions are set apart and labelled.

## Developer thoughts

> (2026-09-24) See screenshot. I want to walk up these stairs

> (2026-09-24) The stairs are part of the model or the invisible ramp. The server would
> know about the ramp.

> (2026-09-24) What I want is true 3d space that I can walk around in. So the stairs on
> my house, I should e able to walk on them. The firepit embeded into the grass layer,
> instead it should sit on top of the grass layer. I should be able to place layers and
> move them up and down, so I can make a path that goies up an hill and over a cliff.

> (2026-09-24) I dont mean move layers, I meant place models at different layers.
>
> Consequence noted: "layers" means heights. A model can be placed at any height,
> including on top of another model. The editor's layers do not move; nothing about
> them changes.

> (2026-09-25) Our next project is height, I want height in the game. Lets thing about
> this in terms of the map itself first. I have a ground layer at 0, then I can have a
> model on top of it at 1. Then for a path like a road or dirt trail it will also be at
> 1.
>
> ```
> 1  [house]     [house]   [dirt-path]
> 0  [ground                    ]
> ```
>
> then for a player walking on the ground
>
> ```
> 1          [player]
> 0  [ground                         ]
> ```
>
> then for a player walking on a road
>
> ```
> 2       [player]
> 1    [road          ]
> 0 [ground                       ]
> ```
>
> Conceptually that is how I would put it. Does that make sense and should we do it
> like that? How does this work when a player moves from the road to the grass, do they
> fall down 1 y level?
>
> Consequence noted: the stacking is right; the numbers are kept as real heights, not
> levels, because models differ in thickness (a road is one voxel, a house about ten
> units), and a level per thing would float the player a whole level above a one-voxel
> road. Road to grass is a step down of one voxel, walked, not a fall. See D0.

The engine question came up on the way here and was settled for now:

> (2026-09-24) Or i continue trying to work with what I have.
>
> Consequence noted: the plan stays on MonoGame and the custom editor. The server
> work below is the same size on any engine, because the server is plain C# either
> way; an engine would change only the client and the tools.

## Already decided

Decisions in force that this plan touches. Four are changed on purpose by the author's
words above; one clashes and is open.

- The ground is never drawn in the scene. The client draws it from `ground.*`
  placements and the zone's default surface. [art-pipeline.md, "The ground"]
  **Changed by the plan:** the ground is placed art, drawn like any model, with a
  height. `ground.*` as a marker kind is not needed for drawing.
- The tool snaps every placement to the ground. [art-pipeline.md]
  **Changed by the plan:** a placement lands on the surface under it, at any height.
- `terrain.*` is sculpted ground with height, sampled into a height field; until then
  it draws nothing and walks flat. [art-pipeline.md, marker kinds]
  **Changed by the plan:** no special kind. Every model's voxels are walkable
  surface, so a hill is just a model.
- Floors above floors (bridges, balconies) are out of scope; upper storeys are separate
  zones like any interior. [art-pipeline.md]
  **Clash, open:** the stairs on the house lead to a balcony, which is a floor above a
  floor. See decision D1.
- Entering a building loads a separate, enlarged interior. No cutaways. [C-2026-09-18]
  Fits: outdoor stairs and balconies are outside; a door still leads to an interior.
- The first playable has server-authoritative walking, collision and a following
  camera. [first-playable.md]
  Fits: the server stays the authority, in three dimensions.
- Non-rectangular scenes, floor cut away at the edge, from `ground.*` placements.
  [backlog.md, second-zone.md F9]
  Changed in shape: where no model is underfoot there is no ground, so a scene's shape
  is what is placed. See decision D3.

Built today:

- A player is a point on a plane: `Player.PositionX` and `PositionY`, the same on
  `PlayerView`. No height anywhere in the simulation, the views or the saved player.
- The compiler measures each model's whole box into a `Footprint`. `ZoneDressing`
  turns every drawn placement at least `BlockingHeight` (0.6) tall into one
  `Obstacle` rectangle, and `IsBlocked` tests a circle against them. A house blocks as
  one box, stairs included.
- `DressingPlacements` draws every placement at height 0, whatever the scene says. The
  scene file stores `y`; the editor always writes 0 and has no way to change it.
- `FacetGround` draws a generated grass plane over the zone's box, under everything.
- The client smooths server positions with `MotionSmoother`; it does not predict.
- The editor draws without lighting. The game lights with `LightSettings` (sun,
  ambient, fog, a preset per time of day). The same grass measures (39, 78, 53) in the
  editor, (49, 99, 67) in the game by day and (22, 33, 25) at dusk.

Deferred elsewhere:

- The editor's own TODO: #10 Height (`y` is always 0, the field waits for it), #13
  shading turns with the model, #21 render settings.

## The plan

### The one design choice: the voxels are the collision

Every model is already a grid of solid cells. The server uses those cells directly:
no hand-drawn ramps or collision boxes. A voxel staircase is a run of steps one or two
voxels high (1/8 to 1/4 unit), and a body steps up anything below a set height, so the
house's stairs work as modelled. A hill built of voxel steps works the same way.

An invisible ramp stays possible as an ordinary model on a hidden layer, for a slope
that should feel smoother than its steps. The server knows about it the same way it
knows about everything else: by its voxels.

> Consequence noted: this is the author's "the stairs are part of the model or the
> invisible ramp", taken with the model as the default and the ramp as the exception.

### Phase 1: models have a height

After it: the fire pit stands on the grass; a path can go up a hill and past a cliff.
Visible only; the player still walks at height 0.

1. Editor: a model dragged or stamped lands on the surface under the mouse (a grass
   tile's top, a hill's top), not at 0. The pick is a ray against the voxels, not the
   bounding boxes it uses today.
2. Editor: a Y field in the inspector. PageUp and PageDown move the selection one
   voxel; with Shift, one unit. The ghost and the selection box show the height.
3. Scene file: `y` is already there. No format change.
4. Game: `ZoneDressing` and `DressingPlacements` carry `y` through, and every placement
   draws at its height.

### Phase 2: players have a height

After it: the player walks up the stairs onto the balcony, and walls block where they
are.

1. Compiler: for each model a zone uses, a compact map of its solid cells goes into the
   package, where the whole-box `Footprint` was. The server still reads no art.
2. Server: a collision world. Placements are found by their bounding boxes; a point is
   tested in the model's own cells, turned by the placement's quarter turn. Models stay
   separate, never merged into one grid.
3. Movement, in `Tick`. The player is an upright cylinder, about 0.35 units in radius
   and 1.5 tall. Each step:
   - ground: the top surface under the feet, within the step-up and step-down limits;
   - wall: the move is blocked when solid cells fill the body above the step height;
   - head room: the move is refused when there is no space for the head;
   - no gravity: a drop bigger than a step is refused like a wall [D4].

   This replaces `Obstacle`, `BlockingHeight` and the flat `IsBlocked`.
4. `Player` and `PlayerView` get a height. `ServerWorldViewUpdate` changes size, so the
   wire version changes. The saved position gets a height column: a migration.

Built 2026-09-25 (`Footing`, `ZoneSolids`, `SolidModel`; migration
`20260925120000_AddPlayerElevation`). Two calls made while building, flagged at the time:

- A floor at height 0 stays walkable across the whole zone, under whatever is placed.
  Scenes did not yet have ground placed everywhere, and every test zone has none, so
  "only placed ground is walkable" would have stranded players. The zone's edge still
  blocks. The stricter rule is left for when scenes cover their ground.
- A model the compiler has no cells for falls back to its footprint rectangle, which is
  what the older tests build by hand. Every model the compiler measures has cells.

### Phase 3: the client follows

1. Players draw at their height, and `MotionSmoother` smooths it with the rest.
2. The chase camera follows the player up and down.

### Phase 4: everything else with a position

- The player spawn takes its height from its marker.
- Items on the ground rest on the surface where they drop.
- Reach to chests, terminals and vendors is measured in 3D.
- A door's trigger has a height.
- Sound sources have a height.
- The party's spread-out positions after a zone change get heights.
- Compiler checks: the spawn is inside solid cells, or floats with nothing under it.

### Phase 5: an editor to trust in 3D

- The editor lights the scene with the game's sun and time of day, so what is placed
  looks the way the game will show it.
- An optional overlay colours the surfaces a player can stand on, to check stairs and
  paths before playing.

### Size, in the model's working time

- Phase 1: 20 to 30 minutes. The voxel pick is most of it.
- Phase 2: the largest, an hour or two with tests. The movement rules and the
  migration take the care.
- Phases 3 and 4: 30 to 60 minutes together.
- Phase 5: about 30 minutes.
- What cannot be sized: how the movement feels. Expect a few rounds of tuning step
  height and speed, judged by playing.

Suggested split into PRs, by dependency: Phase 1 on its own (editor to `master`, game on
a branch); Phases 2 and 3 together, since a server height without a client that draws
it cannot be played; Phase 4; Phase 5 in the editor.

## Decisions

Each had the model's default. Answers are recorded under each.

- **D0. Height as levels or as real values.**
  > Answer (2026-09-25): Real height, auto-stack. `y` in units, snapped to 1/8 (one
  > voxel). Placing on something puts it on its top. 0 is the top of the ground.
  >
  > Consequence noted: a ground tile sits from -0.125 to 0, so everything already
  > placed at 0 stands on it unchanged. The editor does the stacking; a number is only
  > typed to override it.

- **D1. Floors above floors.** The decision in art-pipeline.md puts balconies and
  bridges out of scope. The house's stairs lead to one. Default: change it, so a
  balcony, a bridge or a raised walkway is walkable outdoors; interiors stay separate
  zones.
  > Answer (2026-09-25): Yes, outdoors. Interiors stay separate zones.
- **D2. Step height.** Default: 0.5 units (4 voxels). Enough for voxel stairs and small
  ledges; not enough to walk onto a table.
  > Answer (2026-09-25): 0.5 units, 4 voxels.
- **D3. Where there is no ground.** Default: the player cannot walk off, as at today's
  zone edge. The alternative is falling into the void.
  > Answered by D4: an edge is blocked, so nobody walks off into nothing either.
- **D4. Falling.** Default: a player can walk off a cliff and drop to what is below,
  with no damage.
  > Answer (2026-09-25): Blocked at the edge. A drop higher than a step acts like a
  > wall; nobody ever falls.
  >
  > Consequence noted: no gravity in Phase 2. A move is allowed only when the surface
  > ahead is within one step, up or down, of the feet. Getting down from a balcony is
  > by its stairs. A player can still never be left in the air, which also makes
  > "where there is no ground" the same rule as a cliff edge.
- **D5. The generated ground.** Default: `FacetGround` goes, and every zone uses placed
  ground.
  > Answer (2026-09-25): d5 yes. generated ground goes away.
- **D6. Jumping.** Default: not now.
  > Answer (2026-09-25): Jumping would be cool. lets do it, spacebar to jump
  >
  > Follow-up asked the same day, since a jump goes up and comes down and D4 says
  > nobody falls:
  >
  > Answer (2026-09-25): a jump can carry a player onto something up to the jump
  > height (about 1.25 units: a crate or a low wall, not a house). Edges higher than a
  > step still block, in a jump too: a jump near an edge lands back on the same level.
  >
  > Consequence noted: a jump is the one vertical motion, with its own short arc and
  > gravity for the jump alone. It belongs in Phase 2, with the player's height on the
  > server. The client sends the jump as an intent; the server runs the arc in `Tick`.

## Consequences

- art-pipeline.md: the ground section, the snap-to-ground rule, `terrain.*` and
  `ground.*` in the marker kinds, and (with D1) the floors-above-floors line all change.
- backlog.md: the non-rectangular scenes item is answered by D3.
- The editor's TODO #10 (Height) is Phase 1; #21 (render settings) is part of Phase 5.
- `Footprint`, `Obstacle` and `BlockingHeight` are replaced in Phase 2, with their tests.
- The wire version changes in Phase 2, so every client needs the new build.
