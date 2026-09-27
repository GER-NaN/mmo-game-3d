# Making changes

Recipes for the changes this game keeps needing, with the steps that are easy to miss.
Each step says where it happens: **editor** (Godot, see editor.md), **code** (a C# file),
**catalog** (a JSON or Python list), or **run** (a script). The code's comments explain
the why of each part; this is the how.

For a whole new mechanic from end to end, follow the worked example:
example-tree-chopping.md.

## Rules that hold everywhere

- The server is the authority. A client sends what it wants (an RPC, or "use this"); the
  server checks it (who sent it, are they in reach, can they afford it) and changes the
  world; synced properties and RPCs tell the clients.
- Game rules are plain C# in `src/Rules`, with tests in `tests/Tests/Rules`. Nodes in
  `game/` call them; nothing in `src/Rules` knows Godot.
- Node paths must match on client and server: RPCs and sync find nodes by path. A
  player's body is named after its peer id; a zone sits under a holder named after its
  id, as `World/<id>/Zone` (see `World.cs`). On the server each zone has its own physics
  space, so zones never touch, whatever their size.
- Raise `GameVersion.Protocol` (`src/Rules/GameVersion.cs`) whenever an RPC, a synced
  property, an item type or a skill is added or changes. The server then refuses older
  clients with a clear message instead of failing strangely.
- Ids that are saved or sent (skill numbers, item names, zone ids, achievement ids) are
  added at the end and never renamed or reused.
- No C# on engine-called hot paths (per node per peer per frame): see diagnostics.md.
- Build before running: Godot runs the assembly in `.godot/mono`. Restart the server
  after changing a zone scene: it reads them at start.

## A zone

1. **Editor:** duplicate a small zone's folder as a start (`game/zones/greenhouse/`), or
   make `game/zones/<id>/<id>.tscn` with a root that has `Zone.cs`. The root's name
   does not matter; it is renamed `Zone` at load.
2. **Editor:** select the root and set, in the Inspector, `Surface` (footsteps: rock,
   grass, tile, dirt, or empty for silent), `MapSize` (0 for no map), `ItemAreaSize` and
   `ItemStock` (ground items; `ItemStock` 0 for none).
3. **Editor:** keep the children the code expects: `Interactables`, `Doors`, `Arrivals`
   (a Marker3D per way in), `Spawn`, `Players` with a `PlayerSpawner`, `Items` with an
   `ItemSpawner`. Copy them from `greenhouse.tscn` if you started empty.
4. **Editor:** build the place: ground, walls and props (below, "A model" and "Placing
   things"). Walls and ground block on collision layer 1 (World); tick layer 3 as well
   when they should also stop the camera.
5. **Code:** `src/Rules/World/ZoneIds.cs`: a constant, add it to `All` (the zones the
   server loads at start), and a `DisplayName` case.
6. **Editor:** a door each way (below, "A door between zones").
7. **Catalog:** music and ambience are `music.<id>` and `amb.<id>` in
   `game/audio/sounds.json`, played on entry by `ClientGame.UpdateSoundscape`. A zone
   without them is silent; add entries or a `case` there to reuse another zone's.
8. **Run:** restart the server, walk in. For a lasting test, copy the `college` dev
   scenario, which stands a player in front of a door and walks through.

## A door between zones

1. **Editor:** in the zone you leave from, add an instance of `game/zones/Door.tscn`
   under `Doors`, where the way out is. Stand it in front of walls, not in them.
2. **Editor:** set `TargetZone` (the other zone's id) and `TargetArrival` (a marker name).
3. **Editor:** in the other zone, a `Marker3D` of that name under `Arrivals`, a few
   metres inside, rotated to face the way players should face on arrival.
4. The same in the other direction for a way back. A door whose target is missing does
   nothing and prints an error on the server when walked into.

## A model (art) as a prop

A zone places **prop scenes**, never raw models: a prop carries the model at the game's
scale and its collision, decided once.

**A KayKit model** (packs in `assets/kaykit/`):

1. **Catalog:** add a line to `PROPS` in `tools/kaykit-props/make_props.py`: the pack,
   the model name, the scale constant, and a collision kind (`box`, `block` for
   buildings, `trunk` for trees and posts, `flat` for roads, `car`, `none`).
2. **Run:** `python tools/kaykit-props/make_props.py`. It writes
   `game/props/<pack>/<Name>.tscn`.

**Any other model** (a building from another pack, your own):

1. Copy it into `assets/<pack>/` with its licence file. Add a row to
   `assets/README.md` (where it came from, the licence). A CC BY pack is also credited in
   `game/ui/CreditsPanel.cs`. CC BY-ND or unclear licences: ask before using.
2. **Editor:** it imports on focus. Make a prop scene the same shape as the generated
   ones (open `game/props/city_builder_bits/BuildingA.tscn` to compare):
   - root **StaticBody3D** named after the prop; collision layer 1, plus 3 for a
     building (stops the camera);
   - the model dragged in as a child named `Model`, scaled to size (a door is about
     2 m high, a player about 1.8 m);
   - a **CollisionShape3D** named `Collision`, a `BoxShape3D` around the part that should
     block.
3. **Editor:** save it as `game/props/<pack>/<Name>.tscn`.

To look at a model without the editor: `tools/model-shots`.

## Placing things in a zone

1. **Editor:** open the zone, drag the prop scene from `game/props/` into the viewport.
   Put it under the zone's root (or a grouping node such as `Room`), not under
   `Interactables`, unless it is something to use.
2. Move it with the gizmo; hold Ctrl to snap. Keep it inside the zone's edges.
3. Save, restart the server. Players spawned at runtime are not in the scene; run the
   game to see it in use, or `--screenshot --overview` (running.md).

## A building you can walk into

An interior is its own zone, not a cutaway (world.md).

1. Place the building as a prop in the outside zone.
2. Make the interior as a new zone ("A zone"), enlarged inside as much as it needs.
3. A door in front of the building's entrance to the interior, and one inside back out
   ("A door between zones"). The shop, the college and the greenhouse work this way.

## An item

1. **Code:** `src/Rules/Items/ItemType.cs`: a new name at the end of the enum.
2. **Code:** `src/Rules/Items/ItemCatalog.cs`: its definition (name, description,
   stackable). The game throws on an item without one.
3. **Code:** `src/Rules/Items/Recycling.cs`: its value, if the recycler should buy it.
4. **Code:** where it comes from, one or more of:
   - lying on the ground: an entry in `LootTable.Ground()` (weight, quantity range);
   - in chests: `LootTable.Chest()`;
   - in a shop: a `ShopOffer` in `src/Rules/Shops/Shops.cs` (price in dollars);
   - as a reward: `session.Inventory!.Add(type, tier, quantity)` in a server part, then
     send the bag (`SendInventory`);
   - dropped at a spot: `GroundItems.DropAt(zone, spot, type, tier, quantity)`.
5. On the ground it shows as a placeholder box by type and tier colour
   (`GroundItem.BuildMesh`); a model for it is a change there.
6. **Code:** raise the protocol. A unit test if its rules have logic (loot tables and
   recycling have tests to copy).

## A skill

1. **Code:** `src/Rules/Skills/Skills.cs`: `SkillId` (the next number, at the end), add it
   to `SkillCatalog.All`, a `Name` and a `HowEarned` case, and its XP in `SkillAwards`.
2. **Code:** award it where the act happens:
   `_progress.Award(session, SkillId.X, SkillAwards.XPerAct)`. Level-ups, saving and the
   skills panel follow from the catalog.
3. **Code:** a career that should need it: its gate in `src/Rules/Skills/Careers.cs`.
4. **Code:** raise the protocol.

## A mechanic (something new to do)

The pattern, in full in example-tree-chopping.md:

1. The rules in `src/Rules` (skills, items, numbers), with tests where there is logic.
2. The thing in the world: a class deriving `Interactable` with its `Prompt` and any
   synced state, and its scene with a `Synchronizer` (editor).
3. A server part, `game/server/Server<Thing>.cs`: what using it does.
4. The hook-up: a `case` in `ServerInteractions.Use`, the part built in
   `ServerGame.Start`, ticked in `ServerGame`'s tick if it has timers.
5. Placed in zones under `Interactables` (editor), each with a unique name.
6. Sounds in the catalog, played by the client from the synced state.
7. A dev scenario that proves it.

## Something to use (an interactable)

1. **Code:** a class deriving `Interactable` (`game/interact/`): `Prompt` (empty for
   nothing to do right now). In the scene, `Reach` (measured flat from the node's
   origin) and `Voice` for a person.
2. **Editor:** put it under the zone's `Interactables`, with a unique name: a client asks
   to use a thing by its name.
3. **Code:** handle it in `ServerInteractions.Use` (a `case`); reach is checked there.
4. If it has state everyone sees, give it a `MultiplayerSynchronizer` child named
   `Synchronizer`: `ServerGame` watches every interactable's synchronizer through the
   visibility gate, so only players in that zone are sent it.

## A person

- **Walking round town:** **editor:** a `Path3D` loop under the zone's `Routes`, then an
  instance of `game/town/Townsperson.tscn` under `Interactables` with `RouteName` (the
  path's name), `StartAt`, `PersonName`, `ModelPath` (a character `.glb` in
  `assets/kaykit/characters/`) and `Voice`. The server walks them; their small talk is
  `src/Rules/Town/Chatter.cs`.
- **Behind a counter, selling:** an instance of `game/vendors/Vendor.tscn` with `ShopId`
  and `KeeperName`; the shop's offers are in `src/Rules/Shops/Shops.cs` under that id.
- **A voice:** `voice.<name>.greeting` in the sound catalog; set the person's `Voice` to
  `<name>`.

## A sound

1. **Catalog:** an id in `game/audio/sounds.json`: the file(s) in the sound library
   (`C:\game-sound`; several are picked at random), the bus (Effects, Interface,
   Ambience, Music, Voice), the level in dB, `loop` for a loop, and `range` in metres for
   a sound in the world.
2. **Run:** `python tools/audio-subset/copy.py`, then open the editor once to import it.
   A new pack also gets a row in `assets/README.md`, and CC BY packs a credit.
3. **Code:** play it on the client (the server plays nothing):
   - once, when a synced state changes: `Audio.AudioDirector.Current?.PlayAt(id,
     GlobalPosition)` in the node's `_Process` (the chest when opened);
   - a loop while something is so: `Attach(id, this)`, freed when it stops (a broken
     fixable's crackle, a drone's hum);
   - a flat interface sound: `Play(id)`;
   - by convention, without code: zone music and ambience (`music.<id>`, `amb.<id>`),
     footsteps (`step.<surface>` from the zone's `Surface`), a person's greeting
     (`voice.<name>.greeting`).

More in sound.md.

## A gesture or animation

`src/Rules/Social/Gestures.cs`: an id, the animation's name in the KayKit rig files
(`CharacterModel` loads General, MovementBasic, Simulation and Tools), how many seconds
it plays (0 holds until the player moves), and whether it is a chat emote (`/wave`). The
server shows it with `session.Body?.Show(id)`; it is synced to everyone in the zone.

## An RPC

1. In the right `NetworkNode` subclass in `game/networking/` (Network, TerminalNetwork,
   ItemNetwork...): an event, a `Send...` method, and the `[Rpc]` method.
2. Client to server: `[Rpc(MultiplayerApi.RpcMode.AnyPeer, ...)]`; inside, act only
   `if (Multiplayer.IsServer())`, take `Multiplayer.GetRemoteSenderId()`, wrap in
   `using (Activity? span = Received(MethodName.X, sender, ...))` and raise the event.
3. Server to client: `[Rpc(MultiplayerApi.RpcMode.Authority, ...)]`, sent with `SendTo`
   (one peer) or `SendToMany` (the same to many).
4. Server side, in `ServerGame.Start`: `networks.X.YRequested += (peer, ...) =>
   WithSession(peer, session => ...)`. `WithSession` drops requests from peers not in
   the world.
5. Client side, in `ClientGame.Start`: subscribe to the client event.
6. Raise the protocol.

A new network node: a script in `game/networking/`, a node in `game/Main.tscn` (same
name on both sides), a property in `Networks`, and a line in `Networks.SetLog`.

## Something that spends or gives (an intent)

A click that costs money or items carries an intent id, so a resend never spends twice:
the client calls `ClientIntents.Start(label, id => network.SendX(id, ...))`; the server
handler calls `intents.Run(session, id, () => { ...; return ""; })`, returning "" when
approved or the refusal to show. See the shop, the recycler, give, drop and the
greenhouse.

## A database change

1. A new file in `src/Data/Migrations`, numbered after the last (`0016_...sql`). Never
   edit one that has run: the server records applied names in `schema_migrations`.
2. A store in `src/Data/<area>/` using Dapper, and a test in `tests/Tests/Data` (give
   the test its own ids or names: the test database is not emptied).
3. On the server, database calls go through `PersistenceWorker.Enqueue(work, done,
   failed)`: `work` runs on the worker thread, `done` back on the game thread. Never
   touch Godot or sessions inside `work`; copy what it needs first.
4. Loaded at login? Add it in `ServerGame.OnLoginRequested`'s work, onto the
   `PlayerRecord`, and copy it to the `Session` in `OnPlayerLoaded`.

## Synced state

A property with `[Export]`, a `MultiplayerSynchronizer` child with a replication config
(spawn for the first value, "on change" or interval), and `VisibilityGate.Watch(sync,
zoneId)` on the server for anything not under `Interactables` (TownState, drones,
display plants do this). Spawned nodes: call `Watch` before `AddChild`. Raise the
protocol.

## A terminal app

`src/Rules/Terminals/TerminalApps.cs` (the list; the phone's subset in `PhoneApps`;
a locked notice makes it show but not open), a `case` in `TerminalScreen.ShowApp`, and
groups on its buttons so scenarios can find them.

## Status board, achievements, phone pushes

- A world event: `ServerTerminals.Post(text)` (set as `Post` on the server parts).
- An achievement: add it to `src/Rules/Achievements/Achievements.cs`, then call the
  part's `Achieved(session, id)`; granted once, saved, shown in the skills panel.
- A phone alert: `ServerEquipment.PushToPhones(text)`.

## Proving a change

- A rule or a store: a unit test.
- A feature in the running game: a dev scenario (testing.md). Set the player up exactly;
  never let a bot wander to it. Each ends within a minute.
- Headless clients: `FindChildren` matches engine classes, not C# classes, so find our
  nodes by group; hold a key a moment (`Input.ActionPress`, release next frame), since
  the game polls it.

## Files Godot makes

Godot writes a `.uid` file beside each new script the first time it runs or imports:
commit them. `.godot/` is not in git.

## Git

- Work on a branch; one commit per distinct change, with the why in the message.
- Changes reach `main` only through a pull request (`gh pr create`); never merge to
  `main` locally. The remote is named `mmo-game-3d`, not `origin`.
- Line endings are LF (`.gitattributes`); do not normalize files by hand.
