using Godot;

/// <summary>
/// The red "ON AIR" sign above each room's door/stage, plus its key light. Shared by every room;
/// each room's position, scale and light tuning is authored here so builders stay thin.
/// </summary>
public static class OnAirSignProp
{
	/// <summary>Sprite used by the sign.</summary>
	public const string TexturePath = "res://assets/tiles/props/on_air_sign.png";

	/// <summary>Z-index so the sign paints above the door/wall.</summary>
	public const int SpriteZIndex = 1001;

	/// <summary>Z-index so the sign's glow paints above the sprite.</summary>
	public const int LightZIndex = 1002;

	/// <summary>Per-room sign placement and light tuning.</summary>
	public readonly record struct OnAirSignConfig(
		Vector2 FromAnchor,
		Vector2 Scale,
		Color LightColor,
		float LightEnergy,
		float LightRadius
	);

	/// <summary>Control room: sign above the north door (offset from the grid anchor).</summary>
	public static OnAirSignConfig ControlRoom { get; } =
		new(new Vector2(32, -112), new Vector2(0.75f, 1.0f), new Color(1f, 0.1f, 0.1f), 0.5f, 120f);

	/// <summary>Studio: sign above the stage (offset from the grid anchor).</summary>
	public static OnAirSignConfig Studio { get; } =
		new(new Vector2(224, -112), new Vector2(0.75f, 1.0f), new Color(1f, 0.1f, 0.1f), 0.5f, 120f);

	public static void Create(Node2D parent, OnAirSignConfig config, Vector2 gridAnchor, int lightMask)
	{
		var signPos = gridAnchor + config.FromAnchor;

		var onAirTexture = GD.Load<Texture2D>(TexturePath);
		if (onAirTexture == null)
		{
			GD.PrintErr("OnAirSignProp: Missing on_air_sign.png texture");
			return;
		}

		var onAirSign = new Sprite2D
		{
			Texture = onAirTexture,
			Position = signPos,
			Scale = config.Scale,
			ZIndex = SpriteZIndex
		};
		onAirSign.Set("light_mask", lightMask);
		parent.AddChild(onAirSign);

		var onAirLight = RoomLightingBuilder.MakeLight(
			signPos, config.LightColor, config.LightEnergy, config.LightRadius, false, lightMask, 64, 64
		);
		onAirLight.ZIndex = LightZIndex;
		parent.AddChild(onAirLight);
	}
}