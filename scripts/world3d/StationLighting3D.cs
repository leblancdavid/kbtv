using Godot;

namespace KBTV.World3D;

public partial class StationLighting3D : Node3D
{
	public const uint ControlLayer = 1u << 0;
	public const uint StudioLayer = 1u << 1;
	public const uint EquipmentLayer = 1u << 2;
	public const uint StationLayer = 1u << 3;
	public const uint ExteriorLayer = 1u << 4;
	public const uint AllInteriorLayers = ControlLayer | StudioLayer | EquipmentLayer | StationLayer;

	private static readonly Color AmbientColor = new(0.015f, 0.018f, 0.026f);
	private static readonly Color WarmNoir = new(1.0f, 0.78f, 0.52f);
	private static readonly Color Fluorescent = new(0.88f, 0.93f, 1.0f);
	private static readonly Color OnAirRed = new(1.0f, 0.12f, 0.08f);
	private static readonly Color FixtureDark = new(0.035f, 0.032f, 0.03f);
	private readonly Godot.Collections.Array<Light3D> _controlLights = new();
	private readonly Godot.Collections.Array<Light3D> _studioLights = new();
	private readonly Godot.Collections.Array<Light3D> _equipmentLights = new();
	private readonly Godot.Collections.Array<Light3D> _stationLights = new();
	private readonly Godot.Collections.Array<SpotLight3D> _fluorescentShadowLights = new();
	private readonly Godot.Collections.Dictionary<string, Godot.Collections.Array<Light3D>> _doorSpillLights = new();

	public void Build()
	{
		Name = "StationLighting3D";
		AddEnvironment(this);
		AddControlRoomLights(this);
		AddStudioLights(this);
		AddStationFluorescents(this);
		AddDoorwaySpillLights(this);
		RefreshLightMasks();
	}

	public void SetDoorLightLink(string roomA, string roomB, bool isOpen)
	{
		if (!_doorSpillLights.TryGetValue(MakeDoorLinkKey(roomA, roomB), out var spillLights))
		{
			return;
		}

		foreach (var spillLight in spillLights)
		{
			spillLight.Visible = isOpen;
		}
	}

	public static void ApplyLayerToTree(Node node, uint layerMask)
	{
		if (node is VisualInstance3D visual)
		{
			visual.Layers = layerMask;
		}

		foreach (var child in node.GetChildren())
		{
			ApplyLayerToTree(child, layerMask);
		}
	}

