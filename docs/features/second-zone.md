# A second zone

**Date:** 2026-09-21
**Status:** Built (2026-09-22; the shop interior scene waits on assets, and the
shopkeeper stands on the street until then)
**Moved:** from the mmo-game repo on 2026-09-26. Built in the old MonoGame engine; the
design questions and answers still hold, the code notes do not.
**Sources read:** docs/world.md sections 3 (two worlds), 10 (the map), 15 (servers),
16 (the Training Grounds), 17 (how it looks), 18 (not in the game);
docs/first-playable.md; docs/backlog.md; docs/status.md;
docs/engineering/art-pipeline.md; docs/engineering/pipelines.md; Core/Zones;
Server/ServerServiceExtensions.cs; Client/Client.cs; art/mmo-game.project.json;
art/scenes/starter-zone.scene.json.
**Build from:** the two Outcome sections and the T-list of tests. The Q&A is the
record of how they were reached.

A second scene in the scene editor, and the first zone in the game that is not the
starter zone. Designed now because the author wants to build it next, and because the
game today runs exactly one zone, so a second one is the first test of every piece
that assumes one.

The answers are the author's own words, typed in conversation and recorded verbatim or
near it, one question at a time. Model additions are set apart and labelled:
"Consequence noted", "Options offered", "Note".

## Already decided

Decisions in force that this feature must fit, or change on purpose.

- The first playable is one town. "The generated wild, outskirts, a second town,
  travel" are written down as not in it. [first-playable.md, Q31, Q33]
- Places come in rings out from a town. The town (Old Town) is hand-built and is the
  hub. Around it a known wilderness of fixed layout, which can be several zones.
  Beyond that generated wilderness. [T1]
- Entering a building loads a separate, enlarged interior. The inside does not have to
  fit the outdoor footprint. No cutaways. [C-2026-09-18]
- Upper storeys are separate zones, like any interior. Floors above floors are out of
  scope. [art-pipeline.md]
- The world is discoverable. No blockers and no hard lock-outs on the map. [Q23]
- When one party member changes zones the whole party goes together. [B, Q23]
- Travel between towns is walking, driving, bus, train and flight, in about real time.
  Trains and planes run on a schedule. [B, T1, Q0, Q29, Q30]
- Every safe zone has multiple standard terminals, free, with full use. [Q12]
- The Training Grounds is isolated, cannot be returned to, and is built last. [Q16,
  Q23, Q31]
- The first town is put together by the author in the voxel scene editor and
  MagicaVoxel. Metro Minis is a placeholder. [C-2026-09-20]
- A zone transition is a `door.<name>` marker with a link to a target zone and an
  arrival marker. Planned, not built. [art-pipeline.md]
- The world runs on a real clock in the server's time zone. Lighting follows the hour.
  [C-2026-09-18]

Built:

- One zone. The server reads `Core/Zones/Data/1.json`, which names the scene
  `starter-zone`. The zone id comes from `ServerOptions.StarterZoneId`, default 1.
  `ZoneLoader.Load` reads the compiled scene for the bounds, the spawn and the
  dressing. One `ZoneDefinition`, one `ZoneDressing` and one `WorldSimulation` are
  registered as singletons for the whole server.
- The client loads the zone by the id the server sent at connect, from
  `Content/Zones/<id>.json`, through the same `ZoneLoader`. `ScreenManager` disposes
  the current `ZoneScene` and loads another when the id changes. Nothing changes the
  id after connect.
- A player's zone id and position are persisted in `players.zone_id`, `position_x`
  and `position_y`. `PlayerSaver` writes the server's one zone id. On connect,
  `ConnectHandler` spawns a new player at the one zone's spawn.
- The compiler compiles every scene under `art/scenes/` into
  `Package/zones/<scene>.zone.json`. A scene need not be a zone. A zone file maps an
  id to a scene name. Both server and client bake the package in and the server
  refuses a client whose stamp differs.
- Scene conventions the game reads: a `spawns` layer that is not drawable, one
  placement named `player` on it; free properties `sway`, `effect`, `sound`,
  `interaction` (terminal, workbench) and `terminal` (the terminal type).
- The ground is drawn by the client from the zone's bounds as one flat facet
  surface. No `ground.*`, `terrain.*` or `door.*` markers exist.
- The starter scene is 200 by 200 metres, two layers (`Default`, `spawns`), 187
  placements, one named.
- Model folders in the project: `art/models` and an external folder
  `C:\Users\geral\src\mmmm\vox`. Sixteen compiled models have no source under
  `art/models`, so `compile-art.ps1` fails on a clean checkout. [backlog.md]

Deferred:

- A second town, travel, the outskirts and the generated wild. Not in the first
  playable. [first-playable.md]
- The Training Grounds. Built last. [Q31]
- Asset organisation and a helpful compiler. Parked as a follow-up. [backlog.md]
- Glow, lights and blocking decided by model name or colour brightness instead of
  data. [TODO.md]
- Splitting `WorldSimulation` into subsystems. [CLAUDE.local.md]

## Feature design

### Developer thoughts

> Ok, I want to build a second scene, this will be my first new zone.

### Q&A

**F1.** In the fiction, what place is this second zone?

> Answer (2026-09-21): Its the outskirts of the first town, a wild are that can be
> explored and introduces "zones" or scene hopping. I am also considering doing an
> interier zone for a building in the first town.
>
> Consequence noted: the outskirts is the first ring out from the town, the known
> wilderness of fixed layout [T1]. The zone's job is to introduce zone changes, so
> the transition between the two zones is part of this feature, not only the scene.
> The interior is a second candidate, held as "considering", not decided.
>
> Note: clashes with first-playable.md, which lists "the outskirts" and "travel"
> under what is not in the first playable. Asked in F1a which wins.

**F1a.** Follow-up: the first playable lists the outskirts as not in it. Which wins:
does this zone join the first playable, or is it built outside that list?

> Answer (2026-09-21): Yes it joins the first playable, its an arbitrary explorable
> area with some minimal area of interest.
>
> Consequence noted: first-playable.md changes on purpose. "The outskirts" leaves
> "What is not in it" and an item joins "What to build". The zone has no job in
> the spine (the repair loop). Its job is exploration and the zone change itself,
> with a minimal point of interest. Listed in Consequences.

**F2.** How does the player cross from the town to the outskirts, and back?

> Answer (2026-09-21): It will be a road or dirt path that leads to the edge, this is
> still a to be dtermined mechanic, How do I indicate to the player that there is a
> way to traverse somewhere else adjacent to the current scene
>
> Consequence noted: the crossing sits at the zone's edge, on a road. The mechanic of
> the crossing is open. The author asked how to show the exit, recorded as F2a.

