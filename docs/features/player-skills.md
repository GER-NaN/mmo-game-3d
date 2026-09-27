# Player skills and specializations

**Date:** 2026-09-26
**Status:** Agreed (feature design, 2026-09-26). No technology fork yet.
**Sources read:** docs/world.md sections 1, 2, 5, 6, 7, 10, 13, 16; docs/backlog.md;
docs/first-playable.md ("What is not in it")
**Build from:** the Outcome section. The Q&A is the record of how it was reached.

A world-building session: what skills and specializations are in the game, for the
player and in the fiction. No technology fork. The build is in the new repo
(`mmo-game-3d`), and a technology session comes later, when the author asks for one.

The answers are the author's own words, typed in conversation and recorded verbatim or
near it, one question at a time. Model additions are set apart and labelled:
"Consequence noted", "Options offered", "Note".

## Already decided

Decisions in force that this design must fit, or change on purpose.

- Players have attributes, almost RuneScape-like: specific skills that gain XP. Hacking
  servers, drone control, electrical engineering, structural engineering (repair sewer
  networks and bridges), software development, and so on. [Q13]
- There is also a player level. What it does is open; maybe you can carry more. [Q13,
  backlog]
- Skills and reputation do not go away. Maybe some atrophy by absence from a place; a
  legitimate top-ten score may keep the fame and the boost. [Q15, backlog]
- A naked player at hour one hundred is physically identical to one at hour one. HP does
  not go up with progression. What still helps a naked player is reputation and
  skills. [Q2, Q13]
- Reputation is real and has impact: "Player zooer66 is in town, so everyone gets +10%
  hacking". [Q13]
- A regional leaderboard leader can give a passive buff to everyone operating in that
  area. [B, Q13]
- Governments fail because they lack the combination of technical skill, creative
  sensibility and personal motivation. Only unique individuals with unique skills can
  really beat the AI. [B, Q16]
- The player joined a semi-underground society of adventurers who share and advance
  skills against the AI. [Q16]
- The "uniquely human" pillar: what is uniquely valuable about being human, in contrast
  to AI, shows up in the mechanics. [B]
- A player's discovery (a virus) can become a permanent attack skill for everyone, as a
  world-level event. [B, T2]
- Equipment lines each level up (drones, EMP gun, virus catalogue, GPU tier). The device
  is not a significant ladder to climb. [Q12]
- Drones are companions with specialties and roles with real trade-offs (defence,
  attack or anti-drone, support, observation), so composition is a choice. [B, Q16]
- Runner is a natural emergent profession: a high-level player runs low-level ones
  through dangerous zones for pay. A Runner's protection scales with their
  progression. [B, Q23]
- Crafting has a possibly bigger place than first thought: build phone, laptop, gaming
  rig; repair and upgrade drones. Its full shape is open. [Q12, Q14, backlog]
- A player's own AI assistant scales with progression. [B]
- The Training Grounds forces you through nearly every mechanic. On exit you lose
  everything and start again in a town. [Q12, Q16, Q23]
- An account has two characters, more can be purchased. [Q28]

Built:

- Nothing. No skill, XP, level or reputation exists in the code of either repo.

Deferred:

- Skills, level and reputation are out of the first playable. [`first-playable.md`]
- What the player level does. [backlog]

## Feature design

### Developer thoughts

