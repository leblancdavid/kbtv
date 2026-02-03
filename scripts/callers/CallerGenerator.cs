using System;
using System.Collections.Generic;
using Godot;
using KBTV.Core;
using KBTV.Callers;
using KBTV.Data;
using KBTV.Dialogue;

namespace KBTV.Callers
{
    /// <summary>
    /// Personality type determines whether the random stat effect is positive or negative.
    /// </summary>
    public enum PersonalityType
    {
        Positive,   // Boosts a random stat by 1-2
        Negative,   // Penalizes a random stat by 1-2
        Neutral     // 50/50 chance of positive or negative
    }

    /// <summary>
    /// Personality definition with display name and type.
    /// </summary>
    public readonly struct PersonalityDefinition
    {
        public string Name { get; }
        public PersonalityType Type { get; }

        public PersonalityDefinition(string name, PersonalityType type)
        {
            Name = name;
            Type = type;
        }
    }

    /// <summary>
    /// Generates random callers during the live show.
    /// Spawns a mix of legitimate, questionable, and fake callers.
    /// </summary>
	public partial class CallerGenerator : Node
	{
        [Export] private float _minSpawnInterval = 1f;
        [Export] private float _maxSpawnInterval = 3f;
        [Export] private float _basePatience = 60f;
        [Export] private float _patienceVariance = 10f;

        private float _nextSpawnTime;
        private bool _isGenerating;
        private readonly ICallerRepository _repository;
        private readonly IGameStateManager _gameState;
        private readonly IArcRepository _arcRepository;

        private bool _initialized;

        // Name and location data for caller generation
        private static readonly string[] FirstNames = {
            "John", "Mike", "Dave", "Steve", "Bob", "Tom", "Jim", "Bill",
            "Sarah", "Lisa", "Karen", "Mary", "Linda", "Susan", "Betty", "Helen",
            "Paul", "Mark", "George", "Kevin", "Brian", "Edward", "Ronald", "Timothy",
            "Sandra", "Donna", "Carol", "Ruth", "Sharon", "Michelle", "Laura", "Kimberly"
        };

        private static readonly string[] LastNames = {
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
            "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas",
            "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson", "White",
            "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson", "Walker", "Young"
        };

        private static readonly string[] Locations = {
            "Springfield", "Riverside", "Oakwood", "Maple Valley", "Pinebrook", "Cedar Hills",
            "Elmwood", "Willow Creek", "Birchwood", "Oakridge", "Pine Grove", "Cedar Valley",
            "Elm Creek", "Willow Grove", "Birch Creek", "Oak Creek", "Pine Valley", "Cedar Grove"
        };

        /// <summary>
        /// Personality definitions with type classification.
        /// Positive personalities boost a stat, negative penalize, neutral is 50/50.
        /// </summary>
        private static readonly PersonalityDefinition[] Personalities = {
            // Positive personalities (12) - boost a random stat
            new("Matter-of-fact reporter", PersonalityType.Positive),
            new("Academic researcher", PersonalityType.Positive),
            new("Local history buff", PersonalityType.Positive),
            new("Frequent listener", PersonalityType.Positive),
            new("Genuinely frightened", PersonalityType.Positive),
            new("True believer", PersonalityType.Positive),
            new("Retired professional", PersonalityType.Positive),
            new("Careful observer", PersonalityType.Positive),
            new("Soft-spoken witness", PersonalityType.Positive),
            new("Articulate storyteller", PersonalityType.Positive),
            new("Patient explainer", PersonalityType.Positive),
            new("Earnest truth-seeker", PersonalityType.Positive),

            // Negative personalities (12) - penalize a random stat
            new("Attention seeker", PersonalityType.Negative),
            new("Conspiracy theorist", PersonalityType.Negative),
            new("Rambling storyteller", PersonalityType.Negative),
            new("Joker type", PersonalityType.Negative),
            new("Monotone delivery", PersonalityType.Negative),
            new("Skeptical witness", PersonalityType.Negative),
            new("Know-it-all", PersonalityType.Negative),
            new("Chronic interrupter", PersonalityType.Negative),
            new("Drama queen", PersonalityType.Negative),
            new("Mumbling caller", PersonalityType.Negative),
            new("Easily distracted", PersonalityType.Negative),
            new("Defensive storyteller", PersonalityType.Negative),

            // Neutral personalities (12) - 50/50 positive or negative
            new("Nervous but sincere", PersonalityType.Neutral),
            new("Overly enthusiastic", PersonalityType.Neutral),
            new("First-time caller", PersonalityType.Neutral),
            new("Desperate for answers", PersonalityType.Neutral),
            new("Reluctant witness", PersonalityType.Neutral),
            new("Excitable narrator", PersonalityType.Neutral),
            new("Quiet observer", PersonalityType.Neutral),
            new("Chatty neighbor", PersonalityType.Neutral),
            new("Late-night insomniac", PersonalityType.Neutral),
            new("Curious skeptic", PersonalityType.Neutral),
            new("Nostalgic elder", PersonalityType.Neutral),
            new("Breathless reporter", PersonalityType.Neutral)
        };

