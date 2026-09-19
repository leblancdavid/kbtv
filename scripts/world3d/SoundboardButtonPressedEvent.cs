#nullable enable

using KBTV.Core;

namespace KBTV.World3D
{
    /// <summary>
    /// Published when a broadcast button on the 3D soundboard is pressed and the
    /// board has no direct system action wired for it (Music/Delay). Consumers
    /// (broadcast flow, bleep/delay handling) react to the specific button.
    /// Ads and Drop are executed directly by <see cref="Soundboard3D"/>.
    /// </summary>
    public class SoundboardButtonPressedEvent : GameEvent
    {
        public SoundboardButton Button { get; }

        public SoundboardButtonPressedEvent(SoundboardButton button)
        {
            Button = button;
            Source = "Soundboard3D";
        }
    }
}
