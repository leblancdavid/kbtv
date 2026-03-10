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

		// Cache RoomStateManager and subscribe to location changes
		_roomStateManager = ServiceRegistry.Instance?.Get<RoomStateManager>();
		if (_roomStateManager != null)
		{
			_roomStateManager.PlayerLocationChanged += OnPlayerLocationChanged;
			GD.Print("World: Subscribed to RoomStateManager.PlayerLocationChanged");
		}
		else
		{
			GD.PrintErr("World: RoomStateManager not available!");
		}

		// Load player texture once
		_playerTexture = GD.Load<Texture2D>("res://assets/sprites/characters/player/south.png");
		GD.Print($"World: Loaded south.png texture: {_playerTexture != null}");

		// Defer initial shadow creation to ensure RoomStateManager has performed first bounds check
		CallDeferred(nameof(CreateInitialShadow));
	}

	private void CreateInitialShadow()
	{
		if (_playerTexture == null || Player == null)
			return;

		// Determine initial shadow system based on current location
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
					// If starting outside (doorway), default to studio as it's the main game area
					targetSystem = WorldRoom.StudioShadows;
					break;
			}
		}

		// Fallback to studio if no manager
		targetSystem ??= WorldRoom.StudioShadows;

		// Create initial shadow
		targetSystem.CreateShadowForObject(Player, _playerTexture, offset: new Vector2(0, ShadowOffsetY), createOvalBase: true);
		_currentShadowSystem = targetSystem;
		_initialShadowCreated = true;

		GD.Print($"World: Initial shadow created in {(targetSystem == WorldRoom.StudioShadows ? "Studio" : "ControlRoom")} system (location: {_roomStateManager?.CurrentLocation})");
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
