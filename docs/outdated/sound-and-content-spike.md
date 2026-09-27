# Sound, content and headroom: a spike

A write-up of a conversation on 2026-09-20. It started with how sound should work,
went through where sound assets should live and whether the scene editor and the
compiler are being used correctly, then whether the MonoGame content pipeline is, then
what MonoGame is doing for us at all, and ended on how far the renderer can go. Nothing
changes as a result of this document. It records the analysis, the two things agreed
along the way, and the ideas raised, so that when any of it is picked up the thinking
is already done. `sound.md` is the standing design for sound; this is the record of how
it was reasoned through and everything around it.

## 1. Sound

### Four kinds, split two ways

The kinds of sound the game wants split along two questions: does the sound come from a
point in the world, and does the server have anything to say about it.

| Kind | Examples | From a point? | Server involved? | What starts it |
| --- | --- | --- | --- | --- |
| Music | the zone's soundtrack, the terminal's | no | no | the scene, the hour, whether you are at a terminal |
| Placed emitters | fire crackle, fountain trickle, a humming transformer | yes, a placement | no | being near it |
| Environment | wind, a river, a busy street, crickets, birds | a region, or nowhere | no | zone and hour for the bed; a marker in the scene for a local one |
| Interaction | jacking into a terminal, picking something up | yes, a body | no new messages | a state change the client already receives |

**The server never names a sound.** It owns state; the client turns state into sound.
The first instinct was that the server would reference a sound by id and tell the
client when to start it. On inspection every trigger is already on the client: the zone
id, the world clock's hour, terminal access flipping, an inventory gaining a stack, a
placement being near. A server message naming a sound would only be needed for a
one-shot event with no state behind it, an explosion say, and none exists yet.

### The label model

The scene says a label, never a file. `sound = crackle` is a name that a sound
definition binds to a recording, a loudness and whether it loops. `sound =
fire-crackle-01.wav` would tie every scene to the asset layout, and swapping a
recording would mean editing scenes. The definition is where the binding lives, so
the scene never has to. This is the same rule the art pipeline already has for
`effect = fire`: the scene says what a thing is, the game says what that means.

**A definition's numbers are the thing's own nature.** A waterfall is loud and a candle
is not, the way a lamp glows in every scene it stands in. So loudness belongs to the
definition, not the scene. Two numbers: a gain, and a reference distance inside which
the sound is at full gain. **Range is derived, not authored.** With inverse-distance
falloff, volume at distance `d` is `gain * reference / d`, clamped near the source; the
sound is cut once that drops under a floor. A louder thing carries further, which is
how sound behaves, and it is one fewer number to keep consistent.

### Read at all three levels: agreed

The scene format carries free properties at three levels: the scene, each layer, each
placement. The property `sound` is read at whichever level it was set, placement first,
then layer, then scene, and the game never asks which layer a thing is on. So all of
these are the same to the game, and the scene author picks whichever fits:

- `sound = crackle` on one fire pit.
- `sound = crackle` on a layer of fire pits, so none of them says it.
- A layer called `sounds`, not drawn, holding markers with no model and a `sound` each.

The layer level is inheritance, nothing more. The game makes no assumption about how a
scene is organised. Agreed 2026-09-20.

### Effects and sound are separate: agreed

`effect = fire` gives the embers; `sound = crackle` gives the crackle; a fire pit says
both. Having `effect = fire` imply the crackle would save a word per fire pit and it
would also be two systems reaching into each other. Separate, a fire can be silent, a
sound can have no picture, and neither system knows the other exists. Agreed
2026-09-20.

### Sound and music as two words: proposed

The difference between `sound` and `music` is behaviour, not where they sit. `sound` is
the emitter layer: many at once, each from a point, each fading with distance. `music`
is the one music channel: exactly one thing owns it at a time. The scene's own
`music = beginner_soundtrack` is the default owner. A placement can carry `music` too,
an NPC with `music = flute_song`: within its range it takes the channel over, the
soundtrack crossfades out, the flute plays, and the soundtrack comes back when you walk
away. So `music` on a placement is not a sound that happens to be musical; it is a thing
that takes over the music while you are near it. Same three-level read for both.

Not yet agreed. One constraint noted for when it is: MonoGame streams music as a
`Song`, of which one plays at a time, so an overlapping crossfade is either fade out
then in, or music held in memory as sound effects.

### Sizes

Three ways to say a bonfire is louder than a fire pit: separate definitions per size
(`fire_small`, `fire_medium`, `fire_large`), which multiply names and may share one
file; one definition and a per-placement override (`sound = crackle`, `gain = 2`),
which is the same kind of thing the scene already says with `sway = 0.3`; or both,
with a new definition when the recording is different and the override when it is the
same sound louder. The third was recommended. Not decided.

