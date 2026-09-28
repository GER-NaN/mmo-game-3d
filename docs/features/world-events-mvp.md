# World events (AI Swarm, Drone Swarm): MVP

**Date:** 2026-09-27
**Status:** Built (2026-09-27; how it is built: docs/engineering/world-events.md)
**Scope:** MVP (the fuller version is under "Beyond the MVP", not designed here)
**Sources read:** docs/world.md sections 2, 4, 8, 12, 15, 16; docs/backlog.md; TODO.md;
`game/server/world/ServerDrones.cs`, `ServerTerminals.cs`, `src/Rules/Terminals/TerminalApps.cs`,
`src/Data/Migrations/0005_world_state_and_town_log.sql`, `game/zones/meadows/meadows.tscn`

The first world event: an AI Swarm, of the Drone Swarm kind, in the meadows near the
door from town, announced on a new notifications app on the terminal and the phone. The
MVP builds the frame every later world event uses: kinds, a schedule, a status, a record.

The answers are the author's own words, recorded verbatim or near it, one question at a
time. Model additions are set apart and labelled: "Consequence noted", "Assumed",
"Options offered", "Note".

## Already decided

- Singular, world-level events are wanted: once, narratively significant, not calendar
  events; world state follows the conflict and must be reversible. The author called
  this one of the hardest problems in the concept. [B, T2] (section 2)
- Large events can be gated by player count: distinct, geographically spread players,
  never resources; repeatable, scalable, with a powerful reward. [B] (section 2)
- Drones and AI-controlled things do harm; environmental danger, not a battle system; no
  turn-based combat. [B] (section 8)
- At 0 HP a player faints and is carried to the nearest town; loss on fainting is open.
  [Q1, Q2, Q3] (section 8)
- The AI can break through firewalls; push notifications reach the in-game phone ("Agent
  Defense Required: ..."). [Q21] (section 12)
- The terminal's OS has apps; named ones include a status board of events happening in
  the world. [Q31] (section 4)
- A website with events and announcements beside the game, maybe not at first. [T2]
  (section 15)
- One region at first. [C-2026-09-18] (section 15)

Built:

- `ServerDrones`: drones over Old Town only. A pair spawns when the town has none,
  circling a spot over the streets; they zap players in range for 10 HP every 4 s; the
  EMP Emitter's pulse brings every drone in 10 m down; a downed drone lies 4 s and is
  gone; Town cameras pay $3 a drone spotted. Placeholder numbers.
- Fainting at 0 HP carries the player to Old Town's spawn with full HP.
- The terminal's apps (`TerminalApps`): Status board shows lines posted by the server
  (`ServerTerminals.Post`, a `StatusFeed` in memory); Town log reads a table.
- Persistence for the world: `world_state` (key and value) and `town_log`.
- The meadows: a 3 km Terrain3D zone; its door from town arrives at `FromTown`
  (x -1480); the door back is at x -1490.

Deferred:

- Mini game levels, tips and tricks (TODO.md, each waiting for an MVP session).
- PvP, including Domination with drones (backlog).

## Developer thoughts

> Next feautre to build and I think its new, is a world event. Our world event will be a
> AI Swarm Event, in this specific case we implement it will be drones and they will
> appear in meadows (choose some location near the portal so they players dont hwave to
> walk for 5 minutes) just for this instance. Then we have settings for the AI Swarm
> Event, the number of enemies. Then we need a notification somehow, for now put it on
> the terminal/phone, make a notifications app where we supply the world event
> notifications. I think it breakds down like this
>
> World Event (high level concenpt)
> - AI Swarm Event (high level concept of a world event (this is a type))
> -- Drone Swarm (specific type of swarm event)
> --- Detail (zone, number of entities)
>
> I want a way to track when it started and ended, its status (ongoing.. etc...), a way
> to determine kickoff (schedule, every 50 minutes or something). I am not sure where
> these live, maybe at the 2nd level AI Swarm Event row...

## Facts held in mind

- Drones exist only as Old Town's pair today; nothing spawns them anywhere else, and
  nothing groups them into an event.
- The status board is the nearest thing to notifications: server lines, kept in memory,
  lost on a restart, no per-player read state.
- Nothing is scheduled in the world today except timers inside systems (fixables every
  90 s, chests every 5 min, the lights and the rootkit on their own clocks).

## Q&A

**M1.** Why would a player drop what they are doing and go to a Drone Swarm?

> Answer (2026-09-27): Reason to do world event, it counts towards participation (you get
> world event poitns for credit somewhere in the future), it has special drops when
> completed (for now just gpu drops).
>
> Consequence noted: two things are kept per player: world event points (a running
> total, spent or counted later, not designed here), and whether they took part in each
> event. The drop is on completion, so an event has a completed state that pays out;
> the GPU is the existing `ItemType.GpuCore`.

**M2.** How does a Drone Swarm end?

> Answer (2026-09-27): All drones killed or timeout (timeout needs to be configurable) or
> at some point in the future with more mechanics the swarm can complete its goal
> (destroy something, hacking a thing etc..)
>
> Consequence noted: an event ends in one of named outcomes: completed (every drone
> down), timed out (a time limit, set per event), and later the swarm's own win (its
> goal reached). The MVP builds the first two and leaves the outcome list open for the
> third. Only "completed" pays the special drops [M1].

