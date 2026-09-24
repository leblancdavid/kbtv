extends SceneTree

# MPFB rebake of Vern-native clips (idle_breathing, talking_default, smoking, drink_coffee)
# for Migration Phase 3/4. These clips exist ONLY on the old VernRig GLB (53 bones,
# 47 rotation tracks each covering torso/arms/fingers/legs, no root). The MPFB fitted
# skeleton (137 bones) has the seated pose baked into its imported rest, so:
#   - every MPFB bone not mapped below HOLDS its seat rest rotation;
#   - mapped bones get Verm-space source rotations rewritten into MPFB bone space via
#     Godot's Overwrite-Axis rest-fixer formula (points = nq^-1*oq*q*orq^-1*oq^-1*nq*nrq);
#   - all source POS tracks are DROPPED (on the old rig they place the seated parts; the
#     MPFB rest already is seated) except a constant root POS = fitted rest root origin;
#   - jaw ROT holds seat; jaw speech is carried onto a SCALE pulse (type-3) from the source;
#   - seat/planted bones with non-unit fitted rest scale (root Z=1.000004) get a constant
#     SCALE track so the baked basis matches the baseline pose through the whole scale,
#     not a quaternion reconstruction that drops it;
#   - eyelid/chest/limb SCALE squash and grip bones are dropped (no MPFB equivalents).
#
# Routes:
#   pelvis.L/R        -> seat rotation    (planted: source pelvis ROT is dropped; hips stay put)
#   spine01           <- vern spine       (spine02-04 hold seat)
#   neck01            <- vern neck        (neck02/03 hold seat)
#   head              <- vern head
#   upperarm01.L/R    <- vern upper_arm   (upperarm02 holds seat)
#   lowerarm01.L/R    <- vern forearm     (lowerarm02 holds seat)
#   wrist.L/R         <- vern hand        (metacarpals hold seat)
#   finger1-1..5-3    <- vern thumb..little_{1,2,3} (finger1=thumb..finger5=little)
#   upperleg01/02.L/R <- vern thigh
#   lowerleg01/02.L/R <- vern shin
#   foot.L/R          -> seat rotation    (planted: source foot ROT is dropped)
#   root              -> NO ROT track (rest basis = baseline), constant POS (rest root origin)
#   jaw               -> seat rotation + SCALE pulse from source
#   everything else   -> hold seat (clavicle, shoulder01, breast, spine02-04, neck02/03,
#                        facial, tongue, metacarpals, toes)
#
# Gain for hands/fingers is 1.0 (no amplification - Vern-authored motion is already the
# performance). Loop continuity: each track's final key is clamped to its first.
#
# Run:
#   Godot_v4.6-headless --path <repo> --script res://Tools/modelgen/rebake_vern_clips.gd

const VERN_GLB := "res://assets/models3d/characters/vern/vern.glb"
const MPFB_GLB := "res://assets/models3d/characters/vern_mpfb/vern_mpfb_fitted.glb"
const OUT_DIR := "res://assets/models3d/characters/vern/animations"
const CLIPS: PackedStringArray = ["idle_breathing", "talking_default", "smoking", "drink_coffee"]
const FPS := 24
const SKEL_PATH := "Vern_MPFB_StandardRig/Skeleton3D"
const LOG := "user://rebake_vern_clips.log"

const TYPE_POS := 1
const TYPE_ROT := 2
const TYPE_SCALE := 3

# Vern source bone -> list of MPFB target bones (full body, fingers finger1=thumb..little=finger5)
var AUTH_VERN_TO_MB := {}
var AUTH_MB_TO_V := {}

