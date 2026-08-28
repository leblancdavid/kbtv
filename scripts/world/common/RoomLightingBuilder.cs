using Godot;

/// <summary>
/// Shared factory for creating <see cref="PointLight2D"/> instances used by room builders.
/// All rooms render lighting from an oval gradient texture, so the light creation and
/// texture generation live in one place instead of being copy-pasted per builder.
///
/// Coverage gotcha: when a <c>PointLight2D.texture</c> is set, the <c>range</c> property is
/// IGNORED and the light's reach is <c>texture_size x texture_scale</c>. To make a room's
/// ceiling light cover its whole floor (including where the player stands), raise
/// <paramref name="textureScale"/> rather than <paramref name="radius"/>.
/// </summary>
public static class RoomLightingBuilder
{
	/// <summary>
	/// Default z-index for 2D point lights; anything with a higher z paints in front of them.
	/// </summary>
	public const int LightZIndex = 10;

	/// <summary>Falloff used for point lights that want an explicit soft drop-off (e.g. monitor, desk lamp, on-air).</summary>
	public const float DefaultFalloffRadius = 0.2f;

	/// <summary>
	/// Builds a configured <see cref="PointLight2D"/> with an oval gradient texture.
	/// When <paramref name="textureWidth"/>/<paramref name="textureHeight"/> are 0 the texture is
	/// sized from <paramref name="radius"/> (control room and studio use different scalings).
	/// </summary>
	public static PointLight2D MakeLight(
		Vector2 position,
		Color color,
		float energy,
		float radius,
		bool shadows,
		int itemCullMask = 1,
		int textureWidth = 0,
		int textureHeight = 0,
		float textureScale = 1.0f
	)
	{
		var light = new PointLight2D
		{
			Position = position,
			Color = color,
			Energy = energy,
			ShadowEnabled = shadows,
			ShadowColor = new Color(0, 0, 0, 0.3f),
			ZIndex = LightZIndex
		};
		light.Set("range_item_cull_mask", itemCullMask);

		var texture = OvalGradient(textureWidth, textureHeight, radius);
		light.Texture = texture;
		light.TextureScale = textureScale;
		light.Set("range", radius);

		return light;
	}

	/// <summary>
	/// Convenience for a ceiling light built on a fixed square texture whose size is then
	/// multiplied by <paramref name="textureScale"/> to reach the room's floor (see class doc).
	/// </summary>
	public static PointLight2D MakeCeilingLight(
		Vector2 position,
		Color color,
		float energy,
		float radius,
		bool shadows,
		int itemCullMask,
		int textureSize,
		float textureScale
	)
	{
		return MakeLight(position, color, energy, radius, shadows, itemCullMask, textureSize, textureSize, textureScale);
	}

	/// <summary>
	/// Generates an oval gradient image used as a point-light texture: a hard full-brightness
	/// core out to <c>falloffRadius</c> of the half-size, then a cubic fade to transparent at the edge.
	/// The image is sized from <paramref name="width"/>/<paramref name="height"/>, falling back to
	/// <paramref name="radius"/> (scaled by <paramref name="radiusScale"/>) when those are 0.
	/// </summary>
	public static ImageTexture OvalGradient(int width, int height, float radius, float falloffRadius = DefaultFalloffRadius, float radiusScale = 1.0f)
	{
		var sizeX = width > 0 ? width : (int)(radius * radiusScale);
		var sizeY = height > 0 ? height : (int)(radius * radiusScale);
		sizeX = Mathf.Max(sizeX, 48);
		sizeY = Mathf.Max(sizeY, 48);

		var image = Image.Create(sizeX, sizeY, false, Image.Format.Rgba8);

		var centerX = sizeX / 2f;
		var centerY = sizeY / 2f;

		for (int y = 0; y < sizeY; y++)
		{
			for (int x = 0; x < sizeX; x++)
			{
				var dx = (x - centerX) / centerX;
				var dy = (y - centerY) / centerY;
				var dist = Mathf.Sqrt(dx * dx + dy * dy);

				byte alpha;
				if (dist < falloffRadius)
				{
					alpha = 255;
				}
				else if (dist < 1.0f)
				{
					var t = (dist - falloffRadius) / (1.0f - falloffRadius);
					t = t * t * t;
					alpha = (byte)(255 * (1f - t));
				}
				else
				{
					alpha = 0;
				}

				image.SetPixel(x, y, new Color(1, 1, 1, alpha / 255f));
			}
		}

		return ImageTexture.CreateFromImage(image);
	}
}
