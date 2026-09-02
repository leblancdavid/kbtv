using Godot;

public interface IRoomSection
{
	TileMapLayer FloorLayer { get; }
	TileMapLayer DoorLayer { get; }
	TileMapLayer GridDebugLayer { get; }
	Node2D PropSort { get; }
	Vector2 GridOffset { get; }
	int GridWidth { get; }
	int GridHeight { get; }
	CharacterBody2D Player { get; set; }
	Vector2 GridToWorld(Vector2I gridPos);
}
