using Chickensoft.GoDotTest;
using Godot;
using KBTV.Audio;

namespace KBTV.Tests.Unit.Audio
{
    public class SoundboardGlowTests : KBTVTestClass
    {
        public SoundboardGlowTests(Node testScene) : base(testScene) { }

        [Test]
        public void ChooseSpeakingChannel_BothSilent_None()
        {
            AssertThat(SoundboardGlow.ChooseSpeakingChannel(-80f, -80f) == SoundboardGlow.SpeakingChannel.None);
            AssertThat(SoundboardGlow.ChooseSpeakingChannel(-60f, -80f) == SoundboardGlow.SpeakingChannel.None);
        }

        [Test]
        public void ChooseSpeakingChannel_OnlyCallerAudible_Caller()
        {
            AssertThat(SoundboardGlow.ChooseSpeakingChannel(-20f, -80f) == SoundboardGlow.SpeakingChannel.Caller);
        }

        [Test]
        public void ChooseSpeakingChannel_OnlyVernAudible_Vern()
        {
            AssertThat(SoundboardGlow.ChooseSpeakingChannel(-80f, -25f) == SoundboardGlow.SpeakingChannel.Vern);
        }

        [Test]
        public void ChooseSpeakingChannel_LouderChannelWins()
        {
            AssertThat(SoundboardGlow.ChooseSpeakingChannel(-10f, -25f) == SoundboardGlow.SpeakingChannel.Caller);
            AssertThat(SoundboardGlow.ChooseSpeakingChannel(-30f, -12f) == SoundboardGlow.SpeakingChannel.Vern);
        }

        [Test]
        public void ChooseSpeakingChannel_WithinHysteresis_None()
        {
            // Both audible but within 3 dB of each other: ambiguous, so no color boost.
            AssertThat(SoundboardGlow.ChooseSpeakingChannel(-20f, -21.5f) == SoundboardGlow.SpeakingChannel.None);
        }

        [Test]
        public void ChooseSpeakingChannel_ExactAudibleTie_None()
        {
            AssertThat(SoundboardGlow.ChooseSpeakingChannel(-20f, -20f) == SoundboardGlow.SpeakingChannel.None);
        }

        [Test]
        public void GlowFromPeakDb_LoudPeak_FullGlow()
        {
            AssertThat(Mathf.IsEqualApprox(SoundboardGlow.GlowFromPeakDb(SoundboardGlow.LoudPeakDb), 1f));
        }

        [Test]
        public void GlowFromPeakDb_SilenceFloor_NoGlow()
        {
            AssertThat(Mathf.IsEqualApprox(SoundboardGlow.GlowFromPeakDb(SoundboardGlow.SilenceFloorDb), 0f));
        }

        [Test]
        public void GlowFromPeakDb_Midway_ClampedLinear()
        {
            // -29 dB is exactly halfway between the -50 floor and the -8 loud peak.
            float mid = (SoundboardGlow.SilenceFloorDb + SoundboardGlow.LoudPeakDb) / 2f;
            AssertThat(Mathf.IsEqualApprox(SoundboardGlow.GlowFromPeakDb(mid), 0.5f));
            AssertThat(Mathf.IsEqualApprox(SoundboardGlow.GlowFromPeakDb(-80f), 0f));
            AssertThat(Mathf.IsEqualApprox(SoundboardGlow.GlowFromPeakDb(-5f), 1f));
        }

        [Test]
        public void SizeScaleFromGlow_FullGlow_MaxSize()
        {
            AssertThat(Mathf.IsEqualApprox(SoundboardGlow.SizeScaleFromGlow(1f), 1f));
        }

        [Test]
        public void SizeScaleFromGlow_Silent_MinRingFraction()
        {
            // Silence keeps a small-but-visible idle ring, never disappears.
            AssertThat(Mathf.IsEqualApprox(SoundboardGlow.SizeScaleFromGlow(0f), SoundboardGlow.MinRingFraction));
        }

        [Test]
        public void SizeScaleFromGlow_Midway_ClampedLinear()
        {
            // Half glow scales to halfway between the idle ring and full size.
            float mid = (SoundboardGlow.MinRingFraction + 1f) / 2f;
            AssertThat(Mathf.IsEqualApprox(SoundboardGlow.SizeScaleFromGlow(0.5f), mid));
            AssertThat(Mathf.IsEqualApprox(SoundboardGlow.SizeScaleFromGlow(-1f), SoundboardGlow.MinRingFraction));
            AssertThat(Mathf.IsEqualApprox(SoundboardGlow.SizeScaleFromGlow(2f), 1f));
        }

        [Test]
        public void ChannelOf_CallerControls_MapToCaller()
        {
            AssertThat(SoundboardGlow.ChannelOf(SoundboardControl.CallerGain) == SoundboardGlow.SpeakingChannel.Caller);
            AssertThat(SoundboardGlow.ChannelOf(SoundboardControl.CallerLowPass) == SoundboardGlow.SpeakingChannel.Caller);
            AssertThat(SoundboardGlow.ChannelOf(SoundboardControl.CallerHighPass) == SoundboardGlow.SpeakingChannel.Caller);
            AssertThat(SoundboardGlow.ChannelOf(SoundboardControl.CallerLevel) == SoundboardGlow.SpeakingChannel.Caller);
        }

        [Test]
        public void ChannelOf_VernControls_MapToVern()
        {
            AssertThat(SoundboardGlow.ChannelOf(SoundboardControl.VernGain) == SoundboardGlow.SpeakingChannel.Vern);
            AssertThat(SoundboardGlow.ChannelOf(SoundboardControl.VernLevel) == SoundboardGlow.SpeakingChannel.Vern);
        }

        [Test]
        public void ChannelOf_AdsMasterNone_MapToNone()
        {
            AssertThat(SoundboardGlow.ChannelOf(SoundboardControl.AdsGain) == SoundboardGlow.SpeakingChannel.None);
            AssertThat(SoundboardGlow.ChannelOf(SoundboardControl.AdsLevel) == SoundboardGlow.SpeakingChannel.None);
            AssertThat(SoundboardGlow.ChannelOf(SoundboardControl.Master) == SoundboardGlow.SpeakingChannel.None);
            AssertThat(SoundboardGlow.ChannelOf(SoundboardControl.None) == SoundboardGlow.SpeakingChannel.None);
        }
    }
}