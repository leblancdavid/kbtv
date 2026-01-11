using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
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
        /// <param name="lines">The dialogue lines to preload (with section info for correct audio paths)</param>
        public async Task PreloadConversationAsync(string arcId, string topic, VernMood mood, IEnumerable<DialogueLine> lines)
        {
            // Unload previous conversation if any
            UnloadCurrentConversation();
            
            _currentArcId = arcId;
            _currentTopic = topic;
            _currentMood = mood;
            
            var tasks = new List<Task>();
            
            foreach (var line in lines)
            {
                // Skip lines without arc line index (e.g., dynamically generated lines)
                if (line.ArcLineIndex < 0) continue;
                
                // Load clip for this specific line with its section
                tasks.Add(LoadConversationClipAsync(arcId, topic, mood, line.ArcLineIndex, line.Speaker, line.Section));
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
        /// Preload all clips for a conversation arc using arc lines.
        /// Call this when a conversation starts.
        /// </summary>
        /// <param name="arcId">The arc identifier (e.g., "compelling_whistleblower")</param>
        /// <param name="topic">The topic (e.g., "Conspiracies")</param>
        /// <param name="mood">Vern's current mood</param>
        /// <param name="moodVariant">The mood variant containing all lines with section info</param>
        public async Task PreloadConversationAsync(string arcId, string topic, VernMood mood, ArcMoodVariant moodVariant)
        {
            if (moodVariant == null) return;
            
            // Unload previous conversation if any
            UnloadCurrentConversation();
            
            _currentArcId = arcId;
            _currentTopic = topic;
            _currentMood = mood;
            
            // TEMPORARY DIAGNOSTIC - Remove after debugging audio preload issue
            Debug.Log($"VoiceAudioService: Preload sections for {arcId} ({mood}) - " +
                $"Intro: {moodVariant.Intro?.Count ?? 0}, " +
                $"Dev: {moodVariant.Development?.Count ?? 0}, " +
                $"Skep: {moodVariant.BeliefBranch?.Skeptical?.Count ?? 0}, " +
                $"Beli: {moodVariant.BeliefBranch?.Believing?.Count ?? 0}, " +
                $"Concl: {moodVariant.Conclusion?.Count ?? 0}");
            
            var tasks = new List<Task>();
            
            // Helper to add preload tasks for a section
            void AddSectionTasks(IEnumerable<ArcDialogueLine> sectionLines)
            {
                if (sectionLines == null) return;
                foreach (var line in sectionLines)
                {
                    tasks.Add(LoadConversationClipAsync(arcId, topic, mood, line.ArcLineIndex, line.Speaker, line.Section));
                }
            }
            
            // Preload all sections (including BOTH belief paths)
            AddSectionTasks(moodVariant.Intro);
            AddSectionTasks(moodVariant.Development);
            AddSectionTasks(moodVariant.BeliefBranch?.Skeptical);
            AddSectionTasks(moodVariant.BeliefBranch?.Believing);
            AddSectionTasks(moodVariant.Conclusion);
            
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
        private async Task LoadConversationClipAsync(string arcId, string topic, VernMood mood, int lineIndex, Speaker speaker, ArcSection section)
        {
            string address = BuildConversationAddress(arcId, topic, mood, lineIndex, speaker, section);
            
            try
            {
                // Check if the address exists in Addressables before trying to load
                var locationsHandle = Addressables.LoadResourceLocationsAsync(address, typeof(AudioClip));
                var locations = await locationsHandle.Task;
                
                if (locations == null || locations.Count == 0)
                {
                    // TEMPORARY DIAGNOSTIC - Remove after debugging audio preload issue
                    Debug.LogWarning($"VoiceAudioService: Address not found: '{address}'");
                    Addressables.Release(locationsHandle);
                    return;
                }
                Addressables.Release(locationsHandle);
                
                var handle = Addressables.LoadAssetAsync<AudioClip>(address);
                _activeHandles.Add(handle);
                
                var clip = await handle.Task;
                
                if (clip != null)
                {
                    string cacheKey = BuildCacheKey(lineIndex, speaker, section);
                    _conversationClipCache[cacheKey] = clip;
                }
            }
            catch (Exception e)
            {
                // Log failed loads only if unexpected (not InvalidKeyException for missing clips)
                if (!(e is InvalidKeyException))
                {
                    Debug.LogWarning($"VoiceAudioService: Failed to load clip '{address}': {e.Message}");
                }
            }
        }

        /// <summary>
        /// Get a cached conversation clip.
        /// Returns null if not found.
        /// </summary>
        public AudioClip GetConversationClip(int lineIndex, Speaker speaker, ArcSection section)
        {
            string cacheKey = BuildCacheKey(lineIndex, speaker, section);
            
            if (_conversationClipCache.TryGetValue(cacheKey, out var clip))
            {
                return clip;
            }
            
            // Not finding a clip is normal - not all lines have audio
            return null;
        }

        /// <summary>
        /// Get a conversation clip, loading on-demand if not cached.
        /// Use this when preload hasn't completed yet.
        /// </summary>
        public async Task<AudioClip> GetConversationClipAsync(int lineIndex, Speaker speaker, ArcSection section)
        {
            // Check cache first
            string cacheKey = BuildCacheKey(lineIndex, speaker, section);
            if (_conversationClipCache.TryGetValue(cacheKey, out var cachedClip))
            {
                return cachedClip;
            }
            
            // Not in cache - try to load on demand if we have arc info
            if (string.IsNullOrEmpty(_currentArcId))
            {
                return null;
            }
            
            await LoadConversationClipAsync(_currentArcId, _currentTopic, _currentMood, lineIndex, speaker, section);
            
            // Check cache again after load
            if (_conversationClipCache.TryGetValue(cacheKey, out var loadedClip))
            {
                return loadedClip;
            }
            
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
                return cachedClip;
            }
            
            // Load from Addressables
            string address = BuildBroadcastAddress(clipId);
            
            try
            {
                // Check if the address exists before loading
                var locationsHandle = Addressables.LoadResourceLocationsAsync(address, typeof(AudioClip));
                var locations = await locationsHandle.Task;
                
                if (locations == null || locations.Count == 0)
                {
                    Addressables.Release(locationsHandle);
                    Debug.LogWarning($"VoiceAudioService: Broadcast clip not found: {clipId}");
                    return null;
                }
                Addressables.Release(locationsHandle);
                
                var handle = Addressables.LoadAssetAsync<AudioClip>(address);
                var clip = await handle.Task;
                
                if (clip != null)
                {
                    _broadcastClipCache[clipId] = clip;
                    return clip;
                }
            }
            catch (Exception e)
            {
                if (!(e is InvalidKeyException))
                {
                    Debug.LogWarning($"VoiceAudioService: Failed to load broadcast clip '{address}': {e.Message}");
                }
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
        /// Format for normal sections: {arcId}_{mood}_{lineIndex:D3}_{speaker}
        /// Format for belief branches: {arcId}_{mood}_{beliefTag}_{lineIndex:D3}_{speaker}
        /// Example: ufo_credible_dashcam_neutral_001_vern (normal)
        /// Example: ufo_credible_dashcam_neutral_skep_009_vern (skeptical branch)
        /// </summary>
        private string BuildConversationAddress(string arcId, string topic, VernMood mood, int lineIndex, Speaker speaker, ArcSection section)
        {
            string arcIdLower = arcId?.ToLowerInvariant() ?? "unknown";
            string moodLower = mood.ToString().ToLowerInvariant();
            string speakerLower = speaker == Speaker.Vern ? "vern" : "caller";
            int displayIndex = lineIndex + 1; // 1-based for file names
            
            // Add belief tag for belief branch sections (matches generate_audio.py naming)
            string beliefTag = section switch
            {
                ArcSection.Skeptical => "_skep",
                ArcSection.Believing => "_beli",
                _ => ""
            };
            
            // Note: topic parameter is unused - file names don't include topic prefix
            // Files are named: {arcId}_{mood}[_{beliefTag}]_{index}_{speaker}.ogg
            return $"{arcIdLower}_{moodLower}{beliefTag}_{displayIndex:D3}_{speakerLower}";
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
        /// Includes section to differentiate belief branch clips with same line index.
        /// </summary>
        private string BuildCacheKey(int lineIndex, Speaker speaker, ArcSection section)
        {
            return $"{lineIndex}_{speaker}_{section}";
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
