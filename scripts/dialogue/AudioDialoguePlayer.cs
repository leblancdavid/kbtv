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

            // Try to load voice audio files
            var voiceAudio = LoadVoiceAudioForLine(line);
            if (voiceAudio != null)
            {
                GD.Print($"AudioDialoguePlayer.LoadAudioForLine: Loaded voice audio for {line.SpeakerId}");
                return voiceAudio;
            }

            // Fallback to silent audio
            GD.Print($"AudioDialoguePlayer.LoadAudioForLine: No voice audio found, using 4-second silent audio for {line.SpeakerId}");
            return GetSilentAudioFile();
        }

        private AudioStream? LoadVoiceAudioForLine(BroadcastLine line)
        {
            string audioPath = "";

            if (line.Type == BroadcastLineType.CallerDialogue)
            {
                // Load caller audio: res://assets/audio/voice/Callers/{topic}/{arc_id}_{gender}_{line_index}.mp3
                if (!string.IsNullOrEmpty(line.ArcId) && !string.IsNullOrEmpty(line.CallerGender))
                {
                    // Map topic from arc ID (first part before first underscore)
                    string topic = line.ArcId.Split('_')[0];
                    audioPath = $"res://assets/audio/voice/Callers/{topic}/{line.ArcId}_{line.CallerGender}_{line.LineIndex}.mp3";
                }
            }
            else if (line.Type == BroadcastLineType.VernDialogue)
            {
                // Load Vern conversation audio: res://assets/audio/voice/Vern/ConversationArcs/{arc_id}/{mood}/vern_{arc_id}_{mood}_{line_index}_{total}.mp3
                if (!string.IsNullOrEmpty(line.ArcId))
                {
                    string mood = GetVernMood();
                    // Try different total counts (we'll find the right one by checking existence)
                    for (int total = 1; total <= 10; total++)
                    {
                        audioPath = $"res://assets/audio/voice/Vern/ConversationArcs/{line.ArcId}/{mood}/vern_{line.ArcId}_{mood}_{line.LineIndex:D3}_{total:D3}.mp3";
                        var testStream = GD.Load<AudioStream>(audioPath);
                        if (testStream != null)
                        {
                            return testStream;
                        }
                    }
                }
            }
            else if (line.Type == BroadcastLineType.ShowOpening || line.Type == BroadcastLineType.BetweenCallers ||
                     line.Type == BroadcastLineType.DeadAirFiller || line.Type == BroadcastLineType.ShowClosing)
            {
                // Load Vern broadcast audio: res://assets/audio/voice/Vern/Broadcast/{mood}/vern_{line_type}_{number}_{mood}.mp3
                string mood = GetVernMood();
                string lineType = GetLineTypeForVernAudio(line.Type);

                // Files now have consistent mood suffixes and numbering
                if (line.Type == BroadcastLineType.ShowOpening)
                {
                    // Opening files are numbered 01, 02, etc. per mood
                    for (int num = 1; num <= 5; num++) // Most moods have 1-2 files, neutral has more
                    {
                        audioPath = $"res://assets/audio/voice/Vern/Broadcast/{mood}/vern_opening_{num:D2}_{mood}.mp3";
                        var testStream = GD.Load<AudioStream>(audioPath);
                        if (testStream != null)
                        {
                            return testStream;
                        }
                    }
                }
                else
                {
                    // Special handling for dead air filler (only in neutral, no mood suffix)
                    if (line.Type == BroadcastLineType.DeadAirFiller)
                    {
                        // Dead air fillers are only in neutral and don't have mood suffixes
                        for (int num = 1; num <= 10; num++) // Up to 10 dead air fillers
                        {
                            audioPath = $"res://assets/audio/voice/Vern/Broadcast/neutral/vern_{lineType}_{num:D3}.mp3";
                            var testStream = GD.Load<AudioStream>(audioPath);
                            if (testStream != null)
                            {
                                return testStream;
                            }
                        }
                    }
                    else
                    {
                        // Other types: try numbered versions first (001-005), then non-numbered fallback
                        for (int num = 1; num <= 5; num++)
                        {
                            audioPath = $"res://assets/audio/voice/Vern/Broadcast/{mood}/vern_{lineType}_{num:D3}_{mood}.mp3";
                            var testStream = GD.Load<AudioStream>(audioPath);
                            if (testStream != null)
                            {
                                return testStream;
                            }
                        }
                        // Fallback to non-numbered version
                        audioPath = $"res://assets/audio/voice/Vern/Broadcast/{mood}/vern_{lineType}_{mood}.mp3";
                    }
                }
            }

            if (!string.IsNullOrEmpty(audioPath))
            {
                var audioStream = GD.Load<AudioStream>(audioPath);
                if (audioStream != null)
                {
                    return audioStream;
                }
                else
                {
                    GD.Print($"AudioDialoguePlayer.LoadVoiceAudioForLine: Failed to load {audioPath}");
                }
            }

            return null;
        }

        private string GetLineTypeForVernAudio(BroadcastLineType lineType)
        {
            return lineType switch
            {
                BroadcastLineType.ShowOpening => "opening",
                BroadcastLineType.BetweenCallers => "betweencallers",
                BroadcastLineType.DeadAirFiller => "deadairfiller", // No underscores, only in neutral
                BroadcastLineType.ShowClosing => "closing",
                _ => "unknown"
            };
        }

        private string GetVernMood()
        {
            // Get Vern's current mood from the game state
            var vernStats = ServiceRegistry.Instance?.GameStateManager?.VernStats;
            if (vernStats != null)
            {
                return vernStats.CurrentMoodType.ToString().ToLower();
            }
            return "neutral"; // Default fallback
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
