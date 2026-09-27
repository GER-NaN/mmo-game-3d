# First playable

The first build handed to someone other than the author. Not a public demo and not a
fixed hour of content: the smallest thing a stranger can play end to end and form an
opinion on. Written 2026-09-20 from `world.md`; the Q numbers point at
`archive/decisions.md`.

**Who plays it:** the author's teenage kids, on a home network. [Q32]

**Done when** a player says: "I love the dual-world format of online versus physical
world. It is really cool to see real-time events happening in the world, and that there
are future game mechanics I can take part in." [Q31]

## The shape

One town, its outskirts, and the inside of its electronics shop. The player arrives as
if they had just left the Training Grounds: a phone at ten percent, ten dollars in
pocket change and nothing else. No set order; it is a sandbox. The spine is one link
between the worlds: something in town is broken, the terminal's TODO list has the job,
and when it is done the town changes where everyone can see it. The terminal shows
everything the game will have, and most of it is locked. [Q31, Q33]

## What exists

From the code as of 2026-09-20.

- One zone, server-authoritative walking, collision, a following camera with zoom.
- Items on the ground in four tiers, picked up by walking over them; inventory as
  stacks, persisted; an inventory panel.
- Fixed terminals in the zone and a carried device; use and leave; one player per
  terminal. The terminal screen: a menu of apps with Chat and one locked app.
- Party: invite, accept, leave, see each other across the zone.
- Global chat with a filter.
- Persistence of position, inventory and device across disconnects.
- A real-time world clock in the server's time zone; lighting follows the hour;
  moonlight.
- Bots that wander, collect, recruit and join parties; a scripted client driver.
- The voxel pipeline: scene editor, compiler, package, renderer with effects, wind
  and a character rig.
- Display names at connect; licence key to account to session to player id.

## What to build

Each line is one thing, with where it comes from.

1. **A device with a charge.** The phone starts at ten percent, in the inventory, and
   is dragged into the device slot. Its battery is a component with the charge. A
   dead phone cannot go online. The equipment framework comes with it: slots,
   instance items, components. [Q16, Q17, `features/usable-phone.md`]
2. **A battery to buy.** The electronics shop sells it for $8 and the player has $10.
   Dollars work as far as that: a balance shown as pocket change, a shopkeeper with a
   list and prices. A few items above the player's means show and refuse. [Q17,
   `features/second-zone.md`]
2a. **A workbench.** One or two in town, tagged in the scene on a picnic table or a
    desk. Remove the dead battery, apply the full one. The only thing it does in this
    build is the battery swap. [`features/usable-phone.md`]
3. **Broken street lights as world state.** Lamps dark or lit, persisted, visible to
   everyone, legible at night. [Q20]
4. **The terminal OS shell.** The app list, with every planned app named and most of
   them locked with a notice. [Q7, Q33]
5. **The TODO list app.** The town's repair list. The go-to for a first-time
   player. [Q20, Q31]
6. **One repair job, end to end.** Take the job in the terminal, do the work, the lights
   come on for everyone. What "the work" is (a task in the terminal, a walk to the
   junction box, or both) is the one design choice inside this list. [Q20, Q31]
7. **The town log.** Who repaired what, when, readable in the terminal. [Q20]
8. **The status board.** Events happening elsewhere in the world that the player cannot
   join: a data centre raid in progress, and the like. Fed by a script or by bots until
   the events are real. [Q31]
9. **The exchange rate.** Shown in the terminal; no wallet to use it with. [Q33]
10. **Locked notices.** FPV drone surveillance, Defense Objectives beyond the first, the
    crypto wallet, and everything else named in `world.md` section 4, visible and not
    clickable. [Q33]
11. **Two more zones and the doors between them.** The outskirts, a small safe wood
    with a fountain and one chest that refills, and the inside of the electronics shop.
    A door is a road that leads off the edge with a threshold effect across it; walk in,
    fade, arrive. The party goes together. The purpose is to introduce the zone change.
    [`features/second-zone.md`]

## Art it needs

The author's least experienced side, and the one thing today's decisions did not cover.
Listed as plainly as the code.

**The bar is not met by anything owned.** Metro Minis is a placeholder: the author calls
it comically too poor for the quality he is aiming at. The reference is the density and
finish of Max Parata's Voxel Megabuilding renders: dense voxels, emissive screens and
strip lights with bloom, one warm accent. That pack is CC BY-ND, so it cannot be
recoloured for shipping without a licence from the artist. Where the real art comes from
(a bought pack that allows edits, a commission, or the author learning the tool) is
undecided and is the largest unknown in the whole plan.

**For this build:** not final art, but better than Metro Minis. It will be purchased or
kitbashed from free open-licence art, and the search has not started in earnest; the
search is the blocker. The town is built by the author in the voxel scene editor and
MagicaVoxel. The terminal OS look is not settled and needs iteration on colour and font
before it is built; the canvases give the layouts. [author, 2026-09-20] The list of
things the art must cover is short:

- A town laid out in the scene editor: streets, a library, a few shops, houses. The
  importer for the MagicaVoxel world is the pipeline gap.
- Street lamps in two states: an emissive material and a point light switched by state.
  The emissive shader is on the graphics TODO.
- Night that reads. Moonlight exists; the dark street next to the lit one is the whole
  visual argument of the build.
- A phone and a battery as items and icons. The icon library at
  `C:\Users\geral\src\icon-sets` covers the icons. Also a phone model held in the
  hand, the head-down pose and the pocket-to-face transition, and a screen glow.
  [`features/usable-phone.md`]
- A workbench: no model of its own, a tag on existing dressing, plus the two-slot
  workbench screen. [`features/usable-phone.md`]
- The terminal OS: text-heavy, fonts and icons from the gathered sets, layouts from
  the design canvases, look still to iterate.
- Lock notices: one icon and one line per locked app.
- An electronics shop interior: walls, floor, a few aisles of shelves, a counter, posters
  that foreshadow drones. The search has not started. [`features/second-zone.md`]
- A chest of old hardware, and a small marker model for the door threshold to hang its
  effect on. The outskirts itself is covered by the models owned.

## What is not in it

Written down so it does not creep in. [Q31, Q33]

- The Training Grounds. It is built last.
- A backpack, drones, the EMP gun, any equipment line beyond the phone and its
  battery. The slots for them show, empty.
- Damage, theft on fainting, or hacking of the phone.
- Chat per town. Global chat stays as it is.
- The recycler, and any purchase beyond the electronics shop's list. The Dollar balance
  and that one vendor are in (item 2).
- HP, fainting, loss. Nothing hurts you in the first town.
- Skills, level, reputation.
- Crafting. The workbench exists but does only the battery swap.
- Log-off modes. Disconnect works as it does today.
- Advanced Defense Objectives: FPV drones, data centre raids.
- The generated wild, a second town, travel. The outskirts is in (item 11); a door
  between zones is not travel.
- Character customisation beyond a name.
- Accounts, a public server, moderation. The kids connect the way the author does.
