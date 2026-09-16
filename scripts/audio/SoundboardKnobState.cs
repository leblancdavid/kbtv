#nullable enable

using Godot;

namespace KBTV.Audio
{
    /// <summary>
    /// Normalized (0..1) knob/fader positions for the 3-channel soundboard.
    /// 0.5 (NeutralValue) means "no offset": the equipment preset is unchanged.
    /// Knob deltas are stacked on top of the AudioEffectsProcessor presets by
    /// <see cref="AudioMixerManager.ApplySoundboard"/>.
    /// </summary>
    public sealed class SoundboardKnobState
    {
        public const float MinValue = 0f;
        public const float MaxValue = 1f;
        public const float NeutralValue = 0.5f;

        /// <summary>Caller channel gain (drives distortion + amplify).</summary>
        public float CallerGain { get; set; } = NeutralValue;

        /// <summary>Caller low-pass cutoff offset.</summary>
        public float CallerLowPass { get; set; } = NeutralValue;

        /// <summary>Caller high-pass cutoff offset.</summary>
        public float CallerHighPass { get; set; } = NeutralValue;

        /// <summary>Vern studio channel gain.</summary>
        public float VernGain { get; set; } = NeutralValue;

        /// <summary>Ads/bumper channel gain.</summary>
        public float AdsGain { get; set; } = NeutralValue;

        /// <summary>Caller channel output level (fader strip).</summary>
        public float CallerLevel { get; set; } = NeutralValue;

        /// <summary>Vern studio channel output level (fader strip).</summary>
        public float VernLevel { get; set; } = NeutralValue;

        /// <summary>Ads/bumper channel output level (fader strip).</summary>
        public float AdsLevel { get; set; } = NeutralValue;

        /// <summary>Master (music/program) fader.</summary>
        public float Fader { get; set; } = NeutralValue;

        public void ResetToNeutral()
        {
            CallerGain = NeutralValue;
            CallerLowPass = NeutralValue;
            CallerHighPass = NeutralValue;
            VernGain = NeutralValue;
            AdsGain = NeutralValue;
            CallerLevel = NeutralValue;
            VernLevel = NeutralValue;
            AdsLevel = NeutralValue;
            Fader = NeutralValue;
        }

        public void CopyFrom(SoundboardKnobState other)
        {
            if (other == null)
            {
                return;
            }

            CallerGain = other.CallerGain;
            CallerLowPass = other.CallerLowPass;
            CallerHighPass = other.CallerHighPass;
            VernGain = other.VernGain;
            AdsGain = other.AdsGain;
            CallerLevel = other.CallerLevel;
            VernLevel = other.VernLevel;
            AdsLevel = other.AdsLevel;
            Fader = other.Fader;
        }

        public static SoundboardKnobState Neutral() => new SoundboardKnobState();

        /// <summary>
        /// Clamped knob position (0..1).
        /// </summary>
        public static float Clamp(float knobValue) => Mathf.Clamp(knobValue, MinValue, MaxValue);

        /// <summary>
        /// Raw offset from neutral, clamped to -0.5..+0.5.
        /// </summary>
        public static float KnobDelta(float knobValue) => Clamp(knobValue) - NeutralValue;

        /// <summary>
        /// Normalized offset from neutral, mapped to -1..+1.
        /// </summary>
        public static float NormalizedDelta(float knobValue) => KnobDelta(knobValue) * 2f;
    }
}