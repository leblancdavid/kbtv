using Godot;
using System.Collections.Generic;

namespace KBTV.World3D;

public partial class StationGreybox3D : Node3D
{
	private const float WallHeight = 2.3f;
	private const float WallThickness = 0.2f;
	private const float DoorGap = 2f;
	private const float WallCenterY = WallHeight / 2f;
	private const float WallOpaqueAlpha = 1f;
	private const float WallFadeAlpha = 0.32f;
	private const float WallFadeSpeed = 8f;

	private readonly Rect2 _hallway = new(new Vector2(5f, -14f), new Vector2(3f, 22f));
	private readonly Rect2 _equipmentRoom = new(new Vector2(-5f, -14f), new Vector2(10f, 6f));
	private readonly Rect2 _archive = new(new Vector2(8f, -14f), new Vector2(8f, 6f));
	private readonly Rect2 _office = new(new Vector2(8f, -5f), new Vector2(8f, 5f));
	private readonly Rect2 _kitchen = new(new Vector2(8f, 0f), new Vector2(8f, 5f));
	private readonly Rect2 _bathroom = new(new Vector2(8f, 5f), new Vector2(8f, 3f));
	private readonly Rect2 _lobbyConnector = new(new Vector2(8f, -8f), new Vector2(8f, 3f));
	private readonly Rect2 _frontDesk = new(new Vector2(16f, -14f), new Vector2(10f, 5f));
	private readonly Rect2 _lobby = new(new Vector2(16f, -9f), new Vector2(10f, 9f));
	private readonly Rect2 _parkingLot = new(new Vector2(26f, -14f), new Vector2(12f, 22f));
	private readonly Rect2 _backyard = new(new Vector2(-15f, -14f), new Vector2(10f, 22f));
	private readonly Rect2 _toolshed = new(new Vector2(-13f, -13f), new Vector2(5f, 4f));

	private StandardMaterial3D _hallMaterial = null!;
	private StandardMaterial3D _supportMaterial = null!;
	private StandardMaterial3D _wallMaterial = null!;
	private StandardMaterial3D _equipmentMaterial = null!;
	private StandardMaterial3D _supplyMaterial = null!;
	private StandardMaterial3D _bathroomMaterial = null!;
	private StandardMaterial3D _officeMaterial = null!;
	private StandardMaterial3D _exteriorMaterial = null!;
	private readonly List<WallFadeTarget> _wallFadeTargets = new();
	private Player3D? _player;

	private sealed class WallFadeTarget
	{
		public required MeshInstance3D Mesh { get; init; }
		public required StandardMaterial3D Material { get; init; }
		public required Vector3 Position { get; init; }
		public required Vector3 Size { get; init; }
	}

	public override void _Ready()
	{
		CreateMaterials();
		BuildStationInterior();
		BuildExteriorHooks();
		BuildRouteMarkers();
	}

	public override void _Process(double delta)
	{
		UpdateWallFades(delta);
	}

	public void SetPlayer(Player3D player)
	{
		_player = player;
	}

	public string? GetRoomName(Vector3 playerPosition)
	{
		var point = new Vector2(playerPosition.X, playerPosition.Z);
		if (_parkingLot.HasPoint(point)) return "PARKING LOT";
		if (_toolshed.HasPoint(point)) return "TOOLSHED";
		if (_backyard.HasPoint(point)) return "BACKYARD";
		if (_frontDesk.HasPoint(point)) return "FRONT DESK";
		if (_lobby.HasPoint(point)) return "LOBBY / FRONT DESK";
		if (_equipmentRoom.HasPoint(point)) return "EQUIPMENT";
		if (_bathroom.HasPoint(point)) return "BATHROOM";
		if (_kitchen.HasPoint(point)) return "KITCHEN / BREAK";
		if (_office.HasPoint(point)) return "OFFICE";
		if (_archive.HasPoint(point)) return "DOCUMENT / ARCHIVE";
		if (_lobbyConnector.HasPoint(point)) return "HALLWAY";
		if (_hallway.HasPoint(point)) return "HALLWAY";
		return null;
	}

