# Social page (concept)

**Date:** 2026-09-26
**Status:** Model generated concept. Not a design session and not decided. Nothing here
goes into `world.md` until the author says yes.
**Source:** the author's brief in chat, 2026-09-26, and `world.md` §6 (skills, careers,
"Show Skills" on the social page).

The author's brief, near verbatim: a simple "About Me" page, not a replacement for chat.
It shows the skills list, the career and its progression, where the player is and what
they are doing ("In Old Town", "On light repair assignment"), one custom message, a star
or thumbs-up system, and a friend mechanism that feeds into chat. It needs a name in the
terminal and a UI.

## Name

The page lives in the terminal OS, so the name can come from real computing.

- **Whois** (recommended). The Unix command that answers "who is this user". It reads
  as a terminal app, fits the hacker side of the underground society, and works as a
  verb: "whois zooer66".
- The custom message can borrow from the same history. The old Unix `finger` command
  showed a user's `.plan` file, a free-text note about themselves: the first social
  page. The message can be called **Plan**.
- Other candidates: Profile (plain, no flavour), Dossier (spy flavour, but reads as
  something written about you, not by you), Card.

## What the page shows

Top to bottom, most public first.

1. **Identity.** Display name, player level.
2. **Career.** Career, rank (Apprentice to Elite), and a progress bar to the next rank.
   Public, as decided. A player with no career shows "No career".
3. **Presence.** Two lines, filled by the game, never typed: where ("In Old Town", "In
   the terminal", "Offline, last seen 2 days ago") and what ("On light repair
   assignment", "Idle"). An activity is a short label that each mini-game and task
   declares for itself.
4. **Plan.** One custom message, one or two lines. Player-written text, so it falls
   under the moderation that `world.md` already gives player content.
5. **Skills.** Hidden unless the owner turns on "Show Skills". Then a list: skill name,
   level.
6. **Props.** The thumbs-up count, and a button for the visitor. One prop per visitor
   per page, and the visitor can take it back. No thumbs-down.

Actions at the bottom: **Give props**, **Add friend**, **Message** (opens chat with this
player).

## Friends

- A friend request works like a party invite: send, accept or deny. Friendship is
  mutual.
- Friends feed into chat, and chat stays the one place to talk. The chat gets a
  Friends channel, and a line when a friend comes online or goes offline.
- The page shows a small friends count, not the list.

## How you reach a page

- The Whois app: type a name. Display names are not unique [C-2026-09-18], so a search
  returns a list, with the career and level on each row to tell players apart.
- From a click on a player: in the world, in the party HUD, on a chat line, on a
  leaderboard row, or on a town log entry ("KoolGuy78 repaired street light grid
  aaa-001").
- Your own page opens from the same app, with an **Edit** mode for the Plan and the
  "Show Skills" toggle.

## Screen sketch

Terminal art direction (cold, flat, monospace), as a window inside the terminal OS.

```
┌─ whois ─────────────────────────────────────── [search: zooer66    ] ─┐
│                                                                       │
│  zooer66                                          Level 14            │
│  Mechanical Engineer · Senior   [███████████░░░░░░]  62% to Master     │
│                                                                       │
│  ● In Old Town                                                        │
│    On light repair assignment                                         │
│                                                                       │
│  plan ───────────────────────────────────────────────────────────     │
│  "Fixing the east grid every night. LFG substation, bring an EMP."    │
│                                                                       │
│  skills ─────────────────────────────────────────────────────────     │
│  Field repair ...... 31    Electrical repair .. 28    Agility ... 12  │
│  Workbench ......... 25    Hacking ............  9                    │
│                                                                       │
│  ▲ 142 props      18 friends                                          │
│                                                                       │
│  [ Give props ]   [ Add friend ]   [ Message ]                        │
└───────────────────────────────────────────────────────────────────────┘
```

With "Show Skills" off, the skills block reads "Skills are private."

## Open questions

- Who sees presence. Showing everyone where a player is and what they do can let a
  player follow or grief another. Options: everyone, friends and party only, or a
  toggle like "Show Skills".
  > Author (2026-09-26): "Right location can be turned off and it can go dark when the
  > player is not near town."
  >
  > Reading: the location has an off toggle, and it shows nothing ("goes dark") when
  > the player is away from a town, for example in the wild.
- Whether props feed reputation, or stay a separate count. Reputation already has
  real effects in the world [Q13], so feeding it gives props a weight that invites
  farming.
  > Author (2026-09-26): "good point on props." Not decided.
- Whether presence names the terminal activity when the AI could be watching. The
  fiction gives the AI coverage, dense in towns [T1].
- The Plan length, and whether it can be changed at any time.
