using Godot;

public static partial class PropBuilder
{
	public static Node2D CreateProp(
		Node2D parent,
		string texturePath,
		Vector2I gridCoords,
		Vector2 pixelOffset,
		bool collidable,
		Vector2 colliderSize,
		CastShadowSystem shadowSystem,
		ShaderMaterial depthShadowMaterial,
		Vector2 worldPosition,
		int lightMask = 1,
		bool createCastShadow = true
	)
	{
		var texture = GD.Load<Texture2D>(texturePath);
		if (texture == null)
		{
			GD.PrintErr($"PropBuilder: Missing texture {texturePath}");
			return null;
		}

		var root = collidable ? new StaticBody2D() : new Node2D();
		root.Position = worldPosition;

		var sprite = new Sprite2D
		{
			Texture = texture,
			Position = new Vector2(0, -texture.GetSize().Y * 0.5f)
		};
		sprite.Set("light_mask", lightMask);

		if (depthShadowMaterial != null)
			sprite.Material = depthShadowMaterial;

		root.AddChild(sprite);

		root.ZIndex = (int)root.GlobalPosition.Y;

		if (shadowSystem != null)
		{
			if (createCastShadow)
				shadowSystem.CreateShadowForObject(root, texture);
			else
				shadowSystem.CreateBaseShadowForObject(root, texture);
		}

		if (collidable && root is StaticBody2D body)
		{
			var shape = new RectangleShape2D { Size = colliderSize };
			var collision = new CollisionShape2D { Shape = shape };
			collision.Position = new Vector2(0, -(colliderSize.Y * 0.5f));
			collision.AddToGroup("debug_prop_collision");
			body.AddChild(collision);
		}

		parent.AddChild(root);

		return root;
	}

	public static Node2D CreateProp(
		Node2D parent,
		string texturePath,
		Vector2I gridCoords,
		Vector2 pixelOffset,
		bool collidable,
		Vector2 colliderSize,
		CastShadowSystem shadowSystem,
		ShaderMaterial depthShadowMaterial,
		RoomBase room,
		int lightMask = 1,
		bool createCastShadow = true
	)
	{
		var worldPos = room.GridToWorld(gridCoords) + pixelOffset;
		return CreateProp(parent, texturePath, gridCoords, pixelOffset, collidable, colliderSize, shadowSystem, depthShadowMaterial, worldPos, lightMask, createCastShadow);
	}

	public static Node2D CreateProp(
		Node2D parent,
		string texturePath,
		Vector2I gridCoords,
		Vector2 pixelOffset,
		bool collidable,
		Vector2 colliderSize,
		CastShadowSystem shadowSystem,
		ShaderMaterial depthShadowMaterial,
		IRoomSection roomSection,
		int lightMask = 1,
		bool createCastShadow = true
	)
	{
		var worldPos = roomSection.GridToWorld(gridCoords) + pixelOffset;
		return CreateProp(parent, texturePath, gridCoords, pixelOffset, collidable, colliderSize, shadowSystem, depthShadowMaterial, worldPos, lightMask, createCastShadow);
	}

	public static Node2D CreateProp(
		Node2D parent,
		string texturePath,
		Vector2I gridCoords,
		Vector2 pixelOffset,
		bool collidable,
		Vector2 colliderSize,
		CastShadowSystem shadowSystem,
		ShaderMaterial depthShadowMaterial,
		IRoomSection roomSection,
		bool createCastShadow = true
	)
	{
		var worldPos = roomSection.GridToWorld(gridCoords) + pixelOffset;
		return CreateProp(parent, texturePath, gridCoords, pixelOffset, collidable, colliderSize, shadowSystem, depthShadowMaterial, worldPos, 1, createCastShadow);
	}

	public static Node2D CreateTableGroup(
		Node2D parent,
		Vector2I gridCoords,
		CastShadowSystem shadowSystem,
		ShaderMaterial depthShadowMaterial,
		RoomBase room,
		int lightMask = 1,
		params (string texture, Vector2 offset)[] tabletops
	)
	{
		var worldPos = room.GridToWorld(gridCoords);
		return CreateTableGroupInternal(parent, worldPos, shadowSystem, depthShadowMaterial, lightMask, tabletops);
	}

	public static Node2D CreateTableGroup(
		Node2D parent,
		Vector2I gridCoords,
		CastShadowSystem shadowSystem,
		ShaderMaterial depthShadowMaterial,
		IRoomSection roomSection,
		int lightMask = 1,
		params (string texture, Vector2 offset)[] tabletops
	)
	{
		var worldPos = roomSection.GridToWorld(gridCoords);
		return CreateTableGroupInternal(parent, worldPos, shadowSystem, depthShadowMaterial, lightMask, tabletops);
	}

	private static Node2D CreateTableGroupInternal(
		Node2D parent,
		Vector2 worldPos,
		CastShadowSystem shadowSystem,
		ShaderMaterial depthShadowMaterial,
		int lightMask,
		params (string texture, Vector2 offset)[] tabletops
	)
	{
		var group = new Node2D { Name = "TableGroup" };
		group.Position = worldPos;
		parent.AddChild(group);

		var tableTexture = GD.Load<Texture2D>("res://assets/tiles/props/studio_table.png");
		if (tableTexture == null)
		{
			GD.PrintErr("PropBuilder: Missing table texture");
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

		group.ZIndex = (int)group.GlobalPosition.Y;

		if (shadowSystem != null)
			shadowSystem.CreateBaseShadowForObject(group, tableTexture);

		var tableBody = new StaticBody2D();
		var tableShape = new RectangleShape2D { Size = new Vector2(92, 14) };
		var tableCollision = new CollisionShape2D { Shape = tableShape };
		tableCollision.Position = new Vector2(0, -(tableShape.Size.Y * 0.5f));
		tableCollision.AddToGroup("debug_prop_collision");
		tableBody.AddChild(tableCollision);
		group.AddChild(tableBody);

		foreach (var (texturePath, offset) in tabletops)
		{
			CreateTabletopSprite(group, texturePath, offset, lightMask);
		}

		return group;
	}

	public static void CreateTabletopSprite(Node2D parent, string texturePath, Vector2 offset, int lightMask = 1)
	{
		var texture = GD.Load<Texture2D>(texturePath);
		if (texture == null)
		{
			GD.PrintErr($"PropBuilder: Missing tabletop texture {texturePath}");
			return;
		}

		var sprite = new Sprite2D
		{
			Texture = texture,
			Position = offset
		};
		sprite.Set("light_mask", lightMask);
		parent.AddChild(sprite);
	}
}
