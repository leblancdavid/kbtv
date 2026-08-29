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

	public static GridPlacement Placement { get; } = (new Vector2I(6, 4), new Vector2(0, -16));

	public static int FloorScanHeight { get; } = 48;

	/// <summary>Uniform scale so the table reads 25% smaller on stage.</summary>
	public static float SpriteScale { get; } = 0.75f;

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
			floorScanHeight: FloorScanHeight,
			scale: new Vector2(SpriteScale, SpriteScale)
		);
		if (group == null)
		{
			return null;
		}

		group.Name = "RoundTableGroup";
		return group;
	}
}
