#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using KBTV.Core;
using KBTV.Managers;
using KBTV.Dialogue;

namespace KBTV.Audio
{
    /// <summary>
    /// Service for managing broadcast audio playback.
    /// Uses a pool of AudioStreamPlayer instances for concurrent playback.
    /// </summary>
    public partial class BroadcastAudioService : Node, IBroadcastAudioService
    {
        private const int PLAYER_POOL_SIZE = 5;
        
        private readonly List<AudioStreamPlayer> _availablePlayers = new();
        private readonly List<AudioStreamPlayer> _activePlayers = new();
        private readonly Dictionary<AudioStreamPlayer, TaskCompletionSource> _completionSources = new();

        private BroadcastItem? _currentBroadcastItem;
        private Speaker _currentSpeaker = Speaker.Vern;

        // Audio routing for effects
        private AudioMixerManager? _audioMixer;

        // Track when static started for logging
        private double _staticStartTime = 0.0;

        // Dependency injection
        private GameStateManager GameStateManager => DependencyInjection.Get<GameStateManager>(this);
        private EventBus EventBus => DependencyInjection.Get<EventBus>(this);

        /// <summary>
        /// Check if broadcast audio is disabled (uses 4-second timeouts).
        /// </summary>
        public bool IsAudioDisabled => GameStateManager?.DisableBroadcastAudio ?? false;

        /// <summary>
        /// Event fired when a broadcast item audio completes playback.
        /// Subscribers should advance the conversation or broadcast flow.
        /// </summary>
        public event System.Action<AudioCompletedEvent>? LineCompleted;

    /// <summary>
    /// Called when node enters the scene tree and is ready.
    /// Makes services available to descendants.
    /// </summary>
    public void OnReady() => this.Provide();

    /// <summary>
    /// Called when dependencies are resolved.
    /// </summary>
    public void OnResolved()
    {
        // No longer needed - using dependency injection instead of GetNode
    }

        /// <summary>
        /// Stop current playback if any.
        /// </summary>
        public void Stop()
        {
            StopAllPlayback();
        }

        /// <summary>
        /// Stops all currently playing audio players immediately.
        /// Used to prevent audio overlap during transitions.
        /// </summary>
        public void StopAllPlayback()
        {
            foreach (var player in _activePlayers.ToList())
            {
                player.Stop();
                OnPlayerFinished(player); // Clean up and return to pool
            }
        }

        /// <summary>
        /// Check if player is currently playing audio.
        /// </summary>
        public bool IsPlaying => _activePlayers.Count > 0;

        public override void _Ready()
        {
            // Initialize player pool
            for (int i = 0; i < PLAYER_POOL_SIZE; i++)
            {
                var player = new AudioStreamPlayer();
                AddChild(player);
                _availablePlayers.Add(player);
                player.Finished += () => OnPlayerFinished(player);
            }

            // Initialize audio mixer for effects
            InitializeAudioMixer();
            
            // Subscribe to interruption events for static fade-out
            EventBus.Subscribe<BroadcastInterruptionEvent>(OnBroadcastInterruption);
        }
        
        private void OnBroadcastInterruption(BroadcastInterruptionEvent evt)
        {
            if (evt.Reason == BroadcastInterruptionReason.CallerCursed || 
                evt.Reason == BroadcastInterruptionReason.CallerDropped)
            {
                GD.Print($"BroadcastAudioService: Received interruption {evt.Reason}, fading out static");
                _audioMixer?.GetStaticController()?.StopStaticWithFade(0.15f);
            }
        }

        private void InitializeAudioMixer()
        {
            // Try to get AudioMixerManager from the scene tree
            _audioMixer = GetNode<AudioMixerManager>("/root/AudioMixerManager");
            if (_audioMixer == null)
            {
                GD.PrintErr("BroadcastAudioService: AudioMixerManager not found - audio effects disabled");
            }
            else
            {
                GD.Print("BroadcastAudioService: Found AudioMixerManager");
            }
        }

