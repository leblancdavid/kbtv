extends SceneTree

# MPFB rebake of Vern's "talk_calm" clip for Migration Phase 2.
#
# Ports the CCO reference library's "Sitting_Talking" body rotations onto the MPFB
# fitted skeleton (Vern_MPFB_StandardRig, 137 bones). The fitted rig's imported rest IS
# the seated pose (the glTF importer bakes the animated root's first-frame translation
# into its base TRS), so per-bone seat locals are read straight from the skeleton rest.
#
# Routes (by MPFB bone name):
#   PINNED   pelvis/legs             -> hold seat local (feet stay planted)
#   ROOT     root                    -> no ROT track; constant seated POS only
#   DELTA    spine05, spine01,       -> seat_local * mannequin delta via the DEF-*
#            neck01, head               rewrite band (DEF-spine.001/.002, DEF-neck, DEF-head)
#   WRIST    wrist.L/R               -> seat local * mannequin talking-hand gesture
#                                     (DEF-hand.L/R delta, WRIST_GAIN, hand roll DISABLED)
#   AUTH     upperarm01, lowerarm01, -> Vern's talking_default rewritten into MPFB
#            finger1-1..finger5-3       space (auth_rw), finger gain + seam blend
#   JAW      jaw                     -> Vern's jaw speech, raw cross-rewrite (no gain/seam)
#   HOLD     everything else         -> seat local (spine02..04, neck02/03, clavicle,
#                                     shoulder01, upperarm02/lowerarm02, metacarpal1-4,
#                                     breast, facial, tongue)
#
# The AUTH cross-rewrite reproduces Godot's Overwrite-Axis rest fixer between the Vern
# skeleton (old) and the MPFB skeleton (new): q' = nq^-1 * oq * q * orq^-1 * oq^-1 * nq * nrq.
#
# Self-contained: the clip carries a constant root POS track equal to the fitted
# skeleton's rest root origin (the drop is already baked into the imported rest, so
# emitting the raw drop would double-drop). Root has no ROT track because neither
# the source Vern clips nor `seated_rest` carry root rotation, and reconstructing
# the fitted root rest basis from a quaternion introduces tiny basis drift. Output tracks are
# "Vern_MPFB_StandardRig/Skeleton3D:<mpfb_bone>" for drop-in compatibility.
#
# Tuning defaults chosen for the MPFB seated rig (all flagged for render verification):
#   HAND_ROLL_DEG 0.0 (hands rest naturally on the table; no thumb-up roll needed)
#   FINGER_CURL_DEG all 0 (no cupped curl; MPFB hands already rest relaxed)
#   WRIST_GAIN 1.0, FINGER_GAIN 1.3
#
# Run:
#   Godot_v4.6-headless --path <repo> --script res://Tools/modelgen/bake_talk_calm_mpfb.gd

const REF_GLB := "res://docs/references/Animation Library[Standard]/Godot/AnimationLibrary_Godot_Standard.glb"
const VERN_GLB := "res://assets/models3d/characters/vern/vern.glb"
const MPFB_GLB := "res://assets/models3d/characters/vern_mpfb/vern_mpfb_fitted.glb"
const BM_REF := "res://Tools/modelgen/retarget/bone_map_ref.tres"
const BM_VERN := "res://Tools/modelgen/retarget/bone_map_vern.tres"
const BM_MPFB := "res://Tools/modelgen/retarget/bone_map_mpfb.tres"
const REF_CLIP := "Sitting_Talking"
const VERN_CLIP := "talking_default"
const MPFB_SEAT_CLIP := "seated_rest"
const OUT_PATH := "res://assets/models3d/characters/vern/animations/talk_calm_mpfb.tres"
const FPS := 24
const SKEL_PATH := "Vern_MPFB_StandardRig/Skeleton3D"
const LOG := "user://bake_mpfb.log"

const TYPE_POS := 1
const TYPE_ROT := 2
const TYPE_SCALE := 3

const PINNED := [
	"pelvis.L", "pelvis.R",
	"upperleg01.L", "upperleg01.R", "upperleg02.L", "upperleg02.R",
	"lowerleg01.L", "lowerleg01.R", "lowerleg02.L", "lowerleg02.R",
	"foot.L", "foot.R", "toe1-1.L", "toe1-1.R",
]

