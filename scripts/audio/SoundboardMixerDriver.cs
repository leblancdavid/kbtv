#nullable enable

using Godot;

namespace KBTV.Audio
{
    /// <summary>
    /// The caller-side equipment preset values that knob deltas stack on top of.
    /// Copied by <see cref="AudioMixerManager"/> from its private CallerPresets
    /// table so the knob math stays pure and unit-testable.
    /// </summary>
    public readonly struct SoundboardPresetInfo
    {
        public readonly float LowPassHz;
        public readonly float HighPassHz;
        public readonly float Distortion;

        public SoundboardPresetInfo(float lowPassHz, float highPassHz, float distortion)
        {
            LowPassHz = lowPassHz;
            HighPassHz = highPassHz;
            Distortion = distortion;
        }
    }

    /// <summary>
    /// Final bus/effect parameter settings computed from a knob state, ready to
    /// be pushed into the audio engine. All values are clamped so the effects
    /// never go fully silent or DC. Neutral-equivalent (all dB fields 0, muffle
    /// cutoffs transparent) means "preset audio, unchanged by the board".
    /// Gain knobs/faders raised above their target never get louder: the excess
    /// is turned into drive + compression instead (see <see cref="ComputeEffectSettings"/>).
    /// </summary>
    public readonly struct SoundboardEffectSettings
    {
        public readonly float CallerLowPassHz;
        public readonly float CallerHighPassHz;
        public readonly float CallerDrive;
        public readonly float CallerAmplifyDb;
        public readonly float CallerMuffleHz;
        public readonly float CallerCompression;
        public readonly float VernDrive;
        public readonly float VernCompression;
        public readonly float VernMuffleHz;
        public readonly float AdsDrive;
        public readonly float AdsCompression;
        public readonly float AdsMuffleHz;
        public readonly float CallerLevelDb;
        public readonly float VernLevelDb;
        public readonly float AdsLevelDb;
        public readonly float MusicFaderDb;
        public readonly float MasterFaderDb;

        public SoundboardEffectSettings(
            float callerLowPassHz, float callerHighPassHz, float callerDrive,
            float callerAmplifyDb, float callerMuffleHz,
            float callerCompression,
            float vernDrive, float vernCompression, float vernMuffleHz,
            float adsDrive, float adsCompression, float adsMuffleHz,
            float callerLevelDb, float vernLevelDb, float adsLevelDb,
            float musicFaderDb, float masterFaderDb)
        {
            CallerLowPassHz = callerLowPassHz;
            CallerHighPassHz = callerHighPassHz;
            CallerDrive = callerDrive;
            CallerAmplifyDb = callerAmplifyDb;
            CallerMuffleHz = callerMuffleHz;
            CallerCompression = callerCompression;
            VernDrive = vernDrive;
            VernCompression = vernCompression;
            VernMuffleHz = vernMuffleHz;
            AdsDrive = adsDrive;
            AdsCompression = adsCompression;
            AdsMuffleHz = adsMuffleHz;
            CallerLevelDb = callerLevelDb;
            VernLevelDb = vernLevelDb;
            AdsLevelDb = adsLevelDb;
            MusicFaderDb = musicFaderDb;
            MasterFaderDb = masterFaderDb;
        }

        public static readonly SoundboardEffectSettings Neutral =
            new SoundboardEffectSettings(
                callerLowPassHz: 0f, callerHighPassHz: 0f, callerDrive: 0f,
                callerAmplifyDb: 0f, callerMuffleHz: 0f,
                callerCompression: 0f,
                vernDrive: 0f, vernCompression: 0f, vernMuffleHz: 0f,
                adsDrive: 0f, adsCompression: 0f, adsMuffleHz: 0f,
                callerLevelDb: 0f, vernLevelDb: 0f, adsLevelDb: 0f,
                musicFaderDb: 0f, masterFaderDb: 0f);
    }

    /// <summary>
    /// Holds the soundboard knob state and applies it to the live buses through
    /// <see cref="AudioMixerManager.ApplySoundboard"/>. Knob->effect math lives in
    /// <see cref="ComputeEffectSettings"/> as a pure function so it can be unit
    /// tested without an AudioServer.
    /// </summary>
    public sealed class SoundboardMixerDriver
    {
// Knob deltas are normalized to -1..+1; these spans are the total range
        // they swing either side of the equipment preset.
        public const float CallerLowPassSpanHz = 3000f;
        public const float CallerLowPassMinHz = 150f;
        public const float CallerLowPassMaxHz = 10000f;
        public const float CallerHighPassSpanHz = 1200f;
        public const float CallerHighPassMinHz = 40f;
        public const float CallerHighPassMaxHz = 1500f;
        public const float CallerDriveSpan = 0.35f;
        public const float CallerDriveMin = 0.05f;
        public const float CallerDriveMax = 0.95f;

