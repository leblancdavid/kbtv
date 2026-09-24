extends SceneTree
## Headless probe: fitted MPFB GLB contents vs old Vern GLB + baked clip path compatibility.
##   <console.exe> --headless --path . --script Tools/modelgen/probe_mpfb_fitted.gd

const FITTED := "res://assets/models3d/characters/vern_mpfb/vern_mpfb_fitted.glb"
const OLD := "res://assets/models3d/characters/vern/vern.glb"
const CLIP := "res://assets/models3d/characters/vern/animations/talk_calm_mpfb.tres"

func _initialize() -> void:
	_dump(FITTED, "FITTED")
	_dump(OLD, "OLD")
	_check_clip()
	quit()

func _dump(model_path: String, tag: String) -> void:
	print("==== %s %s" % [tag, model_path])
	var scn = load(model_path)
	if scn == null:
		print("%s: FAILED TO LOAD" % tag)
		return
	var root3d = scn.instantiate()
	_trace(root3d, tag + ":", 0)
	var skel = _find_skel(root3d)
	if skel != null:
		print("  Skeleton name=%s bones=%d" % [skel.name, skel.get_bone_count()])
		var ro = _rest_pos(skel, "root")
		var pel = _rest_pos(skel, "pelvis.L")
		var head = _rest_pos(skel, "head")
		print("  rest y: root=%.3f pelvis.L=%.3f head=%.3f" % [ro.y, pel.y, head.y])
		print("  Skeleton node path: %s" % skel.get_path())
	var ap = _find_ap(root3d)
	if ap != null:
		print("  root_node=%s" % ap.root_node)
		var lib_names = ap.get_animation_library_list()
		for lib_name in lib_names:
			var lib = ap.get_animation_library(lib_name)
			var names: PackedStringArray = []
			var anames = lib.get_animation_list()
			for an in anames:
				names.append(an)
			print("  library '%s': %s" % [lib_name, ", ".join(names)])
		var all_anims = ap.get_animation_list()
		for an in all_anims:
			print("  anim list: %s" % an)
	root3d.free()

func _check_clip() -> void:
	print("==== CLIP %s" % CLIP)
	var scn = load(FITTED)
	var root3d = scn.instantiate()
	var anim = load(CLIP)
	if anim == null:
		print("CLIP FAILED TO LOAD")
		return
	print("  clip length=%.3f tracks=%d" % [anim.length, anim.get_track_count()])
	var prefix := ""
	var skel = _find_skel(root3d)
	if skel != null:
		var skel_path = str(skel.get_path())
		prefix = skel_path + "/"
		print("  skel path: %s" % skel_path)
	var match_count := 0
	for i in range(anim.get_track_count()):
		var p = anim.track_get_path(i)
		if i < 3:
			print("  track %d: %s" % [i, p])
		if prefix != "" and str(p).begins_with(prefix):
			match_count += 1
	print("  tracks matching imported skel prefix: %d / %d" % [match_count, anim.get_track_count()])
	root3d.free()

func _trace(n, prefix: String, depth: int) -> void:
	if depth > 12:
		return
	print("  %s%s (class=%s)" % [prefix, n.name, n.get_class()])
	for c in n.get_children():
		_trace(c, prefix + "  ", depth + 1)

func _find_skel(n) -> Skeleton3D:
	if n is Skeleton3D:
		return n
	for c in n.get_children():
		var r = _find_skel(c)
		if r != null:
			return r
	return null

func _find_ap(n):
	if n is AnimationPlayer:
		return n
	for c in n.get_children():
		var r = _find_ap(c)
		if r != null:
			return r
	return null

func _rest_pos(skel: Skeleton3D, bone: String) -> Vector3:
	var bid = skel.find_bone(bone)
	if bid == -1:
		return Vector3(-999, -999, -999)
	return skel.get_bone_global_rest_pose(bid).origin