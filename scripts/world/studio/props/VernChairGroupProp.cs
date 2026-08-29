using Godot;

/// <summary>
/// Vern's chair and idle animation on the studio stage. Encapsulates the whole group: the
/// collider, the cast shadow and the looping idle sprite atlas slice (9 frames, 80x80).
/// </summary>
public static class VernChairGroupProp
{
	/// <summary>Vern's chair cell (he sits in front of his console).</summary>
	public static Vector2I VernChairCell { get; } = new(5, 2);

	/// <summary>Offset from the chair cell used when placing Vern's cast shadow.</summary>
	public static Vector2 VernShadowOffset { get; } = new(0, -40);

	/// <summary>Vern's chair collider.</summary>
	public static Vector2 VernColliderSize { get; } = new(32, 32);

	public static StaticBody2D? Build(
		Node2D parent,
		IRoomSection roomSection,
		CastShadowSystem shadows,
		int lightMask
	)
	{
		var vernIdleAtlas = GD.Load<Texture2D>("res://assets/sprites/characters/vern/vern_idle.png");
		var vernBaseTexture = GD.Load<Texture2D>("res://assets/sprites/characters/vern/vern.png");

		if (vernIdleAtlas == null || vernBaseTexture == null)
		{
			GD.PrintErr("VernChairGroupProp: Missing vern id texture");
			return null;
		}

		var body = new StaticBody2D { Name = "VernChairGroup" };
		body.Position = roomSection.GridToWorld(VernChairCell);

		if (shadows != null)
		{
			shadows.CreateShadowForObject(body, vernBaseTexture, VernShadowOffset);
		}

		var shape = new RectangleShape2D { Size = VernColliderSize };
		var collision = new CollisionShape2D { Shape = shape };
		collision.Position = new Vector2(0, -(shape.Size.Y * 0.5f));
		collision.AddToGroup("debug_prop_collision");
		body.AddChild(collision);

		var idleFrames = new SpriteFrames();
		var frameSize = new Vector2I(80, 80);
		for (int i = 0; i < 9; i++)
		{
			var region = new Rect2I(new Vector2I(i * frameSize.X, 0), frameSize);
			var frame = new AtlasTexture
			{
				Atlas = vernIdleAtlas,
				Region = region
			};
			idleFrames.AddFrame("default", frame);
		}
		idleFrames.SetAnimationSpeed("default", 3.0f);
		idleFrames.SetAnimationLoop("default", true);

		var vernSprite = new AnimatedSprite2D
		{
			SpriteFrames = idleFrames,
			Position = new Vector2(0, -0.5f * vernIdleAtlas.GetSize().Y),
			Scale = new Vector2(1.0f, 1.0f)
		};
		vernSprite.Set("light_mask", lightMask);
		vernSprite.Play("default");
		body.AddChild(vernSprite);

		body.ZIndex = (int)body.GlobalPosition.Y;
		parent.AddChild(body);

		return body;
	}
}
