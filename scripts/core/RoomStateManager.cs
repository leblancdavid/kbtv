using Godot;
using KBTV.Core;

namespace KBTV.Core
{
    /// <summary>
    /// Singleton that tracks whether the player is inside the broadcast room.
    /// It listens to the ControlRoomBuilder area signals and exposes the state.
    /// </summary>
    [GlobalClass]
    public partial class RoomStateManager : Node
    {
        /// <summary>
        /// True when the player is inside the broadcast room.
        /// </summary>
        [Signal]
        public delegate void PlayerInRoomChangedEventHandler(bool inRoom);

        public bool PlayerInRoom { get; private set; } = false;

        /// <summary>
        /// Called when the node enters the scene tree.
        /// Registers itself with the ServiceRegistry.
        /// </summary>
        public override void _Ready()
        {
            ServiceRegistry.Instance.RegisterSelf<RoomStateManager>(this);
        }

        /// <summary>
        /// Called by ControlRoomBuilder when the player enters the room.
        /// </summary>
        public void OnPlayerEntered()
        {
            PlayerInRoom = true;
            EmitSignal(nameof(PlayerInRoomChanged), true);
        }

        /// <summary>
        /// Called by ControlRoomBuilder when the player exits the room.
        /// </summary>
        public void OnPlayerExited()
        {
            PlayerInRoom = false;
            EmitSignal(nameof(PlayerInRoomChanged), false);
        }
    }
}
