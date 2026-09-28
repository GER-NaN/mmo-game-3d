# Bot testing

Bots that play the game by themselves, judges inside them that notice when something
is wrong, and a watcher that gathers every finding, with its picture and the client's
and the server's records, onto one page. Run it for minutes to see a change played, or
overnight to see what breaks over time. What it has found is in
`bot-testing-findings.md`.

It sits beside the two other ways of testing the game: a **dev scenario**
(`testing.md`) sets a player up for one feature and checks it in seconds; a **load
test** (`load-test.md`) runs hundreds of headless bots in a few processes to measure
the server. Bot testing is players: whole clients, one per bot, playing the game the
way people do, for as long as it runs.

## Running it

```
.\scripts\server-up.ps1                       the server (Postgres must be up)
.\scripts\bots-up.ps1                         9 bots, soak1 to soak9, tiled over every screen
.\scripts\bots-up.ps1 -Count 4 -Layout Full   4 bots, each window the size of its screen
.\scripts\bots-up.ps1 -Personas gamer,curious  who each bot is, in turn (one of each by default)
python tools/bot-watch/watch.py --every 120   a look every 2 minutes, until that server stops
python tools/bot-watch/triage.py --since 02:00   the findings since then, grouped into issues
```

To try every activity there is, one bot does them all, shuffled, each to its end with
nothing cancelling or breaking into it, and prints a tally after each round (finished,
given up with the reason, could not start):

```
.\scripts\bot-everything.ps1                 one bot, profile everybot, the size of the screen
```

Open `%TEMP%\mmo-game-3d-bots\judge\report.html` in a browser to review the findings,
or run `triage.py` first: a run's findings are usually a few issues many times over.

To stop: `.\scripts\bots-stop.ps1`, then `.\scripts\server-stop.ps1` so the server
saves. Wait about 10 seconds after stopping bots before starting them again: the server
drops the old sessions only then, and a bot that logs in first is refused (it retries
from the main menu by itself).

**Overnight:** the same, with the watcher started as its own process so it keeps going
without anyone:

```
Start-Process python -ArgumentList "tools/bot-watch/watch.py", "--every", "120" `
    -RedirectStandardOutput "$env:TEMP\mmo-game-3d-bots\watch.log" -WindowStyle Hidden
