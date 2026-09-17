#nullable enable

using Godot;

namespace KBTV.Audio
{
    /// <summary>
    /// Whether a knob is inside the target ("perfect") or how far / which direction
    /// it is off: GREEN is within tolerance, above the target runs YELLOW to RED,
    /// below the target runs CYAN to BLUE. Also used as LED colors.
    /// <see cref="SoundboardTargetGenerator.GetWorstBand"/> treats GREEN as best,
    /// CYAN/YELLOW as mid, BLUE/RED as worst.
    /// </summary>
    public enum SoundboardBand
    {
        None,
        Green,
        Cyan,
        Yellow,
        Blue,
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
        public const float PerfectTolerance = 0.09f;

        /// <summary>Just below the target within this distance the knob is CYAN.</summary>
        public const float CyanTolerance = 0.12f;

        /// <summary>Just above the target within this distance the knob is YELLOW.</summary>
        public const float YellowTolerance = 0.12f;

        /// <summary>Maximum knob-space offset from neutral a caller's target can reach.</summary>
        public const float JitterRange = 0.08f;

        /// <summary>
        /// Max knob-space spread per knob around the shared volume-jittered center,
        /// from the caller's stable seed so no two callers share an ideal position.
        /// </summary>
        public const float PerKnobJitterRange = 0.11f;

        /// <summary>Caller targets never sit closer to the knob ends than this (keeps them reachable).</summary>
        public const float MinTargetKnob = 0.25f;

        /// <summary>Caller targets never sit closer to the knob ends than this (keeps them reachable).</summary>
        public const float MaxTargetKnob = 0.75f;

        /// <summary>How far from the target the continuous color ramp reaches pure blue/red.</summary>
        public const float ColorRampHalfSpan = 0.30f;

        /// <summary>Shared ramp colors (also used by the 3D hover halo; blue far below, red far above).</summary>
        public static readonly Color RampBlue = new(0.3f, 0.6f, 1f);
        public static readonly Color RampCyan = new(0.25f, 0.95f, 1f);
        public static readonly Color RampGreen = new(0.2f, 0.9f, 0.3f);
        public static readonly Color RampYellow = new(1f, 0.8f, 0.2f);
        public static readonly Color RampRed = new(0.9f, 0.25f, 0.2f);

        private const float MaxEquipmentLevel = 4f;
        private const float MinEquipmentLevel = 1f;

        // Distinct per-knob salts so one seed spreads each CALLER knob differently.
        private const uint GainSalt = 0x475F6911u;
        private const uint LowPassSalt = 0x6F502F01u;
        private const uint HighPassSalt = 0x48503401u;

        /// <summary>
        /// Deterministic 0..1 hash of a seed for one control, so a caller's ideal
        /// positions are stable across frames but differ per knob and per caller.
        /// </summary>
        public static float HashUnit(int seed, uint salt)
        {
            uint h = (uint)seed + salt * 2654435761u;
            h ^= h >> 16;
            h *= 2246822519u;
            h ^= h >> 13;
            h *= 3266489917u;
            h ^= h >> 16;
            return h / 4294967295f;
        }

        /// <summary>
        /// Caller targets that leave every knob at its neutral ("no offset") center.
        /// Used while no caller is on air so the board returns to the clean preset.
        /// </summary>
        public static SoundboardCallerTargets NeutralCallerTargets() =>
            new SoundboardCallerTargets(
                SoundboardKnobState.NeutralValue,
                SoundboardKnobState.NeutralValue,
                SoundboardKnobState.NeutralValue);

        /// <summary>
        /// Target knob positions for the caller channel. The center sits at neutral
        /// plus a small stable jitter derived from the caller's SpeakingVolume, and
        /// each knob is then offset independently ±<see cref="PerKnobJitterRange"/>
        /// from a stable per-caller seed. Targets stay within the reachable band so
        /// the perfect position is always attainable.
        /// </summary>
        public static SoundboardCallerTargets GetCallerTargets(float speakingVolume, int seed = 0)
        {
            float center = SoundboardKnobState.NeutralValue
                + (Mathf.Clamp(speakingVolume, 0f, 1f) - 0.5f) * 2f * JitterRange;

            float KnobTarget(float target, uint salt) => Mathf.Clamp(
                target + (HashUnit(seed, salt) - 0.5f) * 2f * PerKnobJitterRange,
                MinTargetKnob, MaxTargetKnob);

            return new SoundboardCallerTargets(
                KnobTarget(center, GainSalt),
                KnobTarget(center, LowPassSalt),
                KnobTarget(center, HighPassSalt));
        }

        /// <summary>
        /// The target bands for each caller knob given the current knob state, the
        /// caller's SpeakingVolume, and the caller's <see cref="Caller.SoundboardSeed"/>.
        /// VERN/ADS/FADER are nominal (center) targets.
        /// </summary>
        public static SoundboardCallerBands GetCallerBands(
            SoundboardKnobState state, float speakingVolume, int seed = 0)
        {
            if (state == null)
            {
                return new SoundboardCallerBands(SoundboardBand.None, SoundboardBand.None, SoundboardBand.None);
            }

            var targets = GetCallerTargets(speakingVolume, seed);
            return new SoundboardCallerBands(
                GetBand(state.CallerGain, targets.Gain),
                GetBand(state.CallerLowPass, targets.LowPass),
                GetBand(state.CallerHighPass, targets.HighPass));
        }

