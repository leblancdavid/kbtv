using Godot;

/// <summary>
/// A grid cell anchor plus a named pixel fine-tune offset. Room props/boards/lights are
/// authored as "which cell, plus how many pixels of nudging" so positions read relative to
/// the tile grid instead of as opaque world coordinates.
/// </summary>
public readonly record struct GridPlacement(Vector2I Cell, Vector2 Offset)
{
	public static implicit operator GridPlacement((Vector2I cell, Vector2 offset) tuple)
		=> new(tuple.cell, tuple.offset);

	/// <summary>Maps the cell to world via <see cref="IRoomSection.GridToWorld"/> and adds the offset.</summary>
	public Vector2 ToWorld(IRoomSection section) => section.GridToWorld(Cell) + Offset;
}

/// <summary>A tabletop board: its sprite path plus an offset relative to the table group's origin.</summary>
public readonly record struct BoardSpec(string TexturePath, Vector2 Offset)
{
	public static implicit operator BoardSpec((string texturePath, Vector2 offset) tuple)
		=> new(tuple.texturePath, tuple.offset);
}

/// <summary>Full definition of a placed prop: anchor cell + fine-tune, shadow/scan settings, and an optional collider override.</summary>
public readonly record struct PropSpec(
	Vector2I Cell,
	Vector2 Offset,
	int FloorScanHeight = 16,
	bool CreateCastShadow = true,
	Vector4? ColliderOverride = null
);
