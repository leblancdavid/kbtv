using Godot;

public static partial class PropBuilder
{
	/// <summary>
	/// Scans the bottom band of a texture's alpha channel and returns the tight bounding box
	/// of non-transparent pixels. Used to derive the visible floor footprint of an oblique/cabinet
	/// projection sprite so collision matches the actual base of the prop rather than the full sprite.
	///
	/// Result is in IMAGE coordinates (origin top-left). To position the collider on the prop's
	/// bottom-anchored sprite (sprite.Position = (0, -textureHeight/2)), translate by
	/// -texture.GetSize() using <see cref="ImageFootprintToSpriteLocal"/>.
	///
	/// Returns Rect2(0, 0, 0, 0) if the texture is null, has no alpha, or the band is fully transparent.
	/// </summary>
	/// <param name="texture">Source sprite texture.</param>
	/// <param name="floorScanHeight">Pixel rows to scan from the bottom of the image. Clamped to texture height.</param>
	/// <param name="alphaThreshold">0-255 alpha cutoff for "opaque". Defaults to 128.</param>
	public static Rect2 GetBaseFootprint(Texture2D texture, int floorScanHeight = 16, byte alphaThreshold = 128)
	{
		if (texture == null)
			return new Rect2(0, 0, 0, 0);

		var image = texture.GetImage();
		if (image == null)
			return new Rect2(0, 0, 0, 0);

		var size = texture.GetSize();
		var width = (int)size.X;
		var height = (int)size.Y;

		if (width <= 0 || height <= 0)
			return new Rect2(0, 0, 0, 0);

		var scanHeight = Mathf.Clamp(floorScanHeight, 1, height);
		var scanStartY = height - scanHeight;

		int minX = width;
		int maxX = -1;
		int minY = height;
		int maxY = -1;

		for (int y = scanStartY; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				var pixel = image.GetPixel(x, y);
				if (pixel.A * 255f >= alphaThreshold)
				{
					if (x < minX) minX = x;
					if (x > maxX) maxX = x;
					if (y < minY) minY = y;
					if (y > maxY) maxY = y;
				}
			}
		}

		if (maxX < 0)
			return new Rect2(0, 0, 0, 0);

