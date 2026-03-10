using Godot;
using KBTV.UI;
using KBTV.Core;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
    [Export] private float _speed = 150.0f;

    private AnimatedSprite2D _sprite;
    private World? _world;
    private CanvasLayer? _callerTabLayer;
    private CallerScreenerManager? _callerScreenerManager;
    private string _lastAnimation = "walk_south";
    private string _currentDirection = "south";
    private Dictionary<string, Texture2D[]> _animationFrames = new();

    public override void _Ready()
    {
        AddToGroup("player");
        YSortEnabled = false;

        CacheCallerTabLayer();
        SubscribeToScreener();

        _sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        if (_sprite != null)
        {
            SetupSpriteFrames();
        }
        else
        {
            GD.PrintErr("Player: AnimatedSprite2D node not found!");
        }
    }

    private void SetupSpriteFrames()
    {
        if (_sprite == null) return;

        var frames = new SpriteFrames();
        _animationFrames.Clear();
        
        // Load textures for each direction
        string[] directions = { "south", "east", "north", "west" };
        foreach (var dir in directions)
        {
            var animationName = $"walk_{dir}";
            var frameTextures = new Texture2D[6];
            
            for (int i = 0; i < 6; i++)
            {
                var path = $"res://assets/sprites/characters/player/walk_{dir}_{i}.png";
                var texture = ResourceLoader.Load<Texture2D>(path);
                if (texture == null)
                {
                    GD.PrintErr($"Player: Failed to load texture: {path}");
                }
                frameTextures[i] = texture;
            }
            
            // Store in dictionary for later retrieval (shadow updates)
            _animationFrames[animationName] = frameTextures;
            
            frames.AddAnimation(animationName);
            frames.SetAnimationSpeed(animationName, 10.0f);
            frames.SetAnimationLoop(animationName, true);
            
            for (int i = 0; i < 6; i++)
            {
                if (frameTextures[i] != null)
                {
                    frames.AddFrame(animationName, frameTextures[i]);
                }
            }
        }
        
        // Load idle sprites (single frame animations)
        string[] idleDirections = { "south", "east", "north", "west" };
        foreach (var dir in idleDirections)
        {
            var animationName = $"idle_{dir}";
            var path = $"res://assets/sprites/characters/player/{dir}.png";
            var texture = ResourceLoader.Load<Texture2D>(path);
            
            if (texture != null)
            {
                frames.AddAnimation(animationName);
                frames.SetAnimationSpeed(animationName, 0); // Static, no animation
                frames.AddFrame(animationName, texture);
                _animationFrames[animationName] = new Texture2D[] { texture };
            }
            else
            {
                GD.PrintErr($"Player: Failed to load idle texture: {path}");
            }
        }
        
        _sprite.SpriteFrames = frames;
        _sprite.Animation = "idle_south";
        _currentDirection = "south";
        
        // Position sprite so feet are at root origin (center horizontally, bottom at Y=0)
        var firstFrame = _animationFrames["idle_south"][0];
        if (firstFrame != null)
        {
            var size = firstFrame.GetSize();
            _sprite.Position = new Vector2(0, -size.Y * 0.5f);  // Center vertically, feet at origin
        }
        
        GD.Print("Player: SpriteFrames setup complete with " + frames.GetAnimationNames().Length + " animations");
    }

    private void SetDirectionAnimation(Vector2 direction)
    {
        if (_sprite == null) return;

        string animName = "walk_south"; // default

        if (direction.X > 0)
            animName = "walk_east";
        else if (direction.X < 0)
            animName = "walk_west";
        else if (direction.Y < 0)
            animName = "walk_north";
        else
            animName = "walk_south";

        if (_sprite.Animation != animName)
        {
            _sprite.Play(animName);
            _lastAnimation = animName;
            // Extract direction from animation name (e.g., "walk_north" -> "north")
            if (animName.StartsWith("walk_"))
            {
                _currentDirection = animName.Substring(5);
            }
        }
    }

    private void UpdateShadowFrame()
    {
        if (_sprite == null) return;

        // Find the shadow sprite from ShadowPivot
        var shadowPivot = GetNodeOrNull<Node2D>("ShadowPivot");
        if (shadowPivot == null) return;

        if (shadowPivot.GetChild(0) is Sprite2D shadowSprite)
        {
            // Get current frame texture from our dictionary
            var currentAnim = _sprite.Animation;
            var frameIdx = _sprite.Frame;
            if (_animationFrames.TryGetValue(currentAnim, out var frames) && frameIdx >= 0 && frameIdx < frames.Length)
            {
                var frameTexture = frames[frameIdx];
                if (frameTexture != null && shadowSprite.Texture != frameTexture)
                {
                    shadowSprite.Texture = frameTexture;
                }
            }
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
        UpdateShadowFrame();
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

        // Update animation based on movement direction
        if (_sprite != null)
        {
            if (velocity != Vector2.Zero)
            {
                SetDirectionAnimation(velocity);
                _sprite.Play();
            }
            else
            {
                // When stopped, play idle animation for current direction
                string idleAnim = $"idle_{_currentDirection}";
                if (_sprite.Animation != idleAnim)
                {
                    _sprite.Play(idleAnim);
                }
            }
        }
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
