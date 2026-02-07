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
    /// Plays continuous bleep sound with fade in/out effects and displays FCC violation message.
    /// </summary>
    public partial class CursingDelayExecutable : BroadcastExecutable
    {
        private AudioStreamPlayer? _bleepPlayer;
        private bool _isBleeping;

        public CursingDelayExecutable(EventBus eventBus, IBroadcastAudioService audioService, SceneTree sceneTree)
            : base("cursing_delay", BroadcastItemType.CursingDelay, true, 20.0f, eventBus, audioService, sceneTree, null)
        {
        }

        protected override async Task ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            Log.Debug("CursingDelayExecutable: Starting 20-second cursing timer delay with continuous bleep");

            // Start looping bleep with fade effects
            StartRepeatingBleepFade();

            // Publish FCC violation message with red styling metadata
            var item = CreateBroadcastItem();
            var startedEvent = new BroadcastItemStartedEvent(item, 20.0f, 20.0f);
            _eventBus.Publish(startedEvent);

            // Create a task completion source that will complete when timer finishes
            var tcs = new TaskCompletionSource<bool>();

            // Handler for timer completion event
            void OnTimerCompleted(CursingTimerCompletedEvent @event)
            {
                Log.Debug($"CursingDelayExecutable: Timer completed - Successful: {@event.WasSuccessful}");
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
                    Log.Debug("CursingDelayExecutable: Timer completed via timeout (UI may have failed)");
                }
                // If completionTask finished first, OnTimerCompleted was called
            }
            catch (OperationCanceledException)
            {
                Log.Debug("CursingDelayExecutable: Cancelled during execution");
                throw;
            }
            finally
            {
                // Stop bleep loop and clean up
                StopBleepLoop();
                
                // Unsubscribe from timer completion events
                _eventBus.Unsubscribe<CursingTimerCompletedEvent>(OnTimerCompleted);
                Log.Debug("CursingDelayExecutable: Delay completed, broadcast can continue");
            }
        }

        private void StartRepeatingBleepFade()
        {
            if (_audioService == null || _sceneTree == null)
            {
                Log.Error("CursingDelayExecutable: AudioService or SceneTree not available for bleep playback");
                return;
            }

            // Create audio stream player in scene tree
            _bleepPlayer = new AudioStreamPlayer();
            _sceneTree.Root.AddChild(_bleepPlayer);

            // Load bleep sound
            var bleepStream = ResourceLoader.Load<AudioStream>("res://assets/audio/bleep.wav");
            if (bleepStream == null)
            {
                Log.Error("CursingDelayExecutable: Could not load bleep.wav - file may be missing or corrupted");
                return;
            }

            _bleepPlayer.Stream = bleepStream;
            _bleepPlayer.VolumeDb = -6f; // Start audible for testing (was -20f)
            _bleepPlayer.Bus = "Master"; // Use master bus

            // Verify setup
            if (_bleepPlayer.Stream == null)
            {
                Log.Error("CursingDelayExecutable: Stream not set on player after assignment");
                return;
            }


            _isBleeping = true;
            StartFadeCycle();
        }

        private async void StartFadeCycle()
        {
            if (_bleepPlayer == null) return;

            while (_isBleeping)
            {
                // Fade in over 0.2 seconds
                await FadeVolume(-20f, -6f, 0.2f);
                
                // Full volume for 0.5 seconds
                await Task.Delay(500);
                
                // Fade out over 0.3 seconds  
                await FadeVolume(-6f, -20f, 0.3f);
                
                // Silence for 0.5 seconds before repeating
                await Task.Delay(500);
            }
        }

        private async Task FadeVolume(float fromDb, float toDb, float duration)
        {
            if (_bleepPlayer == null) return;

            var startTime = Time.GetTicksMsec() / 1000.0f;
            var endTime = startTime + duration;

            while (Time.GetTicksMsec() / 1000.0f < endTime && _isBleeping)
            {
                var t = Mathf.InverseLerp(startTime, endTime, Time.GetTicksMsec() / 1000.0f);
                _bleepPlayer.VolumeDb = Mathf.Lerp(fromDb, toDb, t);
                await Task.Delay(16); // ~60fps
            }
        }

        private void StopBleepLoop()
        {
            _isBleeping = false;
            
            if (_bleepPlayer != null)
            {
                _bleepPlayer.Stop();
                _bleepPlayer.QueueFree();
                _bleepPlayer = null;
            }
        }

        protected override BroadcastItem CreateBroadcastItem()
        {
            return new BroadcastItem(
                id: _id,
                type: _type,
                text: "FCC Violation detected!",
                audioPath: null,
                duration: _duration,
                metadata: new { 
                    IsFccViolation = true, // Flag for red styling in UI
                    TimerActive = true 
                }
            );
        }
    }
}