using System;
using Godot;
using KBTV.Core;
using KBTV.UI;

namespace KBTV.World3D;

public partial class World3D : Node3D
{
	[Export] public NodePath? PlayerPath { get; set; }
	[Export] public Vector3 CameraOffset { get; set; } = new(0f, 11.5f, 13f);
	[Export] public float CameraFollowSpeed { get; set; } = 8f;
	[Export] public Vector2 CameraXBounds { get; set; } = new(-8f, 34f);
	[Export] public Vector2 CameraZBounds { get; set; } = new(0f, 24f);
	private static readonly Vector3 ControlStudioDoorPosition = new(-4.15f, 0f, 0f);
	private static readonly Vector3 StudioHallDoorPosition = new(5f, 0f, -4f);
	private static readonly Vector3 ControlStudioLeakDirection = Vector3.Back;
	private static readonly Vector3 StudioHallLeakDirection = Vector3.Right;

	private const float TerminalZoomSpeed = 3.2f;
	private const float TerminalFramingWidth = 1.6f;

	private enum TerminalViewState
	{
		None,
		ZoomingIn,
		Open,
		ZoomingOut
	}

	private Camera3D _camera = null!;
	private Label? _status_label;
	private ControlRoom3D _control_room = null!;
	private StudioRoom3D _studio_room = null!;
	private StationGreybox3D? _station_greybox;
	private Player3D? _player;
	private bool _player_at_connection;
	private StationLighting3D? _station_lighting;
	private uint _player_light_layer = StationLighting3D.ControlLayer;
	private RoomStateManager? _roomStateManager;
	private ComputerTerminal3D? _computerTerminal;
	private TerminalViewState _terminalViewState = TerminalViewState.None;
	private float _zoomProgress;
	private Vector3 _normalCameraPos;
	private float _normalCameraSize;
	private Basis _normalCameraBasis;
	private Vector3 _terminalCameraPos;
	private float _terminalCameraSize;
	private Basis _terminalCameraBasis;
	private SubViewport? _terminalViewport;
	private CallerTab? _terminalTab;
	private StandardMaterial3D? _screenLiveMaterial;

	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("WorldCamera");
		_status_label = GetNodeOrNull<Label>("StatusLayer/StatusPanel/StatusLabel");
		_control_room = GetNode<ControlRoom3D>("ControlRoom3D");
		_studio_room = GetNode<StudioRoom3D>("StudioRoom3D");
		_station_greybox = GetNodeOrNull<StationGreybox3D>("StationGreybox3D");
		_station_lighting = new StationLighting3D();
		_station_lighting.Build();
		AddChild(_station_lighting);
		if (_station_greybox != null)
		{
			_station_greybox.DoorLightLinkChanged += OnDoorLightLinkChanged;
		}
		StationLighting3D.ApplyLayerToTree(_control_room, StationLighting3D.ControlLayer);
		StationLighting3D.ApplyLayerToTree(_studio_room, StationLighting3D.StudioLayer);
		_roomStateManager = GetNodeOrNull<RoomStateManager>("/root/RoomStateManager");
		_computerTerminal = _control_room.ComputerTerminal;

		if (PlayerPath != null && !PlayerPath.IsEmpty)
		{
			_player = GetNodeOrNull<Player3D>(PlayerPath);
		}

		if (_player == null)
		{
			_player = GetNodeOrNull<Player3D>("Player3D");
		}

		if (_player != null)
		{
			_player.SetRoomAnchor(_control_room.GlobalPosition + _control_room.PlayerStartPosition);
			StationLighting3D.ApplyLayerToTree(_player, StationLighting3D.ControlLayer);
			_station_greybox?.SetPlayer(_player);
			UpdateCamera(0.0, true);
		}

