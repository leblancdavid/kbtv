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

	private const float ScreenZOffset = 0.105f;
	private const float ScreenCenterY = 0.385f;
	private const float ScreenLightZOffset = 0.24f;

	private static readonly Color ScreenOffColor = new(0.02f, 0.03f, 0.04f);

	public override void _Ready()
	{
		BuildMesh();
		BuildInteractionArea();
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
			LightEnergy = 0.38f,
			LightIndirectEnergy = 0f,
			OmniRange = 1.5f,
			OmniAttenuation = 2.8f,
			ShadowEnabled = false,
			Visible = false
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
