extends SceneTree
## Phase-3 contacts remap: compute wrist-local grips that anchor each contact prop to its
## contract rest AT PICKUP on the MPFB fitted rig, verify old-rig authoring continuity, and
## validate the new mouth_marker head_local. Runtime uses:
##   propLocal = GlobalTransform.affine_inverse() * skeleton.global_transform * boneGlobalPose(hand) * grip
## With K = skeleton->Vern-local (GlobalInverse*SkeletonGlobal), want K*bonePose*grip == rest at pickup,
## so grip = (K*bonePose).affine_inverse() * rest. The MPFB model is yaw-180 under Vern.tscn to cancel
## the yaw-180 VernStation (net identity), so Vern faces the table/camera. MODEL_YAW_DEG must match the
## scene Model node rotation or the runtime grips will misplace.
##   --script Tools/modelgen/remap_contacts.gd
const VERN_GLB := "res://assets/models3d/characters/vern/vern.glb"
const MPFB_GLB := "res://assets/models3d/characters/vern_mpfb/vern_mpfb_fitted.glb"
const CLIP_DIR := "res://assets/models3d/characters/vern/animations"
const MODEL_YAW_DEG := 180.0
const MOUTH_NEW := Vector3(0, 0.0310001, 0.127)

const CASES := [
	{ "clip": "smoking", "new_hand": "wrist.R", "pickup": 1.1, "prop": "cigarette" },
	{ "clip": "drink_coffee", "new_hand": "wrist.L", "pickup": 1.1, "prop": "coffee_mug" },
]

func _initialize() -> void:
	var contract: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(
		"res://assets/models3d/characters/vern/animation_contacts.json"))
	var failed := 0
	for c in CASES:
		if not (await _run_case(contract, c)):
			failed += 1
	_mouth(contract)
	quit(failed)

func _run_case(contract: Dictionary, c: Dictionary) -> bool:
	var clip: String = c.clip
	var hand_new: String = c.new_hand
	var pickup: float = c.pickup
	var prop: String = c.prop
	var rest: Transform3D = _tf(contract["props"][prop]["rest"])
	var old_grip: Transform3D = _tf(contract["actions"][clip]["hand_local_grip"])
	var hand_old: String = contract["actions"][clip]["hand_bone"]
	var ok := true

	# OLD authoring continuity: old rig + old clip at pickup. Grip was authored as pickup_hand^-1 * rest.
	var vroot := (load(VERN_GLB) as PackedScene).instantiate()
	root.add_child(vroot)
	var vap: AnimationPlayer = _find_ap(vroot)
	var vs: Skeleton3D = _find_skel(vroot)
	vap.play(clip)
	vap.seek(pickup, true)
	await process_frame
	var vworld := vs.global_transform * vs.get_bone_global_pose(vs.find_bone(hand_old))
	var vlocal := vworld * old_grip
	var cont_d := vlocal.origin.distance_to(rest.origin)
	print("[%s] OLD continuity: hand_local_grip -> rest at t=%.1f, |d|=%.4f %s"
		% [clip, pickup, cont_d, "OK" if cont_d < 0.02 else "FAIL(old grip drifted)"])
	ok = ok and cont_d < 0.02
	vroot.queue_free()

	# NEW grip: fitted rig under the same Model transform used by Vern.tscn, playing the MPFB rebake.
	var mroot := (load(MPFB_GLB) as PackedScene).instantiate()
	var vern := Node3D.new()
	var model := Node3D.new()
	model.transform = Transform3D(Basis(Vector3.UP, deg_to_rad(MODEL_YAW_DEG)), Vector3.ZERO)
	model.add_child(mroot)
	vern.add_child(model)
	root.add_child(vern)
	var map: AnimationPlayer = _find_ap(mroot)
	var ms: Skeleton3D = _find_skel(mroot)
	var baked := load("%s/%s_mpfb.tres" % [CLIP_DIR, clip]) as Animation
	(map.get_animation_library(map.get_animation_library_list()[0]) as AnimationLibrary).add_animation(clip, baked)
	map.play(clip)
	map.seek(pickup, true)
	await process_frame
	var bone := ms.find_bone(hand_new)
	if bone < 0:
		print("[%s] FAIL: %s not found on fitted rig." % [clip, hand_new])
		return false
	var P := ms.get_bone_global_pose(bone)
	var K := vern.global_transform.affine_inverse() * ms.global_transform
	var grip := (K * P).affine_inverse() * rest   # solves K*bonePose*grip == rest

	var got := K * ms.get_bone_global_pose(bone) * grip
	var d := got.origin.distance_to(rest.origin)
	var q := Quaternion(grip.basis)
	print("[%s] NEW grip for %s with %s at t=%.1f: d=%.5f %s" % [clip, prop, hand_new, pickup, d,
		"OK" if d < 1e-4 else "FAIL(not anchored to rest)"])
	print("  position: [%.7f, %.7f, %.7f]" % [grip.origin.x, grip.origin.y, grip.origin.z])
	print("  rotation_quaternion_xyzw: [%.8f, %.8f, %.8f, %.8f]" % [q.x, q.y, q.z, q.w])
	ok = ok and d < 1e-4
	mroot.queue_free()
	return ok

func _mouth(contract: Dictionary) -> void:
	var vroot := (load(VERN_GLB) as PackedScene).instantiate()
	root.add_child(vroot)
	var vs: Skeleton3D = _find_skel(vroot)
	var mroot := (load(MPFB_GLB) as PackedScene).instantiate()
	var vern := Node3D.new()
	var model := Node3D.new()
	model.transform = Transform3D(Basis(Vector3.UP, deg_to_rad(MODEL_YAW_DEG)), Vector3.ZERO)
	model.add_child(mroot)
	vern.add_child(model)
	root.add_child(vern)
	var ms: Skeleton3D = _find_skel(mroot)
	var old_head_local: Transform3D = _tf(contract["mouth_marker"]["head_local"])
	var old_mouth := vs.global_transform * vs.get_bone_global_pose(vs.find_bone("head")) * old_head_local
	var new_head: Transform3D = ms.global_transform * ms.get_bone_global_pose(ms.find_bone("head"))
	var new_mouth: Vector3 = new_head * MOUTH_NEW
	print("mouth old world=(%.3f,%.3f,%.3f) | new head_local=(0,0.0310001,0.127) -> (%.3f,%.3f,%.3f) | y-offset=%.3f"
		% [old_mouth.origin.x, old_mouth.origin.y, old_mouth.origin.z,
			new_mouth.x, new_mouth.y, new_mouth.z,
			new_mouth.y - old_mouth.origin.y])
	mroot.queue_free()
	vroot.queue_free()

func _tf(v: Dictionary) -> Transform3D:
	var p: Array = v["position"]
	var q: Array = v["rotation_quaternion_xyzw"]
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
