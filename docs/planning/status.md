# MMO expectations

What a player who has played any MMO expects to find, and where this game stands on
each. The left column is the generic expectation, in a few words, nothing specific to
this game. The right column is ours: what exists, what is planned and in what form, what
we are saying no to, and what is still undecided. A "No" is written down on purpose, so
the question is answered rather than forgotten.

Written 2026-09-20 from two lists compared side by side, and revised the same day from
the answers in `archive/decisions.md` (Q numbers below point there). Update the right column as
things land or get decided; the left column should rarely change.

Words used in the right column: **Done** exists and works. **Partial** some of it.
**Planned** decided, not built, with the form it takes. **Undecided** we know it is
needed and have not chosen. **No** we are not doing it, and why.

## Who am I

| Expectation | Where we are |
| --- | --- |
| Character creation | Planned. Today a name at connect. Wanted: the full spectrum for head, body and hair, plus clothing and flair (hats, sunglasses, vests, jewellery, backpacks, drone colours). No class (Q27). |
| Multiple characters per account | Planned. Two per account, more purchasable (Q28). `identity.md` still records one. |
| Visible progression (level, rank, tier) | Planned as skills with XP, RuneScape-style: hacking, drone control, electrical and structural engineering, software development. Also a player level with no job yet, and reputation. Skills and level only rise (Q13, Q15). The device is one equipment line among many, not the ladder (Q12). |
| Build variance (talents, specialisations) | Planned as which skills you train and which equipment lines you invest in: drones, EMP gun, virus catalogue, GPU tier (Q12, Q13). No talent tree. |
| Gear with slots and upgrades | Planned. Parts slot into devices: a better power supply is a longer battery, a better GPU hacks faster (Q14). Items with tiers exist; nothing is equipped yet. |
| Gear augmentation (sockets, enchanting) | Planned, as the slots above. Same mechanism. |
| Consumables | Planned. Batteries, and CPU and GPU wear with terminal use; higher tiers last longer (Q17). Nothing is used up yet. |
| Vitals (health, resource pools) | Planned. An HP meter, 100 to 0, constant across levels; below 50 you move slower, at 0 you faint (Q2, Q13). Hunger and sleep undecided (Q19). |
| Cosmetics (transmog, dyes, pets) | Planned. Everything customisable or skinnable, including equipment and drones (Q27). Player-made art on unique items is the same hook. |
| Titles and achievements | Planned. In-game and as Steam achievements: explore all of the map, play every mini-game (Q23). |

## What do I do

| Expectation | Where we are |
| --- | --- |
| Locomotion and camera | Done. Server-authoritative walking, collision with the dressing, a chase camera with zoom: always behind the player, W/S walk forward and back, A/D turn smoothly while held (150 degrees a second; the server walks any direction it is sent, normalised, at its own speed) and the camera eases round after, trailing the player a little (`ChaseHeading`, `WorldCamera.FollowSmoothly`, 2026-09-24). Right-drag looks round and tilts; the look eases back behind once you walk, the tilt stays. |
| Combat: targeting, auto-attack, skills | No, as a system. Players do act against AI threats with tools: EMP, radio scrambler, defence and attack drones (Q5, Q24). Drones are the heroes of Guild Wars, with specialties (Q16). |
| Status effects (buffs, stuns, roots) | Partial by design. Environmental: a hacked billboard blinds you, low HP slows you, a hack can disable map or chat (Q1, Q2). A reputation holder in town buffs everyone there (Q13). |
| Danger, failure and recovery (death, respawn) | Planned. No death. At 0 HP you faint and wake in the nearest town; loot is lost by tier, common often and high rarely (Q1 to Q4). Threats are drones, hacked vehicles and billboards. Whether a camp counts as "nearest town" is open. |
| Quests with a log and rewards | Planned. Quests with meaning, not grinders (Q12). The town repair list is the terminal's TODO list and the first-playable go-to (Q20, Q31). Agent Defense exists; what a mini-game pays is open (Q16). |
| NPC dialogue | Undecided. NPCs exist as scenery only. By design they are blissfully ignorant: they know about the AI from the news and blow it off, and do not see the fight (Q16). |
| A main story thread | Planned as the Prompt, a year-long collective campaign against the rogue AI. Nothing built. |
| Repeatable content (dailies, runs) | Planned. Tasks are repeatable by nature and scored; the leaderboard is what makes repeating them worth it. |
| Instanced group content (dungeons) | Planned. A hack that needs two terminals, a boss drone that needs two attack drones (Q24); a data centre raid as the mid-game group job (Q6). Nothing gated on party size. |
| Exploration and discovery | Planned. The world is discoverable: the map shows only where you have been, and nothing is hard-locked (Q23). Two zones today, the town and its outskirts, joined by a door. |
| Map and minimap, fog of war | Planned. Reveals by discovery (Q23); no signal in the wild means no map (`data-centers-and-travel.md`). No map yet. |
| Fast travel, mounts | Planned. Robo taxi, train and airplane on a timetable, with fares, in real time; the character arrives while you are logged out (Q0, Q17, Q29, Q30). Paid runs by high-level players (Q23). |
| Harvesting nodes, corpse loot | Planned as salvage: parts from broken drones, a cabinet in a shop. No forest full of PC parts. Today's ground pickups are a test mechanic (Q12, Q14). |
| Crafting and professions | Planned, not designed. Build a phone, laptop or rig from wire, cooling, CPU, RAM and GPU; repair and upgrade drones and defences (Q12, Q14). |
| Housing, a base of your own | Planned. A long-term group owns a safe house in town, purchased or won (Q25). A data centre later, and it decays without upkeep (Q21). |

