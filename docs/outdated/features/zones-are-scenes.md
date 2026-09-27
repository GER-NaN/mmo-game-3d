# Zones are scenes

**Date:** 2026-09-25
**Status:** Built 2026-09-25, on the author's "go ahead". One call made while building:
`ZoneId` keeps its name and becomes the zone's name as text, rather than a rename to
`ZoneName` across well over a hundred lines; flagged at the time.
**Sources read:** docs/engineering/zones.md, docs/engineering/art-pipeline.md (stage 4),
docs/engineering/pipelines.md, docs/engineering/connect-flow.md, docs/features/second-zone.md,
Core/Zones/ZoneLoader.cs, Core/Zones/Data/*.json, Server/ServerOptions.cs,
Core/Simulation/WorldSimulation.cs (`StarterZoneId`, `TerminalIdFor`, `ChestIdFor`),
Data/Players, the `players.zone_id` migration.

A zone is known by its scene's name, one scene to one zone, and only the zone new
players start in needs a player spawn. Raised after deleting every scene still gave a
clean build and a server that would not start: the numbered zone files named scenes the
build never checked.

## Developer thoughts

> (2026-09-25) I don't like that my scene names map to weird, arbitrary zone numbers. I
> feel like the scene to zone mapping should probably have the same names. So, if I have
> a scene called starter zone, that's what my editor outputs, like, okay, here is the
> starter zone scene and the description of it. I feel like my game should know about
> starter zone scene and specifically look for that. The arbitrary numbers I don't
> understand. So I feel like we either do name matching there or by convention or
> something like that. I also don't see any reason why they would not be one-to-one maps,
> so one scene maps to one zone. So there's different names. Scene works in the editor
> because I'm editing a scene. It's not an arbitrary name, it's a generic name. I'm
> building a scene in my voxel editor. But when we talk about the game, a zone is a single
> area of interest. It's a single instance loaded into the game that the player can move
> around, and it's contained. It's a contained area that they can move around. So thinking
> about a second zone that you might want to travel to, it would be like another instanced
> area, and that's sort of where we built the existing transition feature where you can
> transition to a different zone. But I think each zone in our game as a player is playing
> almost always maps to one scene. So some of the language there got confusing. It sounded
> like you could have multiple scenes in a zone or something like that. I also don't think
> every zone requires a player spawn. The player spawn means, okay, I need to spawn the
> player at this point if they are entering this zone, if they're just arbitrarily
> entering the zone. And that doesn't occur with every zone. I can imagine there being
> areas of the game that you just do not arbitrarily spawn in, right? You either have to
> walk there and transition from another zone or arrive in that zone via some other
> mechanism, which I think is transition. That's how you get into scenes.

(Dictated.)

> Note: "a scene is not necessarily a zone" in the compiler's comments meant that a
> scene, such as a test scene, may be used by no zone. It never meant several scenes in
> one zone; each zone file named exactly one scene.

## Already decided

- A zone file (`Core/Zones/Data/<id>.json`) says which scene a zone uses; a scene never
  carries a zone number. [second-zone.md; art-pipeline.md stage 4] **Changed:** there
  are no zone files; the scene's name is the zone's name.
- The starter zone is `ServerOptions.StarterZoneId`, default 1. [zones.md]
  **Changed:** the start is named in a game file.
- A zone has one new-player spawn, the `player` placement on the `spawns` layer, and
  `ZoneLoader` refuses a zone without one. [zones.md; ZoneLoader] **Changed:** only the
  start zone needs one.
- The party rule for later (world zones move the party, town zones do not) needs a
  zone kind, "the zone file can carry one". [zones.md] **Moved:** with no zone files,
  a kind would go in the game file, per zone, when it is wanted.

Built today, by current names:

- `players.zone_id` is an integer, saved and loaded through `PlayerRecord.ZoneId`.
- `ServerConnectionStatus` and `ServerZoneChanged` carry the zone as an integer.
- `TerminalIdFor(zoneId, placementId)` and `ChestIdFor(zoneId, placementId)` build ids
  from the zone number.
- `ZoneLoader.LoadAll` loads every numbered zone file; each names one scene.

## Decisions

- **Name.**
  > Consequence noted from the author's words: every scene in the project is a zone of
  > the same name. The numbers and the zone files go.
- **Where new players start.**
  > Answer (2026-09-25): A small game file.
  >
  > Consequence noted: `art/game/world.json`, beside the art catalog and owned by the
  > game: `{ "start": "starter-zone" }`. The build fails when that scene does not exist
  > or has no `player` spawn.
- **Spawns.**
  > Consequence noted from the author's words: a `player` spawn is needed only in the
  > start zone. Anywhere else it is optional and unused. Zones are entered by doors,
  > which already name scenes (`transition.scene`).
- **A saved spot that can no longer be stood on, in a zone with no spawn.**
  > Answer (2026-09-25): At a door of that zone.
  >
  > Consequence noted: the arrival spot of the zone's first door, in the order the scene
  > lists them. A zone with no spawn and no door sends the player to the start zone's
  > spawn, the one place that is always there. The same goes for a saved zone that no
  > longer exists, as today.
- **Process.**
  > Answer (2026-09-25): Write it up, then build.

## The plan

1. **The saved player.** A migration turns `players.zone_id` (integer) into the zone's
   name (text), rewriting 1 as `starter-zone` and 2 as `outskirts`, the two zone files
   there are today. `PlayerRecord`, the saver and the loader carry the name.
2. **The network.** `ServerConnectionStatus` and `ServerZoneChanged` carry the zone's
   name instead of a number. Message sizes change and their tests with them.
3. **Ids built from a zone.** `TerminalIdFor` and `ChestIdFor` take the zone's name and
   hash it into the id, the same on server and client. Anything saved under the old ids
   is checked while building; if chest contents are saved by id, they carry over in the
   same migration.
4. **Loading.** `ZoneLoader.LoadAll` loads every compiled scene as a zone, and the start
   comes from `world.json`. `ServerOptions.StarterZoneId` goes. `Core/Zones/Data` goes.
5. **The build.** The compiler reads `art/game/world.json` and fails when the start
   scene is missing or has no spawn. Doors already fail the build when they lead to a
   scene that is not there (SC1007). A missing spawn in any other scene is no longer a
   warning.
6. **Tests** for each: the migration's rename, loading by name, the fallback to a door,
   the fallback to the start zone, and the build errors.

## Consequences

- docs/engineering/zones.md, art-pipeline.md (stage 4), pipelines.md and
  connect-flow.md describe zone files and numbers; each is updated when this is built.
- second-zone.md keeps its record; its "zone file names the scene" is superseded here.
- The compiler README: SC2001 to SC2003 (spawn warnings) apply to the start scene only,
  as errors, and a new code covers a missing or unreadable `world.json`.

> Note (2026-09-25): the catalog and the world file moved out of `art/` into a top-level
> `game-data/` folder, as `game-art.json` and `world.json`: they are the game's facts about
> the art, not art. The models stay in `art/game/`, and catalog entries name them by their
> path in the art project (`game/items/battery.vox`). The author: "I feel like these
> probably should be in the database at some point but this is fine for now." The game
> reads both through `GameArt` and `GameWorld`, so the source can change later.
