using Godot;
using KBTV.Core;

namespace KBTV.Core
{
	/// <summary>
	/// Tracks the player's location within the studio rooms (control room, studio, or outside).
	/// Location is set manually by the 3D world's own room detection.
	/// </summary>
	[GlobalClass]
	public partial class RoomStateManager : Node
	{
		/// <summary>
		/// Player location within the world.
		/// </summary>
		public enum PlayerLocation
		{
			Outside,
			InControlRoom,
			InStudio
		}

		/// <summary>
		/// Emitted when the player's location changes.
		/// </summary>
		[Signal]
		public delegate void PlayerLocationChangedEventHandler(PlayerLocation location);

		/// <summary>
		/// Current player location (read-only).
		/// </summary>
		public PlayerLocation CurrentLocation { get; private set; } = PlayerLocation.Outside;

		/// <summary>
		/// Convenience property for backward compatibility: true if in control room.
		/// </summary>
		public bool PlayerInRoom => CurrentLocation == PlayerLocation.InControlRoom;

		/// <summary>
		/// Called when the node enters the scene tree.
		/// Registers itself with the ServiceRegistry.
		/// </summary>
		public override void _Ready()
		{
			ServiceRegistry.Instance.RegisterSelf<RoomStateManager>(this);
		}

		/// <summary>
		/// Sets the player's location manually (used by the 3D world's own room detection).
		/// Emits <see cref="PlayerLocationChanged"/> only when the location actually changes.
		/// </summary>
		public void SetPlayerLocation(PlayerLocation location)
		{
			if (CurrentLocation == location)
				return;

			CurrentLocation = location;
			GD.Print($"RoomStateManager: Player {location} (manual)");
			EmitSignal(nameof(PlayerLocationChanged), Variant.From(CurrentLocation));
		}
	}
}
