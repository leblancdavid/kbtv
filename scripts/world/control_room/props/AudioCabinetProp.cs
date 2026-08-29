using Godot;

/// <summary>
/// Wide audio cabinet against the wall right of the desk. The collider override spans its full
/// base so the player can't clip through the sides.
/// </summary>
public static class AudioCabinetProp
{
	/// <summary>Sprite used by the cabinet.</summary>
	public const string TexturePath = "res://assets/tiles/props/audio_cabinet.png";

	public static PropSpec Spec { get; } =
		new(new Vector2I(12, 1), Vector2.Zero, FloorScanHeight: 24, ColliderOverride: new Vector4(0, -18, 96, 16));
}
