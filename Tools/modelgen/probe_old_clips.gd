extends SceneTree
## Probe old Vern clips' bone track inventories -> informs VernRig->MPFB rebake map.
##   <console.exe> --headless --path . --script Tools/modelgen/probe_old_clips.gd

const VERN_GLB := "res://assets/models3d/characters/vern/vern.glb"
const CLIPS := ["seated_rest", "idle_breathing", "talking_default", "smoking", "drink_coffee"]

func _initialize() -> void:
	var scn = load(VERN_GLB)
	var root = scn.instantiate()
	var ap = _find_ap(root)
	if ap == null:
		print("NO ANIMATIONPLAYER")
		quit(1)
		return
	for clip in CLIPS:
		var anim = ap.get_animation(clip)
		if anim == null:
			print("== %s: MISSING" % clip)
			continue
		var rots := {}
		var scales := {}
		var poss := {}
		for t in range(anim.get_track_count()):
			var typ = int(anim.track_get_type(t))
			var bone = str(anim.track_get_path(t)).get_slice(":", 1)
			if typ == 2:
				rots[bone] = anim.track_get_key_count(t)
			elif typ == 3:
				scales[bone] = anim.track_get_key_count(t)
			elif typ == 1:
				poss[bone] = anim.track_get_key_count(t)
		print("== %s len=%.3f" % [clip, anim.get_length()])
		print("   ROT(%d): %s" % [rots.size(), ", ".join(PackedStringArray(rots.keys()))])
		if scales.size():
			print("   SCL(%d): %s" % [scales.size(), ", ".join(PackedStringArray(scales.keys()))])
		if poss.size():
			print("   POS(%d): %s" % [poss.size(), ", ".join(PackedStringArray(poss.keys()))])
		var skel = _find_skel(root)
		var absent := []
		for b in rots:
			if skel.find_bone(b) < 0:
				absent.append(b)
		if absent.size():
			print("   !! ABSENT ROT TRACK BONES: %s" % ", ".join(PackedStringArray(absent)))
	root.free()
	quit(0)

func _find_ap(n):
	if n is AnimationPlayer:
		return n
	for c in n.get_children():
		var r = _find_ap(c)
		if r != null:
			return r
	return null

func _find_skel(n):
	if n is Skeleton3D:
		return n
	for c in n.get_children():
		var r = _find_skel(c)
		if r != null:
			return r
	return null
