using Godot;

public partial class StudioBuilder : IRoomBuilder
{
	[ExportGroup("Grid Settings")]
	[Export] public Vector2 GridAnchor = new(0, 388);
	[Export] public int GridWidth = 14;
	[Export] public int GridHeight = 6;
	[Export] public int LightMask = 2;

	[ExportGroup("Door Settings")]
	[Export] private int DoorRow = 3;
	[Export] private int DoorHeightTiles = 2;
	[Export] private bool EnableSouthWall = false;
	[Export] private bool EnableSouthDoor = false;
	[Export] private int SouthDoorRow = 3;

	[ExportGroup("Window Settings")]
	[Export] private int WindowStartColumn = 99;
	[Export] private int WindowEndColumn = 0;

	[ExportGroup("Lighting")]
	[Export] private bool EnableCeilingLight = true;
	[Export] private Color CeilingLightColor = new(1f, 0.95f, 0.8f);
	[Export] private float CeilingLightEnergy = 0.8f;
	[Export] private float CeilingLightRadius = 450f;
	[Export] private bool CeilingLightShadows = true;



	[ExportGroup("Ambient")]
	[Export] private Color AmbientColor = new(0.15f, 0.15f, 0.20f);

	[ExportGroup("Props")]
	[Export] private bool PlaceRoundTable = true;
	[Export] private bool PlaceVern = true;

	private TileMapLayer _floorLayer;
	private TileMapLayer _doorLayer;
	private TileMapLayer _gridDebugLayer;
	private Node2D _propSort;
	private RoomSection _section;
	private WallSystem _wallSystem;
	private CastShadowSystem _shadows;
	private RoomDebug _debug;
	private CanvasModulate _canvasModulate;
	private PointLight2D _ceilingLight;
	private CharacterBody2D _player;
	private float _flickerTime;

	public CastShadowSystem GetShadows() => _shadows;

	public void Build(WorldRoom world)
	{
		var tileSet = GD.Load<TileSet>("res://assets/tiles/topdown/topdown_tileset.tres");
		if (tileSet == null)
		{
			GD.PrintErr("StudioBuilder: Failed to load tileset");
			return;
		}

		_floorLayer = new TileMapLayer { Name = "StudioFloorLayer", TileSet = tileSet, ZIndex = 0, LightMask = LightMask };
		_doorLayer = new TileMapLayer { Name = "StudioDoorLayer", TileSet = tileSet, ZIndex = 1000 };
		_gridDebugLayer = new TileMapLayer { Name = "StudioGridDebugLayer", TileSet = tileSet, Visible = false };

		world.AddChild(_floorLayer);
		world.AddChild(_doorLayer);
		world.AddChild(_gridDebugLayer);

		_floorLayer.Position = GridAnchor;
		_doorLayer.Position = GridAnchor;
		_gridDebugLayer.Position = GridAnchor;

		_propSort = world.PropSort;

		_section = new RoomSection
		{
			FloorLayer = _floorLayer,
			DoorLayer = _doorLayer,
			PropSort = _propSort,
			GridOffset = GridAnchor,
			GridWidth = GridWidth,
			GridHeight = GridHeight
		};

		CreateFloors();

		CreateSystems(world);
		CreateLighting(world);
		InitializeDebug();
		CreateProps();
	}

	private void CreateFloors()
	{
		for (int y = 0; y < GridHeight; y++)
		{
			for (int x = 0; x < GridWidth; x++)
			{
				_floorLayer.SetCell(new Vector2I(x, y), 0, new Vector2I(0, 0));
			}
		}
	}

	private void CreateSystems(WorldRoom world)
	{
		_wallSystem = new WallSystem
		{
			DoorRow = DoorRow,
			DoorHeightTiles = DoorHeightTiles,
			EnableSouthWall = EnableSouthWall,
			EnableSouthDoor = EnableSouthDoor,
			SouthDoorRow = SouthDoorRow,
			WindowStartColumn = WindowStartColumn,
			WindowEndColumn = WindowEndColumn,
			LightMask = 0,
			NorthWallLightMask = LightMask
		};
		world.AddChild(_wallSystem);

		_shadows = new CastShadowSystem { LightRadius = CeilingLightRadius };
		world.AddChild(_shadows);

		_debug = new RoomDebug { DebugEnabled = false, ZIndex = 2000 };
		world.AddChild(_debug);

		_wallSystem.Initialize(_section);
		_wallSystem.CreateWalls();
		_wallSystem.CreateWallColliders();
	}

