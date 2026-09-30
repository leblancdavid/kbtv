using Godot;

namespace KBTV.World3D;

public partial class Player3D : CharacterBody3D
{
	[Export] private float _speed = 4.5f;
	[Export] private float _runSpeed = 7f;
	[Export] private float _acceleration = 16f;
	[Export] private float _deceleration = 20f;
	[Export] private float _turnSpeed = 12f;
	private bool _movementLocked;
	private Node3D? _visual;
	private AnimationPlayer? _animationPlayer;
	private string _currentAnimation = "";

	public override void _Ready()
	{
		AddToGroup("player");
		_visual = GetNodeOrNull<Node3D>("Visual");
		_animationPlayer = GetNodeOrNull<AnimationPlayer>("Visual/AnimationPlayer");
		if (_animationPlayer == null)
		{
			GD.PushError("Player3D: missing locomotion AnimationPlayer.");
			return;
		}
		var library = new AnimationLibrary();
		foreach (var name in new[] { "idle_breathing", "walk", "run" })
		{
			var clip = GD.Load<Animation>($"res://assets/models3d/characters/player/animations/{name}.tres");
			if (clip == null)
			{
				GD.PushError($"Player3D: missing animation {name}.");
				return;
			}
			library.AddAnimation(name, clip);
		}
		_animationPlayer.AddAnimationLibrary("", library);
		PlayAnimation("idle_breathing");
	}

	public void SetMovementLocked(bool locked)
	{
		_movementLocked = locked;
		if (locked)
		{
			Velocity = Vector3.Zero;
			PlayAnimation("idle_breathing");
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

		// Movement stays in the room's world axes; turning the Visual must not
		// rotate either input or the collision capsule.
		var running = input != Vector3.Zero && Input.IsActionPressed("run");
		var move = input * (running ? _runSpeed : _speed);
		var horizontal = new Vector2(Velocity.X, Velocity.Z).MoveToward(
			new Vector2(move.X, move.Z),
			(input == Vector3.Zero ? _deceleration : _acceleration) * (float)delta);
		Velocity = new Vector3(horizontal.X, Velocity.Y, horizontal.Y);
		MoveAndSlide();

		var actual = new Vector2(Velocity.X, Velocity.Z);
		if (actual.LengthSquared() > 0.01f)
		{
			if (_visual != null)
			{
				var target = Mathf.Atan2(actual.X, actual.Y); // Player GLB faces +Z.
				_visual.Rotation = new Vector3(0f, Mathf.LerpAngle(_visual.Rotation.Y,
					target, 1f - Mathf.Exp(-_turnSpeed * (float)delta)), 0f);
			}
			PlayAnimation(running ? "run" : "walk");
		}
		else
		{
			PlayAnimation("idle_breathing");
		}
	}

	private void PlayAnimation(string name)
	{
		if (_animationPlayer == null || _currentAnimation == name || !_animationPlayer.HasAnimation(name))
			return;
		_animationPlayer.SpeedScale = name == "idle_breathing" ? 1f : 1.45f;
		_animationPlayer.Play(name, _currentAnimation == "" ? 0d : 0.22d);
		_currentAnimation = name;
	}
}