        /// <summary>
        /// Plays audio from the specified path asynchronously.
        /// Returns a task that completes when playback finishes.
        /// </summary>
        public async Task PlayAudioAsync(string audioPath, CancellationToken cancellationToken = default)
        {
            // Determine speaker based on audio path
            var detectedSpeaker = DetermineSpeakerFromPath(audioPath);
            _currentSpeaker = detectedSpeaker;
            GD.Print($"PlayAudioAsync: Set _currentSpeaker to {detectedSpeaker} from path {audioPath}");
            
            if (IsAudioDisabled)
            {
                await Task.Delay(4000, CancellationToken.None);
                return;
            }
            // Special corruption check for the problematic file - skip loading if audio is disabled to avoid unnecessary loading
            if (!IsAudioDisabled && audioPath == "res://assets/audio/voice/Callers/UFOs/lights/ufos_questionable_lights_caller_2.mp3")
            {
                var testStream = GD.Load<AudioStream>(audioPath);
                if (testStream == null)
                {
                    Log.Debug($"CORRUPTION_CHECK: Failed to load AudioStream for {audioPath}, using 4-second delay");
                    await Task.Delay(4000, cancellationToken);
                    return;
                }
                
                float testLength = 0f;
                if (testStream is AudioStreamMP3 mp3)
                {
                    testLength = (float)mp3.GetLength();
                }
                else if (testStream is Godot.AudioStreamWav wav)
                {
                    testLength = (float)wav.GetLength();
                }
                else if (testStream is AudioStreamOggVorbis ogg)
                {
                    testLength = (float)ogg.GetLength();
                }
                else
                {
                    Log.Debug($"CORRUPTION_CHECK: Unknown AudioStream type for {audioPath}, using 4-second delay");
                    await Task.Delay(4000, cancellationToken);
                    return;
                }
                
                if (testLength <= 0f)
                {
                    Log.Debug($"CORRUPTION_CHECK: Invalid length {testLength}, skipping playback with 4-second delay");
                    await Task.Delay(4000, cancellationToken);
                    return;
                }
            }

            if (!IsAudioStreamValid(audioPath))
            {
                await Task.Delay(4000, cancellationToken);
                return;
            }

            var player = GetAvailablePlayer();
            if (player == null)
            {
                Log.Error($"BroadcastAudioService: No available audio players for {audioPath}");
                return;
            }

            var audioStream = GD.Load<AudioStream>(audioPath);
            if (audioStream == null)
            {
                ReturnPlayer(player);
                await Task.Delay(4000, cancellationToken);
                return;
            }

            await PlayAudioStreamInternalAsync(player, audioStream, audioPath, cancellationToken);
        }

        /// <summary>
        /// Plays audio from the specified path for a maximum duration asynchronously.
        /// Completes when either the duration expires or playback finishes (whichever comes first).
        /// </summary>
        public async Task PlayAudioForDurationAsync(string audioPath, float maxDuration, CancellationToken cancellationToken = default)
        {
            await PlayAudioForDurationAsync(audioPath, maxDuration, false, cancellationToken);
        }

        /// <summary>
        /// Plays audio from the specified path for a maximum duration asynchronously.
        /// Completes when either the duration expires or playback finishes (whichever comes first).
        /// </summary>
        /// <param name="immediateStop">If true, stops playback immediately when duration expires. If false, uses deferred stop for thread safety.</param>
        public async Task PlayAudioForDurationAsync(string audioPath, float maxDuration, bool immediateStop, CancellationToken cancellationToken = default)
        {
            // Determine speaker based on audio path
            var detectedSpeaker = DetermineSpeakerFromPath(audioPath);
            _currentSpeaker = detectedSpeaker;
            GD.Print($"PlayAudioForDurationAsync: Set _currentSpeaker to {detectedSpeaker} from path {audioPath}");
            
            if (IsAudioDisabled)
            {
                await Task.Delay((int)(maxDuration * 1000), cancellationToken);
                return;
            }

            if (!IsAudioStreamValid(audioPath))
            {
                await Task.Delay((int)(maxDuration * 1000), cancellationToken);
                return;
            }

            var player = GetAvailablePlayer();
            if (player == null)
            {
                Log.Error($"BroadcastAudioService: No available audio players for {audioPath}");
                return;
            }

            var audioStream = GD.Load<AudioStream>(audioPath);
            if (audioStream == null)
            {
                ReturnPlayer(player);
                await Task.Delay((int)(maxDuration * 1000), cancellationToken);
                return;
            }

            await PlayAudioStreamForDurationAsync(player, audioStream, audioPath, maxDuration, immediateStop, cancellationToken);
        }