**M3.** What counts as taking part in a Drone Swarm?

> Answer (2026-09-27): Being in the area of effect, so guess this is a new thing. For now
> its just you are in the zone during the event, this will need tightened later.
>
> Consequence noted: an event has an area of effect, a new concept. For now the area is
> the whole zone; later it is smaller (a radius, a region), so the check is one place
> that asks "is this player in the event's area", answered by the zone today.
>
> Assumed: a player takes part when they are in the area at any moment while the event
> runs, not only at its end; each player counts once per event.

**M4.** How do the GPU drops reach the players?

> Answer (2026-09-27): The spawn on the ground near the event, scattered
>
> Consequence noted: the drops are ground items (`GroundItems.DropAt`) scattered round
> the event's spot when it completes; whoever walks over one picks it up, as with any
> ground item.
>
> Assumed: one GPU core per player who took part [M3], so a bigger turnout drops more;
> the drops lie until picked up, like other dropped items, and do not count toward the
> zone's own item stock.
>
> Assumed: the swarm's drones behave as Old Town's do today (they zap players in range,
> the EMP Emitter brings them down), in greater number and spread over the event's spot.

**M5.** What does the notifications app show, and when does a player see it?

> Answer (2026-09-27): Shows Current Notifications/Past Notifications. Its just a table for
> now each row is an event "Drone Swarm in Meadows!".keep it simple for now
>
> Consequence noted: a new terminal app, Notifications, with two tables: current
> (events running now) and past (events that ended). One row per event, its line
> ("Drone Swarm in Meadows!").
>
> Assumed: a row also shows when it started, and for a past event how it ended
> (completed, timed out) and when; nothing pops up outside the app yet (a push notice
> on the phone is left for later); the app is on public terminals and on the phone.

**M6.** What happens to a Drone Swarm that is running when the server restarts?

> Answer (2026-09-27): It ends.
>
> Consequence noted: a third outcome, ended by a restart: the event is recorded as over
> when the server starts again (or as it stops), with no drops. A running event is not
> carried across a restart, so only the record of events is kept, not a live one.
>
> Assumed: taking part still counts for the players who did [M1, M3], whatever the
> outcome; only the drops need "completed".

**M7.** How do you want to change a Drone Swarm's settings, such as its drone count or its schedule?

