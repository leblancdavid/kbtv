using Godot;

/// <summary>
/// The red "ON AIR" sign above each room's door/stage, plus its key light. Shared by every room;
/// rooms pass in their own position, scale and light tuning. Previously these ~35 lines were
/// copy-pasted into each builder.
/// </summary>
public static class OnAirSignProp
{
	/// <summary>Sprite used by the sign.</summary>
	public const string TexturePath = "res://assets/tiles/props/on_air_sign.png";

	/// <summary>Z-index so the sign paints above the door/wall.</summary>
	public const int SpriteZIndex = 1001;

	/// <summary>Z-index so the sign's glow paints above the sprite.</summary>
	public const int LightZIndex = 1002;

	public static void Create(
		Node2D parent,
		Vector2 signPos,
		Vector2 scale,
		Color lightColor,
		float lightEnergy,
		float lightRadius,
		int lightMask
	)
	{
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
			Scale = scale,
			ZIndex = SpriteZIndex
		};
		onAirSign.Set("light_mask", lightMask);
		parent.AddChild(onAirSign);

		var onAirLight = RoomLightingBuilder.MakeLight(
			signPos, lightColor, lightEnergy, lightRadius, false, lightMask, 64, 64
		);
		onAirLight.ZIndex = LightZIndex;
		parent.AddChild(onAirLight);
	}
}