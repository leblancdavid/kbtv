using Godot;
using System;
using System.Collections.Generic;

namespace KBTV.World3D;

public partial class StationGreybox3D : Node3D
{
	private const string InteriorDoorScenePath = "res://assets/models3d/props/interior_door_leaf.glb";
	private const string ExteriorGlassDoorScenePath = "res://assets/models3d/props/exterior_glass_door_leaf.glb";
	private const float WallHeight = 2.3f;
	private const float WallThickness = 0.2f;
	private const float DoorGap = 2f;
	private const float WallCenterY = WallHeight / 2f;
	private const float WallOpaqueAlpha = 1f;
	private const float WallFadeAlpha = 0.32f;
	private const float WallFadeSpeed = 8f;
	private const float DoorHeight = 1.9f;
	private const float DoorThickness = 0.12f;
	private const float DoorOpenSpeed = 9f;
	private const float SingleDoorWidth = 0.95f;
	private const float DoubleDoorWidth = SingleDoorWidth * 2f;
	private const float DoorTriggerPadding = 0.9f;
	private const float DoubleDoorMiddleZone = 0.28f;
	private const float WindowFrameDepth = WallThickness;
	private const float WindowFrameZ = 0f;
	private const float HallTileWorldSize = 0.5f;
	private const float BaseboardHeight = 0.12f;
	private const float BaseboardDepth = 0.045f;
	private const float BaseboardCenterY = BaseboardHeight * 0.5f;
	private const float BaseboardWallOverlap = 0.004f;
	private const float BaseboardCornerOverlap = BaseboardDepth;
	private const float BaseboardCornerCapSize = WallThickness + BaseboardDepth * 2f;
	private const float BaseboardDoorFrameOverlap = 0.01f;
	private const float BaseboardMinSegmentLength = 0.04f;
	private const float DoorFrameTrimWidth = 0.12f;
	private const float ExteriorDoorFrameTrimWidth = 0.075f;
	private const float ExteriorDoorFrameOpeningInset = 0.055f;
	private const float DoorFrameDepth = WallThickness + 0.12f;
	private const float DoorThresholdHeight = 0.035f;
	private const float DoorThresholdDepth = DoorFrameDepth + 0.04f;
	private const float DoorThresholdSideOverlap = 0.04f;

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
	private readonly Rect2 _controlRoomFootprint = new(new Vector2(-5f, 0f), new Vector2(10f, 8f));
	private readonly Rect2 _studioRoomFootprint = new(new Vector2(-5f, -8f), new Vector2(10f, 8f));

	private StandardMaterial3D _hallMaterial = null!;
	private StandardMaterial3D _supportMaterial = null!;
	private StandardMaterial3D _wallMaterial = null!;
	private StandardMaterial3D _wallpaperMaterial = null!;
	private StandardMaterial3D _woodBaseboardMaterial = null!;
	private StandardMaterial3D _offWhiteBaseboardMaterial = null!;
	private StandardMaterial3D _woodDoorFrameMaterial = null!;
	private StandardMaterial3D _metalDoorFrameMaterial = null!;
	private StandardMaterial3D _equipmentMaterial = null!;
	private StandardMaterial3D _supplyMaterial = null!;
	private StandardMaterial3D _bathroomMaterial = null!;
	private StandardMaterial3D _officeMaterial = null!;
	private StandardMaterial3D _exteriorMaterial = null!;
	private StandardMaterial3D _doorMaterial = null!;
	private PackedScene? _interiorDoorScene;
	private PackedScene? _exteriorGlassDoorScene;
	private readonly List<WallFadeTarget> _wallFadeTargets = new();
	private readonly List<Doorway> _doorways = new();
	private readonly List<DoorBaseboardCutout> _doorBaseboardCutouts = new();
	private readonly HashSet<Vector2> _wallCornerPostPositions = new();
	private Player3D? _player;
	public event Action<string, string, bool>? DoorLightLinkChanged;

	private enum DoorOrientation
	{
		Horizontal,
		Vertical
	}

	private sealed class Doorway
	{
		public readonly List<DoorLeaf> Leaves = new();
		public required Vector3 Center { get; init; }
		public required DoorOrientation Orientation { get; init; }
		public string? RoomA { get; init; }
		public string? RoomB { get; init; }
		public bool IsDoubleDoor { get; init; }
		public bool WasOpen { get; set; }
		public int PlayerOverlapCount { get; set; }
	}

	private sealed class DoorLeaf
	{
		public required Node3D Hinge { get; init; }
		public required float ClosedRotationDegrees { get; init; }
		public required float OpenRotationDegrees { get; init; }
		public float SideSign { get; init; }
	}

