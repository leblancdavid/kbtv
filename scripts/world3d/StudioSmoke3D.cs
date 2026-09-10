using Godot;

namespace KBTV.World3D;

public partial class StudioSmoke3D : Node3D
{
	[Export] public int PuffBurstCount { get; set; } = 5;
	[Export] public float PuffInterval { get; set; } = 4.0f;
	[Export] public float AmbientOpacity { get; set; } = 0.38f;
	[Export] public float PuffOpacity { get; set; } = 0.16f;
	[Export] public float DoorLeakOpacity { get; set; } = 0.045f;
	[Export] public float DoorLeakInterval { get; set; } = 0.32f;
	[Export] public float FogMotionSpeed { get; set; } = 0.55f;
	[Export] public float FogMotionStrength { get; set; } = 0.22f;
	[Export] public float DriftSpeed { get; set; } = 0.45f;
	[Export] public Vector3 PuffOrigin { get; set; } = new(-0.85f, 1.25f, 0.35f);
	[Export] public Vector3 RoomHalfExtents { get; set; } = new(4.4f, 1.7f, 3.4f);

	private const int MaxPuffs = 36;
	private const int MaxDoorLeakPuffs = 24;
	private const int SmokeTextureSize = 128;
	private static readonly Color FogColor = new(0.62f, 0.6f, 0.56f);
	private static readonly Color PuffSmokeColor = new(0.66f, 0.68f, 0.7f, 1f);
	private readonly RandomNumberGenerator _rng = new();
	private readonly SmokeWisp[] _puffWisps = new SmokeWisp[MaxPuffs];
	private readonly SmokeWisp[] _doorLeakWisps = new SmokeWisp[MaxDoorLeakPuffs];
	private Texture2D _smokeTexture = null!;
	private FogMaterial _fogMaterial = null!;
	private FogVolume _fogVolume = null!;
	private Vector3 _fogBasePosition;
	private Vector3 _fogBaseSize;
	private float _time;
	private float _puffTimer;
	private float _controlStudioLeakTimer;
	private float _studioHallLeakTimer;
	private int _nextPuffIndex;
	private int _nextDoorLeakIndex;
	private bool _controlStudioLeakActive;
	private bool _studioHallLeakActive;
	private Vector3 _controlStudioLeakOrigin;
	private Vector3 _controlStudioLeakDirection;
	private Vector3 _studioHallLeakOrigin;
	private Vector3 _studioHallLeakDirection;

	private sealed class SmokeWisp
	{
		public required MeshInstance3D Mesh { get; init; }
		public required StandardMaterial3D Material { get; init; }
		public Vector3 Origin { get; set; }
		public Vector3 Drift { get; set; }
		public Vector2 Wobble { get; set; }
		public float Age { get; set; }
		public float Lifetime { get; set; }
		public float Phase { get; set; }
		public float BaseScale { get; set; }
		public float Opacity { get; set; }
		public bool Active { get; set; }
	}

	public override void _Ready()
	{
		_rng.Randomize();
		Name = "StudioSmoke3D";
		_smokeTexture = CreateSmokeTexture();

		CreateFogVolume();
		CreatePuffPool();
		CreateDoorLeakPool();
		SpawnPuffBurst();
		_puffTimer = PuffInterval * 0.65f;
	}

	public override void _Process(double delta)
	{
		var dt = (float)delta;
		_time += dt;
		_puffTimer += dt;

		UpdateFogMotion();
		UpdateActiveDoorLeaks(dt);
		UpdatePuffs(dt);
		UpdateDoorLeakPuffs(dt);

		if (_puffTimer >= PuffInterval)
		{
			_puffTimer = 0f;
			SpawnPuffBurst();
		}
	}

