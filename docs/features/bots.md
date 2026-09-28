# Bots

**Date:** 2026-09-28
**Status:** In session. Feature fork agreed; technology fork paused until the author
codes the first bot by hand.
**Sources read:** docs/world.md (no section mentions bots), CLAUDE.local.md,
docs/engineering/testing.md, docs/engineering/running.md, game/LaunchOptions.cs,
game/dev/
**Build from:** the two Outcome sections and the T-list of tests. The Q&A is the
record of how they were reached.

Automated bots that drive the client application to test the game. Designed now
because all earlier client-driving code was taken out on 2026-09-28 (commits 48f97cb
and 5a15cd3), and the author wants to start again from a clean design.

The answers are the author's own words, typed in conversation and recorded verbatim or
near it, one question at a time. Model additions are set apart and labelled:
"Consequence noted", "Options offered", "Note".

## Already decided

Decisions in force that this feature must fit, or change on purpose.

- The server is the authority. Clients send input by RPC, the server moves bodies and
  syncs the result. Every client-callable RPC checks the sender. [CLAUDE.local.md,
  Stack]
- Game rules live in plain C# classes that xUnit can test. Logic that can run outside
  the engine is proven with `dotnet test`, not by launching the game.
  [CLAUDE.local.md, Stack and Testing]
- Never drive the game window with OS-level automation (clicks, keys). For how
  something looks, ask the author. [CLAUDE.local.md, Testing]
- Multiplayer behaviour is checked headless: a server (`-- --server`), clients with
  `--profile x --autoconnect`, and `--report-every N`. [CLAUDE.local.md, Testing]
- No C# on engine-called hot paths. [CLAUDE.local.md, Coding style]
- No `async`/`await` in our own code. A package that forces async is a discuss-first
  item. [CLAUDE.local.md, Coding style]
- Prefer proven packages and engine features over home-grown mechanics; say how much
  control each option leaves. [CLAUDE.local.md, What this project is]
- "No bots or dev scenarios for now" (2026-09-28). This session is the author's ask
  that lifts it. [CLAUDE.local.md, Testing]

Built:

- `LaunchOptions`: `--profile`, `--autoconnect`, `--address`, `--report-every`,
  `--screenshot`, `--overview`, `--screenshot-after`, `--creator`, `--garden`,
  `--show-characters`, `--windowed`.
- `game/dev/WorldReport`: a client prints each player body and where it is, every few
  seconds.
- `game/dev/Screenshot`: saves the window to a PNG some seconds in, then quits.
- `game/dev/SceneCheck`: reports what an editor save would lose from each scene.
- `ClientGame` with `--autoconnect` makes a character if there is none, then plays.

Deferred:

- Nothing on bots in `docs/backlog.md`.

### Prior artifacts (called out, not used in this session)

These come from the earlier bot work. The author asked that they not influence this
session. They are listed so that the Consequences can say what to do with each.

- `docs/planning/first-playable.md` line 37: "Bots that wander, collect, recruit and
  join parties; a scripted client driver", in the list of what mmo-game had.
- `docs/planning/first-playable.md` item 8: the status board "fed by a script or by
  bots".
- `docs/planning/status.md`: "Bots and scripts keep the fixed 45 degree view."
- `docs/planning/shipping.md` line 40: a soak run with wander bots.
- `docs/features/world-events-mvp.md` [answer on short timespans, Outcome]: a dev
  scenario "swarm" and a bot activity "check world events".
- `docs/features/usable-phone.md` line 1374: "the collector bot".
- `docs/engineering/performance.md`, `diagnostics.md`, `running.md`, `testing.md`:
  history notes on the removed bots, load test and scenarios.
- `docs/engineering/bot-shots/`: pictures from old soak runs, git-ignored, on this
  machine only.
- `tools/map-gaps/README.md`: "bot runs found eight such gaps".
- `docs/outdated/engineering/soak-runs.md`: mmo-game era.
- The author's memory notes from the earlier work: "bots act through input" (2026-09-18),
  "bot tests set up state", "every action covered", "tests as verification", "watch
  and ask", and "bot rebuild" (the pilot shape of a feature class with name and weight).

## Feature design

### Developer thoughts

> We are going to build automated bots that drive the client application to test
> things.

### Q&A

