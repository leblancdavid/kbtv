using Godot;
using KBTV.Core;

namespace KBTV.Core
{
	/// <summary>
	/// Tracks whether the player is inside the control room based on room boundaries.
	/// Uses bounds-based detection instead of trigger areas for accurate room membership.
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

		private Rect2 _controlRoomBounds = new Rect2();
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
		/// <param name="bounds">The rectangle representing the control room floor area</param>
		public void SetControlRoomBounds(Rect2 bounds)
		{
			_controlRoomBounds = bounds;
			GD.Print($"RoomStateManager: Control room bounds set to {_controlRoomBounds}");
		}

		/// <summary>
		/// Called every frame to check if player is inside the control room bounds.
		/// </summary>
		public override void _Process(double delta)
		{
			if (_controlRoomBounds == new Rect2())
				return;

			// Find player if not cached
			if (_player == null)
			{
				_player = GetTree().GetFirstNodeInGroup("player") as Player;
				if (_player == null)
					return;
			}

			var playerPos = _player.GlobalPosition;
			var wasInRoom = PlayerInRoom;
			PlayerInRoom = _controlRoomBounds.HasPoint(playerPos);

			// Emit signal only when state changes
			if (wasInRoom != PlayerInRoom)
			{
				var status = PlayerInRoom ? "entered" : "exited";
				GD.Print($"RoomStateManager: Player {status} control room (pos: {playerPos}, bounds: {_controlRoomBounds})");
				EmitSignal(nameof(PlayerInRoomChanged), PlayerInRoom);
			}
		}
	}
}
