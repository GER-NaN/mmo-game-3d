# Load test: how many players one zone holds

Measured 2026-09-26 with `scripts/load-test.ps1`, on the author's machine (16 cores),
server and bots on the same machine over localhost. Godot 4.7.2, the built-in
multiplayer: one `MultiplayerSynchronizer` per player body syncing `NetPosition` and
`NetYaw` at 20 Hz (`replication_interval = 0.05`).

The worst case on purpose: every bot is in the town, 100 x 100 m, so every player sees
every other and the sync work grows with the square of the player count.

## Results

| Players | Server fps | Physics steps/s | Frame (ms) | Out (KB/s) | Verdict |
| --- | --- | --- | --- | --- | --- |
| 10 | 145 | 60 | 0.5 | 67 | Idle |
| 50 | 145 | 60 | 4 to 5 | 1,300 | Comfortable |
| 100 | 136 | 60 | 13 to 27 | 4,500 to 4,700 | Holds |
| 200 | 6 to 7 | 60 | 53 to 84 | 8,000 to 8,400 | Saturated |
| 300 | 3 to 4 | 60 | 160 to 300 | 11,000 to 17,600 | Does not hold |

"Server fps" is process frames. Physics keeps its 60 steps a second by running several
steps in a slow frame, but the sync to clients runs once per process frame: at 200
players every client's view updates about 6 times a second instead of 20, and at 300
about 3. Traffic stops growing with players once the server cannot send more often.

Per pair of players who see each other, the cost at 20 Hz is about 0.47 to 0.67 KB/s.

## What this says

- The built-in synchronizer holds about 100 players who all see each other, on this
  machine. Somewhere between 100 and 200 it saturates. 300 in one place does not work
  this way.
- The cost is the per-synchronizer, per-peer sync, not physics: physics steps took a
  few ms each even at 300.
- 300 players on one server spread over zones is a different, much lighter case: sync
  cost follows who sees whom, and zones already cut that.

## Ways forward, not decided

The options from the earlier discussion, unchanged: interest management by distance
(fewer pairs where people spread out, no help in a packed plaza); a lower update rate
for far players; or replacing the per-body synchronizer with one packed snapshot per
client per tick sent with `SceneMultiplayer.SendBytes` (keeps Godot, scenes and physics;
the sync becomes a tight C# loop over arrays, and positions can be quantized). Which to
take is a design decision for the author.

## The harness

- `--load-test N` runs N bot clients in one Godot process, each a whole client in its
  own branch of the tree with its own multiplayer (`SceneTree.SetMultiplayer`), so its
  own connection. About 50 bots per process before the process crawls: each bot is a
  full client with its own copy of the town. The server numbers are not affected.
- Load bots walk through their own input (`IPlayerInput`), since Godot has one `Input`
  per process; the normal `--bot` acts through the real keyboard and mouse.
- `--stats-every N` on the server prints the line the table above comes from.
