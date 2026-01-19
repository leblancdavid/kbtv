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
            GD.Print("AudioDialoguePlayer._Ready: Initializing");
            _audioPlayer = new AudioStreamPlayer();
            AddChild(_audioPlayer);
            _audioPlayer.Finished += OnAudioFinished;
            GD.Print("AudioDialoguePlayer._Ready: Event subscription set up");
        }

        public async Task PlayLineAsync(BroadcastLine line)
        {
            GD.Print($"AudioDialoguePlayer.PlayLineAsync: Called with line {line.Speaker}: {line.Text}");

            if (_audioPlayer == null)
            {
                GD.PrintErr("AudioDialoguePlayer.PlayLineAsync: AudioStreamPlayer not initialized");
                return;
            }

            // Stop any current playback
            Stop();

            _currentLineId = line.SpeakerId;

            GD.Print($"AudioDialoguePlayer.PlayLineAsync: Simulating 4s playback for {line.Speaker}: {line.Text}");

            // Simulate 4-second audio playback delay
            await Task.Delay(4000);

            // Fire completion event
            GD.Print($"AudioDialoguePlayer.PlayLineAsync: Playback completed, firing completion event for {_currentLineId}");
            OnAudioFinished();
        }

        public void Stop()
        {
            if (_audioPlayer?.Playing ?? false)
            {
                _audioPlayer.Stop();
                GD.Print("AudioDialoguePlayer: Playback stopped");
            }
            _currentLineId = null;
        }

        private void OnAudioFinished()
        {
            GD.Print($"AudioDialoguePlayer.OnAudioFinished: Called with _currentLineId={_currentLineId}");

            if (_currentLineId != null)
            {
                var completedEvent = new AudioCompletedEvent(_currentLineId, Speaker.Caller); // TODO: Determine speaker from line
                GD.Print($"AudioDialoguePlayer.OnAudioFinished: Firing AudioCompletedEvent for {_currentLineId}");
                LineCompleted?.Invoke(completedEvent);
                GD.Print($"AudioDialoguePlayer.OnAudioFinished: Event fired, resetting _currentLineId");
                _currentLineId = null;
            }
            else
            {
                GD.Print("AudioDialoguePlayer.OnAudioFinished: No current line ID");
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