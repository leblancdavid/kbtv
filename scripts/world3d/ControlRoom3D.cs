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
		player.SetRoomAnchor(GlobalPosition + PlayerStartPosition);
	}

	public void ShowRoom()
	{
		Visible = true;
	}

	public void HideRoom()
	{
		Visible = false;
	}

	public bool IsPlayerAtDoor(Vector3 playerPosition)
	{
		return Visible && playerPosition.X > GlobalPosition.X + 4.8f && Mathf.Abs(playerPosition.Z - GlobalPosition.Z) < 1.4f;
	}
}
