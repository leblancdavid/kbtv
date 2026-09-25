extends SceneTree
## Runtime Vern MPFB side/orientation diagnostic.
##
## This instantiates the real scenes/world3d/Vern.tscn under the same yaw-180
## VernStation transform used in World3D.tscn, then samples key bones in Vern-local
## space. Use this before rebaking/approving animations so left/right/front claims
## are based on the actual runtime chain, not raw GLB screenshots.
##
## Run with Godot 4.6 mono:
##   Godot_v4.6-console.exe --path . --script Tools/modelgen/diagnose_vern_runtime_sides.gd

const VERN_SCENE := "res://scenes/world3d/Vern.tscn"
const CONTACTS := "res://assets/models3d/characters/vern/animation_contacts.json"
const OUT := "user://vern_runtime_sides.json"

const SAMPLES := [
	{ "clip": "seated_rest", "time": 0.0 },
	{ "clip": "idle_breathing", "time": 1.0 },
	{ "clip": "talk_calm", "time": 1.0 },
	{ "clip": "talk_calm", "time": 2.0 },
	{ "clip": "talking_default", "time": 2.5 },
	{ "clip": "smoking", "time": 2.5 },
	{ "clip": "drink_coffee", "time": 2.5 },
]

func _initialize() -> void:
	call_deferred("_run")

func _run() -> void:
	var packed := load(VERN_SCENE) as PackedScene
	if packed == null:
		push_error("missing Vern scene")
		quit(1)
		return

	var station := Node3D.new()
	station.name = "RuntimeVernStation"
	station.transform = Transform3D(Basis(Vector3.UP, PI), Vector3(-0.85, 0.1, -0.05))
	root.add_child(station)

	var vern := packed.instantiate() as Node3D
	station.add_child(vern)
	for i in range(6):
		await process_frame

	var player := _find_ap(vern)
	var skel := _find_skel(vern)
	if player == null or skel == null:
		push_error("Vern runtime chain is missing AnimationPlayer or Skeleton3D")
		quit(1)
		return
	player.callback_mode_process = AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_MANUAL
	player.speed_scale = 0.0

	var contacts: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(CONTACTS))
	var mouth_marker: Transform3D = _tf(contacts["mouth_marker"]["head_local"])
	var report := []
	var failed := false
	for sample in SAMPLES:
		var clip := String(sample.clip)
		var time := float(sample.time)
		var resolved := _resolve(player, clip)
		if resolved.is_empty():
			push_warning("missing animation suffix: " + clip)
			continue
		player.play(resolved, 0.0)
		player.seek(time, true)
		player.advance(0.0)
		await process_frame

		var row := _measure(vern, skel, mouth_marker, clip, time)
		report.append(row)
		failed = failed or not bool(row["ok"])
		print("VERN_SIDE %s@%.2f ok=%s mouth_front=%.4f shoulders L/R %.4f %.4f wrists L/R %.4f %.4f wrist_z L/R %.4f %.4f" % [
			clip,
			time,
			str(row["ok"]),
			float(row["mouth_front_delta_z"]),
			float(row["shoulder_l_x"]),
			float(row["shoulder_r_x"]),
			float(row["wrist_l_x"]),
			float(row["wrist_r_x"]),
			float(row["wrist_l_z"]),
			float(row["wrist_r_z"]),
		])

	var file := FileAccess.open(OUT, FileAccess.WRITE)
	file.store_string(JSON.stringify(report, "  "))
	file.close()
	print("VERN_RUNTIME_SIDES " + ProjectSettings.globalize_path(OUT))
	quit(1 if failed else 0)

func _measure(vern: Node3D, skel: Skeleton3D, mouth_marker: Transform3D, clip: String, time: float) -> Dictionary:
	var head := _bone_local(vern, skel, "head")
	var mouth := head * mouth_marker
	var shoulder_l := _bone_local(vern, skel, "upperarm01.L")
	var shoulder_r := _bone_local(vern, skel, "upperarm01.R")
	var wrist_l := _bone_local(vern, skel, "wrist.L")
	var wrist_r := _bone_local(vern, skel, "wrist.R")
	var shoulder_ok := shoulder_l.origin.x < shoulder_r.origin.x
	var wrist_ok := wrist_l.origin.x < wrist_r.origin.x
	var mouth_front_delta_z := mouth.origin.z - head.origin.z
	var mouth_front_ok := mouth_front_delta_z < 0.0
	return {
		"clip": clip,
		"time": time,
		"ok": shoulder_ok and wrist_ok and mouth_front_ok,
		"shoulder_lr_ok": shoulder_ok,
		"wrist_lr_ok": wrist_ok,
		"mouth_front_ok": mouth_front_ok,
		"mouth_front_delta_z": mouth_front_delta_z,
		"shoulder_l_x": shoulder_l.origin.x,
		"shoulder_r_x": shoulder_r.origin.x,
		"wrist_l_x": wrist_l.origin.x,
		"wrist_r_x": wrist_r.origin.x,
		"wrist_l_z": wrist_l.origin.z,
		"wrist_r_z": wrist_r.origin.z,
	}

func _bone_local(vern: Node3D, skel: Skeleton3D, bone: String) -> Transform3D:
	var idx := skel.find_bone(bone)
	assert(idx >= 0, "missing bone " + bone)
	return vern.global_transform.affine_inverse() * skel.global_transform * skel.get_bone_global_pose(idx)

func _tf(value: Dictionary) -> Transform3D:
	var p: Array = value["position"]
	var q: Array = value["rotation_quaternion_xyzw"]
	return Transform3D(Basis(Quaternion(q[0], q[1], q[2], q[3])), Vector3(p[0], p[1], p[2]))

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

func _find_skel(n: Node) -> Skeleton3D:
	if n is Skeleton3D:
		return n
	for c in n.get_children():
		var r := _find_skel(c)
		if r != null:
			return r
	return null
