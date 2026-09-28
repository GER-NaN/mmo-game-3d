# Testing

Prove game rules with unit tests, and prove that the scenes survive a save in the editor
with the scene check. Nothing drives the client by itself at present: the bot framework,
the dev scenarios and the load test were taken out to be rebuilt with care.

## Unit tests

```
dotnet test tests/Tests
```

- `tests/Tests/Rules`: the plain C# game rules in `src/Rules` (skills, careers, the code
  cracker, Agent Defense's chart and scoring, the subway wall, rootkits, ...). No engine,
  no database.
- `tests/Tests/Data`: the stores in `src/Data` against Postgres, database `mmo3d_test`
  on the local server (or `MMO3D_TEST_DB`). Migrations run first. Tests that need a
  clean board or wall make their own (a new objective or wall name per test), since
  the test database is not emptied between runs.

The big meaningful cases, not every edge. When a change breaks a test, ask before
changing the test.

## Scene check: nothing lost on an editor save

```
.\scripts\scene-check.ps1
```

Every scene must stay editable in the Godot editor. The editor saves a scene by packing
the edited tree, and packing keeps a change to an instanced scene's child only when that
instance has editable children (`[editable path="..."]` in the file). The game applies
such a change either way, so a scene written by hand can work in the game and lose the
change on its first save in the editor: the subway door lost its 2 m trigger that way.
The check (`game/dev/SceneCheck.cs`) packs every scene under `game/` the same way and
lists each property or node the save would lose, then exits 1. Run it after writing or
changing a `.tscn` by hand.

## Looking at a client

- `--report-every N`: a client prints what it sees (the bodies and where they are).
- `--screenshot x.png`: a client saves its window a few seconds in, then quits.
