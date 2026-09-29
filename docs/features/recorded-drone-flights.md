# Recorded drone flights

**Date:** 2026-09-29
**Status:** Agreed (2026-09-29). The build has not started.
**Sources read:** docs/world.md sections 1, 2, 4, 5, 6, 7, 8; TODO.md (first playtest,
2026-09-26); game/drones/Drone.cs, game/server/world/ServerDrones.cs,
src/Rules/Terminals/TerminalApps.cs, src/Rules/Skills/SkillId.cs
**Build from:** the two Outcome sections and the T-list of tests. The Q&A is the
record of how they were reached.

A drone's flight, recorded, becomes a drone's pattern in the game. Raised in the first
playtest; designed now because the author picked it as the next thing to build.

The answers are the author's own words, typed in conversation and recorded verbatim or
near it, one question at a time. Model additions are set apart and labelled:
"Consequence noted", "Options offered", "Note".

## Already decided

Decisions in force that this feature must fit, or change on purpose.

- The rogue AI's drones are part of the fight; the town's people are oblivious to the
  drone battles. [Q16] (§1)
- World events: the Drone Swarm, drones appearing in a zone (the meadows first) in a set
  number; its settings and kickoff are a record the server reads. [C-2026-09-27] (§2)
- The terminal shows locked apps with a notice, so a player sees FPV drone surveillance
  before they can use it. [Q33] (§4)
- Defense Objectives include operating the CCTV camera to spot enemy drones [Q8], and,
  as a concept, piloting a first-person-view drone around the physical world for
  observation [B]. (§5)
- The look is customisable, drone colours included. [Q27] The Drone Operator career:
  carry extra drones, better drone controls, extra drone abilities. [C-2026-09-26] A
  "drone control" skill is named. (§6)
- Personal drones are part of the rig; defence drones and upgrades are an equipment
  line; drones are companions with specialties (defence, attack or anti-drone, support,
  observation); autonomous protection drones defend a body online, mid-game. [B, Q16,
  Q12, Q5] (§7)
- Drones do harm as environmental danger, not a battle system; no turn-based combat.
  [Q1, B] (§8)

Built:

- `Drone` (`game/drones/Drone.cs`): a placeholder look from boxes. It flies a slow circle
  (radius 3 m, 3.5 m up) round its pair's centre. The server flies it; a client flies
  the same circle from the synced centre, phase and flight time. An EMP pulse knocks it
  down; it falls, lies a moment, and is gone.
- `ServerDrones`: over the town a pair spawns whenever it has none, circling a spot over
  the streets, kept clear of buildings; elsewhere drones come only as a world event's
  swarm. It zaps players near it (7 m, 10 HP every 4 s). The EMP Emitter's pulse reaches
  10 m. The town's cameras pay $3 for a reported drone.
- The town cameras app (`CctvView`) and the Drone Swarm world event (`ServerWorldEvents`).
- No drone a player flies. The "FPV drone surveillance" app is locked: "Locked. You need
  a drone first." (`TerminalApps`). The skills built are Agility, Hacking, Workbench,
  Field repair, Electrical repair and Gardening; no drone skill yet.

Deferred:

- The FPV drone objective, personal and companion drones, the Drone Operator career
  (world.md, not built).
- TODO.md's playtest notes: "Drone flight seems stuttery" (since made smooth), and this
  idea, marked for a design session.

## Feature design

### Developer thoughts

> First playtest, 2026-09-26: "I would like to be able to record drone flight and then
> thats their pattern in game."

### Q&A

**F1.** Who records a flight: players in the game, or you as the game's author?

