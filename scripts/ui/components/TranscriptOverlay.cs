using Godot;

namespace KBTV.UI
{
    /// <summary>
    /// Reusable transcript overlay that loads LiveShowPanel.tscn and
    /// positions itself at bottom-center of the viewport.
    /// Width: 60% of viewport, Height: 25% of viewport, 4px bottom padding.
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
            var panelWidth = size.X * 0.6f;
            var panelHeight = size.Y * 0.25f;
            var left = (size.X - panelWidth) * 0.5f;
            var top = size.Y - panelHeight - 4;

            _panel.SetAnchorsPreset(LayoutPreset.TopLeft);
            _panel.OffsetLeft = left;
            _panel.OffsetTop = top;
            _panel.OffsetRight = left + panelWidth;
            _panel.OffsetBottom = top + panelHeight;
        }
    }
}
