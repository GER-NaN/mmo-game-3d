# UI playtest, 2026-09-29: what the author found

A summary of the author's spoken playtest of the in-game world, sorted by kind. The
words are in the transcript: `docs/sources/transcripts/Transcript-2026-09-29-UI-Playtest.md`.
The terminal was not covered; that is for a later playtest.

## What was done (2026-09-29, branch ger/cleanup-ui)

Built:

- Wardrobe: Cancel and Done go back to the pause menu (bug 3); a big character fits the
  preview (bug 4).
- Chat: the lines fade out when not typing; a new line shows them again for 4 s.
- Every screen's close X in its panel's top right-hand corner.
- The bag: no "In the bag" heading; double-click equips, swapping with what the slot
  holds, and double-click on a slot unequips (bug 5); a tooltip card (title, count or
  charge, description).
- Dropped things scatter round the body.
- Friends: an online dot, a mail icon and "..." per row; "..." opens the friend's own
  view (message, remove); the ignored behind an "Ignored" button.
- Party: a bigger title; it folds down to "Party (n)" and opens again.
- Pause menu: bigger, with the server, player and zone on top.
- Main menu: a picker of named servers; the local one is New York; the last choice is
  kept.
- A loading screen from Play on the character screen until your body is in the world.
- Shop: most of the screen, a description under each item.
- The plant card in the middle of the screen.
- Skills: a tooltip card on each skill.
- Subway: the visitor book is gone; a new tag goes to the free spot nearest the player,
  at a random slant, size and colour. Old Tomas walks round the back of the entrance
  (bug 2).
- Potting table: pots and pieces on carousels, up and down for the kind of plant; a
  planted piece is taken by any part of it (bug 6), and the one under the mouse is
  tinted with a hand cursor.

Left for the author: the bag's name, grid and item pictures; where the HUD's five pieces
go; skills, career and achievements as screens; speech bubbles; the registrar and career
screens; dropping a career; the workbench screen; the wardrobe scene; the subway rebuild;
the greenhouse decoration; one settings screen for both menus. Bug 1 (the loose battery)
needs dropping things with an identity, which the ground cannot hold yet; bug 7
(rubber-banding) was seen once and not reproduced.

## Bugs

1. **A battery in the bag that cannot be dropped or equipped.** "Battery 95%" shows in
   the bag after unequipping everything; it has no Drop and no Equip. Likely the battery
   still inside the phone, listed as if loose.
2. **A townsperson walked through the subway entrance's walls.** Their walking loop
   crosses the entrance.
3. **Wardrobe, Cancel closes everything.** Expected: back to the pause menu the
   wardrobe was opened from.
4. **A big character does not fit the wardrobe's preview.** In the world, the big robot
   fit the shop's ceiling.
5. **Double-click to unequip does not work.** Only the Equip and Unequip buttons do.
6. **The potting table: a leaf is picked up only at its base,** not by its top.
7. **Rubber-banding,** once, while walking.

## By screen

**Main menu**
- No server text box. A server picker, a drop-down of named servers, that remembers the
  last one chosen, and shows the name of the server you connect to.
- Servers are named after US cities: Boston, New York, Philadelphia, Pittsburgh,
  Minneapolis. The local server (127.0.0.1) is "New York".
- Settings are fine for now; bigger, with tabs, once there is more in them. Credits look
  good.

**Characters and the wardrobe**
- The character screen is good; Back works.
- A loading screen between the character screen and the world (there was a lag).
- The wardrobe: the whole character, rendered in a scene like the game world, not a small
  preview.

**Pause menu**
- Bigger, with a status on top: the server, the player, the zone; the options under it.
- Settings here and on the main menu should be one screen, not two copies.

**The bag (inventory)**
- One name everywhere: the HUD says "Bag", the panel says "Inventory". Bag, Inventory
  or Backpack, the author leaning to "backpack".
- No "In the bag" heading: what is equipped is on top, the bag below.
- The bag as a grid of square slots, like the equipment slots, each with a picture of
  its item. The pictures: new art, or thumbnails made from the item's 3D model ("the
  item thumbnail"), usable elsewhere too.
