using System;
using System.Collections.Generic;
using Godot;
using KBTV.Data;
using KBTV.Dialogue;
using KBTV.Screening;

namespace KBTV.Callers
{
    /// <summary>
    /// Represents a caller in the radio show system.
    /// Contains all properties and behaviors for individual callers.
    /// Enums are defined in CallerEnums.cs for organization.
    /// </summary>
    [Serializable]
    public partial class Caller : Resource
    {
        // Identity
        private string _name;
        private string _phoneNumber;
        private string _location;

        // Call Info
        private string _claimedTopic;
        private string _actualTopic;
        private string _callReason;

        // Attributes
        private CallerLegitimacy _legitimacy;
        private CallerPhoneQuality _phoneQuality;
        private CallerEmotionalState _emotionalState;
        private CallerCurseRisk _curseRisk;
        private CallerBeliefLevel _beliefLevel;
        private CallerEvidenceLevel _evidenceLevel;
        private CallerCoherence _coherence;
        private CallerUrgency _urgency;
        private string _personality;
        private string _arcId;
        private ConversationArc? _arc;
        private ConversationArc? _claimedArc;
        private ConversationArc? _actualArc;
        private string _screeningSummary;
        private CallerState _state;
        private float _patience;
        private float _quality;

        // Reveal order tracking (random sequence for property revelation)
        private int[] _revealOrder;
        private int _currentRevealIndex;

        // Properties
        public string Name => _name;
        public string PhoneNumber => _phoneNumber;
        public string Location => _location;
        public string ClaimedTopic => _claimedTopic;
        public string ActualTopic => _actualTopic;
        public string CallReason => _callReason;
        public CallerLegitimacy Legitimacy => _legitimacy;
        public CallerPhoneQuality PhoneQuality => _phoneQuality;
        public CallerEmotionalState EmotionalState => _emotionalState;
        public CallerCurseRisk CurseRisk => _curseRisk;
        public CallerBeliefLevel BeliefLevel => _beliefLevel;
        public CallerEvidenceLevel EvidenceLevel => _evidenceLevel;
        public CallerCoherence Coherence => _coherence;
        public CallerUrgency Urgency => _urgency;
        public string Personality => _personality;
        public string ArcId => _arcId;
        public ConversationArc? Arc => _arc;
        public ConversationArc? ClaimedArc => _claimedArc;
        public ConversationArc? ActualArc => _actualArc;
        public string ScreeningSummary => _screeningSummary;
        public CallerState State => _state;
        public float Patience => _patience;
        public float Quality => _quality;

        /// <summary>
        /// Gets the audio level modifier based on phone quality.
        /// Applied to the player's equipment level to get effective audio quality.
        /// </summary>
        public int PhoneQualityModifier => _phoneQuality switch
        {
            CallerPhoneQuality.Terrible => -2,
            CallerPhoneQuality.Poor => -1,
            CallerPhoneQuality.Average => 0,
            CallerPhoneQuality.Good => 1,
            _ => 0
        };

        /// <summary>
        /// Whether the caller is lying about their topic.
        /// </summary>
        public bool IsLyingAboutTopic => _claimedTopic != _actualTopic;

        /// <summary>
        /// Unique ID for this caller instance.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Time this caller has been waiting (in seconds).
        /// </summary>
        public float WaitTime { get; internal set; }

        /// <summary>
        /// Properties that can be revealed during screening.
        /// Each property has a value, revelation state, and stat effects preview.
        /// </summary>
        public ScreenableProperty[] ScreenableProperties { get; private set; }

        /// <summary>
        /// Whether the caller is off-topic (actual topic differs from show topic).
        /// </summary>
        public bool IsOffTopic { get; private set; }

        /// <summary>
        /// Screening patience - drains at 50% rate during active screening.
        /// </summary>
        public float ScreeningPatience { get; private set; }

        public event Action<CallerState, CallerState> OnStateChanged;
        public event Action OnDisconnected;