	public void UpdateFluorescentShadowCaster(Vector3 playerPosition)
	{
		SpotLight3D? closestLight = null;
		var closestDistanceSquared = float.MaxValue;

		foreach (var light in _fluorescentShadowLights)
		{
			var distanceSquared = light.GlobalPosition.DistanceSquaredTo(playerPosition);
			if (distanceSquared < closestDistanceSquared)
			{
				closestDistanceSquared = distanceSquared;
				closestLight = light;
			}
		}

		foreach (var light in _fluorescentShadowLights)
		{
			light.ShadowEnabled = light == closestLight;
		}
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
			VolumetricFogEnabled = true,
			VolumetricFogDensity = 0.0f,
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

	private void AddControlRoomLights(Node3D root)
	{
		_controlLights.Add(AddOverheadSpot(root, "ControlRoomOverhead", new Vector3(0f, 3.25f, 4f), WarmNoir, 10.0f, 100.0f, 80f, true));
		AddPendantFixture(root, "ControlRoomPendant", new Vector3(0f, 3.12f, 4f), WarmNoir);
		_controlLights.Add(AddOmni(root, "ControlEquipmentGlow", new Vector3(1.0f, 1.0f, 0.35f), new Color(0.05f, 0.7f, 0.28f), 0.45f, 3.1f, false));
		_controlLights.Add(AddOmni(root, "ControlOnAirRedGlow", new Vector3(0f, 1.4f, -0.1f), OnAirRed, 0.55f, 2.6f, false));
	}

	private void AddStudioLights(Node3D root)
	{
		_studioLights.Add(AddOverheadSpot(root, "StudioRoomOverhead", new Vector3(0f, 3.25f, -4f), WarmNoir, 10.0f, 100.0f, 80f, true));
		AddPendantFixture(root, "StudioRoomPendant", new Vector3(0f, 3.12f, -4f), WarmNoir);
		_studioLights.Add(AddOmni(root, "StudioOnAirRedGlow", new Vector3(-0.85f, 1.1f, -3.55f), OnAirRed, 0.35f, 2.3f, false));
	}

	private void AddEquipmentRoomLights(Node3D root)
	{
		_equipmentLights.Add(AddOverheadSpot(root, "EquipmentRoomOverhead", new Vector3(0f, 3.05f, -11f), WarmNoir, 10.0f, 100.0f, 80f, true));
		AddPendantFixture(root, "EquipmentRoomPendant", new Vector3(0f, 2.92f, -11f), WarmNoir);
	}

	private void AddStationFluorescents(Node3D root)
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

	private void AddDoorwaySpillLights(Node3D root)
	{
		AddDoorSpillPair(root, "Control", "Studio", new Vector3(-4.15f, 1.05f, 0f), StationLighting3D.ControlLayer, StationLighting3D.StudioLayer);
		AddDoorSpillPair(root, "Control", "Station", new Vector3(5f, 1.05f, 5.4f), StationLighting3D.ControlLayer, StationLighting3D.StationLayer);
		AddDoorSpillPair(root, "Studio", "Station", new Vector3(5f, 1.05f, -4f), StationLighting3D.StudioLayer, StationLighting3D.StationLayer);
		AddDoorSpillPair(root, "Equipment", "Station", new Vector3(5f, 1.05f, -11f), StationLighting3D.EquipmentLayer, StationLighting3D.StationLayer);
	}

	private void AddDoorSpillPair(Node3D root, string roomA, string roomB, Vector3 position, uint layerA, uint layerB)
	{
		var lights = new Godot.Collections.Array<Light3D>
		{
			AddDoorSpill(root, $"{roomA}To{roomB}DoorSpill", position, GetRoomSpillColor(roomA), layerB),
			AddDoorSpill(root, $"{roomB}To{roomA}DoorSpill", position, GetRoomSpillColor(roomB), layerA)
		};

		_doorSpillLights[MakeDoorLinkKey(roomA, roomB)] = lights;
	}

	private static OmniLight3D AddDoorSpill(Node3D root, string name, Vector3 position, Color color, uint targetLayer)
	{
		var light = AddOmni(root, name, position, color, 2.4f, 2.6f, false);
		light.LightCullMask = targetLayer;
		light.OmniAttenuation = 2.8f;
		light.Visible = false;
		return light;
	}

	private static Color GetRoomSpillColor(string room)
	{
		return room == "Station" ? Fluorescent : WarmNoir;
	}

	private void AddFluorescent(Node3D root, string name, Vector3 position, float energy, float range)
	{
		_stationLights.Add(AddOmni(root, $"{name}Fill", position + new Vector3(0f, -0.35f, 0f), Fluorescent, energy * 1.85f, range * 1.15f, false));
		var wash = AddOverheadSpot(root, $"{name}Wash", position, Fluorescent, energy * 0.8f, range * 1.25f, 88f, false);
		_stationLights.Add(wash);
		_fluorescentShadowLights.Add(wash);
	}

	private static SpotLight3D AddOverheadSpot(Node3D root, string name, Vector3 position, Color color, float energy, float range, float angle, bool shadows)
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
		return light;
	}

	private static OmniLight3D AddOmni(Node3D root, string name, Vector3 position, Color color, float energy, float range, bool shadows)
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
		return light;
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

	private void RefreshLightMasks()
	{
		SetLightMasks(_controlLights, ControlLayer);
		SetLightMasks(_studioLights, StudioLayer);
		SetLightMasks(_equipmentLights, EquipmentLayer);
		SetLightMasks(_stationLights, StationLayer);
	}

	private static void SetLightMasks(Godot.Collections.Array<Light3D> lights, uint mask)
	{
		foreach (var light in lights)
		{
			light.LightCullMask = mask;
		}
	}

	private static string MakeDoorLinkKey(string roomA, string roomB)
	{
		return string.CompareOrdinal(roomA, roomB) <= 0 ? $"{roomA}:{roomB}" : $"{roomB}:{roomA}";
	}
}