        /// <summary>
        /// Plays the specified audio stream asynchronously.
        /// Returns a task that completes when playback finishes.
        /// </summary>
        public async Task PlayAudioStreamAsync(AudioStream audioStream, CancellationToken cancellationToken = default)
        {
            var player = GetAvailablePlayer();
            if (player == null)
            {
                Log.Error($"BroadcastAudioService: No available audio players for AudioStream");
                return;
            }

            _activePlayers.Add(player);
            var tcs = new TaskCompletionSource();
            _completionSources[player] = tcs;

            player.Stream = audioStream;
            player.Play();

            // Register cancellation to cancel the TCS
            using var registration = cancellationToken.Register(() => 
            {
                tcs.TrySetCanceled(cancellationToken);
            });

            try
            {
                await tcs.Task;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Stop the player if still playing
                player.Stop();
                throw;
            }
        }

        /// <summary>
        /// Plays silent audio for the specified duration asynchronously.
        /// Used for timing-critical operations like break queuing and show ending.
        /// </summary>
        public async Task PlaySilentAudioAsync(float duration = 4.0f)
        {
            // For now, use the standard 4-second silent audio file
            // Future enhancement could create dynamic silent audio of any duration
            await PlayAudioAsync("res://assets/audio/silence_4sec.wav");
        }

        /// <summary>
        /// Plays audio for a broadcast item by loading it based on item type and metadata.
        /// </summary>
        public async Task PlayAudioForBroadcastItemAsync(BroadcastItem item)
        {
            _currentBroadcastItem = item;
            var detectedSpeaker = GetSpeakerFromBroadcastItemType(item.Type);
            _currentSpeaker = detectedSpeaker;
            GD.Print($"PlayAudioForBroadcastItemAsync: Set _currentSpeaker to {detectedSpeaker} from item.Type={item.Type}");
            
            if (IsAudioDisabled)
            {
                await Task.Delay(4000);
                // Publish AudioCompletedEvent if we have a current broadcast item
                if (_currentBroadcastItem != null)
                {
                    var speaker = GetSpeakerFromBroadcastItemType(_currentBroadcastItem.Type);
                    var completedEvent = new AudioCompletedEvent(_currentBroadcastItem.Id, speaker);
                    LineCompleted?.Invoke(completedEvent);
                }
                _currentBroadcastItem = null;
                return;
            }
            
            var audioStream = LoadAudioForBroadcastItem(item);

            // Validate loaded audio stream to prevent hangs on corrupted files
            if (audioStream != null)
            {
                // Create a temporary path for validation (using item.AudioPath if available, otherwise skip validation)
                string? validationPath = item.AudioPath;
                if (validationPath != null && !IsAudioStreamValid(validationPath))
                {
                    audioStream = null;
                }
            }

            if (audioStream != null)
            {
                await PlayAudioStreamAsync(audioStream, CancellationToken.None);
            }
            else
            {
                // No audio found or invalid, use silent audio
                await PlaySilentAudioAsync();
            }
            _currentBroadcastItem = null;
        }

