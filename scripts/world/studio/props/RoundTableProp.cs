using Godot;

/// <summary>
/// Vern's round table on the studio stage. It casts no depth shadow of its own (the ceiling
/// light keying is handled by the room) and scans a tall 48px floor band to catch the full
/// visible footprint of the tabletop.
/// </summary>
public static class RoundTableProp
{
	/// <summary>Sprite used by the round table.</summary>
	public const string TexturePath = "res://assets/tiles/props/round_table.png";

	public static GridPlacement Placement { get; } = (new Vector2I(6, 4), Vector2.Zero);

	public static int FloorScanHeight { get; } = 48;

	public static Node2D? Create(
		Node2D parent,
		CastShadowSystem shadowSystem,
		ShaderMaterial depthShadowMaterial,
		IRoomSection roomSection,
		int lightMask
	)
	{
		var group = PropBuilder.CreatePropAutoCollider(
			parent,
			TexturePath,
			Placement.Cell,
			Placement.Offset,
			shadowSystem,
			depthShadowMaterial,
			roomSection,
			lightMask,
			createCastShadow: false,
			floorScanHeight: FloorScanHeight
		);
		if (group == null)
		{
			return null;
		}

		group.Name = "RoundTableGroup";
		return group;
	}
}