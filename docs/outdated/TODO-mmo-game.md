# TODO

Things found while working that are not the current task. Update as they land.

## Rendering

Built: `VoxMesher` winds faces clockwise seen from outside, `TestWindingFacesOutward`
holds it there, and `VoxelRenderer` culls counter-clockwise. The back faces that
`CullNone` used to draw leaked through as dark speckles on walls when zoomed in; that
was the visible cost. Winding is now in the convention both meshers share.

Built: wind sway in the vertex shader, driven by a free property `sway` on a placement
or its layer (`ZoneConventions.SwayOf`), metres of movement at the top. Nothing else
animates yet. Next in that line, when wanted: characters split into parts (head, arms,
legs) swung procedurally from velocity, then frame animation for things like a fountain
from MagicaVoxel's multi-model files, which `VoxFile` already reads.

Built: small effects. A placement's `effect` property names one (`fire`, `fountain`;
the numbers are in `ParticleEffect`); particles start on the model's top voxels in
their colours, are stepped on the CPU and drawn as one instanced cube.

1. **A `light` property.** Nothing is decided by a model's name any more: the glow
   and light placeholders went on 2026-09-24 (`PlaceholderEmissive`, which made bright
   colours glow on models named like lamps and signs, and `LampLights`, which hung a
   point light off anything named `obj_stlight*`), and so did the `chr_*` exception to
   blocking, which now goes by height alone. What glows is the `_emit` material in the
   `.vox` file, per palette slot, and no placed model gives light. Left: a free property
   `light` on a placement or layer, for a model to light its surroundings; the light
   sits at the centre of the model's emissive voxels, in their average colour. A
   `blocks` property (`auto`, `yes`, `no`) waits until something needs to differ from
   its height.
2. **An effect that glows should light its surroundings**: a point light on the emitter,
   fed by the same `light` property.
3. **Metal and glass materials.** MagicaVoxel stores a material per palette colour
   (`MATL`), and `VoxFile` reads only the `_emit` kind. Metal, glass and roughness are
   dropped, so a window made metal to reflect the sky renders as flat colour in the
   game while MagicaVoxel's path tracer shows it reflecting. Doing it for real means a
   specular term per palette colour in the voxel shader and something to reflect (a
   sky colour at least). Until then a surface has to read by its colour alone. Found
   2026-09-23 while reskinning a house.

## Client diagnostics

Built: `.\scripts\client-up.ps1 -Stats` writes one CSV line per drawn frame to
`Clientin\stats\<timestamp>.csv` (`ClientStatsLog` over `FileLog`, which writes on its own
thread): the frame's sections (`FrameProfile`), draw calls, particles, heap, collections,
CPU, network, sound voices, position, with the machine and the GPU adapter at the top.
`tools\stats-hitches` reads one and prints the worst frames. Measured 2026-09-20 on
battery: draw 9 ms, of which particles 7 ms and 215 draw calls 1 ms; the adapter was the
Intel UHD, not the RTX 4050.

Built 2026-09-24: particles as instances. One cube mesh is uploaded once and each
particle sends 20 bytes (position, size, colour, glow) to the voxel shader's `Particle`
technique, instead of 24 vertices rebuilt every frame. Checked off screen against the
old path (`../claude-output/mmo-game/particle-probe`): the same pixels to within 2 of
255, and 3000 particles in 0.75 ms a frame against 2.98 ms. The fountain's rate is
halved to 40 a second.

1. **The dynamic buffer may stall.** The same particle count cost 2 ms for ten seconds
   and 7 ms after, which looks like the upload waiting on the GPU. Double-buffer it if
   the instancing change does not make it moot: the upload is now about 60 KB a frame
   for 3000 particles rather than 2 MB, so measure again with `-Stats` first.
2. **The GPU Windows picks.** On battery the client ran on the integrated GPU. The
   preference is per executable, and the client builds into a new folder every launch,
   so either the launch script sets it or the build lands at a fixed path.
3. **First frame in the world** spends 8 ms on the first world view and meshes item and
   terminal models on first sight. A load-time cost; mesh them at startup if it shows.
4. **A debug mode for players**: the same log, switched on in settings, landing in the
   profile folder with the console log beside it, as the start of a bug report.

## Sound

Built: placed emitters, first pass. A placement's `sound` label (placement, then layer,
then scene) names an entry in `audio/sounds.json`; the compiler checks it and copies the
recording into the package; the client plays one loop per source, volume by distance
from the player's body, the loudest eight at once. `docs/engineering/sound.md` is the design.

