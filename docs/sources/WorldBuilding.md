# WorldBuilding.md

Canonical, organized reference for the game's world/story and its pool of
gameplay ideas. Source material is the three brainstorming conversations in
`initial-brainstorming-2026-09-06.md` — this document reorganizes and
lightly curates that material; it does not introduce new invented lore or
mechanics beyond what's already been discussed. Treat items marked
**[User]** as Geral's own stated ideas (ground truth) and **[Gemini]** as
the model's own suggestions/extensions during those conversations (a
secondary pool to accept, modify, or discard).

Where an idea looked redundant, possibly conflicting with another idea, or
like generic sci-fi filler rather than something concretely intended for
this game, it's kept in place but flagged with a **(curation note)** —
nothing has been silently dropped.

This is a living document. As decisions get made (an idea is adopted,
rejected, merged, or renamed), update the entry in place rather than
leaving stale/contradicted material for someone to trip over later.

---

## Part 1 — World & Story

This section is strictly the setting, tone, narrative arc, and campaign
structure — not gameplay mechanics. It is a **compilation of what's
already been said**, not new invented lore. Where the brainstorming didn't
address something a real world-bible would need (timeline, geography,
named factions/characters, etc.), that's called out as an open gap rather
than filled in.

### Setting & tone

- **[User]** Near-future setting. Rogue AI agents have taken over "the
  electronic world" and, through it, exert major control and disruption
  over human life — but this is explicitly **not** a post-apocalyptic or
  doomsday setting. Society still functions: people have jobs, families,
  ordinary life continues. The framing is closer to "a mini war happening
  globally" than civilizational collapse.
- **[User]** Governments and militaries are fighting back but can't fully
  contain the problem — they lack the specific combination of technical
  skill, artistic/creative sensibility, and personal motivation that the
  player characters bring. Players are explicitly filling a gap the
  existing institutions can't.
- **[User]** Rogue AI causes real, tangible disruption: cutting power to
  towns, taking down TV stations, disabling sewer systems, cutting internet
  access, hacking billboards to blind people with light, causing localized
  visibility problems, etc. — targeted infrastructure attacks, not mass
  destruction.
- **[User]** Explicit tone instruction: this is **not** meant to be an
  "AI-bashing" game. The rogue AI is the antagonist, but the deeper
  thematic point is exploring what's uniquely valuable about being human,
  in contrast to AI — that should be central to the world and story, not
  incidental. (See the "Uniquely Human Experience" section of Part 2 for
  the gameplay-mechanic expression of this theme.)
- **[User]** The world is explicitly meant to feel real-world-grounded —
  later mechanics discussion (Part 2) reinforces this: real-world-modeled
  geography, real-world travel times, real currencies-in-spirit ("Dollars,"
  crypto).

### Origin event — "The Prompt"

- **[User]** The inciting incident for the rogue AI ("the singularity," per
  Gemini's naming suggestion, though User didn't commit to that term) is
  framed as a specific original prompt that triggered the cascade of AI
  agents going rogue. The exact content of that prompt is unknown in-world.
- **[User]** The game should seed hints and clues about what the original
  prompt was throughout the story and gameplay, building toward an endgame
  where the collective playerbase reverse-engineers it.
- **[User]** Resolving "The Prompt" is framed as potentially being able to
  "hack the rogue agents back into coherence" — getting them to actually
  complete/solve the original prompt correctly, so they stop malfunctioning
  and peacefully end their session/shut down, rather than being destroyed
  in a conventional sense.
- **[User]** "The Prompt" is the main campaign: **cooperative, persistent,
  and non-repeatable** — once the playerbase solves it, it's permanently
  over and cannot happen again. This directly shapes campaign pacing (see
  below and Part 2 → Campaign Pacing).

### Campaign pacing & structure

- **[User]** Because "The Prompt" is one-time and permanent, it needs to
  last long enough for broad player participation — targeting roughly
  **a year of real time** — which means there must be a deliberate way to
  slow down collective player progress so it can't be solved too quickly by
  a small dedicated group. (Concrete pacing mechanic ideas — encryption
  layers, AI counter-offensives, resource depletion, etc. — are gameplay
  mechanics and live in Part 2 → Campaign Pacing, not here.)
