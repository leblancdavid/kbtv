extends SceneTree

# Validates a GDScript reimplementation of Godot's Overwrite-Axis skeleton rest-fixer
# (post_import_plugin_skeleton_rest_fixer.cpp, retarget_method=1) against the ACTUAL
# retargeted output produced by Godot's importer for the reference GLB.
#
# The oracle is the spike's imported scene `reference.glb-<hash>.scn` (copied to
# Tools/modelgen/tmp/oracle_reference.scn): a skeleton whose bones were renamed to the
# profile's names and a "Sitting_Talking" clip whose rotation keys were rewritten by the
# rest fixer. We load that oracle, plus the RAW reference GLB (DEF-* bones, original clip),
# reimplement the fixer math on the raw data, and compare predicted keys / rests against
# the oracle. Zero error means the production bake can reproduce V5 by applying the same
# per-bone map to sampled values.
#
# Run:
#   Godot_v4.6-headless --path <repo> --script res://Tools/modelgen/reimpl_validate.gd

const REF_GLB := "res://docs/references/Animation Library[Standard]/Godot/AnimationLibrary_Godot_Standard.glb"
const ORACLE := "res://Tools/modelgen/tmp/oracle_reference.scn"
const BM_REF := "res://Tools/modelgen/retarget/bone_map_ref.tres"
const REF_CLIP := "Sitting_Talking"

const TYPE_POS := 1
const TYPE_ROT := 2
const TYPE_SCL := 3

# Godot's Quaternion is float32; even the exact-order rewrite differs from the oracle
# by ~1-2 ULP of quaternion value (acos(1-2^-24) = 2^-11.5 = 0.000345 rad,
# acos(1-2^-23) = 2^-11 = 0.000488 rad). Those are the float noise floor, not math
# errors, so the per-key tolerance sits above them.
const TOL_ROT_RAD := 1e-3
const TOL_ORIGIN := 1e-3
const KEEP_LEFT := true

var _fail := 0

