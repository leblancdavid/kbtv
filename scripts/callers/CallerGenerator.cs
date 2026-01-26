using System;
using Godot;
using KBTV.Core;
using KBTV.Callers;
using KBTV.Dialogue;

namespace KBTV.Callers
{
    /// <summary>
    /// Generates random callers during the live show.
    /// Spawns a mix of legitimate, questionable, and fake callers.
    /// </summary>
	public partial class CallerGenerator : Node
	{
        [Export] private float _minSpawnInterval = 1f;
        [Export] private float _maxSpawnInterval = 3f;
        [Export] private float _basePatience = 30f;
        [Export] private float _patienceVariance = 10f;

        [Export] private float _fakeCallerChance = 0.15f;
        [Export] private float _questionableCallerChance = 0.25f;
        [Export] private float _compellingCallerChance = 0.1f;

        private float _nextSpawnTime;
        private bool _isGenerating;
        private ICallerRepository _repository;

        private GameStateManager _gameState;

        // Simple name generation data
        private static readonly string[] FirstNames = {
            "John", "Mike", "Dave", "Steve", "Bob", "Jim", "Tom", "Bill", "Joe", "Frank",
            "Mary", "Linda", "Susan", "Karen", "Nancy", "Lisa", "Betty", "Dorothy", "Sandra", "Ashley"
        };

        private static readonly string[] LastNames = {
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez"
        };

        private static readonly string[] Locations = {
            "Springfield", "Riverside", "Oakwood", "Pineville", "Maple Creek", "Elmwood", "Cedar Falls", "Willow Grove"
        };

        public override void _Ready()
        {
            ServiceRegistry.Instance.RegisterSelf<CallerGenerator>(this);
            CompleteInitialization();
        }

        private void CompleteInitialization()
        {
            _repository = ServiceRegistry.Instance.CallerRepository;
            _gameState = ServiceRegistry.Instance.GameStateManager;

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

            _gameState.Connect("PhaseChanged", Callable.From<int, int>(HandlePhaseChanged));

            if (_gameState.CurrentPhase == GamePhase.LiveShow)
            {
                StartGenerating();
            }

            GD.Print("CallerGenerator: Initialization complete");
        }

        public override void _ExitTree()
        {
            if (_gameState != null)
            {
                _gameState.Disconnect("PhaseChanged", Callable.From<int, int>(HandlePhaseChanged));
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

            // Simplified attributes
            CallerEmotionalState emotionalState = CallerEmotionalState.Calm;
            CallerCurseRisk curseRisk = CallerCurseRisk.Low;
            CallerBeliefLevel beliefLevel = CallerBeliefLevel.Curious;
            CallerEvidenceLevel evidenceLevel = CallerEvidenceLevel.None;
            CallerCoherence coherence = CallerCoherence.Coherent;
            CallerUrgency urgency = CallerUrgency.Low;

            CallerPhoneQuality phoneQuality = CallerPhoneQuality.Average;

            string personality = "Average caller";

            // Assign arcs based on show topic (90% on-topic, 10% off-topic)
            var arcRepo = ServiceRegistry.Instance?.ArcRepository;
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
                personality, claimedArc, actualArc, screeningSummary, patience, quality);

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

        private void HandlePhaseChanged(int oldPhaseInt, int newPhaseInt)
        {
            GamePhase oldPhase = (GamePhase)oldPhaseInt;
            GamePhase newPhase = (GamePhase)newPhaseInt;

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