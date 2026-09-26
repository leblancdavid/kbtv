using Godot;

namespace KBTV.World3D;

public partial class Player3D : CharacterBody3D
{
	[Export] private float _speed = 4.5f;
	private bool _movementLocked;

	public override void _Ready()
	{
		AddToGroup("player");
		ApplyComicReadableMaterial();
	}

	private void ApplyComicReadableMaterial()
	{
		var visual = GetNodeOrNull<MeshInstance3D>("Visual");
		if (visual == null)
		{
			return;
		}

		var material = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.42f, 0.46f, 0.48f),
			EmissionEnabled = true,
			Emission = new Color(0.42f, 0.46f, 0.48f),
			EmissionEnergyMultiplier = 0.12f,
			Roughness = 0.82f
		};
		visual.MaterialOverride = material;
	}

	public void SetMovementLocked(bool locked)
	{
		_movementLocked = locked;
		if (locked)
		{
			Velocity = Vector3.Zero;
		}
		SetPhysicsProcess(!locked);
	}

	public void SetRoomAnchor(Vector3 roomAnchor)
	{
		GlobalPosition = roomAnchor;
	}

	public override void _PhysicsProcess(double delta)
	{
		var input = Vector3.Zero;
		if (Input.IsActionPressed("move_forward")) input.Z -= 1f;
		if (Input.IsActionPressed("move_back")) input.Z += 1f;
		if (Input.IsActionPressed("move_left")) input.X -= 1f;
		if (Input.IsActionPressed("move_right")) input.X += 1f;

		if (input != Vector3.Zero)
		{
			input = input.Normalized();
		}

		var basis = GlobalTransform.Basis;
		var move = (basis * input) * _speed;
		Velocity = new Vector3(move.X, Velocity.Y, move.Z);
		MoveAndSlide();
	}
}
