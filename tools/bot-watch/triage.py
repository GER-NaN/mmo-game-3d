"""Triage: the judges' findings grouped, so a run's many findings read as a few issues.

    python tools/bot-watch/triage.py                 everything
    python tools/bot-watch/triage.py --since 02:00   from 02:00 today (local time)
    python tools/bot-watch/triage.py --kind stuck    one kind

Findings that share their kind, zone, persona, activity, step and what was underfoot
are one group: the same thing happening again. Each group prints its count, the bots
it happened to, and its newest finding's folder to look at first.
"""

import argparse
import collections
import datetime
import glob
import json
import os
import sys

JUDGE = os.path.join(os.environ.get("TEMP", "/tmp"), "mmo-game-3d-bots", "judge")


def load(since, kind):
    found = []
    for path in glob.glob(os.path.join(JUDGE, "*", "finding.json")):
        try:
            with open(path, encoding="utf-8") as f:
                finding = json.load(f)
        except (OSError, ValueError):
            continue
        at = datetime.datetime.fromisoformat(finding["ts"].replace("Z", "+00:00")).astimezone()
        if since is not None and at < since:
            continue
        if kind and finding["kind"] != kind:
            continue
        finding["_at"] = at
        finding["_dir"] = os.path.dirname(path)
        found.append(finding)
    return found


def key(finding):
    doing = finding.get("activity", "")
    persona = doing.split(": ")[0] if ": " in doing else ""
    activity = doing.split(" > ")[-1].split(": ")[-1]
    return (finding["kind"], finding.get("zone", ""), persona, activity, finding.get("step", ""), finding.get("under", "").split("/")[0])


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--since", help="HH:MM today, local time")
    parser.add_argument("--kind", help="only this kind")
    args = parser.parse_args()

    since = None
    if args.since:
        hour, minute = (int(x) for x in args.since.split(":"))
        since = datetime.datetime.now().astimezone().replace(hour=hour, minute=minute, second=0, microsecond=0)

    findings = load(since, args.kind)
    groups = collections.defaultdict(list)
    for finding in findings:
        groups[key(finding)].append(finding)

    print(str(len(findings)) + " findings in " + str(len(groups)) + " groups" + (" since " + args.since if args.since else "") + ":")
    for (kind, zone, persona, activity, step, under), items in sorted(groups.items(), key=lambda g: -len(g[1])):
        newest = max(items, key=lambda f: f["_at"])
        bots = sorted({f["bot"] for f in items})
        print("")
        print(str(len(items)).rjust(4) + "  " + kind + " in " + (zone or "?") + (" (" + persona + ")" if persona else "")
              + ", doing \"" + activity + "\"" + (" at \"" + step + "\"" if step else "") + (", on " + under if under else ""))
        print("      bots: " + ", ".join(bots) + "; latest " + newest["_at"].strftime("%H:%M:%S") + ": " + newest["detail"][:110])
        print("      look at: " + newest["_dir"])
    return 0


if __name__ == "__main__":
    sys.exit(main())
