using Godot;

namespace KBTV.World3D;

public partial class StudioRoom3D : Node3D
{
	[Export] public Vector3 PlayerStartPosition = new(0f, 0.5f, -3f);
	[ExportGroup("Smoke")]
	[Export] public bool EnableSmoke = true;
	[Export] public int PuffBurstCount = 5;
	[Export] public float PuffInterval = 4.0f;
	[Export] public float AmbientSmokeOpacity = 0.16f;
	[Export] public float PuffSmokeOpacity = 0.16f;
	[Export] public float SmokeDriftSpeed = 0.45f;
	[Export] public Vector3 SmokePuffOrigin = new(-0.85f, 1.25f, 0.35f);
	[Export] public Vector3 SmokeRoomHalfExtents = new(4.4f, 1.7f, 3.4f);

	private const float HalfWidth = 5f;
	private const float HalfDepth = 4f;
	private const float DoorHalfWidth = 1.4f;
	private const float DoorCenterX = -4.15f;
	private StudioSmoke3D? _smoke;

	public override void _Ready()
	{
		Visible = true;
		CreateColliders();
		CreateSmoke();
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
			&& Mathf.Abs(playerPosition.X - (GlobalPosition.X + DoorCenterX)) < DoorHalfWidth;
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
		AddStaticBox(root, "RoundTableCollider", new Vector3(0.65f, 0.35f, 1.15f), new Vector3(3.2f, 0.7f, 0.9f));
		AddStaticBox(root, "VernChairCollider", new Vector3(-0.85f, 0.35f, 0.55f), new Vector3(0.9f, 0.7f, 0.7f));
		AddStaticBox(root, "GuestChairCollider", new Vector3(1.05f, 0.35f, 0.7f), new Vector3(0.75f, 0.7f, 0.65f));
		AddStaticBox(root, "BookcaseLeftCollider", new Vector3(-2.6f, 0.7f, -3.1f), new Vector3(1.6f, 1.4f, 0.7f));
		AddStaticBox(root, "BookcaseRightCollider", new Vector3(2.6f, 0.7f, -3.1f), new Vector3(1.6f, 1.4f, 0.7f));
	}

	private void CreateSmoke()
	{
		if (!EnableSmoke)
		{
			return;
		}

		_smoke = new StudioSmoke3D
		{
			PuffBurstCount = PuffBurstCount,
			PuffInterval = PuffInterval,
			AmbientOpacity = AmbientSmokeOpacity,
			PuffOpacity = PuffSmokeOpacity,
			DriftSpeed = SmokeDriftSpeed,
			PuffOrigin = SmokePuffOrigin,
			RoomHalfExtents = SmokeRoomHalfExtents
		};
		AddChild(_smoke);
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
