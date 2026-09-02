using Godot;
using KBTV.Core;
using KBTV.Data;

/// <summary>
/// The studio (Vern's on-stage group, round table, ambient smoke). A self-contained
/// <see cref="RoomBase"/> node that builds and owns its layers, walls, lighting, shadows,
/// smoke, debug overlay and props.
/// </summary>
public sealed partial class StudioRoom : RoomBase
{
	// ── Room-level layout facts (formerly StudioLayout.cs) ──────────────────────────────
	private const int CeilingLightOffsetY = 32;
	private const int CeilingLightTextureSize = 512;
	private const float CeilingLightTextureScale = 1.0f;
	private const int SmokeRowsFromBottom = 3;
	private const int SmokeColumn = 7;

	[ExportGroup("Door Settings")]
	[Export] public int DoorRow = 3;
	[Export] public int DoorHeightTiles = 2;
	[Export] public bool EnableSouthWall = false;
	[Export] public bool EnableSouthDoor = false;
	[Export] public int SouthDoorRow = 3;

	[ExportGroup("Window Settings")]
	[Export] public int WindowStartColumn = 99;
	[Export] public int WindowEndColumn = 0;

	[ExportGroup("Lighting")]
	[Export] public bool EnableCeilingLight = true;
	[Export] public Color CeilingLightColor = new(1f, 0.95f, 0.9f);
	[Export] public float CeilingLightEnergy = 1.1f;
	[Export] public float CeilingLightRadius = 900f;
	[Export] public bool CeilingLightShadows = true;

	[ExportGroup("Ambient")]
	[Export] public Color AmbientColor = new(0.18f, 0.19f, 0.22f);

	[ExportGroup("Smoke")]
	[Export] public bool EnableSmoke = true;
	[Export] public float SmokeMaxParticles = 100f;
	[Export] public float SmokeDecayTime = 60f;

	[ExportGroup("Props")]
	[Export] public bool PlaceRoundTable = true;
	[Export] public bool PlaceVern = true;

	private WallSystem _wallSystem = null!;
	private RoomDebug _debug = null!;
	private CanvasModulate _canvasModulate = null!;
	private PointLight2D _ceilingLight = null!;
	private readonly StudioSmoke _smoke = new();
	private float _flickerTime;

	protected override void ConfigureRoom()
	{
		GridAnchor = new(0, 776);
		GridWidth = 14;
		GridHeight = 6;
		LightMask = 2;
		FloorSourceId = 9;
	}

	protected override void OnRoomReady()
	{
		CreateSystems();
		CreateLighting();
		CreateSmoke();
		InitializeDebug();
		CreateProps();

		var roomState = ServiceRegistry.Instance?.Get<RoomStateManager>();
		if (roomState != null)
		{
			roomState.SetStudioBounds(GetFloorBounds());
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

		_smoke.Update(ServiceRegistry.Instance?.VernStats);

		Shadows.Update(delta);
		_debug.UpdatePlayerRect();
		_debug.UpdatePropRects();
	}

	private void CreateSystems()
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
			NorthWallLightMask = LightMask,
			CustomSouthWallTexture = GD.Load<Texture2D>("res://assets/tiles/topdown/wall_south_atlas.png"),
			CustomEastWallTexture = GD.Load<Texture2D>("res://assets/tiles/topdown/studio_north_atlas.png")
		};
		AddChild(_wallSystem);

		Shadows = new CastShadowSystem { LightRadius = CeilingLightRadius, GroupName = "shadow_pivots_studio" };
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
		_canvasModulate = new CanvasModulate { Color = AmbientColor, Name = "StudioAmbient" };
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

		Shadows.Initialize(this, _ceilingLight);
		// Keep the depth-shadow light origin in sync with the studio ceiling light so
		// the studio's props (which use the same shader) render at full brightness.
		Shadows.CallDeferred(nameof(CastShadowSystem.UpdateDepthShadowLightPosition));
		_flickerTime = 0f;
	}

	private void CreateSmoke()
	{
		if (!EnableSmoke)
		{
			return;
		}

		var smokePosition = GridToWorld(new Vector2I(SmokeColumn, GridHeight - SmokeRowsFromBottom));
		_smoke.Initialize(PropSort, smokePosition, (int)SmokeMaxParticles, SmokeDecayTime, LightMask);
	}

	private void InitializeDebug()
	{
		_debug.Initialize(this, _wallSystem, Shadows, _ceilingLight, null, null);
	}

	private void CreateProps()
	{
		foreach (var placement in BookcasesProp.Placements)
		{
			PropBuilder.CreatePropAutoCollider(
				PropSort, BookcasesProp.TexturePath, placement.Cell, placement.Offset,
				Shadows, Shadows.DepthShadowMaterial, this, LightMask,
				floorScanHeight: BookcasesProp.FloorScanHeight
			);
		}

		OnAirSignProp.Create(PropSort, OnAirSignProp.Studio, GridAnchor, LightMask);

		if (PlaceRoundTable)
		{
			RoundTableProp.Create(PropSort, Shadows, Shadows.DepthShadowMaterial, this, LightMask);
		}

		if (PlaceVern)
		{
			VernChairGroupProp.Build(PropSort, this, Shadows, LightMask);
		}
	}
}