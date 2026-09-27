# The map project and where art lives — decision record

**Date:** 2026-09-22
**Status:** Decided in shape. Steps 1, 3 and 5 of the plan are built (branch
`ger/map-project-pipeline`); steps 4 and 6 and the editor's half are not.
**Kind:** a decision record, not a Q&A session record. The session that produced it
started as a `/feature-design` interview and was stopped, because it mixed several
topics. Statements labelled "Model:" are the model's framing, not the author's words;
quotations are verbatim.
**Sources read:** docs/engineering/pipelines.md; docs/engineering/art-pipeline.md;
Compiler/README.md; compile-art.ps1; art/mmo-game.project.json; art/props.json;
Server/Server.csproj; Client/Client.csproj; Core/Zones; Compiler/SceneCompiler.cs;
Voxels.Rendering/VoxelScale.cs; Server/MessageHandlers/ConnectHandler.cs.
**Repos:** this one (`mmo-game`) and the editor (`voxel-scene-maker`, separate
repository at `C:\Users\geral\src\voxel-scene-maker`).

The art pipeline felt clunky. Rather than fix the symptoms, the session named the
purpose of each piece, worked backwards from what the game actually consumes, and then
cut what did not fit.

## The frame

Three things the game requires. The first two travel together in one artifact; see
decisions 8 and 9 for how they are versioned.

- **Map Details** — the zone's extent, the placements (model, position, rotation, layer,
  drawable, properties, id, name), the layers and whether each is drawn, zone
  properties.
- **Map Measurements** — per model: width, depth, height. Measured, never authored.
- **Art** — the `.vox` models themselves.

Who needs what:

```
   Map Measurements ──┬──────────────────▶ SERVER
                      │                      ▲
   Map Details ───────┼──────────────────────┘
                      │  │
                      ▼  ▼
   Art ──────────────▶ CLIENT
```

In the author's words:

> So the server needs - Essentially every detail of the map EXCEPT the artwork.

> Lets treat the art like actual first class citizens. Lets reframe it. The art, the
> scenes and the server footprints. They NEED to be in source control.

## Two inputs, and two things that are not inputs

Inputs:

1. **Raw art** (`.vox`), which reaches the client unchanged.
2. **Map details as the editor writes them.** They are editor-shaped and carry a few
   fields the game ignores, such as a layer's `visible` and `locked`. Those ride along
   on purpose: narrowing them buys nothing until a project has hundreds of scenes.

Not inputs:

- **The editor's prebuilt properties.** Nothing in the game or the compiler reads them.
  They are the editor's palette, per project, and they belong inside the editor's
  project file rather than in a file of their own (today: `art/props.json`).
- **The scale** (`voxelsPerUnit`, 8). The game holds the same number in
  `Voxels.Rendering/VoxelScale.cs` as `MetresPerVoxel = 1/8`. The author:

  > both the editor and game need to agree for things to work correctly so its needed
  > two places and thats just annoying for now. But the Editor does not need to pass the
  > value into the game, is what I am getting at. So its not input. Its a hardcoded fact
  > and an agreement between the two programs

- `gridSize` in the project file is read by nothing in this repository.

## The pipeline, after the cut

```
  [Raw Art] ──┐
              ├──▶ Map Editor ──▶ Compiler ──▶ Map Details
  [Map Data] ─┘                              ──▶ Map Measurements
                                             ──▶ Raw Art (passed through)

                                 Server takes details + measurements
                                 Client takes all three
```

Model: the compiler reads the art only to measure it. Copying the art is delivery, not
compilation.

## What the author asked for

> - My scene editor to just edit scenes (place art, assign properties)
> - No extra weird steps
> - One set of art in source control
> - My edited scene file in source control
> - A seemless way to get my scene project (validated, and translated)

## Decisions

1. **One map project folder holds the project**: the scenes, the models those scenes
   use, and the editor's project file, which carries the project's prebuilt properties.
   No absolute paths, no empty placeholders, no second copy of anything.
2. **The map project lives in this repository** (`mmo-game`), and the editor points at
   it. The condition the author set: the editor encapsulates everything it needs, and
   depends on nothing else in this repository.
3. **The editor owns collection.** Placing a model copies it into the project; saving
   prunes models no map uses, across every map in the project; a name clash is refused
   for the author to resolve, never renamed silently.
4. **The project's copy of a model is authoritative.** The library is a shop, and the
   author put the rule best:

   > Updating the library does nothing for the books I have chekced out, I already
   > checked them out and its up to me to return them or keep them

   So a library update never reaches the game by itself; re-importing is an explicit act.
5. **The editor stays game agnostic.** It offers properties; the game decides what they
   mean. The author, on purpose:

   > if I move to game 2 thats a voxel game and I want to add a prop called
   > twitter-handle because my second game does something with twitter, thats perfectly
   > fine, my game needs to figure out what to do with it

