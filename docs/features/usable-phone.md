# A usable phone in the game

**Date:** 2026-09-21
**Status:** Built
**Moved:** from the mmo-game repo on 2026-09-26. Built in the old MonoGame engine; the
design questions and answers still hold, the code notes do not.
**Sources read:** docs/world.md sections 4 (the terminal), 7 (the rig and equipment),
9 (economy: first purchases), 11 (rooms), 16 (Training Grounds: the phone as reward);
docs/first-playable.md items 1 and 4; docs/backlog.md (phone-to-laptop path, real-phone
push notifications).

Adding a usable phone to the game. This is the first equippable item, so the framework
for equippable instance items comes first, and the phone is its first case.

The answers are the author's own words, typed in conversation and recorded verbatim or
near it, one question at a time. Model additions are set apart and labelled:
"Consequence noted", "Options offered", "Note".

## Already decided

Decisions in force that this feature must fit, or change on purpose.

- Phone, laptop, gaming rig, supercomputer and data centre are different access
  points, not ranks. [C-2026-09-18, Q10]
- A phone has only simple tasks and features: a few objectives, GPS, chat. [Q10]
- The device is one equippable item among dozens of equipment lines that each level
  up. It is not a significant ladder. [Q12]
- Some items slot in to upgrade an item. A better power supply is a longer battery on
  a phone or laptop. [Q14]
- A new player's phone is at ten percent when they arrive, so the first purchase is a
  battery. A dead phone cannot go online. [Q16, Q17, first-playable item 1]
- Nobody starts with a phone. The phone is the reward for leaving the Training
  Grounds. [Q12, Q23, C-2026-09-18]
- The AI can damage the rig electronically and hack it by proximity. Hacking disables
  features in degrees: scramble, damage needing repair, brick. [Q1, Q2]
- Push notifications reach the in-game phone. [Q21]
- Chat is reachable from any device. [B, Q26]
- On fainting, whatever hurt you can damage inventory or equipment, or steal. [Q2]
- Nearly everything is sellable at a recycler. [Q14]
- Crafting: build your phone, laptop and gaming rig from parts. Shape undecided.
  [Q12, Q14]

Built:

- Inventory is stacks keyed by item type and tier with a quantity, private to the
  owner, sent as a full snapshot in `ServerInventoryUpdate`. No instances yet.
- The carried device is `players.device_type`, a nullable `TerminalType` on the
  player, not an item. `TerminalAccess.GrantDevice` exists and nothing calls it.
- Terminal verbs are use and leave (`PlayerUsedTerminal`, `PlayerUsedDevice`,
  `PlayerLeftTerminal`). The state is "at a terminal". Front-end labels are
  "Go Online: <type>" and "Go Offline". One terminal per player. Being at a terminal
  does not survive a disconnect.
- The terminal screen is a plain panel. There is nothing to do inside it yet.
- World items on the ground are picked up by walking over them, in `Tick`.

Deferred:

- The path from phone to laptop: buy, build or earn. [backlog, Q12]
- Push notifications to a real phone. [backlog, Q21]
- Inventory deltas. Full snapshots until a reliability layer exists. [session notes,
  2026-09-17]
- Splitting `WorldSimulation` into subsystems. [CLAUDE.local.md]

## Feature design

### Developer thoughts

> (2026-09-21) I think this is also not an isolated concept. A phone is an equipable
> item, its something a character has and uses. So I think this introduces equipment
> slots for players. IE, they have N equipment slots that are their usable items. Each
> equipable thing has some method to use it (button on the HUD for now?). So what we
> are building is the framework for Equipment. My list of things needed for this.
>
> - The items themselves, they need to live somewhere and have definitions (Phone,
>   battle drone, spy drone, GPS device, EMP Device...)
> - We need a way to say that player owns that item and it belongs to them.
> - We need a way to define item behaviors and functionality (Phones have battery
>   drain and they are an access point for the terminal world), drones have unique
>   behaviors in the world and have damage and inventory slots for GPU upgrades, GPS
>   devices have battery, a map interface and signal strength.
> - We need to figure out how to represent these in the game UI (a phone is held in
>   the players hand when in use), a drone is visible in the physical world. These are
>   models but are different than a scene, so far our scene has held all drawable
>   content in the physical world. I dont think a phone fits into the scene or drones,
>   they are unique instance items.
> - Then we need a way to give the player a method to interact and use them, this is
>   the inventory slot idea, equipable slots. So if your phone is in one of the
>   equipable spots you can use it.
>
> Consequence noted: this makes two kinds of item. Stacks (500 tier-1 GPUs, built
> 2026-09-17) and instances (this phone, with its own battery and upgrades). The
> current inventory holds only stacks. The current carried device is a nullable
> `TerminalType` on the player, not an item at all, so the phone as an item replaces
> that.

### Q&A

**F1.** Walk through one use of the phone from the player's seat, from pressing the
HUD button to putting the phone away. What does the player see and do at each step?

> Answer (2026-09-21): The phone opens the terminal world but with a different layout
> (a simpler one when compared with using a laptop or desktop/gaming rig). The exact
> layout and functionality are TBD because we don't have a terminal layout. For now
> that's the only point of it, a portable and convenient way to open the terminal
> world.
>
> Consequence noted: for this build, using the phone is "Go Online" through the
> existing device path. What the phone shows inside the terminal world waits on the
> terminal layout, which is its own design. The layout per access point (phone versus
> laptop versus rig) is one more reason the terminal screen needs to know which
> access point opened it.

**F2.** What is an equipment slot, and how many does a new player have?

> Answer (2026-09-21): An equipment slot is a usable item, it's a visual indicator on
> the character or inventory screen that shows the player "You have this item and can
> use it to do things". I imagine a limited number of equipable slots and probably
> slot types (device/drone/weapon). A new player can have 1 slot, just for their phone.
>
> Consequence noted: a slot has a type, and an item fits only a slot of its type. The
> first build has one slot, of type device. The count and the types are hedged
> ("imagine", "probably"), so the framework should carry a slot type without fixing
> the list.

**F3.** What gives a player more slots, or a slot of a new type?

> Answer (2026-09-21): We really don't need to restrict them, we could give full slot
> view and they are just empty until the character acquires the items that fill them.
> No reason to hide them other than to simplify early game mechanics.
>
> Consequence noted: the slot set is fixed by design, not earned. Every player has
> every slot from the start, and slots sit empty until an item fills them. This
> matches the terminal rule that locked apps show with a locked-out notice [Q10]: the
> player sees what exists before they can use it. F2's "1 slot" reads as "one slot
> filled", not one slot shown.

**F4.** Where is an equippable item when it is not in a slot, and how does it get into
one?

> Answer (2026-09-21): Equipable items can land in your plain inventory or in other
> storage mechanisms (home storage chest, bank, dropped on the ground). A player drags
> an item from their inventory into the slot, then it's equipped and usable. Future we
> can do auto equips, etc but it's mechanical for now Inventory -> Equipped.
>
> Consequence noted: an equippable item is an inventory item first. The inventory
> must hold instances next to stacks, and a slot points at one of them. Equipping is a
> player action, so it is a client message and an intent on the server. Drag and drop
> is the first drag interaction in the client. Storage chest and bank are named but
> not built, and nothing here depends on them.

**F5.** Describe the phone battery: what drains it, what happens at zero, and what
restores it.

> Answer (2026-09-21): Phone battery drains level 100-0. It has an indicator in the
> terminal UI and in the equipment slot (simple level bar `[\\\\\     ]`) with red and
> green and yellow color indicators. It drains from idle and use. At idle it has a
> slow multiplier (it lasts 3 full life days from 100% -> 0%), using it to access
> terminal drains it at a faster rate (this needs calculated so it's somewhat
> reasonable, I don't have the formula now, something like a full 3 hours of constant
> use drains it to zero, similar to real life but more forgiving). At 0 it's dead, the
> battery needs replaced OR it can be charged. So charging ports is a new thing also
> (we don't need it right now but it's a realistic feature, day 1 playable is find a
> new battery to recharge).
>
> Consequence noted: the battery is per-phone state that changes with time, so it is
> the first equipment behaviour that runs in `Tick`. Two drain rates: idle and in
> use. The numbers (3 days, 3 hours) are placeholders until seen in play. Charging
> ports are deferred. The battery item restores the level, and the visible colours
> come from thresholds the design has not set.

**F5a.** Follow-up: are the 3 idle days real time or world-clock time, and does idle
drain run while the player is disconnected?

> Answer (2026-09-21): Idle days are real time, my phone in my pocket will last 3
> days on a full charge if I don't use it, same in the game. Disconnecting from the
> game does grant downtime, I think it would be unreasonable to always open the game
> and have your phone be dead (we can suspend disbelief here for practicality).
>
> Consequence noted: drain runs only while the player is connected, in real seconds,
> from the server tick. There is no catch-up on reconnect, so nothing about the
> battery needs a timestamp in persistence, only the level. "3 days" means 3 days of
> connected play without use.

**F6.** How does a player get their first phone in this build, given the Training
Grounds is not built?

