using Godot;
using System.Collections.Generic;

public partial class ControlRoom : Node2D
{
	[Export] private Vector2 _gridAnchor = new Vector2(320, 180);

	[Export] private float _southWallHideOffset = 8.0f;

	[Export] private int _gridWidth = 14;
	[Export] private int _gridHeight = 10;
	[Export] private int _doorRow = 3;
	[Export] private int _doorHeightTiles = 2;

	// TileSet source IDs from topdown_tileset.tres
	private const int FLOOR_SOURCE_ID = 0;
	private const int WALL_NORTH_SOURCE_ID = 1;
	private const int WALL_SOUTH_SOURCE_ID = 2;
	private const int WALL_WEST_SOURCE_ID = 3;
	private const int WALL_EAST_SOURCE_ID = 4;
	private const int WALL_SOUTH_STRIP_SOURCE_ID = 5;
	private const int GRID_DEBUG_SOURCE_ID = 6;
	private const int WALL_NORTH_STRIP_SOURCE_ID = 5;

	// Atlas coordinates
	private static readonly Vector2I ATLAS_COORDS_LEFT = new Vector2I(0, 0);
	private static readonly Vector2I ATLAS_COORDS_MID = new Vector2I(1, 0);
	private static readonly Vector2I ATLAS_COORDS_RIGHT = new Vector2I(2, 0);
	private static readonly Vector2I ATLAS_COORDS_DOOR = new Vector2I(3, 0);

	private TileMapLayer _floorLayer;
	private TileMapLayer _doorLayer;
	private TileMapLayer _gridDebugLayer;
	private Node2D _propSort;
	private Vector2 _gridOffset = Vector2.Zero;
	private Node2D _player;
	private StaticBody2D _wallColliderBody;
	private readonly List<Rect2> _debugWallRects = new();
	private readonly List<Rect2> _debugPropRects = new();
	private readonly List<Vector2> _debugPropPivots = new();
	private Rect2 _debugPlayerRect;
	private Rect2 _debugDoorRect;
	private bool _debugVisible;
	private float _tableSortY;

	// Wall sprites for visibility toggling
	private readonly List<Sprite2D> _northWallSprites = new();
	private readonly List<Sprite2D> _northWallStripSprites = new();
	private readonly List<Sprite2D> _southWallSprites = new();
	private readonly List<Sprite2D> _southWallStripSprites = new();
	private readonly List<Sprite2D> _westWallSprites = new();
	private readonly List<Sprite2D> _eastWallSprites = new();
	private readonly List<Sprite2D> _southCornerSprites = new();

	private const float TileSize = 16.0f;
	private const float WallThickness = 8.0f;
	private const float WallStripWidth = 16.0f;

	public override void _Ready()
	{
		_floorLayer = GetNode<TileMapLayer>("FloorLayer");
		if (_floorLayer == null)
		{
			GD.PrintErr("ControlRoom: FloorLayer not found!");
			return;
		}

		_doorLayer = GetNode<TileMapLayer>("DoorLayer");
		if (_doorLayer == null)
		{
			GD.PrintErr("ControlRoom: DoorLayer not found!");
			return;
		}

		_gridDebugLayer = GetNode<TileMapLayer>("GridDebugLayer");
		if (_gridDebugLayer == null)
		{
			GD.PrintErr("ControlRoom: GridDebugLayer not found!");
			return;
		}

		_propSort = GetNode<Node2D>("PropSort");

		ZIndex = 1001;
		ZAsRelative = false;
		_floorLayer.ZAsRelative = false;
		_doorLayer.ZAsRelative = false;
		_gridDebugLayer.ZAsRelative = false;
		_propSort.ZAsRelative = false;

		// Create floor tiles on the TileMapLayer
		CreateFloor();

		// Auto-center the floor grid around the anchor
		_gridOffset = AutoCenterFloor();
		_floorLayer.Position = _gridOffset;
		_doorLayer.Position = _gridOffset;
		_gridDebugLayer.Position = _gridOffset;

		// Create walls as sprites
		CreateWalls();

		// Create debug grid overlay
		CreateDebugGrid();

		// Create wall collisions
		CreateWallColliders();

		// Create props
		CreateProps();

		// Position player in center
		_player = GetNode<Node2D>("PropSort/Player");
		if (_player != null)
		{
			var centerX = _gridWidth / 2;
			var centerY = _gridHeight / 2;
			_player.Position = _floorLayer.MapToLocal(new Vector2I(centerX, centerY)) + _gridOffset + new Vector2(0, 8);
		}

		UpdateWallVisibility();
	}

