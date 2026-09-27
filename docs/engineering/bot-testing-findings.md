# Bot testing findings

What the bots turned up, written up by hand: game bugs first, then problems with the
bots themselves, fixed as found. How the bots work is in `bot-testing.md`. A run's
own findings, each with its picture, client log and server records, are in its report
(`tools/bot-watch`); the ones worth keeping are written up here. Pictures are in
`docs/engineering/bot-shots/`, shown in the local wiki and not kept in git.

## Game bugs

### Loading Old Town after a robo taxi ride logs "Handle is not initialized"

**Found** 2026-09-27, run 1, Soak7. **Status:** open.

When a ride ends ("You have arrived. The robo taxi drives off.") and the client loads
Old Town again, Godot's resource loader throws, 48 times in a row:

```
System.InvalidOperationException: Handle is not initialized.
  at Godot.Bridge.ScriptManagerBridge.SwapGCHandleForType(...)
  ...
  at Godot.ResourceLoader.Load<T>(...)
  at MmoGame3d.Zones.World.LoadZone(string)          game/zones/World.cs:34
  at ClientGame.<OnZoneChanged>                       game/client/ClientGame.cs:1116
```

The town still loads and the player plays on. Only one of eight bots hit it, after its
first ride. The error is inside Godot's C# bridge: while loading the scene it swaps the
handle of a C# script instance that is already gone. The next step is to find which
resource in the town scene it is.

![Soak7 after the ride](bot-shots/20260927-0024-soak7.png)

### A player saved inside a taxi ride logs back into it, and the town arrives too early

**Found** 2026-09-27, run 2, Soak6. **Status:** open.

Soak6 was stopped during a robo taxi ride, so the server saved its zone as `taxi-14`.
At its next login the server put it back into that ride, which ended at once ("You have
arrived. The robo taxi drives off."), and moved it to Old Town. The server then sent
the town's spawns and syncs before the client had loaded the town:

```
ERROR: Node not found: "Main/World/town/Zone/ItemSpawner" (relative to "/root").
ERROR: Parameter "spawner" is null.        (on_spawn_receive)
ERROR: ID 2 not found in cache of peer 1.  196 times
ERROR: Ignoring delta for non-authority or invalid synchronizer.
```

It cleared once the town finished loading, and the bot played on. Two things to look
at: a ride's cabin is a place the player should never be saved in (the drop-off in
town is), and a zone change should not send the new zone's nodes before the client is
ready for them.

![Soak6 after its login](bot-shots/20260927-0039-soak6.png)

Seen again in run 3: Soak3 and Soak7 were both saved in the same ride, `taxi-24`,
and both logged back into it. After "You have arrived" Soak3 got no "Now in town" for a
while.

### Players wedge into the gaps between Old Town's buildings

**Found** 2026-09-27, run 3, Soak1, Soak5, Soak8 and others. **Status:** open (map).

Old Town's buildings stand with narrow gaps between them that a player can walk into
and get wedged in; walking straight at a target only pushes deeper in, and the camera
ends up inside the building. A person can usually back out; a bot could not until it
learned to (EscapeStep: eight directions in turn). Hand-built maps should close such
gaps or leave them wide enough to turn in.

![Soak5 in a gap behind a dumpster](bot-shots/20260927-0049-soak5-wedged.png)

![Soak1's camera inside a building](bot-shots/20260927-0049-soak1-wedged.png)

### Robo taxis drive through players

**Found** 2026-09-27, run 3, seen by the author. **Status:** open.

A robo taxi's body is on no physics layer, so it never collides with a player: it
drives through anyone in its way, and bots standing at the drop-off look as if they
ride on its roof. The judge now reports a bot inside a vehicle's footprint
(`in-vehicle`), so the next run shows how often, and where.

### Arrivals put riders on the taxi cabin's roof

**Found** 2026-09-27, run 4, Soak2 (seen by the author). **Status:** fixed.

A zone change put the player on the first thing a ray down from 5 m above the arrival
marker hit. That was added for the meadows, where a marker can end up under sculpted
ground; in a taxi's cabin the first thing hit is its roof. Now a player lands exactly
on the marker, and only a zone with `SnapArrivalsToGround` (the meadows) searches for
the ground. The taxi dev scenario checks that the rider sits inside the cabin. The
judge's `too-high` check (feet well above the zone's arrival and spawn markers) finds
this kind of thing wherever it happens.

### The meadows door once did not take a player

**Found** 2026-09-27, the meadows dev scenario, once in six runs. **Status:** watch.

Walking into the ToMeadows door, the scenario timed out without a zone change, run
after the taxi scenario in the same batch. It passed alone and in every run since. A
door that sometimes does not take a player would be a real bug; if it comes back, its
log is in `%TEMP%\mmo-game-3d-scenarios\meadows.log`.

### The phone's Go Offline button falls off a short window

**Found** 2026-09-27, run 1, all bots. **Status:** open.

In a window about 480 pixels high, an app taller than Town repairs (Chat, Who's online,
Defense Objectives) makes the phone panel taller than the window, and its Go Offline
button is below the bottom edge. Esc still goes offline. A player on a small window
would not see the button.

## Bot problems

Fixed as found; kept here so the same thing is recognised next time.

- **Seven bots on Town repairs.** The phone came out every 20 seconds and always opened
  Town repairs. Now every 1 to 3 minutes, with a random app.
- **One bot at the code cracker without end.** After going offline it stood at the
  terminal and went online again. Now it keeps away from terminals for 45 to 120
  seconds.
- **Bots back at the main menu.** The give step pressed Esc to close a give panel that
  had not opened (the other player was too far), which opened the game menu; a later
  blind click hit Leave to main menu. Now Esc only closes a panel that is open, and a
  keeper (`game/dev/BotKeeper.cs`) brings a bot back from the main menu and closes a
  game menu left open.
- **Refused logins after a quick restart.** Restarted within seconds, the new bots
  logged in while the server still had the old sessions, and were refused. Wait about
  10 seconds after `bots-stop.ps1`; the keeper now retries from the main menu anyway.
- **Walking in straight lines.** Steering straight at a target walked bots into
  buildings and wedged them in gaps. The walker now follows a path from Godot's
  navigation mesh (`BotNavigation`), baked from the zone's collision when the bot
  arrives; stuck moments fell from dozens to a handful per bot.
- **Parties dragging everyone around.** Bots joined every invite, so all eight were in
  one party and every door pulled the rest along, into zones their plans were not
  for. Now a bot on a goal leaves its party and turns invites down; in wander mode it
  joins about a third of the time. A door step also checks that it arrived where the
  door leads.
- **Thrashing that was circling.** The first thrashing check (far travelled, little
  gained) flagged bots wandering in circles. It now counts sharp reversals instead.

## Tooling

- **`scenario-test.ps1` counted a FAIL as a pass** when the failure's notices held the
  word "Passenger": the check was case-blind. Fixed: it matches ": PASS" by case.
- **`scenario-test.ps1` waits 20 s for a server to stop** when another server (a bot
  run on 7070) is up, since `server-stop.ps1` waits for any server. The test server
  does stop; only the wait is wrong. Open.
