using Godot;

namespace KBTV.World3D;

public partial class Player3D : CharacterBody3D
{
	[Export] private float _speed = 4.5f;
	[Export] private float _mouse_sensitivity = 0.006f;

	private Camera3D? _camera;
	private Vector3 _room_anchor;
	private Vector2 _look_delta;

	public override void _Ready()
	{
		_camera = GetNodeOrNull<Camera3D>("Camera3D");
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public void SetRoomAnchor(Vector3 roomAnchor)
	{
		_room_anchor = roomAnchor;
		GlobalPosition = roomAnchor;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion motion)
		{
			_look_delta = motion.Relative;
			RotateY(-_look_delta.X * _mouse_sensitivity);
			if (_camera != null)
			{
				_camera.RotateX(-_look_delta.Y * _mouse_sensitivity);
				var rotation = _camera.Rotation;
				rotation.X = Mathf.Clamp(rotation.X, -1.2f, 1.2f);
				_camera.Rotation = rotation;
			}
		}
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
