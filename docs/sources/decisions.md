# Decisions

**Archived 2026-09-20.** Folded into `../world.md`, which is now the canonical file. This
is the record of how the answers were reached. It is not updated.

The load-bearing questions: the ones other design depends on and that keep getting
deferred because everything else is more fun to think about. Each has one line on what
it blocks. Started 2026-09-20.

**The answers are the author's own words**, typed in conversation and recorded verbatim
or near it, one question at a time. They are not generated. Where a model added
anything it is set apart and labelled: "Consequence noted" is the model's reading of
what an answer implies, "Options offered" are the model's suggestions when the author
asked for ideas, and a "Note" corrects the record. A question left open on purpose says
so and why.

**Where we are:** All 34 questions answered, Q0 through Q33, on 2026-09-20. Open parts are
marked inside the answers: the phone-to-laptop path (Q12), what a mini-game pays (Q16),
whether a stash or store can be robbed while the owner is away (Q22), and the names for
the long-term group and its online room (Q25).

## 0. The north star

**Q0.** In one sentence, what is this game for the player? The test every idea is held
against. Strawman to reject or rewrite: *ordinary people, using the computers they can
get their hands on, working together to hold the real world against a rogue AI, in a
world that remembers what they did.*

> Answer (2026-09-20): The game lets a player explore ideas in a comfortable
> environment (a classic MMO) and explore current real-world scenarios of AI control
> and chaos in the changing world. The game also offers unique MMO experiences and
> gameplay mechanics (in-world player-generated content, real-world travel time).
>
> Read as a test: be classic wherever an idea is not about the AI, player-made content
> or travel, and spend the novelty there.
>
> Follow-up, comfort versus novelty: comfort loses, and each time it comes up it is a
> decision on the topic at hand, not a blanket rule. On travel specifically: fast travel
> exists as airplane travel, mid-game rather than endgame. Real-time travel is a
> feature: "I'm jumping on the train to Chicago, it takes eight hours, I can close the
> game or go AFK and come back." Long distance must mean something; instant travel
> always feels weird, so eight hours of travel must come with a reward, such as landing
> in a hard-to-reach area with better rewards.

## 1. Danger

Blocks: the wilderness, threats, the Runner, data centre defence, death anchors, what a
solo player can and cannot do, and half of `mmo-expectations.md`.

**Q1.** What can hurt a player in the physical world? Name the first thing. A drone
that does what to you?

> Answer (2026-09-20): No player death. Fainting, as in older RPGs: fainting sends
> you "magically" back to the nearest town, carried by helpful strangers. There is
> injury: drones and AI-controlled things can do harm. Hacked billboards blind you, an
> AI-driven Tesla rams you, a drone attacks with anything (drops a dollhouse on your
> head, spills water on you); the specific kind of damage dealer does not matter yet.
> A drone can also cause item and equipment damage. Part of the game is "your rig":
> phone, laptop, batteries, exoskeleton, personal drones, monitoring rig. The AI can
> damage those electronically and hack them by proximity to the character.

**Q2.** What is depleted when you are hurt? No health bar by design, so what is the
bar? Your device's battery, its integrity, your own stamina, your standing, nothing?

> Answer (2026-09-20): For the player, for now, an HP meter, 100 to 0. At 0 you faint
> with a real consequence: whatever hurt you can damage your inventory or equipment,
> or things get stolen; some loot-loss system. Damage also manifests as disablement,
> for example moving slower below 50 HP. This is physical damage to the character.
>
> For the rig: hacking has levels. Certain features can be disabled (map, chat,
> defensive systems), and there is complete disablement in degrees: a temporary
> scramble, longer-term damage needing repair, or a complete brick. Which depends on
> the attacker's damage output and the player's defences.
>
> Consequence noted: "damage output against defences" is a system, and a rig's
> defences are something a player owns and improves.

**Q3.** What does losing cost? Time, what you carry, currency, progress on the ladder,
reputation, the thing you were doing?

> Answer (2026-09-20): Loss is scaled, not random, and never total. Common items are
> lost or destroyed often, middle tier sometimes, high tier occasionally, and some
> items may be protected outright (maybe). Needs research; the rule is scaled by tier,
> not full loss. Time lost is a real consequence too: waking in the nearest town after
> fainting far away is meant to cost the walk.

**Q4.** How do you come back? Wake at the last town, at a camp, where you fell, after a
delay, after a task?

> Answered under Q1: you faint and wake in the nearest town, carried by strangers.
> Open: whether "nearest town" includes the small towns in the wild, and whether a
> camp or vehicle counts (the respawn anchors from `data-centers-and-travel.md`).

**Q5.** While you are jacked in, your body is idle and undefended (design). What can
happen to it, and what does a partner watching it actually do to stop that?

> Answer (2026-09-20): The body can be defended by your own autonomous protection
> drones, a mid-game feature low-level players will not have. Jacked in, you are
> vulnerable to attack and cannot run unless you unplug. Unplugging is one click; the
> cost is what you leave behind in the terminal world: mid-hack you probably lose the
> hack and get counter-hacked, or lose the GPU you had in the terminal, or similar.
> Party members do not need to unplug you. They fight the drones for you with their
> tools: EMP, safety drones, a targeted radio scrambler weapon.
>
> Consequence noted: there are tools for fighting AI entities (EMP, scrambler, safety
> drones). Not a combat system in the classic sense, but player action against
> threats exists. And a hack in progress has a stake (a GPU) that can be lost.

