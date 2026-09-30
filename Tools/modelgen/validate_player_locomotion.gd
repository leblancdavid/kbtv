extends SceneTree
## Smoke-test the shipped animation paths and input-driven runtime player state.

func _initialize() -> void:
    call_deferred("validate")

func check(value: bool, message: String) -> bool:
    if not value:
        push_error(message)
        quit(1)
    return value

func validate() -> void:
    var scene: Node = load("res://scenes/world3d/World3D.tscn").instantiate()
    root.add_child(scene)
    var player := scene.get_node("Player3D") as CharacterBody3D
    var ap := player.get_node("Visual/AnimationPlayer") as AnimationPlayer
    var skeleton := player.get_node("Visual/Model/PlayerRig/Skeleton3D") as Skeleton3D
    var left_ankle := skeleton.get_bone_global_rest(skeleton.find_bone("foot.L")).origin
    var right_ankle := skeleton.get_bone_global_rest(skeleton.find_bone("foot.R")).origin
    if not check(absf(left_ankle.x - right_ankle.x) < 0.30,
            "Player's exported standing stance is wider than the hips"):
        return
    for name in ["idle_breathing", "walk", "run"]:
        if not check(ap.has_animation(name), "Missing player animation " + name):
            return
        var animation := ap.get_animation(name)
        if not check(animation.get_track_count() >= 40, "Too few tracks in " + name):
            return
        var animated_bones := 0
        for i in animation.get_track_count():
            var path := String(animation.track_get_path(i))
            if not check(path.begins_with("Model/PlayerRig/Skeleton3D:"), "Wrong track path: " + path):
                return
            if not check(skeleton.find_bone(path.get_slice(":", 1)) >= 0, "Missing target bone: " + path):
                return
            if path.get_slice(":", 1) == "root":
                if not check(false, "Root motion must stay on the CharacterBody"):
                    return
            var start: Quaternion = animation.track_get_key_value(i, 0)
            var end: Quaternion = animation.track_get_key_value(i, animation.track_get_key_count(i) - 1)
            if not check(start.angle_to(end) < 0.01, "Loop seam on " + path):
                return
            if path.ends_with(":spine05"):
                var rest := skeleton.get_bone_rest(skeleton.find_bone("spine05")).basis.get_rotation_quaternion()
                var lean := start.angle_to(rest)
                if not check(lean > 0.04 and lean < 0.08 if name == "run" else lean < 0.01,
                        "Only running should lean forward from the waist"):
                    return
            if path.ends_with("upperleg01.L"):
                var mid: Quaternion = animation.track_get_key_value(i, animation.track_get_key_count(i) / 2)
                if start.angle_to(mid) > 0.05:
                    animated_bones += 1
        if name != "idle_breathing" and not check(animated_bones > 0, "Legs are static in " + name):
            return
    await physics_frame
    if not check(ap.current_animation == "idle_breathing", "Player should idle at rest"):
        return
    Input.action_press("move_right")
    for i in 20:
        await physics_frame
    if not check(ap.current_animation == "walk" and player.velocity.x > 2.3 and player.velocity.x < 2.7,
            "Walking input must play walk and move right"):
        return
    if not check(absf(ap.speed_scale - 2.1) < 0.12,
            "Walk animation cadence must remain unchanged at the faster travel speed"):
        return
    var visual := player.get_node("Visual") as Node3D
    if not check(absf(wrapf(visual.rotation.y - PI / 2.0, -PI, PI)) < 0.3,
            "Visual must face +X when walking right"):
        return
    Input.action_press("run")
    for i in 20:
        await physics_frame
    if not check(ap.current_animation == "run" and player.velocity.x > 3.8,
            "Run input must play run and increase speed"):
        return
    if not check(absf(ap.speed_scale - 1.7) < 0.12, "Run cadence must match the stride"):
        return
    Input.action_release("run")
    for i in 20:
        await physics_frame
    if not check(ap.current_animation == "walk" and absf(player.velocity.x - 2.5) < 0.12,
            "Releasing run while moving must return to walking"):
        return
    var waist := skeleton.find_bone("spine05")
    var upright := skeleton.get_bone_rest(waist).basis.get_rotation_quaternion()
    if not check(skeleton.get_bone_pose_rotation(waist).angle_to(upright) < 0.02,
            "Run lean must blend away when returning to walk"):
        return
    Input.action_release("move_right")
    for i in 30:
        await physics_frame
    if not check(ap.current_animation == "idle_breathing", "Stop must return to breathing"):
        return
    player.call("SetMovementLocked", true)
    Input.action_press("move_right")
    for i in 5:
        await physics_frame
    if not check(player.velocity.length() < 0.01 and ap.current_animation == "idle_breathing",
            "Movement lock must stop the player and restore idle"):
        return
    Input.action_release("move_right")
    print("PLAYER_LOCOMOTION_VALIDATED clips=3 facing=+X walk/run/idle=ok")
    scene.queue_free()
    quit()
