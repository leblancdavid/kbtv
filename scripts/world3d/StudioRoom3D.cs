using Godot;

namespace KBTV.World3D;

public partial class StudioRoom3D : Node3D
{
	[Export] public Vector3 PlayerStartPosition = new(0f, 0.5f, -3f);
	private const float HalfWidth = 5f;
	private const float HalfDepth = 4f;
	private const float DoorHalfWidth = 1.4f;

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
			&& playerPosition.Z > GlobalPosition.Z + 2.8f
			&& playerPosition.Z < GlobalPosition.Z + HalfDepth + 0.5f
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

		AddStaticBox(root, "FloorCollider", new Vector3(0f, -0.1f, 0f), new Vector3(10f, 0.2f, 8f));
		AddStaticBox(root, "NorthWallCollider", new Vector3(0f, 0.55f, -4f), new Vector3(10f, 1.1f, 0.2f));
		AddStaticBox(root, "WestWallCollider", new Vector3(-4.9f, 0.55f, 0f), new Vector3(0.2f, 1.1f, 8f));
		AddStaticBox(root, "EastWallNorthCollider", new Vector3(4.9f, 0.55f, -2.9f), new Vector3(0.2f, 1.1f, 2.2f));
		AddStaticBox(root, "EastWallSouthCollider", new Vector3(4.9f, 0.55f, 2.9f), new Vector3(0.2f, 1.1f, 2.2f));
		AddStaticBox(root, "RoundTableCollider", new Vector3(0f, 0.4f, 0.3f), new Vector3(1.6f, 0.8f, 0.8f));
		AddStaticBox(root, "StageCollider", new Vector3(0f, 0.25f, -2.5f), new Vector3(3f, 0.5f, 1.8f));
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
