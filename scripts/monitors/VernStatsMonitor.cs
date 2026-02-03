#nullable enable

using Godot;
using KBTV.Core;
using KBTV.Data;

namespace KBTV.Monitors
{
    /// <summary>
    /// Monitors Vern's stats and applies decay over time.
    /// 
    /// Decay Logic (v2):
    /// - Dependencies (Caffeine, Nicotine) decay continuously with stat modifiers
    /// - Core stats (Physical, Emotional, Mental) only decay when dependencies are depleted
    /// - Stat interactions: low stats accelerate other decays
    /// 
    /// Decay Modifiers:
    /// - Caffeine decay: Higher Mental → slower decay
    /// - Nicotine decay: Higher Emotional → slower decay
    /// - Mental < -25: +25% dependency decay
    /// 
    /// Withdrawal Effects (when dependency = 0):
    /// - Caffeine depleted: Physical -6/min, Mental -3/min
    /// - Nicotine depleted: Emotional -6/min, Mental -3/min
    /// 
    /// Stat Interactions:
    /// - Physical < -25: Mental decay +50%
    /// - Emotional < -25: Physical decay +50%
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
            float dtMinutes = deltaTime / 60f;  // Convert to minutes for decay rates

            // ═══════════════════════════════════════════════════════════════════════════════
            // DEPENDENCY DECAY
            // ═══════════════════════════════════════════════════════════════════════════════

            // Caffeine decay: Base rate modified by Mental stat
            float caffeineDecay = stats.CaffeineDecayRate * stats.GetCaffeineDecayModifier();
            
            // Nicotine decay: Base rate modified by Emotional stat  
            float nicotineDecay = stats.NicotineDecayRate * stats.GetNicotineDecayModifier();

            // Mental < -25 accelerates dependency decay by 25%
            if (stats.IsMentalCritical)
            {
                caffeineDecay *= stats.LowMentalDependencyMultiplier;
                nicotineDecay *= stats.LowMentalDependencyMultiplier;
            }

            stats.Caffeine.Modify(-caffeineDecay * dtMinutes);
            stats.Nicotine.Modify(-nicotineDecay * dtMinutes);

            // ═══════════════════════════════════════════════════════════════════════════════
            // CORE STAT DECAY (only when dependencies depleted)
            // ═══════════════════════════════════════════════════════════════════════════════

            float physicalDecay = 0f;
            float emotionalDecay = 0f;
            float mentalDecay = 0f;

            // Caffeine depleted: Physical and Mental decay
            if (stats.IsCaffeineDepleted)
            {
                physicalDecay += stats.PhysicalDecayRate;  // -6/min
                mentalDecay += stats.MentalDecayRate;       // -3/min
            }

            // Nicotine depleted: Emotional and Mental decay
            if (stats.IsNicotineDepleted)
            {
                emotionalDecay += stats.EmotionalDecayRate;  // -6/min
                mentalDecay += stats.MentalDecayRate;        // -3/min (stacks if both depleted)
            }

            // ═══════════════════════════════════════════════════════════════════════════════
            // STAT INTERACTION ACCELERATORS
            // ═══════════════════════════════════════════════════════════════════════════════

            // Physical < -25: Mental decay +50%
            if (stats.IsPhysicalCritical && mentalDecay > 0)
            {
                mentalDecay *= stats.LowStatDecayMultiplier;
            }

            // Emotional < -25: Physical decay +50%
            if (stats.IsEmotionalCritical && physicalDecay > 0)
            {
                physicalDecay *= stats.LowStatDecayMultiplier;
            }

            // Apply core stat decays
            if (physicalDecay > 0)
            {
                stats.Physical.Modify(-physicalDecay * dtMinutes);
            }

            if (emotionalDecay > 0)
            {
                stats.Emotional.Modify(-emotionalDecay * dtMinutes);
            }

            if (mentalDecay > 0)
            {
                stats.Mental.Modify(-mentalDecay * dtMinutes);
            }
        }
    }
}