		_control_room.ShowRoom();
		_studio_room.ShowRoom();
		UpdateStatusLabel("CONTROL ROOM");
	}

	public void SetPlayer(Player3D player)
	{
		_player = player;
		_station_greybox?.SetPlayer(_player);
		_player.SetRoomAnchor(_control_room.GlobalPosition + _control_room.PlayerStartPosition);
		StationLighting3D.ApplyLayerToTree(_player, StationLighting3D.ControlLayer);
		UpdateCamera(0.0, true);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape
			&& _terminalViewState != TerminalViewState.None)
		{
			CloseTerminalView();
			GetViewport().SetInputAsHandled();
			return;
		}

		if (_terminalViewState == TerminalViewState.Open && @event is InputEventMouse mouse)
		{
			ForwardTerminalMouse(mouse);
			return;
		}

		if (!Input.IsActionJustPressed("interact"))
		{
			return;
		}

		if (_computerTerminal != null && _computerTerminal.IsPlayerInRange
			&& _terminalViewState == TerminalViewState.None)
		{
			OpenTerminalView();
			GetViewport().SetInputAsHandled();
			return;
		}

		if (_player_at_connection)
		{
			GD.Print("World3D: Player is at the control room / studio doorway");
		}
	}

	public override void _Process(double delta)
	{
		_UpdatePlayerRoomState();
		if (_player != null)
		{
			_station_lighting?.UpdateFluorescentShadowCaster(_player.GlobalPosition);
		}

		if (_terminalViewState == TerminalViewState.None)
		{
			UpdateCamera(delta);
		}
		else
		{
			UpdateTerminalCamera(delta);
		}

		UpdateComputerHint();
	}


	private void _UpdatePlayerRoomState()
	{
		if (_player == null)
		{
			return;
		}

		var playerPosition = _player.GlobalPosition;
		_player_at_connection = _control_room.IsPlayerAtDoor(playerPosition) || _studio_room.IsPlayerAtDoor(playerPosition);

		// Audio location: control room = full audio, studio = Vern only, everywhere else muffled.
		var location = RoomStateManager.PlayerLocation.Outside;
		if (_control_room.ContainsPlayer(playerPosition))
		{
			location = RoomStateManager.PlayerLocation.InControlRoom;
		}
		else if (_studio_room.ContainsPlayer(playerPosition))
		{
			location = RoomStateManager.PlayerLocation.InStudio;
		}
		else if (_control_room.IsPlayerAtDoor(playerPosition))
		{
			// Control room ↔ studio connection doorway: keep full audio while passing through.
			location = RoomStateManager.PlayerLocation.InControlRoom;
		}

		_roomStateManager?.SetPlayerLocation(location);

		string? nextRoomName = null;

		if (_player_at_connection)
		{
			SetPlayerLightLayer(StationLighting3D.AllInteriorLayers);
			nextRoomName = "DOORWAY";
		}
		else if (_studio_room.ContainsPlayer(playerPosition))
		{
			SetPlayerLightLayer(StationLighting3D.StudioLayer);
			nextRoomName = "STUDIO";
		}
		else if (_control_room.ContainsPlayer(playerPosition))
		{
			SetPlayerLightLayer(StationLighting3D.ControlLayer);
			nextRoomName = "CONTROL ROOM";
		}
		else
		{
			var stationRoom = _station_greybox?.GetRoomName(playerPosition);
			if (stationRoom != null)
			{
				SetPlayerLightLayer(stationRoom == "EQUIPMENT" ? StationLighting3D.EquipmentLayer : StationLighting3D.StationLayer);
				nextRoomName = stationRoom;
			}
		}

		if (nextRoomName == null)
		{
			return;
		}

		UpdateStatusLabel(nextRoomName);
	}

	private void SetPlayerLightLayer(uint layerMask)
	{
		if (_player == null || _player_light_layer == layerMask)
		{
			return;
		}

		_player_light_layer = layerMask;
		StationLighting3D.ApplyLayerToTree(_player, layerMask);
	}

	private void OnDoorLightLinkChanged(string roomA, string roomB, bool isOpen)
	{
		_station_lighting?.SetDoorLightLink(roomA, roomB, isOpen);
		SetStudioDoorSmokeLeak(roomA, roomB, isOpen);
	}

	private void SetStudioDoorSmokeLeak(string roomA, string roomB, bool isOpen)
	{
		if (!IsDoorLink(roomA, roomB, "Control", "Studio") && !IsDoorLink(roomA, roomB, "Studio", "Station"))
		{
			return;
		}

		if (IsDoorLink(roomA, roomB, "Control", "Studio"))
		{
			_studio_room.SetDoorSmokeLeakActive("ControlStudio", ControlStudioDoorPosition, ControlStudioLeakDirection, isOpen);
		}
		else
		{
			_studio_room.SetDoorSmokeLeakActive("StudioHall", StudioHallDoorPosition, StudioHallLeakDirection, isOpen);
		}
	}

	private static bool IsDoorLink(string roomA, string roomB, string expectedA, string expectedB)
	{
		return (roomA == expectedA && roomB == expectedB) || (roomA == expectedB && roomB == expectedA);
	}

	private void UpdateStatusLabel(string roomName)
	{
		if (_status_label != null)
		{
			_status_label.Text = $"KBTV 3D BLOCKOUT | {roomName} | connected floorplan";
		}
	}

	private void UpdateCamera(double delta, bool immediate = false)
	{
		if (_player == null)
		{
			return;
		}

		var target = _player.GlobalPosition + CameraOffset;
		target.X = Mathf.Clamp(target.X, CameraXBounds.X, CameraXBounds.Y);
		target.Z = Mathf.Clamp(target.Z, CameraZBounds.X, CameraZBounds.Y);

		if (immediate)
		{
			_camera.GlobalPosition = target;
		}
		else
		{
			var weight = 1f - Mathf.Exp(-CameraFollowSpeed * (float)delta);
			_camera.GlobalPosition = _camera.GlobalPosition.Lerp(target, weight);
		}

		_camera.RotationDegrees = new Vector3(-35f, 0f, 0f);
	}

	private void UpdateComputerHint()
	{
		if (_status_label == null || _terminalViewState != TerminalViewState.None)
		{
			return;
		}

		if (_computerTerminal != null && _computerTerminal.IsPlayerInRange)
		{
			if (_status_label.Text.EndsWith("| PRESS F TO USE COMPUTER"))
			{
				return;
			}
			_status_label.Text += " | PRESS F TO USE COMPUTER";
		}
	}

	private void OpenTerminalView()
	{
		if (_terminalViewState != TerminalViewState.None || _computerTerminal == null)
		{
			return;
		}

		_player?.SetMovementLocked(true);

		_normalCameraPos = _camera.GlobalPosition;
		_normalCameraSize = _camera.Size;
		_normalCameraBasis = _camera.GlobalTransform.Basis;

		var screenPos = _computerTerminal.ScreenCenter;
		var viewportSize = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1280f, 720f);
		var aspect = viewportSize.X / Mathf.Max(1f, viewportSize.Y);
		_terminalCameraSize = TerminalFramingWidth / (2f * aspect);
		_terminalCameraPos = screenPos + new Vector3(0f, -0.02f, 1.5f);

		var lookTarget = screenPos + new Vector3(0f, -0.12f, 0f);
		var transform = new Transform3D(Basis.Identity, _terminalCameraPos).LookingAt(lookTarget, Vector3.Up);
		_terminalCameraBasis = transform.Basis;

		_zoomProgress = 0f;
		_terminalViewState = TerminalViewState.ZoomingIn;
	}

	private void CloseTerminalView()
	{
		if (_terminalViewState is TerminalViewState.None or TerminalViewState.ZoomingOut)
		{
			return;
		}

		_terminalViewState = TerminalViewState.ZoomingOut;
		DetachScreenTexture();
		_computerTerminal.Visible = false;
		_control_room.ComputerGlb.Visible = true;
		_control_room.SetComputerCollidersEnabled(true);
		_player?.SetMovementLocked(false);
	}

	private void UpdateTerminalCamera(double delta)
	{
		if (_terminalViewState == TerminalViewState.ZoomingIn)
		{
			_zoomProgress = Mathf.MoveToward(_zoomProgress, 1f, TerminalZoomSpeed * (float)delta);
		}
		else if (_terminalViewState == TerminalViewState.ZoomingOut)
		{
			_zoomProgress = Mathf.MoveToward(_zoomProgress, 0f, TerminalZoomSpeed * (float)delta);
		}
		else
		{
			return;
		}

		var smooth = _zoomProgress * _zoomProgress * (3f - 2f * _zoomProgress);
		_camera.GlobalPosition = _normalCameraPos.Lerp(_terminalCameraPos, smooth);
		_camera.Size = Mathf.Lerp(_normalCameraSize, _terminalCameraSize, smooth);
		_camera.GlobalTransform = new Transform3D(
			_normalCameraBasis.Slerp(_terminalCameraBasis, smooth),
			_camera.GlobalPosition);

		if (_terminalViewState == TerminalViewState.ZoomingIn && _zoomProgress >= 1f)
		{
			_terminalViewState = TerminalViewState.Open;
			_control_room.SetComputerCollidersEnabled(false);
			_control_room.ComputerGlb.Visible = false;
			_computerTerminal.Visible = true;
			if (_player != null)
			{
				_player.Visible = false;
			}
			AttachScreenTexture();
		}
		else if (_terminalViewState == TerminalViewState.ZoomingOut && _zoomProgress <= 0f)
		{
			_terminalViewState = TerminalViewState.None;
			_player?.SetMovementLocked(false);
			if (_player != null)
			{
				_player.Visible = true;
			}
			UpdateCamera(0.0, true);
		}
	}

	private void ForwardTerminalMouse(InputEventMouse mouse)
	{
		if (_terminalViewport == null || _computerTerminal == null)
		{
			return;
		}

		var camera = GetViewport().GetCamera3D();
		if (camera == null)
		{
			return;
		}

		var from = camera.ProjectRayOrigin(mouse.Position);
		var to = from + camera.ProjectRayNormal(mouse.Position) * 60f;

		var query = PhysicsRayQueryParameters3D.Create(from, to);
		query.CollideWithBodies = true;
		query.CollideWithAreas = false;
		var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
		if (hit.Count == 0)
		{
			return;
		}

		if (hit["collider"].AsGodotObject() is not StaticBody3D colliderBody
			|| colliderBody != _computerTerminal.ScreenBody)
		{
			return;
		}

		var point = hit["position"].As<Vector3>();
		var local = _computerTerminal.ScreenBody.GlobalTransform.AffineInverse() * point;
		var u = Mathf.Clamp(local.X / ComputerTerminal3D.ScreenWidth + 0.5f, 0f, 1f);
		var v = Mathf.Clamp(0.5f - local.Y / ComputerTerminal3D.ScreenHeight, 0f, 1f);
		var viewportPosition = new Vector2(_terminalViewport.Size.X * u, _terminalViewport.Size.Y * v);

		_terminalViewport.PushInput(new InputEventMouseMotion
		{
			Position = viewportPosition,
			GlobalPosition = viewportPosition
		});

		if (mouse is InputEventMouseButton button)
		{
			_terminalViewport.PushInput(new InputEventMouseButton
			{
				ButtonIndex = button.ButtonIndex,
				Pressed = button.Pressed,
				Position = viewportPosition,
				GlobalPosition = viewportPosition
			});
		}

		GetViewport().SetInputAsHandled();
	}

	private void OnTerminalViewRequested()
	{
		CloseTerminalView();
	}

	private void EnsureTerminalViewport()
	{
		if (_terminalViewport != null)
		{
			return;
		}

		_terminalViewport = new SubViewport
		{
			Name = "ComputerScreenViewport",
			TransparentBg = false,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
			Disable3D = true,
			World2D = new World2D(),
			OwnWorld3D = false,
			Size = new Vector2I(960, 640)
		};
		AddChild(_terminalViewport);

		var backdrop = new ColorRect
		{
			Name = "ComputerScreenBackdrop",
			Color = new Color(0.05f, 0.05f, 0.05f, 1f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_terminalViewport.AddChild(backdrop);

		var callerScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/CallerTab.tscn");
		if (callerScene != null)
		{
			_terminalTab = callerScene.Instantiate<CallerTab>();
			_terminalTab.Name = "ComputerCallerTab";
			_terminalTab.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			_terminalViewport.AddChild(_terminalTab);
			_terminalTab.CloseRequested += OnTerminalViewRequested;
			_terminalTab.BackRequested += OnTerminalViewRequested;
		}

		_screenLiveMaterial = new StandardMaterial3D
		{
			EmissionEnabled = true,
			Emission = new Color(1f, 1f, 1f, 1f),
			EmissionEnergyMultiplier = 1.4f,
			AlbedoColor = new Color(0.05f, 0.05f, 0.06f, 1f)
		};
	}

	private void AttachScreenTexture()
	{
		if (_computerTerminal == null)
		{
			return;
		}

		EnsureTerminalViewport();
		if (_terminalViewport == null || _screenLiveMaterial == null)
		{
			return;
		}

		var texture = _terminalViewport.GetTexture();
		_screenLiveMaterial.AlbedoTexture = texture;
		_screenLiveMaterial.EmissionTexture = texture;
		_computerTerminal.ScreenMesh.MaterialOverride = _screenLiveMaterial;
	}

	private void DetachScreenTexture()
	{
		if (_computerTerminal == null)
		{
			return;
		}

		_computerTerminal.ScreenMesh.MaterialOverride = null;
	}
}
