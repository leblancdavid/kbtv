extends SceneTree

# Fidelity check: compare the production bake against the approved V5 spike output.
# Loads both .tres (production via res://, spike via absolute path) and diffs track-by-track.

const PROD := "res://assets/models3d/characters/vern/animations/talk_calm.tres"
const V5 := "C:/Users/lblan/AppData/Local/Temp/opencode/kbtv_retarget/baked/talk_calm.tres"

func _initialize() -> void:
	var a := load(PROD) as Animation
	var b := load(V5) as Animation
	if a == null or b == null:
		print("LOAD FAIL prod=%s v5=%s" % [a != null, b != null])
		quit(1)
		return
	print("prod length=%.6f step=%.6f loop=%d" % [a.length, a.step, a.loop_mode])
	print("v5   length=%.6f step=%.6f loop=%d" % [b.length, b.step, b.loop_mode])
	print("prod tracks=%d v5 tracks=%d" % [a.get_track_count(), b.get_track_count()])
	var mismatches := 0
	var compared := 0
	var paths_checked := {}
	for ti in a.get_track_count():
		var pa := String(a.track_get_path(ti))
		var pta := int(a.track_get_type(ti))
		var bti := -1
		for tj in b.get_track_count():
			if String(b.track_get_path(tj)) == pa and int(b.track_get_type(tj)) == pta:
				bti = tj
				break
		if bti < 0:
			print("MISSING in v5: %s" % pa)
			mismatches += 1
			continue
		paths_checked[pa + "|" + str(pta)] = true
		var ka := a.track_get_key_count(ti)
		var kb := b.track_get_key_count(bti)
		compared += 1
		if ka != kb:
			print("KEYCOUNT %s prod=%d v5=%d" % [pa, ka, kb])
			mismatches += 1
			continue
		var worst := 0.0
		for k in ka:
			var ta: float = a.track_get_key_time(ti, k)
			var tb: float = b.track_get_key_time(bti, k)
			var va = a.track_get_key_value(ti, k)
			var vb = b.track_get_key_value(bti, k)
			if absf(ta - tb) > 0.000001:
				print("TIME %s key%d prod=%.6f v5=%.6f" % [pa, k, ta, tb])
				mismatches += 1
				continue
			var d: float = _val_dist(va, vb)
			worst = maxf(worst, d)
			if d > 0.0007:  # >2 float32 ULP (acos(1-2^-22)) = float32 storage noise band
				# storage-order float32 noise keeps a handful of keys at 1-2 ULP (worst seen: chest 0.0006905)
				print("VAL %s key%d prod=%s v5=%s dist=%.7f" % [pa, k, va, vb, d])
				mismatches += 1
		print("track %-55s keys=%d worst_dist=%.9f" % [pa, ka, worst])
	# any v5-only tracks?
	for tj in b.get_track_count():
		var pb := String(b.track_get_path(tj))
		var tb := int(b.track_get_type(tj))
		if not paths_checked.has(pb + "|" + str(tb)):
			print("EXTRA in v5 (not in prod): %s" % pb)
			mismatches += 1
	print("compared=%d mismatches=%d" % [compared, mismatches])
	quit(0 if mismatches == 0 else 1)

func _val_dist(a: Variant, b: Variant) -> float:
	if a is Quaternion and b is Quaternion:
		var qa: Quaternion = a
		var qb: Quaternion = b
		var dot := absf(qa.normalized().dot(qb.normalized()))
		return acos(clampf(dot, -1.0, 1.0))
	if a is Vector3 and b is Vector3:
		return (a as Vector3).distance_to(b as Vector3)
	return 1.0 if a != b else 0.0