	public void SetDoorLeakActive(string doorKey, Vector3 localOrigin, Vector3 localDirection, bool active)
	{
		var direction = localDirection == Vector3.Zero ? Vector3.Forward : localDirection.Normalized();
		if (doorKey == "ControlStudio")
		{
			_controlStudioLeakActive = active;
			_controlStudioLeakOrigin = localOrigin;
			_controlStudioLeakDirection = direction;
			_controlStudioLeakTimer = 0f;
		}
		else if (doorKey == "StudioHall")
		{
			_studioHallLeakActive = active;
			_studioHallLeakOrigin = localOrigin;
			_studioHallLeakDirection = direction;
			_studioHallLeakTimer = 0f;
		}

		if (active)
		{
			EmitDoorLeak(localOrigin, direction, 3);
		}
	}

	private void EmitDoorLeak(Vector3 localOrigin, Vector3 localDirection, int count)
	{
		var direction = localDirection == Vector3.Zero ? Vector3.Forward : localDirection.Normalized();
		for (var i = 0; i < count; i++)
		{
			var wisp = _doorLeakWisps[_nextDoorLeakIndex];
			_nextDoorLeakIndex = (_nextDoorLeakIndex + 1) % _doorLeakWisps.Length;

			wisp.Active = true;
			wisp.Mesh.Visible = false;
			wisp.Age = -_rng.RandfRange(0f, 0.4f);
			wisp.Lifetime = _rng.RandfRange(4.2f, 6.2f);
			wisp.Phase = _rng.Randf() * Mathf.Tau;
			wisp.BaseScale = _rng.RandfRange(0.34f, 0.58f);
			wisp.Opacity = _rng.RandfRange(0.3f, 0.62f);
			wisp.Origin = localOrigin + new Vector3(_rng.RandfRange(-0.28f, 0.28f), _rng.RandfRange(0.45f, 1.1f), _rng.RandfRange(-0.28f, 0.28f));
			wisp.Drift = direction * _rng.RandfRange(0.55f, 1.05f) + new Vector3(_rng.RandfRange(-0.18f, 0.18f), _rng.RandfRange(0.12f, 0.36f), _rng.RandfRange(-0.18f, 0.18f));
			wisp.Wobble = new Vector2(_rng.RandfRange(0.04f, 0.18f), _rng.RandfRange(0.04f, 0.16f));
		}
	}

	private void CreateFogVolume()
	{
		_fogMaterial = new FogMaterial
		{
			Albedo = FogColor,
			Density = AmbientOpacity,
			HeightFalloff = 0.01f,
			EdgeFade = 1.0f
		};
		_fogBasePosition = new Vector3(0f, RoomHalfExtents.Y * 0.58f, 0f);
		_fogBaseSize = new Vector3(RoomHalfExtents.X * 2f, RoomHalfExtents.Y * 2f, RoomHalfExtents.Z * 2f);

		_fogVolume = new FogVolume
		{
			Name = "StudioRoomHaze",
			Shape = RenderingServer.FogVolumeShape.Box,
			Size = _fogBaseSize,
			Position = _fogBasePosition,
			Material = _fogMaterial,
			Layers = StationLighting3D.StudioLayer
		};
		AddChild(_fogVolume);
	}

	private void CreatePuffPool()
	{
		for (var i = 0; i < _puffWisps.Length; i++)
		{
			var material = MakeSmokeMaterial(WithAlpha(PuffSmokeColor, 0f));
			var wisp = new SmokeWisp
			{
				Mesh = CreateSmokeMesh($"CigarettePuff_{i}", material),
				Material = material,
				Lifetime = 7f,
				BaseScale = 0.45f,
				Opacity = 1f,
				Active = false
			};
			wisp.Mesh.Visible = false;
			_puffWisps[i] = wisp;
			AddChild(wisp.Mesh);
		}
	}

	private void CreateDoorLeakPool()
	{
		for (var i = 0; i < _doorLeakWisps.Length; i++)
		{
			var material = MakeSmokeMaterial(WithAlpha(PuffSmokeColor, 0f));
			var wisp = new SmokeWisp
			{
				Mesh = CreateSmokeMesh($"DoorSmokeLeak_{i}", material),
				Material = material,
				Lifetime = 4f,
				BaseScale = 0.5f,
				Opacity = 1f,
				Active = false
			};
			wisp.Mesh.Visible = false;
			_doorLeakWisps[i] = wisp;
			AddChild(wisp.Mesh);
		}
	}

