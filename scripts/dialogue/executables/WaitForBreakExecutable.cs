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
        private const float TIMEOUT_SECONDS = 30.0f; // Prevent infinite waiting

        public WaitForBreakExecutable(EventBus eventBus, IBroadcastAudioService audioService, SceneTree sceneTree)
            : base("wait_for_break", BroadcastItemType.Transition, true, TIMEOUT_SECONDS, eventBus, audioService, sceneTree, null)
        {
        }

        protected override async Task ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            GD.Print("WaitForBreakExecutable: Starting wait for break to begin");

            // Create a task completion source to wait for the break starting event
            var tcs = new TaskCompletionSource<bool>();

            // Subscribe to interruption events
            void OnInterruption(BroadcastInterruptionEvent interruptionEvent)
            {
                GD.Print($"WaitForBreakExecutable: Received interruption: {interruptionEvent.Reason}");
                if (interruptionEvent.Reason == BroadcastInterruptionReason.BreakStarting)
                {
                    GD.Print("WaitForBreakExecutable: Break starting detected, completing wait");
                    tcs.TrySetResult(true);
                }
            }

            _eventBus.Subscribe<BroadcastInterruptionEvent>(OnInterruption);

            try
            {
                // Wait for either the break starting event or timeout
                var timeoutTask = Task.Delay((int)(TIMEOUT_SECONDS * 1000), cancellationToken);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    GD.PrintErr($"WaitForBreakExecutable: Timeout waiting for break to start after {TIMEOUT_SECONDS}s");
                    throw new TimeoutException("WaitForBreakExecutable timed out waiting for break to start");
                }

                GD.Print("WaitForBreakExecutable: Break wait completed successfully");
            }
            catch (OperationCanceledException)
            {
                GD.Print("WaitForBreakExecutable: Wait cancelled");
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
                "Waiting for break to begin...",
                null, // No audio
                _duration,
                new { Purpose = "BreakTransitionWait" }
            );
        }
    }
}