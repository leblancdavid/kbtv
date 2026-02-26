using Godot;

public partial class World : Node2D
{
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
		_controlRoomMinY = ControlRoom.Position.Y - TileSize;
		_controlRoomMaxY = ControlRoom.Position.Y + ControlRoom.GridHeight * TileSize;

		_studioRoomMinY = StudioRoom.Position.Y - TileSize;
		_studioRoomMaxY = StudioRoom.Position.Y + StudioRoom.GridHeight * TileSize;
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

		GD.Print($"World: Transitioned from Studio to Control Room. Player now at y={_player.GlobalPosition.Y}");
	}
}