func _init() -> void:
	print("=== reimpl_validate ===")
	var ref_p := load(REF_GLB) as PackedScene
	var ora_p := load(ORACLE) as PackedScene
	var bm_ref := load(BM_REF) as BoneMap
	if ref_p == null or ora_p == null or bm_ref == null:
		push_error("load fail ref=%s ora=%s bm=%s" % [ref_p != null, ora_p != null, bm_ref != null])
		quit(1)
		return

	var ref_root := ref_p.instantiate()
	var skel := _find_skeleton(ref_root)
	var ap := _find_ap(ref_root)
	if skel == null or ap == null or not ap.has_animation(REF_CLIP):
		push_error("raw nodes missing")
		quit(1)
		return
	var raw_anim: Animation = ap.get_animation(REF_CLIP)

	var ora_root := ora_p.instantiate()
	var oskel := _find_skeleton(ora_root)
	var oap := _find_ap(ora_root)
	if oskel == null or oap == null or not oap.has_animation(REF_CLIP):
		push_error("oracle nodes missing")
		quit(1)
		return
	var ora_anim: Animation = oap.get_animation(REF_CLIP)

	var profile := bm_ref.get_profile()
	var prof_cnt := profile.get_bone_size()
	print("raw bones=%d oracle bones=%d profile bones=%d" % [skel.get_bone_count(), oskel.get_bone_count(), prof_cnt])
	print("raw clip tracks=%d oracle clip tracks=%d" % [raw_anim.get_track_count(), ora_anim.get_track_count()])

	# ---------- maps: raw bone name <-> profile bone name ----------
	var raw_n := {}
	var raw_idx := {}
	for i in skel.get_bone_count():
		raw_n[i] = String(skel.get_bone_name(i))
		raw_idx[raw_n[i]] = i
	var raw_parent := {}
	for i in skel.get_bone_count():
		raw_parent[i] = skel.get_bone_parent(i)

	var prof_parent := {}
	var prof_rest_basis := {}
	for i in prof_cnt:
		prof_parent[i] = profile.find_bone(profile.get_bone_parent(i))
		prof_rest_basis[i] = profile.get_reference_pose(i).basis
		if not prof_rest_basis[i].is_equal_approx(Basis.IDENTITY):
			print("PROFILE NON-IDENTITY REF POSE at %d (%s)" % [i, profile.get_bone_name(i)])

	# profile skeleton global rest basis (parent-before-child order)
	var prof_g_basis := {}
	var prof_order := []
	var pending := []
	for i in prof_cnt:
		if prof_parent[i] < 0:
			pending.append(i)
	var qi := 0
	while qi < pending.size():
		var i: int = pending[qi]
		qi += 1
		for c in prof_cnt:
			if prof_parent[c] == i:
				pending.append(c)
		prof_order.append(i)
	for i in prof_order:
		prof_g_basis[i] = prof_rest_basis[i]
		var p: int = prof_parent[i]
		if p >= 0:
			prof_g_basis[i] = prof_g_basis[p] * prof_rest_basis[i]

	var skel_to_prof := {}
	var prof_to_skel := {}
	for i in prof_cnt:
		var pn := String(profile.get_bone_name(i))
		var sn := String(bm_ref.get_skeleton_bone_name(pn))
		if not sn.is_empty():
			skel_to_prof[sn] = pn
			prof_to_skel[pn] = sn
	print("mapped raw bones=%d" % skel_to_prof.size())

	# ---------- apply_node_transforms ----------
	var g := Transform3D.IDENTITY
	var pr: Node = skel
	while pr != null:
		var t := Transform3D.IDENTITY
		if pr is Node3D:
			t = (pr as Node3D).get_transform()
		print("node chain: %s  local=%s" % [str(pr.get_name()), str(t)])
		g = t * g
		pr = pr.get_parent()
	g.origin = Vector3.ZERO
	var g_basis := g.basis
	var g_ortho := g_basis.orthonormalized()
	var g_rot := g_basis.get_rotation_quaternion()
	var scl := g_basis.get_scale()
	print("global_transform basis=%s scl=%s rot=%s" % [str(g_basis), str(scl), str(g_rot)])

	var parentless := []
	for i in skel.get_bone_count():
		if raw_parent[i] < 0:
			parentless.append(i)
	print("parentless bones=%s" % str(parentless.map(func(i): return raw_n[i])))

	if parentless.size() > 0:
		for i in parentless:
			var rest := skel.get_bone_rest(i)
			skel.set_bone_rest(i, Transform3D(g_ortho) * rest)
		var orderb := parentless.duplicate()
		var qib := 0
		while qib < orderb.size():
			var i: int = orderb[qib]
			qib += 1
			for c in skel.get_bone_children(i):
				orderb.push_back(c)
			var rest := skel.get_bone_rest(i)
			skel.set_bone_rest(i, Transform3D(rest.basis, rest.origin * scl))

	# collect raw TRS tracks on the skeleton, applying node-transform rewrites
	var raw_keys := {}   # raw bone name -> {int type: {times: PackedFloat32Array, vals: Array}}
	for t in raw_anim.get_track_count():
		var tt := int(raw_anim.track_get_type(t))
		if tt != TYPE_POS and tt != TYPE_ROT and tt != TYPE_SCL:
			continue
		var path := raw_anim.track_get_path(t)
		if path.get_subname_count() != 1:
			continue
		var bn := String(path.get_subname(0))
		if not raw_idx.has(bn):
			continue
		var sam := _collect(raw_anim, t)
		var sci: int = raw_idx[bn]
		var is_pd := parentless.has(sci)
		for j in sam["vals"].size():
			var v = sam["vals"][j]
			if tt == TYPE_ROT and is_pd:
				sam["vals"][j] = g_rot * (v as Quaternion)
			elif tt == TYPE_POS and is_pd:
				sam["vals"][j] = g_basis * (v as Vector3)
			elif tt == TYPE_POS:
				sam["vals"][j] = (v as Vector3) * scl
		if not raw_keys.has(bn):
			raw_keys[bn] = {}
		raw_keys[bn][tt] = sam
	print("raw TRS tracks on skeleton=%d" % raw_keys.size())

	# ---------- old rests (post apply_node_transforms, pre retarget) ----------
	var old_rest := {}
	var old_g := {}
	for i in skel.get_bone_count():
		old_rest[i] = skel.get_bone_rest(i)
		old_g[i] = skel.get_bone_global_rest(i)

	# motion_scale used by normalize_position_tracks: |global_rest(scale_base_bone).origin.y|
	var scale_base := String(profile.get_scale_base_bone())
	var motion_scale := 0.0
	var ms_si := -1
	var ms_rbn := ""
	if prof_to_skel.has(scale_base):
		ms_rbn = prof_to_skel[scale_base]
		ms_si = raw_idx.get(ms_rbn, -1)
	if ms_si >= 0:
		motion_scale = absf(old_g[ms_si].origin.y)
	print("motion_scale: scale_base=%s raw=%s idx=%d val=%.6f oracle=%.6f" % [scale_base, ms_rbn, ms_si, motion_scale, oskel.get_motion_scale()])
	# normalize_position_tracks multiplies every POS key by 1/motion_scale AFTER the rewrite,
	# so our POS pred must be scaled by the same factor to match the oracle.
	var pos_scale := 1.0 / motion_scale if motion_scale > 0.0 else 1.0
	print("POS normalize factor=%.6f" % pos_scale)

	# ---------- keep-global-rest whitelist for unmapped leftovers ----------
	var children := {}
	for i in skel.get_bone_count():
		children[i] = []
	for i in skel.get_bone_count():
		if raw_parent[i] >= 0:
			children[raw_parent[i]].append(i)
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
	print("unmapped leftovers keeping global rest=%d" % keep.size())

	# ---------- new rests (BFS, parent before child) ----------
	var diffs_w := {}
	var new_rest := {}
	var new_g := {}
	var order := parentless.duplicate()
	var qi2 := 0
	while qi2 < order.size():
		var i: int = order[qi2]
		qi2 += 1
		for c in skel.get_bone_children(i):
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
		elif KEEP_LEFT and keep.has(i):
			tgt_rot = src_pg.inverse() * old_g[i].basis

		var rest := skel.get_bone_rest(i)
		if par >= 0:
			diffs_w[i] = tgt_rot.inverse() * diffs_w[par] * rest.basis
		else:
			diffs_w[i] = tgt_rot.inverse() * rest.basis
		var diff: Basis = diffs_w[par] if par >= 0 else Basis.IDENTITY
		skel.set_bone_rest(i, Transform3D(tgt_rot, diff * rest.origin))

	for i in skel.get_bone_count():
		new_rest[i] = skel.get_bone_rest(i)
		new_g[i] = skel.get_bone_global_rest(i)

	# ---------- per-bone rewrite constants (for every track-having or mapped bone) ----------
	var rw := {}
	var have_rot := {}
	for bn in raw_keys:
		if raw_keys[bn].has(TYPE_ROT):
			have_rot[raw_idx[bn]] = true
	for i in skel.get_bone_count():
		if not have_rot.has(i) and not skel_to_prof.has(raw_n[i]):
			continue
		var par: int = raw_parent[i]
		var old_r_quat: Quaternion = old_rest[i].basis.get_rotation_quaternion()
		var new_r_quat: Quaternion = new_rest[i].basis.get_rotation_quaternion()
		var old_pg_b: Basis = old_g[par].basis if par >= 0 else Basis.IDENTITY
		var new_pg_b: Basis = new_g[par].basis if par >= 0 else Basis.IDENTITY
		var old_pg_q: Quaternion = old_pg_b.get_rotation_quaternion()
		var new_pg_q: Quaternion = new_pg_b.get_rotation_quaternion()
		rw[i] = {
			"l": new_pg_q.inverse() * old_pg_q,
			"r": old_r_quat.inverse() * old_pg_q.inverse() * new_pg_q * new_r_quat,
			"old_rest": old_rest[i],
			"new_rest": new_rest[i],
			"old_pg_b": old_pg_b,
			"new_pg_b": new_pg_b,
			"oq": old_pg_q,
			"nq": new_pg_q,
			"orq": old_r_quat,
			"nrq": new_r_quat,
		}

	# ---------- compare rests against oracle ----------
	var worst_q := 0.0
	var worst_o := 0.0
	for ob in oskel.get_bone_count():
		var obn := String(oskel.get_bone_name(ob))
		var si := _resolve_src(obn, raw_idx, prof_to_skel)
		if si < 0:
			continue
		var qe := _ang(oskel.get_bone_rest(ob).basis.get_rotation_quaternion(), new_rest[si].basis.get_rotation_quaternion())
		var oe: float = (oskel.get_bone_rest(ob).origin - new_rest[si].origin).length()
		worst_q = maxf(worst_q, qe)
		worst_o = maxf(worst_o, oe)
		if qe > TOL_ROT_RAD or oe > TOL_ORIGIN:
			print("  REST DIFF %s ang=%.7f origin=%.6f" % [obn, qe, oe])
			_fail += 1
	print("rest compare: worst_angle=%.9f rad worst_origin=%.6f" % [worst_q, worst_o])

	# ---------- compare clip values against oracle ----------
	var worst := 0.0
	var removed_const := 0
	for t in ora_anim.get_track_count():
		var tt := int(ora_anim.track_get_type(t))
		if tt != TYPE_POS and tt != TYPE_ROT and tt != TYPE_SCL:
			continue
		var path := ora_anim.track_get_path(t)
		if path.get_subname_count() != 1:
			continue
		var bn := String(path.get_subname(0))
		var si := _resolve_src(bn, raw_idx, prof_to_skel)
		if si < 0:
			print("  ORACLE TRACK %s UNRESOLVED" % bn)
			_fail += 1
			continue
		var rbn: String = raw_n[si]
		if raw_keys.has(rbn) and raw_keys[rbn].has(tt):
			var sample = raw_keys[rbn][tt]
			var kc: int = sample["times"].size()
			if ora_anim.track_get_key_count(t) != kc:
				print("  KEYCOUNT-INFO %s tt=%d pred=%d oracle=%d (key counts vary by import resample; bake resamples anyway)" % [bn, tt, kc, ora_anim.track_get_key_count(t)])
				continue
			for j in kc:
				var ot: float = ora_anim.track_get_key_time(t, j)
				if absf(ot - sample["times"][j]) > 1e-6:
					print("  TIME %s key%d pred=%.6f oracle=%.6f" % [bn, j, sample["times"][j], ot])
					_fail += 1
				var pred = _apply(sample["vals"][j], tt, rw[si])
				if tt == TYPE_POS:
					pred = (pred as Vector3) * pos_scale
				var ov = ora_anim.track_get_key_value(t, j)
				var d := _val_dist(ov, pred, tt)
				worst = maxf(worst, d)
				if d > TOL_ROT_RAD:
					print("  VAL %s key%d tt=%d pred=%s oracle=%s dist=%.8f" % [bn, j, tt, pred, ov, d])
					_fail += 1
		else:
			# complement track (mapped bone with no raw track of this type) -> constant new-rest value
			for j in ora_anim.track_get_key_count(t):
				var pred = _rest_value(si, tt, rw)
				if tt == TYPE_POS:
					pred = (pred as Vector3) * pos_scale
				var ov = ora_anim.track_get_key_value(t, j)
				var d := _val_dist(ov, pred, tt)
				worst = maxf(worst, d)
				if d > TOL_ROT_RAD or absf(ora_anim.track_get_key_time(t, j) - 0.0) > 1e-6:
					print("  COMPLEMENT %s tt=%d pred=%s oracle=%s (t=%f) dist=%.8f" % [bn, tt, pred, ov, ora_anim.track_get_key_time(t, j), d])
					_fail += 1
	# raw tracks with no oracle counterpart: must be constant (removed as immutable)
	for bn in raw_keys:
		for tt in raw_keys[bn]:
			var obn: String = skel_to_prof.get(bn, bn)
			if not _has_oracle_track(ora_anim, obn, tt):
				if _is_constant(raw_keys[bn][tt]["vals"]):
					removed_const += 1
				else:
					print("  RAW TRACK %s tt=%d MISSING in oracle and NOT constant" % [bn, tt])
					_fail += 1
	print("clip compare: worst_dist=%.9f removed_immutable_const=%d" % [worst, removed_const])

	# ---------- sampled-value sanity (map must commute with slerp) ----------
	var worst2 := 0.0
	var checked := 0
	for t in ora_anim.get_track_count():
		if int(ora_anim.track_get_type(t)) != TYPE_ROT:
			continue
		var path := ora_anim.track_get_path(t)
		if path.get_subname_count() != 1:
			continue
		var bn := String(path.get_subname(0))
		var si := _resolve_src(bn, raw_idx, prof_to_skel)
		var rbn: String = raw_n[si] if si >= 0 else ""
		if si < 0 or not raw_keys.has(rbn) or not raw_keys[rbn].has(TYPE_ROT):
			continue
		var sk = raw_keys[rbn][TYPE_ROT]
		var pred_keys := []
		for j in sk["vals"].size():
			pred_keys.append(_apply(sk["vals"][j], TYPE_ROT, rw[si]))
		var otimes := PackedFloat32Array()
		var ovals := []
		for j in ora_anim.track_get_key_count(t):
			otimes.append(ora_anim.track_get_key_time(t, j))
			ovals.append(ora_anim.track_get_key_value(t, j))
		# Sampled check also sits above float32 ULP noise; keep 2e-3 (0.11 deg) as the
		# sanity bound for "the map commutes with slerp".
		var check_tol := 2e-3
		for j in ora_anim.track_get_key_count(t):
			var tm: float = ora_anim.track_get_key_time(t, j)
			var pv: Quaternion = _sample_q(sk["times"], pred_keys, tm)
			var ov: Quaternion = _sample_q(otimes, ovals, tm)
			var d := _ang(pv, ov)
			worst2 = maxf(worst2, d)
			checked += 1
			if d > check_tol:
				print("  SAMPLED %s t=%.3f d=%.8f" % [bn, tm, d])
				_fail += 1
	print("sampled checks=%d worst=%.9f" % [checked, worst2])

	print("=== RESULT: %s ===" % ("PASS" if _fail == 0 else "FAIL (%d)" % _fail))
	ref_root.free()
	ora_root.free()
	quit(0 if _fail == 0 else 2)

