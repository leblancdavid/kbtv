#nullable enable

using System.Collections.Generic;

namespace KBTV.UI
{
    /// <summary>
    /// Semantic category for a top-overlay status message, mapped to a display color by the overlay.
    /// </summary>
    public enum StatusFeedKind
    {
        Info,
        EvidenceAvailable,
        EvidenceCollected,
        Curse,
        Warning
    }

    /// <summary>
    /// A single status feed entry: message text, category, the in-show time it was logged
    /// (raw seconds + preformatted display).
    /// </summary>
    public record StatusFeedEntry(string Message, StatusFeedKind Kind, string FormattedTime, float ElapsedSeconds);

    /// <summary>
    /// Rotating store of recent status messages (newest first) used by the top overlay.
    /// The overlay displays entries via <see cref="EntriesWithinWindow"/> (recency-based);
    /// <see cref="MaxEntries"/> is only a memory cap for extreme event bursts.
    /// Kept dependency-free so it can be unit tested without a scene tree.
    /// </summary>
    public class StatusFeedModel
    {
        public const int MaxEntries = 12;

        private readonly List<StatusFeedEntry> _entries = new();

        /// <summary>
        /// Entries newest-first, capped at <see cref="MaxEntries"/>.
        /// </summary>
        public IReadOnlyList<StatusFeedEntry> Entries => _entries;

        /// <summary>
        /// Add a new message, dropping the oldest entry when over capacity.
        /// </summary>
        public void Add(string message, StatusFeedKind kind, float elapsedSeconds)
        {
            _entries.Insert(0, new StatusFeedEntry(message, kind, FormatTime(elapsedSeconds), elapsedSeconds));
            if (_entries.Count > MaxEntries)
            {
                _entries.RemoveAt(MaxEntries);
            }
        }

        /// <summary>
        /// Entries logged within <paramref name="windowSeconds"/> of <paramref name="now"/>,
        /// newest-first. Assumes entries were added in non-decreasing elapsed time.
        /// </summary>
        public IReadOnlyList<StatusFeedEntry> EntriesWithinWindow(float now, float windowSeconds)
        {
            var result = new List<StatusFeedEntry>();
            foreach (var entry in _entries)
            {
                if (now - entry.ElapsedSeconds > windowSeconds)
                {
                    break;
                }

                result.Add(entry);
            }

            return result;
        }

        public void Clear() => _entries.Clear();

        /// <summary>
        /// Format elapsed seconds as M:SS (e.g. 4m 33s -> "04:33").
        /// </summary>
        public static string FormatTime(float elapsedSeconds)
        {
            int minutes = (int)(elapsedSeconds / 60f);
            int seconds = (int)(elapsedSeconds % 60f);
            return $"{minutes:D2}:{seconds:D2}";
        }
    }
}