```

Each bot is its own player (`--profile soakN`), kept between runs, and logs to
`%TEMP%\mmo-game-3d-bots\soakN.log`. Bots have no sound, keep their window whatever the
machine's saved settings say (`--windowed`), and never change those settings.

## How a bot plays

The one rule, kept from mmo-game: **a bot may look anything up, but it acts only through
input** (keys, clicks, typing: the same events a keyboard and a mouse make), so everything
past the input is the real game. And **a bot judges only what its player could see**: where
its body is, what is open on the screen, the prompt, its bag and money. It never asks the
server; the server's side of a finding is attached afterwards, for the person reading it.

A bot is built in layers, each knowing one thing:

| Layer | Knows | Code |
| --- | --- | --- |
| driver | what to do next: the persona, the picks, the interrupts | `BotDriver` |
| router | where the zones are and how to get between them | `BotRouter`, `TravelActivity` |
| activity | one task, its choices, where it happens, what it needs and gives | `BotActivity`, `game/dev/bots/activities/` |
| judge | whether that task worked, from the player's view | `BotActivityJudge` |
| screen | how one screen is worked: its buttons, fields, drags | `game/dev/bots/screens/` (`GardenUi`, `ChatUi`) |
| body | the hands and senses: a click, a key, what is open | `BotBody` |

**Activities** are the things a bot does: make a house plant, emote, buy a battery, enroll
in a career. Each says **where it happens** (its zone: the driver has the router take the
bot there first, from anywhere), what it **needs** and what it **gives** (facts, below),
and how it is picked: **chosen** by weight when the bot is free, or an **aside**. Most are
a list of steps (`StepsActivity`, `BotSteps.cs`), each with a time limit; a step that fails
or runs out ends the activity, as does a zone change no step asked for (a party pull).

**Chains** are activities one after another, for orders random picks would almost never
reach. A **related** chain is written by hand around one piece of state (change career,
then back, then again; buy, recycle, buy). A **random** chain draws three to six of the
activities a bot may choose, with a seed of its own that the log names, so it can be drawn
again exactly. A **goal** is a chain planned from facts: its next activity is worked out
afresh after each one (below). A failed link does not end a chain: the next one must cope
with what it left.

**Goals and facts.** A fact is something a player can see about themselves: in a zone,
money at least so much, an item in the bag or worn, the phone's charge, a career. A goal
is the facts it wants; `BotResolver` works backward: for the first wanted fact that does
not hold, an activity that gives it (one of those that do, at random, since variety tests
more than the shortest way), and for that activity's first need that does not hold, the
same again. "In a zone" is the router's. So "phone at 50%" from an empty wallet becomes:
recycle something for money, then go to the shop and buy a battery, then swap it in, and
the log says so: `plan for "charge the phone": phone at 50% or more <- swap the battery <-
a Battery in the bag <- buy Battery <- $8 or more <- recycle for money`.

**The driver's loop** is the same for every persona. When the bot is free it picks an
activity, a chain or an aside, weighted by the persona; the **interrupt roller** then
decides whether, and when, to **cancel** it (walk away mid-step, leaving everything as
it is: the state a distracted player leaves) or to **lose the connection** in it; the log
names the moment. Asides also fire on **their own timers**, pausing whatever runs and
resuming it after: an emote mid-walk, chat over a terminal. Between runs the bot closes
what is open, except after a cancel.

**Judges for each activity** watch one run and judge how it ended, as a player would:
the emote shows on the body (or, online, does not); the plant is made; a traveller that
has not moved for a while and is not there yet is stuck. And every activity that
finished must have kept its promises: a swap that finished with the phone still low is a
`promise-broken` finding, whatever the reason.

**Clicking** goes by what is on the screen: a button below the window or outside what its
list shows is first brought in with the mouse wheel over the list, as a person scrolls;
one no wheel brings in is an `off-screen` finding and is never clicked.

Bots **say what they are doing** in public chat: `[bot] goal: charge the phone`,
`[bot] charge the phone -> buy Battery`, `[bot] cancelling ... (mid ...)`,
`[bot] wandering: meet someone`.

**Personas** (`BotPersonas.cs`, `--persona`): who a bot is, as numbers the driver reads.
A persona weighs the activities and chains (a factor on each, 0 to leave one out), shares
its free moments between activities, chains and asides, adds activities of its own
(`BotExtraActivities.cs`), and sets its pace, how often asides come, how often it cancels
what it does, how often its connection drops and how readily it joins a party. Several
side by side play the game several ways at once:

| Persona | What it does | Finds |
| --- | --- | --- |
| wanderer | a bit of everything | the ordinary |
| curious | opens every panel and terminal app and clicks what it finds, types into fields (never Quit, Leave to main menu, Delete) | screens that break, buttons that do nothing or too much |
| gamer | Agent Defense, the code cracker, the potting table, over and over | the mini games under repetition |
| escaper | runs straight for a point past the zone's edge, jumping | ways out of the world |
| wedger | walks straight into the gap between two buildings and keeps pushing | where players get wedged |
| earner | picks up, recycles, reports drones, for all the money it can | the economy's loops |
| slow | a wanderer at a third of the pace that finishes what it starts | timing that only fails slowly; easy to follow on screen |
| masher | presses the game's keys fast and in any order, now and then | input the game did not plan for |
| shadow | follows another player at arm's length, uses what they use, and follows them through doors | two players on one terminal, shopkeeper or door at once, arriving on one spot |
| eventer | checks Notifications for world events often, and goes to half of those running | a swarm with several players at once, taking part, the drops, the rows after it ends |
| dropper | a wanderer that loses its connection in about a third of what it does, at a random moment, most of all in taxis, at terminals and in games; the keeper logs it back in | the state a lost connection leaves, and the login back into it |

**Walking** (`BotNavigation`, `Walker`): when a bot arrives in a zone it bakes a
navigation mesh from the zone's collision (Old Town in about 35 ms) and follows its
paths round buildings. A bot still at a terminal (a goal dropped mid-run leaves it open) goes offline before it
walks, as a person would. Doors are walked to by the spot in front of them first. Stuck (not
moving) it backs off, turns and jumps; after two failed walks in a row it escapes,
trying eight directions. Open land (the meadows) is walked straight.

**The keeper** (`BotKeeper.cs`) lives as long as the client: back at the main menu it
clicks Play after 15 seconds, and it closes a game menu left open. It also saves a
picture of the bot's game view when the watcher asks. It finishes a character switch
("switch characters" leaves by the game menu and ends there): Play, then Play on the
other character's card, or a second character made (a random look, the name with
" Two"). Back in the world it judges the switch (`switch-failed`: the same character
again, or over a minute).

## The judges

Each bot judges itself, all the time, with quick checks and room to spare, so a bot a
little off is never a finding:

| Judge | Finding | When |
| --- | --- | --- |
| `BotPositionJudge` | `footing` | standing on something a player should not be on (anything but `Ground`, `Roads`, `Terrain`, `Room`, `Cabin`) for 10 s |
| | `no-footing` | nothing under the feet for 10 s |
| | `floating` | 0.8 m above the surface under it, not jumping, for 10 s |
| | `too-high` | 1.2 m above the zone's arrival and spawn markers (not on terrain), for 10 s |
| | `in-vehicle` | within a car's footprint, checked twice a second |
| | `stuck` | within 2.5 m for 30 s while walking (standing still online or at a panel is not stuck, unless the step is a walk; nor is pushing on purpose, the escaper at the edge or the wedger in a gap, until the next step walks away) |
| | `out-of-bounds` | past the zone's map by 5 m, or 10 m below the zone |
| | `thrashing` | four sharp reversals in 5 s with little gained |
| | `walk-failed` | a walk given up after its tries at working round something |
| `BotZoneJudge` | `zone-churn` | more than 8 zone changes in a minute |
| | `zone-ping-pong` | back and forth between the same two zones 3 times running, each stay under 10 s |
| `BotErrorJudge` | `client-error` | an error in the client's log (a C# exception, an engine error), with its stack; the same message at most every 5 minutes |
| `BotDriver` | `cannot-close` | a panel or terminal still open after 10 s of Esc, Close, Back and Go Offline; the same screens at most every 5 minutes |
| | `activity-failed` | an activity's own judge says it did not work (its reason in the detail) |
| | `promise-broken` | an activity finished, and a fact it gives does not hold (a swap that left the phone low) |
| | `chain-step-cannot-start` | a link of a related chain cannot start where the chain has brought the bot |
| `BotBody` | `off-screen` | a button to click that no window or scroll shows, with where it is and the window's size |

**The judges are tested.** Their decisions are plain C# in `src/BotJudging` (thrashing,
stuck, travel, zone changes, emotes), with no Godot in them; the live judges only look
and report. `tests/Tests/Bots/JudgeTests.cs` hands each check a run of what a bot saw and
checks the verdict: a bot told to go to the college that stands still is caught; one
walking on the spot for half a minute is stuck; standing at a terminal is not; real
tracks from findings are kept as cases, the bugs that must be caught and the false
alarms (a wedged bot's jitter) that must stay quiet. A new judge puts its decision there
too, with a test for what it must catch and one for what it must not.

The same kind for the same bot is written at most once a minute. Each finding is a
folder, complete on its own, so it can be reviewed without anyone having watched:

| File | What |
| --- | --- |
| `finding.json` | what the judge saw: where, on what, doing what (goal and activity), which step, heading where, where it was before or its zone changes; how long since it arrived in the zone (`body_age`), and ENet's view of the link to the server (`net`: round trip, loss, throttle) |
| `picture.png` | the game view at that moment |
| `client.log` | the bot's last 300 log lines |
| `server.jsonl`, `server.txt` | every server record about that player in the minute either side, and a readable summary (added by the watcher) |

## Adding to it

**An activity is a class** under `game/dev/bots/activities/`, made from `StepsActivity`: its
name and weight, where it happens (`Zone`), what it needs and gives (`Needs`, `Gives`),
its steps (`Plan`), and its judge (`NewJudge`). `GreenhouseActivity` is the model:

```csharp
public sealed class GreenhouseActivity : StepsActivity
{
    public GreenhouseActivity() : base("make a house plant", 1) { }

