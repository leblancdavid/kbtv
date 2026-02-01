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
        /// Get the duration of an audio file at the specified path.
        /// </summary>
        protected async Task<float> GetAudioDurationAsync(string audioPath, float fallbackDuration = 4.0f)
        {
            if (string.IsNullOrEmpty(audioPath))
                return fallbackDuration;

            // If audio service is null or audio is disabled, don't attempt to load files
            if (_audioService == null || _audioService.IsAudioDisabled)
                return 0f;

            try
            {
                var audioStream = GD.Load<AudioStream>(audioPath);
                if (audioStream != null)
                {
                    return _audioService.GetAudioDuration(audioStream);
                }
                return fallbackDuration;
            }
            catch
            {
                return fallbackDuration;
            }
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
        /// Delay using Task.Delay for background thread compatibility.
        /// </summary>
        protected async Task DelayAsync(float seconds, CancellationToken cancellationToken)
        {
            await Task.Delay((int)(seconds * 1000), cancellationToken);
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