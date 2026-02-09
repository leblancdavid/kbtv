using System.Collections.Generic;
using System.Linq;
using Godot;
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

        /// <summary>
        /// Whether evidence is available for collection from this caller.
        /// Determined by probability roll when Evidence property is revealed.
        /// </summary>
        public bool EvidenceAvailable { get; private set; }

        /// <summary>
        /// Whether evidence has been collected from this caller.
        /// </summary>
        public bool EvidenceCollected { get; private set; }

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
                
                // Store previous revealed count to detect new revelations
                int previousRevealedCount = PropertiesRevealed;
                
                Caller.UpdateScreenableProperties(deltaTime);
                
                // Check if any new properties were revealed this frame
                if (PropertiesRevealed > previousRevealedCount && !EvidenceAvailable)
                {
                    CheckForEvidenceRevelation();
                }
            }
        }

        /// <summary>
        /// Check if the Evidence property was just revealed and roll for evidence availability.
        /// </summary>
        /// <returns>True if evidence availability changed.</returns>
        private bool CheckForEvidenceRevelation()
        {
            var revealedProperties = Caller.GetRevealedProperties();
            bool hadEvidenceBefore = EvidenceAvailable;
            
            // Look for the Evidence property among newly revealed properties
            foreach (var property in revealedProperties)
            {
                if (property.PropertyKey == "Evidence" && !EvidenceAvailable)
                {
                    // Roll for evidence availability based on caller's evidence level
                    EvidenceAvailable = RollForEvidence(Caller.EvidenceLevel);
                    break;
                }
            }
            
            return EvidenceAvailable != hadEvidenceBefore;
        }

        /// <summary>
        /// Roll for evidence availability based on the caller's evidence level.
        /// </summary>
        /// <param name="level">The caller's evidence level.</param>
        /// <returns>True if evidence is available for collection.</returns>
        private bool RollForEvidence(CallerEvidenceLevel level)
        {
            // TEMPORARY: Set to 100% for testing evidence collection feature
            var probability = level switch
            {
                CallerEvidenceLevel.None => 1.0f,
                CallerEvidenceLevel.Low => 1.0f,
                CallerEvidenceLevel.Medium => 1.0f,
                CallerEvidenceLevel.High => 1.0f,
                CallerEvidenceLevel.Irrefutable => 1.0f,
                _ => 1.0f
            };

            return GD.Randf() < probability;
        }

        /// <summary>
        /// Mark evidence as collected from this caller.
        /// </summary>
        public void CollectEvidence()
        {
            if (EvidenceAvailable && !EvidenceCollected)
            {
                EvidenceCollected = true;
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
            EvidenceAvailable = false;
            EvidenceCollected = false;
            Caller.ResetScreenableProperties();
        }
    }
}