# MPFB delta-route bones -> DEF-* reference bones (via the profile join)
const DELTA_MB := {
	"spine05": "DEF-spine.001",
	"spine01": "DEF-spine.002",
	"neck01": "DEF-neck",
	"head": "DEF-head",
}

# MPFB wrist bones -> DEF-* hands (mannequin talking-hand gesture delta)
const HAND_DEFB := {
	"wrist.L": "DEF-hand.L",
	"wrist.R": "DEF-hand.R",
}

# Vern authored source bone -> MPFB target bone (fingers: finger1=thumb..finger5=little)
var AUTH_VERN_TO_MB := {}
var AUTH_MB_TO_V := {}

func _init() -> void:
	_build_auth_map()
	dbg("[mpfb] start")
	var ref_p := load(REF_GLB) as PackedScene
	var vern_p := load(VERN_GLB) as PackedScene
	var mpfb_p := load(MPFB_GLB) as PackedScene
	var bm_ref := load(BM_REF) as BoneMap
	var bm_mpfb := load(BM_MPFB) as BoneMap
	dbg("[mpfb] inputs: ref=%s vern=%s mpfb=%s bm=%s" % [ref_p != null, vern_p != null, mpfb_p != null, bm_ref != null and bm_mpfb != null])
	if ref_p == null or vern_p == null or mpfb_p == null or bm_ref == null or bm_mpfb == null:
		push_error("cannot load inputs")
		quit(1)
		return

	var ref_root := ref_p.instantiate()
	var ref_skel := _find_skeleton(ref_root)
	var ref_ap := _find_ap(ref_root)
	var vern_root := vern_p.instantiate()
	var vern_skel := _find_skeleton(vern_root)
	var vern_ap := _find_ap(vern_root)
	var mpfb_root := mpfb_p.instantiate()
	var mpfb_skel := _find_skeleton(mpfb_root)
	var mpfb_ap := _find_ap(mpfb_root)
	dbg("[mpfb] nodes: ref_skel=%s ref_ap=%s vern_skel=%s vern_ap=%s mpfb_skel=%s mpfb_ap=%s bones=%d" % [ref_skel != null, ref_ap != null, vern_skel != null, vern_ap != null, mpfb_skel != null, mpfb_ap != null, mpfb_skel.get_bone_count() if mpfb_skel != null else -1])
	if ref_skel == null or ref_ap == null or vern_skel == null or vern_ap == null or mpfb_skel == null or mpfb_ap == null:
		push_error("missing nodes")
		_free(ref_root, vern_root, mpfb_root)
		quit(1)
		return

	var ref_anim: Animation = ref_ap.get_animation(REF_CLIP)
	var vern_anim: Animation = vern_ap.get_animation(VERN_CLIP)
	if ref_anim == null or vern_anim == null:
		push_error("missing clips")
		_free(ref_root, vern_root, mpfb_root)
		quit(1)
		return

	# ---- profile join: DEF-* (reference) -> MPFB bone name
	var profile := bm_mpfb.get_profile()
	dbg("[mpfb] profile bones: %d" % profile.get_bone_size())
	var def_mpfb := {}
	for i in profile.get_bone_size():
		var pb := String(profile.get_bone_name(i))
		var src := String(bm_ref.get_skeleton_bone_name(StringName(pb)))
		var mb := String(bm_mpfb.get_skeleton_bone_name(StringName(pb)))
		if not mb.is_empty() and not src.is_empty():
			def_mpfb[src] = mb
	dbg("[mpfb] def_mpfb joined: %d" % def_mpfb.size())

	# ---- rewrite constants, keyed by DEF-* name (Overwrite-Axis rest fixer)
	var rw := _build_rewrites(ref_skel, bm_ref, def_mpfb)
	# ---- rewrite constants for Vern-authored bones, keyed by MPFB bone name
	var auth_rw := _build_auth_rw(vern_skel, mpfb_skel, AUTH_VERN_TO_MB)
	dbg("[mpfb] rewrites: def=%d auth=%d" % [rw.size(), auth_rw.size()])

	# ---- body rotation samplers from the RAW reference clip, keyed by DEF-* name
	var body_samples := {}
	for t in ref_anim.get_track_count():
		if int(ref_anim.track_get_type(t)) != TYPE_ROT:
			continue
		var bone := String(ref_anim.track_get_path(t)).get_slice(":", 1)
		if not def_mpfb.has(bone):
			continue
		body_samples[bone] = _make_sampler(ref_anim, t)
	dbg("[mpfb] body samplers: %d" % body_samples.size())

	# ---- Vern authored samplers (ROT tracks only; absent track = hold seat)
	var vern_auth_samplers := {}
	for t in vern_anim.get_track_count():
		if int(vern_anim.track_get_type(t)) != TYPE_ROT:
			continue
		var vb := String(vern_anim.track_get_path(t)).get_slice(":", 1)
		if AUTH_VERN_TO_MB.has(vb) and vern_skel.find_bone(vb) >= 0:
			vern_auth_samplers[vb] = _make_sampler(vern_anim, t)
	dbg("[mpfb] vern authored samplers: %d" % vern_auth_samplers.size())

	# ---- jaw speech sampler (SCALE type 3 track; talking_default animates jaw via scale
	# pulse, no ROT). Sampled raw at output time (mirrors the production vern-only copy,
	# no authored-window shift). Vern's jaw POS track is its own absolute rest origin and
	# is NOT carried over - the MPFB jaw holds its own (seated) rest origin instead.
	var jaw_scale_sampler: Variant = null
	for t in vern_anim.get_track_count():
		if int(vern_anim.track_get_type(t)) != TYPE_SCALE:
			continue
		if String(vern_anim.track_get_path(t)).get_slice(":", 1) == "jaw":
			jaw_scale_sampler = _make_sampler(vern_anim, t)
	dbg("[mpfb] jaw scale sampler: %s" % (jaw_scale_sampler != null))

	# ---- MPFB local seat basis = the IMPORTED seated_rest CLIP pose (t=0), falling back
	# to skeleton rest per-bone. VernCharacter3D applies the seated_rest clip before any
	# baked clip plays, so "hold seat" must match that clip pose, not the raw rest (the
	# fitted clip's root POS + feet rotations deviate from rest).
	var seat_local := {}
	for i in mpfb_skel.get_bone_count():
		seat_local[String(mpfb_skel.get_bone_name(i))] = Quaternion(mpfb_skel.get_bone_rest(i).basis)
	var root_origin: Vector3 = mpfb_skel.get_bone_rest(mpfb_skel.find_bone("root")).origin
	var seat_anim: Animation = mpfb_ap.get_animation(MPFB_SEAT_CLIP) if mpfb_ap != null else null
	if seat_anim != null:
		for t in seat_anim.get_track_count():
			var bone := String(seat_anim.track_get_path(t)).get_slice(":", 1)
			if int(seat_anim.track_get_type(t)) == TYPE_ROT and seat_anim.track_get_key_count(t) > 0:
				seat_local[bone] = seat_anim.track_get_key_value(t, 0) as Quaternion
			elif bone == "root" and int(seat_anim.track_get_type(t)) == TYPE_POS and seat_anim.track_get_key_count(t) > 0:
				root_origin = seat_anim.track_get_key_value(t, 0) as Vector3
		dbg("[mpfb] seat basis from %s clip pose (root_origin=%s)" % [MPFB_SEAT_CLIP, root_origin])
	else:
		dbg("[mpfb] no %s clip; seat basis = skeleton rest (best-effort)" % MPFB_SEAT_CLIP)

	# best-effort cross-check: do the seated_rest action frame-0 rotations deviate from rest?
	_seat_cross_check(mpfb_ap, mpfb_skel, seat_local)

	var length := ref_anim.get_length()
	var frames := int(ceil(length * FPS))
	var out := Animation.new()
	out.length = length
	out.step = 1.0 / FPS
	out.loop_mode = Animation.LOOP_LINEAR

	var rot_track := {}

	# window + amplification for Vern-authored fingers (same search as production bake)
	var AUTHORED_WINDOW := 0.0
	var FINGER_GAIN := 1.3
	var SEAM_BLEND := 0.4
	# hand re-orientation is DISABLED for MPFB (hands rest naturally on the table); kept
	# per-side so it can be reinstated if a render proves a thumb-up roll is wanted.
	var HAND_ROLL_DEG := 0.0
	var WRIST_GAIN := 1.0
	var FINGER_CURL_DEG := {}	# disabled for MPFB pending render verification

	if not vern_auth_samplers.is_empty():
		var vern_len := vern_anim.get_length()
		var end := vern_len - length
		var steps := 4
		var best_score := -1e18
		var best_w0 := 0.0
		for w0i in maxi(int(floor(end * FPS)) + 1, 1):
			var w0 := w0i / float(FPS)
			var tot := 0.0
			for vb in vern_auth_samplers:
				var q0: Quaternion = _sample_q(vern_auth_samplers[vb], w0)
				for s in steps + 1:
					var qt: Quaternion = _sample_q(vern_auth_samplers[vb], w0 + length * s / float(steps))
					tot += (q0.inverse() * qt).get_angle()
			if tot > best_score:
				best_score = tot
				best_w0 = w0
		AUTHORED_WINDOW = best_w0
		dbg("[mpfb] authored window offset %.4f tot=%.4f rad" % [AUTHORED_WINDOW, best_score])

	# first-frame LOCAL rotation per DEF-* bone (clip's starting pose), REST-FIXER-REWRITTEN
	var local0 := {}
	for defb in body_samples:
		local0[defb] = _rewrite(rw, defb, _sample_q(body_samples[defb], 0.0))

	var rolled := Quaternion(Vector3(0, 1, 0), deg_to_rad(HAND_ROLL_DEG))

	for f in frames:
		var t := (f + 0.5) / FPS
		if t > length:
			t = length

		for i in mpfb_skel.get_bone_count():
			var mb := String(mpfb_skel.get_bone_name(i))
			if mb == "root":
				continue
			var local: Quaternion
			if PINNED.has(mb):
				local = seat_local[mb]
			elif mb == "wrist.L" or mb == "wrist.R":
				var base: Quaternion = seat_local[mb] * rolled
				if body_samples.has(HAND_DEFB[mb]):
					var ref_local: Quaternion = _rewrite(rw, HAND_DEFB[mb], _sample_q(body_samples[HAND_DEFB[mb]], t))
					var delta: Quaternion = ref_local * local0.get(HAND_DEFB[mb], Quaternion.IDENTITY).inverse()
					if WRIST_GAIN != 1.0:
						var ang := delta.get_angle()
						if ang > 0.0005:
							var axis := delta.get_axis().normalized()
							delta = Quaternion(axis, ang * WRIST_GAIN)
						else:
							delta = Quaternion.IDENTITY
					local = base * delta
				else:
					local = base
			elif DELTA_MB.has(mb):
				var defb: String = DELTA_MB[mb]
				if body_samples.has(defb):
					var ref_local: Quaternion = _rewrite(rw, defb, _sample_q(body_samples[defb], t))
					var delta: Quaternion = ref_local * local0.get(defb, Quaternion.IDENTITY).inverse()
					local = seat_local[mb] * delta
				else:
					local = seat_local[mb]
			elif AUTH_MB_TO_V.has(mb):
				var vb: String = AUTH_MB_TO_V[mb]
				if vern_auth_samplers.has(vb):
					var local_start: Quaternion = _sample_q(vern_auth_samplers[vb], AUTHORED_WINDOW)
					local = _sample_q(vern_auth_samplers[vb], AUTHORED_WINDOW + t)
					local = local_start.slerp(local, FINGER_GAIN)
					var u := clampf((t - (length - SEAM_BLEND)) / SEAM_BLEND, 0.0, 1.0)
					if u > 0.0:
						local = local.slerp(local_start, u)
					# rewrite Vern-space rotation into MPFB space, then apply any per-segment
					# static curl in the MPFB segment's own local space (disabled by default)
					local = _rewrite(auth_rw, mb, local)
					var seg := String(mb).get_slice("-", 1).get_slice(".", 0)
					if FINGER_CURL_DEG.has(seg):
						local = local * Quaternion(Vector3(1, 0, 0), deg_to_rad(FINGER_CURL_DEG[seg]))
				else:
					local = seat_local[mb]
			elif mb == "jaw":
				# jaw speech travels on the SCALE pulse (no ROT in talking_default);
				# the bone itself holds the seated rest orientation.
				local = seat_local["jaw"]
			else:
				local = seat_local[mb]

			if f == 0:
				rot_track[mb] = out.add_track(TYPE_ROT)
				out.track_set_path(rot_track[mb], NodePath(SKEL_PATH + ":" + mb))
				out.track_set_interpolation_type(rot_track[mb], Animation.INTERPOLATION_LINEAR)
			out.track_insert_key(rot_track[mb], t, local)

	# self-contained root placement: constant POS = fitted skeleton's rest root origin
	var pos_t := out.add_track(TYPE_POS)
	out.track_set_path(pos_t, NodePath(SKEL_PATH + ":root"))
	out.track_set_interpolation_type(pos_t, Animation.INTERPOLATION_LINEAR)
	out.track_insert_key(pos_t, 0.0, root_origin)
	out.track_insert_key(pos_t, out.length, root_origin)

	# jaw speech: a SCALE pulse sampled from Vern's talking_default (raw, same local-axis
	# semantics as the source; the loop clamp below re-locks the final key to the first).
	var jaw_scale_t := out.add_track(TYPE_SCALE)
	out.track_set_path(jaw_scale_t, NodePath(SKEL_PATH + ":jaw"))
	out.track_set_interpolation_type(jaw_scale_t, Animation.INTERPOLATION_LINEAR)
	if jaw_scale_sampler != null:
		for f in frames:
			var tj := (f + 0.5) / FPS
			if tj > length:
				tj = length
			out.track_insert_key(jaw_scale_t, tj, _sample(jaw_scale_sampler, tj))

	# loop continuity: clamp each track's final key to equal its first
	for ti in out.get_track_count():
		var kc := out.track_get_key_count(ti)
		if kc >= 2:
			out.track_set_key_value(ti, kc - 1, out.track_get_key_value(ti, 0))

	DirAccess.make_dir_recursive_absolute(OUT_PATH.get_base_dir())
	var err := ResourceSaver.save(out, OUT_PATH)
	if err != OK:
		push_error("save failed: " + str(err))
		_free(ref_root, vern_root, mpfb_root)
		quit(1)
		return

	print("=== BAKED %s (from %s -> %s) ===" % [OUT_PATH, REF_CLIP, MPFB_GLB])
	print("LENGTH=%.3f FRAMES=%d FPS=%d TRACKS=%d BONES=%d" % [out.length, frames, int(FPS), out.get_track_count(), mpfb_skel.get_bone_count()])

	# coverage validation: every MPFB bone must have a track
	var covered := {}
	for t in out.get_track_count():
		covered[String(out.track_get_path(t)).get_slice(":", 1)] = true
	var missing := []
	for i in mpfb_skel.get_bone_count():
		var mb := String(mpfb_skel.get_bone_name(i))
		if not covered.has(mb):
			missing.append(mb)
	print("[mpfb] MISSING_COVERAGE=%d" % missing.size())
	for m in missing:
		print("  MISSING " + m)
	print("[mpfb] pinned=%d delta=%d auth=%d wrist=%d held=%d" % [PINNED.size(), DELTA_MB.size(), AUTH_MB_TO_V.size(), HAND_DEFB.size(), mpfb_skel.get_bone_count() - PINNED.size() - DELTA_MB.size() - AUTH_MB_TO_V.size() - HAND_DEFB.size() - 1])
	for t in out.get_track_count():
		print("  [%d] %s" % [int(out.track_get_type(t)), out.track_get_path(t)])
	_free(ref_root, vern_root, mpfb_root)
	quit(0)

