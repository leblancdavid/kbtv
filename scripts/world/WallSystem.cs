using Godot;
using System.Collections.Generic;

public partial class WallSystem : Node
{
	[ExportGroup("Wall Settings")]
	[Export] public int DoorRow = 3;
	[Export] public int DoorHeightTiles = 2;
	[Export] public int WindowStartColumn = 3;
	[Export] public int WindowEndColumn = 9;
	[Export] public bool EnableSouthWall = true;
	[Export] public bool EnableSouthDoor = false;
	[Export] public int SouthDoorRow = 3;

	[ExportGroup("Grid (set when not using RoomBase)")]
	[Export] public int GridWidthOverride = 14;
	[Export] public int GridHeightOverride = 10;

	[ExportGroup("TileMap Sources")]
	[Export] public int WallNorthSourceId = 1;
	[Export] public int WallSouthSourceId = 2;
	[Export] public int WallWestSourceId = 3;
	[Export] public int WallEastSourceId = 4;
	[Export] public int WallSouthStripSourceId = 5;
	[Export] public int WallWindowSourceId = 7;

	[ExportGroup("Wall Textures")]
	[Export] public Texture2D CustomNorthWallTexture;
	[Export] public Texture2D CustomSouthWallTexture;
	[Export] public Texture2D CustomSideWallTexture;

	[ExportGroup("Wall Dimensions")]
	[Export] public float WallThickness = 8.0f;
	[Export] public float WallStripWidth = 16.0f;

	private RoomBase _room;
	private Node2D _propSort;
	private StaticBody2D _wallColliderBody;

	private readonly List<Rect2> _debugWallRects = new();
	private Rect2 _debugDoorRect = new Rect2(0, 0, 0, 0);
	private Rect2 _debugSouthDoorRect = new Rect2(0, 0, 0, 0);

	private readonly List<Sprite2D> _northWallSprites = new();
	private readonly List<Sprite2D> _northWallStripSprites = new();
	private readonly List<Sprite2D> _southWallSprites = new();
	private readonly List<Sprite2D> _southWallStripSprites = new();
	private readonly List<Sprite2D> _westWallSprites = new();
	private readonly List<Sprite2D> _eastWallSprites = new();
	private readonly List<Sprite2D> _southCornerSprites = new();
	private readonly List<Sprite2D> _windowSprites = new();

	public List<Rect2> DebugWallRects => _debugWallRects;
	public Rect2 DebugDoorRect => _debugDoorRect;
	public Rect2 DebugSouthDoorRect => _debugSouthDoorRect;

	public void Initialize(RoomBase room)
	{
		_room = room;
		_propSort = room.GetNode<Node2D>("PropSort");
	}

	public void Initialize(IRoomSection roomSection)
	{
		_room = null;
		_floorLayer = roomSection.FloorLayer;
		_doorLayer = roomSection.DoorLayer;
		_gridAnchor = roomSection.GridOffset;
		_gridHeight = roomSection.GridHeight;
		_gridWidth = roomSection.GridWidth;
		GridWidthOverride = roomSection.GridWidth;
		GridHeightOverride = roomSection.GridHeight;
		_propSort = roomSection.PropSort;
	}

	private TileMapLayer _floorLayer;
	private TileMapLayer _doorLayer;
	private Vector2 _gridAnchor;
	private int _gridHeight;
	private int _gridWidth;
	private Node _owner;

	private Vector2 GetGridToWorld(Vector2I gridPos)
	{
		if (_room != null)
			return _room.GridToWorld(gridPos);
		return _floorLayer.MapToLocal(gridPos) + _gridAnchor;
	}

	private int GetGridHeight()
	{
		if (_room != null)
			return _room.GridHeight;
		return GridHeightOverride;
	}

	private int GetGridWidth()
	{
		if (_room != null)
			return _room.GridWidth;
		return GridWidthOverride;
	}

	private TileMapLayer GetFloorLayer()
	{
		if (_room != null)
			return _room.FloorLayer;
		return _floorLayer;
	}

	private TileMapLayer GetDoorLayer()
	{
		if (_room != null)
			return _room.DoorLayer;
		return _doorLayer;
	}

	private Vector2 GetGridOffset()
	{
		if (_room != null)
			return _room.GridOffset;
		return _gridAnchor;
	}

