using System;
using UnityEngine;

namespace KBTV.Data
{
    /// <summary>
    /// Vern Tell's stat meters - Sims-style needs that affect show quality.
    /// </summary>
    [CreateAssetMenu(fileName = "VernStats", menuName = "KBTV/Vern Stats")]
    public class VernStats : ScriptableObject
    {
        [Header("Basic Needs")]
        [SerializeField] private float _initialMood = 50f;
        [SerializeField] private float _initialEnergy = 75f;
        [SerializeField] private float _initialHunger = 30f;
        [SerializeField] private float _initialThirst = 30f;

        [Header("Personality")]
        [SerializeField] private float _initialPatience = 50f;
        [SerializeField] private float _initialSusceptibility = 50f;

        [Header("Core Meter")]
        [SerializeField] private float _initialBelief = 25f;

        // Runtime stat instances
        private Stat _mood;
        private Stat _energy;
        private Stat _hunger;
        private Stat _thirst;
        private Stat _patience;
        private Stat _susceptibility;
        private Stat _belief;

        public Stat Mood => _mood;
        public Stat Energy => _energy;
        public Stat Hunger => _hunger;
        public Stat Thirst => _thirst;
        public Stat Patience => _patience;
        public Stat Susceptibility => _susceptibility;
        public Stat Belief => _belief;

        /// <summary>
        /// Event fired when any stat changes. Useful for UI updates.
        /// </summary>
        public event Action OnStatsChanged;

        /// <summary>
        /// Initialize runtime stats. Call this when starting a new game/night.
        /// Only creates Stat objects on first call; subsequent calls reset values.
        /// </summary>
        public void Initialize()
        {
            if (_mood == null)
            {
                // First-time initialization - create Stat objects
                _mood = new Stat("Mood", _initialMood);
                _energy = new Stat("Energy", _initialEnergy);
                _hunger = new Stat("Hunger", _initialHunger);
                _thirst = new Stat("Thirst", _initialThirst);
                _patience = new Stat("Patience", _initialPatience);
                _susceptibility = new Stat("Susceptibility", _initialSusceptibility);
                _belief = new Stat("Belief", _initialBelief);

                // Subscribe to individual stat changes
                _mood.OnValueChanged += NotifyStatsChanged;
                _energy.OnValueChanged += NotifyStatsChanged;
                _hunger.OnValueChanged += NotifyStatsChanged;
                _thirst.OnValueChanged += NotifyStatsChanged;
                _patience.OnValueChanged += NotifyStatsChanged;
                _susceptibility.OnValueChanged += NotifyStatsChanged;
                _belief.OnValueChanged += NotifyStatsChanged;
                
                Debug.Log("VernStats: Initialized new stat objects");
            }
            else
            {
                // Subsequent calls - just reset values (preserves subscriptions)
                _mood.Reset(_initialMood);
                _energy.Reset(_initialEnergy);
                _hunger.Reset(_initialHunger);
                _thirst.Reset(_initialThirst);
                _patience.Reset(_initialPatience);
                _susceptibility.Reset(_initialSusceptibility);
                _belief.Reset(_initialBelief);
                
                Debug.Log("VernStats: Reset stat values to initial");
            }
            
            OnStatsChanged?.Invoke();
        }

        private void NotifyStatsChanged(float oldVal, float newVal)
        {
            OnStatsChanged?.Invoke();
        }

        /// <summary>
        /// Calculate overall show quality based on stats.
        /// Higher belief + good mood/energy = better show.
        /// </summary>
        public float CalculateShowQuality()
        {
            // Belief is the primary driver
            float beliefWeight = 0.4f;
            float moodWeight = 0.25f;
            float energyWeight = 0.2f;
            float patienceWeight = 0.15f;

            // Hunger and thirst negatively impact if too high
            float needsPenalty = ((_hunger.Value + _thirst.Value) / 200f) * 0.2f;

            float quality = (_belief.Normalized * beliefWeight)
                          + (_mood.Normalized * moodWeight)
                          + (_energy.Normalized * energyWeight)
                          + (_patience.Normalized * patienceWeight)
                          - needsPenalty;

            return Mathf.Clamp01(quality);
        }

        /// <summary>
        /// Apply time-based decay to stats during live show.
        /// </summary>
        public void ApplyDecay(float deltaTime, float decayMultiplier = 1f)
        {
            // Energy drains over time
            _energy.Modify(-1f * deltaTime * decayMultiplier);

            // Hunger and thirst increase over time
            _hunger.Modify(0.5f * deltaTime * decayMultiplier);
            _thirst.Modify(0.75f * deltaTime * decayMultiplier);

            // Mood slowly decreases if needs are high
            if (_hunger.Value > 70f || _thirst.Value > 70f)
            {
                _mood.Modify(-0.5f * deltaTime * decayMultiplier);
            }

            // Patience decreases slightly over time
            _patience.Modify(-0.25f * deltaTime * decayMultiplier);
        }
    }
}
