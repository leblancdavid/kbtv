#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using KBTV.Audio;
using KBTV.Core;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Executable for between-callers transitions and dead air filler.
    /// Supports audio playback with text display.
    /// </summary>
    public partial class TransitionExecutable : BroadcastExecutable
    {
        private readonly string _text;
        private readonly string? _audioPath;

        public TransitionExecutable(string id, string text, float duration, EventBus eventBus, IBroadcastAudioService audioService, SceneTree sceneTree, string? audioPath = null) 
            : base(id, BroadcastItemType.Transition, true, duration, eventBus, audioService, sceneTree, new { text, audioPath })
        {
            _text = text;
            _audioPath = audioPath;
        }

        protected override async Task ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(_audioPath))
            {
                await PlayAudioAsync(_audioPath, cancellationToken);
            }
            else
            {
                // Duration-based timing when no audio available
                await DelayAsync(_duration, cancellationToken);
            }
        }

        protected override BroadcastItem CreateBroadcastItem()
        {
            return new BroadcastItem(_id, _type, _text, _audioPath, _duration, new { Speaker = "TRANSITION" });
        }

        protected override async Task<float> GetAudioDurationAsync()
        {
            if (string.IsNullOrEmpty(_audioPath))
                return _duration;

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
    }
}