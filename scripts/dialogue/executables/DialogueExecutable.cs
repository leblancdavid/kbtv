#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using KBTV.Callers;
using KBTV.Audio;
using KBTV.Core;
using KBTV.Data;
using KBTV.Broadcast;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Executable for handling character dialogue (Vern and callers).
    /// </summary>
    public partial class DialogueExecutable : BroadcastExecutable
    {
        private readonly string? _speaker;
        private readonly string? _text;
        private readonly string? _audioPath;
    private readonly Caller? _caller;
    private readonly ConversationArc? _arc;
    private readonly VernLineType? _lineType;
    private readonly BroadcastStateManager? _stateManager;
    private readonly ConversationStatTracker? _statTracker;

        /// <summary>
        /// The specific type of Vern line (null for caller lines).
        /// </summary>
        public VernLineType? LineType => _lineType;

        /// <summary>
        /// Number of lines in this dialogue (1 for single lines, arc count for conversations).
        /// </summary>
        public int LineCount => _arc?.Dialogue.Count ?? 1;

        // For caller dialogue (full conversation arcs)
        public DialogueExecutable(string id, Caller caller, ConversationArc arc, EventBus eventBus, IBroadcastAudioService audioService, BroadcastStateManager stateManager, ConversationStatTracker statTracker) 
            : base(id, BroadcastItemType.Conversation, true, BroadcastConstants.DEFAULT_LINE_DURATION, eventBus, audioService, stateManager.SceneTree, new { caller, arc })
        {
            _caller = caller;
            _arc = arc;
            _speaker = caller.Name;
            _audioPath = null; // Audio handled per line in ExecuteInternalAsync
            _lineType = null; // Not applicable for caller lines
            _stateManager = stateManager;
            _statTracker = statTracker;
        }

        // For Vern dialogue
        public DialogueExecutable(string id, string text, string speaker, EventBus eventBus, IBroadcastAudioService audioService, string? audioPath = null, VernLineType? lineType = null, BroadcastStateManager? stateManager = null, ConversationStatTracker? statTracker = null) 
            : base(id, BroadcastItemType.VernLine, true, BroadcastConstants.DEFAULT_LINE_DURATION, eventBus, audioService, stateManager?.SceneTree ?? throw new ArgumentNullException(nameof(stateManager)), new { text, speaker, audioPath, lineType })
        {
            _text = text;
            _speaker = speaker;
            _audioPath = audioPath;
            _lineType = lineType;
            _stateManager = stateManager;
            _statTracker = statTracker;
        }

        protected override async Task ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            GD.Print($"DialogueExecutable: ExecuteInternalAsync started - Id: {_id}, Type: {_type}, HasArc: {_arc != null}, HasCaller: {_caller != null}");
            
            // Create local cancellation token for interruption handling
            using var localCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var localToken = localCts.Token;

            // Subscribe to interruption events for mid-execution cancellation
            void OnInterruption(BroadcastInterruptionEvent interruptionEvent)
            {
                if (interruptionEvent.Reason == BroadcastInterruptionReason.BreakImminent ||
                    interruptionEvent.Reason == BroadcastInterruptionReason.ShowEnding ||
                    interruptionEvent.Reason == BroadcastInterruptionReason.CallerDropped ||
                    interruptionEvent.Reason == BroadcastInterruptionReason.CallerCursed)
                {
                    // Handle interruption - effects up to current line are preserved
                    _statTracker?.InterruptConversation();
                    localCts.Cancel();
                }
            }
            _eventBus.Subscribe<BroadcastInterruptionEvent>(OnInterruption);

            try
            {
                if (_arc != null && _caller != null)
                {
                     // Initialize stat tracking for this conversation
                     _statTracker?.StartConversation(_caller, _arc);

                     // Track if cursing occurred during this conversation
                     bool cursingOccurred = false;

                     // Play full conversation line by line
                    var topic = _arc.TopicName;
                    foreach (var line in _arc.Dialogue)
                    {
                        if (localToken.IsCancellationRequested)
                        {
                            break;
                        }

                        string audioPath;
                        BroadcastItemType itemType;
                        string speakerName;
                        if (line.Speaker == Speaker.Vern)
                        {
                            audioPath = $"res://assets/audio/voice/Vern/ConversationArcs/{topic}/{_arc.ArcId}/{line.AudioId}.mp3";
                            itemType = BroadcastItemType.VernLine;
                            speakerName = "Vern";
                        }
                        else
                        {
                            audioPath = $"res://assets/audio/voice/Callers/{topic}/{_arc.ArcId}/{line.AudioId}.mp3";
                            itemType = BroadcastItemType.CallerLine;
                            speakerName = _caller.Name;
                        }

                        var item = new BroadcastItem(
                            id: line.AudioId,
                            type: itemType,
                            text: line.Text,
                            audioPath: audioPath,
                            duration: BroadcastConstants.DEFAULT_LINE_DURATION,
                            metadata: new { ArcId = _arc.ArcId, SpeakerId = speakerName, CallerGender = _arc.CallerGender }
                        );

                          // Get actual audio duration - skip loading if audio is disabled
                          float audioDuration = BroadcastConstants.DEFAULT_LINE_DURATION; // Default fallback
                          if (!_audioService.IsAudioDisabled)
                          {
                              audioDuration = await GetAudioDurationAsync(audioPath);
                          }

                          // Pre-playback cursing check for caller lines (not Vern lines)
                          bool willCurse = false;
                          float cursePoint = 0f;
                          if (line.Speaker != Speaker.Vern && _caller != null)
                          {
                                // TEMPORARY: Increased to 50% for testing - TODO: Revert to normal rates
                                float curseProbability = _caller.CurseRisk switch
                                {
                                    CallerCurseRisk.Low => 0.5f,     // TEMP: 50% for testing (was 1%)
                                    CallerCurseRisk.Medium => 0.5f,  // TEMP: 50% for testing (was 3%)
                                    CallerCurseRisk.High => 0.5f,    // TEMP: 50% for testing (was 5%)
                                    _ => 0.5f // TEMP: 50% for testing (was 1%)
                                };

                              if (GD.Randf() < curseProbability)
                              {
                                  willCurse = true;
                                  // Random curse point between 30-70% of audio duration
                                  cursePoint = audioDuration * (float)GD.RandRange(0.3f, 0.7f);
                                  GD.Print($"DialogueExecutable: Caller '{_caller.Name}' will curse at {cursePoint}s (30-70% of {audioDuration}s)");
                              }
                          }

                           // Publish started event for UI (duration depends on whether cursing occurs)
                           float effectiveDuration = willCurse ? cursePoint : BroadcastConstants.DEFAULT_LINE_DURATION;
                           var startedEvent = new BroadcastItemStartedEvent(item, effectiveDuration, willCurse ? cursePoint : audioDuration);
                           GD.Print($"DialogueExecutable: Publishing started event for line '{line.Text}' (effectiveDuration: {effectiveDuration}, audioLength: {(willCurse ? cursePoint : audioDuration)})");
                          _eventBus.Publish(startedEvent);

                            // Play audio for this line (full or partial based on cursing)
                            if (willCurse)
                            {
                                // Play partial audio for cursePoint duration with immediate stop
                                await _audioService.PlayAudioForDurationAsync(audioPath, cursePoint, true, CancellationToken.None);
                                
                                // Play bleep immediately after
                                await PlayAudioAsync("res://assets/audio/bleep.wav", CancellationToken.None);
                                
                                // Then interrupt conversation
                                GD.Print($"DialogueExecutable: Cursing triggered at {cursePoint}s, publishing CallerCursed interruption");
                                _eventBus.Publish(new BroadcastInterruptionEvent(BroadcastInterruptionReason.CallerCursed));
                                cursingOccurred = true;
                                break; // Exit the conversation loop
                            }
                           else
                           {
                               // No cursing: play entire audio normally
                               await PlayAudioAsync(audioPath, localToken);
                           }

                          // Apply stat effects for this completed line (only when no cursing)
                          if (!willCurse)
                          {
                              _statTracker?.OnLineCompleted();
                          }

                         // Check for pending break transition (graceful interruption between lines)
                         if (_stateManager?.PendingBreakTransition == true)
                         {
                             GD.Print($"DialogueExecutable: Break transition pending, exiting conversation loop early");
                             break;
                         }

                         // Check for pending caller drop (immediate interruption)
                         if (_stateManager?._pendingCallerDropped == true)
                         {
                             GD.Print($"DialogueExecutable: Caller drop pending, exiting conversation loop early");
                             break;
                         }

                         // Check for pending caller cursed (immediate interruption)
                         if (_stateManager?._pendingCallerCursed == true)
                         {
                             GD.Print($"DialogueExecutable: Caller cursed pending, exiting conversation loop early");
                             break;
                         }
                    }
                     GD.Print($"DialogueExecutable: Conversation arc loop completed - all {_arc.Dialogue.Count} lines processed");
                     
                     // Conversation completed naturally - apply any remaining effects (only if no cursing occurred)
                     if (!cursingOccurred)
                     {
                         _statTracker?.CompleteConversation();
                     }
                }
                else
                {
                    // Single Vern line (opening/fallback)
                    var displayText = GetDisplayText();
                    
                    // Create and publish broadcast item for UI
                    var item = CreateBroadcastItem();
                    
                    // Get actual audio duration - skip loading if audio is disabled
                    float audioDuration = _duration; // Default fallback
                    if (!_audioService.IsAudioDisabled)
                    {
                        audioDuration = await GetAudioDurationAsync();
                    }
                    
                    var startedEvent = new BroadcastItemStartedEvent(item, _duration, audioDuration);
                    GD.Print($"DialogueExecutable: Publishing started event for single Vern line '{item.Text}' (audioDuration: {audioDuration})");
                    _eventBus.Publish(startedEvent);
                    
                    // Apply penalty for off-topic remarks when they start playing
                    if (_lineType == VernLineType.OffTopicRemark)
                    {
                        GD.Print("DialogueExecutable: Applying off-topic remark penalty");
                        var vernStats = _stateManager?.GameStateManager?.VernStats;
                        vernStats?.ApplyOffTopicRemarkPenalty();
                    }
                    
                    if (!string.IsNullOrEmpty(_audioPath))
                    {
                        await PlayAudioAsync(_audioPath, localToken);
                    }
                    else
                    {
                        await DelayAsync(BroadcastConstants.DEFAULT_LINE_DURATION, localToken);
                    }
                }
            }
            finally
            {
                // Unsubscribe from interruption events
                _eventBus.Unsubscribe<BroadcastInterruptionEvent>(OnInterruption);
                GD.Print($"DialogueExecutable: ExecuteInternalAsync completed - Id: {_id}");
            }
        }

        protected override BroadcastItem CreateBroadcastItem()
        {
            return new BroadcastItem(_id, _type, GetDisplayText(), _audioPath, _duration, new { 
                Speaker = GetSpeakerName(),
                CallerId = _caller?.Id,
                ArcId = _arc?.ArcId,
                lineType = _lineType
            });
        }

        protected override async Task<float> GetAudioDurationAsync()
        {
            if (string.IsNullOrEmpty(_audioPath))
                return 0f;

            return await GetAudioDurationAsync(_audioPath, _duration);
        }

        private string GetDisplayText()
        {
            if (!string.IsNullOrEmpty(_text))
                return _text;

            // For caller dialogue, get current line from arc
            if (_arc != null && _caller != null)
            {
                // This is a simplified version - in practice, we'd track which line index
                // we're on in conversation
                var lines = _arc.Dialogue;
                if (lines != null && lines.Count > 0)
                {
                    return lines[0].Text; // Return first line for now
                }
            }

            return "Dialogue line";
        }

        private string GetSpeakerName()
        {
            if (_caller != null && _arc != null)
            {
                // For caller dialogue, use caller's name
                return _caller.Name;
            }
            // For Vern dialogue, use the speaker field
            return _speaker ?? "UNKNOWN";
        }


    }
}