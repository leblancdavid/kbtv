using Godot;
using KBTV.Core;
using KBTV.UI;

/// <summary>
/// The control room (Vern's desk, boards, screening trigger). A self-contained <see cref="RoomBase"/>
/// node that builds and owns its layers, walls, lighting, shadows, debug overlay and props.
/// </summary>
public sealed partial class ControlRoom : RoomBase
{
	// ── Room-level layout facts (formerly ControlRoomLayout.cs) ─────────────────────────
	private const int CeilingLightOffsetY = 64;
	private const int CeilingLightTextureSize = 512;
	private const float CeilingLightTextureScale = 1.0f;

	[ExportGroup("Door Settings")]
	[Export] public int DoorRow = 3;
	[Export] public int DoorHeightTiles = 2;

	[ExportGroup("Window Settings")]
	[Export] public int WindowStartColumn = 3;
	[Export] public int WindowEndColumn = 9;

	[ExportGroup("Lighting")]
	[Export] public bool EnableCeilingLight = true;
	[Export] public Color CeilingLightColor = new(1f, 0.95f, 0.9f);
	[Export] public float CeilingLightEnergy = 1.6f;
	[Export] public float CeilingLightRadius = 2000f;
	[Export] public bool CeilingLightShadows = true;

	[ExportGroup("Monitor Light")]
	[Export] public bool EnableMonitorLight = true;
	[Export] public Color MonitorLightColor = new(0f, 1f, 0.27f);
	[Export] public float MonitorLightEnergy = 0.3f;
	[Export] public float MonitorLightRadius = 160f;

	[ExportGroup("Desk Lamp")]
	[Export] public bool EnableDeskLampLight = true;
	[Export] public Color DeskLampColor = new(1f, 0.67f, 0.27f);
	[Export] public float DeskLampEnergy = 0.25f;
	[Export] public float DeskLampRadius = 120f;

	[ExportGroup("Ambient")]
	[Export] public Color AmbientColor = new(0.18f, 0.19f, 0.22f);

	[ExportGroup("Props")]
	[Export] public bool PlaceSpeakerStands = true;
	[Export] public bool PlaceTableGroup = true;
	[Export] public bool PlaceAudioCabinet = true;
	[Export] public bool PlaceStorageShelves = true;
	[Export] public bool PlaceChair = true;

	private WallSystem _wallSystem = null!;
	private RoomDebug _debug = null!;
	private CanvasModulate _canvasModulate = null!;
	private PointLight2D _ceilingLight = null!;
	private PointLight2D _monitorLight = null!;
	private PointLight2D _deskLampLight = null!;
	private float _flickerTime;
	private Area2D? _screeningTrigger;
	private bool _playerInScreeningRange;
	private EventBus? _eventBus;
	private GameStateManager? _gameStateManager;

	protected override void ConfigureRoom()
	{
		GridAnchor = new(0, 1000);
		GridWidth = 14;
		GridHeight = 10;
		LightMask = 1;
		FloorSourceId = 9;
	}

	protected override void OnRoomReady()
	{
		CacheServices();
		CreateSystems();
		CreateLighting();
		InitializeDebug();
		CreateProps();

		var roomState = ServiceRegistry.Instance?.Get<RoomStateManager>();
		if (roomState != null)
		{
			roomState.SetControlRoomBounds(GetFloorBounds());
		}
	}

	protected override void OnRoomProcess(double delta)
	{
		if (Player != null)
		{
			_wallSystem.UpdateVisibility(Player);
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

		Shadows.Update(delta);
		_wallSystem.UpdateOnAirSign(delta);
		_debug.UpdatePlayerRect();
		_debug.UpdatePropRects();

		if (_eventBus == null || _gameStateManager == null)
		{
			CacheServices();
		}

		if (_playerInScreeningRange && (Input.IsActionJustPressed("interact") || Input.IsKeyPressed(Key.F)))
		{
			HandleScreeningRequest();
		}
	}

	private void CreateSystems()
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
			EnableOnAirSign = false,
			EnableEastDoor = true,
			CustomSouthWallTexture = GD.Load<Texture2D>("res://assets/tiles/topdown/wall_south_atlas.png"),
			CustomEastWallTexture = GD.Load<Texture2D>("res://assets/tiles/topdown/studio_north_atlas.png"),
			CustomEastDoorTexture = GD.Load<Texture2D>("res://assets/tiles/topdown/wall_east_door_atlas.png")
		};
		AddChild(_wallSystem);

		Shadows = new CastShadowSystem { LightRadius = CeilingLightRadius, GroupName = "shadow_pivots_control" };
		AddChild(Shadows);

		_debug = new RoomDebug { DebugEnabled = false, ZIndex = 2000 };
		AddChild(_debug);
		DebugNode = _debug;

