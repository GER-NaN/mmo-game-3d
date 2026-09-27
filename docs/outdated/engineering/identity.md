# Identity

Four identifiers. Each one is created by exactly one thing, and each stage knows only
the ones it needs.

| Identifier | What it means | Created by | Lifetime |
| --- | --- | --- | --- |
| License key | Proof a copy of the game was bought. Not a person. | The player's machine (`LicenseKeyLoader`) | Forever, per purchase |
| Account id | Who the player is. Owns everything that persists. | The server, from the license key | Forever, per player |
| Session id | This connection, authenticated. | `ClientRegistry` | One connection |
| Player id | The entity in the world this session controls. | The server, at connect | One session, for now |

## Where each one exists

| Stage | License key | Account id | Session id | Player id |
| --- | --- | --- | --- | --- |
| `ClientConnectionRequest` | yes | - | - | - |
| Pending connection | yes | - | - | - |
| Account lookup | in | out | - | - |
| `ClientSession` | no | yes | yes | yes |
| `ServerConnectionStatus` | - | no | yes | yes |
| Simulation | - | - | - | yes |

## Rules

- **The license key stops at the connect boundary.** It reaches the account lookup and
  goes no further. It never reaches the client's answer, the session, or the simulation.
- **The session id is a credential.** It authenticates every later message, so it is
  never broadcast.
- **The player id is public.** Every client receives it in the world view, which is why
  it can never be the session id.
- **The simulation knows player ids only.** No endpoints, no sessions, no accounts.

## Not yet decided

- **Real auth.** The client will present a token from a login, not a license key. The
  key then becomes an entitlement recorded on the account: the account *has* a license,
  rather than the key proving who you are.
- **Characters.** A persistent character id would sit between the account and the world
  entity, and the player id would stop dying with the session - a logged-off character
  can stay in the world.
- **Multiple licenses per account.** `accounts.license_key` assumes one. A `licenses`
  table splits them when a player can own more than one.
