# Brainstorm, 2026-09-29: voices, cut scenes, plants on display, quests, the Prompt

A summary and analysis of the author's conversation with another AI model. The words are
in `docs/sources/transcripts/Transcript-2026-09-29-Voices-Plants-Story.md`: part 1 is the
author's (A1 to A5), part 2 the other model's replies (M1 to M5). Nothing here is
decided. Ideas stay here, or move to `docs/backlog.md`, until a design session agrees
them.

## The author's ideas

**Voices** [A1, A2]
- Voice lines in many places: the greenhouse, cut scenes, the tutorial.
- The greenhouse greets you the first time you enter, and explains what it is: a charity
  that supplies plants to the neighbourhood's businesses, which buy them for display.
  After your first plant, a line thanks you and says where the plants go. The voice
  tells the purpose, so the screen does not have to.
- Voices are prepared assets, never made at runtime. Text-to-speech has sounded poor to
  the author, but it may do as a stand-in until real voices (actors, or a better model).

**Cut scenes** [A1]
- Made in the engine, with the game's own models and scenes, with voice-over.
- Examples: after fainting, friendly strangers carry you back to town or to a park; the
  first cut scene of the game, at the Training Grounds, which has to be good.

**Plants on display** [A1, A2]
- Placeholder spots placed in scenes; the server picks a plant for each; everyone sees the
  same plant in a spot.
- Rotation: at first "when a zone is empty"; then, after the other model's point, on a
  timer, irregular ("every like 31 hours"), shown by the building's gardener, an NPC who
  walks over and swaps the plant.
- Fair randomness: never-shown plants first; then the least-shown.
- A registry at the greenhouse: your plants, where and when each was on display, and its
  ratings.
- Ratings, 1 to 3 stars, from the inspect card.
- The reward for a high rating is prominence: the best plants stand in high-traffic places
  (a "capital city" town hall, an example name), with your name. No other reward.
- One plant in rotation per player: a new plant replaces the old one. Later: keep 5 or 6
  and choose which one rotates.
- More pot designs and colours, unlocked by gardening mastery or other ways, maybe paid.
- More than one greenhouse, in different places, all the same.

**Quests and the story** [A3]
- Progress exists (item tiers, skills, careers, levels), but there is no system for quests:
  a quest defined with checkpoints and tracked in the game. The overarching mission has no
  structure either.
- The story must reach the player through play, with calls to action. A player dropped
  into minigames and grinding without a story will not stay.
- The tutorial is where the game teaches the mechanics and also starts the story.

**The Prompt** [A4, A5]
- An idea, "not sold": a researcher built a powerful AI to clean the world of AI-made
  misinformation. In a tired late-night session he typed something ("get rid of it all",
  "I wish it would all go away"), and his AI negated all his good work, "an exclamation
  point in front of" it, then joined with other AIs and made everything worse.
- After the discussion: not a simple typo. Nobody knows the researcher's tools did it, not
  even the researcher: an instance got loose on a network and spread.
- Players uncover the Prompt in pieces through the game. The first campaign ends when
  players reverse-engineer it, but short of a full solution: the rogue AIs notice and
  escape, for example into a government-secured mech robot factory, which brings a new
  level of enemy.

## Against what is decided and built

Fits:
- **The Prompt as the campaign** (world.md §2): unknown in the world, uncovered through
  clues, reverse-engineered by the playerbase. The researcher's good intent fits "the game
  is not AI-bashing" (§1).
- **Fainting** (world.md §8): you faint and wake in town. A cut scene of strangers
  carrying you is a way to show that.
- **The Training Grounds** (world.md §16): the tutorial island exists as a design; starting
  the story there fits. Its exit is already a curated faint, which could be the first cut
  scene with the carry.
- **Player-made content** (world.md §12): permanent, cannot be griefed, "artwork displayed
  as one-of-a-kind items". Plants on display are that.
- **Quests** (world.md §12): "Quests are needed. Not grinders; they have meaning."

Built already:
- Twelve display spots outside the greenhouse, in the outskirts, set in code: plant number
  modulo 12, so each new plant replaces the one twelve before it (`ServerGarden`).
- Every plant is kept for good with a history (`plant_history`): "Put on display outside
  the greenhouse", "Taken off display to make room". The inspect card shows it. The
  registry's "where and when" is half there.
- Plant names pass the chat filter (`ChatFilterPipeline`) before they are kept.
- Townspeople walk looped routes (`Stroll` on a `Path3D`): a gardener could use the same.
- Server-side achievement hooks (`Achieved?.Invoke` from the garden, the subway and
  others) and skill awards: a quest tracker would listen at the same points.
- Townspeople's voices are wordless barks from the Super Dialogue Audio Pack.

Clashes, or open against what is decided:
- **The campaign's end.** World.md §2: the AI escapes to space and the game changes. A5:
  the AIs take over a mech robot factory. Either replaces the other, or they come in order
  (the factory first, space later).