func _build_auth_map() -> void:
	for side in ["L", "R"]:
		AUTH_VERN_TO_MB["upper_arm." + side] = "upperarm01." + side
		AUTH_VERN_TO_MB["forearm." + side] = "lowerarm01." + side
		var fingers := {
			"thumb": "finger1", "index": "finger2", "middle": "finger3",
			"ring": "finger4", "little": "finger5",
		}
		for vname in fingers:
			for seg in ["1", "2", "3"]:
				AUTH_VERN_TO_MB[vname + "_" + seg + "." + side] = fingers[vname] + "-" + seg + "." + side
	for vb in AUTH_VERN_TO_MB:
		AUTH_MB_TO_V[AUTH_VERN_TO_MB[vb]] = vb

func _seat_cross_check(ap: AnimationPlayer, skel: Skeleton3D, seat_local: Dictionary) -> void:
	var anim: Animation = ap.get_animation(MPFB_SEAT_CLIP)
	if anim == null:
		dbg("[mpfb] no %s action; seat basis = skeleton rest (best-effort)" % MPFB_SEAT_CLIP)
		return
	var worst := 0.0
	var worst_bone := ""
	var dev_list: Array[String] = []
	for t in anim.get_track_count():
		if int(anim.track_get_type(t)) != TYPE_ROT:
			continue
		var bone := String(anim.track_get_path(t)).get_slice(":", 1)
		var qi := skel.find_bone(bone)
		if qi < 0:
			continue
		var val: Variant = _sample_q(_make_sampler(anim, t), 0.0)
		var rest_local: Quaternion = seat_local.get(bone, Quaternion.IDENTITY)
		# shortest-arc angle (handles the q/-q double cover, which get_angle() misreports as
		# 360 deg when the clip stored the negated quaternion despite being the same rotation)
		var dot := clampf(absf(rest_local.dot(val as Quaternion)), 0.0, 1.0)
		var ang := rad_to_deg(2.0 * acos(dot))
		if ang > worst:
			worst = ang
			worst_bone = bone
		if ang > 2.0:
			dev_list.append("%s %.1f" % [bone, ang])
	dbg("[mpfb] seat cross-check (rest vs %s t=0, worst %.2f deg on %s; >2deg: %s)" % [MPFB_SEAT_CLIP, worst, worst_bone, ", ".join(dev_list)])

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

