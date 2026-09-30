extends SceneTree
## Sample the imported rig (not raw animation curves) to calibrate in-place gait.
## Godot 4.6 mono: --headless --path . --script res://Tools/modelgen/measure_player_stride.gd

func _initialize() -> void:
    call_deferred("measure")

func measure() -> void:
    var model: Node3D = load("res://scenes/world3d/PlayerModel.tscn").instantiate()
    root.add_child(model)
    var ap := model.get_node("AnimationPlayer") as AnimationPlayer
    var skeleton := model.get_node("Model/PlayerRig/Skeleton3D") as Skeleton3D
    var library := AnimationLibrary.new()
    for name in ["walk", "run"]:
        library.add_animation(name, load("res://assets/models3d/characters/player/animations/" + name + ".tres"))
    ap.add_animation_library("gait", library)
    for name in ["walk", "run"]:
        var length := ap.get_animation("gait/" + name).length
        var movement_speed := 2.3 if name == "walk" else 5.0
        var playback := 2.0 if name == "walk" else 2.0
        ap.play("gait/" + name)
        print("GAIT ", name, " length=", length)
        var samples := {"L": [], "R": []}
        for frame in 25:
            var time := length * frame / 24.0
            ap.seek(time, true)
            ap.advance(0)
            skeleton.force_update_all_bone_transforms()
            var feet := []
            for side in ["L", "R"]:
                var bone := skeleton.find_bone("foot." + side)
                var point := model.to_local(skeleton.to_global(skeleton.get_bone_global_pose(bone).origin))
                feet.append(Vector3(snappedf(point.x, .001), snappedf(point.y, .001), snappedf(point.z, .001)))
                samples[side].append(point)
            print("%s %.3f L=%s R=%s" % [name, time, feet[0], feet[1]])
        var errors := []
        for side in ["L", "R"]:
            var points: Array = samples[side]
            var low := 99.0
            for point in points:
                low = minf(low, point.y)
            for i in range(points.size() - 1):
                # Jog_Fwd has short contact windows followed by low airborne
                # ankles; height alone cannot distinguish those flight frames.
                if name == "run" and not ((side == "L" and i < 5) or
                        (side == "R" and i >= 12 and i < 17)):
                    continue
                if points[i].y < low + 0.035 and points[i + 1].y < low + 0.035:
                    var dz: float = points[i + 1].z - points[i].z
                    if dz < -0.005:
                        errors.append(absf(dz + movement_speed * length / (24.0 * playback)))
        if not errors.is_empty():
            var average := 0.0
            var maximum := 0.0
            for error in errors:
                average += error
                maximum = maxf(maximum, error)
            print("STANCE_SLIP ", name, " mean_mm_per_sample=", snappedf(1000.0 * average / errors.size(), .1),
                " max_mm_per_sample=", snappedf(1000.0 * maximum, .1), " samples=", errors.size())
    model.queue_free()
    quit()
