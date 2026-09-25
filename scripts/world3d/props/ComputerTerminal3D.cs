using Godot;

namespace KBTV.World3D;

/// <summary>
/// Invisible helper for the control room CRT. It owns the screen plane used to
/// project the zoomed UI overlay and hosts the proximity trigger used to open it.
/// </summary>
public partial class ComputerTerminal3D : Node3D
{
	public const float ScreenWidth = 0.74f;
	public const float ScreenHeight = 0.42f;

	public Area3D InteractionArea { get; private set; } = null!;
	public MeshInstance3D ScreenMesh { get; private set; } = null!;
	public StaticBody3D ScreenBody { get; private set; } = null!;
	public OmniLight3D ScreenLight { get; private set; } = null!;
	public bool IsPlayerInRange { get; private set; }

	public Vector3 ScreenCenter => ScreenMesh.GlobalPosition;

	public const float ScreenZOffset = 0.105f;
	public const float ScreenCenterY = 0.385f;
	private const float ScreenLightZOffset = 0.24f;

	/// <summary>CRT screen centre in the computer model's local space (tuned so the
	/// projected UI sits exactly on the phosphor surface).</summary>
	public static readonly Vector3 ModelScreenCenterOffset = new(0f, 0.335f, -0.105f);

private static readonly Color ScreenOffColor = new(0.02f, 0.03f, 0.04f);

	[ExportGroup("CRT Flicker")]
	[Export] public bool EnableCrtFlicker { get; set; } = true;
	[Export] public float FlickerBaseEnergy { get; set; } = 0.11f;
	[Export] public float FlickerVariation { get; set; } = 0.035f;
	[Export] public float FlickerInterval { get; set; } = 0.16f;
	[Export] public float FlickerSmoothSpeed { get; set; } = 9f;
	[Export] public float DipChancePerSecond { get; set; } = 0.04f;
	[Export] public float DipDuration { get; set; } = 0.22f;
	[Export] public float DipLevel { get; set; } = 0.55f;
	[Export] public float DipSmoothSpeed { get; set; } = 12f;

	private readonly System.Random _random = new();
	private double _flickerTime;
	private double _nextFlickerChange;
	private float _currentLevel = 1f;
	private float _targetLevel = 1f;
	private float _dipRemaining = -1f;
	private float _dipBlend;

	public override void _Ready()
	{
		BuildMesh();
		BuildInteractionArea();
	}

	public override void _Process(double delta)
	{
		if (!EnableCrtFlicker || ScreenLight == null)
		{
			return;
		}

		_flickerTime += delta;

		if (_flickerTime >= _nextFlickerChange)
		{
			_targetLevel = 1f - (float)_random.NextDouble() * FlickerVariation;
			_nextFlickerChange = _flickerTime + FlickerInterval * (0.5f + (float)_random.NextDouble());
		}


		var dt = (float)delta;
		var flickerWeight = 1f - Mathf.Exp(-FlickerSmoothSpeed * dt);
		_currentLevel = Mathf.Lerp(_currentLevel, _targetLevel, flickerWeight);

		if (_dipRemaining < 0f && _random.NextDouble() < DipChancePerSecond * delta)
		{
			_dipRemaining = DipDuration;
		}

		var dipTarget = 0f;
		if (_dipRemaining >= 0f)
		{
			_dipRemaining -= dt;
			dipTarget = 1f;
		}
		_dipBlend = Mathf.Lerp(_dipBlend, dipTarget, 1f - Mathf.Exp(-DipSmoothSpeed * dt));

		var level = Mathf.Lerp(_currentLevel, Mathf.Min(_currentLevel, DipLevel), _dipBlend);

		ScreenLight.LightEnergy = FlickerBaseEnergy * level;
	}

	private void BuildMesh()
	{
		var root = new Node3D { Name = "ComputerScreenAnchor" };

		// Screen surface (dark by default; World3D overrides with the live UI texture)
		var screen = new MeshInstance3D { Name = "Screen" };
		screen.Mesh = new PlaneMesh
		{
			Size = new Vector2(ScreenWidth, ScreenHeight),
			Material = MakeMaterial(ScreenOffColor)
		};
		screen.Position = new Vector3(0f, ScreenCenterY, ScreenZOffset);
		screen.Visible = false;
		root.AddChild(screen);
		ScreenMesh = screen;

		// Screen collider used for raycasting mouse input onto the UI
		ScreenBody = new StaticBody3D { Name = "ScreenCollider", Position = screen.Position };
		var shapeNode = new CollisionShape3D
		{
			Shape = new BoxShape3D { Size = new Vector3(ScreenWidth, ScreenHeight, 0.02f) }
		};
		ScreenBody.AddChild(shapeNode);
		root.AddChild(ScreenBody);

ScreenLight = new OmniLight3D
		{
			Name = "ScreenLight",
			Position = new Vector3(0f, ScreenCenterY, ScreenZOffset + ScreenLightZOffset),
			LightColor = new Color(0.72f, 0.86f, 1.0f),
			LightEnergy = FlickerBaseEnergy,
			LightIndirectEnergy = 0f,
			OmniRange = 1.5f,
			OmniAttenuation = 3.2f,
			ShadowEnabled = false,
			Visible = true
		};
		root.AddChild(ScreenLight);

		AddChild(root);
	}

	public void SetScreenLightEnabled(bool enabled)
	{
		if (ScreenLight != null)
		{
			ScreenLight.Visible = enabled;
		}
	}

	public static void ConfigureGlassMaterial(Node3D model)
	{
		// The exported CRT is merged by material, not split into named glass nodes.
		// Override only the phosphor surface on this instance, never the shared GLB.
		var matched = false;
		foreach (var node in model.FindChildren("*", "MeshInstance3D", true, false))
		{
			if (node is not MeshInstance3D mesh || mesh.Mesh == null) continue;
			for (var surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
			{
				if (mesh.GetActiveMaterial(surface) is not StandardMaterial3D source
					|| !source.ResourceName.StartsWith("Cool phosphor", System.StringComparison.Ordinal)) continue;

				var glass = (StandardMaterial3D)source.Duplicate();
				glass.ResourceName = "CRT dark phosphor glass";
				glass.AlbedoColor = new Color(0.012f, 0.020f, 0.018f);
				glass.EmissionEnabled = false;
				glass.Metallic = 0f;
				glass.MetallicSpecular = 0.05f;
				glass.Roughness = 0.8f;
				mesh.SetSurfaceOverrideMaterial(surface, glass);
				matched = true;
			}
		}
		if (!matched) GD.PushWarning("CRT: no Cool phosphor surface found for dark glass treatment.");
	}

	private void BuildInteractionArea()
	{
		InteractionArea = new Area3D
		{
			Name = "ComputerInteractionArea",
			Position = new Vector3(0f, 1.0f, 1.3f)
		};
		var shape = new CollisionShape3D
		{
			Shape = new BoxShape3D { Size = new Vector3(2.0f, 1.4f, 2.0f) }
		};
		InteractionArea.AddChild(shape);
		InteractionArea.BodyEntered += OnBodyEntered;
		InteractionArea.BodyExited += OnBodyExited;
		AddChild(InteractionArea);
	}

	private void OnBodyEntered(Node body)
	{
		if (body.IsInGroup("player"))
		{
			IsPlayerInRange = true;
		}
	}

	private void OnBodyExited(Node body)
	{
		if (body.IsInGroup("player"))
		{
			IsPlayerInRange = false;
		}
	}

	private static StandardMaterial3D MakeMaterial(Color color)
	{
		return new StandardMaterial3D { AlbedoColor = color };
	}
}
