using System;
using Godot;
using KBTV.Data;

public partial class StudioBuilder : IRoomBuilder
{
	[ExportGroup("Grid Settings")]
	[Export] public Vector2 GridAnchor = new(0, 388);
	[Export] public int GridWidth = 14;
	[Export] public int GridHeight = 6;
	[Export] public int LightMask = 2;

	[ExportGroup("Door Settings")]
	[Export] private int DoorRow = 3;
	[Export] private int DoorHeightTiles = 2;
	[Export] private bool EnableSouthWall = false;
	[Export] private bool EnableSouthDoor = false;
	[Export] private int SouthDoorRow = 3;

	[ExportGroup("Window Settings")]
	[Export] private int WindowStartColumn = 99;
	[Export] private int WindowEndColumn = 0;

	[ExportGroup("Lighting")]
	[Export] private bool EnableCeilingLight = true;
	[Export] private Color CeilingLightColor = new(1f, 0.95f, 0.9f);
	[Export] private float CeilingLightEnergy = 0.8f;
	[Export] private float CeilingLightRadius = 450f;
	[Export] private bool CeilingLightShadows = true;



	[ExportGroup("Ambient")]
	[Export] private Color AmbientColor = new(0.15f, 0.15f, 0.20f);

	[ExportGroup("Smoke")]
	[Export] private bool EnableSmoke = true;
	[Export] private float SmokeMaxParticles = 100f;
	[Export] private float SmokeDecayTime = 60f;

	[ExportGroup("Props")]
	[Export] private bool PlaceRoundTable = true;
	[Export] private bool PlaceVern = true;

	private TileMapLayer _floorLayer;
	private TileMapLayer _doorLayer;
	private TileMapLayer _gridDebugLayer;
	private Node2D _propSort;
	private RoomSection _section;
	private WallSystem _wallSystem;
	private CastShadowSystem _shadows;
	private RoomDebug _debug;
	private CanvasModulate _canvasModulate;
	private PointLight2D _ceilingLight;
	private Node2D _smokeRoot;
	private Node2D[] _smokeLayers = Array.Empty<Node2D>();
	private AnimatedSprite2D[] _smokeSprites = Array.Empty<AnimatedSprite2D>();
	private float[] _smokeInitialX = Array.Empty<float>();
	private float[] _smokeTimeOffsets = Array.Empty<float>();
	private float[] _smokeLayerOffsets = Array.Empty<float>();
	private float[] _smokeCycleLengths = Array.Empty<float>();
	private float[] _smokePhaseOffsets = Array.Empty<float>();
	private CharacterBody2D _player;
	private float _flickerTime;

	public CastShadowSystem GetShadows() => _shadows;

	public void Build(WorldRoom world)
	{
		var tileSet = GD.Load<TileSet>("res://assets/tiles/topdown/topdown_tileset.tres");
		if (tileSet == null)
		{
			GD.PrintErr("StudioBuilder: Failed to load tileset");
			return;
		}

		_floorLayer = new TileMapLayer { Name = "StudioFloorLayer", TileSet = tileSet, ZIndex = 0, LightMask = LightMask };
		_doorLayer = new TileMapLayer { Name = "StudioDoorLayer", TileSet = tileSet, ZIndex = 1000 };
		_gridDebugLayer = new TileMapLayer { Name = "StudioGridDebugLayer", TileSet = tileSet, Visible = false };

		world.AddChild(_floorLayer);
		world.AddChild(_doorLayer);
		world.AddChild(_gridDebugLayer);

		_floorLayer.Position = GridAnchor;
		_doorLayer.Position = GridAnchor;
		_gridDebugLayer.Position = GridAnchor;

		_propSort = world.PropSort;

		_section = new RoomSection
		{
			FloorLayer = _floorLayer,
			DoorLayer = _doorLayer,
			PropSort = _propSort,
			GridOffset = GridAnchor,
			GridWidth = GridWidth,
			GridHeight = GridHeight
		};

		CreateFloors();

		CreateSystems(world);
		CreateLighting(world);
		CreateSmoke(world);
		InitializeDebug();
		CreateProps();
	}

