# Bot testing findings

What the bots turned up, written up by hand: game bugs first, then problems with the
bots themselves, fixed as found. How the bots work is in `bot-testing.md`. A run's
own findings, each with its picture, client log and server records, are in its report
(`tools/bot-watch`); the ones worth keeping are written up here. Pictures are in
`docs/engineering/bot-shots/`, shown in the local wiki and not kept in git.

## The night of 2026-09-27

Eight, then nine bots (one of each persona) ran from about 02:30 to the morning,
restarted after each fix. What came of it:

- **Fixed game bugs:** walks lost for seconds after login (ENet's packet throttle at 0
  of 32; the link no longer throttles down, and an unchanged walk is sent again every
  0.25 s); eight 1 m gaps between Old Town's buildings that held a player for good
  (filled; `gap` scenario, `tools/map-gaps`); a drop facing a wall landing inside it
  (`drop-wall` scenario); chat's cut at 120 splitting an emoji (a test).
- **Waiting on the author:** the size of door triggers; the street kiosk standing in
  the college door's trigger (the most common finding of the night, as zone churn and
  ping-pong); panels opening over the game menu (minor).
- **Open, not understood:** the shop's navigation mesh for bots has holes (the
  college's, built the same way, has none).
- **Watched, not seen again:** "Handle is not initialized" on loading Old Town, none
  since each zone's scene is kept loaded (02:34 on).
- **Framework:** the dropper (the connection cut mid-activity, back in through the
  keeper) and shadow (follows a player, uses what they use) personas; the wardrobe
  feature file; hostile text typed into fields and chat; `cannot-close` findings;
  findings carry the time since arriving and ENet's link statistics; `--bot-only` and
  `bot-try.ps1` take a sequence; the watcher and memory recorder follow their own
  server; a dozen bot problems fixed (below).

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
first ride.

Not reproduced since (run 5): the `taxi-ride` scenario rides a whole taxi ride, 4 s and
then 30 s long, with no error, and neither did four bot rides that night. It comes and
goes; the scenario and the bot watch now flag any C# exception in a client's log, so
the next time it happens it is caught with its log.

Caught again at 02:29 by the client-error judge (Soak6), with its stack, and it is not
about taxis: Soak6 was walking back into town through a door from another zone. This
time it failed inside `PackedScene.Instantiate` (`World.LoadZone`, line 35), where the
first one failed in `ResourceLoader.Load`. The common thread is the client loading Old
Town again after being elsewhere; about twice an hour over eight bots. It is inside
Godot's C# bridge (a C# handle for an engine object is already gone when the engine
swaps it). One thing to try: keep each zone's scene loaded for the whole run, so Old
Town is never loaded from disk again; then see whether the error stops. The error is inside Godot's C# bridge: while loading the scene it swaps the
handle of a C# script instance that is already gone. The next step is to find which
resource in the town scene it is.

![Soak7 after the ride](bot-shots/20260927-0024-soak7.png)

### A player saved inside a taxi ride logs back into it, and the town arrives too early

**Found** 2026-09-27, run 2, Soak6. **Status:** the first half fixed: a player saved in a
ride now logs back in at the taxi drop-off (the `taxi-relog` scenario). The town's
nodes arriving early is open.

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

**Found** 2026-09-27, run 3, Soak1, Soak5, Soak8 and others. **Status:** the traps
fixed (run 7); the map itself open, for the author.

Old Town's buildings stand with narrow gaps between them that a player can walk into
and get wedged in; walking straight at a target only pushes deeper in, and the camera
ends up inside the building. A person can usually back out; a bot could not until it
learned to (EscapeStep: eight directions in turn). Hand-built maps should close such
gaps or leave them wide enough to turn in.

Run 7 showed how bad it is: the wedger pushed into the back of the gap between two
north-side buildings (at x -20.5, z -18) and could not get out at all, in any of eight
directions, for minutes. Every building's collision is a 10 m box, and the rows stand
with 1 m between the boxes: eight such gaps, 10 m deep, each exactly as wide as a
player's capsule. A player pushed in askew is held by both boxes, and logging in again
puts them back on the same spot. Each gap is now filled by an invisible box
(`GapFills` in `town.tscn`), which changes no look; the dev scenario `gap` pushes in
from behind and backs out (it fails without the fills). If the buildings are moved
later, the fills go with them.

