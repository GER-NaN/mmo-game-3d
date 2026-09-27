# mmo-game-3d

A small client/server MMO set in a near-future town fighting a rogue AI, in 3D. The
game's design is `docs/world.md`; this repo builds it.

- Client and server: Godot 4.7 with C# (.NET 8). The server is the same project run
  headless; networking is Godot's built-in multiplayer, and the server is the authority.
- Database: Postgres (`mmo3d`, and `mmo3d_test` for tests), migrations in
  `src/Data/Migrations`, applied by the server at start.
- Game rules live in plain C# (`src/Rules`) with unit tests; nodes call them.
- Art (KayKit, Tiny Treats) and sound are not in git: see `assets/README.md`.

## Running

```
.\scripts\server-up.ps1          the server, headless, in its own window
.\scripts\client-up.ps1          the game (-Profile name for a second player)
.\scripts\server-stop.ps1        stops the server so it saves
```

## Layout

| Folder | What |
| --- | --- |
| `game/` | Godot scenes and nodes: client, server, zones, UI, networking |
| `src/Rules/` | game rules, plain C#, tested |
| `src/Data/` | Postgres stores and migrations |
| `src/Diagnostics/` | OpenTelemetry logs and traces to JSON lines |
| `tests/` | unit tests (rules and stores) |
| `scripts/` | run, stop, load test, scenario tests, native build |
| `tools/` | prop generator, model renders, diagnostics query, sound copy |
| `native/` | the C++ GDExtension for the packet log |
| `docs/engineering/` | how things work and what was measured |

## Docs

`docs/README.md` explains the whole docs folder. The design is `docs/world.md` (canon)
and `docs/backlog.md` (not yet decided). For the code, start with setup, then running;
making-changes has the recipes.

- `docs/engineering/setup.md`: a machine from nothing (tools, Postgres and the two
  databases, art and sound, first run).
- `docs/engineering/running.md`: the scripts, launch options, profiles, where data
  lives, resetting, playing with someone else.
- `docs/engineering/editor.md`: working in the Godot editor: running, zones, props,
  doors, what it does not show.
- `docs/engineering/making-changes.md`: recipes: a zone, a door, a model as a prop,
  placing it, a building to enter, an item, a skill, a mechanic, a person, a sound, a
  gesture, an RPC, an intent, a migration, synced state, a terminal app; the git flow.
- `docs/engineering/example-tree-chopping.md`: a new mechanic from end to end (not
  built), to copy from.
- `docs/engineering/testing.md`: unit tests, dev scenarios, bots, load tests.
- `docs/engineering/sound.md`: the sound catalog, the director, where sounds come from.
- `docs/engineering/diagnostics.md`: logs, traces and the packet log.
- `docs/engineering/load-test.md`: how many players a zone holds.
- `docs/engineering/performance.md`: server load scenarios, what was fixed, what is not
  planned.
- Each tool has a README in its folder.

## Outside this repo

- The older `mmo-game` repo: the MonoGame and voxel version, and the git history of
  the design docs, which were copied here on 2026-09-26 (from its commit `ac8ae05`).
- The art library `C:\game-art` and the sound library `C:\game-sound`, on the author's
  machine (assets/README.md says what is copied from each).
- The Postgres container `game-db`, shared with `mmo-game` (setup.md).
- The play-test checklist and the sound plan are private pages on claude.ai:
  https://claude.ai/artifact/SC2qn9pVeZu9ARAJGB6Nuj and
  https://claude.ai/artifact/1MKB3GyhdAnXvWJCbGj3oD