        public Caller(string name, string phoneNumber, string location,
            string claimedTopic, string actualTopic, string callReason,
            CallerLegitimacy legitimacy, CallerPhoneQuality phoneQuality,
            CallerEmotionalState emotionalState, CallerCurseRisk curseRisk,
            CallerBeliefLevel beliefLevel, CallerEvidenceLevel evidenceLevel,
            CallerCoherence coherence, CallerUrgency urgency,
            string personality, ConversationArc? claimedArc, ConversationArc? actualArc,
            string screeningSummary, float patience, float quality)
        {
            Id = Guid.NewGuid().ToString();
            _name = name;
            _phoneNumber = phoneNumber;
            _location = location;
            _claimedTopic = claimedTopic;
            _actualTopic = actualTopic;
            _callReason = callReason;
            _legitimacy = legitimacy;
            _phoneQuality = phoneQuality;
            _emotionalState = emotionalState;
            _curseRisk = curseRisk;
            _beliefLevel = beliefLevel;
            _evidenceLevel = evidenceLevel;
            _coherence = coherence;
            _urgency = urgency;
            _personality = personality;
            _claimedArc = claimedArc;
            _actualArc = actualArc;
            _arc = actualArc;  // Default to actual arc
            _arcId = actualArc?.ArcId ?? "";
            _screeningSummary = screeningSummary;
            _patience = patience;
            ScreeningPatience = patience;
            _quality = quality;
            _state = CallerState.Incoming;
            WaitTime = 0f;
            IsOffTopic = false;
            InitializeScreenableProperties();
        }

        /// <summary>
        /// Initialize screenable properties for all caller attributes.
        /// Properties are in a fixed display order, but reveal in random sequence.
        /// </summary>
        private void InitializeScreenableProperties()
        {
            var properties = new List<ScreenableProperty>
            {
                // Priority properties (shown first in display order)
                CreateScreenableProperty("Topic", "Topic", _claimedTopic, 5f),
                CreateScreenableProperty("Summary", "Summary", _screeningSummary, 4f),
                CreateScreenableProperty("AudioQuality", "Audio Quality", _phoneQuality, 2f),
                CreateScreenableProperty("Legitimacy", "Legitimacy", _legitimacy, 5f),
                CreateScreenableProperty("Personality", "Personality", _personality, 4f),
                // Remaining properties
                CreateScreenableProperty("EmotionalState", "Emotional State", _emotionalState, 3f),
                CreateScreenableProperty("CurseRisk", "Curse Risk", _curseRisk, 3f),
                CreateScreenableProperty("BeliefLevel", "Belief Level", _beliefLevel, 4f),
                CreateScreenableProperty("Evidence", "Evidence", _evidenceLevel, 4f),
                CreateScreenableProperty("Coherence", "Coherence", _coherence, 5f),
                CreateScreenableProperty("Urgency", "Urgency", _urgency, 4f)
            };

            // No shuffle - properties stay in fixed display order
            ScreenableProperties = properties.ToArray();
            
            // Initialize random reveal order
            InitializeRevealOrder();
        }

        /// <summary>
        /// Create a screenable property with appropriate display value and stat effects.
        /// </summary>
        private ScreenableProperty CreateScreenableProperty(string key, string displayName, object value, float revealDuration)
        {
            var displayValue = GetDisplayValue(key, value);
            var statEffects = CallerStatEffects.GetStatEffects(key, value);

            return new ScreenableProperty(key, displayName, value, displayValue, revealDuration, statEffects);
        }

        /// <summary>
        /// Get a human-readable display value for a property.
        /// </summary>
        private string GetDisplayValue(string key, object value)
        {
            if (value == null)
            {
                return "Unknown";
            }

            // For enums, convert to readable format
            if (value is Enum enumValue)
            {
                return FormatEnumValue(enumValue.ToString());
            }

            // For strings, return as-is
            return value.ToString();
        }

        /// <summary>
        /// Format an enum value for display (e.g., "VeryHigh" -> "Very High").
        /// </summary>
        private string FormatEnumValue(string enumValue)
        {
            // Insert spaces before capital letters (except the first)
            var result = new System.Text.StringBuilder();
            foreach (char c in enumValue)
            {
                if (char.IsUpper(c) && result.Length > 0)
                {
                    result.Append(' ');
                }
                result.Append(c);
            }
            return result.ToString();
        }

