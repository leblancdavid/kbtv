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

        public MusicExecutable(string id, string description, string audioPath, float duration, EventBus eventBus, IBroadcastAudioService audioService, SceneTree sceneTree) 
            : base(id, BroadcastItemType.Music, true, duration, eventBus, audioService, sceneTree, new { audioPath, description })
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
            return await GetAudioDurationAsync(_audioPath, _duration);
        }
    }
}