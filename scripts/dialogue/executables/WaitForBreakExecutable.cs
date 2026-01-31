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
    /// Executable that waits for the break to officially start (T0).
    /// Used to create a clean silent window between Vern's break transition and ad start.
    /// </summary>
    public partial class WaitForBreakExecutable : BroadcastExecutable
    {
        private const float MAX_TIMEOUT_SECONDS = 20.0f; // Maximum wait time (break can't be more than 10s away)
        private readonly string _customMessage;

        public WaitForBreakExecutable(EventBus eventBus, IBroadcastAudioService audioService, SceneTree sceneTree, float duration = MAX_TIMEOUT_SECONDS, string? message = null)
            : base("wait_for_break", BroadcastItemType.Transition, true, duration, eventBus, audioService, sceneTree, null)
        {
            _customMessage = message ?? "Waiting for break to begin...";
        }

        protected override async Task ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();

            // Subscribe to interruption events
            void OnInterruption(BroadcastInterruptionEvent interruptionEvent)
            {
                GD.Print($"WaitForBreakExecutable: Received interruption: {interruptionEvent.Reason}");
                if (interruptionEvent.Reason == BroadcastInterruptionReason.BreakStarting ||
                    interruptionEvent.Reason == BroadcastInterruptionReason.ShowEnding)
                {
                    GD.Print($"WaitForBreakExecutable: {interruptionEvent.Reason} detected, completing wait");
                    tcs.TrySetResult(true);
                }
            }

            _eventBus.Subscribe<BroadcastInterruptionEvent>(OnInterruption);

            try
            {
                // Wait for either the break starting event or timeout
                var timeoutTask = Task.Delay((int)(_duration * 1000), cancellationToken);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    GD.PrintErr($"WaitForBreakExecutable: Timeout waiting for break to start after {_duration}s");
                    throw new TimeoutException("WaitForBreakExecutable timed out waiting for break to start");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
                // Always unsubscribe
                _eventBus.Unsubscribe<BroadcastInterruptionEvent>(OnInterruption);
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