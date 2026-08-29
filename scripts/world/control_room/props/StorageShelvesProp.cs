using Godot;

/// <summary>
/// Two storage shelves low on the south wall. Cast no shadow and sit above the floor scan via
/// their pixel offset; the override gives them a flat base collider.
/// </summary>
public static class StorageShelvesProp
{
	/// <summary>Sprite shared by both shelves.</summary>
	public const string TexturePath = "res://assets/tiles/props/storage_shelf.png";

	public static PropSpec[] Specs { get; } =
	{
		new(new Vector2I(4, 10), new Vector2(0, -32), FloorScanHeight: 16, CreateCastShadow: false, ColliderOverride: new Vector4(0, -4, 64, 16)),
		new(new Vector2I(10, 10), new Vector2(0, -32), FloorScanHeight: 16, CreateCastShadow: false, ColliderOverride: new Vector4(0, -4, 64, 16)),
	};
}
