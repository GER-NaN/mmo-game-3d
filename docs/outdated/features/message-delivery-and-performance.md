# Message delivery and performance

**Date:** 2026-09-22
**Status:** Exploring (not a decision record)
**Sources read:** docs/world.md sections 3 (two worlds), 4 (the terminal), 13 (together),
14 (time), 15 (servers and the whole world); docs/backlog.md (the four message-delivery
ideas, T3); docs/archive/Transcript-2026-09-21-Message-Delivery.md;
docs/features/second-zone.md (T8a, the intent id); docs/features/usable-phone.md (T4c,
T11, batching deferred); Server/ServerApp.cs; Server/Networking; Core/NetCode;
Core/Simulation/Interest.
**Build from:** nothing yet. This file explores five topics and records what is true in
the code today. The author asked on 2026-09-22 to analyze rather than decide, so the
Q&A format was stopped after F1.

How the server decides what to send, when to send it, and how much of the wire each
message costs. Three topics the author raised together: message delivery (a priority
queue, smaller messages, parallel zones), bitmasking to make messages smaller, and
performance tests that hold a timing budget.

The author's words are recorded verbatim or near it. Model additions are set apart and
labelled: "Consequence noted", "Options offered", "Note".

## Already decided

Decisions in force that this feature must fit, or change on purpose.

- The world runs on a real clock and day and night follow it. [C-2026-09-18]
- One region at first, though servers in several parts of the world exist for latency
  and one canonical world overlaps them. [C-2026-09-18, T2]
- The terminal world is the online world, reached from a phone or a fixed terminal, and
  it shows who is online. [Q7, C-2026-09-18]
- Parties hold across distance and disconnects; a HUD indicator shows teammates.
  [B, Q25, C-2026-09-18]
- Real-time travel is a feature, so a player can be in transit and still be a live
  body in the world. [Q28]
- An intent that wants confirming carries an `IntentId`; the server acts on each id
  once and answers with `ServerIntentApproved` or a rejection carrying the id; the
  client resends an unanswered id. [`features/second-zone.md`, T8a]
- Message sizes are declared with `[MessageSize(n)]` and proved by tests, so a message
  stays inside one datagram. [`features/usable-phone.md`, T11]

Built:

- `ServerApp` runs a fixed tick at `ServerOptions.TicksPerSecond` (30 today), with a
  non-blocking `Socket.Poll` read loop, no async.
- Each tick, `SendWorldViews()` walks `_clientRegistry` in order and sends one
  `ServerWorldViewUpdate` per session. The view is built per player by
  `WorldSimulation.GetWorldViewFor(playerId)`, filtered by the interest policy, so it
  is no longer a broadcast of one shared view.
- Private state goes only to its owner and only when dirty: `SendChangedInventories`,
  `SendChangedParties` and `SendChangedTerminalAccess` all run through
  `SendPrivateState`, which asks each state for `GetDirtyOwnerIds()`.
- Other server messages exist per event, not per tick: `ServerChatMessage`,
  `ServerItemDefinition`, `ServerZoneChanged`, `ServerWorldClock`,
  `ServerIntentApproved`, `ServerMessageRejected`, `ServerKeepAliveAck`,
  `ServerConnectionStatus`, `ServerPartyInvited`.
- Serialization is hand-written per message: each class writes its own fields to a
  `BinaryWriter` and reads them back. `MessageTypeRegistry` finds the classes at
  startup and hands the serializer a compiled constructor call.
- Zones live inside `WorldSimulation` as a `Zone` per loaded zone. The simulation is
  single-threaded by design and driven only by the server tick.

Deferred:

- Batching a payload across several datagrams, with an update id and reassembly. That
  is the reliability layer, and it waits for the first real multi-datagram payload.
  [`features/usable-phone.md`, T4c]
- Splitting `WorldSimulation` by subsystem. The split by zone is built; the split by
  subsystem is not. [CLAUDE.local.md]
- Transport encryption and mid-session endpoint rebinding. [CLAUDE.local.md]

## Feature design

### Developer thoughts

From the transcript of 2026-09-21, the author's own words:

> I'm thinking about about the net code and message delivery rates, and How to structure
> that on the server? So I think right now, our system has like a global A global queue
> of things we need to tell the player about. I don't even think it's a cue, it's just
> send everything, but I think we need Some kind of cues system

> Some things can arrive a few seconds late after they happen and don't need to be sent
> each time. So what I'm getting at here is a priority cue. Before messages.

> So like real-time interaction, things that the Players need like intent, their intent
> rejections, their intent things need to be sent to the client very quickly, and then
> other things like road events or high score leaderboard, or other minor things that
> don't need to be real-time, don't need to be sent on the priority queue.

