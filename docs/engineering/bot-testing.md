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
.\scripts\bots-up.ps1                         8 bots, soak1 to soak8, tiled over every screen
.\scripts\bots-up.ps1 -Count 4 -Layout Full   4 bots, each window the size of its screen
.\scripts\bots-up.ps1 -Personas gamer,curious  who each bot is, in turn (one of each by default)
python tools/bot-watch/watch.py --every 120   a look every 2 minutes, until the server stops
python tools/bot-watch/triage.py --since 02:00   the findings since then, grouped into issues
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

`game/dev/BotDriver.cs` is the brain. The one rule, kept from mmo-game: **a bot may
look anything up, but it acts only through input** (keys, clicks, typing: the same
events a keyboard and a mouse make), so everything past the input is the real game.
`BotBody` is what it sees and does; what is open on the screen is always looked up,
never remembered, so a bot that lost track of itself still sees the truth.

**Activities** (`BotActivities.cs`) are the things it does, each a plan of **steps**
(`BotSteps.cs`): visit the college means go through the door, wander, walk to the
registrar, talk, work the panel, close it, emote, walk to the professor, talk, work the
panel, close it, say something, walk out. Every step has a time limit and says when it
is done; past the limit, or when it fails, the activity ends. A zone change that no step
asked for (the party walking through a door) ends it too. Between activities the bot
closes whatever is open, so its window shows the world.

The activities: walk around town, visit the college, go shopping, use a public
terminal, use the phone, check the bag, ride a robo taxi, fix something, repair the
street lights, tag the subway, go to the outskirts, make a house plant, walk the
meadows, meet someone (friend, message, invite or give), leave the party, recycle, look
at the map, look at skills and friends, and go back to town.

**Goals** (`BotGoals.cs`) are what a bot wants, reached through activities it chooses by
what it has, one after another:

| Goal | How it gets there |
| --- | --- |
| fight drones | an EMP worn: equip one from the bag, or buy one ($10), or earn the money first; then hunt drones and bring two down |
| charge the phone | below 20%: swap in a battery from the bag, or buy one ($8), or earn first |
| explore this zone | walk to the parts of the map not discovered yet |
| earn some money | recycle what is carried, pick up what lies about, or report drones on the Town cameras |
| play Agent Defense | go online, open Defense Objectives, start a run, play every cue |
| tidy the bag | drop something |

A bot with no goal is in **wander mode**: it picks activities by weight. A quarter of
goals are **dropped at a random moment**, wherever the bot is then, mid-purchase or
mid-walk, without tidying up: the state a distracted player leaves behind. A goal is
**solo**: the bot leaves its party first and turns invites down until the goal ends, so
bots after different things do not drag each other through doors. Parties belong to
wander mode, where a bot joins about a third of the invites it gets.

Bots **say what they are doing** in public chat: `[bot] goal: fight drones`,
`[bot] fight drones -> buy EMP Emitter`, `[bot] dropping fight drones (mid ...)`,
`[bot] wandering: meet someone`.

**Personas** (`BotPersonas.cs`, `--persona`): who a bot is. A persona weighs the
activities and goals (a factor on each, 0 to leave one out), adds its own
(`BotExtraActivities.cs`), and sets its pace, how often it drops a goal, how readily
it joins a party, and how often its connection drops. Several side by side play the game several ways at once:

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
| dropper | a wanderer that loses its connection in about a third of what it does, at a random moment, most of all in taxis, at terminals and in games; the keeper logs it back in | the state a lost connection leaves, and the login back into it |

**Walking** (`BotNavigation`, `Walker`): when a bot arrives in a zone it bakes a
navigation mesh from the zone's collision (Old Town in about 35 ms) and follows its
paths round buildings. A bot still at a terminal (a goal dropped mid-run leaves it open) goes offline before it
walks, as a person would. Doors are walked to by the spot in front of them first. Stuck (not
moving) it backs off, turns and jumps; after two failed walks in a row it escapes,
trying eight directions. Open land (the meadows) is walked straight.

