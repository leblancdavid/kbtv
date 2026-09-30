extends SceneTree
## Re-author MPFB idle/smoke/coffee arms from explicit front-of-body wrist targets.
## This runs after rebake_vern_clips.gd. Unlike the old retarget, it solves the
## active wrist to a table -> mouth -> table prop path using the runtime contract.

const MPFB_GLB := "res://assets/models3d/characters/vern_mpfb/vern_mpfb_fitted.glb"
const CONTACTS := "res://assets/models3d/characters/vern/animation_contacts.json"
const MARKERS := "res://assets/models3d/characters/vern/prop_contact_markers.json"
const CLIP_DIR := "res://assets/models3d/characters/vern/animations"
const SKEL_PATH := "Vern_MPFB_StandardRig/Skeleton3D"
const MODEL_YAW := PI
const FPS := 24
const ACTION_FPS := 48
const TYPE_ROT := 2
const IDLE_WRIST_POS := {
	# office_chair.py: pads centered x=+/-0.28, top y=0.6975,
	# depth -0.165..0.135 in Godot. Chair and Vern share their origin.
	"L": Vector3(-0.28, 0.735, -0.12),
	"R": Vector3(0.28, 0.735, -0.12),
}
const ELBOW_POLE_LOCAL := {
	"L": Vector3(-0.34, 0.65, 0.02),
	"R": Vector3(0.34, 0.65, 0.02),
}

const ARM_BONES := [
	"upperarm01.L", "upperarm02.L", "lowerarm01.L", "lowerarm02.L", "wrist.L",
	"upperarm01.R", "upperarm02.R", "lowerarm01.R", "lowerarm02.R", "wrist.R",
	"finger1-1.L", "finger1-2.L", "finger1-3.L", "finger2-1.L", "finger2-2.L", "finger2-3.L",
	"finger3-1.L", "finger3-2.L", "finger3-3.L", "finger4-1.L", "finger4-2.L", "finger4-3.L",
	"finger5-1.L", "finger5-2.L", "finger5-3.L",
	"finger1-1.R", "finger1-2.R", "finger1-3.R", "finger2-1.R", "finger2-2.R", "finger2-3.R",
	"finger3-1.R", "finger3-2.R", "finger3-3.R", "finger4-1.R", "finger4-2.R", "finger4-3.R",
	"finger5-1.R", "finger5-2.R", "finger5-3.R",
]

const CASES := {
	"smoking": { "side": "R", "a": 2.3, "b": 2.9 },
	"drink_coffee": { "side": "L", "a": 2.4, "b": 3.0 },
}

func _initialize() -> void:
	call_deferred("_run")

func _run() -> void:
	var contacts: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(CONTACTS))
	var packed := load(MPFB_GLB) as PackedScene
	var failed := 0
	for clip in ["idle_breathing", "talk_calm", "talking_default"]:
		if not await _apply_idle_guide(packed, clip):
			failed += 1
	for clip in CASES.keys():
		if not await _author_clip(packed, contacts, clip):
			failed += 1
	quit(failed)

func _apply_idle_guide(packed: PackedScene, clip: String) -> bool:
	var idle := load("%s/%s_mpfb.tres" % [CLIP_DIR, clip]) as Animation
	if idle == null:
		push_error("missing idle")
		return false
	var imported := packed.instantiate()
	var vern := Node3D.new()
	var model := Node3D.new()
	model.transform = Transform3D(Basis(Vector3.UP, MODEL_YAW), Vector3.ZERO)
	model.add_child(imported)
	vern.add_child(model)
	root.add_child(vern)
	var player := _find_ap(imported)
	player.callback_mode_process = AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_MANUAL
	var skel := _find_skel(imported)
	(player.get_animation_library(player.get_animation_library_list()[0]) as AnimationLibrary).add_animation("__idle", idle)
	var out := idle.duplicate(true) as Animation
	var tracks := {}
	for i in range(out.get_track_count() - 1, -1, -1):
		if int(out.track_get_type(i)) == TYPE_ROT and ARM_BONES.has(String(out.track_get_path(i)).get_slice(":", 1)):
			out.remove_track(i)
	for bone in ARM_BONES:
		var track := out.add_track(TYPE_ROT)
		out.track_set_path(track, NodePath(SKEL_PATH + ":" + bone))
		tracks[bone] = track
	for frame in range(ceili(idle.length * FPS) + 1):
		var time := minf(frame / float(FPS), idle.length)
		skel.reset_bone_poses()
		player.play("__idle", 0)
		player.seek(time, true)
		player.advance(0)
		_apply_front_guide(vern, skel)
		_relax_fingers(skel)
		for bone in ARM_BONES:
			out.track_insert_key(tracks[bone], time, skel.get_bone_pose_rotation(skel.find_bone(bone)))
	var err := ResourceSaver.save(out, "%s/%s_mpfb.tres" % [CLIP_DIR, clip])
	print("APPLY_PAD_GUIDE %s save=%s" % [clip, err])
	vern.queue_free()
	return err == OK

