extends SceneTree

# Production bake of Vern's "talk_calm" clip (ported from the approved V5 retarget spike).
#
# Transfers the CC0 reference library's "Sitting_Talking" body rotations (world-space,
# per-bone local deltas) onto Vern's native skeleton, fused with Vern-only bones (jaw,
# eyelid, grip) sampled from Vern's own talking_default. Arms/fingers are Vern-authored;
# the wrist is re-oriented thumb-up (180deg flip about the finger axis) and driven by the
# mannequin's talking-hand deltas for a natural "talking with his hands" read; a small
# static inward finger curl makes the resting hands read as relaxed/cupped.
#
# Self-contained: reads the RAW reference GLB (no import-time retarget needed) and maps its
# DEF-* bones to Vern's native names through a profile join of the two BoneMap resources
# (bone_map_ref.tres holds profile->DEF-*, bone_map_vern.tres holds profile->Vern). This is
# byte-faithful to V5 because the spike's retarget config was a pure bone rename (no custom
# rest fixer), so the sampled track values are identical.
#
# Output tracks are "VernRig/Skeleton3D:<native_bone>" for drop-in compatibility.
#
# Run:
#   Godot_v4.6-headless --path <repo> --script res://Tools/modelgen/bake_talk_calm.gd

const REF_GLB := "res://docs/references/Animation Library[Standard]/Godot/AnimationLibrary_Godot_Standard.glb"
const VERN_GLB := "res://assets/models3d/characters/vern/vern.glb"
const BM_REF := "res://Tools/modelgen/retarget/bone_map_ref.tres"
const BM_VERN := "res://Tools/modelgen/retarget/bone_map_vern.tres"
const REF_CLIP := "Sitting_Talking"
const VERN_CLIP := "talking_default"
const OUT_PATH := "res://assets/models3d/characters/vern/animations/talk_calm.tres"
const FPS := 24
const SKEL_PATH := "VernRig/Skeleton3D"
const LOG := "user://bake.log"

const TYPE_POS := 1
const TYPE_ROT := 2

func dbg(msg: String) -> void:
	var f := FileAccess.open(LOG, FileAccess.WRITE)
	f.store_line(msg)
	f.close()
	print(msg)