func _init() -> void:
	_build_maps()
	dbg("[vern] start")
	var vern_p := load(VERN_GLB) as PackedScene
	var mpfb_p := load(MPFB_GLB) as PackedScene
	if vern_p == null or mpfb_p == null:
		push_error("cannot load GLBs")
		quit(1)
		return
	var vern_root := vern_p.instantiate()
	var vern_skel := _find_skeleton(vern_root)
	var vern_ap := _find_ap(vern_root)
	var mpfb_root := mpfb_p.instantiate()
	var mpfb_skel := _find_skeleton(mpfb_root)
	var mpfb_ap := _find_ap(mpfb_root)
	if vern_skel == null or vern_ap == null or mpfb_skel == null or mpfb_ap == null:
		push_error("missing nodes")
		vern_root.free(); mpfb_root.free()
		quit(1)
		return

	# seat basis = MPFB skeleton's IMPORTED seated_rest CLIP pose (t=0), falling back
	# to skeleton rest per-bone. VernCharacter3D applies the seated_rest clip before any
	# baked clip plays, so "hold seat" must match that clip pose, not the raw rest (the
	# fitted clip's root POS + feet rotations deviate from rest).
	var seat_local := {}
	var seat_scale_local := {}
	for i in mpfb_skel.get_bone_count():
		var bn := String(mpfb_skel.get_bone_name(i))
		seat_local[bn] = Quaternion(mpfb_skel.get_bone_rest(i).basis)
		seat_scale_local[bn] = mpfb_skel.get_bone_rest(i).basis.get_scale()
	var root_origin: Vector3 = mpfb_skel.get_bone_rest(mpfb_skel.find_bone("root")).origin
	var seat_anim: Animation = mpfb_ap.get_animation("seated_rest")
	if seat_anim != null:
		for t in seat_anim.get_track_count():
			var bone := String(seat_anim.track_get_path(t)).get_slice(":", 1)
			if int(seat_anim.track_get_type(t)) == TYPE_ROT and seat_anim.track_get_key_count(t) > 0:
				seat_local[bone] = seat_anim.track_get_key_value(t, 0) as Quaternion
			elif bone == "root" and int(seat_anim.track_get_type(t)) == TYPE_POS and seat_anim.track_get_key_count(t) > 0:
				root_origin = seat_anim.track_get_key_value(t, 0) as Vector3
		dbg("[vern] seat basis from seated_rest clip pose (root_origin=%s)" % root_origin)

	# rewrite constants per MPFB target (Overwrite-Axis, old=Vern rest new=MPFB rest)
	var auth_rw := _build_auth_rw(vern_skel, mpfb_skel, AUTH_VERN_TO_MB)
	dbg("[vern] map size vern->mb=%d rw=%d" % [AUTH_VERN_TO_MB.size(), auth_rw.size()])

	for clip in CLIPS:
		var anim: Animation = vern_ap.get_animation(clip)
		if anim == null:
			push_error("clip missing: " + clip)
			continue
		var samplers := {}
		for t in anim.get_track_count():
			if int(anim.track_get_type(t)) != TYPE_ROT:
				continue
			var vb := String(anim.track_get_path(t)).get_slice(":", 1)
			if AUTH_VERN_TO_MB.has(vb) and vern_skel.find_bone(vb) >= 0:
				samplers[vb] = _make_sampler(anim, t)
		var jaw_scale_sampler: Variant = null
		for t in anim.get_track_count():
			if int(anim.track_get_type(t)) != TYPE_SCALE:
				continue
			if String(anim.track_get_path(t)).get_slice(":", 1) == "jaw":
				jaw_scale_sampler = _make_sampler(anim, t)

		var length := anim.get_length()
		var frames := int(ceil(length * FPS))
		var out := Animation.new()
		out.length = length
		out.step = 1.0 / FPS
		out.loop_mode = Animation.LOOP_LINEAR

		var rot_track := {}
		for f in frames:
			var t := (f + 0.5) / FPS
			if t > length:
				t = length
			for i in mpfb_skel.get_bone_count():
				var mb := String(mpfb_skel.get_bone_name(i))
				if mb == "root":
					# root rotation stays at REST: neither the source Vern clips nor the fitted
					# seated_rest clip carry a root ROT track, so baking Quaternion(rest_basis) here
					# would lose the fitted rest basis's tiny shear/scale (~1.7e-6 rows) and break
					# Transform3D.IsEqualApprox against the baseline pose. With no ROT track the
					# skeleton keeps root on its exact rest basis, which IS the baseline pose.
					continue
				var local: Quaternion
				if mb == "jaw" or mb == "foot.L" or mb == "foot.R" or mb == "pelvis.L" or mb == "pelvis.R":
					# seat/planted bones hold the imported seated_rest clip pose
					local = seat_local[mb]
				elif AUTH_MB_TO_V.has(mb):
					var vb: String = AUTH_MB_TO_V[mb]
					if samplers.has(vb):
						local = _rewrite(auth_rw, mb, _sample_q(samplers[vb], t))
					else:
						local = seat_local[mb]
				else:
					local = seat_local[mb]
				if f == 0:
					rot_track[mb] = out.add_track(TYPE_ROT)
					out.track_set_path(rot_track[mb], NodePath(SKEL_PATH + ":" + mb))
					out.track_set_interpolation_type(rot_track[mb], Animation.INTERPOLATION_LINEAR)
				out.track_insert_key(rot_track[mb], t, local)

		# self-contained root placement: constant POS = fitted skeleton rest root origin
		var pos_t := out.add_track(TYPE_POS)
		out.track_set_path(pos_t, NodePath(SKEL_PATH + ":root"))
		out.track_set_interpolation_type(pos_t, Animation.INTERPOLATION_LINEAR)
		out.track_insert_key(pos_t, 0.0, root_origin)
		out.track_insert_key(pos_t, out.length, root_origin)

		# jaw speech SCALE pulse from the source clip (raw local-axis semantics)
		var jaw_scale_t := out.add_track(TYPE_SCALE)
		out.track_set_path(jaw_scale_t, NodePath(SKEL_PATH + ":jaw"))
		out.track_set_interpolation_type(jaw_scale_t, Animation.INTERPOLATION_LINEAR)
		if jaw_scale_sampler != null:
			for f in frames:
				var tj := (f + 0.5) / FPS
				if tj > length:
					tj = length
				out.track_insert_key(jaw_scale_t, tj, _sample(jaw_scale_sampler, tj))

		# seat/planted bones with non-unit rest scale get a constant SCALE track so the
		# baked basis reproduces the baseline (seated_rest) pose exactly, including scale.
		# Without it, the ROT-only key resets scale to 1 and root's Z=1.000004 rest scale
		# is lost -> ~1.7e-6 basis delta that trips Transform3D.IsEqualApprox(Epsilon).
		# (root is excluded: it carries NO ROT track, so its rest basis already matches.)
		for mb in ["jaw", "foot.L", "foot.R", "pelvis.L", "pelvis.R"]:
			var rest_scale: Vector3 = seat_scale_local[mb]
			if rest_scale.distance_to(Vector3.ONE) > 1.0e-6:
				var seat_scale_t := out.add_track(TYPE_SCALE)
				out.track_set_path(seat_scale_t, NodePath(SKEL_PATH + ":" + mb))
				out.track_set_interpolation_type(seat_scale_t, Animation.INTERPOLATION_LINEAR)
				out.track_insert_key(seat_scale_t, 0.0, rest_scale)
				out.track_insert_key(seat_scale_t, out.length, rest_scale)

		# loop continuity: clamp final key to first
		for ti in out.get_track_count():
			var kc := out.track_get_key_count(ti)
			if kc >= 2:
				out.track_set_key_value(ti, kc - 1, out.track_get_key_value(ti, 0))

		var out_path := OUT_DIR + "/" + clip + "_mpfb.tres"
		DirAccess.make_dir_recursive_absolute(out_path.get_base_dir())
		var err := ResourceSaver.save(out, out_path)
		if err != OK:
			push_error("save failed: " + clip + " " + str(err))
			continue

		var covered := {}
		for t in out.get_track_count():
			covered[String(out.track_get_path(t)).get_slice(":", 1)] = true
		var missing := []
		for i in mpfb_skel.get_bone_count():
			var mb := String(mpfb_skel.get_bone_name(i))
			if not covered.has(mb):
				missing.append(mb)
		print("=== BAKED %s (from Vern %s) len=%.3f frames=%d tracks=%d MISSING_COVERAGE=%d" % [out_path, clip, length, frames, out.get_track_count(), missing.size()])
		for m in missing:
			print("  MISSING " + m)

	vern_root.free(); mpfb_root.free()
	quit(0)

