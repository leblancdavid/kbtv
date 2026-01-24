#nullable enable

using System;
using Godot;
using KBTV.Core;
using KBTV.Data;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Executes broadcast items by playing audio, displaying text, etc.
    /// Handles the actual execution of each broadcast item type.
    /// </summary>
    public class BroadcastItemExecutor
    {
        private AudioStreamPlayer? _audioPlayer;
        private readonly Action<string> _onItemCompleted;
        private readonly Node _sceneTreeNode;
        private readonly ITranscriptRepository _transcriptRepository;

        // Store current completion handlers for disconnection
        private Action? _currentAudioHandler;
        private Action? _currentTimerHandler;

        public BroadcastItemExecutor(Action<string> onItemCompleted, Node sceneTreeNode)
        {
            _onItemCompleted = onItemCompleted ?? throw new ArgumentNullException(nameof(onItemCompleted));
            _sceneTreeNode = sceneTreeNode ?? throw new ArgumentNullException(nameof(sceneTreeNode));
            
            // Get transcript repository for creating transcript entries
            _transcriptRepository = ServiceRegistry.Instance.TranscriptRepository;

            // Create audio player for music/voice playback
            _audioPlayer = new AudioStreamPlayer();
            // Add to scene tree so signals work properly
            _sceneTreeNode.AddChild(_audioPlayer);
        }

        public void ExecuteItem(BroadcastItem item)
        {
            // Create transcript entry for this item
            CreateTranscriptEntry(item);

            switch (item.Type)
            {
                case BroadcastItemType.Music:
                    PlayAudio(item);
                    break;

                case BroadcastItemType.VernLine:
                case BroadcastItemType.CallerLine:
                    // For lines, we display text and play audio
                    PlayAudio(item);
                    break;

                case BroadcastItemType.Ad:
                    // Ads use timer, publish UI event
                    var adEvent = new BroadcastItemStartedEvent(item, item.Duration, item.Duration);
                    ServiceRegistry.Instance.EventBus.Publish(adEvent);
                    StartTimer(item.Id, item.Duration);
                    break;

                case BroadcastItemType.DeadAir:
                    PlayAudio(item);
                    break;

                case BroadcastItemType.Transition:
                    // Transitions use timer, publish UI event
                    var transitionEvent = new BroadcastItemStartedEvent(item, item.Duration, item.Duration);
                    ServiceRegistry.Instance.EventBus.Publish(transitionEvent);
                    StartTimer(item.Id, item.Duration);
                    break;

                default:
                    GD.PrintErr($"BroadcastItemExecutor: Unknown item type {item.Type}");
                    // Fallback to timer with UI event
                    var fallbackEvent = new BroadcastItemStartedEvent(item, 
                        item.Duration > 0 ? item.Duration : 4.0f, 
                        item.Duration > 0 ? item.Duration : 4.0f);
                    ServiceRegistry.Instance.EventBus.Publish(fallbackEvent);
                    StartTimer(item.Id, item.Duration > 0 ? item.Duration : 4.0f);
                    break;
            }
        }

        private void PlayAudio(BroadcastItem item)
        {
            if (string.IsNullOrEmpty(item.AudioPath))
            {
                GD.Print($"BroadcastItemExecutor: No audio path for {item.Id}, using timer fallback");
                var duration = item.Duration > 0 ? item.Duration : 4.0f;
                StartAudioTimer(item, duration);
                return;
            }

            var audioStream = GD.Load<AudioStream>(item.AudioPath);
            if (audioStream == null)
            {
                GD.PrintErr($"BroadcastItemExecutor: Failed to load audio {item.AudioPath}, using timer fallback");
                var duration = item.Duration > 0 ? item.Duration : 4.0f;
                StartAudioTimer(item, duration);
                return;
            }

            // Get actual audio duration from the stream
            float audioDuration = GetAudioDuration(audioStream) ?? item.Duration;
            float displayDuration = audioDuration > 0 ? audioDuration : item.Duration;

            // Publish UI-specific event with duration information
            var startedEvent = new BroadcastItemStartedEvent(item, displayDuration, audioDuration);
            ServiceRegistry.Instance.EventBus.Publish(startedEvent);

            if (_audioPlayer != null)
            {
                _audioPlayer.Stream = audioStream;

                // Disconnect previous handler to prevent accumulation
                if (_currentAudioHandler != null)
                {
                    _audioPlayer.Finished -= _currentAudioHandler;
                }

                // Create and connect new completion handler
                _currentAudioHandler = () =>
                {
                    GD.Print($"BroadcastItemExecutor: Audio finished for {item.Id}");
                    _onItemCompleted(item.Id);
                };
                _audioPlayer.Finished += _currentAudioHandler;

                _audioPlayer.Play();
            }
        }

        private float? GetAudioDuration(AudioStream stream)
        {
            // For MP3 streams, try to get duration from the stream data
            // Note: Godot's AudioStreamMP3 doesn't expose Length directly, so we need to use fallback
            
            // For WAV streams, same issue - no direct Length property
            
            // Fallback to stream's length if available (some formats support this)
            try
            {
                var streamLength = stream.GetLength();
                if (streamLength > 0)
                {
                    return (float)streamLength;
                }
            }
            catch
            {
                // Audio duration detection failed, will use item duration
            }
            
            return null;
        }

        private void StartAudioTimer(BroadcastItem item, float duration)
        {
            // Publish UI event for timer-based items
            var startedEvent = new BroadcastItemStartedEvent(item, duration, duration);
            ServiceRegistry.Instance.EventBus.Publish(startedEvent);
            
            StartTimer(item.Id, duration);
        }

        private void DisplayText(BroadcastItem item)
        {
            // In real implementation, this would update UI components
            GD.Print($"BroadcastItemExecutor: Displaying text - {item.Text}");
        }

        private void CreateTranscriptEntry(BroadcastItem item)
        {
            if (_transcriptRepository == null)
            {
                GD.PrintErr("BroadcastItemExecutor: TranscriptRepository not available");
                return;
            }

// Map BroadcastItemType to Speaker
            Speaker speaker = item.Type switch
            {
                BroadcastItemType.VernLine => Speaker.Vern,
                BroadcastItemType.CallerLine => Speaker.Caller,
                BroadcastItemType.Music => Speaker.Music,
                BroadcastItemType.Ad => Speaker.System,
                BroadcastItemType.DeadAir => Speaker.Vern,
                BroadcastItemType.Transition => Speaker.System,
                _ => Speaker.System
            };

            // Create transcript entry with appropriate phase
            ConversationPhase transcriptPhase = item.Type switch
            {
                BroadcastItemType.Music => ConversationPhase.Intro,
                BroadcastItemType.VernLine => ConversationPhase.Probe,
                BroadcastItemType.CallerLine => ConversationPhase.Probe,
                BroadcastItemType.Ad => ConversationPhase.Resolution,
                BroadcastItemType.DeadAir => ConversationPhase.Intro,
                BroadcastItemType.Transition => ConversationPhase.Resolution,
                _ => ConversationPhase.Intro
            };

            // Format text for transcript
            string transcriptText = item.Text;
            if (item.Type == BroadcastItemType.Music)
            {
                transcriptText = "[MUSIC PLAYING]";
            }
            else if (item.Type == BroadcastItemType.Ad)
            {
                transcriptText = $"[AD BREAK] {item.Text}";
            }

            // Create and add transcript entry
            var entry = new TranscriptEntry(speaker, transcriptText, transcriptPhase);
            _transcriptRepository.AddEntry(entry);
            
            
        }

        private void StartTimer(string itemId, float duration)
        {
            GD.Print($"BroadcastItemExecutor: Starting timer for {duration}s on {itemId}");

            var tree = _sceneTreeNode.GetTree();
            if (tree == null)
            {
                GD.PrintErr($"BroadcastItemExecutor: Cannot create timer - node not in scene tree");
                return;
            }

            try
            {
                var timer = tree.CreateTimer(duration);

                // Disconnect previous timer handler (though timers are usually single-use)
                if (_currentTimerHandler != null)
                {
                    // Note: This won't work for SceneTreeTimer since we can't disconnect
                    // But SceneTreeTimer is single-use anyway, so accumulation shouldn't happen
                }

                // Connect completion handler
                _currentTimerHandler = () =>
                {
                    GD.Print($"BroadcastItemExecutor: Timer finished for {itemId}");
                    _onItemCompleted(itemId);
                };
                timer.Timeout += _currentTimerHandler;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"BroadcastItemExecutor: Failed to create timer: {ex.Message}");
            }
        }
    }
}