Smaller pockets remain where street furniture stands close to a building corner: by
the street kiosk (with a lamp post and a dumpster), and by the south-west security
camera (its post, the taxi stand's sign and the corner, x -10, z 8). Bots walk in and
are judged stuck; so far every one got out with its escape step, and a person gets
out too. `tools/map-gaps` counts only the buildings' boxes, not props.

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

**Found** 2026-09-27, the meadows dev scenario, once in six runs. **Status:** watch; six
passes in a row since, right after the taxi scenario too.

Walking into the ToMeadows door, the scenario timed out without a zone change, run
after the taxi scenario in the same batch. It passed alone and in every run since. A
door that sometimes does not take a player would be a real bug; if it comes back, its
log is in `%TEMP%\mmo-game-3d-scenarios\meadows.log`.

### Doors swallow players walking past them

**Found** 2026-09-27, run 6, Soak5 (the zone judge's ping-pong). **Status:** open, for the
author: it goes with the playtest note that door thresholds are too big.

A door's trigger is a box 6 m wide and 1 m deep, standing out from the building front
onto the pavement. Soak5, hunting a drone in front of the electronics shop, walked
through it and was in the shop (02:14:55); it walked back out and, 9 seconds later,
crossed it again on its way elsewhere (02:15:07). A player walking along the shops can
be pulled inside without meaning to go. Backing up does it too: a player arrives about
3 m in front of the door they came out of, so a few steps back put them straight
back in (the gamer bot, greenhouse and outskirts, three times in a minute). The bots now keep their paths off doors they
do not mean to use (the doors are carved out of their navigation mesh), so this now
shows only when a bot means it.

One place makes it certain: the street kiosk (`StreetKiosk`, at x 18, z -7) stands at
the very end of the college door's trigger (x 12 to 18). Walking up to the kiosk from
the west crosses the trigger, and a player meaning to use the kiosk is in the college
(Soak8, run 8, twice in a minute). Moving the kiosk a few metres east, or a smaller
trigger, ends it. For bots it is worse: the kiosk stands inside the college door's
carve in their navigation mesh, so from it they have no path, walk straight, and a
walk west goes through the college door (zone churn, run 12, the earner's Agent
Defense goal four times over). Just east of it, the kiosk, a lamp post (x 20, z -6), a
dumpster (x 22.5, z -7) and the building fronts make a pocket whose ways out are
narrower than the bots' mesh allows: two bots walked in and were judged stuck (runs 9
and 14); both got out with the escape step, and a person gets out too. Moving the kiosk
is a chance to open that corner.

### One lost walk packet leaves the server's body standing

**Found** 2026-09-27, run 7, Soak6 and Soak7 (the position judge's thrashing, with its
track). **Status:** fixed.

Five seconds after logging in, both bots held only the forward key, and their bodies
jumped back and forth by more than a metre, the client and the server up to 2.6 m
apart. The server's log had no walk from Soak7 for 14 seconds while it walked. A walk
goes to the server unreliably, and the client sent one only when the direction or the
heading changed: walking straight sends one, and if that packet is lost (more likely
at login, under the burst of everything else sent then), the server's body stands
still. The client walks ahead on its own prediction, is snapped back past 3 m, and
does it again. A person walking straight after logging in, or after any lost packet,
would see it. Now an unchanged walk is sent again every 0.25 s.

That was half of it: the resent walks went missing too, for about five seconds after
each login. Findings now carry ENet's view of the link, and the next one (Soak1, run
10) had the packet throttle at 0 of 32: ENet was dropping every unreliable packet it
was given, because the round trip wavered in the login's burst. ENet's throttle is
there for bulky unreliable streams; ours are small. The client now sets the link to
never throttle down (`ThrottleConfigure(5000, 2, 0)` on connecting), and ENet applies
that at the server's end too, so position updates keep flowing as well.

### A drop facing a wall lands inside it, lost

**Found** 2026-09-27, run 12, Soak6 (four walk-failed findings on one item). **Status:**
fixed.

The earner could not reach an item at x -20, z 9: inside the gap fill between two
south-side buildings. Spawns check for free space, so it was a drop: a drop lands 1.8 m
in front of the player (past pickup reach), whatever is there, and the wedger drops
things while facing walls. An item inside a wall is lost to everyone, and it still
counts toward the zone's stock, so the street refills with one fewer. A drop now goes
in front, or to the right, the left or behind, whichever has a clear line and room;
with none, it is refused and stays in the bag. The dev scenario `drop-wall` drops
facing a building (it fails on the old code).

### The phone's Go Offline button falls off a short window

**Found** 2026-09-27, run 1, all bots. **Status:** open.

In a window about 480 pixels high, an app taller than Town repairs (Chat, Who's online,
Defense Objectives) makes the phone panel taller than the window, and its Go Offline
button is below the bottom edge. Esc still goes offline. A player on a small window
would not see the button.

### A client grows about 20 MB with each new login