	private void CreateFloors()
	{
		for (int y = 0; y < GridHeight; y++)
		{
			for (int x = 0; x < GridWidth; x++)
			{
				_floorLayer.SetCell(new Vector2I(x, y), 0, new Vector2I(0, 0));
			}
		}
	}

	private void CreateSystems(WorldRoom world)
	{
		_wallSystem = new WallSystem
		{
			DoorRow = DoorRow,
			DoorHeightTiles = DoorHeightTiles,
			EnableSouthWall = EnableSouthWall,
			EnableSouthDoor = EnableSouthDoor,
			SouthDoorRow = SouthDoorRow,
			WindowStartColumn = WindowStartColumn,
			WindowEndColumn = WindowEndColumn,
			LightMask = 0,
			NorthWallLightMask = LightMask
		};
		world.AddChild(_wallSystem);

		_shadows = new CastShadowSystem { LightRadius = CeilingLightRadius, GroupName = "shadow_pivots_studio" };
		world.AddChild(_shadows);

		_debug = new RoomDebug { DebugEnabled = false, ZIndex = 2000 };
		world.AddChild(_debug);

		_wallSystem.Initialize(_section);
		_wallSystem.CreateWalls();
		_wallSystem.CreateWallColliders();
	}

	private void CreateLighting(WorldRoom world)
	{
		_canvasModulate = new CanvasModulate { Color = AmbientColor, Name = "StudioAmbient" };
		world.AddChild(_canvasModulate);

		var centerX = GridWidth / 2f;
		var centerY = GridHeight / 2f;
		var center = GridToWorld(new Vector2I((int)centerX, (int)centerY));

		if (EnableCeilingLight)
		{
			_ceilingLight = CreatePointLightWithTexture(
				new Vector2(center.X, center.Y - 16),
				CeilingLightColor,
				CeilingLightEnergy,
				CeilingLightRadius,
				CeilingLightShadows,
				256,
				256,
				LightMask
			);
			world.AddChild(_ceilingLight);
		}

		_shadows.Initialize(_section, _ceilingLight);
		_flickerTime = 0f;
	}

	private void CreateSmoke(WorldRoom world)
	{
		if (!EnableSmoke)
		{
			return;
		}

		var smokePosition = GridToWorld(new Vector2I(7, GridHeight - 3));

		_smokeRoot = new Node2D
		{
			Name = "SmokeRoot",
			Position = smokePosition,
			YSortEnabled = true,
			ZIndex = 480
		};

		_smokeInitialX = new float[(int)SmokeMaxParticles];
		var smokeTexture = GD.Load<Texture2D>("res://assets/tiles/smoke_sheet.png");
		if (smokeTexture == null)
		{
			GD.PrintErr("StudioBuilder: Failed to load smoke_sheet.png");
			return;
		}

		var frames = new SpriteFrames();
		var frameSize = new Vector2I(256, 256);
		for (int y = 0; y < 5; y++)
		{
			for (int x = 0; x < 5; x++)
			{
				var region = new Rect2I(new Vector2I(x * frameSize.X, y * frameSize.Y), frameSize);
				var frame = new AtlasTexture
				{
					Atlas = smokeTexture,
					Region = region
				};
				frames.AddFrame("default", frame);
			}
		}
		frames.SetAnimationSpeed("default", 0.5f);
		frames.SetAnimationLoop("default", true);

		var layerCount = 3;
		_smokeLayers = new Node2D[layerCount];
		_smokeLayerOffsets = new float[layerCount];
		for (int i = 0; i < layerCount; i++)
		{
			var layer = new Node2D { Name = $"SmokeLayer_{i}", YSortEnabled = true };
			_smokeLayers[i] = layer;
			_smokeLayerOffsets[i] = i * 7.5f;
			_smokeRoot.AddChild(layer);
		}

		_smokeSprites = new AnimatedSprite2D[(int)SmokeMaxParticles];
		_smokeInitialX = new float[(int)SmokeMaxParticles];
		_smokeTimeOffsets = new float[(int)SmokeMaxParticles];
		_smokeCycleLengths = new float[(int)SmokeMaxParticles];
		_smokePhaseOffsets = new float[(int)SmokeMaxParticles];

		for (int i = 0; i < SmokeMaxParticles; i++)
		{
			var initialX = GD.Randf() * 240 - 120;
			_smokeInitialX[i] = initialX;
			_smokeTimeOffsets[i] = GD.Randf() * 30f;
			_smokeCycleLengths[i] = 50f + GD.Randf() * 20f;
			_smokePhaseOffsets[i] = GD.Randf() * _smokeCycleLengths[i];

			var smokeSprite = new AnimatedSprite2D
			{
				Name = $"SmokePuff_{i}",
				SpriteFrames = frames,
				Position = new Vector2(initialX, -GD.Randf() * 180 + 160),
				Scale = new Vector2(1.8f + GD.Randf() * 0.6f, 1.8f + GD.Randf() * 0.6f),
				Modulate = new Color(1f, 1f, 1f, 0.02f)
			};
			smokeSprite.Set("light_mask", LightMask);
			smokeSprite.Frame = (int)(GD.Randf() * 25f);

			var layerIndex = i % layerCount;
			_smokeLayers[layerIndex].AddChild(smokeSprite);
			_smokeSprites[i] = smokeSprite;
		}

		_propSort.AddChild(_smokeRoot);
	}

