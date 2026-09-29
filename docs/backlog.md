# Backlog

Ideas raised and not decided, and the questions still open. An idea lives here until the
author says yes, then it moves into `world.md`. Started 2026-09-20. Sources are tagged as
in `world.md`; "M" marks a model's proposal.

## Open questions

- The path from phone to laptop: buy, build or earn. Waits on the economy and
  crafting. [Q12]
- What a Defense Objective pays, and specifically what Agent Defense pays. GPU was the
  example and may be too valuable. [Q16]
- Whether a stash or a player store can be robbed while the owner is away. [Q22]
- Whether "nearest town" after fainting includes the small towns in the wild, and
  whether a camp or a vehicle counts. [Q4]
- Names: the long-term group (not "guild" or "clan"); the group's online room and the
  public meeting room (not "Discord server"); the generated ring of the wild (not
  "unmapped area"). [Q25, T1]
- What the player level does, and its formula. It adds up time played, skill
  experience, career progress and missions completed. [Q13, `features/player-skills.md`,
  F5]
- The word for a skill. "Skill" is the working word. [`features/player-skills.md`, F10]
- The skill and career numbers: XP per act, skill levels, career XP, rank thresholds.
  [`features/player-skills.md`, F8]
- What each skill and each career rank gives. Waits on the mechanics they tie to.
  [`features/player-skills.md`]
- What the Artist career does. [`features/player-skills.md`, F2a]
- Which college is "original" after a career change at a different college's
  registrar. [`features/player-skills.md`, F8b]
- What a chapter's local outcome changes in the next chapter, and how a region's share
  of the campaign is weighted. [T2, M]
- How the Prompt's continuation works after the cliffhanger. [B]
- What is in a generated cell besides terrain: threat kinds, the town template, the
  payoff for long travel. [T1]
- Whether the generated wild is shared between players at the same coordinate or
  personal. [T1, M]
- Whether players in different regions can party together. [B]
- The initial load screen before the Training Grounds. [Q23]
- What "the work" of a repair is in the first playable: a terminal task, a walk to the
  junction box, or both. [`first-playable.md`]
- The phone's terminal layout and its apps. Waits on the terminal layout.
  [C-2026-09-21]
- What losing your terminal connection costs, for example when the phone dies while
  online. [C-2026-09-21]
- What increases the slot count or adds a slot type beyond the fixed set. Not needed
  while every slot shows. [C-2026-09-21]
- The battery drain formula and the colour thresholds of the level bar. Set in the
  renderer, not in words. [C-2026-09-21]
- Whether dropping an equipped instance to the ground is in the phone build.
  [C-2026-09-21]
- When world zones move the whole party through a door and town zones do not. Needs a
  zone kind. Every door moves the party until then. [`features/second-zone.md`, F7]
- Whether the outskirts, a safe zone, gets a free standard terminal. [Q12,
  `features/second-zone.md`]
- Non-rectangular scenes: floor cut away at the edge to give a scene a shape. The box is
  the only floor the game knows; the reading in the session file is to derive the floor
  from `ground.*` placements. Its own session. [`features/second-zone.md`, F9]

## The author's own ideas, undecided

- What a spy drone's spotting does, for later: "a player that is spotted can be
  announced, if the AI is looking for a specific high level player for some game
  mechanic the can be "Found"", and "achievements for Incognito, be a player that is
  least spotted by spy drones". [`features/recorded-drone-flights.md`, F3a]
- Hunger and sleep as player health. On the fence; fits uniquely-human. [Q19]
- PvP at all. Designs exist (Domination with drones, Hall of Heroes); the author is
  very unsure it has a place. De-skilling or de-faming as its cost. [Q15, B]
- Reputation atrophy by absence from a place; a legitimate top-ten score keeping the
  fame. [Q15]
- Player-owned stores in physical locations with their own prices. A primary idea to
  explore partially. [Q19]
- Secret messages passed off-grid between players so the AI does not know. Not
  early-game. [Q20]
- Push notifications to a real phone, not only the in-game one. [Q21]
- Crafting's full shape: what is built, repaired and upgraded, and from what. [Q12, Q14]
- Charging ports for batteries, a realistic feature for later. Day one is a new
  battery. [C-2026-09-21]
