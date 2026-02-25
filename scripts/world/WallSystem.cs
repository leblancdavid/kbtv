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

	public void CreateWalls()
	{
		ClearWallSprites();

		var northTexture = CustomNorthWallTexture ?? GD.Load<Texture2D>("res://assets/tiles/topdown/control_room_north_atlas.png");
		var southTexture = CustomSouthWallTexture ?? GD.Load<Texture2D>("res://assets/tiles/topdown/wall_south_atlas.png");
		var sideTexture = CustomSideWallTexture ?? GD.Load<Texture2D>("res://assets/tiles/topdown/wall_side_atlas.png");
		var southStripTexture = GD.Load<Texture2D>("res://assets/tiles/topdown/wall_south_strip.png");
		var windowTexture = GD.Load<Texture2D>("res://assets/tiles/topdown/wall_window_atlas.png");

		var doorY = Mathf.Clamp(DoorRow, 0, _room.GridHeight - 1);

		for (int x = 0; x < _room.GridWidth; x++)
		{
			if (x >= WindowStartColumn && x <= WindowEndColumn)
			{
				CreateWindow(x, windowTexture);
				continue;
			}

			var atlas = ResolveHorizontalAtlas(x, _room.GridWidth);
			var gridPos = new Vector2I(x, -1);
			var sprite = CreateWallSprite(northTexture, atlas, gridPos);
			_northWallSprites.Add(sprite);
			_propSort.AddChild(sprite);

			var stripSprite = CreateStripSprite(southStripTexture, gridPos);
			_northWallStripSprites.Add(stripSprite);
			stripSprite.Visible = false;
			_propSort.AddChild(stripSprite);
		}

		for (int x = 0; x < _room.GridWidth; x++)
		{
			if (!EnableSouthWall)
				continue;

			var atlas = ResolveHorizontalAtlas(x, _room.GridWidth);
			var gridPos = new Vector2I(x, _room.GridHeight);
			var sprite = CreateWallSprite(southTexture, atlas, gridPos);
			_southWallSprites.Add(sprite);
			_propSort.AddChild(sprite);

			var stripSprite = CreateStripSprite(southStripTexture, gridPos);
			_southWallStripSprites.Add(stripSprite);
			_propSort.AddChild(stripSprite);
		}

		if (EnableSouthWall)
		{
			var leftCorner = CreateWallSprite(southTexture, RoomBase.AtlasCoordsLeft, new Vector2I(-1, _room.GridHeight));
			var rightCorner = CreateWallSprite(southTexture, RoomBase.AtlasCoordsRight, new Vector2I(_room.GridWidth, _room.GridHeight));
			_southCornerSprites.Add(leftCorner);
			_propSort.AddChild(leftCorner);
			_southCornerSprites.Add(rightCorner);
			_propSort.AddChild(rightCorner);
		}

		for (int y = -1; y < _room.GridHeight; y++)
		{
			var atlasY = ResolveVerticalAtlas(y, _room.GridHeight);
			var sprite = CreateWallSprite(sideTexture, atlasY, new Vector2I(-1, y));
			sprite.FlipH = true;
			_westWallSprites.Add(sprite);
			_propSort.AddChild(sprite);
		}

		for (int y = -1; y < _room.GridHeight; y++)
		{
			var atlasY = y == doorY ? RoomBase.AtlasCoordsRight : ResolveVerticalAtlas(y, _room.GridHeight);
			var sprite = CreateWallSprite(sideTexture, atlasY, new Vector2I(_room.GridWidth, y));
			_eastWallSprites.Add(sprite);
			_propSort.AddChild(sprite);
		}

		// Door is handled by DoorLayer - keep it in front
		_room.DoorLayer.SetCell(new Vector2I(_room.GridWidth, doorY), WallEastSourceId, RoomBase.AtlasCoordsRight);

		if (EnableSouthDoor)
		{
			var southDoorY = Mathf.Clamp(SouthDoorRow, 0, _room.GridHeight - 1);
			_room.DoorLayer.SetCell(new Vector2I(southDoorY, _room.GridHeight), WallSouthSourceId, RoomBase.AtlasCoordsRight);
		}
	}

	public void CreateWallColliders()
	{
		_wallColliderBody?.QueueFree();
		_debugWallRects.Clear();
		_debugDoorRect = new Rect2(0, 0, 0, 0);

		_wallColliderBody = new StaticBody2D { Name = "WallColliders", Position = _room.GridOffset };
		_room.AddChild(_wallColliderBody);

		for (int x = 0; x < _room.GridWidth; x++)
		{
			var cellPos = _room.FloorLayer.MapToLocal(new Vector2I(x, -1));
			AddWallCollider(new Rect2(
				cellPos.X - RoomBase.TileSize * 0.5f,
				cellPos.Y - RoomBase.TileSize * 0.5f,
				RoomBase.TileSize,
				RoomBase.TileSize
			));
		}

		for (int x = 0; x < _room.GridWidth; x++)
		{
			if (!EnableSouthWall)
				continue;

			var cellPos = _room.FloorLayer.MapToLocal(new Vector2I(x, _room.GridHeight));

			if (EnableSouthDoor)
			{
				var southDoorY = Mathf.Clamp(SouthDoorRow, 0, _room.GridHeight - 1);
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

		for (int y = 0; y < _room.GridHeight; y++)
		{
			var cellPos = _room.FloorLayer.MapToLocal(new Vector2I(-1, y));
			AddWallCollider(new Rect2(
				cellPos.X - WallStripWidth * 0.5f,
				cellPos.Y - RoomBase.TileSize * 0.5f,
				WallStripWidth,
				RoomBase.TileSize
			));
		}

		var nwPos = _room.FloorLayer.MapToLocal(new Vector2I(-1, -1));
		AddWallCollider(new Rect2(
			nwPos.X - WallStripWidth * 0.5f,
			nwPos.Y - RoomBase.TileSize * 0.5f,
			WallStripWidth,
			RoomBase.TileSize
		));

		var doorY = Mathf.Clamp(DoorRow, 0, _room.GridHeight - 1);
		for (int y = 0; y < _room.GridHeight; y++)
		{
			var cellPos = _room.FloorLayer.MapToLocal(new Vector2I(_room.GridWidth, y));

			if (y >= doorY && y < doorY + DoorHeightTiles)
				continue;

			AddWallCollider(new Rect2(
				cellPos.X - WallStripWidth * 0.5f,
				cellPos.Y - RoomBase.TileSize * 0.5f,
				WallStripWidth,
				RoomBase.TileSize
			));
		}

		var nePos = _room.FloorLayer.MapToLocal(new Vector2I(_room.GridWidth, -1));
		AddWallCollider(new Rect2(
			nePos.X - WallStripWidth * 0.5f,
			nePos.Y - RoomBase.TileSize * 0.5f,
			WallStripWidth,
			RoomBase.TileSize
		));

		if (EnableSouthWall)
		{
			var swPos = _room.FloorLayer.MapToLocal(new Vector2I(-1, _room.GridHeight));
			AddWallCollider(new Rect2(
				swPos.X - WallStripWidth * 0.5f,
				swPos.Y - RoomBase.TileSize * 0.5f,
				WallStripWidth,
				RoomBase.TileSize
			));

			var sePos = _room.FloorLayer.MapToLocal(new Vector2I(_room.GridWidth, _room.GridHeight));
			AddWallCollider(new Rect2(
				sePos.X - WallStripWidth * 0.5f,
				sePos.Y - RoomBase.TileSize * 0.5f,
				WallStripWidth,
				RoomBase.TileSize
			));
		}

		var doorCellPos = _room.FloorLayer.MapToLocal(new Vector2I(_room.GridWidth, doorY));
		var doorTop = doorCellPos.Y - RoomBase.TileSize * 0.5f;
		var doorBottom = doorTop + (DoorHeightTiles * RoomBase.TileSize);
		_debugDoorRect = new Rect2(
			_wallColliderBody.ToGlobal(new Vector2(doorCellPos.X - RoomBase.TileSize * 0.5f, doorTop)),
			new Vector2(WallStripWidth, doorBottom - doorTop)
		);

		if (EnableSouthDoor)
		{
			var southDoorY = Mathf.Clamp(SouthDoorRow, 0, _room.GridHeight - 1);
			var southDoorCellPos = _room.FloorLayer.MapToLocal(new Vector2I(southDoorY, _room.GridHeight));
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

		var playerSize = playerShape.Size;
		var playerRect = new Rect2(
			playerCollision.GlobalPosition - (playerSize * 0.5f),
			playerSize
		);

		var roomLeft = _room.FloorLayer.ToGlobal(_room.FloorLayer.MapToLocal(new Vector2I(0, 0))).X;
		var roomWidth = _room.GridWidth * RoomBase.TileSize;

		var floorTopY = _room.FloorLayer.ToGlobal(_room.FloorLayer.MapToLocal(new Vector2I(0, 0))).Y - RoomBase.TileSize * 0.5f;
		var northRect = new Rect2(roomLeft, floorTopY - 64.0f, roomWidth, 64.0f);
		var hideNorth = northRect.Intersects(playerRect);

		foreach (var sprite in _northWallSprites)
			sprite.Visible = !hideNorth;
		foreach (var sprite in _northWallStripSprites)
			sprite.Visible = hideNorth;

		var southWallBottomY = _room.FloorLayer.ToGlobal(_room.FloorLayer.MapToLocal(new Vector2I(0, _room.GridHeight))).Y - RoomBase.TileSize * 0.5f;
		var southRect = new Rect2(roomLeft, southWallBottomY - 64.0f, roomWidth, 64.0f);
		var hideSouth = EnableSouthWall && southRect.Intersects(playerRect);

		foreach (var sprite in _southWallSprites)
			sprite.Visible = !hideSouth;
		foreach (var sprite in _southWallStripSprites)
			sprite.Visible = hideSouth;
		foreach (var sprite in _southCornerSprites)
			sprite.Visible = EnableSouthWall;

		_room.DoorLayer.Visible = true;
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
		var position = _room.GridToWorld(gridCoords);

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
		var position = _room.GridToWorld(gridCoords);

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
		var position = _room.GridToWorld(gridPos);

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