        /// <summary>
        /// Initialize the random reveal order.
        /// Creates a shuffled array of indices determining which property reveals next.
        /// </summary>
        private void InitializeRevealOrder()
        {
            int count = ScreenableProperties.Length;
            _revealOrder = new int[count];
            
            // Initialize with sequential indices
            for (int i = 0; i < count; i++)
            {
                _revealOrder[i] = i;
            }
            
            // Fisher-Yates shuffle
            var rng = new Random();
            for (int i = count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int temp = _revealOrder[i];
                _revealOrder[i] = _revealOrder[j];
                _revealOrder[j] = temp;
            }
            
            _currentRevealIndex = 0;
        }

        /// <summary>
        /// Set whether this caller is off-topic for the current show.
        /// </summary>
        public void SetOffTopic(bool isOffTopic)
        {
            IsOffTopic = isOffTopic;
        }

        /// <summary>
        /// Set the conversation arc for this caller.
        /// Loaded when caller is approved to on-hold.
        /// </summary>
        public void SetArc(ConversationArc arc)
        {
            _arc = arc;
            _arcId = arc?.ArcId ?? "";
        }

        /// <summary>
        /// Get a screenable property by property key.
        /// </summary>
        public ScreenableProperty GetScreenableProperty(string propertyKey)
        {
            foreach (var prop in ScreenableProperties)
            {
                if (prop.PropertyKey == propertyKey)
                    return prop;
            }
            return null;
        }

        /// <summary>
        /// Get all currently revealed properties.
        /// </summary>
        public List<ScreenableProperty> GetRevealedProperties()
        {
            var revealed = new List<ScreenableProperty>();
            foreach (var prop in ScreenableProperties)
            {
                if (prop.IsRevealed)
                    revealed.Add(prop);
            }
            return revealed;
        }

        /// <summary>
        /// Get the next property that will be revealed (the first hidden one in order).
        /// </summary>
        public ScreenableProperty GetNextPropertyToReveal()
        {
            foreach (var prop in ScreenableProperties)
            {
                if (!prop.IsRevealed)
                    return prop;
            }
            return null;
        }

        /// <summary>
        /// Reset all screenable properties to hidden state.
        /// Used when starting a new screening session.
        /// </summary>
        public void ResetScreenableProperties()
        {
            foreach (var prop in ScreenableProperties)
            {
                prop.Reset();
            }
            
            // Re-shuffle reveal order for next screening
            InitializeRevealOrder();
        }

        /// <summary>
        /// Get the aggregated stat effects from all revealed properties.
        /// Effects for the same stat are added together.
        /// </summary>
        /// <returns>Dictionary of stat type to total effect amount.</returns>
        public Dictionary<StatType, float> GetRevealedStatEffects()
        {
            return CallerStatEffects.AggregateStatEffects(ScreenableProperties, revealedOnly: true);
        }

        /// <summary>
        /// Get the total stat effects from ALL properties (revealed and hidden).
        /// This represents the full impact when the caller goes on-air.
        /// </summary>
        /// <returns>Dictionary of stat type to total effect amount.</returns>
        public Dictionary<StatType, float> GetTotalStatEffects()
        {
            return CallerStatEffects.AggregateStatEffects(ScreenableProperties, revealedOnly: false);
        }

