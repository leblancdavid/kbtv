using Godot;
using KBTV.Core;
using KBTV.Data;

namespace KBTV.Monitors
{
    /// <summary>
    /// Manages dead air penalties and consecutive tracking.
    /// Applies penalties when dead air starts and tracks consecutive occurrences.
    /// </summary>
    public partial class DeadAirManager : Node, IDependent
    {
        public override void _Notification(int what) => this.Notify(what);
        
        private IGameStateManager _gameStateManager => DependencyInjection.Get<IGameStateManager>(this);
        private int _consecutiveDeadAirCount = 0;
        
        /// <summary>
        /// Called when all dependencies are resolved.
        /// </summary>
        public void OnResolved()
        {
            // Dependencies are now available
        }
        
        /// <summary>
        /// Called when dead air starts playing.
        /// Increments consecutive count and applies penalty.
        /// </summary>
        public void OnDeadAirStarted()
        {
            _consecutiveDeadAirCount++;
            var vernStats = _gameStateManager?.VernStats;
            if (vernStats != null)
            {
                vernStats.ApplyDeadAirPenalty(_consecutiveDeadAirCount);
                GD.Print($"DeadAirManager: Applied dead air penalty (consecutive: {_consecutiveDeadAirCount})");
            }
        }
        
        /// <summary>
        /// Called when dead air ends (caller goes on or break starts).
        /// Resets consecutive counter.
        /// </summary>
        public void OnDeadAirEnded()
        {
            if (_consecutiveDeadAirCount > 0)
            {
                GD.Print($"DeadAirManager: Dead air ended, resetting consecutive counter from {_consecutiveDeadAirCount} to 0");
                _consecutiveDeadAirCount = 0;
            }
        }
        
        /// <summary>
        /// Get current consecutive dead air count for UI/debugging.
        /// </summary>
        public int ConsecutiveDeadAirCount => _consecutiveDeadAirCount;
    }
}