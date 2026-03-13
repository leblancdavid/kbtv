using Godot;
using System.Collections.Generic;

public partial class CastShadowSystem : Node
{
	[ExportGroup("Shadow Settings")]
	[Export] public float ShadowLerpFactor = 0.12f;
	[Export] public float LightRadius = 450f;
	[Export] public float ShadowOpacity = 0.3f;
	[Export] public string GroupName = "shadow_pivots";

	[ExportGroup("Shader Parameters")]
	[Export] public float BlurAmount = 0.3f;
	[Export] public float BaseBlurAmount = 0.5f;
	[Export] public float GradientFadeHeight = 0.4f;

	private RoomBase _room;
	private PointLight2D _lightSource;
	private ShaderMaterial _depthShadowMaterial;
	private ShaderMaterial _shadowMaterial;
	private ShaderMaterial _playerShadowMaterial;
	private ShaderMaterial _baseShadowMaterial;
	private Rect2 _shadowRoomBounds;

	private readonly Dictionary<Node2D, Sprite2D> _pivotToShadowSprite = new();
	private readonly List<Sprite2D> _baseShadowSprites = new();

	public ShaderMaterial DepthShadowMaterial => _depthShadowMaterial;
	public Rect2 ShadowRoomBounds => _shadowRoomBounds;

	public void Initialize(RoomBase room, PointLight2D lightSource)
	{
		_room = room;
		_lightSource = lightSource;

		LoadShaders();
		SetShadowRoomBounds();
	}

	public void Initialize(IRoomSection roomSection, PointLight2D lightSource)
	{
		_lightSource = lightSource;
		LoadShaders();

		var gridWidth = roomSection.GridWidth;
		var gridHeight = roomSection.GridHeight;
		var floorLayer = roomSection.FloorLayer;
		var gridOffset = roomSection.GridOffset;

		// Calculate bounds based on actual wall positions, centered on wall bottoms
		// Walls are at: north y=-1, south y=gridHeight, west x=-1, east x=gridWidth
		// Add half-tile (8px) to go past the wall into the room
		const float pastWall = 8f;
		const float margin = 4f;

		// North wall bottom center (y=-1 grid row, shift up by pastWall into room)
		var northWallBottom = floorLayer.MapToLocal(new Vector2I(0, -1)) + gridOffset + new Vector2(0, -pastWall);
		// South wall bottom center (y=gridHeight grid row, shift up by pastWall into room)
		var southWallBottom = floorLayer.MapToLocal(new Vector2I(0, gridHeight)) + gridOffset + new Vector2(0, -pastWall);
		// West wall right edge (x=-1 grid column, shift right by pastWall into room)
		var westWallRight = floorLayer.MapToLocal(new Vector2I(-1, 0)) + gridOffset + new Vector2(pastWall, 0);
		// East wall left edge (x=gridWidth grid column, shift left by pastWall into room)
		var eastWallLeft = floorLayer.MapToLocal(new Vector2I(gridWidth, 0)) + gridOffset + new Vector2(-pastWall, 0);

		// Apply margin
		var roomOrigin = new Vector2(westWallRight.X - margin, northWallBottom.Y - margin);
		var roomWidth = (eastWallLeft.X - westWallRight.X) + margin * 2;
		var roomHeight = (southWallBottom.Y - northWallBottom.Y) + margin * 2;

		_shadowRoomBounds = new Rect2(roomOrigin.X, roomOrigin.Y, roomWidth, roomHeight);

		var roomBounds = new Vector4(roomOrigin.X, roomOrigin.Y, roomWidth, roomHeight);

		if (_shadowMaterial != null)
			_shadowMaterial.SetShaderParameter("room_bounds", roomBounds);
		if (_baseShadowMaterial != null)
			_baseShadowMaterial.SetShaderParameter("room_bounds", roomBounds);
		if (_playerShadowMaterial != null)
			_playerShadowMaterial.SetShaderParameter("room_bounds", roomBounds);
	}

