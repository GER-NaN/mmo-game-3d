# Testing

Four kinds, from fastest to slowest. Prove game rules with unit tests; prove a feature
in the running game with a dev scenario; use bots and load tests for behaviour over
time and for numbers. Apart from these, the scene check proves that the scenes survive
a save in the editor.

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

## Dev scenarios: a feature, set up and tested in seconds

```
.\scripts\scenario-test.ps1                      all of them
.\scripts\scenario-test.ps1 -Scenarios defense,subway
```

The script starts a server on port 7071 with `--dev-scenarios` and runs one headless
client per scenario with `--scenario <name>`, each as a new player ("Test <name>"; names
are at most 16 characters). Each scenario must end within a minute; today the longest
takes 15 s.

- The client sends the scenario's name before it logs in. The server
  (`game/dev/scenarios/ServerScenarios.cs`) sets the player up at login: where they
  stand, what they carry, what is going on around them (the taxis infected and the job
  taken, drones up, short Agent Defense runs). Without `--dev-scenarios` the server
  ignores the request.
- The client's `game/dev/scenarios/ScenarioDriver.cs` then tests the feature through
  input (keys, clicks, typing), step by step, and prints `SCENARIO <name>: PASS` or
  `FAIL` with the step it stopped at and the notices it saw, then quits with exit code 0
  or 1.
- Scenarios: cracker, rootkit, defense, cameras, subway, book, workbench, college, lights,
  taxi, fix, garden, shop.

To add one: a `case` in `ServerScenarios.Apply` (the setup), a `case` in the driver's
`_Ready` (the steps and what to expect), and the name in the script's list. Never let a
bot wander or play its way to a feature to test it: set up exactly what the feature
needs. Scenarios share one server and one world, so keep their spots apart (a scenario's
drones once zapped another's player).

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

## Bots

- `--bot`: one client plays by itself, wandering and using what it passes (terminals,
  shops, the potting table, the code cracker). For soak runs and for seeing the world
  lived in, not for testing a feature.
- `--report-every N`: a client prints what it sees (the bodies and where they are).
- Headless clients never save the machine's `settings.cfg`.

## Load tests

`scripts/load-test.ps1`; see load-test.md for how many players a zone holds and
performance.md for what the load scenarios found and how to read the numbers.
