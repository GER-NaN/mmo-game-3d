# UI playtest 2, 2026-09-29: what the author found

A summary of the author's second spoken playtest, which walked the test list in TODO.md
for the first playtest's changes (`docs/planning/ui-playtest-2026-09-29.md`). The words
are in the transcript: `docs/sources/transcripts/Transcript-2026-09-29-UI-Playtest-2.md`.

## The test list

| Item | Result |
|---|---|
| Bag: tooltip card, double-click | Double-click equips (EMP emitter). The card is far too big. |
| Skills: tooltip card | Far too big. |
| Carousels | Tested at the potting table in the session before. |
| Drones: model and hover | "They look sweet"; the hover is good. |
| Subway: a new tag's place | Not tested: the author's name is on the wall already. |
| Old Tomas round the back of the subway entrance | Fails: he is blocked there and goes through the sidewalk. |
| Dropped things scatter | Works. |
| Chat fades after 4 s | Works. |
| Close X in the corner | Not checked one by one; taken as done. |
| Friends | Works: message, the friend view, remove, the Ignored button. "Much nicer." |
| Party fold | Not tested: no party. |
| Main menu: New York picker | Works, liked. |
| Pause menu: status on top | Works; the rule and the alignment are good. |
| Loading screen | Works (brief). |
| Shop | Not mentioned. |
| Plant card in the middle | Mentioned, not described. |

## Bugs

1. **Old Tomas goes through the sidewalk** at the new detour behind the subway entrance,
   and is blocked there. His route must keep out of buildings and kerbs.
2. **The terminal grew off the screen** after "take the job" (a rootkit job at a public
   terminal); taking a second job brought it back. Not tried again yet.
3. **A drone's laser stays at a fixed point** while the drone moves: it should start
   from the middle of the drone's body.
4. **Drones come back at once** after they are knocked down.
5. **The loose battery** (bug 1 of the first playtest) is still there: it cannot be
   dropped or equipped.

## Changes by screen

**The bag**
- Named "backpack" (still to do).
- The tooltip card is far too big: it should be small.
- The Device slot reads as "phone battery", not "Phone": the phone's line needs work (not
  now).
- The screens are so generic that the unique things are hard to see: the "Open repair
  pack" button, from the player's career, was forgotten. All screens need a redesign
  (later).

**Skills**
- The tooltip card is far too big.
- The level and experience want a progress bar, and the level a UI element of its own,
  not plain text.
- Clicking a skill to change the description below: remove it.

**Drones**
- Knocked down, they fall (good), then lie on the ground about a minute to be looted:
  interact with one to open its inventory and take what it holds. For now one Enhanced
  RAM stick; later a loot table. Looted, a drone dissolves and is removed. Not looted in
  a minute, it dissolves anyway, with its loot. It dissolves, never just vanishes.
- Spawning runs on a tick, every 10 minutes: check whether drones are needed and show
  them, so they do not come back at once.

**The Code Cracker**
- It is too hard. A character in the exact right spot is shown in gold and underlined, as
  a hint.

**Taxis and jobs**
- The rootkit is at the sign, and belongs on the car: the author fixes that mechanic.
- Two jobs at once may be fine: undecided.
- Town missions get a timer, so they can be abandoned and have some urgency (the
  author, after the playtest: "the town missions need a timer so they can be abandoned
  and have some immediacy to them"). Two minutes for now ("Make it 2 minutes for now").

**The pause menu**
- A player count for the server would be nice.

**The world**
- The security cameras are all wrong: the author places them by hand in the scene later.
- The door thresholds are too big: the author works out a new way to show them.

## Future features

- **Drone loot:** a loot table (one RAM stick for now).
- **A player count** on the server status.
- **A redesign of all screens,** so what is special on each stands out.
- **Timed town missions,** two minutes for now, abandoned when the time runs out.