        // Above-target gain never gets louder: below-target pulls the base 0 dB
        // amplify down to -6 dB, above-target is scored as compression instead.
        public const float CallerAttenuateSpanDb = 6f;
        public const float CallerAmplifyMinDb = -6f;
        public const float CallerAmplifyMaxDb = 0f;

        // Low end of the gain knobs sweeps the muffle low-pass down to a dull rumble
        // rather than cutting the channel; high end is scored as drive + compression.
        public const float MuffleTransparentHz = 20000f;
        public const float MuffleMuffledHz = 220f;

        // Vern/Ads gain knobs and faders sit at neutral; pulled above it the excess
        // turns into drive + compression instead of getting louder.
        public const float VernDriveSpan = 0.55f;
        public const float AdsDriveSpan = 0.55f;

        // Per-channel output level faders move the whole bus strip around neutral,
        // capped at 0 dB above (excess above neutral is scored as compression).
        public const float CallerLevelSpanDb = 30f;
        public const float CallerLevelMinDb = -30f;
        public const float CallerLevelMaxDb = 0f;
        public const float VernLevelSpanDb = 30f;
        public const float VernLevelMinDb = -30f;
        public const float VernLevelMaxDb = 0f;
        public const float AdsLevelSpanDb = 30f;
        public const float AdsLevelMinDb = -30f;
        public const float AdsLevelMaxDb = 0f;

        public const float MusicFaderSpanDb = 14f;
        public const float MusicFaderMinDb = -30f;
        public const float MusicFaderMaxDb = 14f;
        public const float MasterFaderSpanDb = 8f;
        public const float MasterFaderMinDb = -12f;
        public const float MasterFaderMaxDb = 8f;

        private AudioMixerManager? _mixer;
        private SoundboardCallerTargets? _callerTargets;

        public SoundboardMixerDriver(AudioMixerManager? mixer = null)
        {
            _mixer = mixer;
        }

        /// <summary>Current knob positions (neutral by default).</summary>
        public SoundboardKnobState State { get; } = SoundboardKnobState.Neutral();

        /// <summary>
        /// The ideal caller knob positions the board grades/DSPs against. Null means
        /// "no caller" and uses neutral targets.
        /// </summary>
        public SoundboardCallerTargets? CallerTargets => _callerTargets;

        public bool HasMixer => _mixer != null;

        public void Attach(AudioMixerManager? mixer) => _mixer = mixer;

        public void Detach() => _mixer = null;

        /// <summary>
        /// Sets the ideal caller knob positions and forwards them to the mixer so
        /// the DSP stack grades against the same values as the board.
        /// </summary>
        public void SetCallerTargets(SoundboardCallerTargets? targets)
        {
            _callerTargets = targets;
            _mixer?.SetCallerTargets(targets);
        }

        /// <summary>Pushes the current knob state into the audio engine (no-op without a mixer).</summary>
        public void Apply() => _mixer?.ApplySoundboard(State);

        /// <summary>Returns every knob to the neutral center and applies the result.</summary>
        public void ResetToNeutral()
        {
            State.ResetToNeutral();
            Apply();
        }

        /// <summary>
        /// Muffle depth (0 = transparent, 1 = fully muffled) for a gain-knob delta
        /// pulled below its target.
        /// </summary>
        private static float MuffleDepth(float gainDelta) => Mathf.Max(-gainDelta, 0f);

        /// <summary>
        /// Normalizes a knob's distance from its target to -1..+1, where +1 means
        /// "pushed all the way above target" and -1 "pulled all the way below".
        /// </summary>
        public static float NormalizedDeltaFrom(float knobValue, float target) =>
            Mathf.Clamp((knobValue - target) * 2f, -1f, 1f);

