using Godot;

public partial class Occluder : Sprite2D
{
    [Export] public new float Height { get; set; } = 64.0f;
    [Export] public new float FadeRange { get; set; } = 32.0f;
    [Export] public new bool Enabled { get; set; } = true;
    [Export] public bool IsFrontWall { get; set; } = false;

    private ShaderMaterial _material;
    private Player _player;

    public override void _Ready()
    {
        var shader = GD.Load<Shader>("res://shaders/occlusion.gdshader");
        if (shader != null)
        {
            _material = new ShaderMaterial();
            _material.Shader = shader;
            Material = _material;
        }

        UpdateOcclusion();
    }

    public override void _Process(double delta)
    {
        UpdateOcclusion();
    }

    private void UpdateOcclusion()
    {
        if (_material == null || !Enabled || !IsFrontWall)
            return;

        _player = GetTree().GetFirstNodeInGroup("player") as Player;
        if (_player != null)
        {
            var playerGlobalPos = _player.GlobalPosition;
            var spriteGlobalPos = GlobalPosition;
            
            float distance = spriteGlobalPos.Y - playerGlobalPos.Y;
            
            float alpha = 1.0f;
            if (distance > 0)
            {
                alpha = Mathf.Clamp(distance / FadeRange, 0.0f, 1.0f);
            }
            
            _material.SetShaderParameter("player_position", playerGlobalPos);
            _material.SetShaderParameter("object_height", Height);
            _material.SetShaderParameter("fade_range", FadeRange);
            _material.SetShaderParameter("occluder_enabled", Enabled);
            _material.SetShaderParameter("custom_alpha", alpha);
        }
    }
}
