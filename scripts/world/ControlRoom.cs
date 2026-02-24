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
	private const int WALL_WINDOW_SOURCE_ID = 7;

	// Window configuration
	private const int WindowStartColumn = 3;
	private const int WindowEndColumn = 9;

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
	private readonly List<Rect2> _debugOccluderRects = new();
	private Rect2 _debugPlayerRect;
	private Rect2 _debugDoorRect;
	private bool _debugVisible;
	private float _tableSortY;

	// Lighting nodes
	private CanvasModulate _canvasModulate;
	private PointLight2D _ceilingLight;
	private PointLight2D _monitorLight;
	private PointLight2D _deskLampLight;
	private float _flickerTime;
	private ShaderMaterial _depthShadowMaterial;

	// Shadow parameters
	private float _shadowMaxDistance = 256f;  // Based on ceiling light texture size
	private float _shadowLerpFactor = 0.12f;
	private readonly Dictionary<Node2D, Sprite2D> _pivotToShadowSprite = new();
	private ShaderMaterial _shadowMaterial;
	private ShaderMaterial _baseShadowMaterial;

	// Wall sprites for visibility toggling
	private readonly List<Sprite2D> _northWallSprites = new();
	private readonly List<Sprite2D> _northWallStripSprites = new();
	private readonly List<Sprite2D> _southWallSprites = new();
	private readonly List<Sprite2D> _southWallStripSprites = new();
	private readonly List<Sprite2D> _westWallSprites = new();
	private readonly List<Sprite2D> _eastWallSprites = new();
	private readonly List<Sprite2D> _southCornerSprites = new();
	private readonly List<Sprite2D> _windowSprites = new();

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

		// Load depth shadow shader for props and player
		var shader = GD.Load<Shader>("res://shaders/depth_shadow.gdshader");
		if (shader != null)
		{
			_depthShadowMaterial = new ShaderMaterial();
			_depthShadowMaterial.Shader = shader;
			// Set default shader parameters
			_depthShadowMaterial.SetShaderParameter("light_radius", 350.0f);
			_depthShadowMaterial.SetShaderParameter("max_brightness", 4.0f);
			_depthShadowMaterial.SetShaderParameter("shadow_factor", 0.8f);
		}

		// Load shadow blur shader for cast shadows
		var shadowShader = GD.Load<Shader>("res://shaders/shadow_blur.gdshader");
		if (shadowShader != null)
		{
			_shadowMaterial = new ShaderMaterial();
			_shadowMaterial.Shader = shadowShader;
			_shadowMaterial.SetShaderParameter("blur_amount", 1.0f);
			_shadowMaterial.SetShaderParameter("shadow_color", new Color(0, 0, 0, 0.6f));
			_shadowMaterial.SetShaderParameter("gradient_fade_height", 0.5f);

			// Base shadow material - dual gradient for smooth edges (3px fade on each side of 10px)
			_baseShadowMaterial = new ShaderMaterial();
			_baseShadowMaterial.Shader = shadowShader;
			_baseShadowMaterial.SetShaderParameter("blur_amount", 1.0f);
			_baseShadowMaterial.SetShaderParameter("shadow_color", new Color(0, 0, 0, 0.6f));
			_baseShadowMaterial.SetShaderParameter("gradient_fade_height", 0.5f);  // 4px fade on each side of 8px
			_baseShadowMaterial.SetShaderParameter("gradient_at_top", true);
		}

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

			// Apply depth shadow shader to player
			var playerSprite = _player.GetNode<Sprite2D>("Sprite2D");
			if (playerSprite != null && _depthShadowMaterial != null)
			{
				playerSprite.Material = _depthShadowMaterial;
			}

			// Create cast shadow for player
			if (playerSprite != null)
			{
				CreateShadowForPlayer(_player, playerSprite.Texture);
			}
		}

		// Initialize lighting
		CreateLighting();

		UpdateWallVisibility();
	}

	public override void _Process(double delta)
	{
		_flickerTime += (float)delta;

		// Update depth shadow shader with ceiling light position
		if (_ceilingLight != null && _depthShadowMaterial != null)
		{
			_depthShadowMaterial.SetShaderParameter("light_position", _ceilingLight.GlobalPosition);
		}

		// Ceiling light - steady, no flickering
		if (_ceilingLight != null)
		{
			_ceilingLight.Energy = 0.8f;
		}

		// Subtle pulse for monitor light
		if (_monitorLight != null)
		{
			var pulse = 0.3f + Mathf.Sin(_flickerTime * 2f) * 0.03f;
			_monitorLight.Energy = pulse;
		}

		// Slight shimmer for desk lamp
		if (_deskLampLight != null)
		{
			var shimmer = 0.25f + Mathf.Sin(_flickerTime * 3f) * 0.02f;
			_deskLampLight.Energy = shimmer;
		}

		// Update shadows
		if (_ceilingLight != null)
		{
			UpdateShadows(delta);
		}

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
		var lightColor = new Color(1, 1, 0, 0.9f);
		var occluderColor = new Color(0, 1, 1, 0.5f);

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

		// Draw light positions
		if (_ceilingLight != null)
			DrawCircle(ToLocal(_ceilingLight.GlobalPosition), 8f, lightColor);
		if (_monitorLight != null)
			DrawCircle(ToLocal(_monitorLight.GlobalPosition), 6f, lightColor);
		if (_deskLampLight != null)
			DrawCircle(ToLocal(_deskLampLight.GlobalPosition), 6f, lightColor);

		foreach (var pivot in _debugPropPivots)
			DrawCircle(ToLocal(pivot), 3f, pivotColor);

		// Draw occluder rectangles
		foreach (var rect in _debugOccluderRects)
			DrawRect(ToLocalRect(rect), occluderColor, true);
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
		var windowTexture = GD.Load<Texture2D>("res://assets/tiles/topdown/wall_window_atlas.png");

		var doorY = Mathf.Clamp(_doorRow, 0, _gridHeight - 1);

		// Create north wall (row -1) with window gap at columns 3-9
		for (int x = 0; x < _gridWidth; x++)
		{
			// Skip wall creation for window columns
			if (x >= WindowStartColumn && x <= WindowEndColumn)
			{
				CreateWindow(x, windowTexture);
				continue;
			}

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

	private void CreateWindow(int column, Texture2D texture)
	{
		// Window is 64px tall, positioned at row -1 (covers rows -1 and -2)
		var gridPos = new Vector2I(column, -1);
		var position = _floorLayer.MapToLocal(gridPos) + _gridOffset;

		// Calculate frame within the window atlas (columns 0-6)
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

		// Apply depth shadow shader
		if (_depthShadowMaterial != null)
		{
			tableSprite.Material = _depthShadowMaterial;
		}

		group.AddChild(tableSprite);

		// Create cast shadow for table
		CreateShadowForTable(group, tableTexture);

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

	private void CreateLighting()
	{
		// CanvasModulate - darkens the entire scene (multiply)
		// This creates the dark base - areas without lights stay dark
		_canvasModulate = new CanvasModulate
		{
			Color = new Color(0.15f, 0.15f, 0.20f) // Dark blue-gray
		};
		AddChild(_canvasModulate);

		// Calculate room center for positioning
		var roomCenterX = _gridWidth / 2;
		var roomCenterY = _gridHeight / 2;
		var roomCenter = _floorLayer.MapToLocal(new Vector2I(roomCenterX, roomCenterY)) + _gridOffset;
		var tablePosition = _floorLayer.MapToLocal(new Vector2I(6, 1)) + _gridOffset;

		// Ceiling Light - positioned at room center, 16x16 tiles (256x256)
		_ceilingLight = CreatePointLightWithTexture(
			_gridAnchor + new Vector2(0, -32),  // 2 tiles north
			new Color(1f, 1f, 1f),  // White
			0.8f,
			450f,  // Large range to fill room
			true,  // shadows enabled
			256,   // Width: 16 tiles
			256    // Height: 16 tiles
		);
		AddChild(_ceilingLight);

		// Screen Light - centered on computer station (soft oval)
		_monitorLight = CreatePointLightWithTexture(
			tablePosition + new Vector2(32, -38),
			new Color(0f, 1f, 0.27f),  // Green
			0.3f,
			80f,
			false
		);
		_monitorLight.TextureScale = 2.0f;
		AddChild(_monitorLight);

		// Desk Lamp Light - centered near phone line (soft glow)
		_deskLampLight = CreatePointLightWithTexture(
			tablePosition + new Vector2(-32, -35),
			new Color(1f, 0.67f, 0.27f),  // Orange
			0.25f,
			60f,
			false
		);
		_deskLampLight.TextureScale = 1.8f;
		AddChild(_deskLampLight);

		// Enable wall occluders to clip ceiling light shadows
		CreateWallOccluders();

		_flickerTime = 0f;
	}

	private PointLight2D CreatePointLightWithTexture(Vector2 position, Color color, float energy, float radius, bool shadows, int textureWidth = 0, int textureHeight = 0)
	{
		var light = new PointLight2D
		{
			Position = position,
			Color = color,
			Energy = energy,
			ShadowEnabled = shadows,
			ShadowColor = new Color(0, 0, 0, 0.3f)
		};

		// Create oval gradient texture programmatically
		var texture = CreateOvalGradientTexture(textureWidth, textureHeight, radius);
		light.Texture = texture;
		light.TextureScale = 1.0f;

		// Set range
		light.Set("range", radius);

		return light;
	}

	private ImageTexture CreateOvalGradientTexture(int width, int height, float radius)
	{
		// Use provided dimensions or calculate from radius
		var sizeX = width > 0 ? width : (int)(radius * 0.8f);
		var sizeY = height > 0 ? height : (int)(radius * 0.8f);
		sizeX = Mathf.Max(sizeX, 48);
		sizeY = Mathf.Max(sizeY, 48);

		var image = Image.Create(sizeX, sizeY, false, Image.Format.Rgba8);

		var centerX = sizeX / 2f;
		var centerY = sizeY / 2f;
		var maxDist = Mathf.Min(centerX, centerY);

		for (int y = 0; y < sizeY; y++)
		{
			for (int x = 0; x < sizeX; x++)
			{
				var dx = (x - centerX) / centerX;
				var dy = (y - centerY) / centerY;
				var dist = Mathf.Sqrt(dx * dx + dy * dy);

				byte alpha;
				if (dist < 0.2f)
				{
					alpha = 255;
				}
				else if (dist < 1.0f)
				{
					var t = (dist - 0.2f) / 0.8f;
					// Soft edge with smooth falloff
					t = t * t * t; // Cubic for very soft edge
					alpha = (byte)(255 * (1f - t));
				}
				else
				{
					alpha = 0;
				}

				image.SetPixel(x, y, new Color(1, 1, 1, alpha / 255f));
			}
		}

		return ImageTexture.CreateFromImage(image);
	}

	private PointLight2D CreatePointLight(Vector2 position, Color color, float energy, float radius, bool shadows)
	{
		var light = new PointLight2D
		{
			Position = position,
			Color = color,
			Energy = energy,
			ShadowEnabled = shadows,
			ShadowColor = new Color(0, 0, 0, 0.7f),
			ShadowFilter = PointLight2D.ShadowFilterEnum.None
		};

		// Create gradient texture: white (center) to transparent (edge)
		var gradientTexture = CreateLightGradient(radius);
		light.Texture = gradientTexture;
		light.TextureScale = 1.0f;

		// Set range
		light.Set("range", radius);

		return light;
	}

	private GradientTexture2D CreateLightGradient(float radius)
	{
		var gradientTexture = new GradientTexture2D();

		// Radial fill from center
		gradientTexture.Set("fill", (int)0); // Radial
		gradientTexture.Set("fill_from", new Vector2(0.5f, 0.5f));
		gradientTexture.Set("fill_to", new Vector2(1f, 1f));

		// Gradient: white at center (alpha 1) -> transparent at edge (alpha 0)
		// This is the KEY: white to transparent, NOT white to black
		var gradient = new Gradient();
		gradient.Set("colors", new Color[]
		{
			new Color(1f, 1f, 1f, 1f),   // Center: white, opaque
			new Color(1f, 1f, 1f, 0f)     // Edge: white, transparent
		});
		gradient.Set("offsets", new float[] { 0f, 1f });
		gradientTexture.Gradient = gradient;

		// Size matches the radius
		var size = (int)radius;
		gradientTexture.Width = size;
		gradientTexture.Height = size;

		return gradientTexture;
	}

	private void CreateWallOccluders()
	{
		var occluderNode = new Node2D { Name = "WallOccluders" };
		AddChild(occluderNode);

		// North wall (row 0) - skip window gap columns 3-9
		for (int x = 0; x < _gridWidth; x++)
		{
			if (x >= WindowStartColumn && x <= WindowEndColumn)
				continue;

			var cellPos = _floorLayer.MapToLocal(new Vector2I(x, 0)) + _gridOffset;
			CreateOccluderTile(occluderNode, cellPos);
		}

		// South wall (row _gridHeight - 1)
		for (int x = 0; x < _gridWidth; x++)
		{
			var cellPos = _floorLayer.MapToLocal(new Vector2I(x, _gridHeight - 1)) + _gridOffset;
			CreateOccluderTile(occluderNode, cellPos);
		}

		// West wall (column 0)
		for (int y = 0; y < _gridHeight; y++)
		{
			var cellPos = _floorLayer.MapToLocal(new Vector2I(0, y)) + _gridOffset;
			CreateOccluderTile(occluderNode, cellPos);
		}

		// East wall (column _gridWidth - 1)
		for (int y = 0; y < _gridHeight; y++)
		{
			var cellPos = _floorLayer.MapToLocal(new Vector2I(_gridWidth - 1, y)) + _gridOffset;
			CreateOccluderTile(occluderNode, cellPos);
		}

		// Corner occluders (all 4 corners)
		CreateOccluderTile(occluderNode, _floorLayer.MapToLocal(new Vector2I(0, 0)) + _gridOffset);
		CreateOccluderTile(occluderNode, _floorLayer.MapToLocal(new Vector2I(_gridWidth - 1, 0)) + _gridOffset);
		CreateOccluderTile(occluderNode, _floorLayer.MapToLocal(new Vector2I(0, _gridHeight - 1)) + _gridOffset);
		CreateOccluderTile(occluderNode, _floorLayer.MapToLocal(new Vector2I(_gridWidth - 1, _gridHeight - 1)) + _gridOffset);
	}

	private void CreateOccluderTile(Node2D parent, Vector2 position)
	{
		var occluder = new LightOccluder2D
		{
			Position = position
		};

		var polygon = new OccluderPolygon2D
		{
			Polygon = new Vector2[]
			{
				new Vector2(-TileSize * 0.5f, -TileSize * 0.5f),
				new Vector2(TileSize * 0.5f, -TileSize * 0.5f),
				new Vector2(TileSize * 0.5f, TileSize * 0.5f),
				new Vector2(-TileSize * 0.5f, TileSize * 0.5f)
			},
			CullMode = OccluderPolygon2D.CullModeEnum.Disabled
		};

		occluder.Occluder = polygon;
		parent.AddChild(occluder);

		var rect = new Rect2(position.X - TileSize * 0.5f, position.Y - TileSize * 0.5f, TileSize, TileSize);
		_debugOccluderRects.Add(rect);
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

		// Apply depth shadow shader
		if (_depthShadowMaterial != null)
		{
			sprite.Material = _depthShadowMaterial;
		}

		root.AddChild(sprite);

		// Create cast shadow as child of prop
		CreateShadowForProp(root, texture);

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

	private void CreateShadowForProp(Node2D propRoot, Texture2D texture)
	{
		var spriteSize = texture.GetSize();

		// Create pivot Node2D slightly above prop's base (feet)
		var shadowPivot = new Node2D
		{
			Name = "ShadowPivot",
			Position = new Vector2(0, -3)  // 3px above base
		};
		shadowPivot.AddToGroup("shadow_pivots");
		propRoot.AddChild(shadowPivot);

		// Create shadow sprite, offset so bottom touches pivot
		var shadowSprite = new Sprite2D
		{
			Texture = texture,
			Material = _shadowMaterial,
			Position = new Vector2(0, spriteSize.Y * 0.5f),  // Offset up so bottom at pivot
			FlipV = true,
			Modulate = new Color(0, 0, 0, 0.6f),
			ZAsRelative = false,
			ZIndex = 1  // Floor + 1
		};
		shadowPivot.AddChild(shadowSprite);
		_pivotToShadowSprite[shadowPivot] = shadowSprite;

		// Create base shadow (bottom 8px) - child of propRoot so it doesn't rotate
		var originalImage = texture.GetImage();
		var regionHeight = 8;
		var region = new Rect2I(0, originalImage.GetHeight() - regionHeight, originalImage.GetWidth(), regionHeight);
		var bottomImage = originalImage.GetRegion(region);
		var bottomTexture = ImageTexture.CreateFromImage(bottomImage);

		var baseShadowSprite = new Sprite2D
		{
			Texture = bottomTexture,
			Material = _baseShadowMaterial,  // Use base shadow material with gradient
			Position = new Vector2(0, 0),  // At object's feet
			FlipV = false,  // No flip - original orientation
			Modulate = new Color(0, 0, 0, 0.6f),
			ZAsRelative = false,
			ZIndex = 1
		};
		propRoot.AddChild(baseShadowSprite);  // Child of propRoot, NOT pivot - stays at feet
	}

	private void CreateShadowForPlayer(Node2D playerRoot, Texture2D texture)
	{
		var spriteSize = texture.GetSize();

		var shadowPivot = new Node2D
		{
			Name = "ShadowPivot",
			Position = new Vector2(0, -3)  // 3px above base
		};
		shadowPivot.AddToGroup("shadow_pivots");
		playerRoot.AddChild(shadowPivot);

		var shadowSprite = new Sprite2D
		{
			Texture = texture,
			Material = _shadowMaterial,
			Position = new Vector2(0, spriteSize.Y * 0.5f),
			FlipV = true,
			Modulate = new Color(0, 0, 0, 0.6f),
			ZAsRelative = false,
			ZIndex = 1  // Floor + 1
		};
		shadowPivot.AddChild(shadowSprite);
		_pivotToShadowSprite[shadowPivot] = shadowSprite;

		// Create base shadow (bottom 8px) - child of playerRoot so it doesn't rotate
		var originalImage = texture.GetImage();
		var regionHeight = 8;
		var region = new Rect2I(0, originalImage.GetHeight() - regionHeight, originalImage.GetWidth(), regionHeight);
		var bottomImage = originalImage.GetRegion(region);
		var bottomTexture = ImageTexture.CreateFromImage(bottomImage);

		var baseShadowSprite = new Sprite2D
		{
			Texture = bottomTexture,
			Material = _baseShadowMaterial,  // Use base shadow material with gradient
			Position = new Vector2(0, 0),  // At player's feet
			FlipV = false,  // No flip - original orientation
			Modulate = new Color(0, 0, 0, 0.6f),
			ZAsRelative = false,
			ZIndex = 1
		};
		playerRoot.AddChild(baseShadowSprite);  // Child of playerRoot, NOT pivot - stays at feet
	}

	private void CreateShadowForTable(Node2D tableRoot, Texture2D texture)
	{
		var spriteSize = texture.GetSize();

		var shadowPivot = new Node2D
		{
			Name = "ShadowPivot",
			Position = new Vector2(0, -3)
		};
		shadowPivot.AddToGroup("shadow_pivots");
		tableRoot.AddChild(shadowPivot);

		var shadowSprite = new Sprite2D
		{
			Texture = texture,
			Material = _shadowMaterial,
			Position = new Vector2(0, spriteSize.Y * 0.5f),
			FlipV = true,
			Modulate = new Color(0, 0, 0, 0.6f),
			ZAsRelative = false,
			ZIndex = 1
		};
		shadowPivot.AddChild(shadowSprite);
		_pivotToShadowSprite[shadowPivot] = shadowSprite;

		// Create base shadow (bottom 8px)
		var originalImage = texture.GetImage();
		var regionHeight = 8;
		var region = new Rect2I(0, originalImage.GetHeight() - regionHeight, originalImage.GetWidth(), regionHeight);
		var bottomImage = originalImage.GetRegion(region);
		var bottomTexture = ImageTexture.CreateFromImage(bottomImage);

		var baseShadowSprite = new Sprite2D
		{
			Texture = bottomTexture,
			Material = _baseShadowMaterial,
			Position = new Vector2(0, 0),
			FlipV = false,
			Modulate = new Color(0, 0, 0, 0.6f),
			ZAsRelative = false,
			ZIndex = 1
		};
		tableRoot.AddChild(baseShadowSprite);
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

		// Windows are always visible (they are part of the wall)

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

	private void UpdateShadows(double delta)
	{
		var lightPos = _ceilingLight.GlobalPosition;

		var shadowPivots = GetTree().GetNodesInGroup("shadow_pivots");

		foreach (Node node in shadowPivots)
		{
			if (node is not Node2D pivot)
				continue;

			var pivotWorldPos = pivot.GlobalPosition;
			var lightToPivot = pivotWorldPos - lightPos;
			var distance = lightToPivot.Length();

			// Rotation - angle from light to object, offset by -90 degrees for sprite's upright default
			var angle = Mathf.Atan2(lightToPivot.Y, lightToPivot.X) - Mathf.DegToRad(90);
			pivot.Rotation = angle;

			// Y-Scale based on distance (inverse: closer = smaller, farther = larger)
			var scaleY = Mathf.Clamp(0.2f + (distance / _shadowMaxDistance) * 1.8f, 0.2f, 2.0f);
			pivot.Scale = new Vector2(1f, scaleY);

			// Flip horizontally when object is below the light
			if (_pivotToShadowSprite.TryGetValue(pivot, out var shadowSprite))
			{
				shadowSprite.FlipH = pivotWorldPos.Y > lightPos.Y;
			}
		}
	}

}
