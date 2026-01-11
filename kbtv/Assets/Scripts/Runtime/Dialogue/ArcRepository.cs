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
                    var arc = ParseArcJson(jsonFile.text);
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

        /// <summary>
        /// Parse arc JSON text into a ConversationArc.
        /// Runtime version that doesn't depend on Editor code.
        /// </summary>
        private ConversationArc ParseArcJson(string jsonText)
        {
            var data = JsonUtility.FromJson<ArcJsonData>(jsonText);
            if (data == null) return null;

            var legitimacy = ParseLegitimacy(data.legitimacy);
            var arc = new ConversationArc(data.arcId, data.topic, legitimacy, data.claimedTopic);

            if (data.moodVariants != null)
            {
                AddMoodVariantIfPresent(arc, VernMood.Tired, data.moodVariants.Tired);
                AddMoodVariantIfPresent(arc, VernMood.Grumpy, data.moodVariants.Grumpy);
                AddMoodVariantIfPresent(arc, VernMood.Neutral, data.moodVariants.Neutral);
                AddMoodVariantIfPresent(arc, VernMood.Engaged, data.moodVariants.Engaged);
                AddMoodVariantIfPresent(arc, VernMood.Excited, data.moodVariants.Excited);
            }

            return arc;
        }

        private void AddMoodVariantIfPresent(ConversationArc arc, VernMood mood, ArcMoodVariantData data)
        {
            if (data == null) return;
            arc.AddMoodVariant(mood, ConvertMoodVariant(data));
        }

        private ArcMoodVariant ConvertMoodVariant(ArcMoodVariantData data)
        {
            var variant = new ArcMoodVariant();

            if (data.intro != null)
            {
                foreach (var line in data.intro)
                    variant.Intro.Add(ConvertLine(line));
            }

            if (data.development != null)
            {
                foreach (var line in data.development)
                    variant.Development.Add(ConvertLine(line));
            }

            if (data.beliefBranch != null)
            {
                if (data.beliefBranch.Skeptical != null)
                {
                    foreach (var line in data.beliefBranch.Skeptical)
                        variant.BeliefBranch.Skeptical.Add(ConvertLine(line));
                }
                if (data.beliefBranch.Believing != null)
                {
                    foreach (var line in data.beliefBranch.Believing)
                        variant.BeliefBranch.Believing.Add(ConvertLine(line));
                }
            }

            if (data.conclusion != null)
            {
                foreach (var line in data.conclusion)
                    variant.Conclusion.Add(ConvertLine(line));
            }

            return variant;
        }

        private ArcDialogueLine ConvertLine(ArcLineData data)
        {
            var speaker = string.Equals(data.speaker, "Vern", StringComparison.OrdinalIgnoreCase)
                ? Speaker.Vern
                : Speaker.Caller;
            return new ArcDialogueLine(speaker, data.text ?? "");
        }

        private CallerLegitimacy ParseLegitimacy(string legitimacyString)
        {
            if (string.IsNullOrEmpty(legitimacyString))
                return CallerLegitimacy.Questionable;

            if (Enum.TryParse<CallerLegitimacy>(legitimacyString, true, out var legitimacy))
                return legitimacy;

            return CallerLegitimacy.Questionable;
        }
    }
}