> Answer (2026-09-27): I would perfer an admin panel on the server but I think thats too
> big of a lift. Hardcode them for now or have them be a database record
>
> Consequence noted: an admin panel on the server is the fuller version's way (Beyond
> the MVP). The MVP's choice is between the two the author named.
>
> Options offered (the answer named two):
> - Hardcoded: the settings as constants in C# (a `DroneSwarm` definition: zone, spot,
>   drone count, time limit, every how many minutes). Simplest; a change is a rebuild
>   and a server restart; an admin panel later has to move them into the database
>   first.
> - A database record: one row per event definition (kind, zone, spot, drone count,
>   time limit, schedule), seeded by a migration with today's values; the server reads
>   it at start. A change today is an SQL update and a restart; the admin panel later
>   reads and writes the same rows, so it is new code, not a move.
>
> The model would take the database record: it is barely more work than constants, and
> it is the seam the admin panel needs, which is what the MVP's frame is for.
>
> Decision (2026-09-27): yes DB record.
>
> Assumed: the author's four levels map to data like this. A world event kind is the top
> (every event has an id, a kind, a status, when it started and ended, and how it
> ended). An AI Swarm is a family of kinds, and a Drone Swarm is one: its definition row
> holds the detail (zone, spot, drone count), the time limit, and the schedule (every so
> many minutes), as the author guessed, at the swarm's level. One definition row today:
> the Drone Swarm in the meadows.
>
> Assumed: the schedule starts the first swarm one interval after the server starts,
> and the next one an interval after the last ended; it starts whether or not anyone is
> online; only one runs at a time per definition.

**M8.** What should a player in the meadows see or hear when a Drone Swarm starts?

> Answer (2026-09-27): Nothing for now. Its manual perception, you need to see it or look
> at your notifications app. Future we can do push notifications on the phone and player
> UI.
>
> Consequence noted: no announcement, sound or marker at the start; a player finds out
> by seeing the drones or by opening Notifications. Push notices on the phone and in the
> HUD are Beyond the MVP.
>
> Assumed: the drones are today's drone model, no new art; the Notifications app is a
> plain table in the terminal's style.
>
> Assumed: a player's world event points are shown in the Notifications app (their
> total at the top, and "you took part" on the past events they were in), so they can
> be seen before they are spent anywhere; one point per event taken part in.
>
> Assumed: the placeholders, until seen in play: the spot about 60 m into the meadows
> from the door from town; 10 drones; a 10 minute time limit; every 50 minutes.

## Assumptions to confirm

- A player takes part when in the area at any moment while the event runs; each player
  counts once per event. [M3]
- One GPU core per player who took part, scattered on the ground round the spot when
  the swarm is completed; they lie until picked up and do not count toward the zone's
  own item stock. [M4]
- The swarm's drones behave as Old Town's do (zap in range, fall to the EMP), in
  greater number, spread over the spot. [M4]
- A Notifications row shows the event's line, when it started, and for a past one how
  it ended and when; no pop-up outside the app; the app is on public terminals and the
  phone. [M5]
- An event that ends by a restart still counts for those who took part; only
  "completed" drops GPUs. [M6]
- Data: a definition row per event kind (a Drone Swarm: zone, spot, drone count, time
  limit, every how many minutes), and a row per event run (kind, status, started, ended,
  outcome). [M7]
- The first swarm starts one interval after the server starts, the next an interval
  after the last ended; whether or not anyone is online; one at a time per definition.
  [M7]
- Today's drone model, no new art; Notifications is a plain table. [M8]
- World event points: one per event taken part in, shown in Notifications (the total,
  and "you took part" on past rows). [M8]
- Placeholders: the spot about 60 m into the meadows from the door; 10 drones; 10
  minutes; every 50 minutes. [M8]

> Answer (2026-09-27): Yep, use shorter timespans so our bots can participate without
> waiting for 50 minutes. Lets also build this into an activity "WorldEventCheck()" 50/50
> roll to participate or not.
>
> Consequence noted: all confirmed, with the times changed: the placeholders are short
> enough for a bot run to meet several swarms (every 5 minutes, a 3 minute limit), and
> they stay a row to change. A bot activity checks the Notifications app and, on a coin
> flip, goes to a running event.

## Outcome

Mechanics:

- A world event has a kind, a status (running, ended), when it started and ended, and
  an outcome: completed, timed out, or ended by a restart; the outcome list is left
  open for the swarm's own win later. [M2, M6]