func _author_clip(packed: PackedScene, contacts: Dictionary, clip: String) -> bool:
	var source := load("%s/%s_mpfb.tres" % [CLIP_DIR, clip]) as Animation
	if source == null:
		push_error("missing " + clip)
		return false
	var vern := Node3D.new()
	var model := Node3D.new()
	model.transform = Transform3D(Basis(Vector3.UP, MODEL_YAW), Vector3.ZERO)
	var imported := packed.instantiate()
	model.add_child(imported)
	vern.add_child(model)
	root.add_child(vern)
	var player := _find_ap(imported)
	player.callback_mode_process = AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_MANUAL
	var skel := _find_skel(imported)
	(player.get_animation_library(player.get_animation_library_list()[0]) as AnimationLibrary).add_animation("__src", source)
	player.play("__src")
	player.seek(1.0, true)
	player.advance(0.0)
	await process_frame
	_apply_front_guide(vern, skel)
	_relax_fingers(skel)
	var guide_rot := _guide_rotations(skel)
	var guide_tf := _guide_transforms(vern, skel)

	var out := source.duplicate(true) as Animation
	var side := String(CASES[clip]["side"])
	for i in range(out.get_track_count() - 1, -1, -1):
		if int(out.track_get_type(i)) == TYPE_ROT and ARM_BONES.has(String(out.track_get_path(i)).get_slice(":", 1)):
			out.remove_track(i)
	var tracks := {}
	for bone in ARM_BONES:
		var track := out.add_track(TYPE_ROT)
		out.track_set_path(track, NodePath(SKEL_PATH + ":" + bone))
		out.track_set_interpolation_type(track, Animation.INTERPOLATION_LINEAR)
		tracks[bone] = track

	var action: Dictionary = contacts["actions"][clip]
	var clavicle_name := "clavicle." + side
	for i in range(out.get_track_count() - 1, -1, -1):
		if out.track_get_type(i) == Animation.TYPE_ROTATION_3D and String(out.track_get_path(i)).get_slice(":", 1) in ["spine01", clavicle_name]:
			out.remove_track(i)
	for bone in ARM_BONES:
		tracks[bone] = out.find_track(NodePath(SKEL_PATH + ":" + bone), Animation.TYPE_ROTATION_3D)
	var spine_track := out.add_track(Animation.TYPE_ROTATION_3D)
	out.track_set_path(spine_track, NodePath(SKEL_PATH + ":spine01"))
	var clavicle_track := out.add_track(Animation.TYPE_ROTATION_3D)
	out.track_set_path(clavicle_track, NodePath(SKEL_PATH + ":" + clavicle_name))
	var prop := String(action["prop"])
	var rest := _tf(contacts["props"][prop]["rest"])
	var grip := _tf(action["hand_local_grip"])
	var mouth_marker := _tf(contacts["mouth_marker"]["head_local"])
	var pickup := float(action["pickup_seconds"])
	var release := float(action["release_seconds"])
	var contact_a := float(CASES[clip]["a"])
	var contact_b := float(CASES[clip]["b"])
	var markers: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(MARKERS))
	var marker: Dictionary = markers[prop]
	var point_values: Array = marker["lip_point"]
	var lip_point := Vector3(point_values[0], point_values[1], point_values[2])
	var angles: Array = marker["contact_rotation_degrees"]
	var contact_basis := Basis.from_euler(Vector3(deg_to_rad(angles[0]), deg_to_rad(angles[1]), deg_to_rad(angles[2])))
	var frames := int(ceil(source.length * ACTION_FPS))
	for f in range(frames + 1):
		var t := minf(f / float(ACTION_FPS), source.length)
		skel.reset_bone_poses()
		player.play("__src")
		player.seek(t, true)
		player.advance(0.0)
		await process_frame
		var reach_weight := _smooth((t - pickup + 0.8) / 0.55) * (1.0 - _smooth((t - pickup - 0.15) / 0.65))
		reach_weight = maxf(reach_weight, _smooth((t - release + 0.8) / 0.55) * (1.0 - _smooth((t - release) / 0.65)))
		var spine := skel.find_bone("spine01")
		var spine_pose := skel.get_bone_global_pose(spine)
		var lean_axis := (skel.global_transform.basis.inverse() * vern.global_transform.basis * Vector3.RIGHT).normalized()
		var lean_angle := 0.45 if clip == "smoking" else 0.38
		spine_pose.basis = Basis(lean_axis, -lean_angle * reach_weight) * spine_pose.basis
		skel.set_bone_global_pose(spine, spine_pose)
		skel.force_update_all_bone_transforms()
		out.track_insert_key(spine_track, t, skel.get_bone_pose_rotation(spine))
		# Small shoulder protraction supplies the extra reach without a deeper
		# torso bow. Measured clavicle-to-shoulder span is about 16 cm.
		var clavicle := skel.find_bone(clavicle_name)
		var shoulder_pose := skel.get_bone_global_pose(clavicle)
		var up_axis := (skel.global_transform.basis.inverse() * vern.global_transform.basis * Vector3.UP).normalized()
		var advance := (-0.28 if side == "L" else 0.28) * reach_weight
		shoulder_pose.basis = Basis(up_axis, advance) * shoulder_pose.basis
		skel.set_bone_global_pose(clavicle, shoulder_pose)
		skel.force_update_all_bone_transforms()
		out.track_insert_key(clavicle_track, t, skel.get_bone_pose_rotation(clavicle))
		# Keep the inactive hand at its support as the torso leans independently.
		# Seed from ONE reference arm pose, not the legacy clip's animated roll.
		if clip == "smoking":
			_apply_guide_rotations(skel, guide_rot)
		_apply_front_guide(vern, skel)
		_relax_fingers(skel)
		var mouth := _bone_local(vern, skel, "head") * mouth_marker
		# Place the actual filter end/rim at the lip marker, not the prop origin.
		var contact := Transform3D(contact_basis, mouth.origin - contact_basis * lip_point)
		var prop_pose := _prop_path(rest, contact, pickup, contact_a, contact_b, release, t, clip == "smoking")
		var guide_wrist := guide_tf["wrist." + side] as Transform3D
		var prop_wrist := prop_pose * grip.affine_inverse()
		var wrist_vern := _wrist_path(guide_wrist, prop_wrist, pickup, release, source.length, t, clip == "smoking")
		var wrist_skel := skel.global_transform.affine_inverse() * vern.global_transform * wrist_vern
		var elbow_skel := skel.global_transform.affine_inverse() * vern.global_transform * Transform3D(Basis(), ELBOW_POLE_LOCAL[side])
		_solve_arm(skel, side, wrist_skel, elbow_skel.origin, clip != "smoking")
		var hold := _smooth((t - pickup + 0.25) / 0.25) * (1.0 - _smooth((t - release) / 0.25))
		for finger in range(2, 6):
			for segment in range(1, 4):
				var idx := skel.find_bone("finger%d-%d.%s" % [finger, segment, side])
				# Different knuckles bend by different amounts; avoid a rigid claw.
				var curls := [28.0, 58.0, 36.0]
				if clip == "smoking" and finger <= 3:
					curls = [10.0, 18.0, 12.0]
				var curl: float = curls[segment - 1] + (finger - 2) * 2.0
				var grasp := skel.get_bone_rest(idx).basis.get_rotation_quaternion() * Quaternion(Vector3.RIGHT, deg_to_rad(curl))
				skel.set_bone_pose_rotation(idx, skel.get_bone_pose_rotation(idx).slerp(grasp, hold))
		for segment in range(1, 4):
			var idx := skel.find_bone("finger1-%d.%s" % [segment, side])
			var curl := 22.0 if segment == 1 else 30.0
			var grasp := skel.get_bone_rest(idx).basis.get_rotation_quaternion() * Quaternion(Vector3.RIGHT, deg_to_rad(curl))
			skel.set_bone_pose_rotation(idx, skel.get_bone_pose_rotation(idx).slerp(grasp, hold))
		if clip == "smoking":
			# Close the lateral gap between index/middle pads around the shaft.
			# Native local curl alone cannot bring these fingers together.
			var wrist_pose := skel.get_bone_global_pose(skel.find_bone("wrist." + side))
			for finger in [2, 3]:
				var idx := skel.find_bone("finger%d-1.%s" % [finger, side])
				var pose := skel.get_bone_global_pose(idx)
				var angle := (-0.32 if finger == 2 else 0.32) * hold
				pose.basis = Basis(wrist_pose.basis.x.normalized(), angle) * pose.basis
				skel.set_bone_global_pose(idx, pose)
				skel.force_update_all_bone_transforms()
		for bone in ARM_BONES:
			out.track_insert_key(tracks[bone], t, skel.get_bone_pose_rotation(skel.find_bone(bone)))

	for ti in out.get_track_count():
		var count := out.track_get_key_count(ti)
		if count >= 2:
			out.track_set_key_value(ti, count - 1, out.track_get_key_value(ti, 0))
	var err := ResourceSaver.save(out, "%s/%s_mpfb.tres" % [CLIP_DIR, clip])
	print("AUTHOR_PROP_ACTION %s save=%s" % [clip, err])
	vern.queue_free()
	return err == OK

