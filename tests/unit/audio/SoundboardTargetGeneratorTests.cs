using Chickensoft.GoDotTest;
using Godot;
using KBTV.Audio;

namespace KBTV.Tests.Unit.Audio
{
    public class SoundboardTargetGeneratorTests : KBTVTestClass
    {
        public SoundboardTargetGeneratorTests(Node testScene) : base(testScene) { }

        [Test]
        public void NeutralCallerTargets_AllKnobsNeutral()
        {
            var targets = SoundboardTargetGenerator.NeutralCallerTargets();

            AssertThat(Mathf.IsEqualApprox(targets.Gain, SoundboardKnobState.NeutralValue));
            AssertThat(Mathf.IsEqualApprox(targets.LowPass, SoundboardKnobState.NeutralValue));
            AssertThat(Mathf.IsEqualApprox(targets.HighPass, SoundboardKnobState.NeutralValue));
        }

        [Test]
        public void GetCallerTargets_DifferentKnobs_DifferPerKnob()
        {
            var targets = SoundboardTargetGenerator.GetCallerTargets(0.5f, 0);

            AssertThat(!Mathf.IsEqualApprox(targets.Gain, targets.LowPass));
            AssertThat(!Mathf.IsEqualApprox(targets.LowPass, targets.HighPass));
        }

