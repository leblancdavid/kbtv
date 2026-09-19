using System;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Audio;

namespace KBTV.Tests.Unit.Audio
{
    public class SoundboardControlApplierTests : KBTVTestClass
    {
        public SoundboardControlApplierTests(Node testScene) : base(testScene) { }

        [Test]
        public void Apply_WritesValueIntoMatchingField()
        {
            var state = SoundboardKnobState.Neutral();

            SoundboardControlApplier.Apply(state, SoundboardControl.CallerGain, 0.9f);
            SoundboardControlApplier.Apply(state, SoundboardControl.CallerLowPass, 0.3f);
            SoundboardControlApplier.Apply(state, SoundboardControl.CallerHighPass, 0.6f);
            SoundboardControlApplier.Apply(state, SoundboardControl.VernGain, 0.1f);
            SoundboardControlApplier.Apply(state, SoundboardControl.AdsGain, 0.8f);
            SoundboardControlApplier.Apply(state, SoundboardControl.CallerLevel, 0.55f);
            SoundboardControlApplier.Apply(state, SoundboardControl.VernLevel, 0.65f);
            SoundboardControlApplier.Apply(state, SoundboardControl.AdsLevel, 0.45f);
            SoundboardControlApplier.Apply(state, SoundboardControl.MasterLeft, 0.25f);

            AssertThat(Mathf.IsEqualApprox(state.CallerGain, 0.9f));
            AssertThat(Mathf.IsEqualApprox(state.CallerLowPass, 0.3f));
            AssertThat(Mathf.IsEqualApprox(state.CallerHighPass, 0.6f));
            AssertThat(Mathf.IsEqualApprox(state.VernGain, 0.1f));
            AssertThat(Mathf.IsEqualApprox(state.AdsGain, 0.8f));
            AssertThat(Mathf.IsEqualApprox(state.CallerLevel, 0.55f));
            AssertThat(Mathf.IsEqualApprox(state.VernLevel, 0.65f));
            AssertThat(Mathf.IsEqualApprox(state.AdsLevel, 0.45f));
            AssertThat(Mathf.IsEqualApprox(state.Fader, 0.25f));
        }

        [Test]
        public void Apply_NoneAndNullArg_AreNoOps()
        {
            var state = SoundboardKnobState.Neutral();

            SoundboardControlApplier.Apply(state, SoundboardControl.None, 0.2f);
            SoundboardControlApplier.Apply(null!, SoundboardControl.MasterLeft, 0.2f);

            AssertThat(Mathf.IsEqualApprox(state.Fader, SoundboardKnobState.NeutralValue));
        }

        [Test]
        public void Apply_ClampsOutOfRangeValues()
        {
            var state = SoundboardKnobState.Neutral();

            SoundboardControlApplier.Apply(state, SoundboardControl.VernGain, 1.7f);
            SoundboardControlApplier.Apply(state, SoundboardControl.MasterLeft, -0.3f);

            AssertThat(Mathf.IsEqualApprox(state.VernGain, 1f));
            AssertThat(Mathf.IsEqualApprox(state.Fader, 0f));
        }

        [Test]
        public void MasterPair_IsLinkedThroughOneFaderValue()
        {
            var state = SoundboardKnobState.Neutral();

            SoundboardControlApplier.Apply(state, SoundboardControl.MasterLeft, 0.7f);
            AssertThat(Mathf.IsEqualApprox(
                SoundboardControlApplier.CurrentValue(state, SoundboardControl.MasterRight), 0.7f));

            SoundboardControlApplier.Apply(state, SoundboardControl.MasterRight, 0.2f);
            AssertThat(Mathf.IsEqualApprox(
                SoundboardControlApplier.CurrentValue(state, SoundboardControl.MasterLeft), 0.2f));
        }

        [Test]
        public void ValueFromDrag_DragUp_IncreasesValue()
        {
            var result = SoundboardControlApplier.ValueFromDrag(0.5f, -110f, 220f);

            AssertThat(Mathf.IsEqualApprox(result, 1f));
        }

        [Test]
        public void ValueFromDrag_DragDown_DecreasesValue()
        {
            var result = SoundboardControlApplier.ValueFromDrag(0.5f, 110f, 220f);

            AssertThat(Mathf.IsEqualApprox(result, 0f));
        }

        [Test]
        public void ValueFromDrag_ClampsBeyondRange()
        {
            AssertThat(Mathf.IsEqualApprox(SoundboardControlApplier.ValueFromDrag(0.9f, -500f, 220f), 1f));
            AssertThat(Mathf.IsEqualApprox(SoundboardControlApplier.ValueFromDrag(0.1f, 500f, 220f), 0f));
        }

        [Test]
        public void ValueFromDrag_ZeroPixelsPerUnit_FallsBackToUnitStep()
        {
            var result = SoundboardControlApplier.ValueFromDrag(0.5f, -100f, 0f);

            AssertThat(Mathf.IsEqualApprox(result, 1f));
        }

        [Test]
        public void CurrentValue_ReadsBackEachControl()
        {
            var state = SoundboardKnobState.Neutral();
            state.CallerGain = 0.7f;
            state.CallerLowPass = 0.7f;
            state.CallerHighPass = 0.7f;
            state.VernGain = 0.2f;
            state.AdsGain = 0.9f;
            state.CallerLevel = 0.75f;
            state.VernLevel = 0.35f;
            state.AdsLevel = 0.85f;
            state.Fader = 0.1f;

            AssertThat(Mathf.IsEqualApprox(SoundboardControlApplier.CurrentValue(state, SoundboardControl.CallerGain), 0.7f));
            AssertThat(Mathf.IsEqualApprox(SoundboardControlApplier.CurrentValue(state, SoundboardControl.CallerLowPass), 0.7f));
            AssertThat(Mathf.IsEqualApprox(SoundboardControlApplier.CurrentValue(state, SoundboardControl.CallerHighPass), 0.7f));
            AssertThat(Mathf.IsEqualApprox(SoundboardControlApplier.CurrentValue(state, SoundboardControl.VernGain), 0.2f));
            AssertThat(Mathf.IsEqualApprox(SoundboardControlApplier.CurrentValue(state, SoundboardControl.AdsGain), 0.9f));
            AssertThat(Mathf.IsEqualApprox(SoundboardControlApplier.CurrentValue(state, SoundboardControl.CallerLevel), 0.75f));
            AssertThat(Mathf.IsEqualApprox(SoundboardControlApplier.CurrentValue(state, SoundboardControl.VernLevel), 0.35f));
            AssertThat(Mathf.IsEqualApprox(SoundboardControlApplier.CurrentValue(state, SoundboardControl.AdsLevel), 0.85f));
            AssertThat(Mathf.IsEqualApprox(SoundboardControlApplier.CurrentValue(state, SoundboardControl.MasterLeft), 0.1f));
            AssertThat(Mathf.IsEqualApprox(
                SoundboardControlApplier.CurrentValue(state, SoundboardControl.None), SoundboardKnobState.NeutralValue));
        }
    }
}