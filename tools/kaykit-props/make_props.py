"""Writes game-ready prop scenes for KayKit models: game/props/<pack>/<Name>.tscn.

Each prop is the model scaled to the game's size, plus a collision box sized from the
model's own bounds, on a StaticBody3D on the World physics layer. Flat pieces (roads,
paving) get no collision and keep their own height, so a road is a surface to walk on
rather than a 0.5 m slab; soft ones (bushes) get no collision. Thin tall things (trees, lights) get a
narrow trunk instead of their full bounds, so a player can walk under the branches.

Run from the repo root after adding models to PROPS below:

    python tools/kaykit-props/make_props.py

It overwrites the prop scenes it writes and leaves others alone. Zone scenes use the
props, not the raw models, so a scale change here reaches every zone.
"""

import json
import os
import struct

ASSETS = "assets/kaykit"
OUT = "game/props"

# Placeholders until seen in the game: the city pieces are miniatures, so x5 makes a
# car 4.7 m long and a building 10 m wide; furniture is near character size.
CITY_SCALE = 5.0
FURNITURE_SCALE = 0.8

# (pack folder, model name, scale, collision): collision is "box", "trunk", "flat" or
# "none". "block" is a box that also stops the chase camera (buildings): the camera
# comes in front of it rather than looking through.
PROPS = []

for letter in "ABCDEFGH":
    PROPS.append(("city_builder_bits", "building_" + letter, CITY_SCALE, "block"))

for name in ["road_straight", "road_straight_crossing", "road_corner", "road_corner_curved",
             "road_junction", "road_tsplit", "base", "park_base"]:
    PROPS.append(("city_builder_bits", name, CITY_SCALE, "flat"))

for name in ["bush", "bush_A", "bush_B", "bush_C"]:
    PROPS.append(("city_builder_bits", name, CITY_SCALE, "none"))

for name in ["tree_A", "tree_B", "tree_C", "tree_D", "tree_E", "streetlight",
             "streetlight_old_single", "streetlight_old_double", "trafficlight_A", "firehydrant"]:
    PROPS.append(("city_builder_bits", name, CITY_SCALE, "trunk"))

for name in ["car_sedan", "car_hatchback", "car_taxi", "car_police", "car_stationwagon",
             "bench", "dumpster", "trash_A", "trash_B", "box_A", "box_B"]:
    PROPS.append(("city_builder_bits", name, CITY_SCALE, "box"))

PROPS.append(("city_builder_bits", "watertower", CITY_SCALE, "block"))

for name in ["desk", "desk_decorated", "monitor", "keyboard", "chair_desk_A", "table_medium",
             "table_small", "shelf_B_large_decorated", "cabinet_medium", "couch"]:
    PROPS.append(("furniture_bits", name, FURNITURE_SCALE, "box"))

# Resource pieces are near character size; the chest is a placeholder "old hardware" chest.
RESOURCE_SCALE = 0.6

for name in ["Gems_Chest", "Gems_Chest_Empty"]:
    PROPS.append(("resource_bits", name, RESOURCE_SCALE, "box"))

# A trunk is this fraction of the model's width, at least this wide in world units.
TRUNK_FRACTION = 0.15
TRUNK_MIN = 0.3


def load_gltf(path):
    if path.endswith(".glb"):
        data = open(path, "rb").read()
        length = struct.unpack_from("<I", data, 12)[0]
        return json.loads(data[20:20 + length])
    with open(path) as f:
        return json.load(f)


# The model's bounds in its own units, from each mesh's POSITION accessor and the node
# offsets above it. KayKit props are not rotated inside the file, so rotations are skipped.
def bounds(path):
    gltf = load_gltf(path)
    nodes = gltf["nodes"]
    low = [1e9, 1e9, 1e9]
    high = [-1e9, -1e9, -1e9]

    def walk(index, offset, scale):
        node = nodes[index]
        t = node.get("translation", [0, 0, 0])
        s = node.get("scale", [1, 1, 1])
        here = [offset[k] + t[k] * scale[k] for k in range(3)]
        scaled = [scale[k] * s[k] for k in range(3)]

        if "mesh" in node:
            for primitive in gltf["meshes"][node["mesh"]]["primitives"]:
                accessor = gltf["accessors"][primitive["attributes"]["POSITION"]]
                for k in range(3):
                    low[k] = min(low[k], here[k] + accessor["min"][k] * scaled[k])
                    high[k] = max(high[k], here[k] + accessor["max"][k] * scaled[k])

        for child in node.get("children", []):
            walk(child, here, scaled)

    for root in gltf["scenes"][0]["nodes"]:
        walk(root, [0, 0, 0], [1, 1, 1])

    return low, high


def scene_name(model):
    return "".join(part[:1].upper() + part[1:] for part in model.split("_"))


def number(value):
    return ("%.3f" % value).rstrip("0").rstrip(".")


def write_prop(pack, model, scale, collision):
    source = os.path.join(ASSETS, pack, "assets", model + ".gltf")
    if not os.path.exists(source):
        print("missing:", source)
        return

    low, high = bounds(source)
    size = [(high[k] - low[k]) * scale for k in range(3)]
    center = [(high[k] + low[k]) / 2 * scale for k in range(3)]
    name = scene_name(model)

    blocks_camera = collision == "block"

    if blocks_camera:
        collision = "box"

    if collision == "trunk":
        width = max(TRUNK_MIN, min(size[0], size[2]) * TRUNK_FRACTION)
        size = [width, size[1], width]
        center = [0.0, center[1], 0.0]

    # Some models (the cars) reach below their origin: lift them so they stand on the
    # ground rather than sink into it.
    lift = 0.0

    if collision != "flat" and low[1] < 0:
        lift = -low[1] * scale
        center[1] += lift

    lines = []
    has_shape = collision not in ("none", "flat")
    height_scale = 1.0 if collision == "flat" else scale
    lines.append("[gd_scene load_steps=%d format=3]" % (3 if has_shape else 2))
    lines.append("")
    lines.append('[ext_resource type="PackedScene" path="res://%s" id="1_model"]' % source.replace("\\", "/"))
    lines.append("")

    if has_shape:
        lines.append('[sub_resource type="BoxShape3D" id="BoxShape3D_prop"]')
        lines.append("size = Vector3(%s, %s, %s)" % tuple(number(v) for v in size))
        lines.append("")
        lines.append('[node name="%s" type="StaticBody3D"]' % name)

        # World (1) plus CameraBlock (4); see game/PhysicsLayers.cs.
        if blocks_camera:
            lines.append("collision_layer = 5")
    else:
        lines.append('[node name="%s" type="Node3D"]' % name)

    lines.append("")
    lines.append('[node name="Model" parent="." instance=ExtResource("1_model")]')
    lines.append("transform = Transform3D(%s, 0, 0, 0, %s, 0, 0, 0, %s, 0, %s, 0)" % (number(scale), number(height_scale), number(scale), number(lift)))

    if has_shape:
        lines.append("")
        lines.append('[node name="Collision" type="CollisionShape3D" parent="."]')
        lines.append("transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, %s, %s, %s)" % tuple(number(v) for v in center))
        lines.append('shape = SubResource("BoxShape3D_prop")')

    folder = os.path.join(OUT, pack)
    os.makedirs(folder, exist_ok=True)

    with open(os.path.join(folder, name + ".tscn"), "w", newline="\n") as f:
        f.write("\n".join(lines) + "\n")


def main():
    for pack, model, scale, collision in PROPS:
        write_prop(pack, model, scale, collision)
    print("wrote", len(PROPS), "props to", OUT)


if __name__ == "__main__":
    main()
