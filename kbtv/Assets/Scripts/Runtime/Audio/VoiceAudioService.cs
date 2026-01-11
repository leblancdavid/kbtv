using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using KBTV.Core;
using KBTV.Dialogue;

namespace KBTV.Audio
{
    /// <summary>
    /// Service for loading and caching voice audio clips via Addressables.
    /// Handles preloading conversation clips and on-demand broadcast clip loading.
    /// </summary>
    public class VoiceAudioService : SingletonMonoBehaviour<VoiceAudioService>
    {
        // Cache for current conversation clips
        private Dictionary<string, AudioClip> _conversationClipCache = new Dictionary<string, AudioClip>();
        
        // Cache for broadcast clips (persist for session)
        private Dictionary<string, AudioClip> _broadcastClipCache = new Dictionary<string, AudioClip>();
        
        // Track async handles for cleanup
        private List<AsyncOperationHandle<AudioClip>> _activeHandles = new List<AsyncOperationHandle<AudioClip>>();
        
        // Current conversation info for address building
        private string _currentArcId;
        private string _currentTopic;
        private VernMood _currentMood;
        
        /// <summary>
        /// Whether conversation clips are currently loaded.
        /// </summary>
        public bool HasConversationLoaded => _conversationClipCache.Count > 0;

        protected override void OnSingletonAwake()
        {
            // Initialize caches
            _conversationClipCache = new Dictionary<string, AudioClip>();
            _broadcastClipCache = new Dictionary<string, AudioClip>();
            _activeHandles = new List<AsyncOperationHandle<AudioClip>>();
        }

        protected override void OnDestroy()
        {
            UnloadAll();
            base.OnDestroy();
        }

        #region Conversation Clips

        /// <summary>
        /// Preload all clips for a conversation arc.
        /// Call this when a conversation starts.
        /// </summary>
        /// <param name="arcId">The arc identifier (e.g., "compelling_whistleblower")</param>
        /// <param name="topic">The topic (e.g., "Conspiracies")</param>
        /// <param name="mood">Vern's current mood</param>
        /// <param name="lineCount">Number of lines in the conversation</param>
        public async Task PreloadConversationAsync(string arcId, string topic, VernMood mood, int lineCount)
        {
            // Unload previous conversation if any
            UnloadCurrentConversation();
            
            _currentArcId = arcId;
            _currentTopic = topic;
            _currentMood = mood;
            
            var tasks = new List<Task>();
            
            for (int i = 0; i < lineCount; i++)
            {
                // Load both Vern and Caller clips for each line index
                tasks.Add(LoadConversationClipAsync(arcId, topic, mood, i, Speaker.Vern));
                tasks.Add(LoadConversationClipAsync(arcId, topic, mood, i, Speaker.Caller));
            }
            
            try
            {
                await Task.WhenAll(tasks);
                Debug.Log($"VoiceAudioService: Preloaded {_conversationClipCache.Count} clips for {topic}/{arcId} ({mood})");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"VoiceAudioService: Some clips failed to load: {e.Message}");
            }
        }

        /// <summary>
        /// Load a single conversation clip and cache it.
        /// </summary>
        private async Task LoadConversationClipAsync(string arcId, string topic, VernMood mood, int lineIndex, Speaker speaker)
        {
            string address = BuildConversationAddress(arcId, topic, mood, lineIndex, speaker);
            
            try
            {
                Debug.Log($"VoiceAudioService: Loading clip at address '{address}'");
                var handle = Addressables.LoadAssetAsync<AudioClip>(address);
                _activeHandles.Add(handle);
                
                var clip = await handle.Task;
                
                if (clip != null)
                {
                    string cacheKey = BuildCacheKey(lineIndex, speaker);
                    _conversationClipCache[cacheKey] = clip;
                    Debug.Log($"VoiceAudioService: Loaded clip '{address}' successfully");
                }
                else
                {
                    Debug.LogWarning($"VoiceAudioService: Loaded null clip for address '{address}'");
                }
            }
            catch (Exception e)
            {
                // Log failed loads - helps diagnose Addressables issues
                Debug.LogWarning($"VoiceAudioService: Failed to load clip '{address}': {e.Message}");
            }
        }