	private MeshInstance3D CreateSmokeMesh(string name, Material material)
	{
		return new MeshInstance3D
		{
			Name = name,
			Mesh = new QuadMesh { Size = new Vector2(1f, 1f) },
			MaterialOverride = material,
			Layers = StationLighting3D.StudioLayer,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		};
	}

	private StandardMaterial3D MakeSmokeMaterial(Color color)
	{
		return new StandardMaterial3D
		{
			AlbedoColor = color,
			AlbedoTexture = _smokeTexture,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
			TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear
		};
	}

	private Texture2D CreateSmokeTexture()
	{
		var image = Image.Create(SmokeTextureSize, SmokeTextureSize, false, Image.Format.Rgba8);
		var center = new Vector2(SmokeTextureSize * 0.5f, SmokeTextureSize * 0.5f);
		var radius = SmokeTextureSize * 0.49f;

		for (var y = 0; y < SmokeTextureSize; y++)
		{
			for (var x = 0; x < SmokeTextureSize; x++)
			{
				var p = new Vector2(x, y);
				var distance = p.DistanceTo(center) / radius;
				var edge = 1f - Mathf.SmoothStep(0.34f, 1f, distance);
				var swirl = Mathf.Sin(x * 0.13f + y * 0.09f) * 0.16f + Mathf.Sin(x * 0.04f - y * 0.17f) * 0.12f;
				var mottling = 0.76f + swirl + _rng.RandfRange(-0.08f, 0.08f);
				var alpha = Mathf.Clamp(edge * mottling, 0f, 1f);
				image.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
			}
		}

		return ImageTexture.CreateFromImage(image);
	}

	private static Color WithAlpha(Color color, float alpha)
	{
		return new Color(color.R, color.G, color.B, alpha);
	}

	private void UpdateFogMotion()
	{
		if (_fogMaterial == null || _fogVolume == null)
		{
			return;
		}

		var primary = Mathf.Sin(_time * FogMotionSpeed);
		var secondary = Mathf.Sin(_time * FogMotionSpeed * 0.57f + 1.8f);
		var densityOffset = primary * FogMotionStrength + secondary * FogMotionStrength * 0.55f;
		_fogMaterial.Density = AmbientOpacity * Mathf.Clamp(1f + densityOffset, 0.55f, 1.45f);

		_fogVolume.Position = _fogBasePosition + new Vector3(
			primary * FogMotionStrength * 1.15f,
			secondary * FogMotionStrength * 0.32f,
			Mathf.Sin(_time * FogMotionSpeed * 0.72f + 0.9f) * FogMotionStrength * 0.85f
		);

		var sizePulse = 1f + secondary * FogMotionStrength * 0.075f;
		_fogVolume.Size = new Vector3(_fogBaseSize.X * sizePulse, _fogBaseSize.Y, _fogBaseSize.Z * sizePulse);
	}

	private void UpdatePuffs(float dt)
	{
		foreach (var wisp in _puffWisps)
		{
			if (wisp == null || !wisp.Active)
			{
				continue;
			}

			wisp.Age += dt;
			if (wisp.Age < 0f)
			{
				wisp.Mesh.Visible = false;
				continue;
			}

			wisp.Mesh.Visible = true;
			var t = wisp.Age / wisp.Lifetime;
			if (t >= 1f)
			{
				wisp.Active = false;
				wisp.Mesh.Visible = false;
				continue;
			}

			var eased = Mathf.SmoothStep(0f, 1f, t);
			var wobbleX = Mathf.Sin(_time * 1.25f + wisp.Phase) * wisp.Wobble.X * eased;
			var wobbleZ = Mathf.Cos(_time * 0.9f + wisp.Phase) * wisp.Wobble.Y * eased;
			wisp.Mesh.Position = wisp.Origin + (wisp.Drift * eased * DriftSpeed) + new Vector3(wobbleX, 0f, wobbleZ);

			var fadeIn = Mathf.Clamp(t / 0.18f, 0f, 1f);
			var fadeOut = Mathf.Clamp((1f - t) / 0.65f, 0f, 1f);
			var color = PuffSmokeColor;
			color.A = PuffOpacity * wisp.Opacity * fadeIn * fadeOut;
			wisp.Material.AlbedoColor = color;
			var scale = wisp.BaseScale * (1f + eased * 2.6f);
			wisp.Mesh.Scale = new Vector3(scale, scale, scale);
		}
	}

