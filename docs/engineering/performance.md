# Server performance

Load tests and what they found. Numbers are from the author's machine (16 logical
cores), the server and the bot clients on the same machine, diagnostics on.

## How to measure

- `scripts/load-test.ps1 -Bots 100 -Seconds 60 -StatsEvery 10` runs a server and
  wandering bots. `-Scenario load-phone | load-defense | load-taxi | load-chat` makes
  every bot keep doing one thing instead (game/dev/load/LoadBot.cs; the server sets each up
  through its dev scenarios).
- The server's `Stats:` line has the frame rate, the engine's frame and physics times,
  traffic, the worst frame, the .NET collector's pauses, and the five parts of the
  server's own tick that took the most (game/server/core/TickProfile.cs).
- `python tools/diag-query/query.py --durations` ranks every span (RPC handlers,
  database calls and their queue waits) by total time.

## Found and fixed (2026-09-26)

| Where | Before | After |
| --- | --- | --- |
| A body spawning, or a door: every synced node in the zone re-asked its C# visibility filter for every peer (nodes x peers calls into C#) | 8 ms mean, 16 p95, per arrival at 100 players | 3.4 mean, 5.2 p95: only the one who moved is asked |
| The online roster: built per viewer and sent every 2 s, each copy converted and logged | 13 ms frames every 2 s with 100 online | built once, sent only on change, converted and logged once (`SendToMany`): 2 to 4 ms while players join |
| Status board posts, each to every online player as its own copy | scoring an Agent Defense run 3 ms mean, 12.5 max | 1 ms mean, 3.3 max |
| Saves: all players queued at once every 30 s | other database calls waited 0.7 s on average | saved a few a frame in turn: other calls wait under 1 ms |
| Chat lines, one copy a player | 0.77 ms a line | 0.61 ms a line |
| Load bots saving the machine's settings.cfg at once | the file broken | tool-run clients never save it |

## Where it stands

- 100 players: about 140 fps; worst frames 12 to 20 ms; the server's own game logic is
  about 5 ms a second in all. The rest is the engine: replication (everyone in a zone
  is sent to everyone there, n squared) and physics.
- 200 players: 7 to 10 fps, frames 55 to 70 ms, worst about 140. Not the test
  machine: the same with the bots at idle priority. As in load-test.md, the cost is
  the per-synchronizer, per-peer sync. The engine's physics time (11 to 17 ms at 200)
  is per frame, and a slow frame runs several of the 60 steps a second, so a step is
  about 1.5 to 2 ms. Jolt physics (a project setting, tried and reverted) gave 13 to 14
  fps: a little better, not a fix.
- A `GD.Print` on the server costs about 2.5 ms of the main thread on Windows, with or
  without diagnostics, flushing or the file log, and with the console or plain exe;
  the cause was not found. The server prints on logins, leaves, rides and breakages.
- The .NET collector pauses about 10 ms in 10 s, at most one full collection: not a
  cause of the long frames.

## Not planned

The author decided (2026-09-26) not to optimize further for now: 100 players in one
place hold, and 200 is not a goal yet. Recorded so they are not rediscovered:

- Replication: interest management, a lower rate for far players, or one packed
  snapshot per client (load-test.md has the options).
- Physics: Jolt, a lower server physics rate, or moving players without physics bodies
  on the server.
- Server prints: route routine lines through the diagnostics log only, or find why a
  print costs milliseconds on Windows.
- A robo taxi ride loads its own cabin zone: about 10 ms a call, up to 20. Many rides at
  once would want pooled cabins.
