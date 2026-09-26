# Making changes

Recipes for the changes this codebase keeps needing, with the steps that are easy to
miss. The code's comments explain the why of each part; this is the how.

## Rules that hold everywhere

- The server is the authority. A client sends what it wants (an RPC); the server checks
  it (who sent it, are they in reach, can they afford it) and changes the world; synced
  properties and RPCs tell the clients.
- Game rules are plain C# in `src/Rules`, with tests in `tests/Tests/Rules`. Nodes in
  `game/` call them; nothing in `src/Rules` knows Godot.
- Node paths must match on client and server: RPCs and sync find nodes by path. A
  player's body is named after its peer id; zones are named after their id.
- Raise `GameVersion.Protocol` (`src/Rules/GameVersion.cs`) whenever an RPC or a synced
  property is added or changes. The server then refuses older clients with a clear
  message instead of failing strangely.
- No C# on engine-called hot paths (per node per peer per frame): see diagnostics.md.
- Build before running: Godot runs the assembly in `.godot/mono`.

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

## A zone

1. `game/zones/<id>/<id>.tscn`, root named `<id>` with `Zone.cs`: set `Surface`
   (footsteps), `MapSize` (0 for no map), `ItemAreaSize` / `ItemStock` (ground items).
2. Children the code expects: `Interactables`, `Doors`, `Arrivals` (Marker3D per way
   in), `Spawn`, `Players` with a `PlayerSpawner`, `Items` with an `ItemSpawner` (copy
   them from `greenhouse.tscn` or `subway.tscn`). Walls on physics layer 5 (world and
   camera block).
3. `src/Rules/World/ZoneIds.cs`: a constant, in `All` (loaded at start on both sides),
   and a `DisplayName`.
4. A `Door` (`game/zones/Door.tscn`) in each zone it joins, with `TargetZone` and
   `TargetArrival` (the arrival marker's name there). Stand the door's trigger in
   front of walls, not in them: the college's once ended inside its wall.
5. Music and ambience: `music.<id>` and `amb.<id>` in the sound catalog.

## Something to use (an interactable)

1. A class deriving `Interactable` (`game/interact/`): `Prompt`, and in the scene
   `Reach` (measured from the node's origin) and `Voice` for a person.
2. Put it under the zone's `Interactables`.
3. Handle it in `ServerInteractions.Use` (a `case`); reach is checked there.
4. If it has state everyone sees, give it a `MultiplayerSynchronizer` child named
   `Synchronizer`: `ServerGame` watches every interactable's synchronizer through the
   visibility gate, so only players in that zone are sent it.

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

## Props, models and looks

- A KayKit model as a prop: add it to `PROPS` in `tools/kaykit-props/make_props.py` and
  run it; zones place the prop scene, never the raw model. Collision kinds are in the
  tool's README.
- To look at a model without the editor: `tools/model-shots`.
- To look at the game without clicking: `--screenshot` with `--overview`, `--garden`,
  `--creator` or `--show-characters` (running.md). Sizes, colours and placements are
  placeholders until the author has seen them.

## A sound

Add an id to `game/audio/sounds.json`, run `tools/audio-subset/copy.py`, import once,
and play it with `AudioDirector` (docs/engineering/sound.md). New packs go in
`assets/README.md` with their licence, and CC BY packs in the credits
(`game/ui/CreditsPanel.cs`).

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
