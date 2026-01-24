#nullable enable

using System;
using Godot;
using KBTV.Core;

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

        // Store current completion handlers for disconnection
        private Action? _currentAudioHandler;
        private Action? _currentTimerHandler;

        public BroadcastItemExecutor(Action<string> onItemCompleted, Node sceneTreeNode)
        {
            _onItemCompleted = onItemCompleted ?? throw new ArgumentNullException(nameof(onItemCompleted));
            _sceneTreeNode = sceneTreeNode ?? throw new ArgumentNullException(nameof(sceneTreeNode));

            // Create audio player for music/voice playback
            _audioPlayer = new AudioStreamPlayer();
            // Add to scene tree so signals work properly
            _sceneTreeNode.AddChild(_audioPlayer);
        }

        public void ExecuteItem(BroadcastItem item)
        {
            GD.Print($"BroadcastItemExecutor: Executing {item.Type} - {item.Id}");

            switch (item.Type)
            {
                case BroadcastItemType.Music:
                    PlayAudio(item);
                    break;

                case BroadcastItemType.VernLine:
                case BroadcastItemType.CallerLine:
                    // For lines, we display text and play audio
                    DisplayText(item);
                    PlayAudio(item);
                    break;

                case BroadcastItemType.Ad:
                    // Ads might have their own handling
                    DisplayText(item);
                    StartTimer(item.Id, item.Duration);
                    break;

                case BroadcastItemType.DeadAir:
                    DisplayText(item);
                    PlayAudio(item);
                    break;

                case BroadcastItemType.Transition:
                    DisplayText(item);
                    StartTimer(item.Id, item.Duration);
                    break;

                default:
                    GD.PrintErr($"BroadcastItemExecutor: Unknown item type {item.Type}");
                    // Fallback to timer
                    StartTimer(item.Id, item.Duration > 0 ? item.Duration : 4.0f);
                    break;
            }
        }

        private void PlayAudio(BroadcastItem item)
        {
            if (string.IsNullOrEmpty(item.AudioPath))
            {
                GD.Print($"BroadcastItemExecutor: No audio path for {item.Id}, using timer fallback");
                StartTimer(item.Id, item.Duration > 0 ? item.Duration : 4.0f);
                return;
            }

            var audioStream = GD.Load<AudioStream>(item.AudioPath);
            if (audioStream == null)
            {
                GD.PrintErr($"BroadcastItemExecutor: Failed to load audio {item.AudioPath}, using timer fallback");
                StartTimer(item.Id, item.Duration > 0 ? item.Duration : 4.0f);
                return;
            }

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
                GD.Print($"BroadcastItemExecutor: Started playing audio for {item.Id}");
            }
        }

        private void DisplayText(BroadcastItem item)
        {
            // In real implementation, this would update UI components
            GD.Print($"BroadcastItemExecutor: Displaying text - {item.Text}");
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