using System.Collections.Generic;
using Godot;
using KBTV.Callers;
using KBTV.Data;

namespace KBTV.Screening
{
    /// <summary>
    /// A caller property that can be revealed during screening.
    /// Contains the property value, revelation state/timing, and stat effects
    /// that preview what impact this property will have on Vern when the caller goes on-air.
    /// </summary>
    public partial class ScreenableProperty : Resource
    {
        // Identity
        [Export] private string _propertyKey;
        [Export] private string _displayName;

        // Value storage
        private object _value;
        [Export] private string _displayValue;

        // Revelation state
        [Export] private RevelationState _state;
        [Export] private float _elapsedTime;
        [Export] private float _revealDuration;

        // Stat effects (preview for when caller goes on-air)
        private List<StatModification> _statEffects;

        /// <summary>
        /// The property key identifier (e.g., "EmotionalState", "CurseRisk").
        /// </summary>
        public string PropertyKey => _propertyKey;

        /// <summary>
        /// Human-readable display name for the property.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// The actual value of this property (enum, string, etc.).
        /// </summary>
        public object Value => _value;

        /// <summary>
        /// Human-readable string representation of the value for UI display.
        /// </summary>
        public string DisplayValue => _displayValue;

        /// <summary>
        /// Current revelation state (Hidden, Revealing, Revealed).
        /// </summary>
        public RevelationState State => _state;

        /// <summary>
        /// Time elapsed since revelation started.
        /// </summary>
        public float ElapsedTime => _elapsedTime;

        /// <summary>
        /// Total time required to reveal this property.
        /// </summary>
        public float RevealDuration => _revealDuration;

        /// <summary>
        /// Progress of revelation (0.0 to 1.0).
        /// </summary>
        public float Progress => _revealDuration > 0 ? Mathf.Clamp(_elapsedTime / _revealDuration, 0f, 1f) : 1f;

        /// <summary>
        /// Whether this property has been fully revealed.
        /// </summary>
        public bool IsRevealed => _state == RevelationState.Revealed;

        /// <summary>
        /// Whether this property is currently being revealed.
        /// </summary>
        public bool IsRevealing => _state == RevelationState.Revealing;

        /// <summary>
        /// Stat effects that will be applied to Vern when this caller goes on-air.
        /// These are previewed to the player during screening.
        /// </summary>
        public IReadOnlyList<StatModification> StatEffects => _statEffects;

        /// <summary>
        /// Parameterless constructor for Godot serialization.
        /// </summary>
        public ScreenableProperty()
        {
            _statEffects = new List<StatModification>();
        }

        /// <summary>
        /// Create a new screenable property.
        /// </summary>
        /// <param name="propertyKey">The property identifier.</param>
        /// <param name="displayName">Human-readable name.</param>
        /// <param name="value">The actual value.</param>
        /// <param name="displayValue">Human-readable value string.</param>
        /// <param name="revealDuration">Time to reveal in seconds.</param>
        /// <param name="statEffects">Stat effects when caller goes on-air.</param>
        public ScreenableProperty(
            string propertyKey,
            string displayName,
            object value,
            string displayValue,
            float revealDuration,
            List<StatModification> statEffects = null)
        {
            _propertyKey = propertyKey;
            _displayName = displayName;
            _value = value;
            _displayValue = displayValue;
            _revealDuration = revealDuration;
            _state = RevelationState.Hidden;
            _elapsedTime = 0f;
            _statEffects = statEffects ?? new List<StatModification>();
        }

        /// <summary>
        /// Update the revelation progress.
        /// Call each frame while screening is active.
        /// </summary>
        /// <param name="deltaTime">Time since last update.</param>
        public void Update(float deltaTime)
        {
            if (_state == RevelationState.Hidden)
            {
                _state = RevelationState.Revealing;
            }

            if (_state == RevelationState.Revealing)
            {
                _elapsedTime += deltaTime;
                if (_elapsedTime >= _revealDuration)
                {
                    _state = RevelationState.Revealed;
                }
            }
        }

        /// <summary>
        /// Reset this property to hidden state.
        /// </summary>
        public void Reset()
        {
            _state = RevelationState.Hidden;
            _elapsedTime = 0f;
        }

        /// <summary>
        /// Get a formatted string showing the stat effects for UI display.
        /// Uses short stat codes (P for Patience, S for Spirit, etc.) with arrows.
        /// Positive effects are formatted for green display, negative for red.
        /// </summary>
        /// <returns>List of formatted effect strings.</returns>
        public List<StatEffectDisplay> GetStatEffectDisplays()
        {
            var displays = new List<StatEffectDisplay>();

            foreach (var effect in _statEffects)
            {
                var code = GetStatCode(effect.StatType);
                var arrow = effect.Amount >= 0 ? "\u2191" : "\u2193"; // ↑ or ↓
                var isPositive = effect.Amount >= 0;
                var text = $"{code}{arrow}";

                displays.Add(new StatEffectDisplay(text, isPositive, effect.StatType, effect.Amount));
            }

            return displays;
        }

        /// <summary>
        /// Get short code for a stat type.
        /// </summary>
        private static string GetStatCode(StatType statType)
        {
            return statType switch
            {
                StatType.Patience => "P",
                StatType.Spirit => "S",
                StatType.Energy => "E",
                StatType.Focus => "F",
                StatType.Discernment => "D",
                StatType.Belief => "B",
                StatType.Alertness => "A",
                StatType.Caffeine => "Ca",
                StatType.Nicotine => "N",
                StatType.Satiety => "Sa",
                _ => "?"
            };
        }
    }

    /// <summary>
    /// Display information for a single stat effect.
    /// Used by UI to render colored stat indicators.
    /// </summary>
    public readonly struct StatEffectDisplay
    {
        /// <summary>
        /// Formatted text (e.g., "P↑", "S↓").
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Whether this is a positive effect (for green/red coloring).
        /// </summary>
        public bool IsPositive { get; }

        /// <summary>
        /// The stat type affected.
        /// </summary>
        public StatType StatType { get; }

        /// <summary>
        /// The amount of the effect.
        /// </summary>
        public float Amount { get; }

        public StatEffectDisplay(string text, bool isPositive, StatType statType, float amount)
        {
            Text = text;
            IsPositive = isPositive;
            StatType = statType;
            Amount = amount;
        }
    }
}
