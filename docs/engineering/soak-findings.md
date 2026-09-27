# Soak findings

What long bot runs turned up: game bugs first, then problems with the bots themselves.
A run is started with `scripts/bots-up.ps1` and watched with `tools/soak-watch`, which
flags stuck bots and new errors and takes a screenshot of each. Screenshots are in
`docs/engineering/soak-shots/`, shown in the local wiki and not kept in git.

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

![Soak7 after the ride](soak-shots/20260927-0024-soak7.png)

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
