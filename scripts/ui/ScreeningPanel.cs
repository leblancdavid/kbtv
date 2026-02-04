using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Screening;
using KBTV.UI.Components;
using KBTV.UI.Themes;

namespace KBTV.UI
{
	public partial class ScreeningPanel : Control, IDependent
	{
		[ExportGroup("Node References")]
		[Export]
		private Label _headerRow = null!;

		[Export]
		private Control _propertiesContainer = null!;

		[Export]
		private Button _approveButton = null!;

		[Export]
		private Button _rejectButton = null!;

		[ExportGroup("Optional UI")]
		[Export]
		private ProgressBar? _patienceProgressBar;

		private IScreeningController _controller = null!;
		private ICallerRepository? _callerRepository;
		private Caller? _pendingCaller;

		private bool _nodesInitialized;
		private Caller? _previousCaller;
		private float _previousProgressPercent = -1f;

		// Property rows for animated reveal
		private List<ScreenablePropertyRow> _propertyRows = new();

		// Stat summary panel for aggregated effects
		private StatSummaryPanel? _statSummaryPanel;

		public override void _Notification(int what) => this.Notify(what);

		public override void _Ready()
		{
			EnsureNodesInitialized();
			_nodesInitialized = _headerRow != null && _propertiesContainer != null &&
			                   _approveButton != null && _rejectButton != null;
			if (_approveButton != null && _rejectButton != null)
			{
				ConnectSignals();
			}
			else
			{
				GD.PrintErr("ScreeningPanel: Skipping ConnectSignals() due to missing node references");
			}
			ValidateNodeReferences();
		}

		public void OnResolved()
		{
			_controller = DependencyInjection.Get<IScreeningController>(this);
			_callerRepository = DependencyInjection.Get<ICallerRepository>(this);
			_controller.PhaseChanged += OnPhaseChanged;
		}

		private void InitializeController()
		{
			_controller = DependencyInjection.Get<IScreeningController>(this);
			_controller.PhaseChanged += OnPhaseChanged;
		}

		private void OnPhaseChanged(ScreeningPhase newPhase)
		{
			if (newPhase != ScreeningPhase.Idle && newPhase != ScreeningPhase.Completed)
			{
				UpdateButtons();
			}
			else if (newPhase == ScreeningPhase.Completed)
			{
				UpdateButtons();
			}
		}

		private void ConnectSignals()
		{
			if (_approveButton == null)
			{
				GD.PrintErr("ScreeningPanel: _approveButton is null - EnsureNodesInitialized failed to find node");
				return;
			}
			if (_rejectButton == null)
			{
				GD.PrintErr("ScreeningPanel: _rejectButton is null - EnsureNodesInitialized failed to find node");
				return;
			}

			_approveButton!.Pressed += OnApprovePressed;
			_rejectButton!.Pressed += OnRejectPressed;
		}

		private void ValidateNodeReferences()
		{
			var missingNodes = GetMissingNodeReferences();
			if (missingNodes.Count > 0)
			{
				GD.PrintErr($"ScreeningPanel: Missing node references: {string.Join(", ", missingNodes)}");
			}
		}

		private List<string> GetMissingNodeReferences()
		{
			var missing = new List<string>();

			if (_headerRow == null) missing.Add("VBoxContainer/CallerInfoScroll/InfoVBox/HeaderRow");
			if (_propertiesContainer == null) missing.Add("VBoxContainer/CallerInfoScroll/InfoVBox/PropertiesContainer");
			if (_approveButton == null) missing.Add("VBoxContainer/HBoxContainer/ApproveButton");
			if (_rejectButton == null) missing.Add("VBoxContainer/HBoxContainer/RejectButton");
			if (_patienceProgressBar == null) missing.Add("VBoxContainer/CallerInfoScroll/InfoVBox/PatienceHBox/PatienceProgressBar");

			return missing;
		}