		return new Rect2(minX, minY, maxX - minX + 1, maxY - minY + 1);
	}

	/// <summary>
	/// Translates an image-space Rect2 (origin top-left) into sprite-local coordinates
	/// for a bottom-anchored Sprite2D (sprite.Position = (0, -textureHeight/2)).
	/// </summary>
	public static Rect2 ImageFootprintToSpriteLocal(Rect2 imageRect, Vector2 textureSize)
	{
		return new Rect2(
			imageRect.Position.X - textureSize.X * 0.5f,
			imageRect.Position.Y - textureSize.Y,
			imageRect.Size.X,
			imageRect.Size.Y
		);
	}

	/// <summary>
	/// Computes the root-relative CENTER position for a <see cref="CollisionShape2D"/> whose
	/// rectangle matches <paramref name="imageFootprint"/> on a bottom-anchored sprite
	/// (sprite.Position = (0, -textureHeight/2)). The collider's bottom edge ends up at the
	/// sprite's bottom-anchor (the floor), and the collider is horizontally centered on the
	/// footprint's center.
	/// </summary>
	public static Vector2 FootprintToCollisionCenter(Rect2 imageFootprint, Vector2 textureSize)
	{
		return new Vector2(
			imageFootprint.Position.X + imageFootprint.Size.X * 0.5f - textureSize.X * 0.5f,
			imageFootprint.Position.Y + imageFootprint.Size.Y * 0.5f - textureSize.Y
		);
	}

	/// <summary>
	/// Creates a prop and derives its collision shape from the sprite's alpha channel,
	/// scanning the bottom <paramref name="floorScanHeight"/> pixels for the tight base footprint.
	/// Pass a non-null <paramref name="colliderOverride"/> (x, y, w, h in sprite-local coords)
	/// to skip auto-derivation for props that need a custom shape (e.g. a table surface strip).
	/// </summary>
	public static Node2D CreatePropAutoCollider(
		Node2D parent,
		string texturePath,
		Vector2I gridCoords,
		Vector2 pixelOffset,
		CastShadowSystem shadowSystem,
		ShaderMaterial depthShadowMaterial,
		Vector2 worldPosition,
		int lightMask = 1,
		bool createCastShadow = true,
		int floorScanHeight = 16,
		Vector4? colliderOverride = null
	)
	{
		var texture = GD.Load<Texture2D>(texturePath);
		if (texture == null)
		{
			GD.PrintErr($"PropBuilder: Missing texture {texturePath}");
			return null;
		}

		var textureSize = texture.GetSize();
		var root = new StaticBody2D { Position = worldPosition };

		var sprite = new Sprite2D
		{
			Texture = texture,
			Position = new Vector2(0, -textureSize.Y * 0.5f)
		};
		sprite.Set("light_mask", lightMask);

		if (depthShadowMaterial != null)
			sprite.Material = depthShadowMaterial;

		root.AddChild(sprite);

		root.ZIndex = (int)root.GlobalPosition.Y;

		RectangleShape2D shape;
		Vector2 collisionPos;

		if (colliderOverride.HasValue)
		{
			var ov = colliderOverride.Value;
			shape = new RectangleShape2D { Size = new Vector2(ov.Z, ov.W) };
			collisionPos = new Vector2(ov.X, ov.Y);
		}
		else
		{
			var footprint = GetBaseFootprint(texture, floorScanHeight);
			if (footprint.Size == Vector2.Zero)
			{
				GD.PrintErr($"PropBuilder: No opaque pixels found in floor band for {texturePath}");
				return null;
			}
			shape = new RectangleShape2D { Size = footprint.Size };
			collisionPos = FootprintToCollisionCenter(footprint, textureSize);
		}

		var collision = new CollisionShape2D { Shape = shape, Position = collisionPos };
		collision.AddToGroup("debug_prop_collision");
		root.AddChild(collision);

		parent.AddChild(root);

		return root;
	}

	public static Node2D CreatePropAutoCollider(
		Node2D parent,
		string texturePath,
		Vector2I gridCoords,
		Vector2 pixelOffset,
		CastShadowSystem shadowSystem,
		ShaderMaterial depthShadowMaterial,
		IRoomSection roomSection,
		int lightMask = 1,
		bool createCastShadow = true,
		int floorScanHeight = 16,
		Vector4? colliderOverride = null
	)
	{
		var worldPos = roomSection.GridToWorld(gridCoords) + pixelOffset;
		return CreatePropAutoCollider(parent, texturePath, gridCoords, pixelOffset, shadowSystem, depthShadowMaterial, worldPos, lightMask, createCastShadow, floorScanHeight, colliderOverride);
	}

	public static Node2D CreatePropAutoCollider(
		Node2D parent,
		string texturePath,
		Vector2I gridCoords,
		Vector2 pixelOffset,
		CastShadowSystem shadowSystem,
		ShaderMaterial depthShadowMaterial,
		RoomBase room,
		int lightMask = 1,
		bool createCastShadow = true,
		int floorScanHeight = 16,
		Vector4? colliderOverride = null
	)
	{
		var worldPos = room.GridToWorld(gridCoords) + pixelOffset;
		return CreatePropAutoCollider(parent, texturePath, gridCoords, pixelOffset, shadowSystem, depthShadowMaterial, worldPos, lightMask, createCastShadow, floorScanHeight, colliderOverride);
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

		var tableBody = new StaticBody2D();
		// Surface strip: 92 wide (1 tile narrower on each side than the 128px sprite),
		// 10 tall, sitting 1 tile above the floor so the player can walk close to the table.
		var tableShape = new RectangleShape2D { Size = new Vector2(108, 10) };
		var tableCollision = new CollisionShape2D { Shape = tableShape };
		tableCollision.Position = new Vector2(0, -(tableShape.Size.Y * 0.5f) - 10);
		tableCollision.AddToGroup("debug_prop_collision");
		tableBody.AddChild(tableCollision);
		group.AddChild(tableBody);

		foreach (var (texturePath, offset) in tabletops)
		{
			CreateTabletopSprite(group, texturePath, offset, lightMask, depthShadowMaterial);
		}

		return group;
	}

	public static void CreateTabletopSprite(Node2D parent, string texturePath, Vector2 offset, int lightMask = 1, ShaderMaterial? material = null)
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
		if (material != null)
			sprite.Material = material;
		parent.AddChild(sprite);
	}
}
