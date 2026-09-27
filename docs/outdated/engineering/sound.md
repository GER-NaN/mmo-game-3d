# Sound

What makes noise in the game and where each decision about it lives, as talked through on
2026-09-20. Built the same day: placed emitters, first pass, the fire pit's crackle, volume
only. `pipelines.md` has the flow. The rest is the shape the other kinds should take so
the first piece does not box them in.

## Four kinds of sound

They split along two questions: does the sound come from a point in the world, and does
the server have anything to say about it.

| Kind | Examples | From a point? | Server involved? | What starts it |
| --- | --- | --- | --- | --- |
| Music | the zone's soundtrack, the terminal's | no | no | zone, hour, whether you are at a terminal |
| Placed emitters | fire crackle, fountain trickle, a humming transformer | yes, a placement | no | being near it |
| Environment | wind, a river, a busy street, crickets, birds | a region, or nowhere | no | zone and hour for the bed; a marker in the scene for a local one |
| Interaction | jacking into a terminal, picking something up | yes, a body | no new messages | a state change the client already receives |

**The server never names a sound.** It owns state, and the client turns state into
sound. This is the art pipeline's rule applied to audio: the scene says what a thing is,
the game says what that means. A server message naming a sound would only be needed for
a one-shot event with no state behind it (an explosion), and none exists yet, so none is
planned.

## Music

*Proposed 2026-09-20, not yet agreed.* Two words, `sound` and `music`, and the difference
between them is behaviour, not where they sit. `sound` is the emitter layer: many at once,
each from a point, each fading with distance. `music` is the one music channel: exactly
one thing owns it at a time.

The scene names its own music, `music = beginner_soundtrack`, as a property of the scene
itself, which the format already carries (`Scene.Properties`, empty today). That is the
default owner of the channel. A placement can carry `music` too, an NPC with
`music = flute_song`: when you come within its range it takes the channel over, the
soundtrack crossfades out, the flute plays, and the soundtrack comes back when you walk
away. So `music` on a placement is not a sound that happens to be musical; it is a thing
that takes over the music while you are near it.

Being at a terminal is client state (`TerminalAccessCache.AtTerminal`) and swaps the
music the same way. Music streams as a MonoGame `Song`, of which one plays at a time, so
an overlapping crossfade is not free: either fade out then in, or hold music as sound
effects in memory. A constraint to settle when the music layer is built.

## Environment

Two layers. A zone-wide bed chosen by zone and hour from a table on the client, which is
how crickets come out at night with no scene work. And local sources, a river or a busy
street, which are placed emitters on a non-drawable layer of the scene: a marker with
`sound = river` and a `radius`, exactly like the spawn marker is a placement with a job
and no model. The compiler warns about a marker on a non-drawable layer that the game
does not read (SC2007), and a marker with a sound is one it reads; the client hears it
as a `SoundMarker` (built 2026-09-24).

## Interaction

Played on transitions of caches the client already keeps: terminal access flipping to
"in" plays the jack-in sound, the inventory gaining a stack plays the pickup. Another
player's body going to a terminal in the world view can play the same sound from where
they stand, through the emitter pass below, or not; undecided.

## Placed emitters

The first piece. A thing standing in the zone gives off a looping sound that is loud
next to it and fades with distance, and a thing moving through the zone carries its
sound with it.

### What a sound is

A **sound definition** is an asset, like a model: a name, a file, how loud it is, how
far it carries, and whether it loops. Definitions live with the art, in
`art/sounds/` as the `.wav` files plus a manifest naming them, and the compiler copies
the ones a zone uses into the package as it does models. Both the client and the compiler
read the same words, so a scene that says `sound = crakle` is reported at compile time
rather than silent in the game. This is the check the `effect` words do not get today.

A definition's numbers are the thing's own nature. A waterfall is loud and a candle is
not, the way a lamp glows in every scene it stands in. So loudness belongs to the
definition, not the scene. Usually it is one number, **level**, from 0 (a fly) to 1 (a
waterfall or a train), which the compiler turns into the two the falloff runs on
(`SoundLevels`): gain rises in a straight line from 0.1 to 1, the reference distance
exponentially from half a metre to sixteen. An entry may set either directly and it
wins; the level fills in what is missing. Built 2026-09-20.

- **gain**: how loud at the reference distance and closer.
- **reference distance**: how close counts as "right next to it", in metres, inside
  which the sound is at full gain.

**Range is derived, not authored.** With an inverse-distance falloff, volume at distance
`d` is `gain * reference / d`, clamped to 1 near the source. The sound is inaudible once
that drops under a floor, so the distance it carries follows from the loudness, which is
how sound works: a louder thing is heard from further away. One fewer number to keep
consistent, and the same floor gives every emitter the same cut-off rule.

### Attaching a sound to a thing

**Read at all three levels. Decided 2026-09-20.** The property `sound` is read at
whichever level it was set: the placement first, then its layer, then the scene. The
game asks a placement "what sound, if any, do you or the things you belong to say", and
never asks which layer it is on. So all of these are the same to the game, and the scene
author picks whichever fits the moment:

- `sound = crackle` on one fire pit.
- `sound = crackle` on a layer of fire pits, so none of them says it.
- A layer called `sounds`, not drawn, holding markers with no model and a `sound` each:
  a river here, a busy street there.

