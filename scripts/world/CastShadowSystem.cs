using Godot;
using System.Collections.Generic;

public partial class CastShadowSystem : Node
{
	[ExportGroup("Shadow Settings")]
	[Export] public float ShadowLerpFactor = 0.12f;
	[Export] public float LightRadius = 450f;
	[Export] public float ShadowOpacity = 0.3f;

	[ExportGroup("Shader Parameters")]
	[Export] public float BlurAmount = 0.3f;
	[Export] public float BaseBlurAmount = 0.5f;
	[Export] public float GradientFadeHeight = 0.4f;

	private RoomBase _room;
	private PointLight2D _lightSource;
	private ShaderMaterial _depthShadowMaterial;
	private ShaderMaterial _shadowMaterial;
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

		var roomOrigin = floorLayer.MapToLocal(new Vector2I(-1, -1)) + gridOffset;
		var roomWidth = (gridWidth + 1) * RoomBase.TileSize;
		var roomHeight = (gridHeight + 1) * RoomBase.TileSize;

		_shadowRoomBounds = new Rect2(roomOrigin.X, roomOrigin.Y, roomWidth, roomHeight);

		var roomBounds = new Vector4(roomOrigin.X, roomOrigin.Y, roomWidth, roomHeight);

		if (_shadowMaterial != null)
			_shadowMaterial.SetShaderParameter("room_bounds", roomBounds);
		if (_baseShadowMaterial != null)
			_baseShadowMaterial.SetShaderParameter("room_bounds", roomBounds);
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
	}

	private void SetShadowRoomBounds()
	{
		var roomOrigin = _room.GridToWorld(new Vector2I(-1, -1));
		var roomWidth = (_room.GridWidth + 1) * RoomBase.TileSize;
		var roomHeight = (_room.GridHeight + 1) * RoomBase.TileSize;

		_shadowRoomBounds = new Rect2(roomOrigin.X, roomOrigin.Y, roomWidth, roomHeight);

		var roomBounds = new Vector4(roomOrigin.X, roomOrigin.Y, roomWidth, roomHeight);

		if (_shadowMaterial != null)
			_shadowMaterial.SetShaderParameter("room_bounds", roomBounds);
		if (_baseShadowMaterial != null)
			_baseShadowMaterial.SetShaderParameter("room_bounds", roomBounds);
	}

	public void CreateShadowForObject(Node2D root, Texture2D texture, Vector2? offset = null, bool createOvalBase = false)
	{
		var spriteSize = texture.GetSize();
		var pivotOffset = offset ?? new Vector2(0, -3);

		var shadowPivot = new Node2D { Name = "ShadowPivot", Position = pivotOffset };
		shadowPivot.AddToGroup("shadow_pivots");
		root.AddChild(shadowPivot);

		var material = _shadowMaterial?.Duplicate() as ShaderMaterial;
		if (material != null)
		{
			material.SetShaderParameter("sprite_world_position", root.GlobalPosition);
			// Disable gradient fade for player shadows (keep blur)
			material.SetShaderParameter("gradient_fade_height", 0.0f);
		}

		var shadowSprite = new Sprite2D
		{
			Texture = texture,
			Material = material,
			Position = new Vector2(0, -spriteSize.Y * 0.5f),
			FlipV = false,
			Modulate = new Color(0, 0, 0, ShadowOpacity),
			ZAsRelative = false,
			ZIndex = 1
		};
		shadowPivot.AddChild(shadowSprite);
		_pivotToShadowSprite[shadowPivot] = shadowSprite;

		if (createOvalBase)
			CreateOvalBaseShadow(root, texture, pivotOffset);
		else
			CreateBaseShadowForObject(root, texture);
	}

	public void CreateBaseShadowForObject(Node2D root, Texture2D texture)
	{
		var originalImage = texture.GetImage();
		var regionHeight = 8;
		var region = new Rect2I(0, originalImage.GetHeight() - regionHeight, originalImage.GetWidth(), regionHeight);
		var bottomImage = originalImage.GetRegion(region);
		var bottomTexture = ImageTexture.CreateFromImage(bottomImage);

		var material = _baseShadowMaterial?.Duplicate() as ShaderMaterial;
		if (material != null)
			material.SetShaderParameter("sprite_world_position", root.GlobalPosition);

		var baseShadowSprite = new Sprite2D
		{
			Texture = bottomTexture,
			Material = material,
			Position = new Vector2(0, -2),
			FlipV = false,
			Modulate = new Color(0, 0, 0, ShadowOpacity),
			ZAsRelative = false,
			ZIndex = 1
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
				float dist = dx*dx + dy*dy;
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

	private void CreateOvalBaseShadow(Node2D root, Texture2D referenceTexture, Vector2 pivotOffset)
	{
		var refSize = referenceTexture.GetSize();
		int ovalWidth = (int)(refSize.X * 0.28f); // keep width
		int ovalHeight = (int)(refSize.Y * 0.15f); // more oval (taller)
		// Ensure minimum size
		ovalWidth = Mathf.Max(ovalWidth, 8);
		ovalHeight = Mathf.Max(ovalHeight, 4);
		
		var ovalTexture = CreateOvalShadowTexture(ovalWidth, ovalHeight);
		
		var material = _baseShadowMaterial?.Duplicate() as ShaderMaterial;
		if (material != null)
		{
			material.SetShaderParameter("sprite_world_position", root.GlobalPosition);
			// Disable gradient fade for player shadows (keep blur)
			material.SetShaderParameter("gradient_fade_height", 0.0f);
		}
		
		// Position: base shadow sits on ground at the foot line (pivotOffset.Y)
		// The top of the oval should be at the foot line, so the shadow extends downward.
		// center = top + ovalHeight/2 = pivotOffset.Y + ovalHeight/2
		// Add a small extra offset (1px) to move it slightly lower.
		Vector2 basePos = new Vector2(0, pivotOffset.Y + ovalHeight * 0.5f + 1);
		var baseShadowSprite = new Sprite2D
		{
			Texture = ovalTexture,
			Material = material,
			Position = basePos,
			FlipV = false,
			Modulate = new Color(0, 0, 0, ShadowOpacity),
			ZAsRelative = false,
			ZIndex = 1
		};
		root.AddChild(baseShadowSprite);
		_baseShadowSprites.Add(baseShadowSprite);
	}

	public void Update(double delta)
	{
		if (_lightSource == null)
			return;

		var lightPos = _lightSource.GlobalPosition;
		var shadowPivots = GetTree().GetNodesInGroup("shadow_pivots");

		foreach (Node node in shadowPivots)
		{
			if (node is not Node2D pivot)
				continue;

			var pivotWorldPos = pivot.GlobalPosition;

			if (!_shadowRoomBounds.HasPoint(pivotWorldPos))
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
				// Determine if horizontal flip is needed for south‑cast shadows
				bool flipH = pivotWorldPos.Y > lightPos.Y;   // true when shadow points south

				// Keep sprite unflipped; we'll handle it in the shader only
				shadowSprite.FlipH = false;

				var material = shadowSprite.Material as ShaderMaterial;
				if (material != null && shadowSprite.Texture != null)
				{
					material.SetShaderParameter("shadow_world_position", shadowSprite.GlobalPosition);
					material.SetShaderParameter("shadow_scale", shadowSprite.GlobalScale);
					material.SetShaderParameter("shadow_rotation", shadowSprite.GlobalRotation);
					material.SetShaderParameter("shadow_texture_size", shadowSprite.Texture.GetSize());
					// Apply conditional flip via shader uniform
					material.SetShaderParameter("shadow_flip_h", flipH);
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
}
