using Godot;

namespace KBTV.World3D;

public partial class World3D : Node3D
{
	[Export] public NodePath? PlayerPath { get; set; }

	private ControlRoom3D _control_room = null!;
	private StudioRoom3D _studio_room = null!;
	private Player3D? _player;
	private bool _showing_control_room = true;

	public override void _Ready()
	{
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
		_control_room.SetPlayer(player);
		_studio_room.SetPlayer(player);
		ShowControlRoom();
	}

	public void ShowControlRoom()
	{
		_showing_control_room = true;
		_control_room.ShowRoom();
		_studio_room.HideRoom();

		if (_player != null)
		{
			_player.SetRoomAnchor(_control_room.PlayerStartPosition);
		}
	}

	public void ShowStudioRoom()
	{
		_showing_control_room = false;
		_control_room.HideRoom();
		_studio_room.ShowRoom();

		if (_player != null)
		{
			_player.SetRoomAnchor(_studio_room.PlayerStartPosition);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_select"))
		{
			if (_showing_control_room)
			{
				ShowStudioRoom();
			}
			else
			{
				ShowControlRoom();
			}
		}
	}
}
