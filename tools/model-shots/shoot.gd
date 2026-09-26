# Renders each model or scene given on the command line to a PNG, framed to fit, so a
# model can be seen without opening the editor. See README.md.
extends SceneTree

const SIZE := Vector2i(256, 256)

var _paths: PackedStringArray = []
var _out := "user://model-shots"


func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	var i := 0

	while i < args.size():
		if args[i] == "--out" and i + 1 < args.size():
			_out = args[i + 1]
			i += 2
			continue

		_paths.append(args[i])
		i += 1

	DirAccess.make_dir_recursive_absolute(_out)
	root.size = SIZE
	_shoot.call_deferred()


func _shoot() -> void:
	RenderingServer.set_default_clear_color(Color(0.8, 0.84, 0.88))

	var light := DirectionalLight3D.new()
	light.rotation_degrees = Vector3(-50, -35, 0)
	root.add_child(light)

	var camera := Camera3D.new()
	camera.fov = 35
	root.add_child(camera)
	camera.make_current()

	for path in _paths:
		var resource = load(path)

		if resource == null:
			print("missing: ", path)
			continue

		var node: Node3D = resource.instantiate()
		root.add_child(node)
		var box := _bounds(node, Transform3D.IDENTITY)
		var middle := box.get_center()
		var reach: float = max(box.size.length(), 0.01)
		camera.position = middle + Vector3(0.9, 0.7, 1.2).normalized() * reach * 1.4
		camera.look_at(middle)

		await process_frame
		await process_frame
		await RenderingServer.frame_post_draw

		var name := path.get_file().get_basename()
		var file := _out.path_join(name + ".png")
		root.get_texture().get_image().save_png(file)
		print("wrote ", file, "  bounds ", box)
		node.queue_free()
		await process_frame

	quit()


func _bounds(node: Node, parent: Transform3D) -> AABB:
	var here := parent
	var box := AABB()
	var found := false

	if node is Node3D:
		here = parent * (node as Node3D).transform

	if node is VisualInstance3D:
		box = here * (node as VisualInstance3D).get_aabb()
		found = true

	for child in node.get_children():
		var inner := _bounds(child, here)

		if inner.size != Vector3.ZERO:
			box = inner if not found else box.merge(inner)
			found = true

	return box