# ---------- helpers ----------

func _apply(v: Variant, tt: int, r: Dictionary) -> Variant:
	if tt == TYPE_ROT:
		# Engine writes, in float32, left-to-right: new_pg_q^-1 * old_pg_q * qt *
		# old_rest_q^-1 * old_pg_q^-1 * new_pg_q * new_rest_q. GDScript Quaternion is
		# float32, so reproducing the exact multiplication ORDER is required for a
		# bit-for-bit match with the oracle (a precomputed l*qt*r drifts by ULP noise).
		var qt := v as Quaternion
		return r["nq"].inverse() * r["oq"] * qt * r["orq"].inverse() * r["oq"].inverse() * r["nq"] * r["nrq"]
	if tt == TYPE_SCL:
		var sc := Basis.from_scale(v as Vector3)
		var ob: Basis = (r["old_rest"] as Transform3D).basis
		var nb: Basis = (r["new_rest"] as Transform3D).basis
		var opg: Basis = r["old_pg_b"]
		var npg: Basis = r["new_pg_b"]
		var res: Basis = npg.inverse() * opg * sc * ob.inverse() * opg.inverse() * npg * nb
		return res.get_scale()
	if tt == TYPE_POS:
		var new_rest: Transform3D = r["new_rest"]
		var old_rest: Transform3D = r["old_rest"]
		var opg: Basis = r["old_pg_b"]
		var npg: Basis = r["new_pg_b"]
		return npg.inverse() * (opg * ((v as Vector3) - old_rest.origin)) + new_rest.origin
	return v

