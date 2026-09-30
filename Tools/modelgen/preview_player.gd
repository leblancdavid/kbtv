extends SceneTree
## Godot 4.6 mono: --path . --script Tools/modelgen/preview_player.gd
## Validates the actual player instance, captures gameplay and close studio lighting.

func _initialize() -> void:
	call_deferred("capture")

func require(condition: bool, message: String) -> bool:
	if not condition:
		push_error(message)
		quit(1)
	return condition

func capture() -> void:
	root.size = Vector2i(1280, 720)
	var world = load("res://scenes/world3d/World3D.tscn").instantiate()
	root.add_child(world)
	for i in range(15):
		await process_frame
	var player = world.get_node("Player3D")
	var visual = player.get_node("Visual")
	var rigs = visual.find_children("*", "Skeleton3D", true, false)
	if not require(rigs.size() == 1, "Player must retain one imported skeleton"):
		return
	var meshes = visual.find_children("*", "MeshInstance3D", true, false)
	if not require(meshes.size() >= 5, "Separate body/wardrobe meshes required"):
		return
	for mesh in meshes:
		if not require(mesh.skin != null and mesh.material_override == null,
				"Player mesh must be skinned and retain its authored material"):
			return
	if not require(player.get_node("CollisionShape3D").shape is CapsuleShape3D,
			"Player collision capsule missing"):
		return
	if DisplayServer.get_name() == "headless":
		print("PLAYER_GODOT_VALIDATED bones=", rigs[0].get_bone_count(), " meshes=", meshes.size())
		quit()
		return
	await RenderingServer.frame_post_draw
	root.get_texture().get_image().save_png("res://docs/art/model_previews/player_godot_gameplay.png")
	world.set_process(false)
	var camera = Camera3D.new()
	world.add_child(camera)
	camera.global_position = player.global_position + Vector3(2.5, 1.7, 3.5)
	camera.look_at(player.global_position + Vector3(0, .9, 0))
	camera.projection = Camera3D.PROJECTION_ORTHOGONAL
	camera.size = 2.4
	camera.current = true
	for i in range(3):
		await process_frame
	await RenderingServer.frame_post_draw
	root.get_texture().get_image().save_png("res://docs/art/model_previews/player_godot_close.png")
	print("PLAYER_GODOT_VALIDATED bones=", rigs[0].get_bone_count(), " meshes=", meshes.size())
	# Separate neutral-light detail stage, explicitly distinct from station lighting.
	world.queue_free()
	await process_frame
	var stage = Node3D.new()
	root.add_child(stage)
	stage.add_child(load("res://scenes/world3d/PlayerModel.tscn").instantiate())
	var detail_camera = Camera3D.new()
	stage.add_child(detail_camera)
	detail_camera.position = Vector3(1.6, 1.75, 3)
	detail_camera.look_at(Vector3(0, 1.47, 0))
	detail_camera.projection = Camera3D.PROJECTION_ORTHOGONAL
	detail_camera.size = .58
	detail_camera.current = true
	for position in [Vector3(1, 3, 3), Vector3(-2, 2, 1)]:
		var light = OmniLight3D.new()
		stage.add_child(light)
		light.position = position
		light.omni_range = 8
		light.light_energy = 2
	for i in range(4):
		await process_frame
	await RenderingServer.frame_post_draw
	root.get_texture().get_image().save_png("res://docs/art/model_previews/player_collar_godot_neutral.png")
	detail_camera.position = Vector3(1.6, 1.75, -3)
	detail_camera.look_at(Vector3(0, 1.47, 0))
	var rear_light = OmniLight3D.new()
	stage.add_child(rear_light)
	rear_light.position = Vector3(1, 3, -3)
	rear_light.omni_range = 8
	rear_light.light_energy = 2
	for i in range(3):
		await process_frame
	await RenderingServer.frame_post_draw
	root.get_texture().get_image().save_png("res://docs/art/model_previews/player_collar_godot_back.png")
	quit()
