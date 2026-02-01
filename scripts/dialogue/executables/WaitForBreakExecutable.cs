#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using KBTV.Audio;
using KBTV.Core;
using KBTV.Callers;
using KBTV.Managers;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Executable that waits for the break to officially start (T0).
    /// Used to create a clean silent window between Vern's break transition and ad start.
    /// </summary>
    public partial class WaitForBreakExecutable : BroadcastExecutable
    {
        private const float MAX_TIMEOUT_SECONDS = 20.0f; // Maximum wait time (break can't be more than 10s away)
        private readonly ICallerRepository _callerRepository;
        private readonly string _customMessage;

        public WaitForBreakExecutable(EventBus eventBus, IBroadcastAudioService audioService, SceneTree sceneTree, ICallerRepository callerRepository, float duration = MAX_TIMEOUT_SECONDS, string? message = null)
            : base("wait_for_break", BroadcastItemType.Transition, true, duration, eventBus, audioService, sceneTree, null)
        {
            _callerRepository = callerRepository;
            _customMessage = message ?? "Waiting for break to begin...";
        }

        protected override async Task ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            // Just wait for the calculated duration
            await Task.Delay((int)(_duration * 1000), cancellationToken);
            
            // Drop the on-air caller at break start
            var onAirCaller = _callerRepository.OnAirCaller;
            if (onAirCaller != null)
            {
                GD.Print($"WaitForBreakExecutable: Dropping on-air caller '{onAirCaller.Name}' at break start");
                _callerRepository.SetCallerState(onAirCaller, CallerState.Disconnected);
                _callerRepository.RemoveCaller(onAirCaller);
            }
        }

        protected override BroadcastItem CreateBroadcastItem()
        {
            return new BroadcastItem(
                _id,
                _type,
                _customMessage,
                null, // No audio
                _duration,
                new { Purpose = "BreakTransitionWait" }
            );
        }
    }
}