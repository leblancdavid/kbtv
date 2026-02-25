using Godot;

[GlobalClass]
public abstract partial class RoomBase : Node2D
{
	[ExportGroup("Grid Settings")]
	[Export] public Vector2 GridAnchor = new(320, 180);
	[Export] public int GridWidth = 14;
	[Export] public int GridHeight = 10;
	[Export] public float SouthWallHideOffset = 8.0f;
	[Export] public bool AutoCenter = true;

	[ExportGroup("TileMap")]
	[Export] public int FloorSourceId = 0;
	[Export] public int GridDebugSourceId = 6;

	public TileMapLayer FloorLayer;
	public TileMapLayer DoorLayer;
	public TileMapLayer GridDebugLayer;
	public Node2D PropSort;
	public Node2D Player;
	public Vector2 GridOffset = Vector2.Zero;

	public const float TileSize = 16.0f;

	public static readonly Vector2I AtlasCoordsLeft = new(0, 0);
	public static readonly Vector2I AtlasCoordsMid = new(1, 0);
	public static readonly Vector2I AtlasCoordsRight = new(2, 0);

	public Vector2 GridToWorld(Vector2I gridPos)
	{
		return FloorLayer.MapToLocal(gridPos) + GridOffset;
	}

	public Vector2I WorldToGrid(Vector2 worldPos)
	{
		return FloorLayer.LocalToMap(worldPos - GridOffset);
	}

	protected Vector2 AutoCenterFloor()
	{
		var topLeft = FloorLayer.MapToLocal(new Vector2I(0, 0));
		var topRight = FloorLayer.MapToLocal(new Vector2I(GridWidth - 1, 0));
		var bottomLeft = FloorLayer.MapToLocal(new Vector2I(0, GridHeight - 1));
		var bottomRight = FloorLayer.MapToLocal(new Vector2I(GridWidth - 1, GridHeight - 1));

		var minX = Mathf.Min(Mathf.Min(topLeft.X, topRight.X), Mathf.Min(bottomLeft.X, bottomRight.X));
		var maxX = Mathf.Max(Mathf.Max(topLeft.X, topRight.X), Mathf.Max(bottomLeft.X, bottomRight.X));
		var minY = Mathf.Min(Mathf.Min(topLeft.Y, topRight.Y), Mathf.Min(bottomLeft.Y, bottomRight.Y));
		var maxY = Mathf.Max(Mathf.Max(topLeft.Y, topRight.Y), Mathf.Max(bottomLeft.Y, bottomRight.Y));

		var center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
		return GridAnchor - center;
	}

	protected void CreateFloor()
	{
		for (int y = 0; y < GridHeight; y++)
		{
			for (int x = 0; x < GridWidth; x++)
			{
				FloorLayer.SetCell(new Vector2I(x, y), FloorSourceId, AtlasCoordsLeft);
			}
		}
	}

	protected void CreateDebugGrid()
	{
		GridDebugLayer.Clear();
		for (int y = 0; y < GridHeight; y++)
		{
			for (int x = 0; x < GridWidth; x++)
			{
				GridDebugLayer.SetCell(new Vector2I(x, y), GridDebugSourceId, AtlasCoordsLeft);
			}
		}
	}

	protected virtual void OnRoomReady() { }
	protected virtual void OnRoomProcess(double delta) { }

	public override void _Ready()
	{
		FloorLayer = GetNode<TileMapLayer>("FloorLayer");
		if (FloorLayer == null)
		{
			GD.PrintErr($"{GetClass()}: FloorLayer not found!");
			return;
		}

		DoorLayer = GetNode<TileMapLayer>("DoorLayer");
		if (DoorLayer == null)
		{
			GD.PrintErr($"{GetClass()}: DoorLayer not found!");
			return;
		}

		GridDebugLayer = GetNode<TileMapLayer>("GridDebugLayer");
		if (GridDebugLayer == null)
		{
			GD.PrintErr($"{GetClass()}: GridDebugLayer not found!");
			return;
		}

		PropSort = GetNode<Node2D>("PropSort");

		ZIndex = 1001;
		ZAsRelative = false;
		FloorLayer.ZAsRelative = false;
		DoorLayer.ZAsRelative = false;
		GridDebugLayer.ZAsRelative = false;
		PropSort.ZAsRelative = false;

		CreateFloor();
		if (AutoCenter)
		{
			GridOffset = AutoCenterFloor();
		}
		else
		{
			GridOffset = GridAnchor;
		}
		FloorLayer.Position = GridOffset;
		DoorLayer.Position = GridOffset;
		GridDebugLayer.Position = GridOffset;

		OnRoomReady();
	}

	public override void _Process(double delta)
	{
		OnRoomProcess(delta);
	}
}