func _build_maps() -> void:
	for side in ["L", "R"]:
		AUTH_VERN_TO_MB["upper_arm." + side] = ["upperarm01." + side]
		AUTH_VERN_TO_MB["forearm." + side] = ["lowerarm01." + side]
		AUTH_VERN_TO_MB["hand." + side] = ["wrist." + side]
		AUTH_VERN_TO_MB["thigh." + side] = ["upperleg01." + side, "upperleg02." + side]
		AUTH_VERN_TO_MB["shin." + side] = ["lowerleg01." + side, "lowerleg02." + side]
		# foot.L/R is NOT routed: source foot ROT is dropped and the MPFB feet hold
		# the imported seated_rest pose so Vern stays planted while seated.
		var fingers := {
			"thumb": "finger1", "index": "finger2", "middle": "finger3",
			"ring": "finger4", "little": "finger5",
		}
		for vname in fingers:
			for seg in ["1", "2", "3"]:
				AUTH_VERN_TO_MB[vname + "_" + seg + "." + side] = [fingers[vname] + "-" + seg + "." + side]
	# pelvis.L/R is NOT routed: source pelvis ROT is dropped and the MPFB hips hold
	# the imported seated_rest pose so Vern's hips stay planted while seated.
	AUTH_VERN_TO_MB["spine"] = ["spine01"]
	AUTH_VERN_TO_MB["neck"] = ["neck01"]
	AUTH_VERN_TO_MB["head"] = ["head"]
	for vb in AUTH_VERN_TO_MB:
		for mb in AUTH_VERN_TO_MB[vb]:
			AUTH_MB_TO_V[mb] = vb

