extends SceneTree
## Renders runtime Vern performing the baked talk_calm MPFB clip.
## Requires a GRAPHICAL Godot 4.6 mono run (NOT --headless):
##   <console.exe> --path . --script Tools/modelgen/preview_mpfb_calm.gd
## Emits PNG frames to OUT then packs them with pack_mpfb_calm.py.
## Renders into an offscreen SubViewport for reliable per-frame get_image().
##
## Important: this instantiates scenes/world3d/Vern.tscn under the yaw-180
## VernStation transform used by World3D.tscn. Earlier versions loaded the raw
## GLB directly, which previewed the wrong orientation for runtime review.

const VERN_SCENE := "res://scenes/world3d/Vern.tscn"
const OUT := "C:/Users/lblan/AppData/Local/Temp/opencode/vern_mpfb_calm_frames"
const FEED_SIZE := Vector2i(640, 360)

var _vp: SubViewport
var _player: AnimationPlayer

func _initialize() -> void:
	call_deferred("capture")

func capture() -> void:
	root.size = FEED_SIZE
	DirAccess.make_dir_recursive_absolute(OUT)
	for old in DirAccess.get_files_at(OUT):
		DirAccess.remove_absolute(OUT + "/" + old)

	_vp = SubViewport.new()
	_vp.size = FEED_SIZE
	_vp.own_world_3d = true
	_vp.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	_vp.transparent_bg = false
	root.add_child(_vp)

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
	var rim := DirectionalLight3D.new()
	rim.rotation_degrees = Vector3(-15, 165, 0)
	rim.light_energy = 0.4
	_vp.add_child(rim)

	var station := Node3D.new()
	station.name = "RuntimeVernStation"
	station.transform = Transform3D(Basis(Vector3.UP, PI), Vector3(-0.85, 0.1, -0.05))
	_vp.add_child(station)

	var vern := (load(VERN_SCENE) as PackedScene).instantiate() as Node3D
	station.add_child(vern)
	_player = _find_ap(vern)
	assert(_player != null)
	_player.callback_mode_process = AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_MANUAL

	for i in range(6):
		await process_frame
	await RenderingServer.frame_post_draw

	var talk := _resolve(_player, "talk_calm")
	assert(not talk.is_empty())
	_player.play(talk, 0)
	var look := station.to_global(Vector3(0, 0.95, 0))
	await _capture_with("feed", Vector3(-2.15, 1.55, 2.3), look + Vector3(0, -0.1, 0.5), 38)
	await _capture_with("front", look + Vector3(-0.25, 0.2, 2.0), look, 40)
	print("MPFB_CALM_FRAMES " + OUT)
	quit()

func _capture_with(tag: String, pos: Vector3, look: Vector3, fov: float) -> void:
	var cam := Camera3D.new()
	_vp.add_child(cam)
	cam.position = pos
	cam.look_at(look, Vector3.UP)
	cam.fov = fov
	cam.current = true
	var talk := _resolve(_player, "talk_calm")
	var length: float = _player.get_animation(talk).length
	print("LEN_BEING_PLAYED %f" % length)
	var total := roundi(length * 12) + 1
	for frame in range(total):
		_player.seek(frame / 12.0, true)
		await process_frame
		await RenderingServer.frame_post_draw
		var img := _vp.get_texture().get_image()
		if img == null:
			print("FRAME_NULL %s %03d" % [tag, frame])
			continue
		print("FRAME_OK %s %03d %dx%d" % [tag, frame, img.get_width(), img.get_height()])
		img.save_png(OUT + "/talk_calm_mpfb_%s_%03d.png" % [tag, frame])
	_vp.remove_child(cam)
	cam.free()

func _resolve(player: AnimationPlayer, suffix: String) -> String:
	for anim in player.get_animation_list():
		var name := String(anim)
		if name == suffix or name.ends_with("/" + suffix):
			return name
	return ""

func _find_ap(n: Node) -> AnimationPlayer:
	if n is AnimationPlayer:
		return n
	for c in n.get_children():
		var r := _find_ap(c)
		if r != null:
			return r
	return null
