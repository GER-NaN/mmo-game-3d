# TODO

Open items, one line each. Tick or delete them as they land; details live where the
link points.

## Decide (the author)

- [ ] The game's name and icon (the window, the main menu and the project still say
      "mmo-game-3d").
- [ ] Music tone: the only music pack is cosy fantasy folk; keep it for the town, or
      only indoors (sound plan, docs/engineering/sound.md).
- [ ] A voice per player character: players have no voice sounds until this is chosen.
- [ ] The other sound questions: hear other players' sounds or not, how much music,
      terminal typing sounds.
- [ ] Should the server and tests create a missing database themselves, so a fresh
      machine needs only a running Postgres (docs/engineering/setup.md, step 2)?
- [ ] Whois location for the zones added later: shown (town, shop, college) or hidden
      (like the outskirts) for the greenhouse and the subway? Today they are hidden.
- [ ] What fainting costs (world.md: "needs research").
- [ ] What Agility and career ranks give.
- [ ] The map achievements fire at 95% of cells (unreachable ones behind buildings), before
      the minimap looks complete: reveal the rest when it fires, or count only reachable
      cells at 100% (playtest).
- [ ] Party ping-pong through doors: a door takes every party member within 10 m, and
      the town's subway arrival is that close to the subway door, so members with
      different goals bounce the whole party between town and subway (bots, 5 times in a
      minute). A cooldown after travel, only the leader's door, or no auto-follow.
- [ ] Drones zap a player standing online at a terminal, who faints mid-minigame (bots).
- [ ] A party member online at the kiosk near the taxi stand rides along when the leader
      calls a taxi (bots).

## Look at or listen to (never seen or heard by the author)

- [ ] Town cameras (terminal app): the picture, the camera angles, clicking a drone.
- [ ] Agent Defense: the lanes, the speed, the timing windows, the score.
- [ ] The subway: the platform, the tags on the wall, the visitor book.
- [ ] Every sound: the choices and the levels are placeholders picked by name.
- [ ] Which voice is whose (Wren, Old Tomas, Ines, Dee, Mara, Professor Okafor).
- [ ] The potting table's new pot banner and piece grid, and the mouse key.
- [ ] The taxi ride's longer view and haze.
- [ ] The meadows (3 km Terrain3D zone east of Main Street): the hills, the textures,
      the size; sculpt and paint it in the editor.
- [ ] The five security cameras: where they hang, what they aim at, the box model.
- [ ] The compass strip, the tools in the hand, the controller layout and camera rates.
- [ ] Door thresholds are too big (playtest): options to show side by side.

## Known issues

- [x] Screens close in different ways. Done 2026-09-29: Esc closes any open panel, the
      bag, Friends and Skills included, and every screen has an X, Close, Cancel or
      Back. Left: the bots' per-screen close list (`BotScreens.CloseKey`) can become
      "Esc".
- [ ] Bodies stick at the ends of the town's 0.5 m bench boxes (Bench0 at (-10, 8),
      Bench1 at (7, -7.4)): asked to walk, touching only a floor-like edge, they cannot
      move out (bots, many times). The box height is the capsule's lower half-sphere
      centre; players may be trapped too.
- [ ] The recycler has a street lamp in front of it and the shop door beside it: little
      room to stand in reach (bots).
