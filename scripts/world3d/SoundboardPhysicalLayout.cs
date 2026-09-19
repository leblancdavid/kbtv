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

    /// <summary>The board's four blocky broadcast buttons (2x2 grid).</summary>
    public enum SoundboardButton
    {
        None,
        Music,
        Delay,
        Ads,
        Drop
    }

    /// <summary>Mapping of one broadcast button to its GLB cap + lamp face.</summary>
    public readonly record struct SoundboardButtonSlot(
        SoundboardButton Button, string PartName, string LampName);

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
        public const float FaderTravel = 0.05f;

        /// <summary>Full knob swing in degrees (each control turns ±135° around rest).</summary>
        public const float KnobTurnDeg = 270f;

        /// <summary>
        /// Fixed knob rotation offset so the index pointer sits at 12 o'clock (up)
        /// at rest. 180° is the validated "up" angle for the notch on the exported
        /// board; rest (value 0.5) lands exactly there, with ±135° of swing around it.
        /// </summary>
        public const float KnobRestOffsetDeg = 180f;

        /// <summary>Board-local Z of a fader cap at rest (authoring y 0.14 → glTF z -0.14).</summary>
        public const float FaderRestLocalZ = -0.14f;

        /// <summary>
        /// One entry per mixer control: GLB part to drive + its channel lamp.
        /// Strips are authored left to right: 0=Vern, 1=Caller, 2=Ads/Music,
        /// 3=stereo master pair (both caps share the linked <c>Fader</c> value).
        /// </summary>
        public static readonly SoundboardSlot[] Slots =
        {
            new(SoundboardControl.VernGain, "Knob_0_2", ControlKind.Knob, "Lamp_0"),
            new(SoundboardControl.VernLevel, "FaderCap_0", ControlKind.Fader, "Lamp_0"),
            new(SoundboardControl.CallerGain, "Knob_1_2", ControlKind.Knob, "Lamp_1"),
            new(SoundboardControl.CallerLowPass, "Knob_1_0", ControlKind.Knob, "Lamp_1"),
            new(SoundboardControl.CallerHighPass, "Knob_1_1", ControlKind.Knob, "Lamp_1"),
            new(SoundboardControl.CallerLevel, "FaderCap_1", ControlKind.Fader, "Lamp_1"),
            new(SoundboardControl.AdsGain, "Knob_2_2", ControlKind.Knob, "Lamp_2"),
            new(SoundboardControl.AdsLevel, "FaderCap_2", ControlKind.Fader, "Lamp_2"),
            new(SoundboardControl.MasterLeft, "FaderCap_3L", ControlKind.Fader, "Lamp_3L"),
            new(SoundboardControl.MasterRight, "FaderCap_3R", ControlKind.Fader, "Lamp_3R")
        };

        /// <summary>One entry per broadcast button: cap part to press + lamp face to light.</summary>
        public static readonly SoundboardButtonSlot[] ButtonSlots =
        {
            new(SoundboardButton.Music, "Button_Music", "BtnLamp_Music"),
            new(SoundboardButton.Delay, "Button_Delay", "BtnLamp_Delay"),
            new(SoundboardButton.Ads, "Button_Ads", "BtnLamp_Ads"),
            new(SoundboardButton.Drop, "Button_Drop", "BtnLamp_Drop")
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

        public static SoundboardButtonSlot ButtonSlotFor(SoundboardButton button)
        {
            foreach (var slot in ButtonSlots)
            {
                if (slot.Button == button)
                {
                    return slot;
                }
            }
            return new SoundboardButtonSlot(button, string.Empty, string.Empty);
        }

        /// <summary>How far a button cap sinks (board-local Z) while pressed.</summary>
        public const float ButtonPressDepth = 0.006f;

        /// <summary>Fader cap local Z for a normalized value (-travel at 0 .. +travel at 1 around rest).</summary>
        public static float FaderLocalZ(float value) => (value - 0.5f) * 2f * FaderTravel + FaderRestLocalZ;

        /// <summary>
        /// Knob rotation in degrees for a normalized value (value-up turns clockwise
        /// on screen): value 0 = 315° (down-right), value 0.5 = 180° (12 o'clock),
        /// value 1 = 45° (up-right).
        /// </summary>
        public static float KnobRotationDeg(float value) =>
            KnobRestOffsetDeg - (value - 0.5f) * KnobTurnDeg;
    }
}
