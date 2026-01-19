#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KBTV.Core;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Runtime repository for managing the broadcast transcript.
    /// Stores transcript entries for the current show only.
    /// </summary>
    [GlobalClass]
    public partial class TranscriptRepository : Node, ITranscriptRepository
    {
        private readonly List<TranscriptEntry> _entries = new();
        private bool _showActive = false;

        public int EntryCount => _entries.Count;

        public override void _Ready()
        {
            ServiceRegistry.Instance.RegisterSelf<ITranscriptRepository>(this);
            ServiceRegistry.Instance.RegisterSelf<TranscriptRepository>(this);
            GD.Print("TranscriptRepository: Initialized");
        }

        public void StartNewShow()
        {
            _entries.Clear();
            _showActive = true;
            GD.Print("TranscriptRepository: New show started");
        }

        public void AddEntry(TranscriptEntry entry)
        {
            if (entry == null)
            {
                GD.PrintErr("TranscriptRepository: Cannot add null entry");
                return;
            }

            if (!_showActive)
            {
                GD.Print("TranscriptRepository: No show active, entry not added");
                return;
            }

            _entries.Add(entry);
            var displayText = entry.Text.Length > 50 ? entry.Text.Substring(0, 50) + "..." : entry.Text;
            GD.Print($"TranscriptRepository: Added entry {entry.SpeakerName}: {displayText} (total: {_entries.Count})");
        }

        public IReadOnlyList<TranscriptEntry> GetCurrentShowTranscript()
        {
            return _entries.AsReadOnly();
        }

        public void ClearCurrentShow()
        {
            _entries.Clear();
            _showActive = false;
            GD.Print("TranscriptRepository: Show cleared");
        }

        public TranscriptEntry? GetLatestEntry()
        {
            return _entries.Count > 0 ? _entries[^1] : null;
        }

        public IReadOnlyList<TranscriptEntry> GetEntriesForArc(string arcId)
        {
            if (string.IsNullOrEmpty(arcId))
            {
                return new List<TranscriptEntry>();
            }

            return _entries.Where(e => e.ArcId == arcId).ToList();
        }
    }
}