> I imagine this is something we can add later, but I'm thinking about it now, in case
> it's something we want to get in early

> Are we doing The worldview correctly. Should we be sending them in different packets?
> Like Here's all the player positions and the player's status and That would be like a
> server Player status message like for all the players in the zone. It gives them, you
> know. A status of that In one message, you know, their position and what they're
> doing, are they standing? Still? Are they walking? Are they looking at their phone?

> I guess, like player inventory, you know, it would send the inventory message to get.
> I'm getting out here like it would. Break up the worldview. And to individual messages
> That could be sent.

> when I have multiple zones. Where players are on each zone I probably don't want to
> iterate them. Sequentially. Let's say I have 10 zones, you know, there's 20 players in
> each zone I probably don't want to just send it sequentially. For every player in
> every zone Send a message, I probably wanna do some parallel delivery on the server as
> well.

> So That You know, one zone's not lagging behind this is applicable, especially for the
> terminal world, where Things are not zoned in the terminal world. So like In the
> physical world of the game You are, you know, physically separated by the zones, so
> you don't need to play Air Positions of people in other zones or their statuses. But
> in the terminal world, you, do you do need to send the status of people in the
> terminal world to all the terminal players you need to, you need to say who's online
> everywhere in the terminal world? Because it's not zone, it's not zone segmented.

> And then message breakdown, I think, we might want to look at breaking down our
> messages into smaller pieces, for what reason I don't know, just so we have smaller
> messages.

Bitmasking and performance tests were added to the session in conversation on
2026-09-22. The author's thoughts on them are recorded below as they are given.

On bitmasking (2026-09-22):

> Bitmasking, the idea of sending a single integer to represent multiple states or
> pieces of information. Can and should we convert our current messages to this practice
> for packet size and speed. I think this is real and might be a future implementation
> and at the network layer, where we convert readable human messages into bitmasked
> transfer messages and then decode on receipt.. My thought is do we do this now or plug
> it in later when its needed. It feels like it might be an easy swap if the layer is
> there to plug it into but I am not sure.

On performance tests (2026-09-22, said while setting up the build rules):

> I am also thinking about at the same time is probably not our exercise now but is the
> MessageSize contract but with performance measurements. For example, Give this
> WorldView and this mocked DB and this mocked network iwth 0 latency, I expect this
> Intent call from client-> server to return me a message in under 0.2ms. So its an
> integration test that measures performance, but I think thats outside this scope.

### Q&A (stopped after F1: the session mixed five topics, so each question read as loaded)

**F1.** Which things in the game must a player see the instant they happen, and which
things are still fine when they show up several seconds later?

> Answer (2026-09-22): Instant: Own char movements, own char actions (open inventory you
> should see your inventory instantly), interactions with env (open chest, enter
> terminal), so your own player actions. A very close to instant is other character
> actions, some can be delayed, like a chat message that comes .25seconds late or an
> emote that comes 0.25 seconds late is ok.
>
> Consequence noted: this splits the traffic by whose action it is, not by message type.
> A player's own actions and their results are the urgent class; everything about other
> players and the world around them can ride a slower class, measured in a quarter of a
> second rather than a tick.

**F1a.** Follow-up: when you walk, should your own character start moving on your screen
before the server has confirmed it?

> Answer (2026-09-22): I am not sure we need this now but I think so, I think this also
> implies client side prediction which I do not want to do now
>
> Note: the answer names the cost correctly, so the reading below is only about what
> the delivery design must leave room for.
>
> Options offered: what the code does today is the starting point. The client sends
> `ClientMovement` with a direction, the server moves the body in `Tick`, and the client
> draws every body, including your own, from the last `WorldView`. `MotionSmoother`
> glides each body over one tick, so what you see is about one tick (33 ms) behind the
> server, plus the trip there and back. Three ways to read "own movement is instant":
>
> 1. Keep it as it is: the server owns every position, and your own body answers your
>    key about one round trip later. Nothing changes.
> 2. Client-side prediction: the client moves your body at once and corrects it when the
>    server disagrees. This needs the correction path (reconciliation), which is a
>    feature of its own.
> 3. Treat "instant" as a delivery budget rather than a prediction: the pieces that carry
>    your own actions get the urgent class, so the round trip stays small and steady, and
>    prediction stays available later without rework.
>
> I would take 3, because it is what this session can decide, and it does not close the
> door on 2.
>
> Decision: pending the author's word.

> Decision (2026-09-22): 3 yea, prediction and correction is always the future goal. We
> can skip the priority for now. This session is looking at long term goals and Best
> state

**F1b.** Follow-up: the words "skip the priority for now" can be read two ways, so which
do you mean: leave client-side prediction out, or leave the priority queue out of what
this session designs?

