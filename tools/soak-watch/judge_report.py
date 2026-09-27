"""Judge report: the bots' position findings (game/dev/BotPositionJudge.cs) as one
report, each with the server's side of it: what the server received from and sent to
that player in the minute around the finding, its zone changes, and its errors.

    python tools/soak-watch/judge_report.py

Reads %TEMP%/mmo-game-3d-bots/judge/findings.jsonl and the server's diagnostics files,
writes %TEMP%/mmo-game-3d-bots/judge/report.md, and prints a summary.
"""

import collections
import datetime
import glob
import json
import os
import sys

FOLDER = os.path.join(os.environ.get("TEMP", "/tmp"), "mmo-game-3d-bots", "judge")
DIAGNOSTICS = os.path.join(os.environ.get("APPDATA", ""), "Godot", "app_userdata", "mmo-game-3d", "diagnostics")
WINDOW = datetime.timedelta(seconds=60)


def when(text):
    return datetime.datetime.fromisoformat(text.replace("Z", "+00:00"))


def load(path):
    rows = []
    with open(path, encoding="utf-8", errors="replace") as f:
        for line in f:
            try:
                rows.append(json.loads(line))
            except ValueError:
                pass
    return rows


def server_records(findings):
    """The server's records in the time the findings span, from the files covering it."""
    if not findings:
        return []
    first = min(when(f["ts"]) for f in findings) - WINDOW
    records = []
    for path in sorted(glob.glob(os.path.join(DIAGNOSTICS, "server-7070-*.jsonl")), key=os.path.getmtime):
        modified = datetime.datetime.fromtimestamp(os.path.getmtime(path), datetime.timezone.utc)
        if modified < first:
            continue
        for record in load(path):
            if "ts" in record:
                records.append(record)
    return records


def server_side(finding, records):
    at = when(finding["ts"])
    name = finding["player"]
    received = collections.Counter()
    sent = collections.Counter()
    events = []
    for record in records:
        t = when(record["ts"])
        if abs(t - at) > WINDOW:
            continue
        attributes = record.get("attributes", {})
        message = record.get("message", "")
        about_player = attributes.get("player.name") == name
        if record["type"] == "span" and about_player and record.get("name", "").count("/") == 1:
            received[record["name"]] += 1
        elif record["type"] == "log" and about_player and message == "rpc out":
            sent[str(attributes.get("rpc.service")) + "/" + str(attributes.get("rpc.method"))] += 1
        elif record["type"] == "log" and name in message:
            events.append(t.strftime("%H:%M:%S") + " " + message[:140])
        elif record["type"] == "log" and record.get("level") in ("Error", "Critical", "Warning"):
            events.append(t.strftime("%H:%M:%S") + " " + record.get("level", "") + ": " + message[:140])
    return received, sent, events


def client_side(f):
    """The judge's own fields: a position finding's, or a zone finding's."""
    if "changes" in f:
        return "**Client:** in " + f["zone"] + ", doing \"" + f["activity"] + "\". Zone changes, newest last: " + "; ".join(f["changes"]) + "."
    text = ("**Client:** at " + str(f["position"]) + " in " + f["zone"] + ", on `" + (f["under"] or "nothing") + "` ("
            + str(f["above"]) + " m above it), HP " + str(f["hp"]) + (", airborne" if f["airborne"] else "") + ". "
            + "Doing \"" + f["activity"] + "\", step \"" + f["step"] + "\""
            + (", heading for " + str(f["target"]) + ", " + str(f["to_target"]) + " m away" if f["target"] else "") + ". "
            + "Before (seconds ago, x, y, z): " + "; ".join(str(r) for r in f["recent"]))
    return text


def write(findings, records):
    lines = ["# Judge report", "",
             "Written " + datetime.datetime.now().strftime("%Y-%m-%d %H:%M") + " from " + str(len(findings)) + " findings.", "",
             "| Time | Bot | Kind | Zone | Detail |", "| --- | --- | --- | --- | --- |"]
    for f in findings:
        lines.append("| " + when(f["ts"]).astimezone().strftime("%H:%M:%S") + " | " + f["bot"] + " | " + f["kind"] + " | "
                     + f["zone"] + " | " + f["detail"] + " |")
    for f in findings:
        received, sent, events = server_side(f, records)
        lines += ["", "## " + when(f["ts"]).astimezone().strftime("%H:%M:%S") + " " + f["bot"] + ": " + f["kind"], "",
                  f["detail"], "", "![" + f["bot"] + "](shots/" + f["shot"] + ")", "",
                  client_side(f), "",
                  "**Server, a minute either side:** received " + (", ".join(k + " x" + str(n) for k, n in received.most_common(8)) or "nothing")
                  + "; sent " + (", ".join(k + " x" + str(n) for k, n in sent.most_common(8)) or "nothing") + "."]
        if events:
            lines += [""] + ["- " + e for e in events[:12]]
    path = os.path.join(FOLDER, "report.md")
    with open(path, "w", encoding="utf-8") as out:
        out.write("\n".join(lines) + "\n")
    return path


def main():
    path = os.path.join(FOLDER, "findings.jsonl")
    if not os.path.exists(path):
        print("No findings yet.")
        return 0
    findings = load(path)
    report = write(findings, server_records(findings))
    kinds = collections.Counter(f["kind"] for f in findings)
    print(str(len(findings)) + " findings (" + ", ".join(k + " " + str(n) for k, n in kinds.most_common()) + "): " + report)
    return 0


if __name__ == "__main__":
    sys.exit(main())
