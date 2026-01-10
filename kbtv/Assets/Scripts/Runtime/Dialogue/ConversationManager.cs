using System;
using System.Collections.Generic;
using UnityEngine;
using KBTV.Core;
using KBTV.Callers;
using KBTV.Data;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Manages conversation playback during on-air calls.
    /// Handles timing, state transitions, and integration with CallerQueue.
    /// </summary>
    public class ConversationManager : SingletonMonoBehaviour<ConversationManager>
    {
        [Header("Timing Settings")]
        [Tooltip("Base delay before showing the next line (seconds)")]
        [SerializeField] private float _baseDelay = 1.5f;

        [Tooltip("Additional delay per character in the line")]
        [SerializeField] private float _perCharacterDelay = 0.04f;

        [Tooltip("Pause between speakers (seconds)")]
        [SerializeField] private float _speakerTransitionDelay = 0.5f;

        [Header("Dead Air Settings")]
        [Tooltip("Time between filler lines when no callers available")]
        [SerializeField] private float _deadAirCycleInterval = 8f;

        [Header("Templates")]
        [Tooltip("Vern's dialogue template")]
        [SerializeField] private VernDialogueTemplate _vernTemplate;

        [Tooltip("Caller dialogue templates (matched by topic/legitimacy)")]
        [SerializeField] private List<CallerDialogueTemplate> _callerTemplates;

        [Header("References")]
        [Tooltip("Vern's stats (for response selection)")]
        [SerializeField] private VernStats _vernStats;

        [Tooltip("Current show topic (optional, set at runtime)")]
        [SerializeField] private Topic _currentTopic;

        // Runtime state
        private Conversation _currentConversation;
        private ConversationGenerator _generator;
        private float _lineTimer;
        private float _currentLineDuration;
        private bool _isWaitingForTransition;

        // Dead air filler state
        private bool _isPlayingDeadAirFiller = false;
        private float _deadAirFillerTimer = 0f;
        private float _currentFillerLineDuration = 0f;
        private bool _callerWaitingAfterFiller = false;
        private DialogueLine _currentFillerLine = null;

        // Properties
        public Conversation CurrentConversation => _currentConversation;
        public DialogueLine CurrentLine => _currentConversation?.CurrentLine;
        public bool IsPlaying => _currentConversation?.State == ConversationState.Playing;
        public bool HasActiveConversation => _currentConversation != null && 
            _currentConversation.State != ConversationState.Completed;
        public float LineProgress => _currentLineDuration > 0 ? _lineTimer / _currentLineDuration : 0f;
        public bool IsPlayingDeadAirFiller => _isPlayingDeadAirFiller;
        public DialogueLine CurrentFillerLine => _currentFillerLine;
        public float FillerLineProgress => _currentFillerLineDuration > 0 ? _deadAirFillerTimer / _currentFillerLineDuration : 0f;
        public Topic CurrentTopic
        {
            get => _currentTopic;
            set => _currentTopic = value;
        }

        // Events
        public event Action<Conversation> OnConversationStarted;
        public event Action<Conversation> OnConversationEnded;
        public event Action<DialogueLine> OnLineDisplayed;
        public event Action<DialogueLine> OnLineCompleted;
        public event Action<ConversationPhase> OnPhaseChanged;
        public event Action<DialogueLine> OnFillerLineDisplayed;
        public event Action OnFillerStopped;

        protected override void OnSingletonAwake()
        {
            InitializeGenerator();
        }

        private void Start()
        {
            // Subscribe to CallerQueue events
            if (CallerQueue.Instance != null)
            {
                CallerQueue.Instance.OnCallerOnAir += HandleCallerOnAir;
                CallerQueue.Instance.OnCallerCompleted += HandleCallerCompleted;
                CallerQueue.Instance.OnCallerApproved += HandleCallerApproved;
            }

            // Subscribe to game phase changes to start/stop filler
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnPhaseChanged += HandleGamePhaseChanged;
            }
        }

        protected override void OnDestroy()
        {
            // Unsubscribe from events
            if (CallerQueue.Instance != null)
            {
                CallerQueue.Instance.OnCallerOnAir -= HandleCallerOnAir;
                CallerQueue.Instance.OnCallerCompleted -= HandleCallerCompleted;
                CallerQueue.Instance.OnCallerApproved -= HandleCallerApproved;
            }

            // Unsubscribe from game phase changes
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnPhaseChanged -= HandleGamePhaseChanged;
            }

            base.OnDestroy();
        }

        private void Update()
        {
            if (IsPlaying)
            {
                UpdateLineTimer();
                return;
            }

            // Handle dead air filler
            if (_isPlayingDeadAirFiller)
            {
                UpdateDeadAirFiller();
            }
        }

        private void InitializeGenerator()
        {
            _generator = new ConversationGenerator(
                _callerTemplates ?? new List<CallerDialogueTemplate>(),
                _vernTemplate,
                _vernStats
            );
        }

        /// <summary>
        /// Start a conversation with the given caller.
        /// </summary>
        public void StartConversation(Caller caller)
        {
            if (_currentConversation != null && _currentConversation.State == ConversationState.Playing)
            {
                Debug.LogWarning("ConversationManager: Already playing a conversation, ending current one.");
                EndConversation();
            }

            // Ensure generator is initialized
            if (_generator == null)
            {
                InitializeGenerator();
            }

            // Generate the conversation
            _currentConversation = _generator.Generate(caller, _currentTopic);

            // Subscribe to conversation events
            _currentConversation.OnLineStarted += HandleLineStarted;
            _currentConversation.OnLineEnded += HandleLineEnded;
            _currentConversation.OnPhaseChanged += HandlePhaseChanged;
            _currentConversation.OnConversationCompleted += HandleConversationCompleted;

            // Start playback
            _currentConversation.Start();
            OnConversationStarted?.Invoke(_currentConversation);

            Debug.Log($"ConversationManager: Started conversation with {caller.Name}, {_currentConversation.Lines.Count} lines");
        }

        /// <summary>
        /// End the current conversation early.
        /// </summary>
        public void EndConversation()
        {
            if (_currentConversation == null)
                return;

            // Unsubscribe from events
            _currentConversation.OnLineStarted -= HandleLineStarted;
            _currentConversation.OnLineEnded -= HandleLineEnded;
            _currentConversation.OnPhaseChanged -= HandlePhaseChanged;
            _currentConversation.OnConversationCompleted -= HandleConversationCompleted;

            if (_currentConversation.State != ConversationState.Completed)
            {
                _currentConversation.End();
            }

            OnConversationEnded?.Invoke(_currentConversation);
            _currentConversation = null;
            _lineTimer = 0f;
            _isWaitingForTransition = false;
        }

        /// <summary>
        /// Pause the current conversation.
        /// </summary>
        public void Pause()
        {
            _currentConversation?.Pause();
        }

        /// <summary>
        /// Resume a paused conversation.
        /// </summary>
        public void Resume()
        {
            _currentConversation?.Resume();
        }

        /// <summary>
        /// Skip to the next line immediately.
        /// </summary>
        public void SkipToNextLine()
        {
            if (!IsPlaying)
                return;

            _lineTimer = _currentLineDuration;
            _isWaitingForTransition = false;
        }

        /// <summary>
        /// Set templates at runtime (useful for loading from resources).
        /// </summary>
        public void SetTemplates(VernDialogueTemplate vernTemplate, List<CallerDialogueTemplate> callerTemplates)
        {
            _vernTemplate = vernTemplate;
            _callerTemplates = callerTemplates ?? new List<CallerDialogueTemplate>();
            InitializeGenerator();
        }

        /// <summary>
        /// Add a caller template at runtime.
        /// </summary>
        public void AddCallerTemplate(CallerDialogueTemplate template)
        {
            if (template == null) return;
            
            _callerTemplates ??= new List<CallerDialogueTemplate>();
            _callerTemplates.Add(template);
            InitializeGenerator();
        }

        private void UpdateLineTimer()
        {
            _lineTimer += Time.deltaTime;

            // Check if we're in transition between speakers
            if (_isWaitingForTransition)
            {
                if (_lineTimer >= _speakerTransitionDelay)
                {
                    _isWaitingForTransition = false;
                    _lineTimer = 0f;
                    
                    // Actually advance to next line now
                    _currentConversation.AdvanceToNextLine();
                }
                return;
            }

            // Check if current line is done
            if (_lineTimer >= _currentLineDuration)
            {
                OnLineCompleted?.Invoke(CurrentLine);

                if (_currentConversation.HasMoreLines)
                {
                    // Check if next line is from a different speaker
                    var nextIndex = _currentConversation.CurrentIndex + 1;
                    var nextLine = _currentConversation.Lines[nextIndex];
                    
                    if (nextLine.Speaker != CurrentLine.Speaker)
                    {
                        // Add transition delay
                        _isWaitingForTransition = true;
                        _lineTimer = 0f;
                    }
                    else
                    {
                        // Same speaker, advance immediately
                        _lineTimer = 0f;
                        _currentConversation.AdvanceToNextLine();
                    }
                }
                else
                {
                    // Conversation complete
                    _currentConversation.AdvanceToNextLine();
                }
            }
        }

        private void HandleLineStarted(DialogueLine line)
        {
            _lineTimer = 0f;
            _currentLineDuration = line.GetDisplayDuration(_baseDelay, _perCharacterDelay);
            _isWaitingForTransition = false;

            OnLineDisplayed?.Invoke(line);

            Debug.Log($"ConversationManager: [{line.Speaker}] {line.Text}");
        }

        private void HandleLineEnded(DialogueLine line)
        {
            // Line completed, handled in UpdateLineTimer
        }

        private void HandlePhaseChanged(ConversationPhase phase)
        {
            OnPhaseChanged?.Invoke(phase);
            Debug.Log($"ConversationManager: Phase changed to {phase}");
        }

        #region Dead Air Filler

        /// <summary>
        /// Update dead air filler timing and cycling.
        /// </summary>
        private void UpdateDeadAirFiller()
        {
            _deadAirFillerTimer += Time.deltaTime;

            // Check if current filler line display time is done
            if (_deadAirFillerTimer >= _currentFillerLineDuration)
            {
                // Wait for the full cycle interval before showing next line
                if (_deadAirFillerTimer >= _deadAirCycleInterval)
                {
                    // Check if caller is waiting
                    if (_callerWaitingAfterFiller)
                    {
                        StopDeadAirFiller();
                        CallerQueue.Instance?.PutNextCallerOnAir();
                    }
                    else
                    {
                        // Cycle to next filler line
                        DisplayNextFillerLine();
                    }
                }
            }
        }

        /// <summary>
        /// Start playing dead air filler lines (Vern monologue when no callers).
        /// </summary>
        private void StartDeadAirFiller()
        {
            if (_isPlayingDeadAirFiller) return;
            if (_vernTemplate == null) return;

            _isPlayingDeadAirFiller = true;
            _callerWaitingAfterFiller = false;
            DisplayNextFillerLine();

            Debug.Log("ConversationManager: Started dead air filler");
        }

        /// <summary>
        /// Stop playing dead air filler.
        /// </summary>
        private void StopDeadAirFiller()
        {
            if (!_isPlayingDeadAirFiller) return;

            _isPlayingDeadAirFiller = false;
            _deadAirFillerTimer = 0f;
            _currentFillerLine = null;
            _callerWaitingAfterFiller = false;

            OnFillerStopped?.Invoke();
            Debug.Log("ConversationManager: Stopped dead air filler");
        }

        /// <summary>
        /// Display the next dead air filler line.
        /// </summary>
        private void DisplayNextFillerLine()
        {
            var template = _vernTemplate.GetDeadAirFiller();
            if (template == null)
            {
                Debug.LogWarning("ConversationManager: No dead air filler templates available");
                StopDeadAirFiller();
                return;
            }

            _currentFillerLine = new DialogueLine(
                Speaker.Vern,
                template.Text,
                template.Tone,
                ConversationPhase.Intro // Use Intro phase for filler
            );

            _deadAirFillerTimer = 0f;
            _currentFillerLineDuration = _currentFillerLine.GetDisplayDuration(_baseDelay, _perCharacterDelay);

            OnFillerLineDisplayed?.Invoke(_currentFillerLine);
            Debug.Log($"ConversationManager: [FILLER] {_currentFillerLine.Text}");
        }

        /// <summary>
        /// Handle when a caller is approved and put on hold.
        /// If filler is playing, flag to auto-advance after current line ends.
        /// </summary>
        private void HandleCallerApproved(Caller caller)
        {
            if (_isPlayingDeadAirFiller)
            {
                _callerWaitingAfterFiller = true;
                Debug.Log("ConversationManager: Caller approved, will auto-advance after filler line ends");
            }
        }

        /// <summary>
        /// Handle game phase changes to start/stop dead air filler.
        /// </summary>
        private void HandleGamePhaseChanged(GamePhase oldPhase, GamePhase newPhase)
        {
            if (newPhase == GamePhase.LiveShow)
            {
                // Start filler if no caller is currently on air
                if (CallerQueue.Instance == null || !CallerQueue.Instance.IsOnAir)
                {
                    StartDeadAirFiller();
                }
            }
            else if (oldPhase == GamePhase.LiveShow)
            {
                // Stop filler when leaving LiveShow
                StopDeadAirFiller();
            }
        }

        #endregion

        private void HandleConversationCompleted()
        {
            Debug.Log($"ConversationManager: Conversation completed");
            
            var conversation = _currentConversation;
            
            // Clean up
            EndConversation();

            // Notify CallerQueue to end the call if still on air
            if (CallerQueue.Instance != null && CallerQueue.Instance.IsOnAir)
            {
                CallerQueue.Instance.EndCurrentCall();
            }

            // Check if we should auto-advance or start filler
            if (CallerQueue.Instance != null && CallerQueue.Instance.HasOnHoldCallers)
            {
                // Auto-put next caller on air
                CallerQueue.Instance.PutNextCallerOnAir();
            }
            else
            {
                // No callers waiting - start dead air filler
                StartDeadAirFiller();
            }
        }

        private void HandleCallerOnAir(Caller caller)
        {
            // Stop dead air filler if playing
            if (_isPlayingDeadAirFiller)
            {
                StopDeadAirFiller();
            }

            Debug.Log($"ConversationManager: Caller {caller.Name} went on air, starting conversation");
            StartConversation(caller);
        }

        private void HandleCallerCompleted(Caller caller)
        {
            // If conversation is still playing when caller completes (e.g., manual end call)
            // end the conversation
            if (_currentConversation != null && _currentConversation.Caller == caller)
            {
                EndConversation();
            }
        }
    }
}