	private void CreateMaterials()
	{
		_hallMaterial = MakeMaterial(new Color(0.11f, 0.12f, 0.14f));
		_supportMaterial = MakeMaterial(new Color(0.13f, 0.12f, 0.11f));
		_wallMaterial = MakeMaterial(new Color(0.2f, 0.18f, 0.16f));
		_equipmentMaterial = MakeMaterial(new Color(0.06f, 0.18f, 0.22f));
		_supplyMaterial = MakeMaterial(new Color(0.22f, 0.15f, 0.08f));
		_bathroomMaterial = MakeMaterial(new Color(0.15f, 0.18f, 0.2f));
		_officeMaterial = MakeMaterial(new Color(0.14f, 0.12f, 0.18f));
		_exteriorMaterial = MakeMaterial(new Color(0.08f, 0.11f, 0.09f));
	}

	private static StandardMaterial3D MakeMaterial(Color color)
	{
		return new StandardMaterial3D { AlbedoColor = color };
	}

	private void BuildStationInterior()
	{
		AddRoom("Hallway", _hallway, _hallMaterial, "HALLWAY");
		AddRoom("Equipment", _equipmentRoom, _supportMaterial, "EQUIPMENT ROOM");
		AddRoom("Archive", _archive, _officeMaterial, "DOCUMENT / ARCHIVE");
		AddRoom("LobbyConnector", _lobbyConnector, _hallMaterial, "HALLWAY");
		AddRoom("Office", _office, _officeMaterial, "OFFICE");
		AddRoom("Kitchen", _kitchen, _supportMaterial, "KITCHEN / BREAK");
		AddRoom("Bathroom", _bathroom, _bathroomMaterial, "BATHROOM");
		AddRoom("FrontDesk", _frontDesk, _supportMaterial, "FRONT DESK");
		AddRoom("Lobby", _lobby, _supportMaterial, "LOBBY");

		AddInteriorDividers();
		AddInteriorProps();
	}

	private void AddInteriorDividers()
	{
		AddHorizontalWall("NorthWallWest", -5f, 5.5f, -14f);
		AddHorizontalWall("NorthWallEast", 7.5f, 26f, -14f);
		AddHorizontalWall("SouthWallWest", -5f, 5.5f, 8f);
		AddHorizontalWall("SouthWallEast", 7.5f, 16f, 8f);

		AddVerticalWall("WestWallEquipment", -5f, -14f, -8f);
		AddVerticalWall("WestWallStudio", -5f, -8f, 0f);
		AddVerticalWall("WestWallControl", -5f, 0f, 8f);
		AddVerticalWall("HallEquipmentWallNorth", 5f, -14f, -12f);
		AddVerticalWall("HallEquipmentWallSouth", 5f, -10f, -8f);
		AddVerticalWall("HallStudioWallNorth", 5f, -8f, -5f);
		AddVerticalWall("HallStudioWallSouth", 5f, -3f, 0f);
		AddVerticalWall("HallControlWallNorth", 5f, 0f, 4f);
		AddVerticalWall("HallControlWallSouth", 5f, 6f, 8f);

		AddHorizontalWall("EquipmentStudioDivider", -5f, 5f, -8f);
		AddHorizontalWall("StudioControlDoorJambWest", -5f, -4.35f, 0f);
		AddHorizontalWall("StudioControlDoorJambEast", -2.35f, -1.7f, 0f);
		AddHorizontalWall("StudioControlEastWall", 2.9f, 5f, 0f);
		AddControlStudioWindow();
		AddWallCornerPosts();

		AddVerticalWall("ArchiveHallWallNorth", 8f, -14f, -12f);
		AddVerticalWall("ArchiveHallWallSouth", 8f, -10f, -8f);
		AddVerticalWall("OfficeHallWallNorth", 8f, -5f, -3.5f);
		AddVerticalWall("OfficeHallWallSouth", 8f, -1.5f, 0f);
		AddVerticalWall("KitchenHallWallNorth", 8f, 0f, 1.5f);
		AddVerticalWall("KitchenHallWallSouth", 8f, 3.5f, 5f);
		AddVerticalWall("BathroomHallWallNorth", 8f, 5f, 5.5f);
		AddVerticalWall("BathroomHallWallSouth", 8f, 7.5f, 8f);

		AddHorizontalWall("ArchiveSouthWall", 8f, 16f, -8f);
		AddHorizontalWall("OfficeNorthWall", 8f, 16f, -5f);
		AddHorizontalWall("OfficeKitchenDivider", 8f, 16f, 0f);
		AddHorizontalWall("KitchenBathroomDividerWest", 8f, 12f, 5f);
		AddHorizontalWall("KitchenBathroomDividerEast", 14f, 16f, 5f);
		AddHorizontalWall("BathroomSouthWall", 8f, 16f, 8f);

		AddVerticalWall("ArchiveFrontDeskWallNorth", 16f, -14f, -12f);
		AddVerticalWall("ArchiveFrontDeskWallSouth", 16f, -10f, -8f);
		AddVerticalWall("OfficeEastWall", 16f, -5f, 0f);
		AddVerticalWall("KitchenEastWall", 16f, 0f, 5f);
		AddVerticalWall("BathroomEastWall", 16f, 5f, 8f);
		AddVerticalWall("FrontDeskEastWall", 26f, -14f, -9f);
		AddVerticalWall("LobbyParkingWallNorth", 26f, -9f, -5f);
		AddVerticalWall("LobbyParkingWallSouth", 26f, -3f, 0f);
		AddHorizontalWall("LobbySouthWall", 16f, 26f, 0f);
		AddBox("FrontDeskLongCounter", new Vector3(21f, 0.45f, -9f), new Vector3(9f, 0.9f, 0.55f), _supplyMaterial, true);
	}

