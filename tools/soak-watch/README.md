# soak-watch

Watches a soak run: the bots started by `scripts/bots-up.ps1`, and the server.

```
python tools/soak-watch/watch.py                one look now
python tools/soak-watch/watch.py --every 300    a look every 5 minutes, until the server stops
python tools/soak-watch/watch.py --no-shots     flag, but take no screenshots
```

Each look prints one line: whether the server is up, how many bots are fine, and each
bot that is not: idle (no action since the last look), looping (one action is most of
what it did), not running, refused at login, or with new errors in its log. New
warnings and errors in the server's diagnostics file are listed too.

A flagged bot's window is saved to `%TEMP%\mmo-game-3d-bots\shots\` by `shot.ps1`,
which only reads the screen and sends the game no input. What a run finds is written
up in `docs/engineering/soak-findings.md`.

Where it looks since the last look is kept in `%TEMP%\mmo-game-3d-bots\watch-state.json`;
delete it to start over. Needs Python 3.
