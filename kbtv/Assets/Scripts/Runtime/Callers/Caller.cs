using System;
using UnityEngine;

namespace KBTV.Callers
{
    /// <summary>
    /// How legitimate/credible the caller appears to be.
    /// </summary>
    public enum CallerLegitimacy
    {
        /// <summary>Clearly fake, prank caller, or troll</summary>
        Fake,
        /// <summary>Suspicious, hard to verify</summary>
        Questionable,
        /// <summary>Seems genuine, average caller</summary>
        Credible,
        /// <summary>Very convincing, has details/evidence</summary>
        Compelling
    }

    /// <summary>
    /// The current state of a caller in the system.
    /// </summary>
    public enum CallerState
    {
        /// <summary>Waiting to be screened</summary>
        Incoming,
        /// <summary>Currently being screened by player</summary>
        Screening,
        /// <summary>Approved and waiting to go on air</summary>
        OnHold,
        /// <summary>Currently on air with Vern</summary>
        OnAir,
        /// <summary>Call completed</summary>
        Completed,
        /// <summary>Rejected by screener</summary>
        Rejected,
        /// <summary>Hung up or disconnected</summary>
        Disconnected
    }

    /// <summary>
    /// Represents a caller to the radio station.
    /// Contains all info the player uses to screen them.
    /// </summary>
    [Serializable]
    public class Caller
    {
        [Header("Identity")]
        [SerializeField] private string _name;
        [SerializeField] private string _phoneNumber;
        [SerializeField] private string _location;

        [Header("Call Info")]
        [SerializeField] private string _claimedTopic;
        [SerializeField] private string _actualTopic;
        [SerializeField] private string _callReason;

        [Header("Attributes")]
        [SerializeField] private CallerLegitimacy _legitimacy;
        [SerializeField] private CallerState _state;
        [SerializeField] private float _patience;
        [SerializeField] private float _quality;

        // Properties
        public string Name => _name;
        public string PhoneNumber => _phoneNumber;
        public string Location => _location;
        public string ClaimedTopic => _claimedTopic;
        public string ActualTopic => _actualTopic;
        public string CallReason => _callReason;
        public CallerLegitimacy Legitimacy => _legitimacy;
        public CallerState State => _state;
        public float Patience => _patience;
        public float Quality => _quality;

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

        public event Action<CallerState, CallerState> OnStateChanged;
        public event Action OnDisconnected;

        public Caller(string name, string phoneNumber, string location, 
            string claimedTopic, string actualTopic, string callReason,
            CallerLegitimacy legitimacy, float patience, float quality)
        {
            Id = Guid.NewGuid().ToString();
            _name = name;
            _phoneNumber = phoneNumber;
            _location = location;
            _claimedTopic = claimedTopic;
            _actualTopic = actualTopic;
            _callReason = callReason;
            _legitimacy = legitimacy;
            _patience = patience;
            _quality = quality;
            _state = CallerState.Incoming;
            WaitTime = 0f;
        }

        /// <summary>
        /// Update the caller's wait time. Returns true if caller disconnected.
        /// </summary>
        public bool UpdateWaitTime(float deltaTime)
        {
            if (_state == CallerState.Completed || 
                _state == CallerState.Rejected || 
                _state == CallerState.Disconnected ||
                _state == CallerState.OnAir)
            {
                return false;
            }

            WaitTime += deltaTime;

            // Check if patience ran out
            if (WaitTime > _patience)
            {
                SetState(CallerState.Disconnected);
                OnDisconnected?.Invoke();
                return true;
            }

            return false;
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

            // Bonus for matching topic
            if (_actualTopic == currentTopic)
            {
                impact *= 1.5f;
            }
            else
            {
                impact *= 0.5f;
            }

            // Legitimacy multiplier
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