        // PersonalityAffectedStats removed - personality effects are now deterministic
        // per-personality via PersonalityStatEffects class

        [Export] private float _fakeCallerChance = 0.15f;
        [Export] private float _questionableCallerChance = 0.25f;
        [Export] private float _compellingCallerChance = 0.1f;

        public CallerGenerator(ICallerRepository repository, IGameStateManager gameState, IArcRepository arcRepository)
        {
            _repository = repository;
            _gameState = gameState;
            _arcRepository = arcRepository;
        }

        public override void _Ready()
        {
            // RegisterSelf removed - now using dependency injection
            CompleteInitialization();
        }

        /// <summary>
        /// Initialize the CallerGenerator service.
        /// Called by ServiceProviderRoot after dependencies are resolved.
        /// </summary>
        public void Initialize()
        {
            // Initialization already handled in _Ready()
            if (!_initialized)
            {
                GD.Print("CallerGenerator: Initialize called (already initialized in _Ready)");
                _initialized = true;
            }
        }

        private void CompleteInitialization()
        {
            if (_repository == null)
            {
                GD.PrintErr("CallerGenerator: ICallerRepository not available after all services ready");
                return;
            }

            if (_gameState == null)
            {
                GD.PrintErr("CallerGenerator: GameStateManager not available after all services ready");
                return;
            }

            _gameState.OnPhaseChanged += HandlePhaseChanged;

            if (_gameState.CurrentPhase == GamePhase.LiveShow)
            {
                StartGenerating();
            }

            GD.Print("CallerGenerator: Initialization complete");
            _initialized = true;
        }

