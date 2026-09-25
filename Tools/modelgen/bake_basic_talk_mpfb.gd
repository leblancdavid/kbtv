extends SceneTree
## Conservative MPFB `talk_calm` bake for Vern.
##
## Purpose: create a visually safe first talking loop after the runtime
## front/left/right contract is proven. This intentionally avoids broad arm
## gestures: legs/pelvis stay planted, hands stay near the tray/rest pose, and
## speech reads through jaw scale, restrained head motion, subtle shoulder/chest
## motion, and tiny wrist/finger life.
##
## Run with Godot 4.6 mono:
##   Godot_v4.6-headless --path . --script res://Tools/modelgen/bake_basic_talk_mpfb.gd
## Then run the existing arm-front post-pass so the seated-rest wrists are moved
## from the raw MPFB back side onto Vern-local front:
##   Godot_v4.6-headless --path . --script res://Tools/modelgen/fix_vern_mpfb_arm_front.gd

const MPFB_GLB := "res://assets/models3d/characters/vern_mpfb/vern_mpfb_fitted.glb"
const OUT_PATH := "res://assets/models3d/characters/vern/animations/talk_calm_mpfb.tres"
const SKEL_PATH := "Vern_MPFB_StandardRig/Skeleton3D"
const FPS := 24
const LENGTH := 3.0

const TYPE_POS := 1
const TYPE_ROT := 2
const TYPE_SCALE := 3

const TALK_BONES := [
	"spine01", "spine02", "neck01", "neck02", "head", "jaw",
	"upperarm01.L", "upperarm01.R", "lowerarm01.L", "lowerarm01.R",
	"wrist.L", "wrist.R",
]

func _initialize() -> void:
	var packed := load(MPFB_GLB) as PackedScene
	if packed == null:
		push_error("missing MPFB GLB")
		quit(1)
		return
	var root_node := packed.instantiate()
	root.add_child(root_node)
	var skel := _find_skel(root_node)
	var player := _find_ap(root_node)
	if skel == null or player == null:
		push_error("missing MPFB skeleton/player")
		quit(1)
		return

	var seat_rot := {}
	var seat_scale := {}
	for i in skel.get_bone_count():
		var bone := String(skel.get_bone_name(i))
		seat_rot[bone] = Quaternion(skel.get_bone_rest(i).basis)
		seat_scale[bone] = skel.get_bone_rest(i).basis.get_scale()
	var root_origin: Vector3 = skel.get_bone_rest(skel.find_bone("root")).origin
	var seat := player.get_animation("seated_rest")
	if seat != null:
		for ti in seat.get_track_count():
			var bone := String(seat.track_get_path(ti)).get_slice(":", 1)
			var type := int(seat.track_get_type(ti))
			if type == TYPE_ROT and seat.track_get_key_count(ti) > 0:
				seat_rot[bone] = seat.track_get_key_value(ti, 0) as Quaternion
			elif bone == "root" and type == TYPE_POS and seat.track_get_key_count(ti) > 0:
				root_origin = seat.track_get_key_value(ti, 0) as Vector3

	var out := Animation.new()
	out.length = LENGTH
	out.step = 1.0 / FPS
	out.loop_mode = Animation.LOOP_LINEAR
	var tracks := {}
	for i in skel.get_bone_count():
		var bone := String(skel.get_bone_name(i))
		if bone == "root":
			continue
		var track := out.add_track(TYPE_ROT)
		out.track_set_path(track, NodePath(SKEL_PATH + ":" + bone))
		out.track_set_interpolation_type(track, Animation.INTERPOLATION_LINEAR)
		tracks[bone] = track

	var frames := int(roundi(LENGTH * FPS)) + 1
	for frame in range(frames):
		var t := minf(frame / float(FPS), LENGTH)
		var u := t / LENGTH
		for bone in tracks:
			var q: Quaternion = seat_rot[bone]
			q = _talk_rot(bone, q, u)
			out.track_insert_key(tracks[bone], t, q)

	var pos_track := out.add_track(TYPE_POS)
	out.track_set_path(pos_track, NodePath(SKEL_PATH + ":root"))
	out.track_set_interpolation_type(pos_track, Animation.INTERPOLATION_LINEAR)
	out.track_insert_key(pos_track, 0.0, root_origin)
	out.track_insert_key(pos_track, LENGTH, root_origin)

	var jaw_scale_track := out.add_track(TYPE_SCALE)
	out.track_set_path(jaw_scale_track, NodePath(SKEL_PATH + ":jaw"))
	out.track_set_interpolation_type(jaw_scale_track, Animation.INTERPOLATION_LINEAR)
	for frame in range(frames):
		var t := minf(frame / float(FPS), LENGTH)
		var u := t / LENGTH
		var open := maxf(0.0, sin(TAU * 5.0 * u)) * 0.055 + maxf(0.0, sin(TAU * 8.0 * u + 0.7)) * 0.025
		out.track_insert_key(jaw_scale_track, t, Vector3(1.0, 1.0 - open, 1.0 + open * 0.55))

	# Preserve non-unit rest scale on sensitive planted/face bones, except jaw where
	# the speech pulse owns the scale track.
	for bone in ["foot.L", "foot.R", "pelvis.L", "pelvis.R"]:
		var rest_scale: Vector3 = seat_scale[bone]
		if rest_scale.distance_to(Vector3.ONE) <= 1.0e-6:
			continue
		var scale_track := out.add_track(TYPE_SCALE)
		out.track_set_path(scale_track, NodePath(SKEL_PATH + ":" + bone))
		out.track_set_interpolation_type(scale_track, Animation.INTERPOLATION_LINEAR)
		out.track_insert_key(scale_track, 0.0, rest_scale)
		out.track_insert_key(scale_track, LENGTH, rest_scale)

	# Exact loop seam.
	for ti in out.get_track_count():
		var count := out.track_get_key_count(ti)
		if count >= 2:
			out.track_set_key_value(ti, count - 1, out.track_get_key_value(ti, 0))

	var err := ResourceSaver.save(out, OUT_PATH)
	print("BASIC_TALK_MPFB save=%s length=%.2f tracks=%d talk_bones=%s" % [err, out.length, out.get_track_count(), ",".join(TALK_BONES)])
	quit(0 if err == OK else 1)