	private void UpdateActiveDoorLeaks(float dt)
	{
		if (_controlStudioLeakActive)
		{
			_controlStudioLeakTimer += dt;
			if (_controlStudioLeakTimer >= DoorLeakInterval)
			{
				_controlStudioLeakTimer = 0f;
				EmitDoorLeak(_controlStudioLeakOrigin, _controlStudioLeakDirection, 2);
			}
		}

		if (_studioHallLeakActive)
		{
			_studioHallLeakTimer += dt;
			if (_studioHallLeakTimer >= DoorLeakInterval)
			{
				_studioHallLeakTimer = 0f;
				EmitDoorLeak(_studioHallLeakOrigin, _studioHallLeakDirection, 2);
			}
		}
	}

	private void UpdateDoorLeakPuffs(float dt)
	{
		foreach (var wisp in _doorLeakWisps)
		{
			if (wisp == null || !wisp.Active)
			{
				continue;
			}

			wisp.Age += dt;
			if (wisp.Age < 0f)
			{
				wisp.Mesh.Visible = false;
				continue;
			}

			wisp.Mesh.Visible = true;
			var t = wisp.Age / wisp.Lifetime;
			if (t >= 1f)
			{
				wisp.Active = false;
				wisp.Mesh.Visible = false;
				continue;
			}

			var eased = Mathf.SmoothStep(0f, 1f, t);
			var wobbleX = Mathf.Sin(_time * 1.1f + wisp.Phase) * wisp.Wobble.X * eased;
			var wobbleZ = Mathf.Cos(_time * 0.8f + wisp.Phase) * wisp.Wobble.Y * eased;
			wisp.Mesh.Position = wisp.Origin + (wisp.Drift * eased) + new Vector3(wobbleX, 0f, wobbleZ);

			var fadeIn = Mathf.Clamp(t / 0.12f, 0f, 1f);
			var fadeOut = Mathf.Clamp((1f - t) / 0.72f, 0f, 1f);
			var color = PuffSmokeColor;
			color.A = DoorLeakOpacity * wisp.Opacity * fadeIn * fadeOut;
			wisp.Material.AlbedoColor = color;
			var scale = wisp.BaseScale * (1f + eased * 1.9f);
			wisp.Mesh.Scale = new Vector3(scale, scale, scale);
		}
	}

	private void SpawnPuffBurst()
	{
		for (var i = 0; i < PuffBurstCount; i++)
		{
			var wisp = _puffWisps[_nextPuffIndex];
			_nextPuffIndex = (_nextPuffIndex + 1) % _puffWisps.Length;

			wisp.Active = true;
			wisp.Mesh.Visible = false;
			wisp.Age = -_rng.RandfRange(0f, 0.6f);
			wisp.Lifetime = _rng.RandfRange(6.5f, 9.5f);
			wisp.Phase = _rng.Randf() * Mathf.Tau;
			wisp.BaseScale = _rng.RandfRange(0.32f, 0.58f);
			wisp.Opacity = _rng.RandfRange(0.65f, 1f);
			wisp.Origin = PuffOrigin + new Vector3(_rng.RandfRange(-0.12f, 0.12f), _rng.RandfRange(-0.05f, 0.08f), _rng.RandfRange(-0.12f, 0.12f));
			wisp.Drift = new Vector3(_rng.RandfRange(-0.7f, 0.85f), _rng.RandfRange(0.85f, 1.45f), _rng.RandfRange(-0.45f, 0.65f));
			wisp.Wobble = new Vector2(_rng.RandfRange(0.08f, 0.3f), _rng.RandfRange(0.05f, 0.22f));
		}
	}
}
