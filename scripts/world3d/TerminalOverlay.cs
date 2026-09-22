using System;
using Godot;
using KBTV.UI;

namespace KBTV.World3D;

public partial class TerminalOverlay : CanvasLayer
{
	private const float MinWidth = 120f;
	private const float MinHeight = 68f;
	private const float ScreenInsetScale = 0.68f;
	private const float ScreenAspect = 16f / 9f;
	private const int ContentMargin = 24;
	private const float ScanlineScrollSpeed = 5f;
	private const float ScanlineMinSpeed = 2f;
	private const float ScanlineMaxSpeed = 12f;
	private const float ScanlineSpeedChangeInterval = 1.2f;
	private const float ScanlineSpeedSmoothing = 5f;
	private const string OutputFeatherShaderPath = "res://shaders/crt_output_feather.gdshader";
	private static readonly Vector2I ScreenViewportSize = new(1152, 640);
	private static readonly Vector2 ScreenFitOffset = new(0f, 48f);

	private Control _root = null!;
	private SubViewportContainer _screenFrame = null!;
	private SubViewport _screenViewport = null!;
	private Control _screenRoot = null!;
	private MarginContainer _contentHost = null!;
	private ShaderMaterial? _outputMaterial;
	private CallerTab? _callerTab;
	private Control? _scanlines;
	private float _overlayTime;
	private double _nextSpeedChange;
	private float _scanlinePhase;
	private float _scanlineSpeed = ScanlineScrollSpeed;
	private float _scanlineSpeedTarget = ScanlineScrollSpeed;

	public event Action? CloseRequested;

	/// <summary>
	/// Phosphor tone applied to content rendered inside the CRT viewport so it
	/// blends with the projected screen look (CallerTab and hosted dialogs).
	/// </summary>
	public static readonly Color PhosphorTint = new(0.85f, 1.0f, 0.82f, 1f);

	/// <summary>
	/// Safe-area container inside the projected CRT viewport. UI registered here
	/// renders on the monitor above the CallerTab and receives the CRT effects
	/// (tint, scanlines, vignette). Used by ModalManager to host dialogs on-screen.
	/// </summary>
	public Control? ContentHost => _contentHost;


	public override void _Ready()
	{
		Layer = 120;
		Visible = false;
		BuildUi();
	}

	public void ShowTerminal()
	{
		EnsureCallerTab();
		Visible = true;
		_root.MouseFilter = Control.MouseFilterEnum.Pass;
		_screenFrame.MouseFilter = Control.MouseFilterEnum.Stop;
	}

	public void HideTerminal()
	{
		Visible = false;
		_screenFrame.MouseFilter = Control.MouseFilterEnum.Ignore;
	}

	public override void _Process(double delta)
	{
		if (!Visible || _scanlines == null)
		{
			return;
		}

		var dt = (float)delta;
		_overlayTime += dt;

		if (_overlayTime >= _nextSpeedChange)
		{
			_scanlineSpeedTarget = ScanlineMinSpeed + GD.Randf() * (ScanlineMaxSpeed - ScanlineMinSpeed);
			_nextSpeedChange = _overlayTime + ScanlineSpeedChangeInterval * (0.5f + GD.Randf());
		}

		_scanlineSpeed = Mathf.MoveToward(_scanlineSpeed, _scanlineSpeedTarget, ScanlineSpeedSmoothing * dt);
		_scanlinePhase = (_scanlinePhase + _scanlineSpeed * dt) % 4f;

		_scanlines.QueueRedraw();
	}

	public void SetScreenBounds(Vector2[] points, Vector2 viewportSize)
	{
		if (points.Length < 4)
		{
			return;
		}

		var center = (points[0] + points[1] + points[2] + points[3]) * 0.25f + ScreenFitOffset;
		var topLeft = center + (points[0] - center) * ScreenInsetScale;
		var topRight = center + (points[1] - center) * ScreenInsetScale;
		var bottomLeft = center + (points[2] - center) * ScreenInsetScale;

		var width = Mathf.Max(MinWidth, topLeft.DistanceTo(topRight));
		var height = Mathf.Max(MinHeight, topLeft.DistanceTo(bottomLeft));
		var aspectHeight = width / ScreenAspect;
		if (aspectHeight < height)
		{
			var yAxis = (bottomLeft - topLeft).Normalized();
			height = aspectHeight;
			bottomLeft = topLeft + yAxis * height;
		}

		var angle = (topRight - topLeft).Angle();
		var logicalSize = new Vector2I(Mathf.CeilToInt(width), Mathf.CeilToInt(height));
		// Allocate enough texels for the final window, not just design-space pixels.
		// Use the root transform to avoid feeding the output's inverse scale back in.
		var pixelTransform = GetViewport().GetStretchTransform() * _root.GetGlobalTransformWithCanvas();
		var density = Mathf.Max(1f, Mathf.Max(pixelTransform.X.Length(), pixelTransform.Y.Length()));
		var renderSize = new Vector2(Mathf.Ceil(width * density), Mathf.Ceil(height * density));
		if (_screenViewport.Size2DOverride != logicalSize)
		{
			_screenViewport.Size2DOverride = logicalSize;
		}
		_screenFrame.Position = topLeft;
		_screenFrame.Size = renderSize;
		_screenFrame.Scale = new Vector2(width, height) / renderSize;
		_screenFrame.Rotation = angle;
		_screenFrame.Stretch = true;
		_outputMaterial?.SetShaderParameter("screen_size", new Vector2(width, height));
	}

