using System;
using System.Collections.Generic;
using Godot;
using KBTV.Callers;
using KBTV.Core;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Runtime repository for conversation arcs.
    /// Arcs are loaded by the editor and stored here for runtime access.
    /// </summary>
    public partial class ArcRepository : Resource, IArcRepository
    {
        [Export] private Godot.Collections.Array<string> _arcJsonFilePaths = new Godot.Collections.Array<string>();

        private Godot.Collections.Array<ConversationArc> _arcs = new Godot.Collections.Array<ConversationArc>();
        private bool _initialized;

        public Godot.Collections.Array<ConversationArc> Arcs
        {
            get
            {
                EnsureInitialized();
                return _arcs;
            }
        }

        public bool IsInitialized => _initialized;

        public void Initialize()
        {
            if (_initialized) return;
            var filePathsToLoad = new Godot.Collections.Array<string>(_arcJsonFilePaths);

            if (filePathsToLoad.Count == 0)
            {
                filePathsToLoad = DiscoverArcFiles();
            }

            _arcs.Clear();

            foreach (var filePath in filePathsToLoad)
            {
                if (string.IsNullOrEmpty(filePath)) continue;

                try
                {
                    var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
                    if (file == null)
                    {
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
                catch
                {
                }
            }

            _initialized = true;
            Log.Debug($"ArcRepository: Initialized with {_arcs.Count} arcs loaded");
        }



        private Godot.Collections.Array<string> DiscoverArcFiles()
        {
            var foundFiles = new Godot.Collections.Array<string>();
            var searchDir = "res://assets/dialogue/arcs";

            var dirAccess = DirAccess.Open(searchDir);
            if (dirAccess == null)
            {
                return foundFiles;
            }

            DiscoverArcFilesRecursive(searchDir, foundFiles);
            return foundFiles;
        }

        private void DiscoverArcFilesRecursive(string dir, Godot.Collections.Array<string> foundFiles)
        {
            var dirAccess = DirAccess.Open(dir);
            if (dirAccess == null)
            {
                return;
            }

            foreach (var entry in dirAccess.GetFiles())
            {
                if (entry.EndsWith(".json"))
                {
                    var fullPath = dir + "/" + entry;
                    foundFiles.Add(fullPath);
                }
            }

            foreach (var subDir in dirAccess.GetDirectories())
            {
                DiscoverArcFilesRecursive(dir + "/" + subDir, foundFiles);
            }
        }

        public List<ConversationArc> FindMatchingArcs(ShowTopic topic, CallerLegitimacy legitimacy)
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

        public ConversationArc GetRandomArc(CallerLegitimacy legitimacy)
        {
            EnsureInitialized();
            var matches = new List<ConversationArc>();

            foreach (var arc in _arcs)
            {
                if (arc.Legitimacy == legitimacy)
                {
                    matches.Add(arc);
                }
            }

            if (matches.Count == 0) return null;

            return matches[(int)(GD.Randi() % matches.Count)];
        }

        public ConversationArc? GetRandomArcForTopic(ShowTopic topic, CallerLegitimacy legitimacy)
        {
            var matches = FindMatchingArcs(topic, legitimacy);
            if (matches.Count == 0) return null;

            return matches[(int)(GD.Randi() % matches.Count)];
        }

        public ConversationArc? GetRandomArcForDifferentTopic(ShowTopic excludeTopic, CallerLegitimacy legitimacy)
        {
            EnsureInitialized();
            var candidates = new List<ConversationArc>();

            foreach (var arc in _arcs)
            {
                if (arc.Legitimacy == legitimacy &&
                    arc.Topic != excludeTopic &&
                    !arc.IsTopicSwitcher)
                {
                    candidates.Add(arc);
                }
            }

            if (candidates.Count == 0) return null;
            return candidates[(int)(GD.Randi() % candidates.Count)];
        }

        public List<ConversationArc> FindTopicSwitcherArcs(ShowTopic claimedTopic, ShowTopic actualTopic, CallerLegitimacy legitimacy)
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

        public ConversationArc GetRandomTopicSwitcherArc(ShowTopic claimedTopic, ShowTopic actualTopic, CallerLegitimacy legitimacy)
        {
            var matches = FindTopicSwitcherArcs(claimedTopic, actualTopic, legitimacy);
            if (matches.Count == 0) return null;

            return matches[(int)(GD.Randi() % matches.Count)];
        }

        public void AddArc(ConversationArc arc)
        {
            EnsureInitialized();
            _arcs.Add(arc);
        }

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
