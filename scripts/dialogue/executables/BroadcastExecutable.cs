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
    public abstract class BroadcastExecutable
    {
        protected string _id;
        protected BroadcastItemType _type;
        protected bool _requiresAwait;
        protected float _duration;
        protected EventBus _eventBus;
        protected object? _metadata;
        protected CancellationTokenSource? _cancellationTokenSource;
        protected IBroadcastAudioService _audioService;
        protected SceneTree _sceneTree;

        protected BroadcastExecutable(
            string id,
            BroadcastItemType type,
            bool requiresAwait,
            float duration,
            EventBus eventBus,
            IBroadcastAudioService audioService,
            SceneTree sceneTree,
            object? metadata = null)
        {
            _id = id;
            _type = type;
            _requiresAwait = requiresAwait;
            _duration = duration;
            _eventBus = eventBus;
            _audioService = audioService;
            _sceneTree = sceneTree;
            _metadata = metadata;
        }

        public string Id => _id;
        public BroadcastItemType Type => _type;
        public bool RequiresAwait => _requiresAwait;
        public float Duration => _duration;

        /// <summary>
        /// Execute the broadcast content asynchronously.
        /// </summary>
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                _cancellationTokenSource?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Source was already disposed, create new one
                _cancellationTokenSource = null;
            }
            
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                // Publish started event for UI
                var startedEvent = new BroadcastItemStartedEvent(
                    CreateBroadcastItem(),
                    _duration,
                    await GetAudioDurationAsync()
                );
                _eventBus.Publish(startedEvent);

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
        /// Publish interruption event.
        /// </summary>
        private void PublishInterruptedEvent()
        {
            var interruptedEvent = new BroadcastEvent(
                BroadcastEventType.Interrupted,
                _id,
                CreateBroadcastItem()
            );
            _eventBus.Publish(interruptedEvent);
        }

        /// <summary>
        /// Play audio with async completion handling.
        /// </summary>
        protected async Task PlayAudioAsync(string audioPath, CancellationToken cancellationToken)
        {
            await _audioService.PlayAudioAsync(audioPath, cancellationToken);
        }

        /// <summary>
        /// Non-blocking delay using Godot timers with cancellation support.
        /// </summary>
        protected async Task DelayAsync(float seconds, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);

            var timer = _sceneTree.CreateTimer(seconds);
            var tcs = new TaskCompletionSource<bool>();

            void OnTimeout()
            {
                tcs.TrySetResult(true);
            }

            timer.Timeout += OnTimeout;

            using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());

            try
            {
                await tcs.Task;
            }
            finally
            {
                timer.Timeout -= OnTimeout;
            }
        }

        /// <summary>
        /// Handle audio completion event.
        /// </summary>
        internal void OnAudioFinished()
        {
            // Audio finished naturally - this is handled by the PlayAudioAsync method
        }

        /// <summary>
        /// Clean up resources.
        /// </summary>
        public void Cleanup()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Source was already disposed, ignore
            }
            _cancellationTokenSource = null;
        }
    }
}