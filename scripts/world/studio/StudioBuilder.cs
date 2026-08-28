using Godot;
using KBTV.Data;

public partial class StudioBuilder : IRoomBuilder
{
	[ExportGroup("Grid Settings")]
	[Export] public Vector2 GridAnchor = new(0, 776);
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
	[Export] private Color CeilingLightColor = new(1f, 0.95f, 0.9f);
	[Export] private float CeilingLightEnergy = 1.1f;
	[Export] private float CeilingLightRadius = 900f;
	[Export] private bool CeilingLightShadows = true;



	[ExportGroup("Ambient")]
	[Export] private Color AmbientColor = new(0.18f, 0.19f, 0.22f);

	[ExportGroup("Smoke")]
	[Export] private bool EnableSmoke = true;
	[Export] private float SmokeMaxParticles = 100f;
	[Export] private float SmokeDecayTime = 60f;

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
	private readonly StudioSmoke _smoke = new();
	private CharacterBody2D _player;
	private float _flickerTime;

	/// <summary>Single source of truth for where everything in this room is placed.</summary>
	private readonly StudioLayout _layout = new();

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
		CreateSmoke(world);
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

		_shadows = new CastShadowSystem { LightRadius = CeilingLightRadius, GroupName = "shadow_pivots_studio" };
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

		_shadows.Initialize(_section, _ceilingLight);
		// The control-room fix keeps the depth-shadow light origin in sync; apply here too so
		// the studio's props (which use the same shader) render at full brightness.
		_shadows.CallDeferred(nameof(CastShadowSystem.UpdateDepthShadowLightPosition));
		_flickerTime = 0f;
	}

	private void CreateSmoke(WorldRoom world)
	{
		if (!EnableSmoke)
		{
			return;
		}

		var smokePosition = GridToWorld(new Vector2I(_layout.SmokeColumn, GridHeight - _layout.SmokeRowsFromBottom));
		_smoke.Initialize(_propSort, smokePosition, (int)SmokeMaxParticles, SmokeDecayTime, LightMask);
	}

	private void InitializeDebug()
	{
		_debug.Initialize(_section, _wallSystem, _shadows, _ceilingLight, null, null);
	}

	private void CreateProps()
	{
		foreach (var placement in BookcasesProp.Placements)
		{
			PropBuilder.CreatePropAutoCollider(
				_propSort, BookcasesProp.TexturePath, placement.Cell, placement.Offset,
				_shadows, _shadows.DepthShadowMaterial, _section, LightMask,
				floorScanHeight: BookcasesProp.FloorScanHeight
			);
		}

		OnAirSignProp.Create(_propSort, GridAnchor + _layout.OnAirSignFromAnchor, _layout.OnAirSignScale,
			_layout.OnAirSignLightColor, _layout.OnAirSignLightEnergy, _layout.OnAirSignLightRadius, LightMask);

		if (PlaceRoundTable)
		{
			RoundTableProp.Create(_propSort, _shadows, _shadows.DepthShadowMaterial, _section, LightMask);
		}

		if (PlaceVern)
		{
			VernChairGroupProp.Build(_propSort, _section, _shadows, LightMask);
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

	public void Update(WorldRoom world, double delta, VernStats? vernStats)
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

		UpdateSmoke(vernStats);

		_shadows.Update(delta);
		_debug.UpdatePlayerRect();
		_debug.UpdatePropRects();
	}

	private void UpdateSmoke(VernStats? vernStats)
	{
		_smoke.Update(vernStats);
	}

	public void ToggleDebug()
	{
		_debug.Toggle();
	}
}
