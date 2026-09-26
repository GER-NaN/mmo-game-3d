"""Search, group and trace the server's diagnostics file (JSON lines).

A stand-in until a viewer is set up: filter records by field, count them grouped by
any field, or print one trace as a tree. See README.md for examples.
"""

import argparse
import glob
import json
import os
import sys
from collections import Counter, defaultdict


def default_file():
    folder = os.path.join(os.environ.get("APPDATA", ""), "Godot", "app_userdata", "mmo-game-3d", "diagnostics")
    files = glob.glob(os.path.join(folder, "*.jsonl"))
    return max(files, key=os.path.getmtime) if files else None


# A field is a top-level key (type, logger, name, level, trace_id...) or an attribute.
def field(record, key):
    if key in record:
        return record[key]
    return record.get("attributes", {}).get(key)


def matches(record, filters):
    for key, value in filters:
        actual = field(record, key)
        if actual is None or value.lower() not in str(actual).lower():
            return False
    return True


def load(path, filters):
    with open(path, encoding="utf-8") as lines:
        for line in lines:
            if not line.strip():
                continue
            record = json.loads(line)
            if matches(record, filters):
                yield record


def short(record, payload):
    attributes = dict(record.get("attributes", {}))
    if not payload and "net.payload" in attributes:
        attributes["net.payload"] = "<%d bytes>" % attributes.get("net.bytes", 0)
    what = record.get("name") or record.get("message")
    extra = " %.2f ms trace=%s" % (record["duration_ms"], record["trace_id"]) if record["type"] == "span" else ""
    return "%s %-4s %-12s %s%s %s" % (record["ts"][11:23], record["type"], record.get("logger", record.get("source", "")), what, extra, json.dumps(attributes, ensure_ascii=False))


def print_trace(records, trace_id, payload):
    spans = [r for r in records if r.get("trace_id") == trace_id]
    children = defaultdict(list)
    for record in spans:
        parent = record.get("parent_id") if record["type"] == "span" else record.get("span_id")
        children[parent].append(record)

    def walk(parent, depth):
        for record in sorted(children.get(parent, []), key=lambda r: r["ts"]):
            print("  " * depth + short(record, payload))
            if record["type"] == "span":
                walk(record["span_id"], depth + 1)

    walk(None, 0)


# Where the server's time goes, by span: the slowest handlers and database calls, and
# for database spans how long they waited in the queue first.
def print_durations(records, keys):
    times = defaultdict(list)
    waits = defaultdict(list)
    for record in records:
        if record.get("type") != "span":
            continue
        group = "  ".join(str(field(record, k)) for k in keys)
        times[group].append(record["duration_ms"])
        wait = field(record, "db.queue_wait_ms")
        if wait is not None:
            waits[group].append(float(wait))

    print("%7s %9s %9s %9s %10s %10s  %s" % ("count", "mean ms", "p95 ms", "max ms", "total ms", "queue ms", "span"))
    for group, values in sorted(times.items(), key=lambda item: -sum(item[1])):
        values.sort()
        p95 = values[min(len(values) - 1, int(len(values) * 0.95))]
        wait = "%10.2f" % (sum(waits[group]) / len(waits[group])) if waits[group] else "%10s" % "-"
        print("%7d %9.2f %9.2f %9.2f %10.1f %s  %s" % (len(values), sum(values) / len(values), p95, values[-1], sum(values), wait, group))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--file", help="the .jsonl file (default: the newest server file)")
    parser.add_argument("--where", action="append", default=[], metavar="KEY=TEXT", help="keep records whose field contains TEXT; repeatable")
    parser.add_argument("--group-by", metavar="KEY[,KEY]", help="count records by these fields instead of listing them")
    parser.add_argument("--trace", metavar="TRACE_ID", help="print one trace as a tree of spans with their logs")
    parser.add_argument("--limit", type=int, default=50, help="records to list (default 50, 0 for all)")
    parser.add_argument("--payload", action="store_true", help="show packet payloads instead of their size")
    parser.add_argument("--durations", action="store_true", help="span times by name (or --group-by): count, mean, p95, max and total, most total first")
    args = parser.parse_args()

    path = args.file or default_file()
    if not path:
        sys.exit("No diagnostics file found; pass --file.")

    filters = []
    for item in args.where:
        key, _, value = item.partition("=")
        filters.append((key, value))

    records = list(load(path, filters))

    if args.trace:
        print_trace(records, args.trace, args.payload)
    elif args.durations:
        print_durations(records, args.group_by.split(",") if args.group_by else ["name"])
    elif args.group_by:
        keys = args.group_by.split(",")
        counts = Counter(tuple(str(field(r, k)) for k in keys) for r in records)
        for group, count in counts.most_common():
            print("%8d  %s" % (count, "  ".join(group)))
    else:
        shown = records if args.limit == 0 else records[: args.limit]
        for record in shown:
            print(short(record, args.payload))
        print("-- %d of %d records, from %s" % (len(shown), len(records), path))


if __name__ == "__main__":
    main()
