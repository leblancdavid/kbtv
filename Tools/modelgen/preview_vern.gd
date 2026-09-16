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
	if "--animations" in OS.get_cmdline_user_args():
		await capture_animations(world, viewport)
	print("VERN_GODOT_PREVIEWS_SAVED")
	quit()

func capture_animations(world: Node, viewport: SubViewport) -> void:
	var players = world.find_children("*", "AnimationPlayer", true, false)
	var player: AnimationPlayer
	for candidate in players:
		if candidate.has_animation("talking_default"):
			player = candidate
			break
	assert(player != null)
	player.callback_mode_process = AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_MANUAL
	var directory = OS.get_environment("LOCALAPPDATA") + "/Temp/opencode/vern_godot_frames"
	DirAccess.make_dir_recursive_absolute(directory)
	for clip in ["idle_breathing", "talking_default", "smoking", "drink_coffee"]:
		var animation = player.get_animation(clip)
		animation.loop_mode = Animation.LOOP_NONE
		player.play(clip, 0)
		for frame in range(roundi(animation.length * 12) + 1):
			player.seek(frame / 12.0, true)
			await process_frame
			await RenderingServer.frame_post_draw
			viewport.get_texture().get_image().save_png(directory + "/%s_feed_%03d.png" % [clip, frame])
			var wide = root.get_texture().get_image()
			wide.resize(640, 360)
			wide.save_png(directory + "/%s_wide_%03d.png" % [clip, frame])
	print("VERN_GODOT_FRAMES " + directory)
