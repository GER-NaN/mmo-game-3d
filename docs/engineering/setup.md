# Setup: a machine from nothing

Everything the game needs that is not in git, in the order to do it. Windows, as the
author's machine.

## 1. Tools

| Tool | Why | Notes |
| --- | --- | --- |
| Godot 4.7.2 .NET (mono), Windows | the engine, client and server | A portable download. Keep the `GodotSharp` folder next to the exe. The scripts expect it at `C:\Users\geral\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\`; elsewhere, pass `-Godot <path to ..._console.exe>` to each script. |
| .NET 8 SDK | builds the C# | `dotnet build`, `dotnet test`. |
| Docker Desktop | Postgres | Or any Postgres 16+. |
| Python 3 | the tools in `tools/` | `Pillow` only for image montages. |
| Git, and the GitHub CLI `gh` | the repo and pull requests | `gh auth login` once. |
| Visual Studio Build Tools 2026, "Desktop development with C++" | only for the native packet log | See `native/README.md`. Not needed to play or test. |

## 2. Postgres

The author's container is `game-db`, shared with the older `mmo-game` repo (it made the
container; its data is in the Docker volume `mmo-game_game-db-data`). User and password
are both `mmo`, on port 5432. On a new machine:

```
docker run -d --name game-db -e POSTGRES_USER=mmo -e POSTGRES_PASSWORD=mmo -e POSTGRES_DB=mmo -p 5432:5432 -v game-db-data:/var/lib/postgresql postgres:18
```

Then make the two databases this repo uses. They were made by hand; nothing makes them:

```
docker exec game-db psql -U mmo -d postgres -c "create database mmo3d;"
docker exec game-db psql -U mmo -d postgres -c "create database mmo3d_test;"
```

The tables come from the migrations in `src/Data/Migrations`: the server applies any
new ones at start (it prints "migrations applied: ..."), and so do the data tests on
`mmo3d_test`. The connection strings are in `src/Data/Database.cs` (server; override
with `--db`) and `tests/Tests/Data/TestDatabase.cs` (tests; override with the
environment variable `MMO3D_TEST_DB`).

## 3. Art and sound

Not in git (licences and size). `assets/README.md` says exactly which folders to copy
from where:

- Art: KayKit (`C:\game-art\3d\kaykit-godot`) and Tiny Treats
  (`C:\game-art\3d\Tiny_Treats_Collection_1_1.0`) into `assets/kaykit/` and
  `assets/tinytreats/`.
- Sound: `python tools/audio-subset/copy.py` copies the files the catalog names from
  `C:\game-sound` into `assets/audio/`.

Then let Godot import them once (a few minutes the first time):

```
& "<godot>\Godot_v4.7.2-stable_mono_win64_console.exe" --headless --path . --import
```

Without the art the game runs with missing models; without sound it runs silent and
logs "Sound not on this machine".

## 4. Build and first run

```
dotnet build mmo-game-3d.sln
dotnet test tests/Tests
.\scripts\server-up.ps1
.\scripts\client-up.ps1
```

Godot runs the C# assembly it finds in `.godot/mono`, so an unbuilt change runs old
code; `client-up.ps1` and `server-up.ps1` build first. To open the project in the Godot
editor, run the non-console exe and open `project.godot`.

## 5. Optional

- The native packet log: `.\scripts\native-build.ps1` (see `native/README.md`).
- `.\scripts\scenario-test.ps1`: all 13 should pass on a working setup
  (docs/engineering/testing.md).