	private sealed class DoorBaseboardCutout
	{
		public required Vector3 Center { get; init; }
		public required DoorOrientation Orientation { get; init; }
		public required float Span { get; init; }
		public required float Depth { get; init; }
	}

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
		BuildDoors();
	}

	public override void _Process(double delta)
	{
		UpdateWallFades(delta);
		UpdateDoors(delta);
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
		_hallMaterial = StationFloorMaterials3D.MakeHallLinoleum();
		_supportMaterial = MakeMaterial(new Color(0.13f, 0.12f, 0.11f));
		_wallMaterial = MakeMaterial(new Color(0.2f, 0.18f, 0.16f));
		_wallpaperMaterial = StationFloorMaterials3D.MakeWallpaper();
		_woodBaseboardMaterial = MakeMaterial(new Color(0.34f, 0.22f, 0.12f));
		_offWhiteBaseboardMaterial = MakeMaterial(new Color(0.78f, 0.74f, 0.65f));
		_woodDoorFrameMaterial = MakeMaterial(new Color(0.28f, 0.17f, 0.09f));
		_metalDoorFrameMaterial = MakeMaterial(new Color(0.42f, 0.44f, 0.43f));
		_equipmentMaterial = MakeMaterial(new Color(0.06f, 0.18f, 0.22f));
		_supplyMaterial = MakeMaterial(new Color(0.22f, 0.15f, 0.08f));
		_bathroomMaterial = MakeMaterial(new Color(0.15f, 0.18f, 0.2f));
		_officeMaterial = MakeMaterial(new Color(0.14f, 0.12f, 0.18f));
		_exteriorMaterial = MakeMaterial(new Color(0.08f, 0.11f, 0.09f));
		_doorMaterial = MakeDoorMaterial();
	}

	private void LoadDoorScenes()
	{
		_interiorDoorScene ??= GD.Load<PackedScene>(InteriorDoorScenePath);
		_exteriorGlassDoorScene ??= GD.Load<PackedScene>(ExteriorGlassDoorScenePath);
	}

	private static StandardMaterial3D MakeMaterial(Color color)
	{
		return new StandardMaterial3D { AlbedoColor = color };
	}

	private static StandardMaterial3D MakeDoorMaterial()
	{
		return new StandardMaterial3D
		{
			AlbedoColor = new Color(0.44f, 0.44f, 0.46f),
			EmissionEnabled = true,
			Emission = new Color(0.44f, 0.44f, 0.46f),
			EmissionEnergyMultiplier = 0.16f
		};
	}

	private static uint DoorLayerMask(string? roomA, string? roomB)
	{
		var mask = RoomLayerMask(roomA) | RoomLayerMask(roomB);
		return mask == 0u ? StationLighting3D.StationLayer : mask;
	}

	private static uint RoomLayerMask(string? room)
	{
		return room switch
		{
			"Control" => StationLighting3D.ControlLayer,
			"Studio" => StationLighting3D.StudioLayer,
			"Equipment" => StationLighting3D.EquipmentLayer,
			"Station" => StationLighting3D.HallLayer,
			"Exterior" => StationLighting3D.ExteriorLayer,
			_ => 0u
		};
	}

	private void BuildStationInterior()
	{
		RegisterDoorBaseboardCutouts();
		AddRoom("Hallway", _hallway, _hallMaterial, "HALLWAY", StationLighting3D.HallLayer);
		AddRoom("Equipment", _equipmentRoom, _supportMaterial, "EQUIPMENT ROOM", StationLighting3D.EquipmentLayer);
		AddRoom("Archive", _archive, _officeMaterial, "DOCUMENT / ARCHIVE", StationLighting3D.StationLayer);
		AddRoom("LobbyConnector", _lobbyConnector, _hallMaterial, "HALLWAY", StationLighting3D.HallLayer);
		AddRoom("Office", _office, _officeMaterial, "OFFICE", StationLighting3D.StationLayer);
		AddRoom("Kitchen", _kitchen, _supportMaterial, "KITCHEN / BREAK", StationLighting3D.StationLayer);
		AddRoom("Bathroom", _bathroom, _bathroomMaterial, "BATHROOM", StationLighting3D.StationLayer);
		AddRoom("FrontDesk", _frontDesk, _supportMaterial, "FRONT DESK", StationLighting3D.StationLayer);
		AddRoom("Lobby", _lobby, _supportMaterial, "LOBBY", StationLighting3D.StationLayer);

		AddInteriorDividers();
		AddInteriorProps();
	}

	private void RegisterDoorBaseboardCutouts()
	{
		_doorBaseboardCutouts.Clear();
		AddDoorBaseboardCutout(new Vector3(5f, 0f, 5.4f), SingleDoorWidth, DoorOrientation.Vertical, false);
		AddDoorBaseboardCutout(new Vector3(-4.15f, 0f, 0f), SingleDoorWidth, DoorOrientation.Horizontal, false);
		AddDoorBaseboardCutout(new Vector3(5f, 0f, -4f), SingleDoorWidth, DoorOrientation.Vertical, false);
		AddDoorBaseboardCutout(new Vector3(5f, 0f, -11f), SingleDoorWidth, DoorOrientation.Vertical, false);
		AddDoorBaseboardCutout(new Vector3(6.5f, 0f, -14f), DoubleDoorWidth, DoorOrientation.Horizontal, true);
		AddDoorBaseboardCutout(new Vector3(8f, 0f, -11f), SingleDoorWidth, DoorOrientation.Vertical, false);
		AddDoorBaseboardCutout(new Vector3(8f, 0f, -2.5f), SingleDoorWidth, DoorOrientation.Vertical, false);
		AddDoorBaseboardCutout(new Vector3(8f, 0f, 2.5f), SingleDoorWidth, DoorOrientation.Vertical, false);
		AddDoorBaseboardCutout(new Vector3(8f, 0f, 6.5f), SingleDoorWidth, DoorOrientation.Vertical, false);
		AddDoorBaseboardCutout(new Vector3(6.5f, 0f, 8f), DoubleDoorWidth, DoorOrientation.Horizontal, true);
		AddDoorBaseboardCutout(new Vector3(16f, 0f, -11f), SingleDoorWidth, DoorOrientation.Vertical, false);
		AddDoorBaseboardCutout(new Vector3(13f, 0f, 5f), SingleDoorWidth, DoorOrientation.Horizontal, false);
		AddDoorBaseboardCutout(new Vector3(26f, 0f, -4f), DoubleDoorWidth, DoorOrientation.Vertical, true);
	}

	private void AddDoorBaseboardCutout(Vector3 center, float openingWidth, DoorOrientation orientation, bool isExterior)
	{
		var trimWidth = isExterior ? ExteriorDoorFrameTrimWidth : DoorFrameTrimWidth;
		var frameOpeningWidth = isExterior
			? Mathf.Max(0.1f, openingWidth - ExteriorDoorFrameOpeningInset * 2f)
			: openingWidth;
		var frameSpan = frameOpeningWidth + trimWidth * 2f;
		_doorBaseboardCutouts.Add(new DoorBaseboardCutout
		{
			Center = center,
			Orientation = orientation,
			Span = Mathf.Max(0.1f, frameSpan - BaseboardDoorFrameOverlap * 2f),
			Depth = Mathf.Max(0.1f, Mathf.Max(DoorFrameDepth, DoorThresholdDepth) - BaseboardDoorFrameOverlap * 2f)
		});
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
		AddVerticalWall("HallEquipmentWallNorth", 5f, -14f, -11.65f);
		AddVerticalWall("HallEquipmentWallSouth", 5f, -10.35f, -8f);
		AddVerticalWall("HallStudioWallNorth", 5f, -8f, -4.65f);
		AddVerticalWall("HallStudioWallSouth", 5f, -3.35f, 0f);
		AddVerticalWall("HallControlWallNorth", 5f, 0f, 4.75f);
		AddVerticalWall("HallControlWallSouth", 5f, 6.05f, 8f);

		AddHorizontalWall("EquipmentStudioDivider", -5f, 5f, -8f);
		AddHorizontalWall("StudioControlDoorJambWest", -5f, -4.75f, 0f, addBaseboards: false);
		AddHorizontalWall("StudioControlDoorJambEast", -3.55f, -1.7f, 0f, registerRightPost: false, addBaseboards: false);
		AddHorizontalWall("StudioControlEastWall", 2.9f, 5f, 0f, registerLeftPost: false, addBaseboards: false);
		AddControlSideBaseboard("StudioControlDoorJambWest", -5f, -4.75f, 0f);
		AddControlSideBaseboard("StudioControlDoorJambEast", -3.55f, -1.7f, 0f);
		AddControlSideBaseboard("StudioControlEastWall", 2.9f, 5f, 0f);
		AddControlStudioWindow();

		AddVerticalWall("ArchiveHallWallNorth", 8f, -14f, -11.65f);
		AddVerticalWall("ArchiveHallWallSouth", 8f, -10.35f, -8f);
		AddVerticalWall("OfficeHallWallNorth", 8f, -5f, -3.1f);
		AddVerticalWall("OfficeHallWallSouth", 8f, -1.9f, 0f);
		AddVerticalWall("KitchenHallWallNorth", 8f, 0f, 1.9f);
		AddVerticalWall("KitchenHallWallSouth", 8f, 3.1f, 5f);
		AddVerticalWall("BathroomHallWallNorth", 8f, 5f, 5.9f);
		AddVerticalWall("BathroomHallWallSouth", 8f, 7.1f, 8f);

		AddHorizontalWall("ArchiveSouthWall", 8f, 16f, -8f);
		AddHorizontalWall("OfficeNorthWall", 8f, 16f, -5f);
		AddHorizontalWall("OfficeKitchenDivider", 8f, 16f, 0f);
		AddHorizontalWall("KitchenBathroomDividerWest", 8f, 12.4f, 5f);
		AddHorizontalWall("KitchenBathroomDividerEast", 13.6f, 16f, 5f);
		AddHorizontalWall("BathroomSouthWall", 8f, 16f, 8f);

		AddVerticalWall("ArchiveFrontDeskWallNorth", 16f, -14f, -11.65f);
		AddVerticalWall("ArchiveFrontDeskWallSouth", 16f, -10.35f, -8f);
		AddVerticalWall("OfficeEastWall", 16f, -5f, 0f);
		AddVerticalWall("KitchenEastWall", 16f, 0f, 5f);
		AddVerticalWall("BathroomEastWall", 16f, 5f, 8f);
		AddVerticalWall("FrontDeskEastWall", 26f, -14f, -9f);
		AddVerticalWall("LobbyParkingWallNorth", 26f, -9f, -5f);
		AddVerticalWall("LobbyParkingWallSouth", 26f, -3f, 0f);
		AddHorizontalWall("LobbySouthWall", 16f, 26f, 0f);
		AddWallCornerPosts();
		AddBox("FrontDeskLongCounter", new Vector3(21f, 0.45f, -9f), new Vector3(9f, 0.9f, 0.55f), _supplyMaterial, true, StationLighting3D.StationLayer);
	}

	private void AddInteriorProps()
	{
		AddBox("EquipmentRackA", new Vector3(-2f, 0.45f, -12.4f), new Vector3(1.2f, 0.9f, 0.6f), _equipmentMaterial, true, StationLighting3D.EquipmentLayer);
		AddBox("EquipmentRackB", new Vector3(2.5f, 0.45f, -10f), new Vector3(1.2f, 0.9f, 0.6f), _equipmentMaterial, true, StationLighting3D.EquipmentLayer);
		AddBox("ArchiveShelves", new Vector3(10f, 0.65f, -11f), new Vector3(0.8f, 1.3f, 2.8f), _officeMaterial, true, StationLighting3D.StationLayer);
		AddBox("OfficeDesk", new Vector3(12f, 0.35f, -2.5f), new Vector3(2f, 0.7f, 0.9f), _officeMaterial, true, StationLighting3D.StationLayer);
		AddBox("CoffeeCounter", new Vector3(13.8f, 0.35f, 2.1f), new Vector3(2.8f, 0.7f, 0.65f), _supplyMaterial, true, StationLighting3D.StationLayer);
		AddBox("SupplyFridge", new Vector3(15.2f, 0.75f, 3.8f), new Vector3(0.8f, 1.5f, 0.7f), _supplyMaterial, true, StationLighting3D.StationLayer);
		AddBox("BathroomSink", new Vector3(12.8f, 0.35f, 5.8f), new Vector3(0.8f, 0.7f, 0.5f), _bathroomMaterial, true, StationLighting3D.StationLayer);
		AddBox("BathroomStall", new Vector3(12.6f, 0.5f, 7f), new Vector3(1.2f, 1f, 0.9f), _bathroomMaterial, true, StationLighting3D.StationLayer);
	}

	private void BuildExteriorHooks()
	{
		AddRoom("ParkingLot", _parkingLot, _exteriorMaterial, "PARKING LOT AREA", StationLighting3D.ExteriorLayer);
		AddRoom("Backyard", _backyard, _exteriorMaterial, "BACKYARD AREA", StationLighting3D.ExteriorLayer);
		AddRoom("Toolshed", _toolshed, _supportMaterial, "TOOLSHED", StationLighting3D.ExteriorLayer);
		AddBox("Walkway", new Vector3(5.5f, -0.08f, 10f), new Vector3(21f, 0.1f, 3f), _hallMaterial, true, StationLighting3D.ExteriorLayer, false);
		AddBox("LadderToRoof", new Vector3(-1f, 0.8f, 8.7f), new Vector3(1.6f, 0.25f, 0.4f), _equipmentMaterial, true, StationLighting3D.ExteriorLayer);
		AddLabel("WALKWAY", new Vector3(5.5f, 1.1f, 10f));
		AddLabel("LADDER TO ROOF", new Vector3(-1f, 1.4f, 8.7f));
	}

	private void BuildDoors()
	{
		AddSingleDoor("ControlToHallDoor", new Vector3(5f, 0f, 5.4f), SingleDoorWidth, 1.8f, DoorOrientation.Vertical, "Control", "Station");
		AddSingleDoor("ControlStudioDoor", new Vector3(-4.15f, 0f, 0f), SingleDoorWidth, 1.2f, DoorOrientation.Horizontal, "Control", "Studio");
		AddSingleDoor("StudioToHallDoor", new Vector3(5f, 0f, -4f), SingleDoorWidth, 1.8f, DoorOrientation.Vertical, "Studio", "Station");
		AddSingleDoor("EquipmentDoor", new Vector3(5f, 0f, -11f), SingleDoorWidth, 1.8f, DoorOrientation.Vertical, "Equipment", "Station");
		AddDoubleDoor("NorthExteriorDoor", new Vector3(6.5f, 0f, -14f), DoubleDoorWidth, 1.8f, DoorOrientation.Horizontal, -1f, "Station", "Exterior");
		AddSingleDoor("ArchiveDoor", new Vector3(8f, 0f, -11f), SingleDoorWidth, 1.6f, DoorOrientation.Vertical);
		AddSingleDoor("OfficeDoor", new Vector3(8f, 0f, -2.5f), SingleDoorWidth, 1.6f, DoorOrientation.Vertical);
		AddSingleDoor("KitchenDoor", new Vector3(8f, 0f, 2.5f), SingleDoorWidth, 1.8f, DoorOrientation.Vertical);
		AddSingleDoor("BathroomDoor", new Vector3(8f, 0f, 6.5f), SingleDoorWidth, 1.6f, DoorOrientation.Vertical);
		AddDoubleDoor("SouthExteriorDoor", new Vector3(6.5f, 0f, 8f), DoubleDoorWidth, 1.8f, DoorOrientation.Horizontal, 1f, "Station", "Exterior");
		AddSingleDoor("ArchiveFrontDeskDoor", new Vector3(16f, 0f, -11f), SingleDoorWidth, 1.6f, DoorOrientation.Vertical);
		AddSingleDoor("KitchenBathroomDoor", new Vector3(13f, 0f, 5f), SingleDoorWidth, 1.6f, DoorOrientation.Horizontal);
		AddDoubleDoor("LobbyExitDoor", new Vector3(26f, 0f, -4f), DoubleDoorWidth, 1.9f, DoorOrientation.Vertical, 1f, "Station", "Exterior");
	}

	private void AddSingleDoor(string name, Vector3 center, float width, float triggerWidth, DoorOrientation orientation, string? roomA = null, string? roomB = null)
	{
		var doorway = AddDoorTrigger(name, center, triggerWidth, orientation, false, roomA, roomB);
		var layerMask = DoorLayerMask(roomA, roomB);
		AddDoorFrame($"{name}Frame", center, width, orientation, layerMask, false);
		var hinge = orientation == DoorOrientation.Horizontal
			? center + new Vector3(-width * 0.5f, 0f, 0f)
			: center + new Vector3(0f, 0f, -width * 0.5f);
		var closedRotation = orientation == DoorOrientation.Horizontal ? 0f : -90f;
		doorway.Leaves.Add(AddDoorLeaf($"{name}Leaf", hinge, width, closedRotation, closedRotation + 90f, true, 0f, layerMask, false));
	}

	private void AddDoubleDoor(string name, Vector3 center, float width, float triggerWidth, DoorOrientation orientation, float outsideDirection, string? roomA = null, string? roomB = null)
	{
		var doorway = AddDoorTrigger(name, center, triggerWidth, orientation, true, roomA, roomB);
		var leafWidth = width * 0.5f;
		var layerMask = DoorLayerMask(roomA, roomB);
		AddDoorFrame($"{name}Frame", center, width, orientation, layerMask, true);

		if (orientation == DoorOrientation.Horizontal)
		{
			doorway.Leaves.Add(AddDoorLeaf($"{name}LeftLeaf", center + new Vector3(-width * 0.5f, 0f, 0f), leafWidth, 0f, -90f * outsideDirection, true, -1f, layerMask, true));
			doorway.Leaves.Add(AddDoorLeaf($"{name}RightLeaf", center + new Vector3(width * 0.5f, 0f, 0f), leafWidth, 0f, 90f * outsideDirection, false, 1f, layerMask, true));
		}
		else
		{
			doorway.Leaves.Add(AddDoorLeaf($"{name}NearLeaf", center + new Vector3(0f, 0f, -width * 0.5f), leafWidth, -90f, -90f + 90f * outsideDirection, true, -1f, layerMask, true));
			doorway.Leaves.Add(AddDoorLeaf($"{name}FarLeaf", center + new Vector3(0f, 0f, width * 0.5f), leafWidth, -90f, -90f - 90f * outsideDirection, false, 1f, layerMask, true));
		}
	}

	private void AddDoorFrame(string name, Vector3 center, float openingWidth, DoorOrientation orientation, uint layerMask, bool isExterior)
	{
		var material = isExterior ? _metalDoorFrameMaterial : _woodDoorFrameMaterial;
		var trimWidth = isExterior ? ExteriorDoorFrameTrimWidth : DoorFrameTrimWidth;
		var frameOpeningWidth = isExterior
			? Mathf.Max(0.1f, openingWidth - ExteriorDoorFrameOpeningInset * 2f)
			: openingWidth;
		var thresholdWidth = frameOpeningWidth + DoorThresholdSideOverlap * 2f;
		if (orientation == DoorOrientation.Horizontal)
		{
			var jambSize = new Vector3(trimWidth, DoorHeight, DoorFrameDepth);
			var leftX = center.X - frameOpeningWidth * 0.5f - trimWidth * 0.5f;
			var rightX = center.X + frameOpeningWidth * 0.5f + trimWidth * 0.5f;
			AddBox($"{name}LeftJamb", new Vector3(leftX, DoorHeight * 0.5f, center.Z), jambSize, material, false, layerMask, false);
			AddBox($"{name}RightJamb", new Vector3(rightX, DoorHeight * 0.5f, center.Z), jambSize, material, false, layerMask, false);

			var headerSize = new Vector3(frameOpeningWidth + trimWidth * 2f, trimWidth, DoorFrameDepth);
			AddBox($"{name}Header", new Vector3(center.X, DoorHeight + trimWidth * 0.5f, center.Z), headerSize, material, false, layerMask, false);
			AddBox($"{name}Threshold", new Vector3(center.X, DoorThresholdHeight * 0.5f, center.Z), new Vector3(thresholdWidth, DoorThresholdHeight, DoorThresholdDepth), material, false, layerMask, false);
			return;
		}

		var verticalJambSize = new Vector3(DoorFrameDepth, DoorHeight, trimWidth);
		var nearZ = center.Z - frameOpeningWidth * 0.5f - trimWidth * 0.5f;
		var farZ = center.Z + frameOpeningWidth * 0.5f + trimWidth * 0.5f;
		AddBox($"{name}NearJamb", new Vector3(center.X, DoorHeight * 0.5f, nearZ), verticalJambSize, material, false, layerMask, false);
		AddBox($"{name}FarJamb", new Vector3(center.X, DoorHeight * 0.5f, farZ), verticalJambSize, material, false, layerMask, false);

		var verticalHeaderSize = new Vector3(DoorFrameDepth, trimWidth, frameOpeningWidth + trimWidth * 2f);
		AddBox($"{name}Header", new Vector3(center.X, DoorHeight + trimWidth * 0.5f, center.Z), verticalHeaderSize, material, false, layerMask, false);
		AddBox($"{name}Threshold", new Vector3(center.X, DoorThresholdHeight * 0.5f, center.Z), new Vector3(DoorThresholdDepth, DoorThresholdHeight, thresholdWidth), material, false, layerMask, false);
	}

	private Doorway AddDoorTrigger(string name, Vector3 center, float width, DoorOrientation orientation, bool isDoubleDoor, string? roomA = null, string? roomB = null)
	{
		var doorway = new Doorway { Center = center, Orientation = orientation, IsDoubleDoor = isDoubleDoor, RoomA = roomA, RoomB = roomB };
		var triggerSize = orientation == DoorOrientation.Horizontal
			? new Vector3(width + DoorTriggerPadding, DoorHeight, 2.0f)
			: new Vector3(2.0f, DoorHeight, width + DoorTriggerPadding);
		var trigger = new Area3D { Name = $"{name}Trigger", Position = new Vector3(center.X, DoorHeight * 0.5f, center.Z) };
		trigger.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = triggerSize } });
		trigger.BodyEntered += body =>
		{
			if (body is Player3D)
			{
				doorway.PlayerOverlapCount++;
			}
		};
		trigger.BodyExited += body =>
		{
			if (body is Player3D)
			{
				doorway.PlayerOverlapCount = Mathf.Max(0, doorway.PlayerOverlapCount - 1);
			}
		};

		AddChild(trigger);
		_doorways.Add(doorway);
		return doorway;
	}

	private DoorLeaf AddDoorLeaf(string name, Vector3 hingePosition, float width, float closedRotationDegrees, float openRotationDegrees, bool extendsPositive, float sideSign, uint layerMask, bool useGlassDoor)
	{
		LoadDoorScenes();
		var hinge = new Node3D
		{
			Name = name,
			Position = hingePosition,
			RotationDegrees = new Vector3(0f, closedRotationDegrees, 0f)
		};
		AddChild(hinge);

		var localCenterX = (extendsPositive ? 1f : -1f) * width * 0.5f;
		var scene = useGlassDoor ? _exteriorGlassDoorScene : _interiorDoorScene;
		if (scene != null)
		{
			var visual = scene.Instantiate<Node3D>();
			visual.Name = "Panel";
			visual.Position = new Vector3(localCenterX, 0f, 0f);
			if (useGlassDoor && !extendsPositive)
			{
				visual.Scale = new Vector3(-1f, 1f, 1f);
			}
			StationLighting3D.ApplyLayerToTree(visual, layerMask);
			hinge.AddChild(visual);
			return new DoorLeaf { Hinge = hinge, ClosedRotationDegrees = closedRotationDegrees, OpenRotationDegrees = openRotationDegrees, SideSign = sideSign };
		}

		var leafSize = new Vector3(width, DoorHeight, DoorThickness);
		var mesh = new MeshInstance3D
		{
			Name = "Panel",
			Position = new Vector3(localCenterX, DoorHeight * 0.5f, 0f),
			Mesh = new BoxMesh { Size = leafSize },
			MaterialOverride = _doorMaterial,
			Layers = layerMask,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		};
		hinge.AddChild(mesh);
		return new DoorLeaf { Hinge = hinge, ClosedRotationDegrees = closedRotationDegrees, OpenRotationDegrees = openRotationDegrees, SideSign = sideSign };
	}

	private void UpdateDoors(double delta)
	{
		_player ??= GetParent()?.GetNodeOrNull<Player3D>("Player3D");
		var weight = 1f - Mathf.Exp(-DoorOpenSpeed * (float)delta);
		foreach (var doorway in _doorways)
		{
			var isOpen = doorway.PlayerOverlapCount > 0;
			if (doorway.RoomA != null && doorway.RoomB != null && isOpen != doorway.WasOpen)
			{
				doorway.WasOpen = isOpen;
				DoorLightLinkChanged?.Invoke(doorway.RoomA, doorway.RoomB, isOpen);
			}

			foreach (var leaf in doorway.Leaves)
			{
				var target = ShouldOpenDoorLeaf(doorway, leaf, isOpen) ? leaf.OpenRotationDegrees : leaf.ClosedRotationDegrees;
				var rotation = leaf.Hinge.RotationDegrees;
				rotation.Y = Mathf.LerpAngle(Mathf.DegToRad(rotation.Y), Mathf.DegToRad(target), weight) * 180f / Mathf.Pi;
				leaf.Hinge.RotationDegrees = rotation;
			}
		}
	}

	private bool ShouldOpenDoorLeaf(Doorway doorway, DoorLeaf leaf, bool isOpen)
	{
		if (!isOpen || _player == null)
		{
			return false;
		}

		if (!doorway.IsDoubleDoor)
		{
			return true;
		}

		var playerPosition = _player.GlobalPosition;
		var offset = doorway.Orientation == DoorOrientation.Horizontal
			? playerPosition.X - doorway.Center.X
			: playerPosition.Z - doorway.Center.Z;

		return Mathf.Abs(offset) <= DoubleDoorMiddleZone || Mathf.Sign(offset) == Mathf.Sign(leaf.SideSign);
	}

	private void AddRoom(string name, Rect2 rect, Material material, string label, uint layerMask)
	{
		var center = GetCenter(rect);
		if (material == _hallMaterial)
		{
			AddFloorPlane($"{name}Floor", new Vector3(center.X, 0.01f, center.Y), rect.Size, material, layerMask);
		}
		else
		{
			AddBox($"{name}Floor", new Vector3(center.X, -0.1f, center.Y), new Vector3(rect.Size.X, 0.2f, rect.Size.Y), material, true, layerMask, false);
		}
		AddLabel(label, new Vector3(center.X, 1.3f, center.Y));
	}

	private void AddFloorPlane(string name, Vector3 position, Vector2 size, Material material, uint layerMask)
	{
		var mesh = new MeshInstance3D
		{
			Name = name,
			Position = position,
			Mesh = StationFloorMaterials3D.MakeTiledFloorMesh(size.X, size.Y, HallTileWorldSize),
			MaterialOverride = material,
			Layers = layerMask,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		};
		AddChild(mesh);

		var body = new StaticBody3D { Name = $"{name}Collider", Position = new Vector3(position.X, -0.1f, position.Z) };
		body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(size.X, 0.2f, size.Y) } });
		AddChild(body);
	}

	private void AddColliderOnly(string name, Vector3 position, Vector3 size)
	{
		var body = new StaticBody3D { Name = $"{name}Collider", Position = position };
		body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
		AddChild(body);
	}

	private static Vector2 GetCenter(Rect2 rect)
	{
		return rect.Position + rect.Size / 2f;
	}

	private void AddWall(string name, Vector3 position, Vector3 size, bool addBaseboards = true)
	{
		var material = MakeWallMaterial();
		var mesh = AddBox(name, position, size, material, true, StationLighting3D.AllInteriorLayers);
		_wallFadeTargets.Add(new WallFadeTarget { Mesh = mesh, Material = material, Position = position, Size = size });
		AddWallpaperSkins(name, position, size);
		if (addBaseboards)
		{
			AddBaseboards(name, position, size);
		}
	}

	private void AddBaseboards(string name, Vector3 position, Vector3 size)
	{
		if (size.X >= size.Z)
		{
			var boardSize = new Vector3(size.X + BaseboardCornerOverlap * 2f, BaseboardHeight, BaseboardDepth);
			var northPosition = position + new Vector3(0f, BaseboardCenterY - position.Y, -size.Z * 0.5f - BaseboardDepth * 0.5f + BaseboardWallOverlap);
			var southPosition = position + new Vector3(0f, BaseboardCenterY - position.Y, size.Z * 0.5f + BaseboardDepth * 0.5f - BaseboardWallOverlap);
			AddHorizontalBaseboardSegments($"{name}BaseboardNorth", northPosition, boardSize, position, size);
			AddHorizontalBaseboardSegments($"{name}BaseboardSouth", southPosition, boardSize, position, size);
			return;
		}

		var verticalBoardSize = new Vector3(BaseboardDepth, BaseboardHeight, size.Z + BaseboardCornerOverlap * 2f);
		var westPosition = position + new Vector3(-size.X * 0.5f - BaseboardDepth * 0.5f + BaseboardWallOverlap, BaseboardCenterY - position.Y, 0f);
		var eastPosition = position + new Vector3(size.X * 0.5f + BaseboardDepth * 0.5f - BaseboardWallOverlap, BaseboardCenterY - position.Y, 0f);
		AddVerticalBaseboardSegments($"{name}BaseboardWest", westPosition, verticalBoardSize, position, size);
		AddVerticalBaseboardSegments($"{name}BaseboardEast", eastPosition, verticalBoardSize, position, size);
	}

	private void AddControlSideBaseboard(string name, float x1, float x2, float z)
	{
		var left = Mathf.Min(x1, x2);
		var right = Mathf.Max(x1, x2);
		var width = right - left;
		if (width <= 0f)
		{
			return;
		}

		var wallPosition = new Vector3(left + width * 0.5f, WallCenterY, z);
		var wallSize = new Vector3(width, WallHeight, WallThickness);
		var boardSize = new Vector3(width + BaseboardCornerOverlap * 2f, BaseboardHeight, BaseboardDepth);
		var southPosition = wallPosition + new Vector3(0f, BaseboardCenterY - wallPosition.Y, wallSize.Z * 0.5f + BaseboardDepth * 0.5f - BaseboardWallOverlap);
		AddHorizontalBaseboardSegments($"{name}ControlBaseboardSouth", southPosition, boardSize, wallPosition, wallSize);
	}

	private void AddHorizontalBaseboardSegments(string name, Vector3 position, Vector3 size, Vector3 wallPosition, Vector3 wallSize)
	{
		var segments = new List<(float Start, float End)> { (position.X - size.X * 0.5f, position.X + size.X * 0.5f) };
		foreach (var cutout in _doorBaseboardCutouts)
		{
			if (cutout.Orientation != DoorOrientation.Horizontal || Mathf.Abs(wallPosition.Z - cutout.Center.Z) > cutout.Depth * 0.5f)
			{
				continue;
			}

			SubtractBaseboardCutout(segments, cutout.Center.X - cutout.Span * 0.5f, cutout.Center.X + cutout.Span * 0.5f);
		}

		AddBaseboardSegments(name, position, size, wallPosition, wallSize, segments, true);
	}

	private void AddVerticalBaseboardSegments(string name, Vector3 position, Vector3 size, Vector3 wallPosition, Vector3 wallSize)
	{
		var segments = new List<(float Start, float End)> { (position.Z - size.Z * 0.5f, position.Z + size.Z * 0.5f) };
		foreach (var cutout in _doorBaseboardCutouts)
		{
			if (cutout.Orientation != DoorOrientation.Vertical || Mathf.Abs(wallPosition.X - cutout.Center.X) > cutout.Depth * 0.5f)
			{
				continue;
			}

			SubtractBaseboardCutout(segments, cutout.Center.Z - cutout.Span * 0.5f, cutout.Center.Z + cutout.Span * 0.5f);
		}

		AddBaseboardSegments(name, position, size, wallPosition, wallSize, segments, false);
	}

	private static void SubtractBaseboardCutout(List<(float Start, float End)> segments, float cutStart, float cutEnd)
	{
		for (var index = segments.Count - 1; index >= 0; index--)
		{
			var segment = segments[index];
			if (cutEnd <= segment.Start || cutStart >= segment.End)
			{
				continue;
			}

			segments.RemoveAt(index);
			if (cutStart > segment.Start)
			{
				segments.Insert(index, (segment.Start, Mathf.Min(cutStart, segment.End)));
				index++;
			}
			if (cutEnd < segment.End)
			{
				segments.Insert(index, (Mathf.Max(cutEnd, segment.Start), segment.End));
			}
		}
	}

	private void AddBaseboardSegments(string name, Vector3 position, Vector3 size, Vector3 wallPosition, Vector3 wallSize, List<(float Start, float End)> segments, bool horizontal)
	{
		var segmentIndex = 0;
		foreach (var segment in segments)
		{
			var length = segment.End - segment.Start;
			if (length < BaseboardMinSegmentLength)
			{
				continue;
			}

			var segmentPosition = horizontal
				? new Vector3(segment.Start + length * 0.5f, position.Y, position.Z)
				: new Vector3(position.X, position.Y, segment.Start + length * 0.5f);
			var segmentSize = horizontal
				? new Vector3(length, size.Y, size.Z)
				: new Vector3(size.X, size.Y, length);
			AddBaseboard($"{name}{segmentIndex++}", segmentPosition, segmentSize, BaseboardMaterialForSide(segmentPosition), wallPosition, wallSize);
		}
	}

	private void AddBaseboard(string name, Vector3 position, Vector3 size, StandardMaterial3D materialSource, Vector3 wallPosition, Vector3 wallSize)
	{
		var material = (StandardMaterial3D)materialSource.Duplicate();
		material.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
		var mesh = AddBox(name, position, size, material, false, StationLighting3D.AllInteriorLayers, false);
		_wallFadeTargets.Add(new WallFadeTarget { Mesh = mesh, Material = material, Position = wallPosition, Size = wallSize });
	}

	private StandardMaterial3D BaseboardMaterialForSide(Vector3 position)
	{
		var sidePoint = new Vector2(position.X, position.Z);
		return _controlRoomFootprint.HasPoint(sidePoint) || _studioRoomFootprint.HasPoint(sidePoint)
			? _woodBaseboardMaterial
			: _offWhiteBaseboardMaterial;
	}

	private void AddWallpaperSkins(string name, Vector3 position, Vector3 size)
	{
		const float skinOffset = 0.006f;
		if (size.X >= size.Z)
		{
			AddWallpaperSkin($"{name}WallpaperNorth", position + new Vector3(0f, 0f, -size.Z * 0.5f - skinOffset), Vector3.Zero, size.X, size.Y, position, size);
			AddWallpaperSkin($"{name}WallpaperSouth", position + new Vector3(0f, 0f, size.Z * 0.5f + skinOffset), new Vector3(0f, 180f, 0f), size.X, size.Y, position, size);
			return;
		}

		AddWallpaperSkin($"{name}WallpaperWest", position + new Vector3(-size.X * 0.5f - skinOffset, 0f, 0f), new Vector3(0f, 90f, 0f), size.Z, size.Y, position, size);
		AddWallpaperSkin($"{name}WallpaperEast", position + new Vector3(size.X * 0.5f + skinOffset, 0f, 0f), new Vector3(0f, -90f, 0f), size.Z, size.Y, position, size);
	}

	private void AddWallpaperSkin(string name, Vector3 position, Vector3 rotationDegrees, float width, float height, Vector3 wallPosition, Vector3 wallSize)
	{
		var material = (StandardMaterial3D)_wallpaperMaterial.Duplicate();
		var mesh = new MeshInstance3D
		{
			Name = name,
			Position = position,
			RotationDegrees = rotationDegrees,
			Mesh = StationFloorMaterials3D.MakeTiledWallMesh(width, height, 1.0f),
			MaterialOverride = material,
			Layers = StationLighting3D.AllInteriorLayers,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		};
		AddChild(mesh);
		_wallFadeTargets.Add(new WallFadeTarget { Mesh = mesh, Material = material, Position = wallPosition, Size = wallSize });
	}

	private StandardMaterial3D MakeWallMaterial()
	{
		var material = (StandardMaterial3D)_wallMaterial.Duplicate();
		material.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
		material.AlbedoColor = new Color(material.AlbedoColor.R, material.AlbedoColor.G, material.AlbedoColor.B, WallOpaqueAlpha);
		return material;
	}

	private void AddWallCornerPosts()
	{
		var index = 0;
		foreach (var position in _wallCornerPostPositions)
		{
			AddWallCornerPost($"CornerPost{index++}", position.X, position.Y);
		}
	}

	private void AddWallCornerPost(string name, float x, float z)
	{
		AddWall(name, new Vector3(x, WallCenterY, z), new Vector3(WallThickness, WallHeight, WallThickness), !IsControlStudioDividerBaseboardPosition(x, z));
		AddBaseboardCornerCaps(name, x, z);
	}

	private void AddBaseboardCornerCaps(string name, float x, float z)
	{
		if (IsControlStudioDividerBaseboardPosition(x, z))
		{
			return;
		}

		var halfSize = BaseboardCornerCapSize * 0.5f;
		var quadrantSize = new Vector3(halfSize, BaseboardHeight, halfSize);
		AddBaseboardCornerCap($"{name}BaseboardCapNorthWest", new Vector3(x - halfSize * 0.5f, BaseboardCenterY, z - halfSize * 0.5f), quadrantSize);
		AddBaseboardCornerCap($"{name}BaseboardCapNorthEast", new Vector3(x + halfSize * 0.5f, BaseboardCenterY, z - halfSize * 0.5f), quadrantSize);
		AddBaseboardCornerCap($"{name}BaseboardCapSouthWest", new Vector3(x - halfSize * 0.5f, BaseboardCenterY, z + halfSize * 0.5f), quadrantSize);
		AddBaseboardCornerCap($"{name}BaseboardCapSouthEast", new Vector3(x + halfSize * 0.5f, BaseboardCenterY, z + halfSize * 0.5f), quadrantSize);
	}

	private static bool IsControlStudioDividerBaseboardPosition(float x, float z)
	{
		return Mathf.IsEqualApprox(z, 0f) && x >= -5f && x <= 5f;
	}

	private void AddBaseboardCornerCap(string name, Vector3 position, Vector3 size)
	{
		if (IsPointInsideDoorBaseboardCutout(position))
		{
			return;
		}

		var material = (StandardMaterial3D)BaseboardMaterialForSide(position).Duplicate();
		material.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
		AddBox(name, position, size, material, false, StationLighting3D.AllInteriorLayers, false);
	}

	private bool IsPointInsideDoorBaseboardCutout(Vector3 position)
	{
		foreach (var cutout in _doorBaseboardCutouts)
		{
			var withinSpan = cutout.Orientation == DoorOrientation.Horizontal
				? Mathf.Abs(position.X - cutout.Center.X) <= cutout.Span * 0.5f
				: Mathf.Abs(position.Z - cutout.Center.Z) <= cutout.Span * 0.5f;
			var withinDepth = cutout.Orientation == DoorOrientation.Horizontal
				? Mathf.Abs(position.Z - cutout.Center.Z) <= cutout.Depth * 0.5f
				: Mathf.Abs(position.X - cutout.Center.X) <= cutout.Depth * 0.5f;
			if (withinSpan && withinDepth)
			{
				return true;
			}
		}

		return false;
	}

	private void AddControlStudioWindow()
	{
		var positionZ = WindowFrameZ;
		var layerMask = StationLighting3D.ControlLayer | StationLighting3D.StudioLayer;
		var halfWallPosition = new Vector3(0.6f, 0.3f, positionZ);
		var halfWallSize = new Vector3(4.6f, 0.6f, WindowFrameDepth);
		AddColliderOnly("ControlStudioWindowHalfWall", halfWallPosition, halfWallSize);
		AddBox("ControlStudioWindowLeftFrame", new Vector3(-1.7f, 1.25f, positionZ), new Vector3(WallThickness, 1.9f, WindowFrameDepth), _wallMaterial, false, layerMask);
		AddBox("ControlStudioWindowRightFrame", new Vector3(2.9f, 1.25f, positionZ), new Vector3(WallThickness, 1.9f, WindowFrameDepth), _wallMaterial, false, layerMask);
		AddBox("ControlStudioWindowTopFrame", new Vector3(0.6f, 2.15f, positionZ), new Vector3(4.6f, WallThickness, WindowFrameDepth), _wallMaterial, false, layerMask);
	}

	private void AddHorizontalWall(string name, float x1, float x2, float z, bool registerLeftPost = true, bool registerRightPost = true, bool addBaseboards = true)
	{
		var left = Mathf.Min(x1, x2);
		var right = Mathf.Max(x1, x2);
		if (registerLeftPost)
		{
			RegisterWallCornerPost(left, z);
		}
		if (registerRightPost)
		{
			RegisterWallCornerPost(right, z);
		}
		left += WallThickness * 0.5f;
		right -= WallThickness * 0.5f;
		var width = right - left;
		if (width <= 0f)
		{
			return;
		}
		var centerX = left + width / 2f;
		AddWall(name, new Vector3(centerX, WallCenterY, z), new Vector3(width, WallHeight, WallThickness), addBaseboards);
	}

	private void AddVerticalWall(string name, float x, float z1, float z2)
	{
		var near = Mathf.Min(z1, z2);
		var far = Mathf.Max(z1, z2);
		RegisterWallCornerPost(x, near);
		RegisterWallCornerPost(x, far);
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

	private void RegisterWallCornerPost(float x, float z)
	{
		_wallCornerPostPositions.Add(new Vector2(x, z));
	}

	private void AddLabel(string text, Vector3 position)
	{
		var label = new Label3D
		{
			Name = $"{text.Replace(" / ", "_").Replace(' ', '_')}Label",
			Text = text,
			Position = position,
			Layers = StationLighting3D.AllInteriorLayers | StationLighting3D.ExteriorLayer,
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
			target.Material.Transparency = color.A < 0.99f
				? BaseMaterial3D.TransparencyEnum.Alpha
				: BaseMaterial3D.TransparencyEnum.Disabled;
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

	private MeshInstance3D AddBox(string name, Vector3 position, Vector3 size, Material material, bool collider, uint layerMask = StationLighting3D.AllInteriorLayers, bool castShadow = true)
	{
		var mesh = new MeshInstance3D
		{
			Name = name,
			Position = position,
			Mesh = new BoxMesh { Size = size },
			MaterialOverride = material,
			Layers = layerMask,
			CastShadow = castShadow ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off
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