func _prop_path(rest: Transform3D, contact: Transform3D, pickup: float, a: float, b: float, release: float, t: float, staged_turn: bool = false) -> Transform3D:
	# Hold the prop still while fingers close, and set down before opening.
	pickup += 0.15
	release -= 0.15
	if t <= pickup or t >= release:
		return rest
	if t >= a and t <= b:
		return contact
	var u := _smooth((t - pickup) / maxf(0.001, a - pickup)) if t < a else 1.0 - _smooth((t - b) / maxf(0.001, release - b))
	# One uninterrupted arc: no zero-velocity stop at a middle waypoint.
	var pose := _lerp_tf(rest, contact, u)
	pose.origin += Vector3(0, 0.035, -0.025) * sin(PI * u)
	if staged_turn:
		# Leave the ashtray palm-down; roll sideways only in the clear middle
		# of the lift. Reverse finishes BEFORE lowering into the notch.
		var phase := (t - pickup) / maxf(0.001, a - pickup) if t < a else 1.0 - (t - b) / maxf(0.001, release - b)
		var turn := _smooth((phase - 0.18) / 0.70)
		pose.basis = rest.basis.slerp(contact.basis, turn)
	return pose

func _wrist_path(guide: Transform3D, prop_wrist: Transform3D, pickup: float, release: float, length: float, t: float, staged_turn: bool = false) -> Transform3D:
	release += 0.2 # Open fingers before withdrawing from the planted prop.
	var reach_start := maxf(0.0, pickup - 0.65)
	var return_end := minf(length, release + 0.65)
	if t < reach_start:
		return guide
	if t < pickup:
		var u := _smooth((t - reach_start) / maxf(0.001, pickup - reach_start - 0.15))
		var pose := _lerp_tf(guide, prop_wrist, u)
		pose.origin.y += sin(u * PI) * 0.06
		if staged_turn:
			# Pre-orient early in the reach, then approach/close without twisting.
			pose.basis = guide.basis.slerp(prop_wrist.basis, _smooth((t - reach_start) / 0.30))
		return pose
	if t <= release:
		return prop_wrist
	var phase := (t - release) / maxf(0.001, return_end - release)
	var u := _smooth(phase)
	var pose := _lerp_tf(prop_wrist, guide, u)
	if staged_turn:
		pose.basis = prop_wrist.basis.slerp(guide.basis, _smooth((phase - 0.20) / 0.80))
	return pose

