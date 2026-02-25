using Godot;

public partial class World : Node2D
{
	[Export] public float DoorRow = 3f;
	[Export] public float TileSize = 16f;

	public ControlRoom ControlRoom { get; private set; } = null!;
	public StudioRoom StudioRoom { get; private set; } = null!;
	public Node2D? CurrentRoom { get; private set; }
	public Node2D? PreviousRoom { get; private set; }

	private Node2D _player = null!;

	private float _controlRoomMinY;
	private float _controlRoomMaxY;
	private float _studioRoomMinY;
	private float _studioRoomMaxY;

	private const float DOOR_TRANSITION_ZONE = 24f;

	public override void _Ready()
	{
		ControlRoom = GetNode<ControlRoom>("ControlRoom");
		StudioRoom = GetNode<StudioRoom>("StudioRoom");

		CalculateRoomBounds();

		CurrentRoom = ControlRoom;
		PreviousRoom = null;

		StudioRoom.Visible = false;
		StudioRoom.ProcessMode = ProcessModeEnum.Disabled;

		GetTree().CallDeferred("call_group", "player", "set_world", this);
	}

	private void CalculateRoomBounds()
	{
		var crHeight = ControlRoom.GridHeight * TileSize;
		_controlRoomMinY = -TileSize;
		_controlRoomMaxY = crHeight + TileSize;

		var srHeight = StudioRoom.GridHeight * TileSize;
		_studioRoomMinY = -TileSize + StudioRoom.Position.Y;
		_studioRoomMaxY = srHeight + TileSize + StudioRoom.Position.Y;
	}

	public void RegisterPlayer(Node2D player)
	{
		_player = player;
	}

	public override void _Process(double delta)
	{
		if (_player == null)
			return;

		var playerY = _player.GlobalPosition.Y;

		if (CurrentRoom == ControlRoom)
		{
			var doorY = _controlRoomMaxY - (DoorRow * TileSize) - TileSize;
			if (playerY < doorY - DOOR_TRANSITION_ZONE)
			{
				TransitionToStudio();
			}
		}
		else if (CurrentRoom == StudioRoom)
		{
			var doorY = _studioRoomMaxY - (DoorRow * TileSize) - TileSize;
			if (playerY > doorY + DOOR_TRANSITION_ZONE)
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

		var doorWorldY = _controlRoomMaxY - (DoorRow * TileSize) - TileSize;
		var studioDoorWorldY = _studioRoomMaxY - (DoorRow * TileSize) - TileSize;
		var offset = studioDoorWorldY - doorWorldY;
		_player.GlobalPosition = new Vector2(_player.GlobalPosition.X, _player.GlobalPosition.Y + offset);

		GD.Print($"World: Transitioned from Control Room to Studio. Player now at y={_player.GlobalPosition.Y}");
	}

	private void TransitionToControlRoom()
	{
		PreviousRoom = CurrentRoom;
		CurrentRoom = ControlRoom;

		StudioRoom.Visible = false;
		StudioRoom.ProcessMode = ProcessModeEnum.Disabled;

		ControlRoom.Visible = true;
		ControlRoom.ProcessMode = ProcessModeEnum.Inherit;

		var studioDoorWorldY = _studioRoomMaxY - (DoorRow * TileSize) - TileSize;
		var controlDoorWorldY = _controlRoomMaxY - (DoorRow * TileSize) - TileSize;
		var offset = controlDoorWorldY - studioDoorWorldY;
		_player.GlobalPosition = new Vector2(_player.GlobalPosition.X, _player.GlobalPosition.Y + offset);

		GD.Print($"World: Transitioned from Studio to Control Room. Player now at y={_player.GlobalPosition.Y}");
	}
}
