using Godot;

public partial class WorldRoom : Node2D
{
	[ExportGroup("Grid Settings")]
	[Export] public Vector2 ControlRoomGridAnchor = new(0, 0);
	[Export] public Vector2 StudioGridAnchor = new(0, -160);
	[Export] public int GridWidth = 14;
	[Export] public int ControlRoomHeight = 10;
	[Export] public int StudioHeight = 6;

	[ExportGroup("TileMap")]
	[Export] public int FloorSourceId = 0;
	[Export] public int GridDebugSourceId = 6;

	[ExportGroup("Control Room - Door Settings")]
	[Export] private int ControlDoorRow = 3;
	[Export] private int ControlDoorHeightTiles = 2;

	[ExportGroup("Control Room - Window Settings")]
	[Export] private int ControlWindowStartColumn = 3;
	[Export] private int ControlWindowEndColumn = 9;

	[ExportGroup("Control Room - Lighting")]
	[Export] private bool ControlEnableCeilingLight = true;
	[Export] private Color ControlCeilingLightColor = new(1f, 1f, 1f);
	[Export] private float ControlCeilingLightEnergy = 0.8f;
	[Export] private float ControlCeilingLightRadius = 450f;
	[Export] private bool ControlCeilingLightShadows = true;

	[ExportGroup("Control Room - Monitor Light")]
	[Export] private bool ControlEnableMonitorLight = true;
	[Export] private Color ControlMonitorLightColor = new(0f, 1f, 0.27f);
	[Export] private float ControlMonitorLightEnergy = 0.3f;
	[Export] private float ControlMonitorLightRadius = 80f;

	[ExportGroup("Control Room - Desk Lamp")]
	[Export] private bool ControlEnableDeskLampLight = true;
	[Export] private Color ControlDeskLampColor = new(1f, 0.67f, 0.27f);
	[Export] private float ControlDeskLampEnergy = 0.25f;
	[Export] private float ControlDeskLampRadius = 60f;

	[ExportGroup("Control Room - Ambient")]
	[Export] private Color ControlAmbientColor = new(0.15f, 0.15f, 0.20f);

	[ExportGroup("Control Room - Props")]
	[Export] private bool ControlPlaceSpeakerStands = true;
	[Export] private bool ControlPlaceTableGroup = true;
	[Export] private bool ControlPlaceAudioCabinet = true;
	[Export] private bool ControlPlaceStorageShelves = true;
	[Export] private bool ControlPlaceChair = true;

	[ExportGroup("Studio - Door Settings")]
	[Export] private int StudioDoorRow = 3;
	[Export] private int StudioDoorHeightTiles = 2;
	[Export] private bool StudioEnableSouthWall = false;
	[Export] private bool StudioEnableSouthDoor = true;
	[Export] private int StudioSouthDoorRow = 3;

	[ExportGroup("Studio - Lighting")]
	[Export] private bool StudioEnableCeilingLight = true;
	[Export] private Color StudioCeilingLightColor = new(1f, 0.95f, 0.8f);
	[Export] private float StudioCeilingLightEnergy = 0.9f;
	[Export] private float StudioCeilingLightRadius = 450f;
	[Export] private bool StudioCeilingLightShadows = true;

	[ExportGroup("Studio - Monitor Light")]
	[Export] private bool StudioEnableMonitorLight = true;
	[Export] private Color StudioMonitorLightColor = new(0.2f, 0.8f, 1f);
	[Export] private float StudioMonitorLightEnergy = 0.35f;
	[Export] private float StudioMonitorLightRadius = 80f;

	[ExportGroup("Studio - Desk Lamp")]
	[Export] private bool StudioEnableDeskLampLight = true;
	[Export] private Color StudioDeskLampColor = new(1f, 0.9f, 0.6f);
	[Export] private float StudioDeskLampEnergy = 0.3f;
	[Export] private float StudioDeskLampRadius = 60f;

