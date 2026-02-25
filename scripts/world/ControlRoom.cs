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

	[ExportGroup("Monitor Light")]
	[Export] private bool EnableMonitorLight = true;
	[Export] private Color MonitorLightColor = new(0f, 1f, 0.27f);
	[Export] private float MonitorLightEnergy = 0.3f;

	[ExportGroup("Desk Lamp")]
	[Export] private bool EnableDeskLampLight = true;
	[Export] private Color DeskLampColor = new(1f, 0.67f, 0.27f);
	[Export] private float DeskLampEnergy = 0.25f;

	[ExportGroup("Props")]
	[Export] private bool PlaceSpeakerStands = true;
	[Export] private bool PlaceTableGroup = true;
	[Export] private bool PlaceAudioCabinet = true;
	[Export] private bool PlaceStorageShelves = true;
	[Export] private bool PlaceChair = true;

	private WallSystem _wallSystem;
	private RoomLighting _lighting;
	private CastShadowSystem _shadows;
	private RoomDebug _debug;

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

		var tablePosition = GridToWorld(new Vector2I(6, 1));
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
		_lighting.Update(delta);
		_shadows.Update(delta);

		_debug.UpdatePlayerRect();
		_debug.UpdatePropRects();

		OnRoomProcess(delta);
	}
}