> Answer (2026-09-26): Players get skills ( I do not want to use the name skill but its
> a good base name for discussion). Skills are a specific set of pre defined actions in
> the game like (Drone Control, Hacking, Networking, Socal, etc.. think of some more that
> may apply). You gain experience and level up your skills as you complete things in the
> game. What increased skill does for you is give you small rewards within the game, mini
> games and pvp games and encounters. For example high level networking may let you see
> extra information when looking at a network map (or in the network defense mini game
> you get hints at which best line to cut or something). We can nail down the exact
> specifics and benefits later beacuse they need to tie to game mechanics we may not have
> yet. We can start with just a few skills. The idea though, gain skills by completing
> everyday things in the game and you get some kind of benefit.
>
> Then I want specializations (Again i dont know what to call this, maybe Career?). These
> are careers and I really only want a few of these <4 which work together well in
> groups. So I can imagine one career being a drone operator, where you get specific
> abilities related to drones (carry extra drones, better controls of drones, or extra
> drone abilities) Or a Computer Scientist (who is good at hacking and doing terminal
> activities), they get extra menus and more information from the terminal during mini
> games and in world use OR Mechanical Engineer (Who is really good at repairing things,
> they get a repair kit so they can repair drones without a workbench or repair phones
> without a workbench). Then with these different specializations this allows you to form
> teams for specific missions in the game to the players benefit. A team of different
> specializations can complete things faster. Thats the basic idea.
>
> The goal here is to build the system first (skills + specializations) and allow you to
> accumulate experience and ranks but not have a meaningful impact yet (because we need
> all the supporting features first).
>
> Follow ups here quicky. Specializations have levels (Apprentice, Graduate, Senior,
> Master, Elite), each level gives you something new. You can switch specializations but
> it is a permanent switch and you loose all existing experience in that specializations
> and have to start over (this is where multipell characters per account would be
> useful).
>
> Options offered (asked for, "think of some more that may apply"; partly withdrawn, see
> the Note under F1a): candidate skills, each
> tied to something already in `world.md`. None chosen yet.
>
> - Drone Control, Hacking, Networking, Social. The author's four.
> - Electrical Engineering: street lights, substations, the grid in Agent Defense. [Q13,
>   Q20]
> - Structural Engineering: sewer networks and bridges. [Q13]
> - Software Development. [Q13]
> - Hardware (or Electronics): build and repair phones, laptops, rigs, drones; the
>   crafting line. [Q12, Q14]
> - Surveillance: CCTV operation, spotting drone activity, FPV observation. [Q8, B]
> - Research (or Forensics): search archive data for clues on the Prompt. [Q8, B]
> - Navigation: exploring the discoverable map, travel in the wild, GPS. [Q23, T1]
> - Trading: buying, selling, player stores. [Q19]
> - Endurance: long walks, and hunger and sleep if they come in. The uniquely-human
>   pillar. [B, Q19]
>
> Consequence noted: skills and specializations are two separate progressions. Skills are
> many, open to everyone, and earned by doing. A specialization is one at a time (the
> switch implies that), fewer than four exist, and it has its own experience and five
> ranks.
>
> Consequence noted: the switch rule fits "skills and reputation do not go away" [Q15]
> only if the loss touches the specialization's experience and not the skills. The answer
> says "experience in that specializations", so skills stay. F-questions check this.
>
> Consequence noted: "Runner" is already a natural emergent profession [Q23]. Runner is
> not a career in this list, so "profession" and "career" need to stay distinct words, or
> Runner changes name.
>
> Consequence noted: "no meaningful impact yet" fits the first playable, which keeps
> skills, level and reputation out. The system records experience and ranks, and the
> benefits come with the mechanics they tie to.

### Q&A

**F1.** How does a player earn experience in a specialization, and how does that relate
to the skill experience that the same action earns?

> Answer (2026-09-26): To start a specialization you need some minimum level of
> experience in related skills. Lets take Mechanical Engineer as the specialization. You
> would need N workbench skill level and N field repair skill and N electrical repair
> skill before you can start the mechanical Engineer specialization. Then the contribute
> skills also begin advancing your specializations, along with use of specialization
> specific mechans. SO the engineer gets a built-in repair pack so you can repair things
> easier. Each time you repair something you get EXp towards your specialization.
>
> Consequence noted: a specialization has a gate. Each one names its contributing skills
> and a minimum level in each. Once started, it earns experience from two sources: the
> experience its contributing skills earn, and use of its own mechanics (the engineer's
> built-in repair pack).
>
> Consequence noted: one action can pay twice. A repair raises the repair skill and, for
> an engineer, the specialization too. The skill itself keeps advancing.
>
> Consequence noted: the three skills named here (workbench, field repair, electrical
> repair) are finer than the broad ones in [Q13] and in the options list (Electrical
> Engineering, Hardware). F1a asks which grain is meant.