        /// <summary>
        /// Band for any single control. Caller knobs use the per-caller targets
        /// (via <see cref="GetCallerBands"/>); level faders, Vern/Ads gain, and the
        /// master knob are all nominal (neutral center) targets.
        /// </summary>
        public static SoundboardBand GetControlBand(
            SoundboardKnobState state, SoundboardControl control, float speakingVolume, int seed = 0)
        {
            if (state == null)
            {
                return SoundboardBand.None;
            }

            switch (control)
            {
                case SoundboardControl.CallerGain:
                case SoundboardControl.CallerLowPass:
                case SoundboardControl.CallerHighPass:
                    var callerBands = GetCallerBands(state, speakingVolume, seed);
                    return control == SoundboardControl.CallerGain ? callerBands.Gain
                        : control == SoundboardControl.CallerLowPass ? callerBands.LowPass
                        : callerBands.HighPass;
                case SoundboardControl.None:
                    return SoundboardBand.None;
                default:
                    return GetBand(
                        SoundboardControlApplier.CurrentValue(state, control),
                        SoundboardKnobState.NeutralValue);
            }
        }

        /// <summary>
        /// Signed knob error (current value − target) for a control: positive is
        /// above the target, negative below. Caller knobs use the per-caller targets,
        /// all others the neutral center.
        /// </summary>
        public static float GetControlError(
            SoundboardKnobState state, SoundboardControl control, float speakingVolume, int seed = 0)
        {
            if (state == null)
            {
                return 0f;
            }

            float target;
            switch (control)
            {
                case SoundboardControl.CallerGain:
                case SoundboardControl.CallerLowPass:
                case SoundboardControl.CallerHighPass:
                    var callerTargets = GetCallerTargets(speakingVolume, seed);
                    target = control == SoundboardControl.CallerGain ? callerTargets.Gain
                        : control == SoundboardControl.CallerLowPass ? callerTargets.LowPass
                        : callerTargets.HighPass;
                    break;
                default:
                    target = SoundboardKnobState.NeutralValue;
                    break;
            }

            return SoundboardControlApplier.CurrentValue(state, control) - target;
        }

        /// <summary>
        /// Continuous color for a signed knob error: pure BLUE far below → CYAN →
        /// GREEN at the target → YELLOW → pure RED far above, over
        /// ±<see cref="ColorRampHalfSpan"/>. Used by the 3D hover halo so the glow
        /// blends with how far and which way the knob is off; the discrete LED bands
        /// come from <see cref="GetBand"/>.
        /// </summary>
        public static Color ColorForError(float error)
        {
            float t = Mathf.Clamp(error / ColorRampHalfSpan, -1f, 1f);

            if (t <= 0f)
            {
                // Blue → Cyan → Green
                return t <= -0.5f
                    ? RampBlue.Lerp(RampCyan, (t + 1f) * 2f)
                    : RampCyan.Lerp(RampGreen, (t + 0.5f) * 2f);
            }

            // Green → Yellow → Red
            return t <= 0.5f
                ? RampGreen.Lerp(RampYellow, t * 2f)
                : RampYellow.Lerp(RampRed, (t - 0.5f) * 2f);
        }

        /// <summary>
        /// Whether the knob counts as "off-perfect" for the mood-drain consequence.
        /// Only GREEN counts as on-perfect; CYAN/YELLOW/BLUE/RED all drain.
        /// </summary>
        public static bool IsOffPerfect(SoundboardBand band) => band != SoundboardBand.Green;

        /// <summary>
        /// LED band for a knob value relative to a target position. Directional:
        /// within <see cref="PerfectTolerance"/> of the target is GREEN, above the
        /// target runs YELLOW → RED, below the target runs CYAN → BLUE.
        /// </summary>
        public static SoundboardBand GetBand(float knobValue, float target)
        {
            float error = knobValue - target;
            if (Mathf.Abs(error) <= PerfectTolerance)
            {
                return SoundboardBand.Green;
            }

            return error > 0f
                ? (error <= YellowTolerance ? SoundboardBand.Yellow : SoundboardBand.Red)
                : (-error <= CyanTolerance ? SoundboardBand.Cyan : SoundboardBand.Blue);
        }

        /// <summary>
        /// The overall channel band is the worst (highest severity) of the given bands.
        /// Severity: GREEN best, CYAN/YELLOW mid, BLUE/RED worst. Ties resolve to the
        /// higher enum value (RED wins over BLUE, YELLOW over CYAN).
        /// </summary>
        public static SoundboardBand GetWorstBand(SoundboardCallerBands bands)
        {
            return GetWorstBand(bands.Gain, bands.LowPass, bands.HighPass);
        }

        public static SoundboardBand GetWorstBand(params SoundboardBand[] bands)
        {
            SoundboardBand worst = SoundboardBand.None;
            int severity = BandSeverity(worst);
            foreach (var band in bands)
            {
                int candidateSeverity = BandSeverity(band);
                if (candidateSeverity > severity ||
                    (candidateSeverity == severity && (int)band > (int)worst))
                {
                    worst = band;
                    severity = candidateSeverity;
                }
            }
            return worst;
        }

        private static int BandSeverity(SoundboardBand band) => band switch
        {
            SoundboardBand.Green => 0,
            SoundboardBand.Cyan => 1,
            SoundboardBand.Yellow => 1,
            SoundboardBand.Blue => 2,
            SoundboardBand.Red => 2,
            _ => -1
        };

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