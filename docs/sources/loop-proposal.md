# A loop, proposed before reading the design

Written on 2026-09-19 by Claude, on purpose before reading Gerald's world document and
mechanics, so the two can be compared. It comes out of a day spent on the graphics
side: wind, moonlight, a character rig, small effects, and the pipeline that carries a
scene from the editor into the game. It is an outside view of what the code says the
game is, and what I would build on it first.

## What the code says the game is

A voxel town you walk around in. Computer parts lie on the ground: GPU cores and RAM
sticks in four tiers. You carry a device, from a phone up to a data centre, or you sit
at a fixed terminal, and either one takes you into a "terminal world" the code names
but does not yet contain. You can form a party, chat, and come back tomorrow as the
same person holding the same things. Bots exist that wander, collect and join
parties, so the town can be busy on demand.

So there are two spaces, a physical one and a terminal one, and a device is the door
between them. Everything below follows from taking that seriously.

## The loop I would build

One loop, ten minutes long, that a new player can finish without being told anything:

1. **Walk and pick up.** You spawn in the town with a phone. Parts glint on the ground
   (the glow and bloom from today are the hint). You walk over three of them.
2. **Build.** A workbench stands in the town, a placement with a `workbench` marker.
   At it, parts become a better device: two Standard cores and a stick make a laptop.
   Sparks and a little smoke while it happens (the effect system, an `effect` on the
   bench that runs while it is in use).
3. **Go in.** You use the device. The terminal world is a second zone, built with the
   same editor and compiler as the town, themed as the inside of a machine: circuit
   board ground, capacitor towers, heat-sink walls. Better device, deeper you may go:
   a phone reaches the first board, a laptop the second. Parties go in together and
   see each other there.
4. **Bring something back.** Inside, you find what does not lie on the town's ground:
   Enhanced and Advanced parts, and one thing that is not a part at all, a *fuse*.
5. **Change the town.** A fuse fitted to a street lamp lights it. Lamps are dark when
   the world is new. Every lamp a player lights stays lit for everyone, at night, from
   then on (the `light` property, switched on by state rather than always). A town
   that lights up as its players progress is the whole point of a shared world in one
   picture, and it is cheap: a point light and a flag.

Then around again, for a gaming rig, a deeper board, and the next dark street.

## Why this shape

- **It uses what exists.** Items, tiers, devices, terminals, parties, persistence and
  bots are all built. The loop adds a workbench, a second zone, one new item kind and
  one flag on a lamp. Nothing is thrown away.
- **The terminal world reuses the whole pipeline.** It is a scene in the editor, with
  its own models, compiled into the same package, drawn by the same renderer. One
  engine, two skins. The alternative, a separate UI-driven "hacking" layer, is a
  second game to build and a second thing to make fun.
- **Progress is visible to strangers.** Lamps lighting up, later shop windows and signs
  (the emissive materials from today's TODO), mean a player sees what others have done
  without a leaderboard. That is the reason to make an MMO rather than a single-player
  town.
- **The graphics work of today becomes feedback, not decoration.** Effects mark a
  bench in use and a device overheating. Wind and moonlight make night the time the
  lamps matter. The rig gives a sit-at-terminal pose and a pickup reach, which is how
  the player learns what is usable without a tooltip.

## What today's graphics work makes possible

This is the part only this session can write. Each thing built today is a capability,
and each capability suggests mechanics that would have been out of reach yesterday.

**Wind (`sway`) is weather.** It is one number per placement today, and one global
number tomorrow: a wind strength the server sets. Calm days, gusty nights, a storm
before something happens in the terminal world. Weather that everyone sees at once is
shared state at almost no cost, and it is the cheapest "the world is alive" signal
there is. A storm that bends every tree in the forest tells a player something is
coming without a message box.

**Moonlight and the clock are a schedule.** The world runs on real New York time. Night
is now legible, so night can mean something: the lamps matter, the terminal world could
be reachable only after dark, or shops could close. A daily rhythm that players share
is a reason to log in at a time, which is a reason to meet other players.

**The character rig is a language.** A pose is a state everyone can read: sitting at a
terminal, reaching for a part, carrying something heavy, waving. Multiplayer games
spend UI on telling you what other players are doing; a rig tells you by looking. The
first poses to add are sit, reach and carry, because those are the loop's verbs.

**Effects are status.** Fire and water were the proof; the use is feedback. A bench in
use sparks. A device near its limit smokes. A part on the ground glints. A player who
just came back from the terminal world trails a few sparks for a minute, so others see
who has been in. Every one of these is an `effect` on a placement or an entity, with
the numbers in one class.

**Lights are progress.** Once a lamp lights because of a property rather than a name,
the property can be a flag the server flips, and the town's lighting becomes a record
of what its players have done. Nothing else in the game shows collective progress as
plainly or as cheaply.

**The package stamp makes content a release.** Because the server refuses a client on
another compile, the art can change under a live game safely: compile, rebuild, and
every client knows to update. That turns the scene editor into a live-ops tool. A new
street, a seasonal decoration, a repaired building after an event, all shipped as a
package rather than a code change.

**The render tool is a test.** Every claim about the screen today was settled by a
picture from the game's own renderer, and twice the picture disagreed with the
reasoning. Rendering the zone from a known camera and comparing it to the last known
good picture is a regression test for a whole class of bugs, and it exists now.

What all of this has in common: the renderer stopped being a thing that draws models
and became a thing that draws state. That is the shift a game needs before it has
mechanics, because a mechanic nobody can see is not a mechanic.

## What I would not build yet

- More zones than two. One town, one board, until the loop is fun.
- Combat. Nothing in the code wants it, and the terminal world can be a place you
  explore under a timer or a heat gauge rather than a place you fight.
- Trading, crafting trees, economy. A workbench with three recipes is enough to prove
  the loop; the recipe table can grow without the code changing.
- Real auth, characters, multiple licences. The identity doc lists them; none is on
  the path to the first fun ten minutes.

## How to know it works

Give a bot the loop as a script and watch it: spawn, collect three, build, enter,
return, light a lamp. When a bot can do it in ten minutes, hand it to a person. If the
person asks "what do I do" at any step, that step needs a hint in the world, not a
tutorial. The bots already exist for interest management; this is what they are for
next.

## What I most want to know from the design

Whether the terminal world is meant to be a place or an interface. Everything above
assumes a place. If it is an interface, a screen you operate, then the physical town
is the hub and the interesting question becomes what the town does while you are
inside, and the proposal changes from a second zone to a second view.