func dbg(msg: String) -> void:
	var f := FileAccess.open(LOG, FileAccess.WRITE)
	f.store_line(msg)
	f.close()
	print(msg)

func _make_sampler(anim: Animation, t: int) -> Dictionary:
	var times := PackedFloat32Array()
	var vals := []
	for k in anim.track_get_key_count(t):
		times.append(anim.track_get_key_time(t, k))
		vals.append(anim.track_get_key_value(t, k))
	return { "times": times, "vals": vals }

func _sample(s: Variant, t: float) -> Variant:
	if s == null:
		return Quaternion.IDENTITY
	var times: PackedFloat32Array = s["times"]
	var vals: Array = s["vals"]
	if times.size() == 0:
		return Quaternion.IDENTITY
	if t <= times[0]:
		return vals[0]
	if t >= times[times.size() - 1]:
		return vals[vals.size() - 1]
	for i in range(times.size() - 1):
		if t >= times[i] and t <= times[i + 1]:
			var u := clampf((t - times[i]) / maxf(times[i + 1] - times[i], 0.000001), 0.0, 1.0)
			var a0 = vals[i]
			var a1 = vals[i + 1]
			if a0 is Quaternion:
				return (a0 as Quaternion).slerp(a1 as Quaternion, u)
			if a0 is Vector3:
				return (a0 as Vector3).lerp(a1 as Vector3, u)
			return a0
	return vals[vals.size() - 1]

func _sample_q(s: Variant, t: float) -> Quaternion:
	var v: Variant = _sample(s, t)
	if v is Quaternion:
		return v
	return Quaternion.IDENTITY

func _find_skeleton(n: Node) -> Skeleton3D:
	if n is Skeleton3D:
		return n
	for c in n.get_children():
		var r := _find_skeleton(c)
		if r != null:
			return r
	return null

func _find_ap(n: Node) -> AnimationPlayer:
	if n is AnimationPlayer:
		return n
	for c in n.get_children():
		var r := _find_ap(c)
		if r != null:
			return r
	return null

# Rewrite constants per MPFB target from source Vern rest (old) and MPFB rest (new).
func _build_auth_rw(vs: Skeleton3D, ms: Skeleton3D, v2m: Dictionary) -> Dictionary:
	var rw := {}
	for vb in v2m:
		var vi := vs.find_bone(vb)
		if vi < 0:
			continue
		var vpar := vs.get_bone_parent(vi)
		var oq: Quaternion = Quaternion(vs.get_bone_global_rest(vpar).basis) if vpar >= 0 else Quaternion.IDENTITY
		var orq: Quaternion = Quaternion(vs.get_bone_rest(vi).basis)
		for mb in v2m[vb]:
			var mi := ms.find_bone(mb)
			if mi < 0:
				continue
			var mpar := ms.get_bone_parent(mi)
			var nq: Quaternion = Quaternion(ms.get_bone_global_rest(mpar).basis) if mpar >= 0 else Quaternion.IDENTITY
			var nrq: Quaternion = Quaternion(ms.get_bone_rest(mi).basis)
			rw[mb] = { "nq": nq, "oq": oq, "orq": orq, "nrq": nrq }
	return rw

func _rewrite(rw: Dictionary, key: String, q: Quaternion) -> Quaternion:
	if not rw.has(key):
		return q
	var r: Dictionary = rw[key]
	return r["nq"].inverse() * r["oq"] * q * r["orq"].inverse() * r["oq"].inverse() * r["nq"] * r["nrq"]