func _talk_rot(bone: String, base: Quaternion, u: float) -> Quaternion:
	var breathe := sin(TAU * u)
	var phrase := sin(TAU * 2.0 * u + 0.4)
	var listen := sin(TAU * 0.5 * u + 0.15)
	match bone:
		"spine01":
			return base * _euler_q(Vector3(deg_to_rad(0.7 * breathe), deg_to_rad(0.6 * listen), deg_to_rad(0.35 * phrase)))
		"spine02":
			return base * _euler_q(Vector3(deg_to_rad(0.45 * breathe), deg_to_rad(0.35 * listen), deg_to_rad(0.25 * phrase)))
		"neck01":
			return base * _euler_q(Vector3(deg_to_rad(0.8 * phrase), deg_to_rad(1.2 * listen), deg_to_rad(0.3 * breathe)))
		"neck02":
			return base * _euler_q(Vector3(deg_to_rad(0.45 * phrase), deg_to_rad(0.7 * listen), 0.0))
		"head":
			return base * _euler_q(Vector3(deg_to_rad(1.0 * phrase), deg_to_rad(1.6 * listen), deg_to_rad(0.45 * breathe)))
		"upperarm01.L":
			return base * _euler_q(Vector3(deg_to_rad(0.35 * breathe), 0.0, deg_to_rad(-0.35 * phrase)))
		"upperarm01.R":
			return base * _euler_q(Vector3(deg_to_rad(0.35 * breathe), 0.0, deg_to_rad(0.35 * phrase)))
		"lowerarm01.L":
			return base * _euler_q(Vector3(deg_to_rad(0.55 * phrase), deg_to_rad(-0.35 * breathe), 0.0))
		"lowerarm01.R":
			return base * _euler_q(Vector3(deg_to_rad(-0.55 * phrase), deg_to_rad(0.35 * breathe), 0.0))
		"wrist.L":
			return base * _euler_q(Vector3(deg_to_rad(0.9 * phrase), deg_to_rad(-0.55 * breathe), deg_to_rad(0.6 * listen)))
		"wrist.R":
			return base * _euler_q(Vector3(deg_to_rad(-0.9 * phrase), deg_to_rad(0.55 * breathe), deg_to_rad(-0.6 * listen)))
		"jaw":
			return base
		_:
			return base

func _euler_q(v: Vector3) -> Quaternion:
	return Basis.from_euler(v).get_rotation_quaternion()

func _find_ap(n: Node) -> AnimationPlayer:
	if n is AnimationPlayer:
		return n
	for c in n.get_children():
		var r := _find_ap(c)
		if r != null:
			return r
	return null

func _find_skel(n: Node) -> Skeleton3D:
	if n is Skeleton3D:
		return n
	for c in n.get_children():
		var r := _find_skel(c)
		if r != null:
			return r
	return null