	private void CreateLighting(WorldRoom world)
	{
		_canvasModulate = new CanvasModulate { Color = AmbientColor, Name = "StudioAmbient" };
		world.AddChild(_canvasModulate);

		var centerX = GridWidth / 2f;
		var centerY = GridHeight / 2f;
		var center = GridToWorld(new Vector2I((int)centerX, (int)centerY));

		if (EnableCeilingLight)
		{
			_ceilingLight = CreatePointLightWithTexture(
				new Vector2(center.X, center.Y - 16),
				CeilingLightColor,
				CeilingLightEnergy,
				CeilingLightRadius,
				CeilingLightShadows,
				256,
				256,
				LightMask
			);
			world.AddChild(_ceilingLight);
		}

		_shadows.Initialize(_section, _ceilingLight);
		_flickerTime = 0f;
	}

	private PointLight2D CreatePointLightWithTexture(Vector2 position, Color color, float energy, float radius, bool shadows, int textureWidth = 0, int textureHeight = 0, int itemCullMask = 2)
	{
		var light = new PointLight2D
		{
			Position = position,
			Color = color,
			Energy = energy,
			ShadowEnabled = shadows,
			ShadowColor = new Color(0, 0, 0, 0.3f),
			ZIndex = 10
		};
		light.Set("range_item_cull_mask", itemCullMask);

		var texture = CreateOvalGradientTexture(textureWidth, textureHeight, radius);
		light.Texture = texture;
		light.TextureScale = 1.0f;
		light.Set("range", radius);

		return light;
	}