func _replace_arm_tracks_with_guide(out: Animation, guide: Dictionary) -> void:
	for i in range(out.get_track_count() - 1, -1, -1):
		if int(out.track_get_type(i)) == TYPE_ROT and ARM_BONES.has(String(out.track_get_path(i)).get_slice(":", 1)):
			out.remove_track(i)
	for bone in ARM_BONES:
		var track := out.add_track(TYPE_ROT)
		out.track_set_path(track, NodePath(SKEL_PATH + ":" + bone))
		out.track_set_interpolation_type(track, Animation.INTERPOLATION_LINEAR)
		out.track_insert_key(track, 0.0, guide[bone])
		out.track_insert_key(track, out.length, guide[bone])

func _guide_rotations(skel: Skeleton3D) -> Dictionary:
	var guide := {}
	for bone in ARM_BONES:
		guide[bone] = skel.get_bone_pose_rotation(skel.find_bone(bone))
	return guide

func _guide_transforms(vern: Node3D, skel: Skeleton3D) -> Dictionary:
	var guide := {}
	for bone in ARM_BONES:
		guide[bone] = _bone_local(vern, skel, bone)
	return guide

func _apply_guide_rotations(skel: Skeleton3D, guide: Dictionary) -> void:
	for bone in ARM_BONES:
		skel.set_bone_pose_rotation(skel.find_bone(bone), guide[bone])
	skel.force_update_all_bone_transforms()

