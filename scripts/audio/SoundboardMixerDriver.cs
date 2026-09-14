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
    /// never go fully silent or DC.
    /// </summary>
    public readonly struct SoundboardEffectSettings
    {
        public readonly float CallerLowPassHz;
        public readonly float CallerHighPassHz;
        public readonly float CallerDrive;
        public readonly float CallerAmplifyDb;
        public readonly float VernGainDb;
        public readonly float AdsGainDb;
        public readonly float MusicFaderDb;
        public readonly float MasterFaderDb;

        public SoundboardEffectSettings(
            float callerLowPassHz, float callerHighPassHz, float callerDrive,
            float callerAmplifyDb, float vernGainDb, float adsGainDb,
            float musicFaderDb, float masterFaderDb)
        {
            CallerLowPassHz = callerLowPassHz;
            CallerHighPassHz = callerHighPassHz;
            CallerDrive = callerDrive;
            CallerAmplifyDb = callerAmplifyDb;
            VernGainDb = vernGainDb;
            AdsGainDb = adsGainDb;
            MusicFaderDb = musicFaderDb;
            MasterFaderDb = masterFaderDb;
        }

        public static readonly SoundboardEffectSettings Neutral =
            new SoundboardEffectSettings(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
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
        public const float CallerLowPassSpanHz = 1000f;
        public const float CallerLowPassMinHz = 150f;
        public const float CallerLowPassMaxHz = 10000f;
        public const float CallerHighPassSpanHz = 400f;
        public const float CallerHighPassMinHz = 40f;
        public const float CallerHighPassMaxHz = 1500f;
        public const float CallerDriveSpan = 0.15f;
        public const float CallerDriveMin = 0.05f;
        public const float CallerDriveMax = 0.8f;
        public const float CallerAmplifyBaseDb = 8f;
        public const float CallerAmplifySpanDb = 6f;
        public const float CallerAmplifyMinDb = 0f;
        public const float CallerAmplifyMaxDb = 14f;
        public const float VernGainSpanDb = 8f;
        public const float VernGainMinDb = -12f;
        public const float VernGainMaxDb = 12f;
        public const float AdsGainSpanDb = 8f;
        public const float AdsGainMinDb = -24f;
        public const float AdsGainMaxDb = 12f;
        public const float MusicFaderSpanDb = 8f;
        public const float MusicFaderMinDb = -24f;
        public const float MusicFaderMaxDb = 8f;
        public const float MasterFaderSpanDb = 4f;
        public const float MasterFaderMinDb = -12f;
        public const float MasterFaderMaxDb = 4f;

        private AudioMixerManager? _mixer;

        public SoundboardMixerDriver(AudioMixerManager? mixer = null)
        {
            _mixer = mixer;
        }

        /// <summary>Current knob positions (neutral by default).</summary>
        public SoundboardKnobState State { get; } = SoundboardKnobState.Neutral();

        public bool HasMixer => _mixer != null;

        public void Attach(AudioMixerManager? mixer) => _mixer = mixer;

        public void Detach() => _mixer = null;

        /// <summary>Pushes the current knob state into the audio engine (no-op without a mixer).</summary>
        public void Apply() => _mixer?.ApplySoundboard(State);

        /// <summary>Returns every knob to the neutral center and applies the result.</summary>
        public void ResetToNeutral()
        {
            State.ResetToNeutral();
            Apply();
        }

        /// <summary>
        /// Pure knob->effect mapping: each target = equipment preset value + knob
        /// delta, fully clamped. Unit-testable without an AudioServer.
        /// </summary>
        public static SoundboardEffectSettings ComputeEffectSettings(
            SoundboardKnobState state, SoundboardPresetInfo preset)
        {
            if (state == null)
            {
                return SoundboardEffectSettings.Neutral;
            }

            float lowPassDelta = SoundboardKnobState.NormalizedDelta(state.CallerLowPass);
            float highPassDelta = SoundboardKnobState.NormalizedDelta(state.CallerHighPass);
            float gainDelta = SoundboardKnobState.NormalizedDelta(state.CallerGain);
            float vernDelta = SoundboardKnobState.NormalizedDelta(state.VernGain);
            float adsDelta = SoundboardKnobState.NormalizedDelta(state.AdsGain);
            float faderDelta = SoundboardKnobState.NormalizedDelta(state.Fader);

            return new SoundboardEffectSettings(
                Mathf.Clamp(preset.LowPassHz + lowPassDelta * CallerLowPassSpanHz,
                    CallerLowPassMinHz, CallerLowPassMaxHz),
                Mathf.Clamp(preset.HighPassHz + highPassDelta * CallerHighPassSpanHz,
                    CallerHighPassMinHz, CallerHighPassMaxHz),
                Mathf.Clamp(preset.Distortion + gainDelta * CallerDriveSpan,
                    CallerDriveMin, CallerDriveMax),
                Mathf.Clamp(CallerAmplifyBaseDb + gainDelta * CallerAmplifySpanDb,
                    CallerAmplifyMinDb, CallerAmplifyMaxDb),
                Mathf.Clamp(vernDelta * VernGainSpanDb, VernGainMinDb, VernGainMaxDb),
                Mathf.Clamp(adsDelta * AdsGainSpanDb, AdsGainMinDb, AdsGainMaxDb),
                Mathf.Clamp(faderDelta * MusicFaderSpanDb, MusicFaderMinDb, MusicFaderMaxDb),
                Mathf.Clamp(faderDelta * MasterFaderSpanDb, MasterFaderMinDb, MasterFaderMaxDb));
        }
    }
}