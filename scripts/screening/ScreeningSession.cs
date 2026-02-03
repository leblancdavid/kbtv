using System.Collections.Generic;
using System.Linq;
using KBTV.Callers;
using KBTV.Data;

namespace KBTV.Screening
{
    /// <summary>
    /// Manages screening state for a single caller.
    /// Tracks property revelations, patience, and phase transitions.
    /// </summary>
    public class ScreeningSession
    {
        public Caller Caller { get; }
        public float ScreeningPatience { get; private set; }
        public float MaxPatience { get; }
        public float ElapsedTime { get; private set; }
        public int PropertiesRevealed => Caller.GetRevealedProperties().Count;
        public int TotalProperties => Caller.ScreenableProperties?.Length ?? 0;

        public ScreeningSession(Caller caller)
        {
            Caller = caller ?? throw new System.ArgumentNullException(nameof(caller));
            ScreeningPatience = caller.Patience;
            MaxPatience = caller.Patience;
            ElapsedTime = 0f;
            // Note: We intentionally do NOT reset screenable properties here.
            // This allows reveal progress to persist when switching between callers.
            // Properties are only reset when a caller is removed (rejected/disconnected).
        }

        public void Update(float deltaTime)
        {
            ElapsedTime += deltaTime;

            if (Caller.State == CallerState.Screening)
            {
                ScreeningPatience -= deltaTime * 0.5f;
                ScreeningPatience = System.Math.Max(0, ScreeningPatience);
                Caller.UpdateScreenableProperties(deltaTime);
            }
        }

        public bool HasPatience => ScreeningPatience > 0;
        public float PatienceRemaining => ScreeningPatience;
        public float Progress => MaxPatience > 0 ? ScreeningPatience / MaxPatience : 0f;
        public float RevelationProgress => TotalProperties > 0 ? (float)PropertiesRevealed / TotalProperties : 0f;

        /// <summary>
        /// Get the aggregated stat effects from all revealed properties so far.
        /// </summary>
        public Dictionary<StatType, float> GetRevealedStatEffects()
        {
            return Caller.GetRevealedStatEffects();
        }

        public void Reset()
        {
            ScreeningPatience = MaxPatience;
            ElapsedTime = 0f;
            Caller.ResetScreenableProperties();
        }
    }
}