        /// <summary>
        /// Update the caller's wait time. Returns true if caller disconnected.
        /// </summary>
        public bool UpdateWaitTime(float deltaTime)
        {
            // Early return for states with no impatience
            if (_state == CallerState.Completed ||
                _state == CallerState.Rejected ||
                _state == CallerState.Disconnected ||
                _state == CallerState.OnAir)
            {
                // Debug: Log if OnAir caller is somehow being updated (shouldn't happen)
                if (_state == CallerState.OnAir)
                {
                    GD.Print($"WARNING: OnAir caller '{Name}' attempted patience update - this should not happen!");
                }
                return false;
            }

            // Special handling for OnHold: slow impatience rate
            if (_state == CallerState.OnHold)
            {
                WaitTime += deltaTime * 0.5f;  // Half speed impatience for queued callers
                if (WaitTime > _patience)
                {
                    SetState(CallerState.Disconnected);
                    OnDisconnected?.Invoke();
                    return true;
                }
                return false;
            }

            WaitTime += deltaTime;

            float patienceCheck = _patience;
            if (_state == CallerState.Screening)
            {
                patienceCheck = ScreeningPatience;
                ScreeningPatience -= deltaTime * 0.5f;

                UpdateScreenableProperties(deltaTime);

                if (ScreeningPatience <= 0f)
                {
                    SetState(CallerState.Disconnected);
                    OnDisconnected?.Invoke();
                    return true;
                }
            }
            else
            {
                if (WaitTime > _patience)
                {
                    SetState(CallerState.Disconnected);
                    OnDisconnected?.Invoke();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Update screenable property revelations.
        /// Only updates one property at a time based on random reveal order.
        /// </summary>
        public void UpdateScreenableProperties(float deltaTime)
        {
            // All properties revealed
            if (_revealOrder == null || _currentRevealIndex >= _revealOrder.Length)
                return;
            
            // Get the property at current reveal position
            int propertyIndex = _revealOrder[_currentRevealIndex];
            var currentProperty = ScreenableProperties[propertyIndex];
            
            // Update only this property
            currentProperty.Update(deltaTime);
            
            // If it just finished revealing, move to next in sequence
            if (currentProperty.IsRevealed)
            {
                _currentRevealIndex++;
            }
        }

        /// <summary>
        /// Change the caller's state.
        /// </summary>
        public void SetState(CallerState newState)
        {
            if (_state == newState) return;

            CallerState oldState = _state;
            _state = newState;
            OnStateChanged?.Invoke(oldState, newState);
        }

        /// <summary>
        /// Get a display string for the caller's info (what screener sees).
        /// </summary>
        public string GetScreeningInfo()
        {
            return $"Name: {_name}\n" +
                   $"Phone: {_phoneNumber}\n" +
                   $"Location: {_location}\n" +
                   $"Topic: {_claimedTopic}\n" +
                   $"Reason: {_callReason}";
        }

        /// <summary>
        /// Calculate how much this caller affects the show based on legitimacy and topic match.
        /// </summary>
        public float CalculateShowImpact(string currentTopic)
        {
            float impact = _quality;

            if (_actualTopic == currentTopic)
            {
                impact *= 1.5f;
            }
            else
            {
                impact *= 0.5f;
            }

            impact *= _legitimacy switch
            {
                CallerLegitimacy.Fake => -1f,
                CallerLegitimacy.Questionable => 0.25f,
                CallerLegitimacy.Credible => 1f,
                CallerLegitimacy.Compelling => 1.5f,
                _ => 1f
            };

            return impact;
        }

        #region Backwards Compatibility

        // These properties and methods provide backwards compatibility
        // with code that uses the old PropertyRevelation naming.
        // They delegate to the new ScreenableProperty system.

        /// <summary>
        /// Backwards compatibility: alias for ScreenableProperties.
        /// </summary>
        [Obsolete("Use ScreenableProperties instead")]
        public ScreenableProperty[] Revelations => ScreenableProperties;

        /// <summary>
        /// Backwards compatibility: alias for GetScreenableProperty.
        /// </summary>
        [Obsolete("Use GetScreenableProperty instead")]
        public ScreenableProperty GetRevelation(string propertyName) => GetScreenableProperty(propertyName);

        /// <summary>
        /// Backwards compatibility: alias for GetNextPropertyToReveal.
        /// </summary>
        [Obsolete("Use GetNextPropertyToReveal instead")]
        public ScreenableProperty GetNextRevelation() => GetNextPropertyToReveal();

        /// <summary>
        /// Backwards compatibility: alias for ResetScreenableProperties.
        /// </summary>
        [Obsolete("Use ResetScreenableProperties instead")]
        public void ResetRevelations() => ResetScreenableProperties();

        /// <summary>
        /// Backwards compatibility: alias for UpdateScreenableProperties.
        /// </summary>
        [Obsolete("Use UpdateScreenableProperties instead")]
        public void UpdateRevelations(float deltaTime) => UpdateScreenableProperties(deltaTime);

        #endregion
    }
}
