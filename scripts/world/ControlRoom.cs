using Godot;

public partial class ControlRoom : Node2D
{
    [Export] private Texture2D _wallNorth;
    [Export] private Texture2D _wallSouth;
    [Export] private Texture2D _wallWest;
    [Export] private Texture2D _wallEast;
    [Export] private Vector2 _gridAnchor = new Vector2(320, 180);

    [Export] private int _gridWidth = 4;
    [Export] private int _gridHeight = 6;

    // TileSet source IDs from iso_tileset.tres
    private const int FLOOR_SOURCE_ID = 0;

    // Atlas coordinates (all tiles are at 0,0 in their sources)
    private static readonly Vector2I ATLAS_COORDS = new Vector2I(0, 0);

    private TileMapLayer _floorLayer;
    private Vector2 _gridOffset = Vector2.Zero;

    public override void _Ready()
    {
        _floorLayer = GetNode<TileMapLayer>("FloorLayer");
        if (_floorLayer == null)
        {
            GD.PrintErr("ControlRoom: FloorLayer not found!");
            return;
        }

        // Create floor tiles on the TileMapLayer
        CreateFloor();

        // Auto-center the floor grid around the anchor
        _gridOffset = AutoCenterFloor();
        _floorLayer.Position = _gridOffset;

        // Create walls as Sprite2Ds for occlusion support
        CreateWalls();

        // Position player in center
        var player = GetNode<Node2D>("Player");
        if (player != null)
        {
            var centerX = _gridWidth / 2;
            var centerY = _gridHeight / 2;
            player.Position = _floorLayer.MapToLocal(new Vector2I(centerX, centerY)) + _gridOffset;
        }
    }

    private void CreateFloor()
    {
        for (int y = 0; y < _gridHeight; y++)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                Vector2I coords = new Vector2I(x, y);
                _floorLayer.SetCell(coords, FLOOR_SOURCE_ID, ATLAS_COORDS);
            }
        }
    }

    private void CreateWalls()
    {
        var wallsNode = new Node2D();
        wallsNode.Name = "Walls";
        wallsNode.Position = _gridOffset;
        AddChild(wallsNode);

        // North wall (back) - above the top row
        for (int x = 0; x < _gridWidth; x++)
        {
            if (_wallNorth != null)
            {
                var sprite = new Sprite2D();
                sprite.Texture = _wallNorth;
                sprite.Position = _floorLayer.MapToLocal(new Vector2I(x, -1));
                sprite.Name = $"WallNorth_{x}";
                // Attach Occluder script for fading
                var occluder = new Occluder();
                sprite.AddChild(occluder);
                wallsNode.AddChild(sprite);
            }
        }

        // South wall (front) - below the bottom row
        for (int x = 0; x < _gridWidth; x++)
        {
            if (_wallSouth != null)
            {
                var sprite = new Sprite2D();
                sprite.Texture = _wallSouth;
                sprite.Position = _floorLayer.MapToLocal(new Vector2I(x, _gridHeight));
                sprite.Name = $"WallSouth_{x}";
                var occluder = new Occluder();
                sprite.AddChild(occluder);
                wallsNode.AddChild(sprite);
            }
        }

        // West wall (left) - along left edge
        for (int y = 0; y < _gridHeight; y++)
        {
            if (_wallWest != null)
            {
                var sprite = new Sprite2D();
                sprite.Texture = _wallWest;
                sprite.Position = _floorLayer.MapToLocal(new Vector2I(-1, y));
                sprite.Name = $"WallWest_{y}";
                var occluder = new Occluder();
                sprite.AddChild(occluder);
                wallsNode.AddChild(sprite);
            }
        }

        // East wall (right) - along right edge
        for (int y = 0; y < _gridHeight; y++)
        {
            if (_wallEast != null)
            {
                var sprite = new Sprite2D();
                sprite.Texture = _wallEast;
                sprite.Position = _floorLayer.MapToLocal(new Vector2I(_gridWidth, y));
                sprite.Name = $"WallEast_{y}";
                var occluder = new Occluder();
                sprite.AddChild(occluder);
                wallsNode.AddChild(sprite);
            }
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


}