		public override void _Process(double delta)
		{
			var currentCaller = _controller.CurrentCaller;
			var isScreening = _callerRepository?.IsScreening == true;
			
			if (currentCaller != _previousCaller || !isScreening)
			{
				if (!isScreening && _previousCaller != null && _previousCaller.State == CallerState.Disconnected)
				{
					SetDisconnected(_previousCaller.Name);
				}
				else
				{
					SetCaller(currentCaller);
				}
				
				_previousCaller = currentCaller;
			}
			
			// Update patience display
			var currentProgress = _controller.Progress;
			if (currentProgress.ProgressPercent != _previousProgressPercent)
			{
				UpdatePatienceDisplay(currentProgress);
				_previousProgressPercent = currentProgress.ProgressPercent;
			}

			// Update property row animations (Matrix scramble + typewriter reveal)
			UpdatePropertyAnimations((float)delta);
		}

		/// <summary>
		/// Update all property row animations each frame.
		/// </summary>
		private void UpdatePropertyAnimations(float delta)
		{
			foreach (var row in _propertyRows)
			{
				row.UpdateAnimation(delta);
			}

			// Update the stat summary with revealed properties
			_statSummaryPanel?.UpdateDisplay();
		}

		public void SetCaller(Caller? caller)
		{
			_pendingCaller = caller;
			if (_nodesInitialized)
			{
				_SetCallerImmediate(_pendingCaller);
			}
			else
			{
				CallDeferred(nameof(_ApplyCallerDeferred));
			}
		}

		private void _ApplyCallerDeferred()
		{
			EnsureNodesInitialized();
			_SetCallerImmediate(_pendingCaller);
		}

		private void _SetCallerImmediate(Caller? caller)
		{
			if (caller != null)
			{
				_headerRow.Text = $"Name: {caller.Name}  |  Phone: {caller.PhoneNumber}  |  Location: {caller.Location}  |  Topic: {caller.ClaimedTopic}";
				BuildPropertyRows(caller);
				UpdateButtons();
			}
			else
			{
				_headerRow.Text = "Name: --  |  Phone: --  |  Location: --  |  Topic: --";
				ClearPropertyRows();
			}
		}

		public void SetDisconnected(string callerName)
		{
			_headerRow.Text = $"CALLER DISCONNECTED: {callerName}";
			ClearPropertyRows();
			UpdateButtons();
		}

		/// <summary>
		/// Clear all property rows from the container.
		/// </summary>
		private void ClearPropertyRows()
		{
			_propertyRows.Clear();

			for (int i = _propertiesContainer.GetChildCount() - 1; i >= 0; i--)
			{
				var child = _propertiesContainer.GetChild(i);
				child.QueueFree();
			}

			// Clear stat summary reference (it will be recreated when new caller is set)
			_statSummaryPanel = null;
		}

		/// <summary>
		/// Build property rows for all screenable properties.
		/// Properties are displayed in the random order determined by the caller.
		/// </summary>
		private void BuildPropertyRows(Caller caller)
		{
			ClearPropertyRows();

			if (caller.ScreenableProperties == null) return;

			// Create a row for each screenable property in the caller's random order
			foreach (var property in caller.ScreenableProperties)
			{
				var row = CreatePropertyRow(property);
				_propertiesContainer.AddChild(row);
				_propertyRows.Add(row);
			}

			// Add stat summary panel at the bottom
			EnsureStatSummaryPanel();
			_statSummaryPanel!.SetProperties(caller.ScreenableProperties);
		}

		/// <summary>
		/// Create a single property row for a screenable property.
		/// </summary>
		private ScreenablePropertyRow CreatePropertyRow(ScreenableProperty property)
		{
			var row = new ScreenablePropertyRow();
			row.SizeFlagsHorizontal = SizeFlags.ExpandFill;

			// Use Ready signal to ensure _Ready() has run before setting property
			row.Ready += () => row.SetProperty(property);

			return row;
		}

		/// <summary>
		/// Ensure the stat summary panel exists at the bottom of the properties container.
		/// </summary>
		private void EnsureStatSummaryPanel()
		{
			if (_statSummaryPanel != null && IsInstanceValid(_statSummaryPanel))
			{
				// Move to bottom if it already exists
				_propertiesContainer.MoveChild(_statSummaryPanel, _propertiesContainer.GetChildCount() - 1);
				return;
			}

			// Create new stat summary panel
			_statSummaryPanel = new StatSummaryPanel
			{
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				CustomMinimumSize = new Vector2(0, 40)
			};

			_propertiesContainer.AddChild(_statSummaryPanel);
		}

