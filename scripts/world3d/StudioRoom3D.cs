using Godot;

namespace KBTV.World3D;

public partial class StudioRoom3D : Node3D
{
	[Export] public Vector3 PlayerStartPosition = new(0f, 0.5f, -3f);
	[ExportGroup("Smoke")]
	[Export] public bool EnableSmoke = true;
	[Export] public int PuffBurstCount = 5;
	[Export] public float PuffInterval = 4.0f;
	[Export] public float AmbientSmokeOpacity = 0.38f;
	[Export] public float PuffSmokeOpacity = 0.16f;
	[Export] public float DoorLeakSmokeOpacity = 0.045f;
	[Export] public float DoorLeakInterval = 0.32f;
	[Export] public float FogMotionSpeed = 0.55f;
	[Export] public float FogMotionStrength = 0.22f;
	[Export] public float SmokeDriftSpeed = 0.45f;
	[Export] public Vector3 SmokePuffOrigin = new(0.58f, 1.4f, 0.11f);
	[Export] public Vector3 SmokeRoomHalfExtents = new(5f, 1.15f, 4f);

	private const float HalfWidth = 5f;
	private const float HalfDepth = 4f;
	private const float DoorHalfWidth = 1.4f;
	private const float DoorCenterX = -4.15f;
	private const float FloorTileWorldSize = 0.5f;
	private StudioSmoke3D? _smoke;

	public override void _Ready()
	{
		Visible = true;
		ApplyTexturedRoomSurfaces();
		CreateColliders();
		CreateSmoke();
	}

	private void ApplyTexturedRoomSurfaces()
	{
		var floor = GetNodeOrNull<MeshInstance3D>("Floor");
		if (floor != null)
		{
			floor.Position = new Vector3(0f, 0.01f, 0f);
			floor.Mesh = StationFloorMaterials3D.MakeTiledFloorMesh(HalfWidth * 2f, HalfDepth * 2f, FloorTileWorldSize);
			floor.MaterialOverride = StationFloorMaterials3D.MakeStudioCarpet();
			floor.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		}
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

	public void SetDoorSmokeLeakActive(string doorKey, Vector3 globalOrigin, Vector3 globalDirection, bool active)
	{
		_smoke?.SetDoorLeakActive(doorKey, ToLocal(globalOrigin), globalDirection, active);
	}

	private void CreateColliders()
	{
		var root = new Node3D { Name = "GeneratedColliders" };
		AddChild(root);

		AddStaticBox(root, "FloorCollider", new Vector3(0f, -0.1f, 0f), new Vector3(10f, 0.2f, 8f));
		AddStaticBox(root, "StudioTableCollider", new Vector3(0.55f, 0.45f, 0.98f), new Vector3(4.01f, 0.9f, 1.62f));
		AddStaticBox(root, "VernChairCollider", new Vector3(0.58f, 0.35f, -0.14f), new Vector3(0.9f, 0.7f, 0.7f));
		AddStaticBox(root, "GuestChairCollider", new Vector3(1.05f, 0.35f, 0.7f), new Vector3(0.75f, 0.7f, 0.65f));
		AddStaticBox(root, "BookcaseLeftCollider", new Vector3(-2.45f, 1.0f, -3.5f), new Vector3(1.85f, 1.9f, 0.9f));
		AddStaticBox(root, "BookcaseRightCollider", new Vector3(2.45f, 1.0f, -3.5f), new Vector3(1.85f, 1.9f, 0.9f));
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
			DoorLeakOpacity = DoorLeakSmokeOpacity,
			DoorLeakInterval = DoorLeakInterval,
			FogMotionSpeed = FogMotionSpeed,
			FogMotionStrength = FogMotionStrength,
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
