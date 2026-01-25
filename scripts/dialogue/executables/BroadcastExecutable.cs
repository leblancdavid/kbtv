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
    /// Base class for all broadcast executables with configurable async behavior.
    /// Each executable type controls whether it requires awaiting and handles interruption.
    /// </summary>
    public abstract partial class BroadcastExecutable : Node
    {
        protected readonly string _id;
        protected readonly BroadcastItemType _type;
        protected readonly bool _requiresAwait;
        protected readonly float _duration;
        protected readonly object? _metadata;
        protected AudioStreamPlayer? _audioPlayer;
        protected CancellationTokenSource? _cancellationTokenSource;

        protected BroadcastExecutable(
            string id,
            BroadcastItemType type,
            bool requiresAwait,
            float duration,
            object? metadata = null)
        {
            _id = id;
            _type = type;
            _requiresAwait = requiresAwait;
            _duration = duration;
            _metadata = metadata;
        }

        public string Id => _id;
        public BroadcastItemType Type => _type;
        public bool RequiresAwait => _requiresAwait;
        public float Duration => _duration;
        public object? Metadata => _metadata;

        /// <summary>
        /// Initialize the executable (called after service registry is available).
        /// </summary>
        public virtual void Initialize()
        {
            _audioPlayer = new AudioStreamPlayer();
            AddChild(_audioPlayer);
            _audioPlayer.Finished += OnAudioFinished;
        }

        /// <summary>
        /// Execute the broadcast content asynchronously.
        /// </summary>
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                // Publish started event for UI
                var startedEvent = new BroadcastItemStartedEvent(
                    CreateBroadcastItem(),
                    _duration,
                    await GetAudioDurationAsync()
                );
                ServiceRegistry.Instance.EventBus.Publish(startedEvent);

                // Execute the specific content
                await ExecuteInternalAsync(_cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Handle interruption gracefully
                PublishInterruptedEvent();
                throw;
            }
        }

        /// <summary>
        /// Internal execution method implemented by subclasses.
        /// </summary>
        protected abstract Task ExecuteInternalAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Create a BroadcastItem for this executable.
        /// </summary>
        protected abstract BroadcastItem CreateBroadcastItem();

        /// <summary>
        /// Get the actual audio duration if available.
        /// </summary>
        protected virtual async Task<float> GetAudioDurationAsync()
        {
            return 0f; // Default fallback
        }

        /// <summary>
        /// Play audio with async completion handling.
        /// </summary>
        protected async Task PlayAudioAsync(string audioPath, CancellationToken cancellationToken)
        {
            if (_audioPlayer == null || string.IsNullOrEmpty(audioPath))
            {
                // Fallback to duration-based timing
                await Task.Delay(TimeSpan.FromSeconds(_duration), cancellationToken);
                return;
            }

            try
            {
                var audioStream = GD.Load<AudioStream>(audioPath);
                if (audioStream != null)
                {
                    _audioPlayer.Stream = audioStream;
                    _audioPlayer.Play();

                    // Wait for audio to finish or cancellation
                    var completionTask = Task.Run(async () =>
                    {
                        while (_audioPlayer.Playing && !cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(50, cancellationToken);
                        }
                    }, cancellationToken);

                    await completionTask;
                }
                else
                {
                    // Fallback if audio not found
                    await Task.Delay(TimeSpan.FromSeconds(_duration), cancellationToken);
                }
            }
            catch (Exception)
            {
                // Fallback on any error
                await Task.Delay(TimeSpan.FromSeconds(_duration), cancellationToken);
            }
        }

        /// <summary>
        /// Handle audio completion event.
        /// </summary>
        private void OnAudioFinished()
        {
            // Audio finished naturally - this is handled by the PlayAudioAsync method
        }

        /// <summary>
        /// Publish interruption event.
        /// </summary>
        private void PublishInterruptedEvent()
        {
            var interruptedEvent = new BroadcastEvent(
                BroadcastEventType.Interrupted,
                _id,
                CreateBroadcastItem()
            );
            ServiceRegistry.Instance.EventBus.Publish(interruptedEvent);
        }

        /// <summary>
        /// Clean up resources.
        /// </summary>
        public void Cleanup()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            
            if (_audioPlayer != null)
            {
                _audioPlayer.Finished -= OnAudioFinished;
                _audioPlayer.Stop();
            }
        }

        public override void _ExitTree()
        {
            Cleanup();
            base._ExitTree();
        }
    }
}