	public void CreateWalls()
	{
		ClearWallSprites();

		var northTexture = CustomNorthWallTexture ?? GD.Load<Texture2D>("res://assets/tiles/topdown/control_room_north_atlas.png");
		var southTexture = CustomSouthWallTexture ?? GD.Load<Texture2D>("res://assets/tiles/topdown/wall_south_atlas.png");
		var sideTexture = CustomSideWallTexture ?? GD.Load<Texture2D>("res://assets/tiles/topdown/wall_side_atlas.png");
		var southStripTexture = GD.Load<Texture2D>("res://assets/tiles/topdown/wall_south_strip.png");
		var windowTexture = GD.Load<Texture2D>("res://assets/tiles/topdown/wall_window_atlas.png");

		var gridHeight = GetGridHeight();
		var gridWidth = GetGridWidth();
		var doorY = Mathf.Clamp(DoorRow, 0, gridHeight - 1);

		for (int x = 0; x < gridWidth; x++)
		{
			if (x >= WindowStartColumn && x <= WindowEndColumn)
			{
				CreateWindow(x, windowTexture);
				continue;
			}

			var atlas = ResolveHorizontalAtlas(x, gridWidth);
			var gridPos = new Vector2I(x, -1);
			var sprite = CreateWallSprite(northTexture, atlas, gridPos);
			_northWallSprites.Add(sprite);
			_propSort.AddChild(sprite);

			var stripSprite = CreateStripSprite(southStripTexture, gridPos);
			_northWallStripSprites.Add(stripSprite);
			stripSprite.Visible = false;
			_propSort.AddChild(stripSprite);
		}

		for (int x = 0; x < gridWidth; x++)
		{
			if (!EnableSouthWall)
				continue;

			var atlas = ResolveHorizontalAtlas(x, gridWidth);
			var gridPos = new Vector2I(x, gridHeight);
			var sprite = CreateWallSprite(southTexture, atlas, gridPos);
			_southWallSprites.Add(sprite);
			_propSort.AddChild(sprite);

			var stripSprite = CreateStripSprite(southStripTexture, gridPos);
			_southWallStripSprites.Add(stripSprite);
			_propSort.AddChild(stripSprite);
		}

		if (EnableSouthWall)
		{
			var leftCorner = CreateWallSprite(southTexture, RoomBase.AtlasCoordsLeft, new Vector2I(-1, gridHeight));
			var rightCorner = CreateWallSprite(southTexture, RoomBase.AtlasCoordsRight, new Vector2I(gridWidth, gridHeight));
			_southCornerSprites.Add(leftCorner);
			_propSort.AddChild(leftCorner);
			_southCornerSprites.Add(rightCorner);
			_propSort.AddChild(rightCorner);
		}

		for (int y = -1; y < gridHeight; y++)
		{
			var atlasY = ResolveVerticalAtlas(y, gridHeight);
			var sprite = CreateWallSprite(sideTexture, atlasY, new Vector2I(-1, y));
			sprite.FlipH = true;
			_westWallSprites.Add(sprite);
			_propSort.AddChild(sprite);
		}

		for (int y = -1; y < gridHeight; y++)
		{
			var atlasY = y == doorY ? RoomBase.AtlasCoordsRight : ResolveVerticalAtlas(y, gridHeight);
			var sprite = CreateWallSprite(sideTexture, atlasY, new Vector2I(gridWidth, y));
			_eastWallSprites.Add(sprite);
			_propSort.AddChild(sprite);
		}

		// Door is handled by DoorLayer - keep it in front
		GetDoorLayer().SetCell(new Vector2I(gridWidth, doorY), WallEastSourceId, RoomBase.AtlasCoordsRight);

		if (EnableSouthDoor)
		{
			var southDoorY = Mathf.Clamp(SouthDoorRow, 0, gridHeight - 1);
			GetDoorLayer().SetCell(new Vector2I(southDoorY, gridHeight), WallSouthSourceId, RoomBase.AtlasCoordsRight);
		}
	}