6. **Prebuilt properties are editor-owned, and duplication is accepted.** The editor's
   palette lists what is easy to set; the game keeps its own list of the properties it
   understands, next to the compiler, and reports one it does not know. The two lists
   name the same things and are allowed to drift; the compiler is the authority.
7. **The compiler runs as part of the build**, with the map project as input, not as a
   command to remember. MSBuild skips it when nothing changed. A broken scene fails the
   build, in the same place every other error appears.
8. **The compiler emits only what it creates.** Map details and map measurements stay
   in one artifact per scene, as they are today: they change together, and nothing asked
   for them apart. The build copies the models straight from the map project into the
   client, which is what keeps one set of art in source control.
9. **Two versions**, each a hash of what went into it, instead of a compile timestamp:
   one for the map artifact, one for the art. The server checks the map version, the
   client checks both. A recolour changes the art version alone and leaves the server
   alone; a resize changes both, and the server does need to know.

## What disappears

- `Package/` as a committed folder, and the duplicated 2.1 MB of models inside it.
- `compile-art.ps1` as a separate step, and the rule "rebuild server and client, then
  commit Package".
- The machine path (`C:\Users\geral\src\mmmm\vox`) inside a versioned project file.
- The machine-specific half of the model library: `art/models` already held 105 models in
  `custom-primatives/` and `metro-minis/`; only 9 of the 28 the maps use came from the
  outside folder.
- `art/props.json` as a file of its own: it moves inside the editor's project file.
- One stamp standing for things that change at different times.

## Why it was confusing

`art/` was doing four jobs with no labels: the editor's project file, the editor's
palette, the maps, and a stand-in for art that actually lived on one machine outside the
repository. Each of those now has one owner:

- **Editor project**: project file (with the palette), scenes, models.
- **Game**: what a property means, and the scale.
- **Generated**: details and measurements, as build output.

## What reading the code changed

The pipeline code was read end to end after the decisions above were taken (912 lines in
`Compiler`, plus `ZoneLoader`, `ZoneDressing` and the two consumers). Three corrections
and two findings.

Corrections:

1. **There is almost no normalization.** A compiled zone is the scene copied whole —
   format, name, width, depth, layers, placements, properties — plus the measured
   footprints. The only real translation is for sounds: paths rewritten and the level
   resolved into gain and reference.
2. **The compiler does not check the property vocabulary.** It checks the conventions it
   knows (sound label, transitions, chest and vendor names) and passes every other
   property through unseen. Reporting an unknown property is new work, not a small
   addition.
3. **The known-property list already exists**, as `Core/Zones/Scenes/ZoneConventions.cs`,
   and it lives in `Core` rather than beside the compiler, because the compiler and the
   shared reader both use it. The author finds the name unsatisfying; a new one is open.

Findings:

