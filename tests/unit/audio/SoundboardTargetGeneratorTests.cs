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