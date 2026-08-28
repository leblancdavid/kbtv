using Godot;

/// <summary>
/// Speaker stands flanking the control-room desk. Same sprite + auto-collider at two cells;
/// the override pins the collider to the stands' base strip.
/// </summary>
public static class SpeakerStandsProp
{
	/// <summary>Sprite shared by both stands.</summary>
	public const string TexturePath = "res://assets/tiles/props/speaker_stand.png";

	public static PropSpec[] Specs { get; } =
	{
		new(new Vector2I(2, 0), Vector2.Zero, FloorScanHeight: 24, ColliderOverride: new Vector4(-22.5f, -4, 45, 8)),
		new(new Vector2I(10, 0), Vector2.Zero, FloorScanHeight: 24, ColliderOverride: new Vector4(-22.5f, -4, 45, 8)),
	};
}