using System;
using System.Collections.Generic;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Screening;
using KBTV.UI.Themes;

namespace KBTV.UI
{
	public partial class ScreeningPanel : Control, IDependent
	{
		[ExportGroup("Node References")]
		[Export]
		private Label _headerRow = null!;

		[Export]
		private GridContainer _propertiesGrid = null!;

		[Export]
		private Button _approveButton = null!;

		[Export]
		private Button _rejectButton = null!;

		[ExportGroup("Optional UI")]
		[Export]
		private ProgressBar? _patienceBar;

		[Export]
		private Label? _patienceLabel;

		private IScreeningController _controller = null!;
		private ICallerRepository? _callerRepository;
		private Caller? _pendingCaller;

		private bool _nodesInitialized;
		private Caller? _previousCaller;
		private float _previousProgressPercent = -1f;

		public override void _Notification(int what) => this.Notify(what);

		public override void _Ready()
		{
			EnsureNodesInitialized();
			_nodesInitialized = _headerRow != null && _propertiesGrid != null &&
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

		private System.Collections.Generic.List<string> GetMissingNodeReferences()
		{
			var missing = new System.Collections.Generic.List<string>();

			if (_headerRow == null) missing.Add("VBoxContainer/CallerInfoScroll/InfoVBox/HeaderRow");
			if (_propertiesGrid == null) missing.Add("VBoxContainer/CallerInfoScroll/InfoVBox/PropertiesGrid");
			if (_approveButton == null) missing.Add("VBoxContainer/HBoxContainer/ApproveButton");
			if (_rejectButton == null) missing.Add("VBoxContainer/HBoxContainer/RejectButton");

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
			
			var currentProgress = _controller.Progress;
			if (currentProgress.ProgressPercent != _previousProgressPercent)
			{
				UpdatePatienceDisplay(currentProgress);
				_previousProgressPercent = currentProgress.ProgressPercent;
			}
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
				BuildPropertyGrid(caller);
				UpdateButtons();
			}
			else
			{
				_headerRow.Text = "Name: --  |  Phone: --  |  Location: --  |  Topic: --";
				ClearPropertyGrid();
			}
		}

		public void SetDisconnected(string callerName)
		{
			_headerRow.Text = $"CALLER DISCONNECTED: {callerName}";
			ClearPropertyGrid();
			UpdateButtons();
		}

		private void ClearPropertyGrid()
		{
			// Clear all children in the properties grid
			for (int i = _propertiesGrid.GetChildCount() - 1; i >= 0; i--)
			{
				var child = _propertiesGrid.GetChild(i);
				child.QueueFree();
			}
		}

		private void BuildPropertyGrid(Caller caller)
		{
			ClearPropertyGrid();

			// Build property pairs for 2-column layout
			var propertyPairs = new List<(string Label, string Value)>
			{
				("Quality", caller.PhoneQuality.ToString()),
				("Belief Level", caller.BeliefLevel.ToString()),
				("Emotional State", caller.EmotionalState.ToString()),
				("Evidence", caller.EvidenceLevel.ToString()),
				("Curse Risk", caller.CurseRisk.ToString()),
				("Coherence", caller.Coherence.ToString()),
				("Urgency", caller.Urgency.ToString()),
				("Legitimacy", caller.Legitimacy.ToString()),
			};

			// Create label-value pairs in 2 columns
			for (int i = 0; i < propertyPairs.Count; i++)
			{
				var (labelText, valueText) = propertyPairs[i];

				// Create left column (property name: value)
				var leftLabel = new Label();
				leftLabel.Text = $"{labelText}: {valueText}";
				leftLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
				leftLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
				_propertiesGrid.AddChild(leftLabel);

				// Create right column (next property, or empty if odd count)
				int nextIndex = i + 1;
				if (nextIndex < propertyPairs.Count)
				{
					var (nextLabel, nextValue) = propertyPairs[nextIndex];
					var rightLabel = new Label();
					rightLabel.Text = $"{nextLabel}: {nextValue}";
					rightLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
					rightLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
					_propertiesGrid.AddChild(rightLabel);
				}
				else
				{
					// Odd number - add empty spacer
					var spacer = new Label();
					spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
					_propertiesGrid.AddChild(spacer);
				}

				i++; // Skip the one we just paired
			}

			// Add personality as full-width row at the bottom
			var personalityLabel = new Label();
			personalityLabel.Text = $"Personality: {caller.Personality}";
			personalityLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			personalityLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			personalityLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			_propertiesGrid.AddChild(personalityLabel);

			// Add screening summary if present
			if (!string.IsNullOrEmpty(caller.ScreeningSummary))
			{
				var summaryLabel = new Label();
				summaryLabel.Text = caller.ScreeningSummary;
				summaryLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
				summaryLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
				summaryLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
				_propertiesGrid.AddChild(summaryLabel);
			}
		}

		private void OnApprovePressed()
		{
			if (_controller.CurrentCaller == null)
			{
				GD.Print("ScreeningPanel: Approve pressed but no caller is being screened - ignoring");
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
				GD.Print("ScreeningPanel: Reject pressed but no caller is being screened - ignoring");
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
			if (_patienceBar != null)
			{
				_patienceBar.Value = progress.ProgressPercent;
				var fillStyle = new StyleBoxFlat { BgColor = UIColors.GetPatienceColor(progress.ProgressPercent) };
				_patienceBar.AddThemeStyleboxOverride("fill", fillStyle);
			}

			if (_patienceLabel != null)
			{
				_patienceLabel.Text = $"Patience: {progress.PatienceRemaining:F1}s / {progress.MaxPatience:F1}s";
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
			GD.Print("ScreeningPanel: Cleanup complete");
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
			if (_propertiesGrid == null)
			{
				try
				{
					_propertiesGrid = GetNode<GridContainer>("VBoxContainer/CallerInfoScroll/InfoVBox/PropertiesGrid");
				}
				catch (Exception ex)
				{
					GD.PrintErr($"ScreeningPanel: Failed to find PropertiesGrid at VBoxContainer/CallerInfoScroll/InfoVBox/PropertiesGrid: {ex.Message}");
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
		}
	}
}