	private void BuildUi()
	{
		_root = new Control
		{
			Name = "TerminalOverlayRoot",
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		_root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_root);

		_screenFrame = new SubViewportContainer
		{
			Name = "ProjectedCrtScreen",
			ClipContents = true,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			TextureFilter = CanvasItem.TextureFilterEnum.Linear
		};
		_root.AddChild(_screenFrame);

		_screenViewport = new SubViewport
		{
			Name = "ProjectedCrtViewport",
			TransparentBg = true,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
			Disable3D = true,
			World2D = new World2D(),
			Size2DOverride = ScreenViewportSize,
			Size2DOverrideStretch = true,
			Size = ScreenViewportSize
		};
		_screenFrame.AddChild(_screenViewport);

		var screenLayer = new CanvasLayer
		{
			Name = "ProjectedCrtCanvas",
			Layer = 1
		};
		_screenViewport.AddChild(screenLayer);

		_screenRoot = new Control
		{
			Name = "ProjectedCrtRoot",
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		_screenRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		screenLayer.AddChild(_screenRoot);

		var outputShader = ResourceLoader.Load<Shader>(OutputFeatherShaderPath);
		if (outputShader != null)
		{
			_outputMaterial = new ShaderMaterial { Shader = outputShader };
			_screenFrame.Material = _outputMaterial;
		}

		var background = new ColorRect
		{
			Name = "ScreenPhosphorBackground",
			Color = new Color(0.015f, 0.025f, 0.022f, 0.96f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_screenRoot.AddChild(background);

		_contentHost = new MarginContainer
		{
			Name = "CrtContentSafeArea",
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		foreach (var side in new[] { "left", "top", "right", "bottom" })
		{
			_contentHost.AddThemeConstantOverride($"margin_{side}", ContentMargin);
		}
		_contentHost.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_screenRoot.AddChild(_contentHost);

		AddCrtEffects();
	}

	private void EnsureCallerTab()
	{
		if (_callerTab != null)
		{
			return;
		}

		var callerScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/CallerTab.tscn");
		if (callerScene == null)
		{
			GD.PushError("TerminalOverlay: CallerTab scene not found");
			return;
		}

		_callerTab = callerScene.Instantiate<CallerTab>();
		_callerTab.Name = "ProjectedCallerTab";
		_callerTab.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_callerTab.Modulate = PhosphorTint;
		_callerTab.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_callerTab.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		_contentHost.AddChild(_callerTab);
	}

	private void AddCrtEffects()
	{
		var tint = new ColorRect
		{
			Name = "CrtTint",
			Color = new Color(0.05f, 0.16f, 0.10f, 0.06f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		tint.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_screenRoot.AddChild(tint);

		var scanlines = new Control
		{
			Name = "CrtScanlines",
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		scanlines.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		scanlines.Draw += () => DrawScanlines(scanlines);
		_screenRoot.AddChild(scanlines);
		_scanlines = scanlines;

		var dust = new Control
		{
			Name = "CrtDust",
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		dust.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		dust.Draw += () => DrawDust(dust);
		_screenRoot.AddChild(dust);

		var glass = new Control
		{
			Name = "CrtGlassOverlay",
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		glass.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		glass.Draw += () => DrawGlass(glass);
		_screenRoot.AddChild(glass);

		var vignette = new Control
		{
			Name = "CrtGlassVignette",
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		vignette.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		vignette.Draw += () => DrawVignette(vignette);
		_screenRoot.AddChild(vignette);
	}

	private void DrawScanlines(Control control)
	{
		var size = control.Size;
		var flicker = Mathf.Clamp(
			1f + 0.22f * Mathf.Sin(_overlayTime * 43f) + 0.07f * (GD.Randf() * 2f - 1f),
			0.6f, 1.5f);
		var dark = new Color(0f, 0f, 0f, 0.18f * flicker);
		var bright = new Color(0.18f, 0.70f, 0.52f, 0.055f * flicker);
		var row = (int)Mathf.Floor(_scanlinePhase);
		for (var y = row - 4f; y < size.Y; y += 4f)
		{
			control.DrawLine(new Vector2(0f, y), new Vector2(size.X, y), dark, 1f);
			control.DrawLine(new Vector2(0f, y + 1f), new Vector2(size.X, y + 1f), bright, 1f);
		}
	}

	private static void DrawDust(Control control)
	{
		var size = control.Size;
		var dustColor = new Color(0.78f, 0.92f, 0.82f, 0.10f);
		var shadowColor = new Color(0f, 0f, 0f, 0.16f);
		for (var i = 0; i < 36; i++)
		{
			var edge = i % 4;
			var t = ((i * 37) % 100) / 100f;
			var jitter = ((i * 19) % 17) - 8f;
			var pos = edge switch
			{
				0 => new Vector2(t * size.X, 4f + Mathf.Abs(jitter) * 0.7f),
				1 => new Vector2(size.X - 5f - Mathf.Abs(jitter) * 0.8f, t * size.Y),
				2 => new Vector2(t * size.X, size.Y - 5f - Mathf.Abs(jitter) * 0.7f),
				_ => new Vector2(5f + Mathf.Abs(jitter) * 0.8f, t * size.Y)
			};
			var radius = 0.7f + (i % 3) * 0.45f;
			control.DrawCircle(pos, radius, dustColor);
		}

		control.DrawRect(new Rect2(Vector2.Zero, new Vector2(size.X, 10f)), shadowColor);
		control.DrawRect(new Rect2(new Vector2(0f, size.Y - 12f), new Vector2(size.X, 12f)), shadowColor);
		control.DrawRect(new Rect2(Vector2.Zero, new Vector2(12f, size.Y)), shadowColor);
		control.DrawRect(new Rect2(new Vector2(size.X - 12f, 0f), new Vector2(12f, size.Y)), shadowColor);
	}

	private static void DrawGlass(Control control)
	{
		var size = control.Size;
		var softGlare = new Color(0.82f, 1.0f, 0.88f, 0.025f);
		var hardGlare = new Color(0.92f, 1.0f, 0.94f, 0.04f);
		var scratch = new Color(0.82f, 0.98f, 0.88f, 0.075f);
		var smudge = new Color(0.52f, 0.78f, 0.64f, 0.045f);

		var topBand = new Vector2[]
		{
			new(0f, size.Y * 0.03f),
			new(size.X, size.Y * 0.0f),
			new(size.X, size.Y * 0.07f),
			new(0f, size.Y * 0.11f)
		};
		control.DrawColoredPolygon(topBand, softGlare);

		// Non-overlapping strips soften both sides without accumulating opacity.
		const int ReflectionStrips = 32;
		for (var i = 0; i < ReflectionStrips; i++)
		{
			var left = i / (float)ReflectionStrips;
			var right = (i + 1f) / ReflectionStrips;
			var strength = Mathf.Pow(Mathf.Sin((left + right) * 0.5f * Mathf.Pi), 2f);
			control.DrawColoredPolygon(new Vector2[]
			{
				new(size.X * Mathf.Lerp(0.06f, 0.22f, left), 0f),
				new(size.X * Mathf.Lerp(0.06f, 0.22f, right), 0f),
				new(size.X * 0.04f * right, size.Y),
				new(size.X * 0.04f * left, size.Y)
			}, new Color(0.72f, 1.0f, 0.84f, strength * 0.014f));
		}

		control.DrawLine(new Vector2(8f, 7f), new Vector2(size.X - 12f, 3f), hardGlare, 1f);
		control.DrawLine(new Vector2(6f, size.Y - 8f), new Vector2(size.X - 10f, size.Y - 5f), new Color(0f, 0f, 0f, 0.12f), 1f);

		for (var i = 0; i < 14; i++)
		{
			var x = ((i * 83) % 100) / 100f * size.X;
			var y = ((i * 47) % 100) / 100f * size.Y;
			var length = 8f + (i % 5) * 4f;
			control.DrawLine(new Vector2(x, y), new Vector2(Mathf.Min(size.X, x + length), y + 1f), scratch, 1f);
		}

		control.DrawCircle(new Vector2(size.X * 0.18f, size.Y * 0.32f), 9f, smudge);
		control.DrawCircle(new Vector2(size.X * 0.82f, size.Y * 0.72f), 11f, new Color(0.52f, 0.78f, 0.64f, 0.025f));
	}

	private static void DrawVignette(Control control)
	{
		var size = control.Size;
		// A shallow inner-bezel shadow: strongest above the glass, lighter elsewhere.
		for (var i = 0; i < 24; i++)
		{
			var fade = 1f - Mathf.SmoothStep(0f, 1f, i / 23f);
			control.DrawRect(new Rect2(0f, i, size.X, 1f), new Color(0f, 0.008f, 0.006f, fade * 0.28f));
			var edge = new Color(0f, 0.008f, 0.006f, fade * 0.12f);
			control.DrawRect(new Rect2(0f, size.Y - i - 1f, size.X, 1f), edge);
			control.DrawRect(new Rect2(i, 0f, 1f, size.Y), edge);
			control.DrawRect(new Rect2(size.X - i - 1f, 0f, 1f, size.Y), edge);
		}
	}

	private void RequestClose()
	{
		CloseRequested?.Invoke();
	}
}
