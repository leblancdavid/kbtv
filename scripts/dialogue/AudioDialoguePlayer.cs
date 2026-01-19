#nullable enable

using System;
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

        public async void PlayLineAsync(BroadcastLine line)
        {
            if (_audioPlayer == null)
            {
                GD.PrintErr("AudioDialoguePlayer.PlayLineAsync: AudioStreamPlayer not initialized");
                return;
            }

            Stop();
            _currentLineId = line.SpeakerId;

            var audioStream = await LoadAudioForLine(line);
            if (audioStream != null)
            {
                _audioPlayer.Stream = audioStream;
                _audioPlayer.Play();
            }
            else
            {
                OnAudioFinished();
            }
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
            var audioPath = $"res://assets/dialogue/audio/{line.SpeakerId}.wav";
            var audioStream = GD.Load<AudioStream>(audioPath);
            if (audioStream != null)
            {
                return audioStream;
            }

            audioPath = $"res://assets/dialogue/audio/{line.SpeakerId}.mp3";
            audioStream = GD.Load<AudioStream>(audioPath);
            if (audioStream != null)
            {
                return audioStream;
            }

            var duration = CalculateDurationForText(line.Text);
            return CreateSilentAudioStream(duration);
        }

        private AudioStream CreateSilentAudioStream(float duration)
        {
            var sampleStream = new AudioStreamGenerator
            {
                MixRate = 44100
            };

            return sampleStream;
        }

        private float CalculateDurationForText(string text)
        {
            // Average speaking rate: ~150 words per minute = ~2.5 words per second
            // Average word length: ~5 characters
            // So roughly 12.5 characters per second
            // Add some padding for pauses between words

            if (string.IsNullOrEmpty(text))
            {
                return 1.0f;
            }

            var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            var duration = Mathf.Max(1.0f, wordCount * 0.4f);
            return duration;
        }
    }
}