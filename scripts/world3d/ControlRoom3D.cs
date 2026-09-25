using Godot;

namespace KBTV.World3D;

public partial class ControlRoom3D : Node3D
{
	[Export] public Vector3 PlayerStartPosition = new(0f, 0f, 2.2f);
	private const float HalfWidth = 5f;
	private const float HalfDepth = 4f;
	private const float DoorHalfWidth = 1.4f;
	private const float DoorCenterX = -4.15f;
	private const float ChairPushRadius = 1.1f;
	private const float ChairMaxDisplacement = 0.8f;
	private const float ChairMoveSpeed = 6f;
	private Node3D? _officeChair;
	private Player3D? _player;
	private Vector3 _officeChairHome;

	public ComputerTerminal3D ComputerTerminal { get; private set; } = null!;
	public Node3D ComputerGlb { get; private set; } = null!;
	public Soundboard3D SoundBoard3D { get; private set; } = null!;

	public override void _Ready()
	{
		Visible = true;
		_officeChair = GetNodeOrNull<Node3D>("OfficeChair");
		_officeChairHome = _officeChair?.Position ?? Vector3.Zero;
		CreateColliders();
		ComputerGlb = GetNode<Node3D>("Computer");
		ComputerTerminal3D.ConfigureGlassMaterial(ComputerGlb);
		ComputerTerminal = new ComputerTerminal3D { Name = "ComputerTerminal" };
		AddChild(ComputerTerminal);
		AlignComputerTerminal();

		SoundBoard3D = new Soundboard3D { Name = "Soundboard3D" };
		var boardNode = GetNodeOrNull<Node3D>("SoundBoard");
		if (boardNode != null)
		{
			// Parented at identity so part transforms share the board's local frame.
			boardNode.AddChild(SoundBoard3D);
			SoundBoard3D.AttachBoard(boardNode);
		}

		DisableShadowsInTree(GetNodeOrNull<Node3D>("SpeakerRight"));
	}

	public void SetComputerCollidersEnabled(bool enabled)
	{
		ToggleColliders(ComputerGlb, enabled);
	}

	/// <summary>Keeps the invisible projection plane centred on and flush with the
	/// CRT's phosphor surface. The helper is flipped 180° against the model's own
	/// rotation so its plane front always faces the player while matching the
	/// model's yaw (so the projected UI sits flat on the real screen).</summary>
	private void AlignComputerTerminal()
	{
		var glb = ComputerGlb.GlobalTransform;
		var planeCenterWorld = glb * ComputerTerminal3D.ModelScreenCenterOffset;
		var planeBasis = new Basis(Vector3.Up, Mathf.Pi) * glb.Basis;
		var planeLocalOffset = new Vector3(
			0f, ComputerTerminal3D.ScreenCenterY, ComputerTerminal3D.ScreenZOffset);
		ComputerTerminal.GlobalTransform = new Transform3D(
			planeBasis,
			planeCenterWorld - planeBasis * planeLocalOffset);
	}

	private static void ToggleColliders(Node node, bool enabled)
	{
		foreach (var child in node.GetChildren())
		{
			if (child is CollisionObject3D collisionObject)
			{
				if (enabled)
				{
					var layer = (uint)(long)child.GetMeta("_saved_collision_layer", 0L);
					var mask = (uint)(long)child.GetMeta("_saved_collision_mask", 0L);
					collisionObject.CollisionLayer = layer;
					collisionObject.CollisionMask = mask;
				}
				else if (collisionObject.CollisionLayer != 0u)
				{
					child.SetMeta("_saved_collision_layer", (long)collisionObject.CollisionLayer);
					child.SetMeta("_saved_collision_mask", (long)collisionObject.CollisionMask);
					collisionObject.CollisionLayer = 0u;
					collisionObject.CollisionMask = 0u;
				}
			}
			ToggleColliders(child, enabled);
		}
	}

	private static void DisableShadowsInTree(Node? node)
	{
		if (node == null)
		{
			return;
		}

		if (node is GeometryInstance3D geometry)
		{
			geometry.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		}

		foreach (var child in node.GetChildren())
		{
			DisableShadowsInTree(child);
		}
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
		AddStaticBox(root, "SpeakerLeftCollider", new Vector3(-2.15f, 0.995f, -3.5f), new Vector3(0.55f, 1.79f, 0.435f));
		AddStaticBox(root, "SpeakerRightCollider", new Vector3(3.35f, 0.995f, -3.5f), new Vector3(0.55f, 1.79f, 0.435f));
		AddStaticBox(root, "AudioCabinetCollider", new Vector3(4.45f, 0.75f, -3.5f), new Vector3(1.05f, 1.5f, 0.975f));
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