    public override string Zone { get { return ZoneIds.Greenhouse; } }

    protected override List<BotStep> Plan(BotBody body)
    {
        BotPlan plan = new BotPlan()
            .WalkTo("Interactables/PottingTable")
            .Use("house plant", b => GardenUi.IsOpen(b));

        // How many pieces, which, where: chosen afresh each run.
        for (int i = 0; i < 1 + body.Random.Next(PlantDesign.MaxPieces); i++)
        {
            plan = plan.Step(GardenUi.Place(body.Random.Next(64), x, z));
        }

        return plan.Step(GardenUi.Complete(name)).Step(GardenUi.WaitDone()).Close().Steps;
    }

    public override BotActivityJudge? NewJudge() { return new GreenhouseJudge(); }
}
```

No door appears in it: the router takes the bot to the greenhouse from wherever it is.
An **aside** is the same, with `Timing` set to `Aside` and `AsideEvery` its mean seconds
between firings (`EmoteActivity`). An activity only chains and goals start has weight 0.

**A screen** a bot works gets one class under `game/dev/bots/screens/`, the only bot code
that knows its groups and layout (`GardenUi.Place`, `GardenUi.Complete`, `ChatUi.Say`);
activities call it, and a change to the screen changes one file. Each operation that
takes more than a frame is a step.

**A judge** watches one run as the player would (`BotActivityJudge`: `Before`, `Watch`,
`After`), from what the client shows, never the server. Keep it coarse: the effect
happened, or a refusal came.

**A goal** is a `GoalChain`: when it may start, the facts it wants, a budget. The
resolver does the rest, from the activities' `Needs` and `Gives`:

```csharp
Add(new GoalChain("charge the phone", 5, body => body.PhonePercent >= 0 && body.PhonePercent < 20,
    body => new List<BotFact> { BotFact.PhoneAtLeast(50) }, 8, 240));
