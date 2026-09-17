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
            AssertThat(Mathf.IsEqualApprox(settings.VernGainDb, 0f));
            AssertThat(Mathf.IsEqualApprox(
                settings.VernMuffleHz, SoundboardMixerDriver.MuffleTransparentHz));
            AssertThat(Mathf.IsEqualApprox(settings.AdsGainDb, 0f));
            AssertThat(Mathf.IsEqualApprox(
                settings.AdsMuffleHz, SoundboardMixerDriver.MuffleTransparentHz));
            AssertThat(Mathf.IsEqualApprox(settings.CallerLevelDb, 0f));
            AssertThat(Mathf.IsEqualApprox(settings.VernLevelDb, 0f));
            AssertThat(Mathf.IsEqualApprox(settings.AdsLevelDb, 0f));
            AssertThat(Mathf.IsEqualApprox(settings.MusicFaderDb, 0f));
            AssertThat(Mathf.IsEqualApprox(settings.MasterFaderDb, 0f));
        }

        [Test]
        public void ComputeEffectSettings_FullCallerGain_RaisesDriveAndAmplify()
        {
            var state = SoundboardKnobState.Neutral();
            state.CallerGain = 1f;
            var preset = new SoundboardPresetInfo(600f, 400f, 0.4f);

            var settings = SoundboardMixerDriver.ComputeEffectSettings(state, preset);

            AssertThat(Mathf.IsEqualApprox(settings.CallerDrive, 0.75f));
            AssertThat(Mathf.IsEqualApprox(settings.CallerAmplifyDb, 18f));
            AssertThat(Mathf.IsEqualApprox(
                settings.CallerMuffleHz, SoundboardMixerDriver.MuffleTransparentHz));
        }

        [Test]
        public void ComputeEffectSettings_LowCallerGain_MufflesInsteadOfCutting()
        {
            var state = SoundboardKnobState.Neutral();
            state.CallerGain = 0f;
            var preset = new SoundboardPresetInfo(600f, 400f, 0.4f);

            var settings = SoundboardMixerDriver.ComputeEffectSettings(state, preset);

            AssertThat(Mathf.IsEqualApprox(settings.CallerAmplifyDb, 0f));
            AssertThat(Mathf.IsEqualApprox(
                settings.CallerMuffleHz, SoundboardMixerDriver.MuffleMuffledHz));
            AssertThat(settings.CallerMuffleHz < SoundboardMixerDriver.MuffleTransparentHz);
        }

        [Test]
        public void ComputeEffectSettings_HighVernAndAdsGain_BoostTheirBuses()
        {
            var state = SoundboardKnobState.Neutral();
            state.VernGain = 1f;
            state.AdsGain = 1f;
            var preset = new SoundboardPresetInfo(600f, 400f, 0.4f);

            var settings = SoundboardMixerDriver.ComputeEffectSettings(state, preset);

            AssertThat(Mathf.IsEqualApprox(settings.VernGainDb, 16f));
            AssertThat(Mathf.IsEqualApprox(settings.AdsGainDb, 16f));
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

            AssertThat(Mathf.IsEqualApprox(settings.VernGainDb, 0f));
            AssertThat(Mathf.IsEqualApprox(
                settings.VernMuffleHz, SoundboardMixerDriver.MuffleMuffledHz));
        }

        [Test]
        public void ComputeEffectSettings_LevelFaders_MoveBusStrips()
        {
            var state = SoundboardKnobState.Neutral();
            state.CallerLevel = 1f;
            state.VernLevel = 0f;
            state.AdsLevel = 0.5f;
            var preset = new SoundboardPresetInfo(600f, 400f, 0.4f);

            var settings = SoundboardMixerDriver.ComputeEffectSettings(state, preset);

            AssertThat(Mathf.IsEqualApprox(settings.CallerLevelDb, 14f));
            AssertThat(Mathf.IsEqualApprox(settings.VernLevelDb, -14f));
            AssertThat(Mathf.IsEqualApprox(settings.AdsLevelDb, 0f));
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

        [Test]
        public void ResetToNeutral_RestoresEveryKnob()
        {
            var driver = new SoundboardMixerDriver();
            driver.State.CallerGain = 1f;
            driver.State.VernGain = 0f;
            driver.State.CallerLevel = 1f;

            driver.ResetToNeutral();

            AssertThat(Mathf.IsEqualApprox(driver.State.CallerGain, SoundboardKnobState.NeutralValue));
            AssertThat(Mathf.IsEqualApprox(driver.State.VernGain, SoundboardKnobState.NeutralValue));
            AssertThat(Mathf.IsEqualApprox(driver.State.CallerLevel, SoundboardKnobState.NeutralValue));
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