	private void LoadShaders()
	{
		var depthShader = GD.Load<Shader>("res://shaders/depth_shadow.gdshader");
		if (depthShader != null)
		{
			_depthShadowMaterial = new ShaderMaterial();
			_depthShadowMaterial.Shader = depthShader;
			_depthShadowMaterial.SetShaderParameter("light_radius", LightRadius);
			_depthShadowMaterial.SetShaderParameter("max_brightness", 4.0f);
			_depthShadowMaterial.SetShaderParameter("shadow_factor", 0.8f);
		}

		var shadowShader = GD.Load<Shader>("res://shaders/shadow_blur.gdshader");
		if (shadowShader != null)
		{
			_shadowMaterial = new ShaderMaterial();
			_shadowMaterial.Shader = shadowShader;
			_shadowMaterial.SetShaderParameter("blur_amount", BlurAmount);
			_shadowMaterial.SetShaderParameter("shadow_color", new Color(0, 0, 0, ShadowOpacity));
			_shadowMaterial.SetShaderParameter("gradient_fade_height", GradientFadeHeight);

			_baseShadowMaterial = new ShaderMaterial();
			_baseShadowMaterial.Shader = shadowShader;
			_baseShadowMaterial.SetShaderParameter("blur_amount", BaseBlurAmount);
			_baseShadowMaterial.SetShaderParameter("shadow_color", new Color(0, 0, 0, ShadowOpacity));
			_baseShadowMaterial.SetShaderParameter("gradient_fade_height", GradientFadeHeight);
			_baseShadowMaterial.SetShaderParameter("gradient_at_top", true);
		}

		var playerShadowShader = GD.Load<Shader>("res://shaders/shadow_blur_player.gdshader");
		if (playerShadowShader != null)
		{
			_playerShadowMaterial = new ShaderMaterial();
			_playerShadowMaterial.Shader = playerShadowShader;
			_playerShadowMaterial.SetShaderParameter("blur_amount", BlurAmount);
			_playerShadowMaterial.SetShaderParameter("shadow_color", new Color(0, 0, 0, ShadowOpacity));
			_playerShadowMaterial.SetShaderParameter("gradient_fade_height", GradientFadeHeight);
			// gradient_at_top defaults to false in shader; keep as false for player
		}
	}

	private void SetShadowRoomBounds()
	{
		// Calculate bounds based on actual wall positions, centered on wall bottoms
		// Walls are at: north y=-1, south y=gridHeight, west x=-1, east x=gridWidth
		// Add half-tile (8px) to go past the wall into the room
		const float pastWall = 8f;
		const float margin = 4f;

		var floorLayer = _room.FloorLayer;
		var gridOffset = _room.GridOffset;
		var gridWidth = _room.GridWidth;
		var gridHeight = _room.GridHeight;

		// North wall bottom center (y=-1 grid row, shift up by pastWall into room)
		var northWallBottom = floorLayer.MapToLocal(new Vector2I(0, -1)) + gridOffset + new Vector2(0, -pastWall);
		// South wall bottom center (y=gridHeight grid row, shift up by pastWall into room)
		var southWallBottom = floorLayer.MapToLocal(new Vector2I(0, gridHeight)) + gridOffset + new Vector2(0, -pastWall);
		// West wall right edge (x=-1 grid column, shift right by pastWall into room)
		var westWallRight = floorLayer.MapToLocal(new Vector2I(-1, 0)) + gridOffset + new Vector2(pastWall, 0);
		// East wall left edge (x=gridWidth grid column, shift left by pastWall into room)
		var eastWallLeft = floorLayer.MapToLocal(new Vector2I(gridWidth, 0)) + gridOffset + new Vector2(-pastWall, 0);

		// Apply margin
		var roomOrigin = new Vector2(westWallRight.X - margin, northWallBottom.Y - margin);
		var roomWidth = (eastWallLeft.X - westWallRight.X) + margin * 2;
		var roomHeight = (southWallBottom.Y - northWallBottom.Y) + margin * 2;

		_shadowRoomBounds = new Rect2(roomOrigin.X, roomOrigin.Y, roomWidth, roomHeight);

		var roomBounds = new Vector4(roomOrigin.X, roomOrigin.Y, roomWidth, roomHeight);

		if (_shadowMaterial != null)
			_shadowMaterial.SetShaderParameter("room_bounds", roomBounds);
		if (_baseShadowMaterial != null)
			_baseShadowMaterial.SetShaderParameter("room_bounds", roomBounds);
		if (_playerShadowMaterial != null)
			_playerShadowMaterial.SetShaderParameter("room_bounds", roomBounds);
	}

