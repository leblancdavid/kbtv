extends SceneTree
## Bake the CC0 Quaternius standing clips onto the shipped MPFB player skeleton.
## Run with Godot 4.6 mono: --headless --path . --script res://Tools/modelgen/bake_player_locomotion.gd
## The reference bundle is gitignored; the generated .tres files are the portable output.

const REFERENCE := "res://docs/references/Animation Library[Standard]/Godot/AnimationLibrary_Godot_Standard.glb"
const PLAYER := "res://scenes/world3d/PlayerModel.tscn"
const REF_MAP := "res://Tools/modelgen/retarget/bone_map_ref.tres"
const TARGET_MAP := "res://Tools/modelgen/retarget/bone_map_mpfb.tres"
const OUTPUT := "res://assets/models3d/characters/player/animations/"
const CLIPS := {"idle_breathing": "Idle", "walk": "Walk", "run": "Jog_Fwd"}
const FPS := 30

func _initialize() -> void:
    call_deferred("bake")

func find_node(node: Node, type: String) -> Node:
    if node.is_class(type):
        return node
    for child in node.get_children():
        var found := find_node(child, type)
        if found != null:
            return found
    return null

func bake() -> void:
    var ref_scene := load(REFERENCE) as PackedScene
    var player_scene := load(PLAYER) as PackedScene
    var ref_map := load(REF_MAP) as BoneMap
    var target_map := load(TARGET_MAP) as BoneMap
    if ref_scene == null or player_scene == null or ref_map == null or target_map == null:
        push_error("Missing locomotion source, player, or humanoid bone map")
        quit(1)
        return
    var ref_root := ref_scene.instantiate()
    var player_root := player_scene.instantiate()
    var ref_skel := find_node(ref_root, "Skeleton3D") as Skeleton3D
    var target_skel := find_node(player_root, "Skeleton3D") as Skeleton3D
    var ref_player := find_node(ref_root, "AnimationPlayer") as AnimationPlayer
    var target_player := find_node(player_root, "AnimationPlayer") as AnimationPlayer
    if ref_skel == null or target_skel == null or ref_player == null or target_player == null:
        push_error("Imported skeleton or AnimationPlayer missing")
        quit(1)
        return
    var root := target_player.get_node(target_player.root_node)
    var skeleton_path := String(root.get_path_to(target_skel))
    var bones := {}
    var profile := target_map.get_profile()
    for i in profile.get_bone_size():
        var name := String(profile.get_bone_name(i))
        var source := String(ref_map.get_skeleton_bone_name(name))
        var target := String(target_map.get_skeleton_bone_name(name))
        if source.is_empty() or target.is_empty() or source == "DEF-root" or target == "root":
            continue
        if ref_skel.find_bone(source) >= 0 and target_skel.find_bone(target) >= 0:
            bones[source] = target
    if bones.size() < 20:
        push_error("Incomplete player retarget bone map: %d" % bones.size())
        quit(1)
        return
    DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUTPUT))
    for name in CLIPS:
        var source_clip := ref_player.get_animation(CLIPS[name])
        if source_clip == null:
            push_error("Reference clip missing: " + CLIPS[name])
            quit(1)
            return
        var animation := Animation.new()
        animation.length = source_clip.length
        animation.step = 1.0 / FPS
        animation.loop_mode = Animation.LOOP_LINEAR
        var count := 0
        for track in source_clip.get_track_count():
            if source_clip.track_get_type(track) != Animation.TYPE_ROTATION_3D:
                continue
            var source := String(source_clip.track_get_path(track)).get_slice(":", 1)
            if not bones.has(source):
                continue
            var target: String = bones[source]
            var si := ref_skel.find_bone(source)
            var ti := target_skel.find_bone(target)
            var sp := ref_skel.get_bone_parent(si)
            var tp := target_skel.get_bone_parent(ti)
            var old_parent := ref_skel.get_bone_global_rest(sp).basis.get_rotation_quaternion() if sp >= 0 else Quaternion.IDENTITY
            var new_parent := target_skel.get_bone_global_rest(tp).basis.get_rotation_quaternion() if tp >= 0 else Quaternion.IDENTITY
            var old_rest := ref_skel.get_bone_rest(si).basis.get_rotation_quaternion()
            var new_rest := target_skel.get_bone_rest(ti).basis.get_rotation_quaternion()
            var index := animation.add_track(Animation.TYPE_ROTATION_3D)
            animation.track_set_path(index, NodePath(skeleton_path + ":" + target))
            var first: Quaternion = Quaternion.IDENTITY
            for frame in int(ceil(animation.length * FPS)) + 1:
                var time := minf(frame / float(FPS), animation.length)
                var q: Quaternion = source_clip.rotation_track_interpolate(track, time)
                # Reorient the source local pose relative to its rest, in world-rest axes,
                # then apply it in the target's parent-rest frame. No root translation.
                var delta := old_parent * q * old_rest.inverse() * old_parent.inverse()
                var result := (new_parent.inverse() * delta * new_parent * new_rest).normalized()
                var phase := TAU * time / animation.length
                var leg := target.begins_with("upperleg") or target.begins_with("lowerleg") or target.begins_with("foot") or target.begins_with("toe")
                if name == "idle_breathing":
                    result = new_rest
                    if target == "spine01" or target == "spine02":
                        result = new_rest * Quaternion(Vector3.RIGHT, 0.018 * sin(phase))
                elif leg:
                    # The mannequin's full stride is too broad for this fitted
                    # office-worker rig. Keep the gait timing, soften its reach.
                    result = new_rest.slerp(result, 0.52 if name == "walk" else 0.48)
                else:
                    result = new_rest
                    if target == "upperarm01.L" or target == "upperarm01.R":
                        var side := 1.0 if target.ends_with(".L") else -1.0
                        var swing := Quaternion(Vector3.RIGHT,
                            side * (0.24 if name == "walk" else 0.36) * sin(phase))
                        result = new_parent.inverse() * swing * new_parent * new_rest
                    elif target == "spine01" or target == "spine02":
                        result = new_rest * Quaternion(Vector3.UP, 0.018 * sin(phase))
                if frame == 0:
                    first = result
                # Retain the source gait but ease the last few samples into the
                # first pose so a repeated cycle has no visible frame jump.
                var seam := clampf((time - (animation.length - 0.16)) / 0.16, 0.0, 1.0)
                result = result.slerp(first, seam * seam * (3.0 - 2.0 * seam))
                animation.rotation_track_insert_key(index, time, result)
            count += 1
        if count < 20:
            push_error("Too few retargeted tracks in %s: %d" % [name, count])
            quit(1)
            return
        var path: String = OUTPUT + String(name) + ".tres"
        var status := ResourceSaver.save(animation, path)
        if status != OK:
            push_error("Cannot save %s: %s" % [path, status])
            quit(1)
            return
        print("PLAYER_CLIP ", name, " length=", animation.length, " tracks=", count, " skeleton=", skeleton_path)
    ref_root.free()
    player_root.free()
    quit()