        public override void _ExitTree()
        {
            if (_gameState != null)
            {
                _gameState.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        public override void _Process(double delta)
        {
            if (!_isGenerating) return;

            if (Time.GetTicksMsec() / 1000.0f >= _nextSpawnTime) // Proper time check
            {
                TrySpawnCaller();
                ScheduleNextSpawn();
            }
        }

        /// <summary>
        /// Start generating callers (called when live show starts).
        /// </summary>
        public void StartGenerating()
        {
            _isGenerating = true;
            ScheduleNextSpawn();
            // GD.Print("CallerGenerator: Started generating callers");
        }

        /// <summary>
        /// Stop generating callers (called when live show ends).
        /// </summary>
        public void StopGenerating()
        {
            _isGenerating = false;
            // GD.Print("CallerGenerator: Stopped generating callers");
        }

        /// <summary>
        /// Manually spawn a caller (for testing).
        /// </summary>
        public Caller SpawnCaller()
        {
            return TrySpawnCaller();
        }

#if DEBUG
        /// <summary>
        /// Generate a caller without repository registration (for testing only).
        /// </summary>
        public Caller GenerateTestCaller()
        {
            return GenerateRandomCaller();
        }
#endif

        private Caller TrySpawnCaller()
        {
            if (_repository == null || !_repository.CanAcceptMoreCallers)
            {
                return null;
            }

            Caller caller = GenerateRandomCaller();
            var result = _repository.AddCaller(caller);
            if (result.IsSuccess)
            {
                // GD.Print($"CallerGenerator: Generated caller {caller.Name}");
                return caller;
            }
            return null;
        }

        private void ScheduleNextSpawn()
        {
            float interval = (float)GD.RandRange(_minSpawnInterval, _maxSpawnInterval);
            _nextSpawnTime = Time.GetTicksMsec() / 1000.0f + interval;
        }

        private Caller GenerateRandomCaller()
        {
            CallerLegitimacy legitimacy = DetermineRandomLegitimacy();

            string firstName = FirstNames[(int)(GD.Randi() % FirstNames.Length)];
            string lastName = LastNames[(int)(GD.Randi() % LastNames.Length)];
            string name = $"{firstName} {lastName}";

            string areaCode = "555"; // Simple placeholder
            string phoneNumber = $"{areaCode}-{GD.Randi() % 900 + 100}-{GD.Randi() % 9000 + 1000}";

            string location = Locations[(int)(GD.Randi() % Locations.Length)];

            string reason = GenerateCallReason(legitimacy);

            // Generate attributes based on legitimacy
            CallerPhoneQuality phoneQuality = GeneratePhoneQuality(legitimacy);
            CallerEmotionalState emotionalState = GenerateEmotionalState(legitimacy);
            CallerCurseRisk curseRisk = GenerateCurseRisk(legitimacy);
            CallerBeliefLevel beliefLevel = GenerateBeliefLevel(legitimacy);
            CallerEvidenceLevel evidenceLevel = GenerateEvidenceLevel(legitimacy);
            CallerCoherence coherence = GenerateCoherence(legitimacy);
            CallerUrgency urgency = GenerateUrgency(legitimacy);
            var personality = GeneratePersonality();
            // Personality effects are now deterministic per-personality via PersonalityStatEffects
            // No need for random generation - CallerStatEffects.GetStatEffects("Personality", name) handles it

            // Assign arcs based on show topic (90% on-topic, 10% off-topic)
            var arcRepo = _arcRepository;
            ConversationArc? claimedArc = null;
            ConversationArc? actualArc = null;
            string actualTopic;
            string claimedTopic;
            bool isOffTopic = false;

            var showTopic = _gameState?.SelectedTopic;

            if (showTopic != null && arcRepo != null)
            {
                ShowTopic showTopicValue = showTopic.TopicValue;

                // Use Topic.OffTopicRate for off-topic probability (default 0.1 = 10%)
                float offTopicChance = showTopic.OffTopicRate;
                bool generateOffTopic = (float)GD.Randf() < offTopicChance;

                if (generateOffTopic)
                {
                    // 10% off-topic: arc from different topic (transparent, not show topic)
                    actualArc = arcRepo.GetRandomArcForDifferentTopic(showTopicValue, legitimacy);
                    if (actualArc != null)
                    {
                        actualTopic = actualArc.TopicName;
                        claimedTopic = actualTopic;  // Transparent - claim actual topic
                        claimedArc = actualArc;
                        isOffTopic = actualArc.Topic != showTopicValue;
                    }
                    else
                    {
                        // Fallback: try any arc with matching legitimacy
                        actualArc = arcRepo.GetRandomArc(legitimacy);
                        if (actualArc != null)
                        {
                            actualTopic = actualArc.TopicName;
                            claimedTopic = actualTopic;
                            claimedArc = actualArc;
                            isOffTopic = actualArc.Topic != showTopicValue;
                        }
                        else
                        {
                            // Ultimate fallback: random ShowTopic enum value
                            var fallbackTopic = (ShowTopic)((int)(GD.Randi() % 4));
                            actualTopic = fallbackTopic.ToTopicName();
                            claimedTopic = actualTopic;
                            isOffTopic = fallbackTopic != showTopicValue;
                        }
                    }
                }
                else
                {
                    // 90% on-topic: arc matching show topic
                    actualArc = arcRepo.GetRandomArcForTopic(showTopicValue, legitimacy);
                    if (actualArc != null)
                    {
                        actualTopic = actualArc.TopicName;
                        claimedTopic = actualTopic;
                        claimedArc = actualArc;
                        isOffTopic = false;
                    }
                    else
                    {
                        // Fallback: try any arc with matching legitimacy
                        actualArc = arcRepo.GetRandomArc(legitimacy);
                        if (actualArc != null)
                        {
                            actualTopic = actualArc.TopicName;
                            claimedTopic = actualTopic;
                            claimedArc = actualArc;
                            isOffTopic = actualArc.Topic != showTopicValue;
                        }
                        else
                        {
                            // Ultimate fallback: use show topic
                            actualTopic = showTopicValue.ToTopicName();
                            claimedTopic = actualTopic;
                            isOffTopic = false;
                        }
                    }
                }
            }
            else
            {
                // No show topic selected - use random ShowTopic enum value
                var randomTopic = (ShowTopic)((int)(GD.Randi() % 4));
                actualTopic = randomTopic.ToTopicName();
                claimedTopic = actualTopic;
                isOffTopic = false;
            }

            string screeningSummary = actualArc?.ScreeningSummary ?? "Generated caller";

            float patience = _basePatience + (float)GD.RandRange(-_patienceVariance, _patienceVariance);
            float quality = 50f; // Base quality

            var caller = new Caller(name, phoneNumber, location,
                claimedTopic, actualTopic, reason,
                legitimacy, phoneQuality, emotionalState, curseRisk,
                beliefLevel, evidenceLevel, coherence, urgency,
                personality.Name, null, claimedArc, actualArc, screeningSummary, patience, quality);

            if (isOffTopic)
            {
                caller.SetOffTopic(true);
            }

            return caller;
        }

        private CallerLegitimacy DetermineRandomLegitimacy()
        {
            float rand = (float)GD.Randf();

            if (rand < _fakeCallerChance)
                return CallerLegitimacy.Fake;
            else if (rand < _fakeCallerChance + _questionableCallerChance)
                return CallerLegitimacy.Questionable;
            else if (rand < _fakeCallerChance + _questionableCallerChance + _compellingCallerChance)
                return CallerLegitimacy.Compelling;

            return CallerLegitimacy.Credible;
        }

        private string GenerateCallReason(CallerLegitimacy legitimacy)
        {
            switch (legitimacy)
            {
                case CallerLegitimacy.Fake:
                    return "Just messing around";
                case CallerLegitimacy.Questionable:
                    return "Heard something weird";
                case CallerLegitimacy.Credible:
                    return "Had a strange experience";
                case CallerLegitimacy.Compelling:
                    return "Need to talk to someone about this";
                default:
                    return "Calling about paranormal activity";
            }
        }

        #region Attribute Generation Methods

        /// <summary>
        /// Select a random value from weighted options.
        /// </summary>
        private T WeightedSelect<T>(params (T value, float weight)[] options)
        {
            float total = 0f;
            foreach (var option in options)
                total += option.weight;

            float rand = (float)GD.Randf() * total;
            float cumulative = 0f;

            foreach (var (value, weight) in options)
            {
                cumulative += weight;
                if (rand < cumulative)
                    return value;
            }

            return options[options.Length - 1].value;
        }

        private CallerPhoneQuality GeneratePhoneQuality(CallerLegitimacy legitimacy)
        {
            return legitimacy switch
            {
                CallerLegitimacy.Fake => WeightedSelect(
                    (CallerPhoneQuality.Terrible, 30f),
                    (CallerPhoneQuality.Poor, 40f),
                    (CallerPhoneQuality.Average, 25f),
                    (CallerPhoneQuality.Good, 5f)),
                CallerLegitimacy.Questionable => WeightedSelect(
                    (CallerPhoneQuality.Terrible, 15f),
                    (CallerPhoneQuality.Poor, 35f),
                    (CallerPhoneQuality.Average, 40f),
                    (CallerPhoneQuality.Good, 10f)),
                CallerLegitimacy.Credible => WeightedSelect(
                    (CallerPhoneQuality.Terrible, 5f),
                    (CallerPhoneQuality.Poor, 20f),
                    (CallerPhoneQuality.Average, 50f),
                    (CallerPhoneQuality.Good, 25f)),
                CallerLegitimacy.Compelling => WeightedSelect(
                    (CallerPhoneQuality.Terrible, 5f),
                    (CallerPhoneQuality.Poor, 15f),
                    (CallerPhoneQuality.Average, 45f),
                    (CallerPhoneQuality.Good, 35f)),
                _ => CallerPhoneQuality.Average
            };
        }

        private CallerEmotionalState GenerateEmotionalState(CallerLegitimacy legitimacy)
        {
            return legitimacy switch
            {
                CallerLegitimacy.Fake => WeightedSelect(
                    (CallerEmotionalState.Calm, 10f),
                    (CallerEmotionalState.Anxious, 10f),
                    (CallerEmotionalState.Excited, 40f),
                    (CallerEmotionalState.Scared, 10f),
                    (CallerEmotionalState.Angry, 30f)),
                CallerLegitimacy.Questionable => WeightedSelect(
                    (CallerEmotionalState.Calm, 25f),
                    (CallerEmotionalState.Anxious, 30f),
                    (CallerEmotionalState.Excited, 20f),
                    (CallerEmotionalState.Scared, 15f),
                    (CallerEmotionalState.Angry, 10f)),
                CallerLegitimacy.Credible => WeightedSelect(
                    (CallerEmotionalState.Calm, 40f),
                    (CallerEmotionalState.Anxious, 30f),
                    (CallerEmotionalState.Excited, 15f),
                    (CallerEmotionalState.Scared, 10f),
                    (CallerEmotionalState.Angry, 5f)),
                CallerLegitimacy.Compelling => WeightedSelect(
                    (CallerEmotionalState.Calm, 30f),
                    (CallerEmotionalState.Anxious, 25f),
                    (CallerEmotionalState.Excited, 10f),
                    (CallerEmotionalState.Scared, 30f),
                    (CallerEmotionalState.Angry, 5f)),
                _ => CallerEmotionalState.Calm
            };
        }

        private CallerCurseRisk GenerateCurseRisk(CallerLegitimacy legitimacy)
        {
            return legitimacy switch
            {
                CallerLegitimacy.Fake => WeightedSelect(
                    (CallerCurseRisk.Low, 20f),
                    (CallerCurseRisk.Medium, 30f),
                    (CallerCurseRisk.High, 50f)),
                CallerLegitimacy.Questionable => WeightedSelect(
                    (CallerCurseRisk.Low, 40f),
                    (CallerCurseRisk.Medium, 40f),
                    (CallerCurseRisk.High, 20f)),
                CallerLegitimacy.Credible => WeightedSelect(
                    (CallerCurseRisk.Low, 60f),
                    (CallerCurseRisk.Medium, 30f),
                    (CallerCurseRisk.High, 10f)),
                CallerLegitimacy.Compelling => WeightedSelect(
                    (CallerCurseRisk.Low, 80f),
                    (CallerCurseRisk.Medium, 15f),
                    (CallerCurseRisk.High, 5f)),
                _ => CallerCurseRisk.Low
            };
        }

        private CallerBeliefLevel GenerateBeliefLevel(CallerLegitimacy legitimacy)
        {
            return legitimacy switch
            {
                CallerLegitimacy.Fake => WeightedSelect(
                    (CallerBeliefLevel.Curious, 30f),
                    (CallerBeliefLevel.Partial, 10f),
                    (CallerBeliefLevel.Committed, 10f),
                    (CallerBeliefLevel.Certain, 10f),
                    (CallerBeliefLevel.Zealot, 40f)),
                CallerLegitimacy.Questionable => WeightedSelect(
                    (CallerBeliefLevel.Curious, 25f),
                    (CallerBeliefLevel.Partial, 35f),
                    (CallerBeliefLevel.Committed, 25f),
                    (CallerBeliefLevel.Certain, 10f),
                    (CallerBeliefLevel.Zealot, 5f)),
                CallerLegitimacy.Credible => WeightedSelect(
                    (CallerBeliefLevel.Curious, 30f),
                    (CallerBeliefLevel.Partial, 30f),
                    (CallerBeliefLevel.Committed, 25f),
                    (CallerBeliefLevel.Certain, 10f),
                    (CallerBeliefLevel.Zealot, 5f)),
                CallerLegitimacy.Compelling => WeightedSelect(
                    (CallerBeliefLevel.Curious, 10f),
                    (CallerBeliefLevel.Partial, 15f),
                    (CallerBeliefLevel.Committed, 35f),
                    (CallerBeliefLevel.Certain, 35f),
                    (CallerBeliefLevel.Zealot, 5f)),
                _ => CallerBeliefLevel.Curious
            };
        }

        private CallerEvidenceLevel GenerateEvidenceLevel(CallerLegitimacy legitimacy)
        {
            return legitimacy switch
            {
                // Fake callers never have real evidence
                CallerLegitimacy.Fake => CallerEvidenceLevel.None,
                CallerLegitimacy.Questionable => WeightedSelect(
                    (CallerEvidenceLevel.None, 50f),
                    (CallerEvidenceLevel.Low, 35f),
                    (CallerEvidenceLevel.Medium, 15f),
                    (CallerEvidenceLevel.High, 0f),
                    (CallerEvidenceLevel.Irrefutable, 0f)),
                CallerLegitimacy.Credible => WeightedSelect(
                    (CallerEvidenceLevel.None, 20f),
                    (CallerEvidenceLevel.Low, 40f),
                    (CallerEvidenceLevel.Medium, 30f),
                    (CallerEvidenceLevel.High, 10f),
                    (CallerEvidenceLevel.Irrefutable, 0f)),
                // Only Compelling callers can have Irrefutable evidence (~5%)
                CallerLegitimacy.Compelling => WeightedSelect(
                    (CallerEvidenceLevel.None, 5f),
                    (CallerEvidenceLevel.Low, 15f),
                    (CallerEvidenceLevel.Medium, 35f),
                    (CallerEvidenceLevel.High, 40f),
                    (CallerEvidenceLevel.Irrefutable, 5f)),
                _ => CallerEvidenceLevel.None
            };
        }

        private CallerCoherence GenerateCoherence(CallerLegitimacy legitimacy)
        {
            return legitimacy switch
            {
                CallerLegitimacy.Fake => WeightedSelect(
                    (CallerCoherence.Coherent, 20f),
                    (CallerCoherence.Questionable, 40f),
                    (CallerCoherence.Incoherent, 40f)),
                CallerLegitimacy.Questionable => WeightedSelect(
                    (CallerCoherence.Coherent, 50f),
                    (CallerCoherence.Questionable, 40f),
                    (CallerCoherence.Incoherent, 10f)),
                CallerLegitimacy.Credible => WeightedSelect(
                    (CallerCoherence.Coherent, 70f),
                    (CallerCoherence.Questionable, 25f),
                    (CallerCoherence.Incoherent, 5f)),
                // Compelling callers are always coherent
                CallerLegitimacy.Compelling => WeightedSelect(
                    (CallerCoherence.Coherent, 90f),
                    (CallerCoherence.Questionable, 10f),
                    (CallerCoherence.Incoherent, 0f)),
                _ => CallerCoherence.Coherent
            };
        }

        private CallerUrgency GenerateUrgency(CallerLegitimacy legitimacy)
        {
            return legitimacy switch
            {
                CallerLegitimacy.Fake => WeightedSelect(
                    (CallerUrgency.Low, 40f),
                    (CallerUrgency.Medium, 20f),
                    (CallerUrgency.High, 30f),
                    (CallerUrgency.Critical, 10f)),
                CallerLegitimacy.Questionable => WeightedSelect(
                    (CallerUrgency.Low, 40f),
                    (CallerUrgency.Medium, 35f),
                    (CallerUrgency.High, 20f),
                    (CallerUrgency.Critical, 5f)),
                CallerLegitimacy.Credible => WeightedSelect(
                    (CallerUrgency.Low, 35f),
                    (CallerUrgency.Medium, 40f),
                    (CallerUrgency.High, 20f),
                    (CallerUrgency.Critical, 5f)),
                CallerLegitimacy.Compelling => WeightedSelect(
                    (CallerUrgency.Low, 20f),
                    (CallerUrgency.Medium, 35f),
                    (CallerUrgency.High, 30f),
                    (CallerUrgency.Critical, 15f)),
                _ => CallerUrgency.Low
            };
        }

        /// <summary>
        /// Generate a random personality with its type.
        /// </summary>
        private PersonalityDefinition GeneratePersonality()
        {
            return Personalities[(int)(GD.Randi() % Personalities.Length)];
        }

        // GeneratePersonalityEffect removed - personality effects are now deterministic
        // per-personality via PersonalityStatEffects class in CallerStatEffects.GetStatEffects()

        #endregion

        private void HandlePhaseChanged(GamePhase oldPhase, GamePhase newPhase)
        {
            if (newPhase == GamePhase.LiveShow)
            {
                StartGenerating();
            }
            else if (oldPhase == GamePhase.LiveShow)
            {
                StopGenerating();
            }
        }
    }
}