extends SceneTree

# Fills a zone's Terrain3D data with rolling hills from a seed, flat around the entry, as
# a start to sculpt from in the editor. Also writes the shared ground material and
# textures (game/terrain/) if they are missing. See README.md for the options.
#
# It works a few frames after start (Terrain3D makes its data object once in the tree)
# and quits by frame 30 whatever happens, so an error never leaves it idle.

var options := {
	"zone": "",
	"size": 1000.0,
	"seed": 1,
	"height": 30.0,
	"frequency": 0.002,
	"octaves": 4,
	"flat": Vector2.ZERO,
	"flat_radius": 30.0,
	"flat_blend": 60.0,
	"terraces": 0,
	"step": 0.15,
}

var frames := 0
var terrain: Terrain3D

const SHARED := "res://game/terrain/"


func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	var i := 0
	while i < args.size() - 1:
		var key: String = args[i].trim_prefix("--").replace("-", "_")
		var value: String = args[i + 1]
		if options.has(key):
			match typeof(options[key]):
				TYPE_FLOAT:
					options[key] = value.to_float()
				TYPE_INT:
					options[key] = value.to_int()
				TYPE_VECTOR2:
					var parts := value.split(",")
					options[key] = Vector2(parts[0].to_float(), parts[1].to_float())
				_:
					options[key] = value
		i += 2

	if options.zone == "":
		printerr("Give a zone: --zone meadows")
		quit(1)
		return

	terrain = Terrain3D.new()
	root.add_child(terrain)


func _process(_delta: float) -> bool:
	frames += 1

	if frames == 5 and terrain != null:
		write_shared()
		write_heights()

	return frames >= 30


func write_heights() -> void:
	# Terrain3D snaps an import to its region grid, so the image starts on a region edge
	# and covers whole regions; otherwise every height lands shifted from where it was
	# worked out.
	var region := float(terrain.region_size)
	var half := ceilf(options.size / 2.0 / region) * region
	var samples := int(half * 2.0)
	var noise := FastNoiseLite.new()
	noise.seed = options.seed
	noise.noise_type = FastNoiseLite.TYPE_PERLIN
	noise.frequency = options.frequency
	noise.fractal_octaves = options.octaves
	var img := Image.create_empty(samples, samples, false, Image.FORMAT_RF)

	for z in samples:
		for x in samples:
			var at := Vector2(x - half, z - half)
			# Noise is -1 to 1; the ground is 0 to height.
			var h := (noise.get_noise_2dv(at) + 1.0) * 0.5
			if options.terraces > 0:
				h = terrace(h)
			var blend := smoothstep(options.flat_radius, options.flat_radius + options.flat_blend, at.distance_to(options.flat))
			img.set_pixel(x, z, Color(h * options.height * blend, 0.0, 0.0, 1.0))

	# The control map with only its "auto" bit set everywhere: the material's auto shader
	# then puts grass on the flat and rock on the slopes until someone paints.
	var bits := PackedInt32Array()
	bits.resize(samples * samples)
	bits.fill(1)
	var control := Image.create_from_data(samples, samples, false, Image.FORMAT_RF, bits.to_byte_array())
	terrain.data.import_images([img, control, null], Vector3(-half, 0.0, -half), 0.0, 1.0)
	var dir := "res://game/zones/%s/terrain" % options.zone
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(dir))
	terrain.data.save_directory(dir)
	print("Wrote ", terrain.data.get_region_count(), " regions to ", dir, " (", half * 2.0, " m, height 0 to ", options.height, " m)")


# Flat levels with a steep step between each: a cliff too steep to walk.
func terrace(h: float) -> float:
	var level: float = h * options.terraces
	var floor_level := floorf(level)
	var step := smoothstep(1.0 - options.step, 1.0, level - floor_level)
	return (floor_level + step) / options.terraces


func write_shared() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(SHARED))

	if not FileAccess.file_exists(SHARED + "ground_assets.tres"):
		var assets := Terrain3DAssets.new()
		# The auto shader puts the base texture (0) on slopes and the overlay (1) on the flat.
		assets.set_texture(0, texture("Rock", "res://assets/terrain/rock023"))
		assets.set_texture(1, texture("Grass", "res://assets/terrain/ground037"))
		ResourceSaver.save(assets, SHARED + "ground_assets.tres")
		print("Wrote ", SHARED, "ground_assets.tres")

	if not FileAccess.file_exists(SHARED + "ground_material.tres"):
		var material := Terrain3DMaterial.new()
		# Grass on the flat, rock on the slopes, without painting.
		material.auto_shader = true
		material.world_background = Terrain3DMaterial.NONE
		ResourceSaver.save(material, SHARED + "ground_material.tres")
		print("Wrote ", SHARED, "ground_material.tres")


func texture(label: String, base: String) -> Terrain3DTextureAsset:
	var asset := Terrain3DTextureAsset.new()
	asset.name = label
	asset.albedo_texture = load(base + "_alb_ht.png")
	asset.normal_texture = load(base + "_nrm_rgh.png")
	asset.uv_scale = 0.1
	return asset
