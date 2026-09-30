extends SceneTree
## Regression gate: steady world-space wrist during grasp/set-down, no joint flips.
func _initialize() -> void:
	call_deferred("run")

func run() -> void:
	var vern := (load("res://scenes/world3d/Vern.tscn") as PackedScene).instantiate()
	root.add_child(vern)
	for i in 12:
		await process_frame
	var player := vern.find_children("*", "AnimationPlayer", true, false)[0] as AnimationPlayer
	var skel := vern.find_children("*", "Skeleton3D", true, false)[0] as Skeleton3D
	player.callback_mode_process = AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_MANUAL
	var previous := {}
	var max_step := 0.0
	var worst := ""
	var windows := {}
	var drift := 0.0
	for frame in range(661):
		var time := frame / 120.0
		player.play("smoking", 0)
		player.seek(time, true)
		player.advance(0)
		for bone in ["upperarm01.R", "upperarm02.R", "lowerarm01.R", "lowerarm02.R", "wrist.R"]:
			var idx := skel.find_bone(bone)
			var q := skel.get_bone_pose_rotation(idx)
			if previous.has(bone):
				var angle := _angle(previous[bone], q)
				if angle > max_step:
					max_step = angle
					worst = "%s at %.3fs" % [bone, time]
			previous[bone] = q
		var window := "pickup" if time >= 0.95 and time <= 1.25 else ("return" if time >= 4.45 and time <= 4.79 else "")
		if not window.is_empty():
			var q := skel.get_bone_global_pose(skel.find_bone("wrist.R")).basis.get_rotation_quaternion()
			if not windows.has(window):
				windows[window] = q
			drift = maxf(drift, _angle(windows[window], q))
	print("SMOKING_TURN contact_drift_deg=%.3f max_joint_step_deg=%.3f at_120fps worst=%s" % [drift, max_step, worst])
	# 12 deg/120Hz rejects single-frame flips without rejecting the deliberate lift turn.
	quit(0 if drift < 1.0 and max_step < 12.0 else 1)

func _angle(a: Quaternion, b: Quaternion) -> float:
	return rad_to_deg(2.0 * acos(clampf(absf(a.normalized().dot(b.normalized())), 0.0, 1.0)))
