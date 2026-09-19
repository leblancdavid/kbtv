using System.Linq;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Audio;
using KBTV.World3D;

namespace KBTV.Tests.Unit.World3D
{
    public class SoundboardPhysicalLayoutTests : KBTVTestClass
    {
        private static readonly SoundboardControl[] AllControls =
        {
            SoundboardControl.CallerGain,
            SoundboardControl.CallerLowPass,
            SoundboardControl.CallerHighPass,
            SoundboardControl.CallerLevel,
            SoundboardControl.VernGain,
            SoundboardControl.VernLevel,
            SoundboardControl.AdsGain,
            SoundboardControl.AdsLevel,
            SoundboardControl.MasterLeft,
            SoundboardControl.MasterRight
        };

        private static readonly SoundboardButton[] AllButtons =
        {
            SoundboardButton.Music,
            SoundboardButton.Delay,
            SoundboardButton.Ads,
            SoundboardButton.Drop
        };

        public SoundboardPhysicalLayoutTests(Node testScene) : base(testScene) { }

        [Test]
        public void EveryControl_HasAPart()
        {
            foreach (var control in AllControls)
            {
                var slot = SoundboardPhysicalLayout.SlotFor(control);
                AssertThat(slot.PartName.Length > 0);
                AssertThat(slot.LampName.Length > 0);
            }
        }

        [Test]
        public void SlotPartNames_AreUnique()
        {
            var names = SoundboardPhysicalLayout.Slots.Select(s => s.PartName).ToList();
            AssertThat(names.Count == names.Distinct().Count());
        }

        [Test]
        public void ButtonSlots_AreComplete()
        {
            foreach (var button in AllButtons)
            {
                var slot = SoundboardPhysicalLayout.ButtonSlotFor(button);
                AssertThat(slot.PartName.Length > 0);
                AssertThat(slot.LampName.Length > 0);
            }

            var names = SoundboardPhysicalLayout.ButtonSlots.Select(s => s.PartName).ToList();
            AssertThat(names.Count == names.Distinct().Count());
        }

        [Test]
        public void SlotLamps_MapToTheExpectedChannels()
        {
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.VernGain).LampName == "Lamp_0");
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.VernLevel).LampName == "Lamp_0");
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.CallerGain).LampName == "Lamp_1");
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.CallerLowPass).LampName == "Lamp_1");
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.CallerHighPass).LampName == "Lamp_1");
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.CallerLevel).LampName == "Lamp_1");
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.AdsGain).LampName == "Lamp_2");
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.AdsLevel).LampName == "Lamp_2");
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.MasterLeft).LampName == "Lamp_3L");
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.MasterRight).LampName == "Lamp_3R");
        }

        [Test]
        public void FadersSlide_KnobsRotate()
        {
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.CallerGain).Kind == ControlKind.Knob);
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.CallerLevel).Kind == ControlKind.Fader);
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.VernGain).Kind == ControlKind.Knob);
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.VernLevel).Kind == ControlKind.Fader);
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.AdsGain).Kind == ControlKind.Knob);
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.AdsLevel).Kind == ControlKind.Fader);
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.CallerLowPass).Kind == ControlKind.Knob);
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.CallerHighPass).Kind == ControlKind.Knob);
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.MasterLeft).Kind == ControlKind.Fader);
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.MasterRight).Kind == ControlKind.Fader);
        }

        [Test]
        public void FaderLocalZ_RestSitsAtTrackCentre()
        {
            AssertThat(Mathf.IsEqualApprox(
                SoundboardPhysicalLayout.FaderLocalZ(0.5f),
                SoundboardPhysicalLayout.FaderRestLocalZ));
        }

        [Test]
        public void FaderLocalZ_SwingIsPlusMinusTravel()
        {
            AssertThat(Mathf.IsEqualApprox(
                SoundboardPhysicalLayout.FaderLocalZ(0f),
                SoundboardPhysicalLayout.FaderRestLocalZ - SoundboardPhysicalLayout.FaderTravel));
            AssertThat(Mathf.IsEqualApprox(
                SoundboardPhysicalLayout.FaderLocalZ(1f),
                SoundboardPhysicalLayout.FaderRestLocalZ + SoundboardPhysicalLayout.FaderTravel));
        }

        [Test]
        public void KnobRotationDeg_SwingsAroundRest()
        {
            AssertThat(Mathf.IsEqualApprox(
                SoundboardPhysicalLayout.KnobRotationDeg(0.5f),
                SoundboardPhysicalLayout.KnobRestOffsetDeg));
            AssertThat(Mathf.IsEqualApprox(
                SoundboardPhysicalLayout.KnobRotationDeg(0f),
                SoundboardPhysicalLayout.KnobRestOffsetDeg + SoundboardPhysicalLayout.KnobTurnDeg / 2f));
            AssertThat(Mathf.IsEqualApprox(
                SoundboardPhysicalLayout.KnobRotationDeg(1f),
                SoundboardPhysicalLayout.KnobRestOffsetDeg - SoundboardPhysicalLayout.KnobTurnDeg / 2f));
        }

        [Test]
        public void KnobRotationDeg_ValueZero_315Degrees()
        {
            AssertThat(Mathf.IsEqualApprox(SoundboardPhysicalLayout.KnobRotationDeg(0f), 315f));
        }

        [Test]
        public void KnobRotationDeg_ValueOne_45Degrees()
        {
            AssertThat(Mathf.IsEqualApprox(SoundboardPhysicalLayout.KnobRotationDeg(1f), 45f));
        }

        [Test]
        public void KnobRotationDeg_Rest_12OClock()
        {
            AssertThat(Mathf.IsEqualApprox(SoundboardPhysicalLayout.KnobRotationDeg(0.5f), 180f));
        }
    }
}
