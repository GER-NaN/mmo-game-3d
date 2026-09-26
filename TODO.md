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

## Not planned (recorded so they are not rediscovered)

- Sending players only those near them, instead of everyone in the zone.
- Faster physics: Jolt, a lower server rate, or players without physics bodies.
- A server `GD.Print` costs about 2.5 ms on Windows; cause not found.
- Pooled taxi cabins instead of loading one per ride.

All four are in docs/engineering/performance.md, with the numbers.
