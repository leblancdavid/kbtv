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
	[Export] private bool EnableSouthDoor = true;
	[Export] private int SouthDoorRow = 3;

	[ExportGroup("Lighting")]
	[Export] private bool EnableCeilingLight = true;
	[Export] private Color CeilingLightColor = new(1f, 0.95f, 0.8f);
	[Export] private float CeilingLightEnergy = 0.8f;
	[Export] private float CeilingLightRadius = 450f;
	[Export] private bool CeilingLightShadows = true;

	[ExportGroup("Monitor Light")]
	[Export] private bool EnableMonitorLight = true;
	[Export] private Color MonitorLightColor = new(0.2f, 0.8f, 1f);
	[Export] private float MonitorLightEnergy = 0.35f;
	[Export] private float MonitorLightRadius = 80f;

	[ExportGroup("Desk Lamp")]
	[Export] private bool EnableDeskLampLight = true;
	[Export] private Color DeskLampColor = new(1f, 0.9f, 0.6f);
	[Export] private float DeskLampEnergy = 0.3f;
	[Export] private float DeskLampRadius = 60f;

	[ExportGroup("Ambient")]
	[Export] private Color AmbientColor = new(0.15f, 0.15f, 0.20f);

	[ExportGroup("Props")]
	[Export] private bool PlaceStudioTable = true;
	[Export] private bool PlaceMonitorConsole = true;
	[Export] private bool PlaceSpeakerStands = true;
	[Export] private bool PlaceStorageCabinet = true;
	[Export] private bool PlaceChair = true;
	[Export] private bool PlaceCoffeeStation = true;
	[Export] private bool PlaceWallDecor = true;

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
	private PointLight2D _monitorLight;
	private PointLight2D _deskLampLight;
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

		var tablePosition = GridToWorld(new Vector2I(6, 2));

		if (EnableMonitorLight)
		{
			_monitorLight = CreatePointLightWithTexture(
				tablePosition + new Vector2(32, -38),
				MonitorLightColor,
				MonitorLightEnergy,
				MonitorLightRadius,
				false,
				0,
				0,
				LightMask
			);
			_monitorLight.TextureScale = 2.0f;
			world.AddChild(_monitorLight);
		}

		if (EnableDeskLampLight)
		{
			_deskLampLight = CreatePointLightWithTexture(
				tablePosition + new Vector2(-32, -35),
				DeskLampColor,
				DeskLampEnergy,
				DeskLampRadius,
				false,
				0,
				0,
				LightMask
			);
			_deskLampLight.TextureScale = 1.8f;
			world.AddChild(_deskLampLight);
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
		_debug.Initialize(_section, _wallSystem, _shadows, _ceilingLight, _monitorLight, _deskLampLight);
	}

	private void CreateProps()
	{
		// Studio props disabled
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

		if (_monitorLight != null)
		{
			var pulse = MonitorLightEnergy + Mathf.Sin(_flickerTime * 2f) * 0.03f;
			_monitorLight.Energy = pulse;
		}

		if (_deskLampLight != null)
		{
			var shimmer = DeskLampEnergy + Mathf.Sin(_flickerTime * 3f) * 0.02f;
			_deskLampLight.Energy = shimmer;
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
