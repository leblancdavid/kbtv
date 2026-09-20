#nullable enable

namespace KBTV.World3D
{
    /// <summary>
    /// Pure, unit-testable decision of how a broadcast button's lamp face should
    /// read, given the game-state facts it depends on. Free of Godot types so the
    /// priority rules can be tested without a scene. <see cref="Soundboard3D"/>
    /// maps a <see cref="ButtonLampLook"/> to an actual colour / energy / pulse.
    /// </summary>
    public enum ButtonLampLook
    {
        /// <summary>Button has no actionable meaning right now (e.g. Ads outside a window).</summary>
        Off,
        /// <summary>Resting idle glow.</summary>
        Idle,
        /// <summary>Green: an action is available now (Ads in a break window, Drop with a caller on air).</summary>
        Ready,
        /// <summary>Amber: the player already committed the action and it is pending (Ads queued, music bed playing).</summary>
        Queued,
        /// <summary>Red pulsing: the action is running (an ad break is live).</summary>
        Active,
        /// <summary>Red urgent pulsing: a caller is cursed and the Drop option is also live.</summary>
        Urgent,
        /// <summary>White-yellow growing/shrinking flash: the button demands a press now.</summary>
        Flashing,
        /// <summary>Short bright flash on press.</summary>
        PressFlash
    }

    /// <summary>The primitive game-state facts the lamp resolver polls each frame.</summary>
    public readonly record struct ButtonStateFacts(
        bool AdBreakActive,
        bool AdQueued,
        bool QueueEnabled,
        bool InBreakWindow,
        bool BreakDue,
        bool MusicBedPlaying,
        bool CallerOnAir,
        bool CurseActive);

    /// <summary>Resolves each broadcast button's lamp look from primitive game-state facts.</summary>
    public static class SoundboardButtonState
    {
        /// <summary>
        /// Ads: live break beats queued beats due-now (flashing reminder at T-0 until
        /// the ads actually roll) beats an open window beats gated.
        /// </summary>
        public static ButtonLampLook AdsLook(in ButtonStateFacts facts)
        {
            if (facts.AdBreakActive) return ButtonLampLook.Active;
            if (facts.AdQueued) return ButtonLampLook.Queued;
            if (facts.BreakDue) return ButtonLampLook.Flashing;
            if (facts.QueueEnabled || facts.InBreakWindow) return ButtonLampLook.Ready;
            return ButtonLampLook.Off;
        }

        /// <summary>Drop: a live curse makes it urgent; a caller on air makes it ready; otherwise off.</summary>
        public static ButtonLampLook DropLook(bool callerOnAir, bool curseActive)
        {
            if (curseActive) return ButtonLampLook.Urgent;
            if (callerOnAir) return ButtonLampLook.Ready;
            return ButtonLampLook.Off;
        }

        /// <summary>Delay: white-yellow flash only while a curse is pending (the cure is available).</summary>
        public static ButtonLampLook DelayLook(bool curseActive) =>
            curseActive ? ButtonLampLook.Flashing : ButtonLampLook.Off;

        /// <summary>
        /// Music: flash while a break window is open and the bed has not been started;
        /// steady amber once the bed is running (the player still has to fade it up);
        /// idle otherwise.
        /// </summary>
        public static ButtonLampLook MusicLook(bool windowOpen, bool bedPlaying)
        {
            if (bedPlaying) return ButtonLampLook.Queued;
            if (windowOpen) return ButtonLampLook.Flashing;
            return ButtonLampLook.Idle;
        }

        /// <summary>Resolves a button's look, applying an active press-flash override.</summary>
        public static ButtonLampLook Resolve(SoundboardButton button, in ButtonStateFacts facts, bool pressed)
        {
            if (pressed) return ButtonLampLook.PressFlash;
            return button switch
            {
                SoundboardButton.Music => MusicLook(facts.InBreakWindow, facts.MusicBedPlaying),
                SoundboardButton.Delay => DelayLook(facts.CurseActive),
                SoundboardButton.Ads => AdsLook(facts),
                SoundboardButton.Drop => DropLook(facts.CallerOnAir, facts.CurseActive),
                _ => ButtonLampLook.Off
            };
        }
    }
}
