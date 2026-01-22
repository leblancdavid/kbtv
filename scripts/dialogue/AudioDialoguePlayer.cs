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
                GD.Print($"AudioDialoguePlayer.PlayLineAsync: Playing audio for {line.SpeakerId}");
                _audioPlayer.Stream = audioStream;
                _audioPlayer.Play();
            }
            else
            {
                GD.PrintErr($"AudioDialoguePlayer.PlayLineAsync: Audio failed to load for {line.SpeakerId}, using 4s timer fallback");
                StartTimerFallback(4.0f);
            }
        }

        private void StartTimerFallback(float duration)
        {
            var timer = GetTree().CreateTimer(duration);
            timer.Timeout += () =>
            {
                GD.Print($"AudioDialoguePlayer: Timer fallback completed after {duration}s");
                OnAudioFinished();
            };
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
                return GetSilentAudioFile();
            }

            if (line.Type == BroadcastLineType.Music && line.SpeakerId == "RETURN_MUSIC")
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
