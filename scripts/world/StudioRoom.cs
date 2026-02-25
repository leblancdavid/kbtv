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

	[ExportGroup("Monitor Light")]
	[Export] private bool EnableMonitorLight = true;
	[Export] private Color MonitorLightColor = new(0.2f, 0.8f, 1f);
	[Export] private float MonitorLightEnergy = 0.35f;

	[ExportGroup("Desk Lamp")]
	[Export] private bool EnableDeskLampLight = true;
	[Export] private Color DeskLampColor = new(1f, 0.9f, 0.6f);
	[Export] private float DeskLampEnergy = 0.3f;

	[ExportGroup("Props")]
	[Export] private bool PlaceStudioTable = true;
	[Export] private bool PlaceMonitorConsole = true;
	[Export] private bool PlaceSpeakerStands = true;
	[Export] private bool PlaceStorageCabinet = true;
	[Export] private bool PlaceChair = true;
	[Export] private bool PlaceCoffeeStation = true;
	[Export] private bool PlaceWallDecor = true;

	private WallSystem _wallSystem;
	private RoomLighting _lighting;
	private CastShadowSystem _shadows;
	private RoomDebug _debug;

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

		_lighting = new RoomLighting
		{
			EnableCeilingLight = EnableCeilingLight,
			CeilingLightColor = CeilingLightColor,
			CeilingLightEnergy = CeilingLightEnergy,
			CeilingLightRadius = CeilingLightRadius,
			EnableMonitorLight = EnableMonitorLight,
			MonitorLightColor = MonitorLightColor,
			MonitorLightEnergy = MonitorLightEnergy,
			EnableDeskLampLight = EnableDeskLampLight,
			DeskLampColor = DeskLampColor,
			DeskLampEnergy = DeskLampEnergy
		};
		AddChild(_lighting);

		_shadows = new CastShadowSystem
		{
			LightRadius = CeilingLightRadius
		};
		AddChild(_shadows);

		_debug = new RoomDebug { DebugEnabled = false };
		AddChild(_debug);

		_wallSystem.Initialize(this);
		_lighting.Initialize(this);

		var tablePosition = GridToWorld(new Vector2I(6, 2));
		_lighting.CreateLighting(tablePosition);

		_shadows.Initialize(this, _lighting.CeilingLight);
		_shadows.UpdateDepthShadowLightPosition();
		_debug.Initialize(this, _wallSystem, _shadows);

		_wallSystem.CreateWalls();
		_wallSystem.CreateWallColliders();

		SetupPlayer();
		CreateProps();

		OnRoomReady();
	}

	private void SetupPlayer()
	{
		Player = GetNode<Node2D>("PropSort/Player");
		if (Player != null)
		{
			var centerX = GridWidth / 2;
			var centerY = GridHeight / 2;
			Player.Position = GridToWorld(new Vector2I(centerX, centerY)) + new Vector2(0, 8);

			var playerSprite = Player.GetNode<Sprite2D>("Sprite2D");
			if (playerSprite != null && _shadows.DepthShadowMaterial != null)
			{
				playerSprite.Material = _shadows.DepthShadowMaterial;
			}

			if (playerSprite != null)
			{
				_shadows.CreateShadowForObject(Player, playerSprite.Texture);
			}
		}
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
		_lighting.Update(delta);
		_shadows.Update(delta);

		_debug.UpdatePlayerRect();
		_debug.UpdatePropRects();

		OnRoomProcess(delta);
	}
}
