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
    /// A single status feed entry: message text, category, and the in-show time it was logged.
    /// </summary>
    public record StatusFeedEntry(string Message, StatusFeedKind Kind, string FormattedTime);

    /// <summary>
    /// Pure rotating store of recent status messages (newest first) used by the top overlay.
    /// Kept dependency-free so it can be unit tested without a scene tree.
    /// </summary>
    public class StatusFeedModel
    {
        public const int MaxEntries = 3;

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
            _entries.Insert(0, new StatusFeedEntry(message, kind, FormatTime(elapsedSeconds)));
            if (_entries.Count > MaxEntries)
            {
                _entries.RemoveAt(MaxEntries);
            }
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