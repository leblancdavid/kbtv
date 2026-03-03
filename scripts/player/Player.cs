using Godot;
using KBTV.UI;
using KBTV.Core;

public partial class Player : CharacterBody2D
{
    [Export] private float _speed = 150.0f;

    private Sprite2D _sprite;
    private World? _world;
    private CanvasLayer? _callerTabLayer;
    private CallerScreenerManager? _callerScreenerManager;

    public override void _Ready()
    {
        AddToGroup("player");
        YSortEnabled = false;

        CacheCallerTabLayer();
        SubscribeToScreener();

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite != null)
        {
            var size = _sprite.Texture?.GetSize() ?? Vector2.Zero;
            _sprite.Position = new Vector2(0, -(size.Y * 0.5f));
        }
    }

    private void SubscribeToScreener()
    {
        if (_callerScreenerManager == null)
        {
            return;
        }

        _callerScreenerManager.Opened += OnScreenerOpened;
        _callerScreenerManager.Closed += OnScreenerClosed;
    }

    private void UnsubscribeFromScreener()
    {
        if (_callerScreenerManager == null)
        {
            return;
        }

        _callerScreenerManager.Opened -= OnScreenerOpened;
        _callerScreenerManager.Closed -= OnScreenerClosed;
    }

    private void OnScreenerOpened()
    {
        SetMovementLocked(true);
    }

    private void OnScreenerClosed()
    {
        SetMovementLocked(false);
    }

    private void CacheCallerTabLayer()
    {
        var tree = GetTree();
        if (tree?.Root == null)
        {
            return;
        }

        _callerTabLayer = tree.Root.GetNodeOrNull<CanvasLayer>("Main/ServiceProviderRoot/CallerScreenerManager/CanvasLayer");

        if (_callerTabLayer == null)
        {
            _callerTabLayer = tree.Root.GetNodeOrNull<CanvasLayer>("/root/Main/ServiceProviderRoot/CallerScreenerManager/CanvasLayer");
        }

        _callerScreenerManager = tree.Root.GetNodeOrNull<CallerScreenerManager>("Main/ServiceProviderRoot/CallerScreenerManager");

        if (_callerScreenerManager == null)
        {
            _callerScreenerManager = tree.Root.GetNodeOrNull<CallerScreenerManager>("/root/Main/ServiceProviderRoot/CallerScreenerManager");
        }
    }

    public void SetWorld(World world)
    {
        _world = world;
    }

    public override void _Process(double delta)
    {
        ZIndex = (int)GlobalPosition.Y;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_callerTabLayer == null || _callerScreenerManager == null)
        {
            CacheCallerTabLayer();
            SubscribeToScreener();
        }

        if (_callerScreenerManager != null && _callerScreenerManager.IsOpen)
        {
            SetMovementLocked(true);
            return;
        }

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

    public void SetMovementLocked(bool locked)
    {
        if (locked)
        {
            Velocity = Vector2.Zero;
        }

        SetPhysicsProcess(!locked);
    }

    public override void _ExitTree()
    {
        UnsubscribeFromScreener();
    }
}
