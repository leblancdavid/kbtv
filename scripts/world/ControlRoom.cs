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
    private Node2D _propsRoot;
    private StaticBody2D _wallColliderBody;
    private readonly List<Rect2> _debugWallRects = new();
    private readonly List<Rect2> _debugPropRects = new();
    private Rect2 _debugPlayerRect;
    private Rect2 _debugDoorRect;
    private bool _debugVisible;
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

        _propsRoot = GetNode<Node2D>("PropSort/Props");
        if (_propsRoot == null)
        {
            GD.PrintErr("ControlRoom: Props root not found!");
            return;
        }

        ZIndex = 1000;
        ZAsRelative = false;
        _floorLayer.ZAsRelative = false;
        _northWallLayer.ZAsRelative = false;
        _westWallLayer.ZAsRelative = false;
        _eastWallLayer.ZAsRelative = false;
        _southWallLayer.ZAsRelative = false;
        _northWallStripLayer.ZAsRelative = false;
        _southWallStripLayer.ZAsRelative = false;
        _doorLayer.ZAsRelative = false;
        _gridDebugLayer.ZAsRelative = false;
        var propSort = GetNodeOrNull<Node2D>("PropSort");
        if (propSort != null)
            propSort.ZAsRelative = false;

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
        _propsRoot.Position = _gridOffset;

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

        foreach (var rect in _debugWallRects)
            DrawRect(ToLocalRect(rect), wallColor, true);

        foreach (var rect in _debugPropRects)
            DrawRect(ToLocalRect(rect), propColor, true);

        DrawRect(ToLocalRect(_debugPlayerRect), playerColor, true);
        if (_debugDoorRect.Size != Vector2.Zero)
            DrawRect(ToLocalRect(_debugDoorRect), doorColor, true);
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
        _propsRoot.QueueFree();
        _debugPropRects.Clear();

        var propSort = GetNode<Node2D>("PropSort");
        _propsRoot = new Node2D { Name = "Props", Position = _gridOffset };
        propSort.AddChild(_propsRoot);

        AddProp(_propsRoot, "res://assets/tiles/props/speaker_stand.png", new Vector2I(2, 2), Vector2.Zero, true, new Vector2(24, 16));
        AddProp(_propsRoot, "res://assets/tiles/props/speaker_stand.png", new Vector2I(10, 2), Vector2.Zero, true, new Vector2(24, 16));

        AddTableGroup(new Vector2I(6, 2));

        AddProp(_propsRoot, "res://assets/tiles/props/audio_cabinet.png", new Vector2I(12, 2), Vector2.Zero, true, new Vector2(24, 16));
        AddProp(_propsRoot, "res://assets/tiles/props/storage_shelf.png", new Vector2I(4, 10), Vector2.Zero, true, new Vector2(28, 12));
        AddProp(_propsRoot, "res://assets/tiles/props/storage_shelf.png", new Vector2I(10, 10), Vector2.Zero, true, new Vector2(28, 12));
        AddProp(_propsRoot, "res://assets/tiles/props/computer_chair.png", new Vector2I(6, 3), Vector2.Zero, false, Vector2.Zero);
    }

    private void AddTableGroup(Vector2I gridCoords)
    {
        var propSort = GetNode<Node2D>("PropSort");
        var group = new Node2D { Name = "TableGroup" };
        group.Position = _floorLayer.MapToLocal(gridCoords) + _gridOffset;
        propSort.AddChild(group);

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
            collision.AddToGroup("debug_prop_collision");
            body.AddChild(collision);
        }

        parent.AddChild(root);
    }


    private void CreateWallColliders()
    {
        _wallColliderBody?.QueueFree();
        _debugWallRects.Clear();
        _debugDoorRect = new Rect2();

        _wallColliderBody = new StaticBody2D { Name = "WallColliders", Position = _gridOffset };
        AddChild(_wallColliderBody);

        var topLeftCell = _floorLayer.MapToLocal(new Vector2I(0, 0));
        var topLeft = topLeftCell - new Vector2(TileSize * 0.5f, TileSize * 0.5f);
        var width = _gridWidth * TileSize;
        var height = _gridHeight * TileSize;
        var stripCell = _northWallStripLayer.MapToLocal(new Vector2I(0, -1));
        var stripBottom = stripCell.Y;
        var wallYOffset = (stripBottom - topLeft.Y) + TileSize;

        for (int x = 0; x < _gridWidth; x++)
        {
            var cellLeft = topLeft.X + (x * TileSize);
            AddWallCollider(new Rect2(
                cellLeft,
                topLeft.Y + wallYOffset,
                TileSize,
                TileSize
            ));

            AddWallCollider(new Rect2(
                cellLeft,
                topLeft.Y + height + wallYOffset,
                TileSize,
                TileSize
            ));
        }

        var doorY = Mathf.Clamp(_doorRow, 0, _gridHeight - 1);
        var doorTop = topLeft.Y + wallYOffset + (doorY * TileSize);
        var doorBottom = doorTop + (_doorHeightTiles * TileSize);

        var westX = topLeft.X - WallStripWidth;
        var eastX = topLeft.X + width;
        _debugDoorRect = new Rect2(
            _wallColliderBody.ToGlobal(new Vector2(eastX, doorTop)),
            new Vector2(WallStripWidth, doorBottom - doorTop)
        );

        for (int y = 0; y < _gridHeight; y++)
        {
            var cellTop = topLeft.Y + wallYOffset + (y * TileSize);
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

        var northBottomY = _floorLayer.ToGlobal(_floorLayer.MapToLocal(new Vector2I(0, 0))).Y;
        var northRect = new Rect2(roomLeft, northBottomY - 64.0f, roomWidth, 64.0f);
        var hideNorth = northRect.Intersects(playerRect);
        _northWallLayer.Visible = !hideNorth;
        _northWallStripLayer.Visible = hideNorth;

        var southBottomY = _floorLayer.ToGlobal(_floorLayer.MapToLocal(new Vector2I(0, _gridHeight - 1))).Y;
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