**F1a.** Follow-up: the skills here (workbench, field repair, electrical repair) are
narrower than the earlier ones (Hacking, Electrical Engineering). How narrow is one skill?

> Answer (2026-09-26): Make sure you know the difference (Skill VS Specialization). I
> think a specialization is more like a Career, and during your career you use many
> skills. Your skills you are good at can guide you to a specific career, etc.... Skills
> are narrow, I want a narrow skill for Agility (based on your movement, jumps, travel
> distance), Hacking (mini games based on code/termianl use) hacking is a narrow meaning.
> Electrical Engineering is more like a Specialization so that may have been a bad
> sentence I typed
>
> Note: this corrects [Q13] in `world.md` §6, which lists "electrical engineering,
> structural engineering (repair sewer networks and bridges), software development" as
> skills. By this answer those read as specializations (careers), not skills. It also
> withdraws Electrical Engineering, Structural Engineering, Software Development and
> Hardware from the options list above as skill candidates. They are candidates for
> careers now, and the cap stays below four.
>
> Consequence noted: a skill is narrow and tied to one kind of activity: Agility is
> movement, jumps and travel distance; Hacking is code and terminal mini-games. A career
> is broad and uses many skills. Your best skills point you to a career.
>
> Consequence noted: Agility is the first skill earned by the body, not by a device or a
> task. Its benefit is not set, but a movement benefit meets [Q13]: "a naked player at
> hour one hundred is physically identical to one at hour one". F1b asks about this.

**F1b.** Follow-up: [Q13] says a naked player at hour one hundred is physically
identical to one at hour one. Can a skill such as Agility change what the body does?

> Answer (2026-09-26): I am undecided on that.
>
> Options offered (on "undecided"): the facts first. HP does not go up with progression,
> and below 50 HP you move slower [Q2, Q13]. [Q13] already says skills help a naked
> player, so the question is only whether that help can be physical. Every benefit the
> author named so far is information or a hint (extra data on a network map, the best
> line to cut), not a stat.
>
> - A. No. A skill never changes the body. Agility pays in information and ease: route
>   hints, travel shown on the map, a cosmetic flourish on jumps. [Q13] stays whole.
> - B. Only at the edges. The body stays the same at full health, but a skill softens
>   a penalty: for example, less slow-down below 50 HP, or slower fatigue on long walks
>   if sleep comes in. [Q13] changes a little, on purpose.
> - C. Yes. Agility makes you faster or jump higher. [Q13] is reversed, and a veteran
>   outruns a new player in the same party.
>
> The model would take A. It matches the benefits the author named, it keeps the
> naked-player rule that the author gave a reason for, and it leaves B possible later
> without a reversal.
>
> Decision: pending the author's word.
>
> Decision (2026-09-26): I like C, the naked player rule is outdated and not a rule it
> was an earlier decisison.
>
> Note: this changes [Q13] in `world.md` §6 on purpose: "A naked player at hour one
> hundred is physically identical to one at hour one." A skill such as Agility can make
> the body faster or jump higher. Listed in Consequences.
>
> Consequence noted: the same paragraph of [Q13] and [Q2] also says HP does not go up
> with progression. F1c asks whether that rule goes too.

**F1c.** Follow-up: [Q2, Q13] also says HP stays 100 to 0 and does not go up with
progression. Does that rule stay?

> Answer (2026-09-26): Right, we dont need to change the HP, we can add some kind of
> armor if we want
>
> Consequence noted: HP stays 100 to 0, flat with progression [Q2, Q13]. Only the
> naked-player sentence changes. Armor is a new idea, not decided; it goes to the backlog
> at close.

