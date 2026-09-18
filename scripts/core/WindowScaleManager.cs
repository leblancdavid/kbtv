using Godot;

namespace KBTV.Core
{
    /// <summary>
    /// Boots the window borderless-fullscreen at the display's native resolution.
    /// The viewport uses canvas_items stretch with aspect "expand", so the game
    /// fills the whole screen at any resolution with no integer-scale letterboxing.
    /// </summary>
    public partial class WindowScaleManager : Node
    {
        public void SetBorderlessFullscreen()
        {
            var displaySize = GetPrimaryDisplaySize();
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
    }
}