	private PointLight2D CreatePointLightWithTexture(Vector2 position, Color color, float energy, float radius, bool shadows, int textureWidth = 0, int textureHeight = 0, int itemCullMask = 2)
	{
		var light = new PointLight2D
		{
			Position = position,
			Color = color,
			Energy = energy,
			ShadowEnabled = shadows,
			ShadowColor = new Color(0, 0, 0, 0.3f),
			ZIndex = 10
		};
		light.Set("range_item_cull_mask", itemCullMask);

		var texture = CreateOvalGradientTexture(textureWidth, textureHeight, radius);
		light.Texture = texture;
		light.TextureScale = 1.0f;
		light.Set("range", radius);

		return light;
	}

	private ImageTexture CreateOvalGradientTexture(int width, int height, float radius)
	{
		var sizeX = width > 0 ? width : (int)(radius * 0.8f);
		var sizeY = height > 0 ? height : (int)(radius * 0.8f);
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
				if (dist < 0.2f)
				{
					alpha = 255;
				}
				else if (dist < 1.0f)
				{
					var t = (dist - 0.2f) / 0.8f;
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

	private void InitializeDebug()
	{
		_debug.Initialize(_section, _wallSystem, _shadows, _ceilingLight, null, null);
	}

	private void CreateProps()
	{
		CreateBookcases();
		CreateOnAirSign();

		if (PlaceRoundTable)
		{
			CreateRoundTableGroup();
		}

		if (PlaceVern)
		{
			CreateVernChairGroup();
		}
	}

	private void CreateBookcases()
	{
		CreatePropWithCollision(
			"res://assets/tiles/props/bookcase.png",
			new Vector2I(1, 1),
			Vector2.Zero,
			new Vector2(48, 32)
		);

		CreatePropWithCollision(
			"res://assets/tiles/props/bookcase.png",
			new Vector2I(12, 1),
			Vector2.Zero,
			new Vector2(48, 32)
		);
	}

	private void CreateOnAirSign()
	{
		var onAirTexture = GD.Load<Texture2D>("res://assets/tiles/props/on_air_sign.png");
		if (onAirTexture == null)
		{
			GD.PrintErr("StudioBuilder: Missing on_air_sign.png texture");
			return;
		}

		var signPos = GridAnchor + new Vector2(112, -56);

		var onAirSign = new Sprite2D
		{
			Texture = onAirTexture,
			Position = signPos,
			Scale = new Vector2(0.375f, 0.5f),
			ZIndex = 1001
		};
		onAirSign.Set("light_mask", LightMask);
		_propSort.AddChild(onAirSign);

		var onAirLight = CreatePointLightWithTexture(
			signPos,
			new Color(1f, 0.1f, 0.1f),
			0.5f,
			60f,
			false,
			32,
			32,
			LightMask
		);
		onAirLight.ZIndex = 1002;
		_propSort.AddChild(onAirLight);
	}

	private Node2D CreatePropWithCollision(
		string texturePath,
		Vector2I gridCoords,
		Vector2 pixelOffset,
		Vector2 colliderSize,
		bool createShadow = true)
	{
		var texture = GD.Load<Texture2D>(texturePath);
		if (texture == null)
		{
			GD.PrintErr($"StudioBuilder: Missing texture {texturePath}");
			return null;
		}

		var worldPos = GridToWorld(gridCoords) + pixelOffset;

		var body = new StaticBody2D();
		body.Position = worldPos;

		var sprite = new Sprite2D
		{
			Texture = texture,
			Position = new Vector2(0, -texture.GetSize().Y * 0.5f)
		};
		sprite.Set("light_mask", LightMask);

		if (_shadows != null && createShadow)
		{
			_shadows.CreateShadowForObject(body, texture);
		}

		body.AddChild(sprite);

		if (colliderSize != Vector2.Zero)
		{
			var shape = new RectangleShape2D { Size = colliderSize };
			var collision = new CollisionShape2D { Shape = shape };
			collision.Position = new Vector2(0, -(colliderSize.Y * 0.5f));
			collision.AddToGroup("debug_prop_collision");
			body.AddChild(collision);
		}

		body.ZIndex = (int)body.GlobalPosition.Y;
		_propSort.AddChild(body);

		return body;
	}

	private Node2D CreatePropNoCollision(string texturePath, Vector2I gridCoords, Vector2 pixelOffset)
	{
		var texture = GD.Load<Texture2D>(texturePath);
		if (texture == null)
		{
			GD.PrintErr($"StudioBuilder: Missing texture {texturePath}");
			return null;
		}

		var worldPos = GridToWorld(gridCoords) + pixelOffset;

		var node = new Node2D();
		node.Position = worldPos;

		var sprite = new Sprite2D
		{
			Texture = texture,
			Position = new Vector2(0, -texture.GetSize().Y * 0.5f)
		};
		sprite.Set("light_mask", LightMask);

		node.AddChild(sprite);

		node.ZIndex = (int)node.GlobalPosition.Y;
		_propSort.AddChild(node);

		return node;
	}

	private void CreateRoundTableGroup()
	{
		var tablePos = new Vector2I(6, 4);

		var tableTexture = GD.Load<Texture2D>("res://assets/tiles/props/round_table.png");
		var tableGroup = CreatePropWithCollision(
			"res://assets/tiles/props/round_table.png",
			tablePos,
			Vector2.Zero,
			new Vector2(48, 48),
			false
		);
		if (tableGroup == null) return;

		tableGroup.Name = "RoundTableGroup";

		if (_shadows != null && tableTexture != null)
		{
			_shadows.CreateBaseShadowForObject(tableGroup, tableTexture);
		}
	}

	private void CreateVernChairGroup()
	{
		var vernTexture = GD.Load<Texture2D>("res://assets/sprites/characters/vern/vern.png");

		if (vernTexture == null)
		{
			GD.PrintErr("StudioBuilder: Missing vern texture");
			return;
		}

		var chairPos = new Vector2I(6, 3);

		var body = new StaticBody2D { Name = "VernChairGroup" };
		body.Position = GridToWorld(chairPos);

		if (_shadows != null)
		{
			var shadowOffset = new Vector2(0, -20);
			_shadows.CreateShadowForObject(body, vernTexture, shadowOffset);
		}

		var shape = new RectangleShape2D { Size = new Vector2(32, 32) };
		var collision = new CollisionShape2D { Shape = shape };
		collision.Position = new Vector2(0, -(shape.Size.Y * 0.5f));
		collision.AddToGroup("debug_prop_collision");
		body.AddChild(collision);

		var vernSprite = new Sprite2D
		{
			Texture = vernTexture,
			Position = new Vector2(0, -vernTexture.GetSize().Y * 0.5f),
			Scale = new Vector2(0.75f, 0.75f)
		};
		vernSprite.Set("light_mask", LightMask);
		body.AddChild(vernSprite);

		body.ZIndex = (int)body.GlobalPosition.Y;
		_propSort.AddChild(body);
	}

	private void CreateTabletopSprite(Node2D parent, string texturePath, Vector2 offset, int lightMask)
	{
		var texture = GD.Load<Texture2D>(texturePath);
		if (texture == null)
		{
			GD.PrintErr($"StudioBuilder: Missing tabletop texture {texturePath}");
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

	public void SetPlayer(CharacterBody2D player)
	{
		_player = player;
		_section.Player = player;
	}

	public Vector2 GridToWorld(Vector2I gridPos)
	{
		return _section.GridToWorld(gridPos);
	}

	public Rect2 GetFloorBounds()
	{
		var topLeft = _section.GridToWorld(new Vector2I(0, 0));
		var bottomRight = _section.GridToWorld(new Vector2I(GridWidth - 1, GridHeight - 1));
		return new Rect2(
			topLeft.X,
			topLeft.Y,
			GridWidth * RoomBase.TileSize,
			GridHeight * RoomBase.TileSize
		);
	}

	public void Update(WorldRoom world, double delta, VernStats? vernStats)
	{
		if (_player != null)
		{
			_wallSystem.UpdateVisibility(_player);
		}

		_flickerTime += (float)delta;

		if (_ceilingLight != null)
		{
			_ceilingLight.Energy = CeilingLightEnergy;
		}

		UpdateSmoke(vernStats);

		_shadows.Update(delta);
		_debug.UpdatePlayerRect();
		_debug.UpdatePropRects();
	}

	private void UpdateSmoke(VernStats? vernStats)
	{
		if (_smokeRoot == null)
		{
			return;
		}

		float intensity;
		if (vernStats == null)
		{
			intensity = 1f;
		}
		else
		{
			var timeSinceLastCigarette = vernStats.TimeSinceLastCigarette;
			if (timeSinceLastCigarette < 5f)
			{
				intensity = 1f;
			}
			else if (timeSinceLastCigarette < SmokeDecayTime)
			{
				float t = (timeSinceLastCigarette - 5f) / (SmokeDecayTime - 5f);
				intensity = 1f - t;
			}
			else
			{
				intensity = 0f;
			}
		}

		var baseAlpha = Mathf.Clamp(intensity, 0f, 1f) * 0.03f;

		var smokeTime = Time.GetTicksMsec() / 1000f;

		for (int i = 0; i < _smokeSprites.Length; i++)
		{
			var sprite = _smokeSprites[i];
			if (sprite == null) continue;

			var layerIndex = i % _smokeLayerOffsets.Length;
			var adjustedTime = smokeTime + _smokeTimeOffsets[i] + _smokeLayerOffsets[layerIndex];
			var cycleLength = _smokeCycleLengths[i];
			var cyclePos = ((adjustedTime + _smokePhaseOffsets[i]) % cycleLength) / cycleLength;
			var eased = Mathf.SmoothStep(0f, 1f, cyclePos);

			var yOffset = eased * 180f;
			var xWobble = Mathf.Sin(smokeTime * 0.12f + i) * 5f;
			var yBias = i * 0.01f;

			sprite.Position = new Vector2(
				_smokeInitialX[i] + xWobble,
				-yOffset + 32 + yBias
			);

			float fadeIn = cyclePos < 0.3f ? cyclePos / 0.3f : 1f;
			float fadeOut = cyclePos > 0.6f ? (1f - cyclePos) / 0.4f : 1f;
			var alpha = baseAlpha * fadeIn * fadeOut;

			sprite.Modulate = new Color(1f, 1f, 1f, alpha);

			var scale = 1.0f + eased * 0.5f;
			sprite.Scale = new Vector2(scale, scale);

			const int totalFrames = 25;
			var frameIndex = Mathf.Clamp((int)(cyclePos * totalFrames), 0, totalFrames - 1);
			sprite.Frame = frameIndex;
		}
	}

	public void ToggleDebug()
	{
		_debug.Toggle();
	}
}
