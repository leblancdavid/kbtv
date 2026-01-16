using System.Collections.Generic;
using System.Linq;
using KBTV.Callers;

namespace KBTV.Screening
{
    /// <summary>
    /// Manages screening state for a single caller.
    /// Tracks revelations, patience, and phase transitions.
    /// </summary>
    public class ScreeningSession
    {
        public Caller Caller { get; }
        public float ScreeningPatience { get; private set; }
        public float MaxPatience { get; }
        public float ElapsedTime { get; private set; }
        public int PropertiesRevealed => Caller.GetRevealedProperties().Count;
        public int TotalProperties => Caller.Revelations?.Length ?? 0;

        public ScreeningSession(Caller caller)
        {
            Caller = caller ?? throw new System.ArgumentNullException(nameof(caller));
            ScreeningPatience = caller.Patience;
            MaxPatience = caller.Patience;
            ElapsedTime = 0f;

            Caller.ResetRevelations();
        }

        public void Update(float deltaTime)
        {
            ElapsedTime += deltaTime;

            if (Caller.State == CallerState.Screening)
            {
                ScreeningPatience -= deltaTime * 0.5f;
                ScreeningPatience = System.Math.Max(0, ScreeningPatience);
                Caller.UpdateRevelations(deltaTime);
            }
        }

        public bool HasPatience => ScreeningPatience > 0;
        public float PatienceRemaining => ScreeningPatience;
        public float Progress => MaxPatience > 0 ? ScreeningPatience / MaxPatience : 0f;
        public float RevelationProgress => TotalProperties > 0 ? (float)PropertiesRevealed / TotalProperties : 0f;

        public void Reset()
        {
            ScreeningPatience = MaxPatience;
            ElapsedTime = 0f;
            Caller.ResetRevelations();
        }
    }
}