        /// <summary>
        /// Internal method to play audio stream on a player.
        /// </summary>
        private async Task PlayAudioStreamInternalAsync(AudioStreamPlayer player, AudioStream audioStream, string debugName, CancellationToken cancellationToken)
        {
            _activePlayers.Add(player);
            var tcs = new TaskCompletionSource();
            _completionSources[player] = tcs;

            GD.Print($"PlayAudioStreamInternalAsync: _currentSpeaker={_currentSpeaker}, debugName={debugName}");
            
            // Start static for caller audio
            GD.Print($"About to call HandleStaticForSpeaker with _currentSpeaker={_currentSpeaker}");
            HandleStaticForSpeaker(_currentSpeaker, true);
            GD.Print($"After HandleStaticForSpeaker call");

            player.Stream = audioStream;
            player.Play();

            // Calculate duration-based timeout with 2-second buffer
            float audioDuration = GetAudioDuration(audioStream);
            var timeoutMs = (int)((audioDuration + 2.0f) * 1000);
            var timeoutTask = Task.Delay(timeoutMs);

            // Register cancellation to cancel the TCS
            using var registration = cancellationToken.Register(() => 
            {
                player.CallDeferred("Stop");
                tcs.TrySetCanceled(cancellationToken);
            });

            try
            {
                // Race between natural completion and timeout
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    player.Stop();
                    tcs.TrySetResult(); // Force completion to prevent hang
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Stop the player if still playing
                player.CallDeferred("Stop");
                throw;
            }
            
            // Normal completion - cleanup happens in OnPlayerFinished
        }

