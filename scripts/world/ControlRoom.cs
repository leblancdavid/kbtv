using Godot;

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
    private TileMapLayer _northWallLayer;
    private TileMapLayer _westWallLayer;
    private TileMapLayer _eastWallLayer;
    private TileMapLayer _southWallLayer;
    private TileMapLayer _northWallStripLayer;
    private TileMapLayer _southWallStripLayer;
    private TileMapLayer _doorLayer;
    private TileMapLayer _gridDebugLayer;
    private Vector2 _gridOffset = Vector2.Zero;
    private Node2D _player;
    private Node2D _propsBackRoot;
    private Node2D _propsFrontRoot;
    private StaticBody2D _wallColliderBody;
    private float _tableSortY;

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

        _northWallLayer = GetNode<TileMapLayer>("NorthWallLayer");
        if (_northWallLayer == null)
        {
            GD.PrintErr("ControlRoom: NorthWallLayer not found!");
            return;
        }

        _westWallLayer = GetNode<TileMapLayer>("WestWallLayer");
        if (_westWallLayer == null)
        {
            GD.PrintErr("ControlRoom: WestWallLayer not found!");
            return;
        }

        _eastWallLayer = GetNode<TileMapLayer>("EastWallLayer");
        if (_eastWallLayer == null)
        {
            GD.PrintErr("ControlRoom: EastWallLayer not found!");
            return;
        }

        _southWallLayer = GetNode<TileMapLayer>("SouthWallLayer");
        if (_southWallLayer == null)
        {
            GD.PrintErr("ControlRoom: SouthWallLayer not found!");
            return;
        }

        _northWallStripLayer = GetNode<TileMapLayer>("NorthWallStripLayer");
        if (_northWallStripLayer == null)
        {
            GD.PrintErr("ControlRoom: NorthWallStripLayer not found!");
            return;
        }

        _southWallStripLayer = GetNode<TileMapLayer>("SouthWallStripLayer");
        if (_southWallStripLayer == null)
        {
            GD.PrintErr("ControlRoom: SouthWallStripLayer not found!");
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

        _propsBackRoot = GetNode<Node2D>("PropSort/PropsBack");
        if (_propsBackRoot == null)
        {
            GD.PrintErr("ControlRoom: PropsBack root not found!");
            return;
        }

        _propsFrontRoot = GetNode<Node2D>("PropSort/PropsFront");
        if (_propsFrontRoot == null)
        {
            GD.PrintErr("ControlRoom: PropsFront root not found!");
            return;
        }

        // Create floor tiles on the TileMapLayer
        CreateFloor();

        // Auto-center the floor grid around the anchor
        _gridOffset = AutoCenterFloor();
        _floorLayer.Position = _gridOffset;
        _northWallLayer.Position = _gridOffset;
        _westWallLayer.Position = _gridOffset;
        _eastWallLayer.Position = _gridOffset;
        _southWallLayer.Position = _gridOffset;
        _northWallStripLayer.Position = _gridOffset;
        _southWallStripLayer.Position = _gridOffset;
        _doorLayer.Position = _gridOffset;
        _gridDebugLayer.Position = _gridOffset;
        _propsBackRoot.Position = _gridOffset;
        _propsFrontRoot.Position = _gridOffset;

        // Create walls as tiles
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
            _player.Position = _floorLayer.MapToLocal(new Vector2I(centerX, centerY)) + _gridOffset;
        }

        UpdateWallVisibility();
    }

    public override void _Process(double delta)
    {
        UpdateWallVisibility();
        UpdatePlayerLayering();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_select"))
        {
            _gridDebugLayer.Visible = !_gridDebugLayer.Visible;
        }
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
        var doorY = Mathf.Clamp(_doorRow, 0, _gridHeight - 1);
        for (int x = 0; x < _gridWidth; x++)
        {
            var northAtlas = ResolveHorizontalAtlas(x, _gridWidth);
            var southAtlas = ResolveHorizontalAtlas(x, _gridWidth);
            _northWallLayer.SetCell(new Vector2I(x, -1), WALL_NORTH_SOURCE_ID, northAtlas);
            _southWallLayer.SetCell(new Vector2I(x, _gridHeight - 1), WALL_SOUTH_SOURCE_ID, southAtlas);
            _northWallStripLayer.SetCell(new Vector2I(x, -1), WALL_NORTH_STRIP_SOURCE_ID, ATLAS_COORDS_LEFT);
        }

        for (int y = -1; y < _gridHeight; y++)
        {
            var westAtlas = ResolveVerticalAtlas(y, _gridHeight);
            var eastAtlas = y == doorY ? ATLAS_COORDS_DOOR : ResolveVerticalAtlas(y, _gridHeight);
            _westWallLayer.SetCell(new Vector2I(-1, y), WALL_WEST_SOURCE_ID, westAtlas);
            _eastWallLayer.SetCell(new Vector2I(_gridWidth, y), WALL_EAST_SOURCE_ID, eastAtlas);
        }

        _doorLayer.SetCell(new Vector2I(_gridWidth, doorY), WALL_EAST_SOURCE_ID, ATLAS_COORDS_DOOR);

        for (int x = 0; x < _gridWidth; x++)
        {
            _southWallStripLayer.SetCell(new Vector2I(x, _gridHeight - 1), WALL_SOUTH_STRIP_SOURCE_ID, ATLAS_COORDS_LEFT);
        }
    }

    private void CreateProps()
    {
        _propsBackRoot.QueueFree();
        _propsFrontRoot.QueueFree();

        var propSort = GetNode<Node2D>("PropSort");
        _propsBackRoot = new Node2D { Name = "PropsBack", Position = _gridOffset };
        _propsFrontRoot = new Node2D { Name = "PropsFront", Position = _gridOffset };

        propSort.AddChild(_propsBackRoot);
        propSort.AddChild(_propsFrontRoot);

        AddProp(_propsBackRoot, "res://assets/tiles/props/speaker_stand.png", new Vector2I(2, 2), Vector2.Zero, true, new Vector2(24, 16));
        AddProp(_propsBackRoot, "res://assets/tiles/props/speaker_stand.png", new Vector2I(10, 2), Vector2.Zero, true, new Vector2(24, 16));

        var tableBase = _floorLayer.MapToLocal(new Vector2I(6, 2));
        _tableSortY = _gridOffset.Y + tableBase.Y - 8;
        AddProp(_propsBackRoot, "res://assets/tiles/props/studio_table.png", new Vector2I(6, 2), Vector2.Zero, true, new Vector2(96, 14));

        AddProp(_propsFrontRoot, "res://assets/tiles/props/phone_line.png", new Vector2I(4, 4), new Vector2(4, -42), true, new Vector2(20, 10));
        AddProp(_propsFrontRoot, "res://assets/tiles/props/sound_board.png", new Vector2I(6, 4), new Vector2(2, -42), true, new Vector2(22, 10));
        AddProp(_propsFrontRoot, "res://assets/tiles/props/computer_station.png", new Vector2I(8, 4), new Vector2(-2, -54), true, new Vector2(22, 12));

        AddProp(_propsBackRoot, "res://assets/tiles/props/audio_cabinet.png", new Vector2I(12, 2), Vector2.Zero, true, new Vector2(24, 16));
        AddProp(_propsBackRoot, "res://assets/tiles/props/storage_shelf.png", new Vector2I(4, 10), Vector2.Zero, true, new Vector2(28, 12));
        AddProp(_propsBackRoot, "res://assets/tiles/props/storage_shelf.png", new Vector2I(10, 10), Vector2.Zero, true, new Vector2(28, 12));
        AddProp(_propsFrontRoot, "res://assets/tiles/props/computer_chair.png", new Vector2I(6, 3), Vector2.Zero, false, Vector2.Zero);
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

        var basePosition = _floorLayer.MapToLocal(gridCoords) + pixelOffset;
        var root = collidable ? new StaticBody2D() : new Node2D();
        root.Position = basePosition;

        var sprite = new Sprite2D { Texture = texture, Position = new Vector2(0, -texture.GetSize().Y * 0.5f) };
        root.AddChild(sprite);

        if (collidable && root is StaticBody2D body)
        {
            var shape = new RectangleShape2D { Size = colliderSize };
            var collision = new CollisionShape2D { Shape = shape };

            collision.Position = new Vector2(0, -(colliderSize.Y * 0.5f));
            body.AddChild(collision);
        }

        parent.AddChild(root);
    }

    private void UpdatePlayerLayering()
    {
        if (_player == null || _propsBackRoot == null || _propsFrontRoot == null)
            return;

        if (_player.GlobalPosition.Y < _tableSortY)
        {
            if (_player.GetParent() != _propsBackRoot)
                _player.Reparent(_propsBackRoot);
        }
        else
        {
            if (_player.GetParent() != _propsFrontRoot)
                _player.Reparent(_propsFrontRoot);
        }
    }

    private void CreateWallColliders()
    {
        _wallColliderBody?.QueueFree();

        _wallColliderBody = new StaticBody2D { Name = "WallColliders", Position = _gridOffset };
        AddChild(_wallColliderBody);

        var topLeftCell = _floorLayer.MapToLocal(new Vector2I(0, 0));
        var topLeft = topLeftCell - new Vector2(TileSize * 0.5f, TileSize * 0.5f);
        var width = _gridWidth * TileSize;
        var height = _gridHeight * TileSize;

        for (int x = 0; x < _gridWidth; x++)
        {
            var cellLeft = topLeft.X + (x * TileSize);
            AddWallCollider(new Rect2(
                cellLeft,
                topLeft.Y,
                TileSize,
                TileSize
            ));

            AddWallCollider(new Rect2(
                cellLeft,
                topLeft.Y + height,
                TileSize,
                TileSize
            ));
        }

        var doorY = Mathf.Clamp(_doorRow, 0, _gridHeight - 1);
        var doorTop = topLeft.Y + (doorY * TileSize);
        var doorBottom = doorTop + (_doorHeightTiles * TileSize);

        var westX = topLeft.X - WallStripWidth;
        var eastX = topLeft.X + width;

        for (int y = 0; y < _gridHeight; y++)
        {
            var cellTop = topLeft.Y + (y * TileSize);
            AddWallCollider(new Rect2(
                westX,
                cellTop,
                WallStripWidth,
                TileSize
            ));

            if (y < doorY || y >= doorY + _doorHeightTiles)
            {
                AddWallCollider(new Rect2(
                    eastX,
                    cellTop,
                    WallStripWidth,
                    TileSize
                ));
            }
        }
    }

    private void AddWallCollider(Rect2 rect)
    {
        var shape = new RectangleShape2D { Size = rect.Size };
        var collision = new CollisionShape2D { Shape = shape };
        collision.Position = rect.Position + (rect.Size * 0.5f);
        _wallColliderBody.AddChild(collision);
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

        var deadZone = 4.0f;

        var playerCollision = _player.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (playerCollision?.Shape is not RectangleShape2D playerShape)
            return;

        var playerSize = playerShape.Size;
        var playerRect = new Rect2(
            _player.GlobalPosition.X - (playerSize.X * 0.5f),
            _player.GlobalPosition.Y - (playerSize.Y * 0.5f),
            playerSize.X,
            playerSize.Y
        );

        var roomLeft = _floorLayer.ToGlobal(_floorLayer.MapToLocal(new Vector2I(0, 0))).X;
        var roomWidth = _gridWidth * TileSize;

        var northBottomY = _floorLayer.ToGlobal(_floorLayer.MapToLocal(new Vector2I(0, 0))).Y - deadZone;
        var northRect = new Rect2(roomLeft, northBottomY - 64.0f, roomWidth, 64.0f);
        var hideNorth = northRect.Intersects(playerRect);
        _northWallLayer.Visible = !hideNorth;
        _northWallStripLayer.Visible = hideNorth;

        var southBottomY = _floorLayer.ToGlobal(_floorLayer.MapToLocal(new Vector2I(0, _gridHeight - 1))).Y + deadZone;
        var southRect = new Rect2(roomLeft, southBottomY - 64.0f, roomWidth, 64.0f);
        var hideSouth = southRect.Intersects(playerRect);
        _southWallLayer.Visible = !hideSouth;
        _southWallStripLayer.Visible = hideSouth;

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