- Auto-equip. For now it is mechanical, inventory to equipped. [C-2026-09-21]
- A recycle bin or recycle machine in town for dead batteries. [Q14, C-2026-09-21]
- Player bank accounts: transactional, with history, balances, loans. One integer
  balance until then. [`features/second-zone.md`, T8]
- Danger in the outskirts. "No dangers yet." [`features/second-zone.md`, F4]
- Some items protected outright from loss on fainting. "Maybe." [Q3]
- Armor, to protect a player without raising HP. "We can add some kind of armor if we
  want." [`features/player-skills.md`, F1c]
- Coop Defense Objective designs. Ideas offered [M], none chosen: spotter and cutter
  (Agent Defense with the chart hidden from the cutter); two-key hack (two puzzles whose
  answers must land within the same second); load sharing (a target needing more GPU
  than one device). [Q9]
- Player-count-gated milestone events at five thousand distinct players. Stated, not
  designed. [B]
- Leaderboard replays. Explicitly not now. [B]
- Players building whole quests and points of interest. Older, deferred. [B]
- A marketplace where remote purchases ship physically in the background. Later. [B]
- A website beside the game. "Maybe not at first." [T2]
- Achievements on Steam. Stated; Steam integration is a launch item. [Q23]

## Model proposals, not adopted

Kept because the author engaged with them. None is design.

- **Chapters open on dates for everyone; outcomes stay local; the global aggregate sets
  the chapter's tone, not its timing.** From `archive/global-events-and-servers.md`.
  Answers the fairness and spoiler fears in T2.
- **The AI presses in proportion to who is there,** so a region of two faces a
  two-player-sized takeover. Same source.
- **Between-region boards count shares of capacity, not heads.** Same source.
- **World events roll around the globe by local evening,** never one instant. Same
  source.
- **A data centre under construction is attackable only in windows the builders
  declare;** the AI raids by a compromise clock as exposure accumulates. From
  `archive/data-centers-and-travel.md` and `archive/Rogue-AI-MMO-Game-Ideas.md`.
- **Loss of a data centre is partial:** it goes dark and loses what it held, the shell
  remains. Same source.
- **Deployable respawn anchors** (a camp, a vehicle) so dying far out does not lose the
  days. Same source.
- **Coverage as data:** one field over the map that AI detection, phone signal and the
  player's map all read. Same source.
- **Name the rings by signal** (dead zones, gridless, null-signal, dark sectors). Same
  source.
- **Transit rooms are the terminal world with a window:** tasks available on the
  train. Same source.
- **Deterministic generation from the coordinate,** with player changes stored as
  deltas. Same source.
- **The analog vault:** move an irreplaceable item into offline physical storage, safe
  from digital theft and unusable until retrieved. From
  `archive/Rogue-AI-MMO-Game-Ideas.md`.
- **Provenance logs on unique items:** who made it, in what event, who owned it since.
  Same source.
- **Overclocking as a sink:** an edge now, permanent degradation, eventual destruction.
  Same source.
- **Signal Boost:** idle capacity of everyone's hardware feeds a slow global meter that
  unlocks small buffs. Same source.
- **Planting false intelligence** in espionage. Same source.
- **Continuations after the Prompt** offered by Gemini: AI factions, orbital migration,
  biological vectors, an off-world architect. From `archive/WorldBuilding.md`.
- **Campaign pacing mechanisms** (encryption layers, AI counter-offensives, resource
  depletion) and the rest of Gemini's idea pool. `archive/WorldBuilding.md`, Part 2,
  items marked [Gemini].

## Engineering notes parked here

Not design. From `archive/NEXT.txt`, for `TODO.md` when picked up.

