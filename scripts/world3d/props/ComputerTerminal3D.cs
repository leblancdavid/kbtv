using Godot;

namespace KBTV.World3D;

/// <summary>
/// Greybox computer terminal for the control room. Builds a simple procedural
/// monitor (housing + screen surface) with a keyboard/mouse, and hosts the
/// proximity trigger the player uses to open the caller screening view.
/// The screen surface accepts a SubViewport texture (set by World3D) so the
/// live UI is rendered directly onto the monitor.
/// </summary>
public partial class ComputerTerminal3D : Node3D
{
	public const float ScreenWidth = 0.9f;
	public const float ScreenHeight = 0.5f;

	public Area3D InteractionArea { get; private set; } = null!;
	public MeshInstance3D ScreenMesh { get; private set; } = null!;
	public StaticBody3D ScreenBody { get; private set; } = null!;
	public bool IsPlayerInRange { get; private set; }

	public Vector3 ScreenCenter => ScreenMesh.GlobalPosition;

	private const float ScreenZOffset = 0.10f;
	private const float ScreenCenterY = 0.35f;

	private static readonly Color MonitorColor = new(0.16f, 0.16f, 0.18f);
	private static readonly Color BezelColor = new(0.20f, 0.20f, 0.22f);
	private static readonly Color KeyboardColor = new(0.12f, 0.12f, 0.14f);
	private static readonly Color ScreenOffColor = new(0.02f, 0.03f, 0.04f);

	public override void _Ready()
	{
		BuildMesh();
		BuildInteractionArea();
	}

	private void BuildMesh()
	{
		var root = new Node3D { Name = "GreyboxComputer" };

		// Monitor stand / base
		AddBox(root, "Base", new Vector3(0f, 0.04f, 0f), new Vector3(0.36f, 0.08f, 0.36f), MonitorColor);

		// Stand neck
		AddBox(root, "Neck", new Vector3(0f, 0.17f, 0f), new Vector3(0.09f, 0.22f, 0.09f), MonitorColor);

		// Monitor housing
		AddBox(root, "Housing", new Vector3(0f, ScreenCenterY, 0f), new Vector3(1.0f, 0.62f, 0.14f), MonitorColor);

		// Bezel frame just behind the screen glass
		AddBox(root, "Bezel", new Vector3(0f, ScreenCenterY, 0.058f), new Vector3(0.98f, 0.56f, 0.02f), BezelColor);

		// Screen surface (dark by default; World3D overrides with the live UI texture)
		var screen = new MeshInstance3D { Name = "Screen" };
		screen.Mesh = new PlaneMesh
		{
			Size = new Vector2(ScreenWidth, ScreenHeight),
			Material = MakeMaterial(ScreenOffColor)
		};
		screen.Position = new Vector3(0f, ScreenCenterY, ScreenZOffset);
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

		// Keyboard + mouse on the desk in front of the monitor
		AddBox(root, "Keyboard", new Vector3(0f, 0.03f, 0.42f), new Vector3(0.6f, 0.06f, 0.3f), KeyboardColor);
		AddBox(root, "Mouse", new Vector3(-0.24f, 0.03f, 0.3f), new Vector3(0.12f, 0.03f, 0.18f), KeyboardColor);

		AddChild(root);
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

	private static void AddBox(Node3D parent, string name, Vector3 position, Vector3 size, Color color)
	{
		var mesh = new MeshInstance3D { Name = name, Position = position };
		mesh.Mesh = new BoxMesh { Size = size };
		mesh.MaterialOverride = MakeMaterial(color);
		parent.AddChild(mesh);
	}

	private static StandardMaterial3D MakeMaterial(Color color)
	{
		return new StandardMaterial3D { AlbedoColor = color };
	}
}