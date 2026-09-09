using Godot;

namespace KBTV.World3D;

public partial class World3D : Node3D
{
	[Export] public NodePath? PlayerPath { get; set; }

	private Camera3D _camera = null!;
	private Label? _status_label;
	private ControlRoom3D _control_room = null!;
	private StudioRoom3D _studio_room = null!;
	private Player3D? _player;
	private bool _player_in_control_door;
	private bool _player_in_studio_door;

	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("WorldCamera");
		_status_label = GetNodeOrNull<Label>("StatusLayer/StatusPanel/StatusLabel");
		_control_room = GetNode<ControlRoom3D>("ControlRoom3D");
		_studio_room = GetNode<StudioRoom3D>("StudioRoom3D");

		if (PlayerPath != null && !PlayerPath.IsEmpty)
		{
			_player = GetNodeOrNull<Player3D>(PlayerPath);
		}

		if (_player == null)
		{
			_player = GetNodeOrNull<Player3D>("Player3D");
		}

		if (_player != null)
		{
			_control_room.SetPlayer(_player);
			_studio_room.SetPlayer(_player);
		}

		ShowControlRoom();
	}

	public void SetPlayer(Player3D player)
	{
		_player = player;
		ShowControlRoom();
	}

	public void ShowControlRoom()
	{
		_control_room.ShowRoom();
		_studio_room.HideRoom();
		SetCameraForRoom(_control_room.Position.X);
		UpdateStatusLabel("CONTROL ROOM");
		GD.Print("World3D: Showing control room");

		if (_player != null)
		{
			_control_room.SetPlayer(_player);
		}
	}

	public void ShowStudioRoom()
	{
		_control_room.HideRoom();
		_studio_room.ShowRoom();
		SetCameraForRoom(_studio_room.Position.X);
		UpdateStatusLabel("STUDIO");
		GD.Print("World3D: Showing studio room");

		if (_player != null)
		{
			_studio_room.SetPlayer(_player);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Input.IsActionJustPressed("interact"))
		{
			return;
		}

		if (_player_in_control_door)
		{
			ShowStudioRoom();
		}
		else if (_player_in_studio_door)
		{
			ShowControlRoom();
		}
	}

	public override void _Process(double delta)
	{
		_UpdatePlayerRoomState();
	}


	private void _UpdatePlayerRoomState()
	{
		if (_player == null)
		{
			return;
		}

		var playerPosition = _player.GlobalPosition;
		_player_in_control_door = _control_room.IsPlayerAtDoor(playerPosition);
		_player_in_studio_door = _studio_room.IsPlayerAtDoor(playerPosition);
	}

	private void UpdateStatusLabel(string roomName)
	{
		if (_status_label != null)
		{
			_status_label.Text = $"KBTV 3D BLOCKOUT | {roomName} | interact at doorway";
		}
	}

	private void SetCameraForRoom(float roomX)
	{
		_camera.Position = new Vector3(roomX, 20f, 12f);
		_camera.RotationDegrees = new Vector3(-68f, 0f, 0f);
	}
}