        /// <summary>
        /// Internal method to play audio stream on a player for a maximum duration.
        /// </summary>
        private async Task PlayAudioStreamForDurationAsync(AudioStreamPlayer player, AudioStream audioStream, string debugName, float maxDuration, bool immediateStop, CancellationToken cancellationToken)
        {
            _activePlayers.Add(player);
            var tcs = new TaskCompletionSource();
            _completionSources[player] = tcs;

            // Start static for caller audio
            HandleStaticForSpeaker(_currentSpeaker, true);

            player.Stream = audioStream;
            player.Play();

            // Create duration timeout task
            var durationMs = (int)(maxDuration * 1000);
            var durationTask = Task.Delay(durationMs);

            // Register cancellation to cancel the TCS
            using var registration = cancellationToken.Register(() => 
            {
                player.CallDeferred("Stop");
                tcs.TrySetCanceled(cancellationToken);
            });

            try
            {
                // Race between duration timeout and cancellation
                var completedTask = await Task.WhenAny(tcs.Task, durationTask);

                if (completedTask == durationTask)
                {
                    // Duration expired - stop playback
                    if (immediateStop)
                    {
                        player.Stop();
                    }
                    else
                    {
                        player.CallDeferred("Stop");
                    }
                    tcs.SetResult(); // Signal completion
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Stop the player if still playing
                player.CallDeferred("Stop");
                throw;
            }
            
            // Cleanup happens in OnPlayerFinished
        }

        private AudioStreamPlayer? GetAvailablePlayer()
        {
            if (_availablePlayers.Count > 0)
            {
                var player = _availablePlayers[0];
                _availablePlayers.RemoveAt(0);

                GD.Print($"GetAvailablePlayer: _currentSpeaker={_currentSpeaker}");

                // Route player to correct bus based on speaker type
                // This ensures audio goes through the appropriate effects chain
                if (_currentSpeaker == Speaker.Caller)
                {
                    player.Bus = "Caller";
                    GD.Print("BroadcastAudioService: Assigned Caller bus to player for speaker: " + _currentSpeaker);
                }
                else
                {
                    player.Bus = "Vern";
                    GD.Print("BroadcastAudioService: Assigned Vern bus to player for speaker: " + _currentSpeaker);
                }

                return player;
            }
            return null;
        }

        private void ReturnPlayer(AudioStreamPlayer player)
        {
            // Reset bus to Master when returning player to pool
            player.Bus = "Master";
            _activePlayers.Remove(player);
            _availablePlayers.Add(player);
        }

        private async void OnPlayerFinished(AudioStreamPlayer player)
        {
            GD.Print($"OnPlayerFinished: player finished, _currentSpeaker={_currentSpeaker}");
            
            if (_completionSources.TryGetValue(player, out var tcs))
            {
                tcs.SetResult();
                _completionSources.Remove(player);
            }
            
            // Stop static for caller audio using _currentSpeaker (natural duration)
            if (_currentSpeaker == Speaker.Caller)
            {
                double elapsed = (Time.GetTicksMsec() / 1000.0) - _staticStartTime;
                GD.Print($"OnPlayerFinished: Static played for {elapsed:F2} seconds, stopping now");
                HandleStaticForSpeaker(_currentSpeaker, false);
            }
            
            // Publish AudioCompletedEvent if we have a current broadcast item
            if (_currentBroadcastItem != null)
            {
                var speaker = GetSpeakerFromBroadcastItemType(_currentBroadcastItem.Type);
                GD.Print($"OnPlayerFinished: speaker from broadcast item = {speaker}");
                var completedEvent = new AudioCompletedEvent(_currentBroadcastItem.Id, speaker);
                LineCompleted?.Invoke(completedEvent);
            }
            
            // Reset speaker to Vern for next audio
            _currentSpeaker = Speaker.Vern;
            GD.Print("OnPlayerFinished: Reset _currentSpeaker to Vern");
            
            ReturnPlayer(player);
        }

        /// <summary>
        /// Handles starting/stopping static based on speaker type.
        /// </summary>
        private void HandleStaticForSpeaker(Speaker speaker, bool start)
        {
            GD.Print($"HandleStaticForSpeaker: speaker={speaker}, start={start}, _audioMixer={_audioMixer != null}");
            if (speaker == Speaker.Caller && _audioMixer != null)
            {
                var staticController = _audioMixer.GetStaticController();
                GD.Print($"HandleStaticForSpeaker: staticController={staticController != null}");
                if (staticController != null)
                {
                    if (start)
                    {
                        GD.Print("HandleStaticForSpeaker: Starting static");
                        staticController.StartStatic();
                        _staticStartTime = Time.GetTicksMsec() / 1000.0;
                        GD.Print($"HandleStaticForSpeaker: Recorded static start time: {_staticStartTime}");
                    }
                    else
                    {
                        GD.Print("HandleStaticForSpeaker: Stopping static");
                        staticController.StopStatic();
                    }
                }
            }
            else
            {
                GD.Print($"HandleStaticForSpeaker: Not handling - speaker={speaker} (expected Caller), _audioMixer={_audioMixer != null}");
            }
        }

        /// <summary>
        /// Gets the duration of an audio stream in seconds.
        /// </summary>
        public float GetAudioDuration(AudioStream audioStream)
        {
            if (audioStream is AudioStreamMP3 mp3Stream)
            {
                return (float)mp3Stream.GetLength();
            }
            else if (audioStream is Godot.AudioStreamWav wavStream)
            {
                return (float)wavStream.GetLength();
            }
            else if (audioStream is AudioStreamOggVorbis oggStream)
            {
                return (float)oggStream.GetLength();
            }
            else
            {
                return 0f;
            }
        }

        /// <summary>
        /// Gets the speaker type from broadcast item type.
        /// </summary>
        private Speaker GetSpeakerFromBroadcastItemType(BroadcastItemType type)
        {
            return type switch
            {
                BroadcastItemType.VernLine => Speaker.Vern,
                BroadcastItemType.CallerLine => Speaker.Caller,
                BroadcastItemType.Conversation => Speaker.Vern, // Default to Vern for conversation containers
                _ => Speaker.Vern // Default to Vern for other types
            };
        }

        /// <summary>
        /// Determines speaker based on audio file path.
        /// </summary>
        private Speaker DetermineSpeakerFromPath(string audioPath)
        {
            if (string.IsNullOrEmpty(audioPath))
                return Speaker.Vern;
            
            // Check path for caller vs vern audio
            if (audioPath.Contains("/Callers/") || audioPath.Contains("\\Callers\\"))
                return Speaker.Caller;
            
            return Speaker.Vern;
        }

        public override void _ExitTree()
        {
            // Stop all audio playback and unsubscribe from player events
            foreach (var player in _availablePlayers.Concat(_activePlayers).ToList())
            {
                player.Stop();
                player.Finished -= () => OnPlayerFinished(player);
            }
            _availablePlayers.Clear();
            _activePlayers.Clear();

            // Clean up completion sources
            foreach (var tcs in _completionSources.Values)
            {
                tcs.TrySetResult();
            }
            _completionSources.Clear();
            
            base._ExitTree();
        }

        /// <summary>
        /// Loads audio for a broadcast item based on its type and metadata.
        /// </summary>
        public AudioStream? LoadAudioForBroadcastItem(BroadcastItem item)
        {
            // Skip loading entirely when audio is disabled
            if (IsAudioDisabled)
            {
                return null;
            }

            // If BroadcastItem has a specific audio path, use it
            if (!string.IsNullOrEmpty(item.AudioPath))
            {
                var audioStream = GD.Load<AudioStream>(item.AudioPath);
                if (audioStream != null)
                {
                    return audioStream;
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
                        return adAudio;
                    }
                    return null;

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
                return voiceAudio;
            }

            // Fallback to silent audio
            return GetSilentAudioFile();
        }

        /// <summary>
        /// Loads voice audio for a broadcast item.
        /// </summary>
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
            }

