using Godot;

namespace KBTV.UI
{
    /// <summary>
    /// Reusable transcript overlay that loads LiveShowPanel.tscn and
    /// positions itself at bottom-center of the viewport.
    /// Width: 78% of viewport, Height: 22% of viewport, 18px bottom padding.
    /// </summary>
    public partial class TranscriptOverlay : Control
    {
        private Control? _panel;

        public override void _Ready()
        {
            Name = "TranscriptOverlay";
            SetAnchorsPreset(LayoutPreset.FullRect);
            MouseFilter = MouseFilterEnum.Ignore;

            var scene = ResourceLoader.Load<PackedScene>("res://scenes/ui/LiveShowPanel.tscn");
            if (scene != null)
            {
                _panel = scene.Instantiate<Control>();
                _panel.Name = "LiveShowPanel";
                AddChild(_panel);
                UpdateLayout();
            }
            else
            {
                GD.PrintErr("TranscriptOverlay: Failed to load LiveShowPanel.tscn");
            }
        }

        public override void _Process(double delta)
        {
            // Re-position every frame to handle viewport resize
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            if (_panel == null) return;

            var viewport = GetViewport();
            if (viewport == null) return;

            var size = viewport.GetVisibleRect().Size;
            var panelWidth = Mathf.Max(720f, size.X * 0.78f);
            var panelHeight = Mathf.Max(150f, size.Y * 0.22f);
            panelWidth = Mathf.Min(panelWidth, size.X - 32f);
            panelHeight = Mathf.Min(panelHeight, size.Y * 0.35f);
            var left = (size.X - panelWidth) * 0.5f;
            var top = size.Y - panelHeight - 18;

            _panel.SetAnchorsPreset(LayoutPreset.TopLeft);
            _panel.OffsetLeft = left;
            _panel.OffsetTop = top;
            _panel.OffsetRight = left + panelWidth;
            _panel.OffsetBottom = top + panelHeight;
        }
    }
}