	private ImageTexture CreateOvalGradientTexture(int width, int height, float radius)
	{
		var sizeX = width > 0 ? width : (int)(radius * 0.8f);
		var sizeY = height > 0 ? height : (int)(radius * 0.8f);
		sizeX = Mathf.Max(sizeX, 48);
		sizeY = Mathf.Max(sizeY, 48);

		var image = Image.Create(sizeX, sizeY, false, Image.Format.Rgba8);

		var centerX = sizeX / 2f;
		var centerY = sizeY / 2f;

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
					t = t * t * t;
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

	private void InitializeDebug()
	{
		_debug.Initialize(_section, _wallSystem, _shadows, _ceilingLight, null, null);
	}

	private void CreateProps()
	{
		CreateBookcases();

		if (PlaceRoundTable)
		{
			CreateRoundTableGroup();
		}

		if (PlaceVern)
		{
			CreateVernChairGroup();
		}
	}

	private void CreateBookcases()
	{
		CreatePropWithCollision(
			"res://assets/tiles/props/bookcase.png",
			new Vector2I(1, 0),
			Vector2.Zero,
			new Vector2(48, 32)
		);

		CreatePropWithCollision(
			"res://assets/tiles/props/bookcase.png",
			new Vector2I(12, 0),
			Vector2.Zero,
			new Vector2(48, 32)
		);
	}

	private Node2D CreatePropWithCollision(
		string texturePath,
		Vector2I gridCoords,
		Vector2 pixelOffset,
		Vector2 colliderSize,
		bool createShadow = true)
	{
		var texture = GD.Load<Texture2D>(texturePath);
		if (texture == null)
		{
			GD.PrintErr($"StudioBuilder: Missing texture {texturePath}");
			return null;
		}

		var worldPos = GridToWorld(gridCoords) + pixelOffset;

		var body = new StaticBody2D();
		body.Position = worldPos;

		var sprite = new Sprite2D
		{
			Texture = texture,
			Position = new Vector2(0, -texture.GetSize().Y * 0.5f)
		};
		sprite.Set("light_mask", LightMask);

		if (_shadows != null && createShadow)
		{
			_shadows.CreateShadowForObject(body, texture);
		}

		body.AddChild(sprite);

		if (colliderSize != Vector2.Zero)
		{
			var shape = new RectangleShape2D { Size = colliderSize };
			var collision = new CollisionShape2D { Shape = shape };
			collision.Position = new Vector2(0, -(colliderSize.Y * 0.5f));
			collision.AddToGroup("debug_prop_collision");
			body.AddChild(collision);
		}

		body.ZIndex = (int)body.GlobalPosition.Y;
		_propSort.AddChild(body);

		return body;
	}

	private Node2D CreatePropNoCollision(string texturePath, Vector2I gridCoords, Vector2 pixelOffset)
	{
		var texture = GD.Load<Texture2D>(texturePath);
		if (texture == null)
		{
			GD.PrintErr($"StudioBuilder: Missing texture {texturePath}");
			return null;
		}

		var worldPos = GridToWorld(gridCoords) + pixelOffset;

		var node = new Node2D();
		node.Position = worldPos;

		var sprite = new Sprite2D
		{
			Texture = texture,
			Position = new Vector2(0, -texture.GetSize().Y * 0.5f)
		};
		sprite.Set("light_mask", LightMask);

		node.AddChild(sprite);

		node.ZIndex = (int)node.GlobalPosition.Y;
		_propSort.AddChild(node);

		return node;
	}

	private void CreateRoundTableGroup()
	{
		var tablePos = new Vector2I(7, 2);

		var tableGroup = CreatePropWithCollision(
			"res://assets/tiles/props/round_table.png",
			tablePos,
			Vector2.Zero,
			new Vector2(48, 48)
		);
		if (tableGroup == null) return;

		tableGroup.Name = "RoundTableGroup";

		CreateTabletopSprite(tableGroup, "res://assets/tiles/props/boom_mic.png", new Vector2(-12, -32), LightMask);
	}

	private void CreateVernChairGroup()
	{
		var chairTexture = GD.Load<Texture2D>("res://assets/tiles/props/vern_chair.png");
		var vernTexture = GD.Load<Texture2D>("res://assets/tiles/props/vern.png");

		if (chairTexture == null || vernTexture == null)
		{
			GD.PrintErr("StudioBuilder: Missing vern chair or vern texture");
			return;
		}

		var chairPos = new Vector2I(5, 2);

		var body = new StaticBody2D { Name = "VernChairGroup" };
		body.Position = GridToWorld(chairPos);

		var chairSprite = new Sprite2D
		{
			Texture = chairTexture,
			Position = new Vector2(0, -chairTexture.GetSize().Y * 0.5f)
		};
		chairSprite.Set("light_mask", LightMask);
		body.AddChild(chairSprite);

		if (_shadows != null)
		{
			_shadows.CreateShadowForObject(body, chairTexture);
		}

		var shape = new RectangleShape2D { Size = new Vector2(32, 32) };
		var collision = new CollisionShape2D { Shape = shape };
		collision.Position = new Vector2(0, -(shape.Size.Y * 0.5f));
		collision.AddToGroup("debug_prop_collision");
		body.AddChild(collision);

		var vernSprite = new Sprite2D
		{
			Texture = vernTexture,
			Position = new Vector2(0, -vernTexture.GetSize().Y * 0.5f)
		};
		vernSprite.Set("light_mask", LightMask);
		body.AddChild(vernSprite);

		body.ZIndex = (int)body.GlobalPosition.Y;
		_propSort.AddChild(body);
	}

	private void CreateTabletopSprite(Node2D parent, string texturePath, Vector2 offset, int lightMask)
	{
		var texture = GD.Load<Texture2D>(texturePath);
		if (texture == null)
		{
			GD.PrintErr($"StudioBuilder: Missing tabletop texture {texturePath}");
			return;
		}

		var sprite = new Sprite2D
		{
			Texture = texture,
			Position = offset
		};
		sprite.Set("light_mask", lightMask);
		parent.AddChild(sprite);
	}

	public void SetPlayer(CharacterBody2D player)
	{
		_player = player;
		_section.Player = player;
	}

	public Vector2 GridToWorld(Vector2I gridPos)
	{
		return _section.GridToWorld(gridPos);
	}

	public Rect2 GetFloorBounds()
	{
		var topLeft = _section.GridToWorld(new Vector2I(0, 0));
		var bottomRight = _section.GridToWorld(new Vector2I(GridWidth - 1, GridHeight - 1));
		return new Rect2(
			topLeft.X,
			topLeft.Y,
			GridWidth * RoomBase.TileSize,
			GridHeight * RoomBase.TileSize
		);
	}

	public void Update(WorldRoom world, double delta)
	{
		if (_player != null)
		{
			_wallSystem.UpdateVisibility(_player);
		}

		_flickerTime += (float)delta;

		if (_ceilingLight != null)
		{
			_ceilingLight.Energy = CeilingLightEnergy;
		}

		_shadows.Update(delta);
		_debug.UpdatePlayerRect();
		_debug.UpdatePropRects();
	}

	public void ToggleDebug()
	{
		_debug.Toggle();
	}
}
