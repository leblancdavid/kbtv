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
            SoundboardControl.VernGain,
            SoundboardControl.AdsGain,
            SoundboardControl.Master
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
        public void SlotLamps_MapToTheExpectedChannels()
        {
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.CallerGain).LampName == "Lamp_0");
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.CallerLowPass).LampName == "Lamp_0");
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.CallerHighPass).LampName == "Lamp_0");
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.VernGain).LampName == "Lamp_1");
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.AdsGain).LampName == "Lamp_2");
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.Master).LampName == "Lamp_7");
        }

        [Test]
        public void FadersSlide_KnobsRotate()
        {
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.CallerGain).Kind == ControlKind.Fader);
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.VernGain).Kind == ControlKind.Fader);
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.AdsGain).Kind == ControlKind.Fader);
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.CallerLowPass).Kind == ControlKind.Knob);
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.CallerHighPass).Kind == ControlKind.Knob);
            AssertThat(SoundboardPhysicalLayout.SlotFor(SoundboardControl.Master).Kind == ControlKind.Knob);
        }

        [Test]
        public void IdleLamps_DoNotOverlapSlotLamps()
        {
            var slotLamps = SoundboardPhysicalLayout.Slots.Select(s => s.LampName).ToHashSet();
            foreach (var idle in SoundboardPhysicalLayout.IdleLamps)
            {
                AssertThat(!slotLamps.Contains(idle));
            }
            AssertThat(SoundboardPhysicalLayout.IdleLamps.Length == 4);
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
            AssertThat(Mathf.IsEqualApprox(SoundboardPhysicalLayout.KnobRotationDeg(0.5f), 0f));
            AssertThat(Mathf.IsEqualApprox(
                SoundboardPhysicalLayout.KnobRotationDeg(0f),
                -SoundboardPhysicalLayout.KnobTurnDeg / 2f));
            AssertThat(Mathf.IsEqualApprox(
                SoundboardPhysicalLayout.KnobRotationDeg(1f),
                SoundboardPhysicalLayout.KnobTurnDeg / 2f));
        }
    }
}