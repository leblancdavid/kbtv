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

            var audioStream = LoadAudioForLine(line);
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

        private AudioStream? LoadAudioForLine(BroadcastLine line)
        {
            // Special handling for ad breaks - always 4 seconds
            if (line.Type == BroadcastLineType.Ad)
            {
                return GetSilentAudioFile();
            }

            // Special handling for return bumper music - random selection
            if (line.Type == BroadcastLineType.Music && line.SpeakerId == "RETURN_MUSIC")
            {
                return LoadRandomReturnBumper();
            }

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
            return CreatePlaceholderAudio(duration);
        }

        private AudioStream? LoadRandomReturnBumper()
        {
            // Get list of return bumper files
            var returnBumperDir = DirAccess.Open("res://assets/audio/bumpers/Return");
            if (returnBumperDir == null)
            {
                GD.PrintErr("AudioDialoguePlayer.LoadRandomReturnBumper: Return bumper directory not found, using silent fallback");
                return GetSilentAudioFile();
            }

            var bumperFiles = new System.Collections.Generic.List<string>();
            returnBumperDir.ListDirBegin();
            string fileName = returnBumperDir.GetNext();
            while (fileName != "")
            {
                if (!fileName.StartsWith(".") && (fileName.EndsWith(".ogg") || fileName.EndsWith(".wav") || fileName.EndsWith(".mp3")))
                {
                    bumperFiles.Add(fileName);
                }
                fileName = returnBumperDir.GetNext();
            }
            returnBumperDir.ListDirEnd();

            if (bumperFiles.Count == 0)
            {
                GD.PrintErr("AudioDialoguePlayer.LoadRandomReturnBumper: No return bumper files found, using silent fallback");
                return GetSilentAudioFile();
            }

            // Randomly select one
            var random = new Random();
            var selectedFile = bumperFiles[random.Next(bumperFiles.Count)];
            var audioPath = $"res://assets/audio/bumpers/Return/{selectedFile}";

            var audioStream = GD.Load<AudioStream>(audioPath);
            if (audioStream == null)
            {
                GD.PrintErr($"AudioDialoguePlayer.LoadRandomReturnBumper: Failed to load {audioPath}, using silent fallback");
                return GetSilentAudioFile();
            }

            GD.Print($"AudioDialoguePlayer: Selected return bumper: {selectedFile}");
            return audioStream;
        }

        /// <summary>
        /// Loads the 4-second silent WAV file for timing-critical scenarios.
        /// </summary>
        private AudioStream? GetSilentAudioFile()
        {
            var audioStream = GD.Load<AudioStream>("res://assets/audio/silence_4sec.wav");
            if (audioStream == null)
            {
                GD.PrintErr("AudioDialoguePlayer.GetSilentAudioFile: Failed to load silent audio file");
                return null;
            }
            return audioStream;
        }

        /// <summary>
        /// Creates a placeholder audio stream for dialogue with flexible duration.
        /// </summary>
        private AudioStream CreatePlaceholderAudio(float duration)
        {
            return new AudioStreamGenerator { MixRate = 44100 };
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