	private void AddInteriorProps()
	{
		AddBox("EquipmentRackA", new Vector3(-2f, 0.45f, -12.4f), new Vector3(1.2f, 0.9f, 0.6f), _equipmentMaterial, true);
		AddBox("EquipmentRackB", new Vector3(2.5f, 0.45f, -10f), new Vector3(1.2f, 0.9f, 0.6f), _equipmentMaterial, true);
		AddBox("ArchiveShelves", new Vector3(10f, 0.65f, -11f), new Vector3(0.8f, 1.3f, 2.8f), _officeMaterial, true);
		AddBox("OfficeDesk", new Vector3(12f, 0.35f, -2.5f), new Vector3(2f, 0.7f, 0.9f), _officeMaterial, true);
		AddBox("CoffeeCounter", new Vector3(13.8f, 0.35f, 2.1f), new Vector3(2.8f, 0.7f, 0.65f), _supplyMaterial, true);
		AddBox("SupplyFridge", new Vector3(15.2f, 0.75f, 3.8f), new Vector3(0.8f, 1.5f, 0.7f), _supplyMaterial, true);
		AddBox("BathroomSink", new Vector3(12.8f, 0.35f, 5.8f), new Vector3(0.8f, 0.7f, 0.5f), _bathroomMaterial, true);
		AddBox("BathroomStall", new Vector3(12.6f, 0.5f, 7f), new Vector3(1.2f, 1f, 0.9f), _bathroomMaterial, true);
	}

	private void BuildExteriorHooks()
	{
		AddRoom("ParkingLot", _parkingLot, _exteriorMaterial, "PARKING LOT AREA");
		AddRoom("Backyard", _backyard, _exteriorMaterial, "BACKYARD AREA");
		AddRoom("Toolshed", _toolshed, _supportMaterial, "TOOLSHED");
		AddBox("Walkway", new Vector3(5.5f, -0.08f, 10f), new Vector3(21f, 0.1f, 3f), _hallMaterial, true);
		AddBox("LadderToRoof", new Vector3(-1f, 0.8f, 8.7f), new Vector3(1.6f, 0.25f, 0.4f), _equipmentMaterial, true);
		AddLabel("WALKWAY", new Vector3(5.5f, 1.1f, 10f));
		AddLabel("LADDER TO ROOF", new Vector3(-1f, 1.4f, 8.7f));
	}