	public void CreateWallColliders()
	{
		_wallColliderBody?.QueueFree();
		_debugWallRects.Clear();
		_debugDoorRect = new Rect2(0, 0, 0, 0);

		var gridWidth = GetGridWidth();
		var gridHeight = GetGridHeight();
		var floorLayer = GetFloorLayer();
		var gridOffset = GetGridOffset();

		_wallColliderBody = new StaticBody2D { Name = "WallColliders", Position = gridOffset };
		AddChild(_wallColliderBody);

		for (int x = 0; x < gridWidth; x++)
		{
			var cellPos = floorLayer.MapToLocal(new Vector2I(x, -1));
			AddWallCollider(new Rect2(
				cellPos.X - RoomBase.TileSize * 0.5f,
				cellPos.Y - RoomBase.TileSize * 0.5f,
				RoomBase.TileSize,
				RoomBase.TileSize
			));
		}

		for (int x = 0; x < gridWidth; x++)
		{
			if (!EnableSouthWall)
				continue;

			var cellPos = floorLayer.MapToLocal(new Vector2I(x, gridHeight));

			if (EnableSouthDoor)
			{
				var southDoorY = Mathf.Clamp(SouthDoorRow, 0, gridHeight - 1);
				if (x >= southDoorY && x < southDoorY + DoorHeightTiles)
					continue;
			}

			AddWallCollider(new Rect2(
				cellPos.X - RoomBase.TileSize * 0.5f,
				cellPos.Y - RoomBase.TileSize * 0.5f,
				RoomBase.TileSize,
				RoomBase.TileSize
			));
		}

		for (int y = 0; y < gridHeight; y++)
		{
			var cellPos = floorLayer.MapToLocal(new Vector2I(-1, y));
			AddWallCollider(new Rect2(
				cellPos.X - WallStripWidth * 0.5f,
				cellPos.Y - RoomBase.TileSize * 0.5f,
				WallStripWidth,
				RoomBase.TileSize
			));
		}

		var nwPos = floorLayer.MapToLocal(new Vector2I(-1, -1));
		AddWallCollider(new Rect2(
			nwPos.X - WallStripWidth * 0.5f,
			nwPos.Y - RoomBase.TileSize * 0.5f,
			WallStripWidth,
			RoomBase.TileSize
		));

		var doorY = Mathf.Clamp(DoorRow, 0, gridHeight - 1);
		for (int y = 0; y < gridHeight; y++)
		{
			var cellPos = floorLayer.MapToLocal(new Vector2I(gridWidth, y));

			if (y >= doorY && y < doorY + DoorHeightTiles)
				continue;

			AddWallCollider(new Rect2(
				cellPos.X - WallStripWidth * 0.5f,
				cellPos.Y - RoomBase.TileSize * 0.5f,
				WallStripWidth,
				RoomBase.TileSize
			));
		}

		var nePos = floorLayer.MapToLocal(new Vector2I(gridWidth, -1));
		AddWallCollider(new Rect2(
			nePos.X - WallStripWidth * 0.5f,
			nePos.Y - RoomBase.TileSize * 0.5f,
			WallStripWidth,
			RoomBase.TileSize
		));

		if (EnableSouthWall)
		{
			var swPos = floorLayer.MapToLocal(new Vector2I(-1, gridHeight));
			AddWallCollider(new Rect2(
				swPos.X - WallStripWidth * 0.5f,
				swPos.Y - RoomBase.TileSize * 0.5f,
				WallStripWidth,
				RoomBase.TileSize
			));

			var sePos = floorLayer.MapToLocal(new Vector2I(gridWidth, gridHeight));
			AddWallCollider(new Rect2(
				sePos.X - WallStripWidth * 0.5f,
				sePos.Y - RoomBase.TileSize * 0.5f,
				WallStripWidth,
				RoomBase.TileSize
			));
		}

		var doorCellPos = floorLayer.MapToLocal(new Vector2I(gridWidth, doorY));
		var doorTop = doorCellPos.Y - RoomBase.TileSize * 0.5f;
		var doorBottom = doorTop + (DoorHeightTiles * RoomBase.TileSize);
		_debugDoorRect = new Rect2(
			_wallColliderBody.ToGlobal(new Vector2(doorCellPos.X - RoomBase.TileSize * 0.5f, doorTop)),
			new Vector2(WallStripWidth, doorBottom - doorTop)
		);

		if (EnableSouthDoor)
		{
			var southDoorY = Mathf.Clamp(SouthDoorRow, 0, gridHeight - 1);
			var southDoorCellPos = floorLayer.MapToLocal(new Vector2I(southDoorY, gridHeight));
			var southDoorTop = southDoorCellPos.Y - RoomBase.TileSize * 0.5f;
			var southDoorBottom = southDoorTop + (DoorHeightTiles * RoomBase.TileSize);
			_debugSouthDoorRect = new Rect2(
				_wallColliderBody.ToGlobal(new Vector2(southDoorCellPos.X - RoomBase.TileSize * 0.5f, southDoorTop)),
				new Vector2(RoomBase.TileSize, southDoorBottom - southDoorTop)
			);
		}
	}

