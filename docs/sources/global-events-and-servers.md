# Global events, campaigns and servers

A synthesis, 2026-09-20, of the author's spoken ideas (saved verbatim in
`design/Transcript-2026-09-20-Servers-and-Global-Events.md`), the model's answer in that
session, which the author judged to have missed the point, and an independent pass made
before reading either. Nothing here is built. The question it works on is the author's:
several servers in different parts of the world, one canonical game world over all of
them, game-altering events and campaigns, and how to make that fair, fun and meaningful
for a region with two players when another has two hundred.

## The problem, in the author's terms

- Servers in East Coast, West Coast, Asia, South America, Europe, for latency. One
  canonical world over them.
- The Prompt is the first campaign: a prompt drove the AI mad, and players find pieces of
  it and reverse-engineer it. When it is solved the AI is beaten, and the cliffhanger
  follows: it escapes to space, gains new powers, the game fundamentally changes.
- The author wants singular, world-level events: a year in, one thing happens to
  everyone and everything is different after.
- Two fears. If Europe progresses fast and triggers the world event, Asia is unfairly
  swept along. If regions run at their own pace, Asia sees Europe's future and is
  spoiled, and the author does not want that as a feature.
- Towns fall to the AI. Two people in Asia cannot retake a town that two hundred in
  Europe can. A shared cross-server pool ("you are one of five hundred") solves the
  arithmetic and feels wrong when you are the only one on your server.
- A website beside the game: leaderboards for the puzzles and tasks, announcements,
  events, lore.

## What the model got wrong

Its headline answer was to abandon regional servers: one global database, throwaway
instances spun up wherever people are, players routed to whichever shard is populated. It
solves the empty-server problem by removing the servers, and with them the thing the
author was asking for: a canonical world in which a place is a place, with real
geography and real travel times, where a server *is* somewhere (`WorldClock` already
keeps a server's own time zone for exactly this reason). "Spoilers are unavoidable, stop
engineering for it" dismissed the pacing question rather than answering it. Its other
three points were sound and are kept below: scale local events to who is there, drive
the campaign from an aggregate the world contributes to, and put the non-spatial
tracking on the web.

## The shape that answers it

### Servers are regions of one world, not copies

One map, one Philadelphia. A server hosts the part of the world that is geographically
its own, and the people nearest a place are usually nearest its server. A low-population
region is not a poorer copy of the game; it is a quiet part of the world, the way the
countryside is. Travel between regions is real travel: a flight to Europe is a transfer
between servers that takes the hours a flight takes. Because the game is coop-first,
that makes a quiet region somewhere a busy one can send help to, not somewhere left
behind.

This is the author's picture. The model's ephemeral shards are the opposite of it.

### The campaign moves by calendar; outcomes stay local

The two fears come from one assumption: that a region *unlocks* the next chapter by
finishing the current one. Drop that assumption and both fears go.

- **Chapters open on dates, for everyone at once.** The Prompt's pieces are revealed on
  a schedule the world shares. Nobody is ahead in the story, so nobody is spoiled, and
  the year-one event is a date, which is what the author wanted: one singular moment
  everyone is in.
- **What each region did with the chapter is its own.** Europe held its grid; Asia's
  went dark. Both carry that into the next chapter. The AI winning in one place and
  losing in another is what a global adversary looks like, and it is the persistence
  pillar (the world remembers) at region scale.
- **The global aggregate decides the story's tone, not its timing.** The model's "war
  effort pool" is right as a measure and wrong as a trigger. The world's collective
  effort, weighted per region (below), decides whether the chapter's date arrives as a
  victory or a setback, how much of the Prompt was recovered, what the AI's next move
  is. The date arrives either way.

### Fair means measured against what a place can do

- **The AI presses in proportion.** Diegetic and simple: the AI attacks where humans are,
  so a region of two faces a town takeover sized for two, and retaking it is as real and
  as hard as retaking one in a region of two hundred. This is the model's "dynamic
  scaling", and it is also what makes the local fight *local*: the two players see the
  two-player-sized threat and beat it themselves. No ghost war, no progress bar moved by
  strangers.
- **The aggregate counts shares, not heads.** A region's contribution to the campaign is
  its share of its own capacity. Two players pulling their weight count as fully as two
  hundred pulling theirs. Between-region boards are per capita.
- **Local outcomes are visible where they happened.** The street lights up in your town
  because you lit it. That is already the first loop.

### The terminal world is where regions meet

Everything behind a terminal has no position, so it needs no region. Leaderboards, the
tasks, the Prompt's board, global chat: one layer over every server from the first day.
That is where a player in a quiet region is visibly part of the whole game, and it costs
nothing to make global because it is the part that is not a place. It is also the
natural back end for the website.

### Transparency where the arithmetic is shared

Where something genuinely is a world effort, the author's instinct was to say so: "you
are one of five hundred". Kept, but confined to the campaign aggregate, which *is* a
world effort. Local fights are local and scaled, so they never need the caption.

### Time of day

An event at 8pm somewhere is 3am somewhere else. World events roll around the globe by
local evening, which the real-time clock already supports and which fits "the AI hits
the grid at night", or they are day-long windows. Never one instant for everyone.

## The website

Agreed by all three: a site beside the game. Leaderboards for tasks and puzzles, the
campaign's public board, announcements, patch notes, lore, event calendars. Reads from
the same global layer the terminal does. Not first; the author said "maybe not at first".

## What stays hard

- **Economy.** One world with one market lets the big regions set prices. Regional
  markets with real transit between them is the honest version, and the design's
  shipping and interception ideas already assume it.
- **Characters that cross servers.** A flight between regions means a character's state
  moving between machines and databases. That is an architecture decision to make before
  there is a second server, not after.
- **A region with nobody.** The AI wins there by default. That has to be a legitimate
  state of the world with a way back (a busy region sends people), not a bug.
- **How many regions at first.** One. Two is the moment every decision above becomes
  real.

## Open

- The weighting of a region's share: by concurrent players, by active accounts, by
  something the AI's "coverage" already measures.
- What a chapter's local outcome changes concretely in the next chapter.
- Whether the aggregate is visible to players as a number (the war effort bar) or only
  as consequences.
- Where the campaign schedule lives: a table on the server, or a service the web and
  the game both read.
