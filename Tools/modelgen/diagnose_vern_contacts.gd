extends SceneTree
## Measures Vern runtime prop contacts in the real Vern.tscn transform chain.
## Run with Godot 4.6 mono: --path . --script Tools/modelgen/diagnose_vern_contacts.gd

const VERN_SCENE := "res://scenes/world3d/Vern.tscn"
const CONTACTS := "res://assets/models3d/characters/vern/animation_contacts.json"
const MODEL_YAW := PI
const OUT := "user://vern_contact_diagnostics.json"

const SAMPLES := [
	{ "clip": "smoking", "time": 1.1 },
	{ "clip": "smoking", "time": 2.3 },
	{ "clip": "smoking", "time": 2.9 },
	{ "clip": "smoking", "time": 4.6 },
	{ "clip": "drink_coffee", "time": 1.1 },
	{ "clip": "drink_coffee", "time": 2.4 },
	{ "clip": "drink_coffee", "time": 3.0 },
	{ "clip": "drink_coffee", "time": 4.6 },
]

func _initialize() -> void:
	call_deferred("_run")

func _run() -> void:
	var contacts: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(CONTACTS))
	var markers: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://assets/models3d/characters/vern/prop_contact_markers.json"))
	var failures := 0
	var vern := (load(VERN_SCENE) as PackedScene).instantiate() as Node3D
	root.add_child(vern)
	for i in range(6):
		await process_frame

	var player := _find_ap(vern)
	var skel := _find_skel(vern)
	if player == null or skel == null:
		push_error("missing AnimationPlayer or Skeleton3D")
		quit(1)
		return
	player.callback_mode_process = AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_MANUAL
	player.speed_scale = 0.0

	var mouth_marker: Transform3D = _tf(contacts["mouth_marker"]["head_local"])
	var rows := []
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

		var action: Dictionary = contacts["actions"][clip]
		var prop_name := String(action["prop"])
		var prop_rest := _tf(contacts["props"][prop_name]["rest"])
		var grip := _tf(action["hand_local_grip"])
		var hand := String(action["hand_bone"])
		var hand_pose := _bone_local(vern, skel, hand)
		var prop_pose := hand_pose * grip
		var mouth := _bone_local(vern, skel, "head") * mouth_marker
		var point: Array = markers[prop_name]["lip_point"]
		var contact_point := prop_pose * Vector3(point[0], point[1], point[2])
		var lip_error := contact_point.distance_to(mouth.origin)
		var first := "finger2-2.R" if clip == "smoking" else "finger1-3.L"
		var second := "finger3-2.R" if clip == "smoking" else "finger2-3.L"
		var fingers := (_bone_local(vern, skel, first).origin + _bone_local(vern, skel, second).origin) * 0.5
		var grasp_point := Vector3(0, 0, -0.035) if clip == "smoking" else Vector3(0.058, 0.084, 0)
		var grip_error := fingers.distance_to(prop_pose * grasp_point)
		var interval: Array = action["contact_seconds"]
		var at_lips := time >= float(interval[0]) and time <= float(interval[1])
		if at_lips and lip_error > 0.01:
			failures += 1
		if at_lips and grip_error > 0.015:
			failures += 1
		print("FINGER_GRIP %s@%.2f center_error_m=%.5f" % [clip, time, grip_error])
		var at_support := is_equal_approx(time, float(action["pickup_seconds"])) or is_equal_approx(time, float(action["release_seconds"]))
		var support_error := prop_pose.origin.distance_to(prop_rest.origin)
		if at_support and support_error > 0.01:
			failures += 1
		print("GRASP_SUPPORT %s@%.2f error_m=%.5f required=%s" % [clip, time, support_error, at_support])
		print("LIP_CONTACT %s@%.2f error_m=%.5f required=%s" % [clip, time, lip_error, at_lips])
		var row := {
			"clip": clip,
			"time": time,
			"hand_bone": hand,
			"hand": _v(hand_pose.origin),
			"prop": prop_name,
			"prop_rest": _v(prop_rest.origin),
			"prop_held": _v(prop_pose.origin),
			"mouth": _v(mouth.origin),
			"lip_marker_error_m": lip_error,
			"finger_grip_center_error_m": grip_error,
			"lip_contact_required": at_lips,
			"rest_to_hand_m": prop_rest.origin.distance_to(hand_pose.origin),
			"held_to_mouth_m": prop_pose.origin.distance_to(mouth.origin),
			"held_z_minus_mouth_z": prop_pose.origin.z - mouth.origin.z,
			"held_y_minus_mouth_y": prop_pose.origin.y - mouth.origin.y,
		}
		rows.append(row)
		print("CONTACT %s@%.2f %s held=(%.3f,%.3f,%.3f) mouth=(%.3f,%.3f,%.3f) d=%.3f" % [
			clip, time, prop_name, prop_pose.origin.x, prop_pose.origin.y, prop_pose.origin.z,
			mouth.origin.x, mouth.origin.y, mouth.origin.z, prop_pose.origin.distance_to(mouth.origin)
		])

	var file := FileAccess.open(OUT, FileAccess.WRITE)
	file.store_string(JSON.stringify(rows, "  "))
	file.close()
	print("VERN_CONTACT_DIAGNOSTICS " + ProjectSettings.globalize_path(OUT))
	quit(1 if failures > 0 else 0)

func _bone_local(vern: Node3D, skel: Skeleton3D, bone: String) -> Transform3D:
	var idx := skel.find_bone(bone)
	assert(idx >= 0, "missing bone " + bone)
	return vern.global_transform.affine_inverse() * skel.global_transform * skel.get_bone_global_pose(idx)

func _tf(value: Dictionary) -> Transform3D:
	var p: Array = value["position"]
	var q: Array = value["rotation_quaternion_xyzw"]
	return Transform3D(Basis(Quaternion(q[0], q[1], q[2], q[3])), Vector3(p[0], p[1], p[2]))

func _v(value: Vector3) -> Array:
	return [snappedf(value.x, 0.0001), snappedf(value.y, 0.0001), snappedf(value.z, 0.0001)]

func _resolve(player: AnimationPlayer, suffix: String) -> String:
	for anim in player.get_animation_list():
		var name := String(anim)
		if name == suffix or name.ends_with("/" + suffix):
			return name
	return ""

func _find_ap(n: Node) -> AnimationPlayer:
	if n is AnimationPlayer: return n
	for c in n.get_children():
		var r := _find_ap(c)
		if r != null: return r
	return null

func _find_skel(n: Node) -> Skeleton3D:
	if n is Skeleton3D: return n
	for c in n.get_children():
		var r := _find_skel(c)
		if r != null: return r
	return null
