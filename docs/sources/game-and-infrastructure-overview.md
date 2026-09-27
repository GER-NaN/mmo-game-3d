# Game & Infrastructure Overview (as of 2026-09-06)

This document captures everything Claude currently knows about the game
concept and the technical infrastructure behind it, at a high level. It's a
snapshot of shared understanding, not a spec — it should be updated as the
game idea and architecture evolve.

## What the project is

A C#/.NET 9 MMO-style game built with UDP client/server networking. The
explicit framing from the start has been: this is a learning project for
game networking and server architecture, but the goal is **production-real
patterns at toy scale** — "we don't need to scale for World of Warcraft, but
I want what is built to be real and not a toy." That framing applies to
networking, security posture, and general software architecture equally:
correct fundamentals over premature scale/perf work, and correct
fundamentals over premature security hardening.

No worldbuilding, story, or gameplay-mechanic decisions have been made
inside the codebase or in prior conversations with Claude — those exist (or
will exist) in the Gemini brainstorming conversations being folded into this
Worldbuilding folder, not here. This document is intentionally scoped to
system design, not game design.

## High-level architecture

**Client/Server split over UDP**, with three main code areas:

- **Core** — shared code referenced by both Server and Client. Holds the
  network message contracts (client→server and server→client messages) and
  the authoritative simulation layer.
- **Server** — the authoritative game server. Owns all game state and the
  fixed-tick simulation loop. Also owns networking/session concerns:
  tracking connections, associating them with player identity, and
  enforcing that only sessioned connections can issue gameplay intent.
- **Client** — the player-facing application, built on MonoGame (converted
  from an earlier, more minimal client). Sends intent messages to the
  server and renders the world views it receives back.

### Simulation model

- The simulation is the single source of truth for game state. It runs on a
  **fixed-tick loop** (not event-driven, not async) — time-based logic like
  movement integration, collisions, and interactions all happen inside the
  tick, not inline with message handling.
- Message handlers exist only to translate incoming network messages into
  *intent* calls into the simulation (e.g. "this player moved with this
  velocity"). Handlers never mutate game state directly — that's the
  simulation's job alone.
- The simulation is explicitly **not thread-safe by design** — it assumes a
  single-threaded driver (the tick loop) and deliberately avoids locking.
  Any move toward concurrency would be a real architecture conversation, not
  an incremental patch.
- There's an intentional split between **internal state** (the simulation's
  own model of the world, e.g. players) and **outward-facing views** (
  simplified, render-only DTOs sent to clients). Views only carry what a
  client needs to render/consume — internal state doesn't leak into them.

### Networking model

- Transport is raw **UDP**, with a fixed-tick server loop using
  non-blocking socket polling rather than async I/O — async/await is
  avoided project-wide by explicit preference.
- Session/identity concepts exist: connections are associated with a
  session, and game-affecting messages are only accepted from authenticated
  sessions. This is "closed by default" authorization at a basic level, not
  full auth — there's no real login/account system yet.
- Deliberately deferred, known gaps (not oversights): no transport
  encryption (UDP traffic is cleartext), and no mid-session
  endpoint/IP-rebinding support (doing that safely would require signed
  tokens tied to a real auth system — a bare session id isn't enough on its
  own to authorize rebinding). These are treated as future, discuss-first
  design decisions, not things to bolt on reactively.
- Networking/session bookkeeping is kept separate from the simulation layer
  — the simulation only ever deals in player identity (a `Guid`), never
  sockets, endpoints, or session state directly.

### Testing & tooling

- xUnit-based unit tests plus a separate integration test project; shared
  test doubles live in their own (non-test) helper project referenced by the
  tests.
- `dotnet test` gates a Docker publish pipeline, so test health is tied to
  deployability, even at this small scale.
- Standalone dev tools (e.g. a pixel-art converter/browser used for art
  pipeline experimentation) are deliberately kept **outside** the main
  solution and outside source control — they're workshop tools, not part of
  the shipped game.

## Current state of gameplay/game-design content

None yet, formally. The simulation currently models basic player presence
and movement (position/velocity-driven movement with collision handling) —
enough to prove out the networking and simulation architecture, not a
reflection of intended final gameplay. Actual gameplay ideas, world lore,
and campaign/story content live in the brainstorming material being
gathered into this Worldbuilding folder, and haven't yet been reconciled
with what's technically implemented.

## What this document deliberately excludes

- Code-level details (class names, file structure, message formats) — those
  live in the codebase and its own `CLAUDE.local.md`, and are expected to
  keep changing as the architecture evolves.
- Any gameplay mechanics, world lore, or narrative content — see the other
  documents in this folder for that.
