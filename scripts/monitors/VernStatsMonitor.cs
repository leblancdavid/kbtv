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
            
            GD.Print($"[DEBUG] VernStatsMonitor.OnResolved:");
            GD.Print($"  - GameStateManager: {GameStateManager != null}");
            GD.Print($"  - _vernStats: {_vernStats != null}");
            GD.Print($"  - _gameState: {_gameState != null}");
            if (_vernStats != null) {
                GD.Print($"  - Initial Caffeine: {_vernStats.Caffeine.Value}");
                GD.Print($"  - Initial Nicotine: {_vernStats.Nicotine.Value}");
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            GD.Print($"[DEBUG] VernStatsMonitor.OnUpdate: dt={deltaTime:F3}");
            
            if (_vernStats == null) {
                GD.Print($"[DEBUG] VernStatsMonitor: _vernStats is null!");
                return;
            }
            if (_gameState == null) {
                GD.Print($"[DEBUG] VernStatsMonitor: _gameState is null!");
                return;
            }
            
            bool isLive = _gameState.IsLive;
            GD.Print($"[DEBUG] VernStatsMonitor: IsLive = {isLive}, Phase = {_gameState.CurrentPhase}");
            
            if (!isLive) {
                GD.Print($"[DEBUG] VernStatsMonitor: Skipping decay - not live show");
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

            // Log current values and modifiers
            float caffeineMod = stats.GetCaffeineDecayModifier();
            float nicotineMod = stats.GetNicotineDecayModifier();
            float mental = stats.Mental.Value;
            float emotional = stats.Emotional.Value;
            
            GD.Print($"[DEBUG] VernStatsMonitor: Mental={mental:F1}, Emotional={emotional:F1}");
            GD.Print($"[DEBUG] VernStatsMonitor: CaffeineMod={caffeineMod:F2}, NicotineMod={nicotineMod:F2}");
            
            // Log before decay
            float oldCaffeine = stats.Caffeine.Value;
            float oldNicotine = stats.Nicotine.Value;
            
            GD.Print($"[DEBUG] VernStatsMonitor: Before decay - Caff:{oldCaffeine:F1}, Nico:{oldNicotine:F1}");

            stats.Caffeine.Modify(-caffeineDecay * dtMinutes);
            stats.Nicotine.Modify(-nicotineDecay * dtMinutes);

            // Log after decay
            float newCaffeine = stats.Caffeine.Value;  
            float newNicotine = stats.Nicotine.Value;
            
            GD.Print($"[DEBUG] VernStatsMonitor: After decay - Caff:{newCaffeine:F1}, Nico:{newNicotine:F1}");
            GD.Print($"[DEBUG] VernStatsMonitor: Change - Caff:{newCaffeine-oldCaffeine:F3}, Nico:{newNicotine-oldNicotine:F3}");

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