func _init() -> void:
	dbg("[bake] start")
	var ref_p := load(REF_GLB) as PackedScene
	dbg("[bake] ref scene loaded")
	var vern_p := load(VERN_GLB) as PackedScene
	dbg("[bake] vern scene loaded")
	var bm_ref := load(BM_REF) as BoneMap
	var bm_vern := load(BM_VERN) as BoneMap
	dbg("[bake] bone maps loaded: ref=%s vern=%s" % [bm_ref != null, bm_vern != null])
	if ref_p == null or vern_p == null or bm_ref == null or bm_vern == null:
		push_error("cannot load inputs")
		quit(1)
		return

	var ref_root := ref_p.instantiate()
	dbg("[bake] ref instantiated")
	var ref_skel := _find_skeleton(ref_root)
	var ref_ap := _find_ap(ref_root)
	var vern_root := vern_p.instantiate()
	dbg("[bake] vern instantiated")
	var vern_skel := _find_skeleton(vern_root)
	var vern_ap := _find_ap(vern_root)
	dbg("[bake] nodes found: ref_skel=%s ref_ap=%s vern_skel=%s vern_ap=%s" % [ref_skel != null, ref_ap != null, vern_skel != null, vern_ap != null])
	if ref_skel == null or ref_ap == null or vern_skel == null or vern_ap == null:
		push_error("missing nodes")
		_free(ref_root, vern_root)
		quit(1)
		return

	var ref_anim: Animation = ref_ap.get_animation(REF_CLIP)
	var vern_anim: Animation = vern_ap.get_animation(VERN_CLIP)
	dbg("[bake] clips: ref=%s vern=%s" % [ref_anim != null, vern_anim != null])
	if ref_anim == null or vern_anim == null:
		push_error("missing clips")
		_free(ref_root, vern_root)
		quit(1)
		return

	# build the profile-join map: DEF-* (reference) -> Vern native bone name
	var profile := bm_vern.get_profile()
	dbg("[bake] profile bones: %d" % profile.get_bone_size())
	var def_vern := {}
	for i in profile.get_bone_size():
		var pb := String(profile.get_bone_name(i))
		var src := String(bm_ref.get_skeleton_bone_name(StringName(pb)))
		var vb := String(bm_vern.get_skeleton_bone_name(StringName(pb)))
		if not vb.is_empty() and not src.is_empty():
			def_vern[src] = vb

	# Vern native bone name -> profile name (only mapped bones); used to keep the
	# vern-only tracks (jaw/eyelid) out of the body passthrough and into their own copy.
	var vern_profile := {}
	for i in profile.get_bone_size():
		var pb := String(profile.get_bone_name(i))
		var vb := String(bm_vern.get_skeleton_bone_name(StringName(pb)))
		if not vb.is_empty():
			vern_profile[vb] = pb

	# Vern native bone name -> DEF-* reference bone name (reverse of def_vern)
	var vern_def := {}
	for src in def_vern:
		vern_def[def_vern[src]] = src

	# Per-bone rewrite constants, keyed by DEF-* bone name, reproducing Godot's Overwrite-Axis
	# rest fixer (reimpl_validate.gd proves this against the actual retargeted oracle clip).
	# The retarget is NOT a pure rename for the reference skeleton: sampled ROT values are
	# l*q*r with non-identity per-bone constants (up to 308 deg of parent-basis change on the
	# fingers), so V5's output (= oracle-rewritten values) differs from the raw clip and the
	# bake must apply the same rewrite to be byte-faithful.
	var rw := _build_rewrites(ref_skel, bm_ref, def_vern)

	# body rotation samplers from the RAW reference clip, keyed by DEF-* bone name
	var body_samples := {}
	for t in ref_anim.get_track_count():
		if int(ref_anim.track_get_type(t)) != TYPE_ROT:
			continue
		var bone := String(ref_anim.track_get_path(t)).get_slice(":", 1)
		if not def_vern.has(bone):
			continue
		body_samples[bone] = _make_sampler(ref_anim, t)
	dbg("[bake] body samplers: %d" % body_samples.size())

	# vern-only samplers (jaw, eyelid, grip, and any rest-region tracks Vern animates)
	var vern_only_tracks := []
	for t in vern_anim.get_track_count():
		var vb := String(vern_anim.track_get_path(t)).get_slice(":", 1)
		if vern_profile.has(vb):
			continue
		if vern_skel.find_bone(vb) < 0:
			continue
		vern_only_tracks.append({
			"bone": vb,
			"type": int(vern_anim.track_get_type(t)),
			"sam": _make_sampler(vern_anim, t),
		})
	dbg("[bake] vern-only tracks: %d" % vern_only_tracks.size())

	# ----- bake frame loop -----
	var length := ref_anim.get_length()
	var frames := int(ceil(length * FPS))
	var out := Animation.new()
	out.length = length
	out.step = 1.0 / FPS
	out.loop_mode = Animation.LOOP_LINEAR

	var rot_track := {}	# vern bone name -> track idx

	# vern bone local rest rotation (identity pose), so we can pre-multiply deltas
	var vern_local_rest := {}
	for vb in vern_profile:
		vern_local_rest[vb] = Quaternion(vern_skel.get_bone_rest(vern_skel.find_bone(vb)).basis)

	# bones that must stay pinned to Vern's seated rest (Vern sits behind a desk).
	# Per-bone local deltas from the mannequin's leg articulation (sit-in, weight shift)
	# would slide the feet, so the leg chain + root/pelvis are clamped to rest.
	var PINNED := ["root", "pelvis", "thigh.L", "thigh.R", "shin.L", "shin.R", "foot.L", "foot.R"]

	# bones driven from Vern's own authored talking_default instead of the mannequin.
	# The mannequin's Sitting_Talking arms are elevated/gesturing; rebasing its arm deltas
	# onto Vern's down-hanging rest arms produces wide unnatural sweeps. Vern's authored
	# clip animates the arms and hands subtly, so sourcing this region from Vern reads
	# naturally. Presence here overrides the mannequin routes (hand bones get a rolled
	# thumb-up rest + mannequin wrist gesture via the dedicated branch).
	var VERN_AUTHORED := []
	for side in ["L", "R"]:
		VERN_AUTHORED.append("upper_arm." + side)
		VERN_AUTHORED.append("forearm." + side)
		VERN_AUTHORED.append("hand." + side)
		for finger in ["thumb", "index", "middle", "ring", "little"]:
			for seg in ["1", "2", "3"]:
				VERN_AUTHORED.append(finger + "_" + seg + "." + side)

	# samplers for authored bones pulled straight from Vern's clip (ROT tracks only; an
	# absent track, e.g. arms, means "hold Vern rest" -> no arm animation at all).
	var vern_auth_samplers := {}
	for t in vern_anim.get_track_count():
		if int(vern_anim.track_get_type(t)) != TYPE_ROT:
			continue
		var vb := String(vern_anim.track_get_path(t)).get_slice(":", 1)
		if VERN_AUTHORED.has(vb) and vern_skel.find_bone(vb) >= 0:
			vern_auth_samplers[vb] = _make_sampler(vern_anim, t)

	# choose a loop window of Vern's talking_default where the authored fingers are most
	# active, so the baked clip samples Vern's liveliest hand language. talking_default is
	# 8s and not cyclic habitually; the mannequin clip is cyclic at its own length, so only
	# the authored (finger) region needs this. The SEAM_BLEND ease-back below keeps the wrap
	# seamless for ANY window, so we are free to maximize finger travel rather than minimize
	# end-to-start mismatch (which previously found the flattest, deadest finger region).
	var AUTHORED_WINDOW := 0.0
	# amplification of Vern's authored finger deltas around their window-start pose
	# (slerp extrapolation; >1 makes his natural motion visibly livelier, same rest shape).
	var FINGER_GAIN := 1.8
	# seconds at the end of the clip over which authored finger values ease back to their
	# window-start pose, so the wrap is seamless even where no perfectly cyclic window exists.
	var SEAM_BLEND := 0.4
	# re-orient Vern's resting hand so the thumb points up. Vern's resting hands hang down
	# (origin y~0.98, fingers pointing down), so a 180deg roll about each hand's local finger
	# axis flips the thumb from pointing down to pointing up. 180deg is self-mirrored, so the
	# same local roll yields the mirrored world pose on each hand. Kept per-side so the two
	# hands can be tuned independently if Vern's rest skeleton proves asymmetric.
	var HAND_ROLL_DEG_L := 180.0
	var HAND_ROLL_DEG_R := 180.0
	# how much of the mannequin's talking-hand wrist gesture (DEF-hand.L/DEF-hand.R deltas) to
	# apply onto Vern's re-oriented wrist. Applied via quaternion exponentiation so values
	# above 1.0 amplify the mannequin's swing. 0 = no wrist gesture (hand holds rolled rest).
	var WRIST_GAIN := 1.2
	# static inward finger curl, one angle per finger segment (1=proximal ... 3=distal),
	# applied in each segment's own local space about its +X knuckle axis. Positive X-rotation
	# moves the fingertip toward the palm normal on both hands (verified via rest-pose probe),
	# so the resting hand reads as slightly cupped and the fingers animate more naturally.
	var FINGER_CURL_DEG := { "1": 5.0, "2": 8.0, "3": 10.0 }
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
		dbg("[bake] authored window offset %.4f tot=%.4f rad" % [AUTHORED_WINDOW, best_score])

	# first-frame LOCAL rotation per DEF-* bone (clip's starting pose). Subtracting it
	# per-bone transfers only each bone's talking-motion delta, never the mannequin's
	# static stance/lean, so Vern stays in its own seated rest with feet planted.
	# Values are the REST-FIXER-REWRITTEN rotations (rw), matching V5's oracle clip.
	var local0 := {}
	for defb in body_samples:
		local0[defb] = _rewrite(rw, defb, _sample_q(body_samples[defb], 0.0))

	# rotation that turns Vern's downward-hanging resting hand thumb-up, applied in hand-local
	# space about the finger axis; 180deg is self-mirrored so the mirrored R hand receives the
	# mirrored world pose (thumb up on both hands).
	var hand_roll_q := {
		"hand.L": Quaternion(Vector3(0, 1, 0), deg_to_rad(HAND_ROLL_DEG_L)),
		"hand.R": Quaternion(Vector3(0, 1, 0), deg_to_rad(HAND_ROLL_DEG_R)),
	}
	var hand_defb := {
		"hand.L": "DEF-hand.L",
		"hand.R": "DEF-hand.R",
	}

	for f in frames:
		var t := (f + 0.5) / FPS
		if t > length:
			t = length

		for vb in vern_profile:
			var local: Quaternion
			if vb in PINNED:
				local = vern_local_rest[vb]
			elif vb == "hand.L" or vb == "hand.R":
				# re-oriented wrist rest (thumb up) + the mannequin's talking-hand gesture,
				# so Vern gestures with his hands while he speaks. The mannequin clip is cyclic
				# at its own length, so rebased deltas wrap cleanly; final clamp re-locks the loop.
				var base: Quaternion = vern_local_rest[vb] * hand_roll_q[vb]
				if body_samples.has(hand_defb[vb]):
					var ref_local: Quaternion = _rewrite(rw, hand_defb[vb], _sample_q(body_samples[hand_defb[vb]], t))
					var delta: Quaternion = ref_local * local0.get(hand_defb[vb], Quaternion.IDENTITY).inverse()
					# amplify the swing angle by WRIST_GAIN (axis-angle exponentiation):
					# WRIST_GAIN=1.0 uses the mannequin's exact swing, >1.0 amplifies it,
					# keeping the rolled rest pose as the zero point. Guard the tiny-angle
					# case (noisy axis from float error, and nothing to amplify anyway).
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
			elif VERN_AUTHORED.has(vb):
				# Vern's own authored arm/finger motion sampled straight from its clip,
				# offset by the chosen loop window so the wrap doesn't snap the hands.
				# Bones Vern does not animate (arms: only static SCL exists) hold rest.
				# Over the final SEAM_BLEND seconds, authored rotations ease back toward
				# their window-start pose so the loop boundary is seamless even though
				# talking_default is not perfectly periodic for the fingers.
				if vern_auth_samplers.has(vb):
					var local_start: Quaternion = _sample_q(vern_auth_samplers[vb], AUTHORED_WINDOW)
					local = _sample_q(vern_auth_samplers[vb], AUTHORED_WINDOW + t)
					# amplify Vern's authored finger motion around its window-start pose
					local = local_start.slerp(local, FINGER_GAIN)
					var u := clampf((t - (length - SEAM_BLEND)) / SEAM_BLEND, 0.0, 1.0)
					if u > 0.0:
						local = local.slerp(local_start, u)
					# static inward finger curl (cupped resting hand), applied in each
					# segment's own local space so it stays mirrored and reads naturally
					var seg := String(vb).get_slice("_", 1).get_slice(".", 0)
					if FINGER_CURL_DEG.has(seg):
						local = local * Quaternion(Vector3(1, 0, 0), deg_to_rad(FINGER_CURL_DEG[seg]))
				else:
					local = vern_local_rest[vb]
			else:
				var defb: String = vern_def.get(vb, "")
				var ref_local: Quaternion = _rewrite(rw, defb, _sample_q(body_samples.get(defb), t)) if body_samples.has(defb) else Quaternion.IDENTITY
				var delta: Quaternion = ref_local * local0.get(defb, Quaternion.IDENTITY).inverse()
				local = vern_local_rest[vb] * delta

			if f == 0:
				rot_track[vb] = out.add_track(TYPE_ROT)
				out.track_set_path(rot_track[vb], NodePath(SKEL_PATH + ":" + vb))
				out.track_set_interpolation_type(rot_track[vb], Animation.INTERPOLATION_LINEAR)
			out.track_insert_key(rot_track[vb], t, local)

		# 3) vern-only bones
		for track in vern_only_tracks:
			var val: Variant = _sample(track["sam"], t)
			if f == 0:
				var ti := out.add_track(track["type"])
				out.track_set_path(ti, NodePath(SKEL_PATH + ":" + track["bone"]))
				out.track_set_interpolation_type(ti, Animation.INTERPOLATION_LINEAR)
				rot_track[track["bone"]] = ti
			out.track_insert_key(rot_track[track["bone"]], t, val)

	# loop continuity: clamp each track's final key to equal its first so the wrap is seamless
	for ti in out.get_track_count():
		var kc := out.track_get_key_count(ti)
		if kc >= 2:
			out.track_set_key_value(ti, kc - 1, out.track_get_key_value(ti, 0))

	DirAccess.make_dir_recursive_absolute(OUT_PATH.get_base_dir())
	var err := ResourceSaver.save(out, OUT_PATH)
	if err != OK:
		push_error("save failed: " + str(err))
		_free(ref_root, vern_root)
		quit(1)
		return

	print("=== BAKED %s (from %s) ===" % [OUT_PATH, REF_CLIP])
	print("LENGTH=%.3f FRAMES=%d FPS=%d" % [out.length, frames, int(FPS)])
	print("TRACKS=%d BODY=%d VERNFACE=%d" % [out.get_track_count(), vern_profile.size(), vern_only_tracks.size()])
	for t in out.get_track_count():
		print("  [%d] %s" % [int(out.track_get_type(t)), out.track_get_path(t)])
	_free(ref_root, vern_root)
	quit(0)

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