- [ ] Rare engine error "Handle is not initialized" in `CharacterModel._Ready` when it
      instantiates the model (Godot's C# bridge; 2 in about 400 bot clients).

- [ ] Every Agent Defense run posts to the status board; with many players it will
      flood it. Post only top-ten runs, or nothing.
- [ ] One 40 ms server frame in a taxi load test, not explained
      (docs/engineering/performance.md).
- [ ] A second client on the same machine needs `-Profile`; nothing in the game says so.
- [ ] settings.cfg was reset to defaults by a load test on the author's machine; any
      custom keys or mouse speed from before 2026-09-26 are gone.

## Placeholders (hardcoded, waiting on a decision or a design; art not listed)

Numbers, for a balance session:
- [ ] Skill level curve and XP per act (`src/Rules/Skills/Skills.cs`)
- [ ] Career gates, rank XP, player level formula (`src/Rules/Skills/Careers.cs`)
- [ ] Shop prices (`Shops.cs`), recycling prices (`Recycling.cs`), loot weights (`LootTable.cs`)
- [ ] Battery drain rates (`src/Rules/Items/Power.cs`)
- [ ] HP regeneration (`src/Rules/Players/Health.cs`)
- [ ] Drones: spawn rate, zap range and damage, EMP range and cooldown (`ServerDrones.cs`)
- [ ] Town cameras pay $3 a drone (`ServerDrones.cs`)
- [ ] Agent Defense: timing windows, points, combo, pay (`AgentDefense.cs`, `ServerDefense.cs`)
- [ ] Code cracker size: 4 digits, 0 to 5, 8 guesses (`CodeCracker.cs`)
- [ ] Gardening XP and reward plants (`src/Rules/Gardening/Gardening.cs`)
- [ ] Fixables break every 90 s, at most 2 (`ServerFixables.cs`); chests refill in 5 min (`ServerChests.cs`)
- [ ] The lights hold 4 h, a taxi cleaning 3 h (`StreetLights.cs`, `TaxiRootkit.cs`)
- [ ] Ground items per zone (`Zone.ItemStock`), taxi speed (`RoboTaxi.cs`), townspeople's pace (`Stroll.cs`)
- [ ] Sunrise, sunset and light strength (`src/Rules/Time/Daylight.cs`)

Content, waiting on a design:
- [ ] The AI's attacks are timers (lights, rootkits); world.md wants intel, not schedules
- [ ] The town's jobs: only the street lights and the rootkit
- [ ] Townspeople's small talk (`src/Rules/Town/Chatter.cs`)
- [ ] The college Class text (`game/ui/CollegePanel.cs`)
- [ ] The achievement list, model-chosen (`src/Rules/Achievements/Achievements.cs`)
- [ ] Item tier names (`src/Rules/Items/ItemType.cs`)
- [ ] Exchange rate app shows a fixed "1 GPU core = 14.20 credits" (`TerminalScreen.cs`)
- [ ] Locked apps and their notices (`src/Rules/Terminals/TerminalApps.cs`)
- [ ] One subway wall, Old Town's (`SubwayWall.OldTown`)

Systems standing in for the real thing:
- [ ] A license-key file is the account, until real auth (`game/client/Profile.cs`)
- [ ] Diagnostics queue sizes (`src/Diagnostics/Telemetry.cs`)

## Not planned (recorded so they are not rediscovered)

- Sending players only those near them, instead of everyone in the zone.
- Faster physics: Jolt, a lower server rate, or players without physics bodies.
- A server `GD.Print` costs about 2.5 ms on Windows; cause not found.
- Pooled taxi cabins instead of loading one per ride.

All four are in docs/engineering/performance.md, with the numbers.

- A lean server copy of terrain: the server holds all of Terrain3D (about 165 MB for a
  3 km zone) but needs only heights; a height grid at 2 m, 16-bit, is about 4.5 MB, and
  collision could be built only round players. Until server memory matters.
- The generated wild (the travel ring, docs/sources/data-centers-and-travel.md) runs on its own server or instance,
  generating ground round its players from a seed; hand-built zones stay loaded.

## Planned, not built

- [ ] Character bases from the whole KayKit collection (the author, 2026-09-29: "update
      the character select to choose from any of those ... select the character base and
      then customize it. Some of them look like test dummies and that is ok actually to
      include them."). Likely a feature session first. Facts: 62 character models in
      `C:\game-art\3d\kaykit-godot` (adventurers, skeletons, mystery series 4 to 6,
      prototype_bits' Dummy); 53 on the medium rig, which our animations fit, and 9 on
      the large rig (Barbarian_Large, BlackKnight, FrostGolem, Clanker, OrcBrute,
      Monstrosity, 4GTN, 4GTN_Forgotten, Skeleton_Golem), which need the rig_large
      animations from the same collection. Only 5 are in the project today. A look is a
      base id ("a", "b") plus colours and two toggles (`src/Rules/Players/Appearance.cs`),
      and the colours and toggles fit the Protagonists only. Open: customizing the other
      bases, choosing among 62 (a drop-down will not do), ids for the new bases, the
      large ones' size against the body's capsule.
- [ ] Bring over mmo-game's code rules and make them run on every build here:
      `src/Analyzers` (GAME0001 to GAME0006: no async/await, no lock, no primary
      constructors, no reflection inspection, no switch expressions, no tuple
      deconstruction) and StyleCop, wired in by mmo-game's `Directory.Build.props`.
      One rule at a time, each for the author to look at, since the code here was
      written without them.
- [ ] Go through mmo-game's tests (their names, mostly): for each, whether the behaviour
      it checks applies to this game and is not tested here yet; list those, to be
      written here.
- [ ] Mini game levels (needs an MVP Q&A, `mvp-design`): the mini games (Agent
      Defense, the code cracker) have levels and get harder (longer puzzles, tighter
      time, more mechanics), scaled to the player's experience with that game.
- [ ] Agent Defense as "cut the wire before the agent gets across it", not guitar
      hero (the current game is a prototype). Six cut areas in two rows of three, on
      the numpad: 4 5 6 on the top row, 1 2 3 below.
- [ ] Tips and tricks (needs an MVP Q&A, `mvp-design`): a tip for each thing, shown
      once (the game keeps which tips a player has seen or acknowledged); tips can be
      turned off, all of them, for a pro. A tips page of its own in the pause menu, to
      read through like a tutorial at the player's pace, which should not show
      advanced mechanics before the player has reached them: likely a new way to track
      what a player has seen and done.
- [ ] More test zones on Terrain3D: cliffs (terraces, `tools/terrain-seed --terraces`),
      woods (many trees and rocks; Terrain3D's instancer has no collision, so trunks
      need their own shapes), a movement proving ground (ramps, stairs, gaps).



## Playtest 2026-09-26 (the author's notes)

Done the same day: door facing, NPCs stop to talk, skill explanations, cameras in the
world (low ones break, Electrical repair), tools in the hand, compass, drones clear of
buildings and smooth. Open: door thresholds, map achievements (both above), recorded
drone flights and redemption receipts (design sessions).

- Exiting a zone (greenhouse for example) walking forwards, I should continue walking forwards out of the greenhouse. I think this might mean lining up directions with the doors / transitions between zones
- Zone entry markers are too big, we need something nicer 
- street light doesnt function as electric repair>???
- Cameras need to be mounted on something and visible in the world (they are part of the repaoir )
- The drone report was a cool idea, make sure thats a real feature in the game
- drones appear to be flying through buildings
- npc should stop when you talk to them instead of keeping walking
- when repairing things, we should equip a tool (kaykit has a tool models
- Requirements for skills need more explanation. I think the skills themselves need an explainer somewhere. For example I am not entirely sure what the electrical repair is
- Old town map exploration triggered even though I had one square left (at least on the mini map)
- Compass?
- Drone flight seems stuttery
- I would like to be able to record drone flight and then thats their pattern in game. 
- No auto payment, you get receipts from the terminal and need to collect in the town <whast the name... courthouse/office/headquarters/majors> I forget what the "headquarters" of a town is... Same goes for recycler, you get a receipt and it needs to be redeemed. This is something that can be traded too but its in the playres name like 
Recycler Redemption 
-----------------
Town: Far-Away-Vill
Date: 2026-11-21 09:08:12 AM
Player: Player XYZ (put this at the top)
Amount: $2.00

Redeem at Far-Away-Vill City Headquarters

Expires On: ....
---------------------
- Same thing happened in outskirts with the map exploration. In the mini map I had 2 spots left and i got the achievement pop up before those two black spots were "discovered" on the mini map





Second playtest 2026-09-26
- Guestbook in the subway is wrong, the spray painted walls are the guestbook
- When running up hill my player glides without moving his legs, its like a hover, same down hill
- while in the camera, and I press the arrow keys I can see the world in the background spinning and rotating
- Security cameras should not be on trees, I think in the future when I edit maps I will place them at expected spots. (this also needs a recipe). If I place a model in the editor how do I hook it up to my game to be functional (something like a camera). This will be a common pattern where I place an item using the godot editor and it needs to then have behaviors in game and be known by the game. (terminals) etc... 
- Street lights seem permantetly fixed? They should break periodicly and this should be tuneable (how often they break).
- Item placement (street lights inside trees). Again wont be an issue when I build maps. but previously problem applies. How does the gtame know my model is a street light and is eligable for breaking and being in need of repair.
- FPV would be a nice view change.
- I want to redo the entire HUD and menus in the game and the terminal layouts. Suggest some UI tools and options. Are there godot asset packs for this that work nicely (UI interface tools to design the GUI and hud items)
- Scrolling in game UI elements (registrar menu) scrolls my view, keep track of where the pointer is
- Interior decoration is slim but thats ok for now. Real maps will be built by hand.w
- I still see a camera floating in the air, but thats ok, real maps these will be placed.