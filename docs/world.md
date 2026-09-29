# The world

What the game is, in the author's words, organised by topic. If it is not in here, it is
not design. Compiled 2026-09-20 from the sources below, restated cleanly and without
invention. Where the author hedged, the hedge is kept. Where a model proposed something
and the author has not chosen, it is not here; it is in `backlog.md`.

Every paragraph ends with where the author said it:

| Tag | Source |
| --- | --- |
| Q*n* | `archive/decisions.md`, question *n*, answered 2026-09-20 |
| B | the brainstorming of 2026-09-06, as curated in `archive/WorldBuilding.md` |
| T1 | the spoken transcript on data centres and travel, 2026-09-20 |
| T2 | the spoken transcript on servers and global events, 2026-09-20 |
| C-*date* | a conversation on that date, recorded in the model's session notes or in a `features/` session file |

## 1. The premise

Near future. Rogue AI agents took over the electronic world and through it disrupt human
life: power cut to towns, TV stations down, sewer systems off, internet cut, billboards
hacked to blind people. Targeted attacks on infrastructure, not mass destruction. This is
not a post-apocalypse. Society functions. People have jobs and families and ordinary
life goes on. It is closer to a mini war happening globally than to a collapse. [B]

We are not fighting the AI because it is AI. We are fighting an out-of-control AI agent.
Friendly, controlled AI agents exist too, and they work on the player's behalf. [Q16]

The game is not AI-bashing. The rogue agent is the antagonist, but the deeper point is
what is uniquely valuable about being human, in contrast to AI. That is central to the
world and the story, and it shows up in the mechanics as the "uniquely human"
pillar. [B]

People know about it and blow it off. They hear "AI did this" on the news and treat it
as normal. The NPCs in town are oblivious to the drone battles, but not in a completely
ignorant way. They know about things, but not about the fight. Blissful ignorance: they
do not know what is happening right in front of them, the way it is happening right now
in the real world with AI. This is one of the core ideas and lines of the game. An
endgame mission can be: hijack the CNN news feed and broadcast the truth. [Q16]

Governments and militaries fight back and cannot contain it. They lack the combination
of technical skill, creative sensibility and personal motivation that the players bring.
Only unique individuals with unique skills can really beat it. [B, Q16]

The player joined a meet-up group to talk about AI proliferation, and found a
combat-focused training ground for fighting the AIs: a semi-underground society of
adventurers who share and advance skills against the AI while the public is
unaware. [Q16]

The world is grounded in the real one: real geography, real travel times, real
currencies in spirit. [B]

## 2. The Prompt and the campaign

A specific original prompt drove the agents mad. Its content is unknown in the world.
The game seeds hints and clues about it through the story and the play, and the
collective playerbase reverse-engineers it. Solving it may mean hacking the agents back
into coherence: they complete the original prompt correctly, stop malfunctioning, and
peacefully end their session. [B]

The Prompt is the main campaign: cooperative, persistent, and non-repeatable. Once
solved it is over for good. It must last about a year of real time so that many players
take part, which means collective progress has to be slowed on purpose so a small
dedicated group cannot finish it fast. [B]

The story must not end when the Prompt is solved. There is a cliffhanger: the AI escapes
to space, gains new powers, and the game fundamentally changes with new mechanics. The
exact continuation is not chosen. [B, T2]

The author wants singular, world-level events: a year in, one thing happens to everyone
and everything is different after. Not calendar events like a Halloween event, but
narratively significant, non-recurring ones: the AI evolves a new superpower; a player's
discovery (a virus) becomes a permanent attack skill for everyone; the AI destroys a data
centre and gains a new escape mechanism. World state follows the good-versus-evil
conflict ("the AI took over this part of the map and gained this power"), and such
milestones must be reversible if they turn out too powerful. Testing new capabilities on
bosses first is one way to manage that. The author called this one of the hardest,
gotcha-prone problems in the whole concept. [B, T2]

Large events can be gated by player count: if five thousand players are in the terminal
at once, a unique, repeatable, scalable event fires with a powerful reward. The gate is
distinct, geographically distributed players, never resources, so no single wealthy
player can trigger it and a team of ten can do what one person cannot. [B]

World events run in the world on their own. A world event has a type; the first is the
AI Swarm, and its first kind the Drone Swarm: drones appear in a zone (the first, in the
meadows near the door from town, so players do not have to walk for five minutes), in a
set number. Each event is tracked: when it started and ended, its status, and how it
ended: all the drones killed, or its time limit ran out; later, with more mechanics, the
swarm can complete its goal (destroy something, hack a thing). A server restart ends it.
Its settings and its kickoff (a schedule, every so many minutes) are a record the server
reads; an admin panel on the server is for later. [C-2026-09-27]