	public void CreateShadowForObject(Node2D root, Texture2D texture, Vector2? offset = null, bool createOvalBase = false, int? zIndex = null)
	{
		// Check if root already has a shadow - remove it first to prevent duplicates
		var existingPivot = root.GetNodeOrNull<Node2D>("ShadowPivot");
		if (existingPivot != null)
		{
			RemoveShadowForObject(root);
		}

		var spriteSize = texture.GetSize();
		var pivotOffset = offset ?? new Vector2(0, -3);

		var shadowPivot = new Node2D { Name = "ShadowPivot", Position = pivotOffset };
		shadowPivot.AddToGroup(GroupName);
		root.AddChild(shadowPivot);

		// Choose material: use player-specific for Player root, else default
		ShaderMaterial? material = null;
		bool isPlayer = root.GetType().Name == "Player";
		if (isPlayer && _playerShadowMaterial != null)
		{
			material = _playerShadowMaterial.Duplicate() as ShaderMaterial;
		}
		else if (_shadowMaterial != null)
		{
			material = _shadowMaterial.Duplicate() as ShaderMaterial;
		}

		if (material != null)
		{
			material.SetShaderParameter("shadow_world_position", root.GlobalPosition);

			// Set room bounds for shader clipping
			var roomBoundsVec = new Vector4(
				_shadowRoomBounds.Position.X,
				_shadowRoomBounds.Position.Y,
				_shadowRoomBounds.Size.X,
				_shadowRoomBounds.Size.Y
			);
			material.SetShaderParameter("room_bounds", roomBoundsVec);
		}

		var shadowSprite = new Sprite2D
		{
			Texture = texture,
			Material = material,
			Position = new Vector2(0, -spriteSize.Y * 0.5f),
			FlipV = false,
			Modulate = new Color(0, 0, 0, ShadowOpacity),
			ZAsRelative = true,
			ZIndex = -1
		};
		shadowPivot.AddChild(shadowSprite);
		_pivotToShadowSprite[shadowPivot] = shadowSprite;

		if (createOvalBase)
			CreateOvalBaseShadow(root, texture, pivotOffset, zIndex);
		else
			CreateBaseShadowForObject(root, texture, zIndex);
	}

	public void CreateBaseShadowForObject(Node2D root, Texture2D texture, int? zIndex = null)
	{
		var originalImage = texture.GetImage();
		var regionHeight = 8;
		var region = new Rect2I(0, originalImage.GetHeight() - regionHeight, originalImage.GetWidth(), regionHeight);
		var bottomImage = originalImage.GetRegion(region);
		var bottomTexture = ImageTexture.CreateFromImage(bottomImage);

		Vector2 shadowSpritePos = new Vector2(0, -2);

		var material = _baseShadowMaterial?.Duplicate() as ShaderMaterial;
		if (material != null)
		{
			// Account for sprite's local position to get actual world position
			material.SetShaderParameter("shadow_world_position", root.GlobalPosition + shadowSpritePos);

			var roomBoundsVec = new Vector4(
				_shadowRoomBounds.Position.X,
				_shadowRoomBounds.Position.Y,
				_shadowRoomBounds.Size.X,
				_shadowRoomBounds.Size.Y
			);
			material.SetShaderParameter("room_bounds", roomBoundsVec);
		}

		var baseShadowSprite = new Sprite2D
		{
			Texture = bottomTexture,
			Material = material,
			Position = shadowSpritePos,
			FlipV = false,
			Modulate = new Color(0, 0, 0, ShadowOpacity),
			ZAsRelative = true,
			ZIndex = -1
		};
		root.AddChild(baseShadowSprite);
		_baseShadowSprites.Add(baseShadowSprite);
	}

