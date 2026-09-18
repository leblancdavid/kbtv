using System;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Audio;

namespace KBTV.Tests.Unit.Audio
{
    public class SoundboardMixerDriverTests : KBTVTestClass
    {
        public SoundboardMixerDriverTests(Node testScene) : base(testScene) { }

        [Test]
        public void ComputeEffectSettings_NeutralState_KeepsEquipmentPreset()
        {
            var state = SoundboardKnobState.Neutral();
            var preset = new SoundboardPresetInfo(600f, 400f, 0.4f);

            var settings = SoundboardMixerDriver.ComputeEffectSettings(state, preset);

            AssertThat(Mathf.IsEqualApprox(settings.CallerLowPassHz, 600f));
            AssertThat(Mathf.IsEqualApprox(settings.CallerHighPassHz, 400f));
            AssertThat(Mathf.IsEqualApprox(settings.CallerDrive, 0.4f));
            AssertThat(Mathf.IsEqualApprox(settings.CallerAmplifyDb, 0f));
            AssertThat(Mathf.IsEqualApprox(
                settings.CallerMuffleHz, SoundboardMixerDriver.MuffleTransparentHz));
            AssertThat(Mathf.IsEqualApprox(settings.CallerCompression, 0f));
            AssertThat(Mathf.IsEqualApprox(settings.VernDrive, 0f));
            AssertThat(Mathf.IsEqualApprox(settings.VernCompression, 0f));
            AssertThat(Mathf.IsEqualApprox(
                settings.VernMuffleHz, SoundboardMixerDriver.MuffleTransparentHz));
            AssertThat(Mathf.IsEqualApprox(settings.AdsDrive, 0f));
            AssertThat(Mathf.IsEqualApprox(settings.AdsCompression, 0f));
            AssertThat(Mathf.IsEqualApprox(
                settings.AdsMuffleHz, SoundboardMixerDriver.MuffleTransparentHz));
            AssertThat(Mathf.IsEqualApprox(settings.CallerLevelDb, 0f));
            AssertThat(Mathf.IsEqualApprox(settings.VernLevelDb, 0f));
            AssertThat(Mathf.IsEqualApprox(settings.AdsLevelDb, 0f));
            AssertThat(Mathf.IsEqualApprox(settings.MusicFaderDb, 0f));
            AssertThat(Mathf.IsEqualApprox(settings.MasterFaderDb, 0f));
        }

[Test]
        public void ComputeEffectSettings_FullCallerGain_RealTrimLouderAndRougher()
        {
            var state = SoundboardKnobState.Neutral();
            state.CallerGain = 1f;
            var preset = new SoundboardPresetInfo(600f, 400f, 0.4f);

            var settings = SoundboardMixerDriver.ComputeEffectSettings(state, preset);

            // Real trim: full-up gain is audibly louder AND rougher, and the
            // informational compression field still reports the over-target depth.
            AssertThat(Mathf.IsEqualApprox(settings.CallerDrive, 0.85f));
            AssertThat(Mathf.IsEqualApprox(settings.CallerAmplifyDb, 10f));
            AssertThat(Mathf.IsEqualApprox(settings.CallerCompression, 1f));
            AssertThat(Mathf.IsEqualApprox(
                settings.CallerMuffleHz, SoundboardMixerDriver.MuffleTransparentHz));
        }

        [Test]
        public void ComputeEffectSettings_LowCallerGain_MufflesAndAttenuatesInsteadOfCutting()
        {
            var state = SoundboardKnobState.Neutral();
            state.CallerGain = 0f;
            var preset = new SoundboardPresetInfo(600f, 400f, 0.4f);

            var settings = SoundboardMixerDriver.ComputeEffectSettings(state, preset);

            AssertThat(Mathf.IsEqualApprox(settings.CallerAmplifyDb, -10f));
            AssertThat(Mathf.IsEqualApprox(
                settings.CallerMuffleHz, SoundboardMixerDriver.MuffleMuffledHz));
            AssertThat(settings.CallerMuffleHz < SoundboardMixerDriver.MuffleTransparentHz);
            AssertThat(Mathf.IsEqualApprox(settings.CallerDrive, preset.Distortion));
            AssertThat(Mathf.IsEqualApprox(settings.CallerCompression, 0f));
        }

[Test]
        public void ComputeEffectSettings_HighVernAndAdsGain_ScoredAsDriveAndCompression()
        {
            var state = SoundboardKnobState.Neutral();
            state.VernGain = 1f;
            state.AdsGain = 1f;
            var preset = new SoundboardPresetInfo(600f, 400f, 0.4f);

            var settings = SoundboardMixerDriver.ComputeEffectSettings(state, preset);

            AssertThat(Mathf.IsEqualApprox(settings.VernDrive, 0.65f));
            AssertThat(Mathf.IsEqualApprox(settings.VernCompression, 1f));
            AssertThat(Mathf.IsEqualApprox(settings.AdsDrive, 0.65f));
            AssertThat(Mathf.IsEqualApprox(settings.AdsCompression, 1f));
            AssertThat(Mathf.IsEqualApprox(
                settings.VernMuffleHz, SoundboardMixerDriver.MuffleTransparentHz));
            AssertThat(Mathf.IsEqualApprox(
                settings.AdsMuffleHz, SoundboardMixerDriver.MuffleTransparentHz));
        }