- **The client's shape, ideas from a conversation (2026-09-29), not decisions.** The
  author: "Right now this is a big spaghetti mess IMO." `ClientGame` is 1,848 lines, two
  thirds of the client: 61 fields, 22 network events wired, 56 screen button handlers.
  Every mechanic's client side is spread through it (a field, a handler, an open and a
  close method, a line in the open-screens list), and screens open from 17 places.
  - *A screen is a mechanic's two ends.* The author: "a screen, it is only the input and
    output interface of a game mechanic." The rules and state live in `src/Rules` and on
    the server; a screen shows state and turns clicks into intents, and holds no rule.
  - *Desktop MVC, with the model across the network.* The author: "You have your state
    and business logic in models. Then you have your visual screens ... then eventing
    ... which calls into model code." And WinForms: one file for how the screen looks,
    one wiring its buttons (`button32_save.Click()` to `EntityModel.Save()`), and the
    model calling the repository. Here: the `.tscn`, the screen's script, the client's
    side of the mechanic (a network send), the server's stores. Unlike WinForms, the
    wiring sits in `ClientGame`, not beside the screen.
  - *Something that keeps the open screens, like a screen manager* (the author's
    words). It would own a screen's life only: what is open, in order, the top one,
    open and close, Esc, the timeline, finding an open screen by class. Not what a
    screen shows or what its buttons do, or it grows into a second `ClientGame`.
    Considered: an event per screen ("InventoryScreenRequested") answered by it, or the
    trigger handing it the screen directly (fewer hops, the model's preference).
  - *One client class per mechanic*, as `ClientParty` already is (219 lines: the target
    frame, the invite prompt, the party panel and the party network's events together).
    `ClientGame` would keep the connection, the world, zones and startup.
  - *It extends past screens.* A mechanic's client side has three kinds of ends: screens
    (the tangled part), world objects (a drone, a lamp: scene nodes with their own
    scripts, already per thing, e.g. `game/drones/Drone.cs`), and messages and feedback
    (notices, sounds, the Notifications feed, today through `ClientGame`'s generic
    handlers). The server already has the shape, one class per mechanic
    (`ServerWorldEvents`, `ServerDrones`, `ServerGarden`). Lined up, a mechanic would have
    its piece on every side: rules, server, wire, client.
  - Related, in `TODO.md`: every screen closes the same way; with the screen keeper,
    that is one rule.

- **A helpful compiler.** The asset half of this note is done: the build compiles the
  map project from `art/` and the package is build output (`map-project-and-assets.md`).
  Left: make the compiler's errors as helpful as a tool we own can be, saying which
  folders were searched and suggesting the nearest name for a missing model.
- Batch packets: slice a world update into MTU-safe pieces with an update id, reassemble
  or discard per tick, with a give-up policy for partial updates. Decided 2026-09-21
  to wait: this is the reliability layer (fragmentation, acks, ordering), its own
  feature session, triggered by the first real multi-datagram payload. Item
  definitions go one per message and the held-items snapshot stays one message until
  then. [`features/usable-phone.md`, T4c, T11]
- The state shape for many attributes across item types (a column each, or an
  attribute table). One nullable charge column until a second stateful item exists.
  [`features/usable-phone.md`, T3]
- The intent id, `ServerIntentApproved` and the server acting once per id are the
  first piece of the reliability layer, built for the buy. Which other intents adopt
  an id is decided per message when there is a reason. [`features/second-zone.md`,
  T8a]
- Persisting the chest across server starts. A start finds it full until then.
  [`features/second-zone.md`, T7]
- Property bundles in Voxel Scene Maker (other repo): a named group of props added to
  a placement in one step, flattened to prefixed properties in the file. Until built,
  the two transition properties are typed by hand and listed in the project's props.
  [`features/second-zone.md`, developer thoughts]
- **Message delivery, four ideas from the author, undecided.** Needs a feature-design
  session on the technology fork before any of it is built. From
  `archive/Transcript-2026-09-21-Message-Delivery.md` (T3):
  - A priority queue for outgoing messages: intents, rejections and real-time
    interaction first; world events and leaderboards may arrive seconds late.
  - Breaking the world view into smaller messages: a zone-wide player status message
    (position, standing, walking, on the phone), per-player inventory, world-object
    state (a building on fire) on their own.
  - Parallel delivery across zones, so ten zones of twenty players are not sent one
    after another and no zone lags.
  - The terminal world is not zoned: who is online must reach every terminal player
    everywhere, so it needs its own delivery design.
  The author's own caveat: "I imagine this is something we can add later, but I'm
  thinking about it now, in case it's something we want to get in early."
