# Server diagnostics

The server writes its logs and traces through OpenTelemetry, the open standard most
viewers read. They go to a JSON lines file, and by OTLP to Grafana running in Docker
(chosen 2026-09-26: free, local, one container with one compose file).

## Reading them in Grafana

1. Start the viewer once; it restarts with Docker:
   `docker compose -f docker/docker-compose.yml up -d`
2. Open http://localhost:3000, then **Explore**. No login is needed (anonymous access
   is on in this image; admin / admin exists for settings).
3. **Loki** holds the logs: `{service_name="mmo-server"}`, then filter on any
   attribute (`| scope_name="Net.Rpc"`, `| player_name="Diag"`, `| rpc_method="Login"`).
4. **Tempo** holds the traces: search `{resource.service.name="mmo-server"}`, or open
   a trace id from a log line. A trace takes a few seconds to show in search.

The image is `grafana/otel-lgtm` (Loki, Tempo, Prometheus and Grafana in one), meant
for development, which is what it is for here. Its data is in the Docker volume
`mmo-game-3d_lgtm-data`. The server sends to `http://localhost:4318` by default;
`--viewer url` sends elsewhere and `--viewer off` sends nowhere. With no viewer running
the server plays on: the viewer's batches fail on their own thread and are dropped.

Known gap: an RPC argument that is itself an array (a character list, an inventory)
shows in Grafana as `System.String[]`, since OTLP carries only flat lists of simple
values. The file has the full values. The fix, for later: build `rpc.args` into a
readable form on the server before it is recorded, not the exporter's.

## What is recorded

| What | How | Where in the code |
| --- | --- | --- |
| Every RPC the server receives | A span named `Node/Method` (`Network/Login`), with the arguments and the sender's context. What the handler does runs inside it. | `NetworkNode.Received`, in every server-side `[Rpc]` method |
| Every RPC the server sends | A log record `rpc out`, with the arguments; one record for a send to many (`net.peers` is how many) | `NetworkNode.SendTo`, `NetworkNode.SendToMany` |
| Database work | A span `db <caller>` on the worker thread, a child of whatever queued it, and `db done <caller>` for the completion on the game thread | `PersistenceWorker.Enqueue` |
| Everything Godot prints | Log records under `Engine`: every `GD.Print`, and every engine error and warning with its file and line | `EngineLog`, registered with `OS.AddLogger` |
| The game's own spans | `Travel`, `SaveEveryone` | `ServerDiagnostics.Source` |
| Where each frame goes (load tests) | The `Stats:` console line with `--stats-every N`: frame, physics, traffic, worst frame, .NET collector pauses, the slowest parts of the tick | `ServerGame.PrintStats`, `TickProfile` (see performance.md) |
| Health | A record `diagnostics` every 10 s: packets and bytes in and out, and how many records the logging dropped | `ServerDiagnostics.Tick` |
| Every packet, replication included (off by default) | A log record `packet in` / `packet out` with the peer, channel, mode, kind (`sync`, `spawn`, `remote_call`...), size and payload | `PacketLogPeer` (C++, `native/`) drained by `NativePacketLog`, with `--log-packets` |

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
| With the packet log, C# peer wrapper (first try, removed) | 24 to 27 | 37 to 53 |
| With the packet log, native peer wrapper (`-LogPackets`) | 134 to 138 | 15 to 18 |

The default costs nothing measurable, and neither does the native packet log: about
40,000 packets out every 10 s were all recorded, none dropped. The first packet log,
a C# wrapper, cut the server to about 25 fps, and the cost was not the logging or the
packets:

- With the packet records switched off but the peer still wrapped, the server was
  just as slow. So the cost is the wrapper, a C# `MultiplayerPeerExtension`.
- A probe on every overridden method, at 100 players, found `_GetUniqueId` called
  about 480,000 times a second: about 19,000 times a frame, 110 times per packet.
  Our code in those calls took 20 ms a second in all; the rest is the call itself,
  from the engine into C#, about 1.5 microseconds each. 19,000 of those is about
  30 ms a frame, which is the drop from about 130 frames a second to about 25.
- Where the calls come from: Godot's replication (`scene_replication_interface.cpp`)
  checks `_has_authority()` for every synchronizer, for every client, every network
  frame, and that compares the node's authority with `multiplayer->get_unique_id()`.
  With the native ENet peer that call costs next to nothing; through a C# extension
  every one is a crossing.
