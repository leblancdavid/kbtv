using System.Collections.Generic;
using KBTV.Core;
using KBTV.Data;
using Godot;

namespace KBTV.Screening
{
    /// <summary>
    /// Static utility class that maps caller personalities to unique stat effects.
    /// Each of the 36 personalities has a distinct combination of Physical, Emotional, and Mental effects.
    /// 
    /// Personality Categories:
    /// - Positive (12): Total effect +5 to +6, generally boost stats
    /// - Negative (12): Total effect -6 to -8, generally drain stats
    /// - Neutral (12): Total effect -2 to +4, mixed effects with trade-offs
    /// </summary>
    public static class PersonalityStatEffects
    {
        private static Godot.Collections.Dictionary _personalitiesData;
        private static bool _isLoaded = false;

        /// <summary>
        /// Load personality configuration from JSON file.
        /// Should be called once during game initialization.
        /// </summary>
        private static void LoadConfig()
        {
            if (_isLoaded) return;

            var jsonPath = "res://assets/config/stat_modifiers.json";
            var file = Godot.FileAccess.FileExists(jsonPath) ? Godot.FileAccess.Open(jsonPath, Godot.FileAccess.ModeFlags.Read) : null;
            if (file == null)
            {
                Log.Error($"Failed to load stat modifiers config from {jsonPath}");
                _personalitiesData = new Godot.Collections.Dictionary();
                _isLoaded = true;
                return;
            }

            try
            {
                var jsonText = file.GetAsText();
                file.Close();

                var json = new Json();
                var error = json.Parse(jsonText);
                if (error != Error.Ok)
                {
                    Log.Error($"Failed to parse stat modifiers JSON: {json.GetErrorMessage()}");
                    _personalitiesData = new Godot.Collections.Dictionary();
                    _isLoaded = true;
                    return;
                }

                var configData = json.Data.As<Godot.Collections.Dictionary>();
                if (configData != null && configData.ContainsKey("personalities"))
                {
                    _personalitiesData = configData["personalities"].As<Godot.Collections.Dictionary>();
                    if (_personalitiesData == null)
                    {
                        _personalitiesData = new Godot.Collections.Dictionary();
                    }
                }
                else
                {
                    _personalitiesData = new Godot.Collections.Dictionary();
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"Exception loading personality config: {ex.Message}");
                _personalitiesData = new Godot.Collections.Dictionary();
            }

            _isLoaded = true;
        }

        /// <summary>
        /// Parse stat effects from a dictionary containing physical/emotional/mental values.
        /// </summary>
        private static List<StatModification> ParseStatEffects(Godot.Collections.Dictionary effects)
        {
            var result = new List<StatModification>();

            if (effects.ContainsKey("physical"))
            {
                var physical = effects["physical"].AsSingle();
                if (physical != 0) result.Add(new StatModification(StatType.Physical, physical));
            }

            if (effects.ContainsKey("emotional"))
            {
                var emotional = effects["emotional"].AsSingle();
                if (emotional != 0) result.Add(new StatModification(StatType.Emotional, emotional));
            }

            if (effects.ContainsKey("mental"))
            {
                var mental = effects["mental"].AsSingle();
                if (mental != 0) result.Add(new StatModification(StatType.Mental, mental));
            }

            return result;
        }

        /// <summary>
        /// Get stat effects for a given personality name.
        /// </summary>
        /// <param name="personalityName">The personality name (e.g., "Matter-of-fact reporter").</param>
        /// <returns>List of stat modifications, empty if personality not found.</returns>
        public static List<StatModification> GetEffects(string personalityName)
        {
            if (!_isLoaded) LoadConfig();
            if (string.IsNullOrEmpty(personalityName)) return new List<StatModification>();

            if (_personalitiesData.ContainsKey(personalityName))
            {
                var statEffects = _personalitiesData[personalityName].As<Godot.Collections.Dictionary>();
                if (statEffects != null)
                {
                    return ParseStatEffects(statEffects);
                }
            }

            return new List<StatModification>();
        }

        /// <summary>
        /// Check if a personality has defined effects.
        /// </summary>
        /// <param name="personalityName">The personality name to check.</param>
        /// <returns>True if the personality has defined effects.</returns>
        public static bool HasEffects(string personalityName)
        {
            if (!_isLoaded) LoadConfig();
            return !string.IsNullOrEmpty(personalityName) && _personalitiesData.ContainsKey(personalityName);
        }

        /// <summary>
        /// Get all personality names with defined effects.
        /// </summary>
        /// <returns>Collection of personality names.</returns>
        public static IEnumerable<string> GetAllPersonalityNames()
        {
            if (!_isLoaded) LoadConfig();
            var keys = new List<string>();
            foreach (var key in _personalitiesData.Keys)
            {
                keys.Add(key.AsString());
            }
            return keys;
        }
    }
}
