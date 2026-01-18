#nullable enable

using System.Collections.Generic;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Interface for transcript repository operations.
    /// Manages the broadcast transcript for the current show.
    /// </summary>
    public interface ITranscriptRepository
    {
        /// <summary>
        /// Start a new show transcript. Clears any previous entries.
        /// </summary>
        void StartNewShow();

        /// <summary>
        /// Add an entry to the transcript.
        /// </summary>
        /// <param name="entry">The transcript entry to add.</param>
        void AddEntry(TranscriptEntry entry);

        /// <summary>
        /// Get all entries for the current show.
        /// </summary>
        /// <returns>Read-only list of transcript entries.</returns>
        IReadOnlyList<TranscriptEntry> GetCurrentShowTranscript();

        /// <summary>
        /// Clear the current show transcript.
        /// </summary>
        void ClearCurrentShow();

        /// <summary>
        /// Get the number of entries in the current show.
        /// </summary>
        int EntryCount { get; }

        /// <summary>
        /// Get the most recent transcript entry.
        /// </summary>
        TranscriptEntry? GetLatestEntry();

        /// <summary>
        /// Get entries for a specific arc.
        /// </summary>
        /// <param name="arcId">The arc ID to filter by.</param>
        /// <returns>List of entries for the arc.</returns>
        IReadOnlyList<TranscriptEntry> GetEntriesForArc(string arcId);
    }
}