func _free(a: Node, b: Node, c: Node) -> void:
	a.free()
	b.free()
	c.free()

# Builds per-bone Overwrite-Axis rewrite constants keyed by DEF-* name mapping into an
# MPFB target (`mb_map`). q' = nq^-1 * oq * q * orq^-1 * oq^-1 * nq * nrq, where o*/orq
# come from the reference skeleton's raw rest and n*/nrq from the (seated) MPFB rest.
func _build_rewrites(skel: Skeleton3D, bm_ref: BoneMap, mb_map: Dictionary) -> Dictionary:
	var profile := bm_ref.get_profile()
	var cnt := profile.get_bone_size()
	var prof_parent := {}
	var prof_rest_basis := {}
	for i in cnt:
		prof_parent[i] = profile.find_bone(profile.get_bone_parent(i))
		prof_rest_basis[i] = profile.get_reference_pose(i).basis
	var prof_g_basis := {}
	var prof_order := []
	var pending := []
	for i in cnt:
		if prof_parent[i] < 0:
			pending.append(i)
	var qi := 0
	while qi < pending.size():
		var i: int = pending[qi]
		qi += 1
		for c in cnt:
			if prof_parent[c] == i:
				pending.append(c)
		prof_order.append(i)
	for i in prof_order:
		prof_g_basis[i] = prof_rest_basis[i]
		var p: int = prof_parent[i]
		if p >= 0:
			prof_g_basis[i] = prof_g_basis[p] * prof_rest_basis[i]

	var raw_parent := {}
	var raw_children := {}
	var old_rest := {}
	var old_g := {}
	for i in skel.get_bone_count():
		raw_parent[i] = skel.get_bone_parent(i)
		raw_children[i] = []
		old_rest[i] = skel.get_bone_rest(i)
		old_g[i] = skel.get_bone_global_rest(i)
	for i in skel.get_bone_count():
		if raw_parent[i] >= 0:
			raw_children[raw_parent[i]].append(i)
	var raw_n := {}
	for i in skel.get_bone_count():
		raw_n[i] = String(skel.get_bone_name(i))
	var prof_to_skel := {}
	for i in cnt:
		var pn := String(profile.get_bone_name(i))
		var sn := String(bm_ref.get_skeleton_bone_name(pn))
		if not sn.is_empty():
			prof_to_skel[pn] = sn

	var children := {}
	for i in skel.get_bone_count():
		children[i] = []
	for i in skel.get_bone_count():
		if raw_parent[i] >= 0:
			children[raw_parent[i]].append(i)
	var skel_to_prof := {}
	for i in cnt:
		var pn := String(profile.get_bone_name(i))
		var sn := String(bm_ref.get_skeleton_bone_name(pn))
		if not sn.is_empty():
			skel_to_prof[sn] = pn
	var keep := []
	for i in skel.get_bone_count():
		if skel_to_prof.has(raw_n[i]):
			continue
		var found_mapped := false
		var dq: Array = children[i].duplicate()
		while dq.size() > 0:
			var d: int = dq.pop_front()
			if skel_to_prof.has(raw_n[d]):
				found_mapped = true
				break
			dq.append_array(children[d])
		if not found_mapped:
			keep.append(i)

	# new rests via BFS (mutates skel in place, same order as rest_fixer.cpp)
	var parentless := []
	for i in skel.get_bone_count():
		if raw_parent[i] < 0:
			parentless.append(i)
	var diffs_w := {}
	var order := parentless.duplicate()
	var qi2 := 0
	while qi2 < order.size():
		var i: int = order[qi2]
		qi2 += 1
		for c in raw_children[i]:
			order.push_back(c)
		var tgt_rot := Basis.IDENTITY
		var par: int = raw_parent[i]
		var src_pg := Basis.IDENTITY
		if par >= 0:
			src_pg = skel.get_bone_global_rest(par).basis
		var pn: String = skel_to_prof.get(raw_n[i], "")
		var pi := profile.find_bone(pn) if not pn.is_empty() else -1
		if not pn.is_empty() and pi >= 0:
			tgt_rot = src_pg.inverse() * prof_g_basis[pi]
		elif keep.has(i):
			tgt_rot = src_pg.inverse() * old_g[i].basis
		var rest := skel.get_bone_rest(i)
		if par >= 0:
			diffs_w[i] = tgt_rot.inverse() * diffs_w[par] * rest.basis
		else:
			diffs_w[i] = tgt_rot.inverse() * rest.basis
		var diff: Basis = diffs_w[par] if par >= 0 else Basis.IDENTITY
		skel.set_bone_rest(i, Transform3D(tgt_rot, diff * rest.origin))
	var new_rest := {}
	var new_g := {}
	for i in skel.get_bone_count():
		new_rest[i] = skel.get_bone_rest(i)
		new_g[i] = skel.get_bone_global_rest(i)

	var rw := {}
	for i in skel.get_bone_count():
		if not mb_map.has(raw_n[i]):
			continue
		var par: int = raw_parent[i]
		var old_pg_q: Quaternion = old_g[par].basis.get_rotation_quaternion() if par >= 0 else Quaternion.IDENTITY
		var new_pg_q: Quaternion = new_g[par].basis.get_rotation_quaternion() if par >= 0 else Quaternion.IDENTITY
		rw[raw_n[i]] = {
			"nq": new_pg_q,
			"oq": old_pg_q,
			"orq": old_rest[i].basis.get_rotation_quaternion(),
			"nrq": new_rest[i].basis.get_rotation_quaternion(),
		}
	return rw

