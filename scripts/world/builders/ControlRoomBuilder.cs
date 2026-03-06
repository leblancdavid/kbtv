using Godot;
using KBTV.Core;
using KBTV.UI;
public partial class ControlRoomBuilder : IRoomBuilder
{
	[ExportGroup("Grid Settings")]
	[Export] public Vector2 GridAnchor = new(0, 500);
	[Export] public int GridWidth = 14;
	[Export] public int GridHeight = 10;
	[Export] public int LightMask = 1;

	[ExportGroup("Door Settings")]
	[Export] private int DoorRow = 3;
	[Export] private int DoorHeightTiles = 2;

	[ExportGroup("Window Settings")]
	[Export] private int WindowStartColumn = 3;
	[Export] private int WindowEndColumn = 9;

	[ExportGroup("Lighting")]
	[Export] private bool EnableCeilingLight = true;
	[Export] private Color CeilingLightColor = new(1f, 0.95f, 0.9f);
	[Export] private float CeilingLightEnergy = 1.2f;
	[Export] private float CeilingLightRadius = 1000f;
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
	private Area2D? _screeningTrigger;
	private bool _playerInScreeningRange;
	private EventBus? _eventBus;
	private GameStateManager? _gameStateManager;

	public CastShadowSystem GetShadows() => _shadows;

	public void Build(WorldRoom world)
	{
		CacheServices(world);
		var tileSet = GD.Load<TileSet>("res://assets/tiles/topdown/topdown_tileset.tres");
		if (tileSet == null)
		{
			GD.PrintErr("ControlRoomBuilder: Failed to load tileset");
			return;
		}

		_floorLayer = new TileMapLayer { Name = "ControlFloorLayer", TileSet = tileSet, ZIndex = 0, LightMask = LightMask };
		_doorLayer = new TileMapLayer { Name = "ControlDoorLayer", TileSet = tileSet, ZIndex = 1000 };
		_gridDebugLayer = new TileMapLayer { Name = "ControlGridDebugLayer", TileSet = tileSet, Visible = false };

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

		// Set control room bounds for audio muffling detection
		var roomState = ServiceRegistry.Instance?.Get<RoomStateManager>();
		if (roomState != null)
		{
			roomState.SetControlRoomBounds(GetFloorBounds());
		}

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
			WindowStartColumn = WindowStartColumn,
			WindowEndColumn = WindowEndColumn,
			LightMask = 0,
			NorthWallLightMask = LightMask,
			EnableNorthDoor = true,
			NorthDoorStartColumn = 0,
			NorthDoorWidth = 2,
			WallNorthDoorSourceId = 8,
			EnableOnAirSign = false
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
		_canvasModulate = new CanvasModulate { Color = AmbientColor, Name = "ControlAmbient" };
		world.AddChild(_canvasModulate);

		var centerX = GridWidth / 2f;
		var centerY = GridHeight / 2f;
		var center = GridToWorld(new Vector2I((int)centerX, (int)centerY));

		if (EnableCeilingLight)
		{
			_ceilingLight = CreatePointLightWithTexture(
				new Vector2(center.X, center.Y - 32),
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

		var tablePosition = GridToWorld(new Vector2I(6, 1));

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
			_monitorLight.TextureScale = 1.0f;
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
			_deskLampLight.TextureScale = 1.0f;
			world.AddChild(_deskLampLight);
		}

		_shadows.Initialize(_section, _ceilingLight);
		_flickerTime = 0f;
	}

	private PointLight2D CreatePointLightWithTexture(Vector2 position, Color color, float energy, float radius, bool shadows, int textureWidth = 0, int textureHeight = 0, int itemCullMask = 1)
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
		var sizeX = width > 0 ? width : (int)(radius);
		var sizeY = height > 0 ? height : (int)(radius);
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
		if (PlaceSpeakerStands)
		{
			PropBuilder.CreateProp(_propSort, "res://assets/tiles/props/speaker_stand.png",
				new Vector2I(2, 1), Vector2.Zero, true, new Vector2(24, 16),
				_shadows, _shadows.DepthShadowMaterial, _section, LightMask);
			PropBuilder.CreateProp(_propSort, "res://assets/tiles/props/speaker_stand.png",
				new Vector2I(10, 1), Vector2.Zero, true, new Vector2(24, 16),
				_shadows, _shadows.DepthShadowMaterial, _section, LightMask);
		}

		if (PlaceTableGroup)
		{
			PropBuilder.CreateTableGroup(_propSort, new Vector2I(6, 1),
				_shadows, _shadows.DepthShadowMaterial, _section, LightMask,
				("res://assets/tiles/props/phone_line.png", new Vector2(-32, -26)),
				("res://assets/tiles/props/sound_board.png", new Vector2(0, -26)),
				("res://assets/tiles/props/computer_station.png", new Vector2(32, -38))
			);
			CreateScreeningTrigger();
		}

		if (PlaceAudioCabinet)
		{
			PropBuilder.CreateProp(_propSort, "res://assets/tiles/props/audio_cabinet.png",
				new Vector2I(12, 1), Vector2.Zero, true, new Vector2(24, 16),
				_shadows, _shadows.DepthShadowMaterial, _section, LightMask);
		}

		if (PlaceStorageShelves)
		{
			PropBuilder.CreateProp(_propSort, "res://assets/tiles/props/storage_shelf.png",
				new Vector2I(4, 10), new Vector2(0, -16), true, new Vector2(48, 32),
				_shadows, _shadows.DepthShadowMaterial, _section, LightMask);
			PropBuilder.CreateProp(_propSort, "res://assets/tiles/props/storage_shelf.png",
				new Vector2I(10, 10), new Vector2(0, -16), true, new Vector2(48, 32),
				_shadows, _shadows.DepthShadowMaterial, _section, LightMask);
		}

		if (PlaceChair)
		{
			PropBuilder.CreateProp(_propSort, "res://assets/tiles/props/computer_chair.png",
				new Vector2I(6, 2), Vector2.Zero, false, Vector2.Zero,
				_shadows, _shadows.DepthShadowMaterial, _section, LightMask);
		}

		CreateOnAirSign();
	}