**F2a (author's question).** How do I indicate to the player that there is a way to
traverse somewhere else adjacent to the current scene?

> Options offered (asked for). Facts from the code first. The server clamps every
> player to the zone's box, so today the edge is an invisible wall. The camera stops
> 12 metres short of the edge so the ground always fills the screen. The client draws
> the ground as one flat surface to the box and nothing beyond it. Name plates over
> players and terminal screens exist and are drawn as sprites in the world. The scene
> has an `effect` property for a placement (`fire`, `fountain`) and a sign model
> (`obj_sign1`) in the package. The art pipeline plans a `door.<name>` marker with a
> link to a target zone and an arrival marker, not built. The four ways to show an
> exit, from cheapest to most visible:
>
> 1. The art alone. The road runs to the edge and does not stop. A signpost model
>    stands beside it. Players read a road that leaves the screen as a way out.
> 2. A name over the exit. The destination's name ("Outskirts") drawn as a plate
>    above the door area, the way player names are drawn, visible from a distance.
> 3. A visible threshold. A strip of glow or drifting particles across the road at
>    the boundary, through the existing `effect` property. The Guild Wars zone portal
>    is this: a shimmer you walk into.
> 4. A prompt on approach. Text near the player when within a metre or two, "Walk on
>    to the Outskirts". Needs a proximity prompt the client does not have yet.
>
> The one I would take: 1 plus 3, and 2 if the plate is cheap. The road is the real
> signal, and a glow at the edge marks where the wall becomes a door, which matters
> because the rest of the edge stays a wall. Effects are a large part of the look by
> decision [C-2026-09-20], so a shimmer fits. Option 4 is the only one that needs new
> UI, and the outskirts do not need it to be understood.
>
> For the mechanic itself, the open part of F2: the crossing can happen when the
> player walks into the door area, or when they stand in it and press use. Walking
> in is the Guild Wars manner, and a road that just continues reads as walking. The
> arrival marker in the other zone sits a few metres inside its own door, so the
> player does not land on the return trigger. I would take walk-in.
>
> Decision (2026-09-21): Ok yea, 1 and 3 make sense. and yes, walk in. fade to the
> new scene entrance spawn.
>
> Consequence noted: the exit is shown by the road and a threshold effect at the
> boundary. No name plate, no prompt. The crossing is walk-in. The client fades out,
> loads the other zone and fades in at that zone's entrance spawn. So a zone now has
> more than one spawn: the new-player spawn and one arrival spawn per door.

**F3.** What is the minimal area of interest in the outskirts, and what does the
player do there?

> Answer (2026-09-21): I am thinking its just a brief wooded area with a fountaion and
> some scenery. In one location we have old hardware chest with basic loot in it, this
> could be the location of the required batter but probably not.
>
> Consequence noted: "thinking" and "probably not" are hedges and stay hedges. The
> scenery is the existing pipeline: trees with `sway`, a fountain with its `effect`.
> The chest is the one new thing. Today items lie on the ground in four tiers and are
> picked up by walking over them, a test mechanic [Q12]. A chest is a container, and
> the game has no container. The battery's location stays with first-playable item 2,
> "somewhere in town", unless the author moves it.
>
> Note: [Q12] says no forest full of PC parts, and salvage is the planned source of
> parts. One chest of old hardware is salvage. It fits.
>
> Added by the author (2026-09-21): The whole purpose of it is to introduce the zone
> change mechanic.
>
> Consequence noted: the chest and the scenery are dressing for the mechanic, not
> features of their own. Where the chest would need new work, the smallest thing
> wins.

**F3a.** Follow-up: how does the player take the loot from the chest?

> Answer (2026-09-21): open it by clicking and drag/drop to invenotyr or double click
> to insta move.
>
> Consequence noted: this is the workbench pattern. The workbench is selected by
> clicking its footprint and opens a panel with slots, and items drag between the
> panel and the inventory. A chest is a second interaction kind with the same panel
> and its own contents. Double-click to move is new for both panels. Unlike a
> workbench, a chest holds items the server owns and other players can see taken, so
> it is server state, not only a screen. That is more than the ground pickup that
> exists, and the author chose it knowing the chest is dressing. Asked in F3b what
> the chest holds after a player takes from it, because that sets how much state.

**F3b.** Follow-up: after a player takes the loot, what does the chest hold, for that
player and for the next one?

> Answer (2026-09-21): Its empty, its a random spawnable item every N minutes. So
> there is a chance there is nothing in it for the player.
>
> Consequence noted: the chest is shared world state. One item at a time, taken by
> whoever gets there first, empty for everyone until the server refills it on a
> timer with a random item. N is a number in the game's data, not in the scene. The
> item table is the game's too. This is the first world object with state the server
> changes on its own clock, ahead of the street lights in first-playable item 3.

**F4.** Is the outskirts safe in this build, or does anything threaten the player
there?

> Answer (2026-09-21): yes its safe, no dangers yet
>
> Consequence noted: fits first-playable, "nothing hurts you in the first town",
> now extended to the outskirts. "Yet" keeps the wild as the place danger arrives
> later [T1].

**F5.** The interior zone you are considering: is it part of this session, or its
own session after the outskirts?

> Answer (2026-09-21): its part of the town
>
> Note: the answer places the interior in the town, but does not say whether this
> session designs it. Asked in F5a.

**F5a.** Follow-up: so is the interior designed here, in this session, or later?

> Answer (2026-09-21): oh this session.
>
> Consequence noted: this session designs two new zones, the outskirts and one
> interior in the town. Both use the same door mechanic from F2a. The interior is
> the second case of it and the first use of the decision that an interior is a
> separate, enlarged zone [C-2026-09-18]. The file title stays; the Outcome lists
> both zones.

**F6.** Which building in the town gets the interior, and what is inside it?

> Answer (2026-09-21): An electronics shop, this is where you can purchase the
> battery.
>
> Note: clashes with first-playable.md on two lines. "Dollars as a working currency,
> vendors, the recycler" are not in it: "Prices can show; nothing is bought." And
> item 2 is "a battery to find in town", found, not bought. The player also arrives
> with a phone at ten percent and nothing else, so no money to buy with. Asked in
> F6a which wins.

**F6a.** Follow-up: the first playable says nothing is bought, and the battery is
found. Which wins: a working shop with Dollars in this build, or the shop as the
place the battery is found?

> Answer (2026-09-21): We can use money in the first build, give the player $10 to
> start, a battery costs $8 and is sold in the ship.
>
> Consequence noted: first-playable.md changes on purpose in three places. Dollars
> become a working currency in the build. A vendor exists. Item 2, "a battery to
> find in town", becomes "a battery to buy in the electronics shop". The player's
> starting kit gains $10 beside the phone at ten percent. Dollars are already the
> currency of the early game in world.md [Q16], so the world does not change, only
> the build's scope. A wallet is new state on the player, persisted. Listed in
> Consequences.

**F6b.** Follow-up: how does the purchase happen inside the shop? What does the
player click, and what do they see?

> Answer (2026-09-21): They see the inside of a shop, its big enough to walk around
> and explor rows/isles. Only a few. everything is cosmetic in the isles so its
> scenery that matahces an electronic shop. We might put posters on the wall that
> advertise "Drones" or something like that for foreshadow9ing. To buy the player
> does what a player does in every mmo, click on the ship keeper. They are shown a
> list of items to purchase, this one has only the battery for now (maybe a few
> other canonical items we come up with along the way that are out of price range
> of %10 but thats it)
>
> Consequence noted: the shopkeeper is the first NPC a player acts on. NPCs today
> are scenery, `chr_*` models you walk through. A vendor is a third interaction
> kind beside terminal and workbench, set on the shopkeeper's placement, opening a
> list with a price per item and a buy action. The list is the game's data, keyed
> by the vendor's name in the scene, so the scene says "this is a vendor" and the
> game says what they sell. Items above $10 show and refuse, which is the "prices
> can show" line of first-playable kept. Posters are art. Aisles are scenery that
> blocks by the height rule.

**F7.** When one party member walks through a door, does the whole party go with
them in this build, as decided for the game?

> Answer (2026-09-21): Yes, if they have a party thats the mechanic, party travels
> between scenes. At some point I wnat to differentaiate between world scenes and
> town scenes and change that. So world scenes cause party travel but inter-town
> zone scenes dont.
>
> Consequence noted: in this build every door moves the whole party, the shop door
> too. The later rule needs a zone to know its kind (world or town), and the art
> pipeline already plans a `kind` on a zone. The zone file can carry the kind now
> without the rule reading it, so the rule slots in later. The rule itself is
> deferred. Follow-up asked, because the party has to arrive somewhere.

**F7a.** Follow-up: where do the other party members appear when they are pulled
through a door, and what do they see happen?

> Answer (2026-09-21): as a group at the entrence, to the zone. So zones/scenes (I am
> using these interchangably) will have multiple spawn points. I think the most
> common is the connecting spawn point, or zone transition spawn point.
>
> Consequence noted: the party lands together at the arrival spawn of the door they
> came through, spread so they do not stand in one spot. What they see is not said;
> the same fade as the player who walked in is assumed until the author says
> otherwise. Spawn kinds so far: the new-player spawn, one per zone, and an arrival
> spawn per door, the common one. On words: the code keeps them apart. A scene is
> the editor's file, and a zone is the game's use of one, named in a zone file. The
> author uses them interchangeably in conversation. The file keeps the code's split
> and does not ask the author to change their speech.

**F8.** How large is each of the two zones, and where do their models come from?

> Answer (2026-09-21): Eache zone has its own scene. I think the town is much larger
> than the outskirts for this build. The outskirts are intentionally kept small here.
>
> Consequence noted: one scene per zone, three scenes in the project. The outskirts
> is small on purpose; no number given, and none needed by the design. The shop's
> size is "big enough to walk around" from F6b. Where the models come from was not
> answered; asked in F8a.

**F8a.** Follow-up: where do the models for the outskirts and the shop interior come
from?

> Answer (2026-09-21): We have enough models now to do a simple wooded area. I need
> to find interier assets for the shop
>
> Consequence noted: the outskirts scene can be laid out today from the models in
> the project. The shop interior waits on an asset search: walls, floor, shelves,
> counter, posters. That search is the one blocker in this feature, and it sits on
> the art side, the same gap first-playable names. Two more models the design needs
> and the project may not have: a chest, and a small marker model for the door
> threshold to hang its effect on. The shopkeeper can be any `chr_*` model.

### Outcome

- Two new zones, each its own scene: the outskirts of the town, and the interior of
  an electronics shop in the town. [F1, F5a, F6, F8]
- Both join the first playable, which changes on purpose. [F1a, F6a]
- The purpose of the outskirts is to introduce the zone change. Everything in it is
  dressing for that. [F1, F3]
- The outskirts is a small wooded area with a fountain, scenery, and one chest of
  old hardware. It is safe. [F3, F4, F8]
- A door is a place at a zone's edge where a road leads out. The exit is shown by
  the road continuing and a threshold effect across it. No plate, no prompt. [F2,
  F2a]
- Crossing is walk-in. The client fades out, loads the other zone, fades in at that
  door's arrival spawn. [F2a]
- A zone has a new-player spawn and one arrival spawn per door. The arrival spawn is
  the common one. [F2a, F7a]
- The whole party goes through a door together and lands as a group at the arrival
  spawn. In this build, every door. [F7, F7a]
- The chest opens by click into a panel. Items drag to the inventory, or double-click
  to move. It holds one random item, refilled by the server every N minutes, empty
  for everyone once taken. [F3a, F3b]
- The shop is big enough to walk around a few aisles of cosmetic scenery, with
  posters that foreshadow later equipment. [F6b]
- Buying: click the shopkeeper, see a list with prices, buy. The list holds the
  battery at $8 and may hold a few canonical items above the player's means, which
  show and refuse. [F6a, F6b]
- Dollars work in this build. A new player starts with $10. [F6a]
- The outskirts can be laid out from the models the project has. The shop interior
  waits on an asset search. [F8a]

**F9 (added by the author at close).** Scene shape.

> Answer (2026-09-21): scenes dont need to be square or the same size as other
> scenes. I also want to be able to cutparts of scenes away (dead grass ad the edge.
> to give shape to the scene and reduce. So I think that means a floor erasal tool or
> some way to delete the coordinates of a scene so it has a shape thats non square.
> IF this is a difficulty (maybe it is cuase then you need to store the shape or
> individual tiles of the floor instead of just anX*y square.
>
> Consequence noted. Facts: a scene carries its own `width` and `depth`, the bounds
> are that box centred on the origin, the server clamps to it, and the client draws
> one flat surface over it. So two things the author asks for are already true:
> scenes differ in size, and a scene need not be square, only rectangular. The third,
> a shape cut away at the edge, is not: the box is the only floor the game knows.
> Two ways to hold a shape, both touching the editor (other repo), the compiler and
> both programs. One, the floor is drawn from `ground.<surface>` placements, which
> art-pipeline.md already plans as areas, and the compiler derives the walkable
> ground from them; the box stays the outer limit and what no ground covers is not
> walked on and not drawn. Two, the scene file carries a cell mask of the floor.
> The first keeps the scene a layout of placements, which is the tool's design,
> and needs no new file shape; it is the one I would take. Left open on purpose:
> the author closed the session here, and it is its own piece of work, not this
> feature's. Recorded in the backlog.

Deferred:

- Non-rectangular scenes: floor cut away at the edge. Own session; the box stays
  for these two zones. [F9]

- World zones move the party, town zones do not. Needs a zone kind. The author wants
  it "at some point". [F7]
- Danger in the outskirts. "No dangers yet." [F4]
- The chest as the battery's location. "Probably not"; the shop sells it. [F3, F6]
- What the pulled party members see. The same fade is assumed. [F7a]
- Where the $10 comes from in the fiction. Not asked; the Training Grounds is built
  last and the player arrives "as if" they left it. [F6a]

## Technology design

### Developer thoughts

> (2026-09-21) I think the biggest question is on the zone connection topic. My
> thought is to have the scene also place the exit and entrances for a scene or zone.
> So in the spawns layer (or any layer of my choosing). I add a property spawn_type =
> exit and connects_to=<scene_name>.. This also brings up another point. Some
> "Prepackaged props" are really an object I want to attach (a collection of props).
> So I like the idea of building that into the scene editor. "I want to build an
> object, which is a collection of properties and give it a name. When i put that
> object on one of my placements (just like a regular prop UX) it adds those
> predefined propertis in a sub-panel grouped by the object name. So in this
> instance, I do the following
>
> - Click "Define Custom Object" button
> - Enter a name: "Scene Transition Trigger"
> - Supply Properties for Object: <prop addition widget>  ... { "type" :
>   "enter|exit|two-way", "connecting_scene" : "old-town.scene", }
> - Then I click some placement on the scene
> - On the inspection panel I add my custom object and supply the property values I
>   want for it.
>
> Consequence noted: two proposals. First, the door is a placement in the scene with
> properties, the same way a terminal is. That is the art pipeline's plan
> (`door.<name>` with a link) in property form, and it fits the rule that the scene
> says what a thing is and the game says what that means. Second, the editor gains
> named property bundles. That is a change to Voxel Scene Maker, a separate repo that
> is generic on purpose; a bundle is a generic idea (a named group of props from the
> project's vocabulary), so it fits the tool's design. What this repo needs from it
> is only the shape the bundle takes in the scene file, which the game's reader has
> to parse. Asked in the T questions. One fact to hold: the `spawns` layer is not
> drawable, and the threshold effect from F2a needs a drawn placement to hang on, so
> the trigger and the effect are either two placements or one on a drawable layer.

### Facts held in mind

What the code does today for the pieces this feature touches, in play terms, with
current names. Live means a player meets it in play. Scaffolding means it exists and
nothing reaches it.

- **One simulation, one zone.** `WorldSimulation` owns every player, ground item,
  terminal, workbench and party, with one `ZoneDefinition` and one `ZoneDressing`,
  all singletons. Movement is clamped to the zone's box and blocked by the dressing's
  obstacles. Live.
- **Who sees whom.** `RadiusInterestPolicy` decides per pair of players, per item,
  per terminal. Each tick the world is marked dirty, `GetWorldViewFor(player)` is
  built per session and sent as `ServerWorldViewUpdate`. Live. `WholeZoneInterestPolicy`
  exists and is not wired. Scaffolding.
- **Connect.** `ConnectHandler` posts a load to the persistence worker with the
  server's one zone id and spawn. When the database answers, the player is added at
  the saved position and `ServerConnectionStatus` carries `ZoneId` once. Nothing
  sends a zone id after that. Live.
- **Persistence.** `players.zone_id`, `position_x`, `position_y` exist.
  `PlayerSaver` writes the server's one zone id every save, so the column is always
  1. Written, never read for a choice. Scaffolding. Migrations live in
  `Data.Migrator/SchemaMigrations`.
- **The client's zone.** `ServerConnection.ZoneId` is set from the connection
  status. `ScreenManager.ShowWorld` loads `Content/Zones/<id>.json` through
  `ZoneLoader`, builds a `ZoneScene`, the HUD and the `WorldScreen`, and clamps the
  camera to the bounds. Leaving the world disposes the scene and clears every cache.
  There is no path that swaps the zone while in the world. Live.
- **Interactables.** The `interaction` property makes a placement a terminal or a
  workbench (`InteractionKind`). The simulation keeps workbenches by placement id
  and terminals by a Guid derived from it. Use needs the player within
  `TerminalReach`, 1.5 m. The client selects a workbench by clicking its footprint
  and opens a panel; intents go as `ClientWorkbenchApply` and `ClientWorkbenchRemove`;
  failure is `ServerMessageRejected` with a `RejectionReason`. Live.
- **Items.** Stacks and instances in one `Inventory`; `ItemCatalog` holds the
  definitions and sends them one per message after connect. Ground items come from a
  `LootTable` at start and are picked up by walking over them. No currency, no
  vendor, no container. Live, and the ground pickup is a test mechanic by decision.
- **Party.** `PartyRoster`, up to 6, a private view per member. It knows nothing of
  position or zone. Live.
- **Scenes.** The compiler turns every scene in `art/scenes/` into
  `Package/zones/<scene>.zone.json`; a zone file `Core/Zones/Data/<id>.json` maps an
  int id to a scene name. `ZoneConventions` holds every word the game reads from a
  scene: `spawns`, `player`, `sway`, `effect`, `sound`, `interaction`, `terminal`.
  Properties are flat strings on a placement, its layer or the scene. The editor's
  vocabulary is `art/props.json`. Live.
- **The edge.** Server clamp, camera margin of 12 m, flat ground to the box. Live.

### Q&A

**T1.** When two players stand in different zones, what does the server hold for
each zone, and what is shared between them?

> Answer (2026-09-21): The server knows what player is in each zone for multiple
> mechanics (chat, party forming, future eventing logic for localities). Global world
> state is shared between two zones, which doesnt really apply to first playable.
>
> Consequence noted: per zone, the server holds the set of players standing there,
> and with them what only that zone has: its dressing, its ground items, terminals,
> workbenches, the chest. Shared: parties, chat, the world clock, accounts, and later
> global world state. Per-zone membership is a query other systems ask ("who is in
> zone X"), so it is a first-class fact, not a side effect. The answer does not say
> how the code holds it; asked in T1a.

**T1a.** Follow-up: in code, is that one `WorldSimulation` per zone, or the one
simulation with a zone on every player?

> Answer (2026-09-21): Oh interesting question. In my mind it was one WorldSimulation.
> I guess it comes down to if there is benefit in splitting it or if we need to
> simulate interactions across zones. I would need to think more.
>
> Options offered. Facts first. `WorldSimulation` holds, in one class: the players,
> the ground items, the terminals, the workbenches, the one zone's bounds and
> dressing, the interest policy, the loot table, and three things that are already
> their own objects, `InventoryStore`, `PartyRoster` and `TerminalAccess`. Movement
> clamps to the one zone and collides with the one dressing. `Tick` walks every
> player. `GetWorldViewFor(viewer)` asks the interest policy about every other
> player, item and terminal. Handlers call it by player id and know nothing else.
> `CLAUDE.local.md` warns about this class growing and lists "splitting
> WorldSimulation into subsystems" as deferred; it also records that the author
> floated delegating to sub-simulations later. Nothing in the first playable needs
> two zones to interact: parties are not spatial and chat is global.
>
> The T1 answer already split the state in two: per zone (who stands there and what
> only that zone has) and shared (parties, chat, clock, later global state). The
> three options are three places to put that line.
>
> A. **One simulation, a zone id on every player.** Every per-zone collection gets
>    keyed by zone, every method filters by the player's zone, and the interest
>    policy answers "different zone, do not see" first. Least new structure. The
>    class grows, and a forgotten filter shows a player through the wall between
>    zones, which a test has to catch each time.
> B. **One simulation per zone, a world above them.** What the class does today is
>    per-zone, so it becomes a `ZoneSimulation` made once per zone. A new world
>    object holds the shared parts and the player-to-zone map, and a door move is
>    remove from one zone, add to the other. Cleanest seam. Every handler and test
>    that talks to `WorldSimulation` changes its entry point.
> C. **B inside A's door.** `WorldSimulation` stays the one entry point handlers
>    and tests call, and holds the shared parts and the player-to-zone map. A new
>    `Zone` object holds what is per zone: bounds, dressing, the players standing
>    there, items, terminals, workbenches, the chest. The simulation finds the
>    player's zone and hands the call down. One tick loop ticks each zone in turn.
>
> The one I would take: C. The code then mirrors the design's own split from T1,
> "who is in zone X" is the zone's player set and nothing has to filter, a view is
> built from one zone so cross-zone invisibility costs nothing, and handlers stay as
> they are. It is the sub-simulation direction the author floated, cut by zone
> rather than by subsystem, and cut now because the feature needs it, which is the
> case `CLAUDE.local.md` reserves for the author's call. Cross-zone interaction, if
> it ever comes (an event that spans localities), lives in the world object, where
> it can see every zone.
>
> Convention flagged: this touches the deferred "splitting WorldSimulation" item.
> The proposal splits by zone, not by subsystem, and leaves the subsystem question
> where it is.
>
> Decision (2026-09-21): I like C, I think C also allows me to pass in other zone
> details to individual zones if needed in the future without rework, Thin
> old-town-outskirts has some wild invasion, we could announce that in old-town
> proper to get help...
>
> Consequence noted: option C. A `Zone` object per loaded zone inside
> `WorldSimulation`, which keeps the shared parts and routes by the player's zone.
> The world object is where one zone's event reaches another zone's players, later.

**T2.** How does a door in one scene name the exact arrival spot in the other scene,
when two scenes may be joined by more than one door?

> Answer (2026-09-21): Right, so we only have "this is an exit" and it leads to "X",
> but what it X has two entrances. I think this should be settled early to avoid
> reworks later. I think we go with named transitions. So we for the ship example.
> We will ahve a front door and a back door. You can walk down old-town street, go
> in the ship front door and then out the back door into old-town 2nd street. The
> tranistions are
> - old-town-transition-street-1, old-town-ship-transition-ship-front,..... you get
>   it. So everything is a transition with a name, and its only property becomes
>   what its connected to.
>
> Consequence noted: a door is a placement that is a transition, with a name, and one
> property: the name of the transition it connects to. The enter/exit/two-way type
> from the developer thoughts falls away: a door is two-way when each end names the
> other, one-way when only one does. The arrival spot is the far transition's own
> position, so no separate arrival marker exists. The compiler can check that every
> connection names a transition that exists and warn when the two ends do not agree.
> The shop gains a back door onto a second street, which is a third door in the town
> scene and a second in the shop. Asked in T2a how the name finds its scene.

**T2a.** Follow-up: is a transition's name unique across the whole project, so
"connects to X" finds X in any scene, or does the connection name the scene too?

> Answer (2026-09-21): lets require specifying the name of the scene, so we dont have
> to worry about duplicates. So the PK for the transition is composit (transition
> name and source_scene and target_scene)... although you only need to set the
> target in the props.
>
> Consequence noted: a transition is identified by its scene and its name, unique
> within a scene, which the compiler checks. The connection names the target scene
> and the target transition. The source scene is the file the placement is in, so
> it is never written. The word "scene" is right here: a transition joins scenes,
> and the game maps a scene to a zone when it loads it.

**T3.** How does the game recognise a placement as a transition, and what do its
properties look like in the scene file?

> Answer (2026-09-21): we talked about this earlier, we do a flag of some sort, this
> is almost the Kind=transition, but I feel like its not exactly that. I feel like
> "Kind" is a too generic catch all. I think the props are Name of the transition
> instance (the one we are speciying), the target scene, and the target scene
> transition). So we need 3 pieces of info and we inherit 1 from the scene we are
> currently in.
>
> Consequence noted: no generic "kind" word. Three pieces on the placement: its
> name, the target scene, the target transition. The flag is the presence of the
> target: a placement with a target is a transition, the way a placement with
> `interaction=terminal` is a terminal. The name is the placement's own `name`
> field, as `player` is on the spawn. In the file, the editor's bundle "Scene
> Transition" flattens to prefixed properties, in the shape the reader already
> parses: `transition.scene` and `transition.name` as a first proposal, the prefix
> being the bundle's word and both words going into `ZoneConventions`. The reader
> treats a placement with one of the two and not the other as an error at compile
> time. `interaction` is not the right family: a transition is not used by a click,
> it is walked into. Names open to the author's change.

**T3a.** Follow-up: which layer does a transition placement sit on, and is it drawn?
The threshold effect from F2a needs a drawn placement to hang on.

> Answer (2026-09-21): any layer, we do not restrict things to specific layers yet.
>
> Consequence noted: the reader finds transitions on every layer, drawn or not. The
> `spawns` layer rule for `player` stays as it is. At layout time the author
> chooses: one placement on a drawable layer, a small marker model with the
> threshold `effect` on it, which is both the trigger and the shimmer; or a hidden
> trigger and a separate drawn placement for the effect. The trigger's footprint
> must not block, or the player cannot walk into it, so the marker model is under
> the blocking height or the dressing exempts transitions.

**T4.** What area on the ground triggers the crossing when a player walks into it?

> Answer (2026-09-21): I think we need an area of effect warning, which also helps
> players know that it does something. Along with a sound would be ncie to have. At
> some close point, very close they get locked into the transition and it occurs.
>
> Consequence noted: two distances from the transition's centre. An outer one where
> the client warns: the threshold effect responds to the player's approach, and a
> sound plays, which the existing `sound` property on the same placement already
> gives by distance. An inner one, very close, where the server locks the player in:
> movement is ignored and the crossing happens. Both numbers are the game's, in
> code beside `TerminalReach`, not in the scene. The warning is not a text prompt;
> F2a ruled that out. It is the effect and the sound.

**T5.** Once the server has moved a player into the other zone, what is the client
told, and what does it do with it?

> Answer (2026-09-21): The client is told you are now in zone X, this is the scene,
> load it, this ist he zone and world state for the zone you're in render id.
>
> Consequence noted: a new server message, one per crossing, carrying the zone id
> and the arrival position. The scene itself is not sent: the client has the
> package baked in and loads `Content/Zones/<id>.json` as it does at connect. The
> world state follows in the ordinary world view updates, now built from the new
> zone. On the message the client fades out, disposes the `ZoneScene`, clears the
> world view cache so no entity from the old zone is drawn for a frame, loads the
> zone, rebuilds the world screen and HUD, and fades in. `ShowWorld` does most of
> that today; the new part is doing it without leaving the world. The pulled party
> members get the same message, so they need no other path.

**T6.** When a player disconnects in the outskirts or the shop and connects again,
where do they appear?

> Answer (2026-09-21): They appear back in the wild for our first playable (and
> future) but we dont have the AFK and idle sleeper or tent mechanics for first
> buildable.
>
> Consequence noted: where they were, in the zone they were in. `players.zone_id`
> becomes live: `PlayerSaver` writes the player's own zone, and `ConnectHandler`
> adds the player to the saved zone at the saved position. A new player still
> starts at the starter zone's `player` spawn. A saved zone that no longer exists
> in the package is the one failure case; the loader refuses rather than guesses
> today, and a connect should fall back to the starter spawn and say so in the log,
> because refusing a connect over a removed test scene would lock the account out.
> The log-off modes (sleeper, sheltered, autonomous) [Q22] stay deferred.

**T7.** The chest: where does its state live, who ticks the refill, and how does a
player's take reach the server?

> Answer (2026-09-21): The chests state lives on the server, it has an inventory and
> is an instance item with a fixed position and the scene/world is the owner. The
> server decides the refill, for now we hardcode this chest to have 1 single item at
> all times, it can be empty for 10 minutes and then refill. so I guess the tick
> timer starts after someone takes out of it, it refills in 10 minutes. This is
> first playable mechanic and not a common one.
>
> Consequence noted: the chest is an `ItemInstance` whose owner is the zone, with
> one slot that holds one item. That is the existing tree: the player owns the
> phone, the phone owns the battery, the zone owns the chest, the chest owns its
> one item. A take moves that item's owner from the chest to the player, the same
> move an equip makes, checked for reach as a workbench is. The zone ticks the
> chest: empty for 10 minutes, then one random item from a table. The 10 minutes
> and the table are code, beside the loot table that exists. Not persisted: a
> server start finds the chest full. Assumed, since the author did not say and the
> mechanic is "not a common one". The chest is the first instance owned by
> something that is not a player, so `OwnerId` gains a zone owner.

**T8.** The wallet and the shop: where do the player's Dollars live, and what does
a purchase do on the server?

> Author's note (2026-09-21), before answering: We need to remember here we are
> designing for the first playable with most of these current QA sessions.
> Sometimes full game gameplay leaks in.
>
> Note: taken as a rule for the rest of the session. Every T answer is for the
> first playable. Where a full-game shape is cheaper to build now than to rework
> later, the file says so and the author picks. T8 stands, in that scope.
>
> Answer (2026-09-21): For now money is just a $$ displayed in the inventory on a
> label "Pocket Change: $10". The dollars live on the server alongside the player
> inventory, I guess in theory dollars are a stackable item that doesnt show in the
> inventory, but actually tahts dumb, the money acts differently than an item. But
> the dollars live somewhere, I think long term we have player bank accounts that
> are transactional have history and balances, loans, etc... maybe for now the
> bankaccount is a backend tool to server a simple label in the ui/ux to show the
> player they have N dollars. Purchase on the server. Player sends intent "Buy Item
> N from Shop Z", server verifies its an eligable purchase and confirms the
> transaction and item movement via world state, this is another one where I think
> a bespoke Accepted Message would be helpful but we really dont need to, maybe we
> do though.. im on the fence. I think this also means the server has control of
> shops and I think this is required long term also to control purchasing power and
> economics, so I can rais prices for items to scale inflation etc...
>
> Consequence noted: Dollars are a balance, not an item. For the first playable it
> is one whole-dollar integer on the player, persisted as a column beside position,
> carried in the inventory view so the HUD's "Pocket Change: $10" label and the bag
> arrive in one snapshot. The bank account with history is the long-term shape and
> is deferred; a single balance column becomes the account's opening balance when
> that comes. New players start at 10. The vendor is server data: a list per vendor
> name of item and price, so prices are the server's to change. The intent is one
> message, buy this item from this vendor; the server checks reach to the
> shopkeeper, that the vendor sells it, and that the balance covers it, then
> deducts and gives.
>
> Options offered (the author is on the fence about an Accepted message). Fact: the
> convention from the phone build is that success is the next snapshot and failure
> is `ServerMessageRejected` with a reason; equip, unequip, terminal use and the
> workbench all follow it. With the balance in the inventory view, one snapshot
> after a purchase shows both the new item and the new balance, so the client has
> nothing an Accepted message would add except a moment to play a sound or a
> notice. The choice: keep the convention and let the panel react to the snapshot,
> or add an Accepted message and deviate. I would keep the convention for the first
> playable; the reason to deviate arrives when a purchase has a result the
> snapshot cannot show, such as a receipt or a delivery time, which is the
> marketplace idea in the backlog, not this build.
>
> Decision: held, see T8a.

**T8a (author's question).** Assume frame N we send intent, on frame N+1 we dont
have a state change, on frame n+5 we still dont see any state change... how long do
we wait until we assume (well the money didnt move and the item didnt move so I
guess this failed)?

> Options offered (asked for). Facts first. No intent today has an acknowledgement
> or a timeout. Equip, unequip, terminal use and the workbench send once, and a
> lost datagram is silent; the recovery is the player clicking again. Connect is
> the same, with a comment in `MenuScreen` saying so. Movement is the one exception:
> it repeats every 0.1 s while moving, so loss does not matter. A snapshot and an
> Accepted message are both datagrams too, so either can be lost the same way; an
> Accepted message does not remove the silence, it only names the outcome when it
> arrives. The backlog already holds the reliability layer (acks, ordering,
> fragmentation) as its own session.
>
> The question is not how long to wait. It is what the client does when the wait
> ends, and that is where a purchase differs from an equip: clicking again on an
> equip that already happened does nothing, and clicking again on a buy that
> already happened buys twice. So a purchase needs the server to see a repeat and
> answer it once. Three options:
>
> 1. **Today's convention, unchanged.** The buy button stays live. The player
>    clicks again after a silence. A double purchase is possible and shows at once
>    with $10 in hand.
> 2. **A pending state and an intent id.** The buy message carries an id the client
>    makes. The panel shows the purchase as pending and disables the button. If no
>    snapshot or rejection has answered within about a second, the client resends
>    the same id; the server remembers the last few intent ids per player and
>    answers a repeat by resending the current snapshot, without buying again. The
>    changed balance is the acknowledgement. No new message type.
> 3. **Option 2 plus an Accepted message** naming the intent id. Adds an explicit
>    answer when the snapshot cannot carry the outcome. Nothing in this build needs
>    that.
>
> The one I would take: 2. It is the first case of the reliability layer, built for
> the one intent where a repeat costs the player something, and the general layer
> in the backlog grows from it rather than replacing it. The convention gains one
> line: an intent that spends something carries an id and the server acts on each
> id once. The about-a-second is a number in code.
>
> Answer (2026-09-21): I dont like 2, it feels too hacky. How much does an ack
> message cost. Client generates an intent message with a unique id, server send
> ServerAccepted { IntentId: <id> } for simple things like this one.
>
> Options offered (asked for): the cost. Facts. A message is a class with a
> `MessageType`, a fixed `MessageSize`, and hand-written serialize and deserialize;
> there are 25 types. `ServerMessageRejected` is 2 bytes and carries only a reason,
> no id, so today a rejection cannot say which intent it answers. A Guid is 16
> bytes; `ClientEquipItem` is 29 bytes with the session id.
>
> What `ServerAccepted { IntentId }` costs: one message class of 17 bytes, one
> message type, one client handler, an `IntentId` field on the buy message, and a
> pending map on the client keyed by id, cleared by Accepted or Rejected. For
> Rejected to answer the same way it gains an `IntentId` too, and its size goes
> from 2 to 18 bytes. That is small: a few files, an afternoon.
>
> What it does not buy, said plainly. The ack answers "did the server hear me".
> It does not stop a double purchase by itself, because the ack can be lost like
> any datagram, and then the client has a pending intent, a server that already
> sold, and a player about to click again. The server has to recognise a repeated
> intent id and answer it without selling twice, whichever way the answer travels.
> That is the part of option 2 that stays, and it is not the hacky part: it is one
> small set of recent ids per player. The hacky part was using the snapshot as the
> ack, and the author's proposal replaces exactly that.
>
> So the shape is: intents that spend carry an `IntentId`; the server acts on each
> id once and answers every id with `ServerAccepted` or `ServerMessageRejected`
> carrying it; the client holds the intent pending until one arrives and resends
> the same id after about a second. Equip and the rest keep sending without an id
> until they want one; the field is optional by message, not a rule for all. I
> would take this. It is option 3 with the reason for it stated.
>
> Decision (2026-09-21): Ok bad name. The idea is for any intent message from the
> client can be uniquely identified and "Approved" from the server. This would be
> for things where we want to try to confirm things.
>
> Consequence noted: a general shape, not a purchase-only one. Any client intent
> may carry an intent id. An intent that wants confirmation is answered by
> `ServerIntentApproved` with the id, or `ServerMessageRejected` with the id and
> the reason. Approved and Rejected are the pair; the earlier "Accepted" is
> withdrawn. The buy is the first intent to use it. The server acting once per id
> is taken as part of the shape, since without it the resend sells twice; the
> author did not object to that part and can still strike it. Which other intents
> adopt an id is decided per message when there is a reason. Closes T8: the
> convention gains "an intent that wants confirmation carries an id and is
> approved or rejected by it".

**T9.** What does the compiler check about transitions, chests and vendors before
a package is written?

> Answer (2026-09-21): transitions need to match up correctly and not be missing.
> chests need an Id so their loot can be correctly rolled and supplied by the
> server, vendors need an id so they can also be controlled by the server and set
> their prices and items.
>
> Consequence noted: the id is the placement's `name`, the stable key the server's
> data is written against; the editor's numeric placement id changes when a scene
> is rebuilt and is not it. Chest and vendor join `interaction` as roles beside
> terminal and workbench, so the same property marks all four. Compiler checks, as
> new codes: a transition whose target scene is not in the project, error; whose
> target transition is not in that scene, error; two transitions with one name in
> one scene, error; a transition whose target does not point back, warning, since
> one-way is allowed; a transition with only one of its two target properties,
> error. A chest or vendor with no name, or a name used twice in the scene, error.
> What a chest rolls and what a vendor sells is the server's data and not the
> compiler's to check; a name with no server entry is found by a test in
> `Tests/Compiler/RealArtTests.cs`, which already compiles the real art.

**T10.** Which behaviours must a test prove before this is called built?

> Answer (2026-09-21): I would say for transitions and scenes the need to complete a
> full graph but thats too complex.
>
> Options offered. Fact: `Tests/Compiler/RealArtTests.cs` already compiles the real
> art under `art/` in a test. The graph is three scenes and four transitions, and a
> reachability walk from the starter zone over the compiled transitions is a dozen
> lines on top of that test. It is not too complex; it is the cheapest test in the
> list, and the one that catches a scene the author forgot to connect. The T-list,
> in the project's manner (the big cases, not the edges), with the test project each
> lands in:
>
> - **Compiler** (`Tests/Compiler`): each of the six checks from T9 fires on a scene
>   built in the test, by code, and a good scene passes them all.
> - **Real art** (`Tests/Compiler/RealArtTests.cs`): every scene compiles; every
>   transition resolves; every zone file is reachable from zone 1 by walking
>   transitions; every chest and vendor name in the art has an entry in the
>   server's data.
> - **Zones in the simulation** (`Tests/Core`): a player in one zone is not in the
>   world view of a player in another; movement clamps and collides against the
>   player's own zone only; a player who steps inside the lock distance of a
>   transition is in the target zone at the target's position after the tick, and
>   one outside it is not.
> - **Party** (`Tests/Core`): when one member crosses, every member standing in that
>   zone is in the target zone after the tick, not on one spot.
> - **Chest** (`Tests/Core`): a take moves the item to the taker; a second take is
>   rejected as empty; after ten minutes of ticks the chest holds one item again,
>   and before that it does not.
> - **Vendor** (`Tests/Core`): a buy in reach with the balance deducts and gives;
>   refused for no funds, out of reach, and an item the vendor does not sell; the
>   same intent id twice sells once and is approved twice.
> - **Persistence** (`Tests/Integration`): the saved zone is the one the player
>   stood in; a connect places them there; a saved zone missing from the package
>   places them at the starter spawn.
> - **Messages** (`Tests/Core`): the new messages round-trip through serialize and
>   deserialize at their declared size, and a rejection carries its intent id.
>
> Not tested by code: the fade and the rebuilt scene on the client, seen in play, as
> the project's testing convention allows for client-side behaviour.
>
> Decision (2026-09-21): we should do that walk then. I am more concerned about
> transitions that lead to a non existance scene but yes. thats good. No more test
> changes.
>
> Consequence noted: the T-list stands as written. The missing-scene case is both a
> compiler error [T9] and part of the real-art walk, so it is caught twice.

### Outcome

- `WorldSimulation` stays the one entry point and holds the shared parts: parties,
  inventories, terminal access, and the map of player to zone. A new `Zone` object
  per loaded zone holds its bounds, dressing, the players standing in it, its ground
  items, terminals, workbenches and chest. The simulation routes each call to the
  player's zone and ticks the zones in turn. [T1, T1a]
- A transition is a placement, on any layer, with a `name` and two properties
  naming the target scene and the target transition. Their presence is the flag.
  No generic kind word. The source scene is the file. Both words live in
  `ZoneConventions`; names open to change. [T2, T2a, T3, T3a]
- A transition is identified by its scene and its name, unique within the scene. A
  door is two-way when each end names the other. The arrival spot is the far
  transition's position. [T2, T2a]
- Two distances in code beside `TerminalReach`: an outer one where the client's
  effect and sound respond, an inner one where the server locks the player and
  crosses. The trigger's model must not block. [T3a, T4]
- One new server message per crossing carries the zone id and the arrival position.
  The client fades, drops the scene and world view cache, loads the zone from the
  package as at connect, rebuilds the world screen and HUD, fades in. Pulled party
  members get the same message. [T5]
- `players.zone_id` becomes live: saved as the player's own zone, read at connect. A
  saved zone missing from the package falls back to the starter spawn with a log
  line. New players start at the starter zone's `player` spawn. [T6]
- The chest is an `ItemInstance` owned by the zone with one slot. A take moves the
  item's owner to the player, checked for reach. The zone refills it ten minutes
  after it empties with one item from a table in code. Not persisted. [T7]
- Dollars are one whole-dollar integer on the player, a column beside position,
  carried in the inventory view. New players start at 10. [T8]
- A vendor is server data keyed by the shopkeeper placement's name: items and
  prices. The buy intent names the vendor and the item; the server checks reach,
  that the vendor sells it, and the balance, then deducts and gives. [T8, T9]
- Any client intent may carry an intent id. One that wants confirmation is answered
  by `ServerIntentApproved` with the id or `ServerMessageRejected` with the id and
  reason, and the server acts on each id once. The client holds it pending and
  resends the same id after about a second. The buy is the first such intent. [T8a]
- `interaction` gains `chest` and `vendor`. A chest or vendor is keyed by its
  placement `name`. [T9]
- Compiler checks, new codes: target scene missing, target transition missing,
  duplicate transition name in a scene, half a target, unnamed or duplicate chest or
  vendor, all errors; a target that does not point back, warning. [T9]
- Tests: the T-list under T10, as written. [T10]

Deferred:

- The bank account with history, balances and loans. One integer until then. [T8]
- The reliability layer as a whole (acks for every message, ordering,
  fragmentation). The intent id is its first piece. [T8a, backlog]
- Which other intents adopt an intent id. Per message, when there is a reason.
  [T8a]
- A zone kind (world or town) in the zone file, for the party rule. Can be written
  now, read by nothing. [F7]
- The property bundle in Voxel Scene Maker. A change in that repo; this repo needs
  only the flattened shape. [developer thoughts]
- Persisting the chest across server starts. [T7]
- The client's fade timing: on the server's message, or already at lock-in. Seen in
  play. [T5]

## Gatekeeping

Each "Already decided" item against the Outcome.

- First playable is one town, no outskirts, no travel: **changed on purpose** by
  [F1a]. The outskirts joins. Travel between towns stays out; a door is not travel.
- Rings out from a town, the first ring a fixed wilderness [T1 transcript]: fits.
  The outskirts is that ring's first piece.
- An interior is a separate, enlarged zone, no cutaways [C-2026-09-18]: fits. The
  shop is the first.
- Upper storeys are separate zones: fits, none built.
- No blockers, no hard lock-outs [Q23]: fits. Doors are open.
- The party changes zones together [B, Q23]: fits, every door in this build [F7].
- Travel in real time on a schedule: not touched.
- Every safe zone has free standard terminals [Q12]: **open**. The outskirts has no
  terminal in the design, and whether it is a "safe zone" in that sense was not
  asked. It is safe [F4]. Left to layout.
- The Training Grounds isolated, built last: not touched.
- The first town built by the author, Metro Minis a placeholder: fits. The shop
  interior waits on assets [F8a].
- `door.<name>` marker with a link, planned: **changed on purpose** by [T2, T3].
  The door is a named transition with target properties, not a `kind.name` marker.
  art-pipeline.md needs an update.
- The world clock and lighting: not touched. Every zone shares the clock.
- First playable: nothing bought, battery found: **changed on purpose** by [F6a].
- Deferred split of `WorldSimulation` [CLAUDE.local.md]: **changed on purpose** by
  [T1a], by zone, not by subsystem.

## Consequences

- `docs/first-playable.md`: remove "the outskirts" from "What is not in it"; add the
  outskirts and the electronics shop to "What to build"; change item 2 to "a battery
  to buy in the electronics shop for $8"; move "Dollars as a working currency,
  vendors" from not-in-it to in, with the recycler still out; add $10 to the
  starting kit in "The shape". [F1a, F6a]
- `docs/world.md`: fold in, in the author's words, tagged [C-2026-09-21]: the
  outskirts as the first ring's first piece; doors as walk-in transitions shown by
  the road and a threshold effect; a zone has a new-player spawn and an arrival
  spawn per door; the party crosses together, with the world-versus-town rule as
  the stated direction; an electronics shop in town that sells the battery; a new
  player has $10.
- `docs/backlog.md`: add the world-versus-town party rule, the bank account, which
  intents take an id, persisting the chest, and the property bundle for the editor.
- `docs/engineering/art-pipeline.md`: the planned `door.<name>` marker becomes the
  transition as decided; the marker table and the reader checks change; the
  interaction roles gain chest and vendor. After the code exists.
- `docs/engineering/`: a zones doc (loading, the `Zone` object, transitions,
  spawns) and additions to items-and-equipment.md for the chest, the balance and
  the vendor. After the code exists.
- `docs/status.md`: "Zones and travel between them" from one zone to three zones
  with doors; "Currency" and "Vendors and shops" gain the first-playable shape.
- `CLAUDE.local.md`: the deferred "splitting WorldSimulation" item gains a line that
  the split by zone was decided in this session and the split by subsystem stays
  deferred. The convention on refusals gains the intent id line.
- `Compiler/README.md`: the new codes.
- Voxel Scene Maker (other repo): the property bundle, when the author builds it.
  Until then the two transition properties are typed by hand, and `art/props.json`
  gains them so the editor offers them.

Build order, by dependency, as PRs:

1. Zones in the simulation: the `Zone` object, routing, per-zone views, the zone id
   live in persistence. No doors yet. Tests: zones, persistence.
2. Transitions: the scene words, compiler checks, the crossing, the party pull, the
   client's zone change and fade, the outskirts scene. Tests: compiler, real art,
   crossing, party.
3. The chest: zone-owned instance, take, refill. Tests: chest.
4. Dollars and the vendor, with the intent id, Approved and the pending state.
   Tests: vendor, messages. The shop scene lands when its assets do.
