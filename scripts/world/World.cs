using Godot;
using KBTV.Core;

public partial class World : Node2D
{
	[Export] public float TileSize = 16f;

	public WorldRoom WorldRoom { get; private set; } = null!;
	public Player Player { get; private set; } = null!;

	private RoomStateManager? _roomStateManager;
	private CastShadowSystem? _currentShadowSystem;
	private Texture2D? _playerTexture;
	private bool _initialShadowCreated = false;
	private const float ShadowOffsetY = -6f;

	public override void _Ready()
	{
		Player = GetNode<Player>("Player");
		WorldRoom = GetNode<WorldRoom>("WorldRoom");

		GD.Print($"World: Player found: {Player != null}, WorldRoom found: {WorldRoom != null}");

		Player.SetWorld(this);
		WorldRoom.SetPlayer(Player);

		// Defer full initialization until all nodes have completed _Ready()
		CallDeferred(nameof(DeferredInitialize));
	}

	private void DeferredInitialize()
	{
		// Load player texture once
		_playerTexture = GD.Load<Texture2D>("res://assets/sprites/characters/player/south.png");
		GD.Print($"World: Loaded south.png texture: {_playerTexture != null}");

		// Cache RoomStateManager (should be available now that all _Ready() calls have completed)
		_roomStateManager = ServiceRegistry.Instance?.Get<RoomStateManager>();
		if (_roomStateManager != null)
		{
			_roomStateManager.PlayerLocationChanged += OnPlayerLocationChanged;
			GD.Print("World: Subscribed to RoomStateManager.PlayerLocationChanged");
		}
		else
		{
			GD.PrintErr("World: RoomStateManager not available after deferred init!");
			return;
		}

		// Create initial shadow based on current room location
		CreateInitialShadow();
	}

	private void CreateInitialShadow()
	{
		if (_playerTexture == null || Player == null)
			return;

		// Compute initial location using room bounds (RoomStateManager hasn't updated yet)
		// Use ShadowRoomBounds from each system which were set during builder initialization
		var controlBounds = WorldRoom.ControlShadows?.ShadowRoomBounds ?? new Rect2();
		var studioBounds = WorldRoom.StudioShadows?.ShadowRoomBounds ?? new Rect2();
		var playerPos = Player.GlobalPosition;

		CastShadowSystem? targetSystem;
		if (studioBounds != new Rect2() && studioBounds.HasPoint(playerPos))
		{
			targetSystem = WorldRoom.StudioShadows;
		}
		else if (controlBounds != new Rect2() && controlBounds.HasPoint(playerPos))
		{
			targetSystem = WorldRoom.ControlShadows;
		}
		else
		{
			// Default to studio if neither bound contains player (e.g., starting position in doorway)
			targetSystem = WorldRoom.StudioShadows;
		}

		// Create initial shadow
		targetSystem.CreateShadowForObject(Player, _playerTexture, offset: new Vector2(0, ShadowOffsetY), createOvalBase: true);
		_currentShadowSystem = targetSystem;
		_initialShadowCreated = true;

		GD.Print($"World: Initial shadow created in {(targetSystem == WorldRoom.StudioShadows ? "Studio" : "ControlRoom")} system (player at {playerPos})");
	}

	private void OnPlayerLocationChanged(RoomStateManager.PlayerLocation location)
	{
		GD.Print($"World: Player location changed to {location}, updating shadow");
		UpdatePlayerShadow();
	}

	private void UpdatePlayerShadow()
	{
		if (!_initialShadowCreated || _playerTexture == null || Player == null)
			return;

		// Determine which shadow system to use based on player location
		CastShadowSystem? targetSystem = null;
		if (_roomStateManager != null)
		{
			switch (_roomStateManager.CurrentLocation)
			{
				case RoomStateManager.PlayerLocation.InStudio:
					targetSystem = WorldRoom.StudioShadows;
					break;
				case RoomStateManager.PlayerLocation.InControlRoom:
					targetSystem = WorldRoom.ControlShadows;
					break;
				case RoomStateManager.PlayerLocation.Outside:
					// Keep current shadow system (conservative approach)
					targetSystem = _currentShadowSystem;
					break;
			}
		}

		// If no valid target system, nothing to do
		if (targetSystem == null)
			return;

		// If we're already using this system, no change needed
		if (_currentShadowSystem == targetSystem)
			return;

		// Remove shadow from old system (guard against null)
		if (_currentShadowSystem != null)
		{
			_currentShadowSystem.RemoveShadowForObject(Player);
		}

		// Create shadow in new system
		targetSystem.CreateShadowForObject(Player, _playerTexture, offset: new Vector2(0, ShadowOffsetY), createOvalBase: true);
		_currentShadowSystem = targetSystem;

		GD.Print($"World: Player shadow now using {(targetSystem == WorldRoom.StudioShadows ? "Studio" : "ControlRoom")} system (location: {_roomStateManager?.CurrentLocation})");
	}

	public override void _ExitTree()
	{
		// Unsubscribe from signal to prevent memory leaks
		if (_roomStateManager != null)
		{
			_roomStateManager.PlayerLocationChanged -= OnPlayerLocationChanged;
		}
	}
}
