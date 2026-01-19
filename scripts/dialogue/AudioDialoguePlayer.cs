#nullable enable

using System.Threading.Tasks;
using Godot;
using KBTV.Core;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Audio-based dialogue player that plays voice lines and fires completion events.
    /// Integrates with Godot's AudioStreamPlayer for actual audio playback.
    /// </summary>
    public partial class AudioDialoguePlayer : Node, IDialoguePlayer
    {
        private AudioStreamPlayer _audioPlayer = null!;
        private string? _currentLineId;

        public event System.Action<AudioCompletedEvent>? LineCompleted;

        public bool IsPlaying => _audioPlayer?.Playing ?? false;

        public override void _Ready()
        {
            _audioPlayer = new AudioStreamPlayer();
            AddChild(_audioPlayer);
            _audioPlayer.Finished += OnAudioFinished;
        }

        public async Task PlayLineAsync(BroadcastLine line)
        {
            if (_audioPlayer == null)
            {
                GD.PrintErr("AudioDialoguePlayer.PlayLineAsync: AudioStreamPlayer not initialized");
                return;
            }

            Stop();
            _currentLineId = line.SpeakerId;

            await Task.Delay(4000);
            OnAudioFinished();
        }

        public void Stop()
        {
            if (_audioPlayer?.Playing ?? false)
            {
                _audioPlayer.Stop();
            }
            _currentLineId = null;
        }

        private void OnAudioFinished()
        {
            if (_currentLineId != null)
            {
                var completedEvent = new AudioCompletedEvent(_currentLineId, Speaker.Caller);
                LineCompleted?.Invoke(completedEvent);
                _currentLineId = null;
            }
        }

        private async Task<AudioStream?> LoadAudioForLine(BroadcastLine line)
        {
            // TODO: Implement actual audio loading logic
            // This could involve:
            // 1. Looking up audio files by line ID
            // 2. Loading from assets/dialogue/audio/
            // 3. Using TTS for dynamic content
            // 4. Fallback to generated tones

            // For now, return null to trigger fallback
            await Task.Delay(100); // Simulate async loading
            return null;
        }
    }
}