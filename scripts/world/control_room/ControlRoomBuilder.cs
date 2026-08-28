using Godot;
using KBTV.Core;
using KBTV.UI;
public partial class ControlRoomBuilder : IRoomBuilder
{
	[ExportGroup("Grid Settings")]
	[Export] public Vector2 GridAnchor = new(0, 1000);
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
	[Export] private float CeilingLightEnergy = 1.6f;
	[Export] private float CeilingLightRadius = 2000f;
	[Export] private bool CeilingLightShadows = true;

	[ExportGroup("Monitor Light")]
	[Export] private bool EnableMonitorLight = true;
	[Export] private Color MonitorLightColor = new(0f, 1f, 0.27f);
	[Export] private float MonitorLightEnergy = 0.3f;
	[Export] private float MonitorLightRadius = 160f;

	[ExportGroup("Desk Lamp")]
	[Export] private bool EnableDeskLampLight = true;
	[Export] private Color DeskLampColor = new(1f, 0.67f, 0.27f);
	[Export] private float DeskLampEnergy = 0.25f;
	[Export] private float DeskLampRadius = 120f;

	[ExportGroup("Ambient")]
	[Export] private Color AmbientColor = new(0.18f, 0.19f, 0.22f);

	/// <summary>Single source of truth for where everything in this room is placed.</summary>
	private readonly ControlRoomLayout _layout = new();

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

		_shadows = new CastShadowSystem { LightRadius = CeilingLightRadius, GroupName = "shadow_pivots_control" };
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

		var center = GridToWorld(new Vector2I((int)(GridWidth / 2f), (int)(GridHeight / 2f)));

		if (EnableCeilingLight)
		{
			_ceilingLight = RoomLightingBuilder.MakeCeilingLight(
				new Vector2(center.X, center.Y - _layout.CeilingLightOffsetY),
				CeilingLightColor,
				CeilingLightEnergy,
				CeilingLightRadius,
				CeilingLightShadows,
				LightMask,
				_layout.CeilingLightTextureSize,
				_layout.CeilingLightTextureScale
			);
			world.AddChild(_ceilingLight);
		}

		// The table group's origin, including the downward drop so the lights track the desk.
		var tablePosition = ControlTableGroupProp.GetTablePosition(_section);

		if (EnableMonitorLight)
		{
			_monitorLight = RoomLightingBuilder.MakeLight(
				tablePosition + ControlTableGroupProp.MonitorLightOffset,
				MonitorLightColor,
				MonitorLightEnergy,
				MonitorLightRadius,
				false,
				LightMask
			);
			world.AddChild(_monitorLight);
		}

		if (EnableDeskLampLight)
		{
			_deskLampLight = RoomLightingBuilder.MakeLight(
				tablePosition + ControlTableGroupProp.DeskLampLightOffset,
				DeskLampColor,
				DeskLampEnergy,
				DeskLampRadius,
				false,
				LightMask
			);
			world.AddChild(_deskLampLight);
		}

		_shadows.Initialize(_section, _ceilingLight);
		// Set the depth-shadow shader's light origin to the ceiling light's real position.
		// Deferred so the just-added light's global transform is settled before we read it
		// (CastShadowSystem._Process also keeps it fresh every frame).
		_shadows.CallDeferred(nameof(CastShadowSystem.UpdateDepthShadowLightPosition));
		_flickerTime = 0f;
	}

	private void InitializeDebug()
	{
		_debug.Initialize(_section, _wallSystem, _shadows, _ceilingLight, _monitorLight, _deskLampLight);
	}

	private void CreateProps()
	{
		if (PlaceSpeakerStands)
		{
			foreach (var spec in SpeakerStandsProp.Specs)
			{
				CreateProp(spec, SpeakerStandsProp.TexturePath);
			}
		}

		if (PlaceTableGroup)
		{
			ControlTableGroupProp.CreateTableGroup(_propSort, _shadows, _shadows.DepthShadowMaterial, _section, LightMask);
			CreateScreeningTrigger();
		}

		if (PlaceAudioCabinet)
		{
			CreateProp(AudioCabinetProp.Spec, AudioCabinetProp.TexturePath);
		}

		if (PlaceStorageShelves)
		{
			foreach (var spec in StorageShelvesProp.Specs)
			{
				CreateProp(spec, StorageShelvesProp.TexturePath);
			}
		}

		if (PlaceChair)
		{
			// The chair is walk-through, so place it via the non-collidable CreateProp overload.
			PropBuilder.CreateProp(_propSort, ControlChairProp.TexturePath,
				ControlChairProp.Placement.Cell, ControlChairProp.Placement.Offset, false, Vector2.Zero,
				_shadows, _shadows.DepthShadowMaterial, _section, LightMask);
		}

		OnAirSignProp.Create(_propSort, GridAnchor + _layout.OnAirSignFromAnchor, _layout.OnAirSignScale,
			_layout.OnAirSignLightColor, _layout.OnAirSignLightEnergy, _layout.OnAirSignLightRadius, LightMask);
	}

	/// <summary>Applies a <see cref="PropSpec"/> (anchor cell + offset + collider) via <see cref="PropBuilder"/>.</summary>
	private Node2D CreateProp(PropSpec spec, string texturePath)
	{
		return PropBuilder.CreatePropAutoCollider(
			_propSort,
			texturePath,
			spec.Cell,
			spec.Offset,
			_shadows,
			_shadows.DepthShadowMaterial,
			_section,
			LightMask,
			spec.CreateCastShadow,
			spec.FloorScanHeight,
			spec.ColliderOverride
		);
	}

	private void CreateScreeningTrigger()
	{
		if (_screeningTrigger != null)
		{
			return;
		}

		var trigger = new Area2D { Name = "ScreeningTrigger" };
		var shape = new RectangleShape2D { Size = ControlTableGroupProp.ScreeningTriggerSize };
		var collision = new CollisionShape2D { Shape = shape };

		trigger.AddChild(collision);
		trigger.Position = ControlTableGroupProp.ScreeningTrigger.ToWorld(_section);
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