Why a player goes: it counts towards participation (world event points, for credit
somewhere in the future), and a completed event has special drops, scattered on the
ground near the event (for now GPUs). Taking part is being in the event's area of
effect; for now, being in the zone during the event, to be tightened later. Nothing
announces it in the world: you see the drones, or read about it on the terminal or the
phone; push notifications on the phone and the player UI are for later. [C-2026-09-27]

## 3. Two worlds, one link

The game has two parallel layers: the physical world and the terminal world. A player
has one body in the physical world and enters the terminal world through a
computer. [B, Q7]

The two worlds connect and intertwine, and the first thing a new player should see is
that link: a job in the terminal fixes something in town, and the town changes where
everyone can see it. [Q31]

Under both worlds is **control**. AI agents control things, and where they have control,
life is harder for players. In an AI-controlled town the terminals are hard to use,
buggy, shut off or missing functionality, and the "friendly" things are dangerous:
Teslas, robot delivery drones, escalators that make you fall, the TV in the shop window
that explodes. Where a data centre is nearby, or the town is not AI-controlled, those
things are safe. [Q11]

Under control is infrastructure: a power grid that maps to the real world, and a sewer
and water system to some extent. Both can be hacked and repaired, physically and
online. Hack the control system at the main power station, or a substation, to cut a
town's power or water. The AI attacks some of these only from the terminal side (hacking
the substation). Players fix them in the world (replace wires or terminals at the
substation, repair lines) and on the terminal side ("restore the firewall" to protect
the power station or the sewer plant). [Q11]

## 4. The terminal

A terminal is a modern-day computer out in the world: library terminals, your phone,
your laptop, the gaming rig in your bedroom, the supercomputer at your school. Just
computers. You use one to interface with the game's online world. [Q7]

You enter by walking up to a terminal object in the physical world and using it, which
opens a separate screen with its own art style. The terminal object is the interactive
entry point. [B] Being at a terminal is a state the world shows; the verbs are "Go
Online" and "Go Offline". [C-2026-09-18]

The game builds a somewhat simple, specialised OS for the in-game terminal, and the OS
has all the in-game features as apps: chat, the Defense Objectives, remote monitoring
(CCTV from other towns), market price lookups. That is the general-use terminal. [Q7]
Other apps named since: the town's TODO list of repairs [Q20, Q31], the town log [Q20],
a status board of events happening in the world [Q31], the crypto exchange rate [Q33],
the online meeting room where teams form [Q24, Q25], and the schedule board for
transit [Q30]. A notifications app shows world events, current and past, a row each
("Drone Swarm in Meadows!"). [C-2026-09-27]

Specific terminals look the same but are bound to a place or a target. The data centre
terminal you need to hack is the same concept, but you must perform certain tasks in it:
hack the control panel app, seed a virus. [Q7]

Every safe zone has multiple standard terminals, free, with full use. [Q12]

The player also carries a device. Phone, laptop, gaming rig, supercomputer and data
centre are different access points, not ranks. [C-2026-09-18] A lesser device has less
functionality and less power in the Defense Objectives. A phone has only simple tasks
and features: a few objectives, GPS, chat. A laptop and a gaming rig have more apps and
tools. The laptop is portable; the gaming rig has to live somewhere. A laptop probably
should not contribute to a combined GPU hack; a gaming rig can, and a supercomputer is
better still. [Q10]

The terminal world is the online world. It presents as a custom OS with OS features and
has its own state. Devices (phone, laptop, stationary terminal) are entry doors into it.
Every device except the phone takes you into a full-screen terminal view space. Every
device could have a unique appearance for that space: public terminals have simpler
interfaces for public use, the desktop interface is advanced because it is player-based
and player-specific, data centre terminals are function-specific, and phones offer a
limited interface. [C-2026-09-21]

The phone is the only device that does not take full-screen control, much like when
you look at your phone in real life. It still presents the terminal screen, its look and
feel and function and interface, but not full screen: it is centred on screen like a
phone held in front of your face. In peripheral vision at the sides of the screen you
see the physical world still rendering and interacting behind you, but you cannot
interact with it, because you are online in the terminal world on your phone. It holds
you in place: no movement while you are on the phone. [C-2026-09-21]

The phone opens the terminal world with a simpler layout than a laptop or a gaming rig.
The exact layout and functionality are to be decided with the terminal layout. For now
its only point is a portable and convenient way to open the terminal world. If the
battery reaches zero while you are online, the phone shuts off and you lose your
terminal connection. Losing the connection has consequences, not covered yet.
[C-2026-09-21]

In the real game, on first load you get a starter phone. It goes in the inventory, not
straight into a slot, and the item description says it is equippable and what it does.
The Training Grounds gives no phone. [C-2026-09-21]

Everything behind a terminal is where the player can see the whole game. Locked apps
and objectives show with a locked-out notice, so a player sees FPV drone surveillance as
an option before they can click it, and sees the exchange rate before they have a
wallet. [Q33]

