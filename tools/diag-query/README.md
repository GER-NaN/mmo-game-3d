# diag-query

Searches the server's diagnostics file, for when Grafana is not running or a question
is easier to answer from a script. It filters,
groups and counts records, and prints a trace as a tree. The file format is in
`docs/engineering/diagnostics.md`.

Needs Python 3. With no `--file`, it reads the newest file in
`%APPDATA%\Godot\app_userdata\mmo-game-3d\diagnostics`.

```
# The last records, newest file
python tools/diag-query/query.py

# Everything one player sent and received, by RPC
python tools/diag-query/query.py --where player.name=Bea --group-by type,rpc.method

# Every span, with its duration
python tools/diag-query/query.py --where type=span --limit 0

# One trace: the login RPC, its database work, and what was sent back
python tools/diag-query/query.py --where name=Network/Login --limit 1
python tools/diag-query/query.py --trace <trace_id from the line above>

# With --log-packets: packets by kind and direction
python tools/diag-query/query.py --where logger=Net.Packets --group-by net.direction,net.kind

# Where the time goes: every span by name, with count, mean, p95, max and total, and
# for database calls the mean wait in the queue first (a load test's slow handlers)
python tools/diag-query/query.py --durations
python tools/diag-query/query.py --durations --where name=Network/

# Errors and warnings from the engine or the game
python tools/diag-query/query.py --where logger=Engine --where level=Error
```

`--where KEY=TEXT` matches when the field contains the text, ignoring case. A key is a
top-level field (`type`, `logger`, `name`, `level`, `trace_id`) or an attribute
(`player.name`, `rpc.method`, `net.kind`). Repeat `--where` to combine them.
