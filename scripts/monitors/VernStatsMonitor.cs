#nullable enable

using Godot;
using KBTV.Core;
using KBTV.Data;

namespace KBTV.Monitors
{
    /// <summary>
    /// Monitors Vern's stats and applies decay over time.
    /// Handles dependency decay (caffeine, nicotine), physical decay (energy, satiety),
    /// and cognitive decay (alertness, focus, patience).
    ///
    /// State Updates:
    /// - Dependencies: Caffeine and nicotine decay continuously
    /// - Physical: Energy and satiety decay
    /// - Cognitive: Alertness, focus, and patience decay
    /// - Spirit: Not decayed directly (fluctuates based on other stats)
    /// - Belief: Not decayed (long-term persistent stat)
    ///
    /// Side Effects:
    /// - VernStats emits StatsChanged, VibeChanged, MoodTypeChanged when stats change
    /// - Low dependency levels affect other stat multipliers (see VernStats)
    /// </summary>
    public partial class VernStatsMonitor : DomainMonitor, IDependent
    {
        public override void _Notification(int what) => this.Notify(what);

        private VernStats? _vernStats;
        private GameStateManager? _gameState;

        private GameStateManager GameStateManager => DependencyInjection.Get<GameStateManager>(this);

        public override void OnResolved()
        {
            _vernStats = GameStateManager?.VernStats;
            _gameState = GameStateManager;
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (_vernStats == null || _gameState == null || !_gameState.IsLive)
            {
                return;
            }

            var stats = _vernStats;
            var dt = deltaTime;

            stats.Caffeine.Modify(-stats.CaffeineDecayRate * dt);
            stats.Nicotine.Modify(-stats.NicotineDecayRate * dt);
            stats.Energy.Modify(-stats.EnergyDecayRate * dt);
            stats.Satiety.Modify(-stats.SatietyDecayRate * dt);
            stats.Alertness.Modify(-stats.EnergyDecayRate * 0.5f * dt);
            stats.Focus.Modify(-stats.EnergyDecayRate * 0.3f * dt);
            stats.Patience.Modify(-stats.PatienceDecayRate * dt);
        }
    }
}
