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
                // Play the actual loaded audio at natural speed and duration
                GD.Print($"AudioDialoguePlayer: Playing loaded audio for {line.SpeakerId}");
                _audioPlayer.Stream = audioStream;
                _audioPlayer.Play();
                // Audio will naturally trigger OnAudioFinished when it completes
            }
            else
            {
                // No audio file found - use timer fallback with warning
                GD.Print($"AudioDialoguePlayer: WARNING - No audio file found for {line.SpeakerId}, using timer fallback");
                StartTimerFallback(4.0f);
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
                // Try to load ad audio, fallback to timer if not found
                var adAudio = LoadAdAudio(line);
                if (adAudio != null)
                {
                    GD.Print($"AudioDialoguePlayer.LoadAudioForLine: Loaded ad audio");
                    return adAudio;
                }
                else
                {
                    GD.Print($"AudioDialoguePlayer.LoadAudioForLine: No ad audio found, using timer fallback");
                    StartTimerFallback(4.0f);
                    return null;
                }
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
                // Load Vern conversation audio: res://assets/audio/voice/Vern/ConversationArcs/{topic}/{arc_folder}/{id}.mp3
                if (!string.IsNullOrEmpty(line.ArcId) && !string.IsNullOrEmpty(line.Id))
                {
                    string arcTopic = GetTopicFromArcId(line.ArcId);
                    string arcFolder = GetArcFolderFromArcId(line.ArcId);
                    audioPath = $"res://assets/audio/voice/Vern/ConversationArcs/{arcTopic}/{arcFolder}/{line.Id}.mp3";
                }
            }
            else if (line.Type == BroadcastLineType.ShowOpening || line.Type == BroadcastLineType.BetweenCallers ||
                     line.Type == BroadcastLineType.DeadAirFiller || line.Type == BroadcastLineType.ShowClosing ||
                     line.Type == BroadcastLineType.OffTopicRemark)
            {
                // Load Vern broadcast audio: res://assets/audio/voice/Vern/Broadcast/{id}.mp3
                // Note: Broadcast files already have mood encoded in filename (e.g., opening_irritated_3.mp3)
                if (!string.IsNullOrEmpty(line.Id))
                {
                    audioPath = $"res://assets/audio/voice/Vern/Broadcast/{line.Id}.mp3";
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

        private string GetTopicFromArcId(string arcId)
        {
            // Extract topic from arc ID (e.g., "conspiracies_credible_govt_contractor" -> "Conspiracies")
            if (arcId.StartsWith("ufos") || arcId.Contains("ufos_"))
                return "UFOs";
            if (arcId.StartsWith("ghosts") || arcId.Contains("ghosts_"))
                return "Ghosts";
            if (arcId.StartsWith("cryptids") || arcId.Contains("cryptids_") || arcId.Contains("cryptid_"))
                return "Cryptids";
            if (arcId.StartsWith("conspiracies") || arcId.Contains("conspiracies_"))
                return "Conspiracies";

            // Fallback: first part
            var parts = arcId.Split('_');
            if (parts.Length >= 1)
            {
                var topicPart = parts[0];
                return topicPart switch
                {
                    "ufos" => "UFOs",
                    "ghosts" => "Ghosts",
                    "cryptids" => "Cryptids",
                    "cryptid" => "Cryptids",
                    "conspiracies" => "Conspiracies",
                    _ => "UFOs"
                };
            }
            return "UFOs"; // Default
        }

        private string GetArcFolderFromArcId(string arcId)
        {
            // Extract folder name from arc ID
            // Format: {topic}_{legitimacy}_{name} -> {name}
            // e.g., "ufos_compelling_pilot" -> "pilot"
            // e.g., "ufos_credible_dashcam" -> "dashcam_trucker"
            var parts = arcId.Split('_');
            if (parts.Length >= 3)
            {
                // Join all parts after the first two (topic + legitimacy)
                var folderParts = new System.Collections.Generic.List<string>();
                for (int i = 2; i < parts.Length; i++)
                {
                    folderParts.Add(parts[i]);
                }
                return string.Join("_", folderParts);
            }

            // Fallback: use the whole arcId as folder (shouldn't happen with proper data)
            GD.PrintErr($"AudioDialoguePlayer.GetArcFolderFromArcId: Unexpected arcId format: {arcId}");
            return arcId;
        }

        private AudioStream? LoadAdAudio(BroadcastLine line)
        {
            // Try to load ad audio files
            // Ads are stored in assets/audio/ads/ with various sponsor folders
            string[] possibleAdPaths = {
                "res://assets/audio/ads/area_51_tours_v1.mp3",
                "res://assets/audio/ads/big_earls_auto_v1.mp3",
                "res://assets/audio/ads/cryptid_hunters_v1.mp3",
                "res://assets/audio/ads/ghost_busters_v1.mp3",
                "res://assets/audio/ads/ufology_today_v1.mp3"
            };

            foreach (var path in possibleAdPaths)
            {
                var testStream = GD.Load<AudioStream>(path);
                if (testStream != null)
                {
                    return testStream;
                }
            }

            return null;
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
