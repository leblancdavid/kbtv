using Godot;

public partial class World : Node2D
{
	[Export] public float TileSize = 16f;

	public ControlRoom ControlRoom { get; private set; } = null!;
	public StudioRoom StudioRoom { get; private set; } = null!;
	public Player Player { get; private set; } = null!;
	public Node2D? CurrentRoom { get; private set; }
	public Node2D? PreviousRoom { get; private set; }

	private float _controlRoomMinY;
	private float _controlRoomMaxY;
	private float _studioRoomMinY;
	private float _studioRoomMaxY;

	private const float DOOR_TRANSITION_ZONE = 24f;

	public override void _Ready()
	{
		Player = GetNode<Player>("Player");
		ControlRoom = GetNode<ControlRoom>("ControlRoom");
		StudioRoom = GetNode<StudioRoom>("StudioRoom");

		Player.SetWorld(this);
		ControlRoom.SetPlayer(Player);
		StudioRoom.SetPlayer(Player);

		var playerSprite = Player.GetNode<Sprite2D>("Sprite2D");
		if (playerSprite != null)
		{
			ControlRoom.Shadows.CreateShadowForObject(Player, playerSprite.Texture);
		}

		CalculateRoomBounds();

		CurrentRoom = ControlRoom;
		PreviousRoom = null;

		StudioRoom.Visible = false;
		StudioRoom.ProcessMode = ProcessModeEnum.Disabled;
	}

	private void CalculateRoomBounds()
	{
		_controlRoomMinY = ControlRoom.Position.Y - TileSize;
		_controlRoomMaxY = ControlRoom.Position.Y + ControlRoom.GridHeight * TileSize;

		_studioRoomMinY = StudioRoom.Position.Y - TileSize;
		_studioRoomMaxY = StudioRoom.Position.Y + StudioRoom.GridHeight * TileSize;
	}

	public override void _Process(double delta)
	{
		if (Player == null)
			return;

		var playerY = Player.GlobalPosition.Y;

		if (CurrentRoom == ControlRoom)
		{
			if (playerY < _controlRoomMinY - DOOR_TRANSITION_ZONE)
			{
				TransitionToStudio();
			}
		}
		else if (CurrentRoom == StudioRoom)
		{
			if (playerY > _studioRoomMaxY + DOOR_TRANSITION_ZONE)
			{
				TransitionToControlRoom();
			}
		}
	}

	private void TransitionToStudio()
	{
		PreviousRoom = CurrentRoom;
		CurrentRoom = StudioRoom;

		ControlRoom.Visible = false;
		ControlRoom.ProcessMode = ProcessModeEnum.Disabled;

		StudioRoom.Visible = true;
		StudioRoom.ProcessMode = ProcessModeEnum.Inherit;

		GD.Print($"World: Transitioned from Control Room to Studio. Player now at y={Player.GlobalPosition.Y}");
	}

	private void TransitionToControlRoom()
	{
		PreviousRoom = CurrentRoom;
		CurrentRoom = ControlRoom;

		StudioRoom.Visible = false;
		StudioRoom.ProcessMode = ProcessModeEnum.Disabled;

		ControlRoom.Visible = true;
		ControlRoom.ProcessMode = ProcessModeEnum.Inherit;

		GD.Print($"World: Transitioned from Studio to Control Room. Player now at y={Player.GlobalPosition.Y}");
	}
}
