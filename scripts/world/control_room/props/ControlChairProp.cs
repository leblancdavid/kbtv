using Godot;

/// <summary>
/// The player's chair just south of the desk. Walk-through (non-collidable), so it places with no
/// collider.
/// </summary>
public static class ControlChairProp
{
	/// <summary>Sprite used by the chair.</summary>
	public const string TexturePath = "res://assets/tiles/props/computer_chair.png";

	public static GridPlacement Placement { get; } = (new Vector2I(6, 3), new Vector2(0, -16));

	/// <summary>Builds the walk-through chair (non-collidable, no collider).</summary>
	public static Node2D? Create(
		Node2D parent,
		CastShadowSystem shadowSystem,
		ShaderMaterial depthShadowMaterial,
		IRoomSection roomSection,
		int lightMask
	) => PropBuilder.CreateProp(
		parent, TexturePath, Placement.Cell, Placement.Offset, false, Vector2.Zero,
		shadowSystem, depthShadowMaterial, roomSection, lightMask);
}