        [Test]
        public void ComputeEffectSettings_LowVernGain_MufflesVern()
        {
            var state = SoundboardKnobState.Neutral();
            state.VernGain = 0f;
            var preset = new SoundboardPresetInfo(600f, 400f, 0.4f);

            var settings = SoundboardMixerDriver.ComputeEffectSettings(state, preset);

            AssertThat(Mathf.IsEqualApprox(settings.VernDrive, 0f));
            AssertThat(Mathf.IsEqualApprox(settings.VernCompression, 0f));
            AssertThat(Mathf.IsEqualApprox(
                settings.VernMuffleHz, SoundboardMixerDriver.MuffleMuffledHz));
        }

[Test]
        public void ComputeEffectSettings_LevelFaders_MoveBusStripsBelowNeutral()
        {
            var state = SoundboardKnobState.Neutral();
            state.CallerLevel = 0f;
            state.VernLevel = 0f;
            state.AdsLevel = 0.5f;
            var preset = new SoundboardPresetInfo(600f, 400f, 0.4f);

            var settings = SoundboardMixerDriver.ComputeEffectSettings(state, preset);

            AssertThat(Mathf.IsEqualApprox(settings.CallerLevelDb, -18f));
            AssertThat(Mathf.IsEqualApprox(settings.VernLevelDb, -30f));
            AssertThat(Mathf.IsEqualApprox(settings.AdsLevelDb, 0f));
        }

        [Test]
        public void ComputeEffectSettings_LevelFadersAboveNeutral_ScoreCompressionNotVolume()
        {
            var state = SoundboardKnobState.Neutral();
            state.CallerLevel = 1f;
            state.VernLevel = 1f;
            var preset = new SoundboardPresetInfo(600f, 400f, 0.4f);

            var settings = SoundboardMixerDriver.ComputeEffectSettings(state, preset);

            AssertThat(Mathf.IsEqualApprox(settings.CallerLevelDb, 0f));
            AssertThat(Mathf.IsEqualApprox(settings.VernLevelDb, 0f));
            AssertThat(Mathf.IsEqualApprox(settings.CallerCompression, 1f));
            AssertThat(Mathf.IsEqualApprox(settings.VernCompression, 1f));
            AssertThat(Mathf.IsEqualApprox(settings.VernDrive, 0.65f));
        }

        [Test]
        public void ComputeEffectSettings_WithCallerTargets_GradesAgainstTarget()
        {
            var state = SoundboardKnobState.Neutral();
            state.CallerGain = 0.7f;
            var preset = new SoundboardPresetInfo(600f, 400f, 0.4f);
            var targets = new SoundboardCallerTargets(0.7f, 0.5f, 0.5f, 0.5f);

            var settings = SoundboardMixerDriver.ComputeEffectSettings(state, preset, targets);

            AssertThat(Mathf.IsEqualApprox(settings.CallerDrive, preset.Distortion));
            AssertThat(Mathf.IsEqualApprox(settings.CallerCompression, 0f));
            AssertThat(Mathf.IsEqualApprox(settings.CallerAmplifyDb, 0f));
            AssertThat(Mathf.IsEqualApprox(
                settings.CallerMuffleHz, SoundboardMixerDriver.MuffleTransparentHz));
        }

        [Test]
        public void ComputeEffectSettings_AboveCallerTarget_GetsLouderAndRougher()
        {
            var state = SoundboardKnobState.Neutral();
            state.CallerGain = 1f;
            var preset = new SoundboardPresetInfo(600f, 400f, 0.4f);
            var targets = new SoundboardCallerTargets(0.7f, 0.5f, 0.5f, 0.5f);

            var settings = SoundboardMixerDriver.ComputeEffectSettings(state, preset, targets);

            // Above the per-caller target the caller is audibly louder (+trim) and
            // rougher (drive); the compression field still reports the overshoot.
            AssertThat(settings.CallerCompression > 0f);
            AssertThat(settings.CallerAmplifyDb > 0f);
            AssertThat(settings.CallerDrive > preset.Distortion);
        }

        [Test]
        public void ComputeEffectSettings_CallerLevel_GradesAgainstCallerTargetVolume()
        {
            var state = SoundboardKnobState.Neutral();
            var preset = new SoundboardPresetInfo(600f, 400f, 0.4f);
            var targets = new SoundboardCallerTargets(0.5f, 0.5f, 0.5f, 0.7f);

            var settings = SoundboardMixerDriver.ComputeEffectSettings(state, preset, targets);

            // The resting fader (0.5) sits below the 0.7 volume target, so the
            // caller strip is pulled quiet - the fader no longer passes at rest.
            AssertThat(settings.CallerLevelDb < 0f);
            AssertThat(Mathf.IsEqualApprox(settings.CallerCompression, 0f));

            // Sliding the fader onto the target returns the strip to 0 dB.
            state.CallerLevel = 0.7f;
            var aligned = SoundboardMixerDriver.ComputeEffectSettings(state, preset, targets);

            AssertThat(Mathf.IsEqualApprox(aligned.CallerLevelDb, 0f));
            AssertThat(Mathf.IsEqualApprox(aligned.CallerCompression, 0f));
        }

