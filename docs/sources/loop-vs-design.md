# The loop proposal against the design

Written on 2026-09-19, after `loop-proposal.md` was committed and then the design folder
(`docs/design/`) was read for the first time. Three parts: where the proposal and the
design agree, where they differ and which should win, and what the design asks of the
graphics that today's work does and does not give it.

## Where they agree

- **Two worlds, one character, a device as the door.** The proposal read this off the
  code; the design says it in so many words, with the hardware ladder as the power
  curve. No change.
- **A world that remembers.** The design wants Minecraft-style persistence where
  changes stick and strangers see them. The proposal's lamps that stay lit are the
  smallest possible version of that, and the design's "small wins along the way",
  resolving a local disruption for an immediate upgrade and local reputation, is the
  same loop with the design's own words on it.
- **Mock first, mechanics before art, iterate graphics in a sandbox.** The design says
  this three times across the documents. It is how the last two weeks went.
- **No combat system.** The design rules out turn-based combat and wants environmental
  threats instead. The proposal said no combat yet. Same place.
- **Two zones, then stop.** The design's Training Grounds, a self-contained starting
  area you leave once and never return to, maxing it an achievement in itself, is the
  starter town. That reconciles the proposal's "one town, one other place" with a
  design that spans a continent: the town is not a placeholder to grow out of, it is
  the first canonical zone, and it is allowed to be finished.

## Where they differ, and what should win

**The terminal world is an interface, not a place.** The proposal assumed a second
voxel zone themed as the inside of a machine. The design is explicit: walking up to a
terminal opens a separate screen with its own art style, and what is behind it is
tasks, the first of which is Agent Defense, a one-minute rhythm game whose fiction is
cutting or restoring the electrical grid, scored into GPU units, with a top-ten
leaderboard and a personal best. The design wins here, for three reasons: it is what
the author wants; it is far cheaper than a second zone; and a deterministic one-minute
task is something a bot can play and a leaderboard can rank, which the design cares
about and a voxel dungeon cannot give. The proposal's "circuit board zone" is not
wrong, it is later: a network space that is a place can be added behind the same
terminal without changing how you enter, once there is a reason.

**Reward and progress come from the design's currencies, not a new item.** The
proposal invented a fuse. The design already has GPU units as the mini-game's payout
and hardware tiers as progress. The lamp can stay: it is the visible result, not the
reward.

**Threats are dressing with behaviour, not enemies.** The design's real-world danger
is hacked drones, RC cars and vehicles. In pipeline terms those are placements with
paths, a rig for wheels or rotors, and an effect for sparks, all of which exist or are
one step away. They are not a combat system and should not be built as one.

**The proposal was too small in one direction.** It had no answer for the things the
design treats as pillars: mini-games with leaderboards as their own layer, the Prompt
as a year-long collective campaign, the uniquely-human pillar and player-submitted
media, the Runner escort mechanic, three currencies, espionage. None of them belongs
in the first loop, but the first loop should not make any of them harder. The one
below does not.

## The loop, merged

Take the proposal's shape and the design's parts, and the first loop writes itself
from things that already exist plus one mini-game:

1. **A street is dark.** The rogue AI cut its power. At night the moonlight shows a
   lit street next to a dark one, and the difference is the assignment. (Lamps off by
   server state: the `light` property from the TODO, switched by a flag.)
2. **Find the junction box.** A terminal placement at the end of the street, named as
   such in the scene. Walk up, use it. (Terminals exist; the terminal-use flow exists.)
3. **Run the task.** Agent Defense: one minute, four or five lanes, deterministic after
   its seed, scored server-side. In-fiction it is restoring the grid. (New: the one
   piece of real new code in the loop.)
4. **The street lights up, for everyone, from then on.** The lamps' flag flips and
   persists. A player arriving tomorrow sees a lit street and does not know who did
   it, which is the design's persistent world in one picture. (Persistence exists;
   the flag is small.)
5. **Get paid in GPU units, climb the ladder.** Score converts to GPU units. A laptop
   unlocks streets whose junction boxes refuse a phone, and harder charts. (Items and
   tiers exist; the gate is small.)
6. **Look at the board.** Top ten for the task, and your own best. (New, small, and it
   is the design's leaderboard layer started.)

That is the Training Grounds: a town with, say, eight dark streets, and leaving it
once every street is lit is the achievement. A bot can run the whole thing, which is
how to know it works before a person tries it.

## What the design asks of the graphics

The 2026-09-18 transcript lays out the fear list for staying on MonoGame: instancing,
culling, lighting, shadows, skinned animation, and an engine that would eat the
project. One day later, here is where each stands, because the design's needs can now
be checked against a renderer that exists rather than one that was feared.

- **Camera swapping for FPV drones, RC cars, CCTV and cinematic orbits.** The world
  is real 3D geometry with a perspective camera, which is the one thing the transcript
  said was required. A different view is a different view matrix. The renderer, the
  models, the lighting and the effects do not change. This is the biggest thing the
  design gains from the voxel decision, and it is already true.
- **Blackouts and grid sabotage** are the `light` property driven by server state, and
  moonlight is what makes a dark street legible instead of black. Built today, except
  the property.
- **Hacked billboards that blind** are the emissive material plus bloom with the
  boost turned up. A flare is a number change on a sign. Built, pending the material
  work in the TODO.
- **Weather synced to the real world** is sway strength and a lighting preset chosen by
  the server, on top of the real-time clock that already runs. Built, minus the server
  choosing.
- **Hacked drones and RC cars as threats** are dressing on paths with a rig and an
  effect. Paths, rig and effects exist; a drone model with rotors as named parts is
  the art rule from `animation.md`.
- **A hundred players in a town square.** Each is six draws with the rig, so six
  hundred draws plus the town, which a modest GPU handles, and rigid parts sidestep
  skinned instancing entirely, which the transcript rightly called the hard one. If it
  ever matters, instancing the parts is the next step, not a rewrite.
- **Not done: frustum culling and shadows.** Neither matters at one zone's scale. Both
  are known, ordinary work when they do.
- **The one real gap: textures.** The renderer draws vertex colours only. The design's
  player-submitted video on in-game TVs, player art as unique items, and billboard
  content all need an image on a surface. That is a textured-quad path in the shader
  and a way to get an image into the package. It is not hard, but nothing built so far
  touches it, and it should be the first graphics feature added when the
  uniquely-human pillar comes up.

## What to do next

Build the merged loop, in this order: the `light` property and dark streets; Agent
Defense as a screen behind the existing terminal flow, deterministic from a seed and
scored on the server; the flag that lights a street; GPU payout; the leaderboard.
Then let a bot run it. Everything in the design that is not on that list stays in the
design until the loop is fun.