Built 2026-09-24: a sound with no picture. A placement the zone does not draw that
names a sound becomes a `SoundMarker` in the dressing, and the client hears it like any
other source. Before, the compiler allowed it but the client only heard drawn things.

Built: hysteresis at the voice boundary (`SoundEmitters.Hysteresis`, 1.25). A source
with a voice counts that much louder when the voices are chosen, so a silent one has
to be clearly louder to take a voice, and a voice holds on a little under the floor.

Built: pan, from the source's offset against the camera's right on the ground
(`SoundEmitters.PanAt`). At most 0.8 to a side, and drifting to the middle inside the
reference distance so walking past a fire does not flip it between ears.

1. **Loops out of step.** Every instance starts at the beginning, so two fire pits
   crackle together. A random start offset needs a seek MonoGame's instance does not
   have; two or three takes per definition, picked at random, is the way round it.
2. **Blank means what?** A `sound` set to empty on a placement falls through to the
   layer today, as `effect` does. If a layer default should be switchable off for one
   thing, blank has to mean silent instead. `SoundConventionTest` pins the current rule.
3. **The mixer** lives in the scene editor's TODO: definitions edited there, labels
   dragged rather than typed, positional preview while orbiting.

## Zones

Built 2026-09-22: a `Zone` per loaded zone inside `WorldSimulation`, doors as named
transitions, the chest, Dollars and the vendor (`docs/engineering/zones.md`).

Built: a cooldown after a crossing (`TransitionCooldownSeconds`, 3 s from the arrival,
including the 0.8 s hold). No door takes a player during it, one still standing on a
door when it ends has to step off and on again, and the client keeps every threshold
at its resting rate for as long. A body walking back and forth over a door spot crosses
at most once per cooldown. 3 s chosen 2026-09-24.

## Scene compiler

Built: `src/Compiler/` turns `art/scenes/*.scene.json` into `Package/`, with coded
diagnostics (`src/Compiler/README.md`); `src/Core/Zones/ZoneLoader.cs` turns a zone file plus a
compiled scene into a `ZoneDefinition` and a `ZoneDressing` for both server and client.
The generator, `footprints.json` and the client's own copy of the art are gone. The
package carries a stamp (`package.json`, yyyyMMddHHmmss) the compiler writes; the client
sends it and its own build version on connect, and the server refuses a stamp that is
not its own or a build its `ClientCompatibility` map does not list. Left:

Built: a placement on a non-drawable layer that the game does not read warns (SC2007).
Read are the player spawn, a door and a sound; anything else would not be in the zone.
A placement switched off on a drawn layer is left alone.

Built: a player spawn inside something that blocks warns (SC2008), checked with the
game's own `ZoneDressing.FromScene` and `WorldSimulation.PlayerRadius`.

Built: two model files with the same art under different names warn (SC2009), naming
both. Same means the same size and the same voxels in the same colours, compared by
colour value, not palette slot. Every file in the model folders is checked, used or not.

1. **`kind` as a prop on the layer** (`kind = spawn`) so the compiler finds spawns by a
   declared prop rather than the layer's name, and the layer name goes back to being
   the editor's business.
