# overseer

The Overseer runs bots: it starts each bot in its own game client, reads what the bot
reports, and stops it. The design is `docs/features/bots.md`. The bots are in
`game/bots/`.

Run it with the script, which builds everything first:

```
.\scripts\bots-run.ps1                             to the main menu
.\scripts\bots-run.ps1 -Bot QuitBotMain            clicks Quit on the main menu
.\scripts\bots-run.ps1 -Bot FullscreenBotMain      fullscreen on and off in the settings
.\scripts\bots-run.ps1 -Bot JumpBotMain -Connect   into the world, jumps once
.\scripts\bots-run.ps1 -Timeout 60                 give it longer
```

What it does today: one bot per run. The run passes (exit code 0) when the bot reported
`done` and its client quit with exit code 0. After `done` the Overseer leaves the stop
file; a bot that has not quit 10 seconds later is killed, and the run fails.

With `-Connect` the client connects at once as the player named after the execution
(`--autoconnect --profile bot1`). If nothing listens on the server's port (UDP 7070), the
Overseer starts the server first, headless and without a window, and leaves it running;
stop it with `scripts/server-stop.ps1` so it saves.

## How a bot is started

```
Godot --path <project> --scene res://game/bots/<Bot>.tscn --audio-driver Dummy
      --log-file <folder>/client.log
      -- --windowed --settings-file <folder>/settings.cfg [--autoconnect --profile bot1]
         --bot-folder <folder>
```

Each bot scene inherits `Main.tscn` and adds one child, the bot's node, so the game's
own code does not change and node paths stay the same as the server's. The options
after `--` are the game's (`game/LaunchOptions.cs`), which ignores `--bot-folder`.
`--settings-file` keeps the machine's settings out of reach: a bot reads and saves its
own.

## The folders

Each run is a folder `bot-runs/<yyyyMMdd-HHmmss>/`, with one folder per bot execution:

| File | Written by | What |
| --- | --- | --- |
| `bot.json` | the Overseer | what the execution is: its name and scene |
| `events.jsonl` | the bot | one JSON line per event: `time`, `kind`, `detail` |
| `client.log` | Godot | the client's whole log |
| `settings.cfg` | the game | the execution's own settings, if the game saved any |
| `stop` | the Overseer | asks the bot to quit; the bot checks for it four times a second |

`bot-runs/` is ignored by git, and holds a `.gdignore` so Godot never imports what is
in it.
