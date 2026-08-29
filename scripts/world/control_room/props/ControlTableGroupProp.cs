using Godot;

/// <summary>
/// The desk group: the desktop sprite, the player collider, the boards that sit on it, the
/// monitor/desk-lamp light offsets that track it, and the screening interaction trigger below it.
/// The whole group is anchored at one grid cell and shifted down by <see cref="TableDropPixels"/>;
/// the lights and boards are offset from that shifted origin.
/// </summary>
public static class ControlTableGroupProp
{
	/// <summary>Anchor cell for the whole desk group (desktop + boards).</summary>
	public static GridPlacement TableGroup { get; } = (new Vector2I(6, 1), Vector2.Zero);

	/// <summary>Pixels the desk + boards are shifted down; the table lights track this too.</summary>
	public static int TableDropPixels { get; } = 10;

	/// <summary>Surface sprite that forms the desk top.</summary>
	public const string TableTexturePath = "res://assets/tiles/props/studio_table.png";

	/// <summary>
	/// The desk's player collider: a surface strip 108 wide (1 tile narrower on each side than
	/// the 128px sprite) and 10 tall.
	/// </summary>
	public static Vector2 ColliderSize { get; } = new(256, 24);

	/// <summary>How high the collider sits above the floor, letting the player walk close to the desk.</summary>
	public static float ColliderLift { get; } = 16;

	/// <summary>Boards that sit on the desk, each relative to the table group's origin (after the drop).</summary>
	public static BoardSpec[] Boards { get; } =
	{
		("res://assets/tiles/props/phone_board.png", new Vector2(-68, -60)),
		("res://assets/tiles/props/sound_board.png", new Vector2(8, -60)),
		("res://assets/tiles/props/computer_station.png", new Vector2(72, -76)),
	};

	/// <summary>Monitor light offset relative to the table group origin (the monitor screen sits right of the desk).</summary>
	public static Vector2 MonitorLightOffset { get; } = new(64, -66);

	/// <summary>Desk-lamp light offset relative to the table group origin (the lamp sits left of the desk).</summary>
	public static Vector2 DeskLampLightOffset { get; } = new(-64, -60);

	/// <summary>Screening interaction trigger centered at this cell, below the desk.</summary>
	public static GridPlacement ScreeningTrigger { get; } = (new Vector2I(6, 2), new Vector2(0, 16));
	public static Vector2 ScreeningTriggerSize { get; } = new(240, 100);

	/// <summary>Builds the desk group (desktop + boards + collider) at the anchored cell, dropping it by <see cref="TableDropPixels"/>.</summary>
	public static Node2D CreateTableGroup(
		Node2D parent,
		ShaderMaterial depthShadowMaterial,
		IRoomSection roomSection,
		int lightMask
	)
	{
		var worldPos = TableGroup.ToWorld(roomSection) + new Vector2(0, TableDropPixels);

		var group = new Node2D { Name = "TableGroup", Position = worldPos };
		group.ZIndex = (int)group.GlobalPosition.Y;
		parent.AddChild(group);

		var tableTexture = GD.Load<Texture2D>(TableTexturePath);
		if (tableTexture == null)
		{
			GD.PrintErr($"ControlTableGroupProp: Missing table texture {TableTexturePath}");
			return group;
		}

		var tableSprite = new Sprite2D
		{
			Texture = tableTexture,
			Position = new Vector2(0, -tableTexture.GetSize().Y * 0.5f)
		};
		tableSprite.Set("light_mask", lightMask);
		if (depthShadowMaterial != null)
			tableSprite.Material = depthShadowMaterial;
		group.AddChild(tableSprite);

		var tableBody = new StaticBody2D();
		var tableShape = new RectangleShape2D { Size = ColliderSize };
		var tableCollision = new CollisionShape2D { Shape = tableShape };
		tableCollision.Position = new Vector2(0, -(ColliderSize.Y * 0.5f) - ColliderLift);
		tableCollision.AddToGroup("debug_prop_collision");
		tableBody.AddChild(tableCollision);
		group.AddChild(tableBody);

		foreach (var board in Boards)
		{
			PropBuilder.CreateTabletopSprite(group, board.TexturePath, board.Offset, lightMask, depthShadowMaterial);
		}

		return group;
	}

	/// <summary>World position of the shifted table origin, used so the table lights track the desk.</summary>
	public static Vector2 GetTablePosition(IRoomSection roomSection) =>
		TableGroup.ToWorld(roomSection) + new Vector2(0, TableDropPixels);
}