**F2.** The careers named so far are Drone Operator, Computer Scientist and Mechanical
Engineer, and F1a calls Electrical Engineering a specialization too. That is four, and the
cap is below four. Which careers are in the game?

> Answer (2026-09-26): I also want Librarian (gets more access to information). I also
> want some uniquly human careers (Artist, Truck Driver, etc..) So now there is no limit.
>
> Note: this changes the developer thoughts on purpose: "I really only want a few of
> these <4 which work together well in groups." There is no cap on careers now.
>
> Consequence noted: the career list so far is Drone Operator, Computer Scientist,
> Mechanical Engineer, Electrical Engineer (from F1a), Librarian, and the uniquely-human
> careers, Artist and Truck Driver as examples. The list is open.
>
> Consequence noted: some of these meet things already in `world.md`. Truck Driver meets
> the data-centre upkeep that ships materials in by rail, drone or player delivery [Q21],
> and the Runner, an emergent profession [Q23]. Artist meets the player-submitted content
> pipeline (videos, music, artwork) [B] and the subway wall [Q20]. Librarian meets the
> library terminal [B, Q26] and the search of archive data for Prompt clues [Q8].

**F2a.** Follow-up: what does a uniquely-human career, such as Artist or Truck Driver,
do in the fight against the AI?

> Answer (2026-09-26): The point is we are not limiting ourselves, we will pick a few to
> implement first.... FOr your follow up, good questions. The truck driver gets extra
> control of vehicles and repairing, they get extra ability with car/train/planes etc..
> the artist IDK and I am ok with the specialization not have a direct counter in the
> fight against AI
>
> Consequence noted: the career list is open-ended, and the first build picks a few. A
> career does not need a part in the fight against the AI. The developer thoughts said
> careers "work together well in groups"; that now holds for some careers, not all.
>
> Consequence noted: Truck Driver covers all vehicles (car, train, plane), in control
> and in repair, so the name is narrower than the career. Vehicle repair overlaps with
> Mechanical Engineer's repair. Vehicles in `world.md` today: robo taxis with rootkits
> [Q20], a connected vehicle made to explode [§8], rail and airdrop for data-centre
> upkeep [Q21].
>
> Note: what the Artist does stays open on purpose. The author is fine with it having no
> part in the fight, and the first build picks only a few careers.

**F3.** Which careers and which skills go in the first build?

> Answer (2026-09-26): I am more interested in building and including the system than
> legitimate picks but lets go wtih Mechanical Engineer and Computer Scientist
>
> Consequence noted: the first build is about the system: skills that earn experience,
> careers with gates and ranks. The content is Mechanical Engineer and Computer
> Scientist. The skills are the ones those two careers need as gates: for Mechanical
> Engineer, workbench, field repair and electrical repair [F1]; for Computer Scientist,
> Hacking [F1a] and others not named yet. Agility [F1a] is a candidate that no career
> needs yet.

**F4.** What does a player do to start a career once they meet its minimums?

> Answer (2026-09-26): Go to the college and enroll in a class (the first time you pick a
> career you need to do a "Class" which is just a tutorial on what the career selection
> means). After that you go to the college, talk to the registrar and can change careers
> (but you loose all progress in your current one)
>
> Consequence noted: a career starts at a place in the world, the college, not from a
> menu. The first career needs a Class, a tutorial on what a career selection means.
> Every later change goes through the registrar at the college and resets the progress
> in the career you leave.
>
> Consequence noted: the college is a new building, so its inside is a separate,
> enlarged interior [§10, C-2026-09-18]. The registrar is an NPC the player talks to.
> Neither exists in `world.md` or the code.

**F4a.** Follow-up: where is the college in the world?

> Answer (2026-09-26): Anywhere, probably its own zone, there can be multiple sperad
> throughout the world. For now its just a building int he town.
>
> Consequence noted: colleges can be many, spread through the world, each probably its
> own zone. The first build has one: a building in the town.

