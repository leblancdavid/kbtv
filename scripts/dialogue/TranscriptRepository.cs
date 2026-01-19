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

        public event System.Action<TranscriptEntry>? EntryAdded;

        public override void _Ready()
        {
            ServiceRegistry.Instance.RegisterSelf<ITranscriptRepository>(this);
            ServiceRegistry.Instance.RegisterSelf<TranscriptRepository>(this);
        }

        public void StartNewShow()
        {
            _entries.Clear();
            _showActive = true;
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
                return;
            }

            _entries.Add(entry);
            EntryAdded?.Invoke(entry);
        }

        public IReadOnlyList<TranscriptEntry> GetCurrentShowTranscript()
        {
            return _entries.AsReadOnly();
        }

        public void ClearCurrentShow()
        {
            _entries.Clear();
            _showActive = false;
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
