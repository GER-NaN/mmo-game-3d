# overseer

The Overseer runs bots: it starts each bot in its own game client, reads what the bot
reports, and stops it. The design is `docs/features/bots.md`. The bot itself is one
folder up, in `game/bots/`: `BotRunner` runs one activity from `BotActivities.cs`, step
by step.

This folder is a .NET console project of its own, inside the Godot project: the game's
`.csproj` leaves it out of its build (`Compile Remove`), and `.gdignore` keeps the
Godot editor out of it.

Run it with the script, which builds everything first:

```
.\scripts\bots-run.ps1                            one bot, to the main menu
.\scripts\bots-run.ps1 -Bots jump:connect         into the world as its own kept player
.\scripts\bots-run.ps1 -Bots quit,jump:connect    several at once
.\scripts\bots-run.ps1 -All                       every activity, at once
```

A bot is `activity`, `activity:connect` (a kept player of its own, `bot-<activity>`) or
`activity:fresh` (a new player every launch). The script's header lists them.

Every bot starts at once. A bot passes when it reported `done` and its client quit with
exit code 0; the run passes when every bot passed. After a bot's `done` or `failed` the
Overseer leaves its stop file; a bot that has not quit 10 seconds later is killed. If any
bot connects and nothing listens on the server's port (UDP 7070), the Overseer starts
the server first, headless and without a window, and leaves it running; stop it with
`scripts/server-stop.ps1` so it saves.

## How a bot is started

```
Godot --path <project> --scene res://game/bots/BotMain.tscn --audio-driver Dummy
      --log-file <folder>/client.log
      -- --windowed --settings-file <folder>/settings.cfg
         [--autoconnect --profile bot-<name> --name <name, 16 characters at most>]
         --bot-folder <folder>
```

`BotMain.tscn` inherits `Main.tscn` and adds one child, the `BotRunner`, so the game's
own code does not change and node paths stay the same as the server's. The options
after `--` are the game's (`game/LaunchOptions.cs`), which ignores `--bot-folder`.
`--settings-file` keeps the machine's settings out of reach: a bot reads and saves its
own.

## The folders

Each run is a folder `bot-runs/<yyyyMMdd-HHmmss>/`, with one folder per bot, named after
its activity:

| File | Written by | What |
| --- | --- | --- |
| `bot.json` | the Overseer | what the execution is: its name and the activity it runs |
| `events.jsonl` | the bot | one JSON line per event: `time`, `kind`, `detail`; a `step` line as each step starts |
| `client.log` | Godot | the client's whole log |
| `settings.cfg` | the game | the execution's own settings, if the game saved any |
| `stop` | the Overseer | asks the bot to quit; the bot checks for it four times a second |

`bot-runs/` is ignored by git, and holds a `.gdignore` so Godot never imports what is
in it.