	private void BuildRouteMarkers()
	{
		AddBox("ControlToHallThreshold", new Vector3(5f, 0.04f, 5.4f), new Vector3(0.8f, 0.08f, 1.8f), _equipmentMaterial, false);
		AddBox("ControlStudioDoorMarker", new Vector3(-3.35f, 0.04f, 0f), new Vector3(1.8f, 0.08f, 0.8f), _equipmentMaterial, false);
		AddBox("StudioToHallThreshold", new Vector3(5f, 0.04f, -4f), new Vector3(0.8f, 0.08f, 1.8f), _equipmentMaterial, false);
		AddBox("EquipmentDoorMarker", new Vector3(5f, 0.04f, -11f), new Vector3(0.8f, 0.08f, 1.8f), _equipmentMaterial, false);
		AddBox("NorthExteriorDoorMarker", new Vector3(6.5f, 0.04f, -14f), new Vector3(1.8f, 0.08f, 0.8f), _equipmentMaterial, false);
		AddBox("ArchiveDoorMarker", new Vector3(8f, 0.04f, -11f), new Vector3(0.8f, 0.08f, 1.6f), _officeMaterial, false);
		AddBox("OfficeDoorMarker", new Vector3(8f, 0.04f, -2.5f), new Vector3(0.8f, 0.08f, 1.6f), _officeMaterial, false);
		AddBox("KitchenDoorMarker", new Vector3(8f, 0.04f, 2.5f), new Vector3(0.8f, 0.08f, 1.8f), _supplyMaterial, false);
		AddBox("BathroomDoorMarker", new Vector3(8f, 0.04f, 6.5f), new Vector3(0.8f, 0.08f, 1.6f), _bathroomMaterial, false);
		AddBox("SouthExteriorDoorMarker", new Vector3(6.5f, 0.04f, 8f), new Vector3(1.8f, 0.08f, 0.8f), _equipmentMaterial, false);
		AddBox("ArchiveFrontDeskDoorMarker", new Vector3(16f, 0.04f, -11f), new Vector3(0.8f, 0.08f, 1.6f), _officeMaterial, false);
		AddBox("KitchenBathroomDoorMarker", new Vector3(13f, 0.04f, 5f), new Vector3(1.6f, 0.08f, 0.8f), _bathroomMaterial, false);
		AddBox("LobbyExitMarker", new Vector3(26f, 0.04f, -4f), new Vector3(0.8f, 0.08f, 1.6f), _equipmentMaterial, false);
	}

	private void AddRoom(string name, Rect2 rect, Material material, string label)
	{
		var center = GetCenter(rect);
		AddBox($"{name}Floor", new Vector3(center.X, -0.1f, center.Y), new Vector3(rect.Size.X, 0.2f, rect.Size.Y), material, true);
		AddLabel(label, new Vector3(center.X, 1.3f, center.Y));
	}

	private static Vector2 GetCenter(Rect2 rect)
	{
		return rect.Position + rect.Size / 2f;
	}

	private void AddWall(string name, Vector3 position, Vector3 size)
	{
		var material = MakeWallMaterial();
		var mesh = AddBox(name, position, size, material, true);
		_wallFadeTargets.Add(new WallFadeTarget { Mesh = mesh, Material = material, Position = position, Size = size });
	}

	private StandardMaterial3D MakeWallMaterial()
	{
		var material = (StandardMaterial3D)_wallMaterial.Duplicate();
		material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		material.AlbedoColor = new Color(material.AlbedoColor.R, material.AlbedoColor.G, material.AlbedoColor.B, WallOpaqueAlpha);
		return material;
	}

	private void AddWallCornerPosts()
	{
		AddWallCornerPost("CornerPostNorthWest", -5f, -14f);
		AddWallCornerPost("CornerPostNorthHallWest", 5f, -14f);
		AddWallCornerPost("CornerPostNorthHallEast", 8f, -14f);
		AddWallCornerPost("CornerPostArchiveFrontDesk", 16f, -14f);
		AddWallCornerPost("CornerPostFrontDeskParking", 26f, -14f);
		AddWallCornerPost("CornerPostEquipmentStudioWest", -5f, -8f);
		AddWallCornerPost("CornerPostStudioControlWest", -5f, 0f);
		AddWallCornerPost("CornerPostStudioControlHall", 5f, 0f);
		AddWallCornerPost("CornerPostSouthWest", -5f, 8f);
		AddWallCornerPost("CornerPostSouthHallWest", 5f, 8f);
		AddWallCornerPost("CornerPostSouthHallEast", 8f, 8f);
		AddWallCornerPost("CornerPostSupportEast", 16f, 8f);
		AddWallCornerPost("CornerPostLobbyEastSouth", 26f, 0f);
	}

