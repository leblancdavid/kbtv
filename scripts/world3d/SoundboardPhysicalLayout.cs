#nullable enable

using KBTV.Audio;

namespace KBTV.World3D
{
    /// <summary>Kind of drive a physical board part needs.</summary>
    public enum ControlKind
    {
        Fader,
        Knob
    }

    /// <summary>Mapping of one mixer control to its GLB part + channel lamp.</summary>
    public readonly record struct SoundboardSlot(
        SoundboardControl Control, string PartName, ControlKind Kind, string LampName);

    /// <summary>
    /// Pure mapping between mixer controls and the named parts of the regenerated
    /// soundboard.glb (chassis + separate fader caps, knobs, lamps). Free of
    /// Godot-only types so the layout can be unit-tested without a scene.
    /// Pumping velocities: knobs / master spin around glTF-local Y; fader caps
    /// slide along glTF-local Z (authoring y=0.12 maps to rest z=-0.12).
    /// </summary>
    public static class SoundboardPhysicalLayout
    {
        /// <summary>Name of the static chassis mesh inside the GLB.</summary>
        public const string ChassisNode = "soundboard";

        /// <summary>Half-travel (metres) of a fader cap from its rest (track-centre) position.</summary>
        public const float FaderTravel = 0.06f;

        /// <summary>Full knob swing in degrees (each control turns ±45° around rest).</summary>
        public const float KnobTurnDeg = 90f;

        /// <summary>Board-local Z of a fader cap at rest (authoring y 0.12 → glTF z -0.12).</summary>
        public const float FaderRestLocalZ = -0.12f;

        /// <summary>Per-channel lamps driven as status LEDs; the master channel reuses the last lamp.</summary>
        public static readonly string[] IdleLamps = { "Lamp_3", "Lamp_4", "Lamp_5", "Lamp_6" };

        /// <summary>One entry per mixer control: GLB part to drive + its channel lamp.</summary>
        public static readonly SoundboardSlot[] Slots =
        {
            new(SoundboardControl.CallerGain, "FaderCap_0", ControlKind.Fader, "Lamp_0"),
            new(SoundboardControl.CallerLowPass, "Knob_0_0", ControlKind.Knob, "Lamp_0"),
            new(SoundboardControl.CallerHighPass, "Knob_0_1", ControlKind.Knob, "Lamp_0"),
            new(SoundboardControl.VernGain, "FaderCap_1", ControlKind.Fader, "Lamp_1"),
            new(SoundboardControl.AdsGain, "FaderCap_2", ControlKind.Fader, "Lamp_2"),
            new(SoundboardControl.Master, "MasterKnob", ControlKind.Knob, "Lamp_7")
        };

        public static SoundboardSlot SlotFor(SoundboardControl control)
        {
            foreach (var slot in Slots)
            {
                if (slot.Control == control)
                {
                    return slot;
                }
            }
            return new SoundboardSlot(control, string.Empty, ControlKind.Knob, string.Empty);
        }

        /// <summary>Fader cap local Z for a normalized value (-travel at 0 .. +travel at 1 around rest).</summary>
        public static float FaderLocalZ(float value) => (value - 0.5f) * 2f * FaderTravel + FaderRestLocalZ;

        /// <summary>Knob rotation in degrees for a normalized value (full swing around rest).</summary>
        public static float KnobRotationDeg(float value) => (value - 0.5f) * 2f * KnobTurnDeg;
    }
}