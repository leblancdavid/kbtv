extends SceneTree
## Godot 4.6 mono: --path . --script Tools/modelgen/preview_wall_prints.gd
## Captures actual room lighting with the gameplay camera angle into %TEMP%/opencode.

func _initialize() -> void:
	call_deferred("capture")

func capture() -> void:
	root.size = Vector2i(1280, 720)
	var world := load("res://scenes/world3d/World3D.tscn").instantiate() as Node3D
	root.add_child(world)
	var prints := get_nodes_in_group("WallPrint")
	assert(prints.size() == 9, "Expected nine mounted wall prints")
	for print_node in prints:
		assert(print_node is MeshInstance3D and print_node.material_override.albedo_texture != null)
	var camera := world.get_node("WorldCamera") as Camera3D
	world.set_process(false)
	var output: String = OS.get_environment("LOCALAPPDATA") + "/Temp/opencode"
	assert(DirAccess.make_dir_recursive_absolute(output) == OK)
	for room in ["control", "studio"]:
		var center := Vector3(0.0, 0.0, 1.2 if room == "control" else -6.0)
		camera.global_position = center + Vector3(0, 10, 13)
		camera.look_at(center)
		for frame in range(8):
			await process_frame
		await RenderingServer.frame_post_draw
		var path: String = output + "/wall_prints_" + room + ".png"
		assert(root.get_texture().get_image().save_png(path) == OK)
		print("Wall print capture: " + path)
		camera.projection = Camera3D.PROJECTION_PERSPECTIVE
		camera.fov = 65
		camera.global_position = Vector3(0, 2.1, 2.6 if room == "control" else -5.1)
		camera.look_at(Vector3(0, 1.45, 0 if room == "control" else -8))
		for frame in range(3):
			await process_frame
		await RenderingServer.frame_post_draw
		var detail: String = output + "/wall_prints_" + room + "_detail.png"
		assert(root.get_texture().get_image().save_png(detail) == OK)
		camera.projection = Camera3D.PROJECTION_ORTHOGONAL
	var player := world.get_node("Player3D") as CharacterBody3D
	player.set_physics_process(false)
	var north_print := world.get_node("StudioRoom3D/WallPrintPosterMothman") as MeshInstance3D
	player.global_position = Vector3(-4.15, 0, -7.65)
	for frame in range(40):
		await process_frame
	assert(north_print.material_override.albedo_color.a < 0.65, "Wall print should fade near player")
	player.global_position = Vector3(0, 0, 4)
	for frame in range(50):
		await process_frame
	assert(north_print.material_override.albedo_color.a > 0.95, "Wall print should return to opaque")
	print("Wall print fading verified")
	quit()