> Answer (2026-09-21): On first load they get a phone. Training Grounds = no phone,
> real game = you get a starter phone.
>
> Consequence noted: "first load" is player creation, once ever, not once per boot.
> The starter phone is created with the player record. This fits "nobody starts with
> a phone" because that rule is about the Training Grounds, which is not built. The
> phone arrives at ten percent [Q16] unless F-answers say otherwise.

**F6a.** Follow-up: does the starter phone arrive equipped in the device slot, or in
the inventory for the player to drag in?

> Answer (2026-09-21): Put it in the inventory, I think we need an indicator on the
> item description that says it's equipable and what it does to help the player out.
>
> Consequence noted: the first thing a new player does is open the inventory and drag
> the phone into the slot. An item definition therefore carries a description, the
> slot type it fits, and what it does, and the inventory panel shows them. Today the
> panel shows stacks by type and tier only.

**F7.** What does another player see in the world when someone is using their phone,
and when the phone is equipped but not in use?

> Answer (2026-09-21): In use, the other players see the character with a phone in
> their hand. They see it glowing and see color changes (like you would in real life,
> think TV shadows on the wall). You see the screen flickering so you know they are
> doing something (this is arbitrary flicker, it does not tie to activity, it's just a
> demonstration that the player is "on their phone"). This is new because it's the
> first "combination" of models. We have the character model that needs to now show
> that they are using a phone. It should be the standard phone held up to face, head
> down position, staring at phone. When it's equipped it's just in their pocket, so
> nothing is visible.
>
> Consequence noted: three new client pieces. A character pose (head down, phone
> up), a phone model attached to the hand, and a light effect on the phone screen
> that is client-side only and carries no server state. This is the first case of
> "effects over voxels" [C-2026-09-20]. The world view already says a player is at a
> terminal, but not whether it is a fixed terminal or their phone, and the pose
> differs, so the view has to say which.

**F8.** What can happen to the phone in this build besides battery drain: damage,
theft on fainting, hacking, or none of these yet?

> Answer (2026-09-21): None of these for the first playable.
>
> Consequence noted: the phone has one piece of changing state, the battery level.
> Damage, theft and hacking stay in world.md as decided and unbuilt [Q1, Q2].

**F9.** How does the player apply a battery to the phone, and what happens to the
battery item after?