The layer level is inheritance, nothing more. The game makes no assumption about how a
scene is organised; that is the scene author's business.

**`effect` and `sound` are separate. Decided 2026-09-20.** `effect = fire` gives the
embers; `sound = crackle` gives the crackle; a fire pit says both. It is one more word
per thing, and in exchange a fire can be silent, a sound can have no picture, and
neither system reaches into the other.

**The scene says a label, never a file.** `crackle` is a name the sound manifest binds
to `fire-crackle-01.wav`, its gain and its reference distance. Renaming or replacing the
recording touches the manifest and no scene. The editor's vocabulary, the props in
`art/mmo-game.project.json`, offers the labels the manifest defines, which is how it already offers
`fire` and `fountain` for `effect`.

Sizes are the open question. Three ways to say a bonfire is louder than a fire pit:

1. Three definitions, `fire_small`, `fire_medium`, `fire_large`, each with its own gain.
   Simple, but the names multiply, and the three might share one file.
2. One definition and a per-placement override: `sound = crackle`, `gain = 2`. The scene
   already carries a number like this in `sway = 0.3`, so it is not a new kind of thing
   for a scene to say. The definition stays the default and the scene says how this one
   differs.
3. Both: definitions for sounds that are genuinely different (a crackle is not a roar),
   and the override for the same sound louder or quieter.

The third is the recommendation. It keeps definitions honest (a name means one recording
at one natural loudness) and keeps the scene free to say "this fire is bigger" without a
new word.

### Following a moving thing

The emitter pass on the client does not care what a source is. Every frame it is given a
list of sources, each a position and a definition, and it does the same work for all of
them: distance to the listener, volume from the falloff, pan from where the source sits
relative to the camera, and which of them get one of the limited voices.

Static sources come from the dressing once, when the zone loads. Moving sources come
from wherever the position is that frame: a placement on a path is animated by the
client, so its animated position is handed in; an entity the server drives arrives in
the world view every tick, and the client's motion smoothing gives a position every frame
in between. A train on a rail is the first kind and a hacked RC car is the second, and
the emitter pass hears no difference. The sound file is the same loop either way; what
follows the train is the position, not the sound.

### What the listener is

The player's own body, not the camera. The camera sits behind and above and would hear
things the body is nowhere near. Pan comes from the source's offset in camera space, so
a fire to the right on screen is a fire in the right ear.

### Budget

MonoGame sound effect instances are cheap but not free, and a town square full of fire
pits should not open a voice per pit. The pass keeps the nearest few audible sources
(eight, say) on a voice each, with a little hysteresis so a source at the boundary does
not start and stop every frame. A source out of range keeps no voice and costs a
distance check. Each loop starts at a random offset so ten fires do not crackle in step.

### Files

`.wav` for anything positional or short, because a `SoundEffect` is held in memory and
can play many instances at once. `.ogg` for music only, because a `Song` streams and only
one plays at a time. Each pack of sounds carries its licence file, as `src/Client/Content/
Licenses` does for the UI art and fonts.

### What the pieces are

- `art/sounds/*.wav` and `art/sounds.json`: the definitions, with the art.
- Compiler: validates `sound` words against the manifest, copies the used files and
  their definitions into the package.
- The props in `art/mmo-game.project.json`: `sound` and `gain` offered by the scene editor.
- `ZoneConventions.SoundOf`: placement first, layer second, like the other properties.
- Client: a library that loads the package's sounds, and one emitter pass fed by the
  dressing and by whatever moves.
- Server: nothing. It reads the zone file as before and skips what it does not use.

## Is this the right place for it?

The scene editor is generic and freehand, and it should stay that way. A `sound`
property does not change that: the tool still knows nothing about sound, files or the
game. It stores a string under a key, the same as it does for `sway` and `effect`, and
the game is the only thing that reads meaning into it. This is how every generic level
editor that has lasted works: Tiled's custom properties, Unity's tags and Godot's
metadata are all "the editor holds a label, the game decides what it means". The editor
would only stop being generic if it played the sound, listed the files, or validated the
word, and none of that is proposed. Validation belongs to the compiler, which is the
game's.

The line to hold is label versus file. `sound = crackle` is a label the game binds.
`sound = fire-crackle-01.wav` would make the scene depend on the game's asset layout,
and every scene would need editing when a recording is swapped. The manifest is where the
binding lives, so the scene never has to.

There is one other honest place a sound could be attached: on the model, as a sidecar
beside the `.vox`, saying "a fire pit crackles wherever it stands". That is the
MagicaVoxel stage's question (what is this thing) and the `.vox` format cannot carry the
answer, which is why it would need a sidecar. It would save saying `sound = crackle` on
every fire pit in every scene, and the compiler would merge the default into each
placement with the scene able to override. It is not proposed now, because `effect`
already set the precedent that a fire pit's fire is said in the scene, and one mechanism
is enough until the repetition hurts.

## Not decided

- `music` as its own property, owned by one thing at a time (above).
- Do you hear another player's jack-in from where they stand?
- Local environment sounds as scene markers from the start, or zone-wide beds only until
  a river exists?
- The floor below which a sound is cut, and the voice count. Numbers to find by ear.
