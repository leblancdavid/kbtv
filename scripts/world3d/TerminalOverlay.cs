using System;
using Godot;
using KBTV.UI;

namespace KBTV.World3D;

public partial class TerminalOverlay : CanvasLayer
{
	private const float MinWidth = 120f;
	private const float MinHeight = 68f;
	private const float ScreenInsetScale = 0.67f;
	private const float ScreenAspect = 16f / 9f;
	private static readonly Vector2 ScreenFitOffset = new(0f, 48f);

	private Control _root = null!;
	private Control _screenFrame = null!;
	private ColorRect _glow = null!;
	private CallerTab? _callerTab;

	public event Action? CloseRequested;

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
		_screenFrame.Position = topLeft;
		_screenFrame.Size = new Vector2(width, height);
		_screenFrame.Rotation = angle;

		_glow.Position = topLeft;
		_glow.Size = new Vector2(width, height);
		_glow.Rotation = angle;
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

		_glow = new ColorRect
		{
			Name = "CrtGlow",
			Color = new Color(0.06f, 0.58f, 0.42f, 0.055f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_root.AddChild(_glow);

		_screenFrame = new Control
		{
			Name = "ProjectedCrtScreen",
			ClipContents = true,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_root.AddChild(_screenFrame);

		var background = new ColorRect
		{
			Name = "ScreenPhosphorBackground",
			Color = new Color(0.015f, 0.025f, 0.022f, 0.96f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_screenFrame.AddChild(background);

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
		_callerTab.Modulate = new Color(0.74f, 1.0f, 0.9f, 1f);
		_callerTab.CloseRequested += RequestClose;
		_callerTab.BackRequested += RequestClose;
		_screenFrame.AddChild(_callerTab);
		_screenFrame.MoveChild(_callerTab, 1);
	}

	private void AddCrtEffects()
	{
		var tint = new ColorRect
		{
			Name = "CrtTint",
			Color = new Color(0.0f, 0.20f, 0.15f, 0.10f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		tint.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_screenFrame.AddChild(tint);

		var scanlines = new Control
		{
			Name = "CrtScanlines",
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		scanlines.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		scanlines.Draw += () => DrawScanlines(scanlines);
		_screenFrame.AddChild(scanlines);

		var dust = new Control
		{
			Name = "CrtDust",
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		dust.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		dust.Draw += () => DrawDust(dust);
		_screenFrame.AddChild(dust);

		var glass = new Control
		{
			Name = "CrtGlassOverlay",
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		glass.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		glass.Draw += () => DrawGlass(glass);
		_screenFrame.AddChild(glass);

		var vignette = new Control
		{
			Name = "CrtGlassVignette",
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		vignette.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		vignette.Draw += () => DrawVignette(vignette);
		_screenFrame.AddChild(vignette);
	}

	private static void DrawScanlines(Control control)
	{
		var size = control.Size;
		var dark = new Color(0f, 0f, 0f, 0.18f);
		var bright = new Color(0.18f, 0.70f, 0.52f, 0.055f);
		for (var y = 1f; y < size.Y; y += 4f)
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
		var softGlare = new Color(0.82f, 1.0f, 0.88f, 0.055f);
		var hardGlare = new Color(0.92f, 1.0f, 0.94f, 0.085f);
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

		var diagonalBand = new Vector2[]
		{
			new(size.X * 0.06f, 0f),
			new(size.X * 0.22f, 0f),
			new(size.X * 0.04f, size.Y),
			new(0f, size.Y)
		};
		control.DrawColoredPolygon(diagonalBand, new Color(0.72f, 1.0f, 0.84f, 0.035f));

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
		for (var i = 0; i < 14; i++)
		{
			var t = i / 13f;
			var alpha = Mathf.Lerp(0.18f, 0.010f, t);
			var thickness = 2f + i * 2.6f;
			var edge = new Color(0.0f, 0.012f, 0.01f, alpha);
			control.DrawRect(new Rect2(Vector2.Zero, new Vector2(size.X, thickness)), edge);
			control.DrawRect(new Rect2(new Vector2(0f, size.Y - thickness), new Vector2(size.X, thickness)), edge);
			control.DrawRect(new Rect2(Vector2.Zero, new Vector2(thickness, size.Y)), edge);
			control.DrawRect(new Rect2(new Vector2(size.X - thickness, 0f), new Vector2(thickness, size.Y)), edge);
		}

		var radius = Mathf.Min(34f, Mathf.Min(size.X, size.Y) * 0.11f);
		for (var i = 2; i >= 0; i--)
		{
			var cornerRadius = radius + i * 5f;
			var alpha = i == 0 ? 0.18f : 0.055f - i * 0.015f;
			var mask = new Color(0.0f, 0.012f, 0.01f, alpha);
			DrawCornerCutout(control, size, cornerRadius, 0, mask);
			DrawCornerCutout(control, size, cornerRadius, 1, mask);
			DrawCornerCutout(control, size, cornerRadius, 2, mask);
			DrawCornerCutout(control, size, cornerRadius, 3, mask);
		}
	}

	private static void DrawCornerCutout(Control control, Vector2 size, float radius, int corner, Color color)
	{
		const int Steps = 8;
		var points = new Vector2[Steps + 3];
		var center = corner switch
		{
			0 => new Vector2(radius, radius),
			1 => new Vector2(size.X - radius, radius),
			2 => new Vector2(size.X - radius, size.Y - radius),
			_ => new Vector2(radius, size.Y - radius)
		};

		var cornerPoint = corner switch
		{
			0 => Vector2.Zero,
			1 => new Vector2(size.X, 0f),
			2 => size,
			_ => new Vector2(0f, size.Y)
		};

		var startAngle = corner switch
		{
			0 => -Mathf.Pi / 2f,
			1 => 0f,
			2 => Mathf.Pi / 2f,
			_ => Mathf.Pi
		};

		points[0] = cornerPoint;
		for (var i = 0; i <= Steps; i++)
		{
			var angle = startAngle + i * Mathf.Pi / (2f * Steps);
			points[i + 1] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
		}
		points[^1] = cornerPoint;

		control.DrawColoredPolygon(points, color);
	}

	private void RequestClose()
	{
		CloseRequested?.Invoke();
	}
}