func _free(a: Node, b: Node) -> void:
	a.free()
	b.free()

# Builds per-bone Overwrite-Axis rewrite constants (see reimpl_validate.gd for the
# derivation and ULP validation). For each reference bone keyed by DEF-* name that maps
# into `vern_def`, returns the basis quats needed to rewrite a raw clip rotation:
#   q' = nq^-1 * oq * q * orq^-1 * oq^-1 * nq * nrq
# where oq = old parent global rest quat, nq = new (profile) parent global rest quat,
# orq = old rest quat, nrq = new rest quat. Returns {defb: {nq,oq,orq,nrq}}.
func _build_rewrites(skel: Skeleton3D, bm_ref: BoneMap, vern_def: Dictionary) -> Dictionary:
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

	# raw skeleton: parent/children, old rest + global rest
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
		if not vern_def.has(raw_n[i]):
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

# Rewrites a raw clip rotation for a DEF-* bone using the engine's exact float32 product
# order (see reimpl_validate.gd `_apply`). defb missing from rw -> identity passthrough.
func _rewrite(rw: Dictionary, defb: String, q: Quaternion) -> Quaternion:
	if not rw.has(defb):
		return q
	var r: Dictionary = rw[defb]
	return r["nq"].inverse() * r["oq"] * q * r["orq"].inverse() * r["oq"].inverse() * r["nq"] * r["nrq"]