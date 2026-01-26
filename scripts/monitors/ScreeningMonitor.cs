#nullable enable

using Godot;
using KBTV.Core;
using KBTV.Screening;

namespace KBTV.Monitors
{
    /// <summary>
    /// Monitors screening progress and updates time-based properties.
    /// Handles property revelation timing, patience drain, and progress polling.
    ///
    /// State Updates:
    /// - Active screening session: Update revelations and patience
    /// - Inactive: No updates
    ///
    /// Side Effects:
    /// - Triggers ProgressUpdated event for UI polling
    /// </summary>
    public partial class ScreeningMonitor : Node, IDependent
    {
        public override void _Notification(int what) => this.Notify(what);

        private IScreeningController? _controller;

        private IScreeningController ScreeningController => DependencyInjection.Get<IScreeningController>(this);

        public void OnResolved()
        {
            _controller = ScreeningController;
        }

        public override void _Process(double delta)
        {
            if (_controller == null || !_controller.IsActive)
            {
                return;
            }

            _controller.Update((float)delta);
        }
    }
}