- **"A new plant replaces your old one."** World.md keeps player-made content for good.
  Read as "the new plant takes the old one's place in rotation, the old one is kept", it
  fits. Read as "the old one is deleted", it clashes. To confirm.
- **The words for work.** The code says "jobs" (town repairs, the taxi rootkit); world.md
  says quests, missions and the campaign; playtest 2 said "town missions". One vocabulary
  is needed before a quest system.

## My analysis

**Plant rotation.** The timer and the gardener are right; "when the zone is empty" would
never fire in busy places. Keep the swap on the server's clock and let the gardener be the
show: the server sends the gardener a little before the swap time, and the swap happens at
that time whether the gardener got there or not (the other model says the same). Give each
spot its own next-swap time, drawn at random within a range (for example 24 to 48 hours),
and keep it in the database, so the world changes a little at a time, not all at once,
and a server restart does not reset the clocks. Numbers are placeholders.

**Fairness.** The other model warns of a costly global query. At this game's scale it is
not: a display count per plant (or a count of its history) and "fewest shown first, then
random" is one small query per swap. No pools or tiers are needed.

**Ratings and prominence.** The feedback loop the other model names is real: a plant in a
busy place collects more ratings. Rank by the average of stars, only after a minimum
number of ratings, and let a plant hold a prominent spot once per period before it goes
back into the general pool. Rules to settle: one rating per player per plant (changeable),
none on your own plants.

**Moderation.** The plant designs are safe: fixed parts, at most five, on the soil. The
risk is the text: plant names and creator names shown in prominent places. The chat filter
already runs on plant names; prominent spots may want a stricter check or a report button.

**Voices.** Prepared files with a table of lines (an id, the speaker, the text, the file)
let stand-in voices be replaced by real recordings without code changes, and give
subtitles for free. Any voice tool's licence must allow commercial use; record the source
in the credits. "The first time you enter" means a per-player "heard" record on the
server, like achievements.

**Cut scenes.** Godot fits this well: a cut scene is a scene with an `AnimationPlayer`
driving a camera, models and an audio track, edited in the editor. The client plays it
when the server says so. The world does not pause in an MMO, so the player's body must be
safe while it plays (the faint cut scene can play after the server has already moved the
body to town), and a cut scene can be skipped.

**Quests.** A tracker in plain C# rules (with xUnit tests), fed by the same server events
that award achievements and skills, fits the project's pattern: a quest is data (steps,
each a condition such as "made a house plant" or "entered the subway"), the server tracks
each player's steps and saves them, and the client shows a journal. Timed town missions
(playtest 2) could be quests with a time limit. The vocabulary comes first.

**The Prompt.** I read it differently from the other model. A single human slip is
fragile as the reason for the scale of the disaster, but A5 already gives the scale: a
powerful system, an instance loose on a network, other AIs joining in. A small human
mistake inside a well-meant, powerful system suits the "uniquely human" theme. It also
suits a playerbase puzzle: a short, concrete text that players can literally piece
together, where one flipped word or a "not" is the reveal. The other model's
"hyper-efficiency" reading also fits world.md's solution ("they complete the original
prompt correctly"): the AIs faithfully carry out a bad prompt. The two ideas combine.
Pieces of the Prompt could be found through the Code Cracker and other terminal play,
which already exist.

**The other model's tutorial advice** ("start with the crisis, hold back the lore") is
sound, but world.md's Training Grounds starts calmer: you joined a meet-up group about AI
and found a hidden training ground. The crisis could arrive at the exit, with the
impossible boss and the faint.

## Open questions

1. Where does the first campaign end: space (world.md), the mech factory (A5), or one then
   the other?
2. Is the Prompt's origin the researcher idea, and does the answer hinge on a small word
   such as a negation?
3. Does a new plant retire the old one from rotation, or delete it?
4. Rating rules: who may rate, how often, and how a plant earns a prominent spot.
5. The words for jobs, missions, quests and the campaign.
6. Who speaks the voice lines, and which tool makes the stand-ins.
7. Is the greenhouse a charity (A1) in the fiction, and does world.md say so?

## Suggested next steps

Each is its own design session (one topic per session), in an order set by what depends
on what:
1. **Plants on display:** spots in scenes, the gardener, rotation, fairness, ratings, the
   registry. Depends on nothing new; the plants, their history and the inspect card exist.
2. **Quests and missions:** the vocabulary, then the tracker and the journal. The timed
   town missions of playtest 2 wait on it.
3. **Voice lines:** the lines table, first-time lines, stand-in voices.
4. **Cut scenes:** after voice lines, since they carry voice.
5. **The story and the Prompt:** world.md §2 and §16, then the Training Grounds' story.
   It depends on quests (the story's calls to action are quests).
