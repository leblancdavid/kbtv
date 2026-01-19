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
            GD.Print($"[AudioDialoguePlayer] _Ready() called, IsInsideTree={IsInsideTree()}, Parent={GetParent()?.Name ?? "null"}");
            _audioPlayer = new AudioStreamPlayer();
            AddChild(_audioPlayer);
            GD.Print($"[AudioDialoguePlayer] _Ready(): AudioStreamPlayer created, Playing={_audioPlayer.Playing}");
            _audioPlayer.Finished += OnAudioFinished;
            GD.Print($"[AudioDialoguePlayer] _Ready() completed");
        }

        public async void PlayLineAsync(BroadcastLine line)
        {
            GD.Print($"[AudioDialoguePlayer] PlayLineAsync() called, _audioPlayer={(_audioPlayer != null ? "initialized" : "NULL")}");
            if (_audioPlayer == null)
            {
                GD.PrintErr("AudioDialoguePlayer.PlayLineAsync: AudioStreamPlayer not initialized");
                return;
            }

            Stop();
            _currentLineId = line.SpeakerId;
            GD.Print($"[AudioDialoguePlayer] _currentLineId set to: '{_currentLineId}' (line.SpeakerId='{line.SpeakerId}')");
            GD.Print($"[AudioDialoguePlayer] Starting playback for line: {line.SpeakerId}, text='{line.Text}', IsInsideTree={IsInsideTree()}, Parent={GetParent()?.Name ?? "null"}");

            // Load audio for the line (real audio or silent stream)
            var audioStream = await LoadAudioForLine(line);
            if (audioStream != null)
            {
                _audioPlayer.Stream = audioStream;
                _audioPlayer.Play();
                GD.Print($"[AudioDialoguePlayer] Playing audio stream");
            }
            else
            {
                // No audio available, fire completion immediately
                GD.Print($"[AudioDialoguePlayer] No audio stream available, firing completion immediately");
                OnAudioFinished();
            }
        }

        public void Stop()
        {
            GD.Print($"[AudioDialoguePlayer] Stop() called, _currentLineId='{_currentLineId ?? "null"}'");
            if (_audioPlayer?.Playing ?? false)
            {
                _audioPlayer.Stop();
            }
            _currentLineId = null;
        }

        private void OnAudioFinished()
        {
            GD.Print($"[AudioDialoguePlayer] OnAudioFinished called, _currentLineId={_currentLineId}");
            if (_currentLineId != null)
            {
                var completedEvent = new AudioCompletedEvent(_currentLineId, Speaker.Caller);
                GD.Print($"[AudioDialoguePlayer] Invoking LineCompleted event for {_currentLineId}");
                LineCompleted?.Invoke(completedEvent);
                _currentLineId = null;
            }
            else
            {
                GD.Print($"[AudioDialoguePlayer] OnAudioFinished: _currentLineId is null, skipping event");
            }
        }

        private async Task<AudioStream?> LoadAudioForLine(BroadcastLine line)
        {
            // Try to load actual audio file for this line
            var audioPath = $"res://assets/dialogue/audio/{line.SpeakerId}.wav";
            var audioStream = GD.Load<AudioStream>(audioPath);
            if (audioStream != null)
            {
                GD.Print($"[AudioDialoguePlayer] Loaded audio file: {audioPath}");
                return audioStream;
            }

            // Check for MP3
            audioPath = $"res://assets/dialogue/audio/{line.SpeakerId}.mp3";
            audioStream = GD.Load<AudioStream>(audioPath);
            if (audioStream != null)
            {
                GD.Print($"[AudioDialoguePlayer] Loaded audio file: {audioPath}");
                return audioStream;
            }

            // Generate silent audio stream with duration matching line text length
            var duration = CalculateDurationForText(line.Text);
            GD.Print($"[AudioDialoguePlayer] No audio file found, creating silent stream with duration: {duration}s");
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