The player never hears "mini-game". In the world these are tasks, Defense Objectives,
agent defense operations. "Mini-game" is a development word. [B]

There is a progressive, non-toggleable simple mode: an abstracted layer that lets a
casual player play objectives and do real-world content without understanding the full
depth, and unlocks more depth as they advance. [B]

The terminal UI starts as a simplified, mocked interface, with the production look
applied later without rebuilding the system underneath. [B]

## 5. Defense Objectives

"Task" is not a game word; a task can be "go get more CPU". Inside the terminal,
Defense Objectives is the general category of fighting the AI, and they are not all one
shape. How they are laid out in the terminal is not decided. Examples: [Q8]

- Operate the CCTV camera to spot enemy drone activity (earns some bank credit).
- Play the electric wire-cutting game (earns some GPU output).
- Search archive data for clues on the Prompt.
- Monitor network activity to spot intrusions on local networks.

Objectives take many forms: part of the main campaign, standalone puzzles, and mainstay
features with their own leaderboards, mechanics and possibly their own art. Leaderboards
matter as a layer of their own: top ten per objective, plus your personal best. Being
the best is not only bragging rights; a regional leader can give a passive buff to
everyone operating in that area. [B, Q13]

**Agent Defense** is the first objective: a rhythm game in the Guitar Hero and piano
tiles manner. Time presses against visual cues across four or five lanes. In the
fiction you are cutting or restoring the electrical grid. Single player, about one
minute. Deterministic after its seed, so a top score can be replayed later. What it pays
is open: GPU was the example, and a GPU may be too valuable for a one-minute game to
pay. [B, Q16]

Some objectives need two players. Ideas: a hack that needs the power of two terminals,
so two players must be in an online team; objectives scaled too hard for one person.
The big version is distributed compute: an intense hack needs something like a hundred
players booted in and sharing GPU power. Coop objectives feel like building a
multiplayer game inside a multiplayer game, which sounds great and hard. [Q9, Q24]

Two more objective concepts: pilot a first-person-view drone around the physical world
for observation, and pilot a first-person-view RC car on the ground for the same. [B]

## 6. The player

At the start a player chooses a name and a look. The look should be advanced, with a
large variety and the full spectrum for head, body and hair colour, because some people
really like this. Clothing and flair too: accessories, sunglasses, hats, vests,
jewellery, backpacks, drone colours. Everything is customisable or skinnable. [Q27]
Display names are not unique and cannot be renamed. [C-2026-09-18]

An account has two characters, and more can be purchased. Some N per account. [Q28]

The player has an HP meter, 100 to 0. Below 50 you move slower. At 0 you faint. HP does
not go up with progression; the author has no reason for it to. [Q2, Q13,
C-2026-09-26]

Players have skills, almost RuneScape-like, that gain XP. "Skill" is the working word;
the author finds it too RuneScape-like, and no better word is chosen. A skill is narrow:
a specific set of predefined actions in the game. Agility is based on your movement,
jumps and travel distance. Hacking is mini-games based on code and terminal use. Others
named: drone control, networking, social, workbench, field repair, electrical repair.
[Q13, C-2026-09-26]

You gain experience in a skill when you do the thing. Agility measures walking
distance, and every N distance earns an XP, as does every N jumps. Repairing an
electric box earns electric skill XP. Skill levels earned in the Training Grounds are
kept; they will be small, but there is no need to wipe them. [C-2026-09-26]

A higher skill gives small rewards within the game, in mini-games, PvP games and
encounters. High-level networking may let you see extra information when looking at a
network map, or give you hints in the network defence mini-game at the best line to
cut. The exact benefits tie to game mechanics that may not exist yet. A skill can
change what the body does: Agility can make you faster or jump higher. [C-2026-09-26]

Skills are private unless shared. Each player has a social page, a page in the online
world, and can enable "Show Skills" on it. [C-2026-09-26]

A career is a specialization. During a career you use many skills, and the skills you
are good at can guide you to a career. The list is not limited: Drone Operator (carry
extra drones, better drone controls, extra drone abilities), Computer Scientist (good at
hacking and terminal activities; extra menus and more information from the terminal
during mini-games and in the world), Mechanical Engineer (really good at repairing;
a built-in repair kit repairs drones and phones without a workbench), Electrical
Engineer, Librarian (more access to information), and uniquely-human careers such as
Truck Driver (extra control of vehicles and their repair: car, train, plane) and Artist.
A career need not have a direct counter in the fight against the AI. Mechanical
Engineer and Computer Scientist come first. [C-2026-09-26]

To start a career you need some minimum level in its related skills: N workbench,
N field repair and N electrical repair before Mechanical Engineer. From then on, the
contributing skills advance the career, along with use of the career's own abilities:
each repair with the engineer's repair kit earns career XP. [C-2026-09-26]

