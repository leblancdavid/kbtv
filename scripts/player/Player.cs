using Godot;

public partial class Player : CharacterBody2D
{
    [Export] private float _speed = 150.0f;
    [Export] private float _spriteForwardOffset = 2.0f;

    private Sprite2D _sprite;

    public override void _Ready()
    {
        AddToGroup("player");
        YSortEnabled = true;

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite != null)
        {
            var size = _sprite.Texture?.GetSize() ?? Vector2.Zero;
            _sprite.Position = new Vector2(0, -(size.Y * 0.5f) + _spriteForwardOffset);
        }
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
