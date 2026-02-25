using Godot;

public partial class RoomLighting : Node
{
	[ExportGroup("Ceiling Light")]
	[Export] public bool EnableCeilingLight = true;
	[Export] public Color CeilingLightColor = Colors.White;
	[Export] public float CeilingLightEnergy = 0.8f;
	[Export] public float CeilingLightRadius = 450f;
	[Export] public int CeilingLightWidth = 256;
	[Export] public int CeilingLightHeight = 256;
	[Export] public bool CeilingLightShadows = true;

	[ExportGroup("Monitor Light")]
	[Export] public bool EnableMonitorLight = false;
	[Export] public Color MonitorLightColor = new(0f, 1f, 0.27f);
	[Export] public float MonitorLightEnergy = 0.3f;
	[Export] public float MonitorLightRadius = 80f;

	[ExportGroup("Desk Lamp Light")]
	[Export] public bool EnableDeskLampLight = false;
	[Export] public Color DeskLampColor = new(1f, 0.67f, 0.27f);
	[Export] public float DeskLampEnergy = 0.25f;
	[Export] public float DeskLampRadius = 60f;

	[ExportGroup("Ambient")]
	[Export] public Color AmbientColor = new(0.15f, 0.15f, 0.20f);

	private RoomBase _room;
	private CanvasModulate _canvasModulate;
	private PointLight2D _ceilingLight;
	private PointLight2D _monitorLight;
	private PointLight2D _deskLampLight;
	private float _flickerTime;
	private bool _flickerEnabled = true;

	public PointLight2D CeilingLight => _ceilingLight;
	public PointLight2D MonitorLight => _monitorLight;
	public PointLight2D DeskLampLight => _deskLampLight;
	public CanvasModulate CanvasModulate => _canvasModulate;

	public void Initialize(RoomBase room)
	{
		_room = room;
	}

	public void CreateLighting(Vector2 tablePosition)
	{
		_canvasModulate = new CanvasModulate { Color = AmbientColor };
		_room.AddChild(_canvasModulate);

		var roomCenterX = _room.GridWidth / 2;
		var roomCenterY = _room.GridHeight / 2;
		var roomCenter = _room.GridToWorld(new Vector2I(roomCenterX, roomCenterY));

		if (EnableCeilingLight)
		{
			_ceilingLight = CreatePointLightWithTexture(
				_room.GridAnchor + new Vector2(0, -32),
				CeilingLightColor,
				CeilingLightEnergy,
				CeilingLightRadius,
				CeilingLightShadows,
				CeilingLightWidth,
				CeilingLightHeight
			);
			_room.AddChild(_ceilingLight);
		}

		if (EnableMonitorLight)
		{
			_monitorLight = CreatePointLightWithTexture(
				tablePosition + new Vector2(32, -38),
				MonitorLightColor,
				MonitorLightEnergy,
				MonitorLightRadius,
				false
			);
			_monitorLight.TextureScale = 2.0f;
			_room.AddChild(_monitorLight);
		}

		if (EnableDeskLampLight)
		{
			_deskLampLight = CreatePointLightWithTexture(
				tablePosition + new Vector2(-32, -35),
				DeskLampColor,
				DeskLampEnergy,
				DeskLampRadius,
				false
			);
			_deskLampLight.TextureScale = 1.8f;
			_room.AddChild(_deskLampLight);
		}

		_flickerTime = 0f;
	}

	public void Update(double delta)
	{
		if (!_flickerEnabled)
			return;

		_flickerTime += (float)delta;

		if (_ceilingLight != null)
		{
			_ceilingLight.Energy = CeilingLightEnergy;
		}

		if (_monitorLight != null)
		{
			var pulse = MonitorLightEnergy + Mathf.Sin(_flickerTime * 2f) * 0.03f;
			_monitorLight.Energy = pulse;
		}

		if (_deskLampLight != null)
		{
			var shimmer = DeskLampEnergy + Mathf.Sin(_flickerTime * 3f) * 0.02f;
			_deskLampLight.Energy = shimmer;
		}
	}

	public void SetFlickerEnabled(bool enabled)
	{
		_flickerEnabled = enabled;
	}

	private PointLight2D CreatePointLightWithTexture(Vector2 position, Color color, float energy, float radius, bool shadows, int textureWidth = 0, int textureHeight = 0)
	{
		var light = new PointLight2D
		{
			Position = position,
			Color = color,
			Energy = energy,
			ShadowEnabled = shadows,
			ShadowColor = new Color(0, 0, 0, 0.3f)
		};

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
		var maxDist = Mathf.Min(centerX, centerY);

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
}