func _apply_front_guide(vern: Node3D, skel: Skeleton3D) -> void:
	for side in ["L", "R"]:
		var target_vern := _idle_wrist_target(side)
		var target_skel := skel.global_transform.affine_inverse() * vern.global_transform * target_vern
		var elbow_skel := skel.global_transform.affine_inverse() * vern.global_transform * Transform3D(Basis(), ELBOW_POLE_LOCAL[side])
		_solve_arm(skel, side, target_skel, elbow_skel.origin)

func _idle_wrist_target(side: String) -> Transform3D:
	# MPFB palm plane needs pronation about the finger axis as well as
	# pointing the fingers forward. Mirrored hands use opposite pronation.
	var basis := Basis(Vector3.RIGHT, -PI / 2.0) * Basis(Vector3.UP, -PI / 2.0 if side == "R" else PI / 2.0)
	return Transform3D(basis, IDLE_WRIST_POS[side])

func _relax_fingers(skel: Skeleton3D) -> void:
	for side in ["L", "R"]:
		for finger in ["finger2", "finger3", "finger4", "finger5"]:
			for seg in ["1", "2", "3"]:
				var bone := "%s-%s.%s" % [finger, seg, side]
				var idx := skel.find_bone(bone)
				if idx >= 0:
					var amount := 16.0 if seg == "1" else 24.0
					skel.set_bone_pose_rotation(idx, skel.get_bone_rest(idx).basis.get_rotation_quaternion() * Quaternion(Vector3(1, 0, 0), deg_to_rad(amount)))
		for seg in ["1", "2", "3"]:
			var thumb := "finger1-%s.%s" % [seg, side]
			var thumb_idx := skel.find_bone(thumb)
			if thumb_idx >= 0:
				skel.set_bone_pose_rotation(thumb_idx, skel.get_bone_rest(thumb_idx).basis.get_rotation_quaternion() * Quaternion(Vector3(1, 0, 0), deg_to_rad(10.0)))
	skel.force_update_all_bone_transforms()

