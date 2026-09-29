# Running the game

## Scripts

| Script | What |
| --- | --- |
| `scripts/server-up.ps1` | builds, then the headless server in its own console window (port 7070) |
| `scripts/server-stop.ps1 [-Port 7071]` | asks the server to save everyone and quit; use it rather than closing the window |
| `scripts/client-up.ps1 [-Profile name] [-AutoConnect]` | builds, then the game |
| `scripts/scene-check.ps1` | what an editor save would lose from each scene (testing.md) |
| `scripts/native-build.ps1` | the C++ packet log (native/README.md) |

Ctrl+C or closing the server's window is a hard stop: players online are not saved.
`server-stop.ps1` writes a stop file the server checks each frame.

Killing any Godot process from outside while C# runs prints "Fatal error. Internal CLR
error" from `GetCurrentStackInfo`. That is the kill, not a bug.

## Launch options

Arguments after `--` on Godot's command line are the game's own; the scripts pass them.
The full list, with defaults, is the comment at the top of `game/LaunchOptions.cs`. The
ones used most:

| Option | Side | What |
| --- | --- | --- |
| `--server` | server | run as the headless server (with Godot's `--headless`) |
| `--port N` | both | default 7070 |
| `--db "Host=..."` | server | another database |
| `--time-offset 6` | server | shift the world's hour, to see night by day |
| `--diagnostics off` | server | no logs and traces |
| `--log-packets` | server | also every packet (needs the native build) |
| `--profile name` | client | which player; `fresh` is a new one each launch |
| `--name Gerald` | client | the name a new player gets |
| `--autoconnect`, `--address 1.2.3.4` | client | skip the menu; connect elsewhere |
| `--report-every 3` | client | print what it sees |
| `--screenshot x.png` (+ `--overview`, `--garden`, `--creator`, `--show-characters`, `--screenshot-after 4`) | client | save a picture and quit: how looks were checked without clicking |

## The graphics panel (F9)

In a debug build (the editor, `client-up.ps1`), F9 shows or hides a small panel of knobs
on how the world looks: edges (MSAA, FXAA or SMAA, TAA), pixels (3D resolution,
nearest upscaling, pixelate, posterize), tone and colour, light (glow, SSAO, SSIL, SSR,
SDFGI, shadows), air (fog, volumetric fog), the camera (field of view, blur, the hour of
day) and screen effects (vignette, grain, colour fringe, scanlines, sharpen, tint). A
knob applies at once; Reset goes back to how the game started. Save writes every value
and what changed as JSON, with a screenshot, to `graphics-saves/` in the repo (git
ignores it). Nothing is kept between launches: a look is made real by writing its values
into `game/zones/World.tscn` or `project.godot`. The code is in `game/dev/`
(`DevGraphics`, `GraphicsPanel`, `ScreenEffects.gdshader`).

## Controller

An Xbox-layout controller works in the world, beside the keys. The bindings are in
`project.godot` (Project Settings > Input Map in the editor); rebinding keys in the
game's settings leaves them alone. The layout is a placeholder until played.

| Control | Action |
| --- | --- |
| Left stick | walk forward and back, strafe |
| Right stick | left and right turn; up and down tilt the camera |
| LT / RT | zoom out / in |
| A | jump |
| X | use (interact) |
| Y | phone |
| RB | EMP |
| D-pad | up map, down bag, left social, right skills (Back is the map too) |
| B, Start | close what is open, else the game menu |

Menus and panels still need the mouse: Godot's `ui_accept` has no controller button
here, and the panels do not take focus. Chat needs the keyboard.

## Players and profiles

A client's identity is a license key in `profiles/<name>/license.txt` under Godot's user
data folder. The same profile is the same account every launch; a second player on the
same machine needs another profile (`client-up.ps1 -Profile second`). The profile
`fresh` makes a new account each launch and saves nothing. An account has two character
slots. Real authentication does not exist yet: the key file is the whole secret.

## Where things are on disk

Godot's user data folder is `%APPDATA%\Godot\app_userdata\mmo-game-3d`:

| Path | What |
| --- | --- |
| `settings.cfg` | the machine's settings: display, address, mouse, camera, keys, volumes, names. Clients run by tools never write it. |
| `profiles/<name>/license.txt` | each profile's account key |
| `diagnostics/server-<port>-<time>.jsonl` | the server's logs and traces, a file per run (`tools/diag-query`) |
| `server-stop-<port>` | the stop request `server-stop.ps1` writes |
| `logs/` | Godot's own log files |

## Resetting

- The world and every player: drop and recreate `mmo3d`; the server migrates it again
  at start.

  ```
  docker exec mmo3d-db psql -U mmo -d postgres -c "drop database mmo3d with (force);"
  docker exec mmo3d-db psql -U mmo -d postgres -c "create database mmo3d;"
  ```

  The dev database may hold test players from the old bot and scenario runs ("Test
  cracker", "Load0", "Soak1", ...) and their house plants, subway tags and scores.
- The test database: the same with `mmo3d_test`; tests never need it empty.
- A client's identity: delete its `profiles/<name>` folder (the old account stays in
  the database, unreachable).
- Settings: delete `settings.cfg`.

## Playing with someone else

The server listens on UDP (ENet) port 7070. Another machine types the server's address
on the main menu. Allow the port through the Windows firewall (Windows asks on the
server's first run); over the internet it needs a port forward, and traffic is not
encrypted. Clients must be the same build: the server refuses a client whose
`GameVersion.Protocol` differs, with a clear message.
