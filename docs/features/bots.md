# Bots

**Date:** 2026-09-28
**Status:** Agreed, and built in part (branch `ger/bot-rebuild`). See "Decided in the
author's absence" for the choices made while the author was away.
**Sources read:** docs/world.md (no section mentions bots), CLAUDE.local.md,
docs/engineering/testing.md, docs/engineering/running.md, game/LaunchOptions.cs,
game/dev/; at the close only, the removed bot code and docs (commits 48f97cb, 5a15cd3)
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

### Developer thoughts

> Do not start yet. I want to code 1 bot by hand so things end up where I want them
>
> Note: "do not start yet" was about code, not about this fork. The author then said:
> "Ok you were going to go for the technology fork, that makes sense to do". The fork
> goes on; the author codes the first bot by hand after it.

> Mapping UI and HUD actions and a way to concretely say "Click the X Button", I dont
> know how possible this is today. I do not want them to be fragile scripts either. So
> if this means we need to redo a piece of architecture in the regular game app then it
> should be considered. I would like some sort of fluentAPI so that I can easily build
> test scenarios like bot.goto(...).goto(...).play(minigame). and even more refined like
> bot.wander(10).stepTo(namedThing).openUI(screen).click(button)... No other real
> thoughts on architecture.
>
> Consequence noted:
>
> - A bot names UI and HUD controls concretely ("click the X button"), and a rename must
>   not silently break it. Reshaping game UI code for that is allowed.
> - Bot steps chain in a fluent API: `goto`, `play`, `wander`, `stepTo`, `openUI`,
>   `click`.
> - Convention flag: no `async`/`await` (CLAUDE.local.md). Each step takes frames or
>   seconds (a walk, a panel opening). So the chain builds a list of steps, and the bot
>   runs the list one step at a time from its frame callback, polling each step until it
>   is done. Nothing awaits.
> - Convention flag, against [F7]: a game change made for bots must be one the game can
>   own on its own terms (named controls, a list of open screens), with no mention of
>   bots in game code.

### Facts held in mind

What the code does today for the pieces a bot touches, as of 2026-09-28. All of it is
live in play unless marked.

- **One project, two roles.** `Main` reads `LaunchOptions`. As the server it builds
  `World` and `ServerGame`; as a client it builds `ClientGame`. Both build the RPC
  nodes (`Networks`) at the same paths. A bot is a client process.
- **How a player walks.** `Player.ReadInput` reads Godot input actions
  (`move_forward`, `move_back`, `turn_left`, `turn_right`, `strafe_left`,
  `strafe_right`, `jump`) with `Input.GetAxis`, and sends `SendWalk`, `SendStop` and
  `SendJump` on the session's `Network`. The keys are ignored while a text field has
  focus or a full screen is open (`ChaseCamera.ScreenGroup`).
- **How a player uses a thing.** `InteractionFinder` finds the nearest `Interactable`
  in reach, shows its prompt, and on the `interact` action raises `UseRequested`. The
  server checks reach again.
- **Menus and panels.** `ClientGame._UnhandledInput` opens them on actions: `phone`,
  `inventory`, `map`, `social`, `skills`, `emp`, `chat`, `ui_cancel`. Panels are scenes
  under `game/ui/` with buttons. `ClientGame` holds which panel is open in private
  fields (`_shop`, `_workbench`, `_map`, ...); a few things are findable by group
  (`Player.LocalGroup`, `ChaseCamera.ScreenGroup`, `Hud.ActionGroupPrefix`).
- **Acting as a player from inside the process.** Everything above reads Godot input
  actions or button signals. Godot can press an action (`Input.ActionPress`,
  `Input.ActionRelease`) or feed an input event (`Input.ParseInputEvent`) inside the
  game process. That is not OS-level automation (CLAUDE.local.md). Nothing uses it
  today.
- **Who the bot is.** `--profile name` keeps a license key in
  `user://profiles/<name>/`, so the same profile is the same player on every launch.
  `--profile fresh` is a new player on every launch. `--autoconnect` skips the menu,
  makes a character if there is none, and plays. So "new or kept character" [F2b]
  exists today.
- **Errors.** The server copies every `GD.Print` and every engine error and warning,
  with file and line, into its log through `EngineLog` (`OS.AddLogger`); the log goes to
  a JSON lines file (`--diagnostics path`) and to Grafana. The client has no logger: it
  prints to its console only.
- **Screenshots.** `game/dev/Screenshot` saves the client's own viewport to a PNG
  (`GetViewport().GetTexture().GetImage()`), then quits. It needs a window.
  `game/dev/WorldReport` prints the bodies a client sees. Both are dev tools reached by
  launch flags.
- **Start and stop.** `scripts/server-up.ps1` starts a headless server;
  `scripts/client-up.ps1` builds and starts a client. `scripts/server-stop.ps1` writes a
  stop file the server polls (`StopSignals`), so the server saves before it quits. A
  kill skips the save.
- **Where code lives.** The Godot project compiles every `.cs` file under the root
  except `src/` and `tests/`, into one assembly, namespace `MmoGame3d.*`. Plain C#
  libraries are separate projects: `src/Rules`, `src/Data`, `src/Diagnostics`, tested by
  xUnit in `tests/Tests`. Dev tools sit in `game/dev/` (`MmoGame3d.Dev`).
- **Nothing bot-shaped exists.** No bot, judge, overseer, catalog or run script.

### Q&A