		private void OnApprovePressed()
		{
			if (_controller.CurrentCaller == null)
			{
				return;
			}

			var result = _controller.Approve();
			if (!result.IsSuccess)
			{
				GD.PrintErr($"ScreeningPanel: Approve failed - {result.ErrorCode}: {result.ErrorMessage}");
			}
		}

		private void OnRejectPressed()
		{
			if (_controller.CurrentCaller == null)
			{
				return;
			}

			var result = _controller.Reject();
			if (!result.IsSuccess)
			{
				GD.PrintErr($"ScreeningPanel: Reject failed - {result.ErrorCode}: {result.ErrorMessage}");
			}
		}

		private void UpdateButtons()
		{
			var hasCaller = _controller.CurrentCaller != null && _callerRepository?.IsScreening == true;
			_approveButton!.Disabled = !hasCaller;
			_rejectButton!.Disabled = !hasCaller;

			ButtonStyler.StyleApprove(_approveButton!, hasCaller);
			ButtonStyler.StyleReject(_rejectButton!, hasCaller);
		}

		private void UpdatePatienceDisplay(ScreeningProgress progress)
		{
			if (_patienceProgressBar != null)
			{
				_patienceProgressBar.Value = progress.ProgressPercent * 100f;
				var fillStyle = new StyleBoxFlat { BgColor = UIColors.GetPatienceColor(progress.ProgressPercent * 100f) };
				_patienceProgressBar.AddThemeStyleboxOverride("fill", fillStyle);
			}
		}



		public void ConnectButtons(Callable approveCallable, Callable rejectCallable)
		{
			if (_approveButton != null)
			{
				_approveButton.Connect("pressed", approveCallable);
			}
			if (_rejectButton != null)
			{
				_rejectButton.Connect("pressed", rejectCallable);
			}
		}

		public override void _ExitTree()
		{
			if (_controller != null)
			{
				_controller.PhaseChanged -= OnPhaseChanged;
			}
		}

		private void EnsureNodesInitialized()
		{
			if (_headerRow == null)
			{
				try
				{
					_headerRow = GetNode<Label>("VBoxContainer/CallerInfoScroll/InfoVBox/HeaderRow");
				}
				catch (Exception ex)
				{
					GD.PrintErr($"ScreeningPanel: Failed to find HeaderRow at VBoxContainer/CallerInfoScroll/InfoVBox/HeaderRow: {ex.Message}");
				}
			}
			if (_propertiesContainer == null)
			{
				try
				{
					// Try the new VBoxContainer first, fall back to GridContainer for backwards compatibility
					_propertiesContainer = GetNodeOrNull<VBoxContainer>("VBoxContainer/CallerInfoScroll/InfoVBox/PropertiesContainer");
					if (_propertiesContainer == null)
					{
						_propertiesContainer = GetNodeOrNull<GridContainer>("VBoxContainer/CallerInfoScroll/InfoVBox/PropertiesGrid");
					}
				}
				catch (Exception ex)
				{
					GD.PrintErr($"ScreeningPanel: Failed to find PropertiesContainer: {ex.Message}");
				}
			}
			if (_approveButton == null)
			{
				try
				{
					_approveButton = GetNode<Button>("VBoxContainer/HBoxContainer/ApproveButton");
				}
				catch (Exception ex)
				{
					GD.PrintErr($"ScreeningPanel: Failed to find ApproveButton at VBoxContainer/HBoxContainer/ApproveButton: {ex.Message}");
				}
			}
			if (_rejectButton == null)
			{
				try
				{
					_rejectButton = GetNode<Button>("VBoxContainer/HBoxContainer/RejectButton");
				}
				catch (Exception ex)
				{
					GD.PrintErr($"ScreeningPanel: Failed to find RejectButton at VBoxContainer/HBoxContainer/RejectButton: {ex.Message}");
				}
			}

			// Optional patience UI nodes
			if (_patienceProgressBar == null)
			{
				try
				{
					_patienceProgressBar = GetNodeOrNull<ProgressBar>("VBoxContainer/CallerInfoScroll/InfoVBox/PatienceHBox/PatienceProgressBar");
				}
				catch (Exception ex)
				{
					GD.PrintErr($"ScreeningPanel: Failed to find PatienceProgressBar: {ex.Message}");
				}
			}
		}
	}
}