**The keeper** (`BotKeeper.cs`) lives as long as the client: back at the main menu it
clicks Play after 15 seconds, and it closes a game menu left open. It also saves a
picture of the bot's game view when the watcher asks.

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
| | `stuck` | within 2.5 m for 30 s while walking (standing still online or at a panel is not stuck, unless the step is a walk) |
| | `out-of-bounds` | past the zone's map by 5 m, or 10 m below the zone |
| | `thrashing` | four sharp reversals in 5 s with little gained |
| | `walk-failed` | a walk given up after its tries at working round something |
| `BotZoneJudge` | `zone-churn` | more than 8 zone changes in a minute |
| | `zone-ping-pong` | back and forth between the same two zones 3 times running, each stay under 10 s |

The same kind for the same bot is written at most once a minute. Each finding is a
folder, complete on its own, so it can be reviewed without anyone having watched:

| File | What |
| --- | --- |
| `finding.json` | what the judge saw: where, on what, doing what (goal and activity), which step, heading where, where it was before or its zone changes |
| `picture.png` | the game view at that moment |
| `client.log` | the bot's last 300 log lines |
| `server.jsonl`, `server.txt` | every server record about that player in the minute either side, and a readable summary (added by the watcher) |

## Adding to it

**A game feature gets its bot behaviour from one file** under `game/dev/features/`: a
class that implements `IBotFeature` and adds its activities and goals to the catalog.
`BotCatalog` finds every such class when bots start, so nothing else needs editing.
`BotWhoisFeature.cs` is the model:

```csharp
public sealed class BotWhoisFeature : IBotFeature
{
    public void AddTo(BotCatalog catalog)
    {
        catalog.Add(new BotActivity("edit my Whois page", 2, body => body.Has(ItemType.Phone), body =>
            new BotPlan()
                .Equip(ItemType.Phone)
                .Phone()
                .Click(TerminalScreen.AppGroupPrefix + TerminalApps.Whois)
                .Click(TerminalScreen.WhoisMineGroup)
                .Type(TerminalScreen.WhoisPlanGroup, "LFG substation repair")
                .Click(TerminalScreen.WhoisShowSkillsGroup, "", true)
                .Close()
                .Steps));
    }
}
```

**`BotPlan`** writes a plan as it reads: `Door`, `WalkTo`, `Use`, `UseOnce`, `Click` (a
group's button, or the one on the row naming an item), `Type`, `Press`, `Equip`,
`Phone`, `Pause`, `Wander`, `Say`, `Close`, and `Do` or `Step` for anything else. Each
adds one step with its time limit and its way out.

**Try it** with one bot that does only that, again and again, in a window to watch:

```
.\scripts\bot-try.ps1 "edit my Whois page"
```

The same replays a finding: run the activity it names, with the persona it names.

- **What a new screen needs:** its buttons and fields in a group (`AddToGroup`), so bots
  find them as a person finds them by looking, and its panel type in
  `BotBody.IsPanel` so bots can close it. Buttons whose label says Quit, Leave to main
  menu or Delete are never pressed by the curious bot.
- **A goal:** a `BotGoal` added the same way: its `Next` looks at what the bot has and
  names the next activity, or null when the goal is met (with `GiveUp` set when it
  cannot be). Give it a budget and its usual length, for the random drop.
- **A persona:** a case in `BotPersonas.Get`: its factors on activities and goals
  (`Likes`), its own activities (`Own`), its pace and chances; then its name in the
  default mix in `scripts/bots-up.ps1`.
- **A judge:** a class like `BotZoneJudge`, ticked from `BotDriver`, writing through
  `BotFindings.Write`. Keep the checks quick and the limits loose.
- **A dev scenario** for the feature too (`testing.md`): the scenario checks the
  feature in seconds; the bots play it for hours.

## Next

Agreed on 2026-09-27, in this order:

1. **The bots' screen.** Bot windows are small, and the UI has no scaling, so buttons
   fall off the edge. Bots will draw their UI at 1280 by 720 and scale it into their
   window. The real fix, a UI that fits any window, belongs to the HUD and menu redo.
2. **Scenarios as activities.** The dev scenarios and the bots share one step library,
   so each scenario is also a bot activity (without its setup and checks), and a new
   feature is played by bots as soon as its scenario exists.
3. **Headless bots**, for runs nobody watches. A headless client draws nothing, so its
   findings come without pictures; visible windows stay the default while they matter.
