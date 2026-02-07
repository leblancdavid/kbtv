#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using KBTV.Audio;
using KBTV.Broadcast;
using KBTV.Core;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Executable that waits for the cursing timer to complete.
    /// Blocks the broadcast loop during the 20-second cursing penalty window.
    /// </summary>
    public partial class CursingDelayExecutable : BroadcastExecutable
    {
        public CursingDelayExecutable(EventBus eventBus)
            : base("cursing_delay", BroadcastItemType.CursingDelay, true, 20.0f, eventBus, null!, null!, null)
        {
        }

        protected override async Task ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            GD.Print("CursingDelayExecutable: Starting 20-second cursing timer delay");

            // Create a task completion source that will complete when timer finishes
            var tcs = new TaskCompletionSource<bool>();

            // Handler for timer completion event
            void OnTimerCompleted(CursingTimerCompletedEvent @event)
            {
                GD.Print($"CursingDelayExecutable: Timer completed - Successful: {@event.WasSuccessful}");
                tcs.SetResult(true);
            }

            // Subscribe to timer completion events
            _eventBus.Subscribe<CursingTimerCompletedEvent>(OnTimerCompleted);

            try
            {
                // Wait for timer completion OR 20-second timeout as safety net
                var delayTask = Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
                var completionTask = tcs.Task;

                var completedTask = await Task.WhenAny(completionTask, delayTask);

                if (completedTask == delayTask)
                {
                    GD.Print("CursingDelayExecutable: Timer completed via timeout (UI may have failed)");
                }
                // If completionTask finished first, OnTimerCompleted was called
            }
            catch (OperationCanceledException)
            {
                GD.Print("CursingDelayExecutable: Cancelled during execution");
                throw;
            }
            finally
            {
                // Clean up subscription
                _eventBus.Unsubscribe<CursingTimerCompletedEvent>(OnTimerCompleted);
                GD.Print("CursingDelayExecutable: Delay completed, broadcast can continue");
            }
        }

        protected override BroadcastItem CreateBroadcastItem()
        {
            return new BroadcastItem(
                id: _id,
                type: _type,
                text: "Cursing penalty timer active...",
                audioPath: null,
                duration: _duration,
                metadata: new { TimerActive = true }
            );
        }
    }
}