**F5.** [Q13] says there is also a player level, and what it does is open. Is there still
a player level next to skills and careers?

> Answer (2026-09-26): I think yes. Player leve will be an accumulation of overall time,
> skill accumulation and career progression and mission completion. We will need to nail
> this down later.
>
> Consequence noted: the player level stays, as a sum of four things: time played, skill
> experience, career progress and missions completed. The formula is open, and so is
> what the level does [backlog]. The level does not reset when a career is left, unless
> the career-progress part is taken out of it; that also waits for the formula.

**F6.** The Training Grounds exit makes you lose everything and start again in a town
[Q16, Q23]. What happens to the skill experience earned in the Training Grounds?

> Answer (2026-09-26): You keep your skill levels from training grounds, I expect they
> will be small but we dont need to wipe them
>
> Consequence noted: "lose everything" at the Training Grounds exit means items, not
> skills. Skill levels carry over into the real world. This fits "skills do not go
> away" [Q15].

**F7.** What does a player see of their own skills and career, and what do other players
see of them?

> Answer (2026-09-26): Career and Career progress is visible to everyone, Skill sare kept
> private unless shared.
>
> Consequence noted: a career and its rank are public, which fits forming teams by
> career. Skills are private by default, with a way to share them.

**F7a.** Follow-up: how does a player share their skills, and with whom?

> Answer (2026-09-26): They can ebale sharing on their social page (this is new I know).
> Social page is just a page in the online world for the player, they can enable "Show
> Skills" when looking at their social page.
>
> Consequence noted: the social page is new. It is a page for each player in the online
> (terminal) world. It shows the career and rank to everyone, and the skills only when
> its owner turns on "Show Skills". Sharing is to everyone who opens the page, not to
> chosen players.

**F8.** How does a career move up a rank, from Apprentice to Graduate and on to Elite?

> Answer (2026-09-26): Accumulation of supporting skills (need to figure out math here),
> use if career specific abilities.
>
> Consequence noted: rank comes from career experience, and career experience has the
> two sources from [F1]: the supporting skills and the career's own abilities. The
> numbers are open.

**F8a.** Follow-up: the first career needs a Class at the college [F4]. Does a new rank
need a visit to the college too?

> Answer (2026-09-26): Neat idea, yes lets require you to go to a college and meet with
> your old professor to get the level up in your career.
>
> Consequence noted: a rank does not arrive by itself. The experience makes you
> eligible, and the rank-up happens at a college, in a meeting with your old professor.
> The professor is a second college NPC next to the registrar [F4].
>
> Consequence noted: colleges can be many [F4a], and "your old professor" reads as one
> person tied to you. F8b asks which.

**F8b.** Follow-up: is "your old professor" one person you go back to, or does every
college have a professor who can do it?

> Answer (2026-09-26): Must be your original college.
>
> Consequence noted: a player belongs to one college, the one where they took the Class.
> Every rank-up is a trip back to it. With real geography and real travel times [B],
> that trip is a cost of its own when the player has moved far away.
>
> Note: open, not asked: which college is "original" after a career change at a
> different college's registrar. Left for the formula and college session, since the
> first build has one college.

**F9.** The developer thoughts say a team of different careers completes things faster.
How does the game treat a team that mixes careers?

> Answer (2026-09-26): It treats them no differently.
>
> Consequence noted: there is no bonus for team make-up. A mixed team is faster only
> because each member brings their own career's abilities: the engineer repairs in the
> field, the computer scientist sees more in the terminal.

**F10.** The developer thoughts say "skill" is a base name for discussion, not the word
you want. What is the word for a skill?

> Answer (2026-09-26): I dont have one yet. Skill sounds too Runecrafty. I guess that
> probably is the best name though. I dont know yet. Keep skill for now
>
> Consequence noted: "skill" is the working word. The name stays open; the author
> finds it too RuneScape-like.