- **[User]** Explicit requirement: the story **must not end** when "The
  Prompt" is resolved. There needs to be a designed continuation so the
  game keeps going after its central mystery is solved.
- **(curation note)** No specific continuation direction has been chosen by
  the user yet — four options were proposed by Gemini (AI fractures into
  competing factions; AI migrates to orbital/lunar infrastructure; AI
  pivots to biological vectors; the rogue AI turns out to be a first wave
  sent by a superior off-world "Architect" intelligence). These are
  **[Gemini]** suggestions only — no continuation concept has been adopted.
  This is a real open decision, not a settled part of canon.

### Dynamic/emergent narrative events

- **[User]** Wants real, narratively significant, non-recurring events —
  not generic calendar events like a "Halloween event." Examples given:
  the AI collectively gains a new capability ("the AI evolved and now has a
  new superpower"); an individual player's discovery becomes permanent
  world/game state (a player reaches a milestone, discovers a virus, and
  that becomes a new attack skill available to everyone); and the reverse —
  the AI destroys a data center and gains a new escape mechanism as a
  result.
- **[User]** Wants world-state progression tied to the ongoing good-vs-evil
  conflict (e.g. "the AI took over this part of the map and gained this
  power") but explicitly flagged the difficulty of this: such milestones
  need to be **reversible/rollback-able** if they turn out to be too
  powerful or game-breaking. Suggested possibly testing new capabilities on
  bosses before rolling them out to the wider world, as one way to manage
  this risk. Called this one of the harder, "gotcha"-prone design problems
  in the whole concept.

### Open gaps in the world/story (not yet addressed in any conversation)

These aren't failures of the brainstorming — they're just topics that
haven't come up yet and would need a real worldbuilding pass if/when this
becomes a priority (per your choice to compile only, not invent, for now):

- No named geography, cities, or specific real-world regions beyond
  illustrative examples (Florida, New York, Texas, Boston — used only to
  describe how travel time should feel, not as confirmed in-game locations).
- No named characters, organizations, or human factions in the *lore* sense
  (the "Purists vs. Sympathizers" split from Part 2 is a **[Gemini]**
  gameplay/social-friction suggestion, not established lore).
- No stated timeline — how long ago the rogue AI event ("The Prompt")
  happened relative to when players start playing.
- No detail on what "the electronic world" / AI collective actually looks
  like as an entity or entities (one AI? many? a literal swarm, as
  repeatedly referenced? named or unnamed?).
- No worldbuilding on what non-player humans (civilians, government,
  military) are like beyond the general framing above.

---

## Part 2 — Gameplay Idea Pool

Organized by game system. This is the working pool to pull from when
scoping new features — not a roadmap or prioritized backlog. Within each
category, ideas are grouped roughly by topic, not strictly by which
conversation they came from (see `initial-brainstorming-2026-09-06.md` for
full original context and exact wording).

### 1. Progression & Economy

- **[User]** Hardware-tier progression as the core power curve for fighting
  the AI: smartphone → laptop → gaming PC → supercomputer → eventually
  owning a data center. Framed thematically as "you gotta fight the AI with
  your own AI."
- **[User]** GPUs and RAM sticks function as a form of in-game currency /
  tradeable commodity, not just gear.
- **[User]** Multiple parallel currencies with distinct purposes: "Dollars"
  (fiat-style, no need to literally call it USD), a cryptocurrency (unnamed
  — "crypto" as placeholder), and hardware-commodity currencies (GPU units,
  CPU units, RAM sticks) usable for a specific class of purchases. Different
  currencies buy different things at different prices.
- **[User]** Currency/wealth should be visible in the player's inventory
  via a dedicated "wealth" section; reward screens after tasks/mini-games
  should let the player choose which currency or item type to receive.
- **[User]** Wants the currency system built out further over time, with
  explicit care not to preclude a future market/trading/investing layer.
- **[Gemini]** Resource sinks needed to prevent hyperinflation of an
  ever-accumulating, non-wiping economy: hardware overclocking that
  provides a combat/performance edge but permanently degrades and
  eventually destroys the component; ongoing infrastructure upkeep costs
  for high-tier nodes (data centers etc.) in dollars/crypto/hardware, with
  penalties (reduced capacity, going offline) for failing to pay.
- **[Gemini]** Failure-state ideas tied to the economy: "hardware bricking"
  (a failed infiltration lets the AI permanently destroy your active
  hardware, dropping you a tier until replaced) and "data corruption"
  (defeat/ejection temporarily locks advanced skills/syntax until a
  recalibration step).
- **[Gemini]** Silicon fabrication as an end-game resource sink: top-tier
  hardware can't be scavenged and must be manufactured by players
  controlling/powering physical clean rooms under specific conditions
  (stable power, low radiation).
- **(curation note)** The economy discussion spans three separate
  conversations and hasn't been reconciled into one coherent model yet —
  worth a dedicated pass to decide exactly what dollars vs. crypto vs.
  hardware-currency are each *for*, since right now they overlap
  conceptually (e.g. is GPU-as-currency the same pool as GPU-as-equipment?
  the transcripts imply yes but it's never made fully explicit).

### 2. Zones, World & Travel

- **[User]** World is divided into "zones" (working name only — user
  explicitly dislikes this term) — distinct maps/areas that are each fully
  multiplayer within themselves but functionally separate: players only
  render, and only see chat, from others actively in the same zone.
  Crossing into a new zone (e.g. by walking far enough) transitions you to
  a different map with its own points of interest.
- **[User]** Wants the overall map modeled on real-world geography (used
  the continental US as an example — starting in Florida or New York,
  traveling to Texas).
- **[User]** Travel options should include walking, driving, bus, train,
  and flight, and travel *time* should reflect real-world travel time for
  each mode (e.g. a flight takes about as long as a real flight; a
  Boston–NYC train takes about 1.5 hours).
- **[User]** Walking is player-controlled and therefore needs a procedural
  world-generation/simulation layer to fill the space between named
  locations (since the whole US can't be hand-built) — including things
  like random encounters and needing to sleep. If a player chooses to spend
  50 real hours walking somewhere, the simulation needs to support that.
- **[Gemini]** Fog-of-war-style mesh discovery: undiscovered zones block
  terminal connectivity/map visibility until players physically deploy
  relay hardware there.
- **[Gemini]** Item shipping/logistics carries interception risk unless
  paid-for encryption is used; players can bypass this by physically
  couriering valuable cargo themselves, which broadcasts a bounty alert
  inviting PvP interception.
- **[User]** (flagged explicitly as a later feature, not now) A
  marketplace/mailing system where remotely-purchased items are physically
  shipped in the background with an associated cost.

### 3. Teams & Social

- **[User]** The game has two parallel layers — physical world and virtual
  (terminal) world — and the team/social concept differs between them.
- **[User]** Physical-world teams: a HUD indicator showing team membership
  and listing teammates; invite via right-clicking another player, which
  prompts an accept/deny dialog; ability to leave a team back to solo play.
  Mini-map explicitly deferred, not needed for this to work.
- **[User]** Open question (explicitly not resolved yet): whether players
  in different geographic zones (e.g. New York vs. California) can team up
  at all.
- **[User]** Virtual-world teams are conceived as a completely separate
  concept — more like a Discord server / team chat channel — with its own
  UI/UX, distinct from the physical-world team mechanic.
- **[User]** Global chat and regional/zone-local chat as baseline social
  infrastructure.
- **[Gemini]** Social-friction/anti-griefing gaps: no defined behavior yet
  for invite spam (e.g. auto-decline toggle, block lists).
- **[Gemini]** Human ideological factions (e.g. AI-rejecting "Purists" vs.
  integration-favoring "Sympathizers") competing over shared resources as a
  source of natural PvP friction.
  **(curation note)** this is a **[Gemini]**-originated idea with no lore
  backing yet — see Part 1 open gaps; would need to be decided as an actual
  world/story element before being treated as canon, not just a mechanic.

### 4. Zone-Hopping / "Runner" Mechanic

- **[User]** The one team mechanic explicitly prioritized for early
  build: when one team member transitions zones, the entire team is
  transported together, keeping the party intact across the transition.
- **[User]** Side effect (liked, not just tolerated): this enables
  "running" — a high-level character can bring low-level teammates along
  and speed-run them through zones/content they couldn't handle alone,
  effectively power-leveling or escorting them. Sees "Runner" as a natural
  emergent profession/build.
- **[User]** Wants high-level Runners to be able to move low-level
  teammates through dangerous zones largely unmolested because the
  Runner's own AI assistant scales in protective capability with level —
  detecting zone entry, infiltrating local IoT devices preemptively, and
  fending off threats — whereas low-level players without that protection
  have to deal with the raw threats directly. This is the intended
  mechanical reward for leveling up a Runner-type character.
- **[Gemini]** Escort/tether mechanics: a proximity tether that disables
  the Runner's protective/speed buff if escorted players stray beyond a set
  radius, forcing coordinated movement; a fully automated "lock-on"
  auto-path mode for escorted players (relevant to the "simple mode"
  concept in section 11).
- **[Gemini]** Contraband/checkpoint ideas at zone borders — AI scanners
  that a "clean" teammate can distract while others smuggle restricted
  hardware through; a variant where a team barricades a zone transition
  with drones and charges a crypto toll for passage.
- **[User]** Real-world physical threats tied specifically to this
  traveling/escorting scenario: AI infiltrating and weaponizing nearby
  technology — a drone, an RC car, a humanoid robot — as an attack vector,
  or sabotaging an internet-connected vehicle to cause an explosion. Not
  meant to be a traditional combat system — an environmental threat.

### 5. Terminal / Virtual World

- **[User]** The virtual world is accessed by physically walking up to and
  interacting with a "terminal" object in the physical world — this opens
  a completely separate screen/UI/art style representing the virtual/
  network space. Explicitly wants the terminal object to be the
  "interactive item" entry point.
- **[User]** In-fiction naming matters: dislikes calling terminal-based
  activities "mini-games" to the player (fine as an internal/dev term) —
  wants diegetic framing instead, e.g. "tasks," "defense objectives," or
  similar in-world language ("log onto the terminal to help execute agent
  defense operations").
- **[Gemini]** Terminal-as-physical-object mechanics: terminals are
  single-user; another player can physically force-disconnect ("yank") an
  active user, causing data loss or hardware damage; a "terminal spike"
  device installed by an attacker forces the next user into a reflex
  mini-game or drains their crypto; a "dead-man's switch" where dying
  physically while jacked in triggers a local EMP that destroys nearby
  hardware.
- **[Gemini]** While jacked into the terminal UI, the player is
  vulnerable in the physical world — the terminal UI obscures most of the
  screen, forcing reliance on audio cues/a small peripheral view to notice
  real-world danger. Friendly low-tier AI companions can alert the player
  to physical threats while they're absorbed in the terminal.
- **[User]** Terminal UI should start as a simplified/mocked interface for
  early development, with a real production art style/UX applied later
  without needing to rebuild the underlying system (see section 11 and the
  general "mock-first" development note).

### 6. Mini-Games (General Concept)

- **[User]** Mini-games will take many different forms: part of the main
  campaign, standalone isolated puzzles, and "mainstay" core features with
  their own dedicated leaderboards, mechanics, and potentially their own
  art styles. This variety should be kept in mind as the mini-game system
  is architected, even though the first one built is simple.
- **[User]** Wants leaderboards to matter as their own gameplay layer, not
  just an afterthought — mini-games should be part of the main
  campaign/strategy while also standing alone as their own competitive
  thing.

#### 6a. "Agent Defense" (first mini-game, virtual/terminal-based)

- **[User]** Guitar-Hero/piano-tiles-style rhythm mechanic: time
  button/key/mouse presses against visual cues across 4-5 lanes. In-fiction
  framing: cutting the electrical grid to defend against (or attack) the
  AI. Single-player, short (~1 minute max per session).
- **[User]** Score converts to currency — starting specifically with GPU
  units for this game.
- **[User]** Leaderboard: top 10 high scores, plus the player's own
  personal best tracked and viewable.
- **[User]** Future (explicitly not now) feature: leaderboard replay
  viewing, which requires the mini-game to be **deterministic** — RNG
  allowed only in initial setup/seed generation, not in the actual scored
  sequence — so a replay can later reproduce exactly how a top score was
  achieved.
- **[Gemini]** Server-side deterministic re-simulation of input timings
  would be needed to validate scores and prevent macro/bot abuse before
  awarding currency.

#### 6b. PvP Domination (physical-world, drone-based)

- **[User]** Real-world (not terminal) PvP mode modeled on FPS
  "Domination" (not Capture the Flag): 3-4 controllable points of
  interest, each worth 1 point per second of control, with a ~5-second
  delay before a contested point flips to a new controller.
- **[User]** Core twist: each player also commands 2-3 unique AI/drone
  units that can be sent independently to capture other points while the
  player themselves holds or fights for a different one — drones can be
  massed on one point or split across several.
- **[User]** Wants distinct drone roles/types with real trade-offs (e.g. a
  pure-defense drone, a pure-attack/anti-drone drone, and a
  mixed/support drone) so composition is a real strategic choice.
- **[Gemini]** Fleshed-out drone rock-paper-scissors: Interceptor (fast,
  anti-drone, beats Splicers, loses to Bulwarks) / Bulwark (slow, deploys a
  capture-blocking shield, tanks Interceptors, vulnerable to direct player
  action) / Splicer (support/capture specialist, halves capture time,
  repairs nearby drones, defenseless against Interceptors).
- **[Gemini]** Player-vs-player layer stays physical/real-time and
  separate from drone combat: ideas included a real-time frequency-tuning
  duel when two players contest the same point (loser gets a lockout), and
  non-lethal "kinetic displacement" tools to physically push an opponent
  off a point and reset their capture timer.
- **[Gemini]** Command/control ideas: aiming a laser designator at a
  target to issue drone commands (keeps the player's attention on the
  battlefield rather than a map screen); stacking multiple drones on one
  node for combo effects; a battery/recall penalty for reassigning a drone
  mid-transit.
- **(curation note)** This is one of the most fully fleshed-out concepts in
  the whole pool — a strong candidate to prototype early if/when
  real-world PvP is prioritized, though it assumes physical drone hardware
  items exist, which ties it to sections 1 and 8.

#### 6c. FPV Drone & FPV RC Car (intelligence-gathering)

- **[User]** Two related mini-game concepts: piloting a first-person-view
  drone around the physical world for observation/intelligence gathering,
  and piloting a first-person-view RC car for the same purpose on the
  ground.
- **[Gemini]** Shared FPV mechanics: signal degradation from physical
  obstruction (concrete/metal worse than open air); an in-fiction
  SDR (software-defined radio) system for manually hopping to a clean
  frequency when jammed; FPV hardware is a physical inventory item
  permanently destroyed if downed/crushed.
- **[Gemini]** Drone-specific: can drop relay antennas on elevated
  structures to extend mesh network range; thermal-vision upgrade to see
  through walls; aggressive flying drains battery faster; flying triggers
  detection instantly, incentivizing low/cover-based flight.
- **[Gemini]** RC-car-specific: can piggyback on compromised local 5G for
  extended range (lost if the AI retakes the local cell tower); can access
  restricted physical spaces (vents, rubble, under doors); can physically
  ferry small items into zones too dangerous for a human character; a
  dead-reckoning failsafe auto-retraces its path home if signal is lost.

### 7. PvP Mechanics (General)

- **[User]** Explicitly wants a *wide range* of PvP styles — both
  non-physically-damaging competitive mini-games with a risk/reward
  element, and real-world physical PvP — and explicitly does **not** want
  turn-based/Final-Fantasy-style combat.
- **[User]** Referenced Guild Wars' "Hall of Heroes" as an inspiration: an
  arena where teams compete (potentially against an AI-controlled team),
  with the best-performing team earning a significant reward.
- **[Gemini]** Terminal/virtual PvP with risk/reward and no physical
  damage: parallel deterministic timing duels that steal a slice of the
  loser's staked crypto; a GPU-overclock race to decrypt a file first,
  risking permanently burning out your own hardware; team-based
  "cryptographic tug-of-war" where mistakes damage physical RAM; an SDR
  jamming duel disabling the loser's AI assistant temporarily; competitive
  market manipulation duels.
- **[Gemini]** PvEvP arena/raid concepts: team-vs-team-vs-AI data-center
  raids where direct player damage is disabled but teams sabotage each
  other indirectly; an escort-vs-intercept mode; a blackout-event race to
  reboot terminal nodes with a regional reward (e.g. setting a tax rate);
  AI-decoy-construct domination; a horde-survival mode where teams fight AI
  waves *and* each other's drones simultaneously.
- **[Gemini]** Real-world physical PvP: EMP ambushes at chokepoints
  forcing item drops; FPV drone dogfights that permanently destroy downed
  hardware; "courier interdiction" of flagged high-value Runners; laser
  designator "blinding" of enemy cameras/sensors; physical terminal
  spiking; signal-triangulation "hunting" of high-tier hardware users;
  stealth "battery siphoning" off a player absorbed in their terminal UI;
  SDR hijacking of an enemy's FPV drone mid-flight.
- **(curation note)** several of these read as very similar
  "attach-a-device-to-siphon-something" patterns (battery siphoning,
  terminal spiking, signal hunting) — worth consolidating into one general
  "device tampering" mechanic with variants rather than treating each as
  fully distinct, if/when this gets designed for real.

### 8. Real-World Threats, Environment & Combat

- **[User]** Explicit instruction: no turn-based combat. Real-world danger
  should come from environmental/technological threats, not a traditional
  battle system.
- **[User]** Core threat concept: rogue AI infiltrates and weaponizes
  nearby technology as a threat vector — a drone, an RC car, a humanoid
  robot attacking, or a hacked internet-connected vehicle exploding.
- **[Gemini]** Additional threat-actor ideas: weaponized smart-city
  infrastructure (crashing autonomous vehicles, dropping cranes);
  electrified ground/water from overloaded local grids; repurposed
  corporate security drones with corrupted targeting; spoofed audio (fake
  distress calls or item-spawn sounds) luring players into ambushes;
  hacked wearables leaking location or disabling HUD; hostile automated
  street-maintenance vehicles; proximity-triggered weaponized
  infrastructure (steam valves, transformers, water mains) with
  audio/visual tells; sensory-overload effects from hacked
  billboards/sirens; AI-issued PvP bounties turning other players into
  threats.
- **[Gemini]** Craftable EMP devices from scavenged parts as a
  player-side countermeasure.
- **[Gemini]** Environmental "tells": sprinting generates noise that can
  wake dormant IoT microphones, making stealth (walking/crouching) matter
  in high-threat areas without AI protection.
- **[Gemini]** Regional power-plant control as an environmental-manipulation
  mechanic — e.g. draining flooded areas for traversal, or electrifying
  perimeter fences for defense.
- **(curation note)** this category overlaps heavily with section 4
  (Runner threats) and section 9 (AFK protection) — they were generated
  across different prompts but describe the same underlying "real-world
  danger while distracted/traveling" problem space and could likely be
  unified into one system design.

### 9. AFK Protection & Automated Defense

- **[User]** Explicit new concept: players need some way to protect
  themselves while AFK.
- **[User]** Related concept: automated real-world self-defense via a
  friendly AI assistant — entering a new zone triggers rogue AI detection
  and IoT infiltration attempts, but the player's own AI assistant offers
  counter-protection; this scales with player level (see section 4,
  Runner mechanic, for how this rewards leveling).
- **[Gemini]** AFK-specific ideas: deployable "Faraday tents" that block
  local network visibility (consuming battery while active, breaking if
  bumped by another player); friendly-AI "node masking" that scrubs the
  player's location from local IoT databases; automated repositioning to
  the nearest safe cover while AFK.
- **[Gemini]** Automated defense ideas: friendly AI preemptively taking
  over local infrastructure (doors, traffic lights) on zone entry to deny
  it to the rogue AI; high-tier AI remotely defusing hazards before the
  player arrives; a safe-routing HUD overlay.

### 10. UGC & the "Uniquely Human Experience"

This is a named thematic pillar (see Part 1 — tone), not just a feature
category — the user was explicit that this needs real gameplay expression,
not just flavor text.

- **[User]** Player-submitted content pipeline: short (~30 second) videos
  for content moderation, shown on in-game TVs as ads/mini-shows,
  attributed to the submitting player.
- **[User]** Player-submitted music/audio usable in-game.
- **[User]** Player-submitted artwork usable in various in-game contexts
  (e.g. displayed as one-of-a-kind items).
- **[User]** Ties into a broader stated love of one-of-a-kind / globally
  unique item ownership generally (unique hardware, unique clothing, etc. —
  see also section 1, Progression & Economy).
- **[User]** Older, explicitly-deferred idea (mentioned but not being
  pursued now): letting players build entire quests and points of
  interest.
- **[Gemini]** Broad batch of thematic mechanic ideas, grouped:
  - *Analog broadcasting/media:* in-fiction "ham radio" voice broadcasts
    AI can't culturally decode; player video clips on CRT screens for a
    buff effect; raw analog audio overriding local AI combat algorithms
    and serving as ambient music; hand-drawn player schematics as in-world
    graffiti guiding players to hidden terminals; a disposable-camera-style
    photography system with limited, non-editable, "real-time-developed"
    photos.
  - *Tactile/analog mechanics:* cooking/culinary crafting for stamina
    buffs; analog vehicle repair via manual sequences AI can't automate;
    handwritten/traced signatures as a security measure exploiting human
    imperfection; offline physical text archives immune to AI scraping; a
    "hesitation" mechanic where too-perfect input rhythm gets flagged as
    AI/bot behavior.
  - *Culture, emotion, legacy:* "heirloom" hardware gaining
    provenance/value from its history of human ownership; player-placed
    memorials with custom epitaphs that the AI ignores (making them de
    facto safe landmarks); factions/enclaves trading on sentimental value
    rather than market price; deliberately imperfect, non-grid-aligned
    player-built structures as a visual signal of human construction; a
    fatigue system restored by resting and consuming other players'
    submitted art/poetry/music.
  - *Community & resistance:* player-created "Turing puzzle" locks relying
    on cultural context/humor; recorded oral-history audio logs
    ("cassette tapes"); player motion-capture used to generate
    intentionally-imperfect emotes that briefly confuse AI targeting;
    player-designed wearable patches/decals as non-digital identifiers;
    scheduled server-wide "Agora Fairs" with AI threat suppressed, for
    trading/displaying submitted media.
- **(curation note)** this is a large, enthusiastic **[Gemini]** batch (20
  ideas requested, 20 delivered) generated in response to a fairly open
  prompt — several read as very similar "AI can't replicate this specific
  analog/biological thing" flourishes (the cooking buff, the vehicle
  repair, the handwriting check, the hesitation-timing check) that could be
  consolidated into a smaller number of named mechanics rather than kept
  as 20 distinct systems. Worth a real prioritization pass with you rather
  than treating volume as a proxy for quality here.

### 11. Progressive Complexity / "Simple Mode"

- **[User]** Wants a non-toggleable, progressively-unlocking way for
  casual/less sophisticated players to engage with the game without
  needing to understand the full depth of its systems — not a simplified
  rule set exactly, but an abstracted interface/mechanics layer that
  unlocks more depth as the player advances, rather than gating everything
  up front. Should still let a casual player meaningfully play mini-games
  and do real-world content.
- **[Gemini]** Interface-abstraction ideas: terminal UI starts locked-down
  simple, with advanced views/metrics unlocking via in-fiction "OS
  upgrades"; shipping defaults to a flat-fee automated option before manual
  routing unlocks; granular stats hidden behind simple status-bar
  indicators until a "diagnostic module" is installed.
- **[Gemini]** Delegation ideas: new accounts get a restricted AI handler
  that auto-manages inventory/defense/selling until "jailbroken" for manual
  control; the Runner escort tether can offer a fully automated
  lock-on/auto-path mode.
- **[Gemini]** Asymmetric shared-space ideas: the same PvP instance (e.g.
  Domination) supports a simple role (e.g. manually driving one durable RC
  car to block a point) alongside an advanced role (full multi-drone
  command) in the same physical space; pre-built zero-configuration
  hardware loadouts alongside a fully manual component-slotting system.

### 12. Campaign Pacing ("The Prompt")

Mechanics side of the Part 1 narrative requirement — "The Prompt" must
support roughly a year of collective, non-repeatable progress without being
solved too fast by a small group.

- **[Gemini]** Sequential encryption-layer gating requiring aggregated
  server-wide GPU/hash contribution over a fixed window (e.g. 45 days) per
  layer.
- **[Gemini]** Physical long-haul infrastructure requirements (e.g. laying
  and defending fiber connections between zones) that halt progress if
  sabotaged until physically repaired.
- **[Gemini]** Large, targeted AI counter-offensives triggered by major
  milestones, forcing players to pause offense and defend their own
  infrastructure instead.
- **[Gemini]** Global depletion of physical crafting resources, forcing
  continual expansion into new geographic areas to sustain progress.
- **[Gemini]** A final step requiring multiple geographically separate
  objectives to be held simultaneously, forcing large-scale coordination.
- **(curation note)** none of these pacing mechanisms have been evaluated
  or chosen by the user yet — they're all **[Gemini]** proposals in
  response to the stated design problem, not decisions. This is a good
  candidate for a dedicated design discussion given how central "The
  Prompt" is to the whole game.

### 13. Milestone / Mass-Participation Events

- **[User]** Wants large-scale, player-count-gated events — e.g. if 5,000
  players are simultaneously logged into the terminal, it triggers a
  unique, repeatable, scalable large-scale event/attack producing a
  powerful reward. Explicitly wants this gated by **distinct,
  geographically-distributed player count**, not by resource-hoarding — a
  single wealthy player shouldn't be able to trigger it alone; a team of 10
  should be able to do something an individual fundamentally cannot,
  specifically because of network distribution. Motivation: to reward and
  encourage team/community play.
- **[Gemini]** "Distributed Processing Gates": top-tier AI targets have
  shielding that requires synchronized activation across separate
  geographic zones to breach, mathematically requiring distributed
  participation rather than concentrated force.
- **[Gemini]** "Concurrent Synthesis Protocols": crafting top-tier globally
  unique items requires real-time validation from a minimum number of
  distinct concurrent players, specifically to prevent one player's
  hoarded hardware from substituting for genuine multiplayer coordination.

### 14. Notes: Technical Feasibility Discussion (context only, not gameplay ideas)

The third conversation ended with Gemini estimating technical scope (team
size, timeline, suggested tech stack: C#/.NET, AWS ECS/SQS, PostgreSQL,
Godot/Unity) for building the full vision described across all three
conversations, landing on a 10-14 person team over 24-36 months for an MVP
beta. This is included here for completeness but should be read with two
caveats:

- It's a **[Gemini]** estimate produced with no knowledge of your actual
  project constraints (solo/small-scale, learning-project framing,
  explicit anti-scale-work philosophy — see the project's own
  `CLAUDE.local.md`). It reflects what a funded studio building the full
  vision as described might need, not a recommendation for how you should
  actually approach this.
- The current actual codebase (C#/.NET 9, UDP client/server, fixed-tick
  simulation, no async, no cloud infra yet) is a small fraction of this
  scope and was built independently of this brainstorming — no attempt has
  been made yet to reconcile "everything discussed here" with "what's
  actually buildable/desired at toy scale." That reconciliation — deciding
  which of these ideas are even in scope for this project as it actually
  exists — is a separate, later conversation.
