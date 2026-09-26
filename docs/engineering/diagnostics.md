# Server diagnostics

The server writes its logs and traces through OpenTelemetry, the open standard most
viewers read (SigNoz, Grafana, Jaeger, Seq, the Datadog Agent). Today they go to a
JSON lines file. A viewer is a second exporter in `src/Diagnostics/Telemetry.cs`,
and nothing that writes records changes.

## What is recorded

| What | How | Where in the code |
| --- | --- | --- |
| Every RPC the server receives | A span named `Node/Method` (`Network/Login`), with the arguments and the sender's context. What the handler does runs inside it. | `NetworkNode.Received`, in every server-side `[Rpc]` method |
| Every RPC the server sends | A log record `rpc out`, with the arguments | `NetworkNode.SendTo` |
| Database work | A span `db <caller>` on the worker thread, a child of whatever queued it, and `db done <caller>` for the completion on the game thread | `PersistenceWorker.Enqueue` |
| Everything Godot prints | Log records under `Engine`: every `GD.Print`, and every engine error and warning with its file and line | `EngineLog`, registered with `OS.AddLogger` |
| The game's own spans | `Travel`, `SaveEveryone` | `ServerDiagnostics.Source` |
| Health | A record `diagnostics` every 10 s: packets and bytes in and out, and how many records the logging dropped | `ServerDiagnostics.Tick` |
| Every packet, replication included (off by default) | A log record `packet in` / `packet out` with the peer, channel, mode, kind (`sync`, `spawn`, `remote_call`...), size and payload | `LoggedPeer`, with `--log-packets` |

Context: records about a peer carry `player.name`, `player.id` and `player.zone`, set
by `ServerGame` with `ServerDiagnostics.Tag`. A log written inside a span carries the
span's `trace_id` and `span_id`, so a login reads as one trace: the Login RPC, the
account lookup on the worker, the completion, the Accept sent back, and the "logged
in" line.

## Performance

Writing a record puts it on a bounded queue in memory. It formats nothing and does no
I/O. One background thread per signal writes the queue out about once a second. When
a queue is full, new records are dropped and counted (`logs.dropped_total`,
`spans.dropped_total` in the health record), so the game thread never waits.

Measured 2026-09-26 with `scripts/load-test.ps1 -Bots 100`, the same worst case as
`load-test.md` (100 players who all see each other):

| Server | fps | Frame (ms) |
| --- | --- | --- |
| Diagnostics off (`-NoDiagnostics`) | 126 to 131 | 15 to 29 |
| Diagnostics on (the default) | 132 to 137 | 16 to 18 |
| With the packet log (`-LogPackets`) | 24 to 27 | 37 to 53 |

The default costs nothing measurable. The packet log does, and the cost is not the
logging: with the packet records switched off but the peer still wrapped, the server
was just as slow. The wrapper is a C# `MultiplayerPeerExtension`, and every packet
then crosses from the engine into C# several times (transfer mode, channel, target,
the packet itself). At 100 players that is about 4,400 packets a second out. The
packet log also writes about 6 MB a second of JSON at that load.

So the packet log is for small tests (a few players), not for load. Ways to capture
packets at load without that cost, not decided:

- Out of process: a UDP relay or a packet capture on the server's port, which then
  decodes ENet. No cost in the server. It sees ENet frames, not Godot's commands.
- A native (C++) GDExtension peer wrapper. No C# crossing, but a C++ build in the
  project.

## The file

A new file per server run: `%APPDATA%\Godot\app_userdata\mmo-game-3d\diagnostics\
server-<port>-<date>-<time>.jsonl`. `--diagnostics path` chooses another,
`--diagnostics off` writes none. One JSON object per line:

```
{"ts":"...","type":"log","service":"mmo-server","level":"Debug","logger":"Net.Rpc",
 "message":"rpc out","trace_id":"...","span_id":"...",
 "attributes":{"rpc.service":"Network","rpc.method":"Accept","net.peer":1588405868,
 "rpc.args":["town","Diag"],"player.name":"Diag","player.id":"..."}}

{"ts":"...","type":"span","service":"mmo-server","name":"Network/Login","kind":"Server",
 "source":"MmoGame3d.Server","trace_id":"...","span_id":"...","duration_ms":2.37,
 "status":"Unset","attributes":{"rpc.method":"Login","rpc.args":[5,"...","Diag","a"],...},
 "events":[]}
```

Payloads are base64. `tools/diag-query` searches, groups and prints traces from the
file.
