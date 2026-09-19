#nullable enable

using Godot;

namespace KBTV.Audio
{
    /// <summary>
    /// The controls on the 3D soundboard. Faders are the per-channel output
    /// strips (they slide); knobs are the rotary CALLER filter controls and the
    /// per-channel gain knobs. The stereo master pair (channel 4's two faders)
    /// are linked: both read and write the single <see cref="SoundboardKnobState.Fader"/>.
    /// </summary>
    public enum SoundboardControl
    {
        None,
        CallerGain,
        CallerLowPass,
        CallerHighPass,
        VernGain,
        AdsGain,
        CallerLevel,
        VernLevel,
        AdsLevel,
        MasterLeft,
        MasterRight
    }

    /// <summary>
    /// Pure mapping between screen-space drag input and knob/fader values on a
    /// <see cref="SoundboardKnobState"/>. Kept static and dependency-free so the
    /// 3D interaction in World3D can be unit tested without a scene tree.
    /// </summary>
    public static class SoundboardControlApplier
    {
        /// <summary>
        /// Converts a vertical mouse drag into a new knob value. Dragging upward
        /// (negative screen delta) raises the value, matching a physical fader.
        /// </summary>
        public static float ValueFromDrag(float startValue, float dragDeltaScreenY, float pixelsPerUnit)
        {
            return Mathf.Clamp(startValue - dragDeltaScreenY / Mathf.Max(1f, pixelsPerUnit), 0f, 1f);
        }

        /// <summary>Clamped 0..1 knob value.</summary>
        public static float ClampValue(float value) => Mathf.Clamp(value, 0f, 1f);

        /// <summary>Writes the (clamped) value into the matching knob field.</summary>
        public static void Apply(SoundboardKnobState state, SoundboardControl control, float value)
        {
            if (state == null)
            {
                return;
            }

            var clamped = ClampValue(value);
            switch (control)
            {
                case SoundboardControl.CallerGain:
                    state.CallerGain = clamped;
                    break;
                case SoundboardControl.CallerLowPass:
                    state.CallerLowPass = clamped;
                    break;
                case SoundboardControl.CallerHighPass:
                    state.CallerHighPass = clamped;
                    break;
                case SoundboardControl.VernGain:
                    state.VernGain = clamped;
                    break;
                case SoundboardControl.AdsGain:
                    state.AdsGain = clamped;
                    break;
                case SoundboardControl.CallerLevel:
                    state.CallerLevel = clamped;
                    break;
                case SoundboardControl.VernLevel:
                    state.VernLevel = clamped;
                    break;
                case SoundboardControl.AdsLevel:
                    state.AdsLevel = clamped;
                    break;
                case SoundboardControl.MasterLeft:
                case SoundboardControl.MasterRight:
                    state.Fader = clamped;
                    break;
            }
        }

        /// <summary>Reads the current value for a control from the state.</summary>
        public static float CurrentValue(SoundboardKnobState state, SoundboardControl control)
        {
            if (state == null)
            {
                return SoundboardKnobState.NeutralValue;
            }

            switch (control)
            {
                case SoundboardControl.CallerGain:
                    return state.CallerGain;
                case SoundboardControl.CallerLowPass:
                    return state.CallerLowPass;
                case SoundboardControl.CallerHighPass:
                    return state.CallerHighPass;
                case SoundboardControl.VernGain:
                    return state.VernGain;
                case SoundboardControl.AdsGain:
                    return state.AdsGain;
                case SoundboardControl.CallerLevel:
                    return state.CallerLevel;
                case SoundboardControl.VernLevel:
                    return state.VernLevel;
                case SoundboardControl.AdsLevel:
                    return state.AdsLevel;
                case SoundboardControl.MasterLeft:
                case SoundboardControl.MasterRight:
                    return state.Fader;
                default:
                    return SoundboardKnobState.NeutralValue;
            }
        }
    }
}