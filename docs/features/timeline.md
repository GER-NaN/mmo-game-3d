# Timeline

**Date:** 2026-09-29
**Status:** Agreed in conversation; built for zones, screens, interactions, chat,
notices, money, items and the party.
**Sources read:** docs/features/bots.md (the judge, F1 and T7b), TODO.md (tips and tricks),
game/client/ClientGame.cs, game/client/ClientView.cs, game/networking/.

The client's own record of what happened, in order: what the player did and what the world
did to the player. Designed now as the evidence the bots' judges need, and as the base for
player features that need "what this player has seen and done".

Decided in conversation, not a Q&A session. The author's words are quoted.

## Decisions

- **Purposes.** Evidence for the bots' judges (a judge checks an activity's slice of the
  timeline, not the bot's own claims), and player features such as the tips ("a new way to
  track what a player has seen and done", TODO.md). Playtest review comes with it.
- **Client only, no extra network.** "I dont want extra network activity just for this."
  The server already confirms what matters by what it sends back; the timeline records what
  the client sees. Intents stay as they are: 6 of 48 kinds carry an id and an answer.
  "We dont need the server work for now": entries the tips must keep across sessions are
  for later.
- **Everything typed.** "everything gets typed (no free strings), everything is
  identifyable by class/enum/type etc.. except raw values (chat messages, money level,
  experience levles, pretty much any "value"). If its a thing, its a type."
  - Screens and things are identified by their C# class ("yes use classes"), with the
    node's name when a zone has several of one class (a `Fixable` is a bench or a camera).
  - Notices get a kind: "they are ok being generic though Pickup, Faint,
    CameraUseNotice". The text stays a value. A new field on the notice message, set at
    each of the server's 71 calls; not extra traffic.
  - Zones keep their ids from `ZoneIds` ("taxi-3" for a ride): "we dont need to force ID
    unless there is some gain".
- **Names.** `Timeline`, `TimelineEntry`, `TimelineKind`. "Interaction" already means
  pressing Use on an `Interactable`; "event" means world events and `BotEventLog`.
- **Shape.** One entry: the time, a kind, and what it is about (a zone, later a thing's
  class and a value). A bounded list with a running count, as the notices: a reader notes
  the count and later asks for what came after it.
- **Where.** `game/client/`, beside `ClientView`: the view is what is true now, the
  timeline what happened. Readable from `ClientGame.Timeline`. No xUnit tests for now.

## Built

- `ZoneEntered`, `ZoneExited`: entering the world, a door (exit then enter), leaving the
  world. The timeline keeps the current zone, so every other entry says where.
- `ScreenOpened`, `ScreenClosed`, by the screen's class: ClientGame compares its open
  screens each frame, one place for every screen (until screens open through one place,
  docs/backlog.md, the client's shape).
- `Interacted`: F on a thing, by its class, with its node name.
- `ChatSent` (what the player submitted) and `ChatReceived` (what the server delivered,
  the player's own lines included), with the chat kind, the other side and the text.
- `Notice`: the text, until notices have kinds.
- `MoneyChanged` (by how much) and `ItemGained`, `ItemLost` (type, tier, how many), from
  each bag update after the first.
- `PartyJoined`, `PartyLeft`, in ClientParty.

Seen on a bot run: server announcements ("X joined.") are the noisiest entries, as
`ChatReceived` with the System kind; a direct line sent names the partner by id, not name.

## Next

- Notice kinds (a protocol field; the server's calls each get a kind).
- The bots' judge reading an activity's slice (docs/features/bots.md).
- Entries kept by the server, for the tips.
