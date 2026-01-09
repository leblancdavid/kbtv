using System.Collections.Generic;
using UnityEngine;
using KBTV.Core;

namespace KBTV.Callers
{
    /// <summary>
    /// Generates random callers during the live show.
    /// Spawns a mix of legitimate, questionable, and fake callers.
    /// </summary>
    public class CallerGenerator : SingletonMonoBehaviour<CallerGenerator>
    {
        [Header("Generation Settings")]
        [Tooltip("Minimum seconds between caller spawns")]
        [SerializeField] private float _minSpawnInterval = 5f;

        [Tooltip("Maximum seconds between caller spawns")]
        [SerializeField] private float _maxSpawnInterval = 15f;

        [Tooltip("Base patience for callers (seconds they'll wait)")]
        [SerializeField] private float _basePatience = 30f;

        [Tooltip("Patience variance (+/-)")]
        [SerializeField] private float _patienceVariance = 10f;

        [Header("Caller Distribution")]
        [Tooltip("Chance of generating a fake/prank caller (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _fakeCallerChance = 0.15f;

        [Tooltip("Chance of generating a questionable caller (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _questionableCallerChance = 0.25f;

        [Tooltip("Chance of generating a compelling caller (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _compellingCallerChance = 0.1f;

        [Header("Available Topics")]
        [SerializeField] private Topic[] _availableTopics;

        private float _nextSpawnTime;
        private bool _isGenerating;
        private CallerQueue _queue;
        private Topic _currentTopic;

        // Name generation data
        private static readonly string[] FirstNames = {
            "John", "Mike", "Dave", "Steve", "Bob", "Jim", "Tom", "Bill", "Joe", "Frank",
            "Mary", "Linda", "Susan", "Karen", "Nancy", "Lisa", "Betty", "Dorothy", "Sandra", "Ashley",
            "Earl", "Cletus", "Bubba", "Hank", "Dale", "Rusty", "Buck", "Clem", "Jeb", "Cooter"
        };

        private static readonly string[] LastNames = {
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Wilson", "Taylor",
            "Jenkins", "McAllister", "Thornberry", "Blackwood", "Nightingale", "Ravenscroft", "Moonbeam", "Starlight"
        };

        private static readonly string[] Locations = {
            "Roswell, NM", "Area 51, NV", "Phoenix, AZ", "Portland, OR", "Austin, TX",
            "Denver, CO", "Seattle, WA", "Chicago, IL", "Miami, FL", "Boston, MA",
            "Rural Montana", "Somewhere in the Desert", "An Undisclosed Location", "A Van Down by the River"
        };

        private static readonly string[] AreaCodes = {
            "505", "702", "602", "503", "512", "303", "206", "312", "305", "617", "555"
        };

        private static readonly string[] FakeReasons = {
            "I saw a UFO... it was actually a plane",
            "The government is putting chemicals in the water to make frogs gay",
            "My neighbor is definitely a lizard person",
            "I can communicate with my toaster telepathically",
            "The moon landing was filmed in my backyard",
            "Elvis lives in my basement"
        };

        private static readonly string[] CredibleReasons = {
            "I witnessed strange lights over the desert last night",
            "I have documents from my time working at a classified facility",
            "I've been researching this phenomenon for 20 years",
            "I have photographic evidence I'd like to share",
            "My family has had multiple encounters over generations",
            "I'm a former military pilot with unexplained experiences"
        };

        private void Start()
        {
            _queue = CallerQueue.Instance;

            // Subscribe to game state changes
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnPhaseChanged += HandlePhaseChanged;

                // Check if we're already in LiveShow (in case we missed the event)
                if (GameStateManager.Instance.CurrentPhase == GamePhase.LiveShow)
                {
                    StartGenerating(CallerScreeningManager.Instance?.CurrentTopic);
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void Update()
        {
            if (!_isGenerating) return;

            if (Time.time >= _nextSpawnTime)
            {
                TrySpawnCaller();
                ScheduleNextSpawn();
            }
        }

        /// <summary>
        /// Start generating callers (called when live show starts).
        /// </summary>
        public void StartGenerating(Topic topic = null)
        {
            _currentTopic = topic;
            _isGenerating = true;
            ScheduleNextSpawn();
            Debug.Log("CallerGenerator: Started generating callers");
        }

        /// <summary>
        /// Stop generating callers (called when live show ends).
        /// </summary>
        public void StopGenerating()
        {
            _isGenerating = false;
            Debug.Log("CallerGenerator: Stopped generating callers");
        }

        /// <summary>
        /// Manually spawn a caller (for testing).
        /// </summary>
        public Caller SpawnCaller()
        {
            return TrySpawnCaller();
        }

        private Caller TrySpawnCaller()
        {
            if (_queue == null || !_queue.CanAcceptMoreCallers)
            {
                return null;
            }

            Caller caller = GenerateRandomCaller();
            _queue.AddCaller(caller);
            return caller;
        }

        private void ScheduleNextSpawn()
        {
            float interval = Random.Range(_minSpawnInterval, _maxSpawnInterval);
            _nextSpawnTime = Time.time + interval;
        }

        private Caller GenerateRandomCaller()
        {
            // Determine legitimacy
            CallerLegitimacy legitimacy = DetermineRandomLegitimacy();

            // Generate identity
            string firstName = FirstNames[Random.Range(0, FirstNames.Length)];
            string lastName = LastNames[Random.Range(0, LastNames.Length)];
            string name = $"{firstName} {lastName}";

            string areaCode = AreaCodes[Random.Range(0, AreaCodes.Length)];
            string phoneNumber = $"{areaCode}-{Random.Range(100, 999)}-{Random.Range(1000, 9999)}";

            string location = Locations[Random.Range(0, Locations.Length)];

            // Determine topic
            string actualTopic = DetermineActualTopic();
            string claimedTopic = DetermineClaimedTopic(actualTopic, legitimacy);

            // Generate reason
            string reason = GenerateCallReason(legitimacy);

            // Calculate quality and patience
            float quality = CalculateCallerQuality(legitimacy);
            float patience = _basePatience + Random.Range(-_patienceVariance, _patienceVariance);

            // Fake callers are more impatient
            if (legitimacy == CallerLegitimacy.Fake)
            {
                patience *= 0.5f;
            }

            return new Caller(name, phoneNumber, location, claimedTopic, actualTopic, 
                reason, legitimacy, patience, quality);
        }

        private CallerLegitimacy DetermineRandomLegitimacy()
        {
            float roll = Random.value;

            if (roll < _fakeCallerChance)
                return CallerLegitimacy.Fake;
            
            roll -= _fakeCallerChance;
            if (roll < _questionableCallerChance)
                return CallerLegitimacy.Questionable;
            
            roll -= _questionableCallerChance;
            if (roll < _compellingCallerChance)
                return CallerLegitimacy.Compelling;

            return CallerLegitimacy.Credible;
        }

        private string DetermineActualTopic()
        {
            if (_currentTopic != null)
            {
                // Most callers are on-topic
                if (Random.value > 0.3f)
                {
                    return _currentTopic.TopicId;
                }
            }

            // Random off-topic
            if (_availableTopics != null && _availableTopics.Length > 0)
            {
                return _availableTopics[Random.Range(0, _availableTopics.Length)].TopicId;
            }

            return "general";
        }

        private string DetermineClaimedTopic(string actualTopic, CallerLegitimacy legitimacy)
        {
            // Fake callers often lie about their topic
            if (legitimacy == CallerLegitimacy.Fake && Random.value < 0.5f)
            {
                if (_currentTopic != null)
                {
                    return _currentTopic.TopicId; // Pretend to be on-topic
                }
            }

            // Check topic deception rate
            if (_currentTopic != null && actualTopic != _currentTopic.TopicId)
            {
                if (Random.value < _currentTopic.DeceptionRate)
                {
                    return _currentTopic.TopicId; // Lie to get on the show
                }
            }

            return actualTopic;
        }

        private string GenerateCallReason(CallerLegitimacy legitimacy)
        {
            if (legitimacy == CallerLegitimacy.Fake)
            {
                return FakeReasons[Random.Range(0, FakeReasons.Length)];
            }

            return CredibleReasons[Random.Range(0, CredibleReasons.Length)];
        }

        private float CalculateCallerQuality(CallerLegitimacy legitimacy)
        {
            return legitimacy switch
            {
                CallerLegitimacy.Fake => Random.Range(-10f, 0f),
                CallerLegitimacy.Questionable => Random.Range(0f, 5f),
                CallerLegitimacy.Credible => Random.Range(5f, 15f),
                CallerLegitimacy.Compelling => Random.Range(15f, 25f),
                _ => 5f
            };
        }

        private void HandlePhaseChanged(GamePhase oldPhase, GamePhase newPhase)
        {
            if (newPhase == GamePhase.LiveShow)
            {
                StartGenerating(CallerScreeningManager.Instance?.CurrentTopic);
            }
            else if (oldPhase == GamePhase.LiveShow)
            {
                StopGenerating();
            }
        }
    }
}
