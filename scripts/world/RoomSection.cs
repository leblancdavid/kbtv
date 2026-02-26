using Godot;

public partial class RoomSection : Node2D, IRoomSection
{
	public TileMapLayer FloorLayer { get; set; } = null!;
	public TileMapLayer DoorLayer { get; set; } = null!;
	public Node2D PropSort { get; set; } = null!;
	public Vector2 GridOffset { get; set; } = Vector2.Zero;
	public int GridWidth { get; set; } = 14;
	public int GridHeight { get; set; } = 10;
	public CharacterBody2D Player { get; set; } = null!;

	public Vector2 GridToWorld(Vector2I gridPos)
	{
		return FloorLayer.MapToLocal(gridPos) + GridOffset;
	}
}
