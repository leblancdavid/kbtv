#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using KBTV.Callers;
using KBTV.Audio;
using KBTV.Core;
using KBTV.Data;
using KBTV.Managers;

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

        /// <summary>
        /// The specific type of Vern line (null for caller lines).
        /// </summary>
        public VernLineType? LineType => _lineType;

        // For caller dialogue (full conversation arcs)
        public DialogueExecutable(string id, Caller caller, ConversationArc arc, EventBus eventBus, IBroadcastAudioService audioService, BroadcastStateManager stateManager) 
            : base(id, BroadcastItemType.Conversation, true, 4.0f, eventBus, audioService, stateManager.SceneTree, new { caller, arc })
        {
            _caller = caller;
            _arc = arc;
            _speaker = caller.Name;
            _audioPath = null; // Audio handled per line in ExecuteInternalAsync
            _lineType = null; // Not applicable for caller lines
            _stateManager = stateManager;
        }

        // For Vern dialogue
        public DialogueExecutable(string id, string text, string speaker, EventBus eventBus, IBroadcastAudioService audioService, string? audioPath = null, VernLineType? lineType = null, BroadcastStateManager? stateManager = null) 
            : base(id, BroadcastItemType.VernLine, true, 4.0f, eventBus, audioService, stateManager?.SceneTree ?? throw new ArgumentNullException(nameof(stateManager)), new { text, speaker, audioPath, lineType })
        {
            _text = text;
            _speaker = speaker;
            _audioPath = audioPath;
            _lineType = lineType;
            _stateManager = stateManager;
        }

        protected override async Task ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            // Create local cancellation token for interruption handling
            using var localCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var localToken = localCts.Token;

            // Subscribe to interruption events for mid-execution cancellation
            void OnInterruption(BroadcastInterruptionEvent interruptionEvent)
            {
                GD.Print($"DialogueExecutable: Received interruption event: {interruptionEvent.Reason}");
                if (interruptionEvent.Reason == BroadcastInterruptionReason.BreakImminent)
                {
                    GD.Print("DialogueExecutable: Received break interruption - cancelling execution");
                    localCts.Cancel();
                }
            }
            _eventBus.Subscribe<BroadcastInterruptionEvent>(OnInterruption);

            try
            {
                if (_arc != null && _caller != null)
                {
                    // Play full conversation line by line
                    var topic = _arc.TopicName;
                    GD.Print($"DialogueExecutable: Starting conversation with {_arc.Dialogue.Count} lines");
                    foreach (var line in _arc.Dialogue)
                    {
                        if (localToken.IsCancellationRequested)
                        {
                            GD.Print("DialogueExecutable: Cancellation requested - breaking out of dialogue loop");
                            break;
                        }

                        GD.Print($"DialogueExecutable: Processing line {line.AudioId} for {line.Speaker}");
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
                            duration: 4.0f,
                            metadata: new { ArcId = _arc.ArcId, SpeakerId = speakerName, CallerGender = _arc.CallerGender }
                        );

                        // Get actual audio duration
                        var audioDuration = await GetAudioDurationAsync(audioPath);

                        // Publish started event for UI
                        var startedEvent = new BroadcastItemStartedEvent(item, 4.0f, audioDuration);
                        _eventBus.Publish(startedEvent);

                        GD.Print($"DialogueExecutable: {speakerName}: {line.Text}");

                        // Play audio for this line
                        if (!string.IsNullOrEmpty(audioPath))
                        {
                            GD.Print($"DialogueExecutable: Playing audio for line {line.AudioId}: {audioPath}");
                            await PlayAudioAsync(audioPath, localToken);
                            GD.Print($"DialogueExecutable: Finished playing audio for line {line.AudioId}");
                        }
                        else
                        {
                            GD.Print($"DialogueExecutable: No audio path for line {line.AudioId}, delaying 4 seconds");
                            await DelayAsync(4.0f, localToken);
                        }

                        // Check for pending break transition (graceful interruption between lines)
                        if (_stateManager?.PendingBreakTransition == true)
                        {
                            GD.Print("DialogueExecutable: Graceful interruption - ending conversation early for break transition");
                            break;
                        }
                    }
                }
                else
                {
                    // Single Vern line (opening/fallback)
                    var displayText = GetDisplayText();
                    GD.Print($"DialogueExecutable: {GetSpeakerName()}: {displayText}");
                    
                    // Create and publish broadcast item for UI
                    var item = CreateBroadcastItem();
                    var audioDuration = await GetAudioDurationAsync();
                    var startedEvent = new BroadcastItemStartedEvent(item, _duration, audioDuration);
                    _eventBus.Publish(startedEvent);
                    
                    if (!string.IsNullOrEmpty(_audioPath))
                    {
                        await PlayAudioAsync(_audioPath, localToken);
                    }
                    else
                    {
                        GD.Print("DialogueExecutable: No audio path, delaying 4 seconds");
                        await DelayAsync(4.0f, localToken);
                    }
                }
            }
            finally
            {
                // Unsubscribe from interruption events
                _eventBus.Unsubscribe<BroadcastInterruptionEvent>(OnInterruption);
            }
        }

        protected override BroadcastItem CreateBroadcastItem()
        {
            return new BroadcastItem(_id, _type, GetDisplayText(), _audioPath, _duration, new { 
                Speaker = GetSpeakerName(),
                CallerId = _caller?.Id,
                ArcId = _arc?.ArcId
            });
        }

        protected override async Task<float> GetAudioDurationAsync()
        {
            if (string.IsNullOrEmpty(_audioPath))
                return 0f;

            try
            {
                var audioStream = GD.Load<AudioStream>(_audioPath);
                if (audioStream is AudioStreamMP3 mp3Stream)
                {
                    return (float)mp3Stream.GetLength();
                }
                else if (audioStream is AudioStreamOggVorbis vorbisStream)
                {
                    return (float)vorbisStream.GetLength();
                }
                
                var length = audioStream?.GetLength() ?? 0.0;
                return length > 0 ? (float)length : _duration;
            }
            catch
            {
                return _duration;
            }
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

        private async Task<float> GetAudioDurationAsync(string audioPath)
        {
            if (string.IsNullOrEmpty(audioPath))
                return 0f;

            try
            {
                var audioStream = GD.Load<AudioStream>(audioPath);
                if (audioStream is AudioStreamMP3 mp3Stream)
                {
                    return (float)mp3Stream.GetLength();
                }
                else if (audioStream is AudioStreamOggVorbis vorbisStream)
                {
                    return (float)vorbisStream.GetLength();
                }
                
                var length = audioStream?.GetLength() ?? 0.0;
                return length > 0 ? (float)length : 4.0f;
            }
            catch
            {
                return 4.0f;
            }
        }
    }
}