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
	private const float TerminalFramingWidth = 1.85f;

	private enum TerminalViewState
	{
		None,
		ZoomingIn,
		Open,
		ZoomingOut
	}

	private Camera3D _camera = null!;
	private CanvasLayer? _statusLayer;
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
	private TerminalOverlay? _terminalOverlay;
	private StandardMaterial3D? _screenLiveMaterial;
	private TextureRect? _screenDebugPreview;
	private int _debugSampleTicks = -1;

	private double _lastTerminalLogTime = -1.0;
	private int _terminalPushCount;
	private int _terminalMissCount;
	private bool _terminalLeftPressed;
	private bool _terminalRightPressed;
	private bool _terminalMiddlePressed;

	private void LogTerminalMouse(string line)
	{
		var now = Time.GetTicksMsec() / 1000.0;
		if (now - _lastTerminalLogTime < 0.25)
		{
			return;
		}
		_lastTerminalLogTime = now;
		GD.Print($"[TerminalMouse] {line}");
	}

	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("WorldCamera");
		_statusLayer = GetNodeOrNull<CanvasLayer>("StatusLayer");
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
		_terminalOverlay = new TerminalOverlay { Name = "TerminalOverlay" };
		_terminalOverlay.CloseRequested += OnTerminalViewRequested;
		AddChild(_terminalOverlay);
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

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape
			&& _terminalViewState != TerminalViewState.None)
		{
			CloseTerminalView();
			GetViewport().SetInputAsHandled();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_terminalViewState != TerminalViewState.None
			&& Input.IsActionJustPressed("interact"))
		{
			CloseTerminalView();
			GetViewport().SetInputAsHandled();
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
		UpdateDebugSample();
		UpdateTerminalOverlayBounds();
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
		_terminalCameraPos = screenPos + new Vector3(0.09f, 0.12f, 1.42f);

		var lookTarget = screenPos + new Vector3(-0.02f, -0.08f, 0f);
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
		if (_statusLayer != null)
		{
			_statusLayer.Visible = true;
		}
		_terminalOverlay?.HideTerminal();
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
			_control_room.ComputerGlb.Visible = true;
			_computerTerminal.Visible = true;

			var screenBody = _computerTerminal.ScreenBody.GlobalTransform.Origin;
			var camPos = _camera.GlobalPosition;
			var visRect = GetViewport().GetVisibleRect().Size;
			var vpDesc = _terminalViewport == null ? "null" : $"{_terminalViewport.Size} guiDisable={_terminalViewport.GuiDisableInput} kids={_terminalViewport.GetChildCount()}";
			GD.Print($"[Terminal] OPEN win={DisplayServer.WindowGetSize()} visibleRect={visRect} " +
				$"stretch={ProjectSettings.GetSetting("display/window/stretch/mode")} " +
				$"camProjection={_camera.Projection} camSize={_camera.Size:N3} camPos=({camPos.X:N3},{camPos.Y:N3},{camPos.Z:N3}) " +
				$"screen=({screenBody.X:N3},{screenBody.Y:N3},{screenBody.Z:N3}) screenSize={ComputerTerminal3D.ScreenWidth}x{ComputerTerminal3D.ScreenHeight} " +
				$"subViewport={vpDesc}");
			_terminalPushCount = 0;
			_terminalMissCount = 0;
			if (_player != null)
			{
				_player.Visible = false;
			}
			ShowTerminalOverlay();
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
		if (!TryGetTerminalViewportPosition(mouse.Position, true, out var viewportPosition))
		{
			return;
		}

		var buttonMask = mouse is InputEventMouseMotion motion ? motion.ButtonMask : (MouseButtonMask)0;

		_terminalPushCount++;
		LogTerminalMouse($"PUSH #{_terminalPushCount} type={mouse.GetType().Name} mouse={mouse.Position} vpPos={viewportPosition} mask={buttonMask}");

		_terminalViewport.PushInput(new InputEventMouseMotion
		{
			Position = viewportPosition,
			GlobalPosition = viewportPosition,
			ButtonMask = buttonMask
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

	private void ShowTerminalOverlay()
	{
		if (_statusLayer != null)
		{
			_statusLayer.Visible = false;
		}
		UpdateTerminalOverlayBounds();
		_terminalOverlay?.ShowTerminal();
	}

	private void ApplyTerminalScreenGlow()
	{
		if (_computerTerminal == null)
		{
			return;
		}

		_screenLiveMaterial ??= new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			EmissionEnabled = true,
			Emission = new Color(0.10f, 0.48f, 0.38f, 1f),
			EmissionEnergyMultiplier = 0.35f,
			AlbedoColor = new Color(0.015f, 0.03f, 0.025f, 1f)
		};

		_computerTerminal.ScreenMesh.MaterialOverride = _screenLiveMaterial;
		_addedScreenMaterial = true;
	}

	private void UpdateTerminalOverlayBounds()
	{
		if (_terminalViewState != TerminalViewState.Open || _terminalOverlay == null
			|| _computerTerminal == null)
		{
			return;
		}

		var points = ProjectTerminalScreenCorners();
		var viewportSize = GetViewport().GetVisibleRect().Size;
		_terminalOverlay.SetScreenBounds(points, viewportSize);
	}

	private Vector2[] ProjectTerminalScreenCorners()
	{
		var transform = _computerTerminal!.ScreenBody.GlobalTransform;
		var halfWidth = ComputerTerminal3D.ScreenWidth * 0.5f;
		var halfHeight = ComputerTerminal3D.ScreenHeight * 0.5f;
		var corners = new[]
		{
			transform * new Vector3(-halfWidth, halfHeight, 0f),
			transform * new Vector3(halfWidth, halfHeight, 0f),
			transform * new Vector3(-halfWidth, -halfHeight, 0f),
			transform * new Vector3(halfWidth, -halfHeight, 0f)
		};

		var points = new Vector2[corners.Length];
		for (var i = 0; i < corners.Length; i++)
		{
			points[i] = _camera.UnprojectPosition(corners[i]);
		}
		return points;
	}

	private void PollTerminalMouse()
	{
		if (_terminalViewState != TerminalViewState.Open || _terminalViewport == null)
		{
			_terminalLeftPressed = false;
			_terminalRightPressed = false;
			_terminalMiddlePressed = false;
			return;
		}

		var mousePosition = GetViewport().GetMousePosition();
		if (!TryGetTerminalViewportPosition(mousePosition, false, out var viewportPosition))
		{
			UpdateTerminalDebugStatus("mouse off screen");
			return;
		}

		var buttonMask = GetPolledMouseButtonMask();
		_terminalViewport.PushInput(new InputEventMouseMotion
		{
			Position = viewportPosition,
			GlobalPosition = viewportPosition,
			ButtonMask = buttonMask
		});

		PushPolledButton(MouseButton.Left, ref _terminalLeftPressed, viewportPosition);
		PushPolledButton(MouseButton.Right, ref _terminalRightPressed, viewportPosition);
		PushPolledButton(MouseButton.Middle, ref _terminalMiddlePressed, viewportPosition);
		UpdateTerminalDebugStatus($"mouse {viewportPosition.X:N0},{viewportPosition.Y:N0} mask={buttonMask}");
	}

	private bool TryGetTerminalViewportPosition(Vector2 mousePosition, bool logFailures, out Vector2 viewportPosition)
	{
		viewportPosition = Vector2.Zero;
		if (_terminalViewport == null || _computerTerminal == null)
		{
			if (logFailures)
			{
				LogTerminalMouse($"EARLY: viewport null (vp={_terminalViewport != null}, term={_computerTerminal != null})");
			}
			return false;
		}

		var camera = GetViewport().GetCamera3D();
		if (camera == null)
		{
			if (logFailures)
			{
				LogTerminalMouse("EARLY: camera null");
			}
			return false;
		}

		var from = camera.ProjectRayOrigin(mousePosition);
		var to = from + camera.ProjectRayNormal(mousePosition) * 60f;

		var query = PhysicsRayQueryParameters3D.Create(from, to);
		query.CollideWithBodies = true;
		query.CollideWithAreas = false;
		var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
		if (hit.Count == 0)
		{
			_terminalMissCount++;
			if (logFailures)
			{
				var p = camera.ProjectRayNormal(mousePosition);
				LogTerminalMouse($"RAY MISS ({_terminalMissCount}) mouse={mousePosition} origin={from} dir=({p.X:N3},{p.Y:N3},{p.Z:N3})");
			}
			return false;
		}

		if (hit["collider"].AsGodotObject() is not StaticBody3D colliderBody
			|| colliderBody != _computerTerminal.ScreenBody)
		{
			_terminalMissCount++;
			if (logFailures)
			{
				var queued = hit["collider"].AsGodotObject();
				LogTerminalMouse($"WRONG COLLIDER ({_terminalMissCount}) hit='{queued}' want='{_computerTerminal.ScreenBody}' at {hit["position"].As<Vector3>()}");
			}
			return false;
		}

		var point = hit["position"].As<Vector3>();
		var local = _computerTerminal.ScreenBody.GlobalTransform.AffineInverse() * point;
		var u = Mathf.Clamp(local.X / ComputerTerminal3D.ScreenWidth + 0.5f, 0f, 1f);
		var v = Mathf.Clamp(0.5f - local.Y / ComputerTerminal3D.ScreenHeight, 0f, 1f);
		viewportPosition = new Vector2(_terminalViewport.Size.X * u, _terminalViewport.Size.Y * v);
		return true;
	}

	private static MouseButtonMask GetPolledMouseButtonMask()
	{
		var mask = (MouseButtonMask)0;
		if (Input.IsMouseButtonPressed(MouseButton.Left)) mask |= MouseButtonMask.Left;
		if (Input.IsMouseButtonPressed(MouseButton.Right)) mask |= MouseButtonMask.Right;
		if (Input.IsMouseButtonPressed(MouseButton.Middle)) mask |= MouseButtonMask.Middle;
		return mask;
	}

	private void PushPolledButton(MouseButton button, ref bool wasPressed, Vector2 viewportPosition)
	{
		var isPressed = Input.IsMouseButtonPressed(button);
		if (isPressed == wasPressed)
		{
			return;
		}

		wasPressed = isPressed;
		_terminalViewport?.PushInput(new InputEventMouseButton
		{
			ButtonIndex = button,
			Pressed = isPressed,
			Position = viewportPosition,
			GlobalPosition = viewportPosition
		});
	}

	private void UpdateTerminalDebugStatus(string detail)
	{
		if (_status_label != null)
		{
			_status_label.Text = $"KBTV 3D BLOCKOUT | TERMINAL | {detail}";
		}
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
			Size = new Vector2I(1152, 640)
		};
		AddChild(_terminalViewport);

		var screenLayer = new CanvasLayer
		{
			Name = "ComputerScreenCanvas",
			Layer = 1
		};
		_terminalViewport.AddChild(screenLayer);

		var screenRoot = new Control
		{
			Name = "ComputerScreenRoot"
		};
		screenRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		screenLayer.AddChild(screenRoot);

		var backdrop = new ColorRect
		{
			Name = "ComputerScreenBackdrop",
			Color = new Color(0.05f, 0.05f, 0.05f, 1f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		screenRoot.AddChild(backdrop);

		var callerScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/CallerTab.tscn");
		if (callerScene != null)
		{
			_terminalTab = callerScene.Instantiate<CallerTab>();
			_terminalTab.Name = "ComputerCallerTab";
			_terminalTab.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			screenRoot.AddChild(_terminalTab);
			_terminalTab.CloseRequested += OnTerminalViewRequested;
			_terminalTab.BackRequested += OnTerminalViewRequested;
		}

		_screenLiveMaterial = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			EmissionEnabled = true,
			Emission = new Color(1f, 1f, 1f, 1f),
			EmissionEnergyMultiplier = 1.4f,
			AlbedoColor = new Color(0.05f, 0.05f, 0.06f, 1f)
		};

		GD.Print($"EnsureTerminalViewport: viewport={_terminalViewport.Size} tab={_terminalTab != null}");
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
		_screenLiveMaterial.EmissionTexture = texture;
		_computerTerminal.ScreenMesh.MaterialOverride = _screenLiveMaterial;
		_addedScreenMaterial = true;

		SetupScreenDebugPreview();
		ScheduleDebugSample();
		GD.Print($"AttachScreenTexture: texture={texture.GetWidth()}x{texture.GetHeight()} applied");
	}

	private void DetachScreenTexture()
	{
		if (_computerTerminal == null)
		{
			return;
		}

		_computerTerminal.ScreenMesh.MaterialOverride = null;
		_addedScreenMaterial = false;
		HideScreenDebugPreview();
	}

	private bool _addedScreenMaterial;

	private void SetupScreenDebugPreview()
	{
		if (_screenDebugPreview != null || _terminalViewport == null)
		{
			return;
		}

		var panel = new PanelContainer
		{
			Name = "TerminalScreenPreview",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Position = new Vector2(16f, 64f)
		};
		var preview = new TextureRect
		{
			Texture = _terminalViewport.GetTexture(),
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			CustomMinimumSize = new Vector2(320f, 213f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		panel.AddChild(preview);
		_status_label?.GetParent()?.GetParent()?.AddChild(panel);
		_screenDebugPreview = preview;
	}

	private void HideScreenDebugPreview()
	{
		if (_screenDebugPreview == null)
		{
			return;
		}

		var panel = _screenDebugPreview.GetParent();
		_screenDebugPreview = null;
		panel?.QueueFree();
	}

	private void ScheduleDebugSample()
	{
		if (_debugSampleTicks < 0)
		{
			_debugSampleTicks = 30;
		}
	}

	private void UpdateDebugSample()
	{
		if (_debugSampleTicks < 0 || _terminalViewport == null)
		{
			return;
		}

		if (_debugSampleTicks-- > 0)
		{
			return;
		}

		_debugSampleTicks = -1;
		var viewportTexture = _terminalViewport.GetTexture();
		var image = viewportTexture.GetImage();
		if (image == null)
		{
			GD.Print("ScreenDebug: unable to read viewport image");
			return;
		}

		var nonBlack = 0;
		var total = 0;
		var size = image.GetSize();
		var data = image.GetData();
		for (var i = 0; i < data.Length; i += 4)
		{
			var r = data[i];
			var g = data[i + 1];
			var b = data[i + 2];
			total++;
			if (r > 24 || g > 24 || b > 24)
			{
				nonBlack++;
			}
		}

		GD.Print($"ScreenDebug: {size.X}x{size.Y}, non-black pixels={nonBlack}/{total} ({(total == 0 ? 0 : nonBlack * 100 / total)}%), materialApplied={_addedScreenMaterial}");
	}
}
