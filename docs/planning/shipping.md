# Shipping: when to host, when to add Steam, how updates and assets travel

**Date:** 2026-09-23. Direction, not decisions. Nothing here is built or scheduled; it
records what triggers each piece of work and what it buys, so a later session does not
re-derive it.

The author's plan, in order: clean up the assets, add a little more functionality, build
real maps rather than test scenes, then put the server on a real host.

## The order, and what starts each one

| Work | Starts when | Held back because |
|---|---|---|
| A real host | Soon: as soon as the current pipeline work lands | Nothing. It is cheap and it answers questions localhost cannot |
| Steam accounts | Someone other than the author runs a build he did not hand over | It forces the Steam client into every dev session and complicates local testing |
| Updates and deploy (CI) | The same moment: another person runs a client against a hosted server | `scripts/play.ps1` is the whole pipeline while there is one machine |
| Achievements | After the content is stable | They name things in the game that do not exist yet |
| Asset protection | Just before a public release, and only the cheap level | It costs iteration speed for protection that cannot hold |

## A real host, first

The reason to go early is that these cannot be learned on one machine:

- **Latency and jitter.** The client has no prediction, so a step waits a round trip. On
  localhost that is 0 ms. At 40 ms it is a different game, and the feel should be known
  before the design leans on it.
- **Loss and MTU.** The protocol is stateless by repetition: a lost datagram costs one
  tick. That is a theory until packets really drop.
- **NAT and firewalls.** UDP from a home connection to a cloud host is the first real
  test of the port and of the keep-alive interval.
- **Restarts and persistence.** A deploy mid-session, and whether the world comes back.
- **A second person connecting**, which is the point of all of it.

Cost: a small VPS, Docker Compose, one UDP port. The image exists and `scripts/up.ps1` is nearly
the deploy script.

What to measure there, because it feeds the parked message-delivery topic
(`features/message-delivery-and-performance.md`): round-trip time and loss from home to
the host; the tick's own time against its 33 ms budget; bytes out per client per second
against the 10.8 KB/s measured locally; and a soak run with the wander bots connecting
over the internet rather than the loopback (`engineering/soak-runs.md`).

**The caveat.** There is no transport encryption and no real auth: a license key in a
file is the whole identity, on purpose (`CLAUDE.local.md`). That is fine for a closed
test with people the author knows, and not fine for an open port advertised anywhere.
Restrict by address, or keep the address private, until Steam auth exists.

## Steam accounts

The trigger is distribution, not features. When a build leaves the author's machine,
identity has to stop being a file a player can copy. Steam gives that: the client asks
Steam for an auth ticket, the server validates it against Steam's Web API and gets a
Steam ID back.

The code is already shaped for the swap. `LicenseKey` is a `Guid` in a profile file, and
the server resolves an account from it; Steam replaces the credential, not the shape. It
stays small as long as nothing in the simulation learns what a Steam ID is.

Cost to know: a Steam app is $100 once, and the auth path cannot be tested without it.

## Updates

Two pipelines, and Steam owns one of them:

- **The client** goes to Steam as a depot upload, and players update automatically. The
  **assets** ride with it, because they are baked into the client build.
- **The server** is the author's own: an image to a host. Steam has nothing to do with
  it.

One consequence of the version decision (2026-09-23): maps and art share one
`AssetVersion`, so an art change is a new client build **and** a server redeploy.
Splitting the version would let art ship alone. Do that when the coupling actually
annoys, not before.

When the trigger arrives, the first thing to build is CI that builds the server image and
the client, runs the tests, and publishes both. That is the test gate removed from the
Dockerfile on 2026-09-22, arriving where it belongs.

## Asset protection

Plainly: a shipped client can be extracted. Whatever it draws is in memory and on the
GPU. Protection buys effort, not prevention. Cheapest first:

1. **One packed archive** instead of loose `.vox` files. Stops copying them out of the
   folder, which is the case the author described. It fits the pipeline: the compiler
   already writes the package, so packing is one more step.
2. **Obfuscate or encrypt that archive** with a key in the binary. Stops the lazy;
   anyone with a debugger walks through it.
3. **Stream assets per session from the server.** Real cost, real complexity, still
   extractable.

Do 1 at release. Go further only if the art turns out to be what people value. Two things
to keep in mind: packing works against mods, which for this game may be a feature worth
keeping; and the model packs carry licences (`src/Client/Content/Licenses`), some requiring
attribution, so packing must not hide a licence file that has to ship.