	private void AddWallCornerPost(string name, float x, float z)
	{
		AddWall(name, new Vector3(x, WallCenterY, z), new Vector3(WallThickness, WallHeight, WallThickness));
	}

	private void AddControlStudioWindow()
	{
		AddBox("ControlStudioWindowHalfWall", new Vector3(0.6f, 0.3f, 0f), new Vector3(4.6f, 0.6f, WallThickness), _wallMaterial, true);
		AddBox("ControlStudioWindowLeftFrame", new Vector3(-1.7f, 1.25f, 0f), new Vector3(WallThickness, 1.9f, WallThickness), _wallMaterial, false);
		AddBox("ControlStudioWindowRightFrame", new Vector3(2.9f, 1.25f, 0f), new Vector3(WallThickness, 1.9f, WallThickness), _wallMaterial, false);
		AddBox("ControlStudioWindowTopFrame", new Vector3(0.6f, 2.15f, 0f), new Vector3(4.6f, WallThickness, WallThickness), _wallMaterial, false);
	}

	private void AddHorizontalWall(string name, float x1, float x2, float z)
	{
		var left = Mathf.Min(x1, x2);
		var right = Mathf.Max(x1, x2);
		left += WallThickness * 0.5f;
		right -= WallThickness * 0.5f;
		var width = right - left;
		if (width <= 0f)
		{
			return;
		}
		var centerX = left + width / 2f;
		AddWall(name, new Vector3(centerX, WallCenterY, z), new Vector3(width, WallHeight, WallThickness));
	}

	private void AddVerticalWall(string name, float x, float z1, float z2)
	{
		var near = Mathf.Min(z1, z2);
		var far = Mathf.Max(z1, z2);
		near += WallThickness * 0.5f;
		far -= WallThickness * 0.5f;
		var depth = far - near;
		if (depth <= 0f)
		{
			return;
		}
		var centerZ = near + depth / 2f;
		AddWall(name, new Vector3(x, WallCenterY, centerZ), new Vector3(WallThickness, WallHeight, depth));
	}

	private void AddLabel(string text, Vector3 position)
	{
		var label = new Label3D
		{
			Name = $"{text.Replace(" / ", "_").Replace(' ', '_')}Label",
			Text = text,
			Position = position,
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled
		};
		AddChild(label);
	}

	private void UpdateWallFades(double delta)
	{
		_player ??= GetParent()?.GetNodeOrNull<Player3D>("Player3D");
		if (_player == null)
		{
			return;
		}

		var player = _player.GlobalPosition;
		var weight = 1f - Mathf.Exp(-WallFadeSpeed * (float)delta);

		foreach (var target in _wallFadeTargets)
		{
			var targetAlpha = ShouldFadeWall(target, player) ? WallFadeAlpha : WallOpaqueAlpha;
			var color = target.Material.AlbedoColor;
			color.A = Mathf.Lerp(color.A, targetAlpha, weight);
			target.Material.AlbedoColor = color;
		}
	}

	private static bool ShouldFadeWall(WallFadeTarget wall, Vector3 player)
	{
		var minX = wall.Position.X - wall.Size.X * 0.5f - 0.8f;
		var maxX = wall.Position.X + wall.Size.X * 0.5f + 0.8f;
		var minZ = wall.Position.Z - wall.Size.Z * 0.5f;
		var maxZ = wall.Position.Z + wall.Size.Z * 0.5f;

		var horizontallyAligned = player.X >= minX && player.X <= maxX;
		var inFrontOfPlayer = minZ >= player.Z - 0.2f && minZ <= player.Z + 3.2f;
		var veryClose = player.X >= minX && player.X <= maxX && player.Z >= minZ - 0.7f && player.Z <= maxZ + 0.7f;

		return (horizontallyAligned && inFrontOfPlayer) || veryClose;
	}

	private MeshInstance3D AddBox(string name, Vector3 position, Vector3 size, Material material, bool collider)
	{
		var mesh = new MeshInstance3D
		{
			Name = name,
			Position = position,
			Mesh = new BoxMesh { Size = size },
			MaterialOverride = material
		};
		AddChild(mesh);

		if (!collider)
		{
			return mesh;
		}

		var body = new StaticBody3D { Name = $"{name}Collider", Position = position };
		body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
		AddChild(body);
		return mesh;
	}
}
