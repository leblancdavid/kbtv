extends SceneTree
## Validate prop footprints against the actual scene transform and authored tabletop.
func _initialize() -> void:
	var world := (load("res://scenes/world3d/World3D.tscn") as PackedScene).instantiate()
	var studio := world.get_node("StudioRoom3D")
	var table := studio.get_node("StudioTable") as Node3D
	var station := studio.get_node("VernStation") as Node3D
	var vern := station.get_node("Vern") as Node3D
	var to_table := table.transform.affine_inverse() * station.transform * vern.transform
	var data: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://assets/models3d/characters/vern/animation_contacts.json"))
	var failed := false
	# Table top: 3.81 x 1.44, top y=.77 (studio_table.py).
	for name in ["coffee_mug", "ashtray"]:
		var p: Array = data.props[name].rest.position
		var point := to_table * Vector3(p[0], p[1], p[2])
		var half_size := Vector2(0.10, 0.045) if name == "coffee_mug" else Vector2(0.049, 0.049)
		var q: Array = data.props[name].rest.rotation_quaternion_xyzw
		var basis := to_table.basis * Basis(Quaternion(q[0], q[1], q[2], q[3]))
		half_size = Vector2(absf(basis.x.x) * half_size.x + absf(basis.z.x) * half_size.y, absf(basis.x.z) * half_size.x + absf(basis.z.z) * half_size.y)
		var edge_clearance := minf(1.905 - absf(point.x) - half_size.x, 0.72 - absf(point.z) - half_size.y)
		var supported := absf(point.y - 0.77) < 0.001 and edge_clearance >= 0.075
		print("TABLE_SUPPORT %s local=%s edge_clearance_m=%.4f supported=%s" % [name, point, edge_clearance, supported])
		failed = failed or not supported
	world.free()
	quit(1 if failed else 0)
