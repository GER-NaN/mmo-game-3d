# Items and equipment

How the code holds items, equipment, the phone as a door, and the workbench, as built
2026-09-21 from `docs/features/usable-phone.md`. That file is the design and the
record of how it was reached; this one is the code.

## Two kinds of entry

An inventory (`src/Core/Simulation/State/Inventory.cs`) holds stacks and instances side
by side. A stack is a quantity of identical things (500 tier-1 GPU cores). An instance
is one thing with an identity and state of its own: this phone, this battery at 43
percent. The type definition says which an item starts as (`ItemDefinition.Stackable`).
A battery is a stack until one of them gains state, which happens when it goes into a
phone at a workbench; after that it is an instance for good.

## Instances are a tree

`ItemInstance` has one owner (`OwnerId`: a player, or the instance it sits inside) and
may fill one slot of that owner (`Slot`). Its own slots hold other instances. So the
player owns the phone, the phone owns the battery, and dropping the phone would move
the whole subtree. Ownership is exclusive: an equipped thing is owned by one thing.

The player's equipment slots and an item's component slots are the same mechanism at
two levels. `SlotType` serves both: a player has Device, Drone and Weapon (all of them,
from the start, empty until filled: `ItemCatalog.PlayerSlots`); a phone has Battery.
`ItemCatalog.CanEquip(item, slot, owner)` is the whole rule, and both the client's
drag guard and the server's intent check call it.

## Behaviours are components

`IItemBehavior` follows the Component pattern (Nystrom): an instance is a plain
container and its behaviour is a list of small objects, ticked in turn, each owning
one concern and mutating only its own instance. The definition lists them as
`BehaviorKind` (data); the catalog builds the classes (code). Three exist:

- `PowerCellBehavior` on a battery: holds the charge, the one persisted state. It
  drains by whatever draw its owner set this tick and clears the draw after, so a
  battery loose in a bag keeps its charge.
- `BatteryPoweredBehavior` on a phone: reads the tick context ("in use") and sets the
  draw on the battery in its slot. The rates are the design's placeholders: about
  three days idle, about three hours in use.
- `TerminalDoorBehavior` on a phone: says which `TerminalType` the door opens and
  whether it can open, which is a question of power.

Context is pull, every tick: `WorldSimulation.TickEquipment` ticks each player's
equipped instances with an `ItemTickContext`, the parent turns it into a draw for its
battery, the battery knows only the draw. Results flow up by return: the tick reads
the door's `CanOpen` after and, if the player was in through it, calls leave. A
behaviour never touches the simulation.

Only equipped instances tick. The owner is marked dirty when a whole percent of charge
changes, not every tick, or the snapshot would go out thirty times a second.

## Definitions reach the client one per message

`ServerItemDefinition` carries one definition (name, description, stackable, slots,
behaviour kinds, door type), sent for every type after connect and again on
`ClientRequestItemDefinitions`. One per datagram rather than the whole catalogue in
one, so a lost datagram loses one definition and the request repairs it. The client
keeps them in `ItemDefinitionCache`. The multi-packet system waits for the reliability
layer (backlog).

## One snapshot for what you hold

`ServerInventoryUpdate` carries the stacks as before plus a flat list of every instance
in the owner's tree (`ItemInstanceView`: id, type, tier, owner, slot, charge). The
tree is rebuilt from owner ids; nothing nests on the wire. Equipment is not a second
message: an equip moves one instance's slot, and one snapshot shows the bag and the
slots agreeing. A hundred stacks and ten instances sit under the datagram cap.

## Refusals say why

Convention from this build: success is the next snapshot, failure is
`ServerMessageRejected` with a `RejectionReason`. Equip, unequip, terminal use and the
workbench intents all answer that way. Movement stays silent: it repeats every tick.
The client's `RejectedMessageHandler` ends the session only on `Unauthenticated`; every
other reason goes to `RejectionLog`, and the HUD shows it in words for three seconds.

