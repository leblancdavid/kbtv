#nullable enable

namespace KBTV.World3D
{
    /// <summary>
    /// Pure, unit-testable decision of how a broadcast button's lamp face should
    /// read, given the game-state facts it depends on. Free of Godot types so the
    /// priority rules can be tested without a scene. <see cref="Soundboard3D"/>
    /// maps a <see cref="Look"/> to an actual colour / energy / pulse.
    /// </summary>
    public enum ButtonLampLook
    {
        /// <summary>Button has no actionable meaning right now (e.g. Ads outside a window).</summary>
        Off,
        /// <summary>Resting idle glow.</summary>
        Idle,
        /// <summary>Green: an action is available now (Ads in a break window, Drop with a caller on air).</summary>
        Ready,
        /// <summary>Amber: the player already committed the action and it is pending (Ads queued).</summary>
        Queued,
        /// <summary>Red pulsing: the action is running (an ad break is live).</summary>
        Active,
        /// <summary>Amber pulsing: the button is the cure for a live problem (Delay while a caller is cursed).</summary>
        Pending,
        /// <summary>Red urgent pulsing: a caller is cursed and the Drop option is also live.</summary>
        Urgent,
        /// <summary>Short bright flash on press.</summary>
        PressFlash
    }

    /// <summary>Resolves each broadcast button's lamp look from primitive game-state facts.</summary>
    public static class SoundboardButtonState
    {
        /// <summary>Ads: live break beats queued beats an open window beats gated.</summary>
        public static ButtonLampLook AdsLook(bool adBreakActive, bool queued, bool queueEnabled, bool inBreakWindow)
        {
            if (adBreakActive) return ButtonLampLook.Active;
            if (queued) return ButtonLampLook.Queued;
            if (queueEnabled || inBreakWindow) return ButtonLampLook.Ready;
            return ButtonLampLook.Off;
        }

        /// <summary>Drop: a live curse makes it urgent; a caller on air makes it ready; otherwise off.</summary>
        public static ButtonLampLook DropLook(bool callerOnAir, bool curseActive)
        {
            if (curseActive) return ButtonLampLook.Urgent;
            if (callerOnAir) return ButtonLampLook.Ready;
            return ButtonLampLook.Off;
        }

        /// <summary>Delay: pulses amber only while a curse is pending (the cure is available).</summary>
        public static ButtonLampLook DelayLook(bool curseActive) =>
            curseActive ? ButtonLampLook.Pending : ButtonLampLook.Off;

        /// <summary>Music has no ambient game state — it only flashes when pressed.</summary>
        public static ButtonLampLook MusicLook() => ButtonLampLook.Idle;

        /// <summary>Resolves a button's look, applying an active press-flash override.</summary>
        public static ButtonLampLook Resolve(SoundboardButton button, bool pressed,
            bool adBreakActive, bool queued, bool queueEnabled, bool inBreakWindow,
            bool callerOnAir, bool curseActive)
        {
            if (pressed) return ButtonLampLook.PressFlash;
            return button switch
            {
                SoundboardButton.Music => MusicLook(),
                SoundboardButton.Delay => DelayLook(curseActive),
                SoundboardButton.Ads => AdsLook(adBreakActive, queued, queueEnabled, inBreakWindow),
                SoundboardButton.Drop => DropLook(callerOnAir, curseActive),
                _ => ButtonLampLook.Off
            };
        }
    }
}
