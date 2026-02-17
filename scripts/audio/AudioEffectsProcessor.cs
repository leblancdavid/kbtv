#nullable enable

using System;
using Godot;

namespace KBTV.Audio
{
    /// <summary>
    /// Applies DSP effects to caller audio based on equipment level.
    /// Uses Godot's AudioEffect infrastructure for LowPass, HighPass, and Distortion.
    /// </summary>
    public partial class AudioEffectsProcessor : Node
    {
        private int _equipmentLevel = 1;
        
        // Effect parameters for each equipment level
        private static readonly (float lowPass, float highPass, float distortion)[] EffectPresets = 
        {
            (2200f, 500f, 0.12f),  // Level 1: Very muffled, lots of distortion
            (3500f, 350f, 0.08f),   // Level 2: Muffled, some distortion
            (6000f, 200f, 0.04f),   // Level 3: Near clear, minimal distortion
            (10000f, 100f, 0.0f)    // Level 4: Crystal clear, no distortion
        };

        public override void _Ready()
        {
            // AudioEffectsProcessor is primarily for configuration
            // Actual effect application would be done through Godot's AudioBus system
        }

        /// <summary>
        /// Sets the equipment level and returns the effect parameters.
        /// </summary>
        public void SetEquipmentLevel(int level)
        {
            _equipmentLevel = Mathf.Clamp(level, 1, 4);
        }

        /// <summary>
        /// Gets the current equipment level.
        /// </summary>
        public int GetEquipmentLevel() => _equipmentLevel;

        /// <summary>
        /// Gets the effect parameters for the current equipment level.
        /// </summary>
        public (float lowPassHz, float highPassHz, float distortion) GetCurrentEffectSettings()
        {
            int index = Mathf.Clamp(_equipmentLevel - 1, 0, EffectPresets.Length - 1);
            return EffectPresets[index];
        }

        /// <summary>
        /// Gets effect parameters for a specific level.
        /// </summary>
        public static (float lowPassHz, float highPassHz, float distortion) GetSettingsForLevel(int level)
        {
            int index = Mathf.Clamp(level - 1, 0, EffectPresets.Length - 1);
            return EffectPresets[index];
        }

        /// <summary>
        /// Returns a human-readable description of the current audio quality.
        /// </summary>
        public string GetQualityDescription()
        {
            return _equipmentLevel switch
            {
                1 => "Poor - Heavy static, narrow band",
                2 => "Fair - Some static, wider band",
                3 => "Good - Minimal static, clear",
                4 => "Excellent - Crystal clear",
                _ => "Unknown"
            };
        }
    }
}
