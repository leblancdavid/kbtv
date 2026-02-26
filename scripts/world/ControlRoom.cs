using Godot;

public partial class ControlRoom : RoomBase
{
	[ExportGroup("Door Settings")]
	[Export] private int DoorRow = 3;
	[Export] private int DoorHeightTiles = 2;

	[ExportGroup("Window Settings")]
	[Export] private int WindowStartColumn = 3;
	[Export] private int WindowEndColumn = 9;

	[ExportGroup("Lighting")]
	[Export] private bool EnableCeilingLight = true;
	[Export] private Color CeilingLightColor = new(1f, 1f, 1f);
	[Export] private float CeilingLightEnergy = 0.8f;
	[Export] private float CeilingLightRadius = 450f;
	[Export] private int CeilingLightWidth = 256;
	[Export] private int CeilingLightHeight = 256;
	[Export] private bool CeilingLightShadows = true;

	[ExportGroup("Monitor Light")]
	[Export] private bool EnableMonitorLight = true;
	[Export] private Color MonitorLightColor = new(0f, 1f, 0.27f);
	[Export] private float MonitorLightEnergy = 0.3f;
	[Export] private float MonitorLightRadius = 80f;

	[ExportGroup("Desk Lamp")]
	[Export] private bool EnableDeskLampLight = true;
	[Export] private Color DeskLampColor = new(1f, 0.67f, 0.27f);
	[Export] private float DeskLampEnergy = 0.25f;
	[Export] private float DeskLampRadius = 60f;

	[ExportGroup("Ambient")]
	[Export] private Color AmbientColor = new(0.15f, 0.15f, 0.20f);

	[ExportGroup("Props")]
	[Export] private bool PlaceSpeakerStands = true;
	[Export] private bool PlaceTableGroup = true;
	[Export] private bool PlaceAudioCabinet = true;
	[Export] private bool PlaceStorageShelves = true;
	[Export] private bool PlaceChair = true;

	private WallSystem _wallSystem;
	private CastShadowSystem _shadows;
	private RoomDebug _debug;

	private CanvasModulate _canvasModulate;
	private PointLight2D _ceilingLight;
	private PointLight2D _monitorLight;
	private PointLight2D _deskLampLight;
	private float _flickerTime;

	public PointLight2D CeilingLight => _ceilingLight;
	public CastShadowSystem Shadows => _shadows;

	public override void _Ready()
	{
		base._Ready();

		_wallSystem = new WallSystem
		{
			DoorRow = DoorRow,
			DoorHeightTiles = DoorHeightTiles,
			WindowStartColumn = WindowStartColumn,
			WindowEndColumn = WindowEndColumn
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

		var tablePosition = GridToWorld(new Vector2I(6, 1));

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

		if (PlaceSpeakerStands)
		{
			PropBuilder.CreateProp(propSort, "res://assets/tiles/props/speaker_stand.png",
				new Vector2I(2, 1), Vector2.Zero, true, new Vector2(24, 16),
				_shadows, _shadows.DepthShadowMaterial, this);
			PropBuilder.CreateProp(propSort, "res://assets/tiles/props/speaker_stand.png",
				new Vector2I(10, 1), Vector2.Zero, true, new Vector2(24, 16),
				_shadows, _shadows.DepthShadowMaterial, this);
		}

		if (PlaceTableGroup)
		{
			PropBuilder.CreateTableGroup(propSort, new Vector2I(6, 1),
				_shadows, _shadows.DepthShadowMaterial, this,
				("res://assets/tiles/props/phone_line.png", new Vector2(-32, -26)),
				("res://assets/tiles/props/sound_board.png", new Vector2(0, -26)),
				("res://assets/tiles/props/computer_station.png", new Vector2(32, -38))
			);
		}

		if (PlaceAudioCabinet)
		{
			PropBuilder.CreateProp(propSort, "res://assets/tiles/props/audio_cabinet.png",
				new Vector2I(12, 1), Vector2.Zero, true, new Vector2(24, 16),
				_shadows, _shadows.DepthShadowMaterial, this);
		}

		if (PlaceStorageShelves)
		{
			PropBuilder.CreateProp(propSort, "res://assets/tiles/props/storage_shelf.png",
				new Vector2I(4, 10), new Vector2(0, -8), true, new Vector2(48, 32),
				_shadows, _shadows.DepthShadowMaterial, this);
			PropBuilder.CreateProp(propSort, "res://assets/tiles/props/storage_shelf.png",
				new Vector2I(10, 10), new Vector2(0, -8), true, new Vector2(48, 32),
				_shadows, _shadows.DepthShadowMaterial, this);
		}

		if (PlaceChair)
		{
			PropBuilder.CreateProp(propSort, "res://assets/tiles/props/computer_chair.png",
				new Vector2I(6, 2), Vector2.Zero, false, Vector2.Zero,
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
