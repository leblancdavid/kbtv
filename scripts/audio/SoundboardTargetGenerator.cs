#nullable enable

using Godot;

namespace KBTV.Audio
{
    /// <summary>
    /// Whether a knob is inside the target ("perfect"), close ("acceptable"),
    /// needing attention, or badly off. Also used as LED colors.
    /// Ordered from best to worst.
    /// </summary>
    public enum SoundboardBand
    {
        None,
        Green,
        Blue,
        Yellow,
        Red
    }

    /// <summary>
    /// Per-knob target knob positions for the CALLER channel, all near neutral
    /// (0.5) but jittered per caller so no two callers sound exactly alike.
    /// </summary>
    public readonly struct SoundboardCallerTargets
    {
        public readonly float Gain;
        public readonly float LowPass;
        public readonly float HighPass;

        public SoundboardCallerTargets(float gain, float lowPass, float highPass)
        {
            Gain = gain;
            LowPass = lowPass;
            HighPass = highPass;
        }
    }

    /// <summary>
    /// Per-knob LED bands for the CALLER channel.
    /// </summary>
    public readonly struct SoundboardCallerBands
    {
        public readonly SoundboardBand Gain;
        public readonly SoundboardBand LowPass;
        public readonly SoundboardBand HighPass;

        public SoundboardCallerBands(SoundboardBand gain, SoundboardBand lowPass, SoundboardBand highPass)
        {
            Gain = gain;
            LowPass = lowPass;
            HighPass = highPass;
        }
    }

    /// <summary>
    /// Pure static logic that turns caller state into soundboard LED bands.
    /// Used by both the UI (<see cref="UI.SoundboardOverlay"/>) and the
    /// <see cref="Monitors.SoundboardMonitor"/> so the two can never disagree.
    /// </summary>
    public static class SoundboardTargetGenerator
    {
        /// <summary>Within this distance of the target the knob is GREEN (perfect).</summary>
        public const float PerfectTolerance = 0.05f;

        /// <summary>Within this distance of the target the knob is BLUE (acceptable).</summary>
        public const float AcceptableTolerance = 0.12f;

        /// <summary>Within this distance of the target the knob is YELLOW (attention).</summary>
        public const float YellowTolerance = 0.25f;

        /// <summary>Maximum knob-space offset from neutral a caller's target can reach.</summary>
        public const float JitterRange = 0.08f;

        private const float MaxEquipmentLevel = 4f;
        private const float MinEquipmentLevel = 1f;

        /// <summary>
        /// Target knob positions for the caller channel. The center sits at neutral
        /// plus a small stable jitter derived from the caller's SpeakingVolume, so
        /// callers differ but their perfect position always stays near the preset.
        /// </summary>
        public static SoundboardCallerTargets GetCallerTargets(float speakingVolume)
        {
            float jitter = Mathf.Clamp(speakingVolume, 0f, 1f);
            jitter = (jitter - 0.5f) * 2f * JitterRange;
            float center = SoundboardKnobState.NeutralValue + jitter;
            return new SoundboardCallerTargets(center, center, center);
        }

        /// <summary>
        /// The target bands for each caller knob given the current knob state and
        /// the caller's SpeakingVolume. VERN/ADS/FADER are nominal (center) targets.
        /// </summary>
        public static SoundboardCallerBands GetCallerBands(SoundboardKnobState state, float speakingVolume)
        {
            if (state == null)
            {
                return new SoundboardCallerBands(SoundboardBand.None, SoundboardBand.None, SoundboardBand.None);
            }

            var targets = GetCallerTargets(speakingVolume);
            return new SoundboardCallerBands(
                GetBand(state.CallerGain, targets.Gain),
                GetBand(state.CallerLowPass, targets.LowPass),
                GetBand(state.CallerHighPass, targets.HighPass));
        }

        /// <summary>
        /// Whether the knob counts as "off-perfect" for the mood-drain consequence.
        /// Only GREEN counts as on-perfect; BLUE/YELLOW/RED all drain.
        /// </summary>
        public static bool IsOffPerfect(SoundboardBand band) => band != SoundboardBand.Green;

        /// <summary>
        /// LED band for a knob value relative to a target position, using the
        /// perfect/acceptable/yellow tolerances.
        /// </summary>
        public static SoundboardBand GetBand(float knobValue, float target)
        {
            float distance = Mathf.Abs(knobValue - target);
            if (distance <= PerfectTolerance)
            {
                return SoundboardBand.Green;
            }
            if (distance <= AcceptableTolerance)
            {
                return SoundboardBand.Blue;
            }
            if (distance <= YellowTolerance)
            {
                return SoundboardBand.Yellow;
            }
            return SoundboardBand.Red;
        }

        /// <summary>
        /// The overall channel band is the worst (highest severity) of the given bands.
        /// </summary>
        public static SoundboardBand GetWorstBand(SoundboardCallerBands bands)
        {
            return GetWorstBand(bands.Gain, bands.LowPass, bands.HighPass);
        }

        public static SoundboardBand GetWorstBand(params SoundboardBand[] bands)
        {
            SoundboardBand worst = SoundboardBand.None;
            foreach (var band in bands)
            {
                if ((int)band > (int)worst)
                {
                    worst = band;
                }
            }
            return worst;
        }

        /// <summary>
        /// Effective caller line quality: the phone-line equipment level adjusted by
        /// the caller's phone quality. Clamped to the equipment range 1..4.
        /// </summary>
        public static int GetCallerEffectiveLevel(int phoneLineLevel, int phoneQualityModifier)
        {
            return Mathf.RoundToInt(Mathf.Clamp(
                phoneLineLevel + phoneQualityModifier,
                MinEquipmentLevel,
                MaxEquipmentLevel));
        }
    }
}