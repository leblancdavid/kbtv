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
        private SceneTreeTimer? _currentTimer; // Track active timer to prevent multiples

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
            GD.Print($"AudioDialoguePlayer.PlayLineAsync: Starting - SpeakerId={line.SpeakerId}, Type={line.Type}");

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
                // TEMPORARILY FORCED TIMEFALLBACK: Audio file not providing proper timing
                // Use timer to ensure consistent 4-second pacing
                GD.Print($"AudioDialoguePlayer: Audio loaded but using timer fallback for consistent timing");
                StartTimerFallback(4.0f);
            }
            else
            {
                // LoadAudioForLine already started timer for ads, or audio failed to load
                // Timer fallback already initiated
                GD.Print($"AudioDialoguePlayer: Timer fallback already initiated for {line.SpeakerId}");
            }
        }

        private void StartTimerFallback(float duration)
        {
            // Cancel any existing timer first
            if (_currentTimer != null)
            {
                _currentTimer.Disconnect("timeout", Callable.From(OnTimerTimeout));
                _currentTimer = null;
                GD.Print($"AudioDialoguePlayer: Cancelled previous timer");
            }

            _currentTimer = GetTree().CreateTimer(duration);
            _currentTimer.Timeout += OnTimerTimeout;
            GD.Print($"AudioDialoguePlayer: Started timer fallback for {duration}s");
        }

        private void OnTimerTimeout()
        {
            _currentTimer = null; // Clear reference
            GD.Print($"AudioDialoguePlayer: Timer fallback completed");
            OnAudioFinished();
        }

        public void Stop()
        {
            if (_audioPlayer?.Playing ?? false)
            {
                _audioPlayer.Stop();
            }

            // Cancel any active timer
            if (_currentTimer != null)
            {
                _currentTimer.Disconnect("timeout", Callable.From(OnTimerTimeout));
                _currentTimer = null;
                GD.Print($"AudioDialoguePlayer: Cancelled timer in Stop()");
            }

            _currentLineId = null;
        }

        private void OnAudioFinished()
        {
            GD.Print($"AudioDialoguePlayer.OnAudioFinished: Audio completed - _currentLineId={_currentLineId}");
            if (_currentLineId != null)
            {
                var completedEvent = new AudioCompletedEvent(_currentLineId, Speaker.Caller);
                LineCompleted?.Invoke(completedEvent);
                _currentLineId = null;
            }
            else
            {
                GD.PrintErr("AudioDialoguePlayer.OnAudioFinished: _currentLineId is null, not firing event");
            }
        }

        private AudioStream? LoadAudioForLine(BroadcastLine line)
        {
            if (line.Type == BroadcastLineType.Ad)
            {
                // Ads also need timer fallback due to audio file timing issues
                GD.Print($"AudioDialoguePlayer.LoadAudioForLine: Ad line - using timer fallback");
                StartTimerFallback(4.0f);
                return null; // Return null to trigger fallback logic
            }

            if (line.Type == BroadcastLineType.Music && (line.SpeakerId == "RETURN_MUSIC" || line.SpeakerId == "OUTRO_MUSIC"))
            {
                return LoadRandomReturnBumper();
            }

            GD.Print($"AudioDialoguePlayer.LoadAudioForLine: Using 4-second silent audio for {line.SpeakerId}");
            return GetSilentAudioFile();
        }

        private AudioStream? LoadRandomReturnBumper()
        {
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

            var random = new Random();
            var selectedFile = bumperFiles[random.Next(bumperFiles.Count)];
            var path = $"res://assets/audio/bumpers/Return/{selectedFile}";

            var audioStream = GD.Load<AudioStream>(path);
            if (audioStream == null)
            {
                GD.PrintErr($"AudioDialoguePlayer.LoadRandomReturnBumper: Failed to load {path}, using silent fallback");
                return GetSilentAudioFile();
            }

            GD.Print($"AudioDialoguePlayer: Selected return bumper: {selectedFile}");
            return audioStream;
        }

        private AudioStream? GetSilentAudioFile()
        {
            var audioStream = GD.Load<AudioStream>("res://assets/audio/silence_4sec.wav");
            if (audioStream == null)
            {
                GD.PrintErr("AudioDialoguePlayer.GetSilentAudioFile: Failed to load silent audio file - returning null!");
                return null;
            }
            GD.Print($"AudioDialoguePlayer.GetSilentAudioFile: Loaded silent audio successfully");
            return audioStream;
        }
    }
}
