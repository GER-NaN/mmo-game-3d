# Zones

How the code holds more than one zone, the doors between them, the chest and the
shop, as built 2026-09-22 from `docs/features/second-zone.md`. That file is the design
and the record of how it was reached; this one is the code.

## One simulation, a Zone per zone

`WorldSimulation` is still the one entry point every handler calls, and it holds what
is shared between zones: parties, inventories, who is online, and the map of player to
zone. A `Zone` (`src/Core/Simulation/State/Zone.cs`) holds what only that zone has: its
box and dressing, the players standing in it, its ground items, terminals, benches,
chests and vendors. Every intent is routed to the player's zone by
`TryGetPlayer(playerId, out player, out zone)`, and a world view is built from one
zone only, so nobody in another zone is ever in it. `PlayersInZone(zoneId)` is the
zone's own player set, for chat and events later.

A zone is a scene: every compiled scene in the package is a zone named after it
(`ZoneLoader.LoadAll`), and `ZoneId` is that name, "starter-zone", everywhere: the saved
player, the network, a door's `transition.scene`. Names compare without regard to case.
The zone new players start in is named in `game-data/world.json` (`GameWorld`), which the
compiler checks and packages; there were numbered zone files until 2026-09-25
(`docs/features/zones-are-scenes.md`).

Terminal ids carry the zone (`TerminalIdFor(zoneId, placementId)`, from a stable hash of
the name), so two scenes that both number their placements from 1 never collide. Ground items are stocked per
zone at the starter zone's density, so a small zone is not carpeted.

`players.zone_id` is live: `PlayerSaver` writes the zone the player stands in and
`ConnectHandler` adds them to it. A saved zone that is no longer in the package puts
them at the starter spawn with a log line rather than refusing the connect.

## Doors

A door is a placement with a `name` and two properties, `transition.scene` and
`transition.name` (`ZoneConventions.TransitionSceneProperty`, `TransitionNameProperty`).
Their presence is the flag; there is no kind word. It is identified by its scene and
its name, unique within the scene, and the far door is the one it names. The arrival
spot is the far door's own position, so no separate arrival marker exists. A door is
two-way when each end names the other, one-way when only one does.

`ZoneDressing.FromScene` reads a `Transition` from every placement with both words,
drawn or not, and a drawn door never blocks. The starter scene marks its door with a
sidewalk slab carrying `effect=threshold`; the outskirts does the same.

The crossing (`WorldSimulation.FindCrossings`, `Cross`, `MoveToZone`):

- A player who steps within `TransitionLockDistance` (0.75 m) of a door's centre is
  moved to the far door. It fires on stepping in, not on standing in
  (`Player.InsideTransition`), so a player who has just arrived, or who was saved on a
  door, is not taken straight back. They walk off it and back on.
- Every party member standing in the same zone goes with them, spread
  `ArrivalSpread` around the far door.
- A body at a terminal gets up first.
- The player is held on the far door for `TransitionLockSeconds` (0.8 s): walking
  intent is dropped, so a held key does not walk them off before the fade.
- For `TransitionCooldownSeconds` (3 s) after arriving, no door takes them, so a body
  walking back and forth over a door spot does not bounce between zones. The client
  keeps the thresholds at their resting rate for as long (`ZoneScene.DimDoors`).
- Each one moved is a `ZoneChange`; `ServerApp.SendZoneChanges` sends
  `ServerZoneChanged` (zone name, position) to each before the world views go out.

