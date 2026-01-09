using UnityEngine;
using KBTV.Core;
using KBTV.Data;

namespace KBTV.Callers
{
    /// <summary>
    /// Manages caller screening during live shows.
    /// Validates callers against topic rules and applies effects to Vern's stats.
    /// </summary>
    public class CallerScreeningManager : MonoBehaviour
    {
        public static CallerScreeningManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Topic _currentTopic;

        [Header("Stat Modifiers")]
        [SerializeField] private StatModifier _goodCallerModifier;
        [SerializeField] private StatModifier _badCallerModifier;
        [SerializeField] private StatModifier _greatCallerModifier;

        [Header("Settings")]
        [Tooltip("Belief bonus/penalty multiplier based on caller quality")]
        [SerializeField] private float _beliefImpactMultiplier = 1f;

        public Topic CurrentTopic => _currentTopic;

        private CallerQueue _queue;
        private GameStateManager _gameState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            _queue = CallerQueue.Instance;
            _gameState = GameStateManager.Instance;

            if (_queue != null)
            {
                _queue.OnCallerCompleted += HandleCallerCompleted;
            }
        }

        private void OnDestroy()
        {
            if (_queue != null)
            {
                _queue.OnCallerCompleted -= HandleCallerCompleted;
            }
        }

        /// <summary>
        /// Set the topic for tonight's show.
        /// </summary>
        public void SetTopic(Topic topic)
        {
            _currentTopic = topic;
            Debug.Log($"CallerScreeningManager: Topic set to '{topic.DisplayName}'");
        }

        /// <summary>
        /// Screen the current caller against the topic rules.
        /// Returns the screening result.
        /// </summary>
        public ScreeningResult ScreenCurrentCaller()
        {
            if (_queue == null || _queue.CurrentScreening == null)
            {
                return new ScreeningResult(false, "No caller to screen");
            }

            if (_currentTopic == null)
            {
                // No topic set, accept anyone
                return new ScreeningResult(true, "No topic rules set");
            }

            return _currentTopic.ScreenCaller(_queue.CurrentScreening);
        }

        /// <summary>
        /// Approve the current caller (player decision).
        /// </summary>
        public bool ApproveCaller()
        {
            if (_queue == null) return false;
            return _queue.ApproveCurrentCaller();
        }

        /// <summary>
        /// Reject the current caller (player decision).
        /// </summary>
        public bool RejectCaller()
        {
            if (_queue == null) return false;
            return _queue.RejectCurrentCaller();
        }

        /// <summary>
        /// Put the next on-hold caller on air.
        /// </summary>
        public Caller PutNextOnAir()
        {
            if (_queue == null) return null;
            return _queue.PutNextCallerOnAir();
        }

        /// <summary>
        /// End the current on-air call.
        /// </summary>
        public void EndCurrentCall()
        {
            _queue?.EndCurrentCall();
        }

        /// <summary>
        /// Start screening the next incoming caller.
        /// </summary>
        public Caller ScreenNext()
        {
            if (_queue == null) return null;
            return _queue.StartScreeningNext();
        }

        private void HandleCallerCompleted(Caller caller)
        {
            if (_gameState == null || _gameState.VernStats == null) return;

            // Calculate impact based on caller quality and topic match
            string topicId = _currentTopic != null ? _currentTopic.TopicId : "";
            float impact = caller.CalculateShowImpact(topicId);

            Debug.Log($"CallerScreeningManager: Caller {caller.Name} impact: {impact:F2}");

            // Apply appropriate modifier based on caller quality
            if (impact >= 15f && _greatCallerModifier != null)
            {
                _greatCallerModifier.Apply(_gameState.VernStats);
            }
            else if (impact >= 5f && _goodCallerModifier != null)
            {
                _goodCallerModifier.Apply(_gameState.VernStats);
            }
            else if (impact < 0f && _badCallerModifier != null)
            {
                _badCallerModifier.Apply(_gameState.VernStats);
            }

            // Apply direct belief impact
            float beliefChange = impact * _beliefImpactMultiplier;
            _gameState.VernStats.Belief.Modify(beliefChange);
        }

        /// <summary>
        /// Get info about what went wrong/right with a caller after they're done.
        /// </summary>
        public string GetCallerPostMortem(Caller caller)
        {
            string topicId = _currentTopic != null ? _currentTopic.TopicId : "";
            bool topicMatch = caller.ActualTopic == topicId;
            bool wasLying = caller.IsLyingAboutTopic;

            string result = $"Caller: {caller.Name}\n";
            result += $"Claimed: {caller.ClaimedTopic}\n";
            result += $"Actual: {caller.ActualTopic}\n";
            result += $"Legitimacy: {caller.Legitimacy}\n";
            result += $"Topic Match: {(topicMatch ? "YES" : "NO")}\n";
            
            if (wasLying)
            {
                result += "<color=red>LIAR - They lied about their topic!</color>\n";
            }

            float impact = caller.CalculateShowImpact(topicId);
            result += $"Show Impact: {(impact >= 0 ? "+" : "")}{impact:F1}";

            return result;
        }
    }
}