	[ExportGroup("Studio - Ambient")]
	[Export] private Color StudioAmbientColor = new(0.15f, 0.15f, 0.20f);

	[ExportGroup("Studio - Props")]
	[Export] private bool StudioPlaceStudioTable = true;
	[Export] private bool StudioPlaceMonitorConsole = true;
	[Export] private bool StudioPlaceSpeakerStands = true;
	[Export] private bool StudioPlaceStorageCabinet = true;
	[Export] private bool StudioPlaceChair = true;
	[Export] private bool StudioPlaceCoffeeStation = true;
	[Export] private bool StudioPlaceWallDecor = true;

	public const float TileSize = 16.0f;
	public static readonly Vector2I AtlasCoordsLeft = new(0, 0);

	public Node2D PropSort = null!;
	public CastShadowSystem ControlShadows => _controlShadows;
	public CastShadowSystem StudioShadows => _studioShadows;
	public CharacterBody2D Player = null!;

	private TileMapLayer _controlFloorLayer = null!;
	private TileMapLayer _controlDoorLayer = null!;
	private TileMapLayer _controlGridDebugLayer = null!;
	private TileMapLayer _studioFloorLayer = null!;
	private TileMapLayer _studioDoorLayer = null!;
	private TileMapLayer _studioGridDebugLayer = null!;

	private WallSystem _controlWallSystem = null!;
	private WallSystem _studioWallSystem = null!;
	private CastShadowSystem _controlShadows = null!;
	private CastShadowSystem _studioShadows = null!;
	private RoomDebug _controlDebug = null!;
	private RoomDebug _studioDebug = null!;

	private CanvasModulate _controlCanvasModulate = null!;
	private CanvasModulate _studioCanvasModulate = null!;
	private PointLight2D _controlCeilingLight = null!;
	private PointLight2D _controlMonitorLight = null!;
	private PointLight2D _controlDeskLampLight = null!;
	private PointLight2D _studioCeilingLight = null!;
	private PointLight2D _studioMonitorLight = null!;
	private PointLight2D _studioDeskLampLight = null!;

	private float _controlFlickerTime;
	private float _studioFlickerTime;

	public void SetPlayer(CharacterBody2D player)
	{
		Player = player;
		if (_controlSection != null)
			_controlSection.Player = player;
		if (_studioSection != null)
			_studioSection.Player = player;
	}

	public Vector2 ControlRoomGridToWorld(Vector2I gridPos)
	{
		return _controlSection.GridToWorld(gridPos);
	}

	public Vector2 StudioGridToWorld(Vector2I gridPos)
	{
		return _studioSection.GridToWorld(gridPos);
	}

	private RoomSection _controlSection = null!;
	private RoomSection _studioSection = null!;

	public override void _Ready()
	{
		CreateTileMapLayers();
		CreateSystems();
		CreateLighting();
		CreateProps();
	}

