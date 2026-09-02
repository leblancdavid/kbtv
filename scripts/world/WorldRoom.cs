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
}