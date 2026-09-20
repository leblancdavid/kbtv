#nullable enable

using KBTV.Core;

namespace KBTV.World3D
{
    /// <summary>
    /// Published every time a broadcast button on the 3D soundboard is pressed.
    /// <see cref="Soundboard3D"/> also executes the direct system actions (Ads queue,
    /// Music bed, caller drop) itself; consumers react to the specific button for
    /// everything else — LiveShowFooter resolves the curse QTE on Delay/Drop.
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
