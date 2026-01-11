using System;
using System.Collections.Generic;
using UnityEngine;
using KBTV.Callers;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Runtime repository for conversation arcs.
    /// Arcs are loaded by the editor and stored here for runtime access.
    /// </summary>
    [CreateAssetMenu(fileName = "ArcRepository", menuName = "KBTV/Dialogue/Arc Repository")]
    public class ArcRepository : ScriptableObject
    {
        [SerializeField] private List<TextAsset> _arcJsonFiles = new List<TextAsset>();
        
        // Runtime cache of loaded arcs
        private List<ConversationArc> _arcs;
        private bool _initialized;

        /// <summary>
        /// All loaded conversation arcs.
        /// </summary>
        public IReadOnlyList<ConversationArc> Arcs
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
            _arcs = new List<ConversationArc>();

            foreach (var jsonFile in _arcJsonFiles)
            {
                if (jsonFile == null) continue;

                try
                {
                    var arc = ArcJsonParser.Parse(jsonFile.text);
                    if (arc != null)
                    {
                        _arcs.Add(arc);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"ArcRepository: Failed to parse arc JSON '{jsonFile.name}': {e.Message}");
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

            return matches[UnityEngine.Random.Range(0, matches.Count)];
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

            return matches[UnityEngine.Random.Range(0, matches.Count)];
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
            _arcs?.Clear();
            _initialized = false;
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        private void OnEnable()
        {
            // Reset on domain reload
            _initialized = false;
            _arcs = null;
        }
    }
}