```

**A related chain** lists its links; each link makes its activity from the bot as it is
when its turn comes:

```csharp
Add(new RelatedChain("change careers and back", 1,
    body => new EnrollActivity(Other(body)),
    body => new EnrollActivity(Other(body)),
    body => new EnrollActivity(Other(body))));
```

These go in `BotCatalog`, or in a feature file under `game/dev/bots/features/`: a class that
implements `IBotFeature` and adds its activities and chains. `BotCatalog` finds every
such class when bots start. `BotWhoisFeature.cs` still shows the short form, an activity
written as a plan inline (`new StepsActivity(name, weight, canStart, body => plan)`).

**`BotPlan`** writes a plan as it reads: `Door`, `WalkTo`, `Use`, `UseOnce`, `Click` (a
group's button, or the one on the row naming an item), `Type`, `Press`, `Equip`,
`Phone`, `Pause`, `Wander`, `Say`, `Close`, and `Do` or `Step` for anything else. Each
adds one step with its time limit and its way out.

**Try it** with one bot that does only that, again and again, in a window to watch:

```
.\scripts\bot-try.ps1 "make a house plant"
.\scripts\bot-try.ps1 "charge the phone"                 a goal
.\scripts\bot-try.ps1 "change careers and back"          a chain
.\scripts\bot-try.ps1 "ride a robo taxi,visit the college"   these, in turn
```

The same replays a finding: run the activity or chain it names, with the persona it
names. A random chain's log line gives its seed.

- **What a new screen needs in the game:** its buttons and fields in a group
  (`AddToGroup`), so bots find them as a person finds them by looking, and its panel
  type in `BotBody.IsPanel` so bots can close it. Buttons whose label says Quit, Leave to
  main menu or Delete are never pressed by the curious bot.
- **A persona:** a case in `BotPersonas.Get`: its factors (`Likes`), its shares between
  activities, chains and asides, its own activities (`Own`), its pace and chances; then
  its name in the default mix in `scripts/bots-up.ps1`.
- **A dev scenario** for the feature too (`testing.md`): the scenario checks the
  feature in seconds; the bots play it for hours.
- **Where things are:** activities in `game/dev/bots/activities/`, one class per file
  (the models are `GreenhouseActivity` and `EmoteActivity`); screens in
  `game/dev/bots/screens/`; steps that several activities share in
  `game/dev/bots/steps/`; chains in `game/dev/bots/chains/`; goals and the list of
  chains in `BotCatalog`.

## Next

Agreed on 2026-09-27, in this order, with where each stands:

1. **The bots' screen.** Bot windows are small, and the UI has no scaling, so buttons
   fall off the edge. Runs now use `-Layout Full` (each window the size of its screen),
   which fits everything; the tiled layout still loses the phone's Go Offline button
   (a game finding). Drawing bot UIs at 1280 by 720 is only needed if tiled runs come
   back. The real fix, a UI that fits any window, belongs to the HUD and menu redo.
2. **Scenarios as activities.** The dev scenarios and the bots share one step library,
   so each scenario is also a bot activity (without its setup and checks), and a new
   feature is played by bots as soon as its scenario exists. Not started; `BotPlan` and
   the feature files are the step library's likely start.
3. **Headless bots**, for runs nobody watches. A headless client draws nothing, so its
   findings come without pictures; visible windows stay the default while pictures
   matter.

Waiting on the author (see `bot-testing-findings.md`): the size of door triggers, and
the street kiosk standing in the college door's trigger. Both keep showing as zone
ping-pong until then.
