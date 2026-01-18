using System;
using System.Collections.Generic;
using Godot;
using KBTV.Dialogue;

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
        /// <summary>
        /// Tracks the revelation progress of a single property.
        /// </summary>
        [Serializable]
        public partial class PropertyRevelation : Resource
        {
            [Export] public string PropertyKey;
            [Export] public RevelationState State;
            [Export] public float ElapsedTime;
            [Export] public float RevealDuration;
            [Export] public int IntValue;
            [Export] public string StringValue;

            public PropertyRevelation(string propertyName, float revealDuration)
            {
                PropertyKey = propertyName;
                State = RevelationState.Hidden;
                ElapsedTime = 0f;
                RevealDuration = revealDuration;
                IntValue = 0;
                StringValue = null;
            }

            public bool IsRevealed => State == RevelationState.Revealed;

            public void Update(float deltaTime)
            {
                if (State == RevelationState.Hidden)
                {
                    State = RevelationState.Revealing;
                }

                if (State == RevelationState.Revealing)
                {
                    ElapsedTime += deltaTime;
                    if (ElapsedTime >= RevealDuration)
                    {
                        State = RevelationState.Revealed;
                    }
                }
            }

            public float Progress => Mathf.Clamp(ElapsedTime / RevealDuration, 0f, 1f);
        }

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
        public float WaitTime { get; private set; }

        /// <summary>
        /// Properties being revealed during screening.
        /// </summary>
        public PropertyRevelation[] Revelations { get; private set; }

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
            InitializeRevelations();
        }

        /// <summary>
        /// Initialize revelation tracking for all screening properties.
        /// Properties are assigned to tiers for weighted random ordering.
        /// </summary>
        private void InitializeRevelations()
        {
            Revelations = new PropertyRevelation[]
            {
                new PropertyRevelation("AudioQuality", 2f),
                new PropertyRevelation("EmotionalState", 3f),
                new PropertyRevelation("CurseRisk", 3f),
                new PropertyRevelation("BeliefLevel", 4f),
                new PropertyRevelation("Evidence", 4f),
                new PropertyRevelation("Coherence", 5f),
                new PropertyRevelation("Urgency", 4f),
                new PropertyRevelation("Summary", 4f),
                new PropertyRevelation("Topic", 5f),
                new PropertyRevelation("Legitimacy", 5f),
                new PropertyRevelation("Personality", 4f)
            };

            ShuffleRevelations();
        }

        /// <summary>
        /// Shuffle revelations within weight tiers for random but weighted ordering.
        /// </summary>
        private void ShuffleRevelations()
        {
            var tier1 = new List<PropertyRevelation>();
            var tier2 = new List<PropertyRevelation>();
            var tier3 = new List<PropertyRevelation>();

            foreach (var rev in Revelations)
            {
                switch (rev.PropertyKey)
                {
                    case "AudioQuality":
                    case "EmotionalState":
                    case "CurseRisk":
                        tier1.Add(rev);
                        break;
                    case "BeliefLevel":
                    case "Evidence":
                    case "Coherence":
                    case "Urgency":
                        tier2.Add(rev);
                        break;
                    case "Summary":
                    case "Topic":
                    case "Legitimacy":
                    case "Personality":
                        tier3.Add(rev);
                        break;
                }
            }

            var rng = new Random();
            ShuffleList(tier1, rng);
            ShuffleList(tier2, rng);
            ShuffleList(tier3, rng);

            var shuffled = new List<PropertyRevelation>();
            shuffled.AddRange(tier1);
            shuffled.AddRange(tier2);
            shuffled.AddRange(tier3);
            Revelations = shuffled.ToArray();
        }

        private void ShuffleList<T>(List<T> list, Random rng)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
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
        /// Get a revelation by property name.
        /// </summary>
        public PropertyRevelation GetRevelation(string propertyName)
        {
            foreach (var rev in Revelations)
            {
                if (rev.PropertyKey == propertyName)
                    return rev;
            }
            return null;
        }

        /// <summary>
        /// Get all currently revealed properties.
        /// </summary>
        public List<PropertyRevelation> GetRevealedProperties()
        {
            var revealed = new List<PropertyRevelation>();
            foreach (var rev in Revelations)
            {
                if (rev.IsRevealed)
                    revealed.Add(rev);
            }
            return revealed;
        }

        /// <summary>
        /// Get the next property that will reveal (the first hidden one).
        /// </summary>
        public PropertyRevelation GetNextRevelation()
        {
            foreach (var rev in Revelations)
            {
                if (!rev.IsRevealed)
                    return rev;
            }
            return null;
        }

        /// <summary>
        /// Reset all revelations to hidden state.
        /// Used when starting a new screening session.
        /// </summary>
        public void ResetRevelations()
        {
            foreach (var rev in Revelations)
            {
                rev.State = RevelationState.Hidden;
                rev.ElapsedTime = 0f;
            }
        }

        /// <summary>
        /// Update the caller's wait time. Returns true if caller disconnected.
        /// </summary>
        public bool UpdateWaitTime(float deltaTime)
        {
            if (_state == CallerState.Completed ||
                _state == CallerState.Rejected ||
                _state == CallerState.Disconnected ||
                _state == CallerState.OnAir ||
                _state == CallerState.OnHold)
            {
                return false;
            }

            WaitTime += deltaTime;

            float patienceCheck = _patience;
            if (_state == CallerState.Screening)
            {
                patienceCheck = ScreeningPatience;
                ScreeningPatience -= deltaTime * 0.5f;

                UpdateRevelations(deltaTime);

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
        /// Update all property revelations.
        /// </summary>
        public void UpdateRevelations(float deltaTime)
        {
            foreach (var rev in Revelations)
            {
                rev.Update(deltaTime);
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
    }
}
