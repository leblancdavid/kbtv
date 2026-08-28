using Godot;

/// <summary>
/// The player's chair just south of the desk. Walk-through (non-collidable), placed via the
/// non-colliding <c>CreateProp</c> overload.
/// </summary>
public static class ControlChairProp
{
	/// <summary>Sprite used by the chair.</summary>
	public const string TexturePath = "res://assets/tiles/props/computer_chair.png";

	public static GridPlacement Placement { get; } = (new Vector2I(6, 2), new Vector2(0, -16));
}