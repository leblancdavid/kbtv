using Godot;

namespace KBTV.World3D;

public partial class ControlRoom3D : Node3D
{
	[Export] public Vector3 PlayerStartPosition = new(0f, 0f, 2.2f);
	private const float HalfWidth = 5f;
	private const float HalfDepth = 4f;
	private const float DoorHalfWidth = 1.4f;
	private const float DoorCenterX = -3.35f;
	private const float ChairPushRadius = 1.1f;
	private const float ChairMaxDisplacement = 0.8f;
	private const float ChairMoveSpeed = 6f;
	private MeshInstance3D? _officeChair;
	private Player3D? _player;
	private Vector3 _officeChairHome;

	public override void _Ready()
	{
		Visible = true;
		_officeChair = GetNodeOrNull<MeshInstance3D>("OfficeChair");
		_officeChairHome = _officeChair?.Position ?? Vector3.Zero;
		CreateColliders();
	}

	public override void _Process(double delta)
	{
		UpdateOfficeChair(delta);
	}

	public void SetPlayer(Player3D player)
	{
		_player = player;
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
			&& playerPosition.Z < GlobalPosition.Z - 2.8f
			&& playerPosition.Z > GlobalPosition.Z - HalfDepth - 0.5f
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
		AddStaticBox(root, "DeskCollider", new Vector3(0.6f, 0.4f, -3.55f), new Vector3(4.8f, 0.8f, 0.6f));
		AddStaticBox(root, "SpeakerLeftCollider", new Vector3(-2.75f, 0.45f, -3.55f), new Vector3(0.8f, 0.9f, 0.8f));
		AddStaticBox(root, "SpeakerRightCollider", new Vector3(3.75f, 0.45f, -3.55f), new Vector3(0.8f, 0.9f, 0.8f));
		AddStaticBox(root, "AudioCabinetCollider", new Vector3(4.45f, 0.55f, -2.3f), new Vector3(0.9f, 1.1f, 0.8f));
		AddStaticBox(root, "ShelfLeftCollider", new Vector3(-2.8f, 0.55f, 2.45f), new Vector3(1.2f, 1.1f, 0.8f));
		AddStaticBox(root, "ShelfCenterCollider", new Vector3(0f, 0.55f, 2.45f), new Vector3(1.2f, 1.1f, 0.8f));
		AddStaticBox(root, "ShelfRightCollider", new Vector3(2.8f, 0.55f, 2.45f), new Vector3(1.2f, 1.1f, 0.8f));
	}

	private void UpdateOfficeChair(double delta)
	{
		if (_officeChair == null)
		{
			return;
		}

		_player ??= GetParent()?.GetNodeOrNull<Player3D>("Player3D");
		var target = _officeChairHome;

		if (_player != null)
		{
			var playerLocal = ToLocal(_player.GlobalPosition);
			var away = new Vector3(_officeChairHome.X - playerLocal.X, 0f, _officeChairHome.Z - playerLocal.Z);
			var distance = away.Length();

			if (distance > 0.001f && distance < ChairPushRadius)
			{
				var strength = 1f - distance / ChairPushRadius;
				target += away.Normalized() * ChairMaxDisplacement * strength;
			}
		}

		var weight = 1f - Mathf.Exp(-ChairMoveSpeed * (float)delta);
		_officeChair.Position = _officeChair.Position.Lerp(target, weight);
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
