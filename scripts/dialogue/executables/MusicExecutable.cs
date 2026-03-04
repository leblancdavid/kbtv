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
        private readonly AudioStream? _loadedAudio;

        public MusicExecutable(string id, string description, string audioPath, float duration, EventBus eventBus, IBroadcastAudioService audioService, SceneTree sceneTree) 
            : base(id, BroadcastItemType.Music, true, duration, eventBus, audioService, sceneTree, new { audioPath, description })
        {
            _audioPath = audioPath;
            _description = description;

            if ((id == "INTRO_MUSIC" || id == "RETURN_MUSIC") && audioService is BroadcastAudioService bas)
            {
                _loadedAudio = id == "INTRO_MUSIC" 
                    ? bas.LoadRandomIntroBumper() 
                    : bas.LoadRandomReturnBumper();
            }
        }

        protected override async Task ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            Log.Debug($"MusicExecutable: Playing music - {_description}");

            if (_loadedAudio != null)
            {
                await _audioService.PlayAudioStreamAsync(_loadedAudio, cancellationToken);
                return;
            }

            await PlayAudioAsync(_audioPath, cancellationToken);
        }

        protected override BroadcastItem CreateBroadcastItem()
        {
            return new BroadcastItem(_id, _type, _description, _audioPath, _duration, new { Speaker = "MUSIC" });
        }

        public override async Task<float> GetEstimatedDurationAsync()
        {
            if (_loadedAudio != null && _audioService is BroadcastAudioService bas)
            {
                return bas.GetAudioDuration(_loadedAudio);
            }

            float audioDuration = await GetAudioDurationAsync(_audioPath, _duration);
            if (audioDuration > 0)
                return audioDuration;

            return _duration;
        }

        protected override async Task<float> GetAudioDurationAsync()
        {
            if (_loadedAudio != null && _audioService is BroadcastAudioService bas)
            {
                return bas.GetAudioDuration(_loadedAudio);
            }
            return await GetAudioDurationAsync(_audioPath, _duration);
        }
    }
}