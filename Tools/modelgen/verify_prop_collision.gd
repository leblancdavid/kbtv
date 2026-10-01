extends SceneTree

func require(condition: bool, message: String) -> bool:
	if condition:
		return true
	push_error(message)
	quit(1)
	return false

func _initialize() -> void:
	call_deferred("verify")

func verify() -> void:
	var scene: Node3D = load("res://scenes/world3d/World3D.tscn").instantiate()
	root.add_child(scene)
	current_scene = scene
	for room_name in ["ControlRoom3D", "StudioRoom3D"]:
		var room: Node3D = scene.get_node(room_name)
		var generated := room.get_node("GeneratedColliders")
		if not require(generated.get_child_count() == 1, room_name + " has stale prop colliders"):
			return
		if not require(generated.get_child(0).name == "FloorCollider", room_name + " has no floor collider"):
			return
	for path in [
		"ControlRoom3D/Desk", "ControlRoom3D/SpeakerLeft", "ControlRoom3D/SpeakerRight",
		"ControlRoom3D/AudioCabinet", "ControlRoom3D/ShelfLeft", "ControlRoom3D/ShelfCenter",
		"ControlRoom3D/ShelfRight", "ControlRoom3D/OpenArchiveBox", "ControlRoom3D/SparePartsBin",
		"StudioRoom3D/StudioTable", "StudioRoom3D/VernStation/Chair",
		"StudioRoom3D/BookcaseLeft", "StudioRoom3D/BookcaseRight",
		"StudioRoom3D/RecordCrate", "StudioRoom3D/RecordCrate/RecordCrate",
		"StudioRoom3D/ArchiveBox", "StudioRoom3D/ArchiveBox/ArchiveBox",
		"ControlRoom3D/VariantManualsBox", "ControlRoom3D/VariantSealedBox",
		"StudioRoom3D/VariantTapeBox", "StudioRoom3D/VariantTapeBox/VariantTapeBox",
		"StudioRoom3D/VariantCableBin", "StudioRoom3D/VariantFullRecordCrate",
		"StudioRoom3D/VariantSinglesCrate", "StudioRoom3D/VariantLeaningRecordCrate"
	]:
		var prop: Node3D = scene.get_node(path)
		var body: StaticBody3D = prop.get_node("Collision")
		if not require(body.get_node("Shape").shape != null, path + " missing shape"):
			return
		var before := body.global_position
		prop.global_position += Vector3(0.48, 0, 0.52)
		if not require(body.global_position.distance_to(before + Vector3(0.48, 0, 0.52)) < 0.0001,
				path + " does not follow its prop"):
			return
		prop.global_position -= Vector3(0.48, 0, 0.52)
	await physics_frame
	var box: Node3D = scene.get_node("ControlRoom3D/OpenArchiveBox")
	var old_pos := box.global_position
	box.global_position += Vector3(1.0, 0, 0)
	await physics_frame
	await physics_frame
	var space := box.get_world_3d().direct_space_state
	var old_hit := space.intersect_ray(PhysicsRayQueryParameters3D.create(
		old_pos + Vector3(0, 1, 0), old_pos - Vector3(0, 0.01, 0)))
	var new_pos := box.global_position
	var new_hit := space.intersect_ray(PhysicsRayQueryParameters3D.create(
		new_pos + Vector3(0, 1, 0), new_pos - Vector3(0, 0.01, 0)))
	if not require(old_hit.is_empty() or old_hit.collider != box.get_node("Collision"), "old location still blocks"):
		return
	if not require(not new_hit.is_empty() and new_hit.collider == box.get_node("Collision"), "moved box does not block"):
		return
	print("PROP_COLLISION_VERIFIED")
	quit()
