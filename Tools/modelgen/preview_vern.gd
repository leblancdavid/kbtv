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
	if "--review" in OS.get_cmdline_user_args():
		await capture_review(world)
		quit()
		return
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
		await capture_close_review(world, player, clip, animation, directory)
	print("VERN_GODOT_FRAMES " + directory)

func capture_close_review(world: Node, player: AnimationPlayer, clip: String, animation: Animation, directory: String) -> void:
	var camera := Camera3D.new()
	world.add_child(camera)
	camera.fov = 34
	camera.current = true
	var target := Vector3(-0.52, 0.92, -3.84)
	var samples: Array = {
		"idle_breathing": [0.5, 1.5, 2.5, 3.5],
		"talking_default": [0.5, 1.5, 2.5, 3.5, 4.5, 5.5, 6.5, 7.5],
		"smoking": [1.1, 2.5, 4.6],
		"drink_coffee": [1.1, 2.5, 4.6],
	}.get(clip, [minf(1.0, animation.length)])
	var sheets := { "front": [], "side": [] }
	for time in samples:
		player.seek(clampf(time, 0.0, animation.length), true)
		for view in ["front", "side"]:
			if view == "front":
				camera.position = Vector3(-1.45, 1.25, -4.75)
			else:
				camera.position = Vector3(-1.85, 1.20, -3.78)
			camera.look_at(target)
			await process_frame
			await RenderingServer.frame_post_draw
			var image := root.get_texture().get_image()
			image.resize(640, 360)
			sheets[view].append(image.duplicate())
			image.save_png(directory + "/%s_close_%s_%03d.png" % [clip, view, roundi(time * 10.0)])
	for view in ["front", "side"]:
		_write_sheet(sheets[view], directory + "/%s_sheet_%s.png" % [clip, view])
	camera.queue_free()

func _write_sheet(images: Array, path: String) -> void:
	if images.is_empty():
		return
	var tile := images[0] as Image
	var cols := mini(4, images.size())
	var rows := ceili(float(images.size()) / float(cols))
	var sheet := Image.create(tile.get_width() * cols, tile.get_height() * rows, false, tile.get_format())
	for i in images.size():
		var image := images[i] as Image
		var pos := Vector2i((i % cols) * tile.get_width(), int(i / cols) * tile.get_height())
		sheet.blit_rect(image, Rect2i(Vector2i.ZERO, image.get_size()), pos)
	sheet.save_png(path)

func capture_review(world: Node3D) -> void:
	# Review-only removal of foreground occluders; never saved into the scene.
	for name in ["StudioDeskLamp", "StudioComputer", "StudioBoomMic"]:
		(world.get_node("StudioRoom3D/" + name) as Node3D).hide()
	var vern := world.get_node("StudioRoom3D/VernStation/Vern") as Node3D
	var player := vern.find_children("*", "AnimationPlayer", true, false)[0] as AnimationPlayer
	# Isolated clip review: suppress the controller, retain evaluated prop attachment.
	for child in vern.get_children():
		if child.get_script() != null and "VernAnimationController" in str(child.get_script().resource_path):
			child.process_mode = Node.PROCESS_MODE_DISABLED
	world.set_process(false)
	player.callback_mode_process = AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_MANUAL
	var directory := OS.get_environment("LOCALAPPDATA") + "/Temp/opencode/vern_reviews/" + str(Time.get_unix_time_from_system()).replace(".", "_")
	assert(DirAccess.make_dir_recursive_absolute(directory) == OK)
	var camera := Camera3D.new()
	vern.add_child(camera)
	camera.fov = 44
	camera.make_current()
	var light := OmniLight3D.new()
	vern.add_child(light)
	light.position = Vector3(0, 1.8, -1.0)
	light.omni_range = 4.0
	light.light_energy = 1.5
	light.light_cull_mask = 2
	var overlay := CanvasLayer.new()
	root.add_child(overlay)
	var label := Label.new()
	overlay.add_child(label)
	label.position = Vector2(16, 12)
	label.add_theme_font_size_override("font_size", 26)
	label.add_theme_color_override("font_shadow_color", Color.BLACK)
	var views := {"side": Vector3(1.9, 1.25, -0.15), "threequarter": Vector3(1.35, 1.6, -1.45), "contact": Vector3(0.65, 1.45, -0.95), "grip": Vector3(-0.65, 1.4, -0.85)}
	var manifest := {"kind": "isolated runtime assets; review fill light", "hidden_occluders": ["StudioDeskLamp", "StudioComputer", "StudioBoomMic"], "fps": 8, "clips": [], "lip_contact_verified": false}
	manifest["views"] = views.keys()
	var polish := "--polish" in OS.get_cmdline_user_args()
	var fps := 24 if polish else 8
	manifest["fps"] = fps
	var clips := ["smoking", "drink_coffee"] if polish else ["idle_breathing", "talk_calm", "talking_default", "smoking", "drink_coffee"]
	for clip in clips:
		var resolved := ""
		for name in player.get_animation_list():
			if String(name).get_slice("/", String(name).get_slice_count("/") - 1) == clip:
				resolved = name
		assert(not resolved.is_empty(), "Missing runtime clip: " + clip)
		var animation := player.get_animation(resolved)
		manifest.clips.append({"requested": clip, "resolved": resolved, "length": animation.length})
		for view in views:
			camera.position = views[view]
			camera.look_at(vern.to_global(Vector3(0, 1.15 if view in ["contact", "grip"] else 0.9, -0.05)))
			var images: Array = []
			var count := ceili(animation.length * fps)
			for frame in range(count):
				var time := frame / float(fps)
				player.play(resolved, 0)
				player.seek(time, true)
				label.text = "%s | %s | %.3fs | %s" % [resolved, view, time, directory.get_file()]
				await process_frame
				await RenderingServer.frame_post_draw
				var image := root.get_texture().get_image()
				image.resize(640, 360)
				assert(image.save_png(directory + "/%s_%s_%03d.png" % [clip, view, frame]) == OK)
				if frame % maxi(1, count / 8) == 0:
					images.append(image)
			_write_sheet(images, directory + "/%s_%s_sheet.png" % [clip, view])
	var file := FileAccess.open(directory + "/manifest.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(manifest, "  "))
	print("VERN_REVIEW " + directory)
