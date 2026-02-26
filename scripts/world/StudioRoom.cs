using Godot;

public partial class StudioRoom : RoomBase
{
	[ExportGroup("Grid")]
	[Export] public int GridHeightOverride = 6;

	[ExportGroup("Door Settings")]
	[Export] private int DoorRow = 3;
	[Export] private int DoorHeightTiles = 2;
	[Export] private bool EnableSouthWall = false;
	[Export] private bool EnableSouthDoor = true;
	[Export] private int SouthDoorRow = 3;

	[ExportGroup("Wall Textures")]
	[Export] private Texture2D StudioNorthWallTexture;

	[ExportGroup("Lighting")]
	[Export] private bool EnableCeilingLight = true;
	[Export] private Color CeilingLightColor = new(1f, 0.95f, 0.8f);
	[Export] private float CeilingLightEnergy = 0.9f;
	[Export] private float CeilingLightRadius = 450f;
	[Export] private int CeilingLightWidth = 256;
	[Export] private int CeilingLightHeight = 256;
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

	private WallSystem _wallSystem;
	private CastShadowSystem _shadows;
	private RoomDebug _debug;

	private CanvasModulate _canvasModulate;
	private PointLight2D _ceilingLight;
	private PointLight2D _monitorLight;
	private PointLight2D _deskLampLight;
	private float _flickerTime;

	public PointLight2D CeilingLight => _ceilingLight;

	public override void _Ready()
	{
		GridHeight = GridHeightOverride;

		base._Ready();

		_wallSystem = new WallSystem
		{
			DoorRow = DoorRow,
			DoorHeightTiles = DoorHeightTiles,
			CustomNorthWallTexture = StudioNorthWallTexture,
			EnableSouthWall = EnableSouthWall,
			EnableSouthDoor = EnableSouthDoor,
			SouthDoorRow = SouthDoorRow
		};
		AddChild(_wallSystem);

		_shadows = new CastShadowSystem
		{
			LightRadius = CeilingLightRadius
		};
		AddChild(_shadows);

		_debug = new RoomDebug { DebugEnabled = false };
		AddChild(_debug);

		_wallSystem.Initialize(this);

		CreateLighting();

		_shadows.Initialize(this, _ceilingLight);
		_shadows.UpdateDepthShadowLightPosition();
		_debug.Initialize(this, _wallSystem, _shadows, _ceilingLight, _monitorLight, _deskLampLight);

		_wallSystem.CreateWalls();
		_wallSystem.CreateWallColliders();

		CreateProps();

		OnRoomReady();
	}

	private void CreateLighting()
	{
		_canvasModulate = new CanvasModulate { Color = AmbientColor };
		AddChild(_canvasModulate);

		var roomCenterX = GridWidth / 2;
		var roomCenterY = GridHeight / 2;
		var roomCenter = GridToWorld(new Vector2I(roomCenterX, roomCenterY));

		if (EnableCeilingLight)
		{
			_ceilingLight = CreatePointLightWithTexture(
				new Vector2(roomCenter.X, 32),
				CeilingLightColor,
				CeilingLightEnergy,
				CeilingLightRadius,
				CeilingLightShadows,
				CeilingLightWidth,
				CeilingLightHeight
			);
			AddChild(_ceilingLight);
		}

		var tablePosition = GridToWorld(new Vector2I(6, 2));

		if (EnableMonitorLight)
		{
			_monitorLight = CreatePointLightWithTexture(
				tablePosition + new Vector2(32, -38),
				MonitorLightColor,
				MonitorLightEnergy,
				MonitorLightRadius,
				false
			);
			_monitorLight.TextureScale = 2.0f;
			AddChild(_monitorLight);
		}

		if (EnableDeskLampLight)
		{
			_deskLampLight = CreatePointLightWithTexture(
				tablePosition + new Vector2(-32, -35),
				DeskLampColor,
				DeskLampEnergy,
				DeskLampRadius,
				false
			);
			_deskLampLight.TextureScale = 1.8f;
			AddChild(_deskLampLight);
		}

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

	private void CreateProps()
	{
		var propSort = GetNode<Node2D>("PropSort");

		if (PlaceStudioTable)
		{
			PropBuilder.CreateProp(propSort, "res://assets/tiles/props/studio_table.png",
				new Vector2I(6, 2), Vector2.Zero, true, new Vector2(48, 24),
				_shadows, _shadows.DepthShadowMaterial, this);
		}

		if (PlaceMonitorConsole)
		{
			PropBuilder.CreateProp(propSort, "res://assets/tiles/props/monitor_console.png",
				new Vector2I(6, 1), new Vector2(0, -26), false, Vector2.Zero,
				_shadows, _shadows.DepthShadowMaterial, this);
		}

		if (PlaceSpeakerStands)
		{
			PropBuilder.CreateProp(propSort, "res://assets/tiles/props/speaker_stand.png",
				new Vector2I(2, 1), Vector2.Zero, true, new Vector2(24, 16),
				_shadows, _shadows.DepthShadowMaterial, this);
			PropBuilder.CreateProp(propSort, "res://assets/tiles/props/speaker_stand.png",
				new Vector2I(10, 1), Vector2.Zero, true, new Vector2(24, 16),
				_shadows, _shadows.DepthShadowMaterial, this);
		}

		if (PlaceStorageCabinet)
		{
			PropBuilder.CreateProp(propSort, "res://assets/tiles/props/audio_cabinet.png",
				new Vector2I(11, 1), Vector2.Zero, true, new Vector2(24, 16),
				_shadows, _shadows.DepthShadowMaterial, this);
		}

		if (PlaceChair)
		{
			PropBuilder.CreateProp(propSort, "res://assets/tiles/props/computer_chair.png",
				new Vector2I(6, 3), Vector2.Zero, false, Vector2.Zero,
				_shadows, _shadows.DepthShadowMaterial, this);
		}

		if (PlaceCoffeeStation)
		{
			PropBuilder.CreateProp(propSort, "res://assets/tiles/props/coffee_station.png",
				new Vector2I(1, 1), Vector2.Zero, true, new Vector2(20, 24),
				_shadows, _shadows.DepthShadowMaterial, this);
		}

		if (PlaceWallDecor)
		{
			PropBuilder.CreateProp(propSort, "res://assets/tiles/props/poster.png",
				new Vector2I(3, 1), Vector2.Zero, true, new Vector2(16, 16),
				_shadows, _shadows.DepthShadowMaterial, this);
			PropBuilder.CreateProp(propSort, "res://assets/tiles/props/wall_clock.png",
				new Vector2I(10, 1), Vector2.Zero, true, new Vector2(16, 16),
				_shadows, _shadows.DepthShadowMaterial, this);
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_select"))
		{
			_debug.Toggle();
		}
	}

	public override void _Process(double delta)
	{
		_wallSystem.UpdateVisibility(Player);
		UpdateLighting(delta);
		_shadows.Update(delta);

		_debug.UpdatePlayerRect();
		_debug.UpdatePropRects();

		OnRoomProcess(delta);
	}

	private void UpdateLighting(double delta)
	{
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
	}
}
