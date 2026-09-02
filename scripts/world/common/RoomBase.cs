using Godot;

[GlobalClass]
public abstract partial class RoomBase : Node2D, IRoomSection
{
	[ExportGroup("Grid Settings")]
	[Export] public Vector2 GridAnchor = new(640, 360);
	[Export] public int GridWidth { get; set; } = 14;
	[Export] public int GridHeight { get; set; } = 10;
	[Export] public int LightMask = 1;
	[Export] public float SouthWallHideOffset = 16.0f;

	[ExportGroup("TileMap")]
	[Export] public int FloorSourceId = 0;
	[Export] public int GridDebugSourceId = 6;
	[Export] public TileSet RoomTileSet;

	public const float TileSize = 32.0f;
	public const string DefaultTileSetPath = "res://assets/tiles/topdown/topdown_tileset.tres";

	public static readonly Vector2I AtlasCoordsLeft = new(0, 0);
	public static readonly Vector2I AtlasCoordsMid = new(1, 0);
	public static readonly Vector2I AtlasCoordsRight = new(2, 0);

	public TileMapLayer FloorLayer { get; private set; } = null!;
	public TileMapLayer DoorLayer { get; private set; } = null!;
	public TileMapLayer GridDebugLayer { get; private set; } = null!;
	public Node2D PropSort { get; private set; } = null!;
	public Vector2 GridOffset { get; private set; }
	public CharacterBody2D Player { get; set; } = null!;

	public CastShadowSystem Shadows { get; protected set; } = null!;

	protected RoomDebug? DebugNode { get; set; }

	public void SetPlayer(CharacterBody2D player) => Player = player;

	public void ToggleDebug() => DebugNode?.Toggle();

	public Vector2 GridToWorld(Vector2I gridPos) => FloorLayer.MapToLocal(gridPos) + GridOffset;

	public Vector2I WorldToGrid(Vector2 worldPos) => FloorLayer.LocalToMap(worldPos - GridOffset);

	public Rect2 GetFloorBounds()
	{
		var topLeft = GridToWorld(new Vector2I(0, 0));
		return new Rect2(topLeft.X, topLeft.Y, GridWidth * TileSize, GridHeight * TileSize);
	}

	protected virtual void ConfigureRoom() { }
	protected virtual void OnRoomReady() { }
	protected virtual void OnRoomProcess(double delta) { }

	public override void _Ready()
	{
		ConfigureRoom();

		var tileSet = RoomTileSet ?? GD.Load<TileSet>(DefaultTileSetPath);
		if (tileSet == null)
		{
			GD.PrintErr($"{GetClass()}: Failed to load tileset");
			return;
		}

		FloorLayer = new TileMapLayer { Name = "FloorLayer", TileSet = tileSet, ZIndex = 0, LightMask = LightMask };
		DoorLayer = new TileMapLayer { Name = "DoorLayer", TileSet = tileSet, ZIndex = 1000 };
		GridDebugLayer = new TileMapLayer { Name = "GridDebugLayer", TileSet = tileSet, Visible = false };
		PropSort = new Node2D { Name = "PropSort", YSortEnabled = true };

		AddChild(FloorLayer);
		AddChild(DoorLayer);
		AddChild(GridDebugLayer);
		AddChild(PropSort);

		GridOffset = GridAnchor;
		FloorLayer.Position = GridOffset;
		DoorLayer.Position = GridOffset;
		GridDebugLayer.Position = GridOffset;

		CreateFloor();

		OnRoomReady();
	}

	public override void _Process(double delta)
	{
		OnRoomProcess(delta);
	}

	protected void CreateFloor()
	{
		for (int y = 0; y < GridHeight; y++)
		{
			for (int x = 0; x < GridWidth; x++)
			{
				// Stamp the floor atlas as a 2x2 pattern so 64x64 sources (e.g. floor_beige)
				// reconstruct at native scale instead of repeating only their first tile.
				var atlas = new Vector2I(x % 2, y % 2);
				FloorLayer.SetCell(new Vector2I(x, y), FloorSourceId, atlas);
			}
		}
	}
}