### Moving things

The emitter pass on the client does not care what a source is. Each frame it takes a
list of sources, a position and a definition each, and does the same work for all:
distance to the listener, volume from the falloff, pan from where the source sits
relative to the camera, and which sources get one of the limited voices. Static sources
are handed in once from the dressing. Moving ones are handed in with that frame's
position wherever it comes from: a placement on a path is animated by the client, so
the animated position goes in; a server-driven entity arrives in the world view every
tick, and the client's motion smoothing gives a position between ticks. A train on a
rail and a hacked RC car are the two cases, and the pass hears no difference. What
follows the train is the position, not the sound.

### Listener, budget, files

The listener would be the player's body, not the camera, which sits behind and above
and would hear things the body is nowhere near; pan comes from the source's offset in
camera space. The pass would keep the nearest few audible sources on a voice each, with
hysteresis at the boundary, and start each loop at a random offset so ten fires do not
crackle in step. `.wav` for anything positional or short, because a `SoundEffect` is
held in memory and plays many instances; `.ogg` for music, which streams. None of these
is decided; they are the shape the first build would take.

## 2. Is the scene editor being used correctly?

The scene editor is Voxel Scene Maker, a sibling repo, generic and freehand on
purpose. Its own design notes say the decision was made twice: no game words in the
tool. The worry was that a `sound` property would make the editor into a thing that
sets up entities and their sounds, which sounds specific to this game.

From the editor's README and code: it stores free properties at all three levels and the
inspector edits all three; `props.json` beside the project pre-packages a property with
a name, a type (`bool`, `int`, `float`, `choice`, `string`) and a default, and that file
is this repo's (`art/props.json`), not the editor's; the editor ships no props and gives
none a meaning; markers without a model and regions are on its TODO; and it plans to
run this repo's compiler on save without learning what the words mean.

So a `sound` property is correct by the editor's own rules. The tool would only stop
being generic if it played sounds, listed sound files or checked the word, and none of
that is proposed. This is how every generic level editor that has lasted works: Tiled's
custom properties, Unity's tags, Godot's metadata are all "the editor holds a label, the
game decides what it means". Validation belongs to the compiler.

One consequence to weigh: if `sound` is a `choice` prop, its options list in
`props.json` duplicates the names in the sound manifest. Either the compiler checks the
two agree, or `sound` is a plain `string` prop and the compiler is the only checker.
`effect` has the same duplication today and nobody checks it.

One other honest place a sound could attach: on the model, as a sidecar beside the
`.vox`, saying "a fire pit crackles wherever it stands". That is the MagicaVoxel stage's
question (what is this thing) and the `.vox` format cannot carry the answer. It would
save saying `sound = crackle` on every fire pit in every scene, with the compiler
merging the default into each placement and the scene able to override. Not proposed
now: `effect` already set the precedent that a fire pit's fire is said in the scene, and
one mechanism is enough until the repetition hurts.

## 3. Working folder, compiler, package, content

The discomfort: `art/` is a working folder for external editors, kept in the repo for
source control, and the game should not depend on anything in it. Content should be
the source of truth for what the game knows. And graphics and sound felt like they
should be separated somehow.

**The game does not read `art/`.** The only readers are the compiler and one compiler
test. The client links `Package/zones`, `Package/models` and the stamp into its
`Content` folder at build; the server links `Package/zones` and the stamp. The chain
wanted is the chain that exists: working folder, compiler, package, and the package is
what lands in content.

**Two kinds of content already live in the client.** The `Content` folder holds
MGCB-built assets the client owns outright (fonts, UI art, shaders), and the linked
package (zones, models) that the compiler produced from the scenes, that is stamped,
and that the server refuses a client for not matching. Those are different in kind, and
"graphics versus sound" is not the line that separates them.

**The line that does:** whatever a scene can name by label goes through the compiler,
so the label can be checked and the package carries what it names; whatever only client
code triggers is plain client content. By that rule `sound = crackle` is pipeline
content like a model; a button click or the jack-in sound is client content beside the
fonts; and music is whichever the `music` decision makes it. Models are client-only
presentation and are in the package anyway, because the scene names them, so the
precedent for "client-only, but through the pipeline" is already set.

**The working folder is a separate question.** Whether recordings sit in `art/sounds/`
or a sibling folder is about tools and who edits what. Sounds are made with different
tools than voxels, so a sibling folder is reasonable; the compiler would read two
working folders, or the project file would list the audio folder the way it lists
model folders. The package is the same on the other end.

## 4. The MonoGame content pipeline

### What it builds today

