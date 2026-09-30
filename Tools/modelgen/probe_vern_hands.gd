extends SceneTree

func _initialize() -> void:
	call_deferred("run")

func run() -> void:
	var vern := (load("res://scenes/world3d/Vern.tscn") as PackedScene).instantiate()
	root.add_child(vern)
	for i in 12:
		await process_frame
	var skel := vern.find_children("*", "Skeleton3D", true, false)[0] as Skeleton3D
	var player := vern.find_children("*", "AnimationPlayer", true, false)[0] as AnimationPlayer
	player.callback_mode_process = AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_MANUAL
	if "--shoulders" in OS.get_cmdline_user_args():
		player.play("idle_breathing", 0)
		player.seek(1.0, true)
		for i in skel.get_bone_count():
			var bone := skel.get_bone_name(i)
			if "clavicle" in bone or "shoulder" in bone or "upperarm01" in bone:
				print("SHOULDER %s parent=%s position=%s" % [bone, skel.get_bone_name(skel.get_bone_parent(i)), skel.get_bone_global_pose(i).origin])
		quit()
		return
	for clip in ["idle_breathing", "smoking", "drink_coffee"]:
		player.play(clip, 0)
		player.seek(2.5, true)
		player.advance(0)
		for side in ["L", "R"]:
			var wrist := skel.get_bone_global_pose(skel.find_bone("wrist." + side))
			print("HAND_GEOMETRY %s %s" % [clip, side])
			for finger in range(1, 6):
				var points: Array = []
				for segment in range(1, 4):
					var bone := "finger%d-%d.%s" % [finger, segment, side]
					var pose := skel.get_bone_global_pose(skel.find_bone(bone))
					points.append(wrist.affine_inverse() * pose.origin)
				print("finger%d %s" % [finger, points])
	quit()