	private void CreateTileMapLayers()
	{
		var tileSet = GD.Load<TileSet>("res://assets/tiles/topdown/topdown_tileset.tres");
		if (tileSet == null)
		{
			GD.PrintErr("WorldRoom: Failed to load tileset");
			return;
		}

		_controlFloorLayer = new TileMapLayer { Name = "ControlFloorLayer", TileSet = tileSet };
		_controlDoorLayer = new TileMapLayer { Name = "ControlDoorLayer", TileSet = tileSet, ZIndex = 1000 };
		_controlGridDebugLayer = new TileMapLayer { Name = "ControlGridDebugLayer", TileSet = tileSet, Visible = false };

		_studioFloorLayer = new TileMapLayer { Name = "StudioFloorLayer", TileSet = tileSet };
		_studioDoorLayer = new TileMapLayer { Name = "StudioDoorLayer", TileSet = tileSet, ZIndex = 1000 };
		_studioGridDebugLayer = new TileMapLayer { Name = "StudioGridDebugLayer", TileSet = tileSet, Visible = false };

		AddChild(_controlFloorLayer);
		AddChild(_controlDoorLayer);
		AddChild(_controlGridDebugLayer);
		AddChild(_studioFloorLayer);
		AddChild(_studioDoorLayer);
		AddChild(_studioGridDebugLayer);

		PropSort = new Node2D { Name = "PropSort" };
		PropSort.YSortEnabled = true;
		AddChild(PropSort);

		_controlFloorLayer.Position = ControlRoomGridAnchor;
		_controlDoorLayer.Position = ControlRoomGridAnchor;
		_controlGridDebugLayer.Position = ControlRoomGridAnchor;
		_studioFloorLayer.Position = StudioGridAnchor;
		_studioDoorLayer.Position = StudioGridAnchor;
		_studioGridDebugLayer.Position = StudioGridAnchor;

		_controlSection = new RoomSection
		{
			FloorLayer = _controlFloorLayer,
			DoorLayer = _controlDoorLayer,
			PropSort = PropSort,
			GridOffset = ControlRoomGridAnchor,
			GridWidth = GridWidth,
			GridHeight = ControlRoomHeight
		};

		_studioSection = new RoomSection
		{
			FloorLayer = _studioFloorLayer,
			DoorLayer = _studioDoorLayer,
			PropSort = PropSort,
			GridOffset = StudioGridAnchor,
			GridWidth = GridWidth,
			GridHeight = StudioHeight
		};

		CreateFloors();
	}

	private void CreateFloors()
	{
		for (int y = 0; y < ControlRoomHeight; y++)
		{
			for (int x = 0; x < GridWidth; x++)
			{
				_controlFloorLayer.SetCell(new Vector2I(x, y), FloorSourceId, AtlasCoordsLeft);
			}
		}

		for (int y = 0; y < StudioHeight; y++)
		{
			for (int x = 0; x < GridWidth; x++)
			{
				_studioFloorLayer.SetCell(new Vector2I(x, y), FloorSourceId, AtlasCoordsLeft);
			}
		}
	}

	private void CreateSystems()
	{
		_controlWallSystem = new WallSystem
		{
			DoorRow = ControlDoorRow,
			DoorHeightTiles = ControlDoorHeightTiles,
			WindowStartColumn = ControlWindowStartColumn,
			WindowEndColumn = ControlWindowEndColumn
		};
		AddChild(_controlWallSystem);

		_controlShadows = new CastShadowSystem { LightRadius = ControlCeilingLightRadius };
		AddChild(_controlShadows);

		_controlDebug = new RoomDebug { DebugEnabled = false };
		AddChild(_controlDebug);

		_studioWallSystem = new WallSystem
		{
			DoorRow = StudioDoorRow,
			DoorHeightTiles = StudioDoorHeightTiles,
			EnableSouthWall = StudioEnableSouthWall,
			EnableSouthDoor = StudioEnableSouthDoor,
			SouthDoorRow = StudioSouthDoorRow
		};
		AddChild(_studioWallSystem);

		_studioShadows = new CastShadowSystem { LightRadius = StudioCeilingLightRadius };
		AddChild(_studioShadows);

		_studioDebug = new RoomDebug { DebugEnabled = false };
		AddChild(_studioDebug);

		_controlWallSystem.Initialize(_controlSection);
		_studioWallSystem.Initialize(_studioSection);

		_controlShadows.Initialize(_controlSection, _controlCeilingLight);
		_studioShadows.Initialize(_studioSection, _studioCeilingLight);

		_controlDebug.Initialize(_controlSection, _controlWallSystem, _controlShadows, _controlCeilingLight, _controlMonitorLight, _controlDeskLampLight);
		_studioDebug.Initialize(_studioSection, _studioWallSystem, _studioShadows, _studioCeilingLight, _studioMonitorLight, _studioDeskLampLight);

		_controlWallSystem.CreateWalls();
		_controlWallSystem.CreateWallColliders();
		_studioWallSystem.CreateWalls();
		_studioWallSystem.CreateWallColliders();
	}