	private Texture2D CreateOvalShadowTexture(int width, int height)
	{
		// Add padding for blur to avoid edge clipping
		const int padding = 4;
		int texWidth = width + padding * 2;
		int texHeight = height + padding * 2;

		var img = Image.Create(texWidth, texHeight, false, Image.Format.Rgba8);
		img.Fill(new Color(0, 0, 0, 0)); // transparent background

		float centerX = texWidth / 2f;
		float centerY = texHeight / 2f;
		float radiusX = width / 2f;
		float radiusY = height / 2f;

		for (int y = 0; y < texHeight; y++)
		{
			for (int x = 0; x < texWidth; x++)
			{
				float dx = (x - centerX) / radiusX;
				float dy = (y - centerY) / radiusY;
				float dist = dx * dx + dy * dy;
				if (dist <= 1.0f)
				{
					// Solid black with uniform ShadowOpacity
					img.SetPixel(x, y, new Color(0, 0, 0, ShadowOpacity));
				}
			}
		}

		var texture = ImageTexture.CreateFromImage(img);
		return texture;
	}

	private void CreateOvalBaseShadow(Node2D root, Texture2D referenceTexture, Vector2 pivotOffset, int? zIndex = null)
	{
		var refSize = referenceTexture.GetSize();
		int ovalWidth = (int)(refSize.X * 0.28f); // keep width
		int ovalHeight = (int)(refSize.Y * 0.15f); // more oval (taller)
												   // Ensure minimum size
		ovalWidth = Mathf.Max(ovalWidth, 8);
		ovalHeight = Mathf.Max(ovalHeight, 4);

		var ovalTexture = CreateOvalShadowTexture(ovalWidth, ovalHeight);

		// Position: base shadow sits on ground directly under the player.
		// Texture has 4px padding; place top-left at -4 so visible oval starts at Y=0.
		Vector2 basePos = new Vector2(0, -4);

		var material = _baseShadowMaterial?.Duplicate() as ShaderMaterial;
		if (material != null)
		{
			// Account for sprite's local position to get actual world position
			material.SetShaderParameter("shadow_world_position", root.GlobalPosition + basePos);

			var roomBoundsVec = new Vector4(
				_shadowRoomBounds.Position.X,
				_shadowRoomBounds.Position.Y,
				_shadowRoomBounds.Size.X,
				_shadowRoomBounds.Size.Y
			);
			material.SetShaderParameter("room_bounds", roomBoundsVec);
			// Disable gradient fade completely for base shadow: solid oval
			material.SetShaderParameter("gradient_fade_height", 0.0f);
			material.SetShaderParameter("gradient_at_top", false);
		}

		var baseShadowSprite = new Sprite2D
		{
			Texture = ovalTexture,
			Material = material,
			Position = basePos,
			FlipV = false,
			Modulate = new Color(0, 0, 0, ShadowOpacity),
			ZAsRelative = true,
			ZIndex = -1
		};
		root.AddChild(baseShadowSprite);
		_baseShadowSprites.Add(baseShadowSprite);
	}