        /// <summary>
        /// Pure knob->effect mapping: each target = equipment preset value + knob
        /// delta, fully clamped. Unit-testable without an AudioServer. Caller knobs
        /// grade against the caller's per-caller targets (null targets = neutral,
        /// e.g. no caller on air); Vern/Ads gain + the output faders grade against
        /// neutral center. Above-target gain never gets louder — it becomes drive
        /// and compression; below-target muffles instead of attenuating.
        /// </summary>
        public static SoundboardEffectSettings ComputeEffectSettings(
            SoundboardKnobState state, SoundboardPresetInfo preset,
            SoundboardCallerTargets? targets = null)
        {
            if (state == null)
            {
                return SoundboardEffectSettings.Neutral;
            }

            var t = targets ?? SoundboardTargetGenerator.NeutralCallerTargets();

            float center = SoundboardKnobState.NeutralValue;
            float gainDelta = NormalizedDeltaFrom(state.CallerGain, t.Gain);
            float lowPassDelta = NormalizedDeltaFrom(state.CallerLowPass, t.LowPass);
            float highPassDelta = NormalizedDeltaFrom(state.CallerHighPass, t.HighPass);
            float vernDelta = NormalizedDeltaFrom(state.VernGain, center);
            float adsDelta = NormalizedDeltaFrom(state.AdsGain, center);
            float callerLevelDelta = NormalizedDeltaFrom(state.CallerLevel, center);
            float vernLevelDelta = NormalizedDeltaFrom(state.VernLevel, center);
            float adsLevelDelta = NormalizedDeltaFrom(state.AdsLevel, center);
            float faderDelta = SoundboardKnobState.NormalizedDelta(state.Fader);

            float gainOver = Mathf.Max(gainDelta, 0f);
            float gainBelow = Mathf.Max(-gainDelta, 0f);
            float vernOver = Mathf.Max(vernDelta, 0f);
            float adsOver = Mathf.Max(adsDelta, 0f);

            // A knob/fader pushed above target is never louder — score the excess
            // as compression; either channel's over turns into drive too.
            float callerTotalOver = Mathf.Max(gainOver, Mathf.Max(callerLevelDelta, 0f));
            float vernTotalOver = Mathf.Max(vernOver, Mathf.Max(vernLevelDelta, 0f));
            float adsTotalOver = Mathf.Max(adsOver, Mathf.Max(adsLevelDelta, 0f));

            float callerMuffleHz = Mathf.Lerp(MuffleTransparentHz, MuffleMuffledHz,
                MuffleDepth(gainDelta));
            float vernMuffleHz = Mathf.Lerp(MuffleTransparentHz, MuffleMuffledHz,
                MuffleDepth(vernDelta));
            float adsMuffleHz = Mathf.Lerp(MuffleTransparentHz, MuffleMuffledHz,
                MuffleDepth(adsDelta));

            return new SoundboardEffectSettings(
                Mathf.Clamp(preset.LowPassHz + lowPassDelta * CallerLowPassSpanHz,
                    CallerLowPassMinHz, CallerLowPassMaxHz),
                Mathf.Clamp(preset.HighPassHz + highPassDelta * CallerHighPassSpanHz,
                    CallerHighPassMinHz, CallerHighPassMaxHz),
                Mathf.Clamp(preset.Distortion + callerTotalOver * CallerDriveSpan,
                    CallerDriveMin, CallerDriveMax),
                Mathf.Clamp(-gainBelow * CallerAttenuateSpanDb,
                    CallerAmplifyMinDb, CallerAmplifyMaxDb),
                callerMuffleHz,
                callerTotalOver,
                Mathf.Clamp(vernTotalOver * VernDriveSpan, 0f, 1f),
                vernTotalOver,
                vernMuffleHz,
                Mathf.Clamp(adsTotalOver * AdsDriveSpan, 0f, 1f),
                adsTotalOver,
                adsMuffleHz,
                Mathf.Clamp(callerLevelDelta * CallerLevelSpanDb,
                    CallerLevelMinDb, CallerLevelMaxDb),
                Mathf.Clamp(vernLevelDelta * VernLevelSpanDb,
                    VernLevelMinDb, VernLevelMaxDb),
                Mathf.Clamp(adsLevelDelta * AdsLevelSpanDb,
                    AdsLevelMinDb, AdsLevelMaxDb),
                Mathf.Clamp(faderDelta * MusicFaderSpanDb, MusicFaderMinDb, MusicFaderMaxDb),
                Mathf.Clamp(faderDelta * MasterFaderSpanDb, MasterFaderMinDb, MasterFaderMaxDb));
        }
    }
}
