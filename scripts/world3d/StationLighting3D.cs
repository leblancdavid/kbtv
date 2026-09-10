using Godot;

namespace KBTV.World3D;

public static class StationLighting3D
{
	private static readonly Color AmbientColor = new(0.015f, 0.018f, 0.026f);
	private static readonly Color WarmNoir = new(1.0f, 0.78f, 0.52f);
	private static readonly Color WarmDim = new(0.75f, 0.42f, 0.25f);
	private static readonly Color Fluorescent = new(0.62f, 0.78f, 0.95f);
	private static readonly Color FluorescentSickly = new(0.58f, 0.86f, 0.78f);
	private static readonly Color OnAirRed = new(1.0f, 0.12f, 0.08f);
	private static readonly Color FixtureDark = new(0.035f, 0.032f, 0.03f);

	public static Node3D Build()
	{
		var root = new Node3D { Name = "StationLighting3D" };
		AddEnvironment(root);
		AddControlRoomLights(root);
		AddStudioLights(root);
		AddStationFluorescents(root);
		return root;
	}

	private static void AddEnvironment(Node3D root)
	{
		var environment = new Environment
		{
			BackgroundMode = Environment.BGMode.Color,
			BackgroundColor = new Color(0.006f, 0.007f, 0.01f),
			AmbientLightSource = Environment.AmbientSource.Color,
			AmbientLightColor = AmbientColor,
			AmbientLightEnergy = 0.32f,
			TonemapMode = Environment.ToneMapper.Filmic,
			TonemapExposure = 1.05f,
			TonemapWhite = 1.6f
		};

		root.AddChild(new WorldEnvironment
		{
			Name = "NoirWorldEnvironment",
			Environment = environment
		});
	}

	private static void AddControlRoomLights(Node3D root)
	{
		AddOverheadSpot(root, "ControlDeskOverhead", new Vector3(0.8f, 3.2f, 0.4f), WarmNoir, 2.8f, 8.5f, 54f, true);
		AddOverheadSpot(root, "ControlShelvesOverhead", new Vector3(0.0f, 2.9f, 6.4f), WarmDim, 1.25f, 6.8f, 58f, false);
		AddPendantFixture(root, "ControlDeskPendant", new Vector3(0.8f, 3.08f, 0.4f), WarmNoir);
		AddPendantFixture(root, "ControlShelvesPendant", new Vector3(0.0f, 2.78f, 6.4f), WarmDim);
		AddOmni(root, "ControlEquipmentGlow", new Vector3(1.0f, 1.0f, 0.35f), new Color(0.05f, 0.7f, 0.28f), 0.45f, 3.1f, false);
		AddOmni(root, "ControlOnAirRedGlow", new Vector3(0f, 1.4f, -0.1f), OnAirRed, 0.55f, 2.6f, false);
	}

	private static void AddStudioLights(Node3D root)
	{
		AddOverheadSpot(root, "StudioTableOverhead", new Vector3(0.6f, 3.35f, -2.8f), WarmNoir, 2.65f, 8.0f, 54f, true);
		AddOverheadSpot(root, "StudioBackOverhead", new Vector3(0.0f, 2.9f, -6.7f), WarmDim, 1.1f, 6.4f, 60f, false);
		AddPendantFixture(root, "StudioTablePendant", new Vector3(0.6f, 3.23f, -2.8f), WarmNoir);
		AddPendantFixture(root, "StudioBackPendant", new Vector3(0.0f, 2.78f, -6.7f), WarmDim);
		AddOmni(root, "StudioOnAirRedGlow", new Vector3(-0.85f, 1.1f, -3.55f), OnAirRed, 0.35f, 2.3f, false);
	}

	private static void AddStationFluorescents(Node3D root)
	{
		AddFluorescent(root, "HallFluorescentNorth", new Vector3(6.5f, 2.55f, -11.5f), 1.35f, 5.5f);
		AddFluorescent(root, "HallFluorescentMiddle", new Vector3(6.5f, 2.55f, -3.0f), 1.15f, 5.5f);
		AddFluorescent(root, "HallFluorescentSouth", new Vector3(6.5f, 2.55f, 4.8f), 1.2f, 5.5f);

		AddFluorescent(root, "EquipmentFluorescent", new Vector3(0.0f, 2.45f, -11.0f), 0.9f, 5.0f, FluorescentSickly);
		AddFluorescent(root, "ArchiveFluorescent", new Vector3(12.0f, 2.45f, -11.0f), 0.85f, 4.7f);
		AddFluorescent(root, "OfficeFluorescent", new Vector3(12.0f, 2.4f, -2.5f), 0.8f, 4.2f);
		AddFluorescent(root, "KitchenFluorescent", new Vector3(12.0f, 2.4f, 2.5f), 0.95f, 4.4f, FluorescentSickly);
		AddFluorescent(root, "BathroomFluorescent", new Vector3(12.0f, 2.35f, 6.5f), 0.75f, 3.4f, FluorescentSickly);
		AddFluorescent(root, "FrontDeskFluorescent", new Vector3(21.0f, 2.45f, -11.4f), 1.0f, 5.2f);
		AddFluorescent(root, "LobbyFluorescent", new Vector3(21.0f, 2.5f, -4.8f), 1.1f, 6.2f);
	}

