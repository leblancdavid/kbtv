#nullable enable

using Godot;

namespace KBTV.World3D;

public partial class ComicPostLayer : Node3D
{
	private const string ShaderPath = "res://shaders/comic_post.gdshader";
	private const int PostRenderPriority = -128;

	[Export] public bool ComicPostEnabled { get; set; } = true;
	[Export] public Key ToggleKey { get; set; } = Key.F10;
	[Export] public Key ToggleOutlinesKey { get; set; } = Key.F9;
	[Export] public float EffectStrength { get; set; } = 1.0f;
	[Export] public int PosterizeSteps { get; set; } = 6;
	[Export] public float Saturation { get; set; } = 1.2f;
	[Export] public float OutlineThreshold { get; set; } = 0.14f;
	[Export] public float OutlineBias { get; set; } = 0.045f;
	[Export] public float OutlineWidthPx { get; set; } = 2.0f;
	[Export] public Color OutlineColor { get; set; } = new(0f, 0f, 0f, 1.0f);
	[Export] public float OutlineMix { get; set; } = 1.2f;
	// Depth jumps are compared as a fraction of distance, so these are
	// dimensionless: threshold is "1.5% closer/farther", width is the diagonal
	// tap spacing in pixels.
	[Export] public bool DepthEdgesEnabled { get; set; } = true;
	[Export] public float DepthEdgeMix { get; set; } = 1.0f;
	[Export] public float DepthEdgeThreshold { get; set; } = 0.045f;
	[Export] public float DepthEdgeBias { get; set; } = 0.018f;
	[Export] public float DepthEdgeWidthPx { get; set; } = 2.0f;
	[Export] public bool NormalEdgesEnabled { get; set; } = true;
	[Export] public float NormalEdgeMix { get; set; } = 0.35f;
	[Export] public float NormalEdgeThreshold { get; set; } = 0.10f;
	[Export] public float NormalEdgeBias { get; set; } = 0.10f;
	[Export] public float LumaEdgeMix { get; set; } = 0.18f;
	[Export] public float DetailInkSuppression { get; set; } = 0.92f;
	[Export] public float BrightDetailInkSuppression { get; set; } = 0.75f;
	[Export] public float BrightDetailThreshold { get; set; } = 0.34f;
	[Export] public float BrightDetailSoftness { get; set; } = 0.18f;
	[Export] public float MacroEdgeWidthPx { get; set; } = 7.0f;
	[Export] public float MacroEdgeThreshold { get; set; } = 0.16f;
	[Export] public float MacroEdgeBias { get; set; } = 0.10f;
	[Export] public float HalftoneSizePx { get; set; } = 14.0f;
	[Export] public float HalftoneStrength { get; set; } = 0.0f;
	[Export] public Vector2 HalftoneBand { get; set; } = new(0.30f, 0.72f);
	[Export] public bool MedianFilterEnabled { get; set; } = true;
	[Export] public float MedianFilterStrength { get; set; } = 0.18f;
	[Export] public float MedianFilterThreshold { get; set; } = 0.08f;
	[Export] public float MedianFilterRadiusPx { get; set; } = 1.5f;
	[Export] public float SobelPrefilterStrength { get; set; } = 0.45f;
	[Export] public bool ShadowFlattenEnabled { get; set; } = false;
	[Export] public float ShadowFlattenThreshold { get; set; } = 0.30f;
	[Export] public float ShadowFlattenSoftness { get; set; } = 0.035f;
	[Export] public float ShadowFlattenStrength { get; set; } = 0.0f;
	[Export] public bool ShadowSmoothEnabled { get; set; } = true;
	[Export] public float ShadowSmoothThreshold { get; set; } = 0.55f;
	[Export] public float ShadowSmoothSoftness { get; set; } = 0.14f;
	[Export] public float ShadowSmoothEdgeReject { get; set; } = 0.34f;
	[Export] public float ShadowSmoothEdgeSoftness { get; set; } = 0.12f;
	[Export] public float ShadowSmoothStrength { get; set; } = 0.0f;
	[Export] public float ShadowSmoothRadiusPx { get; set; } = 4.0f;
	[Export] public bool LightPosterizeEnabled { get; set; } = true;
	[Export] public float LightPosterizeSteps { get; set; } = 4.0f;
	[Export] public float LightPosterizeStrength { get; set; } = 0.65f;
	[Export] public float LightPosterizeThreshold { get; set; } = 0.34f;
	[Export] public float LightPosterizeSoftness { get; set; } = 0.18f;

	private ShaderMaterial? _material;
	private float _savedOutlineMix = 1.0f;
	// The shader needs the active camera's near/far to linearize depth, and Godot
	// does not expose the projection matrix to the fragment stage. Cached so the
	// per-frame path is two float compares instead of a viewport lookup.
	private Camera3D? _trackedCamera;
	private Vector2 _nearFar = new(0.05f, 100.0f);

	public override void _Ready()
	{
		BuildOverlay();
		ApplyShaderParameters();
		Visible = ComicPostEnabled;
	}

