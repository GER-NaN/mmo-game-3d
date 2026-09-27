"""Bot findings report: completes each judge finding with the server's side and writes
one page to review them all.

    python tools/bot-watch/report.py

For every finding folder under %TEMP%/mmo-game-3d-bots/judge/ (written by the bots'
judges, game/dev/BotFindings.cs) that has no server side yet, it adds:

- server.jsonl: every record in the server's diagnostics about that player, in the
  minute either side of the finding, as the server wrote it;
- server.txt: the same, readable: what the server received from the player, what it
  sent, zone changes, and any warnings or errors.

Then it rewrites judge/report.html: every finding, newest first, with its picture, what
the judge saw, and links to all its files. watch.py runs it on every look.
"""

import collections
import datetime
import glob
import html
import json
import os
import sys

JUDGE = os.path.join(os.environ.get("TEMP", "/tmp"), "mmo-game-3d-bots", "judge")
DIAGNOSTICS = os.path.join(os.environ.get("APPDATA", ""), "Godot", "app_userdata", "mmo-game-3d", "diagnostics")
WINDOW = datetime.timedelta(seconds=60)

# Only the end of the server's file is read: new findings are minutes old at most.
TAIL_BYTES = 40 * 1024 * 1024


def when(text):
    return datetime.datetime.fromisoformat(text.replace("Z", "+00:00"))


def findings():
    found = []
    for path in glob.glob(os.path.join(JUDGE, "*", "finding.json")):
        try:
            with open(path, encoding="utf-8") as f:
                finding = json.load(f)
        except (OSError, ValueError):
            continue
        finding["_dir"] = os.path.dirname(path)
        found.append(finding)
    found.sort(key=lambda f: f["ts"], reverse=True)
    return found


def server_tail():
    files = sorted(glob.glob(os.path.join(DIAGNOSTICS, "server-7070-*.jsonl")), key=os.path.getmtime)
    if not files:
        return []
    path = files[-1]
    records = []
    with open(path, "rb") as f:
        f.seek(max(0, os.path.getsize(path) - TAIL_BYTES))
        if f.tell() > 0:
            f.readline()
        for raw in f:
            try:
                records.append(json.loads(raw))
            except ValueError:
                pass
    return records


def complete(finding, records):
    """Writes server.jsonl and server.txt for one finding."""
    at = when(finding["ts"])
    name = finding.get("player", "")
    near = []
    for record in records:
        if "ts" not in record or abs(when(record["ts"]) - at) > WINDOW:
            continue
        attributes = record.get("attributes", {})
        message = record.get("message", "")
        if attributes.get("player.name") == name or name in message or record.get("level") in ("Error", "Critical", "Warning"):
            near.append(record)

    with open(os.path.join(finding["_dir"], "server.jsonl"), "w", encoding="utf-8") as out:
        for record in near:
            out.write(json.dumps(record) + "\n")

    received = collections.Counter()
    sent = collections.Counter()
    lines = []
    for record in near:
        t = when(record["ts"]).astimezone().strftime("%H:%M:%S")
        attributes = record.get("attributes", {})
        message = record.get("message", "")
        if record["type"] == "span" and record.get("name", "").count("/") == 1:
            received[record["name"]] += 1
        elif record["type"] == "log" and message == "rpc out":
            sent[str(attributes.get("rpc.service")) + "/" + str(attributes.get("rpc.method"))] += 1
        elif record["type"] == "log":
            lines.append(t + "  " + record.get("level", "") + "  " + message[:200])

    text = ["Server side of " + finding["folder"] + ", " + str(len(near)) + " records in the minute either side of "
            + at.astimezone().strftime("%H:%M:%S") + ".", "",
            "Received from " + name + ": " + (", ".join(k + " x" + str(n) for k, n in received.most_common()) or "nothing"),
            "Sent to " + name + ": " + (", ".join(k + " x" + str(n) for k, n in sent.most_common()) or "nothing"), "",
            "Log lines (the player's, and every warning or error):"] + ["  " + l for l in lines]
    with open(os.path.join(finding["_dir"], "server.txt"), "w", encoding="utf-8") as out:
        out.write("\n".join(text) + "\n")


def page(all_findings):
    kinds = collections.Counter(f["kind"] for f in all_findings)
    parts = ["<!doctype html><meta charset='utf-8'><title>Bot findings</title>",
             "<style>body{font-family:system-ui,sans-serif;margin:24px;background:#16181c;color:#e6e6e6}"
             "a{color:#8cc4ff}.f{display:flex;gap:16px;border-top:1px solid #333;padding:14px 0}"
             "img{width:420px;border:1px solid #444}table{border-collapse:collapse;font-size:13px}"
             "td{padding:2px 8px;vertical-align:top}td:first-child{color:#999}h2{margin:0 0 6px;font-size:17px}"
             ".k{color:#ffcf66}</style>",
             "<h1>Bot findings</h1>",
             "<p>" + str(len(all_findings)) + " findings: " + ", ".join(html.escape(k) + " " + str(n) for k, n in kinds.most_common())
             + ". Written " + datetime.datetime.now().strftime("%Y-%m-%d %H:%M") + " by tools/bot-watch/report.py.</p>"]
    for f in all_findings:
        folder = os.path.basename(f["_dir"])
        rows = []
        for key, value in f.items():
            if key.startswith("_") or key in ("folder", "detail", "kind", "bot", "ts"):
                continue
            rows.append("<tr><td>" + html.escape(key) + "</td><td>" + html.escape(json.dumps(value) if not isinstance(value, str) else value) + "</td></tr>")
        links = " · ".join("<a href='" + folder + "/" + n + "'>" + n + "</a>"
                           for n in ("finding.json", "client.log", "server.txt", "server.jsonl") if os.path.exists(os.path.join(f["_dir"], n)))
        parts.append("<div class='f'><a href='" + folder + "/picture.png'><img src='" + folder + "/picture.png'></a><div>"
                     + "<h2>" + when(f["ts"]).astimezone().strftime("%H:%M:%S") + " " + html.escape(f["bot"])
                     + " <span class='k'>" + html.escape(f["kind"]) + "</span></h2>"
                     + "<p>" + html.escape(f["detail"]) + "</p><table>" + "".join(rows) + "</table><p>" + links + "</p></div></div>")
    with open(os.path.join(JUDGE, "report.html"), "w", encoding="utf-8") as out:
        out.write("\n".join(parts))


def run():
    """Completes new findings and rewrites the page; a short summary line."""
    if not os.path.isdir(JUDGE):
        return "no findings yet"
    all_findings = findings()
    new = [f for f in all_findings if not os.path.exists(os.path.join(f["_dir"], "server.txt"))]
    if new:
        records = server_tail()
        for finding in new:
            complete(finding, records)
    page(all_findings)
    return str(len(all_findings)) + " findings (" + str(len(new)) + " new): " + os.path.join(JUDGE, "report.html")


if __name__ == "__main__":
    print(run())
    sys.exit(0)
