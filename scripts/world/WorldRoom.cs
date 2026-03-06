using Godot;
using KBTV.Data;
using KBTV.Core;

public partial class WorldRoom : Node2D
{
	[ExportGroup("Grid Settings")]
	[Export] public int GridWidth = 14;

	[ExportGroup("TileMap")]
	[Export] public int FloorSourceId = 0;
	[Export] public int GridDebugSourceId = 6;

	public Node2D PropSort = null!;
	public CharacterBody2D Player = null!;

	public CastShadowSystem ControlShadows => _controlBuilder.GetShadows();
	public CastShadowSystem StudioShadows => _studioBuilder.GetShadows();

	private ControlRoomBuilder _controlBuilder = null!;
	private StudioBuilder _studioBuilder = null!;

	public void SetPlayer(CharacterBody2D player)
	{
		Player = player;
		_controlBuilder.SetPlayer(player);
		_studioBuilder.SetPlayer(player);
	}

	public Vector2 ControlRoomGridToWorld(Vector2I gridPos)
	{
		return _controlBuilder.GridToWorld(gridPos);
	}

	public Vector2 StudioGridToWorld(Vector2I gridPos)
	{
		return _studioBuilder.GridToWorld(gridPos);
	}

	public Rect2 GetStudioBounds()
	{
		return _studioBuilder.GetFloorBounds();
	}

	public override void _Ready()
	{
		PropSort = new Node2D { Name = "PropSort", YSortEnabled = true };
		AddChild(PropSort);

		_controlBuilder = new ControlRoomBuilder();
		_controlBuilder.Build(this);

		_studioBuilder = new StudioBuilder();
		_studioBuilder.Build(this);

		// Register bounds with RoomStateManager
		var roomState = ServiceRegistry.Instance?.Get<RoomStateManager>();
		if (roomState != null)
		{
			roomState.SetControlRoomBounds(_controlBuilder.GetFloorBounds());
			roomState.SetStudioBounds(_studioBuilder.GetFloorBounds());
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_select"))
		{
			_controlBuilder.ToggleDebug();
			_studioBuilder.ToggleDebug();
		}
	}

	public override void _Process(double delta)
	{
		var vernStats = GetVernStats();
		_controlBuilder.Update(this, delta);
		_studioBuilder.Update(this, delta, vernStats);

		UpdatePlayerLightMask();
	}

	private VernStats? GetVernStats()
	{
		return ServiceRegistry.Instance?.VernStats;
	}

	private void UpdatePlayerLightMask()
	{
		if (Player == null) return;

		var sprite = Player.GetNodeOrNull<Sprite2D>("Sprite2D");
		if (sprite == null) return;

		var playerPos = Player.GlobalPosition;
		var studioBounds = _studioBuilder.GetFloorBounds();
		var controlBounds = _controlBuilder.GetFloorBounds();

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
