using Godot;

namespace KBTV.World3D;

public partial class StudioSmoke3D : Node3D
{
	[Export] public int PuffBurstCount { get; set; } = 5;
	[Export] public float PuffInterval { get; set; } = 4.0f;
	[Export] public float AmbientOpacity { get; set; } = 0.16f;
	[Export] public float PuffOpacity { get; set; } = 0.16f;
	[Export] public float DriftSpeed { get; set; } = 0.45f;
	[Export] public Vector3 PuffOrigin { get; set; } = new(-0.85f, 1.25f, 0.35f);
	[Export] public Vector3 RoomHalfExtents { get; set; } = new(4.4f, 1.7f, 3.4f);

	private const int MaxPuffs = 36;
	private const int SmokeTextureSize = 128;
	private const int HazeTextureSize = 256;
	private const int HazeVeilCount = 3;
	private static readonly Color FogColor = new(0.62f, 0.6f, 0.56f);
	private static readonly Color HazeColor = new(0.64f, 0.62f, 0.58f, 1f);
	private static readonly Color PuffSmokeColor = new(0.66f, 0.68f, 0.7f, 1f);
	private readonly RandomNumberGenerator _rng = new();
	private readonly MeshInstance3D[] _hazeVeils = new MeshInstance3D[HazeVeilCount];
	private readonly StandardMaterial3D[] _hazeMaterials = new StandardMaterial3D[HazeVeilCount];
	private readonly SmokeWisp[] _puffWisps = new SmokeWisp[MaxPuffs];
	private Texture2D _smokeTexture = null!;
	private Texture2D _hazeTexture = null!;
	private float _time;
	private float _puffTimer;
	private int _nextPuffIndex;

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
		_hazeTexture = CreateHazeTexture();

		CreateFogVolume();
		CreateHazeVeils();
		CreatePuffPool();
		SpawnPuffBurst();
		_puffTimer = PuffInterval * 0.65f;
	}

	public override void _Process(double delta)
	{
		var dt = (float)delta;
		_time += dt;
		_puffTimer += dt;

		UpdateHazeVeils();
		UpdatePuffs(dt);

		if (_puffTimer >= PuffInterval)
		{
			_puffTimer = 0f;
			SpawnPuffBurst();
		}
	}

	private void CreateFogVolume()
	{
		var material = new FogMaterial
		{
			Albedo = FogColor,
			Density = AmbientOpacity,
			HeightFalloff = 0.01f,
			EdgeFade = 1.0f
		};

		var fog = new FogVolume
		{
			Name = "StudioRoomHaze",
			Shape = RenderingServer.FogVolumeShape.Box,
			Size = new Vector3(RoomHalfExtents.X * 2f, RoomHalfExtents.Y * 2f, RoomHalfExtents.Z * 2f),
			Position = new Vector3(0f, RoomHalfExtents.Y * 0.58f, 0f),
			Material = material,
			Layers = StationLighting3D.StudioLayer
		};
		AddChild(fog);
	}

	private void CreateHazeVeils()
	{
		for (var i = 0; i < HazeVeilCount; i++)
		{
			var material = MakeHazeMaterial(0f);
			var veil = new MeshInstance3D
			{
				Name = $"StudioFogVeil_{i}",
				Mesh = new QuadMesh { Size = new Vector2(RoomHalfExtents.X * 2.05f, RoomHalfExtents.Y * 1.35f) },
				MaterialOverride = material,
				Layers = StationLighting3D.StudioLayer,
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
			};

			veil.Position = new Vector3(0f, RoomHalfExtents.Y * (0.45f + i * 0.18f), -RoomHalfExtents.Z + i * RoomHalfExtents.Z);
			_hazeVeils[i] = veil;
			_hazeMaterials[i] = material;
			AddChild(veil);
		}
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

	private StandardMaterial3D MakeHazeMaterial(float alpha)
	{
		return new StandardMaterial3D
		{
			AlbedoColor = WithAlpha(HazeColor, alpha),
			AlbedoTexture = _hazeTexture,
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

	private Texture2D CreateHazeTexture()
	{
		var image = Image.Create(HazeTextureSize, HazeTextureSize, false, Image.Format.Rgba8);

		for (var y = 0; y < HazeTextureSize; y++)
		{
			for (var x = 0; x < HazeTextureSize; x++)
			{
				var u = x / (float)(HazeTextureSize - 1);
				var v = y / (float)(HazeTextureSize - 1);
				var edgeX = Mathf.SmoothStep(0f, 0.18f, u) * (1f - Mathf.SmoothStep(0.82f, 1f, u));
				var edgeY = Mathf.SmoothStep(0f, 0.2f, v) * (1f - Mathf.SmoothStep(0.8f, 1f, v));
				var noise = Mathf.Sin(x * 0.035f + y * 0.027f) * 0.08f + Mathf.Sin(x * 0.011f - y * 0.019f) * 0.05f;
				var alpha = Mathf.Clamp((0.78f + noise) * edgeX * edgeY, 0f, 1f);
				image.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
			}
		}

		return ImageTexture.CreateFromImage(image);
	}

	private static Color WithAlpha(Color color, float alpha)
	{
		return new Color(color.R, color.G, color.B, alpha);
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

	private void UpdateHazeVeils()
	{
		var baseAlpha = AmbientOpacity * 0.38f;
		for (var i = 0; i < _hazeMaterials.Length; i++)
		{
			var material = _hazeMaterials[i];
			var veil = _hazeVeils[i];
			if (material == null || veil == null)
			{
				continue;
			}

			var alpha = baseAlpha * (0.78f + Mathf.Sin(_time * 0.16f + i * 1.7f) * 0.08f);
			material.AlbedoColor = WithAlpha(HazeColor, alpha);
			veil.Position = new Vector3(
				Mathf.Sin(_time * 0.055f + i) * 0.18f,
				veil.Position.Y,
				veil.Position.Z
			);
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
