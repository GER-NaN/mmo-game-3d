# bot-watch

Watches a bot run (`scripts/bots-up.ps1`) and gathers what the bots' judges find into
one page to review. The framework itself is described in
`docs/engineering/bot-testing.md`.

```
python tools/bot-watch/watch.py                one look now
python tools/bot-watch/watch.py --every 120    a look every 2 minutes, until the server stops
python tools/bot-watch/watch.py --no-shots     flag, but take no pictures
python tools/bot-watch/report.py               only complete the findings and rewrite the page
```

Each look prints one line: whether the server is up, how many bots are fine, the
findings so far, and each bot that is not fine: idle (no action since the last look),
looping (one action is most of what it did), not running, refused at login, with new
errors in its log, or with new judge findings. New warnings and errors in the server's
diagnostics are listed too. A flagged bot's picture goes to
`%TEMP%\mmo-game-3d-bots\shots\`: the watcher leaves a request file, and the bot's own
client (`game/dev/BotKeeper.cs`) saves its game view there, so nothing else on the
screen is ever in it.

## Memory

```
powershell -File toolsbot-watch\memory.ps1                  every 5 minutes, until the server stops
powershell -File toolsbot-watch\memory.ps1 -EverySeconds 60
```

Adds a row per process to `%TEMP%\mmo-game-3d-bots\memory.csv` on each look: the time,
the process id, who it is (`server`, or the bot's profile), and its working set and
private memory in MB. A new process id for a profile is a bot that restarted. A number
that only climbs over hours is a leak.

## Findings

The judges in each bot (`BotPositionJudge`, `BotZoneJudge`) write a folder per finding
under `%TEMP%\mmo-game-3d-bots\judge\`:

| File | What | Written by |
| --- | --- | --- |
| `finding.json` | what the judge saw: where, on what, doing what, heading where, where it was before (or its zone changes) | the bot |
| `picture.png` | the game view at that moment | the bot |
| `client.log` | the bot's last 300 log lines | the bot |
| `server.jsonl` | every server record about that player in the minute either side, as written | `report.py` |
| `server.txt` | the same, readable: what the server received and sent, zone changes, warnings and errors | `report.py` |

`report.py`, run on every look, completes new folders and rewrites
`judge\report.html`: every finding, newest first, with its picture and links to its
files. Open it in a browser. `judge\findings.jsonl` lists every finding, a line each.

Where the watcher has looked so far is kept in `%TEMP%\mmo-game-3d-bots\watch-state.json`;
delete it to start over. Needs Python 3.