            return null;
        }

        /// <summary>
        /// Extracts topic from arc ID.
        /// </summary>
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

        /// <summary>
        /// Extracts arc ID from metadata.
        /// </summary>
        private string? GetArcIdFromMetadata(object? metadata)
        {
            if (metadata == null) return null;
            
            // Try to extract ArcId from metadata object
            var metadataType = metadata.GetType();
            var arcIdProperty = metadataType.GetProperty("ArcId");
            return arcIdProperty?.GetValue(metadata)?.ToString();
        }

        /// <summary>
        /// Loads ad audio for a broadcast item.
        /// </summary>
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

        /// <summary>
        /// Loads a random return bumper audio file.
        /// </summary>
        private AudioStream? LoadRandomReturnBumper()
        {
            var returnBumperDir = DirAccess.Open("res://assets/audio/bumpers/Return");
            if (returnBumperDir == null)
            {
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
                return GetSilentAudioFile();
            }

            var random = new Random();
            var selectedFile = bumperFiles[random.Next(bumperFiles.Count)];
            var path = $"res://assets/audio/bumpers/Return/{selectedFile}";

            var audioStream = GD.Load<AudioStream>(path);
            if (audioStream == null)
            {
                return GetSilentAudioFile();
            }

            return audioStream;
        }

        /// <summary>
        /// Gets the silent audio file for fallbacks.
        /// </summary>
        private AudioStream? GetSilentAudioFile()
        {
            var audioStream = GD.Load<AudioStream>("res://assets/audio/silence_4sec.wav");
            if (audioStream == null)
            {
                return null;
            }
            return audioStream;
        }

        /// <summary>
        /// Validates if an audio stream is valid and not corrupted.
        /// Checks both load success and positive duration.
        /// </summary>
        public bool IsAudioStreamValid(string audioPath)
        {
            // Skip validation when audio is disabled
            if (IsAudioDisabled)
            {
                return false;
            }

            var audioStream = GD.Load<AudioStream>(audioPath);
            if (audioStream == null)
            {
                Log.Error($"CORRUPTION_CHECK: Failed to load AudioStream: {audioPath}");
                return false;
            }

            float duration = 0f;
            if (audioStream is AudioStreamMP3 mp3Stream)
            {
                duration = (float)mp3Stream.GetLength();
            }
            else if (audioStream is Godot.AudioStreamWav wavStream)
            {
                duration = (float)wavStream.GetLength();
            }
            else if (audioStream is AudioStreamOggVorbis oggStream)
            {
                duration = (float)oggStream.GetLength();
            }
            else
            {
                Log.Error($"CORRUPTION_CHECK: Unsupported AudioStream type: {audioStream.GetType()} for {audioPath}");
                return false;
            }

            if (duration <= 0f)
            {
                Log.Error($"CORRUPTION_CHECK: CORRUPTED FILE - Invalid duration {duration}s for {audioPath}");
                return false;
            }

            return true;
        }
    }
}