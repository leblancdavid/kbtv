using Godot;

namespace KBTV.World3D;

public partial class World3D : Node3D
{
	[Export] public NodePath? PlayerPath { get; set; }

	private ControlRoom3D _control_room = null!;
	private StudioRoom3D _studio_room = null!;
	private Player3D? _player;

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
	}

	public void SetPlayer(Player3D player)
	{
		_player = player;
		_control_room.SetPlayer(player);
		_studio_room.SetPlayer(player);
	}

	public void ShowControlRoom()
	{
		_control_room.ShowRoom();
		_studio_room.HideRoom();
	}

	public void ShowStudioRoom()
	{
		_control_room.HideRoom();
		_studio_room.ShowRoom();
	}
}
