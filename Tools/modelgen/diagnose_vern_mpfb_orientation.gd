extends SceneTree
## Runtime orientation diagnostic for the MPFB Vern model.
##
## Run with a graphical Godot 4.6 mono executable, not headless:
##   Godot_v4.6-console.exe --path . --script Tools/modelgen/diagnose_vern_mpfb_orientation.gd
##
## Captures raw/seated/rebaked poses with and without the Model-node yaw used by
## scenes/world3d/Vern.tscn. Camera is placed on runtime +Z, so a +Z-facing model
## should show its face and a -Z-facing model should show its back.

const MODEL := "res://assets/models3d/characters/vern_mpfb/vern_mpfb_fitted.glb"
const IDLE := "res://assets/models3d/characters/vern/animations/idle_breathing_mpfb.tres"
const TALK := "res://assets/models3d/characters/vern/animations/talk_calm_mpfb.tres"
const OUT := "C:/Users/lblan/AppData/Local/Temp/opencode/vern_mpfb_orientation"
const SIZE := Vector2i(640, 640)

var _vp: SubViewport

func _initialize() -> void:
	call_deferred("capture")

func capture() -> void:
	root.size = SIZE
	DirAccess.make_dir_recursive_absolute(OUT)
	for old in DirAccess.get_files_at(OUT):
		DirAccess.remove_absolute(OUT + "/" + old)

	_vp = SubViewport.new()
	_vp.size = SIZE
	_vp.own_world_3d = true
	_vp.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	_vp.transparent_bg = false
	root.add_child(_vp)
	_add_environment()

	var cases := [
		{"name": "raw_no_yaw", "yaw": false, "mode": "raw", "station": false},
		{"name": "raw_yaw", "yaw": true, "mode": "raw", "station": false},
		{"name": "seated_no_yaw", "yaw": false, "mode": "seated", "station": false},
		{"name": "seated_yaw", "yaw": true, "mode": "seated", "station": false},
		{"name": "idle_no_yaw", "yaw": false, "mode": "idle", "station": false},
		{"name": "idle_yaw", "yaw": true, "mode": "idle", "station": false},
		{"name": "talk_no_yaw", "yaw": false, "mode": "talk", "station": false},
		{"name": "talk_yaw", "yaw": true, "mode": "talk", "station": false},
		{"name": "station_seated_no_yaw", "yaw": false, "mode": "seated", "station": true},
		{"name": "station_seated_yaw", "yaw": true, "mode": "seated", "station": true},
		{"name": "station_idle_no_yaw", "yaw": false, "mode": "idle", "station": true},
		{"name": "station_idle_yaw", "yaw": true, "mode": "idle", "station": true},
	]
	var only := OS.get_environment("VERN_DIAG_CASE")
	for c in cases:
		if not only.is_empty() and c.name != only:
			continue
		await _capture_case(c.name, c.yaw, c.mode, c.station)
	print("VERN_MPFB_ORIENTATION " + OUT)
	quit()

func _capture_case(case_name: String, yaw: bool, mode: String, station: bool) -> void:
	var root3d := Node3D.new()
	root3d.name = case_name
	if station:
		root3d.transform = Transform3D(Basis(Vector3.UP, PI), Vector3(-0.85, 0.1, -0.05))
	_vp.add_child(root3d)

	var model := (load(MODEL) as PackedScene).instantiate() as Node3D
	if yaw:
		model.rotation.y = PI
	root3d.add_child(model)

	var player := _find_ap(model)
	assert(player != null)
	player.callback_mode_process = AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_MANUAL
	if mode == "seated":
		_play_suffix(player, "seated_rest", 0.0)
	elif mode == "idle":
		_add_clip(player, "diag", "idle_breathing", IDLE)
		_play_suffix(player, "idle_breathing", 1.0)
	elif mode == "talk":
		_add_clip(player, "diag", "talk_calm", TALK)
		_play_suffix(player, "talk_calm", 1.0)

	var cam := Camera3D.new()
	_vp.add_child(cam)
	if station:
		var look := root3d.to_global(Vector3(0, 0.88, 0))
		cam.position = look + Vector3(-1.05, 0.35, 2.8)
		cam.look_at(look, Vector3.UP)
		cam.fov = 38
	else:
		cam.position = Vector3(0, 1.05, 2.45)
		cam.look_at(Vector3(0, 0.82, 0), Vector3.UP)
		cam.fov = 34
	cam.current = true

	for i in range(4):
		await process_frame
	await RenderingServer.frame_post_draw
	var img := _vp.get_texture().get_image()
	assert(img != null)
	img.save_png(OUT + "/" + case_name + ".png")
	print("CASE " + case_name + " saved")

	_vp.remove_child(cam)
	cam.free()
	_vp.remove_child(root3d)
	root3d.free()

func _add_environment() -> void:
	var env := WorldEnvironment.new()
	var e := Environment.new()
	e.background_mode = Environment.BG_COLOR
	e.background_color = Color("242730")
	e.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	e.ambient_light_color = Color(0.22, 0.22, 0.24)
	e.ambient_light_energy = 1.0
	e.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	e.tonemap_exposure = 1.15
	env.environment = e
	_vp.add_child(env)

	var key := DirectionalLight3D.new()
	key.rotation_degrees = Vector3(-55, -38, 0)
	key.light_energy = 2.2
	key.shadow_enabled = true
	_vp.add_child(key)
	var fill := DirectionalLight3D.new()
	fill.rotation_degrees = Vector3(-25, 55, 0)
	fill.light_energy = 0.55
	_vp.add_child(fill)

func _add_clip(player: AnimationPlayer, library_name: String, clip_name: String, path: String) -> void:
	if player.has_animation(clip_name):
		return
	var anim := load(path) as Animation
	assert(anim != null)
	var lib := AnimationLibrary.new()
	lib.add_animation(clip_name, anim)
	player.add_animation_library(library_name, lib)

func _play_suffix(player: AnimationPlayer, suffix: String, time: float) -> void:
	for anim in player.get_animation_list():
		if String(anim) == suffix or String(anim).ends_with("/" + suffix):
			player.play(anim, 0)
			player.seek(time, true)
			player.advance(0)
			return
	push_error("Missing animation suffix: " + suffix)

func _find_ap(n: Node) -> AnimationPlayer:
	if n is AnimationPlayer:
		return n
	for c in n.get_children():
		var r := _find_ap(c)
		if r != null:
			return r
	return null
