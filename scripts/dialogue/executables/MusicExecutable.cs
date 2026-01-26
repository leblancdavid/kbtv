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
    /// Executable for playing music (show openings, closings, bumpers).
    /// </summary>
    public partial class MusicExecutable : BroadcastExecutable
    {
        private readonly string _audioPath;
        private readonly string _description;

        public MusicExecutable(string id, string description, string audioPath, float duration, EventBus eventBus) 
            : base(id, BroadcastItemType.Music, true, duration, eventBus, new { audioPath, description })
        {
            _audioPath = audioPath;
            _description = description;
        }

        protected override async Task ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            GD.Print($"MusicExecutable: Playing music - {_description}");
            
            await PlayAudioAsync(_audioPath, cancellationToken);
        }

        protected override BroadcastItem CreateBroadcastItem()
        {
            return new BroadcastItem(_id, _type, _description, _audioPath, _duration, new { Speaker = "MUSIC" });
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
                
                // For other stream types, try to get length
                var length = audioStream?.GetLength() ?? 0.0;
                return length > 0 ? (float)length : _duration;
            }
            catch
            {
                return _duration; // Fallback to configured duration
            }
        }
    }
}