**T1.** How does a bot name a button, and how does it press it, so that the step is not
fragile? (Raised by the author's thoughts: "I dont know how possible this is today".)

> Options offered (the author did not know what the code allows):
>
> Facts from the code. Panel scenes mark their controls with unique names, and the
> script finds them that way (`ShopPanel` uses `%Close`, `%Title`, `%Wallet`,
> `%Offers`). Some controls are built in code: the HUD action bar (`Hud.ShowActions`,
> each button in a group `hud_action_<action>`) and list rows such as the shop's
> offers. Which panel is open is held in about twelve private fields of `ClientGame`
> (`_shop`, `_workbench`, `_map`, ...), so nothing outside `ClientGame` can ask "is the
> shop open?". It is possible today; the question is how robust.
>
> How the bot names a control:
>
> 1. **Scene unique names, as strings.** The bot finds `%Close` inside the open shop.
>    No game change. A rename in the editor breaks the bot when it runs, not when it
>    builds.
> 2. **Names owned by each screen, plus a list of open screens.** Each screen class
>    declares its controls as constants (`ShopPanel.Close`), and the game keeps one
>    list of which screens are open, in place of `ClientGame`'s private fields. The bot
>    writes `OpenUI(Screen.Shop).Click(ShopPanel.Close)`. A renamed constant breaks the
>    build, and the scene check can prove that every constant exists in its scene. The
>    game gains a plain answer to "which screens are open", with no bot in it [F7].
> 3. **Groups on controls**, as the HUD action bar does. Works for controls built in
>    code; a group name is a string, so it is as fragile as option 1.
>
> How the bot presses it:
>
> a. **Emit the button's `Pressed` signal.** Simple. It passes even when the button is
>    hidden, disabled or covered by another panel, where a player could not press it.
> b. **A real click at the control's centre**, fed through the viewport
>    (`Input.ParseInputEvent`). The press goes the way a player's does, so a covered or
>    disabled button fails the step, which is a bug a judge wants [F1d]. Whether this
>    works in a headless client is to be proven with one test.
>
> Recommendation: 2 with b. It is the least fragile, it matches "acts only as a player"
> [F2], and it replaces `ClientGame`'s private panel fields with one list the game can
> use too. Rows built in code (shop offers) need a way to be named by their content;
> that is the next question.
>
> Decision (2026-09-28): 2b makes sense there
>
> Consequence noted: each screen class declares its controls as constants, the game
> keeps one list of open screens in place of `ClientGame`'s private panel fields, and
> the scene check proves each constant exists in its scene. A bot presses a control with
> a real click at its centre, through the viewport. One test proves the click works in a
> headless client before the rest is built on it.

**T2.** How does a bot name a control that code builds while the game runs, such as a
row in the shop's offers?

> Answer (2026-09-28): We should not do that. The shop should offer N things to buy.
> Those things should be identifiable (Item (and item descriptions), price, etc..)
> Should all be specifically identifiable. Maybe I dont understand the shop well
> enough.
>
> Note, on the shop as built: the offers are data in `src/Rules/Shops/Shops.cs`, a list
> of `ShopOffer` per shop (item type, tier, price; for example Battery, Standard, $8).
> `ShopPanel.ShowDollars` builds one row per offer: a name label, a price label and a
> Buy button. The rows have no names of their own, and the Buy button reports only the
> offer's index. So the identity exists in the data, but the row on screen does not
> carry it.
>
> Consequence noted: a control built from data carries the identity of what it shows.
> A shop row is identified by its offer (item type and tier), not by its position or
> its text. The bot says which offer, for example buy the Standard Battery, and the
> screen finds that row. This is the same rule as T1: the screen owns how its controls
> are named.

**T3.** How does a bot find its way to a named thing, such as a terminal in another
zone?

> Answer (2026-09-28): We need a concept of a navigation graph I think. so Zone A
> conencts to Zone B via door Z. If bot needs to move it can reference the graph and
> find the door and then it can proceed to the door. Same for terminals I think. Each
> zone gets an entity graph with locations
>
> Consequence noted: two levels.
>
> - **Between zones:** a graph of zones joined by doors. The bot finds the doors on the
>   way to the target zone and walks to each in turn. Each `Door` already exports
>   `TargetZone` and `TargetArrival`, so the graph can be read from the zone scenes, not
>   written by hand. Taxis (`game/taxis`) and the subway (`game/subway`) also move a
>   player between places; whether they are edges in the graph is not yet said.
> - **Inside a zone:** the named things and where they are (terminals, doors,
>   interactables). Interactables already sit under each zone's `Interactables` node
>   with a name that client and server share.
>
> The graph says where a thing is. It does not say how to walk there around buildings.
> No zone has a navigation mesh today (no `NavigationRegion3D` in any scene).

**T3a.** Follow-up: Inside one zone, how does a bot walk from where it stands to a
location without being blocked by buildings and props?

> Answer (2026-09-28): I think this should be an isolated procedure. I think we might
> need to inspect the map at startup and provide a navigation path? The procedure idea
> NavigateWithinZone(goal) does the work of bot.move(...)... On the navigation path
> idea, can we look at a map and identify safe paths and build a walkable path along the
> way. I think we also want a way to say "Just use a pathfinding algorithm and dont
> follow the Known Paths". So maybe thats navigation modes
>
> Consequence noted: walking inside a zone is one isolated procedure,
> `NavigateWithinZone(goal)`, which the fluent steps (`goto`, `stepTo`, `move`) use. The
> map is inspected at startup. There are navigation modes: follow the Known Paths, or
> use a pathfinding algorithm and ignore them. The bot walks the path by pressing the
> walk and turn actions, as a player would [F2].

**T3b.** (author's question) Can we look at a map and identify safe paths, and build a
walkable path along the way?

> Options offered (asked for):
>
> Yes, with an engine feature. Godot's navigation server can bake a navigation mesh at
> run time from a zone's collision shapes (`NavigationServer3D` with
> `NavigationMeshSourceGeometryData3D`), then answer "the path from here to there"
> (`NavigationServer3D.MapGetPath`). The mesh is the walkable ground minus buildings and
> props, shrunk by the body's radius. Baking in the bot's client at start keeps it out
> of game code [F7]. Whether the client holds each zone's collision, as the server does,
> is to be proven with one test. That is the "pathfinding" mode.
>
> A Known Path differs from pathfinding only if something chose it. Where that choice
> comes from:
>
> 1. **Placed by hand in each zone scene.** Waypoints (`Marker3D`) joined into routes
>    under a node such as `Paths`, read into Godot's `AStar3D`. Full control over where
>    bots walk (the pavement, not the road). A person keeps them right when the map
>    changes. NPC routines, planned on the server (status.md), may walk the same routes
>    later, which makes them game content, not bot code.
> 2. **Derived from the baked mesh.** The same mesh with lower travel costs on chosen
>    ground (Godot's region travel cost). Nothing to place, but it is still pathfinding
>    with a preference, not a known path.
>
> Recommendation: pathfinding mode from a mesh baked at bot start, and Known Paths
> placed by hand in the zone scenes (option 1). Pathfinding alone reaches any place.
> Known Paths add walks that look like a player's and that can be checked by eye.
>
> Decision (2026-09-28): Quick Note. All class/function/argument suggestions are for
> demosntration only and example. Do not take those as literal directions. it helps me
> to think and demosntrate by typing out a representative function name but those are
> not instructions or decisions...... No waypoints for now, I think the baked mesh as
> "Known Paths" a baked mesh with lower travel costs as "idk a good name". I still kinda
> want something that does a, not poor, but a rudamentary path finding.
>
> Note: names the author types in answers (`goto`, `stepTo`, `openUI`, `click`,
> `NavigateWithinZone`, `bot.move`) show an idea; they are not decided names. Where this
> file wrote them in code font as if decided, read them as examples. `BotActivity` is
> the one name decided on purpose [F7].
>
> Consequence noted: no waypoints placed by hand, for now. The navigation modes:
>
> - **Known Paths:** a path over the navigation mesh baked at start.
> - **A second mode, not named yet:** the same mesh with lower travel costs on chosen
>   ground.
> - **A rudimentary pathfinding mode**, "not poor", wanted as well. What it is for is
>   asked below.

**T3c.** Follow-up: What should the rudimentary pathfinding do that a path over the
baked mesh does not?

> Answer (2026-09-28): What it does, finds bugs in maps where people get stuck, cant
> escape from (because its not super intelligent). So we can say things like, if you're
> in areaT of this zone, its really hard to get to areaZ unless you know the path. We
> can do things like time how long things take and observe
>
> Consequence noted: the rudimentary mode walks like a player who does not know the
> map. It is meant to fail where a player would: traps in the map, places a body cannot
> escape, areas that are hard to reach from other areas unless the path is known. The
> bot times how long a walk takes, and the report says where it got stuck and how long
> each walk took [F3]. Its judge uses "is it stuck?" [F1c]. The mesh modes stay for
> bots that must reach a place to test something else.

**T4.** Where does the Overseer run?

> Answer (2026-09-28): Good question, I dont know. Somewhere it can read both the server
> and each client and judge. It runs on my computer, depends on how you frame that
> question. It runs where the clients run so it can kill a client and start another one
> and also output data if needed about the current execution
>
> Consequence noted: the Overseer runs on the author's computer, where the clients run.
> It starts and kills client processes, reads the server and every client, runs the
> judging, and shows data about the run while it goes on.
>
> Options offered (the author did not know):
>
> Facts from the code. Each client is its own Godot process, and the server is another.
> Scripts start them today (`client-up.ps1`, `server-up.ps1`). The server writes its log
> to a JSON lines file (`--diagnostics path`); a client prints only to its console. The
> server stops cleanly on a stop file (`server-stop.ps1`).
>
> 1. **A PowerShell script.** The run script [F4] is itself the Overseer: it starts and
>    kills processes and reads their files. Little to build. Judging in PowerShell
>    cannot be tested with xUnit, and taking a helper request while a bot runs [F6] is
>    awkward.
> 2. **A C# console program, outside Godot.** Its own small .NET project. It starts the
>    game exe with launch options for each bot, kills and replaces them, reads each
>    bot's output and the server's log file, runs the judges as plain C# classes that
>    xUnit tests, prints the run's state, and saves the reports. The run script builds
>    and starts it. No engine in it, and it can loop and poll without `async`.
> 3. **A third role of the game itself** (as the project is the server with
>    `--server`). The Overseer is a Godot process with a frame loop, and it starts and
>    kills clients with `OS.CreateProcess` and `OS.Kill`. It could talk to bots through
>    Godot's own networking. The engine is loaded for a job that draws nothing and plays
>    nothing.
>
> Recommendation: option 2. The judging is the part that grows with every feature, and
> as plain C# it is tested like the game rules. It keeps the most control with the
> fewest moving parts, and it keeps bot concerns out of the game process except the bot
> itself [F7].
>
> Decision (2026-09-28): 2 yes. I also want to note at this time I dont need unit tests
> for this> I expec them to break all the time and need quick changes. testes will slow
> all this done.
>
> Consequence noted: the Overseer is a C# console program outside Godot, in its own
> .NET project. No unit tests for the bot code, the judges or the Overseer, since they
> are expected to change often. My reason for option 2 that rested on xUnit
> ("tested like the game rules") falls away; the choice stands on control and on keeping
> bot concerns out of the game process.
>
> Note: this departs on purpose from "prove server and game logic with tests"
> (CLAUDE.local.md, Testing), for bot code only. Game code changed for bots (the screen
> list, named controls [T1]) is game code and keeps the usual rule.

**T5.** How do a bot and the Overseer talk while the bot runs?

> Answer (2026-09-28): I dont know about this. Direct communication channel?
>
> Options offered (the author did not know):
>
> What has to pass, from the answers so far:
>
> - Bot to Overseer: the log of the bot's actions, its progress, snapshots and
>   screenshots [F3], its client's errors [F1d], "I need a helper" [F6], "I am done"
>   [F1a].
> - Overseer to bot: what to play (known at launch), who to join for a helper [F6]
>   (known at launch of the helper), "stop now" [F4a].
>
> So almost everything flows from bot to Overseer, and the Overseer's side is mostly
> known when it starts the process.
>
> 1. **Files, one folder per bot.** The bot appends one line per event to a file
>    (JSON lines) and saves screenshots beside it. The Overseer reads new lines each
>    time round its loop. The Overseer tells a bot what to play by launch options, and
>    tells it to stop with a stop file the bot checks, as the server does today
>    (`StopSignals`). A crash leaves everything written up to that moment, and the
>    Overseer sees the process end without "done". The folder is also the report the
>    author reads [F3].
> 2. **The bot's console output.** The Overseer reads each bot's standard output line by
>    line. Godot's own prints and errors arrive there too. The way back is poor: a Godot
>    process can only read its input with a call that blocks.
> 3. **A local network socket.** A direct two-way channel: the bot connects to the
>    Overseer on localhost at start (Godot's `StreamPeerTcp`, polled each frame; no
>    `async`). Fastest, and the Overseer can send at any time. More code on both sides,
>    and a crash loses what was not yet sent.
>
> Client errors in every option: the bot adds its own logger to the engine
> (`OS.AddLogger`, as `EngineLog` does on the server) and writes each error as an event.
> That is bot code in the client, not game code [F7].
>
> Recommendation: option 1. Nothing the bots need is urgent to the millisecond, the
> Overseer's side is known at launch, a crash keeps its evidence, and the author can
> open the files by hand. A socket can be added later if the Overseer ever needs to
> steer a bot mid-run.
>
> Decision: pending the author's word.

**T5a.** (author's question) Can we monitor in game chat? Would that be easier?

> Options offered (asked for):
>
> It is possible, and it is not easier. Facts from the code:
>
> - The Overseer is a console program outside Godot [T4]. To read chat it must either
>   be a game client itself (connect, log in, keep a character), or read chat out of the
>   server's log, where every RPC the server receives is recorded with its arguments.
> - Chat has limits made for players: at most 5 lines in any 10 seconds per player
>   (`ChatRateLimit`), 120 characters a line (`CleanFilter.MaxLength`), and a filter
>   pipeline. A bot's action log would hit the rate limit at once.
> - Screenshots and snapshots cannot go through chat.
> - Chat is a feature the bots test. If chat breaks, the reports break with it, at the
>   moment they are needed most.
> - Other players, and other bots, see the messages.
>
> Chat is a good thing for a bot to use as a player (a `BotActivity` that sends a line
> and checks it arrives). It is a poor channel for the bots' own reports. Option 1 of T5
> stays the recommendation.
>
> Note: the author found T5 unclear ("You asked about communication, not screenshots").
> T5 meant everything that passes between a bot and the Overseer. The Overseer collects
> and saves each bot's data [F1a], so the screenshots and the action log pass too, not
> only short messages. Restated in chat as two parts: the short messages (done, need a
> helper, stop, an error) and the report data (action log, screenshots, snapshots). One
> channel can carry both, or each can have its own.
>
> Decision (2026-09-28): Yes option 1. each bot execution should be isolated and stand
> alone.
>
> Consequence noted: files, one folder per bot execution. The bot appends its events
> (actions, progress, errors, helper requests, done) as JSON lines and saves its
> screenshots and snapshots in the same folder. The Overseer gives a bot its task as
> launch options and stops it with a stop file. Each execution is isolated and stands
> alone: its folder holds everything about it, and nothing in it depends on another
> bot's execution. Reading, for the author to correct: since a bot gets everything at
> launch and writes only to its folder, a single bot can also be started by hand
> without the Overseer.

**T6.** How does a bot start inside a client without game code knowing that bots exist?

> Answer (2026-09-28): I dont know, we need to explore options for that.
>
> Options offered (the author did not know):
>
> Facts from the code. `project.godot` sets `run/main_scene` to `res://game/Main.tscn`.
> `Main.tscn` is a `Main` node with the RPC nodes as children; its `_Ready` reads
> `LaunchOptions` and builds `ClientGame` or the server. `LaunchOptions.Parse` ignores
> options it does not know, so a bot's own options can ride on the same command line.
> Godot compiles every `.cs` under the project into one assembly, and a node script must
> be in that assembly, so bot code that runs inside the client lives in the game project
> (for example in its own folder, `game/bots/`, and namespace).
>
> 1. **A bot scene named on the command line.** Godot runs a scene given as an argument
>    in place of the main scene. The bot scene inherits `Main.tscn`, so its root is
>    still `Main` and every RPC path matches the server's, and it adds one child: the
>    bot. The Overseer starts
>    `Godot --path <project> res://game/bots/<bot scene>.tscn -- --profile x ...`.
>    `Main` and `ClientGame` do not change. For a release build, the export can leave
>    out the bot folder and its scene, so no bot code ships. To prove with one run: the
>    extra child under `Main` disturbs nothing.
> 2. **A launch option in `Main`.** `Main` sees `--bot` and adds the bot node. One
>    line, the simplest, but it is game code that names bots [F7], and bot code ships in
>    every build.
> 3. **An autoload.** A node Godot adds at every start; it looks at the command line and
>    does nothing without a bot option. Game code does not name it, but it runs in every
>    player's game, and it is in `project.godot` for everyone.
>
> Whatever the start, a rule that game code never refers to the bot namespace can be
> enforced later by an analyzer like those in `src/Analyzers`, if the author wants one.
>
> Recommendation: option 1. Game code stays as it is, the node paths stay the same as
> the server's, and the bot code can be left out of a release build.
>
> Decision (2026-09-28): Lets go with 1, but lets make a not to look at the old code and
> see how that worked (wait until the end for that). At the end I would like a compare
> and contrast with what the old code did and our decisions. (also to dientify existing
> features of the bot programs that we did not account for)
>
> Consequence noted: a bot scene that inherits `Main.tscn` and adds the bot as one child,
> named on the command line. `Main` and `ClientGame` do not change. The release export
> leaves out the bot folder.
>
> Note: at the close of the session, and not before, read the removed bot code (git
> history, commit 48f97cb and its parent) and add a section "Compared with the removed
> bot code": how it worked, how it differs from these decisions, and features it had
> that this design did not account for. Until then the removed code stays unread, as the
> author asked at the start.

**T7.** A `BotActivity`'s expectations read client state (was the minigame played?),
and the Overseer runs outside the client [T4]. Where are they checked?

> Answer (2026-09-28): I think the checks are done by the judge right? That component is
> not in this question. But I dont know the exact technical answer to this. I imagine
> its with game output somewhere. We need to check was the minigame on screen and the
> bot clikcing things, maybe we query the DB?
>
> Consequence noted: the judge does the checks. It checks from evidence the game puts
> out: was the minigame on screen, did the bot click things, and perhaps what the
> database holds after.
>
> Options offered (the author did not know the technical answer):
>
> Evidence the judge can read from the Overseer, with no change to game code:
>
> - **The bot's events file** [T5]. The bot reads client state and writes what it saw
>   and did: "the minigame screen opened", "clicked Start", "the score showed 40". These
>   are observations, not verdicts.
> - **The server's log.** Every RPC the server received, with its arguments, and every
>   server error (`EngineLog`, the JSON lines file). It shows what reached the server,
>   whatever the bot believed.
> - **The database.** What lasted: the score saved, the money moved. The Overseer can
>   read it through the stores in `src/Data`, which are plain C#.
> - **Screenshots** are for the author to look at, not for the judge.
>
> Where the expectation code lives:
>
> 1. **In the Overseer, with the judge.** The bot only observes and records. Each
>    `BotActivity` has two halves under one name: how to play it (in the client) and what
>    must be true after (in the Overseer). The judge does not take the bot's word: the
>    server log and the database confirm it. The two halves can still sit in one folder:
>    the Godot project leaves the expectation files out of its build, and the Overseer's
>    project takes only them.
> 2. **In the client, inside the `BotActivity`.** The activity checks its own
>    expectations from client state and writes "met" or "not met"; the judge in the
>    Overseer adds the common checks and gives the verdict. One place per activity, but
>    the bot grades itself: a mistake in how it reads the screen hides a bug.
>
> Recommendation: option 1, with the two halves in one folder. It is what the author
> described (the judge checks, from the game's output), and a verdict that rests on the
> server log and the database is harder to fool than the bot's own report.
>
> Note: option 1 bends [F5a], "expectations live with the catalog entry": they keep the
> entry's name and folder, but build in another project.
>
> Decision (2026-09-28): I think a combination of 1 and 2. The overseer and judge
> determine final output but the bot can evaluate its own state, unless this means
> duplicating code or some other code smell I dont understand.
>
> Consequence noted: the judge, in the Overseer, gives the verdict from the bot's
> events, the server log and the database. The bot also evaluates its own state and
> records what it finds.
>
> On the duplication worry: it is not duplication if each side checks a different
> thing. The bot checks what it needs in order to go on playing: did the screen open
> after the click, is it progressing [F1d], did its step work. It records the result and
> acts on it (retry, give up). The judge checks the outcome against evidence the bot does
> not control: the server log and the database. The smell to avoid is one expectation
> written twice, for example "the score was saved" checked from the screen in the bot and
> from the database in the judge. Rule: the bot checks only what it needs to act; the
> judge owns every expectation about the outcome.

**T8.** Which server and which database does a bot run play on?

> Answer (2026-09-28): The main one, we dont need an extra one
>
> Consequence noted: bots play on the main local server and its database (`mmo3d`), the
> same world the author plays in. Bot characters stay in that database under their
> profiles [F2b]. The judge reads that server's log and that database [T7].

**T8a.** Follow-up: Does the Overseer start the server, or join one that is already
running?

> Answer (2026-09-28): overseer can ensure its started.
>
> Consequence noted: the Overseer checks for a running server on the main port and
> starts one if there is none. Either way it must find that server's log: by default
> the server writes a new file per run under `user://diagnostics` (`--diagnostics`), so
> the Overseer reads the newest one, or names the path itself when it starts the server.

**T9.** How does a bot know which `BotActivity` entries exist?

> Answer (2026-09-28): We need a registry
>
> Consequence noted: one registry lists every `BotActivity`. A bot that picks at random
> [F5] picks from it, and a prerequisite [F2a] is found in it. The judge in the Overseer
> needs the same list for the expectation halves [T7].

**T9a.** Follow-up: How does a new `BotActivity` get into the registry?

> Answer (2026-09-28): I can manually add it or it gets registered. This is the one spot
> I am ok with reflection in the repo if it helps or makes things easier
>
> Consequence noted: either way is acceptable to the author, and reflection is allowed
> here as the one place in the repo.
>
> Options offered (the author left the choice open):
>
> Facts from the code. Reflection is banned today: `BannedSymbols.txt` bans the
> reflection types, and analyzer GAME0006 (`NoReflectionInspectionAnalyzer`) warns on
> `Type` methods that inspect a type. So reflection here needs an exception scoped to the
> registry file. Since [T7], each `BotActivity` has two halves in two projects: the
> play half in the game project, the expectation half in the Overseer's.
>
> 1. **A list written by hand.** One line per activity in a registry file. Plain to
>    read. With two halves, it is two lists in two projects, and a line missing from one
>    of them is skipped silently.
> 2. **Found by reflection at start.** Each project's registry finds every type of its
>    half at start. A new activity is registered by existing. The Overseer can check at
>    start that every play half has an expectation half, and say which does not. Needs
>    the scoped exception to GAME0006 and to the banned symbols, for the registry files
>    only.
>
> Recommendation: option 2. Two hand lists that must agree across two projects is the
> kind of drift that breaks without a sound; one check at start catches it instead.
>
> Decision: pending the author's word, after T7b.
>
> Note: the author objected to the split this rests on: "I dont like this, lets address
> first: Also, since T7 each BotActivity has two parts in two projects." T7 is reopened
> below; T9a waits on it.

**T7b.** (reopened by the author) How does one `BotActivity` stay in one place, when the
bot plays in the client and the judge was placed in the Overseer?

> Options offered (asked for):
>
> The split comes from where the judge runs. The play half must run in the client (it
> presses and reads the game). The expectation half went to the Overseer because the
> judge was placed there [T4, T7]. Three ways out:
>
> 1. **The judge runs in the client, beside the bot.** The judge stays its own
>    component, apart from the bot's logic, but lives in the same process. It reads the
>    bot's observations, the server's log file (same machine), and the database through
>    `src/Data`, which the game project already references. A `BotActivity` is then one
>    folder in one project: how to play and what must be true. The Overseer stays the
>    orchestrator: it starts and stops, collects each execution's folder, and adds what
>    only it can see, a crash or a hang (the process ended or went silent without a
>    verdict). Changes the placement of the judge in T4's consequence; the Overseer is
>    still the console program T4 chose.
> 2. **Expectations as data.** The `BotActivity` writes its expectations into its events
>    file as data ("an RPC Buy from me reaches the server", "my dollars drop by 8", "the
>    event minigame-closed is recorded"). The judge in the Overseer has a small, fixed set
>    of check kinds it knows how to evaluate. One place per activity, but an expectation
>    the check kinds cannot say needs a new kind in the Overseer, so a second place
>    returns for anything unusual.
> 3. **A shared library for activities**, referenced by both the game and the Overseer.
>    The play half uses Godot types, so the Overseer would load GodotSharp and must never
>    call it outside the engine. Rejected as a trap; listed for completeness.
>
> Recommendation: option 1. One activity, one folder, one project, one registry. The
> judge still does not take the bot's word: its evidence is the server log and the
> database. The one thing a judge in the client cannot report is its own client's crash,
> and the Overseer sees that from the process.
>
> Decision (2026-09-28): Ugh, I hate that our client can read the database. I guess this
> is a consequence of Godot... anyway thats a follow up TODO....Put judges in the client,
> thats fine. Utlimetly I think our Bots we program now might play as NPCs at some point
> in the future so maybe thats ok.
>
> Consequence noted: the judge runs in the client, beside the bot, as its own component.
> A `BotActivity` is one folder in one project: how to play it and what must be true
> after. The judge's evidence is the bot's observations, the server's log file and the
> database. The Overseer orchestrates, collects each execution's folder, and adds the
> crash and hang checks from the process. This replaces "the Overseer runs the judges"
> in T4's consequence and option 1 of T7.
>
> Note: the client can read the database because client and server are one Godot
> project, and that project references `src/Data`. The author dislikes this; it is a
> follow-up TODO, recorded under Deferred, not part of this design. The judge reading the
> database is bot code, left out of a release build [T6].
>
> Consequence noted: the author sees a future where bots written now play as NPCs.
> Designed toward, not built: nothing in this design is made for NPCs.

**T9a**, asked again now that an activity lives in one project.

> Options offered (the author left the choice open):
>
> 1. **A list written by hand.** One line per activity in one registry file. A missing
>    line means random bots never pick that activity; a bot made for it still reaches it
>    by name.
> 2. **Found by reflection at start.** A new activity is registered by existing. Needs
>    an exception to GAME0006 and to the banned symbols, for the registry file only.
>
> Recommendation: option 1 now. The drift that argued for reflection came from two lists
> in two projects, and that is gone. One line per activity is plain to read, and the
> ban on reflection stays whole.
>
> Decision (2026-09-28): 1
>
> Consequence noted: one registry file, one line per `BotActivity`, written by hand. No
> reflection; the ban stays whole.

### Outcome

Agreed 2026-09-28 ("Sounds good."). Names in code font are examples unless marked
decided.

> Answer (2026-09-28), on agreeing: Keep in mind, if reflection here in the bots could
> help in other places.. I have ideas but dont understand the code well enough to
> suggest.
>
> Consequence noted: reflection is allowed in bot code wherever it helps, not only in
> the registry; the registry itself stays a hand list [T9a]. Where else it could help is
> for the build, when the code shows the need.

- **Three processes.** The main local server and database [T8]. One Godot client per bot
  execution. The Overseer, a C# console program outside Godot, in its own .NET project
  [T4]. The Overseer makes sure the server runs, and starts it if not [T8a].
- **How a bot starts.** A bot scene inherits `Main.tscn` (root still `Main`, so RPC
  paths match the server) and adds the bot as one child. The Overseer names that scene
  on the command line, with the bot's own options after `--`. `Main` and `ClientGame`
  do not change. Bot code lives in its own folder and namespace in the game project, and
  the release export leaves it out. [T6, F7]
- **How a bot acts.** Only as a player: it presses Godot input actions and clicks
  controls with real mouse events through the viewport (`Input.ParseInputEvent`), all
  inside the client process. It may read any client state. [F2, T1]
- **Naming controls (a game change).** Each screen class declares its controls as
  constants; the game keeps one list of open screens in place of `ClientGame`'s private
  panel fields; the scene check proves each constant exists in its scene. A control built
  from data carries the identity of what it shows (a shop row is its offer's item type
  and tier). Game code does not mention bots. [T1, T2]
- **Steps.** A bot's scenario is a fluent chain of steps (walk to, open a screen, click,
  play). The chain builds a list; the bot runs one step at a time from its frame
  callback and polls it until done. No `async`. [Developer thoughts]
- **Navigation.** Between zones: a graph of zones joined by doors, read from each
  `Door`'s `TargetZone` and `TargetArrival`. Inside a zone: the named things and where
  they are. Walking inside a zone is one isolated procedure with modes: Known Paths (a
  path over a navigation mesh baked at start from the zone's collision), a second mode
  over the same mesh with lower travel costs on chosen ground (name open), and a
  rudimentary mode that walks like a player who does not know the map, to find traps and
  hard-to-reach areas, timed. No hand-placed waypoints. [T3, T3a, T3b, T3c]
- **`BotActivity` and the registry** (name decided). One folder per activity in one
  project: how to play it and what must be true after. One registry file lists them, one
  line each, by hand. [F7, T7b, T9, T9a]
- **The judge** runs in the client beside the bot, as its own component. Its evidence:
  the bot's observations, the server's log file, and the database through `src/Data`.
  It owns every expectation about the outcome; the bot checks only what it needs in
  order to go on. [T7, T7b]
- **Common checks** [F1d]: client errors, through a logger the bot adds to the engine
  (`OS.AddLogger`); server errors, from the server's log; a disconnect. A crash or hang
  is seen by the Overseer from the process.
- **Bot and Overseer talk through files.** One folder per bot execution: an events file
  (JSON lines: actions, observations, progress, errors, helper requests, the judge's
  verdict, done), screenshots and snapshots. The Overseer gives the task as launch
  options and stops a bot with a stop file. Each execution is isolated and stands alone.
  [T5]
- **The Overseer** starts bots, replaces a finished one, launches a helper on request
  [F6], reads each events folder, shows the run's state, and saves the folders as the
  report. It finds the server's log (newest file under `user://diagnostics`, or a path it
  gave). [T4, T5, T8a]
- **No unit tests** for bot code, judges or the Overseer. Game code changed for bots
  keeps the usual test rule. [T4]

Checks to make once, each by one run, before building on them (not unit tests):

- A click fed through the viewport presses a button in a headless client. [T1]
- The client holds each zone's collision, so a navigation mesh can be baked there.
  [T3b]
- An extra child under `Main` in the bot scene disturbs nothing on client or server.
  [T6]

Deferred:

- The client can read the database, since client and server are one project that
  references `src/Data`. The author dislikes it; a follow-up TODO. [T7b]
- Bots written now may play as NPCs one day. Designed toward, not built. [T7b]
- The name of the mesh mode with lower travel costs. [T3b]
- Seeding, and what a run's configuration holds (from the feature fork). [F2b, F2c,
  F4b]

## Gatekeeping

Each "Already decided" item against the Outcome.

- Server authority; clients send input by RPC: fits. A bot is a client and acts only
  through input [F2, T1].
- Game rules in plain C#, proven with `dotnet test`: changed on purpose for bot code
  only; bots, judges and the Overseer have no unit tests [T4]. Game code changed for bots
  keeps the rule.
- No OS-level automation: fits. Every press and click is fed inside the client process
  [T1].
- Multiplayer checked headless with `--server`, `--profile`, `--autoconnect`: fits. Bots
  use the same options and may run headless [F3a, T6].
- No C# on engine-called hot paths: fits. One bot node per client process, stepping once
  a frame, is C# calling the engine, not the engine's inner loop calling C#.
- No `async`/`await`: fits. The fluent chain builds a list of steps that is polled from
  the frame callback [Developer thoughts]; the Overseer loops and polls files [T4, T5].
- Prefer proven packages and engine features: fits. Navigation uses Godot's navigation
  server [T3b]; the rest is small and ours, which keeps full control.
- "No bots or dev scenarios for now": changed by this session, at the author's ask.
- Built `LaunchOptions`: fits. Unknown options are ignored, so a bot's options ride on
  the same command line [T6].
- Reflection banned (`BannedSymbols.txt`, GAME0006): fits for the registry, a hand list
  [T9a]; allowed on purpose elsewhere in bot code where it helps [Outcome].
- Scenes stay editable in the editor (project memory): the bot scene inherits
  `Main.tscn` and must pass `scripts/scene-check.ps1` like every scene.

## Compared with the removed bot code

Read at the close, as the author asked [T6]: the code taken out in commit 48f97cb and
the docs taken out in 5a15cd3 (`docs/engineering/bot-testing.md`,
`bot-testing-findings.md`, the dev scenario and load test parts of `testing.md`).

### How it worked

- **Layers inside the client:** a driver (what to do next), a router (zones and the way
  between them), activities (one task each), judges (did the task work), screen classes
  (how one screen is worked), and a body (keys, clicks, what is open).
- **Activities** were classes with a name, a weight, a zone, what they need and give
  (facts), their steps, and their own judge. Steps were written with a fluent `BotPlan`
  (`WalkTo`, `Use`, `Click`, `Type`, `Press`, `Close`, ...), each with a time limit.
- **Chains:** related chains written by hand, random chains drawn with a seed the log
  named (to replay), and goal chains planned backward from facts by a resolver ("phone
  at 50%" became recycle, buy a battery, swap it in).
- **Personas** weighted what a bot did and how: wanderer, curious (opens every panel and
  clicks what it finds), gamer, escaper, wedger, earner, slow, masher, shadow, eventer,
  dropper.
- **An interrupt roller** cancelled activities mid-step or dropped the connection, to
  leave the state a distracted or disconnected player leaves. **Asides** fired on timers
  in the middle of other activities.
- **A keeper** clicked Play again from the main menu, closed stray menus, and finished a
  character switch.
- **Judges ran inside the bot and judged only what its player could see, never asking
  the server.** Position judges (footing, no footing, floating, too high, in a vehicle,
  stuck, out of bounds, thrashing, walk failed), zone judges (churn, ping-pong), a client
  error judge, `cannot-close`, `promise-broken`, `off-screen`. Their decisions were plain
  C# in `src/BotJudging`, with xUnit tests built from real findings.
- **Findings** were written at most once a minute per kind, each a folder: what the judge
  saw, the network link's state, a picture, the client's last log lines; a Python watcher
  added the server's records for the minute either side, and a triage tool grouped
  findings into issues and wrote an HTML report.
- **Screens** were known to bot-side classes (`GardenUi`, `ChatUi`) by groups the game
  added to its buttons for bots; the bot scrolled a list to bring a button into view.
- **Walking:** a navigation mesh baked from the zone's collision on arrival, a router
  between zones, and stuck recovery (back off, turn, jump, then try eight directions).
- **Bots said what they did in public chat** (`[bot] goal: ...`).
- **The catalog** found every feature class by reflection when bots started.
- **Launch** was by flags that `LaunchOptions`, `Main` and `ClientGame` read.
- **Beside the bots:** dev scenarios (the server set a player up at login, then a
  headless client tested one feature through input in under a minute and passed or
  failed), and a load test (hundreds of headless bots in a few processes, to measure
  the server).

### Where this design agrees

- Bots act only through input, and may read anything [F2].
- A catalog of activities with play steps and per-activity expectations [F5, F5a]; the
  name `BotActivity` is the same.
- Steps written as a fluent chain, each step polled until done.
- A navigation mesh baked from zone collision, and a zone graph from the doors [T3].
- Client errors caught by the bot, stuck bots judged, a folder per record with
  pictures [F1c, F1d, T5].

### Where this design differs

- **The judge's evidence.** Then: only what the player could see, never the server. Now:
  the server's log and the database as well [T7, T7b].
- **Orchestration.** Then: bots ran for hours with personas, and a Python watcher
  gathered findings afterwards. Now: an Overseer starts bots on specific tests, replaces
  them, launches helpers, and collects each execution's folder [F1a, F6, T4].
- **Tests.** Then: judge decisions in `src/BotJudging` with xUnit tests. Now: no unit
  tests for bot code [T4].
- **Screens.** Then: bot-side screen classes and groups added to game buttons for bots.
  Now: each screen owns constants for its controls, the game keeps one list of open
  screens, and a click is a real mouse event at the control's centre [T1, T2].
- **Launch.** Then: flags read by game code. Now: a bot scene inheriting `Main.tscn`;
  game code does not change [T6].
- **The catalog.** Then: found by reflection. Now: a hand list [T9a].
- **Chat.** Then: bots announced themselves in public chat. Now: files only [T5, T5a].
- **Prerequisites.** Then: facts (needs and gives) and a backward resolver. Now: a
  prerequisite is another `BotActivity` [F2a]; how one is chosen is not designed yet.
- **Setup.** Then: dev scenarios set state on the server. Now: a bot earns what it needs
  by play; seeding is deferred [F2a, F2c].
- **Walking modes.** Then: the mesh plus stuck recovery. Now: named modes, including a
  rudimentary one meant to find map traps [T3c].

### What the old code had that this design does not account for

- **Interrupts:** cancelling an activity mid-step, and dropping the connection mid-step,
  to test the state a distracted or disconnected player leaves.
- **Asides:** small actions on timers in the middle of others (an emote mid-walk).
- **Personas:** several play styles at once with weights and pace; among them a curious
  bot that clicks every control on every screen, a key masher, an edge escaper, a gap
  wedger, and a shadow that follows another player through doors.
- **Chains and seeds:** a hand-written order of activities around one piece of state,
  and seeded random orders that can be replayed exactly.
- **Planning prerequisites:** the resolver that worked out which activities give a
  missing fact. This design names prerequisites [F2a] but not how they are found.
- **Recovery:** a keeper that returns a bot to the world from the main menu, closes
  stray menus, and handles a character switch; and the login retry while the server
  still holds the old session (about 10 seconds).
- **More judge kinds:** footing, floating, too high, in a vehicle, out of bounds,
  thrashing, zone churn and ping-pong, a panel that cannot close, a button off the
  screen, and a finished activity whose promise does not hold.
- **Finding hygiene:** one finding per kind per minute, triage into issues, the network
  link's state in each record, the server's records around the moment.
- **Client hygiene:** bots never saved the machine's `settings.cfg`, stayed windowed
  whatever the settings said, had no sound, and windows were laid out by the run script
  (small tiled windows made buttons fall off the screen).
- **Authoring:** one command to run one activity again and again in a window.
- **Dev scenarios and the load test:** fast single-feature checks with server-side setup,
  and a performance test with hundreds of headless bots. Neither is in this design.

### The old features, taken into this design

> Answer (2026-09-28): Pick best ideas and options, I kind of like all the features but
> what I did not like was the code
>
> Consequence noted: the author keeps the old features and delegates the choice of form.
> Each one below is fitted to the agreed Outcome with the fewest new concepts; where an
> earlier decision already covers a feature, it is named. Numbered R1 to R11 for
> tracing. Decision (2026-09-28): delegated to the model by the author's answer above.
>
> - **R1. Interrupts.** Two kinds. *Walk away:* a bot may abandon a `BotActivity` at a
>   random step and leave everything as it is; the next activity must cope. *Connection
>   drop:* the Overseer kills the client at a random moment and starts it again with the
>   same profile; the new execution's judge checks the login back into what was left.
>   The Overseer already kills and starts clients [F1a, T4], so no new part. The kill
>   prints "Internal CLR error" (CLAUDE.local.md, Testing); the Overseer ignores it for
>   a kill it made.
> - **R2. Asides.** A `BotActivity` can be marked short enough to run between two steps of
>   another (an emote mid-walk). A random bot may slip one in; the interrupted activity
>   then goes on. No timers of their own: the chance is per step.
> - **R3. Personas.** A persona is a named part of the run's configuration [F4b]: weights
>   over the registry's activities, the pace, and the chances of R1 and R2. The old
>   special bots become activities or modes, not personas with code of their own:
>   *curious* is a `BotActivity` that clicks every control of the open screen, which the
>   screens' constants make easy [T1]; *masher* is a `BotActivity` that presses random
>   actions; *escaper* and *wedger* are the rudimentary walking mode aimed at the zone's
>   edge or at a gap [T3c]; *shadow* is the helper's follow mode [F6].
> - **R4. Seeds.** One seed per execution drives every random choice, written first in the
>   events file. The Overseer can start an execution again with the same seed to replay
>   it. Hand-written orders (the old related chains) are the author's fluent chains
>   [Developer thoughts], so no chain concept is needed.
> - **R5. Planning prerequisites.** A `BotActivity` says what it needs and what it gives,
>   as simple facts a player can see ($500 or more, a Battery in the bag, in the college).
>   Before an activity, a small resolver finds, for each missing need, an activity in the
>   registry that gives it, chosen at random among those that do, and runs that first.
>   This is how [F2a] works. After an activity, a fact it gives that does not hold is a
>   finding (the old "promise broken").
> - **R6. Recovery.** Part of every bot, not an activity: at the main menu it presses
>   Play; before an activity it closes every open screen through the open-screens list
>   [T1]; it retries a refused login while the server still holds the old session.
> - **R7. More judge kinds.** Added to the common checks every judge runs [F1d]: footing
>   (standing on what a player should not), floating, out of bounds, thrashing, zone
>   churn and ping-pong, a screen that does not close, and a control to click that lies
>   outside the window (a real click there fails, as it would for a player [T1]).
>   Thresholds are placeholders, generous so a bot a little off is not a finding.
> - **R8. Finding hygiene.** A judge records the same kind of finding at most once a
>   minute per bot. Each finding carries the network link's state (round trip, loss) and
>   the server's log lines around its moment. The Overseer's summary groups findings of
>   the same kind and place across executions into issues.
> - **R9. Client hygiene.** Bots run with no sound (Godot's `--audio-driver Dummy`, an
>   engine option), windowed (`--windowed`, built), and must not save the machine's
>   settings. The last needs a launch option in the game that is not about bots (for
>   example, "do not save settings"), so game code still does not know bots [F7]. The
>   Overseer lays out windows; large windows by default, since small ones push controls
>   off the screen.
> - **R10. Authoring.** The run script can run one `BotActivity`, or one fluent chain,
>   again and again in a window, to watch it while writing it.
> - **R11. Dev scenarios and the load test.** Server-side setup is the same idea as
>   seeding, which stays deferred [F2c]. The load test measures the server, not the
>   game's behaviour; it stays out of this design and is designed on its own when needed.
>   Bots announcing themselves in chat stays rejected [T5a].

## Decided in the author's absence

On 2026-09-28 the author went away and asked for the old features to be built, the bots
run and tested, and a soak run. Their words:

> Answer (2026-09-28): Go ahead and add all the features from the old bots (dont copy
> their code, but you can reference it to help reason about things, The game code cannot
> know about bots, we can give entrypoints or expose things or do small restrucutres or
> rebuild a small scene the correct way [...] GO as far as you can without my input and
> make your best judgements.
>
> Answer (2026-09-28), on the soak and its players: About 8 windowed. They dont need to
> be fresh, you can reuse old ones, that might be a good way to test things also. A
> fresh player should only be for things you want to be tested on a fresh account.
> [...] Add tests for things we didnt do yet. Like character editing, menus we didnt
> build tests for, UI items we didnt test for. Features we didnt test yet.
>
> Answer (2026-09-28), on the new zones: Include the new areas, they have no usable
> things but it may be interesting to see how a bot behaves when it needs to cross zones
> or something.. test things like that, just because it doesnt make sense doesnt mean we
> cant test it [...] If its completely obvious and deterministic then we dont need to do
> it.
>
> Decision (2026-09-28): "Take my recommendation" on the questions asked before the
> author left. Every item below is the model's choice under that delegation, for the
> author to review.

- **Personas [R3].** Eight, in `BotPersonas.cs`: wanderer, curious, gamer, escaper,
  earner, traveller, masher, dropper. Each is weights over activity names and a chance
  to walk away at a step boundary [R1]. The soak is every persona at once, each its own
  kept player (`-Soak`).
- **Connection drops [R1], changed.** The bot ends its own client (`DropStep`) after it
  writes a `dropping` event, rather than the Overseer killing it at a random moment. The
  bot knows the moments worth testing (mid-walk, online at a terminal). The Overseer
  reads `dropping`, counts the end as intended, and starts the bot again as the same
  player. Any other early end of a persona bot is a failure and is restarted too.
- **Asides [R2].** Not built. The chatter is the only thing that runs between steps.
- **Two-player features without the helper [F6].** In a soak the other bots are the
  other players, so social activities pick whoever is in town: `PickPlayerStep` walks
  up to another player and clicks on their body. Befriend, invite, give and message are
  activities. Answering an invite is a reflex of every bot (`BotInviteAnswers`), not an
  activity, since the invite comes whatever the plan is doing. The helper bot of [F6]
  stays deferred; a feature that needs a partner in a known state still needs it.
- **Shared things.** A terminal serves one player. `UseTerminalStep` waits for a free
  one, walks up, and backs off and tries again when someone got there first. A chest
  that is empty, a job already taken, a map indoors: the plan stops as completed
  (`StopIf`) or the click is optional (`ClickIfThere`), since each is a normal state of
  a shared world, not a failure.
- **Findings [R7, R8].** Stuck, out of bounds, floating, zone churn (three stays under
  1.5 s in a minute) and client errors, each at most once a minute per kind and detail.
  The network state and the server's log lines of [R8] are not built.
- **Not built:** window layout by the Overseer [R9], the authoring loop [R10], seeding
  [F2c], helpers [F6].
- **Game changes, none about bots [F7]:**
  - `--settings-file` [R9].
  - `ClientGame.View` with the open screens and the last notices [T1].
  - Named rows in the shop, recycler and bag; named take-the-job buttons
    (`Take_<job id>`) [T2].
  - Street lamps that read the day-night state (`NightLight`, group `day_night`).
  - The two new zones, `wooded_path` and `new_town`.
  - `--autoconnect` chooses a character once per launch.
  - The settings panel scrolls: it ran off a 648-pixel window (found by a bot).
  - The `fresh` profile is one player per launch, not per connect: leaving and playing
    again made another player (found by a bot).

## Consequences

- `CLAUDE.local.md`: "No bots or dev scenarios for now" replaced (2026-09-28) by a
  pointer to this file, with the two exceptions for bot code only: no unit tests [T4],
  and reflection allowed where it helps [Outcome]. Agents still write no bot code unless
  the author asks.
- A game launch option that keeps a client from saving the machine's settings, named for
  what it does, not for bots [R9].
- Game code, before or with the first bot: one list of open screens in place of
  `ClientGame`'s private panel fields, constants for each screen's controls, controls
  built from data that carry their data's identity (the shop's rows first), and the
  scene check extended to prove the constants [T1, T2]. The HUD action bar is built in
  code (`Hud.ShowActions`) and needs the same treatment.
- The release export preset leaves out the bot folder and the bot scene [T6].
- Follow-up TODO, outside this design: the client can read the database, because client
  and server are one project that references `src/Data` [T7b].
- Stale mentions of the removed bots, to update or leave as history:
  `docs/planning/status.md` ("Bots and scripts keep the fixed 45 degree view"),
  `docs/features/world-events-mvp.md` (the "swarm" scenario and the "check world events"
  bot activity), `docs/features/usable-phone.md` ("the collector bot"),
  `docs/planning/first-playable.md` (its bot lines are in the list of what mmo-game had,
  so they stay).
- The author's memory notes from the removed work ("bot tests set up state", "every
  action covered", "tests as verification", "watch and ask") name removed tools, and one
  ("no wandering bots", "set up exactly what a feature needs") disagrees with [F1c] and
  [F2a].
- `docs/world.md` and `docs/backlog.md`: nothing to fold; bots are not part of the game's
  fiction.
- `docs/engineering/`: a bots doc once the code exists, not before.
- A split into PRs, by dependency. The author codes the first bot by hand, so this is the
  order the pieces depend on, not a plan to build them:
  1. The three one-run checks [Outcome]: a headless click, zone collision in the client,
     the extra child under `Main`. Everything else rests on them.
  2. The game change for named controls and the open-screens list [T1, T2].
  3. The bot scene, the bot node, the step chain, the events folder and the client
     logger: one bot that runs one `BotActivity` by hand.
  4. The judge in the client [T7b], then the Overseer [T4, T5], then navigation modes
     [T3], then helpers [F6].
