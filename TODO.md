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
- The generated wild (world.md's travel ring) runs on its own server or instance,
  generating ground round its players from a seed; hand-built zones stay loaded.

## Planned, not built

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