**F11.** Is "career" the word for a specialization?

> Answer (2026-09-26): Yes, I like career, it fits
>
> Consequence noted: "career" replaces "specialization" from here on. "Profession"
> stays the word for an emergent role such as the Runner [Q23], which is not a career.

**F12.** The first build records experience and ranks with no real impact yet. Does it
give a career any of its abilities, such as the engineer's repair pack?

> Answer (2026-09-26): Lets forget about first build, I dont want to restrict and decide
> on "first build" its really annoying and it limits implementation artifically.
>
> Note: F12 is withdrawn. The model's readings that frame a "first build" (under the
> developer thoughts, [F3], [F4a], [F8b]) are withdrawn with it. This record designs the
> system, not a build scope. Which careers and skills get built first is the author's
> call when the build starts; [F3] names Mechanical Engineer and Computer Scientist.

**F13.** Can a player play the whole game without a career?

> Answer (2026-09-26): Yes
>
> Consequence noted: a career is optional. Nothing in the game is locked behind one; a
> career makes some things easier or richer.

**F14.** At what moment does a skill earn experience?

> Answer (2026-09-26): When you do the thing. If agility is a skill, we measure walking
> distance and every N distance than earn an XP in agility. ever N jumps. When you
> repair an electric box you earn electric skill XP.
>
> Consequence noted: experience comes from the act itself, counted by the game. Some
> skills count a quantity (every N distance walked, every N jumps). Others count a
> completed act (each electric box repaired). The skill names Agility and "electric
> skill" are working names.

### Outcome

Agreed 2026-09-26. "DT" is the developer thoughts.

Skills:

- A skill is narrow: one kind of activity. Named so far: Agility (movement, jumps,
  travel distance), Hacking (code and terminal mini-games), Drone Control, Networking,
  Social, workbench, field repair, electrical repair. [DT, F1, F1a]
- A skill earns experience when you do the thing, counted by the game: every N distance
  walked, every N jumps, each electric box repaired. [DT, F14]
- A higher skill gives small rewards in play, mini-games, PvP and encounters, for
  example extra information on a network map or a hint at the best line to cut. The
  exact benefits wait on the mechanics they tie to. [DT]
- A skill can change what the body does: Agility can make you faster or jump higher.
  HP stays 100 to 0 and does not grow. [F1b, F1c]
- Skills are private. A player can turn on "Show Skills" on their social page, a page for
  each player in the online world. [F7, F7a]
- Skill levels earned in the Training Grounds carry over; the exit does not wipe them.
  [F6]

Careers:

- A career is broad and uses many skills. Your best skills point you to one. [F1a]
- The list of careers is open, with no cap. Named: Drone Operator, Computer Scientist,
  Mechanical Engineer, Electrical Engineer, Librarian, Truck Driver (all vehicles: car,
  train, plane), Artist. A career need not have a part in the fight against the AI.
  [DT, F1a, F2, F2a]
- Mechanical Engineer and Computer Scientist are the first two to build. [F3]
- A career has a gate: a minimum level in each of its supporting skills. [F1]
- A career has its own experience, from two sources: the experience its supporting
  skills earn, and use of the career's own abilities (the engineer's built-in repair
  pack). [F1, F8]
- A career has five ranks: Apprentice, Graduate, Senior, Master, Elite. Each rank gives
  something new. [DT]
- A player starts a career at a college. The first time, they take a Class, a tutorial
  on what a career selection means. [F4]
- A rank-up happens at the player's original college, in a meeting with their old
  professor, once the experience is there. [F8a, F8b]
- A player changes career through the registrar at a college. The change is permanent,
  and all progress in the career left behind is lost. Skills stay. [DT, F4]
- A player has one career at a time, and a career is optional: the whole game can be
  played without one. [DT, F13]
- A player's career and rank are public. [F7]
- A team that mixes careers gets no bonus for the mix. Each member brings their own
  career's abilities. [F9]
