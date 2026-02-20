using Godot;

public partial class ControlRoom : Node2D
{
    [Export] private Vector2 _gridAnchor = new Vector2(320, 180);

    [Export] private float _southWallHideOffset = 8.0f;

    [Export] private int _gridWidth = 7;
    [Export] private int _gridHeight = 7;

    // TileSet source IDs from topdown_tileset.tres
    private const int FLOOR_SOURCE_ID = 0;
    private const int WALL_NORTH_SOURCE_ID = 1;
    private const int WALL_SOUTH_SOURCE_ID = 2;
    private const int WALL_WEST_SOURCE_ID = 3;
    private const int WALL_EAST_SOURCE_ID = 4;
    private const int WALL_SOUTH_STRIP_SOURCE_ID = 5;

    // Atlas coordinates
    private static readonly Vector2I ATLAS_COORDS_LEFT = new Vector2I(0, 0);
    private static readonly Vector2I ATLAS_COORDS_MID = new Vector2I(1, 0);
    private static readonly Vector2I ATLAS_COORDS_RIGHT = new Vector2I(2, 0);
    private static readonly Vector2I ATLAS_COORDS_DOOR = new Vector2I(3, 0);

    private TileMapLayer _floorLayer;
    private TileMapLayer _wallLayer;
    private TileMapLayer _southWallLayer;
    private TileMapLayer _southWallStripLayer;
    private Vector2 _gridOffset = Vector2.Zero;
    private Node2D _player;

    public override void _Ready()
    {
        _floorLayer = GetNode<TileMapLayer>("FloorLayer");
        if (_floorLayer == null)
        {
            GD.PrintErr("ControlRoom: FloorLayer not found!");
            return;
        }

        _wallLayer = GetNode<TileMapLayer>("WallLayer");
        if (_wallLayer == null)
        {
            GD.PrintErr("ControlRoom: WallLayer not found!");
            return;
        }

        _southWallLayer = GetNode<TileMapLayer>("SouthWallLayer");
        if (_southWallLayer == null)
        {
            GD.PrintErr("ControlRoom: SouthWallLayer not found!");
            return;
        }

        _southWallStripLayer = GetNode<TileMapLayer>("SouthWallStripLayer");
        if (_southWallStripLayer == null)
        {
            GD.PrintErr("ControlRoom: SouthWallStripLayer not found!");
            return;
        }

        // Create floor tiles on the TileMapLayer
        CreateFloor();

        // Auto-center the floor grid around the anchor
        _gridOffset = AutoCenterFloor();
        _floorLayer.Position = _gridOffset;
        _wallLayer.Position = _gridOffset;
        _southWallLayer.Position = _gridOffset;
        _southWallStripLayer.Position = _gridOffset;

        // Create walls as tiles
        CreateWalls();

        // Position player in center
        _player = GetNode<Node2D>("Player");
        if (_player != null)
        {
            var centerX = _gridWidth / 2;
            var centerY = _gridHeight / 2;
            _player.Position = _floorLayer.MapToLocal(new Vector2I(centerX, centerY)) + _gridOffset;
        }

        UpdateSouthWallVisibility();
    }

    public override void _Process(double delta)
    {
        UpdateSouthWallVisibility();
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
        var doorY = Mathf.Clamp(3, 0, _gridHeight - 1);
        for (int x = 0; x < _gridWidth; x++)
        {
            var northAtlas = ResolveHorizontalAtlas(x, _gridWidth);
            var southAtlas = ResolveHorizontalAtlas(x, _gridWidth);
            _wallLayer.SetCell(new Vector2I(x, -1), WALL_NORTH_SOURCE_ID, northAtlas);
            _southWallLayer.SetCell(new Vector2I(x, _gridHeight - 1), WALL_SOUTH_SOURCE_ID, southAtlas);
        }

        for (int y = -1; y < _gridHeight; y++)
        {
            var westAtlas = ResolveVerticalAtlas(y, _gridHeight);
            var eastAtlas = y == doorY ? ATLAS_COORDS_DOOR : ResolveVerticalAtlas(y, _gridHeight);
            _wallLayer.SetCell(new Vector2I(-1, y), WALL_WEST_SOURCE_ID, westAtlas);
            _wallLayer.SetCell(new Vector2I(_gridWidth, y), WALL_EAST_SOURCE_ID, eastAtlas);
        }

        for (int x = 0; x < _gridWidth; x++)
        {
            _southWallStripLayer.SetCell(new Vector2I(x, _gridHeight - 1), WALL_SOUTH_STRIP_SOURCE_ID, ATLAS_COORDS_LEFT);
        }
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

    private void UpdateSouthWallVisibility()
    {
        if (_southWallLayer == null || _player == null)
            return;

        var southWallLocal = _floorLayer.MapToLocal(new Vector2I(0, _gridHeight - 1));
        var southWallGlobalY = _floorLayer.ToGlobal(southWallLocal).Y;
        var shouldHide = _player.GlobalPosition.Y < southWallGlobalY - _southWallHideOffset;

        _southWallLayer.Visible = !shouldHide;
        if (_southWallStripLayer != null)
        {
            _southWallStripLayer.Visible = shouldHide;
        }
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