        [Test]
        public void GetCallerTargets_AnySeed_StaysInReachableRange()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var targets = SoundboardTargetGenerator.GetCallerTargets(0.5f, seed);
                AssertThat(targets.Gain >= SoundboardTargetGenerator.MinTargetKnob);
                AssertThat(targets.Gain <= SoundboardTargetGenerator.MaxTargetKnob);
                AssertThat(targets.LowPass >= SoundboardTargetGenerator.MinTargetKnob);
                AssertThat(targets.LowPass <= SoundboardTargetGenerator.MaxTargetKnob);
                AssertThat(targets.HighPass >= SoundboardTargetGenerator.MinTargetKnob);
                AssertThat(targets.HighPass <= SoundboardTargetGenerator.MaxTargetKnob);
            }
        }

        [Test]
        public void GetCallerTargets_SameSeed_IsDeterministic()
        {
            var first = SoundboardTargetGenerator.GetCallerTargets(0.3f, 42);
            var second = SoundboardTargetGenerator.GetCallerTargets(0.3f, 42);

            AssertThat(Mathf.IsEqualApprox(first.Gain, second.Gain));
            AssertThat(Mathf.IsEqualApprox(first.LowPass, second.LowPass));
            AssertThat(Mathf.IsEqualApprox(first.HighPass, second.HighPass));
        }

        [Test]
        public void GetCallerTargets_LoudVolume_RaisesCenterAboveQuiet()
        {
            float Sum(SoundboardCallerTargets t) => t.Gain + t.LowPass + t.HighPass;

            var loud = SoundboardTargetGenerator.GetCallerTargets(1.0f, 0);
            var quiet = SoundboardTargetGenerator.GetCallerTargets(0f, 0);

            AssertThat(Sum(loud) > Sum(quiet));
        }

        [Test]
        public void HashUnit_DeterministicBoundedAndSeedSensitive()
        {
            uint salt = 0x475F6911u;
            var a = SoundboardTargetGenerator.HashUnit(0, salt);
            var b = SoundboardTargetGenerator.HashUnit(1, salt);

            AssertThat(a >= 0f && a < 1f);
            AssertThat(b >= 0f && b < 1f);
            AssertThat(!Mathf.IsEqualApprox(a, b));
            AssertThat(Mathf.IsEqualApprox(
                a, SoundboardTargetGenerator.HashUnit(0, salt)));
        }

        [Test]
        public void GetCallerBands_KnobsAtTarget_AllGreen()
        {
            var state = SoundboardKnobState.Neutral();
            var targets = SoundboardTargetGenerator.GetCallerTargets(0.5f, 7);
            state.CallerGain = targets.Gain;
            state.CallerLowPass = targets.LowPass;
            state.CallerHighPass = targets.HighPass;

            var bands = SoundboardTargetGenerator.GetCallerBands(state, 0.5f, 7);

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
        public void GetBand_AtTarget_Green()
        {
            AssertThat(SoundboardTargetGenerator.GetBand(0.5f, 0.5f) == SoundboardBand.Green);
        }

        [Test]
        public void GetBand_FarBelowTarget_Blue()
        {
            var band = SoundboardTargetGenerator.GetBand(0.1f, 0.5f);

            AssertThat(band == SoundboardBand.Blue);
        }

        [Test]
        public void GetBand_JustBelowTarget_Cyan()
        {
            var band = SoundboardTargetGenerator.GetBand(0.4f, 0.5f);

            AssertThat(band == SoundboardBand.Cyan);
        }

        [Test]
        public void GetBand_JustAboveTarget_Yellow()
        {
            var band = SoundboardTargetGenerator.GetBand(0.6f, 0.5f);

            AssertThat(band == SoundboardBand.Yellow);
        }

        [Test]
        public void GetBand_FarAboveTarget_Red()
        {
            var band = SoundboardTargetGenerator.GetBand(0.9f, 0.5f);

            AssertThat(band == SoundboardBand.Red);
        }

        [Test]
        public void GetBand_WithinPerfectTolerance_EitherSideIsGreen()
        {
            AssertThat(SoundboardTargetGenerator.GetBand(0.5f + SoundboardTargetGenerator.PerfectTolerance * 0.5f, 0.5f) == SoundboardBand.Green);
            AssertThat(SoundboardTargetGenerator.GetBand(0.5f - SoundboardTargetGenerator.PerfectTolerance * 0.5f, 0.5f) == SoundboardBand.Green);
        }

        [Test]
        public void GetControlError_NeutralStateAndTarget_Zero()
        {
            var state = SoundboardKnobState.Neutral();
            var targets = SoundboardTargetGenerator.GetCallerTargets(0.5f, 3);
            state.CallerGain = targets.Gain;

            AssertThat(Mathf.Abs(SoundboardTargetGenerator.GetControlError(state, SoundboardControl.CallerGain, 0.5f, 3)) < 0.0001f);
            AssertThat(Mathf.Abs(SoundboardTargetGenerator.GetControlError(state, SoundboardControl.Master, 0.5f)) < 0.0001f);
            AssertThat(Mathf.Abs(SoundboardTargetGenerator.GetControlError(state, SoundboardControl.VernLevel, 0.5f)) < 0.0001f);
        }

        [Test]
        public void GetControlError_SignedAboveTarget_Positive()
        {
            var state = SoundboardKnobState.Neutral();
            state.CallerGain = 0.9f;
            state.VernLevel = 0.2f;

            AssertThat(SoundboardTargetGenerator.GetControlError(state, SoundboardControl.CallerGain, 0.5f) > 0f);
            AssertThat(SoundboardTargetGenerator.GetControlError(state, SoundboardControl.VernLevel, 0.5f) < 0f);
        }

        [Test]
        public void GetControlError_NullState_Zero()
        {
            AssertThat(SoundboardTargetGenerator.GetControlError(null!, SoundboardControl.Master, 0.5f) == 0f);
        }

        [Test]
        public void ColorForError_AtTarget_Green()
        {
            AssertThat(SoundboardTargetGenerator.ColorForError(0f).IsEqualApprox(SoundboardTargetGenerator.RampGreen));
        }

        [Test]
        public void ColorForError_FarBelow_ClampsToBlue()
        {
            var color = SoundboardTargetGenerator.ColorForError(-1f);

            AssertThat(color.IsEqualApprox(SoundboardTargetGenerator.RampBlue));
        }

        [Test]
        public void ColorForError_HalfSpanBelow_Cyan()
        {
            var color = SoundboardTargetGenerator.ColorForError(-SoundboardTargetGenerator.ColorRampHalfSpan * 0.5f);

            AssertThat(color.IsEqualApprox(SoundboardTargetGenerator.RampCyan));
        }

        [Test]
        public void ColorForError_HalfSpanAbove_Yellow()
        {
            var color = SoundboardTargetGenerator.ColorForError(SoundboardTargetGenerator.ColorRampHalfSpan * 0.5f);

            AssertThat(color.IsEqualApprox(SoundboardTargetGenerator.RampYellow));
        }

        [Test]
        public void ColorForError_FarAbove_ClampsToRed()
        {
            var color = SoundboardTargetGenerator.ColorForError(1f);

            AssertThat(color.IsEqualApprox(SoundboardTargetGenerator.RampRed));
        }

        [Test]
        public void GetWorstBand_PicksHighestSeverity()
        {
            var worst = SoundboardTargetGenerator.GetWorstBand(SoundboardBand.Green, SoundboardBand.Blue, SoundboardBand.Red);

            AssertThat(worst == SoundboardBand.Red);
        }

        [Test]
        public void GetWorstBand_BlueBeatsCyanAndYellowAndGreen()
        {
            AssertThat(SoundboardTargetGenerator.GetWorstBand(SoundboardBand.Green, SoundboardBand.Cyan, SoundboardBand.Blue) == SoundboardBand.Blue);
            AssertThat(SoundboardTargetGenerator.GetWorstBand(SoundboardBand.Green, SoundboardBand.Yellow, SoundboardBand.Blue) == SoundboardBand.Blue);
        }

        [Test]
        public void GetWorstBand_TieBetweenCyanAndYellow_YellowWins()
        {
            AssertThat(SoundboardTargetGenerator.GetWorstBand(SoundboardBand.Cyan, SoundboardBand.Yellow) == SoundboardBand.Yellow);
        }

        [Test]
        public void GetWorstBand_NoneAndGreen_Green()
        {
            AssertThat(SoundboardTargetGenerator.GetWorstBand(SoundboardBand.None, SoundboardBand.Green) == SoundboardBand.Green);
        }

        [Test]
        public void IsOffPerfect_GreenIsPerfect_EverythingElseOff()
        {
            AssertThat(!SoundboardTargetGenerator.IsOffPerfect(SoundboardBand.Green));
            AssertThat(SoundboardTargetGenerator.IsOffPerfect(SoundboardBand.Blue));
            AssertThat(SoundboardTargetGenerator.IsOffPerfect(SoundboardBand.Cyan));
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