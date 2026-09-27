# Docs

| Folder or file | What it is | Trust it? |
| --- | --- | --- |
| `world.md` | The game, in the author's words. If it is not here, it is not design. | Canon |
| `backlog.md` | Ideas raised and not decided, and open questions. An idea moves to `world.md` when the author says yes. | Undecided by definition |
| `features/` | Feature design sessions (`/feature-design`): the interview, the decisions and how they were reached. | The decisions, yes; folded into `world.md` |
| `planning/` | What the first playable is, when to host and ship, and what MMO players expect. | Direction, not decisions |
| `fiction/` | Scenes written from the design, to find its gaps. | Not canon |
| `engineering/` | How this repo's code works, how to run and test it, what was measured. | Current |
| `sources/` | Raw material the design came from: spoken transcripts, the first brainstorms, the old question-and-answer record, syntheses and model proposals. | History, not updated |
| `outdated/` | The MonoGame and voxel era: its engineering docs, its feature records (3D in MonoGame, the map project, the art catalog, zones as scenes, message delivery), its TODO. | Reference only; describes code that is gone |

Everything outside `engineering/` came from the `mmo-game` repo on 2026-09-26 (its
commit `ac8ae05`), where its history is. Paths inside those files still point at
`mmo-game`'s layout: `docs/first-playable.md` is now `planning/first-playable.md`,
`docs/status.md` is `planning/status.md`, `docs/archive/` is `sources/` or `outdated/`,
and `docs/engineering/` files of that repo are in `outdated/engineering/`.