4. **`ZoneConventions` holds two jobs**: the vocabulary (the names) and the resolution
   (which levels may set a property, in what order, with what default and parsing). The
   agreed direction is to declare the vocabulary as data in `Core` — name, type, levels,
   default — and have small typed readers walk the levels that entry allows, instead of
   each accessor rewriting "placement, then layer, then scene" by hand. The same
   declarations then give the compiler a better reason when something fails.
   Two things the table cannot hold: rules that span properties (a door needs both
   halves; a chest must be named and unique), which stay as compiler code; and values
   owned elsewhere (`sound` by the audio manifest, `effect` by the client's effects).
5. **`DressingPlacement` is where the property list would hurt.** It gains a field per
   property, and three of them — `Sway`, `Effect`, `Sound` — are presentation the server
   builds and never reads. The fix is a split by consumer: what both sides need
   (position, rotation, model, blocks, interaction) stays shared, and presentation
   becomes something the client builds. "The server's zone data carries no presentation"
   is also the kind of rule an architecture test could hold.

Deferred, as a direction rather than a decision: a `kind` on a placement, which the game
maps to a bundle of look, effects, collision and interaction, with properties left for
what is genuinely per placement (a door's target, a chest's name, one tree's sway). It
would shrink both the vocabulary and `DressingPlacement`, and it changes what the
editor's palette should offer, so it waits.

## Consequences to handle when the code is written

- **Docker.** The compiler running inside the build means the map project must be in the
  image's build context, and the image must run the compiler. Note the related finding
  from the same day: a Linux SDK image cannot build the client's MonoGame content
  pipeline, which is why the image now publishes the server only.
- **A missing package fails silently today.** `Server.csproj` and `Client.csproj` copy
  `..\Package\...` with wildcards, so a build with nothing there still succeeds and
  produces a game with no zones. Whatever replaces it must fail loudly.
- **Migration.** The 28 models that exist today only inside `Package/models` are the art
  the game actually runs, and they move into the map project.
- **Two gates on connect.** The server refuses a client on build version
  (`ClientCompatibility`) and on content. Nobody has asked yet what each catches that the
  other does not.

## Prelim sound plan

Preliminary, written 2026-09-22 so the pipeline work does not paint sound into a corner.
It is not a decision, and none of it is being built in this pass. `docs/engineering/sound.md`
already holds the design thinking (four kinds, and the rule that the server never names a
sound); this section only places sound inside the frame above.

**Where sound sits in the frame.** The map names a sound; it never carries one.

```
   Map Details (a label: sound = fire-crackle)  ──▶ CLIENT
   Sound Catalogue (label -> file, loop, level) ──▶ CLIENT
   Recordings (.wav)                            ──▶ CLIENT

   SERVER: nothing. It owns state; the client turns state into sound.
```

So sound adds no requirement to the server, and two client-only artifacts to the three
already named.

**Who owns what, following the same rules as models:**

- **The label is a property**, so the editor stays ignorant: it offers a property, the
  game gives the word meaning. This is already true today.
- **The catalogue is the game's** (`audio/sounds.json`), the same way the known-property
  list is the game's. It is where a label becomes a file, a loop flag and a level.
- **The recordings stay in the game repository**, not in the map project. The editor
  never places a recording, so the map project has no reason to own one. This is the one
  place sound differs from models, and the reason is the editor's scope, not convenience.
- **The compiler keeps validating labels** against the catalogue, which is the check that
  catches a typo, and it keeps emitting the resolved manifest (paths rewritten, level
  resolved into gain and reference). That is something it creates, so decision 8 covers
  it.
- **Delivery follows decision 8 as well**: the build copies the used recordings to the
  client, rather than the compiler copying them into a package folder.

**Versioning.** Recordings and the resolved catalogue are client-only, like the art. The
simplest first answer is to fold them into the art version, so there are still two
hashes. Give sound its own version only if the two prove to change at different rates.

**What this plan does not settle**, and what a sound session has to take up:

- The music channel as its own property, proposed in `sound.md` and not agreed.
- Zone-wide environment beds versus local markers on a non-drawable layer, which is the
  one case where a scene would carry a sound with no model.
- Authoring: a label is free text in the editor today, so a typo is caught at compile
  time rather than while placing. The same question as the property palette, and the
  same answer is available (the palette can offer the property; only the game can check
  the value).
- The numbers: cut-off distance and voice count, which `sound.md` says are found by ear.

## What was built, 2026-09-22

On branch `ger/map-project-pipeline`, off this one.

1. **The map project is self-contained.** `art/models` already held 105 models; the 9
   that lived only in the folder outside the repository are now in
   `art/models/imported/`. The project file names its own models folder and nothing
   else, and `gridSize` is gone, since nothing reads it. Compiling with the old and the
   new project file produced byte-identical output.
2. **The package is built during the build.** `MapPackage.targets`, imported by Server,
   Client and Sandbox, compiles the map project into that project's own `generated/`
   folder and copies what the project needs: zones and the stamp for all three, models
   and sounds for the client only. `Package/` is untracked and ignored,
   `compile-art.ps1` is deleted, and `play.ps1` no longer calls it. The Docker image
   compiles the map project too, and its `/app/Zones` holds the compiled zones.

3. **The scene vocabulary is declared.** `ZoneConventions` held three jobs and is gone.
   `KnownProperties` declares each property the game understands (name, value kind, and
   the levels that may set it). `SceneProperties` reads them, walking the levels each
   entry declares rather than each reader rewriting "placement, then layer, then scene".
   `ZoneRequirements` holds the `spawns` layer and the `player` placement, which are
   optional for a scene and required only once a zone uses it. The compiler reports
   `SC2006` for a property name the game gives no meaning to, with two tests; today the
   scenes use only known names, so it starts quiet.

**Step 2 of the plan was dropped.** "Stop the compiler copying art" existed to remove a
second copy of the models from source control. Once the package became build output, the
copy is no longer in source control at all, so the change bought nothing.

**Three things to review:**

- **The scene warnings are now build warnings.** The 9 `SC2004` stacked-placement
  warnings appear on every build of the server, client and sandbox. They are real, and
  they are in the maps, but the build is no longer at zero warnings.
- **Two tests changed.** `TheCommittedZoneLoadsFromTheCommittedPackage` became
  `TheZoneLoadsFromWhatTheBuildCompiles`, and both it and the reachability test now
  compile the package themselves instead of reading a committed folder. The message the
  loader throws no longer names `compile-art.ps1`, and the 2 tests asserting on it
  follow.
- **MSBuild detail worth knowing.** Adding `None` items inside a target and giving them
  a `Link` with a filename metadata reference batches over every `None` item in the
  project: a `Dockerfile` ended up in `Zones`, and zone files in `models`. The targets
  copy the files explicitly instead.

## Not decided: the code

Nothing here says how the change is made. Still to work out, in three places:

- **Editor** (`voxel-scene-maker`): collect on save, prune, refuse clashes, the palette
  inside the project file, the library path as a machine setting.
- **Compiler**: input becomes the map project alone; drop the copy stage; hash the map
  artifact and the art separately.
- **Game**: build-time compilation, models copied from the map project, and the
  connect-time check against the new hashes.

Sounds are parked for a session of their own.
