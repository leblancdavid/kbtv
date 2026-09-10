using Godot;

namespace KBTV.World3D;

public static class StationLighting3D
{
	private static readonly Color AmbientColor = new(0.015f, 0.018f, 0.026f);
	private static readonly Color WarmNoir = new(1.0f, 0.78f, 0.52f);
	private static readonly Color Fluorescent = new(0.88f, 0.93f, 1.0f);
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
			AmbientLightEnergy = 0.8f,
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
		AddOverheadSpot(root, "ControlRoomOverhead", new Vector3(0f, 3.25f, 4f), WarmNoir, 10.0f, 100.0f, 80f, true);
		AddPendantFixture(root, "ControlRoomPendant", new Vector3(0f, 3.12f, 4f), WarmNoir);
		AddOmni(root, "ControlEquipmentGlow", new Vector3(1.0f, 1.0f, 0.35f), new Color(0.05f, 0.7f, 0.28f), 0.45f, 3.1f, false);
		AddOmni(root, "ControlOnAirRedGlow", new Vector3(0f, 1.4f, -0.1f), OnAirRed, 0.55f, 2.6f, false);
	}

	private static void AddStudioLights(Node3D root)
	{
		AddOverheadSpot(root, "StudioRoomOverhead", new Vector3(0f, 3.25f, -4f), WarmNoir, 10.0f, 100.0f, 80f, true);
		AddPendantFixture(root, "StudioRoomPendant", new Vector3(0f, 3.12f, -4f), WarmNoir);
		AddOmni(root, "StudioOnAirRedGlow", new Vector3(-0.85f, 1.1f, -3.55f), OnAirRed, 0.35f, 2.3f, false);
	}

	private static void AddEquipmentRoomLights(Node3D root)
	{
		AddOverheadSpot(root, "EquipmentRoomOverhead", new Vector3(0f, 3.05f, -11f), WarmNoir, 10.0f, 100.0f, 80f, true);
		AddPendantFixture(root, "EquipmentRoomPendant", new Vector3(0f, 2.92f, -11f), WarmNoir);
	}

	private static void AddStationFluorescents(Node3D root)
	{
		AddEquipmentRoomLights(root);

		AddFluorescent(root, "HallFluorescentNorth", new Vector3(6.5f, 2.55f, -11.5f), 5.0f, 20.0f);
		AddFluorescent(root, "HallFluorescentMiddle", new Vector3(6.5f, 2.55f, -3.0f), 5.0f, 20.0f);
		AddFluorescent(root, "HallFluorescentSouth", new Vector3(6.5f, 2.55f, 4.8f), 5.0f, 20.0f);

		AddFluorescent(root, "ArchiveFluorescent", new Vector3(12.0f, 2.45f, -11.0f), 5.0f, 20.0f);
		AddFluorescent(root, "OfficeFluorescent", new Vector3(12.0f, 2.4f, -2.5f), 5.0f, 20.0f);
		AddFluorescent(root, "KitchenFluorescent", new Vector3(12.0f, 2.4f, 2.5f), 5.0f, 20.0f);
		AddFluorescent(root, "BathroomFluorescent", new Vector3(12.0f, 2.35f, 6.5f), 5.0f, 20.0f);
		AddFluorescent(root, "FrontDeskFluorescent", new Vector3(21.0f, 2.45f, -11.4f), 5.0f, 20.0f);
		AddFluorescent(root, "LobbyFluorescent", new Vector3(21.0f, 2.5f, -4.8f), 5.0f, 20.0f);
	}

	private static void AddFluorescent(Node3D root, string name, Vector3 position, float energy, float range)
	{
		AddOmni(root, $"{name}Fill", position + new Vector3(0f, -0.35f, 0f), Fluorescent, energy * 1.85f, range * 1.15f, false);
		AddOverheadSpot(root, $"{name}Wash", position, Fluorescent, energy * 0.8f, range * 1.25f, 88f, false);
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
