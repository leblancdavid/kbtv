using Godot;

namespace KBTV.World3D;

public partial class ControlRoom3D : Node3D
{
	[Export] public Vector3 PlayerStartPosition = new(0f, 0f, 3.2f);
	private const float HalfWidth = 7f;
	private const float HalfDepth = 5f;
	private const float DoorHalfWidth = 2f;

	public override void _Ready()
	{
		Visible = true;
		CreateColliders();
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
		return Visible
			&& playerPosition.Z < GlobalPosition.Z - 3.8f
			&& playerPosition.Z > GlobalPosition.Z - HalfDepth - 0.5f
			&& Mathf.Abs(playerPosition.X - GlobalPosition.X) < DoorHalfWidth;
	}

	public bool ContainsPlayer(Vector3 playerPosition)
	{
		return Visible
			&& playerPosition.X >= GlobalPosition.X - HalfWidth
			&& playerPosition.X <= GlobalPosition.X + HalfWidth
			&& playerPosition.Z >= GlobalPosition.Z - HalfDepth
			&& playerPosition.Z <= GlobalPosition.Z + HalfDepth;
	}

	private void CreateColliders()
	{
		var root = new Node3D { Name = "GeneratedColliders" };
		AddChild(root);

		AddStaticBox(root, "FloorCollider", new Vector3(0f, -0.1f, 0f), new Vector3(14f, 0.2f, 10f));
		AddStaticBox(root, "NorthWallWestCollider", new Vector3(-4.5f, 0.55f, -5f), new Vector3(5f, 1.1f, 0.2f));
		AddStaticBox(root, "NorthWallEastCollider", new Vector3(4.5f, 0.55f, -5f), new Vector3(5f, 1.1f, 0.2f));
		AddStaticBox(root, "SouthWallCollider", new Vector3(0f, 0.55f, 5f), new Vector3(14f, 1.1f, 0.2f));
		AddStaticBox(root, "WestWallCollider", new Vector3(-6.6f, 0.55f, 0f), new Vector3(0.2f, 1.1f, 10f));
		AddStaticBox(root, "EastWallCollider", new Vector3(6.6f, 0.55f, 0f), new Vector3(0.2f, 1.1f, 10f));
		AddStaticBox(root, "DeskCollider", new Vector3(0f, 0.4f, 1.2f), new Vector3(2f, 0.8f, 1f));
	}

	private static void AddStaticBox(Node3D parent, string name, Vector3 position, Vector3 size)
	{
		var body = new StaticBody3D { Name = name, Position = position };
		var shape = new CollisionShape3D
		{
			Shape = new BoxShape3D { Size = size }
		};

		body.AddChild(shape);
		parent.AddChild(body);
	}
}
