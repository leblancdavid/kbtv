extends SceneTree
## Numeric world-geometry validation: old Vern clip on old rig vs MPFB rebake on fitted rig.
##   --script Tools/modelgen/validate_rebake.gd
const VERN_GLB := "res://assets/models3d/characters/vern/vern.glb"
const MPFB_GLB := "res://assets/models3d/characters/vern_mpfb/vern_mpfb_fitted.glb"
const CLIPS: PackedStringArray = ["idle_breathing", "smoking", "drink_coffee", "talking_default"]
const MOUTH_OLD := Vector3(0, 0.0310001, -0.127)  # old head bone frame (front = -Z)
const MOUTH_NEW := Vector3(0, 0.0310001, 0.127)   # probe candidate: MPFB head frame (front = +Z)

func _initialize() -> void:
	var vp := load(VERN_GLB) as PackedScene
	var mp := load(MPFB_GLB) as PackedScene
	for clip in CLIPS:
		var vroot := vp.instantiate()
		root.add_child(vroot)
		var vap: AnimationPlayer = _find_ap(vroot)
		var vs: Skeleton3D = _find_skel(vroot)
		var mroot := mp.instantiate()
		root.add_child(mroot)
		var map: AnimationPlayer = _find_ap(mroot)
		var ms: Skeleton3D = _find_skel(mroot)
		var baked := load("res://assets/models3d/characters/vern/animations/%s_mpfb.tres" % clip) as Animation
		var libs := map.get_animation_library_list()
		var lib: AnimationLibrary = map.get_animation_library(libs[0])
		lib.add_animation(clip, baked)
		print("== %s (old len = %.3f, mpfb len = %.3f)" % [clip, vap.get_animation(clip).get_length(), baked.length])
		var times := [0.1, 0.75, 1.5, 2.5, maxf(0.0, baked.length - 0.05)]
		for t in times:
			vap.play(clip); vap.seek(t, true)
			map.play(clip); map.seek(t, true)
			await process_frame
			var vhead := vs.global_transform * vs.get_bone_global_pose(vs.find_bone("head"))
			var mhead := ms.global_transform * ms.get_bone_global_pose(ms.find_bone("head"))
			var vhandL := vs.global_transform * vs.get_bone_global_pose(vs.find_bone("hand.L"))
			var mwristL := ms.global_transform * ms.get_bone_global_pose(ms.find_bone("wrist.L"))
			var vhandR := vs.global_transform * vs.get_bone_global_pose(vs.find_bone("hand.R"))
			var mwristR := ms.global_transform * ms.get_bone_global_pose(ms.find_bone("wrist.R"))
			var vpel := vs.global_transform * vs.get_bone_global_pose(vs.find_bone("pelvis"))
			var mpel := ms.global_transform * ms.get_bone_global_pose(ms.find_bone("pelvis.R"))
			var vmouth := vhead * MOUTH_OLD
			var mmouth_newcand := mhead * MOUTH_NEW
			print("  t=%.2f" % t)
			print("    head old=(%.3f,%.3f,%.3f) new=(%.3f,%.3f,%.3f) d=%.3f" % [vhead.origin.x, vhead.origin.y, vhead.origin.z, mhead.origin.x, mhead.origin.y, mhead.origin.z, vhead.origin.distance_to(mhead.origin)])
			print("    mouth oldW=(%.3f,%.3f,%.3f) newCandW=(%.3f,%.3f,%.3f) d=%.3f" % [vmouth.x, vmouth.y, vmouth.z, mmouth_newcand.x, mmouth_newcand.y, mmouth_newcand.z, vmouth.distance_to(mmouth_newcand)])
			print("    handL old=(%.3f,%.3f,%.3f) wristL new=(%.3f,%.3f,%.3f) d=%.3f" % [vhandL.origin.x, vhandL.origin.y, vhandL.origin.z, mwristL.origin.x, mwristL.origin.y, mwristL.origin.z, vhandL.origin.distance_to(mwristL.origin)])
			print("    handR old=(%.3f,%.3f,%.3f) wristR new=(%.3f,%.3f,%.3f) d=%.3f" % [vhandR.origin.x, vhandR.origin.y, vhandR.origin.z, mwristR.origin.x, mwristR.origin.y, mwristR.origin.z, vhandR.origin.distance_to(mwristR.origin)])
			print("    pelvis old=(%.3f,%.3f,%.3f) new=(%.3f,%.3f,%.3f) d=%.3f" % [vpel.origin.x, vpel.origin.y, vpel.origin.z, mpel.origin.x, mpel.origin.y, mpel.origin.z, vpel.origin.distance_to(mpel.origin)])
		vroot.queue_free(); mroot.queue_free()
	quit(0)

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