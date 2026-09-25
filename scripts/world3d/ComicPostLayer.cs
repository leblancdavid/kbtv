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
	[Export] public float HalftoneStrength { get; set; } = 0.0f;
	[Export] public Vector2 HalftoneBand { get; set; } = new(0.30f, 0.72f);
	[Export] public bool MedianFilterEnabled { get; set; } = true;
	[Export] public float MedianFilterStrength { get; set; } = 0.25f;
	[Export] public float MedianFilterThreshold { get; set; } = 0.08f;
	[Export] public float MedianFilterRadiusPx { get; set; } = 1.5f;
	[Export] public bool ShadowFlattenEnabled { get; set; } = false;
	[Export] public float ShadowFlattenThreshold { get; set; } = 0.30f;
	[Export] public float ShadowFlattenSoftness { get; set; } = 0.035f;
	[Export] public float ShadowFlattenStrength { get; set; } = 0.0f;
	[Export] public bool ShadowSmoothEnabled { get; set; } = true;
	[Export] public float ShadowSmoothThreshold { get; set; } = 0.55f;
	[Export] public float ShadowSmoothSoftness { get; set; } = 0.14f;
	[Export] public float ShadowSmoothEdgeReject { get; set; } = 0.34f;
	[Export] public float ShadowSmoothEdgeSoftness { get; set; } = 0.12f;
	[Export] public float ShadowSmoothStrength { get; set; } = 1.0f;
	[Export] public float ShadowSmoothRadiusPx { get; set; } = 4.0f;

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
		_material.SetShaderParameter("median_filter_enabled", MedianFilterEnabled);
		_material.SetShaderParameter("median_filter_strength", MedianFilterStrength);
		_material.SetShaderParameter("median_filter_threshold", MedianFilterThreshold);
		_material.SetShaderParameter("median_filter_radius_px", MedianFilterRadiusPx);
		_material.SetShaderParameter("shadow_flatten_enabled", ShadowFlattenEnabled);
		_material.SetShaderParameter("shadow_flatten_threshold", ShadowFlattenThreshold);
		_material.SetShaderParameter("shadow_flatten_softness", ShadowFlattenSoftness);
		_material.SetShaderParameter("shadow_flatten_strength", ShadowFlattenStrength);
		_material.SetShaderParameter("shadow_smooth_enabled", ShadowSmoothEnabled);
		_material.SetShaderParameter("shadow_smooth_threshold", ShadowSmoothThreshold);
		_material.SetShaderParameter("shadow_smooth_softness", ShadowSmoothSoftness);
		_material.SetShaderParameter("shadow_smooth_edge_reject", ShadowSmoothEdgeReject);
		_material.SetShaderParameter("shadow_smooth_edge_softness", ShadowSmoothEdgeSoftness);
		_material.SetShaderParameter("shadow_smooth_strength", ShadowSmoothStrength);
		_material.SetShaderParameter("shadow_smooth_radius_px", ShadowSmoothRadiusPx);
	}
}