**Found** 2026-09-27, run 14, Soak1 (the dropper; `tools/bot-watch/memory.ps1`).
**Status:** watch.

Over an hour every bot client grew from about 1.25 GB to about 2 GB, most of it in the
first half hour and then slowly (caches filling). The dropper alone kept growing in a
straight line, about 515 MB in the hour, and it logged in again 24 times: roughly
20 MB a login. At each login it now logs Godot's counts: nodes (178), orphan nodes (6)
and resources (164) stay flat, objects go up and down, and the managed heap is small.
So it is not leaked nodes or resources but native memory (rendering, ENet, navigation);
finding it needs a native memory profiler. A player who logs in again a few times a
session would not notice.

### A client jumps into the subway entrance where the server does not

**Found** 2026-09-27, run 16, Soak2 (the new `door-ignored` finding, twice). **Status:**
open, minor.

The subway entrance is a pit behind walls a metre high, with a 2 m trigger inside
(its own shape in `town.tscn`, not the doors' usual 6 m). Soak2 walked and jumped at
it from the side: on its client it cleared the wall and stood in the doorway (y 0.6),
while on the server its body stayed outside the wall, 1 m away. The server logged the
body held against the entrance's right wall, and no door touch. A gap of a metre is
under the 3 m a client snaps back at, and a walking client settles back only when it
stops, so the player sees themselves in the doorway, not going down, until they let go
of the keys. The jump is predicted by the client and applied by the server a little
later, so a jump over a low wall can land on one side for one and the other side for
the other. The `door-ignored` judge now uses the server's position, so it flags only a
door that really did not take someone.

### The workbench runs off the screen with a few phones and batteries

**Found** 2026-09-27, the first run of the layered bots, Soak1 and Soak3 (`off-screen`).
**Status:** open, for the author.

The workbench lists, under every phone, a button for every loose battery. Soak1 carried
several phones and a handful of batteries: the list ran past the top and the bottom of a
992-pixel window and over the HUD, with nothing to scroll, and the buttons at the top
could not be clicked ("Put in the loose battery at 100%" at y -86 and -207). A player
who keeps spare phones and batteries could not use the bench. The list grows as phones
times batteries; a scroll, or one list of batteries shared by the phones, would hold it.

### The registrar's list is rebuilt every few seconds, under the pointer

**Found** 2026-09-27, while the new enroll activity was tried. **Status:** open, minor.

The college panel frees and rebuilds all its rows on every progress update from the
server, which comes every few seconds. At 1280 by 720 the second career's button is
below what the list shows, so a player scrolls to it; a rebuild can land between that
and the click. The bots showed it as clicks that did nothing: a new button has no size
or place until the next layout (the bots now wait for one). Rebuilding only when
something shown changed would keep the list still.

### An emote right after walking may not show

**Found** 2026-09-27, Soak7 (`activity-failed`, the emote judge). **Status:** watch.

Soak7 was hunting a drone; the emote aside stopped the walk and typed /cheer, with
nothing open and not online, and the body did not cheer within two seconds. Walks go
unreliably and emotes reliably, so a walk sent just before the stop may reach the
server after the emote; the server ends a gesture on any walk. If it comes back, a
person who emotes straight after walking sees the emote cut off at once.

### Panels open over the game menu

**Found** 2026-09-27, run 12, Soak8 (the masher; the first `cannot-close`). **Status:**
open, for the author (minor).

With the game menu up, the other keys still work: M opened the map over the menu,
hiding its Resume button, and Enter put the cursor in the chat line, which then took
M as a letter. A person gets out (click away from the chat, M, then Resume), but the
game menu is usually a screen that holds the rest still while it is up.

### A body can stand on the workbench chair

**Found** 2026-09-27, the dropper at the shop's workbench (`floating`).
**Status:** open, minor.

Walking to the workbench, the body stepped up onto the chair's seat and stood there,
1 m above the floor, for over 10 s. The ray under the body passes the chair and finds
the floor, so the judge read it as floating. A player can walk off again. There is no
seat mechanic; a chair that the body cannot step onto, or one that seats it, would fix
it.

### Panels run off a short window

**Found** 2026-09-27, 8 bots tiled on one screen (`off-screen`).
**Status:** open, for the author.

In a window 944 by 476 pixels, Agent Defense's "Start a run", the bag's "Drop" and the
recycler's "Recycle" sat below the bottom edge, with nothing to scroll. Full-size
windows do not show it. The game has no smallest window size, so a player may make one
this short.

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
- **Frozen at a terminal after a dropped goal.** A goal dropped mid-run leaves its
  state as it is, on purpose, so the bot stayed online at the kiosk and its next walk
  went nowhere: the keys belong to the terminal. The judge caught it (stuck, with the
  terminal in the picture). A person closes the terminal before walking away, and now
  so does the walker; other panels stay open, so a dropped goal still leaves state
  behind.
- **Judges crying wolf.** Doors and party pulls moved a bot from one zone's
  coordinates to another's inside the thrashing check's five seconds, which read as up
  to 177 m of travel; zone ping-pong flagged every trip into a building and back. The
  thrashing track now restarts on a zone change or a jump, and ping-pong counts only
  quick bounces. `tools/bot-watch/triage.py` groups findings, so noise shows as one big
  group rather than a long list.
- **A wedged bot's jitter read as thrashing.** Turning in place in a gap, the wedger's
  body jittered about 0.1 m a sample, and the check counted each as a move. A move now
  counts from 1 m/s; real thrashing swings a metre or more a sample.
- **Paths grazing doors.** A carve in the navigation mesh is not grown by the agent's
  radius, so paths hugged the door boxes with about 5 cm to spare, and an earner
  heading for the outskirts went into the shop three times in a minute. Doors are now
  carved 1.5 m wider.
- **Wandering out of the college.** The wander step presses keys blind, and in the
  college's small room it walked into the exit door within seconds of arriving, so a
  visit never reached the registrar (three ping-pongs in a row). It now turns from a
  door's trigger up to 3 m ahead, as a person idling about would.
- **Pushing on purpose read as stuck.** The escaper presses against the world's edge
  for 45 seconds and the wedger into gaps; both were called stuck. A step now says it
  presses, and the judges count from the step after (where the gap trap showed).
- **Walking straight on arrival.** A stuck finding had a bot walk from the taxi
  drop-off straight into the back of the college's building. A new log line (a walk
  whose path is missing or ends short) then showed the first path in every zone
  coming back empty: Godot's navigation map takes a new mesh only on the next physics
  frame, whatever `MapForceUpdate` says, so each arrival started with a straight walk,
  through doors too. Bots now wait for the map (a second at most). The shop still has
  no path from its entrance to the shopkeeper or the workbench: the bots' mesh there
  is 3.5 m from the entrance and 3.0 m from the shopkeeper, so most of the floor has
  none. The college, built the same way (same room, same door), is fine; the cause is
  not found. Both meshes span the same area (x -7.2 to 7.4, z -5.2 to 5.4, from the
  bake's log line), so the shop's has holes; seeing them needs the mesh drawn in the
  editor (Debug, Visible Navigation). It matters when a bot gets stuck there: working round it, it backs out
  through the door (the wedger's zone churn, run 13).
- **Closing under the chat line.** With the chat line focused, the map's own key
  typed into it, so the map never closed. Closing now lets go of a text field first,
  and closes the top panel first.
- **Working round into a door.** Stuck at the shop's counter (the shop's mesh has
  holes, so walks there go straight), the walker's unstick stepped sideways or pushed
  forward out through the door: zone churn in the shop, over and over, from several
  bots. It now takes the side without a door and skips the push with a door ahead;
  no finding in the 15 minutes after.
- **Swapping batteries for ever.** The charge-the-phone goal swapped whenever the bag
  held a battery; after a swap the old, low one is back in the bag, so the dropper went
  to the shop's workbench and back every 15 seconds (zone churn). It now swaps only for
  a battery clearly fuller than the phone's (else buys one), and gives up when a
  finished swap left the phone low.
- **Walking into parked cars.** The blind wander walked into a car spell after spell;
  a spell that got nowhere now turns away first.
- **Bobbing read as thrashing.** Against the world's edge the escaper bobbed up and
  down; the check now counts moves across the ground only.
- **A swap that found no battery button called itself done.** With the workbench
  running off the screen, "put the fullest battery in" saw no button for 3 s and ended
  as done, so the judge said the swap broke its promise (phone still at 0%). A swap
  starts only with a spare battery, so no button now fails the step.

## Tooling

- **What holds a server body.** A thrashing finding had the walks arriving every
  0.25 s and the server's body still. The server now logs, once per hold, a body asked
  to walk that has not moved for a second: online or not, its gesture, on the floor or
  not, what it is against. The line names the player, so a finding's `server.txt`
  carries it. Most holds are props (the shop's counter, the subway entrance's walls,
  a signal box, the lamp post and dumpster by the kiosk).

- **`scenario-test.ps1` counted a FAIL as a pass** when the failure's notices held the
  word "Passenger": the check was case-blind. Fixed: it matches ": PASS" by case.
- **`scenario-test.ps1` waits 20 s for a server to stop** when another server (a bot
  run on 7070) is up, since `server-stop.ps1` waited for any server. Fixed: it waits for
  the server on its own port.
