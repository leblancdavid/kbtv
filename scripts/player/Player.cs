using Godot;

public partial class Player : CharacterBody2D
{
    [Export] private float _speed = 150.0f;

    private Sprite2D _sprite;
    private World? _world;

    public override void _Ready()
    {
        AddToGroup("player");
        YSortEnabled = false;

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite != null)
        {
            var size = _sprite.Texture?.GetSize() ?? Vector2.Zero;
            _sprite.Position = new Vector2(0, -(size.Y * 0.5f));
        }
    }

    public void SetWorld(World world)
    {
        _world = world;
        _world.RegisterPlayer(this);
    }

    public override void _Process(double delta)
    {
        ZIndex = (int)GlobalPosition.Y;
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
