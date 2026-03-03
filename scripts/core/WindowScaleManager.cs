using Godot;

namespace KBTV.Core
{
    public partial class WindowScaleManager : Node
    {
        private Vector2I _baseSize;
        private bool _isSnapping;

        public override void _Ready()
        {
            _baseSize = new Vector2I(
                (int)ProjectSettings.GetSetting("display/window/size/viewport_width"),
                (int)ProjectSettings.GetSetting("display/window/size/viewport_height")
            );

            GetViewport().SizeChanged += OnViewportSizeChanged;
            SnapToNearestScale();
        }

        private void OnViewportSizeChanged()
        {
            if (_isSnapping)
            {
                return;
            }

            _isSnapping = true;
            CallDeferred(nameof(SnapToNearestScale));
        }

        private void SnapToNearestScale()
        {
            var windowSize = DisplayServer.WindowGetSize();
            var maxScale = GetMaxIntegerScaleForDisplay();
            var scale = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(
                windowSize.X / (float)_baseSize.X,
                windowSize.Y / (float)_baseSize.Y
            )), 1, maxScale);

            var targetSize = new Vector2I(_baseSize.X * scale, _baseSize.Y * scale);
            if (windowSize != targetSize)
            {
                DisplayServer.WindowSetSize(targetSize);
            }

            _isSnapping = false;
        }

        public void SetBorderlessFullscreen()
        {
            var displaySize = GetPrimaryDisplaySize();
            var maxScale = GetMaxIntegerScaleForDisplay();
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);
            DisplayServer.WindowSetSize(displaySize);
            CenterWindow(displaySize);
        }

        private void CenterWindow(Vector2I displaySize)
        {
            DisplayServer.WindowSetPosition(Vector2I.Zero);
        }

        private Vector2I GetPrimaryDisplaySize()
        {
            var screen = DisplayServer.WindowGetCurrentScreen();
            return DisplayServer.ScreenGetSize(screen);
        }

        private int GetMaxIntegerScaleForDisplay()
        {
            var displaySize = GetPrimaryDisplaySize();
            return Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(
                displaySize.X / (float)_baseSize.X,
                displaySize.Y / (float)_baseSize.Y
            )));
        }

        public override void _ExitTree()
        {
            GetViewport().SizeChanged -= OnViewportSizeChanged;
        }
    }
}
