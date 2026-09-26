# model-shots

Renders models or scenes to PNG files, one each, framed to fit, so a model can be looked
at (or shown side by side) without opening the editor. It opens a small window for a
moment and closes by itself.

```
& $godot --path . --script tools/model-shots/shoot.gd -- --out C:/temp/shots res://assets/kaykit/city_builder_bits/assets/trash_A.gltf res://game/props/city_builder_bits/CarSedan.tscn
```

- `$godot` is the console Godot exe named in `scripts/client-up.ps1`.
- Paths are `res://` paths to a `.gltf`, `.glb` or `.tscn`.
- `--out` is the folder for the PNGs (default: the project's `user://model-shots`).
- It prints each model's bounds too, which is how the prop scales were checked.
