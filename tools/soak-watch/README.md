# soak-watch

Watches a soak run: the bots started by `scripts/bots-up.ps1`, and the server.

```
python tools/soak-watch/watch.py                one look now
python tools/soak-watch/watch.py --every 300    a look every 5 minutes, until the server stops
python tools/soak-watch/watch.py --no-shots     flag, but take no screenshots
```

Each look prints one line: whether the server is up, how many bots are fine, and each
bot that is not: idle (no action since the last look), looping (one action is most of
what it did), not running, refused at login, with new errors in its log, or with new
judge findings. New warnings and errors in the server's diagnostics file are listed
too.

A flagged bot's picture is saved to `%TEMP%\mmo-game-3d-bots\shots\`: the watcher
leaves a request file, and the bot's own client (`game/dev/BotKeeper.cs`) saves its
game view there. Nothing else on the screen is ever in it.

Where it looks since the last look is kept in `%TEMP%\mmo-game-3d-bots\watch-state.json`;
delete it to start over. Needs Python 3.

## The judge

Each bot judges its own position (`game/dev/BotPositionJudge.cs`): what is under its
feet (a player stands on `Ground`, `Roads`, `Terrain`, `Room` or `Cabin`, never on a
car, a roof or a tree), and whether it has stayed within a few metres for minutes while
walking. A finding is written once per kind per bot per 10 minutes, to
`%TEMP%\mmo-game-3d-bots\judge\findings.jsonl`, with a picture of the game view.

```
python tools/soak-watch/judge_report.py
```

writes `judge\report.md` beside it: every finding with its picture, the client's side
(where, on what, doing what, heading where, where it was before) and the server's
(what it received from and sent to that player in the minute around it, zone changes,
errors).

What the runs turn up is written up in `docs/engineering/soak-findings.md`.
