using Chickensoft.GoDotTest;
using Godot;
using KBTV.Audio;

namespace KBTV.Tests.Unit.Audio
{
    public class SoundboardTargetGeneratorTests : KBTVTestClass
    {
        public SoundboardTargetGeneratorTests(Node testScene) : base(testScene) { }

        [Test]
        public void GetCallerTargets_NeutralSpeakingVolume_CenterAtNeutral()
        {
            var targets = SoundboardTargetGenerator.GetCallerTargets(0.5f);

            AssertThat(Mathf.IsEqualApprox(targets.Gain, SoundboardKnobState.NeutralValue));
            AssertThat(Mathf.IsEqualApprox(targets.LowPass, SoundboardKnobState.NeutralValue));
            AssertThat(Mathf.IsEqualApprox(targets.HighPass, SoundboardKnobState.NeutralValue));
        }

        [Test]
        public void GetCallerTargets_LoudSpeakingVolume_JitteredAboveNeutral()
        {
            var targets = SoundboardTargetGenerator.GetCallerTargets(1.0f);

            AssertThat(targets.Gain > SoundboardKnobState.NeutralValue);
        }

        [Test]
        public void GetCallerBands_KnobsAtTarget_AllGreen()
        {
            var state = SoundboardKnobState.Neutral();

            var bands = SoundboardTargetGenerator.GetCallerBands(state, 0.5f);

            AssertThat(bands.Gain == SoundboardBand.Green);
            AssertThat(bands.LowPass == SoundboardBand.Green);
            AssertThat(bands.HighPass == SoundboardBand.Green);
        }

        [Test]
        public void GetCallerBands_SlightlyOffTarget_StillGreenWithinTolerance()
        {
            var state = SoundboardKnobState.Neutral();
            var targets = SoundboardTargetGenerator.GetCallerTargets(0.5f);
            var within = targets.Gain + SoundboardTargetGenerator.PerfectTolerance * 0.5f;
            state.CallerGain = within;

            var bands = SoundboardTargetGenerator.GetCallerBands(state, 0.5f);

            AssertThat(bands.Gain == SoundboardBand.Green);
        }

        [Test]
        public void GetControlBand_CallerControls_DelegatesToCallerBands()
        {
            var state = SoundboardKnobState.Neutral();
            state.CallerGain = 0f;
            state.AdsLevel = 0.9f;

            var expected = SoundboardTargetGenerator.GetCallerBands(state, 1.0f);
            AssertThat(
                SoundboardTargetGenerator.GetControlBand(state, SoundboardControl.CallerGain, 1.0f) == expected.Gain);
            AssertThat(
                SoundboardTargetGenerator.GetControlBand(state, SoundboardControl.CallerLowPass, 1.0f) == expected.LowPass);
            AssertThat(
                SoundboardTargetGenerator.GetControlBand(state, SoundboardControl.CallerHighPass, 1.0f) == expected.HighPass);
        }

        [Test]
        public void GetControlBand_LevelsAndMaster_ScaleFromNeutral()
        {
            var state = SoundboardKnobState.Neutral();
            state.VernLevel = 1f;
            state.Fader = 0f;

            AssertThat(
                SoundboardTargetGenerator.GetControlBand(state, SoundboardControl.VernLevel, 0.5f) != SoundboardBand.Green);
            AssertThat(
                SoundboardTargetGenerator.GetControlBand(state, SoundboardControl.Master, 0.5f) != SoundboardBand.Green);
            AssertThat(
                SoundboardTargetGenerator.GetControlBand(state, SoundboardControl.None, 0.5f) == SoundboardBand.None);
        }

        [Test]
        public void GetControlBand_NullState_ReturnsNone()
        {
            AssertThat(SoundboardTargetGenerator.GetControlBand(null!, SoundboardControl.Master, 0.5f) == SoundboardBand.None);
        }

        [Test]
        public void PerfectTolerance_AllowsWiggleroom()
        {
            AssertThat(SoundboardTargetGenerator.PerfectTolerance == 0.09f);
        }

        [Test]
        public void GetBand_WayOutsideTarget_Red()
        {
            var band = SoundboardTargetGenerator.GetBand(0.1f, 0.5f);

            AssertThat(band == SoundboardBand.Red);
        }

        [Test]
        public void GetWorstBand_PicksHighestSeverity()
        {
            var worst = SoundboardTargetGenerator.GetWorstBand(SoundboardBand.Green, SoundboardBand.Blue, SoundboardBand.Red);

            AssertThat(worst == SoundboardBand.Red);
        }

        [Test]
        public void IsOffPerfect_GreenIsPerfect_EverythingElseOff()
        {
            AssertThat(!SoundboardTargetGenerator.IsOffPerfect(SoundboardBand.Green));
            AssertThat(SoundboardTargetGenerator.IsOffPerfect(SoundboardBand.Blue));
            AssertThat(SoundboardTargetGenerator.IsOffPerfect(SoundboardBand.Yellow));
            AssertThat(SoundboardTargetGenerator.IsOffPerfect(SoundboardBand.Red));
            AssertThat(SoundboardTargetGenerator.IsOffPerfect(SoundboardBand.None));
        }

        [Test]
        public void GetCallerEffectiveLevel_ClampsToEquipmentRange()
        {
            AssertThat(SoundboardTargetGenerator.GetCallerEffectiveLevel(4, 2) == 4);
            AssertThat(SoundboardTargetGenerator.GetCallerEffectiveLevel(1, -3) == 1);
            AssertThat(SoundboardTargetGenerator.GetCallerEffectiveLevel(3, -1) == 2);
        }
    }
}