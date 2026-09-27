# Data centers, wilderness and travel

A synthesis, 2026-09-20, of three things: the author's spoken ideas (saved verbatim in
`design/Transcript-2026-09-20-Data-Centers-and-Travel.md`), the critique a model gave
them, and a short independent pass made before reading either. Where the three agree
the idea is written as the shape of the thing. Where they differ it says so and which
way we lean. Nothing here is built, and the open questions are listed rather than
answered.

## Data centers

### What is agreed

- **Built, not bought.** A data center is the top of the hardware ladder and the
  endgame weapon against the AI, and it is something a group *constructs*: the EVE model
  of a player-made mega-structure. Only a powerful group can afford one.
- **It costs in every currency, and in time.** Consumables to build it, so the parts that
  lie on the ground finally have a destination by the pallet; dollars for the site and
  power; and real time, about a week, so it is a project and not a purchase. The time is
  also what makes losing one hurt.
- **Where you build it is part of the cost.** The AI has coverage: dense in towns, thin
  in the wild, none in the desert. Build in coverage and the AI sees it going up and
  acts against it: a tax that makes it cost more to finish, a slowdown, or outright
  withdrawal. Build in a dead zone and it is safe from the AI but far from everything,
  hard to supply and hard to defend from anyone else. The trade the design already gives
  hardware tiers (a laptop works anywhere, a data center is fixed and must be defended),
  applied to the map.
- **Loss is partial and recoverable.** The design wants shifts that roll back. A data
  center that falls goes dark, loses what it held and hands the AI its escape trick
  (`Rogue-AI-MMO-Game-Ideas.md`), but the site and the shell remain. Total loss makes
  people quit; recovery makes people rally.
- **Defence is the coop endgame.** Watching a body while its owner is jacked in scales up
  to watching a building: turns, schedules, a guild's reason to exist.
- **Who raids, and how.** The AI, by the design's compromise clock: exposure accumulates
  with offensive use until the AI pinpoints the node. Other players, only by the PvP
  rule: a strategy mini-game, never direct damage; the brainstorm's team-versus-team-
  versus-AI raid with damage disabled is that idea.

### Where the critique lands

**A week-long build is a week-long vulnerability.** The critique is right that without
scheduled vulnerability windows, in EVE's manner, a dominant group wipes a small one's
build at four in the morning and the small group never builds again. Our PvP rule takes
most of the sting out: players cannot damage a build directly, only contest it through a
strategy task, and that task can be scheduled. The AI's attention is the other attacker,
and it is ours to pace. Lean: a build under construction is attackable only in windows
the builders declare, and the AI's raids follow the compromise clock rather than a
timer. Undecided in detail.

### Open

- **What a data center does, minute to minute, for its owner.** "Reach targets built to
  need that much power" is the design's answer; what that looks like on screen is not
  decided, and it is the thing to pin before build costs mean anything.
- **What "coverage" is as data.** A field over the map the server owns, with towns dense
  and the wild thin, that both the AI's detection and the player's map read from. The
  same field would decide where a phone has signal (below).
- **Build as a group task.** Whether construction is a timer that runs, or a series of
  deliveries and tasks the group performs over the week. The second is content; the
  first is a countdown.

## The world in rings

### What is agreed

The author's words and the independent pass drew the same picture: rings out from a
town, hand-built near, generated far, with a reason to be in each.

| Ring | Made by | What is there | Why go |
| --- | --- | --- | --- |
| **Town** (Old Town) | hand | the hub, the market, the people; safe by rule | everything social and economic |
| **Outskirts** | hand | known wilderness, the RuneScape kind: fixed layout, points of interest, threats, campaign quests; can be several zones and large | quests, the loop, grinding on a known map |
| **The generated wild** | procedure | terrain that differs each time, roaming threats, small towns sprinkled in | grinding for those who want it (hundreds of GPU units from roaming drones), exploration, and the space that long-distance travel crosses |
| **Transit** | instanced room | a train carriage or a plane cabin shared with other travellers | skipping rings at real-time cost |

- **Not a grinder, but grinding is allowed.** The game must not require grinding; a
  player who wants to grind gets the generated wild to do it in, and it should pay.
- **Real-time travel.** Distances map to real durations. Chicago to Philadelphia on
  foot is weeks. Nobody is made to walk it; anyone who wants to gets a world to walk
  through.
- **Small towns in the wild.** Not hand-placed: a generic small-town template
  randomised so it is never quite the same, sprinkled through the generated wild as pit
  stops where you resupply and save.
- **Analog navigation out there.** No signal means no map and no GPS. You find the town
  because someone handed you a paper map, or you did not.

### Naming

The author wants a name for the generated ring and rejected "unmapped area". The
critique suggests naming the rings by their relation to the AI's network rather than by
geography, since detection and signal are what the rings *mean* mechanically: "Dead
Zones", "The Gridless", "Null-Signal", "Dark Sectors". That fits the coverage idea above:
the outskirts are where signal thins, the generated wild is where it is gone. Lean:
name the rings by signal. Which words, undecided.

### Where the critique lands

- **Saving in generated terrain.** Deterministic generation from the world coordinate:
  the same place generates the same terrain every time, so the server stores nothing
  about the terrain, only where the player is and what they carry. Agreed; it is also
  how Agent Defense already works, randomness in the seed and nowhere after. What
  players *change* out there (a camp, a fire, a data center) is the exception and has to
  be stored as a delta on the coordinate.
- **Death far from anywhere.** Respawning at the last town after days of walking loses
  the days, and the critique is right that this would make people leave. Its fix,
  deployable anchors (a camp, a vehicle) that you respawn at, with what you carried as
  the cost, fits the small-town pit stops: a town saves you, a camp saves you nearer.
  Lean: yes to anchors. What dying is at all is still the open question from
  `mmo-expectations.md`.
- **Transit rooms need something to do.** A two-hour train with nothing in it is a
  reason to close the game. The design already wants transit as a shared room where
  people talk, trade and sometimes get ambushed; the room also has the one thing every
  player carries, a device, so the terminal and its tasks are available on the train.
  Lean: transit is the terminal world with a window.
- **One more the critique did not raise: a session is an hour.** A two-hour ride has to
  be something you start and come back to, with the character arriving whether or not
  the client stayed open. That is a persistence decision (the character exists and
  moves while you are logged out), and it decides whether long transit is fun or a
  reason never to travel.
- **Coordinates and handoffs.** A world the size of a country needs coordinates in two
  parts (which cell, where in it), hand-built zones as cells with a fixed place in the
  grid, and generated cells around a player until the next hand-built cell's box is
  reached. The critique calls this quadtrees and navmeshes; the shape is right, the
  machinery is later.

### Open

- Which words name the rings.
- What is in a generated cell besides terrain: the threat kinds, the town template,
  the dead drops that give travel a payoff.
- Roads. Driving follows a highway system nobody will hand-build, so roads are generated
  too, and a car is the walk at ten times the speed with the same rings.
- Whether the generated wild is shared (two players at the same coordinate see each
  other and the same terrain) or personal. Shared follows from deterministic generation
  and from the game being coop-first.
- What dying is, everywhere. It gates anchors, threats and the whole of the wild.