- So the cost grows with synchronized nodes times clients times frames, not with
  packets, and nothing in the C# wrapper (caching the id, say) can remove it: the
  crossing happens before our code runs.
- Everything else was small: sending the packets 46 ms a second (4,200 calls), the
  packet records 5 to 7 ms a second.

So the packet log's peer is C++ now (`native/src/packet_log_peer.cpp`), and that is
the project's rule for anything the engine calls in its inner loops (CLAUDE.local.md,
"No C# on engine-called hot paths"). Every engine call stays native; the records wait
in a native buffer, and `NativePacketLog` takes them once a frame, one call into C#
for all of them. The buffer holds up to 16 MB a frame; past that, packets are dropped
and counted (`net.packets_dropped_total` in the health record). A record's time is
when it was drained, at most a frame after the packet.

What is left is volume: at 100 players the packet log writes about 6 MB a second of
JSON (446 MB in a 70 s load test). Fine for a test, not for leaving on.

The native extension is built by `scripts/native-build.ps1` (see `native/README.md`).
Without the build, `--log-packets` prints that and the server runs without it.

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

## Extending it

- **A new RPC node** (a `NetworkNode` subclass under Main): add it to `Networks`, and to
  `Networks.SetLog`, or its RPCs are not recorded. In each server-side `[Rpc]` method,
  wrap the handler in `using (Activity? span = Received(MethodName.X, sender, args...))`;
  send with `SendTo` (one peer) or `SendToMany` (the same message to many: converted and
  logged once).
- **A span of the game's own:** `using (Activity? span = ServerDiagnostics.Source.
  StartActivity("Name")) { ... }`, with `span?.SetTag("key", value)` for attributes.
  Database work queued inside it becomes its child by itself.
- **Context on a player's records:** `ServerDiagnostics.Tag(peer, "key", value)`; it is
  added to every record about that peer from then on.
- **A log line:** `GD.Print` goes to the file under `Engine` (and the console). Note a
  print costs about 2.5 ms of the main thread on Windows (performance.md), so keep them
  to events, not per frame.
- **Another destination:** records go through OpenTelemetry in
  `src/Diagnostics/Telemetry.cs`, with our JSON lines exporters behind a
  `BoundedProcessor` (drop and count, never block), and the OTLP exporter beside them
  on its own batch processor (it drops without a count: the SDK hands the service name
  only to a processor it holds itself, so it cannot sit behind the cap). Anything that
  takes OTLP (the Datadog Agent, a collector) needs only a different `--viewer`.

## The native packet log, in detail

- `native/src/packet_log_peer.cpp`: `PacketLogPeer`, a `MultiplayerPeerExtension` in
  C++. `wrap(inner)` takes the server's `ENetMultiplayerPeer`; every method passes
  straight to it, and `_put_packet` / `_get_packet` also append a record to a byte
  buffer: direction (1 byte), peer (4), channel (1), transfer mode (1), size (4), then
  the payload. Past 16 MB in a frame, records are dropped and counted.
  `drain()` returns the buffer and starts a new one; `take_dropped()` the count.
- `native/packet_log.gdextension` names the DLL; `register_types.cpp` registers the
  class. The extension is loaded at runtime, only by a server with `--log-packets`
  (`NativePacketLog.Wrap` calls `GDExtensionManager.LoadExtension`), so the editor and
  clients never need the DLL, and a machine without the build just runs without the
  packet log.
- `game/server/diagnostics/NativePacketLog.cs` drains once a frame (`ServerGame._Process`),
  parses the records (the header layout above; change both sides together) and hands
  each to `ServerDiagnostics` as a `packet in` / `packet out` record, with the packet's
  kind read from Godot's multiplayer header (`sync`, `spawn`, `remote_call`...).
- To check it works: `.\scripts\native-build.ps1`, then
  `.\scripts\load-test.ps1 -Bots 10 -Seconds 30 -LogPackets`, then
  `python tools/diag-query/query.py --where logger=Net.Packets --group-by net.direction,net.kind`.
- Building, versions (godot-cpp is pinned to 4.5, which loads in 4.7), Linux, and adding
  another extension: `native/README.md`.