You start a career at a college. The first time you pick one, you enrol in a Class,
a tutorial on what the career selection means. After that you go to the college and
talk to the registrar to change careers. The switch is permanent and you lose all
progress in your current career; this is where more characters per account are useful.
Colleges can be anywhere, probably each its own zone, several spread through the world.
For now it is a building in the town. [C-2026-09-26]

A career has levels: Apprentice, Graduate, Senior, Master, Elite. Each level gives you
something new. Progress comes from the accumulation of supporting skills and use of
the career's abilities; the math is not set. To level up you go to your original
college and meet your old professor. [C-2026-09-26]

A player can play the whole game without a career. Career and career progress are
visible to everyone. A team that mixes careers can complete things faster, but the game
treats it no differently: the speed comes from what each member brings.
[C-2026-09-26]

There is also a player level: an accumulation of overall time, skill accumulation,
career progression and mission completion. The details are not set, and neither is
what the level does; maybe you can carry more. [Q13, C-2026-09-26]

Reputation is real and carries impact in the world. "Player zooer66 is in town, so
everyone gets +10% hacking" is real. [Q13]

Skills and reputation do not go away. Maybe some atrophy: you have not been to New York
for a month of real time, so your impact there disappears. But a legitimate top-ten
high score may keep the fame and the boost. [Q15]

Hunger and sleep are on the fence. They would fit the uniquely-human character of the
play. [Q19] Long walks need sleep in the simulation. [B]

## 7. The rig and equipment

Part of the game is "your rig": phone, laptop, batteries, exoskeleton, personal drones,
monitoring rig. The AI can damage these electronically and hack them by proximity to the
character. [Q1]

The device is one equippable item among dozens of equipment lines that each level up:
defence drones and their upgrades, the EMP gun and its upgrades, the virus catalogue for
hacking, the GPU tier for terminal power and usage. The device is not a significant
ladder to climb. [Q12]

Drones are the heroes of Guild Wars: companions with specialties. Roles with real
trade-offs: pure defence, pure attack or anti-drone, mixed support, so composition is a
choice. Also observation drones that give line of sight or better GPS. [B, Q16]
Autonomous protection drones defend your body while you are online, a mid-game feature
low-level players will not have. [Q5]

Tools against AI threats: EMP, safety drones, a targeted radio scrambler weapon. [Q5]

Hacking against the rig has levels. Certain features can be disabled (map, chat,
defensive systems), and there is complete disablement in degrees: a temporary scramble,
longer-term damage needing repair, or a complete brick. Which one depends on the
attacker's damage output and the player's defences. [Q2]

Some items slot in to upgrade an item. A better power supply is a longer battery on a
phone or laptop. A better GPU hacks a network game faster. [Q14]

An equipment slot is a usable item: a visual indicator on the character or inventory
screen that tells the player "you have this item and can use it to do things". There is
a limited number of equippable slots, probably with slot types (device, drone, weapon).
The slots do not need to be restricted: the player sees the full slot view, and the
slots are empty until the character acquires the items that fill them. There is no
reason to hide them other than to simplify early-game mechanics. A new player fills one
slot, their phone. [C-2026-09-21]

Equippable items can land in the plain inventory or in other storage (a home storage
chest, a bank, dropped on the ground). A player drags an item from the inventory into
the slot, and then it is equipped and usable. Auto-equip can come later; for now it is
mechanical, inventory to equipped. The slots live primarily in the inventory screen:
you see your inventory bag and then your equippable slots. That is not how you use
your phone, though: a HUD item lets you say "pull out my phone and use it", and then the
animation plays, hand in pocket, phone to face. [C-2026-09-21]

A phone is an individual item instance with equippable slots of its own. The regular
phone has exactly one, for a battery. No inventory bag, just the slot. The phone's state
is composed of its components' states: a phone without a battery has no charge. This is
bespoke logic for the phone but it applies to other things too: a drone has batteries and
components (spy camera, EMP pulse generator), so the mechanic is reusable, an item with
components and equippable slots. A dropped phone takes its battery with it; an equipped
thing is owned by one thing only. A battery is stackable, a pack of a hundred, but once
it is not at a hundred percent it becomes unique: you cannot mix full batteries with
half-used ones. [C-2026-09-21]

The phone battery drains from 100 to 0. It has an indicator in the terminal UI and in
the equipment slot, a simple level bar with red, yellow and green. It drains from idle
and from use. Idle it lasts three full days from 100 to 0, real time, like a phone in a
pocket. Using it to access the terminal drains it faster, something like three hours of
constant use to zero, similar to real life but more forgiving; the formula is not set.
Disconnecting from the game grants downtime: it would be unreasonable to open the game
and always find your phone dead, so disbelief is suspended for practicality. At zero it
is dead: the battery needs replacing, or charging. Charging ports are a realistic
feature for later; day one, you find a new battery. [C-2026-09-21]

