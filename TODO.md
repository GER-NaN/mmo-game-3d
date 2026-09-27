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
- [ ] Sculpted ground path forward: Godot has no terrain tools. Terrain3D add-on (sculpt
      in the editor; not yet tried on the headless server) or our own noise terrain
      (`game/zones/terrain/Terrain.cs`, uncommitted draft). Blocks the new zones
      (meadows, cliffs, woods, proving ground); controller support does not wait on it.

## Look at or listen to (never seen or heard by the author)

- [ ] Town cameras (terminal app): the picture, the camera angles, clicking a drone.
- [ ] Agent Defense: the lanes, the speed, the timing windows, the score.
- [ ] The subway: the platform, the tags on the wall, the visitor book.
- [ ] Every sound: the choices and the levels are placeholders picked by name.
- [ ] Which voice is whose (Wren, Old Tomas, Ines, Dee, Mara, Professor Okafor).
- [ ] The potting table's new pot banner and piece grid, and the mouse key.
- [ ] The taxi ride's longer view and haze.

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
