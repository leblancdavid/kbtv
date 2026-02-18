using Godot;

public partial class Occluder : Node
{
    [Export] public float Height { get; set; } = 32.0f;
    [Export] public float FadeRange { get; set; } = 16.0f;
    [Export] public bool Enabled { get; set; } = true;

    private Sprite2D _sprite;
    private ShaderMaterial _material;
    private Player _player;

    public override void _Ready()
    {
        _sprite = GetParent<Sprite2D>();
        if (_sprite == null)
        {
            GD.PrintErr("Occluder: Parent is not a Sprite2D!");
            return;
        }

        var shader = GD.Load<Shader>("res://shaders/occlusion.gdshader");
        if (shader != null)
        {
            _material = new ShaderMaterial();
            _material.Shader = shader;
            _sprite.Material = _material;
        }

        UpdatePlayerPosition();
    }

    public override void _Process(double delta)
    {
        UpdatePlayerPosition();
    }

    private void UpdatePlayerPosition()
    {
        if (_material == null || !Enabled)
            return;

        _player = GetTree().GetFirstNodeInGroup("player") as Player;
        if (_player != null)
        {
            var playerGlobalPos = _player.GlobalPosition;
            _material.SetShaderParameter("player_position", playerGlobalPos);
            _material.SetShaderParameter("object_height", Height);
            _material.SetShaderParameter("fade_range", FadeRange);
            _material.SetShaderParameter("occluder_enabled", Enabled);
        }
    }
}
