# Transcript: message delivery, 2026-09-21

Spoken by the author, transcribed by the recorder, saved here verbatim on 2026-09-21.
Tag in `backlog.md`: T3. Not design: engineering direction the author raised and has
not decided, waiting for a feature-design session on the technology fork.

## Topics covered

- **Priority queue for outgoing messages:** replace "send everything" with a queue that
  separates urgent traffic (intents, intent rejections, real-time interaction) from
  low-priority traffic (world events, leaderboards) that can arrive a few seconds late.
- **Breaking up the world view:** split world state into smaller messages, such as a
  zone-wide player status message (position, standing, walking, looking at phone), a
  per-player inventory message, and separate world-object state (e.g. a building on
  fire).
- **Parallel delivery across zones:** avoid iterating every player in every zone
  sequentially (e.g. 10 zones x 20 players) so no zone lags behind.
- **Terminal world is not zoned:** player status and online presence must reach all
  terminal players everywhere, so it needs its own delivery design.

## Full transcript

**Speaker 1 (00:00)** Some of these, all right and nap code for my mmo game. I'm
thinking about about the net code and message delivery rates, and How to structure
that on the server? So I think right now, our system has like a global A global queue of
things we need to tell the player about.

**Speaker 1 (00:24)** I don't even think it's a cue, it's just send everything, but I
think we need Some kind of cues system like, okay. Here's all the things I need to send
to the clients, you know, world, state these events, these things just happen to tell
them about that. But I don't think It all has the same importance.

**Speaker 1 (00:53)** Some things can arrive a few seconds late after they happen and
don't need to be sent each time. So what I'm getting at here is a priority cue. Before
messages.

**Speaker 1 (01:08)** So like real-time interaction, things that the Players need like
intent, their intent rejections, their intent things need to be sent to the client very
quickly, and then other things like road events or high score leaderboard, or other
minor things that don't need to be real-time, don't need to be sent on the priority
queue. So my idea is to break up some kind of back end queue system to handle that. I
imagine this is something we can add later, but I'm thinking about it now, in case it's
something we want to get in early And then also, I'm thinking about Are we doing?

**Speaker 1 (02:10)** The worldview correctly. Should we be sending them in different
packets? Like Here's all the player positions and the player's status and That would be
like a server.

**Speaker 1 (02:30)** Player status message like for all the players in the zone. It
gives them, you know. A status of that In one message, you know, their position and what
they're doing, are they standing?

**Speaker 1 (02:44)** Still? Are they walking? Are they looking at their phone?

**Speaker 1 (02:47)** Things like that? Um. And then a second message would be I don't
know what else is in our world simulation view.

**Speaker 1 (03:02)** I guess, like player inventory, you know, it would send the
inventory message to get. I'm getting out here like it would. Break up the worldview.

**Speaker 1 (03:18)** And to individual messages That could be sent. So yeah, you'd have
like your player positions. That's something you send to every player in the zone.

**Speaker 1 (03:43)** Then, you have Other things that the client needs to know about
like You know, statuses and states of various things on the screen, you know, if
there's a building that's on fire or something you send that separately down getting to
the point of when you get when I have multiple zones. Where players are on each zone I
probably don't want to iterate them. Sequentially.

**Speaker 1 (04:27)** Let's say I have 10 zones, you know, there's 20 players in each
zone I probably don't want to just send it sequentially. For every player in every zone
Send a message, I probably wanna do some parallel delivery on the server as well. So
that's another Another Optimization, or design decision to make on the server that I
probably want to break up your message delivery and do that in parallel.

**Speaker 1 (05:08)** So that You know, one zone's not lagging behind this is
applicable, especially for the terminal world, where Things are not zoned in the
terminal world. So like so like In the physical world of the game You are, you know,
physically separated by the zones, so you don't need to play Air Positions of people in
other zones or their statuses. But in the terminal world, you, do you do need to send
the status of people in the terminal world to all the terminal players you need to, you
need to say who's online everywhere in the terminal world?

**Speaker 1 (06:03)** Because it's not zone, it's not zone segmented. So that's another
design concept to think about. So I think we covered parallel message delivery from the
server and how to structure that and then also A priority queue of our messages.

**Speaker 1 (06:27)** And then message breakdown, I think, we might want to look at
breaking down our messages into smaller pieces, for what reason I don't know, just so we
have smaller messages.
