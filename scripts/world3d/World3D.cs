using Godot;

namespace KBTV.World3D;

public partial class World3D : Node3D
{
	[Export] public NodePath? PlayerPath { get; set; }
	[Export] public Vector3 CameraOffset { get; set; } = new(0f, 20f, 12f);
	[Export] public float CameraFollowSpeed { get; set; } = 8f;
	[Export] public Vector2 CameraXBounds { get; set; } = new(-4f, 4f);
	[Export] public Vector2 CameraZBounds { get; set; } = new(4f, 20f);

	private Camera3D _camera = null!;
	private Label? _status_label;
	private ControlRoom3D _control_room = null!;
	private StudioRoom3D _studio_room = null!;
	private Player3D? _player;
	private bool _player_at_connection;

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
			_player.SetRoomAnchor(_control_room.GlobalPosition + _control_room.PlayerStartPosition);
			UpdateCamera(0.0, true);
		}

		_control_room.ShowRoom();
		_studio_room.ShowRoom();
		UpdateStatusLabel("CONTROL ROOM");
	}

	public void SetPlayer(Player3D player)
	{
		_player = player;
		_player.SetRoomAnchor(_control_room.GlobalPosition + _control_room.PlayerStartPosition);
		UpdateCamera(0.0, true);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Input.IsActionJustPressed("interact") || !_player_at_connection)
		{
			return;
		}

		GD.Print("World3D: Player is at the control room / studio doorway");
	}

	public override void _Process(double delta)
	{
		_UpdatePlayerRoomState();
		UpdateCamera(delta);
	}


	private void _UpdatePlayerRoomState()
	{
		if (_player == null)
		{
			return;
		}

		var playerPosition = _player.GlobalPosition;
		_player_at_connection = _control_room.IsPlayerAtDoor(playerPosition) || _studio_room.IsPlayerAtDoor(playerPosition);

		if (_player_at_connection)
		{
			UpdateStatusLabel("DOORWAY");
		}
		else if (_studio_room.ContainsPlayer(playerPosition))
		{
			UpdateStatusLabel("STUDIO");
		}
		else if (_control_room.ContainsPlayer(playerPosition))
		{
			UpdateStatusLabel("CONTROL ROOM");
		}
	}

	private void UpdateStatusLabel(string roomName)
	{
		if (_status_label != null)
		{
			_status_label.Text = $"KBTV 3D BLOCKOUT | {roomName} | connected floorplan";
		}
	}

	private void UpdateCamera(double delta, bool immediate = false)
	{
		if (_player == null)
		{
			return;
		}

		var target = _player.GlobalPosition + CameraOffset;
		target.X = Mathf.Clamp(target.X, CameraXBounds.X, CameraXBounds.Y);
		target.Z = Mathf.Clamp(target.Z, CameraZBounds.X, CameraZBounds.Y);

		if (immediate)
		{
			_camera.GlobalPosition = target;
		}
		else
		{
			var weight = 1f - Mathf.Exp(-CameraFollowSpeed * (float)delta);
			_camera.GlobalPosition = _camera.GlobalPosition.Lerp(target, weight);
		}

		_camera.RotationDegrees = new Vector3(-68f, 0f, 0f);
	}
}