	private static void AddFluorescent(Node3D root, string name, Vector3 position, float energy, float range, Color? color = null)
	{
		var lightColor = color ?? Fluorescent;
		AddOmni(root, $"{name}Fill", position + new Vector3(0f, -0.35f, 0f), lightColor, energy * 1.35f, range, false);
		AddOverheadSpot(root, $"{name}Wash", position, lightColor, energy * 0.55f, range * 1.1f, 82f, false);
		AddLightBar(root, $"{name}Fixture", position + new Vector3(0f, -0.06f, 0f), lightColor);
	}

	private static void AddOverheadSpot(Node3D root, string name, Vector3 position, Color color, float energy, float range, float angle, bool shadows)
	{
		var light = new SpotLight3D
		{
			Name = name,
			Position = position,
			RotationDegrees = new Vector3(-90f, 0f, 0f),
			LightColor = color,
			LightEnergy = energy,
			LightIndirectEnergy = 0.15f,
			ShadowEnabled = shadows,
			SpotRange = range,
			SpotAngle = angle,
			SpotAngleAttenuation = shadows ? 1.5f : 1.0f
		};
		root.AddChild(light);
	}

	private static void AddOmni(Node3D root, string name, Vector3 position, Color color, float energy, float range, bool shadows)
	{
		var light = new OmniLight3D
		{
			Name = name,
			Position = position,
			LightColor = color,
			LightEnergy = energy,
			LightIndirectEnergy = 0f,
			ShadowEnabled = shadows,
			OmniRange = range,
			OmniAttenuation = 1.8f
		};
		root.AddChild(light);
	}

	private static void AddLightBar(Node3D root, string name, Vector3 position, Color color)
	{
		var material = new StandardMaterial3D
		{
			AlbedoColor = new Color(color.R, color.G, color.B, 1f),
			EmissionEnabled = true,
			Emission = color,
			EmissionEnergyMultiplier = 0.45f
		};

		var fixture = new MeshInstance3D
		{
			Name = name,
			Position = position,
			Mesh = new BoxMesh { Size = new Vector3(1.45f, 0.035f, 0.22f) },
			MaterialOverride = material
		};
		DisableMeshShadows(fixture);
		root.AddChild(fixture);
	}

	private static void AddPendantFixture(Node3D root, string name, Vector3 position, Color glowColor)
	{
		var fixtureRoot = new Node3D { Name = name, Position = position };
		var darkMaterial = new StandardMaterial3D { AlbedoColor = FixtureDark };
		var glowMaterial = new StandardMaterial3D
		{
			AlbedoColor = new Color(glowColor.R, glowColor.G, glowColor.B, 1f),
			EmissionEnabled = true,
			Emission = glowColor,
			EmissionEnergyMultiplier = 0.25f
		};

		var cord = new MeshInstance3D
		{
			Name = "Cord",
			Position = new Vector3(0f, 0.32f, 0f),
			Mesh = new CylinderMesh { TopRadius = 0.025f, BottomRadius = 0.025f, Height = 0.75f },
			MaterialOverride = darkMaterial
		};
		DisableMeshShadows(cord);
		fixtureRoot.AddChild(cord);

		var shade = new MeshInstance3D
		{
			Name = "Shade",
			Mesh = new CylinderMesh { TopRadius = 0.18f, BottomRadius = 0.42f, Height = 0.28f },
			MaterialOverride = darkMaterial
		};
		DisableMeshShadows(shade);
		fixtureRoot.AddChild(shade);

		var bulb = new MeshInstance3D
		{
			Name = "BulbGlow",
			Position = new Vector3(0f, -0.12f, 0f),
			Mesh = new SphereMesh { Radius = 0.12f, Height = 0.24f },
			MaterialOverride = glowMaterial
		};
		DisableMeshShadows(bulb);
		fixtureRoot.AddChild(bulb);

		root.AddChild(fixtureRoot);
	}

	private static void DisableMeshShadows(GeometryInstance3D mesh)
	{
		mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
	}
}