# Builds rewrite constants for Vern-authored bones, keyed by MPFB bone name: old = Vern
# skeleton rest, new = MPFB skeleton rest (seated).
func _build_auth_rw(vs: Skeleton3D, ms: Skeleton3D, v2m: Dictionary) -> Dictionary:
	var rw := {}
	for vb in v2m:
		var mb: String = v2m[vb]
		var vi := vs.find_bone(vb)
		var mi := ms.find_bone(mb)
		if vi < 0 or mi < 0:
			continue
		var vpar := vs.get_bone_parent(vi)
		var oq: Quaternion = Quaternion(vs.get_bone_global_rest(vpar).basis) if vpar >= 0 else Quaternion.IDENTITY
		var orq: Quaternion = Quaternion(vs.get_bone_rest(vi).basis)
		var mpar := ms.get_bone_parent(mi)
		var nq: Quaternion = Quaternion(ms.get_bone_global_rest(mpar).basis) if mpar >= 0 else Quaternion.IDENTITY
		var nrq: Quaternion = Quaternion(ms.get_bone_rest(mi).basis)
		rw[mb] = { "nq": nq, "oq": oq, "orq": orq, "nrq": nrq }
	return rw

# Rewrites a raw clip rotation using the engine's exact float32 product order.
# Bone missing from rw -> identity passthrough.
func _rewrite(rw: Dictionary, key: String, q: Quaternion) -> Quaternion:
	if not rw.has(key):
		return q
	var r: Dictionary = rw[key]
	return r["nq"].inverse() * r["oq"] * q * r["orq"].inverse() * r["oq"].inverse() * r["nq"] * r["nrq"]
