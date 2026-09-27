# Soak runs: leaving the game running and watching it

**Started:** 2026-09-22. Status: one run observed, nothing formalized yet.

The idea, in the author's words (2026-09-22):

> I read an article about the original doom creators who let Doom run day and night with
> real clients doing play testing. I want to do something similiar, run real clients
> against the game and watch the behavior long term (do bots get stuck anywhere, do we
> have any memory issues, etc... find edge cases).

The source is a video the author could not place again, so nothing here cites it. The
practice it described is the point, in the author's words (2026-09-22):

> Soak Runs with robust diagnostic logging.

This file holds what a soak run is for, what the first one showed, and what the game
would have to report for the next one to be worth more than this one.

## The first run, 2026-09-22

Not planned as a run: three bot clients were started at 06:36 and forgotten until 13:31,
so they ran about 7 hours against the server in Docker.

What was running:

- Three `Client.exe`, all `--bot recruiter,join-party,wander`, profiles `bot-01` to
  `bot-03`, from `src/Client/bin/bots`.
- `game-instance` (the server) and `game-db` (postgres 18) in Docker.
- One zone occupied: all three bots in zone 1. Zone 2 held nobody.

### What was measured

| Measure | Value |
|---|---|
| Server container | 0.52% CPU, 62 MB memory, up 7 hours |
| Database container | 0.00% CPU, 55 MB memory |
| Server traffic | 813 MB sent, 34.4 MB received |
| Messages received | 480,032, about 19 per second for three clients |
| World items, zone 1 | 150 |
| Client CPU | about 2,390 s each, so 9.5% of one core each |
| Client memory (private) | 269 MB, 317 MB, 324 MB |
| Client handles / threads | 629 / 15, identical on all three |
| Sessions | all three alive, last activity within the same second |

### What was checked and found correct

- **The 150 items are not a leak.** `ServerOptions.InitialItemCount` is 150, zone 1 is
  the starter zone, and `RespawnItems` tops a zone up to `TargetItemCount`, one item per
  8 seconds while below it. 66 GpuCore and 84 RamStick is the loot table doing its work.
- **No errors, rejections or retries.** In the last 4,000 log lines, 6 were not world
  views: three inventory sends and two counters.
- **No memory growth while watched.** Ten samples over five minutes: private bytes flat
  to 0.1 MB, handles and threads unchanged. One earlier step (310 to 323 MB on two
  clients over four minutes) did not continue, so it reads as ordinary GC behaviour.
  Seven hours at 270 to 324 MB is a plateau, not a climb.

### What the traffic says

A world view for one of these bots holds 3 players, about 7 items and 2 terminals:

```
 17 bytes  header and four list counts
132 bytes  3 players   (16 id + 1+name + 8 position + 3 flags = 44 each)
210 bytes  7 items     (16 id + 1 type + 1 tier + 8 position + 4 quantity = 30 each)
 52 bytes  2 terminals (16 id + 1 type + 8 position + 1 enabled = 26 each)
= about 410 bytes, 30 times a second, per client
```

Three clients at that rate is about 37 KB/s, which is 930 MB over seven hours. Docker
measured 813 MB. So the model of the wire cost is right within about 15%, and it can be
trusted for the questions in `docs/features/message-delivery-and-performance.md`.

Two numbers stand out:

- **24 to 1.** The server sends 24 bytes for every byte a client sends. Almost nothing
  in those bytes changed since the tick before.
- **About 27 players.** At 44 bytes per player, the player list alone fills the
  1200-byte datagram at about 27 visible players, before items and terminals.

## What made this run hard to read

The run produced almost no evidence. Everything above came from live sampling at the
end, not from anything the game recorded while it ran.

1. **The server logs one line per client per tick.** 90 lines a second, about 2.3
   million lines in seven hours, with no rotation configured in `docker/docker-compose.yml`.
   It costs disk and CPU, and it buries the events that matter: six interesting lines in
   four thousand.
2. **Nothing keeps a history.** `server-state.json` is a snapshot of now
   (`src/Server/Diagnostics/ServerStateDumper`), so a question like "when did memory step
   up?" or "was that bot stuck at 09:00?" cannot be answered afterwards.
3. **The client's own stats file was not on.** `--stats <path>` writes a CSV per frame
   (time, position, memory, CPU, frame timings), and `tools/stats-hitches` reads it, but
   the bots were started without it.
4. **Nothing watches for "stuck".** A wandering bot that walks into a wall for six hours
   looks exactly like a healthy one in every number above.

## Recommendations, not yet agreed

Ordered by what the next run needs most. None of this is built.

1. **Make the per-tick log lines stop being the default.** Options: a log level, a
   sampled line (one per N ticks), or a counter line every few seconds instead of a line
   per send. The event lines (connect, disconnect, rejection, zone change, intent) stay.
2. **Record counters over time, not only a snapshot.** The pieces exist:
   `src/Server/Utils/Counters.cs` and the state dumper. A line appended every few seconds
   with tick time, messages in and out, bytes out, session count and player count per
   zone would answer most questions a soak run raises, and it is one small file.
3. **Give the tick a measured budget.** At 30 ticks a second the budget is 33 ms.
   Recording the tick's own time (mean and worst in the period) turns "the server feels
   fine" into a number, and it is the same number the performance tests in `TODO.md`
   would assert against.
4. **Run the bots with `--stats` in a soak run**, one CSV per client, so client memory,
   frame time and position history are on disk instead of sampled by hand at the end.
5. **Add stuck detection to the bot runner.** A wander bot that has not moved more than
   a few units in a minute writes one line saying where. That is the cheapest way to
   find the edge cases this exercise is for.
6. **Cap the container log** in `docker/docker-compose.yml` (`max-size`, `max-file`) so a long
   run cannot fill a disk.
7. **Write down what a run is.** How many clients, which behaviours, how long, what is
   collected, and what is compared afterwards. A short checklist is enough.

## Open questions

- Where do the soak artefacts live? A run folder per start time, with the server
  counters file and each client's CSV, would keep runs comparable.
- Does the server counters file belong in the container's volume, or should the server
  expose the numbers and let a small tool on the host poll them?
- What is worth alerting on during a run, rather than reading afterwards?