**F1.** What should a bot run tell you, or catch, that you cannot learn today?

> Answer (2026-09-28): There are too many features and things to test in the game that
> are only discoverable by playing it. With a many bots automatically performing things
> and playing the game mechanics along with a judgement protocol and an overseer we can
> detect things that dont work. Bots can also explore and do things randomly that are
> hard to anticipate by a playtester (like going in and out of Zone1 5 times causes xyz
> bug).
>
> Consequence noted: the answer names four parts. Many bots that play the mechanics. A
> judgement protocol. An overseer. Random exploration, where sequences nobody planned
> (in and out of a zone five times) are the target. The bots are a bug-finding tool, not
> a scripted check of one known path. "Judgement protocol" and "overseer" are new words
> in the project vocabulary and need a definition before the rest can be asked.

**F1a.** Follow-up: What is the overseer, and what does it do?

> Answer (2026-09-28): I think there are 3 main parts the Bot (an entity that goes
> aroudn the game and plays, like a player, a judge which judges a specific bots actions
> and looks for bugs or irregularities, the overseer controls the whole thing and is the
> orchestrator). For example it may start 4 bots doing specific tests. WHen one of the
> bots completes it can start another bot doing another test. The Overseer is the
> orchestrator of the entire bot testing scenario. It can watch a bot finish, collect its
> data and save it.
>
> Consequence noted: three parts.
>
> - **Bot:** plays the game like a player.
> - **Judge:** one per bot. It judges that bot's actions and looks for bugs or
>   irregularities.
> - **Overseer:** the orchestrator of a whole run. It starts bots (four at a time in the
>   example), each on a specific test. When a bot completes, it can start another bot on
>   another test. It watches a bot finish, collects its data and saves it.
>
> A bot's test has an end ("when one of the bots completes"). F1 also wants random
> exploration. Whether random play is one kind of test with an end, or a separate mode,
> is not yet said.

**F1b.** Follow-up: How does a judge know that something did not work?