	public override void _Process(double delta)
	{
		UpdateWallVisibility();
		if (_debugVisible)
		{
			UpdateDebugPlayerRect();
			UpdateDebugPropRects();
			QueueRedraw();
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_select"))
		{
			_gridDebugLayer.Visible = !_gridDebugLayer.Visible;
			_debugVisible = _gridDebugLayer.Visible;
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		if (!_debugVisible)
			return;

		var wallColor = new Color(1, 0, 0, 0.2f);
		var propColor = new Color(0, 1, 0, 0.2f);
		var playerColor = new Color(0, 0.5f, 1, 0.25f);
		var doorColor = new Color(1, 1, 0, 0.2f);
		var pivotColor = new Color(1, 0, 1, 0.9f);

		foreach (var rect in _debugWallRects)
			DrawRect(ToLocalRect(rect), wallColor, true);

		foreach (var rect in _debugPropRects)
			DrawRect(ToLocalRect(rect), propColor, true);

		DrawRect(ToLocalRect(_debugPlayerRect), playerColor, true);
		if (_debugDoorRect.Size != Vector2.Zero)
			DrawRect(ToLocalRect(_debugDoorRect), doorColor, true);

		// Draw pivot points
		if (_player != null)
			DrawCircle(ToLocal(_player.GlobalPosition), 3f, pivotColor);

		foreach (var pivot in _debugPropPivots)
			DrawCircle(ToLocal(pivot), 3f, pivotColor);
	}

	private void CreateFloor()
	{
		for (int y = 0; y < _gridHeight; y++)
		{
			for (int x = 0; x < _gridWidth; x++)
			{
				Vector2I coords = new Vector2I(x, y);
				_floorLayer.SetCell(coords, FLOOR_SOURCE_ID, ATLAS_COORDS_LEFT);
			}
		}
	}

	private void CreateWalls()
	{
		// Clear existing wall sprites
		foreach (var sprite in _northWallSprites)
			sprite.QueueFree();
		foreach (var sprite in _northWallStripSprites)
			sprite.QueueFree();
		foreach (var sprite in _southWallSprites)
			sprite.QueueFree();
		foreach (var sprite in _southWallStripSprites)
			sprite.QueueFree();
		foreach (var sprite in _westWallSprites)
			sprite.QueueFree();
		foreach (var sprite in _eastWallSprites)
			sprite.QueueFree();
		foreach (var sprite in _southCornerSprites)
			sprite.QueueFree();
		
		_northWallSprites.Clear();
		_northWallStripSprites.Clear();
		_southWallSprites.Clear();
		_southWallStripSprites.Clear();
		_westWallSprites.Clear();
		_eastWallSprites.Clear();
		_southCornerSprites.Clear();

		// Get PropSort node for direct sprite addition (for Y-sorting)
		var propSort = GetNode<Node2D>("PropSort");

		// Load wall textures
		var northTexture = GD.Load<Texture2D>("res://assets/tiles/topdown/wall_north_atlas.png");
		var southTexture = GD.Load<Texture2D>("res://assets/tiles/topdown/wall_south_atlas.png");
		var sideTexture = GD.Load<Texture2D>("res://assets/tiles/topdown/wall_side_atlas.png");
		var southStripTexture = GD.Load<Texture2D>("res://assets/tiles/topdown/wall_south_strip.png");

		var doorY = Mathf.Clamp(_doorRow, 0, _gridHeight - 1);

		// Create north wall (row -1)
		for (int x = 0; x < _gridWidth; x++)
		{
			var atlas = ResolveHorizontalAtlas(x, _gridWidth);
			var gridPos = new Vector2I(x, -1);
			var sprite = CreateWallSprite(northTexture, atlas, gridPos);
			_northWallSprites.Add(sprite);
			_propSort.AddChild(sprite);

			// North strip (for when hiding)
			var stripSprite = CreateStripSprite(southStripTexture, gridPos);
			_northWallStripSprites.Add(stripSprite);
			stripSprite.Visible = false;
			_propSort.AddChild(stripSprite);
		}

		// Create south wall (row _gridHeight)
		for (int x = 0; x < _gridWidth; x++)
		{
			var atlas = ResolveHorizontalAtlas(x, _gridWidth);
			var gridPos = new Vector2I(x, _gridHeight);
			var sprite = CreateWallSprite(southTexture, atlas, gridPos);
			_southWallSprites.Add(sprite);
			_propSort.AddChild(sprite);

			// South strip (for when hiding)
			var stripSprite = CreateStripSprite(southStripTexture, gridPos);
			_southWallStripSprites.Add(stripSprite);
			_propSort.AddChild(stripSprite);
		}

		// South corners
		var leftCorner = CreateWallSprite(southTexture, ATLAS_COORDS_LEFT, new Vector2I(-1, _gridHeight));
		leftCorner.Position = _floorLayer.MapToLocal(new Vector2I(0, _gridHeight)) + new Vector2(-16, 0) + _gridOffset;
		_southCornerSprites.Add(leftCorner);
		_propSort.AddChild(leftCorner);

		var rightCorner = CreateWallSprite(southTexture, ATLAS_COORDS_RIGHT, new Vector2I(_gridWidth, _gridHeight));
		rightCorner.Position = _floorLayer.MapToLocal(new Vector2I(_gridWidth - 1, _gridHeight)) + new Vector2(16, 0) + _gridOffset;
		_southCornerSprites.Add(rightCorner);
		_propSort.AddChild(rightCorner);

		// Create west wall (column -1)
		for (int y = -1; y < _gridHeight; y++)
		{
			var atlas = ResolveVerticalAtlas(y, _gridHeight);
			var sprite = CreateWallSprite(sideTexture, atlas, new Vector2I(-1, y));
			sprite.FlipH = true;
			_westWallSprites.Add(sprite);
			_propSort.AddChild(sprite);
		}

		// Create east wall (column _gridWidth) - with door gap
		for (int y = -1; y < _gridHeight; y++)
		{
			var atlas = y == doorY ? ATLAS_COORDS_DOOR : ResolveVerticalAtlas(y, _gridHeight);
			var sprite = CreateWallSprite(sideTexture, atlas, new Vector2I(_gridWidth, y));
			_eastWallSprites.Add(sprite);
			_propSort.AddChild(sprite);
		}

		// Door is handled by TileMapLayer - keep it in front
		_doorLayer.SetCell(new Vector2I(_gridWidth, doorY), WALL_EAST_SOURCE_ID, ATLAS_COORDS_DOOR);
	}

	private Sprite2D CreateWallSprite(Texture2D texture, Vector2I atlasCoords, Vector2I gridCoords)
	{
		var position = _floorLayer.MapToLocal(gridCoords) + _gridOffset;

		var sprite = new Sprite2D
		{
			Texture = texture,
			Position = position,
			Offset = new Vector2(0, -24),
			Hframes = 4,
			Vframes = 1,
			Frame = atlasCoords.X,
			ZIndex = (int)position.Y
		};

		return sprite;
	}

	private Sprite2D CreateStripSprite(Texture2D texture, Vector2I gridCoords)
	{
		var position = _floorLayer.MapToLocal(gridCoords) + _gridOffset;

		var sprite = new Sprite2D
		{
			Texture = texture,
			Position = position,
			Offset = new Vector2(0, -24),
			Hframes = 1,
			Vframes = 1,
			Frame = 0,
			ZIndex = (int)position.Y
		};

		return sprite;
	}

	private void CreateProps()
	{
		_debugPropRects.Clear();
		_debugPropPivots.Clear();

		var propSort = GetNode<Node2D>("PropSort");

		AddProp(propSort, "res://assets/tiles/props/speaker_stand.png", new Vector2I(2, 1), Vector2.Zero, true, new Vector2(24, 16));
		AddProp(propSort, "res://assets/tiles/props/speaker_stand.png", new Vector2I(10, 1), Vector2.Zero, true, new Vector2(24, 16));

		AddTableGroup(new Vector2I(6, 1));

		AddProp(propSort, "res://assets/tiles/props/audio_cabinet.png", new Vector2I(12, 1), Vector2.Zero, true, new Vector2(24, 16));
		AddProp(propSort, "res://assets/tiles/props/storage_shelf.png", new Vector2I(4, 10), new Vector2(0, -8), true, new Vector2(48, 32));
		AddProp(propSort, "res://assets/tiles/props/storage_shelf.png", new Vector2I(10, 10), new Vector2(0, -8), true, new Vector2(48, 32));
		AddProp(propSort, "res://assets/tiles/props/computer_chair.png", new Vector2I(6, 2), Vector2.Zero, false, Vector2.Zero);
	}

	private void AddTableGroup(Vector2I gridCoords)
	{
		var propSort = GetNode<Node2D>("PropSort");
		var group = new Node2D { Name = "TableGroup" };
		group.Position = _floorLayer.MapToLocal(gridCoords) + _gridOffset;
		propSort.AddChild(group);
		_debugPropPivots.Add(group.GlobalPosition);

		var tableTexture = GD.Load<Texture2D>("res://assets/tiles/props/studio_table.png");
		if (tableTexture == null)
		{
			GD.PrintErr("ControlRoom: Missing table texture");
			return;
		}

		var tableSprite = new Sprite2D
		{
			Texture = tableTexture,
			Position = new Vector2(0, -tableTexture.GetSize().Y * 0.5f)
		};
		group.AddChild(tableSprite);

		var tableBody = new StaticBody2D();
		var tableShape = new RectangleShape2D { Size = new Vector2(92, 14) };
		var tableCollision = new CollisionShape2D { Shape = tableShape };
		tableCollision.Position = new Vector2(0, -(tableShape.Size.Y * 0.5f));
		tableCollision.AddToGroup("debug_prop_collision");
		tableBody.AddChild(tableCollision);
		group.AddChild(tableBody);

		group.ZIndex = (int)group.GlobalPosition.Y;

		AddTabletopSprite(group, "res://assets/tiles/props/phone_line.png", new Vector2(-32, -26));
		AddTabletopSprite(group, "res://assets/tiles/props/sound_board.png", new Vector2(0, -26));
		AddTabletopSprite(group, "res://assets/tiles/props/computer_station.png", new Vector2(32, -38));
	}

	private void AddTabletopSprite(Node2D parent, string texturePath, Vector2 offset)
	{
		var texture = GD.Load<Texture2D>(texturePath);
		if (texture == null)
		{
			GD.PrintErr($"ControlRoom: Missing tabletop texture {texturePath}");
			return;
		}

		var sprite = new Sprite2D
		{
			Texture = texture,
			Position = offset
		};
		parent.AddChild(sprite);
	}

	private void CreateDebugGrid()
	{
		_gridDebugLayer.Clear();
		for (int y = 0; y < _gridHeight; y++)
		{
			for (int x = 0; x < _gridWidth; x++)
			{
				_gridDebugLayer.SetCell(new Vector2I(x, y), GRID_DEBUG_SOURCE_ID, ATLAS_COORDS_LEFT);
			}
		}
	}

	private void AddProp(Node2D parent, string texturePath, Vector2I gridCoords, Vector2 pixelOffset, bool collidable, Vector2 colliderSize)
	{
		var texture = GD.Load<Texture2D>(texturePath);
		if (texture == null)
		{
			GD.PrintErr($"ControlRoom: Missing prop texture {texturePath}");
			return;
		}

		var basePosition = _floorLayer.MapToLocal(gridCoords) + pixelOffset + _gridOffset;
		var root = collidable ? new StaticBody2D() : new Node2D();
		root.Position = basePosition;

		var sprite = new Sprite2D { Texture = texture, Position = new Vector2(0, -texture.GetSize().Y * 0.5f) };
		root.AddChild(sprite);

		if (collidable && root is StaticBody2D body)
		{
			var shape = new RectangleShape2D { Size = colliderSize };
			var collision = new CollisionShape2D { Shape = shape };

			collision.Position = new Vector2(0, -(colliderSize.Y * 0.5f));
			collision.AddToGroup("debug_prop_collision");
			body.AddChild(collision);
		}

		root.ZIndex = (int)root.GlobalPosition.Y;
		parent.AddChild(root);
		_debugPropPivots.Add(root.GlobalPosition);
	}


	private void CreateWallColliders()
	{
		_wallColliderBody?.QueueFree();
		_debugWallRects.Clear();
		_debugDoorRect = new Rect2();

		_wallColliderBody = new StaticBody2D { Name = "WallColliders", Position = _gridOffset };
		AddChild(_wallColliderBody);

		// North wall colliders (row y=-1)
		for (int x = 0; x < _gridWidth; x++)
		{
			var cellPos = _floorLayer.MapToLocal(new Vector2I(x, -1));
			AddWallCollider(new Rect2(
				cellPos.X - TileSize * 0.5f,
				cellPos.Y - TileSize * 0.5f,
				TileSize,
				TileSize
			));
		}

		// South wall colliders (row y=_gridHeight)
		for (int x = 0; x < _gridWidth; x++)
		{
			var cellPos = _floorLayer.MapToLocal(new Vector2I(x, _gridHeight));
			AddWallCollider(new Rect2(
				cellPos.X - TileSize * 0.5f,
				cellPos.Y - TileSize * 0.5f,
				TileSize,
				TileSize
			));
		}

		// West wall colliders (column x=-1)
		for (int y = 0; y < _gridHeight; y++)
		{
			var cellPos = _floorLayer.MapToLocal(new Vector2I(-1, y));
			AddWallCollider(new Rect2(
				cellPos.X - TileSize * 0.5f,
				cellPos.Y - TileSize * 0.5f,
				WallStripWidth,
				TileSize
			));
		}

		// NW corner
		var nwPos = _floorLayer.MapToLocal(new Vector2I(-1, -1));
		AddWallCollider(new Rect2(
			nwPos.X - TileSize * 0.5f,
			nwPos.Y - TileSize * 0.5f,
			WallStripWidth,
			TileSize
		));

		// East wall colliders (column x=_gridWidth) - with door gap
		var doorY = Mathf.Clamp(_doorRow, 0, _gridHeight - 1);
		for (int y = 0; y < _gridHeight; y++)
		{
			var cellPos = _floorLayer.MapToLocal(new Vector2I(_gridWidth, y));
			
			// Skip door gap
			if (y >= doorY && y < doorY + _doorHeightTiles)
				continue;

			AddWallCollider(new Rect2(
				cellPos.X - TileSize * 0.5f,
				cellPos.Y - TileSize * 0.5f,
				WallStripWidth,
				TileSize
			));
		}

		// NE corner
		var nePos = _floorLayer.MapToLocal(new Vector2I(_gridWidth, -1));
		AddWallCollider(new Rect2(
			nePos.X - TileSize * 0.5f,
			nePos.Y - TileSize * 0.5f,
			WallStripWidth,
			TileSize
		));

		// SW corner
		var swPos = _floorLayer.MapToLocal(new Vector2I(-1, _gridHeight));
		AddWallCollider(new Rect2(
			swPos.X - TileSize * 0.5f,
			swPos.Y - TileSize * 0.5f,
			WallStripWidth,
			TileSize
		));

		// SE corner
		var sePos = _floorLayer.MapToLocal(new Vector2I(_gridWidth, _gridHeight));
		AddWallCollider(new Rect2(
			sePos.X - TileSize * 0.5f,
			sePos.Y - TileSize * 0.5f,
			WallStripWidth,
			TileSize
		));

		// Door collider
		var doorCellPos = _floorLayer.MapToLocal(new Vector2I(_gridWidth, doorY));
		var doorTop = doorCellPos.Y - TileSize * 0.5f;
		var doorBottom = doorTop + (_doorHeightTiles * TileSize);
		_debugDoorRect = new Rect2(
			_wallColliderBody.ToGlobal(new Vector2(doorCellPos.X - TileSize * 0.5f, doorTop)),
			new Vector2(WallStripWidth, doorBottom - doorTop)
		);
	}

	private void AddWallCollider(Rect2 rect)
	{
		var shape = new RectangleShape2D { Size = rect.Size };
		var collision = new CollisionShape2D { Shape = shape };
		collision.Position = rect.Position + (rect.Size * 0.5f);
		_wallColliderBody.AddChild(collision);
		_debugWallRects.Add(new Rect2(_wallColliderBody.ToGlobal(rect.Position), rect.Size));
	}

	private void UpdateDebugPlayerRect()
	{
		var playerCollision = _player.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (playerCollision?.Shape is not RectangleShape2D playerShape)
			return;

		var size = playerShape.Size;
		_debugPlayerRect = new Rect2(
			playerCollision.GlobalPosition - (size * 0.5f),
			size
		);
	}

	private void UpdateDebugPropRects()
	{
		_debugPropRects.Clear();
		var debugNodes = GetTree().GetNodesInGroup("debug_prop_collision");
		foreach (var node in debugNodes)
		{
			if (node is not CollisionShape2D shape)
				continue;
			if (!IsInstanceValid(shape))
				continue;
			if (shape.Shape is not RectangleShape2D rectShape)
				continue;

			_debugPropRects.Add(new Rect2(
				shape.GlobalPosition - (rectShape.Size * 0.5f),
				rectShape.Size
			));
		}
	}

	private Rect2 ToLocalRect(Rect2 rect)
	{
		var topLeft = ToLocal(rect.Position);
		return new Rect2(topLeft, rect.Size);
	}

	private Vector2 AutoCenterFloor()
	{
		var topLeft = _floorLayer.MapToLocal(new Vector2I(0, 0));
		var topRight = _floorLayer.MapToLocal(new Vector2I(_gridWidth - 1, 0));
		var bottomLeft = _floorLayer.MapToLocal(new Vector2I(0, _gridHeight - 1));
		var bottomRight = _floorLayer.MapToLocal(new Vector2I(_gridWidth - 1, _gridHeight - 1));

		var minX = Mathf.Min(Mathf.Min(topLeft.X, topRight.X), Mathf.Min(bottomLeft.X, bottomRight.X));
		var maxX = Mathf.Max(Mathf.Max(topLeft.X, topRight.X), Mathf.Max(bottomLeft.X, bottomRight.X));
		var minY = Mathf.Min(Mathf.Min(topLeft.Y, topRight.Y), Mathf.Min(bottomLeft.Y, bottomRight.Y));
		var maxY = Mathf.Max(Mathf.Max(topLeft.Y, topRight.Y), Mathf.Max(bottomLeft.Y, bottomRight.Y));

		var center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
		return _gridAnchor - center;
	}

	private void UpdateWallVisibility()
	{
		if (_player == null)
			return;

		var playerCollision = _player.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (playerCollision?.Shape is not RectangleShape2D playerShape)
			return;

		var playerSize = playerShape.Size;
		var playerRect = new Rect2(
			playerCollision.GlobalPosition - (playerSize * 0.5f),
			playerSize
		);

		var roomLeft = _floorLayer.ToGlobal(_floorLayer.MapToLocal(new Vector2I(0, 0))).X;
		var roomWidth = _gridWidth * TileSize;

		// North wall visibility
		var floorTopY = _floorLayer.ToGlobal(_floorLayer.MapToLocal(new Vector2I(0, 0))).Y - TileSize * 0.5f;
		var northRect = new Rect2(roomLeft, floorTopY - 64.0f, roomWidth, 64.0f);
		var hideNorth = northRect.Intersects(playerRect);
		
		foreach (var sprite in _northWallSprites)
			sprite.Visible = !hideNorth;
		foreach (var sprite in _northWallStripSprites)
			sprite.Visible = hideNorth;

		// South wall visibility (now at row _gridHeight)
		var southWallBottomY = _floorLayer.ToGlobal(_floorLayer.MapToLocal(new Vector2I(0, _gridHeight))).Y - TileSize * 0.5f;
		var southRect = new Rect2(roomLeft, southWallBottomY - 64.0f, roomWidth, 64.0f);
		var hideSouth = southRect.Intersects(playerRect);
		
		foreach (var sprite in _southWallSprites)
			sprite.Visible = !hideSouth;
		foreach (var sprite in _southWallStripSprites)
			sprite.Visible = hideSouth;
		foreach (var sprite in _southCornerSprites)
			sprite.Visible = true; // Corners always visible

		_doorLayer.Visible = true;
	}

	private static Vector2I ResolveHorizontalAtlas(int x, int width)
	{
		if (x == 0)
			return ATLAS_COORDS_LEFT;
		if (x == width - 1)
			return ATLAS_COORDS_RIGHT;
		return ATLAS_COORDS_MID;
	}

	private static Vector2I ResolveVerticalAtlas(int y, int height)
	{
		if (y <= 0)
			return ATLAS_COORDS_LEFT;
		if (y == height - 1)
			return ATLAS_COORDS_RIGHT;
		return ATLAS_COORDS_MID;
	}


}
