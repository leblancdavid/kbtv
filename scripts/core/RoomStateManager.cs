using Godot;
using KBTV.Core;

namespace KBTV.Core
{
	/// <summary>
	/// Tracks the player's location within the studio rooms (control room, studio, or outside).
	/// Uses bounds-based detection for accurate room membership.
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

		private Rect2 _controlRoomBounds = new Rect2();
		private Rect2 _studioBounds = new Rect2();
		private Player? _player;

		/// <summary>
		/// Called when the node enters the scene tree.
		/// Registers itself with the ServiceRegistry.
		/// </summary>
		public override void _Ready()
		{
			ServiceRegistry.Instance.RegisterSelf<RoomStateManager>(this);
		}

		/// <summary>
		/// Sets the boundary of the control room for bounds-based player detection.
		/// </summary>
		public void SetControlRoomBounds(Rect2 bounds)
		{
			_controlRoomBounds = bounds;
			GD.Print($"RoomStateManager: Control room bounds set to {_controlRoomBounds}");
		}

		/// <summary>
		/// Sets the boundary of the studio room for bounds-based player detection.
		/// </summary>
		public void SetStudioBounds(Rect2 bounds)
		{
			_studioBounds = bounds;
			GD.Print($"RoomStateManager: Studio bounds set to {_studioBounds}");
		}

		/// <summary>
		/// Called every frame to check if player is inside any room bounds.
		/// </summary>
		public override void _Process(double delta)
		{
			if (_controlRoomBounds == new Rect2() && _studioBounds == new Rect2())
				return;

			// Find player if not cached
			if (_player == null)
			{
				_player = GetTree().GetFirstNodeInGroup("player") as Player;
				if (_player == null)
					return;
			}

			var playerPos = _player.GlobalPosition;
			var wasInRoom = CurrentLocation;
			var newLocation = PlayerLocation.Outside;

			if (_controlRoomBounds.HasPoint(playerPos))
				newLocation = PlayerLocation.InControlRoom;
			else if (_studioBounds.HasPoint(playerPos))
				newLocation = PlayerLocation.InStudio;

			// Emit signal only when state changes
			if (wasInRoom != newLocation)
			{
				CurrentLocation = newLocation;
				var locationName = newLocation.ToString();
				GD.Print($"RoomStateManager: Player {locationName} (pos: {playerPos})");
				EmitSignal(nameof(PlayerLocationChanged), Variant.From(CurrentLocation));
			}
		}
	}
}