> Answer (2026-09-21): Workbench for repairing items, workbenches need to be easily
> accessible (1 or 2 per town in public, they should not look the same. For instance a
> picnic table in the park can be a workbench, a reading corner in the library or a
> dedicated shop in town or a desk in the player's room). Workbench is simple for now
> (item slot (what's being repaired or built) and component slot (what you are adding))
> and an apply button. So I guess the phone itself carries an inventory/equipable and
> that equipable is the battery.
>
> Here is my thought:
>
> - Regular phone, has equipable slots for exactly 1 thing right now. A battery. No
>   inventory bag, just an equipable slot.
> - Dead phone, the battery is drained to zero in the phone's slot. We can't charge
>   yet so it needs to be repaired.
> - In the workbench you remove the dead battery and it goes into the player's
>   inventory. Then add a full battery (item pickup from ground or purchase in a shop)
>   using the apply component button.
> - Voila, their phone is recharged.
> - Dead battery stays in inventory or can be recycled (recycle bin or recycle
>   machine in town).
>
> Consequence noted: this answer adds three things the basic idea did not name.
> First, slots nest: the phone has a slot of type battery, the same slot concept the
> player has. Second, the battery is an instance item with its own state, and the
> charge may belong to the battery, not the phone. Third, the workbench is a new
> fixed interactable in the zone, like a terminal: walk up, use, a two-slot screen
> with an apply button. Several looks map to one function, so the workbench is a role
> a dressing object plays, not one model. The recycler is named and stays unbuilt.
>
> Note: `docs/first-playable.md` item 1 says "a battery is an item that restores it".
> This answer changes that on purpose: the battery is swapped at a workbench, not
> used from the inventory. Both the workbench and the battery swap join the first
> playable.

**F9a.** Follow-up: does the charge live on the battery rather than the phone, so a
phone with an empty battery slot is dead and a new battery item arrives at 100
percent?

> Answer (2026-09-21): Yes that's correct, so the phone's "state" is comprised of
> its components' states. I think this is bespoke logic for the phone but it
> probably applies to other things, a drone has batteries and components (spy
> camera, EMP pulse generator etc.) so the mechanic is reusable (item with
> components and equipable slots).
>
> Consequence noted: the reusable shape is an item instance with typed component
> slots, where each component is itself an item instance with its own state. The
> phone's "can go online" is derived: it has a battery and the battery's level is
> above zero. The player's equipment slots and an item's component slots are the same
> mechanism at two levels.

**F10.** When the battery reaches zero while the player is online through the phone,
what happens?

> Answer (2026-09-21): Disconnects, the phone "shuts off and they lose their terminal
> connection". There are consequences for losing your terminal connection but we
> don't need to cover that now.
>
> Consequence noted: the server ends the terminal access in `Tick` when the drain
> reaches zero, the same path as leave. The client shows the terminal screen closing
> without a player action. Consequences of a lost connection are deferred.

**F11.** Where do the equipment slots sit on screen, and what does the player press to
use the phone?

> Answer (2026-09-21): For the player, I think they primarily live in the inventory
> screen. You see your inventory bag and then your equipable slots. But that's not
> how you "use your phone", we should add a HUD item that lets you easily say, pull
> out my phone and use it. Then the animation plays, hand in pocket, phone to face
> pose.
>
> Consequence noted: two surfaces. The inventory panel gains a slots section next to
> the bag, where drag and drop happens. The HUD gains a quick-use element for the
> equipped device, which shows the battery bar from F5. "Hand in pocket, phone to
> face" is a transition, so the character needs a short animation in and out, not
> only a held pose.

**F12.** What art does this need, and which pieces exist already: phone model,
battery model, item icons, workbench looks, the pose and transition animation?

> Answer (2026-09-21): None of those exist, everything is new. I think we need:
>
> - Phone model for inventory item view.
> - Phone model for physical in-game world view (what other players see while in
>   use).
> - The animation for the phone glowing (maybe it's not actually animation).
> - The pose and transition for the character is needed.
> - The battery model is needed.
> - The workbench model is needed (although I think we allow multiple things to be a
>   workbench, see previous answers), so I think this might just be a model type or
>   functionality (functionsAs(workbench)) tag.
> - UI for workbench, equipment slots in inventory, terminal phone use all need
>   figured out.
>
> Consequence noted: "functions as" is a tag on a placed dressing object, which is
> how a picnic table and a library desk both become workbenches. The same tag can
> later say "functions as terminal" and replace the random terminal placement. The
> inventory shows icons today, so "phone model for inventory item view" means either
> an icon drawn from the model or the model itself rendered small, to be decided in
> the technology fork. The glow can be a client effect, not an animation.

**F13.** Can the character move while online through the phone, or does using it hold
them in place like a fixed terminal?

> Answer (2026-09-21): It holds them in place. When using the phone the entire game
> screen is replaced by the terminal. I think this phone view of the terminal is
> different than the full terminal. It would be neat to show a peripheral view while
> using the phone (i.e. the world continues to render behind you but "in the
> background greyed out" and your phone terminal is in the center, hopefully that
> makes sense). But no movement while you're on the phone.
>
> Consequence noted: the server rule is the same as a fixed terminal, no movement
> while at a terminal. The client differs: the fixed terminal replaces the screen, the
> phone terminal is a centred panel over the still-rendering, greyed world. The
> world view keeps arriving while online through the phone.

### Outcome

Agreed 2026-09-21.

The framework:

- A player has a fixed set of equipment slots, each of a type (device, drone,
  weapon, ...). All slots show from the start and sit empty until filled. [F2, F3]
- An equippable item is an inventory item first. The player drags it from the bag
  into a slot of its type. Auto-equip is future. [F4]
- An item instance can have component slots of its own, filled by other item
  instances with their own state. The phone has one, of type battery. The mechanic is
  reusable (drones and their components). [F9, F9a]
- An item's description says it is equippable and what it does. [F6a]
- Equipment slots live in the inventory screen next to the bag. The HUD has a
  quick-use element for the equipped device. [F11]
- A workbench is a fixed interactable in the zone: an item slot, a component slot,
  an apply button. Several dressing objects function as a workbench (picnic table,
  library corner, shop, room desk), one or two per town in public. [F9, F12]

The phone:

- Using the phone opens the terminal world with a simpler layout than a laptop or
  rig. The layout is TBD with the terminal layout. For now its only point is a
  portable way to go online. [F1]
- The battery is a component with a charge from 100 to 0. The phone's charge is its
  battery's charge. No battery, or zero, means dead: it cannot go online. [F5, F9a]
- Drain runs in real seconds while the player is connected: about 3 days idle,
  about 3 hours in use. Numbers are placeholders until seen in play. No drain while
  disconnected. [F5, F5a]
- At zero while online, the phone shuts off and the player loses the terminal
  connection. [F10]
- A dead battery is swapped at a workbench: remove it to the bag, apply a full one.
  A new battery item is at 100. Full batteries come from the ground or a shop. [F9]
- The level shows as a bar with red, yellow and green in the equipment slot and in
  the terminal UI. [F5]
- On first load in the real game the player gets a starter phone in the inventory,
  at ten percent [Q16]. The Training Grounds gives none. [F6, F6a]
- In use, others see the character holding the phone up, head down, screen glowing
  and flickering. Equipped and not in use, nothing shows. A transition plays, hand
  from pocket to face. [F7, F11]
- No movement while on the phone. The phone terminal is a centred panel with the
  world rendering greyed behind it. A fixed terminal replaces the screen. [F13]
- Damage, theft on fainting and hacking of the phone: none in the first playable.
  [F8]

Art, all new: phone model for the inventory view and for the world, battery model,
the pose and transition, the screen glow (an effect, not necessarily an animation), a
"functions as workbench" tag rather than one workbench model, and UI for the
workbench, the equipment slots and the phone terminal. [F12]

Deferred:

- The phone's terminal layout and its apps. Waits on the terminal layout. [F1]
- What increases the slot count or adds slot types beyond the fixed set. Not needed
  while all slots show. [F3]
- Auto-equip. [F4]
- Charging ports. [F5]
- Consequences of losing the terminal connection. [F10]
- Recycler, shop, home storage chest, bank. Named, not designed. [F4, F9]
- The drain formula and colour thresholds. Set in the renderer, not in words. [F5]

## Technology design

### Developer thoughts

> (2026-09-21) We need something to store and persist real items held by the player.
> I think this expands the inventory subsystem and is a different storage mechanism
> (db table probably). Since a phone is also composable (battery component equipable)
> it also gets an inventory so we are at nested inventories now. I would need to think
> longer on this but I am sure there is a solution here. We also need to persist state
> of items (phone battery level) and provide ticks for them to drain properly. I think
> this is a whole new type of item in the world that has a tick cycle of its own. We
> need a core place to define these things or a generalized framework for
> implementing tickable items (drones drain battery and have speed is a future one). I
> am thinking we should tackle that now, in a simplistic way though. We need to codify
> animations and how to trigger them along with poses. We need a way to model an
> animation and pose in our graphics pipeline. We need a way to trigger effects on
> the phone screen. The phone overlay on the active physical world is a new drawing
> mechanic, might just be a new type of HUD draw actually that takes focus. The HUD
> display for the equipped item is new and it shows status from an equipable item.
> Each of these is actually not simple to think of the code/database/server/pipeline
> process.

### Q&A

Facts held in mind for this fork, from the code on 2026-09-21: inventory is stacks
keyed by type and tier, one row per stack, saved by replace. The device is a nullable
`TerminalType` on the player and in `TerminalAccess`. Private state reaches one player
through `IPrivateState<TView>` with dirty owners. The character rig has six parts and
procedural poses. Clips as data are planned, not built. Dressing placements carry free
properties (`sway`, `effect`, `sound`) from the scene.

**T1.** How should the inventory hold the phone and the battery next to the existing
stacks of GPUs and RAM?

> Answer (2026-09-21): Two pieces here. Phone is an individual item instance, it's
> not a stackable <arbitrary item>. Battery is stackable, I can have a pack of 100
> batteries, but once it is not at 100% it kinda becomes unique or an instance in a
> way. You can't mix 100% batteries with 50% batteries, that would be a really weird
> state to handle and UX would be bad.
>
> Consequence noted: an inventory entry is a stack or an instance, and the item's
> definition says which it starts as. A battery starts as a stack and becomes an
> instance the moment it has state of its own, when one leaves the stack for a
> phone's slot. It never returns to a stack. The phone is an instance from creation.
> So the existing stack model stays, and instances are added next to it, not in place
> of it.

**T2.** How does the phone own its battery: where does the battery instance live
while it is in the phone, and what does the phone hold to say so?

> Answer (2026-09-21): A phone is an individual item instance with equipable slots.
> When the battery is in the phone its sole existence is in the phone. A dropped
> phone means the battery goes with it. The phone holds a list of its equipped slots
> and what's in them. Things that are equipped can only be owned by one thing.
>
> Consequence noted: ownership is a tree with one parent per instance. The player
> owns the phone, the phone owns the battery. An instance therefore has one owner id,
> which is a player or another instance, plus which slot of that owner it sits in
> when it is equipped. Dropping the phone moves the root of a subtree, and the
> children move with it without a change of their own. The existing `Inventory`
> already says "owners are not only players", and this is that promise used.

**T3.** What does the database hold for an item instance, and how is its changing
state, the battery charge, stored and saved?

> Answer (2026-09-21): Item instance, example case phone:
>
> - Type (phone).
> - Unique ID.
> - Owner (the player).
> - Owner if it's not equipped by the player: what if it's dropped or in a storage
>   bin. The storage bin is the owner. Or the world/scene is the owner.
> - Children of the instances (the components or equipable slots), things the item
>   owns.
> - Available slots and slot types. I think we also need to define this for each type
>   (a phone has 1 slot that is for type battery).
> - State, might be tricky here. The phone state (battery level) is fed from the
>   battery slot. A phone without a battery has no "charge" available. So you might
>   say the phone item has dependencies on its slots to help formulate its actual
>   state. When you are looking at your phone you don't ask yourself, what is the
>   battery capacity and level, you ask yourself is my phone charged up? So there is
>   a storage issue for the DB, a dependency tree and then some kind of formulaic
>   meaning that comes out of all that.
>
> Battery, all the above, some of them are null. A battery owns nothing. It's a
> base/terminal node in an item tree. Not sure if the terminal/no ownership below
> needs to be defined. A battery has no available slots. The battery is owned by the
> phone it's in or the player whose inventory it's in or the chest it's saved in.
>
> - Battery has a real attribute and that is charge level, that gets persisted. I
>   imagine a row in the database that I can query and ask the question ("For every
>   player on the server, check their phones and see what the average battery level
>   is").
>
> Consequence noted: the row is type, id, owner id, and the slot it fills in its
> owner (null when it sits loose in the owner's bag). Children are found by owner id,
> so the row does not list them. Available slots come from the type definition, not
> the row. The battery's charge is a real column, so SQL can answer the average
> question with a join from phone to battery on owner id. Derived state, "is my phone
> charged", is computed in the simulation from the tree and is never stored. A dropped
> phone is owned by the world, which today is `WorldItem` with a position, so a
> dropped instance needs a position the way a dropped stack has one.
>
> Note: the battery charge is the only stateful attribute in this build, so one
> nullable column serves. The shape for many attributes across many item types (a
> column each, or an attribute table) is left open until a second stateful item
> exists.

**T4.** Where do item type definitions live (slots, stackable or not, description,
behaviour), and who reads them?

> Answer (2026-09-21): I don't know the answer to this. I think it's in the database
> but could also be code based.
>
> Who reads them?
>
> - The simulation reads them and knows about them. It needs to know to tick the
>   battery level down, because that item is in a phone and the player is online.
> - The server reads them so it knows that a player action is valid (you can't put a
>   tree into the battery slot on a phone). Those rules are probably cached client
>   side so we can give helpers and avoid the round trip and validation on the server
>   (i.e. client knows you can't put tree in a phone, it knows you only put battery in
>   the slot of a phone).
> - Client and server know description and behaviors. Behavior is code based
>   (Battery.Charge = charge - 1 every N ticks). I think these behaviors are
>   extendable and composable (an item can have several behaviors and a behavior can
>   be placed on many items). Sometimes a behavior is unique and never applied to
>   more than 1 item. But I think we go with a general case here.
>
> Consequence noted: a definition has two halves. Data (slots, stackable, description,
> which behaviours) and code (the behaviours). Both client and server read the data
> half. The client has no database, so data in the database would need a message or
> a packaged file to reach it, while data in `Core` reaches both by being compiled
> in. Behaviours are `Core` code either way, and the simulation runs them in `Tick`.
> The client checks a drag before sending, and the server checks again in the intent
> method, closed by default as every other intent is.

**T4a.** Follow-up: the client has no database access, so definitions in the database
need a way to reach the client. Is that acceptable for this build, or should the
definitions be code in `Core` for now?

> Answer (2026-09-21): I think it's a piece of data the server sends the client at
> load. This probably is extendable. Send information to the client that can be
> cached client side (type definitions, descriptions etc.), the client caches this
> and the server can refresh whenever it wants to do that. So cache front load that
> can be refreshed, on client load it requests the cache or the server provides it
> automatically as part of the connect process. The client saves this because it's
> used often.
>
> Consequence noted: definitions are server data, sent as a message after connect
> and held in a client cache, the same shape as the other caches. The server can
> resend at any time and the client replaces its copy. Nothing in the definitions is
> compiled into the client except the behaviour code and the enum of types.

**T4b.** Follow-up: each of our messages is one datagram, without fragmentation or
acks. Definitions with descriptions for many items will not fit one datagram. How
should the definitions reach the client given that?

> Answer (2026-09-21): We either break it up into individual packets that are
> generic or we build the multi packet system. I.e. I can imagine how we break down
> the giant list to something like MessageType: <ServerItemDescriptionCache> and we
> just send N many for each item we need the client to cache.
>
> I am not opposed to either direction.
>
> Consequence noted: the two directions differ in what a lost packet costs. One
> message per definition loses one definition, and a resend of that one repairs it.
> A multi-packet message loses the whole message when any part is lost, and needs
> ordering and acks to reassemble, which is the reliability layer that inventory
> deltas already wait on [session notes, 2026-09-17]. One message per definition
> keeps the rule that every message stands alone and self-repairs.

**T4c.** Follow-up: one message per definition keeps the rule that a lost message
repairs itself, the same reason inventory deltas were deferred. Do we take that for
this build, with the multi-packet system left to the reliability layer?

> Answer (2026-09-21): We should do a cost analysis for the multi-packet system and
> see if it's worth doing now or waiting.
>
> Options offered (cost analysis, asked for). Facts from the code: a message is one
> datagram, capped at `SafeUdpPayloadBytes` = 1200 bytes, received into a 2048-byte
> buffer. There are no sequence numbers, acks, or retransmits anywhere. Every
> message today is a full snapshot that a later message repairs.
>
> Size of the actual problem in this build: four item types (GPU core, RAM stick,
> phone, battery). A definition with a name, a description of up to 120 characters,
> a stackable flag and a slot list is about 150 to 200 bytes. All four fit in one
> datagram with room for a dozen more. The multi-packet need does not exist until
> the catalogue is about 6 or more definitions with long descriptions, or until
> something else large arrives (inventory deltas, a chat backlog, zone data).
>
> Option A, one message per definition. A `ServerItemDefinition` message, sent once
> per type after connect, and a `ClientRequestItemDefinitions` message the client
> sends when it meets a type it does not have. A client cache like the others. A
> lost definition is repaired by the request. About one message pair, one handler
> each side, one cache. No change to the send path. Scales to any catalogue size
> without a reliability layer.
>
> Option B, a multi-packet system. It is not only fragmentation. Fragmentation alone
> (split above 1200 bytes, header with message id, index and count, reassembly per
> sender) is small. But a lost fragment loses the whole message, so it also needs
> acks, a send buffer with resend on timeout, sequence numbers, and a cap on
> in-flight memory per client. That is the reliability layer: it changes
> `ClientSession` and `ServerConnection`, the dispatcher, and needs loss-simulation
> tests. Several times the size of option A, and it pays back only when the second
> and third large payload arrive. It is also a design session of its own (ordered
> versus unordered, per-message versus per-channel reliability).
>
> Option C, definitions in the content package. The connect already refuses a
> content version mismatch, so a definitions file next to the zone data reaches the
> client with zero network cost. The cost is that "the server refreshes whenever it
> wants" becomes a content version bump and a client restart, which is the opposite
> of the answer to T4a.
>
> Reading: A now. B when the first real multi-datagram payload exists, as its own
> feature session, with A's messages unchanged by it. C only if the T4a answer
> changes.
>
> Decision (2026-09-21): Let's go with A for now.

**T5.** How should item behaviour (the battery drain) be defined and run in the
simulation, so that a second behaviour later fits the same shape?

> Answer (2026-09-21): So I imagine we have a class called "ItemInstance". Our
> battery item, that's in the phone, that's equipped by the player, gets a Tick call
> every N ticks, or some pattern (I don't know yet). It has callable methods
> ApplyBehaviors. Our simulation calls that method (I am not hard on the name, this
> is me freeballing). And then the behavior executes, I think the behavior is also a
> class, call it "ElectricChargeBehavior". It knows what owns it and can interact
> with that thing via interface? and decrement the charge. I think the mechanic or
> pattern I am getting at is composition, maybe not. I know there is a pattern for
> this but I need to look it up. Then that same "Behavior class" can run on the drone
> also.
>
> Consequence noted: this is the Component pattern as game programming names it
> (Nystrom, "Game Programming Patterns", chapter Component): an entity is a plain
> container, and its behaviour is a list of small objects that each own one concern
> and are updated in turn. Composition over inheritance, so a battery and a drone
> share `ElectricChargeBehavior` without a common base class. The full entity
> component system (ECS, with behaviour in systems that scan components across all
> entities) is the larger version; the per-instance list is the smaller one and is
> enough here. Two conventions hold: the behaviour runs only from `Tick`, and it
> mutates only its own instance's state. Anything a behaviour changes on the
> simulation goes through the simulation, not around it.

**T5a.** Follow-up: the drain rate depends on facts the battery does not hold,
whether its phone is equipped and whether the owner is online through that phone.
How does the behaviour learn that?

> Answer (2026-09-21): I would like to know how the component system describes
> handling this. I suppose the ElectricChargeBehavior might be parameterized or
> modifiable. We can set the behavior to Off when the phone is off, we can set the
> drain rate when the phone state changes. So the phone's state changes the battery
> state and behaviors. I am not sure where this layers in.
>
> Options offered (asked for). Nystrom's Component chapter gives three ways
> components talk: through shared state on the container object, by holding a
> reference to a sibling, or by messages sent through the container. For context
> from outside the entity, the update call takes the world, or the parent entity
> passes what its children need. Applied here, the layering is:
>
> 1. `WorldSimulation.Tick` knows the only outside fact: who is online through which
>    device (`TerminalAccess`). It walks each player's equipped instances and ticks
>    each with a context: the delta time and whether that instance is in use.
> 2. The phone's behaviour reads the context and sets the drain rate on its
>    battery's `ElectricChargeBehavior` (off, idle, in use). That is the parent
>    passing down what its child needs, which is the author's "phone state changes
>    the battery behavior".
> 3. `ElectricChargeBehavior` subtracts rate times delta from its own charge and
>    nothing else.
> 4. Results flow up by return, not by reaching out. The phone's tick reports "went
>    dead this tick", and the simulation, which owns terminal access, calls leave.
>    A behaviour never touches the simulation.
>
> Two ways to do step 2. Push: change the rate when state changes (equip, use,
> leave), so the rate is stored and can go stale if one transition is missed. Pull:
> derive the rate every tick from the context, so nothing is stored and nothing
> goes stale. Pull is the same reasoning as full snapshots over deltas, and the walk
> is small (a few instances per player). Reading: pull.
>
> Decision (2026-09-21): Yes, pull makes sense I guess.

**T5b.** Follow-up (author's question): Let's think about the context. In this
instance we have a phone item and a drone item. Each have a battery item with the
same behavior, every N ticks the battery level drops. Where does the distinction lie
and what does context represent? For instance does a battery know it's inside a drone
vs a phone?

> Options offered (asked for). The battery does not know, and should not. The
> distinction lies in the parent. Two layers of context:
>
> - The simulation's context is generic and the same for every instance: delta
>   time, and the facts the simulation holds about that instance's situation. For an
>   equipped phone that is "in use at a terminal". For a deployed drone it would be
>   "deployed, at this position". The simulation does not know what a phone does
>   with that.
> - The parent's context is what the parent hands its components. The phone's
>   behaviour turns "in use" into a draw. A drone's behaviour turns "flying" or
>   "docked" into a draw. Both hand the battery one number.
>
> So the battery's behaviour has one input, the draw, and one state, the charge. It
> is the same as a real battery: it does not know the device, it knows the current
> being drawn. The numbers from F5 (3 days idle, 3 hours in use) are the phone's draw
> table, not the battery's. Later, capacity can be a battery property by tier, and
> the same draw lasts longer in a better battery [Q14], without the phone or the
> drone changing.
>
> One consequence: the item-type-specific code is the parent's behaviour (a phone
> behaviour, a drone behaviour), and the reusable code is the leaf behaviour (charge
> drain). The type definition lists which behaviours an item type carries, so
> "phone" = device behaviour + one battery slot, "battery" = charge behaviour, no
> slots.

**T5c.** Follow-up (author's question): Draw this for me then. Do I have a Phone.cs
class anywhere that is the phone instance, or do I have an ItemInstance of
type(Phone)?

> Options offered (asked for). No `Phone.cs` instance class. A phone is an
> `ItemInstance` whose type is Phone and whose behaviour list holds a
> `DeviceBehavior`. Phone logic lives in that behaviour, and the definition says the
> Phone type carries it. A `Phone : ItemInstance` subclass would be the inheritance
> route the composition decision rejected. Sketch, names open:
>
> ```
> Core/Simulation/Items/
>   ItemDefinition        Type, Name, Description, Stackable, Slots[], Behaviors[]
>   SlotType (enum)       Device, Drone, Weapon, Battery
>   ItemInstance          InstanceId, Type, OwnerId, Slot?, Slots{SlotType->ItemInstance}, Behaviors[]
>   IItemBehavior         Tick(ItemInstance self, ItemTickContext context, float delta)
>   ItemTickContext       InUse (from the simulation; grows as items need more)
>   DeviceBehavior        phone logic: sets its battery's Draw from InUse; answers CanGoOnline
>   ElectricChargeBehavior  Charge (persisted), Draw (set each tick, not persisted)
>   ItemCatalog           definitions by type; Create(type) builds an instance with its behaviours
>
> Core/Simulation/
>   EquipmentStore : IPrivateState<EquipmentView>   player -> {SlotType -> ItemInstance}
> ```
>
> One tick, as a call stack:
>
> ```
> WorldSimulation.Tick(delta)
>   TickEquipment(delta)
>     for each player, for each equipped instance (the phone):
>       context.InUse = _terminalAccess is in through this instance
>       phone.Tick(context, delta)
>         DeviceBehavior.Tick   battery = phone.Slots[Battery]
>                               battery.ElectricCharge.Draw = InUse ? InUseDraw : IdleDraw
>         battery.Tick(context, delta)          children after the parent, so this
>           ElectricChargeBehavior.Tick         tick's draw applies this tick
>                               Charge -= Draw * delta
>       if DeviceBehavior says dead and the player is in through it:
>         _terminalAccess.LeaveTerminal(playerId)   the simulation acts, not the behaviour
> ```
>
> The charge is state on the leaf behaviour, and it is the column the database
> holds. The draw is derived each tick and never stored. "Is my phone charged" is a
> question to `DeviceBehavior`, which reads its battery slot.
>
> Answer (2026-09-21): Ok. So we would, for any device in our world (drone, laptop,
> deployable electronic item) have a device behavior. Batteries would have the
> ElectricChargeBehavior.
>
> Note: "device" already means a door into the terminal world (phone, laptop) in
> the code and in world.md [C-2026-09-18]. A drone draws power but is not a door. So
> the answer names two concerns under one word: "draws power from a battery slot"
> (phone, laptop, drone, any deployable electronic) and "opens the terminal world"
> (phone, laptop). If they are one behaviour, a drone gets terminal logic it cannot
> use. If they are two, the phone is powered + terminal access, and the drone is
> powered + whatever flight becomes. That is composition doing its job.

**T5d.** Follow-up: should "draws power from a battery" and "opens the terminal
world" be two behaviours, and if so, what do we call them? The word "device" is
already taken by the terminal vocabulary.

> Answer (2026-09-21): Device also has a broader meaning and would be easy to
> mistake, as I already did, so we might need to consider that more. So the question
> becomes: does this consume batteries? If YES it gets an electric charge behavior
> (or maybe a better name actually). Then the question: is this a door to the
> terminal world? If YES I don't know what that implies yet but it's not the battery
> behavior.
>
> Consequence noted: two questions per type, two behaviours, and a third on the
> battery itself. So three behaviours in this build:
>
> 1. On the battery: holds a charge (the persisted level, drains by draw).
> 2. On the phone: consumes a battery (has a battery slot, sets the draw from its
>    situation, answers "is it powered").
> 3. On the phone: is a door to the terminal world (what that implies is open; today
>    it is what `TerminalAccess.UseDevice` checks before letting a player in).
>
> Options offered (names, since a better one was asked for):
>
> - Holds a charge: `BatteryBehavior`, `ChargeStoreBehavior`, `PowerCellBehavior`.
> - Consumes a battery: `PowerDrawBehavior`, `BatteryPoweredBehavior`,
>   `PoweredBehavior`.
> - Door to the terminal world: `TerminalDoorBehavior`, after world.md's "a terminal
>   is any door into the terminal world"; or `GoOnlineBehavior`, after the UI verb.
>
> Reading: `BatteryBehavior` + `PoweredBehavior` + `TerminalDoorBehavior`. Short,
> and none of them says "device".

**T5e.** Follow-up: which names, or others?

> Answer (2026-09-21): PowerCellBehavior sounds good and re-usable.
> BatteryPoweredBehavior (distinction here because I want items that require hard
> line power to run). TerminalDoorBehavior sounds good, but I am not sure if this is
> required at the moment, but if "does this open a terminal" fits into this system
> then that's a good name.
>
> Consequence noted: `PowerCellBehavior` on the battery, `BatteryPoweredBehavior`
> on the phone, and a future mains-powered behaviour for the gaming rig and the
> supercomputer, which is why "battery" stays in the name. `TerminalDoorBehavior` is
> required as soon as the phone is an item: today "has a device" is a
> `TerminalType` per player in `TerminalAccess`, and with the phone as an equipped
> instance that check becomes "the device slot holds an instance with a terminal
> door behaviour that is powered". It also carries which `TerminalType` layout the
> door opens.

**T6.** Today `TerminalAccess` holds a device type per player, and the player row
persists it as `device_type`. With the phone as an equipped item, what does
`TerminalAccess` hold, and does `device_type` go away?

> Answer (2026-09-21): As far as I am aware, terminals spawn in the world as items
> on the ground. A player walks up and "logs in" and is shown a simple terminal
> view. I think we record that the player is connected to the terminal. That's the
> current state and that does not jive with how you are reading to me that it's
> implemented. It sounds like the implementation went a little bit further and
> applied ownership of devices (terminals). Not to say that's wrong but the
> mechanics and the implementation don't align, which is where I am confused.
>
> Note: the author is right about the mechanics in play. Only fixed terminals work
> today. The device half of `TerminalAccess` (the carry dictionary, `GrantDevice`,
> `players.device_type`, the "Go Online: Phone" button path) is dormant scaffolding
> from 2026-09-18, built as the hook for the Training Grounds exit and never
> reachable in play. It modelled the carried phone as a type on the player, not as
> an item. This feature replaces it.

**T6 (restated).** Do we remove the dormant device half (the carry dictionary,
`GrantDevice`, the `device_type` column) and let the equipped phone replace it, with
`TerminalAccess` keeping only "who is at which terminal"?

> Answer (2026-09-21): I think a full redesign of the existing code is needed here.
> I don't want to ask the question "what do we keep and work into the new design we
> have". I think we say "what's the best way to implement our feature, how do we
> want to build our new feature". Then we say "rip this old stuff out and add our
> new things and rebuild anything from the old way". We are at a point now that it's
> okay to do that.
>
> Consequence noted: the terminal access code is not a constraint on this design.
> The Outcome describes the new shape from scratch, and the build removes the old
> device half and rebuilds the fixed-terminal half to fit. The rules from world.md
> still hold (one player per fixed terminal, one terminal per player, being at a
> terminal does not survive a disconnect, use and leave as the verbs) because they
> are design, not code.

**T7.** Designed fresh: when a player goes online, through a fixed terminal or through
the equipped phone, what does the server record about it, and what does it tell the
client?

> Answer (2026-09-21): It records that they are online and accessing the terminal
> world. The server needs to tell the client everything about the terminal world
> (status shown there but not in the physical world, states, availability of apps in
> the terminal).
>
> Consequence noted: "online" is a per-player state with a door: a fixed terminal id
> or the phone instance id. The server keeps the fixed-terminal occupancy next to it.
> What the client gets is a private terminal-world view: online or not, the door's
> `TerminalType` (which picks the layout), and the app list with availability. The
> app list is a placeholder until the terminal layout is designed [F1], but the view
> has a place for it from the start. Other players learn only "at a terminal" and
> which kind of door from the world view, for the pose [F7].

**T8.** What does the client send for equip, unequip, use phone, and workbench apply,
and how does the server answer each?

> Answer (2026-09-21):
>
> Equip:
>
> - Client should have client-side guards (i.e. you can't equip a tree into a
>   battery slot).
> - If the client-side protection passes, it sends the intent to the server because
>   simulation logic requires knowing what's equipped for game rules.
> - If successfully equipped via server-authoritative logic, we reply "item equipped
>   successfully".
>
> Unequip: same logic as equip.
>
> Workbench apply:
>
> - Same client-side validation, rudimentary rule checks at the client (can't put a
>   tree in a phone).
> - Server round trip to produce the result (a phone with a battery in it). Server
>   records ownership and starts battery drain states.
>
> Consequence noted: the client guard and the server check read the same
> definitions (T4), so the rule lives once in `Core` and both sides call it. New
> client messages: equip (instance, slot), unequip (slot), workbench remove
> (workbench, target instance, slot) and workbench apply (workbench, target
> instance, component instance). F9 makes remove and apply two steps, so they are
> two messages. "Use phone" is the existing use-terminal intent with the equipped
> door as the target. Every intent is closed by default: the server checks
> ownership, slot type, reach to the workbench, and that the workbench is one.
>
> Note: "reply item equipped successfully" differs from the current convention. A
> party invite and a terminal use answer with silence on refusal, and the client
> learns success when its private snapshot changes. An explicit success reply would
> be a new pattern.

**T8a.** Follow-up: for equip, unequip and workbench, is the changed equipment
snapshot the answer, as parties and terminals do today, or do you want an explicit
success or failure reply?

> Answer (2026-09-21): There is no reason I can think of for the success reply. I
> think we follow the pattern, or we establish a new one here and now. If we should
> be replying success/fail on every intent then let's do that now. If we reply only
> on failure and then send updates about the new state as a replacement for the
> success message, then that's good also.
>
> Options offered (asked which). Facts: `ServerMessageRejected` exists with one
> reason, `Unauthenticated`, sent by the dispatcher for a bad session. No intent
> handler sends it. A success reply on every intent is redundant with the snapshot
> that follows it, and on UDP one of the two can arrive without the other, so the
> client must handle the snapshot alone anyway. A failure reply is not redundant:
> silence and a lost packet look the same to the client, and a drag that quietly
> snaps back tells the player nothing. Reading: keep the pattern for success (the
> snapshot is the answer) and adopt "reply on failure" now, by extending
> `ServerMessageRejected` with reasons per intent (slot type mismatch, not owned,
> out of reach, occupied). Equip is the first intent to use it, and parties and
> terminals can follow when touched.
>
> Decision (2026-09-21): Yes, let's use that pattern and try to use it as
> convention. If an intent fails for some reason we tell the client why, so we can
> produce some in-game notification (bump into tree: "you can't move that way";
> workbench failed: invalid item message and a clank sound) etc.
>
> Consequence noted: convention from here on: success is the next snapshot, failure
> is `ServerMessageRejected` with a reason the client can turn into a notice or a
> sound. One care: movement is an intent repeated every tick while a key is held, so
> a rejection per blocked tick would flood. The client already knows the walls from
> the dressing, so a wall bump is a client-side notice, and the server rejection
> covers one-shot intents (equip, workbench, terminal use, party).

**T9.** How does the client know to show the phone pose and the pocket-to-face
transition, and where does that pose come from, given the rig today is procedural?

> Answer (2026-09-21): Pocket-to-face transition comes first. When the player clicks
> on the phone/laptop HUD interact button we start the animation, wait for the
> animation to complete, freeze in the pose and then open the terminal world.
>
> That pose comes from some animation art I guess. Do we need to now define
> animations or rigs for poses and animated movements? I think so. So we have an
> animation library and pose library we can apply. Freeballing here, I don't know
> the best way to do this but I think we need to define these and then a way to
> enact them on models.
>
> Consequence noted: this is the "clips as data" piece that `docs/engineering/
> animation.md` planned and did not build: a clip is rotations per named part over
> time, and a pose is a clip with one frame held. The library is a folder of clip
> files beside the models, and the animator plays a clip on top of, or instead of,
> the procedural walk. For other players, the trigger is the world view: when a
> body's "at a terminal through a held door" flag turns on, their client plays the
> transition and holds the pose, and when it turns off, plays it back. For the local
> player, the click is the trigger.
>
> Note: the answer orders click, animation, then open. That leaves the order of the
> animation and the server intent open. If the intent is sent after the animation,
> a refusal (dead battery, already at a terminal) arrives after the player has
> watched the phone come out. If the intent is sent on the click, the animation
> covers the round trip and a refusal can play the phone going back.

**T9a.** Follow-up: is the use intent sent on the click, with the animation covering
the round trip, or after the animation ends?

> Answer (2026-09-21): So I think it goes click -> send intent -> confirm valid via
> world state (this is an instance where I think it would be good to send a message
> back saying "TerminalOpenApproved" or something, but we can probably do it with
> game state. Need to think more, but either way). Click -> send intent -> await
> approval -> start animation.
>
> Consequence noted: under the T8a convention the approval is the private
> terminal-world snapshot arriving with "online" set, and a refusal is the rejection
> message. So: click, send use, wait, snapshot says online, play the transition,
> hold the pose, open the phone terminal panel. The server counts the player as
> online from the approval, so in-use drain and the other players' pose start a
> second before the local terminal panel opens. That gap is the animation length and
> is fine.

**T9b.** Follow-up (author's addition to T9a): the online flag arrives in every
snapshot, so what stops the transition from replaying?

> Answer (2026-09-21): This has impacts. We can't just spin up the animation state
> for every online flag we see. We probably need to have a character state "Going
> Online Started" or "Login Playing" and then not show it again, i.e. let the
> animation play out, then a flag for "Yes the terminal is showing, don't play the
> animation and instead hold the pose".
>
> Consequence noted: the server sends a fact, "at a terminal through a held door",
> in every snapshot. The client keeps a small state per body: offline, going online
> (transition playing), online (pose held), going offline (transition playing back).
> The transition plays on the edge, when the fact changes, not on the fact. A body
> first seen with the fact already on goes straight to the held pose, so a client
> that connects late does not watch phones come out all over town. The same rule
> serves the local player. No new server flag is needed, and the server never
> tracks animation.

**T10.** In the client today a screen replaces the world and pauses input to it,
and a HUD panel sits over a live world. Which is the phone terminal, which is the
fixed terminal, and what does the greyed world behind the phone still do?

> Answer (2026-09-21): Only the phone terminal lets you see into the physical world
> via peripheral vision. This is the greyed-out sides of the real world where you
> still see things rendering but in a fog. The laptop and stationary terminals take
> over full screen and do not render the physical world.
>
> Consequence noted (withdrawn, see the Note below): the phone terminal is a
> HUD-layer panel that takes focus.
>
> Answer (2026-09-21, correcting the reading above): I think you got the phone UI/UX
> wrong. Let's talk about just the terminal world.
>
> - Terminal world: the online world that presents as a custom OS with OS features.
>   It has its own state.
> - Devices (phone, laptop, stationary terminal) are entry doors into the terminal
>   world.
> - Every device except for the phone takes you into a fullscreen terminal view
>   space.
> - Every device could have a unique "appearance" for the terminal view space
>   (public terminals have simpler interfaces for public use, the desktop interface
>   is advanced because it's player-based and player-specific, data centre terminals
>   are function-specific, phones offer a limited interface).
> - Phones are the only device that do not take full screen control. Much like when
>   you look at your phone in real life. It still presents the terminal screen (look
>   and feel and function and interface) but it is not full screen, it's centred on
>   screen like a phone held in front of your face. In peripheral vision at the
>   sides of the screen you can see the physical world still rendering and
>   interacting behind you but you cannot interact with it (because you are online
>   in the terminal world on your phone). So the phone use is not a HUD display.
>
> Note: the withdrawn reading put the phone on the HUD layer. It is not. There is
> one terminal screen, the terminal world's, and it has an appearance per door. The
> phone appearance is the same screen drawn in a phone-shaped frame at the centre,
> with the world screen still drawn behind it through a fog and receiving no input.
> Every other appearance covers the whole window. Where the door type comes from is
> unchanged: the terminal-world view says which door the player came in through.
> In the client this means the terminal screen owns the choice of appearance, and
> the phone appearance keeps drawing the world screen beneath it, which the screen
> manager does not do for any screen today.

**T11.** What does the equipment snapshot to the client carry: the slot map only, or
each instance with its components and their state (the charge)?

> Answer (2026-09-21): I am not sure how to represent this or the message it is
> carried under. This would take some planning and consideration.
>
> Options offered (the author was unsure). What the client needs: the bag (stacks
> and loose instances) for the inventory panel, which instance fills which player
> slot for the slots section, each instance's components for the workbench, and the
> charge for the battery bar. All of it is private to one player, all of it changes
> together on a drag, and all of it is small: a phone, its battery and a few stacks
> is about a hundred bytes.
>
> Reading: one private snapshot, "what you hold", replacing `ServerInventoryUpdate`.
> It carries the stacks as today, plus the instances as a list where each instance
> has its id, type, the slot it fills in its owner (or none), its owner (the player
> or a parent instance), and a short list of state values (charge). Children are
> found by owner id, so the tree needs no nesting in the wire format. Equipment is
> not a second message: an equip moves one instance's owner slot, and one snapshot
> shows the bag and the slots agreeing. Two messages could arrive in either order
> and show the phone in both places for a frame. The terminal-world view stays its
> own snapshot, as it is a different concern with a different trigger.
>
> Decision (2026-09-21): I think one message, but I see what you're getting at and
> it's growth of the inventory message. But one message for now until we run into
> that.
>
> Consequence noted: the growth limit is the datagram. A stack is 6 bytes and an
> instance about 40, so a bag of 100 stacks and 10 instances is about 1000 bytes,
> under the 1200 cap. The reliability layer (T4c, option B) is the answer when a
> bag passes that, the same as for deltas.

**T12.** Where does the workbench come from in a zone: how does a placement say "I
function as a workbench", and what does the server keep about it?

> Answer (2026-09-21): Can we do this with scene attributes (picnic table in the
> park gets interaction=workbench) and then our game can perform logic based on
> that? I think we can. The server needs to know about it I believe also, although
> maybe not. I would need to think this through more.
>
> Consequence noted: yes, and it is the existing mechanism. `sway`, `effect` and
> `sound` are free properties on a placement or its layer, read by
> `ZoneConventions` into `DressingPlacement` on both sides from the same compiled
> zone. `interaction=workbench` is one more. The server does need it: the workbench
> intents are closed by default, so the server checks that the target is a
> workbench and that the player is in reach of it, and reach needs the placement's
> position. So the dressing gains a list of interactables: which placement, what it
> functions as, where. Both sides load the same compiled zone, so the placement's
> index in it is a stable id neither side has to invent. The same property gives
> fixed terminals their placement from data (`interaction=terminal`), which replaces
> the random scatter, and it is the tag the workbench answer in F12 asked for.
> Nothing is persisted for a workbench: it holds no items between uses, the player's
> bag does.

**T13.** When is an instance's state saved: the battery charge changes every tick,
and today a player is saved on disconnect, on the periodic sweep, and at shutdown.
Is that enough for the charge?

> Answer (2026-09-21): Probably yes. I think we probably want some other method to
> save state long term like a periodic state save. And then probably more state
> saves (like zone transitions or scene transitions).
>
> Consequence noted: the periodic save exists: `PlayerSaveInterval` is 30 seconds
> in `ServerOptions`, and `PlayerSaver` takes the same copy for the sweep, the
> disconnect and the shutdown. The instance rows join that copy, saved by replace
> per owner as the stacks are, so a charge is at most 30 seconds stale after a
> crash. Zone transitions are a future save point on the same path. No per-tick
> write.

**T14.** What tests prove this feature? Name the big cases you want to see pass.

> Answer (2026-09-21): The whole phone feature? That's a lot.
>
> Options offered (the list, for the author to strike or add). Big cases only, in
> `Tests/Core` unless said otherwise:
>
> Items and equipment:
>
> - A new player's bag holds one phone instance with a battery inside at ten
>   percent.
> - Equip moves the phone from the bag to the device slot. Equip of a stack, or into
>   the wrong slot type, is refused and nothing moves.
> - Unequip moves it back.
> - Dropping an equipped phone puts one world item on the ground and the battery
>   goes with it. (Only if drop is in this build.)
>
> Battery and tick:
>
> - Charge does not change before a tick.
> - An equipped phone drains at the idle rate per second of tick. An unequipped
>   phone in the bag does not drain.
> - Online through the phone drains at the in-use rate.
> - Charge reaching zero while online ends the terminal access in that tick.
> - A phone with no battery, or a dead one, refuses to go online.
>
> Workbench:
>
> - Remove takes the battery out of the phone into the bag. Apply puts a full one
>   in, and one battery leaves the stack as an instance at 100.
> - Apply out of reach, or at a placement that is not a workbench, or with a
>   non-battery in the component slot, is refused with a reason.
>
> Terminal world, rebuilt:
>
> - Use a fixed terminal in reach: online through it, one player per terminal, one
>   terminal per player. Leave ends it. Disconnect ends it.
> - Use the equipped phone: online through it, no fixed terminal taken.
>
> Persistence, `Tests/Integration`:
>
> - Save then load round-trips the phone, its battery, the slot it fills and the
>   charge.
>
> Definitions:
>
> - The catalogue serialises one definition per message under the datagram cap.
>
> Not tested by code: the pose, the transition, the glow, the fog, the phone frame.
> Seen in the client by the author.
>
> Decision (2026-09-21): Go ahead. That's a fine list of tests.

### Outcome

Agreed 2026-09-21. Names are working names.

Items, in `Core/Simulation/Items`:

- An inventory entry is a stack or an instance. Stacks stay as built. Instances are
  new, next to them. The type definition says which an item starts as. A battery is
  a stack until one gains state, then it is an instance for good. [T1]
- `ItemInstance`: id, type, owner id (a player or a parent instance), the slot it
  fills in its owner or none, its own slots by type, and a list of behaviours.
  Ownership is a tree with one parent per instance. Children are found by owner id.
  [T2, T3]
- `ItemDefinition`: type, name, description, stackable, slot list, behaviour list.
  Held in an `ItemCatalog` that creates instances with their behaviours. Both sides
  read definitions. [T4, T5c]
- Behaviours follow the Component pattern: small classes on the instance, ticked in
  turn, each owning one concern, mutating only their own instance. Three in this
  build: `PowerCellBehavior` on the battery (charge, the persisted state, drains by
  draw), `BatteryPoweredBehavior` on the phone (a battery slot, sets the draw from
  its situation, answers "is it powered"), `TerminalDoorBehavior` on the phone (opens
  the terminal world, carries the door's `TerminalType`). A mains-powered behaviour
  comes later for the rig. [T5, T5b, T5d, T5e]
- Context is pull, every tick. The simulation hands each equipped instance a
  generic context (delta time, in use). The parent turns it into a draw for its
  battery. The battery knows only the draw. Results return up: the phone reports
  dead, the simulation ends the terminal access. [T5a, T5b, T5c]

Simulation, in `WorldSimulation`:

- `EquipmentStore : IPrivateState`: player to slot type to instance. All slot types
  exist for every player from the start. [F3, T11]
- New intents: equip, unequip, workbench remove, workbench apply. Each is closed by
  default: ownership, slot type, reach, and that the target placement functions as
  a workbench. [T8]
- `Tick` gains `TickEquipment`: walk each player's equipped instances, tick with
  context, act on results. [T5c]
- Terminal access is rebuilt from scratch. The device half (carry dictionary,
  `GrantDevice`, `players.device_type`) is removed. "Online" is a per-player state
  with a door: a fixed terminal placement or the phone instance. Fixed terminal
  occupancy stays. The world.md rules hold. [T6, T7]
- Fixed terminals and workbenches come from the compiled zone: a free property
  `interaction=terminal` or `interaction=workbench` on a placement, read by
  `ZoneConventions` into the dressing as interactables with a stable index and a
  position. Random terminal scatter goes. [T12]
- Player creation gives one phone instance with a battery at ten percent, in the
  bag. [F6, F6a]

Messages:

- Convention from here on: success is the next snapshot, failure is
  `ServerMessageRejected` with a reason per intent. Equip is the first user. One-shot
  intents only; movement stays silent. [T8a]
- Private snapshot "what you hold" replaces `ServerInventoryUpdate`: stacks as
  today plus a flat instance list (id, type, owner, slot, state values). One message
  until a bag nears the datagram cap. [T11]
- Private snapshot "terminal world": online or not, the door's `TerminalType`, an app
  list with availability (placeholder until the terminal layout exists). Replaces
  `ServerTerminalAccessUpdate`. [T7]
- `ServerItemDefinition`, one per type, sent after connect. `ClientRequestItemDefinitions`
  for a type the client lacks. Multi-packet waits for the reliability layer. [T4a,
  T4b, T4c]
- Client intents: equip, unequip, workbench remove, workbench apply. Use terminal
  stays and takes the equipped door as a target. [T8]
- The world view says a body is at a terminal and through which kind of door, for
  the pose. [F7, T7]

Persistence:

- A table for instances: id, type, owner id, slot, and a nullable charge column.
  Saved by replace per owner on the existing path: 30-second sweep, disconnect,
  shutdown. The shape for many state attributes is open until a second stateful item
  exists. [T3, T13]
- `players.device_type` is dropped by a roll-forward migration. [T6]

Client:

- Caches: item definitions, what you hold, terminal world. Client-side guards call
  the same `Core` rule the server checks. [T4, T8]
- Inventory panel: bag plus slots section, drag and drop from bag to slot. Item
  description shows equippable and what it does. [F4, F6a, F11]
- HUD: quick-use element for the equipped device with the battery bar. [F11]
- Use sequence: click, send use, wait, snapshot says online, play the transition,
  hold the pose, open the terminal screen. Refusal plays nothing. [T9a]
- Per-body state machine: offline, going online, online, going offline. Transition
  plays on the edge. A body first seen online goes straight to the pose. [T9b]
- One terminal screen with an appearance per door. Phone: a centred phone frame
  with the world screen drawn behind through a fog, no input to the world. All
  others: full window. [T10]
- Clips as data from `animation.md`: a clip file per transition, a pose is a
  one-frame clip. The animator plays a clip over or instead of the walk. [T9]
- Phone model attached to the hand in the pose, with a screen glow as a client
  effect. [F7, F12]
- Workbench screen: item slot, component slot, remove and apply. [F9]

Tests: the list under T14.

Deferred:

- The reliability layer (fragmentation, acks, ordering). Its own feature session,
  triggered by the first real multi-datagram payload. [T4c]
- The state shape for many attributes across item types. [T3]
- Drop of an instance to the ground. Named in F4 and T2, not in the F Outcome.
  Decide at build time whether it is in this build.
- Mains-powered behaviour, drone behaviours. [T5e]
- The app list and the terminal layout per appearance. [F1, T7, T10]
- Client-side wall-bump notice. [T8a]

## Gatekeeping

Each "Already decided" item against the Outcome.

- Access points, not ranks [C-2026-09-18, Q10]: fits. `TerminalType` stays
  unordered, and the door behaviour carries it for the appearance only.
- A phone has only simple tasks [Q10]: fits, as the phone appearance. Apps deferred.
- The device is one equippable item among many lines [Q12]: fits. It is the first.
- A better power supply is a longer battery [Q14]: fits. Capacity by tier is a
  `PowerCellBehavior` property later, and the draw model supports it.
- Phone at ten percent, first purchase a battery, dead phone cannot go online [Q16,
  Q17, first-playable item 1]: fits, with one change on purpose: the battery is
  swapped at a workbench, not used from the bag. Changed by [F9]. The first-playable
  checklist needs the workbench added.
- Nobody starts with a phone, it is the Training Grounds reward [Q12, Q23]: fits.
  The rule is about the Training Grounds, which is not built. Real game start gives
  a starter phone. [F6]
- Rig damage and hacking [Q1, Q2]: open on purpose, not in this build. [F8]
- Push notifications to the in-game phone [Q21]: open, untouched.
- Chat from any device [B, Q26]: open, waits on the app list.
- Loss on fainting [Q2]: open, not in this build. [F8]
- Recycler [Q14]: open, named in F9, not built.
- Crafting [Q12, Q14]: open. The workbench is its first surface. [F9]
- Built, inventory stacks: fits, stacks stay. Instances added next to them. [T1]
- Built, device as `players.device_type`: changed by [T6]. Removed and replaced by
  the equipped phone.
- Built, terminal verbs and labels, one per player each way, no survival across
  disconnect: fits. Kept as rules in the rebuild. [T6, T7]
- Built, plain terminal screen: changed by [T10]. One screen, appearance per door.
- Built, pickup by walking over: fits, untouched.
- Deferred, phone-to-laptop path: open, untouched.
- Deferred, real-phone push: open, untouched.
- Deferred, inventory deltas: fits. The same reasoning chose per-definition messages
  and one snapshot. [T4c, T11]
- Deferred, splitting `WorldSimulation`: fits. `EquipmentStore` and `ItemCatalog`
  are stores like `InventoryStore` and `PartyRoster`, not a sub-simulation. Flag:
  `TickEquipment` is the second time-driven block in `Tick` after movement and
  pickup, and it acts on two entity kinds (players and items). By the agreed
  vocabulary that is close to a sub-simulation. Noted, not acted on.

## Consequences

- `docs/world.md`: add, in the author's words, tagged [C-2026-09-21]: equipment
  slots and their fixed set; instance items with component slots; the battery as a
  component with charge; drain rates and no drain while disconnected; the workbench
  and what functions as one; the phone's appearance versus full-screen doors; the
  pose and the peripheral view; the starter phone on first load. Section 4 (the
  terminal) and section 7 (the rig) are the homes.
- `docs/first-playable.md`: item 1 changes to a battery swap at a workbench, and the
  workbench joins the build list and the art list.
- `docs/backlog.md`: add the reliability layer, charging ports, the recycler, the
  state shape for many attributes, drop of instances.
- `docs/engineering/`: after the build, a doc for items and equipment, and an update
  to `animation.md` when clips as data exist. `identity.md` or `connect-flow.md`
  loses `device_type`.
- Code decisions changed: the device half of terminal access (2026-09-18) is
  removed; random terminal scatter is replaced by placement from data; the
  refusal-by-silence pattern becomes reject-with-reason for one-shot intents.
- Memory notes to correct: the terminal-world decisions note says device is
  persisted as `players.device_type`. No longer true after the build.


## Build plan

Written 2026-09-21 for branch `ger/phone-instance-item-and-frameworks`. One branch,
one PR, one commit per step. `dotnet test` green after every step. Server first, so
the client always has a working server to target. Order is by dependency.

1. **Items in Core.** `Core/Simulation/Items`: `SlotType`, `ItemDefinition`,
   `ItemCatalog` (phone, battery, and the two existing stack types), `ItemInstance`,
   `IItemBehavior`, `ItemTickContext`, `PowerCellBehavior`, `BatteryPoweredBehavior`,
   `TerminalDoorBehavior`. No simulation changes. Tests: the catalog builds a phone
   with one battery slot; a battery drains by draw and not before a tick; a phone
   with no battery or a dead one is not powered; the phone sets idle or in-use draw
   from the context.
2. **Instances in the inventory, equipment, tick.** `Inventory` holds instances next
   to stacks; `EquipmentStore : IPrivateState`; intents `PlayerEquipped`,
   `PlayerUnequipped`; `TickEquipment` in `Tick` with in-use always false for now;
   "what you hold" view (extended `InventoryView`); a battery leaving a stack becomes
   an instance. Tests: equip and unequip, refusals (stack, wrong slot, not owned),
   idle drain only when equipped. Breaks: inventory view tests and the collector bot
   read the old view shape. Flagged before touching.
3. **Messages.** `ServerMessageRejected` gains reasons; `ClientEquipItem`,
   `ClientUnequipItem`; `ServerItemDefinition`, `ClientRequestItemDefinitions`; the
   inventory message serialises instances. Handlers on the server. Tests: round trip
   each message; the largest definition fits the datagram cap.
4. **Persistence.** Migration: `item_instances` (id, type, owner_id, slot, charge);
   migration: drop `players.device_type`. `IItemInstanceStore`, in-memory double,
   `PlayerSaver` and `ConnectHandler` carry instances; player creation adds the
   starter phone with a battery at ten. Integration test: save then load round-trips
   the phone, battery, slot, charge. Breaks: `PlayerRestoreTest` and the connect flow
   read `device_type`. Flagged.
5. **Terminal access rebuilt.** Remove the device half. Online state with a door
   (fixed placement or phone instance); occupancy for fixed terminals; use-terminal
   takes the door as target; terminal-world view replaces `TerminalAccessView`;
   `TickEquipment` gets in-use from it; dead phone ends the access. Tests: the
   existing terminal cases rewritten to the new shape, plus phone use, plus zero
   while online. Breaks: `TerminalTest`, integration `TerminalTests`. Flagged.
6. **Scene interactables and the workbench.** `interaction=` property in
   `ZoneConventions` and the dressing; fixed terminals from data replace the random
   scatter (the starter scene gets the tags); workbench intents remove and apply with
   reach; their two client messages. Tests: remove and apply, refusals with reasons,
   a terminal placed from data is usable.
7. **Client, data and panels.** Caches for definitions, held items, terminal world;
   `ServerMessageRejected` shown as a notice; inventory panel with slots and drag
   and drop; item descriptions; HUD quick-use with the battery bar; the use
   sequence (send, wait for online, then animate). Verified by the scripted driver
   dump and by the author's eyes.
8. **Client, terminal appearance and workbench screen.** One terminal screen with an
   appearance per door; the phone frame with the world screen drawn behind through a
   fog; the workbench screen.
9. **Client, animation.** Clips as data per `animation.md`, written against named
   parts, never against a region of the model: loop or one-shot, layering by part,
   rate drivers next to clips, events at a time. The pocket-to-face transition and
   held pose as the first clip; the per-body edge state machine; the phone model in
   hand as a stand-in; the screen glow effect. No vox scene-graph reader in this MR:
   the proportional cut supplies the six part names until real models carry them
   (decided 2026-09-21).
10. **Docs and close.** `docs/engineering/items-and-equipment.md`; `connect-flow.md`
    and `identity.md` lose `device_type`; `animation.md` updated for clips; session
    file status Built; memory note.

The sub-simulation flag lands at step 2. `TickEquipment` stays a private method of
`WorldSimulation` unless the author says split.

## Build notes

Built 2026-09-21 on `ger/phone-instance-item-and-frameworks`, one commit per step.
Where the build differed from the plan:

- Steps 2 and 4 broke no existing test: instances sit next to stacks and the old
  `AddPlayer` overloads stayed until step 5.
- The `device_type` drop moved from step 4 to step 5, with the code that read it.
- The `EquipmentStore` in the Outcome was not built as a separate store: equipment is
  the `Slot` on an instance in the inventory, which is what T11's one snapshot implied.
- Terminal use, party and workbench refusals all answer with a reason; parties still
  refuse in silence (untouched).
- The starter scene's tags were written into both the scene source and the compiled
  zone, because this machine lacks the model folders to recompile. `compile-art.ps1`
  will produce the same.
- The workbench panel is three columns of labelled buttons rather than two drag
  slots, so the driver can work it; drag can come with the art.
- The phone pose is a clip against the six part names the proportional cut supplies;
  no vox scene-graph reader.
- Sub-simulation flag: `TickEquipment` is a private method of `WorldSimulation`, as
  agreed, and is the second time-driven block after movement and pickup.

Engineering doc: `docs/engineering/items-and-equipment.md`.

Seen in play by the author, 2026-09-21, two fresh clients against the container:

- Works: the phone in the bag, drag to the device slot, Go Online on the phone, the
  centred frame with the world still going behind it, the battery draining, the pose
  and the glow on the other client.
- Not fixed on purpose: labels and slot names overlap in the inventory panel (the
  panel wants a refresh as a whole); the held phone rises well over the head on the
  chibi models (the hand point comes from the proportional cut, and the real models
  bring their own).
- Stacks cannot be dragged. As designed: only instances equip. Moving stacks about
  the bag is not a feature yet.
- The phone reads as a GPU chip in the bag until hovered: every item type draws the
  chip stand-in there. Real icons or per-type stand-ins fix it.