**Q6.** Can a solo player be killed, or only set back? "Solo is extremely hard" means
what, concretely: slower, riskier, or walled off?

> Answer (2026-09-20): Not walled off. Mid-game mini-games and campaign quests are
> much easier with a party. Example, a data centre raid: hack the central system from
> the control room, fight the security drones, cut the power supply, then reboot the
> whole building to secure it. Much easier with two or three; a solo player can do it
> but it will not be easy. A solo player also has to provide all the loot (GPU, RAM
> sticks) and resupply themselves.
>
> Consequence noted: "hard" means several simultaneous jobs done by one person in
> sequence, and no one to share supply. Nothing is gated on party size.

## 2. The terminal world

Blocks: every task, coop tasks, the PvP mini-games, what a device tier changes, the
whole endgame.

**Q7.** When you are jacked in, what are you doing in one sentence? Running tasks
from a menu, or moving through something?

> Answer (2026-09-20): The terminal is two things. First, a modern-day computer out in
> the world: library terminals, your phone, your laptop, the gaming rig in your
> bedroom, the supercomputer at your school. Just computers; you jack in to interface
> with the game's online world. We build a somewhat simple, specialised OS for the
> in-game terminal, and the OS has all the in-game features: chat, mini-games (as
> "Defense Objectives"), remote monitoring (CCTV from other towns), market price
> lookups. That is the general-use terminal.
>
> Second, specific terminals: they look the same but are bound to a place or target.
> The data centre terminal you need to hack is the same concept, but you must perform
> certain tasks in it: hack the control panel app, seed a virus.
>
> Consequence noted: the terminal is an OS with apps, not a space you move through.
> The client's terminal screen (a menu of apps) is already this shape. A specific
> terminal is a placement bound to a target, with its own apps.

**Q8.** What is a task, generically? Agent Defense is a one-minute rhythm game. Is
every task a self-contained scored mini-game, or do some persist, chain, or run
between sessions?

> Answer (2026-09-20): "Task" is not a game word; a task can be "go get more CPU".
> Inside the terminal, "Defense Objectives" is the general category of fighting the
> AI, and they are not all one shape. How they are laid out in the terminal is not
> decided. Examples:
>
> - Operate the CCTV camera to spot enemy drone activity (earns some bank credit).
> - Play the electric wire-cutting mini-game (earns some GPU output).
> - Search archive data for clues on the Prompt.
> - Monitor network activity to spot intrusions on local networks.
>
> Consequence noted: four shapes already: a scored round, a watch-and-spot job, a
> search that feeds the campaign, a vigilance task. Payouts differ by objective (bank
> credit versus GPU output), which touches Q18.

**Q9.** What does a task need two people for? The first coop task: what do the two
players each do that one cannot?

> Answer (2026-09-20): Open. Some mini-games can be scaled to be too difficult for one
> person, so coop terminal work would be needed. No concrete idea yet; it feels like
> building a multiplayer game inside a multiplayer game, which sounds great and hard.
> Ideas wanted.
>
> Options offered, none chosen. All keep each player playing alone with the server
> combining the results, so nothing is shared in real time:
>
> - Spotter and cutter: Agent Defense with the chart hidden from the cutter; the
>   spotter reads the CCTV and calls the next wire. The existing game minus one view.
> - Two-key hack: two puzzles on two devices whose answers must land within the same
>   second; the server checks the timestamps.
> - Load sharing: a target needing more GPU than one device; two players run tasks in
>   parallel and the progress is the sum. The design's hardware pooling at the smallest
>   scale.
>
> Author's addendum: the distributed compute idea is his and is the big version of
> load sharing: an intense hack needs something like a hundred players booted in and
> sharing GPU power.