	public void Update(double delta)
	{
		if (_lightSource == null)
			return;

		var lightPos = _lightSource.GlobalPosition;
		var shadowPivots = GetTree().GetNodesInGroup(GroupName);

		foreach (Node node in shadowPivots)
		{
			if (node is not Node2D pivot)
				continue;

			var pivotWorldPos = pivot.GlobalPosition;

			// Skip bounds check for player shadows (update in any room)
			bool isPlayerShadow = pivot.GetParent() is Player;
			if (!isPlayerShadow && !_shadowRoomBounds.HasPoint(pivotWorldPos))
				continue;
			var lightToPivot = pivotWorldPos - lightPos;
			var distance = lightToPivot.Length();

			var angle = Mathf.Atan2(lightToPivot.Y, lightToPivot.X) - Mathf.DegToRad(90) + Mathf.DegToRad(180);
			pivot.Rotation = angle;

			var radiusHalf = LightRadius / 2f;
			var scaleY = Mathf.Clamp(0.5f + (distance / radiusHalf) * 1.0f, 0.5f, 1.5f);
			var scaleX = 0.5f + Mathf.Abs(Mathf.Cos(angle)) * 0.5f;

			pivot.Scale = new Vector2(scaleX, scaleY);

			if (_pivotToShadowSprite.TryGetValue(pivot, out var shadowSprite))
			{
			// Flip when shadow points south (player below light) to match desired orientation
			bool shouldFlip = pivotWorldPos.Y > lightPos.Y;
			shadowSprite.FlipH = shouldFlip;

				var material = shadowSprite.Material as ShaderMaterial;
				if (material != null && shadowSprite.Texture != null)
				{
					material.SetShaderParameter("shadow_world_position", shadowSprite.GlobalPosition);
					material.SetShaderParameter("shadow_scale", pivot.Scale);
					material.SetShaderParameter("shadow_rotation", pivot.Rotation);
					material.SetShaderParameter("shadow_texture_size", shadowSprite.Texture.GetSize());
					// Disable shader flip to avoid double flip (CPU already flipped)
					material.SetShaderParameter("shadow_flip_h", false);
			}
		}
		}

		foreach (var baseSprite in _baseShadowSprites)
		{
			if (!IsInstanceValid(baseSprite))
				continue;

			var spritePos = baseSprite.GlobalPosition;
			if (!_shadowRoomBounds.HasPoint(spritePos))
				continue;

			var material = baseSprite.Material as ShaderMaterial;
			if (material != null && baseSprite.Texture != null)
			{
				material.SetShaderParameter("shadow_world_position", baseSprite.GlobalPosition);
				material.SetShaderParameter("shadow_scale", baseSprite.GlobalScale);
				material.SetShaderParameter("shadow_rotation", baseSprite.GlobalRotation);
				material.SetShaderParameter("shadow_texture_size", baseSprite.Texture.GetSize());
			}
		}
	}

	public void UpdateDepthShadowLightPosition()
	{
		if (_lightSource != null && _depthShadowMaterial != null)
		{
			_depthShadowMaterial.SetShaderParameter("light_position", _lightSource.GlobalPosition);
		}
	}

	public void RemoveShadowForObject(Node2D root)
	{
		// Find all shadow pivots that belong to this root by scanning dictionary
		var pivotsToRemove = new List<Node2D>();
		foreach (var kvp in _pivotToShadowSprite)
		{
			if (kvp.Key.GetParent() == root)
			{
				pivotsToRemove.Add(kvp.Key);
			}
		}

		// Remove each pivot from parent and dictionary, then queue for deletion
		foreach (var pivot in pivotsToRemove)
		{
			// Remove from dictionary first
			_pivotToShadowSprite.Remove(pivot);
			// Detach from parent immediately to prevent naming conflicts
			if (pivot.GetParent() != null)
			{
				pivot.GetParent().RemoveChild(pivot);
			}
			// Queue for deletion
			pivot.QueueFree();
		}

		// Remove associated base shadow sprites that belong to this root
		_baseShadowSprites.RemoveAll(sprite =>
		{
			if (!IsInstanceValid(sprite) || sprite.GetParent() != root)
				return false;

			// Detach and queue for deletion
			if (sprite.GetParent() != null)
			{
				sprite.GetParent().RemoveChild(sprite);
			}
			sprite.QueueFree();
			return true;
		});
	}
}