	private void CreateScreeningTrigger()
	{
		if (_screeningTrigger != null)
		{
			return;
		}

		var trigger = new Area2D { Name = "ScreeningTrigger" };
		var shape = new RectangleShape2D { Size = new Vector2(120, 50) };
		var collision = new CollisionShape2D { Shape = shape };

		trigger.AddChild(collision);
		trigger.Position = _section.GridToWorld(new Vector2I(6, 2)) + new Vector2(0, 8);
		trigger.Monitoring = true;
		trigger.Monitorable = true;

		trigger.BodyEntered += OnScreeningTriggerEntered;
		trigger.BodyExited += OnScreeningTriggerExited;

		_propSort.AddChild(trigger);
		_screeningTrigger = trigger;
		_playerInScreeningRange = false;
	}

	private void OnScreeningTriggerEntered(Node body)
	{
		if (body.IsInGroup("player"))
		{
			_playerInScreeningRange = true;
			GD.Print("ControlRoomBuilder: Player entered screening trigger");
			// Note: Room membership is now handled by bounds-based detection in RoomStateManager
		}
	}

	private void OnScreeningTriggerExited(Node body)
	{
		if (body.IsInGroup("player"))
		{
			_playerInScreeningRange = false;
			GD.Print("ControlRoomBuilder: Player exited screening trigger");
			// Note: Room membership is now handled by bounds-based detection in RoomStateManager
		}
	}

	private void CreateOnAirSign()
	{
		var onAirTexture = GD.Load<Texture2D>("res://assets/tiles/props/on_air_sign.png");
		if (onAirTexture == null)
		{
			GD.PrintErr("ControlRoomBuilder: Missing on_air_sign.png texture");
			return;
		}

		// Position: above door, shifted up 64px from wall bottom
		// Wall bottom is at GridAnchor.y + 8 = 508, shift up 64 = 444
		var signPos = GridAnchor + new Vector2(16, -56);

		var onAirSign = new Sprite2D
		{
			Texture = onAirTexture,
			Position = signPos,
			Scale = new Vector2(0.375f, 0.5f),
			Offset = new Vector2(0, -0),
			ZIndex = 1001
		};
		_propSort.AddChild(onAirSign);

		// Light - use helper to create texture so it's visible
		var onAirLight = CreatePointLightWithTexture(
			signPos,
			new Color(1f, 0.1f, 0.1f),
			0.5f,
			60f,
			false,
			32,
			32,
			LightMask
		);
		onAirLight.ZIndex = 1002;
		_propSort.AddChild(onAirLight);
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
		_wallSystem.UpdateOnAirSign(delta);
		_debug.UpdatePlayerRect();
		_debug.UpdatePropRects();

		if (_eventBus == null || _gameStateManager == null)
		{
			CacheServices(world);
		}

		if (_playerInScreeningRange && (Input.IsActionJustPressed("interact") || Input.IsKeyPressed(Key.F)))
		{
			GD.Print("ControlRoomBuilder: interact pressed in range");
			if (_gameStateManager != null)
			{
				GD.Print($"ControlRoomBuilder: CurrentPhase={_gameStateManager.CurrentPhase}");
			}
			else
			{
				GD.PrintErr("ControlRoomBuilder: GameStateManager not available");
			}

			if (_gameStateManager == null || _gameStateManager.CurrentPhase != GamePhase.LiveShow)
			{
				return;
			}

			if (_eventBus != null)
			{
				GD.Print("ControlRoomBuilder: Publishing ScreeningRequestedEvent");
				_eventBus.Publish(new ScreeningRequestedEvent());
			}
			else
			{
				GD.PrintErr("ControlRoomBuilder: EventBus not available for screening request");
			}
		}
	}

	private void CacheServices(WorldRoom world)
	{
		var scene = world.GetTree()?.CurrentScene;
		var root = scene?.GetNodeOrNull<ServiceProviderRoot>("ServiceProviderRoot");
		if (root == null)
		{
			return;
		}

		_eventBus = DependencyInjection.Get<EventBus>(root);
		_gameStateManager = DependencyInjection.Get<GameStateManager>(root);
	}

	public void ToggleDebug()
	{
		_debug.Toggle();
	}
}