- Colleges can be many, spread through the world, probably each its own zone. One
  college is a building in the town. [F4a]

Player level and names:

- The player level stays. It adds up time played, skill experience, career progress
  and missions completed. [F5]
- "Career" is the word for a specialization. "Skill" is the working word; the name is
  open. [F10, F11]

Deferred:

- The word for a skill. [F10]
- The numbers: experience per act, skill levels, career experience, rank thresholds.
  [F8]
- The player level formula, and what the level does. [F5, backlog]
- What each skill and each rank gives. Waits on the mechanics they tie to. [DT]
- What the Artist does. Open on purpose. [F2a]
- Which college is "original" after a career change at a different college. [F8b]
- Armor, as a way to protect a player without raising HP. An idea, not decided. [F1c]

## Gatekeeping

Each "Already decided" item against the Outcome.

- Skills are RuneScape-like attributes that gain XP [Q13]: fits. The listed examples
  change by [F1a]: electrical engineering, structural engineering and software
  development are careers, not skills.
- The player level exists, and what it does is open [Q13]: fits. [F5] adds what it
  counts; what it does stays open.
- Skills and reputation do not go away [Q15]: fits. A career change loses career
  progress, not skills [DT, F4]; the Training Grounds exit keeps skills [F6].
- A naked player at hour one hundred is physically identical to one at hour one [Q13]:
  changed by [F1b].
- HP does not go up with progression [Q2, Q13]: fits [F1c].
- Reputation is real and has impact [Q13]: fits. Reputation is separate from skills and
  careers; this session did not touch it.
- A regional leaderboard leader gives a passive buff [B, Q13]: fits, untouched.
- Only unique individuals with unique skills can beat the AI [B, Q16]: fits.
- A semi-underground society that shares and advances skills [Q16]: fits. The college
  is public; nothing in this session says who the college serves in the fiction.
- The uniquely-human pillar [B]: fits. [F2] adds uniquely-human careers.
- A player's discovery can become a permanent attack skill for everyone [B, T2]: fits.
  That "skill" is an ability, not a skill in this session's sense; the word needs care
  when it is built.
- Equipment lines level up, the device is not a ladder [Q12]: fits. Item levels and skill
  levels are separate.
- Drones have roles with trade-offs [B, Q16]: fits. The Drone Operator career sits on top.
- Runner as an emergent profession [B, Q23]: fits. Runner is not a career [F11].
- Crafting's full shape is open [Q12, Q14]: fits. The engineer's repair pack repairs
  away from a workbench [DT]; the workbench stays the general place [C-2026-09-21].
- The player's AI assistant scales with progression [B]: fits, untouched.
- The Training Grounds exit loses everything [Q16, Q23]: fits. "Everything" means items;
  skills stay [F6].
- Two characters per account [Q28]: fits. Skills and careers are per character; the
  permanent career change is where more characters help [DT].
- Skills, level and reputation out of the first playable [`first-playable.md`]: open.
  The author set build scope aside in this session [F12].

## Consequences

- `world.md` §6: the skill examples in [Q13] change (electrical engineering, structural
  engineering and software development are careers); the naked-player sentence goes
  [F1b]; new paragraphs for skills, careers, the college and the player level, tagged
  [C-2026-09-26].
- `world.md`: the social page, a page for each player in the online world [F7a], has no
  section yet. §4 (the terminal) or §13 (together) are candidates.
- `backlog.md`: "What the player level does" stays, with what it counts added [F5]. New
  entries: the word for a skill, the Artist, armor, the original college after a
  career change.
- `first-playable.md` lists skills, level and reputation as out. Not changed here.
- New NPCs: the registrar and the professor. New place: the college. None exists in
  either repo.

Done 2026-09-26: the decisions are folded into `world.md` §6, tagged [C-2026-09-26],
and the open items are in `backlog.md`. The social page has no section of its own yet.