Exactly five things: the two shaders, the UI font, three UI textures. Those become
`.xnb` files loaded with `Content.Load`. Everything else the client reads is copied raw
into the output folder by `None` items in the csproj: the zone files, the package's
models and the stamp. The `.vox` files are parsed at runtime by the client's own reader
and the pipeline never sees them. Sounds could take the same raw route:
`SoundEffect.FromFile` loads a `.wav` and `Song.FromUri` streams an `.ogg` with no
`.xnb` involved.

This is the standard setup and it is right. Shaders cannot be compiled at runtime on
DesktopGL; a `SpriteFont` has to be rasterised somewhere. Custom formats loaded at
runtime is how MonoGame games handle Tiled maps and JSON, and `.vox` is a custom format
with no built-in importer.

### What it would add

The pipeline does nothing on its own; every benefit is a specific processor doing a
specific conversion.

| Gain | For | Free? |
| --- | --- | --- |
| Audio conversion: `.mp3`/`.ogg`/`.wma` sources to PCM, resampling to one rate, ADPCM compression (about a quarter of the memory for in-memory effects) | sounds | yes, built-in processor |
| Music to the platform codec | music | yes, built-in processor |
| Pre-meshing: ship the mesh, never parse `.vox` at load | models | no: a pipeline extension wrapping our mesher plus a type reader, which is still writing it ourselves inside MonoGame's framework |
| Texture compression, mipmaps, premultiplied alpha | textures | yes, but there are no textures yet |
| Per-platform output from one source | everything | yes, but there is one platform |
| Build-time failure on a missing or corrupt asset | everything | yes; the compiler already gives this for models |

Costs: `.xnb` output is opaque (a `.vox` in the output opens in MagicaVoxel, an `.xnb`
opens in nothing); a changed asset needs a rebuild rather than a file drop, though MGCB
rebuilds only what changed; a pipeline extension is a separate assembly with version
constraints and errors that show only when the build tool runs. And ownership: the
`.mgcb` names every file it builds, one entry each, while the compiler decides the
package contents from the scenes. Two tools deciding the contents of one folder is the
actual sloppiness to avoid.

### Not one or the other

The compiler and the pipeline are two stages with one job each, and the package is the
seam. The compiler translates scenes into what the game needs and writes the package.
If part of the package is an `.mgcb` listing the models and sounds a scene uses, the
client references it as a second content reference beside its own `Content.mgcb` and
MonoGame cooks those assets at build. The server links the zones and the stamp raw, as
now. The ownership problem goes away because the compiler writes both lists. The
scene's label then becomes the asset name: `sound = crackle` loads `sounds/crackle`,
and the manifest holds the label's meaning, not its path.

To check before committing to that: that a client build accepts two content references
with separate output folders (each `.mgcb` carries its own output directory, so it
should); that the stamp still means what it means when cooked output is derived from the
package; and what one changed `.wav` costs at rebuild.

The rule either way: one owner per file. The pipeline owns what only the client uses and
needs processing; the compiler owns whatever a scene can name; nothing goes through
both without the compiler writing the entry. On the raw route, the compiler is the
right place to check a `.wav` header and refuse an odd encoding, the same job it does
for models.

## 5. What MonoGame is doing for us

Counted from the client and `Voxels.Rendering`, the framework is used for five things:
window, input and the fixed-step loop (SDL2 underneath); the GPU abstraction (device,
buffers, render targets, states, shaders compiled per backend); 2D drawing and text;
the math types, by far the most used; and the content pipeline for shaders and the
font. Audio is not used yet.

What it does not do, on purpose: particles, a scene graph, a UI toolkit, animation,
physics, an editor, networking. MonoGame is a framework in the XNA sense, not an
engine. Everything written custom in this repo is on that list, and would have been
bent out of an engine's version instead. The 2026-09-18 transcript's worry was an
engine that eats the project; the custom pieces are the price of not having one.

**How tied we are.** `Core`, `Server`, `Compiler`, `Data` and `Voxels` (the mesher, the
character rig, the `.vox` reader) reference MonoGame nowhere. The dependency lives in
`Client` and `Voxels.Rendering`, about 2,700 lines. That is the size of a port, not the
size of the game. The boundary is worth keeping deliberate.

**Where it could limit us.** The OpenGL backend is a shader-model-3-era feature set;
instancing works, compute shaders should not be counted on without checking the
version. Audio is basic: pan, volume, pitch, simple 3D positioning, one streamed song;
that covers the sound design above but not mixer buses, reverb zones or occlusion, for
which the answer in any engine short of Unity or Unreal is FMOD or Wwise bindings. No
profiler or frame debugger; RenderDoc fills that. Consoles need MonoGame's private
repositories through the platform holders.