Added 2026-09-22: an intent that wants confirming carries an `IntentId`, and the
server answers it with `ServerIntentApproved` or a `ServerMessageRejected` carrying
the same id, acting on each id once. The purchase is the first such intent; see
`zones.md`.

## Persistence

`item_instances` (migration 20260921120000): id, holder_id (the player at the root),
owner_id (the parent), item_type, tier, slot (null for loose), charge (null for an
item with none). A whole tree loads and saves by holder id, by replace, on the same
path as the stacks: the 30-second sweep, disconnect, shutdown.
`src/Server/Persistence/ItemInstanceRecords.cs` flattens the view out and rebuilds the
tree through the catalog on the way in.

The starter phone is made once ever: `IPlayerStore.GetOrCreate` reports `created`, and
the worker writes the phone with a battery at ten percent before it answers the
connect, so a crash before the first save cannot leave a player with no phone.

`players.device_type` is gone (migration 20260921130000). The carried device was a
type on the player; it is an item now.

## Terminal access, rebuilt

`TerminalAccess` keeps only who is online and through which door: a fixed terminal id,
or the instance id of a held door (the phone). One player per fixed terminal, one door
per player, and being online does not survive a disconnect. `PlayerUsedDevice` finds
the door in the device slot and refuses with `NoDevice` or `DeviceDead`. The private
view is `TerminalWorldView` (online, door type, through a held door, and a place for
the app list). `PlayerView.AtHeldTerminal` tells other clients to play the pose.

## Placed terminals and workbenches

A placement with `interaction=terminal` or `interaction=workbench` in the scene (and
`terminal=<TerminalType>` for a door that is not a gaming rig) becomes an
`Interactable` in the dressing with its footprint, on both sides from the same
compiled zone. The simulation places terminals from it, with ids made from the
placement id (`WorldSimulation.TerminalIdFor`), and reach is measured to the
footprint's edge. The random scatter is gone. The starter scene tags two store fronts
as terminals and one as a workbench; the tags were written into both the scene source
and the compiled zone, because this machine cannot recompile without the model folders.

The workbench intents, `PlayerRemovedComponent` and `PlayerAppliedComponent`, check
the bench exists, the player is at its edge, and the item is theirs. Apply takes a
loose instance by id or one off a stack by type and tier. The client's
`WorkbenchPanel` is three columns of labelled buttons (item, slot, then remove or
apply), so the driver can work it.

## The client

- `InventoryPanel`: the player slots in a row over the bag; drag an instance onto a
  slot to equip, off it to unequip; hover shows the definition's description.
- `TerminalPanel`: the Go Online buttons and the battery bar over the phone's.
- `TerminalScreen`: one screen, an appearance per door. The phone is a frame in the
  middle with the world fogged behind it (`TerminalFrame.DrawPhone`); every other door
  covers the window.
- Animation: `AnimationClip` is a clip as data; see `animation.md`.
- Driver: `equip <slot>` drags through the panel; the snapshot reports the terminal
  world, the equipped device and its charge. `tools/client-scripts/phone-online.txt`
  walks the phone end to end.

## Chests, Dollars and vendors

Built 2026-09-22 and described in `zones.md`: a chest is an instance the zone owns
with an inventory in the same store; Dollars are `Inventory.Dollars`, carried in the
inventory view and persisted as `players.dollars`; a vendor sells from `VendorCatalog`
by the placement's name.

## Tests

`tests/Core/Simulation/Items/ItemInstanceTest.cs`, `EquipmentTest.cs`,
`TerminalTest.cs`, `WorkbenchTest.cs`, `PlayerRestoreTest.cs`;
`tests/Core/NetCode/ItemMessageTests.cs`; `tests/Core/Zones/ZoneDressingTest.cs`;
`tests/Integration/EquipmentTests.cs`, `ItemDefinitionTests.cs`, `TerminalTests.cs`;
`tests/Voxels/AnimationClipTest.cs`. The pose, the frame and the glow are seen in the
client, not tested.