> Answer (2026-09-29): I record the flight, this will be two things. Either a drone path
> around a known town/scnene that the drone flies on and it just loops or two. A
> freeform set of "stances" not sure of a good name, but essentailly moves that the drone
> does while in free flight.
>
> Consequence noted: recording is an authoring tool for the author, not something
> players do. It makes two kinds of thing: (1) a path tied to one known zone, which a
> drone flies and loops; (2) moves not tied to a place, which a drone does while it
> flies free. The circle drones fly today is one fixed case of each: a loop round a
> centre.
>
> Options offered (the name was left open, "not sure of a good name"):
> - "Manoeuvre" (in code `Maneuver`, as the code spells Color): the ordinary word for a
>   move an aircraft makes on purpose; reads well beside "path".
> - "Move": plain, but also what a walking body does.
> - "Trick": playful, suits a flip or a roll, less so a hover or a strafe.
> - "Routine": a sequence of moves, more than one move.
>
> The model would take "manoeuvre", with "path" for the looped kind: two words for the
> two things, neither used in the code today. Decision: pending the author's word.
>
> Decision (2026-09-29): yes, drone path will be of the form "Fly around this building,
> go over to X, flow low for a little and back to the building"... maneuvers will be
> shorter and sweet like "Hover in Circle Rotation", "Dive bomb the ground to about the
> player height", "Climb up and Back Down", "Zig Zag approach towaards a player height".
>
> Consequence noted: the names are "drone path" and "maneuver" (the author's spelling).
> A path is long and placed: it goes round named things in one zone (a building, a spot
> X) and loops. A maneuver is short and not placed, and some are relative to something:
> "to about the player height", "approach towards a player", so a maneuver has a target
> or a reference (the ground, a player), not fixed coordinates.

**F2.** How do you fly the drone while you record?

> Answer (2026-09-29): I will use a controller with predicatable known flight controls
> for a drone. I dont know how we technically record this yet but im sure we can figure
> it out.
>
> Consequence noted: recording is flying a drone by hand with a game controller, with
> the controls a drone pilot would expect (the game already plays with an Xbox-layout
> pad for walking). How the flight is captured and stored is for the technology fork.

**F3.** Which of the game's drones fly a drone path?

> Answer (2026-09-29): I think we need a new concept for drones. Every N minutes a drone
> appears in a zone that does spying, it will follow the path.
>
> Consequence noted: a new kind of drone, a spying drone. Every N minutes one appears in
> a zone and flies that zone's drone path. It is not the town's circling pair
> (`ServerDrones`' patrols) nor a world event's swarm, which stay as they are unless
> said otherwise.

**F3a.** Follow-up: What does a spying drone do to the players it spies on?

> Answer (2026-09-29): Nothing for now, there are some cool features that can build on
> this in the future (like a player that is spotted can be announced, if the AI is
> looking for a specific high level player for some game mechanic the can be "Found")
> etc... and achievements for Incognito, be a player that is least spotted by spy drones
>
> Consequence noted: a spy drone has no effect on play yet; it flies its path and is
> seen. Designed toward, not built: a drone "spotting" a player (announced; the AI
> "finding" a player it looks for; an "Incognito" achievement for the least spotted).
> The name the author uses is "spy drone".

**F4.** What can a player do to a spy drone?

> Answer (2026-09-29): Players can Spot via cameras and also use weapons.
>
> Consequence noted: a spy drone is reported on the town cameras and brought down by
> weapons, as the other drones are today (the cameras app, the EMP Emitter).

**F5.** When does a drone do a maneuver?

> Answer (2026-09-29): So I think we also have roaming drones that attack players like
> they do today but their circle animations become maneuvers
>
> Consequence noted: two kinds of drone. Spy drones fly a zone's drone path and do
> nothing to players (F3, F3a). Roaming drones are today's attacking drones: they zap
> players as now, and in place of the fixed circle they fly maneuvers. The model reads
> "roaming" as both the town's pair and a world event's swarm, the drones that attack
> today.

**F5a.** Follow-up: How does a roaming drone choose which maneuver to do next?

> Answer (2026-09-29): Random, lets use the circular hover as default and it will pick a
> maneuver every so often
>
> Consequence noted: a roaming drone hovers in its circle by default (today's flight),
> and every so often does a maneuver picked at random, then goes back to the circle.
> How often is a placeholder number.

**F5b.** Follow-up: A maneuver such as "Zig Zag approach towards a player height" needs a
player: which player does it go for?

> Answer (2026-09-29): Random pick near it.
>
> Consequence noted: a random player near the drone. With nobody near, the model reads
> that the drone picks only among maneuvers that need no player.

**F6.** What does a player see that tells a spy drone from a roaming drone?

> Answer (2026-09-29): Nothing, they will be able to spot behaviors and be able to
> guess, this one flying high away will probably not attack me.
>
> Consequence noted: one look for both kinds. A player tells them apart by behaviour: a
> spy drone keeps to its path, high and away; a roaming drone circles and comes at
> players.

**F7.** How long does a spy drone stay in its zone?

> Answer (2026-09-29): Forever, observing
>
> Consequence noted: a spy drone loops its path until a player brings it down (F4).
> With one appearing every N minutes (F3), a zone would gather more and more of them.

**F7a.** Follow-up: One appears every N minutes and each stays forever: how many can a zone
hold at once?

> Answer (2026-09-29): Lets start with 2 for now. Players can kill them if they get close
> enough
>
> Consequence noted: at most two spy drones in a zone; below two, a new one appears
> every N minutes. A path should come within a weapon's reach somewhere (the EMP reaches
> 10 m today), as the author's own example does ("flow low for a little"), or no player
> can bring its drone down.

**F8.** How many drone paths can one zone have?

> Answer (2026-09-29): Multiple

**F8a.** Follow-up: When a spy drone appears, which of its zone's paths does it fly?

> Answer (2026-09-29): random pick
>
> Consequence noted: a zone with no drone path gets no spy drones.

**F9.** Where do you do the recording: in the running game, or somewhere else?

> Answer (2026-09-29): Yes I think in the running game, we need a specail mode or
> something where I start in Zone X with a camera attached and my path is tracked, I
> guess we need a recording sequence to capture my locations, so lets use chat for now
> when I type start the path recording starts, when i chat stop the path recording
> stops
>
> Consequence noted: a recording mode in the running game: the author starts in a zone,
> flies as a drone with a camera on it, and the flight is tracked between a "start" and
> a "stop" typed in chat. The game's chat commands begin with "/" today (emotes such as
> "/wave", "/p " for the party), so the model reads "start" and "stop" as commands in
> that form, not lines everyone sees. A mode only the author can reach: players never
> see it.

**F9a.** Follow-up: When you record a maneuver, what stands in for the player it goes for?

> Answer (2026-09-29): Good point, i will need a dummy player that stands still I
> guess, I half though about building a seperate app just to record these things but
> thats probably not worth anything
>
> Consequence noted: recording a maneuver places a dummy player that stands still, and
> the flight is kept relative to it (and to the ground), so it can be played towards
> any player. The recording stays in the game; a separate app is not worth it.

**F10.** How do you name a recording, and say whether it is a drone path or a maneuver?

> Answer (2026-09-29): Hmm, I guess that depends how we setup the recording
> architecture. I will provide a type and name and I think we also need to say what
> zone its in etc... So maybe each recording gets a config. well the recording is not
> the final output right. The recording is something we build a path from, that path
> gets the type, zone, name, etc..
>
> Consequence noted: two stages. A recording is the raw capture of a flight. A drone
> path or a maneuver is built from a recording, and it carries the type, the name and,
> for a path, the zone. How that is set up is for the technology fork.

### Outcome

- Recording is the author's authoring tool, not a player feature. [F1]
- It makes two things, named "drone path" and "maneuver" [F1, decision]:
  - A drone path is long and placed in one zone ("Fly around this building, go over to
    X, flow low for a little and back to the building") and loops. A zone can have
    several. [F1, F8]
  - A maneuver is short and not placed ("Hover in Circle Rotation", "Dive bomb the
    ground to about the player height", "Climb up and Back Down", "Zig Zag approach
    towaards a player height"); some are relative to a player or the ground. [F1]
- The author flies by hand with a game controller, with the controls a drone pilot
  expects. [F2]
- Recording happens in the running game, in a mode only the author reaches: start in a
  zone, fly as a drone with a camera on it, and a chat "start" and "stop" mark the
  flight. [F9]
- A maneuver is recorded against a dummy player that stands still, and kept relative to
  it. [F9a]
- A recording is raw; a drone path or a maneuver is built from it and carries a type, a
  name and, for a path, its zone. [F10]
- Spy drones, a new kind: in a zone with drone paths, a new one appears every N minutes
  while there are fewer than two, flies a path picked at random, and loops it until a
  player brings it down. It does nothing to players. [F3, F3a, F7, F7a, F8a]
- Players report spy drones on the town cameras and bring them down with weapons. [F4]
- Roaming drones are today's attacking drones: they zap players as now, circle as
  their default, and every so often do a maneuver picked at random; a maneuver that
  needs a player goes for a random player near it. [F5, F5a, F5b]
- Spy and roaming drones look alike; a player tells them apart by what they do. [F6]

Deferred:

- What spotting a player does: announced, "found" by an AI that looks for a player, an
  "Incognito" achievement for the least spotted. [F3a]
- N, the maneuver interval and the like are placeholder numbers. [F3, F5a]
- A separate recording app: not worth it. [F9a]

## Technology design

### Developer thoughts

> Recording, I launch the game in a special mode "Drone Recording" with a start zone. I
> am placed into that zone at startup with a flying capabilities and a camera so I can
> see what I am doing. I type start in chat and the recording starts, i do the flight
> path and say stop. Then thats the path. Then we translate the recording into
> something the game can use as a predefined flight path for a drone. (Flight path
> serves both purposes, a spy goes on a long path, a maneuver is a short path). Then yea
> a way to identify the paths, zones, type (maneuver vs long form) and name.
>
> Consequence noted: one concept underneath, the "flight path", with a type: a long one
> (a spy drone's, placed in a zone) or a short one (a maneuver). A recording mode is a
> way of launching the game, as the developer launch options are today.

### Facts held in mind

What the code does today for the pieces this feature touches:

- **A drone's flight is a formula.** `Drone` (`game/drones/Drone.cs`) sits at
  `Center + (cos, height + bob, sin)` of an angle that grows with flight time from a
  start `Phase`. The server works it out each tick; a client works out the same circle
  from the synced `Center`, `Phase` and `Flown` (flight time), leaning its clock towards
  the server's, so it moves smoothly between updates. `NetPosition` and `Down` are synced
  too; a downed drone falls to the ground.
- **The server owns drones.** `ServerDrones` spawns them, flies them, zaps players,
  takes EMP hits, and pays for camera reports. Town patrols come in pairs; a world
  event's swarm comes from `ServerWorldEvents`.
- **Where data lives today.** World event definitions are rows in Postgres
  (`world_event_definitions`, read by `WorldEventStore`). Shops, items and skills are
  C# in `src/Rules`. Zones and their placed things are scenes (`game/zones/<id>/`).
- **Developer launch modes exist.** `LaunchOptions` has `--creator` (open the character
  creator at start), `--garden` (open the potting table at start) and
  `--show-characters`, marked "dev". A recording mode would be one more.
- **Chat commands are read on the client.** `ClientGame.OnChatSubmitted` turns "/wave"
  and the other emotes into gestures and "/p " into party chat before anything is sent;
  the rest goes to the server as a line everyone hears.
- **A controller already plays.** An Xbox-layout pad walks, turns, looks and zooms
  (`Player.ReadInput`, `ChaseCamera.ReadPad`). There are no actions for flying yet.
- **No player flies.** Bodies walk and jump; nothing in the game flies but drones, and
  no drone takes input.

Live in play: drones, their circles, EMP, cameras, world events. Scaffolding nothing
reaches: none for this feature.

### Q&A

**T1.** Where does a built flight path live?

> Answer (2026-09-29): I think with the drone code, it lives wherever the current drone
> paths live now.
>
> Note: today's drone flight is not stored anywhere. The circle is a formula in
> `Drone.cs` (its radius and speed are constants there); `ServerDrones` picks the centre
> and the start phase when it spawns a pair, and syncs them. A recorded flight path is
> data, a list of positions over time, so it needs a home the circle never needed.
>
> Options offered (a reading, since the premise was not so):
> - **A file per flight path in the repo**, beside the drone code
>   (`game/drones/paths/`), as a Godot resource. The server and the clients are the
>   same Godot project and load the same files, so a client can fly a path from the
>   file as it flies the circle from the formula today: only which path and when it
>   started are synced, not the positions. Versioned with the code and the zones it
>   belongs to, and open in the editor.
> - **Rows in Postgres**, as the world event definitions are. The server can read them,
>   but the clients do not have them: the positions would be sent over the network, or
>   the clients would read the database, which the author wants less of, not more.
> - **Generated C#** in `src/Rules`. Testable, but a path of hundreds of points as code
>   is hard to read and to replace.
>
> The model would take files in the repo: the nearest to "with the drone code", and the
> one that keeps a path's flight a local calculation on every machine, as the circle
> is. Decision: pending the author's word.
>
> Decision (2026-09-29): yes option 1.
>
> Consequence noted: a flight path is a file in the repo under `game/drones/paths/`, a
> Godot resource. The server and every client load it; what a drone syncs is which path
> it flies and when it started, and each machine works out where it is.

**T2.** Besides where the drone was, what must a flight path keep from your flight?

> Answer (2026-09-29): I think thats all, the path I took and the speed at which I moved
> but I guess thats confgiuration, if we get faster drones in the future it wont matter
> the path, the drone will run along the path at waheever speed it has... so I think
> just paths.
>
> Consequence noted: a flight path keeps only its shape, the line flown. Speed belongs
> to the drone: a drone runs along a path at its own speed, so a faster drone later
> flies the same paths. The timing of the recorded flight is not kept (a dive is as
> fast as the rest). A maneuver's shape is kept relative to the dummy player it was
> recorded against (F9a).

**T3.** While you record, should your drone be stopped by buildings and the ground?

> Answer (2026-09-29): Yes collisions should exist, unless thats reallyhard. I think I
> also need a way to restart the recording. So maybe also a restart keyword in chat.
>
> Consequence noted: not hard: the recording drone is a physics body that slides along
> what it meets, as a player's body does. So a recorded path does not pass through a
> building, and a spy drone flying it does not either. A third chat command, restart,
> throws the flight so far away and records again; the model reads it as starting over
> from where the drone is now.

**T4.** When you stop, how do you give the flight path its name and its type?

> Answer (2026-09-29): So lets do this, before the flight we require a file
> "flightpath.path" or whatever its a json format and has the required props
> - name
> - type (zonepath | maneuver)
> - zone (zone name if applicable)
> - recordedpath {xyz, xyz, xyz}  <-- This probably needs sampled and allow the in game
>   drones to follow it as best possible.
>
> Then when we run the custom record startup it takes the file as input
>
> Consequence noted: the name, type and zone are written before the flight, in a JSON
> file that is the recording mode's input. The recording fills in the positions. Types
> are "zonepath" (a spy drone's path, in the zone named) and "maneuver" (no zone). The
> positions are sampled as the drone flies and thinned to points a drone can follow
> smoothly at its own speed (T2). ".path" is a file Godot does not import; the game
> reads it as text, and a release export must be told to include it (a shipping step,
> docs/planning/shipping.md).
>
> Note: differs from the reading chosen in T1, which named a Godot resource. A JSON file
> in the repo keeps what T1 was for: one file per flight path, beside the drone code,
> loaded by the server and every client. The model reads JSON as the author's choice
> of format, replacing "Godot resource". Pending the author's word.
>
> Decision (2026-09-29): the JSON idea was for the recording part. We can compile into
> a Godot resource if needed for the game. There are two big parts here (Recording the
> path and documenting it AND then whatever the game needs to reproduce the flight).
>
> Consequence noted: two parts. (1) Recording and documenting: the JSON file, written
> by hand before a flight (name, type, zone) and filled by the recording. (2) What the
> game flies from: whatever form reproduces the flight best, built from the JSON if a
> different form is needed. T1's decision stands for part 2 (a file in the repo,
> loaded by the server and every client).
>
> Options offered (on "if needed"):
> - **The game reads the JSON as it is.** One format, no build step; the server and
>   clients parse it when a zone loads. Enough to fly a drone along the points.
> - **A Godot resource built from the JSON** (a curve, `Curve3D`). Worth it if the
>   paths should show in the editor inside their zone, where the author places things,
>   and Godot's own curve sampling does the smoothing. It costs a build step to keep in
>   step with the JSON.
>
> The model would start with the game reading the JSON, and build the resource only
> when seeing the paths in the editor is wanted. Decision: pending the author's word.
>
> Decision (2026-09-29): A
>
> Consequence noted: one file per flight path, the JSON the recording writes; the
> server and the clients read it directly. No build step. A Godot curve resource stays
> an option for when the paths should show in the editor.

**T5.** A maneuver was recorded around a dummy player standing in one spot. In the game,
a roaming drone plays it somewhere else, near a real player. Where does the maneuver
start: at the drone's position, or at the spot matching where you started, next to that
player?

> Answer (2026-09-29): So maneuvers are different than a spy path, they need to have a
> focal point, they also dont have an absolute position system. they have a relative
> postiion system (they need to execute in the oldtown street and in the grassy area
> behind the college, etc...)
>
> Consequence noted: a maneuver's positions are kept relative to its focal point: the
> dummy player when recorded, the target player when played. The model's reading of the
> rest, for the author to correct:
> - The maneuver is placed around the target player, and turned about the upright line
>   through that player so that its first point lies on the side the drone is on.
> - The drone flies a short straight hop from where it is to the maneuver's first
>   point, then flies the maneuver, then goes back to its circle.
> - A maneuver with no player in it ("Hover in Circle Rotation", "Climb up and Back
>   Down") takes the point under the drone, on the ground, as its focal point.
> - A maneuver recorded in the open may meet a wall where it is played. The drone is
>   kept clear of what is solid, as today's circle is, and the maneuver may be cut
>   short there.

**T6.** When you record a maneuver, where does the recording mode put the dummy player?

> Answer (2026-09-29): anywhere in open space, we can use a custom zone if needed that
> is flag ground with nothing in it.
>
> Consequence noted: since a maneuver is relative, where it is recorded does not
> matter; open ground with nothing in the way is best. If no zone has such ground, a new
> recording zone: flat ground, nothing on it, no doors, so no player ever reaches it.
> The T5 reading stands (not corrected).

**T7.** "Predictable known flight controls" for a drone on a controller: which layout do
you mean?

> Answer (2026-09-29): an xbox, I just need a simple driving layout with height and the
> ability to move around and direct a flying entity
>
> Consequence noted: the model's reading of "a simple driving layout with height", for
> the author to correct: the left stick moves (forward, back, sideways); the right stick
> turns left and right; the right trigger climbs and the left trigger sinks. In the
> world the triggers zoom the camera (`ChaseCamera`); in the recording mode they mean
> height instead.

**T8.** When you say stop, does the recording write the positions into the same file you
started with, or into a new file?

> Answer (2026-09-29): Why not the same file
>
> Consequence noted: the same file. "stop" writes the positions into the file the mode
> was started with, replacing any positions it held; git keeps the earlier take.

**T9.** Does the recording mode need the game server running, or does it work on its own
with only the zone loaded?

> Answer (2026-09-29): I dont know, does it?
>
> Options offered (asked for). The facts: the recording needs the zone's scene (its
> ground, buildings and their collision), a flying body, a camera, the three chat
> commands, and a file to write. All of that is on the client: a zone is a scene file
> the client can load itself (`World.LoadZone`), chat commands are read on the client
> already, and the file is local. What the server adds is the live world (other
> players, drones, the clock), none of which a recording uses.
> - **On its own (no server).** The mode loads the zone named in the file and puts the
>   drone in it. Starts at once, no account, nothing to log in to, and nothing a bug in
>   the mode could do to the live world.
> - **With the server.** Log in as a player, then the mode swaps the body for a flying
>   drone. More parts to build (the server must allow the swap, and only for the
>   author), for no gain the recording needs.
>
> The model would take "on its own". Decision: pending the author's word.
>
> Decision (2026-09-29): onits own

### Outcome

From the answers:

- One concept, the flight path, of two types: "zonepath" (a spy drone's, placed in one
  zone) and "maneuver" (short, relative to a focal point). [developer thoughts, T4]
- A flight path is one JSON file in the repo, under `game/drones/paths/`, holding
  `name`, `type`, `zone` (zonepaths only) and `recordedpath` (the positions). It is
  written by hand before a flight, filled by the recording, and read directly by the
  server and every client; no build step. [T1, T4, the part-2 decision]
- It keeps only the line flown; a drone runs along it at the drone's own speed. [T2]
- A maneuver's positions are relative to its focal point (the dummy when recorded, the
  target player when played). [F9a, T5]
- The recording mode runs on its own, without the server: it loads the zone the file
  names (for a maneuver, open flat ground, a recording zone if needed, with a dummy
  player standing still) and puts the author in it as a flying drone with a camera,
  stopped by buildings and the ground. [T3, T6, T9]
- Flown on an Xbox controller with a simple driving layout plus height. [T7]
- Chat commands start, stop and restart the recording; stop writes the positions into
  the same file. [F9, T3, T8]

The model's design for the rest, for the author to approve:

- **Launch.** A developer launch option, `--record-flight <file>`, starts the game in
  the recording mode on that file. The zone loads from its scene; no login.
- **The recording drone.** A `CharacterBody3D` with a collision shape, moved by the
  controller and sliding along what it meets, with the chase camera behind it. Its
  layout (reading of T7, open to correction): left stick moves, right stick turns, right
  trigger climbs, left trigger sinks.
- **Commands.** `/start`, `/stop`, `/restart`, read on the client like the emotes. The
  position is sampled every physics tick while recording. On stop, the samples are
  thinned (a point every half metre, more where the line bends) and written to the
  file; for a maneuver, as offsets from the dummy's feet.
- **The rules as plain C#** in `src/Rules/Drones/`: reading a flight path file, the
  position at a distance along it (smoothed between points, wrapping for a loop), and
  placing a maneuver round a focal point, turned so its first point is on the drone's
  side (T5 reading). Tested with xUnit.
- **Playback, server-owned as drones are today.** `ServerDrones` loads the flight
  paths at start. Every machine works out a drone's position from the same inputs, as
  it does for the circle today; what is synced is which flight it flies and when it
  started (and, for a maneuver, the focal point and the turn). `NetPosition` stays
  synced for the server's own checks.
- **Spy drones** (new): in a zone with zonepaths, while it has fewer than two, one
  appears every N minutes on a path picked at random and loops it. No zap. Cameras
  report it; the EMP brings it down.
- **Roaming drones** (today's pairs and swarms): the circle is their default; every so
  often one does a maneuver picked at random, aimed at a random player near it (only
  maneuvers with no player when none is near), hops to its first point, flies it, and
  goes back to circling.

Tests (xUnit, `src/Rules/Drones/`):

- A flight path file reads back as written; a bad one is refused with the reason.
- Thinning keeps the shape: no point is further than the tolerance from the flown line.
- The position at a distance runs along the path at an even pace and wraps for a loop.
- A maneuver placed round a focal point starts on the drone's side and keeps its shape.

Convention flags:

- **Developer launch options.** The author asked, in this session, for `--garden`,
  `--creator` and `--show-characters` to be looked at again as "bot tools". The
  recording mode is a launch option too, but the author flies it by hand; nothing in
  it drives the client by itself.
- Server authority holds: the server decides every drone's flight; clients only work
  out where it is.
- No C# on engine-called hot paths: a drone's position is worked out once a frame per
  drone, a few drones per zone; C# at that rate is fine.

Deferred:

- A Godot curve built from the JSON, to see paths in the editor. [part-2 decision]
- Spotting and what it does. [F3a]
- N, the maneuver interval, speeds and the like: placeholder numbers.

## Gatekeeping

Each "Already decided" item against the Outcome.

- The AI's drones are part of the fight; town people oblivious [Q16]: fits.
- The Drone Swarm world event [C-2026-09-27]: fits; its drones become roaming drones
  that fly maneuvers besides their circle [F5].
- FPV drone surveillance shown as a locked app [Q33]: fits; unchanged. The recording
  mode is the author's tool, not the FPV objective.
- Spotting drones on the CCTV cameras [Q8]: fits; spy drones are spotted the same way
  [F4]. Piloting an FPV drone as an objective [B]: open, not this feature.
- Drone colours, the Drone Operator career, a drone control skill [Q27, C-2026-09-26]:
  open, not touched.
- Personal, defence and companion drones [B, Q16, Q12, Q5]: open, not touched.
- Drones do harm as environmental danger [Q1, B]: fits; roaming drones zap as today,
  spy drones do nothing.

## Consequences

- Raised during the session (2026-09-29), outside this feature: the author finds the
  developer launch options (`--garden`, `--creator`, `--show-characters`) "read like bot
  tools", and wants them looked at again. For TODO.md at the close.
- `docs/world.md`: the two kinds of drone, spy drones on recorded paths and roaming
  drones with maneuvers, folded into section 8 in the author's words, tagged
  `[C-2026-09-29]` (the author's yes, 2026-09-29). The undecided spotting ideas went to
  `docs/backlog.md`.
- `TODO.md`: the build, as one item; and the developer launch options to look at again.
  The first playtest's note ("record drone flight") points here.
- `docs/engineering/`: a drones doc once the code exists.
- A split into PRs, by dependency, not by schedule:
  1. The rules and the file format (`src/Rules/Drones/`), with the xUnit tests.
  2. The recording mode (launch option, flying drone, commands, writing the file).
  3. Playback for roaming drones (maneuvers).
  4. Spy drones (zonepaths), which need recorded paths from 2.

