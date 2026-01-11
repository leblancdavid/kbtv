using System;
using System.Threading.Tasks;
using UnityEngine;
using KBTV.Dialogue;

namespace KBTV.Audio
{
    /// <summary>
    /// Helper for loading and playing voice audio clips with async guard patterns.
    /// Centralizes the common pattern of: load clip → check if cancelled → play → return duration.
    /// </summary>
    public static class VoicePlaybackHelper
    {
        /// <summary>
        /// Result of an async voice playback operation.
        /// </summary>
        public struct PlaybackResult
        {
            /// <summary>Duration of the audio clip, or 0 if no clip was loaded/played.</summary>
            public float AudioDuration;
            /// <summary>True if the operation was cancelled (state changed during async load).</summary>
            public bool WasCancelled;
        }

        /// <summary>
        /// Load and play a broadcast clip (opening, closing, filler, between callers, etc.).
        /// </summary>
        /// <param name="clipId">The clip ID to load (from DialogueTemplate.Id)</param>
        /// <param name="isCancelledCheck">Function that returns true if the operation should be cancelled</param>
        /// <returns>PlaybackResult with audio duration (0 if no clip) and cancellation status</returns>
        public static async Task<PlaybackResult> PlayBroadcastClipAsync(string clipId, Func<bool> isCancelledCheck)
        {
            var result = new PlaybackResult { AudioDuration = 0f, WasCancelled = false };

            if (VoiceAudioService.Instance == null || string.IsNullOrEmpty(clipId))
            {
                return result;
            }

            try
            {
                var clip = await VoiceAudioService.Instance.GetBroadcastClipAsync(clipId);

                // Guard: Check if state changed during async load
                if (isCancelledCheck())
                {
                    result.WasCancelled = true;
                    return result;
                }

                if (clip != null)
                {
                    AudioManager.Instance?.PlayVoiceClip(clip, Speaker.Vern);
                    result.AudioDuration = clip.length;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"VoicePlaybackHelper: Exception loading broadcast audio '{clipId}': {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Load and play a conversation clip for a specific line.
        /// </summary>
        /// <param name="arcLineIndex">The original arc line index (0-based, maps to audio file naming)</param>
        /// <param name="speaker">The speaker of the line</param>
        /// <param name="section">The arc section this line belongs to (affects audio file naming for belief branches)</param>
        /// <param name="isCancelledCheck">Function that returns true if the operation should be cancelled</param>
        /// <returns>PlaybackResult with audio duration (0 if no clip) and cancellation status</returns>
        public static async Task<PlaybackResult> PlayConversationClipAsync(int arcLineIndex, Speaker speaker, ArcSection section, Func<bool> isCancelledCheck)
        {
            var result = new PlaybackResult { AudioDuration = 0f, WasCancelled = false };

            if (VoiceAudioService.Instance == null)
            {
                return result;
            }

            // First try cached clip (from preload)
            var clip = VoiceAudioService.Instance.GetConversationClip(arcLineIndex, speaker, section);

            // If not cached, load on-demand (preload may not have completed)
            if (clip == null)
            {
                clip = await VoiceAudioService.Instance.GetConversationClipAsync(arcLineIndex, speaker, section);

                // Guard: Check if state changed during async load
                if (isCancelledCheck())
                {
                    result.WasCancelled = true;
                    return result;
                }
            }

            if (clip != null)
            {
                AudioManager.Instance?.PlayVoiceClip(clip, speaker);
                result.AudioDuration = clip.length;
            }

            return result;
        }
    }
}
