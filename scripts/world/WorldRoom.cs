using Godot;
using KBTV.Core;

/// <summary>
/// Thin host for the world's rooms. Each room is a self-contained <see cref="RoomBase"/> node
/// (code-built in its own <c>_Ready</c>) that registers its own bounds with
/// <see cref="RoomStateManager"/>. This node only adds the rooms and forwards a small
/// cross-room API (player assignment, coordinate translation, shadow systems).
/// </summary>
public partial class WorldRoom : Node2D
{
	public CastShadowSystem ControlShadows => _controlRoom.Shadows;
	public CastShadowSystem StudioShadows => _studioRoom.Shadows;

	private CharacterBody2D _player = null!;
	private ControlRoom _controlRoom = null!;
	private StudioRoom _studioRoom = null!;
	private bool _wasPlayerInEastDoor;
	private bool _wasPlayerInWestDoor;

	public void SetPlayer(CharacterBody2D player)
	{
		_player = player;
		_controlRoom.SetPlayer(player);
		_studioRoom.SetPlayer(player);
	}

	public Vector2 ControlRoomGridToWorld(Vector2I gridPos)
	{
		return _controlRoom.GridToWorld(gridPos);
	}

	public Vector2 StudioGridToWorld(Vector2I gridPos)
	{
		return _studioRoom.GridToWorld(gridPos);
	}

	public Rect2 GetStudioBounds()
	{
		return _studioRoom.GetFloorBounds();
	}

	public override void _Ready()
	{
		_controlRoom = new ControlRoom { Name = "ControlRoom" };
		AddChild(_controlRoom);

		_studioRoom = new StudioRoom { Name = "StudioRoom" };
		AddChild(_studioRoom);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_select"))
		{
			_controlRoom.ToggleDebug();
			_studioRoom.ToggleDebug();
		}
	}

	public override void _Process(double delta)
	{
		UpdatePlayerLightMask();
		UpdateDoorAnimations(delta);
	}

	private void UpdatePlayerLightMask()
	{
		if (_player == null) return;

		var sprite = _player.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (sprite == null) return;

		var playerPos = _player.GlobalPosition;
		var studioBounds = _studioRoom.GetFloorBounds();
		var controlBounds = _controlRoom.GetFloorBounds();

		int targetMask;
		if (studioBounds.HasPoint(playerPos))
		{
			targetMask = 2;
		}
		else if (controlBounds.HasPoint(playerPos))
		{
			targetMask = 1;
		}
		else
		{
			return;
		}

		sprite.Set("light_mask", targetMask);
	}

	private void UpdateDoorAnimations(double delta)
	{
		if (_player == null) return;

		_controlRoom.UpdateDoorAnimations(delta);
		_studioRoom.UpdateDoorAnimations(delta);

		var playerCollision = _player.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (playerCollision?.Shape is not RectangleShape2D playerShape)
			return;

		var playerSize = playerShape.Size;
		var playerRect = new Rect2(
			playerCollision.GlobalPosition - (playerSize * 0.5f),
			playerSize
		);

		var eastBounds = _controlRoom.EastDoorBounds;
		bool isPlayerInEastDoor = eastBounds != new Rect2(0, 0, 0, 0) && eastBounds.Intersects(playerRect);

		if (isPlayerInEastDoor && !_wasPlayerInEastDoor)
		{
			_controlRoom.TriggerEastDoorAnimation();
		}
		else if (!isPlayerInEastDoor && _wasPlayerInEastDoor)
		{
			_controlRoom.TriggerEastDoorClose();
		}
		_wasPlayerInEastDoor = isPlayerInEastDoor;

		var westBounds = _studioRoom.WestDoorBounds;
		bool isPlayerInWestDoor = westBounds != new Rect2(0, 0, 0, 0) && westBounds.Intersects(playerRect);

		if (isPlayerInWestDoor && !_wasPlayerInWestDoor)
		{
			_studioRoom.TriggerWestDoorAnimation();
		}
		else if (!isPlayerInWestDoor && _wasPlayerInWestDoor)
		{
			_studioRoom.TriggerWestDoorClose();
		}
		_wasPlayerInWestDoor = isPlayerInWestDoor;
	}
}