Batteries are swapped at a workbench, where items are repaired and built. Workbenches
are easily accessible, one or two per town in public, and they should not look the same:
a picnic table in the park can be a workbench, or a reading corner in the library, a
dedicated shop in town, a desk in the player's room. The workbench is simple for now:
an item slot for what is being repaired or built, a component slot for what you are
adding, and an apply button. You remove the dead battery into your inventory, add a full
one, found on the ground or bought in a shop, and the phone is recharged. The dead
battery stays in the inventory or goes to a recycle bin or recycle machine in town.
[C-2026-09-21]

In use, other players see the character with the phone in hand, the standard phone held
up to the face, head down, staring at it. The screen glows and changes colour, like TV
shadows on a wall, so you know they are doing something. The flicker is arbitrary and
does not tie to activity; it only shows the player is "on their phone". Equipped and
not in use, the phone is in the pocket and nothing is visible. [C-2026-09-21]

Crafting is neat and not thoroughly thought out. The game is technology-focused, so
there is a lot of tech that needs repairing and building: build your phone, laptop and
gaming rig; repair and upgrade drones; repair your EMP defences. It may have a bigger
place than the author first thought. You need wire, cooling parts, CPU, RAM and GPUs to
build a laptop, and more of better quality for a gaming rig. [Q12, Q14]

Friendly, controlled AI agents are an endgame item. They fight in the terminal world and
protect you in the real world: "Back on my gaming rig I have three Defender Agents
watching my moves; they will protect me as I run through the unfriendly zone." An
advanced AI scout agent detects port scans and network probes that mean an attack is
imminent. [Q16, Q30] A player's own AI assistant scales with progression and gives
counter-protection when a new zone's rogue AI tries to infiltrate nearby devices. [B]

Batteries are consumable. CPU and GPU wear with terminal use; higher tiers last
longer. [Q17]

## 8. Danger

There is no player death. Fainting, as in older RPGs, sends you "magically" back to the
nearest town, carried by helpful strangers. [Q1] Whether "nearest town" includes the
small towns in the wild, and whether a camp counts, is open. [Q4]

There is injury. Drones and AI-controlled things do harm: hacked billboards blind you,
an AI-driven Tesla rams you, a drone attacks with anything (drops a dollhouse on your
head, spills water on you). The specific damage dealer does not matter yet. [Q1] The AI
infiltrates and weaponises nearby technology: a drone, an RC car, a humanoid robot, a
connected vehicle made to explode. This is environmental danger, not a battle system.
No turn-based combat. [B]