The client: `ZoneChangedHandler` sets the connection's zone id and bumps
`ZoneVersion`. `ScreenManager.UpdateZoneFade` sees the version change, fades to black
over 0.35 s, drops the world view cache and the selection, rebuilds the world through
`ShowWorld` (which loads the zone's compiled scene by name as at connect), and fades in. The
threshold effect (`ParticleEffect.Threshold`) brightens as the player nears, from its
resting rate at `TransitionWarnDistance` (4 m) to four times that on the door.

The compiler checks doors once every scene is read (`SceneCompiler.CheckTransitions`):
half a door (SC1006), a target scene the project lacks (SC1007), a target name the
scene lacks (SC1008) and two doors with one name (SC1009) are errors; a door that does
not lead back (SC2005) is a warning. `tests/Compiler/RealArtTests.cs` walks every zone
in the committed package from zone 1 over its doors.

## Spawns

Only the start zone must have a new-player spawn, the `player` placement on the
`spawns` layer; any other zone may have one, and is otherwise entered through its doors.
Every door is an arrival spawn: the door itself. A player whose saved spot can no longer
be stood on wakes at the zone's spawn, or without one at its first door, or without a
door at the start zone's spawn. The party rule for later (world zones move the party,
town zones do not) needs a zone kind; with no zone files, it would go in `world.json`,
per zone, and nothing reads one yet.

## The chest

A placement with `interaction=chest` and a name is a `Chest`
(`src/Core/Simulation/State/Chest.cs`): an `ItemInstance` of type `Chest` the zone owns
(`ZoneOwnerId`), with an inventory of its own in the same `InventoryStore` every bag is
in, keyed by `ChestIdFor(zoneId, placementId)`. It starts full with one roll of the
loot table. `PlayerTookFromChest` checks the chest is in the player's zone and in
reach and holds something, then moves the stack to the player. Empty, it counts
`SecondsEmpty` and refills after `ChestRefillSeconds` (600) with one roll. Not
persisted: a server start finds it full.

What it holds travels in the world view as a `ChestView` (id, position, has-item,
type, tier, quantity) to everyone the interest policy admits, so it empties on screen
for everyone. The client's `ChestPanel` opens on the click and takes by the Take
button, a double-click on the slot, or a drag of the slot onto the open inventory,
all `ClientChestTake`.

## Dollars and the vendor

Dollars are a whole-dollar balance on the inventory (`Inventory.Dollars`), not an
item, persisted as `players.dollars` (migration 20260922000000) and carried in
`InventoryView.Dollars` so the bag and the balance arrive in one snapshot. A new
player has `WorldSimulation.StartingDollars` (10). The inventory panel shows it as
"Pocket Change".

A placement with `interaction=vendor` and a name is a shopkeeper. What they sell is
`VendorCatalog`, the server's data keyed by the vendor's name; the client draws the
list from the same table and the server checks its own copy at the sale.
`PlayerBought` checks the vendor is in reach, sells the thing, and the balance covers
it, then `TrySpend` and `AddItem`. The electronics shop sells the battery for $8 and
two things above a new player's means, which show and refuse.

## Intents with an id

A purchase spends something, so it is the first intent that carries an id
(`ClientBuyItem.IntentId`). The server acts on each id once
(`Player.RecentIntents`) and answers every one with `ServerIntentApproved` or
`ServerMessageRejected` carrying the id. The client (`ServerConnection`) holds the
intent pending, reads "Buying..." in the vendor panel, and sends the same id again
every second (`ResendPendingIntentsIfDue`) until an answer arrives. A refused intent
is not remembered as done, so it can be tried again. Equip and the rest keep sending
without an id until they want one.

## Compiler

New words the compiler knows: `transition.scene`, `transition.name`, `interaction`
values `chest` and `vendor`. A chest or vendor with no name (SC1010) or a shared name
(SC1011) is an error, because the server keys what it holds by name.
The props in `art/mmo-game.project.json` list the words so the editor offers them.

## Placeholders

- `art/scenes/outskirts.scene.json` is laid out by hand: a small wood, a fountain, a
  path to the town's door, a metal newsbox standing in for the chest.
- The town's door is a path north out of town with a sidewalk slab as the threshold.
- The shopkeeper stands on the street outside the store, because the shop interior
  waits on assets. The interior scene is the next piece of art.

## Tests

`tests/Core/Simulation/ZoneTest.cs`, `TransitionTest.cs`, `ChestTest.cs`,
`VendorTest.cs`; `tests/Core/NetCode/MessageSizeTests.cs`;
`tests/Compiler/SceneCompilerTests.cs`, `RealArtTests.cs`;
`tests/Integration/ZoneTests.cs`, `VendorTests.cs`. The fade, the threshold's glow and
the panels are seen in play.