		_wallSystem.Initialize(this);
		_wallSystem.CreateWalls();
		_wallSystem.CreateWallColliders();
	}

	private void CreateLighting()
	{
		_canvasModulate = new CanvasModulate { Color = AmbientColor, Name = "ControlAmbient" };
		AddChild(_canvasModulate);

		var center = GridToWorld(new Vector2I((int)(GridWidth / 2f), (int)(GridHeight / 2f)));

		if (EnableCeilingLight)
		{
			_ceilingLight = RoomLightingBuilder.MakeCeilingLight(
				new Vector2(center.X, center.Y - CeilingLightOffsetY),
				CeilingLightColor,
				CeilingLightEnergy,
				CeilingLightRadius,
				CeilingLightShadows,
				LightMask,
				CeilingLightTextureSize,
				CeilingLightTextureScale
			);
			AddChild(_ceilingLight);
		}

		// The table group's origin, including the downward drop so the lights track the desk.
		var tablePosition = ControlTableGroupProp.GetTablePosition(this);

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
			AddChild(_monitorLight);
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
			AddChild(_deskLampLight);
		}

		Shadows.Initialize(this, _ceilingLight);
		// Set the depth-shadow shader's light origin to the ceiling light's real position.
		// Deferred so the just-added light's global transform is settled before we read it
		// (CastShadowSystem._Process also keeps it fresh every frame).
		Shadows.CallDeferred(nameof(CastShadowSystem.UpdateDepthShadowLightPosition));
		_flickerTime = 0f;
	}

	private void InitializeDebug()
	{
		_debug.Initialize(this, _wallSystem, Shadows, _ceilingLight, _monitorLight, _deskLampLight);
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
			ControlTableGroupProp.CreateTableGroup(PropSort, Shadows.DepthShadowMaterial, this, LightMask);
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
			ControlChairProp.Create(PropSort, Shadows, Shadows.DepthShadowMaterial, this, LightMask);
		}

		OnAirSignProp.Create(PropSort, OnAirSignProp.ControlRoom, GridAnchor, LightMask);
	}

	/// <summary>Applies a <see cref="PropSpec"/> (anchor cell + offset + collider) via <see cref="PropBuilder"/>.</summary>
	private Node2D CreateProp(PropSpec spec, string texturePath)
	{
		return PropBuilder.CreatePropAutoCollider(
			PropSort,
			texturePath,
			spec.Cell,
			spec.Offset,
			Shadows,
			Shadows.DepthShadowMaterial,
			this,
			LightMask,
			spec.CreateCastShadow,
			spec.FloorScanHeight,
			spec.ColliderOverride,
			spec.FlipV,
			spec.FlipH,
			spec.Scale
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
		trigger.Position = ControlTableGroupProp.ScreeningTrigger.ToWorld(this);
		trigger.Monitoring = true;
		trigger.Monitorable = true;

		trigger.BodyEntered += OnScreeningTriggerEntered;
		trigger.BodyExited += OnScreeningTriggerExited;

		PropSort.AddChild(trigger);
		_screeningTrigger = trigger;
		_playerInScreeningRange = false;
	}

	private void OnScreeningTriggerEntered(Node body)
	{
		if (body.IsInGroup("player"))
		{
			_playerInScreeningRange = true;
			GD.Print("ControlRoom: Player entered screening trigger");
		}
	}

	private void OnScreeningTriggerExited(Node body)
	{
		if (body.IsInGroup("player"))
		{
			_playerInScreeningRange = false;
			GD.Print("ControlRoom: Player exited screening trigger");
		}
	}

	private void HandleScreeningRequest()
	{
		GD.Print("ControlRoom: interact pressed in range");
		if (_gameStateManager != null)
		{
			GD.Print($"ControlRoom: CurrentPhase={_gameStateManager.CurrentPhase}");
		}
		else
		{
			GD.PrintErr("ControlRoom: GameStateManager not available");
		}

		if (_gameStateManager == null || _gameStateManager.CurrentPhase != GamePhase.LiveShow)
		{
			return;
		}

		if (_eventBus != null)
		{
			GD.Print("ControlRoom: Publishing ScreeningRequestedEvent");
			_eventBus.Publish(new ScreeningRequestedEvent());
		}
		else
		{
			GD.PrintErr("ControlRoom: EventBus not available for screening request");
		}
	}

	private void CacheServices()
	{
		var scene = GetTree()?.CurrentScene;
		var root = scene?.GetNodeOrNull<ServiceProviderRoot>("ServiceProviderRoot");
		if (root == null)
		{
			return;
		}

		_eventBus = DependencyInjection.Get<EventBus>(root);
		_gameStateManager = DependencyInjection.Get<GameStateManager>(root);
	}
}