	private void CreateLighting()
	{
		_controlCanvasModulate = new CanvasModulate { Color = ControlAmbientColor, Name = "ControlAmbient" };
		AddChild(_controlCanvasModulate);

		_studioCanvasModulate = new CanvasModulate { Color = StudioAmbientColor, Name = "StudioAmbient" };
		AddChild(_studioCanvasModulate);

		var controlCenterX = GridWidth / 2f;
		var controlCenterY = ControlRoomHeight / 2f;
		var controlCenter = ControlRoomGridToWorld(new Vector2I((int)controlCenterX, (int)controlCenterY));

		if (ControlEnableCeilingLight)
		{
			_controlCeilingLight = CreatePointLightWithTexture(
				new Vector2(controlCenter.X, 32),
				ControlCeilingLightColor,
				ControlCeilingLightEnergy,
				ControlCeilingLightRadius,
				ControlCeilingLightShadows,
				256,
				256
			);
			AddChild(_controlCeilingLight);
		}

		var controlTablePosition = ControlRoomGridToWorld(new Vector2I(6, 1));

		if (ControlEnableMonitorLight)
		{
			_controlMonitorLight = CreatePointLightWithTexture(
				controlTablePosition + new Vector2(32, -38),
				ControlMonitorLightColor,
				ControlMonitorLightEnergy,
				ControlMonitorLightRadius,
				false
			);
			_controlMonitorLight.TextureScale = 2.0f;
			AddChild(_controlMonitorLight);
		}

		if (ControlEnableDeskLampLight)
		{
			_controlDeskLampLight = CreatePointLightWithTexture(
				controlTablePosition + new Vector2(-32, -35),
				ControlDeskLampColor,
				ControlDeskLampEnergy,
				ControlDeskLampRadius,
				false
			);
			_controlDeskLampLight.TextureScale = 1.8f;
			AddChild(_controlDeskLampLight);
		}

		var studioCenterX = GridWidth / 2f;
		var studioCenterY = StudioHeight / 2f;
		var studioCenter = StudioGridToWorld(new Vector2I((int)studioCenterX, (int)studioCenterY));

		if (StudioEnableCeilingLight)
		{
			_studioCeilingLight = CreatePointLightWithTexture(
				new Vector2(studioCenter.X, 32),
				StudioCeilingLightColor,
				StudioCeilingLightEnergy,
				StudioCeilingLightRadius,
				StudioCeilingLightShadows,
				256,
				256
			);
			AddChild(_studioCeilingLight);
		}

		var studioTablePosition = StudioGridToWorld(new Vector2I(6, 2));

		if (StudioEnableMonitorLight)
		{
			_studioMonitorLight = CreatePointLightWithTexture(
				studioTablePosition + new Vector2(32, -38),
				StudioMonitorLightColor,
				StudioMonitorLightEnergy,
				StudioMonitorLightRadius,
				false
			);
			_studioMonitorLight.TextureScale = 2.0f;
			AddChild(_studioMonitorLight);
		}

		if (StudioEnableDeskLampLight)
		{
			_studioDeskLampLight = CreatePointLightWithTexture(
				studioTablePosition + new Vector2(-32, -35),
				StudioDeskLampColor,
				StudioDeskLampEnergy,
				StudioDeskLampRadius,
				false
			);
			_studioDeskLampLight.TextureScale = 1.8f;
			AddChild(_studioDeskLampLight);
		}

		_controlFlickerTime = 0f;
		_studioFlickerTime = 0f;
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
		CreateControlRoomProps();
		CreateStudioProps();
	}