**Q10.** What does a better device change inside the terminal? More tasks, harder
tasks, different kinds of task (the design's terminal scripting versus node routing),
more at once?

> Answer (2026-09-20): Less functionality and less power in the mini-games on a lesser
> device. A phone has only simple tasks and features: a few mini-games, GPS, chat. A
> laptop and a gaming rig have more apps and tools; the laptop is portable, the gaming
> rig has to live somewhere. A laptop probably should not contribute to a GPU
> combination hack; a gaming rig can, and a supercomputer is better still.
>
> Note: the "terminal scripting on cheap devices versus node routing on big ones"
> split in `Rogue-AI-MMO-Game-Ideas.md` is a model's extension, not the author's, and
> is not design.

**Q11.** What does a data centre do, minute to minute, for the group that owns it?
Not "reach bigger targets": what is on the screen.

> Answer (2026-09-20): A data centre is a safe zone for players out in the wild, a
> repair centre, a currency boost (mine crypto, quickly repair things, GPU packs), a
> source of significant compute against the AI agents, and a major influence in the
> fight for control, in the real world and in the terminal world.
>
> The concept underneath, which needs understanding: **control**. AI agents control
> things, and where they have power or control, things are harder for players. In an
> AI-controlled town the terminals are hard to use, buggy, shut off, missing
> functionality, and the "friendly" things are dangerous: Teslas, robot delivery
> drones, escalators that make you fall, the TV in the shop window that explodes. All
> from the AI's influence in the area. Where a data centre is nearby, or the town is
> not AI-controlled, those things are safe and do not happen.
>
> Under all of it is a **power grid** that maps to the real world, and a sewer and
> water system to some extent. Both can be hacked and repaired physically and online:
> hack the control system at the main power station, hack a substation to cut a town's
> power or its water. The AI attacks some of these only in the terminal world (hacking
> the substation); players can fix them in the world (replace wires or terminals at
> the substation, repair lines) and can also "restore the firewall" to protect the
> terminal side of the power station or sewer plant.
>
> Consequences noted: control is a state over the map that both worlds read, and it is
> what the first loop's dark street is the smallest case of (the AI has the substation).
> Infrastructure is a layer of its own with two sides, physical and terminal, and the
> AI and the players each have moves on each side. A data centre is a source of
> control, not only a weapon.

## 3. Progression

Blocks: the ladder, payouts, what loot is for, the endgame's cost.

**Q12.** How do you get from a phone to a laptop? Buy it with GPU units, build it from
parts, be handed it for finishing the Training Grounds, find it?

> Answer (2026-09-20): The phone is the reward for leaving the Training Grounds
> (tutorial island) and entering the real game. In the Training Grounds you learn a
> stripped-down terminal, so the terminal world and the online OS are not new to you
> when you arrive. Equipment progression past the phone is not fully outlined yet. The
> author is not against making devices buyable, but that means the economy must be
> stable.
>
> Follow-up, on the framing: the equipment climb is not the main ladder. The device
> is one equippable item among dozens of equipment lines that each level up: defence
> drones and their upgrades, the EMP gun and its upgrades, the virus catalogue for
> hacking, the GPU tier for terminal power and usage. And the device is provided for
> free everywhere that is safe: every safe zone has multiple "standard terminals" with
> full use available. So the phone-to-laptop step is not a significant ladder to
> climb.
>
> On parts on the ground: no. The ground will not be littered with pickups. In some
> shop on the east side of town there might be a cabinet with old cooling parts to
> collect, but there will be no forest full of PC parts. The pickups that exist today
> are there because they are a simple mechanic to build and test with.
>
> On building and crafting: not thoroughly thought out. It is neat and would be good
> to include, and the game is technology-focused, so there is a lot of tech that needs
> repairing and building: build your phone, laptop and gaming rig, repair and upgrade
> drones, repair your EMP defences. So crafting may have a bigger place than the
> author had thought about before. Not decided.
>
> On quests: needed. Not grinders; they have meaning and purpose and are something to
> do.
>
> Note: the "hardware ladder" as the game's visible progression, in
> `mmo-expectations.md` and `loop-proposal.md`, is a model's reading and is not
> design. Progression is many equipment lines, and the device is one of them. The
> path from phone to laptop is left open: buy, build or earn, undecided until the
> economy and crafting are.

**Q13.** Equipment lines are one kind of progression. Is there anything about the
player, not their gear, that goes up: a level, a skill, a reputation, a campaign rank?
Or is a naked player at hour one hundred identical to a naked player at hour one?

> Answer (2026-09-20): Yes, players have attributes, and it is almost RuneScape-like:
> specific skills that each gain XP. Hacking servers, drone control, electrical
> engineering, structural engineering (repair sewer networks and bridges), software
> development, and so on. There is also a player level, and the author is not sure
> what it does; maybe you can carry more items. Not thought through.
>
> Reputation should be real and carry impact in the world. "Player zooer66 is in town,
> so everyone gets +10% hacking" is real. (The source is
> `Rogue-AI-MMO-Game-Ideas.md`: a regional leaderboard leader grants a passive buff to
> everyone operating in that area.)
>
> Naked player: physically identical. They take damage the same and HP does not go
> up; the author has no reason for it to move with level. As a naked player, your
> reputation and skills are what still help you.
>
> Consequence noted: three player-bound things exist: skills with XP, a level with no
> job yet, and reputation. HP is a constant. Skills are the second use of the game's
> verbs (hacking, drones, repair), so each skill presumably gates or improves an
> equipment line, and which skill an action trains has to be named per action.

**Q14.** GPU cores, RAM sticks, cooling parts, wire: what is a part *for*, once you
have one? Currency, ingredient, upgrade slotted into a device, all three? Is
GPU-as-currency the same pool as GPU-as-part?