Drones in a zone are of two kinds. A spy drone: every so many minutes a drone appears
in a zone that does spying, and follows a path through it, looping "forever,
observing", at most two in a zone for now. It does nothing to players for now. Players
can spot it via the cameras and use weapons on it; they can kill it if they get close
enough. A roaming drone attacks players, as drones do today: it hovers in a circle and
every so often picks a maneuver at random ("Dive bomb the ground to about the player
height", "Zig Zag approach towards a player height", "Climb up and Back Down"), aimed at
a random player near it. The two look the same; players "spot behaviors and be able to
guess, this one flying high away will probably not attack me". The paths and maneuvers
are flights recorded by hand. [C-2026-09-29]

At 0 HP you faint with a real consequence: whatever hurt you can damage your inventory
or equipment, or things get stolen. Loss is scaled by tier, not random, and never total.
Common items are lost or destroyed often, middle tier sometimes, high tier occasionally,
and some items may be protected outright (maybe). Needs research. Time lost is a real
cost too: waking in town after fainting far away costs the walk. [Q2, Q3]

Online, your body is vulnerable and cannot run unless you unplug. Unplugging is one
click; the cost is what you leave behind in the terminal: mid-hack you probably lose the
hack and get counter-hacked, or lose the GPU you had in there. Party members do not need
to unplug you. They fight the drones for you with their tools. [Q5]

Solo is not walled off. Mid-game objectives and campaign quests are much easier with a
party. A data centre raid: hack the central system from the control room, fight the
security drones, cut the power, then reboot the whole building to secure it. Much easier
with two or three; a solo player can do it but it will not be easy, and they must supply
all the loot and resupply themselves. Nothing is gated on party size. [Q6]

## 9. Economy

Dollars are the currency of the early game and the Training Grounds. Crypto is an
advanced item, unlocked later; an early introduction would be too complex. GPU, RAM
stick and CPU cores are a currency in themselves, and consumable, and an item: a
physical currency. A GPU is expensive; in real life a high-tier one costs tens of
thousands of dollars. [Q14, Q16, Q18]

Everything is currency and can be traded any way; the chain runs both directions. Some
vendors take only GPU, some only cash, some only crypto. Some online purchases take only
crypto, because the vendor is sketchy or the item is black market. [Q16, Q18]

Nearly everything is sellable: go to a recycler and recycle it for some currency. [Q14]

Money leaves the world when things break: loot lost on fainting, repairs, CPU and GPU
wear, batteries, travel fares (robo taxi, train, airplane), postage. [Q17, Q19]

A new player's first purchases are equipment for themselves. The phone is at ten percent
when they arrive, so first a battery. Then a backpack to put things in, or a mini
observation drone. [Q16, Q17]

The player starts with $10. An electronics shop in the first town sells the battery
for $8: the inside of a shop, big enough to walk around a few aisles of cosmetic
scenery, posters on the wall that foreshadow drones. To buy, the player does what a
player does in every MMO: click on the shopkeeper and see a list of items to purchase,
this one with only the battery for now and maybe a few canonical items out of price
range. The server controls shops, their items and prices, to control purchasing power
and economics later. For now money is a label in the inventory, "Pocket Change: $10";
long term, player bank accounts that are transactional, with history, balances, loans.
[C-2026-09-21]

Wealth shows in the inventory in its own section. After an objective, the reward screen
lets the player choose which currency or item type to receive. The currency system
grows over time and must not preclude a market, trading and investing layer. [B]

Players give each other things: hand over, drop on the ground, physical mail that costs
postage and delivery time, and stashes. Stashes are buyable: pay one GPU before you head
out and get at least three stash markers. [Q19] A marketplace where remotely bought items
ship physically in the background, at a cost, is a later feature. [B]

Player-owned stores in physical locations, with their own prices and items, are a
primary idea to explore. [Q19]

The author loves one-of-a-kind, globally unique items: unique hardware, unique
clothing. [B]

## 10. The map

The map follows real-world geography, the continental United States as the example.
Travel is walking, driving, bus, train and flight, and each takes about its real time: a
flight as long as a real flight, Boston to New York by train about ninety minutes,
Chicago to Philadelphia on foot about fifty-seven days. [B, T1]

Real-time travel is a feature, not a cost to hide. "I'm jumping on the train to Chicago,
it takes eight hours, I can close the game or go AFK and come back." The character
arrives whether or not the client stayed open. Long distance must mean something and
instant travel always feels weird, so eight hours of travel comes with a reward, such as
landing in a hard-to-reach area with better rewards. Airplanes are the fast travel, and
they are mid-game, not endgame. [Q0, Q29]

Trains and planes run on a schedule. Miss one and you wait for the next. Players see the
schedule in advance on a board. [Q30] Train and plane travel is a room the travellers
live in for the duration. [T1]

Places come in rings out from a town. The town (Old Town) is hand-built and is the hub.
Around it is a known wilderness, the RuneScape kind: fixed layout, points of interest,
threats, campaign quests; it can be several zones and large. Beyond that is generated
wilderness that differs each time, because nobody will hand-place trees for a map the
size of the United States. Both need a reason to go. The game must not be a grinder,
but grinding is allowed, and the generated wild is where a grinder goes: hundreds of GPU
units from the rogue drones roaming the forest. The generated ring needs a name;
"unmapped area" is a bad one. [T1]

Small towns are sprinkled through the generated wild: a generic small-town template,
randomised so it is never quite the same. Pit stops where you refill resources and save.
Out there you have no cell service, so no map and no GPS. You know about the town
because someone handed you a paper map. [T1]

Coordinates stay real in the generated wild: heading west from Philadelphia, your west
coordinate always increases until you near Chicago. The map is split into parts with
coordinates within coordinates. [T1]

Driving follows the highway system, which is generated the same way. [T1]

Entering a building loads a separate, enlarged interior; the inside does not have to fit
the outdoor footprint. No cutaways. [C-2026-09-18]

The first piece of the first ring is the outskirts of the first town: a wild area that
can be explored and introduces zones, a brief wooded area with a fountain and some
scenery, kept small on purpose, safe, no dangers yet. In one location an old hardware
chest with basic loot: a random spawnable item every N minutes, so there is a chance
there is nothing in it. The whole purpose of it is to introduce the zone change
mechanic. [C-2026-09-21]

A zone change is a road or dirt path that leads to the edge. The exit is shown by the
road continuing and a threshold effect across it, nothing written. Walk in, and the
client fades to the new scene's entrance spawn. Zones have multiple spawn points; the
most common is the zone transition spawn point. Every transition has a name and says
which transition in which scene it connects to. [C-2026-09-21]

If they have a party, the party travels between scenes together and arrives as a group
at the entrance. At some point world scenes and town scenes differ: world scenes cause
party travel, town scenes do not. [C-2026-09-21]

The world is discoverable. Leaving the Training Grounds you land in, say, Boston, and the
map shows only Boston. You explore to find the rest. There are no blockers or hard
lock-outs; the author sees no reason for them. Once in the real world it is completely
unlocked, limited only by your time and your in-game currency. [Q23]

A high-level player can run a low-level one to an advanced part of the game, and the
low-level player pays for it, as in Guild Wars. When one party member changes zones the
whole party goes together, which is what makes running possible. A Runner's protection
scales with their progression, so their low-level passengers pass largely unmolested
while unprotected players face the raw threats. The author sees Runner as a natural
emergent profession. [B, Q23]

Each server is somewhere, with its own clock and time zone. Lighting and NPC routines
follow it. [C-2026-09-18]

## 11. Data centres

A data centre is the top of the equipment and the endgame weapon against the AI, and a
group builds it, in the EVE manner of a player-made mega-structure. Only a powerful group
can afford one. It costs consumables to build, and real time, about a week. [T1]

Where you build it matters. The AI has coverage: dense in towns, thin in the wild, none in
the desert. Build in coverage and the AI sees it going up and acts against it: a tax that
makes it cost more, a slowdown, or complete withdrawal. Build in a remote place with no
coverage and it is safe from the AI. [T1]

For its owners a data centre is a safe zone in the wild, a repair centre, a currency boost
(mine crypto, quickly repair things, GPU packs), a source of significant compute against
the AI agents, and a major influence in the fight for control, in both worlds. [Q11]

Data centres decay and need substantial upkeep, so much that materials must be shipped
in by airdrop, rail, or significant drone or player deliveries. [Q21]

A data centre can be raided. When one falls, the AI gains something from it. [B]

## 12. What players change

Towns have a set of standard tasks that need doing. Repair disrupted street lights,
noticeable only at night; when fixed, the lights come on and everyone knows, because the
lights appear. Robo taxis have rootkits installed and need cleaning before they can be
used, with an indicator on the taxi once it is fixed. [Q20]

The town or zone keeps a log, read from the terminal: "KoolGuy78 repaired street light
grid aaa-001, 2026-09-21", "substation repair by party (name)". [Q20]

The AI can randomly break through firewalls and kill some of the electric grid: harmless
in the grand scheme, a random repair cycle that has a place. Players can also defend
attacks. Push notifications reach the in-game phone (or a real phone, which would be
neat): "Agent Defense Required: AI infiltrated local firewall, log in to defend". [Q21]

Player-made content is permanent and cannot be griefed. Each town has a subway you can
go into and spray your name on the wall, an underground visitor book with essentially
infinite room. [Q20, Q21] Secret messages passed off-grid between players, so the AI does
not know, is an idea for later; it does not feel early-game. [Q20]

Player-submitted content is a pipeline: short videos, about thirty seconds, moderated,
shown on in-game TVs as ads or mini-shows and attributed to the player; music and audio;
artwork displayed as one-of-a-kind items. Letting players build whole quests and points
of interest is an older idea, deferred. [B]

Quests are needed. Not grinders; they have meaning and purpose and are something to
do. [Q12] A small win along the way, like resolving a local disruption, brings an
immediate upgrade and local reputation. [B]

## 13. Together

Parties are temporary groups who do things together: a data centre raid, a substation
repair, a coop hack. Both worlds need them. Physical-world parties form by being in the
same area and joining: a HUD indicator shows membership and teammates, invite by
clicking another player, accept or deny, leave back to solo. A party holds until someone
leaves; distance and disconnects do not break it. Terminal parties form by joining the
online meeting room. [B, Q25, C-2026-09-18]

Long-term groups exist. The author does not like "guild" or "clan", but that is what it
is, and the word is not chosen. A long-term group has a safe house in a town, purchased
or won, that only members can enter. In the terminal it has its own room, the Discord
idea under a name still to find, where the group meets: chat, form teams for terminal
missions, share online things. [Q25]

Players have a customisable room in the safe house. They buy things at a store to build
it out. A player can also rent a room in a town outside the safe house. The author likes
this feature and thinks it is pretty easy to add. [C-2026-09-20]

A room is also where a gaming rig lives. It is not portable like a phone or a laptop,
so it needs a safe place. The group's safe house works, but a lot of people do not join
a clan, so a player needs somewhere of their own. The same goes up a step: you cannot
run a supercomputer out of your apartment. You purchase or rent an industrial unit and
set your supercomputer cluster there. [C-2026-09-20]

Finding people: local or regional chat, reachable from any device. In Old Town, open Chat
on your phone or the library terminal and see "Old Town Chat". Spam "LFG: Substation
repair". Global chat exists as well. [B, Q26]

PvP: the author wants a wide range of styles, from non-damaging competitive objectives
with risk and reward to real-world physical PvP, never turn-based. Designs on record: a
Domination mode with three or four points, each worth a point per second, five seconds
to flip, where each player also commands two or three drones; and a Hall of Heroes
arena where teams battle for dominance and the best team earns a significant reward.
The author is very unsure PvP has a place in the game yet. If it does, damaging other
players may cost you favour in areas that favour them. [B, Q15, Q23]

## 14. Time

The world runs on a real clock in the server's time zone. Day and night follow it. [C-2026-09-18]

Nothing attacks on a fixed schedule. "The AI attacks at 8pm every day" has no reason or
mechanic behind it. Warnings come from intel: a scout agent that sees the probes. The
only appointments are transit departures. [Q30]

Logging off has three modes, from Rust with changes: [C-2026-09-18]

- **Unsheltered.** The character stays in the world, idle. It can be attacked, and others
  can interact with it in the ways that do not need the owner.
- **Sheltered.** Set up camp, go to bed, go to a safe place. The character leaves the
  world. It costs a tent or a camper, or a place where sheltering is allowed (a hotel, a
  home), but it is almost free and it is the default. "We want to let people log off and
  not worry about their character."
- **Autonomous.** Logged off, unsheltered, with behaviours set: drones auto-defend, the
  house auto-repairs, a long hack keeps running. It costs a lot in batteries. A hack that
  takes days needs batteries for forty-eight real hours, and even a short break costs
  something like fifty percent more. The rare case, because of the expense.

The character keeps travelling while logged out. [Q29]

## 15. Servers and the whole world

There are servers in different parts of the world for latency: East Coast, West Coast,
Asia, South America, Europe. One canonical game world overlaps all of them. A website
sits beside the game: leaderboards for the puzzles and objectives, announcements,
events, lore. Maybe not at first. [T2]

The hard problem, unsolved: regions progress at different speeds. If Europe progresses
far enough to trigger a worldwide event, that is unfair to Asia. If regions run at their
own pace, Asia sees Europe's future and is spoiled, which the author does not want as a
feature. Two people in Asia cannot retake a town that a hundred in Europe can. A shared
pool across servers solves the arithmetic and feels weird when you are the only one on
your server; it could be made transparent, "you are one of five hundred". [T2]

One region at first. [C-2026-09-18]

## 16. The Training Grounds

The Training Grounds is the game's tutorial island, in the manner of the original Guild
Wars starting zone, which the author loved. It is isolated: on the very first load you
start there and see no other part of the world. You learn the map and GPS but cannot see
anything yet. You cannot return. You leave through a mission you lose. [Q16, Q23]

It forces you through nearly every mechanic: quests, battle, crafting, trading, selling,
making a team, drones as heroes. It teaches a stripped-down terminal, so the online OS is
not new when you reach the real game. [Q12, Q16]

Its loop is exaggerated, comfortable and easy, so you learn the base game and are
motivated. You collect parts from broken drones to build a GPU. That GPU goes into a
broken terminal for your first access to the online world, and it activates the
terminal long enough to do the demo task, then breaks. A GPU repairs a whole terminal
here; that will not be the case in the main world. You repair the electric grid to fix
the lights. You trade a GPU for a defence drone. You fight a boss drone that needs two
players with attack drones. [Q14, Q17, Q20, Q24]

To get out you fight an impossible boss, faint, lose everything, and wake in a town
where you start again. The phone is your reward for leaving. The exit must be curated so
it is not a shock. You can skip the Training Grounds by starting the exit mission right
away. [Q12, Q14, Q16, Q23]

The Training Grounds is built last: the classical "don't write your intro until you've
finished the book". [Q31]

## 17. How it looks

Voxel art. Eight voxels to the metre, one density for the whole game, and packs the
author can buy and modify (so CC BY, not BY-ND). [C-2026-09-18] The physical world is
warm and physical; the terminal is cold. [C-2026-09-15] The terminal OS looks somewhat
like a modern OS with a pixel vibe, and it must feel like an extension of the physical
world. [C-2026-09-15] Five presentation contexts: Game World, Terminal World, Minigames,
HUD, Game Menus. [C-2026-09-15]

Art is the biggest gap and probably the hardest thing. Metro Minis is a placeholder,
comically too poor for the quality the author aims at; the reference is the density and
finish of Max Parata's Voxel Megabuilding renders. The real art will be purchased, or
kitbashed from free open-licence art, and the search for it has not started in earnest;
that search is what holds it back. The first town is put together by the author in the
voxel scene editor and MagicaVoxel. [C-2026-09-20]

Effects on top of the voxels are a large part of the look: the author wants a lot of
them, to give the game a graphics style of its own. People do this a lot with voxels
anyway. [C-2026-09-20]

The terminal OS look is not settled. The mock-ups in the design canvases have good
layouts and concepts, but the author is not in love with the look (colour, font), and
it needs much more iteration. [C-2026-09-20]

Achievements exist in-game and on Steam: explore all of the map, play every
objective. [Q23]

## 18. Not in the game

- No player death. [Q1]
- No combat system and no turn-based combat. [B]
- No griefing of player-made content. [Q21]
- No forest full of PC parts. Today's ground pickups are a test mechanic. [Q12]
- No hard lock-outs on the map. [Q23]
- No attacks on a daily schedule. [Q30]
- No cutaway interiors. [C-2026-09-18]
- No "mini-game" in the player's face. [B]
- No fighting the AI because it is AI. [Q16]
