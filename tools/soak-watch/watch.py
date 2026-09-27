"""Soak watch: one look at the bots of a soak run (scripts/bots-up.ps1) and the server,
printed as one line, with a screenshot of any bot that looks stuck.

    python tools/soak-watch/watch.py                one look now
    python tools/soak-watch/watch.py --every 300    a look every 5 minutes, until the
                                                    server stops

A bot is flagged when it did nothing since the last look (idle), when one action is
most of what it did (looping), when its process is gone, or when its log has new
errors. The server is flagged for new errors in its diagnostics file. Screenshots go
to %TEMP%/mmo-game-3d-bots/shots/.
"""

import argparse
import collections
import datetime
import glob
import json
import os
import re
import subprocess
import sys
import time

LOGS = os.path.join(os.environ.get("TEMP", "/tmp"), "mmo-game-3d-bots")
SHOTS = os.path.join(LOGS, "shots")
STATE = os.path.join(LOGS, "watch-state.json")
DIAGNOSTICS = os.path.join(os.environ.get("APPDATA", ""), "Godot", "app_userdata", "mmo-game-3d", "diagnostics")
SHOT_SCRIPT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "shot.ps1")

# One action this share of a bot's actions, over at least this many, is a loop.
LOOP_SHARE = 0.7
LOOP_MIN = 12


def load_state():
    try:
        with open(STATE, encoding="utf-8") as f:
            return json.load(f)
    except (OSError, ValueError):
        return {}


def save_state(state):
    with open(STATE, "w", encoding="utf-8") as f:
        json.dump(state, f)


def new_lines(path, state):
    """The lines added to a file since the last look; a shorter file restarted."""
    size = os.path.getsize(path)
    start = state.get(path, 0)
    if size < start:
        start = 0
    with open(path, encoding="utf-8", errors="replace") as f:
        f.seek(start)
        text = f.read()
    state[path] = size
    return text.splitlines()


def running_bots():
    out = subprocess.run(
        ["powershell", "-NoProfile", "-Command",
         "Get-CimInstance Win32_Process -Filter \"Name like 'Godot%'\" | ForEach-Object { $_.CommandLine }"],
        capture_output=True, text=True).stdout
    return set(re.findall(r"--profile (soak\d+)", out)), "--server" in out


def action(line):
    # "Bot: pressing F at "[F] Go Online: Public terminal"" -> "pressing F at ..."
    return re.sub(r"\"[^\"]*\"", "...", line[5:].strip())


def look(state, shots):
    bots, server_up = running_bots()
    flags = []
    quiet = 0
    for path in sorted(glob.glob(os.path.join(LOGS, "soak*.log"))):
        name = os.path.splitext(os.path.basename(path))[0]
        lines = new_lines(path, state)
        acts = [action(l) for l in lines if l.startswith("Bot:")]
        errors = sorted({l.strip()[:120] for l in lines if l.startswith("ERROR") or "Exception" in l or "Fatal" in l})
        problem = ""
        if name not in bots:
            problem = "not running"
        elif any("refused" in l for l in lines):
            problem = "login refused"
        elif not acts:
            problem = "idle"
        else:
            top, count = collections.Counter(acts).most_common(1)[0]
            if len(acts) >= LOOP_MIN and count / len(acts) >= LOOP_SHARE:
                problem = "looping on '" + top + "' (" + str(count) + " of " + str(len(acts)) + ")"
        if errors:
            problem = (problem + "; " if problem else "") + str(len(errors)) + " new errors: " + " | ".join(errors[:2])
        if problem:
            shot = ""
            if shots and name in bots:
                shot = take_shot(name)
            flags.append(name + " " + problem + (" [" + shot + "]" if shot else ""))
        else:
            quiet += 1

    server = server_errors(state)
    stamp = datetime.datetime.now().strftime("%H:%M")
    line = stamp + " server " + ("up" if server_up else "DOWN") + ", " + str(quiet) + " bots fine"
    if server:
        line += "; server: " + server
    if flags:
        line += "; " + "; ".join(flags)
    return line, server_up


def server_errors(state):
    files = sorted(glob.glob(os.path.join(DIAGNOSTICS, "server-7070-*.jsonl")), key=os.path.getmtime)
    if not files:
        return ""
    found = collections.Counter()
    for raw in new_lines(files[-1], state):
        try:
            record = json.loads(raw)
        except ValueError:
            continue
        if record.get("type") == "log" and record.get("level") in ("Error", "Critical", "Warning"):
            found[record.get("message", "")[:100]] += 1
    return " | ".join(str(n) + "x " + m for m, n in found.most_common(3))


def take_shot(name):
    os.makedirs(SHOTS, exist_ok=True)
    path = os.path.join(SHOTS, datetime.datetime.now().strftime("%Y%m%d-%H%M") + "-" + name + ".png")
    subprocess.run(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", SHOT_SCRIPT, name, path],
                   capture_output=True)
    return os.path.basename(path) if os.path.exists(path) else ""


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--every", type=int, default=0, help="seconds between looks; 0 looks once")
    parser.add_argument("--no-shots", action="store_true", help="flag bots, take no screenshots")
    args = parser.parse_args()

    state = load_state()
    while True:
        line, server_up = look(state, not args.no_shots)
        save_state(state)
        print(line, flush=True)
        if args.every <= 0 or not server_up:
            return 0
        time.sleep(args.every)


if __name__ == "__main__":
    sys.exit(main())
