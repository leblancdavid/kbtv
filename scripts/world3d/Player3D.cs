using Godot;

namespace KBTV.World3D;

public partial class Player3D : CharacterBody3D
{
	[Export] private float _speed = 4.5f;

	public override void _Ready()
	{
	}

	public void SetRoomAnchor(Vector3 roomAnchor)
	{
		GlobalPosition = roomAnchor;
	}

	public override void _PhysicsProcess(double delta)
	{
		var input = Vector3.Zero;
		if (Input.IsActionPressed("ui_up")) input.Z -= 1f;
		if (Input.IsActionPressed("ui_down")) input.Z += 1f;
		if (Input.IsActionPressed("ui_left")) input.X -= 1f;
		if (Input.IsActionPressed("ui_right")) input.X += 1f;

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