**Alternatives, looked at and set aside.** Rolling our own means SDL bindings, an
OpenGL binding, per-backend shader compilation, a sprite batcher, font rasterisation
and audio bindings, to arrive back where we are with nothing custom gone. Godot with C#
would bring audio buses, a UI toolkit, particles and an editor, and cost the renderer,
the UI, the sandbox and a fight over how a voxel world with an external authoritative
server fits its scene model; `Core` would survive. FNA is the same API and a sideways
move. Triggers that would reopen the question: needing compute or GPU-driven
rendering, real audio DSP, a console target, or a UI grown past hand-written buttons.

## 6. How far the renderer goes

### A frame today

One mesh per model, shared by every placement of it. One draw call per placement and
per character part, plus the ground and one buffer for all particles. Every visible voxel
face is two triangles with its shade baked in; flat runs are not merged. One shader,
shader model 3, with sun, fog, and the nearest eight point lights chosen once per frame
for the whole scene. Bloom as a full-screen pass. No frustum culling, no shadows, no
instancing. The package holds 26 models in about 2 MB.

### The target

One scene with 200 buildings and hundreds of scenery objects, 10 to 100 players moving,
emoting and giving off particles, and an FPV drone task that flies a first-person camera
through the same world under its own overlay. About as complex as it will get.

### Where the ceilings are

| Cost | Today | At the target | Ceiling | The fix, in order |
| --- | --- | --- | --- | --- |
| Draw calls | dozens | ~500 placements + 6 per player, ~1,100 | a few thousand per frame on desktop, with overhead | frustum culling, then instancing per model |
| Triangles | small | tens of thousands per building before culling | tens of millions on a discrete GPU, far less on integrated | culling, then greedy meshing in the compiler |
| Point lights | 8 per frame | 200 lamps at night, 8 lit | visible popping, not a crash | pick the nearest lights per draw instead of per frame |
| Particles | one buffer, CPU stepped | a few thousand | tens of thousands | nothing needed |
| Shadows | none | wanted eventually | one extra scene pass | after culling and instancing, since it doubles draws |

Draw calls hit first: per-draw overhead on OpenGL is small but not free, and a thousand
draws with a parameter upload each is where a frame starts to feel it. Culling alone
removes most of a 200 by 200 zone at the walking camera's zoom; instancing then
collapses 200 buildings into one draw per model type and 100 players into one draw per
body part. Triangles are fine with culling, and greedy meshing (merging same-colour
runs into one quad, a pre-meshing step for the compiler) is the reserve. Lights are the
visible limit: eight is a constant in the shader, not a hardware cap, and choosing the
nearest eight per object is cheap.

### The FPV drone

The stress case, and it reorders the fixes. At altitude the whole town is in the
frustum, so culling stops helping and instancing is the lever. Distant buildings become
tiny triangles whose vertex colours shimmer, so a lower-resolution mesh per model for far
draws, which the compiler could produce, keeps the picture clean. The fog already hides
the horizon. Frame rate stability matters more for a drone than a walking camera
because the controls feel every hitch. Nothing about the drone needs a second renderer:
it is the same scene through a different camera with a sprite overlay on top.

### What breaks first is not graphics

A world view carries 43 bytes per visible player, and everyone in one square is inside
one interest radius. A hundred players overflows the safe datagram size several times
over. `MessageSizeTests` carries the note that batching across packets is the planned
answer, and a networking rebuild for large and batched packets is expected shortly. It
comes before any graphics ceiling.

## Agreed, proposed, open

**Agreed on 2026-09-20**

- `effect` and `sound` are separate properties.
- `sound` is read at placement, then layer, then scene; the game assumes nothing about
  how a scene is organised.

**Proposed, not yet agreed**

- `music` as its own property, owned by one thing at a time, with the scene as the
  default owner.
- Sound definitions as assets with a gain and a reference distance, range derived.
- A per-placement `gain` override for sizes, alongside definitions for different
  recordings.
- Whatever a scene can name goes through the compiler; whatever only client code
  triggers is client content.

**Open**

- Where recordings sit while being worked on: `art/sounds/` or a sibling folder.
- Sounds and models through the content pipeline via a compiler-written `.mgcb`, or
  raw-copied like models today.
- Whether you hear another player's jack-in from where they stand.
- Local environment sounds as scene markers from the start, or zone-wide beds only.
- The listener, the voice count and the cut-off floor: numbers to find by ear.
- The order of graphics work when it is needed: culling, instancing, per-draw lights,
  greedy meshing, shadows.
- Landing the package under `Content/Package` rather than mixed into `Content/Zones`
  and `Content/models`, so the two kinds of content show in the folder layout.