func _solve_arm(skel: Skeleton3D, side: String, target: Transform3D, pole_hint: Variant = null, share_roll: bool = true) -> void:
	var shoulder_idx := skel.find_bone("upperarm01." + side)
	var elbow_idx := skel.find_bone("lowerarm01." + side)
	var wrist_idx := skel.find_bone("wrist." + side)
	var shoulder := skel.get_bone_global_pose(shoulder_idx).origin
	var elbow0 := skel.get_bone_global_pose(elbow_idx).origin
	var wrist0 := skel.get_bone_global_pose(wrist_idx).origin
	var a := shoulder.distance_to(elbow0)
	var b := elbow0.distance_to(wrist0)
	var delta := target.origin - shoulder
	var d := clampf(delta.length(), absf(a - b) + 0.001, a + b - 0.001)
	var axis := delta.normalized()
	var pole := (pole_hint as Vector3) - shoulder if pole_hint is Vector3 else elbow0 - shoulder
	pole = (pole - axis * pole.dot(axis)).normalized()
	var along := (a * a - b * b + d * d) / (2.0 * d)
	var elbow := shoulder + axis * along + pole * sqrt(maxf(0.0, a * a - along * along))
	_aim_bone(skel, "upperarm01." + side, elbow)
	_aim_bone(skel, "upperarm02." + side, elbow)
	_aim_bone(skel, "lowerarm01." + side, target.origin)
	_aim_bone(skel, "lowerarm02." + side, target.origin)
	# Share pronation along the forearm instead of forcing the entire turn
	# into the wrist joint. Rolling about the shaft preserves the reached point.
	# Smoking already carries the continuous roll in its staged wrist path.
	# Projecting that sideways palm onto the forearm shaft crosses a singularity
	# and flips the inferred roll by 180 degrees during the lift.
	for bone in (["lowerarm01.", "lowerarm02."] if share_roll else []):
		var idx := skel.find_bone(bone + side)
		var pose := skel.get_bone_global_pose(idx)
		var shaft := pose.basis.y.normalized()
		var desired := target.basis.x - shaft * target.basis.x.dot(shaft)
		var current := pose.basis.x - shaft * pose.basis.x.dot(shaft)
		if desired.length_squared() > 0.001 and current.length_squared() > 0.001:
			var roll := current.normalized().signed_angle_to(desired.normalized(), shaft)
			pose.basis = Basis(shaft, clampf(roll, -1.4, 1.4) * 0.5) * pose.basis
			skel.set_bone_global_pose(idx, pose)
			skel.force_update_all_bone_transforms()
	# Rotate the wrist without teleporting it to an unreachable target. Only
	# rotations are baked; artificial wrist translations poisoned later solves.
	var achieved := skel.get_bone_global_pose(wrist_idx)
	skel.set_bone_global_pose(wrist_idx, Transform3D(target.basis, achieved.origin))
	skel.force_update_all_bone_transforms()

func _aim_bone(skel: Skeleton3D, bone: String, target: Vector3) -> void:
	var idx := skel.find_bone(bone)
	var pose := skel.get_bone_global_pose(idx)
	var delta := target - pose.origin
	if delta.length() < 0.001:
		return
	var swing := Quaternion((pose.basis * Vector3.UP).normalized(), delta.normalized())
	skel.set_bone_global_pose(idx, Transform3D(Basis(swing) * pose.basis, pose.origin))
	skel.force_update_all_bone_transforms()

func _lerp_tf(a: Transform3D, b: Transform3D, u: float) -> Transform3D:
	return Transform3D(a.basis.slerp(b.basis, u), a.origin.lerp(b.origin, u))

func _smooth(t: float) -> float:
	var u := clampf(t, 0.0, 1.0)
	# Minimum-jerk timing: zero velocity and acceleration at either endpoint.
	return u * u * u * (10.0 + u * (-15.0 + 6.0 * u))

func _bone_local(vern: Node3D, skel: Skeleton3D, bone: String) -> Transform3D:
	return vern.global_transform.affine_inverse() * skel.global_transform * skel.get_bone_global_pose(skel.find_bone(bone))

func _tf(value: Dictionary) -> Transform3D:
	var p: Array = value["position"]
	var q: Array = value["rotation_quaternion_xyzw"]
	return Transform3D(Basis(Quaternion(q[0], q[1], q[2], q[3])), Vector3(p[0], p[1], p[2]))

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