- Double-click an item to equip it, swapping it with what that slot holds (a drone, a
  phone, a tool); double-click to unequip.
- A tooltip card: title, count, description, and more later.
- Things dropped scatter round the body, not in one pile in front.

**All screens**
- The close X is too close to the content: in the top corner.

**Friends**
- An online indicator.
- A row is the friend's name, status and town, a mail icon to message them, and "..."
  to open that friend's own screen (remove, more detail, a link to their Whois page). No
  big Remove button in the row.
- The ignored list off the main page, behind an "Ignored" button, with unignore there.

**Party**
- The title is tiny.
- It can be closed or minimised, with a way to bring it back.

**HUD**
- The compass is good. The name, zone, time, money and HP are right, but the author
  does not love them in the top-left corner.

**Chat**
- The chat history shows only while typing; after Enter it fades out, not left over the
  screen. The tabs are good.

**Map**
- The minimap is great as it is, and walking while it is open matters.
- Later: see the maps of other zones already explored, from anywhere; a world map apart
  from the minimap of the zone you are in.

**Skills**
- The screen is tiny. Skills, career and achievements are probably separate screens (a
  career page; skills and career maybe together, undecided); achievements have their own.
- Skills have no tooltips; achievements do.
- Achievements say what they are and how to earn them, in tracks: every zone's "explore
  it all", then an epic one for every zone (Guild Wars 1's mastery and epic levels).

**The college**
- The professor, with nothing to offer a player without a career, should be a speech
  bubble, not a screen (the author is undecided on speech bubbles in general). Closing
  when walking away is right.
- The registrar's careers are a wall of text; the career advancement screen too. The
  author designs both.
- A career can be dropped, back to none (today only swapped for one the player
  qualifies for).

**The workbench**
- Not two buttons ("take battery out, put battery in"). A screen, maybe first-person
  like the potting table: drag the item to work on (the phone, equipped or not) onto the
  bench; a components area (maybe simply the bag); drag components in to install them
  (a battery, later an antenna) and out to remove them. How a component comes out of the
  item needs design.

**The shop**
- Most of the screen, not a small panel; a short description under each item. No sell
  tab: that is the recycler's.

**The subway**
- The tag wall moves from the entrance down the tunnel, along the rails a short way,
  with flickering lights (the author rebuilds the scene). There it can grow without end.
- The visitor book goes away: the tag wall is the visitor book. (This answers the TODO
  item "the subway guestbook as the spray-paint wall".)
- A new tag finds the nearest spot where it fits without overlapping anyone's, at a
  random angle and colour. The angles are good.

**The outskirts and the greenhouse**
- The inspect card for a displayed plant is exactly right; it should appear near the
  middle of the screen. The display plants could be arranged differently.
- The greenhouse needs more decoration.
- The pot and the leaves on carousels (in TODO.md already). The leaves' carousel has two
  directions: left and right for the styles of a plant, up and down for the plant kind
  (Monstera, snake plant).
- A pointer that shows which leaf the mouse is over, for picking it up.
- The mouse diagram is loved; the controls (shift leans, control sizes) need some work.

**Test zones**
- The meadows is a test area, deleted some day. The wooded path and the new town are
  test areas that stay for now.

## Future features

- **Servers:** a list of named servers, no autoscaling at first; players meet only on
  the same server. Worth testing: two local servers on one database, joined at will.
  Servers far away (Asia) against one database are a later problem.
- **A loading screen.**
- **Item thumbnails** made from the 3D models.
- **A world map** of explored zones.
- **Achievement tracks** with epic achievements.
- **Speech bubbles** for people in the world.
- **Dropping a career.**
- **Workbench components** (an antenna for better signal).
- **Custom sprays:** designed by the player, or designs unlocked as you progress.
- **Player plants in the world:** each scene has places a plant can stand (a
  windowsill), dozens a scene, and shows plants players made, rotating them; the same
  inspect card.
