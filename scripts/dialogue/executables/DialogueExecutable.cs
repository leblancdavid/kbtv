#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using KBTV.Callers;
using KBTV.Audio;
using KBTV.Core;

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

        // For caller dialogue
        public DialogueExecutable(string id, Caller caller, ConversationArc arc) 
            : base(id, BroadcastItemType.CallerLine, true, 4.0f, new { caller, arc })
        {
            _caller = caller;
            _arc = arc;
            _speaker = caller.Name;
            _audioPath = GetCallerAudioPath(caller, arc);
        }

        // For Vern dialogue
        public DialogueExecutable(string id, string text, string speaker, string? audioPath = null) 
            : base(id, BroadcastItemType.VernLine, true, 4.0f, new { text, speaker, audioPath })
        {
            _text = text;
            _speaker = speaker;
            _audioPath = audioPath;
        }

        protected override async Task ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            var displayText = GetDisplayText();
            GD.Print($"DialogueExecutable: {GetSpeakerName()}: {displayText}");
            
            if (!string.IsNullOrEmpty(_audioPath))
            {
                await PlayAudioAsync(_audioPath, cancellationToken);
            }
            else
            {
                // Fallback to duration-based timing if no audio
                await Task.Delay(TimeSpan.FromSeconds(_duration), cancellationToken);
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
                var lines = _arc.Lines;
                if (lines != null && lines.Length > 0)
                {
                    return lines[0].Text; // Return first line for now
                }
            }

            return "Dialogue line";
        }

        private string? GetCallerAudioPath(Caller caller, ConversationArc arc)
        {
            // Generate audio path based on conversation arc and caller
            var topicName = caller.Topic.ToString();
            var arcId = arc.ArcId;
            var gender = caller.Gender.ToString().ToLower();
            
            // This would need to match actual line index in the conversation
            // For now, we'll use a generic pattern
            return $"res://assets/audio/voice/Callers/{topicName}/{arcId}_{gender}_0.mp3";
        }
    }
}