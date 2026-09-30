extends SceneTree
## Godot 4.6 mono graphical preview: --path . --script res://Tools/modelgen/preview_player_locomotion.gd
## Saves three points per cycle under %TEMP%/opencode/player_locomotion.

func _initialize() -> void:
    call_deferred("capture")

func capture() -> void:
    root.size = Vector2i(960, 720)
    var stage := Node3D.new()
    root.add_child(stage)
    var player: Node3D = load("res://scenes/world3d/PlayerModel.tscn").instantiate()
    stage.add_child(player)
    var ap := player.get_node("AnimationPlayer") as AnimationPlayer
    var camera := Camera3D.new()
    stage.add_child(camera)
    camera.position = Vector3(2.7, 1.8, 3.6)
    camera.look_at(Vector3(0, 0.9, 0))
    camera.projection = Camera3D.PROJECTION_ORTHOGONAL
    camera.size = 2.4
    camera.current = true
    for position in [Vector3(2, 3, 3), Vector3(-3, 2, 0)]:
        var light := OmniLight3D.new()
        stage.add_child(light)
        light.position = position
        light.omni_range = 9
        light.light_energy = 2.5
    var output := OS.get_environment("TEMP").path_join("opencode/player_locomotion")
    DirAccess.make_dir_recursive_absolute(output)
    for name in ["idle_breathing", "walk", "run"]:
        var clip: Animation = load("res://assets/models3d/characters/player/animations/" + name + ".tres")
        var library := AnimationLibrary.new()
        library.add_animation(name, clip)
        ap.add_animation_library(name, library)
        ap.play(name + "/" + name)
        ap.advance(0)
        for frame in 3:
            ap.seek(clip.length * frame / 3.0, true)
            await process_frame
            await RenderingServer.frame_post_draw
            root.get_texture().get_image().save_png(output.path_join("%s_%d.png" % [name, frame]))
            camera.position = Vector3(4, 1.8, 0.2)
            camera.look_at(Vector3(0, 0.9, 0))
            await process_frame
            await RenderingServer.frame_post_draw
            root.get_texture().get_image().save_png(output.path_join("%s_%d_side.png" % [name, frame]))
            camera.position = Vector3(-2.7, 1.8, -3.6)
            camera.look_at(Vector3(0, 0.9, 0))
            await process_frame
            await RenderingServer.frame_post_draw
            root.get_texture().get_image().save_png(output.path_join("%s_%d_back.png" % [name, frame]))
            camera.position = Vector3(2.7, 1.8, 3.6)
            camera.look_at(Vector3(0, 0.9, 0))
    print("PLAYER_LOCOMOTION_PREVIEW ", output)
    quit()
