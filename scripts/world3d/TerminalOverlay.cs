using System;
using Godot;
using KBTV.UI;

namespace KBTV.World3D;

public partial class TerminalOverlay : CanvasLayer
{
	private const float MinWidth = 640f;
	private const float MinHeight = 360f;
	private const float Padding = 10f;

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
		if (points.Length == 0)
		{
			return;
		}

		var min = points[0];
		var max = points[0];
		foreach (var point in points)
		{
			min = new Vector2(Mathf.Min(min.X, point.X), Mathf.Min(min.Y, point.Y));
			max = new Vector2(Mathf.Max(max.X, point.X), Mathf.Max(max.Y, point.Y));
		}

		var size = max - min;
		if (size.X < MinWidth || size.Y < MinHeight)
		{
			var center = (min + max) * 0.5f;
			size = new Vector2(Mathf.Max(size.X, MinWidth), Mathf.Max(size.Y, MinHeight));
			min = center - size * 0.5f;
		}

		var availableSize = new Vector2(
			Mathf.Max(64f, viewportSize.X - Padding * 2f),
			Mathf.Max(64f, viewportSize.Y - Padding * 2f));
		size = new Vector2(
			Mathf.Min(size.X - Padding * 2f, availableSize.X),
			Mathf.Min(size.Y - Padding * 2f, availableSize.Y));
		min += new Vector2(Padding, Padding);
		min.X = Mathf.Clamp(min.X, 0f, Mathf.Max(0f, viewportSize.X - size.X));
		min.Y = Mathf.Clamp(min.Y, 0f, Mathf.Max(0f, viewportSize.Y - size.Y));

		_screenFrame.Position = min;
		_screenFrame.Size = size;
		_glow.Position = min - new Vector2(8f, 8f);
		_glow.Size = size + new Vector2(16f, 16f);
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
			Color = new Color(0.05f, 0.45f, 0.35f, 0.16f),
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
	}

	private void AddCrtEffects()
	{
		var tint = new ColorRect
		{
			Name = "CrtTint",
			Color = new Color(0.0f, 0.22f, 0.16f, 0.12f),
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

		var vignette = new Panel
		{
			Name = "CrtGlassVignette",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Modulate = new Color(0f, 0f, 0f, 0.28f)
		};
		vignette.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_screenFrame.AddChild(vignette);
	}

	private static void DrawScanlines(Control control)
	{
		var size = control.Size;
		var color = new Color(0f, 0f, 0f, 0.24f);
		for (var y = 1f; y < size.Y; y += 4f)
		{
			control.DrawLine(new Vector2(0f, y), new Vector2(size.X, y), color, 1f);
		}
	}

	private void RequestClose()
	{
		CloseRequested?.Invoke();
	}
}
