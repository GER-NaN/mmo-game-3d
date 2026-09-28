# World events

How world events are built. The design is `docs/features/world-events-mvp.md`, and the
decisions are in `docs/world.md` section 2 (world events) and section 4 (Notifications).

## The pieces

- **Rules** (`src/Rules/Events/WorldEvents.cs`, tested in
  `tests/Tests/Rules/WorldEventTests.cs`):
  - `WorldEventDefinition`: one kind of event as its row holds it (family, kind, line,
    zone, spot, count, time limit, interval).
  - `WorldEventSchedule`: the first event one interval after the server starts, the
    next one interval after the last ended, one at a time.
  - `WorldEventRun`: who took part (once each), the end check (every enemy down is
    completed; the time limit is timed out), and the drops (one per player who took
    part, only when completed).
  - `WorldEventOutcome`: completed, timed out, ended by a restart. A swarm reaching its
    own goal comes later, as another value.
- **Store** (`src/Data/Events/WorldEventStore.cs`, migration
  `0016_world_events.sql`): the definitions, the runs, who took part, and each
  player's points. Taking part adds the point in the same transaction, once per run.
- **Server** (`game/server/world/ServerWorldEvents.cs`): the schedule, the start, the
  participation check each tick, the end, the drops, and the rows sent to the
  Notifications app. The live event is only in memory. At start it ends every run the
  database still has as running, as ended by a restart.
- **Drones** (`game/server/world/ServerDrones.cs`): one instance a zone. The town's patrols
  (a pair when there is none). The meadows' only swarms (`SpawnSwarm`). The EMP goes to
  the instance of the player's zone.
- **Client**: the Notifications app (`TerminalApps.Notifications`, on terminals and the
  phone) in `game/ui/TerminalScreen.cs`. The server sends it rows
  (`TerminalNetwork.SendEvents`) when a terminal or the phone opens, and to every
  player online when an event starts or ends.

## Changing an event

The definition row is the setting. Edit `world_event_definitions` (count,
time_limit_seconds, every_seconds, enabled) and restart the server. The spot is a
`Marker3D` under the zone's `Events` node, named in the row's `spot`. To move it, move
the marker in the scene.

A new kind in the same family (RC cars, robots) needs:

1. its row;
2. a spawn that `ServerWorldEvents.Start` calls for its kind;
3. the count of what is still up, for the end check.

The rows, the points and the app need no change.

## Testing it

- `dotnet test`: the rules, the store, and the bots' judge (`WorldEventCheck`).
- Dev scenario `swarm` (`.\scripts\scenario-test.ps1 -Scenarios swarm`). The player
  stands at the spot with an EMP worn. A swarm of two starts at once. The player brings
  it down, then reads "completed" and "you took part" in Notifications.
- Bot activity "check world events"
  (`game/dev/bots/activities/CheckWorldEventsActivity.cs`). The bot takes the phone out and reads
  Notifications. With the swarm on, a coin flip decides if it goes. If it goes, it
  hunts with an EMP (if worn) while drones fly, then reads the past row. The `eventer`
  persona checks often. Try it alone with `.\scripts\bot-try.ps1 "check world events"`;
  the first swarm is 5 minutes after the server starts.