	public void UpdateVisibility(Node2D player)
	{
		if (player == null)
			return;

		var playerCollision = player.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (playerCollision?.Shape is not RectangleShape2D playerShape)
			return;

		var gridWidth = GetGridWidth();
		var gridHeight = GetGridHeight();
		var floorLayer = GetFloorLayer();

		var playerSize = playerShape.Size;
		var playerRect = new Rect2(
			playerCollision.GlobalPosition - (playerSize * 0.5f),
			playerSize
		);

		var roomLeft = floorLayer.ToGlobal(floorLayer.MapToLocal(new Vector2I(0, 0))).X;
		var roomWidth = gridWidth * RoomBase.TileSize;

		var floorTopY = floorLayer.ToGlobal(floorLayer.MapToLocal(new Vector2I(0, 0))).Y - RoomBase.TileSize * 0.5f;
		var northRect = new Rect2(roomLeft, floorTopY - 64.0f, roomWidth, 64.0f);
		var hideNorth = northRect.Intersects(playerRect);

		foreach (var sprite in _northWallSprites)
			sprite.Visible = !hideNorth;
		foreach (var sprite in _northWallStripSprites)
			sprite.Visible = hideNorth;

		var southWallBottomY = floorLayer.ToGlobal(floorLayer.MapToLocal(new Vector2I(0, gridHeight))).Y - RoomBase.TileSize * 0.5f;
		var southRect = new Rect2(roomLeft, southWallBottomY - 64.0f, roomWidth, 64.0f);
		var hideSouth = EnableSouthWall && southRect.Intersects(playerRect);

		foreach (var sprite in _southWallSprites)
			sprite.Visible = !hideSouth;
		foreach (var sprite in _southWallStripSprites)
			sprite.Visible = hideSouth;
		foreach (var sprite in _southCornerSprites)
			sprite.Visible = EnableSouthWall;

		GetDoorLayer().Visible = true;
	}

	private void ClearWallSprites()
	{
		foreach (var sprite in _northWallSprites) sprite.QueueFree();
		foreach (var sprite in _northWallStripSprites) sprite.QueueFree();
		foreach (var sprite in _southWallSprites) sprite.QueueFree();
		foreach (var sprite in _southWallStripSprites) sprite.QueueFree();
		foreach (var sprite in _westWallSprites) sprite.QueueFree();
		foreach (var sprite in _eastWallSprites) sprite.QueueFree();
		foreach (var sprite in _southCornerSprites) sprite.QueueFree();

		_northWallSprites.Clear();
		_northWallStripSprites.Clear();
		_southWallSprites.Clear();
		_southWallStripSprites.Clear();
		_westWallSprites.Clear();
		_eastWallSprites.Clear();
		_southCornerSprites.Clear();
	}

	private Sprite2D CreateWallSprite(Texture2D texture, Vector2I atlasCoords, Vector2I gridCoords)
	{
		var position = GetGridToWorld(gridCoords);

		return new Sprite2D
		{
			Texture = texture,
			Position = position,
			Offset = new Vector2(0, -24),
			Hframes = 4,
			Vframes = 1,
			Frame = atlasCoords.X,
			ZIndex = (int)position.Y
		};
	}

	private Sprite2D CreateStripSprite(Texture2D texture, Vector2I gridCoords)
	{
		var position = GetGridToWorld(gridCoords);

		return new Sprite2D
		{
			Texture = texture,
			Position = position,
			Offset = new Vector2(0, -24),
			Hframes = 1,
			Vframes = 1,
			Frame = 0,
			ZIndex = (int)position.Y
		};
	}

	private void CreateWindow(int column, Texture2D texture)
	{
		var gridPos = new Vector2I(column, -1);
		var position = GetGridToWorld(gridPos);

		int frame = column - WindowStartColumn;

		var sprite = new Sprite2D
		{
			Texture = texture,
			Position = position,
			Offset = new Vector2(0, -24),
			Hframes = 7,
			Vframes = 1,
			Frame = frame,
			ZIndex = (int)position.Y
		};

		_windowSprites.Add(sprite);
		_propSort.AddChild(sprite);
	}

	private void AddWallCollider(Rect2 rect)
	{
		var shape = new RectangleShape2D { Size = rect.Size };
		var collision = new CollisionShape2D { Shape = shape };
		collision.Position = rect.Position + (rect.Size * 0.5f);
		_wallColliderBody.AddChild(collision);
		_debugWallRects.Add(new Rect2(_wallColliderBody.ToGlobal(rect.Position), rect.Size));
	}

	private static Vector2I ResolveHorizontalAtlas(int x, int width)
	{
		if (x == 0) return RoomBase.AtlasCoordsLeft;
		if (x == width - 1) return RoomBase.AtlasCoordsRight;
		return RoomBase.AtlasCoordsMid;
	}

	private static Vector2I ResolveVerticalAtlas(int y, int height)
	{
		if (y <= 0) return RoomBase.AtlasCoordsLeft;
		if (y == height - 1) return RoomBase.AtlasCoordsRight;
		return RoomBase.AtlasCoordsMid;
	}
}