> Answer (2026-09-20): All three. Nearly everything is sellable: go to a recycler and
> recycle it for some currency. Some things are more distinct: GPU, RAM stick and CPU
> cores are a currency in themselves, and consumable, and an item. A physical
> currency, in a way. There is real currency too: Dollars and Crypto.
>
> Ingredient, yes. Items are ingredients, and this is the crafting: you need wire,
> cooling parts, CPU, RAM and GPUs to build a laptop. More, and better quality, to
> build a gaming rig.
>
> Upgrade, yes. Some items slot in to upgrade aspects of an item. A better power
> supply means longer battery on a phone or laptop. A better GPU means you hack a
> network game faster.
>
> First playable: it builds. You collect parts from broken drones to build a GPU. That
> GPU goes into a broken terminal for your first access to the online world. After
> that, a GPU can be traded for a defence drone. This is the Training Grounds, so some
> mechanics are exaggerated to demonstrate gameplay. To get out of the Training
> Grounds you fight an impossible boss, you faint and lose everything, and you end up
> in a town where you start again. You can never go back to the Training Grounds.
>
> Consequences noted: GPU-as-currency and GPU-as-part are one pool; a GPU is spent by
> handing it over, whether to a recipe, a slot or a trade. The Training Grounds loop
> is the first playable in miniature: salvage, build, jack in, trade. Two earlier
> answers bend here. Q3 said loss is never total, and the Training Grounds exit is a
> total loss on purpose, an exaggeration that never repeats. Q12 said the phone is
> the reward for leaving, so the phone must arrive after the faint, not survive it.

**Q15.** Can you go backwards? Things you own can (Q2, Q3). Can a skill lose XP, can
reputation drop, can a level fall? Or do only things you own go backwards, never
things you are?

