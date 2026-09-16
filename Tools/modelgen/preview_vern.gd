extends SceneTree
## Run with Godot 4.6 mono (graphical renderer): --path . --script Tools/modelgen/preview_vern.gd
## Captures the actual studio and its broadcast SubViewport, without starting a show.

func _initialize() -> void:
	call_deferred("capture")

func capture() -> void:
	root.size = Vector2i(1280, 720)
	var world = load("res://scenes/world3d/World3D.tscn").instantiate()
	root.add_child(world)
	for i in range(12):
		await process_frame
	await RenderingServer.frame_post_draw
	var viewport = world.get_node("VernCameraViewport") as SubViewport
	var image = viewport.get_texture().get_image()
	assert(not image.is_empty(), "Broadcast viewport must render")
	assert(image.save_png("res://docs/art/model_previews/vern_godot_feed.png") == OK)
	# Independent review camera; studio lighting, props and character are unchanged.
	var camera = Camera3D.new()
	world.add_child(camera)
	camera.position = Vector3(-3.4, 2.9, -0.8)
	camera.look_at(Vector3(-.6, .95, -3.95))
	camera.fov = 48
	camera.current = true
	world.set_process(false)
	for i in range(3):
		await process_frame
	await RenderingServer.frame_post_draw
	assert(root.get_texture().get_image().save_png("res://docs/art/model_previews/vern_godot_studio.png") == OK)
	print("VERN_GODOT_PREVIEWS_SAVED")
	quit()
