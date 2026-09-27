"""Finds narrow gaps between the box colliders of a zone scene, where a player can get
wedged. See README.md.

    python tools/map-gaps/map_gaps.py game/zones/town/town.tscn
    python tools/map-gaps/map_gaps.py game/zones/*/*.tscn --max 2.5
"""

import argparse
import glob
import itertools
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
IDENTITY = [1.0, 0, 0, 0, 1.0, 0, 0, 0, 1.0, 0, 0, 0]


def transform(block):
    match = re.search(r"transform = Transform3D\(([^)]*)\)", block)
    return [float(x) for x in match.group(1).split(",")] if match else IDENTITY


def boxes_of_shapes(text):
    """Box sizes by sub_resource id."""
    sizes = {}
    for match in re.finditer(r'\[sub_resource type="BoxShape3D" id="([^"]+)"\]\s*\nsize = Vector3\(([^)]*)\)', text):
        sizes[match.group(1)] = [float(x) for x in match.group(2).split(",")]
    return sizes


def footprint(owner, shape, size):
    """The x and z range a box covers, from its owner's transform and its shape's own."""
    # Transform3D(xx, xy, xz, yx, yy, yz, zx, zy, zz, ox, oy, oz): the basis columns first.
    cx = owner[9] + shape[9] * owner[0] + shape[11] * owner[6]
    cz = owner[11] + shape[9] * owner[2] + shape[11] * owner[8]
    ax = abs(owner[0]) * size[0] / 2 + abs(owner[6]) * size[2] / 2
    az = abs(owner[2]) * size[0] / 2 + abs(owner[8]) * size[2] / 2
    return cx - ax, cx + ax, cz - az, cz + az, size[1]


def solid_boxes(path):
    """A solid scene's boxes, in its own space: (shape transform, size). None for an
    Area (a door's trigger stops no one)."""
    text = open(path, encoding="utf-8").read()
    root = re.search(r'\[node name="[^"]+" type="([^"]+)"', text)
    if not root or root.group(1) != "StaticBody3D":
        return None
    sizes = boxes_of_shapes(text)
    found = []
    for block in re.split(r"\n(?=\[node )", text):
        shape = re.search(r'shape = SubResource\("([^"]+)"\)', block)
        if block.startswith("[node") and "CollisionShape3D" in block and shape and shape.group(1) in sizes:
            found.append((transform(block), sizes[shape.group(1)]))
    return found


def zone_boxes(path):
    text = open(os.path.join(ROOT, path), encoding="utf-8").read()
    scenes = dict((m.group(2), m.group(1)) for m in re.finditer(r'\[ext_resource type="PackedScene" path="res://([^"]+)" id="([^"]+)"\]', text))
    sizes = boxes_of_shapes(text)
    blocks = re.split(r"\n(?=\[node )", text)
    bodies = {}
    boxes = []

    for block in blocks:
        header = re.search(r'\[node name="([^"]+)"(?: type="([^"]+)")?(?: parent="([^"]*)")?(?: instance=ExtResource\("([^"]+)"\))?', block)
        if not header:
            continue
        name, kind, parent, instance = header.groups()
        path_here = name if parent in (None, ".") else parent + "/" + name

        if instance and instance in scenes:
            scene = os.path.join(ROOT, scenes[instance])
            inner = solid_boxes(scene) if os.path.exists(scene) else None
            for shape, size in inner or []:
                boxes.append((name,) + footprint(transform(block), shape, size))
        elif kind == "StaticBody3D":
            bodies[path_here] = transform(block)
        elif kind == "CollisionShape3D" and parent in bodies:
            shape = re.search(r'shape = SubResource\("([^"]+)"\)', block)
            if shape and shape.group(1) in sizes:
                boxes.append((parent.split("/")[-1],) + footprint(bodies[parent], transform(block), sizes[shape.group(1)]))

    return boxes


def covered(a, b, axis, boxes):
    """Whether another box fills the gap between a and b."""
    for c in boxes:
        if c is a or c is b:
            continue
        if axis == "x":
            lo, hi = min(a[2], b[2]), max(a[1], b[1])
            if c[1] <= lo + 0.05 and c[2] >= hi - 0.05 and c[3] < max(a[3], b[3]) + 0.5 and c[4] > min(a[4], b[4]) - 0.5:
                return True
        else:
            lo, hi = min(a[4], b[4]), max(a[3], b[3])
            if c[3] <= lo + 0.05 and c[4] >= hi - 0.05 and c[1] < max(a[1], b[1]) + 0.5 and c[2] > min(a[2], b[2]) - 0.5:
                return True
    return False


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("zones", nargs="+", help="zone scenes, relative to the repo")
    parser.add_argument("--max", type=float, default=2.0, help="report gaps narrower than this, in metres (default 2)")
    parser.add_argument("--min", type=float, default=0.9, help="ignore gaps narrower than this: a player (1 m wide) cannot get in (default 0.9)")
    parser.add_argument("--depth", type=float, default=1.0, help="ignore gaps shallower than this: too short to be held in (default 1)")
    parser.add_argument("--min-height", type=float, default=2.5, help="ignore boxes lower than this: a player steps or jumps over them")
    args = parser.parse_args()

    paths = []
    for pattern in args.zones:
        paths.extend(glob.glob(os.path.join(ROOT, pattern)) or [pattern])

    open_gaps = 0
    for path in paths:
        rel = os.path.relpath(path, ROOT).replace("\\", "/")
        boxes = [b for b in zone_boxes(rel) if b[5] >= args.min_height]
        for a, b in itertools.combinations(boxes, 2):
            gx = max(a[1], b[1]) - min(a[2], b[2])
            gz = max(a[3], b[3]) - min(a[4], b[4])
            if args.min <= gx < args.max and -gz >= args.depth and not covered(a, b, "x", boxes):
                print("%s: %.2f m gap in x between %s and %s, x %.2f..%.2f, z %.2f..%.2f" % (rel, gx, a[0], b[0], min(a[2], b[2]), max(a[1], b[1]), max(a[3], b[3]), min(a[4], b[4])))
                open_gaps += 1
            if args.min <= gz < args.max and -gx >= args.depth and not covered(a, b, "z", boxes):
                print("%s: %.2f m gap in z between %s and %s, z %.2f..%.2f, x %.2f..%.2f" % (rel, gz, a[0], b[0], min(a[4], b[4]), max(a[3], b[3]), max(a[1], b[1]), min(a[2], b[2])))
                open_gaps += 1
        print("%s: %d boxes" % (rel, len(boxes)))

    print("%d open gaps under %.1f m" % (open_gaps, args.max))
    return 1 if open_gaps else 0


if __name__ == "__main__":
    sys.exit(main())