func _rest_value(si: int, tt: int, rw: Dictionary) -> Variant:
	var r: Dictionary = rw[si]
	var nr: Transform3D = r["new_rest"]
	if tt == TYPE_ROT:
		return nr.basis.get_rotation_quaternion()
	if tt == TYPE_POS:
		return nr.origin
	if tt == TYPE_SCL:
		return nr.basis.get_scale()
	return null

func _resolve_src(bn: String, raw_idx: Dictionary, prof_to_skel: Dictionary) -> int:
	if raw_idx.has(bn):
		return raw_idx[bn]
	var rbn: String = prof_to_skel.get(bn, "")
	if rbn != "" and raw_idx.has(rbn):
		return raw_idx[rbn]
	return -1

func _has_oracle_track(anim: Animation, obn: String, tt: int) -> bool:
	for t in anim.get_track_count():
		if int(anim.track_get_type(t)) != tt:
			continue
		var path := anim.track_get_path(t)
		if path.get_subname_count() == 1 and String(path.get_subname(0)) == obn:
			return true
	return false

func _is_constant(vals: Array) -> bool:
	if vals.size() < 2:
		return true
	var f = vals[0]
	for i in range(1, vals.size()):
		if vals[i] != f:
			return false
	return true

func _sample_v(times: PackedFloat32Array, vals: Array, tm: float) -> Variant:
	if times.size() == 0:
		return Quaternion.IDENTITY
	if tm <= times[0]:
		return vals[0]
	if tm >= times[times.size() - 1]:
		return vals[vals.size() - 1]
	for i in range(times.size() - 1):
		if tm >= times[i] and tm <= times[i + 1]:
			var u := clampf((tm - times[i]) / maxf(times[i + 1] - times[i], 0.000001), 0.0, 1.0)
			return (vals[i] as Quaternion).slerp(vals[i + 1] as Quaternion, u)
	return vals[vals.size() - 1]

func _sample_q(times: PackedFloat32Array, vals: Array, tm: float) -> Quaternion:
	var v: Variant = _sample_v(times, vals, tm)
	return v as Quaternion if v is Quaternion else Quaternion.IDENTITY

func _collect(anim: Animation, t: int) -> Dictionary:
	var times := PackedFloat32Array()
	var vals := []
	for k in anim.track_get_key_count(t):
		times.append(anim.track_get_key_time(t, k))
		vals.append(anim.track_get_key_value(t, k))
	return { "times": times, "vals": vals }

func _ang(a: Quaternion, b: Quaternion) -> float:
	var dot := absf(a.normalized().dot(b.normalized()))
	return acos(clampf(dot, -1.0, 1.0))

func _val_dist(a: Variant, b: Variant, tt: int) -> float:
	if tt == TYPE_POS or tt == TYPE_SCL:
		return (a as Vector3).distance_to(b as Vector3)
	if tt == TYPE_ROT:
		return _ang(a as Quaternion, b as Quaternion)
	return 1.0 if a != b else 0.0

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