        /// <summary>
        /// Get a cached conversation clip.
        /// Returns null if not found.
        /// </summary>
        public AudioClip GetConversationClip(int lineIndex, Speaker speaker)
        {
            string cacheKey = BuildCacheKey(lineIndex, speaker);
            
            if (_conversationClipCache.TryGetValue(cacheKey, out var clip))
            {
                Debug.Log($"VoiceAudioService: Found cached clip for key '{cacheKey}'");
                return clip;
            }
            
            // Log warning in development
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"VoiceAudioService: Clip not found in cache for key '{cacheKey}' (line {lineIndex}, speaker {speaker}). Cache has {_conversationClipCache.Count} entries.");
            #endif
            
            return null;
        }

        /// <summary>
        /// Unload all cached conversation clips.
        /// Call this when a conversation ends.
        /// </summary>
        public void UnloadCurrentConversation()
        {
            _conversationClipCache.Clear();
            
            // Release Addressable handles
            foreach (var handle in _activeHandles)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            _activeHandles.Clear();
            
            _currentArcId = null;
            _currentTopic = null;
        }

        #endregion

        #region Broadcast Clips

        /// <summary>
        /// Get a broadcast clip by ID (e.g., "vern_opening_001").
        /// Loads on-demand and caches for session.
        /// </summary>
        public async Task<AudioClip> GetBroadcastClipAsync(string clipId)
        {
            if (string.IsNullOrEmpty(clipId))
                return null;
            
            // Check cache first
            if (_broadcastClipCache.TryGetValue(clipId, out var cachedClip))
            {
                Debug.Log($"VoiceAudioService: Returning cached broadcast clip '{clipId}'");
                return cachedClip;
            }
            
            // Load from Addressables
            string address = BuildBroadcastAddress(clipId);
            
            try
            {
                Debug.Log($"VoiceAudioService: Loading broadcast clip at address '{address}'");
                var handle = Addressables.LoadAssetAsync<AudioClip>(address);
                var clip = await handle.Task;
                
                if (clip != null)
                {
                    _broadcastClipCache[clipId] = clip;
                    Debug.Log($"VoiceAudioService: Loaded broadcast clip '{address}' successfully");
                    return clip;
                }
                else
                {
                    Debug.LogWarning($"VoiceAudioService: Loaded null broadcast clip for address '{address}'");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"VoiceAudioService: Failed to load broadcast clip '{address}': {e.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// Synchronous version - returns cached clip or null.
        /// Use GetBroadcastClipAsync for loading.
        /// </summary>
        public AudioClip GetBroadcastClipCached(string clipId)
        {
            if (string.IsNullOrEmpty(clipId))
                return null;
                
            _broadcastClipCache.TryGetValue(clipId, out var clip);
            return clip;
        }

        #endregion

        #region Address Building

        /// <summary>
        /// Build Addressable address for a conversation clip.
        /// Format: {arcId}_{mood}_{lineIndex:D3}_{speaker}
        /// Example: ufo_credible_dashcam_neutral_001_vern
        /// </summary>
        private string BuildConversationAddress(string arcId, string topic, VernMood mood, int lineIndex, Speaker speaker)
        {
            string arcIdLower = arcId?.ToLowerInvariant() ?? "unknown";
            string moodLower = mood.ToString().ToLowerInvariant();
            string speakerLower = speaker == Speaker.Vern ? "vern" : "caller";
            int displayIndex = lineIndex + 1; // 1-based for file names
            
            // Note: topic parameter is unused - file names don't include topic prefix
            // Files are named: {arcId}_{mood}_{index}_{speaker}.ogg
            return $"{arcIdLower}_{moodLower}_{displayIndex:D3}_{speakerLower}";
        }

        /// <summary>
        /// Build Addressable address for a broadcast clip.
        /// The clipId is already the address (e.g., "vern_opening_001").
        /// </summary>
        private string BuildBroadcastAddress(string clipId)
        {
            // clipId is already formatted correctly
            return clipId;
        }

        /// <summary>
        /// Build cache key for conversation clips.
        /// </summary>
        private string BuildCacheKey(int lineIndex, Speaker speaker)
        {
            return $"{lineIndex}_{speaker}";
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Unload all cached clips (conversation and broadcast).
        /// </summary>
        public void UnloadAll()
        {
            UnloadCurrentConversation();
            _broadcastClipCache.Clear();
        }

        #endregion
    }
}
