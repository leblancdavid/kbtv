using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KBTV.Core;
using KBTV.Callers;
using KBTV.Data;
using KBTV.Audio;

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

        [Header("Arc System")]
        [Tooltip("Repository of conversation arcs")]
        [SerializeField] private ArcRepository _arcRepository;

        [Header("References")]
        [Tooltip("Vern's stats (for response selection)")]
        [SerializeField] private VernStats _vernStats;

        [Tooltip("Current show topic (optional, set at runtime)")]
        [SerializeField] private Topic _currentTopic;

        // Runtime state
        private Conversation _currentConversation;
        private ArcConversationGenerator _arcGenerator;
        private float _lineTimer;
        private float _currentLineDuration;
        private bool _isWaitingForTransition;

        // Dead air filler state
        private bool _isPlayingDeadAirFiller = false;
        private float _deadAirFillerTimer = 0f;
        private float _currentFillerLineDuration = 0f;
        private DialogueLine _currentFillerLine = null;

        // Broadcast flow state
        private bool _isPlayingBroadcastLine = false;
        private float _broadcastLineTimer = 0f;
        private float _broadcastLineDuration = 0f;
        private DialogueLine _currentBroadcastLine = null;
        private System.Action _onBroadcastLineComplete = null;
        private string _currentBroadcastClipId = null; // ID for current broadcast line audio

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
        public bool IsPlayingBroadcastLine => _isPlayingBroadcastLine;
        public DialogueLine CurrentBroadcastLine => _currentBroadcastLine;
        public float BroadcastLineProgress => _broadcastLineDuration > 0 ? _broadcastLineTimer / _broadcastLineDuration : 0f;
        public Topic CurrentTopic
        {
            get => _currentTopic;
            set => _currentTopic = value;
        }

        // Events
        public event Action<Conversation> OnConversationStarted;
        public event Action<Conversation> OnConversationEnded;
        public event Action<DialogueLine> OnLineDisplayed;
        /// <summary>
        /// Fired when a line is displayed with its audio duration (0 if no audio).
        /// Use this to sync typewriter speed to audio.
        /// </summary>
        public event Action<DialogueLine, float> OnLineDisplayedWithDuration;
        public event Action<DialogueLine> OnLineCompleted;
        public event Action<ConversationPhase> OnPhaseChanged;
        public event Action<DialogueLine> OnFillerLineDisplayed;
        public event Action OnFillerStopped;
        public event Action<DialogueLine> OnBroadcastLineDisplayed;
        /// <summary>
        /// Fired when a broadcast line is displayed with its audio duration (0 if no audio).
        /// </summary>
        public event Action<DialogueLine, float> OnBroadcastLineDisplayedWithDuration;
        public event Action OnBroadcastLineCompleted;

        protected override void OnSingletonAwake()
        {
            // NOTE: Do NOT initialize arc generator here!
            // GameBootstrap sets _arcRepository via reflection AFTER Awake() completes.
            // Lazy initialization in StartConversation() handles this correctly.
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
            else
            {
                Debug.LogWarning("ConversationManager: CallerQueue.Instance is null, cannot subscribe to events");
            }

            // Subscribe to game phase changes to start/stop filler
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnPhaseChanged += HandleGamePhaseChanged;
            }
            else
            {
                Debug.LogWarning("ConversationManager: GameStateManager.Instance is null, cannot subscribe to phase changes");
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

            // Handle broadcast flow lines (opening, closing, between callers)
            if (_isPlayingBroadcastLine)
            {
                UpdateBroadcastLine();
                return;
            }

            // Handle dead air filler
            if (_isPlayingDeadAirFiller)
            {
                UpdateDeadAirFiller();
            }
        }

        private void InitializeArcGenerator()
        {
            _arcGenerator = new ArcConversationGenerator(_arcRepository, _vernStats);
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

            // Ensure arc generator is initialized (lazy init - must happen after GameBootstrap sets _arcRepository)
            if (_arcGenerator == null)
            {
                InitializeArcGenerator();
            }

            // Generate the conversation using the arc system
            _currentConversation = _arcGenerator.Generate(caller, _currentTopic);

            // Handle case where no matching arc was found (caller "dropped")
            if (_currentConversation == null)
            {
                HandleCallerDropped(caller);
                return;
            }

            // Preload voice audio for this conversation
            if (VoiceAudioService.Instance != null && _arcGenerator.LastUsedArc != null)
            {
                string arcId = _arcGenerator.LastUsedArc.ArcId;
                string topic = _arcGenerator.LastUsedArc.Topic.ToString();
                VernMood mood = _arcGenerator.LastMood;
                int lineCount = _currentConversation.Lines.Count;
                
                // Start preloading (fire and forget - let conversation start while loading)
                _ = VoiceAudioService.Instance.PreloadConversationAsync(arcId, topic, mood, lineCount);
            }

            // Subscribe to conversation events
            _currentConversation.OnLineStarted += HandleLineStarted;
            _currentConversation.OnLineEnded += HandleLineEnded;
            _currentConversation.OnPhaseChanged += HandlePhaseChanged;
            _currentConversation.OnConversationCompleted += HandleConversationCompleted;

            // Start playback (arc conversations already include Vern's intro as line 001)
            _currentConversation.Start();
            OnConversationStarted?.Invoke(_currentConversation);


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

            // Stop any playing voice audio
            AudioManager.Instance?.StopVoice();
            
            // Unload conversation voice clips
            VoiceAudioService.Instance?.UnloadCurrentConversation();

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
        /// Set the arc repository at runtime.
        /// </summary>
        public void SetArcRepository(ArcRepository arcRepository)
        {
            _arcRepository = arcRepository;
            InitializeArcGenerator();
        }

        /// <summary>
        /// Set the Vern dialogue template at runtime (for broadcast flow lines).
        /// </summary>
        public void SetVernTemplate(VernDialogueTemplate vernTemplate)
        {
            _vernTemplate = vernTemplate;
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

        private async void HandleLineStarted(DialogueLine line)
        {
            _lineTimer = 0f;
            _isWaitingForTransition = false;
            
            // Set initial duration based on text BEFORE async operations
            // This prevents Update() from completing the line prematurely during audio load
            _currentLineDuration = line.GetDisplayDuration(_baseDelay, _perCharacterDelay);
            
            // Store reference to check if conversation changes during async operations
            var conversationAtStart = _currentConversation;
            int lineIndexAtStart = _currentConversation?.CurrentIndex ?? -1;
            
            // Try to get and play voice audio
            float audioDuration = 0f;
            if (VoiceAudioService.Instance != null && _currentConversation != null)
            {
                int lineIndex = _currentConversation.CurrentIndex;
                
                // First try cached clip (from preload)
                var clip = VoiceAudioService.Instance.GetConversationClip(lineIndex, line.Speaker);
                
                // If not cached, load on-demand (preload may not have completed)
                if (clip == null)
                {
                    clip = await VoiceAudioService.Instance.GetConversationClipAsync(lineIndex, line.Speaker);
                    
                    // Guard: Make sure conversation is still active after async load
                    if (_currentConversation != conversationAtStart || _currentConversation?.CurrentIndex != lineIndexAtStart)
                    {
                        return;
                    }
                }
                
                if (clip != null)
                {
                    AudioManager.Instance?.PlayVoiceClip(clip, line.Speaker);
                    audioDuration = clip.length;
                }
            }
            
            // Guard: Check again before firing events
            if (_currentConversation != conversationAtStart)
            {
                return;
            }
            
            // Use audio duration if available, otherwise calculate from text
            _currentLineDuration = audioDuration > 0f 
                ? audioDuration 
                : line.GetDisplayDuration(_baseDelay, _perCharacterDelay);

            OnLineDisplayed?.Invoke(line);
            OnLineDisplayedWithDuration?.Invoke(line, audioDuration);
        }

        private void HandleLineEnded(DialogueLine line)
        {
            // Line completed, handled in UpdateLineTimer
        }

        private void HandlePhaseChanged(ConversationPhase phase)
        {
            OnPhaseChanged?.Invoke(phase);
        }

        #region Broadcast Flow Lines

        /// <summary>
        /// Update broadcast line timing.
        /// </summary>
        private void UpdateBroadcastLine()
        {
            _broadcastLineTimer += Time.deltaTime;

            if (_broadcastLineTimer >= _broadcastLineDuration)
            {
                CompleteBroadcastLine();
            }
        }

        /// <summary>
        /// Play a broadcast flow line (opening, closing, between callers).
        /// </summary>
        private async void PlayBroadcastLine(DialogueTemplate template, System.Action onComplete)
        {
            if (template == null)
            {
                onComplete?.Invoke();
                return;
            }

            _currentBroadcastLine = new DialogueLine(
                Speaker.Vern,
                template.Text,
                template.Tone,
                ConversationPhase.Intro
            );
            
            _currentBroadcastClipId = template.Id;

            _broadcastLineTimer = 0f;
            // Set initial duration based on text - will be updated if audio loads
            _broadcastLineDuration = _currentBroadcastLine.GetDisplayDuration(_baseDelay, _perCharacterDelay);
            _onBroadcastLineComplete = onComplete;
            _isPlayingBroadcastLine = true;
            
            // Try to load and play voice audio
            float audioDuration = 0f;
            if (VoiceAudioService.Instance != null && !string.IsNullOrEmpty(template.Id))
            {
                try
                {
                    var clip = await VoiceAudioService.Instance.GetBroadcastClipAsync(template.Id);
                    
                    // Guard: Check if broadcast was cancelled during async load (e.g., caller went on air)
                    if (!_isPlayingBroadcastLine)
                    {
                        return;
                    }
                    
                    if (clip != null)
                    {
                        AudioManager.Instance?.PlayVoiceClip(clip, Speaker.Vern);
                        audioDuration = clip.length;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"ConversationManager: Exception loading broadcast audio: {ex.Message}");
                }
            }
            
            // Guard: Check again in case state changed during async operations
            if (!_isPlayingBroadcastLine)
            {
                return;
            }
            
            // Use audio duration if available, otherwise calculate from text
            _broadcastLineDuration = audioDuration > 0f 
                ? audioDuration 
                : _currentBroadcastLine?.GetDisplayDuration(_baseDelay, _perCharacterDelay) ?? 3f;

            if (_currentBroadcastLine != null)
            {
                OnBroadcastLineDisplayed?.Invoke(_currentBroadcastLine);
                OnBroadcastLineDisplayedWithDuration?.Invoke(_currentBroadcastLine, audioDuration);
            }
        }

        /// <summary>
        /// Complete the current broadcast line and invoke callback.
        /// </summary>
        private void CompleteBroadcastLine()
        {
            _isPlayingBroadcastLine = false;
            _currentBroadcastLine = null;
            
            var callback = _onBroadcastLineComplete;
            _onBroadcastLineComplete = null;
            
            OnBroadcastLineCompleted?.Invoke();
            callback?.Invoke();
        }

        /// <summary>
        /// Cancel any playing broadcast line (e.g., when caller goes on air).
        /// Unlike CompleteBroadcastLine, does NOT invoke the callback.
        /// </summary>
        private void CancelBroadcastLine()
        {
            if (!_isPlayingBroadcastLine) return;
            
            _isPlayingBroadcastLine = false;
            _currentBroadcastLine = null;
            _onBroadcastLineComplete = null; // Discard callback
            
            // Stop any playing voice audio
            AudioManager.Instance?.StopVoice();
            
            OnBroadcastLineCompleted?.Invoke();
        }

        /// <summary>
        /// Play the show opening line when LiveShow begins.
        /// </summary>
        private void PlayShowOpening(System.Action onComplete)
        {
            if (_vernTemplate == null)
            {
                onComplete?.Invoke();
                return;
            }

            var template = _vernTemplate.GetShowOpening();
            if (template == null)
            {
                onComplete?.Invoke();
                return;
            }
            
            PlayBroadcastLine(template, onComplete);
        }

        /// <summary>
        /// Play the show closing line when LiveShow ends.
        /// </summary>
        private void PlayShowClosing(System.Action onComplete)
        {
            if (_vernTemplate == null)
            {
                onComplete?.Invoke();
                return;
            }

            var template = _vernTemplate.GetShowClosing();
            PlayBroadcastLine(template, onComplete);
        }

        /// <summary>
        /// Play a between-callers transition line.
        /// </summary>
        private void PlayBetweenCallers(System.Action onComplete)
        {
            if (_vernTemplate == null)
            {
                onComplete?.Invoke();
                return;
            }

            var template = _vernTemplate.GetBetweenCallers();
            PlayBroadcastLine(template, onComplete);
        }

        #endregion

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
                // Check if there are callers waiting on hold - put them on air
                if (CallerQueue.Instance != null && CallerQueue.Instance.HasOnHoldCallers)
                {
                    StopDeadAirFiller();
                    CallerQueue.Instance.PutNextCallerOnAir();
                    return;
                }
                
                // Otherwise, wait for the full cycle interval before showing next line
                if (_deadAirFillerTimer >= _deadAirCycleInterval)
                {
                    // Cycle to next filler line
                    DisplayNextFillerLine();
                }
            }
        }

        /// <summary>
        /// Start playing dead air filler lines (Vern monologue when no callers).
        /// </summary>
        private void StartDeadAirFiller()
        {
            if (_isPlayingDeadAirFiller) return;
            if (_vernTemplate == null)
            {
                return;
            }

            _isPlayingDeadAirFiller = true;
            DisplayNextFillerLine();
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
            
            // Stop any playing voice audio from filler
            AudioManager.Instance?.StopVoice();

            OnFillerStopped?.Invoke();
        }

        /// <summary>
        /// Display the next dead air filler line.
        /// </summary>
        private async void DisplayNextFillerLine()
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
            
            // Set initial duration based on text BEFORE async operations
            // This prevents UpdateDeadAirFiller() from seeing duration as 0 during audio load
            _currentFillerLineDuration = _currentFillerLine.GetDisplayDuration(_baseDelay, _perCharacterDelay);
            
            // Try to load and play voice audio
            float audioDuration = 0f;
            if (VoiceAudioService.Instance != null && !string.IsNullOrEmpty(template.Id))
            {
                var clip = await VoiceAudioService.Instance.GetBroadcastClipAsync(template.Id);
                
                // Guard: Check if filler was stopped during async load (e.g., caller went on air)
                if (!_isPlayingDeadAirFiller)
                {
                    return;
                }
                
                if (clip != null)
                {
                    AudioManager.Instance?.PlayVoiceClip(clip, Speaker.Vern);
                    audioDuration = clip.length;
                }
            }
            
            // Guard: Check again in case state changed during async operations
            if (!_isPlayingDeadAirFiller)
            {
                return;
            }
            
            // Use audio duration if available, otherwise calculate from text
            _currentFillerLineDuration = audioDuration > 0f 
                ? audioDuration 
                : _currentFillerLine.GetDisplayDuration(_baseDelay, _perCharacterDelay);

            OnFillerLineDisplayed?.Invoke(_currentFillerLine);
        }

        /// <summary>
        /// Handle when a caller is approved and put on hold.
        /// The caller will automatically go on air when current content finishes.
        /// </summary>
        private void HandleCallerApproved(Caller caller)
        {
            // If nothing is playing, put caller on air immediately
            if (!_isPlayingDeadAirFiller && !_isPlayingBroadcastLine && _currentConversation == null)
            {
                CallerQueue.Instance?.PutNextCallerOnAir();
            }
            // Otherwise, caller waits - they'll be picked up when current content finishes
        }

        /// <summary>
        /// Handle game phase changes to start/stop dead air filler.
        /// </summary>
        private void HandleGamePhaseChanged(GamePhase oldPhase, GamePhase newPhase)
        {
            if (newPhase == GamePhase.LiveShow)
            {
                // Defer show opening by one frame to allow UI panels to activate and subscribe to events.
                // The phase change event activates the LiveShowUI, which then enables child panels.
                // Without this delay, the show opening would fire before panels have subscribed.
                StartCoroutine(PlayShowOpeningDeferred());
            }
            else if (oldPhase == GamePhase.LiveShow)
            {
                // Stop filler when leaving LiveShow
                StopDeadAirFiller();

                // Play show closing
                PlayShowClosing(null);
            }
        }

        /// <summary>
        /// Play show opening after a one-frame delay to allow UI to subscribe.
        /// </summary>
        private IEnumerator PlayShowOpeningDeferred()
        {
            // Wait one frame for UI panels to activate and subscribe
            yield return null;
            
            PlayShowOpening(() =>
            {
                // After show opening, check if caller is already on air
                if (CallerQueue.Instance != null && CallerQueue.Instance.IsOnAir)
                {
                    return;
                }
                
                // Check if there are callers on hold - put them on air
                if (CallerQueue.Instance != null && CallerQueue.Instance.HasOnHoldCallers)
                {
                    CallerQueue.Instance.PutNextCallerOnAir();
                    return;
                }
                
                StartDeadAirFiller();
            });
        }

        #endregion

        #region Dropped Caller Handling

        /// <summary>
        /// Handle when no matching arc is found for a caller.
        /// Plays a "lost the caller" line and moves on to next caller or filler.
        /// </summary>
        private void HandleCallerDropped(Caller caller)
        {
            Debug.LogWarning($"ConversationManager: No arc found for caller {caller.Name} " +
                             $"(Topic: {caller.ClaimedTopic}, Legitimacy: {caller.Legitimacy}). Treating as dropped.");

            // Play a dropped caller line
            PlayDroppedCallerLine(() =>
            {
                // End the caller's slot in the queue
                if (CallerQueue.Instance != null && CallerQueue.Instance.IsOnAir)
                {
                    CallerQueue.Instance.EndCurrentCall();
                }

                // Check if we should auto-advance or start filler
                if (CallerQueue.Instance != null && CallerQueue.Instance.HasOnHoldCallers)
                {
                    PlayBetweenCallers(() =>
                    {
                        CallerQueue.Instance?.PutNextCallerOnAir();
                    });
                }
                else
                {
                    StartDeadAirFiller();
                }
            });
        }

        /// <summary>
        /// Play a dropped caller line (when no arc is found).
        /// </summary>
        private void PlayDroppedCallerLine(System.Action onComplete)
        {
            if (_vernTemplate == null)
            {
                onComplete?.Invoke();
                return;
            }

            var template = _vernTemplate.GetDroppedCaller();
            PlayBroadcastLine(template, onComplete);
        }

        #endregion

        private void HandleConversationCompleted()
        {
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
                // Play between-callers transition, then auto-put next caller on air
                PlayBetweenCallers(() =>
                {
                    CallerQueue.Instance?.PutNextCallerOnAir();
                });
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
            
            // Cancel any playing broadcast line (show opening, between callers, etc.)
            if (_isPlayingBroadcastLine)
            {
                CancelBroadcastLine();
            }

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