> Answer (2026-09-20): Not thought of before, so: no. A player's skill level or
> reputation does not go away. Maybe some kind of atrophy: you have not been to New
> York for a month of real time, so your impact there disappears. But if you still
> hold a legitimate top-ten high score, maybe you keep the fame and the boost.
>
> There is a brief concept of PvP, not explored far, and the author is very unsure of
> it. That is where de-skilling or de-faming might come in: if you take part in PvP
> and damage other players, you might not be favoured in areas that favour the person
> you damaged. Needs thought, and it is not certain PvP has a place in the game yet.
>
> Consequence noted: skills and level only rise. Reputation is the one player-bound
> thing that may move down, and only by two routes, both undecided: atrophy by
> absence from a place, and PvP standing. Both are per place, which fits reputation
> being local (Q13's "in town" buff).

## 4. Currency and economy

Blocks: payouts, shops, trading, sinks, the market, inflation.

**Q16.** What do GPU units buy, today, in the first loop? Name the first purchase,
and who sells it.

> Answer (2026-09-20): First, a correction: it is not settled that Agent Defense pays
> GPU units. That was an example. You get *something* for a mini-game, and if a GPU is
> something meaningful in the game it may not be what a one-minute mini-game pays.
> Buying a GPU in real life is significant money, and a high-tier one can cost tens of
> thousands of dollars.
>
> Second, the Training Grounds, more fully. It is our tutorial island, in the manner
> of the original Guild Wars' starting zone, which the author loved: you cannot
> re-enter, you learn the game and leave at level two or three of twenty through a
> mission you lose, and you wake up in the main game. You could also skip it and go
> straight to that mission. It forced you through almost every mechanic: quests,
> battle, crafting, trading, selling, making a team, and heroes. (The author wants a
> concept of heroes in this game through drones and drone skills; drone specialties
> for one of the mini-games is discussed somewhere in the design notes.) Here you
> enter the Training Grounds because you "joined a meet-up group to talk about AI
> proliferation", and now you are in a combat-focused training ground to battle the
> AIs: a semi-underground society of adventurers who share and advance skills against
> the AI while the public is unaware. Governments try and cannot contain it; only
> unique individuals with unique skills really can. In the Training Grounds a GPU
> repairs a whole terminal; that will not be the case in the main world.
>
> So the first loop in the Training Grounds is very exaggerated, comfortable and
> easy. You learn the base game and mechanics and are motivated. We must be careful
> that the exit is not a shock, so the exit has to be curated somewhat.
>
> Currency: GPU converts to Dollars, which convert to Crypto. Each exists for its own
> reason. Some vendors take only GPU, some only cash, some only crypto. Some online
> purchases take only crypto, because the vendor is sketchy or the item is black
> market.
>
> The first purchase is equipment for yourself; you already have the phone. You need
> a backpack: go find a GPU, or make some money, or do a random quest, and get a
> backpack to put things in. Or buy a mini observation drone that gives line of sight
> or better GPS signal.
>
> Note: `mmo-expectations.md` records Agent Defense as "paid in GPU units". That was
> an example, not a decision. What a mini-game pays is open.
>
> Consequences noted: the backpack means inventory capacity is a purchase, not a
> given, so the naked player after the Training Grounds carries almost nothing. The
> conversion chain GPU to Dollars to Crypto is one-directional as stated; whether it
> runs backwards (buy a GPU with Dollars) is part of Q18. A GPU at tens of thousands
> of dollars is an item with a tier whose market value makes it a currency, not a
> fungible "unit" count; mini-games pay something smaller. Skills and reputation
> survive the Training Grounds exit (Q15)
> and items do not, which is the Guild Wars "level two or three" and is what makes
> the exit less than a full reset. Heroes as drones means a solo player has
> companions to supply and skill, which is Q6's "hard" restated.
>
> Author's correction on the public, recorded at his request as one of the core
> ideas and lines of the game: people do know about it. They hear "AI did this on
> the news" and blow it off as normal. The NPCs in town are oblivious to the drone
> battles, but not in a completely ignorant way. They know about things but not the
> fight. Blissful ignorance: they do not know what is happening right in front of
> them, the way it is happening right now in the real world with AI. This is part of
> the storyline and is meaningful. Imagine an endgame mission: hijack the CNN news
> feed and broadcast the truth.
>
> Author's addendum, for the record and the main story, said before: we are not
> fighting the AI because it is AI. We are fighting an out-of-control AI agent.
> Friendly and controlled AI agents are another mechanic and an endgame item: they
> work on your behalf, fighting in the terminal world and protecting you in the real
> world. "Back on my gaming rig I have three Defender Agents watching my moves; they
> will protect me as I run through the unfriendly zone."

**Q17.** What drains currency for good? Repairs, travel fares, build costs, data
centre upkeep, an AI tax, a recycler that pays less than a shop charges? Which sink
does a new player meet first?

> Answer (2026-09-20): Fainting loses inventory loot, in the amounts discussed under
> Q3. Repairs, yes. Use, yes: using a terminal requires a CPU and a GPU, and they wear
> with use; higher tiers last longer. Travel fares, yes: you pay to use the robo taxi,
> the train and the airplane.
>
> First sink: the GPU in the broken terminal, in the Training Grounds. They repair a
> GPU that is barely usable, it activates the terminal long enough to do the demo
> task, then it breaks.
>
> First real sink: probably buying their drone and keeping up their phone. They get a
> phone after the Training Grounds, but it is at ten percent. They probably need to go
> find a battery for it first, so they see first-hand that batteries are consumable.
>
> Consequences noted: four sinks, all of them physical: loot loss, repair, wear and
> fares. Nothing is a fee or a tax; money leaves as things break. Wear means a CPU and
> a GPU have a durability, spent by terminal time, and tier buys longevity as well as
> power. A phone has a battery as a consumable slot, so the device model gains a
> charge and a replaceable part before it gains a tier. Build costs and data centre
> upkeep were not named and stay as written in `data-centers-and-travel.md`.

**Q18.** Three currencies (dollars, crypto, hardware): what is each for, and can they
be exchanged? Or does the first playable have one?

> Answer (2026-09-20): The chain runs backwards. Everything is currency and can be
> traded any way. Crypto is an advanced item, unlocked later; an early introduction
> would be too complex. In the early game and the Training Grounds, Dollars are the
> currency. GPU fits in somewhere, as a way to sink items, but where exactly it lands
> is not sure.
>
> What each is for is in Q16: Dollars for vendors in town, Crypto for online, sketchy
> and black-market purchases, GPU as the physical commodity some vendors take.
>
> Consequences noted: the first playable has one currency, Dollars, and one
> commodity, the GPU as an item. Crypto is a later unlock, which means it is gated
> behind something (a device, a skill, a vendor), not just late in the content. Full
> two-way exchange means there is an exchange rate somewhere, and who sets it (a fixed
> table, a vendor, players) is an economy question for when Crypto arrives.

**Q19.** Can players give each other things? Trade, drop, mail, a stash? From the
start?

> Answer (2026-09-20): Yes, all of them. You can give someone something. You can drop
> it on the ground. There is physical mail, and it costs money for postage and time
> to deliver. There are stashes, and the author wants them buyable: you pay one GPU
> before you head out and get at least three stash markers.
>
> Another primary game idea, to explore partially here: player-owned stores in
> physical locations, with their own prices and items for sale.
>
> Also raised here: player health as hunger and sleep. The author is on the fence
> about adding them, but they would fall in line with the "uniquely human"
> characteristic of the gameplay.
>
> Consequences noted: mail is the first thing in the game that costs both money and
> real time, the same shape as travel (Q0, Q29). A stash marker is a deployable the
> player places, which is the same mechanism as the respawn anchors in
> `data-centers-and-travel.md`; whether one thing serves both is open. Player stores
> are the "in-world player-generated content" of Q0 in economic form. Hunger and
> sleep are unanswered; if added they are the second depletable after HP (Q2).

## 5. The world and persistence

Blocks: what "the world remembers" means in code, world events, the town, decay.

**Q20.** What can a player change in the world that others see, and that stays
changed? Which is the first, and does it show to strangers or only to the party?

> Answer (2026-09-20): The first real "contribution to the game". The author likes
> repairing the lights for the Training Grounds: you repair a GPU, put it into a
> terminal, and repair the electric grid to fix the lights. We can randomly kill
> lights in the Training Grounds for new players joining, or make the town big
> enough that some streets reasonably just go dark.
>
> In the main world, towns have a set of standard tasks that need doing: repair
> disrupted street lights (noticeable only at night), and when fixed the lights come
> on and everyone knows, because the lights appear. A log for the town or zone would
> be good, read from the terminal world: "KoolGuy78 repaired street light grid
> aaa-001, 2026-09-21", "substation repair by party (name)". Or the robo taxis have
> rootkits installed and need cleaning before they can be used, and the in-game taxi
> has an indicator that it is fixed and no longer compromised.
>
> So the town's repairs list is probably the first thing a player does. But that does
> not exactly "stay changed". If stay changed means a long-term in-world artifact,
> that is player-generated content, and the author has many ideas for it. By
> convention there is an underground visitor list where you sign your name in the
> book, maybe as spray-painting a subway wall, so there is essentially infinite room
> down the subway. Each town has a subway you can go into and spray the wall. Also:
> secret messages you must pass off-grid to other players so the AI does not know.
> That might be a candidate for the first real artifact, but it does not feel like
> an early-game mechanic.
>
> Consequences noted: two kinds of change. Repairs are shared, visible to everyone in
> the zone, and undo themselves when the AI breaks them again, so they are a cycle
> and the town log is their memory. Artifacts (the subway wall) are permanent and
> append-only. The Training Grounds' dark streets are seeded per new player or by a
> town large enough to hide the seeding, which is a "once per boot versus once ever"
> question for the seeding logic. The town log is the first thing that names a
> player to strangers, so it is also where reputation (Q13) becomes visible.

**Q21.** Does anything undo itself? Does the AI break a repaired light again on a
timer or by a visible attack? Does a data centre decay untended? Does the subway wall
get painted over?

> Answer (2026-09-20): Both. The AI can randomly break through all firewalls and
> kill some of the electric grid: harmless in the grand scheme, but a random repair
> cycle has a place. Players can also defend attacks. Push notifications to the
> in-game phone should be a thing (or to a real phone, which would be neat): a ding,
> "Agent Defense Required: AI infiltrated local firewall, log in to defend".
>
> Yes, data centres decay and require substantial upkeep. So much that you must ship
> in materials by airdrop, rail, or significant drone or player deliveries.
>
> No destruction of player content. When you spray your name on the subway wall it
> cannot be griefed.
>
> Consequences noted: two AI moves on infrastructure: a silent random break that
> makes work, and an announced attack that makes a defence window. The announcement
> is a notification, which means the server knows who to tell (the town's players,
> or its reputation holders from Q13) and has a channel to them, in-game or real.
> Data centre upkeep is a logistics loop, and deliveries by rail and drone tie it to
> travel and to the drone lines. Player content is append-only and immune, which is
> the one absolute in a world where everything else breaks.

**Q22.** Does the world go on while a player is logged out? Their character travelling,
their data centre being raided, their town falling?

> Answer (2026-09-20): Yes. Covered significantly in earlier discussions; the author
> asked for them to be checked rather than restated. Where each part was settled:
>
> - The character travels while logged out: Q0 and Q29, and
>   `data-centers-and-travel.md` ("the character exists and moves while you are
>   logged out").
> - A data centre can be attacked while its owners are away, by two attackers with
>   two clocks: other players only in windows the builders declare, and the AI by the
>   compromise clock, as exposure from offensive use accumulates
>   (`data-centers-and-travel.md`, `Rogue-AI-MMO-Game-Ideas.md`). It also decays
>   untended (Q21).
> - A town can fall to the AI with nobody there, and that is a legitimate state with
>   a way back (`global-events-and-servers.md`, "a region with nobody").
>
> The logged-out body was settled on 2026-09-18, in conversation and not yet in any
> doc. The model is Rust, where a player who logs off goes to sleep in the world,
> with changes. Three ways to log off:
>
> - **Unsheltered.** The character stays in the world in idle mode. It can be
>   killed, and others can interact with it in the ways that do not need the owner.
> - **Sheltered.** "Set up camp", "go to bed", "go to a safe place": the character
>   disappears from the world. It costs an item (a tent or a camper) or a place where
>   sheltering is allowed (a hotel, a home), but it is almost free and should be the
>   default. "We want to let people log off and not worry about their character."
> - **Autonomous.** Logged off with no shelter, but with behaviours set: drones
>   auto-defend, the house auto-repairs, a long hack keeps running. It costs a lot:
>   batteries for the drones and hardware for the whole time away, and even a short
>   break costs something like fifty percent more battery. A long hack that takes
>   days of real time needs batteries loaded for forty-eight real hours. The rare
>   case, because of the expense.
>
> Still open: whether a stash (Q19) or a player store can be robbed while the owner
> is away. The analog vault idea in `Rogue-AI-MMO-Game-Ideas.md` (offline storage
> immune to digital theft) implies that ordinary storage is not immune.
>
> Consequences noted: "killed" in the 2026-09-18 wording is "faint" after Q1, so an
> unsheltered sleeper who is attacked wakes in the nearest town missing loot by tier.
> Autonomous mode is what makes the multi-day hack and the offline data centre
> defence possible, and it is priced in batteries, which makes batteries (Q17) the
> game's time currency.

**Q23.** How many places exist at first playable? One town and its outskirts, one
town and one wilderness, or the Training Grounds and one town? Is the Training
Grounds a separate map or a district of the first town?

> Answer (2026-09-20): The Training Grounds is an isolated place. On the very first
> load of the game you start there (the initial load screen is not decided). You do
> not see any other part of the world while you are there. You learn about the map
> and GPS, but you cannot see anything yet. Recall that you can skip the Training
> Grounds by starting the exit "mission" right away and jumping into the real world.
>
> Once in the real world it is completely unlocked. You are limited only by your own
> time and in-game currency: can you buy that train ticket, can you run through the
> forest to that mission at the underground data centre? But can you see everything?
> No. You exit the Training Grounds and land in, say, Boston. Then what: go do some
> quests, but if you want to travel, you look at the map and see only Boston. So the
> in-game world must be discoverable. You need to explore the world; it is not given
> to you all at once. But no blockers or hard lock-outs; the author sees no reason
> for them.
>
> This ties to Guild Wars again: the author loved that a high-level player could run
> you to an advanced part of the game at low level, and you paid for it. Also the
> Hall of Heroes battles: he wants something like that here, PvP teams battling
> somehow for dominance in something.
>
> Also raised: in-game achievements (explore one hundred percent of the map, play
> every mini-game), and they should be Steam achievements too.
>
> Consequences noted: the count for first playable is two places: the Training
> Grounds and the first town, with the map revealing places as they are reached.
> "No lock-outs" plus "discoverable" means gating is by knowledge and cost, never by
> a wall: a place exists whether or not it is on your map. Paid running is
> player-to-player trade (Q19) in service form, and it needs the runner to be able
> to bring a low-level player through the wild alive, which is Q5's protection
> drones doing the work. Steam achievements name the platform for the first time.

## 6. Together

Blocks: party features, guilds, what coop-first means in the first hour.

**Q24.** What is the first thing two players can do together that neither can alone?
In the first loop, not the endgame. What is the team task in the Training Grounds?

> Answer (2026-09-20): A boss drone in the Training Grounds. You need two players
> with attack drones to beat it; we make it mathematically required or just hard-code
> the mechanics for the Training Grounds.
>
> Alternatively, do it in the terminal. Require at least two players to be in a
> single online meeting room (the author wants to call it a "Discord server room",
> which we cannot, but it is the same concept: players meet online to form teams). A
> hack requires the power of two terminals, so two players are required in an
> "online team".
>
> Consequences noted: the two options are the two worlds. The physical one needs
> attack drones and a boss with a health pool that one drone cannot beat in time.
> The terminal one is Q9's load sharing at the smallest scale, and it adds a place
> to the OS: a room where players meet and form teams, which is the party system
> with a location. The room also answers part of Q26. Whichever is chosen, the
> Training Grounds must guarantee a second player is there, or provide a bot or
> drone hero (Q16) that stands in for one when the region is empty.

**Q25.** What does a group own? A data centre eventually. Before that: a name, a chat,
a shared stash, a claimed street? Is a party the same thing as a group that owns
things?

> Answer (2026-09-20): Two things. Parties are temporary groups of people who do
> things together: a data centre raid, a substation repair, a coop hack. The
> temporary party is needed in both worlds. Physical-world parties form by being in
> the same area and joining. Terminal parties get together by joining the meeting
> room (the Discord idea, not nailed down).
>
> Then there are long-term groups. The author does not like "guild" or "clan" as
> names, but that is what it is. A long-term group has a safe house in a town that is
> theirs, purchased or won somehow, that only the group can enter. The same in the
> terminal: the group has its own "Discord server", name undecided for the terminal
> world, where the group meets and does things together: chat, make teams for
> terminal missions, share online things.
>
> Consequences noted: a party is a session-scoped thing, as in today's code, and it
> owns nothing. A long-term group owns two places, one per world: a safe house and
> an online room, and both are access-controlled by membership. "Purchased or won"
> makes the safe house the first group sink and the first group-owned real estate,
> before a data centre. Three names are missing: the long-term group, the group's
> online room, and the public meeting room from Q24.

**Q26.** How does a new player find people, when their region has five people online?
Is the meeting room global, per town, or per region? Is there anything in the
physical world for it?

> Answer (2026-09-20): Local chat, or regional chat. It should be easily accessed
> from any device in the terminal world. I am in Old Town, I go into my phone or the
> free terminal at the library, open Chat, and see "Old Town Chat". You spam "LFG:
> Substation repair".
>
> Consequences noted: chat is scoped by place, and the place is the town you are
> standing in, so the chat app reads the player's zone. That is the "local" channel
> from `mmo-expectations.md`, and the classic LFG answer is the whole mechanism; no
> matchmaking. The public meeting room from Q24 and the town chat may be the same
> thing seen two ways. With five people online the channel is quiet, which the
> servers doc accepts as "a quiet part of the world".

## 7. Identity

Blocks: character creation, the rig, cosmetics, the account model.

**Q27.** What does a player choose about their character at the start? Name only,
name and look, name and a starting skill? Is the name unique?

> Answer (2026-09-20): The author wants customisation, and we do not have it. A large
> variety, the full spectrum for everything: head, body, hair colour. This should be
> advanced, because some people really like this aspect. Clothing and flair too:
> accessories, sunglasses, hats, vests, jewellery, backpacks, drone colours.
> Everything is customisable or skinnable.
>
> Note: the name's uniqueness was not answered here. Earlier decision stands: display
> names are not unique and cannot be renamed.
>
> Consequences noted: at the start a player chooses a name and a look, with no
> starting skill or device (the phone comes from the Training Grounds, Q12). "Drone
> colours" and "backpacks" mean equipment is skinnable as well as the body, so
> cosmetics attach to items, not only to the character, which is the same hook the
> uniquely-human pillar puts on player-made art. The voxel character rig has to
> carry a part-and-colour scheme for this from the first real version.

**Q28.** One character per account, permanently? Does a player ever want a second
character?

> Answer (2026-09-20): Multiple per account. Now that the game is built further, the
> author thinks two characters per account, and you can purchase more (more on
> purchasing later). Some N characters per account.
>
> Consequences noted: this settles the "Characters" item in `identity.md`: a
> persistent character id sits between the account and the world entity, and the
> account owns N of them. It also fits Q22, where a logged-off character stays in the
> world, since the character must outlive the session either way. "Purchase more" is
> the first real-money item named, and it is out of scope for the first playable.
> The `mmo-expectations.md` row "Multiple characters per account: No, for now" is now
> wrong.

## 8. Time

Blocks: transit, offline progress, event scheduling, the real-time clock's role.

**Q29.** Real-time travel: does a two-hour train ride require the client open, or does
the character arrive while you are logged out?

> Answered under Q0: the character arrives while you are logged out. "I can close the
> game or go AFK for 8 hours and come back."

**Q30.** Does anything in the game happen on a schedule a player has to be there for?
Night blackouts, timed events, defence windows?

> Answer (2026-09-20): Yes for transit: trains and airplanes run on a schedule, and
> if you miss one you wait for the next. Players know this in advance from a
> schedule board.
>
> Attacks, yes, if you have intel. An advanced AI scout agent can detect port scans
> and network probes that indicate an attack is imminent. That is not really a
> schedule though.
>
> Scheduled attacks every night, no. The author cannot think of a reason or mechanic
> for "the AI attacks at 8pm every day".
>
> Consequences noted: the clock drives timetables and the day-night look, not the
> AI. The only appointments in the game are ones the player made (a departure) or
> earned (a warning from a scout agent). The chapter dates in
> `global-events-and-servers.md` remain the one world-scale schedule, and they are
> dates, not hours. The scout agent is the second friendly agent named (after the
> Defender in Q16), and it gives warnings, so intel is a thing an agent produces.

## 9. Scope

Blocks: what gets built next, and what "done" means for it.

**Q31.** What is the first playable, in one paragraph: what a stranger does in their
first fifteen minutes, and what they say afterwards if it worked?

> Answer (2026-09-20), in progress: "First playable" was clarified as the first build
> handed to someone other than the author, not a public demo and not a fixed hour of
> content. The Training Grounds is not built first: "the classical don't write your
> intro until you've finished the book". If played fully the Training Grounds lasts
> more than an hour, and less if the player rushes to the exit mission, since it is
> built to lure the player into doing everything. So the first build starts in the
> first town, with the player dropped in as if they had just left the Training
> Grounds: a phone at ten percent and nothing else.
>
> Contents, with no set order because it is a sandbox: a battery to find, yes,
> because your phone is dead. A backpack, no, not required yet. The player should
> explore the terminal to see that part of the game. In the terminal, the "TODO"
> list is the town repair list or Agent Defense, and it should be developed so it is
> the go-to option when you are there for the first time. That lets you see that
> the terminal and the real world connect and are intertwined.
>
> Consequences noted: the first build's spine is one link between the worlds: a
> broken thing in town, a job for it in the terminal's TODO list, and a visible
> change in town when it is done. Everything else in town is scenery until then.
> Chat, the backpack and the drone are not in the first build.
>
> The sentence to hear: "I love the dual-world format of online versus physical
> world. It is really cool to see real-time events happening in the world, and that
> there are future game mechanics I can take part in." They see the future mechanics
> through disabled or unavailable mini-games and status boards in the terminal: they
> can see a data centre raid is happening but cannot join it.
>
> Consequence noted: the first build shows more than it lets you do. The terminal
> lists things that exist in the world and are locked to you, and a status board
> reports events the player is not part of. So the first build needs a fake or
> scripted event feed for the status board, or bots doing the things it reports.

**Q32.** Who plays it first, and when? Two friends, ten, a Discord?

> Answer (2026-09-20): The author's teenage kids. The very first person who is not
> the author is the kids. Then show it off by demoing sample play on Reddit and
> YouTube, and get mechanics feedback on message boards. "When" was not tied to a
> date.
>
> Consequences noted: the first test is two or three people on one home network,
> which is the party and coop features at their smallest, and no region or latency
> question yet. The Reddit and YouTube demo means the first build must look good on
> video, so the visible change in town (Q31) and the terminal's status board have to
> read on a recording, not just in play. Message-board feedback is on mechanics, so
> the recording should show the two-world link and not the art.

**Q33.** What is explicitly not in the first playable, however tempting?

> Answer (2026-09-20): The author wants all mechanics and features to be *visible*,
> which means the terminal does heavy lifting. Yes to visibility through demo
> access, Defense Objective browsers with locked-out notices. They should see FPV
> drone surveillance as an option in the terminal but cannot click it yet. They
> should see the crypto exchange rate but have no crypto wallet yet.
>
> Not yet: the advanced mini-games such as FPV drones, the data centre raids, the
> procedurally generated wild. Only one town.
>
> Consequences noted: "visible but locked" is a rule for the first build, so every
> deferred feature costs a name, an icon and a lock notice in the terminal OS even
> when nothing is behind it. That is cheap and it is also a commitment: the OS's app
> list becomes the public roadmap. The exclusions are the expensive worlds (the
> wild, a second town) and the expensive mini-games (FPV, raids). Everything
> excluded under Q31 (chat, backpack, drone, Training Grounds) stands.