- The Drone Swarm, an AI Swarm kind: drones appear over a spot in the meadows about
  60 m from the door from town and behave as Old Town's do (zap, fall to the EMP). It
  is completed when every drone is down, and times out at its time limit. [M2, M4]
- Taking part: being in the event's area (the zone, for now) at any moment while it
  runs; once per player per event; it counts whatever the outcome. [M3, M6]
- A completed swarm scatters one GPU core per player who took part on the ground round
  the spot; they lie until picked up and are not part of the zone's item stock. [M1,
  M4]
- One world event point per event taken part in, kept per player. [M1, M8]
- A schedule per definition: the first one interval after the server starts, the next
  an interval after the last ended, one at a time, whether or not anyone is online.
  [M7]
- No announcement in the world: the drones are seen, or read about in Notifications.
  [M8]

Server and client:

- The server owns everything: the schedule, the start, the drones, who takes part, the
  end, the drops, the points. Nothing a client sends starts or ends an event.
- The rules in plain C# for xUnit: the schedule, the end (all down, time up), who took
  part, how many drops, the points. [CLAUDE.local.md]
- The swarm's drones come from the drone code Old Town uses (`ServerDrones`), made to
  serve a zone and a spawn of its own: N drones at a spot, none spawned again.
- Notifications: a new terminal app on public terminals and the phone, with two
  tables, current and past; a row is the event's line, when it started, and for a past
  one how and when it ended and "you took part"; the player's point total at the top.
  The server sends a player the rows when an event starts or ends and at login. [M5,
  M8]
- A marker in the meadows scene for the swarm's spot.

Persistence (a new migration):

- Event definitions, one row per kind of event run: kind, family (ai-swarm), line
  ("Drone Swarm in Meadows!"), zone, spot, drone count, time limit, every how many
  seconds. One row: the Drone Swarm in the meadows, 10 drones, 3 minutes, every 5
  minutes (placeholders). [M7, and the answer above]
- Event runs: definition, status, started, ended, outcome.
- Who took part in each run, and each player's point total.
- At start, the server ends any run still marked running as "ended by a restart". The
  live swarm (its drones) is only in memory. [M6]

Art:

- None new: today's drone model; Notifications in the terminal's own style. [M8]

Tests:

- xUnit: the schedule's times; completed when every drone is down, timed out at the
  limit; taking part once however often a player comes and goes; drops per player; a
  run left running at a restart ends as such; points added once.
- A dev scenario, "swarm": a swarm starts at once with two drones, the player stands at
  the spot with an EMP worn, brings them down, and sees the swarm completed, GPUs on
  the ground and the row in Notifications.
- A bot activity, "check world events" (the author's WorldEventCheck): the phone out,
  Notifications opened; with an event running, a coin flip to go (travel to its zone,
  walk to the spot, hunt its drones if an EMP is worn, stay while it runs); its judge
  checks from the player's side that the past row says "you took part" after it ends.

Frame:

- The outcome list, open for the swarm's own win. [M2]
- The area check in one place, the zone today, a smaller area later. [M3]
- Definitions as rows, for an admin panel later. [M7]
- The family and kind fields, for other swarms (RC cars, robots) and other events.
- Notifications as rows of any event, for push notices on the phone and the HUD later.
  [M8]

## Beyond the MVP

- An admin panel on the server for the definitions. [M7]
- Push notices on the phone and in the HUD when an event starts. [M8]
- A smaller area of effect than the whole zone. [M3]
- The swarm's own goals: destroying or hacking something, and losing to it. [M2]
- What world event points buy or count toward. [M1]
- Other AI Swarm kinds; other world events; the singular world-level events and the
  player-count gates of world.md section 2.
- Loot of its own per event, beyond the GPU.

## Consequences

- `ServerDrones` serves a zone and a spawn given to it, not only Old Town's pair.
- `TerminalApps` gains Notifications, also on the phone.
- A new migration; a store for events under `src/Data`.
- world.md, on the author's word: the world event, its AI Swarm and Drone Swarm, taking
  part, points and drops, tagged `[C-2026-09-27]`.
- An engineering doc once built.
