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
        private readonly GameStateManager _gameStateManager;

        public event System.Action<AudioCompletedEvent>? LineCompleted;

        public bool IsPlaying => _audioPlayer?.Playing ?? false;

        public AudioDialoguePlayer(GameStateManager gameStateManager)
        {
            _gameStateManager = gameStateManager;
        }

        public override void _Ready()
        {
            _audioPlayer = new AudioStreamPlayer();
            AddChild(_audioPlayer);
            _audioPlayer.Finished += OnAudioFinished;
        }

public async void PlayBroadcastItemAsync(BroadcastItem item)
        {
            GD.Print($"AudioDialoguePlayer.PlayBroadcastItemAsync: Starting - Id={item.Id}, Type={item.Type}");

            if (_audioPlayer == null)
            {
                GD.PrintErr("AudioDialoguePlayer.PlayBroadcastItemAsync: AudioStreamPlayer not initialized");
                return;
            }

            Stop();
            _currentLineId = item.Id;

            var audioStream = LoadAudioForBroadcastItem(item);
            if (audioStream != null)
            {
                // Play actual loaded audio at natural speed and duration
                GD.Print($"AudioDialoguePlayer: Playing loaded audio for {item.Id}");
                _audioPlayer.Stream = audioStream;
                _audioPlayer.Play();
                // Audio will naturally trigger OnAudioFinished when it completes
            }
            else
            {
                // No audio file found - use timer fallback with warning
                GD.Print($"AudioDialoguePlayer: WARNING - No audio file found for {item.Id}, using timer fallback");
                StartTimerFallback(item.Duration > 0 ? item.Duration : 4.0f);
            }
        }

