using Godot;
using System.Collections.Generic;

public partial class RoomDebug : Node2D
{
	[Export] public bool DebugEnabled = false;

	private RoomBase _room;
	private WallSystem _wallSystem;
	private CastShadowSystem _shadowSystem;
	private Node2D _player;
	private bool _debugVisible;

	private PointLight2D _ceilingLight;
	private PointLight2D _monitorLight;
	private PointLight2D _deskLampLight;

	private readonly List<Rect2> _debugPropRects = new();
	private readonly List<Vector2> _debugPropPivots = new();
	private readonly List<Rect2> _debugOccluderRects = new();
	private Rect2 _debugPlayerRect;

	public List<Rect2> DebugPropRects => _debugPropRects;
	public List<Vector2> DebugPropPivots => _debugPropPivots;

	public void Initialize(RoomBase room, WallSystem wallSystem, CastShadowSystem shadowSystem, PointLight2D ceilingLight = null, PointLight2D monitorLight = null, PointLight2D deskLampLight = null)
	{
		_room = room;
		_wallSystem = wallSystem;
		_shadowSystem = shadowSystem;
		_player = room.Player;
		_ceilingLight = ceilingLight;
		_monitorLight = monitorLight;
		_deskLampLight = deskLampLight;

		Visible = DebugEnabled;
		_debugVisible = DebugEnabled;
	}

	public void Initialize(IRoomSection roomSection, WallSystem wallSystem, CastShadowSystem shadowSystem, PointLight2D ceilingLight = null, PointLight2D monitorLight = null, PointLight2D deskLampLight = null)
	{
		_wallSystem = wallSystem;
		_shadowSystem = shadowSystem;
		_player = roomSection.Player;
		_ceilingLight = ceilingLight;
		_monitorLight = monitorLight;
		_deskLampLight = deskLampLight;

		Visible = DebugEnabled;
		_debugVisible = DebugEnabled;
	}

	public void Toggle()
	{
		_debugVisible = !_debugVisible;
		Visible = _debugVisible;
		if (_room != null)
		{
			_room.GetNode<TileMapLayer>("GridDebugLayer").Visible = _debugVisible;
			_room.QueueRedraw();
		}
	}

	public void UpdatePropRects()
	{
		_debugPropRects.Clear();
		var debugNodes = GetTree().GetNodesInGroup("debug_prop_collision");
		foreach (var node in debugNodes)
		{
			if (node is not CollisionShape2D shape)
				continue;
			if (!IsInstanceValid(shape))
				continue;
			if (shape.Shape is not RectangleShape2D rectShape)
				continue;

			_debugPropRects.Add(new Rect2(
				shape.GlobalPosition - (rectShape.Size * 0.5f),
				rectShape.Size
			));
		}
	}

	public void UpdatePlayerRect()
	{
		if (_player == null)
			return;

		var playerCollision = _player.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (playerCollision?.Shape is not RectangleShape2D playerShape)
			return;

		var size = playerShape.Size;
		_debugPlayerRect = new Rect2(
			playerCollision.GlobalPosition - (size * 0.5f),
			size
		);
	}

	public void AddDebugPropPivot(Vector2 pivot)
	{
		_debugPropPivots.Add(pivot);
	}

	public override void _Draw()
	{
		if (!_debugVisible)
			return;

		var wallColor = new Color(1, 0, 0, 0.2f);
		var propColor = new Color(0, 1, 0, 0.2f);
		var playerColor = new Color(0, 0.5f, 1, 0.25f);
		var doorColor = new Color(1, 1, 0, 0.2f);
		var pivotColor = new Color(1, 0, 1, 0.9f);
		var lightColor = new Color(1, 1, 0, 0.9f);
		var boundsColor = new Color(1, 0.5f, 0, 0.8f);
		var shadowLineColor = new Color(0, 1, 1, 0.8f);
		var shadowDirColor = new Color(1, 0, 0, 0.8f);

		if (_wallSystem != null)
		{
			foreach (var rect in _wallSystem.DebugWallRects)
				DrawRect(ToLocalRect(rect), wallColor, true);

			if (_wallSystem.DebugDoorRect.Size != Vector2.Zero)
				DrawRect(ToLocalRect(_wallSystem.DebugDoorRect), doorColor, true);
		}

		foreach (var rect in _debugPropRects)
			DrawRect(ToLocalRect(rect), propColor, true);

		DrawRect(ToLocalRect(_debugPlayerRect), playerColor, true);

		if (_player != null)
			DrawCircle(ToLocal(_player.GlobalPosition), 3f, pivotColor);

		var lightPos = Vector2.Zero;
		float lightRadius = 200f;
		if (_ceilingLight != null)
		{
			lightPos = _ceilingLight.GlobalPosition;
			lightRadius = _shadowSystem != null ? _shadowSystem.LightRadius : 200f;
			DrawCircle(ToLocal(lightPos), 8f, lightColor);
			DrawArc(ToLocal(lightPos), lightRadius, 0, Mathf.Tau, 64, new Color(1, 1, 0, 0.3f), 2f);
		}
		if (_monitorLight != null)
			DrawCircle(ToLocal(_monitorLight.GlobalPosition), 6f, lightColor);
		if (_deskLampLight != null)
			DrawCircle(ToLocal(_deskLampLight.GlobalPosition), 6f, lightColor);

		foreach (var pivot in _debugPropPivots)
			DrawCircle(ToLocal(pivot), 3f, pivotColor);

		foreach (var rect in _debugOccluderRects)
			DrawRect(ToLocalRect(rect), new Color(0, 1, 1, 0.5f), true);

		if (_shadowSystem != null && _shadowSystem.ShadowRoomBounds.Size != Vector2.Zero)
		{
			DrawRect(ToLocalRect(_shadowSystem.ShadowRoomBounds), boundsColor, false);
		}

		if (_ceilingLight != null && lightPos != Vector2.Zero)
		{
			var shadowPivots = GetTree().GetNodesInGroup("shadow_pivots");
			foreach (Node node in shadowPivots)
			{
				if (node is not Node2D pivot)
					continue;

				var pivotPos = pivot.GlobalPosition;
				DrawLine(ToLocal(lightPos), ToLocal(pivotPos), shadowLineColor, 2f);

				var shadowDir = new Vector2(Mathf.Cos(pivot.Rotation), Mathf.Sin(pivot.Rotation));
				DrawLine(ToLocal(pivotPos), ToLocal(pivotPos + shadowDir * 30f), shadowDirColor, 3f);
			}

			if (_player != null)
			{
				var playerPos = _player.GlobalPosition;
				DrawLine(ToLocal(lightPos), ToLocal(playerPos), new Color(0, 1, 0, 0.8f), 3f);
			}
		}
	}

	private Rect2 ToLocalRect(Rect2 rect)
	{
		var topLeft = ToLocal(rect.Position);
		return new Rect2(topLeft, rect.Size);
	}
}
