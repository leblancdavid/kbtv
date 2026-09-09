using Godot;

namespace KBTV.World3D;

public partial class ControlRoom3D : Node3D
{
	[Export] public Vector3 PlayerStartPosition = new(0f, 0.5f, 3f);

	public override void _Ready()
	{
		Visible = true;
	}

	public void SetPlayer(Player3D player)
	{
		player.SetRoomAnchor(PlayerStartPosition);
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
