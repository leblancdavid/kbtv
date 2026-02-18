using Godot;

public partial class Player : CharacterBody2D
{
    [Export] private float _speed = 150.0f;

    public override void _Ready()
    {
        AddToGroup("player");
    }

    public override void _PhysicsProcess(double delta)
    {
        var velocity = Vector2.Zero;

        if (Input.IsActionPressed("ui_up"))
            velocity.Y = -1;
        if (Input.IsActionPressed("ui_down"))
            velocity.Y = 1;
        if (Input.IsActionPressed("ui_left"))
            velocity.X = -1;
        if (Input.IsActionPressed("ui_right"))
            velocity.X = 1;

        if (velocity != Vector2.Zero)
            velocity = velocity.Normalized();

        Velocity = velocity * _speed;
        MoveAndSlide();
    }
}
