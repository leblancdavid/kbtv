using Godot;

/// <summary>
/// Two bookcases, one on each wall flanking the stage. Shared sprite; the auto-deriver scans
/// a 16px floor band so the player can walk close to the base.
/// </summary>
public static class BookcasesProp
{
	/// <summary>Sprite shared by both bookcases.</summary>
	public const string TexturePath = "res://assets/tiles/props/bookcase.png";

	public static GridPlacement[] Placements { get; } =
	{
		(new Vector2I(1, 1), Vector2.Zero),
		(new Vector2I(12, 1), Vector2.Zero),
	};

	public static int FloorScanHeight { get; } = 16;
}