using Godot;

/// <summary>
/// The desk group: the desktop sprite, the boards that sit on it, the monitor/desk-lamp light
/// offsets that track it, and the screening interaction trigger below it. The whole group is
/// anchored at one grid cell and shifted down by <see cref="TableDropPixels"/>; the lights and
/// boards are offset from that shifted origin.
/// </summary>
public static class ControlTableGroupProp
{
	/// <summary>Anchor cell for the whole desk group (desktop + boards).</summary>
	public static GridPlacement TableGroup { get; } = (new Vector2I(6, 1), Vector2.Zero);

	/// <summary>Pixels the desk + boards are shifted down; the table lights track this too.</summary>
	public static int TableDropPixels { get; } = 10;

	/// <summary>Boards that sit on the desk, each relative to the table group's origin (after the drop).</summary>
	public static BoardSpec[] Boards { get; } =
	{
		("res://assets/tiles/props/phone_board.png", new Vector2(-68, -80)),
		("res://assets/tiles/props/sound_board.png", new Vector2(8, -88)),
		("res://assets/tiles/props/computer_station.png", new Vector2(72, -96)),
	};

	/// <summary>Monitor light offset relative to the table group origin (the monitor screen sits right of the desk).</summary>
	public static Vector2 MonitorLightOffset { get; } = new(64, -66);

	/// <summary>Desk-lamp light offset relative to the table group origin (the lamp sits left of the desk).</summary>
	public static Vector2 DeskLampLightOffset { get; } = new(-64, -60);

	/// <summary>Screening interaction trigger centered at this cell, below the desk.</summary>
	public static GridPlacement ScreeningTrigger { get; } = (new Vector2I(6, 2), new Vector2(0, 16));
	public static Vector2 ScreeningTriggerSize { get; } = new(240, 100);

	/// <summary>Builds the desk group (desktop + boards) at the anchored cell, dropping it by <see cref="TableDropPixels"/>.</summary>
	public static Node2D CreateTableGroup(
		Node2D parent,
		CastShadowSystem shadowSystem,
		ShaderMaterial depthShadowMaterial,
		IRoomSection roomSection,
		int lightMask
	)
	{
		return PropBuilder.CreateTableGroup(
			parent,
			TableGroup.Cell,
			shadowSystem,
			depthShadowMaterial,
			roomSection,
			lightMask,
			new Vector2(0, TableDropPixels),
			Boards
		);
	}

	/// <summary>World position of the shifted table origin, used so the table lights track the desk.</summary>
	public static Vector2 GetTablePosition(IRoomSection roomSection) =>
		TableGroup.ToWorld(roomSection) + new Vector2(0, TableDropPixels);
}