> Answer (2026-09-28): There will be coded expectaions in a specific jduge. A judge will
> judge a specific bot, at least I think thats how it works. For example. If our Bot is
> designed to play minigameX, the judge will check if the bot executed that minigame and
> played it.
>
> Consequence noted: a judge holds coded expectations, and it is specific to one kind of
> bot. The minigame X bot has a judge that checks the bot started minigame X and played
> it. The author is not certain the pairing is one judge per bot ("at least I think
> thats how it works"). That stays open until the random exploration case below is
> answered, because it is the case where a per-bot expectation is hardest to write.

**F1c.** Follow-up: What judges a bot that explores at random, where nobody coded an
expectation for what it did?

> Answer (2026-09-28): A random exploration bot should be expected to explore. The judge
> will ask, is this bot stuck? That is likely the only judgement made on a random
> explore bot.
>
> Consequence noted: the random explorer's judge has one expectation, that the bot keeps
> exploring and is not stuck. So the F1 example (in and out of Zone1 five times causes a
> bug) is caught by that judge only when the bug leaves the bot stuck. A bug with another
> symptom (an error, a crash, a wrong state) needs some check that is not in the
> explorer's judge.

**F1d.** Follow-up: Does anything outside a bot's own judge report a failure, the same
for every bot?

> Answer (2026-09-28): Undecided here, the fail/pass was the intent of the judge, but I
> think there is some sense of "am I progressing towards my goal" logic inside a bot.
>
> Consequence noted: two things. Pass or fail belongs to the judge. A bot also has its
> own sense of progress towards its goal. The explorer's "is this bot stuck?" [F1c] is
> one form of that sense.
>
> Options offered (the author was undecided):
>
> Facts from the code. The server already records every engine error and warning, with
> file and line, and every `GD.Print`, through `EngineLog` (registered with
> `OS.AddLogger`) into the diagnostics JSON lines file and Grafana
> (docs/engineering/diagnostics.md). An unhandled C# exception in a node callback
> reaches the same logger as an engine error. The client has no such recording today; it
> prints to its console.
>
> 1. **The judge only.** Each judge decides pass or fail from its own expectations.
>    Nothing is shared. A bug that shows only as an error, and does not break the bot's
>    goal, goes unseen.
> 2. **Common checks plus the judge.** Every run also fails on the same few signals,
>    whatever the bot: an engine error or exception in that bot's client, an error on the
>    server while the bot played, a disconnect or a crash. The judge adds the checks that
>    are specific to the bot. The judge still gives the verdict; it takes the common
>    checks as input.
> 3. **Progress only.** Each bot reports whether it is progressing; the judge fails a bot
>    that stops progressing. No error checks.
>
> Recommendation: option 2, with the bot's progress sense feeding the judge as in
> option 3. The F1 example (in and out of Zone1 five times) most likely shows as an
> error or a wrong state, not as a stuck bot, so the explorer catches it only through
> common checks. The server already collects its errors; the client would need the same.
>
> Decision (2026-09-28): 2
>
> Consequence noted: every run has common checks (a client error or exception, a server
> error during the run, a disconnect, a crash) plus the judge's own checks. The judge
> gives the verdict and takes the common checks and the bot's progress sense as input.
> The client needs to record its errors, as the server does through `EngineLog`.

**F2.** What may a bot do that a player cannot?

> Answer (2026-09-28): Nothing, technically it can do things like read client state to
> ask (is the window open?) but from an actions standpoint it must behave as a player.
>
> Consequence noted: a bot may read any client state to decide (is the window open?).
> Every action it takes is one a player could take. That rules out server shortcuts such
> as a teleport or a granted item. It fits the rule against OS-level automation only if
> the bot acts inside the game process, not by clicks and keys from outside
> (CLAUDE.local.md, Testing). How it acts is a technology question.

**F2a.** Follow-up: When a bot's test needs something a player has to earn first, how
does the bot get it?

> Answer (2026-09-28): It must also perform the actions required for that state. So
> prerequisits are a thing we need to include. If you need 500 to test the "feature" you
> must earn $500 first.
>
> Consequence noted: prerequisites are part of the design. A test names what it needs
> ($500), and the bot earns it by playing before the test proper starts. So earning a
> thing is itself something a bot knows how to do, and one test can reuse it as another
> test's prerequisite. A test with a large prerequisite takes as long as a player takes
> to earn it.

**F2b.** Follow-up: Does a bot start each test with a new character, or keep one
character across tests and runs?

> Answer (2026-09-28): This should be configurable. Think long term deep in the
> end-game. I wouldnt want a bot to start from scratch, so we likely want to seed an
> account with unlocks, money, locations, etc.. but we can skip that for now.
>
> Consequence noted: new character or kept character is a setting of the run. Seeding
> an account (unlocks, money, locations) is wanted for end-game tests and deferred.
>
> Note: clashes with [F2a]. Seeding gives an account money without play. F2a says the
> bot performs the actions for the state it needs. The two fit if seeding happens
> outside the bot, before it plays, and the bot itself still acts only as a player [F2].
> Asked below.

**F2c.** Follow-up: Seeding gives an account what F2a says a bot must earn by play.
Which wins?

> Answer (2026-09-28): Ignore for now
>
> Note: left open on purpose, with seeding [F2b]. Until it is decided, F2a holds: a bot
> earns its prerequisites by play.

**F3.** When a run ends, what do you look at?

> Answer (2026-09-28): I look at judges output (its judgement, screenshots provided by
> the bot, logs of the bot actions, whatever snapshot data we get along the way)
>
> Consequence noted: the judge's output is the report, one per bot. It holds the
> judgement, screenshots the bot took, a log of the bot's actions, and snapshots of
> state taken along the way. The overseer collects and saves these [F1a]. A screenshot
> needs a client that draws: `game/dev/Screenshot` says a headless client draws nothing.

**F3a.** Follow-up: Screenshots need a client that draws. Do bots run in a window,
headless, or either?

> Answer (2026-09-28): Either, I think we can run many clients with the graphics showing
> and they can take a screenshot of their specific window.
>
> Consequence noted: a bot runs in a window or headless, as the run chooses. Many
> windowed clients can run at once, and each saves a picture of its own window from
> inside the game, as `game/dev/Screenshot` does today. A headless bot's report has no
> screenshots.

**F4.** How does a run start, and when does it end?

> Answer (2026-09-28): Like literally? I execute some command like run-bots.ps1
>
> Consequence noted: the author starts a run by hand with one script
> (`run-bots.ps1` as a working name). The end of a run is not answered yet.

**F4a.** Follow-up: What ends a run?

> Answer (2026-09-28): I think this again depends on configuration. I would like to be
> able to configure bots (Play the minigame and thats all). Once the minigame is
> completed the run ends. Others are not so simple, the random wander bot, just wanders
> randomly forever. I would suggest a stop script also.
>
> Consequence noted: the run's configuration decides its end. A run of bots with an end
> (play the minigame and that is all) ends when they complete. A run with a wander bot
> goes on until the author stops it with a stop script. A stop must still leave the
> judges' reports saved, since the wander bot's report exists only then.

**F4b.** Follow-up: What does a run's configuration hold?

> Answer (2026-09-28): Not sure yet, needs discussed as we built
>
> Consequence noted: left open on purpose, to be settled while building. The answers so
> far already put these in it: which bots run and how many at once [F1a], a new or a
> kept character [F2b], a window or headless [F3a], and whether a bot ends or goes on
> until stopped [F4a].

**F5.** When you build a new feature, what do you do to give it a bot?

> Answer (2026-09-28): Lets use Minigame as an example. I want to build MinigameX Bot,
> with instructions on how to play the minigame. I expect this will be custom for each
> feature. I would also like other bots to be able to pick up the feature as well. So if
> I have a bot that can randomly pick features and go play them. I guess this means a
> catalog of "Things" bots can do.
>
> Consequence noted: each feature gets custom instructions for how to play it. Those
> instructions go in a catalog of things bots can do, so more than one bot can use them.
> The MinigameX bot plays one entry. Another bot can pick entries at random and play
> them. The prerequisites of F2a (earn $500) are likely entries in the same catalog.
> "Things" is a placeholder; the name is not decided.
>
> Note: F1b pairs a judge with a specific bot. A bot that picks entries at random plays
> many features, so a judge per bot has no single set of expectations for it. Asked
> below.

**F5a.** Follow-up: A bot that picks features at random plays many of them. Whose
expectations judge each one?

> Answer (2026-09-28): That is a good question, I need to think about that.
>
> Options offered (the author was undecided):
>
> Facts from the session. A judge holds coded expectations and judges one bot [F1b].
> Every run also has common checks [F1d]. Each feature has custom play instructions in a
> catalog that many bots use [F5].
>
> 1. **Expectations live with the catalog entry.** An entry holds how to play the
>    feature and what must be true after it (the minigame started and was played). Each
>    bot still has one judge. That judge applies the expectations of every entry its bot
>    played, plus the common checks. The MinigameX bot's judge is then the MinigameX
>    entry's expectations.
> 2. **A judge per kind of bot.** Each judge holds the expectations for every feature
>    its bot may play. A feature played by three kinds of bot has its expectations
>    written three times.
> 3. **The random bot is judged like the explorer.** Common checks and progress only
>    [F1c, F1d]. Feature expectations apply only to a bot made for one feature.
>
> Recommendation: option 1. A feature's expectations are written once, next to its play
> instructions, when the feature is built. Any bot that plays it is judged the same
> way. The pairing of one judge with one bot [F1b] stays.
>
> Decision (2026-09-28): 1
>
> Consequence noted: a catalog entry holds how to play a feature and what must be true
> after it. Each bot has one judge, which applies the expectations of every entry the
> bot played, plus the common checks [F1d]. The explorer has no entry to play, so its
> judge has only the common checks and "is it stuck?" [F1c].

**F6.** Some features need two players, such as a party invite. How do bots test those?

> Answer (2026-09-28): This is part of the overseer work I think. If a bot needs a party.
> The overseer can launch a helper bot, that will go to the primary bot and join his
> party an dplay along (follow mode would be the simple scenario)
>
> Consequence noted: a bot can need another player. The overseer then launches a helper
> bot. The helper goes to the primary bot, joins its party and plays along; following the
> primary is the simple form. So a bot can ask the overseer for something while it runs,
> not only report at the end [F1a]. The helper still acts only as a player [F2].

**F7.** What do you call an entry in the catalog?

> Answer (2026-09-28): undecided
>
> Options offered (the author was undecided):
>
> The name has to read well in three uses: a bot picks one at random [F5], one is a
> prerequisite of another [F2a], and one holds expectations the judge applies [F5a].
> Words already taken: "routine" (NPC routines on the world clock, world.md and
> status.md), "task" and "job" (the terminal's repair jobs; world.md section 5 says
> "task" is not a game word but uses it in the fiction), "skill" (player skills),
> "quest", "action" (one input, too small).
>
> 1. **Activity.** "The bot picks an activity." "Earning $500 is a prerequisite
>    activity." "The activity's expectations." Plain, and it reads well in all three
>    uses. The removed bot code used this word, and world.md uses it loosely in the
>    fiction ("drone activity"), not as a term. In the code, `Activity` is already the
>    .NET tracing span (`System.Diagnostics.Activity`) in every RPC handler, so the type
>    would need a longer name, such as `BotActivity`.
> 2. **Behaviour.** The usual game-AI word. Broad: the wander bot's aimless play is also
>    a behaviour, though it has no catalog entry.
> 3. **Drill.** Unused anywhere. Says "practice for a test". Reads oddly as a
>    prerequisite ("the earn $500 drill").
> 4. **Play**, with the catalog as the **playbook**. Vivid, but "a bot plays a play" is
>    ambiguous with the verb.
>
> Recommendation: option 1, Activity, with the type named `BotActivity`. It is the
> plainest word in all three uses and clashes with no design term. The name of the .NET
> type is the one cost.
>
> Decision (2026-09-28): BotActivity makes sense, another goal here is our code and
> namespace, I dont want bot code leaking into regular game code.
>
> Consequence noted: an entry in the catalog is a `BotActivity`. Bot code stays apart
> from game code, in its own place and namespace. A bot reads client state [F2], so bot
> code depends on game code. The reverse must not happen: game code does not know bots
> exist. Where the line runs is a technology question.

### Outcome

Agreed 2026-09-28 ("That looks good.").

- Bots play the game's mechanics to find what does not work, including sequences no
  playtester would try. [F1]
- Three parts: the **Bot** plays like a player, the **Judge** judges one bot, the
  **Overseer** orchestrates the run. [F1a]
- The Overseer starts several bots at once, each on a specific test, and starts another
  when one completes. It watches a bot finish, collects its data and saves it. [F1a]
- A `BotActivity` is one entry in a catalog of things bots can do. It holds how to play
  one feature and what must be true after it. Each feature gets its own. [F5, F5a, F7]
- A bot can be made for one `BotActivity` (the MinigameX bot), or pick them at random.
  [F5]
- Each bot has one Judge. It applies the expectations of every `BotActivity` its bot
  played, the common checks, and the bot's own sense of progress. [F1b, F1d, F5a]
- Common checks, for every bot: an error or exception in its client, an error on the
  server while it played, a disconnect, a crash. [F1d]
- The explorer (random wander) bot has no `BotActivity`. Its Judge asks whether it is
  stuck, with the common checks. It runs until stopped. [F1c, F4a]
- A bot may read any client state. It acts only as a player could. [F2]
- A bot earns its prerequisites by play; a prerequisite is another `BotActivity`. [F2a,
  F5]
- A new or a kept character is a setting of the run. [F2b]
- When a feature needs another player, the Overseer launches a helper bot. It goes to
  the primary bot, joins its party and plays along; following is the simple form. [F6]
- The report, per bot: the Judge's verdict, screenshots, a log of the bot's actions,
  snapshots taken along the way. [F3]
- A bot runs in a window or headless. A windowed bot saves pictures of its own window
  from inside the game. [F3a]
- A script starts a run. A run ends when its bots complete, or when the author runs a
  stop script. A stop still saves the reports. [F4, F4a]
- Bot code stays out of game code, in its own place and namespace. [F7]

Deferred:

- Seeding an account with unlocks, money and locations for end-game tests, and whether
  seeding overrides "earn by play". Left open by the author. [F2b, F2c]
- What a run's configuration holds, to be settled while building. [F4b]
- Helper bot forms beyond following. [F6]

## Technology design

Paused 2026-09-28, before T1. The author codes the first bot by hand, so the pieces end
up where the author wants them. This fork resumes from that code: "Facts held in mind"
is written from it, and the questions start there.

### Developer thoughts

> Do not start yet. I want to code 1 bot by hand so things end up where I want them

### Q&A

### Outcome

## Gatekeeping

## Consequences
