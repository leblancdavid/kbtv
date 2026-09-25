#nullable enable

using Godot;

namespace KBTV.World3D;

public partial class ComicPostLayer : CanvasLayer
{
	private const string ShaderPath = "res://shaders/comic_post.gdshader";
	private const int LayerConstant = -10;

	[Export] public bool ComicPostEnabled { get; set; } = true;
	[Export] public Key ToggleKey { get; set; } = Key.F8;
	[Export] public float EffectStrength { get; set; } = 0.78f;
	[Export] public int PosterizeSteps { get; set; } = 5;
	[Export] public float Saturation { get; set; } = 1.22f;
	[Export] public float OutlineThreshold { get; set; } = 0.22f;
	[Export] public float OutlineBias { get; set; } = 0.07f;
	[Export] public float OutlineWidthPx { get; set; } = 2.0f;
	[Export] public Color OutlineColor { get; set; } = new(0.02f, 0.03f, 0.07f, 1.0f);
	[Export] public float OutlineMix { get; set; } = 1.0f;
	[Export] public float HalftoneSizePx { get; set; } = 14.0f;
	[Export] public float HalftoneStrength { get; set; } = 0.012f;
	[Export] public Vector2 HalftoneBand { get; set; } = new(0.10f, 0.72f);

	private ShaderMaterial? _material;

	public ComicPostLayer()
	{
		Layer = LayerConstant;
	}

	public override void _Ready()
	{
		BuildOverlay();
		ApplyShaderParameters();
		Visible = ComicPostEnabled;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
		{
			return;
		}

		if (ToggleKey == Key.None || keyEvent.Keycode != ToggleKey)
		{
			return;
		}

		ComicPostEnabled = !ComicPostEnabled;
		Visible = ComicPostEnabled;
		GD.Print($"Comic post filter {(ComicPostEnabled ? "enabled" : "disabled")}");
		GetViewport().SetInputAsHandled();
	}

	private void BuildOverlay()
	{
		var shader = ResourceLoader.Load<Shader>(ShaderPath);
		if (shader == null)
		{
			GD.PushWarning($"ComicPostLayer: shader not found at {ShaderPath}");
			return;
		}

		_material = new ShaderMaterial { Shader = shader };
		var rect = new ColorRect
		{
			Name = "ComicPostRect",
			Color = Colors.White,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Material = _material
		};
		rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(rect);
	}

	private void ApplyShaderParameters()
	{
		if (_material == null)
		{
			return;
		}

		_material.SetShaderParameter("effect_strength", EffectStrength);
		_material.SetShaderParameter("posterize_steps", PosterizeSteps);
		_material.SetShaderParameter("saturation", Saturation);
		_material.SetShaderParameter("outline_threshold", OutlineThreshold);
		_material.SetShaderParameter("outline_bias", OutlineBias);
		_material.SetShaderParameter("outline_width_px", OutlineWidthPx);
		_material.SetShaderParameter("outline_color", OutlineColor);
		_material.SetShaderParameter("outline_mix", OutlineMix);
		_material.SetShaderParameter("halftone_size_px", HalftoneSizePx);
		_material.SetShaderParameter("halftone_strength", HalftoneStrength);
		_material.SetShaderParameter("halftone_band", HalftoneBand);
	}
}
