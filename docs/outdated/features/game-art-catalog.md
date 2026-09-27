# The game's own art

**Date:** 2026-09-25
**Status:** Built 2026-09-25, with metro-minis placeholders for every entry.
**Sources read:** Client/Rendering/EntityModels.cs, Client/Rendering/ItemIcons.cs,
Voxels.Rendering/StandInModels.cs (removed), Compiler/Package.cs, MapPackage.targets,
Core/Simulation/Items/ItemCatalog.cs, the editor's ProjectAssets (saving prunes models no
scene places).

Models the game draws that no scene places: the player's body, items on the ground and in
the inventory, the phone in a hand, and later robots. Designed now because the package
had lost every character model in a scene cleanup, and every player silently became a
body built in code that nobody had seen before.

## Developer thoughts

> (2026-09-25) OK so I want to remove the built in character I don't think we should have
> a fall back character. So short of building a character select screen like a character
> modification screen I think for now we'll kind of do something like we're doing now i'm
> going to go pick a vox file that I want to use as my default character Then at some
> point in the future we're going to build a character customization screen but we're not
> going to do that now So I guess we're going to replace what's going on now but tweak it
> so that there's one standard Fox file that represents my character in the game and
> there's nothing else selected or searched about it it's always 1 direct thing one direct
> file. I guess what this changes is that now this is the first time we're going to be
> selecting a vox file that is included in the game but outside of my editor so let's talk
> about that quick umm this might be like a new concept something that's not on the map
> file but it's a VOX file that I want to render in the game. And then also not important
> right now but consider how in the future we will need to render other players custom
> characters

(Dictated; "Fox file" is a .vox file.)

## Already decided

- The package holds what scenes place, collected by the compiler. [Package.cs]
- Saving a scene in the editor deletes project models no scene places, by the author's
  choice the same day. So game art cannot live in `art/models`.
- The editor has no game words; game rules live in the game. [editor TODO, design notes]

## Decisions

- **Where it lives.**
  > Answer (2026-09-25): I think we need something like 1 but more structured. For
  > instance I want to randomly spawn items on the ground (these will be vox models). I
  > want to have robots spawn automaically, again as vox model.
  >
  > Consequence noted: `art/game/` beside `art/models/`, which the editor never sees,
  > with `catalog.json` in sections keyed by the game's own names: `characters`
  > (`default`), `items` (item type, then tier), `held` (item type), `robots` (robot
  > kind, empty until robots exist).
- **Fallbacks.**
  > Answer (2026-09-25): Remove all fallbacks now.
  >
  > Consequence noted: `StandInModels` is gone: the person, the item chips, the kiosk
  > and the held phone. The compiler fails a build whose catalog lacks anything the
  > game draws (SC0004, SC1013 to SC1016), and the client throws rather than drawing
  > something made up.
- **Tiers.**
  > Answer (2026-09-25): One model per type and tier.
- **Process.**
  > Answer (2026-09-25): Build from this proposal.
- **Until the game's own art exists.**
  > Answer (2026-09-25): Use metro-minis objects for now.
  >
  > Consequence noted: placeholders from git history: `chr_goth1` as the default
  > character, `obj_cone1`, `obj_hydrant`, `obj_newsbox2` and `obj_trashcan1` for the
  > four carried items (every tier the same file), `obj_mailbox` as the held phone. They
  > are street props at full size, so on the ground and in a hand they look big. The
  > author will name the real character file.

## How it works

- `art/game/catalog.json` maps each entry to a file in `art/game`. Two entries may
  name one file.
- The compiler's `GameArtCompiler` checks it both ways: every entry the game needs
  (`GameArt.CarriedItems` in every `ItemTier`, `GameArt.HeldItems`, the `default`
  character) must name a readable .vox, and every entry must be one the game knows, in
  the game's own case. It runs as its own step, after the scenes.
- The package gets each file as `game.<section>.<key>.vox` and a `game-art.json` saying
  which, which is hashed into the asset version like every other package file.
- The client's `EntityModels` asks the catalog: the default character for every player,
  the item's type and tier, the held phone. The animation rig works on any model.

## Later: other players' own characters

Not built. The shape it slots into: the `characters` section grows into a catalog of
parts (bodies, heads, hair, colours), all shipped in the package. A player's appearance
is a small description, saved by the server and sent to other clients once, when they
first see that player, not in every snapshot. Clients build the look from parts they
already have. Players never upload .vox files, which keeps file size, security and
moderation out of it, and every client able to draw everyone. `EntityModels.ForPlayer`
already takes the player's id for that reason.

## Robots

The catalog has a `robots` section so their art has a place. Robots that spawn by
themselves are a feature of their own: server-side creatures, their views on the wire,
spawning rules. Not started.

> Note (2026-09-25): the catalog and the world file moved out of `art/` into a top-level
> `game-data/` folder, as `game-art.json` and `world.json`: they are the game's facts about
> the art, not art. The models stay in `art/game/`, and catalog entries name them by their
> path in the art project (`game/items/battery.vox`). The author: "I feel like these
> probably should be in the database at some point but this is fine for now." The game
> reads both through `GameArt` and `GameWorld`, so the source can change later.