2. **Ship the checker as a command** the editor can run on save (see the tool's TODO),
   so errors show against rows while editing.
3. **Sandbox** still drops its own `.vox` copies in `src/Sandbox/vox` (not in git). It shows
   the compiled starter zone now, but its lineup wants every model, so it keeps that
   folder rather than the package.
4. **Zones and scenes are wired together only by name.** A zone file
   (`src/Core/Zones/Data/1.json`) names its scene, and the server refuses to start when
   that scene is gone, but nothing on the art side knows it. The editor deletes a scene
   in a click, and an incremental build may not even notice, since a deleted input is
   never newer than the package, so the old zone lingers until a clean build. Make the
   mapping part of the asset pipeline: the compiler reports a zone whose scene is
   missing. Decided 2026-09-24: not now; and when it is done, no editor part, since the
   compiler's report at build is enough and the editor stays general.
5. **Rebuild the real-art tests without the real art.** `tests/Compiler/RealArtTests.cs`
   compiled the real map project, so editing a scene could fail `dotnet test`. Removed
   2026-09-24; the file now lists what each one covered. Rebuild them on small scenes
   made in the test, like `SceneCompilerTests`.

## Performance tests

Not started; agreed 2026-09-22 as later work. The idea is a timing contract next to the
`[MessageSize]` byte contract: an integration test that fixes the setup and asserts a
budget. For example: given this world view, a fake database and a fake network with no
latency, an intent sent from the client gets its answer within 0.2 ms.

1. **Where the time is measured.** From the client's send to the server's answer,
   through the real handler, the simulation and the serializer, with the fakes on the
   edges only.
2. **Noise.** Timing on a shared machine or in Docker varies. Decide how a test
   tolerates it (warm-up, several runs, a median) before the first budget is written.
3. **Where they run.** Not in the Docker image, which stopped running tests on
   2026-09-22; probably not in the plain `dotnet test` either, where a slow host would
   fail for no code reason. A CI job with its own step when there is one.

## Tests

1. **The item respawn test is flaky.** `TestATakenItemComesBackAfterAWhile` in
   `tests/Core/Simulation/WorldSimulationTest.cs` failed about one run in six: items
   spawn at random, and when two land within pickup reach the player takes both.
   Commented out 2026-09-24. Not to be fixed: item spawning is being redesigned, and
   the test goes with the old design.
2. **Rebuild the real-art tests** (see Scene compiler) without the real art.

## Deploy

Built: the build context is a whitelist (`.dockerignore` starts with `*`, then `!` for
each file and folder the server image needs, then `**/bin` and `**/obj` again). A cold
build sent 89.7 MB before; the image it makes has the same files. One visible
difference: `.git` no longer reaches the image, so the informational version loses its
`+<commit>` suffix. `BuildVersion` reads the assembly version, which is unchanged.

Built 2026-09-24: the image compiles the art itself. The compiler writes its JSON with
LF on every OS (`SceneFile.Options`, `SoundManifest`), so a Windows build and the Linux
image make the same stamp for the same art (`e376a2e5df284306` both, checked);
`**/generated` is ignored; and the compiler DLL is an input of `CompileMapPackage`, so a
compiler change recompiles a package whose art has not changed. The asset version
changed once with this, so client and server must be rebuilt together.


Editor Specific Notes (Cleanup Items)
- Unhandled exceptions in editor crash it, atempt to catch log and dont crash the application
- Deleting a non existant folder crashes the application
- Empty folders in project remain, maybe thats fine
- Whey I paste a group of objects I want them to stay selected
- When I open a new model in MagicaVoxel and rotate the item in that application. I can see my scene being rotated in the editor. Maybe the editor is not respecting if its actually focused?
-in my layers, give me a way to collaps all the models so i dont have to look at them, right now they just list in the scene. This gets annoying if I have a lot. Maybe do a default folder layer-models and allow organization also.
- search in the art library, this just filters the view to what matches, do a wildcard on both sides so if I searc asd, it would match anything with *asd* in it
- search box in the project art
- prop list in Inspector needs to longer
- when I drop an item onto the scene from the art library it should not scroll to the item in my project folder.
- q should drop everything and go back to select mode, its the "Drop everything and escape everything button"
- HIde this layers placements from the list, not a good tooltip for the collapse menu, "Collapse Layer" is better. I would almost prefer that the layer UI be a litter more structured that having an HR as the header for the layers and then the layer names sort of to the right of the UI elements like the collaps and the two checkboxs. this feels clunky and different from the rest of the UI
- Some of the tooltips say "Game" this is maybe ok but its bordering on talking about game rules and game logic in the editor. I would prefer to say Marks the <thing> as drawable.
- is it a placement named player on spawns or a place with type spawn named player?
- If I hide my default layer in the editor and then go to a new layer (for example spawns) I have nothing to reference when I draw. The entire grid disapeared. Instead I see one weird grid at the edge of the map where my default layer was, its strange, ask for a screenshot
- The language on the hide/show layer is hard to understand, It feels like clicking the Visible: drawn in the scene would mark it as hidden in the game, so lets work on that language
- When I run client again and rotate the editor rotates the scene on the editor main area. weird, Going to check if vice-versa is true, yes it is. maybe its something with monogame or are we hooking directly into windows keyboard input or something weird? It must be a focus issue because I am sure we duplicate files so it couldnt be file based.
- So I added a spawn layer. Added a placement for my player named the placement player and when i run the game I cant see my character. It looks like I am spawning off the map but in my scene editor the placement is in the center of the map. Maybe related to the grid visibility issue from before.
