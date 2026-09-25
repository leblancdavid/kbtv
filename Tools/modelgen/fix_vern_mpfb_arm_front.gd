extends SceneTree

const MPFB_GLB := "res://assets/models3d/characters/vern_mpfb/vern_mpfb_fitted.glb"
const CLIP_DIR := "res://assets/models3d/characters/vern/animations"
const CLIPS := ["talk_calm", "idle_breathing", "talking_default", "smoking", "drink_coffee"]
const FPS := 24
const MODEL_YAW := PI
const SKEL_PATH := "Vern_MPFB_StandardRig/Skeleton3D"
const TYPE_ROT := 2

func _initialize() -> void:
	var packed := load(MPFB_GLB) as PackedScene
	if packed == null:
		push_error("missing MPFB GLB")
		quit(1)
		return
	var failed := 0
	for clip in CLIPS:
		if not await _fix_clip(packed, clip):
			failed += 1
	quit(failed)

func _fix_clip(packed: PackedScene, clip: String) -> bool:
	var source := load("%s/%s_mpfb.tres" % [CLIP_DIR, clip]) as Animation
	if source == null:
		push_error("missing clip " + clip)
		return false
	var vern := Node3D.new()
	var model := Node3D.new()
	model.transform = Transform3D(Basis(Vector3.UP, MODEL_YAW), Vector3.ZERO)
	var imported := packed.instantiate()
	model.add_child(imported)
	vern.add_child(model)
	root.add_child(vern)
	var ap := _find_ap(imported)
	var skel := _find_skel(imported)
	if ap == null or skel == null:
		push_error("missing nodes for " + clip)
		vern.queue_free()
		return false
	(ap.get_animation_library(ap.get_animation_library_list()[0]) as AnimationLibrary).add_animation("__fix", source)

	var out := source.duplicate(true) as Animation
	var corrected := []
	for side in ["L", "R"]:
		corrected.append_array(["upperarm01." + side, "upperarm02." + side, "lowerarm01." + side, "lowerarm02." + side, "wrist." + side])
	for i in range(out.get_track_count() - 1, -1, -1):
		if int(out.track_get_type(i)) == TYPE_ROT and corrected.has(String(out.track_get_path(i)).get_slice(":", 1)):
			out.remove_track(i)
	var tracks := {}
	for bone in corrected:
		var ti := out.add_track(TYPE_ROT)
		out.track_set_path(ti, NodePath(SKEL_PATH + ":" + bone))
		out.track_set_interpolation_type(ti, Animation.INTERPOLATION_LINEAR)
		tracks[bone] = ti

	var frames := int(ceil(source.length * FPS))
	var worst_back := 0.0
	for f in frames:
		var t := minf((f + 0.5) / FPS, source.length)
		ap.play("__fix")
		ap.seek(t, true)
		await process_frame
		for side in ["L", "R"]:
			var wrist := skel.find_bone("wrist." + side)
			var wrist_vern := vern.global_transform.affine_inverse() * skel.global_transform * skel.get_bone_global_pose(wrist)
			worst_back = maxf(worst_back, wrist_vern.origin.z)
			if wrist_vern.origin.z > 0.0:
				wrist_vern.origin.z = -wrist_vern.origin.z
			var target := skel.global_transform.affine_inverse() * vern.global_transform * wrist_vern.origin
			_solve_arm(skel, side, target)
		for bone in corrected:
			var idx := skel.find_bone(bone)
			out.track_insert_key(tracks[bone], t, skel.get_bone_pose_rotation(idx))
	for ti in out.get_track_count():
		var kc := out.track_get_key_count(ti)
		if kc >= 2:
			out.track_set_key_value(ti, kc - 1, out.track_get_key_value(ti, 0))
	var err := ResourceSaver.save(out, "%s/%s_mpfb.tres" % [CLIP_DIR, clip])
	print("ARM_FRONT %s worst_back_before=%.4f save=%s" % [clip, worst_back, err])
	vern.queue_free()
	return err == OK

func _solve_arm(skel: Skeleton3D, side: String, target: Vector3) -> void:
	var shoulder_idx := skel.find_bone("upperarm01." + side)
	var elbow_idx := skel.find_bone("lowerarm01." + side)
	var wrist_idx := skel.find_bone("wrist." + side)
	var shoulder := skel.get_bone_global_pose(shoulder_idx).origin
	var elbow0 := skel.get_bone_global_pose(elbow_idx).origin
	var wrist0 := skel.get_bone_global_pose(wrist_idx).origin
	var a := shoulder.distance_to(elbow0)
	var b := elbow0.distance_to(wrist0)
	var delta := target - shoulder
	var d := clampf(delta.length(), absf(a - b) + 0.001, a + b - 0.001)
	var axis := delta.normalized()
	var pole := elbow0 - shoulder
	pole = pole - axis * pole.dot(axis)
	if pole.length() < 0.001:
		pole = Vector3.UP.cross(axis)
		if pole.length() < 0.001:
			pole = Vector3.RIGHT
	pole = pole.normalized()
	var along := (a * a - b * b + d * d) / (2.0 * d)
	var elbow := shoulder + axis * along + pole * sqrt(maxf(0.0, a * a - along * along))
	_aim_bone(skel, "upperarm01." + side, elbow)
	_aim_bone(skel, "upperarm02." + side, elbow)
	_aim_bone(skel, "lowerarm01." + side, target)
	_aim_bone(skel, "lowerarm02." + side, target)
	var lower := skel.get_bone_global_pose(skel.find_bone("lowerarm02." + side))
	skel.set_bone_global_pose(wrist_idx, Transform3D(lower.basis, target))
	skel.force_update_all_bone_transforms()

func _aim_bone(skel: Skeleton3D, bone: String, target: Vector3) -> void:
	var idx := skel.find_bone(bone)
	var pose := skel.get_bone_global_pose(idx)
	var delta := target - pose.origin
	if delta.length() < 0.001:
		return
	var old := (pose.basis * Vector3.UP).normalized()
	var swing := Quaternion(old, delta.normalized())
	skel.set_bone_global_pose(idx, Transform3D(Basis(swing) * pose.basis, pose.origin))
	skel.force_update_all_bone_transforms()

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