## What do I have

| Expectation | Where we are |
| --- | --- |
| Inventory | Done. Stacks by type and tier, persisted. Planned: capacity comes from a backpack you buy (Q16). |
| Storage, a bank | Planned. Stash markers you place, bought for one GPU per three (Q19). Whether a stash can be robbed while you are away is open (Q22). |
| Currency | Partial. Dollars work as pocket change: a balance on the player, persisted, shown in the inventory; a new player has $10 (`features/second-zone.md`). Planned: GPU, RAM and CPU as a physical commodity currency, and a GPU is expensive. Crypto is a later unlock for online and black-market vendors. All exchange both ways (Q16, Q18). |
| Currency sinks (repair, fees) | Planned. Loot lost on fainting, repairs, CPU and GPU wear, batteries, fares, postage (Q17, Q19). Nothing drains yet. |
| Loot with rarity | Done. World items in four tiers. Loss on fainting scales by tier (Q3). |
| Vendors and shops | Partial. One shopkeeper sells the battery for $8 and shows two things a new player cannot afford; click them, see the list, buy (`features/second-zone.md`). Planned: a recycler buys almost anything; vendors take Dollars, GPU or crypto by their nature (Q14, Q16). Player-owned stores in physical locations (Q19). |
| Player-to-player trading | Planned. Give, drop, mail and stash, from the start (Q19). |
| Auction house or market | Planned, late. The main trading hub is the biggest city and the natural place for it. |
| Item linking in chat | Undecided. Small, noticed quickly. |

## Who am I with

| Expectation | Where we are |
| --- | --- |
| Chat: global, local, party, whisper | Partial. Global only, with a filter layer. Planned: a chat per town ("Old Town Chat") reachable from any device (Q26). |
| Party | Done. Invite, accept, leave, see each other across the zone. Planned: temporary parties in both worlds, terminal parties formed in an online meeting room (Q25). |
| Shared party progress (experience, quests) | Undecided. Nothing to share yet. |
| Guilds or clans | Planned, name undecided. A long-term group owns a safe house and an online room (Q25). |
| Friends list and presence | Undecided. |
| Block, ignore, report | Undecided. Expected on day one of any public build. |
| Emotes | Undecided. The character rig makes them cheap. |
| Inspecting other players | Partial. Name and party membership. |
| Mail, offline handoff | Planned. Physical mail that costs postage and delivery time (Q19). |
| Finding people to play with | Planned. Local chat and LFG (Q26). Solo is hard, not walled off (Q6). |

## The world

| Expectation | Where we are |
| --- | --- |
| Persistence | Done for position, inventory and device. Planned: three log-off modes, unsheltered sleeper, sheltered (the default) and autonomous (Q22); player content is permanent and cannot be griefed (Q21). |
| Day and night, weather | Partial. Real-time clock with the server's time zone, lighting follows the hour, moonlight at night. Weather planned, synced to the real world. |
| Zones and travel between them | Partial. Two zones, the town and its outskirts, joined by walk-in doors: the road leads off the edge through a shimmer, the screen fades, the party comes along (`features/second-zone.md`). The shop interior is a third zone waiting on assets. Travel between towns is not built; the Training Grounds is built last, in the Guild Wars manner, and can be skipped (Q16, Q23, Q31). |
| Safe hubs | Planned. Every safe zone has free standard terminals with full use (Q12). A data centre is a safe zone in the wild (Q11). |
| Public world events | Planned. The AI randomly breaks grid and firewalls for a repair cycle, and announced attacks arrive as push notifications (Q21). No fixed nightly schedule (Q30). The Prompt as the global one. |
| World bosses | Undecided. |
| Faction reputation | Planned. Reputation is real and local; a top player in town buffs everyone there; a town log names who repaired what (Q13, Q20). Atrophy by absence, maybe (Q15). |
| NPCs with routines | Planned. Server-side routines on the world clock, places named in the scene. |

## Competition and recognition

| Expectation | Where we are |
| --- | --- |
| Leaderboards | Planned. Top ten and personal best per task. |
| Player versus player | Limited by design: strategy mini-games behind the terminal, not open-world. The author is unsure PvP has a place yet; wanted: team battles for dominance in the Hall of Heroes manner (Q15, Q23). |
| Rankings, seasons | Undecided. |
| Espionage, raiding | Planned, late. A data centre can be raided; espionage is a design pillar. Form undecided. |

## What every game has

| Expectation | Where we are |
| --- | --- |
| Onboarding, the first ten minutes | Planned as the Training Grounds: isolated, exaggerated, forces every mechanic, exited by a mission you lose. Built last (Q16, Q31). |
| Settings, keybinds, accessibility | Partial. Fullscreen, rebinding, UI scale. No audio sliders, no colour or text options. |
| Help and reference | No. Undecided whether in-game or a site. |
| A reason to log in tomorrow | Planned. The repair cycle, a defence ping on your phone, a train arriving, a town log with your name in it (Q20, Q21). |
| Stable, fair, cheat-resistant | Partial. Server authority over movement and scoring; deterministic tasks scored on the server. No anti-cheat beyond that. |
| Moderation (mute, kick, ban) | Undecided. Nothing exists. |
