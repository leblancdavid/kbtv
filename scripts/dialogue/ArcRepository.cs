using System;
using System.Collections.Generic;
using Godot;
using KBTV.Callers;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Runtime repository for conversation arcs.
    /// Arcs are loaded by the editor and stored here for runtime access.
    /// </summary>
    public partial class ArcRepository : Resource, IArcRepository
    {
        [Export] private Godot.Collections.Array<string> _arcJsonFilePaths = new Godot.Collections.Array<string>();

        // Runtime cache of loaded arcs
        private Godot.Collections.Array<ConversationArc> _arcs = new Godot.Collections.Array<ConversationArc>();
        private bool _initialized;

        /// <summary>
        /// All loaded conversation arcs.
        /// </summary>
        public Godot.Collections.Array<ConversationArc> Arcs
        {
            get
            {
                EnsureInitialized();
                return _arcs;
            }
        }

        /// <summary>
        /// Initialize the repository by parsing all arc JSON files.
        /// </summary>
        public void Initialize()
        {
            _arcs.Clear();

            foreach (var filePath in _arcJsonFilePaths)
            {
                if (string.IsNullOrEmpty(filePath)) continue;

                try
                {
                    var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
                    if (file == null)
                    {
                        GD.PrintErr($"ArcRepository: Failed to open arc JSON file '{filePath}': {FileAccess.GetOpenError()}");
                        continue;
                    }

                    string json = file.GetAsText();
                    file.Close();

                    var arc = ArcJsonParser.Parse(json);
                    if (arc != null)
                    {
                        _arcs.Add(arc);
                    }
                }
                catch (Exception e)
                {
                    GD.PrintErr($"ArcRepository: Failed to parse arc JSON '{filePath}': {e.Message}");
                }
            }

            _initialized = true;
        }

        /// <summary>
        /// Find all arcs matching the given topic and legitimacy.
        /// </summary>
        public List<ConversationArc> FindMatchingArcs(string topic, CallerLegitimacy legitimacy)
        {
            EnsureInitialized();
            var matches = new List<ConversationArc>();

            foreach (var arc in _arcs)
            {
                if (arc.Matches(topic, legitimacy))
                {
                    matches.Add(arc);
                }
            }

            return matches;
        }

        /// <summary>
        /// Get a random arc matching the given criteria.
        /// Returns null if no matching arcs found.
        /// </summary>
        public ConversationArc GetRandomArc(string topic, CallerLegitimacy legitimacy)
        {
            var matches = FindMatchingArcs(topic, legitimacy);
            if (matches.Count == 0) return null;

            return matches[(int)(GD.Randi() % matches.Count)];
        }

        /// <summary>
        /// Find all topic-switcher arcs matching a caller who lied about their topic.
        /// </summary>
        public List<ConversationArc> FindTopicSwitcherArcs(string claimedTopic, string actualTopic, CallerLegitimacy legitimacy)
        {
            EnsureInitialized();
            var matches = new List<ConversationArc>();

            foreach (var arc in _arcs)
            {
                if (arc.MatchesTopicSwitcher(claimedTopic, actualTopic, legitimacy))
                {
                    matches.Add(arc);
                }
            }

            return matches;
        }

        /// <summary>
        /// Get a random topic-switcher arc for a caller who lied about their topic.
        /// Returns null if no matching switcher arc found.
        /// </summary>
        public ConversationArc GetRandomTopicSwitcherArc(string claimedTopic, string actualTopic, CallerLegitimacy legitimacy)
        {
            var matches = FindTopicSwitcherArcs(claimedTopic, actualTopic, legitimacy);
            if (matches.Count == 0) return null;

            return matches[(int)(GD.Randi() % matches.Count)];
        }

        /// <summary>
        /// Add an arc to the repository (used by editor tools).
        /// </summary>
        public void AddArc(ConversationArc arc)
        {
            EnsureInitialized();
            _arcs.Add(arc);
        }

        /// <summary>
        /// Clear all arcs from the repository.
        /// </summary>
        public void Clear()
        {
            _arcs.Clear();
            _initialized = false;
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }
    }
}