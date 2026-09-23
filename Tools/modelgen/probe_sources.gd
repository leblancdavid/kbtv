extends SceneTree

func _initialize() -> void:
	print("PROBE_START")
	_probe("res://docs/references/Animation Library[Standard]/Godot/AnimationLibrary_Godot_Standard.glb", "REF")
	_probe("res://assets/models3d/characters/vern/vern.glb", "VERN")
	quit()

func _probe(path: String, tag: String) -> void:
	print("== " + tag + " loading " + path)
	var res: Resource = load(path)
	if res == null:
		print(tag + " LOAD_FAILED (null)")
		return
	print(tag + " type=" + str(typeof(res)) + " class=" + res.get_class())
	var obj: Node = null
	if res is PackedScene:
		obj = (res as PackedScene).instantiate()
	else:
		print(tag + " NOT_A_SCENE")
		return
	root.add_child(obj)
	print(tag + " tree:")
	_dump(obj, "  ")
	var anim: AnimationPlayer = _find_anim_player(obj)
	if anim == null:
		print(tag + " NO_ANIMATIONPLAYER")
		obj.queue_free()
		return
	print(tag + " anim_player_path_in_scene=ROOT/" + str(obj.get_path_to(anim)))
	print(tag + " animations (" + str(anim.get_animation_list().size()) + "):")
	for n in anim.get_animation_list():
		var a: Animation = anim.get_animation(n)
		var first: String = ""
		if a.get_track_count() > 0:
			first = " | first_track=" + str(a.track_get_path(0))
		print(tag + "  clip='" + str(n) + "' len=" + str(a.length) + " tracks=" + str(a.get_track_count()) + first)
	var skels := _find_skels(obj)
	for sk in skels:
		var rel := obj.get_path_to(sk)
		print(tag + " skeleton_rel_path=" + str(rel) + " bones=" + str(sk.get_bone_count()) + " first_bones=" + str(sk.get_bone_name(0)) + "," + str(sk.get_bone_name(1)))
	obj.queue_free()

func _find_anim_player(node: Node) -> AnimationPlayer:
	if node is AnimationPlayer:
		return node
	for c in node.get_children():
		var r := _find_anim_player(c)
		if r != null:
			return r
	return null

func _find_skels(node: Node) -> Array[Skeleton3D]:
	var out: Array[Skeleton3D] = []
	if node is Skeleton3D:
		out.append(node)
	for c in node.get_children():
		out.append_array(_find_skels(c))
	return out

func _dump(node: Node, indent: String) -> void:
	print(indent + node.get_class() + " " + node.name + ((" (scene_file=" + node.scene_file_path + ")") if node.scene_file_path != "" else ""))
	for c in node.get_children():
		_dump(c, indent + "  ")