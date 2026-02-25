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
		Vector2 worldPosition
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

		if (depthShadowMaterial != null)
			sprite.Material = depthShadowMaterial;

		root.AddChild(sprite);

		if (shadowSystem != null)
			shadowSystem.CreateShadowForObject(root, texture);

		if (collidable && root is StaticBody2D body)
		{
			var shape = new RectangleShape2D { Size = colliderSize };
			var collision = new CollisionShape2D { Shape = shape };
			collision.Position = new Vector2(0, -(colliderSize.Y * 0.5f));
			collision.AddToGroup("debug_prop_collision");
			body.AddChild(collision);
		}

		root.ZIndex = (int)root.GlobalPosition.Y;
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
		RoomBase room
	)
	{
		var worldPos = room.GridToWorld(gridCoords) + pixelOffset;
		return CreateProp(parent, texturePath, gridCoords, pixelOffset, collidable, colliderSize, shadowSystem, depthShadowMaterial, worldPos);
	}

	public static Node2D CreateTableGroup(
		Node2D parent,
		Vector2I gridCoords,
		CastShadowSystem shadowSystem,
		ShaderMaterial depthShadowMaterial,
		RoomBase room,
		params (string texture, Vector2 offset)[] tabletops
	)
	{
		var group = new Node2D { Name = "TableGroup" };
		group.Position = room.GridToWorld(gridCoords);
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

		if (depthShadowMaterial != null)
			tableSprite.Material = depthShadowMaterial;

		group.AddChild(tableSprite);

		if (shadowSystem != null)
			shadowSystem.CreateShadowForObject(group, tableTexture);

		var tableBody = new StaticBody2D();
		var tableShape = new RectangleShape2D { Size = new Vector2(92, 14) };
		var tableCollision = new CollisionShape2D { Shape = tableShape };
		tableCollision.Position = new Vector2(0, -(tableShape.Size.Y * 0.5f));
		tableCollision.AddToGroup("debug_prop_collision");
		tableBody.AddChild(tableCollision);
		group.AddChild(tableBody);

		group.ZIndex = (int)group.GlobalPosition.Y;

		foreach (var (texturePath, offset) in tabletops)
		{
			CreateTabletopSprite(group, texturePath, offset);
		}

		return group;
	}

	public static void CreateTabletopSprite(Node2D parent, string texturePath, Vector2 offset)
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
		parent.AddChild(sprite);
	}
}