        [Test]
        public void ComputeEffectSettings_KnobValuesClampedIntoSpan()
        {
            var state = SoundboardKnobState.Neutral();
            state.CallerLowPass = 0f;
            var preset = new SoundboardPresetInfo(600f, 400f, 0.4f);

            var settings = SoundboardMixerDriver.ComputeEffectSettings(state, preset);

            AssertThat(settings.CallerLowPassHz >= SoundboardMixerDriver.CallerLowPassMinHz);
        }

        /// <summary>Ads fader at the board default (0) cuts the SFX bus.</summary>
        [Test]
        public void ComputeEffectSettings_DefaultAdsFader_CutsSfxBus()
        {
            var state = SoundboardKnobState.Default();
            var preset = new SoundboardPresetInfo(600f, 400f, 0.4f);

            var settings = SoundboardMixerDriver.ComputeEffectSettings(state, preset);

            AssertThat(Mathf.IsEqualApprox(settings.AdsLevelDb, -30f));
            AssertThat(Mathf.IsEqualApprox(settings.CallerLevelDb, 0f));
            AssertThat(Mathf.IsEqualApprox(settings.VernLevelDb, 0f));
        }

        /// <summary>Default() places knobs at 0.5, Caller/Vern faders at 50%, Ads fader at the bottom.</summary>
        [Test]
        public void DefaultState_AdsFaderAtBottom()
        {
            var state = SoundboardKnobState.Default();

            AssertThat(Mathf.IsEqualApprox(state.AdsLevel, 0f));
            AssertThat(Mathf.IsEqualApprox(state.CallerGain, SoundboardKnobState.NeutralValue));
            AssertThat(Mathf.IsEqualApprox(state.CallerLevel, SoundboardKnobState.NeutralValue));
            AssertThat(Mathf.IsEqualApprox(state.VernLevel, SoundboardKnobState.NeutralValue));
            AssertThat(Mathf.IsEqualApprox(state.Fader, SoundboardKnobState.NeutralValue));
        }

        /// <summary>Neutral() keeps every control at 0.5 — pure DSP baseline.</summary>
        [Test]
        public void NeutralState_AllControlsCenter()
        {
            var state = SoundboardKnobState.Neutral();

            AssertThat(Mathf.IsEqualApprox(state.AdsLevel, SoundboardKnobState.NeutralValue));
            AssertThat(Mathf.IsEqualApprox(state.CallerGain, SoundboardKnobState.NeutralValue));
            AssertThat(Mathf.IsEqualApprox(state.CallerLevel, SoundboardKnobState.NeutralValue));
            AssertThat(Mathf.IsEqualApprox(state.Fader, SoundboardKnobState.NeutralValue));
        }

        /// <summary>ResetToDefault restores board resting values (AdsLevel 0, everything else 0.5).</summary>
        [Test]
        public void ResetToDefault_RestoresBoardDefaults()
        {
            var driver = new SoundboardMixerDriver();
            driver.State.CallerGain = 1f;
            driver.State.VernGain = 0f;
            driver.State.CallerLowPass = 1f;
            driver.State.CallerHighPass = 1f;
            driver.State.AdsGain = 1f;
            driver.State.CallerLevel = 1f;
            driver.State.VernLevel = 0f;
            driver.State.AdsLevel = 1f;
            driver.State.Fader = 0f;

            driver.ResetToDefault();

            AssertThat(Mathf.IsEqualApprox(driver.State.CallerGain, SoundboardKnobState.NeutralValue));
            AssertThat(Mathf.IsEqualApprox(driver.State.CallerLowPass, SoundboardKnobState.NeutralValue));
            AssertThat(Mathf.IsEqualApprox(driver.State.CallerHighPass, SoundboardKnobState.NeutralValue));
            AssertThat(Mathf.IsEqualApprox(driver.State.VernGain, SoundboardKnobState.NeutralValue));
            AssertThat(Mathf.IsEqualApprox(driver.State.AdsGain, SoundboardKnobState.NeutralValue));
            AssertThat(Mathf.IsEqualApprox(driver.State.CallerLevel, SoundboardKnobState.NeutralValue));
            AssertThat(Mathf.IsEqualApprox(driver.State.VernLevel, SoundboardKnobState.NeutralValue));
            AssertThat(Mathf.IsEqualApprox(driver.State.AdsLevel, 0f));
            AssertThat(Mathf.IsEqualApprox(driver.State.Fader, SoundboardKnobState.NeutralValue));
        }

        [Test]
        public void Drive_NullState_ReturnsNeutralSettings()
        {
            var settings = SoundboardMixerDriver.ComputeEffectSettings(null!, new SoundboardPresetInfo(600f, 400f, 0.4f));

            AssertThat(Mathf.IsEqualApprox(settings.MasterFaderDb, 0f));
        }
    }
}