// Legacy method for backward compatibility - marked as obsolete
        [System.Obsolete("Use PlayBroadcastItemAsync(BroadcastItem) instead")]
        public async void PlayLineAsync(BroadcastLine line)
        {
            GD.Print($"AudioDialoguePlayer.PlayLineAsync: Legacy method called - converting BroadcastLine to BroadcastItem");
            
            // Convert legacy BroadcastLine to BroadcastItem
            var item = ConvertBroadcastLineToItem(line);
            PlayBroadcastItemAsync(item);
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

        private AudioStream? LoadAudioForBroadcastItem(BroadcastItem item)
        {
            // If BroadcastItem has a specific audio path, use it
            if (!string.IsNullOrEmpty(item.AudioPath))
            {
                var audioStream = GD.Load<AudioStream>(item.AudioPath);
                if (audioStream != null)
                {
                    GD.Print($"AudioDialoguePlayer.LoadAudioForBroadcastItem: Loaded specified audio from {item.AudioPath}");
                    return audioStream;
                }
                else
                {
                    GD.Print($"AudioDialoguePlayer.LoadAudioForBroadcastItem: Failed to load specified audio {item.AudioPath}");
                }
            }

            // Load based on BroadcastItemType
            switch (item.Type)
            {
                case BroadcastItemType.Ad:
                    // Try to load ad audio, fallback to timer if not found
                    var adAudio = LoadAdAudio(item);
                    if (adAudio != null)
                    {
                        GD.Print($"AudioDialoguePlayer.LoadAudioForBroadcastItem: Loaded ad audio");
                        return adAudio;
                    }
                    else
                    {
                        GD.Print($"AudioDialoguePlayer.LoadAudioForBroadcastItem: No ad audio found, using timer fallback");
                        return null;
                    }

                case BroadcastItemType.Music:
                    // Handle special music cases
                    if (item.Id == "RETURN_MUSIC" || item.Id == "OUTRO_MUSIC")
                    {
                        return LoadRandomReturnBumper();
                    }
                    // Fall through to general audio loading
                    break;
            }

            // Try to load voice audio files
            var voiceAudio = LoadVoiceAudioForItem(item);
            if (voiceAudio != null)
            {
                GD.Print($"AudioDialoguePlayer.LoadAudioForBroadcastItem: Loaded voice audio for {item.Id}");
                return voiceAudio;
            }

            // Fallback to silent audio
            GD.Print($"AudioDialoguePlayer.LoadAudioForBroadcastItem: No voice audio found, using 4-second silent audio for {item.Id}");
            return GetSilentAudioFile();
        }

        private AudioStream? LoadAudioForLine(BroadcastLine line)
        {
            GD.Print($"AudioDialoguePlayer.LoadAudioForLine: Legacy method called - converting BroadcastLine to BroadcastItem");
            var item = ConvertBroadcastLineToItem(line);
            return LoadAudioForBroadcastItem(item);
        }

        private AudioStream? LoadVoiceAudioForItem(BroadcastItem item)
        {
            string audioPath = "";

            switch (item.Type)
            {
                case BroadcastItemType.CallerLine:
                    // Load caller audio: res://assets/audio/voice/Callers/{topic}/{arcId}/{item.Id}.mp3
                    var arcId = GetArcIdFromMetadata(item.Metadata);
                    if (!string.IsNullOrEmpty(arcId))
                    {
                        string topic = GetTopicFromArcId(arcId);
                        audioPath = $"res://assets/audio/voice/Callers/{topic}/{arcId}/{item.Id}.mp3";
                    }
                    break;

                case BroadcastItemType.VernLine:
                    // Load Vern conversation audio: res://assets/audio/voice/Vern/ConversationArcs/{topic}/{arcId}/{item.Id}.mp3
                    arcId = GetArcIdFromMetadata(item.Metadata);
                    if (!string.IsNullOrEmpty(arcId))
                    {
                        string topic = GetTopicFromArcId(arcId);
                        audioPath = $"res://assets/audio/voice/Vern/ConversationArcs/{topic}/{arcId}/{item.Id}.mp3";
                    }
                    break;

                case BroadcastItemType.Music:
                case BroadcastItemType.DeadAir:
                case BroadcastItemType.Transition:
                    // Load Vern broadcast audio: res://assets/audio/voice/Vern/Broadcast/{id}.mp3
                    // Note: Broadcast files already have mood encoded in filename (e.g., opening_irritated_3.mp3)
                    if (!string.IsNullOrEmpty(item.Id))
                    {
                        audioPath = $"res://assets/audio/voice/Vern/Broadcast/{item.Id}.mp3";
                    }
                    break;
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
                    GD.Print($"AudioDialoguePlayer.LoadVoiceAudioForItem: Failed to load {audioPath}");
                }
            }

            return null;
        }

        private AudioStream? LoadVoiceAudioForLine(BroadcastLine line)
        {
            GD.Print($"AudioDialoguePlayer.LoadVoiceAudioForLine: Legacy method called - converting BroadcastLine to BroadcastItem");
            var item = ConvertBroadcastLineToItem(line);
            return LoadVoiceAudioForItem(item);
        }

        private string GetVernMood()
        {
            // Get Vern's current mood from the game state
            var vernStats = _gameStateManager?.VernStats;
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

        private BroadcastItem ConvertBroadcastLineToItem(BroadcastLine line)
        {
            // Map BroadcastLineType to BroadcastItemType
            var itemType = line.Type switch
            {
                BroadcastLineType.VernDialogue => BroadcastItemType.VernLine,
                BroadcastLineType.CallerDialogue => BroadcastItemType.CallerLine,
                BroadcastLineType.AdBreak or BroadcastLineType.Ad => BroadcastItemType.Ad,
                BroadcastLineType.Music or BroadcastLineType.ReturnMusic => BroadcastItemType.Music,
                BroadcastLineType.BetweenCallers or BroadcastLineType.DeadAirFiller or BroadcastLineType.ShowOpening 
                    or BroadcastLineType.ShowClosing or BroadcastLineType.OffTopicRemark => BroadcastItemType.Transition,
                _ => BroadcastItemType.VernLine // Default fallback
            };

            // Create metadata object with arc info
            var metadata = new
            {
                ArcId = line.ArcId,
                SpeakerId = line.SpeakerId,
                Phase = line.Phase,
                LineIndex = line.LineIndex,
                CallerGender = line.CallerGender,
                Speaker = line.Speaker
            };

            return new BroadcastItem(
                id: line.SpeakerId ?? line.Id,
                type: itemType,
                text: line.Text,
                audioPath: null, // Will be determined by loading logic
                duration: 4.0f, // Default duration for legacy lines
                metadata: metadata
            );
        }

        private string? GetArcIdFromMetadata(object? metadata)
        {
            if (metadata == null) return null;
            
            // Try to extract ArcId from metadata object
            var metadataType = metadata.GetType();
            var arcIdProperty = metadataType.GetProperty("ArcId");
            return arcIdProperty?.GetValue(metadata)?.ToString();
        }

        private AudioStream? LoadAdAudio(BroadcastItem item)
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
