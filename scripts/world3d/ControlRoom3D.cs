using Godot;

namespace KBTV.World3D;

public partial class ControlRoom3D : Node3D
{
	[Export] public Vector3 PlayerStartPosition = new(0f, 0.5f, 3f);

	private Player3D? _player;

	public override void _Ready()
	{
		Visible = true;
	}

	public void SetPlayer(Player3D player)
	{
		_player = player;
		_player.SetRoomAnchor(PlayerStartPosition);
	}

	public void ShowRoom()
	{
		Visible = true;
	}

	public void HideRoom()
	{
		Visible = false;
	}
}