	public override void _Process(double delta)
	{
		SyncCameraNearFar();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
		{
			return;
		}

		if (ToggleKey != Key.None && keyEvent.Keycode == ToggleKey)
		{
			ComicPostEnabled = !ComicPostEnabled;
			Visible = ComicPostEnabled;
			GD.Print($"Comic post filter {(ComicPostEnabled ? "enabled" : "disabled")}");
			GetViewport().SetInputAsHandled();
			return;
		}

		if (ToggleOutlinesKey != Key.None && keyEvent.Keycode == ToggleOutlinesKey)
		{
			ToggleOutlines();
			GetViewport().SetInputAsHandled();
		}
	}

	private void SyncCameraNearFar()
	{
		var camera = GetViewport().GetCamera3D();
		if (camera is null)
		{
			// Leave the last known values in place; a frame with no active camera
			// should not blank the outlines.
			return;
		}

		if (camera != _trackedCamera)
		{
			_trackedCamera = camera;
			_nearFar = new Vector2(camera.Near, camera.Far);
		}
		else if (Mathf.IsEqualApprox(camera.Near, _nearFar.X) && Mathf.IsEqualApprox(camera.Far, _nearFar.Y))
		{
			return;
		}

		if (_material is null)
		{
			return;
		}

		_nearFar = new Vector2(camera.Near, camera.Far);
		_material.SetShaderParameter("camera_near_far", _nearFar);
	}

	private void ToggleOutlines()
	{
		if (OutlineMix > 0.001f)
		{
			_savedOutlineMix = OutlineMix;
			OutlineMix = 0.0f;
		}
		else
		{
			OutlineMix = _savedOutlineMix <= 0.001f ? 1.0f : _savedOutlineMix;
		}

		_material?.SetShaderParameter("outline_mix", OutlineMix);
		GD.Print($"Comic outlines {(OutlineMix > 0.001f ? "enabled" : "disabled")}");
	}

	private void BuildOverlay()
	{
		var shader = ResourceLoader.Load<Shader>(ShaderPath);
		if (shader == null)
		{
			GD.PushWarning($"ComicPostLayer: shader not found at {ShaderPath}");
			return;
		}

		_material = new ShaderMaterial { Shader = shader, RenderPriority = PostRenderPriority };
		var quad = new MeshInstance3D
		{
			Name = "ComicPostQuad",
			Mesh = new QuadMesh { Size = new Vector2(2f, 2f) },
			MaterialOverride = _material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			ExtraCullMargin = 16384f
		};
		AddChild(quad);
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
		_material.SetShaderParameter("depth_edges_enabled", DepthEdgesEnabled);
		_material.SetShaderParameter("depth_edge_mix", DepthEdgeMix);
		_material.SetShaderParameter("depth_edge_threshold", DepthEdgeThreshold);
		_material.SetShaderParameter("depth_edge_bias", DepthEdgeBias);
		_material.SetShaderParameter("depth_edge_width_px", DepthEdgeWidthPx);
		_material.SetShaderParameter("normal_edges_enabled", NormalEdgesEnabled);
		_material.SetShaderParameter("normal_edge_mix", NormalEdgeMix);
		_material.SetShaderParameter("normal_edge_threshold", NormalEdgeThreshold);
		_material.SetShaderParameter("normal_edge_bias", NormalEdgeBias);
		_material.SetShaderParameter("luma_edge_mix", LumaEdgeMix);
		_material.SetShaderParameter("detail_ink_suppression", DetailInkSuppression);
		_material.SetShaderParameter("bright_detail_ink_suppression", BrightDetailInkSuppression);
		_material.SetShaderParameter("bright_detail_threshold", BrightDetailThreshold);
		_material.SetShaderParameter("bright_detail_softness", BrightDetailSoftness);
		_material.SetShaderParameter("macro_edge_width_px", MacroEdgeWidthPx);
		_material.SetShaderParameter("macro_edge_threshold", MacroEdgeThreshold);
		_material.SetShaderParameter("macro_edge_bias", MacroEdgeBias);
		_material.SetShaderParameter("halftone_size_px", HalftoneSizePx);
		_material.SetShaderParameter("halftone_strength", HalftoneStrength);
		_material.SetShaderParameter("halftone_band", HalftoneBand);
		_material.SetShaderParameter("median_filter_enabled", MedianFilterEnabled);
		_material.SetShaderParameter("median_filter_strength", MedianFilterStrength);
		_material.SetShaderParameter("median_filter_threshold", MedianFilterThreshold);
		_material.SetShaderParameter("median_filter_radius_px", MedianFilterRadiusPx);
		_material.SetShaderParameter("sobel_prefilter_strength", SobelPrefilterStrength);
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
		_material.SetShaderParameter("light_posterize_enabled", LightPosterizeEnabled);
		_material.SetShaderParameter("light_posterize_steps", LightPosterizeSteps);
		_material.SetShaderParameter("light_posterize_strength", LightPosterizeStrength);
		_material.SetShaderParameter("light_posterize_threshold", LightPosterizeThreshold);
		_material.SetShaderParameter("light_posterize_softness", LightPosterizeSoftness);
		_material.SetShaderParameter("camera_near_far", _nearFar);
	}
}