	private void CreateControlRoomProps()
	{
		if (ControlPlaceSpeakerStands)
		{
			PropBuilder.CreateProp(PropSort, "res://assets/tiles/props/speaker_stand.png",
				new Vector2I(2, 1), Vector2.Zero, true, new Vector2(24, 16),
				_controlShadows, _controlShadows.DepthShadowMaterial, ControlRoomGridToWorld(new Vector2I(2, 1)));
			PropBuilder.CreateProp(PropSort, "res://assets/tiles/props/speaker_stand.png",
				new Vector2I(10, 1), Vector2.Zero, true, new Vector2(24, 16),
				_controlShadows, _controlShadows.DepthShadowMaterial, ControlRoomGridToWorld(new Vector2I(10, 1)));
		}

		if (ControlPlaceTableGroup)
		{
			PropBuilder.CreateTableGroup(PropSort, new Vector2I(6, 1),
				_controlShadows, _controlShadows.DepthShadowMaterial, _controlSection,
				("res://assets/tiles/props/phone_line.png", new Vector2(-32, -26)),
				("res://assets/tiles/props/sound_board.png", new Vector2(0, -26)),
				("res://assets/tiles/props/computer_station.png", new Vector2(32, -38))
			);
		}

		if (ControlPlaceAudioCabinet)
		{
			PropBuilder.CreateProp(PropSort, "res://assets/tiles/props/audio_cabinet.png",
				new Vector2I(12, 1), Vector2.Zero, true, new Vector2(24, 16),
				_controlShadows, _controlShadows.DepthShadowMaterial, ControlRoomGridToWorld(new Vector2I(12, 1)));
		}

		if (ControlPlaceStorageShelves)
		{
			PropBuilder.CreateProp(PropSort, "res://assets/tiles/props/storage_shelf.png",
				new Vector2I(4, 10), new Vector2(0, -8), true, new Vector2(48, 32),
				_controlShadows, _controlShadows.DepthShadowMaterial, ControlRoomGridToWorld(new Vector2I(4, 10)));
			PropBuilder.CreateProp(PropSort, "res://assets/tiles/props/storage_shelf.png",
				new Vector2I(10, 10), new Vector2(0, -8), true, new Vector2(48, 32),
				_controlShadows, _controlShadows.DepthShadowMaterial, ControlRoomGridToWorld(new Vector2I(10, 10)));
		}

		if (ControlPlaceChair)
		{
			PropBuilder.CreateProp(PropSort, "res://assets/tiles/props/computer_chair.png",
				new Vector2I(6, 2), Vector2.Zero, false, Vector2.Zero,
				_controlShadows, _controlShadows.DepthShadowMaterial, ControlRoomGridToWorld(new Vector2I(6, 2)));
		}
	}

	private void CreateStudioProps()
	{
		if (StudioPlaceStudioTable)
		{
			PropBuilder.CreateProp(PropSort, "res://assets/tiles/props/studio_table.png",
				new Vector2I(6, 2), Vector2.Zero, true, new Vector2(48, 24),
				_studioShadows, _studioShadows.DepthShadowMaterial, StudioGridToWorld(new Vector2I(6, 2)));
		}

		if (StudioPlaceMonitorConsole)
		{
			PropBuilder.CreateProp(PropSort, "res://assets/tiles/props/monitor_console.png",
				new Vector2I(6, 1), new Vector2(0, -26), false, Vector2.Zero,
				_studioShadows, _studioShadows.DepthShadowMaterial, StudioGridToWorld(new Vector2I(6, 1)));
		}

		if (StudioPlaceSpeakerStands)
		{
			PropBuilder.CreateProp(PropSort, "res://assets/tiles/props/speaker_stand.png",
				new Vector2I(2, 1), Vector2.Zero, true, new Vector2(24, 16),
				_studioShadows, _studioShadows.DepthShadowMaterial, StudioGridToWorld(new Vector2I(2, 1)));
			PropBuilder.CreateProp(PropSort, "res://assets/tiles/props/speaker_stand.png",
				new Vector2I(10, 1), Vector2.Zero, true, new Vector2(24, 16),
				_studioShadows, _studioShadows.DepthShadowMaterial, StudioGridToWorld(new Vector2I(10, 1)));
		}

		if (StudioPlaceStorageCabinet)
		{
			PropBuilder.CreateProp(PropSort, "res://assets/tiles/props/audio_cabinet.png",
				new Vector2I(11, 1), Vector2.Zero, true, new Vector2(24, 16),
				_studioShadows, _studioShadows.DepthShadowMaterial, StudioGridToWorld(new Vector2I(11, 1)));
		}

		if (StudioPlaceChair)
		{
			PropBuilder.CreateProp(PropSort, "res://assets/tiles/props/computer_chair.png",
				new Vector2I(6, 3), Vector2.Zero, false, Vector2.Zero,
				_studioShadows, _studioShadows.DepthShadowMaterial, StudioGridToWorld(new Vector2I(6, 3)));
		}

		if (StudioPlaceCoffeeStation)
		{
			PropBuilder.CreateProp(PropSort, "res://assets/tiles/props/coffee_station.png",
				new Vector2I(1, 1), Vector2.Zero, true, new Vector2(20, 24),
				_studioShadows, _studioShadows.DepthShadowMaterial, StudioGridToWorld(new Vector2I(1, 1)));
		}

		if (StudioPlaceWallDecor)
		{
			PropBuilder.CreateProp(PropSort, "res://assets/tiles/props/poster.png",
				new Vector2I(3, 1), Vector2.Zero, true, new Vector2(16, 16),
				_studioShadows, _studioShadows.DepthShadowMaterial, StudioGridToWorld(new Vector2I(3, 1)));
			PropBuilder.CreateProp(PropSort, "res://assets/tiles/props/wall_clock.png",
				new Vector2I(10, 1), Vector2.Zero, true, new Vector2(16, 16),
				_studioShadows, _studioShadows.DepthShadowMaterial, StudioGridToWorld(new Vector2I(10, 1)));
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_select"))
		{
			_controlDebug.Toggle();
			_studioDebug.Toggle();
		}
	}

	public override void _Process(double delta)
	{
		if (Player != null)
		{
			_controlWallSystem.UpdateVisibility(Player);
			_studioWallSystem.UpdateVisibility(Player);
		}

		UpdateLighting(delta);

		_controlShadows.Update(delta);
		_studioShadows.Update(delta);

		_controlDebug.UpdatePlayerRect();
		_controlDebug.UpdatePropRects();
		_studioDebug.UpdatePlayerRect();
		_studioDebug.UpdatePropRects();
	}

	private void UpdateLighting(double delta)
	{
		_controlFlickerTime += (float)delta;

		if (_controlCeilingLight != null)
		{
			_controlCeilingLight.Energy = ControlCeilingLightEnergy;
		}

		if (_controlMonitorLight != null)
		{
			var pulse = ControlMonitorLightEnergy + Mathf.Sin(_controlFlickerTime * 2f) * 0.03f;
			_controlMonitorLight.Energy = pulse;
		}

		if (_controlDeskLampLight != null)
		{
			var shimmer = ControlDeskLampEnergy + Mathf.Sin(_controlFlickerTime * 3f) * 0.02f;
			_controlDeskLampLight.Energy = shimmer;
		}

		_studioFlickerTime += (float)delta;

		if (_studioCeilingLight != null)
		{
			_studioCeilingLight.Energy = StudioCeilingLightEnergy;
		}

		if (_studioMonitorLight != null)
		{
			var pulse = StudioMonitorLightEnergy + Mathf.Sin(_studioFlickerTime * 2f) * 0.03f;
			_studioMonitorLight.Energy = pulse;
		}

		if (_studioDeskLampLight != null)
		{
			var shimmer = StudioDeskLampEnergy + Mathf.Sin(_studioFlickerTime * 3f) * 0.02f;
			_studioDeskLampLight.Energy = shimmer;
		}
	}
}
