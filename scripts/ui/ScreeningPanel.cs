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
	public partial class ScreeningPanel : Control
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

		[Export]
		private Control _statSummaryContainer = null!;

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

        // Pending properties for deferred stat summary update
        private ScreenableProperty[]? _pendingProperties;

		public override void _Notification(int what) => this.Notify(what);

		public override void _Ready()
		{
			// Initialize the screening controller dependency
			_controller = DependencyInjection.Get<IScreeningController>(this);
			_callerRepository = DependencyInjection.Get<ICallerRepository>(this);
 
			// Initialize node references
			EnsureNodesInitialized();

			// Connect button signals
			if (_approveButton != null)
			{
				_approveButton.Pressed += OnApprovePressed;
			}
			if (_rejectButton != null)
			{
				_rejectButton.Pressed += OnRejectPressed;
			}

			_nodesInitialized = true;
			GD.Print("ScreeningPanel _Ready called");
		}

		/// <summary>
		/// Ensure all node references are initialized from the scene tree.
		/// </summary>
		private void EnsureNodesInitialized()
		{
			_headerRow ??= GetNodeOrNull<Label>("VBoxContainer/CallerInfoScroll/InfoVBox/HeaderRow");
			_propertiesContainer ??= GetNodeOrNull<Control>("VBoxContainer/CallerInfoScroll/InfoVBox/PropertiesContainer");
			_approveButton ??= GetNodeOrNull<Button>("VBoxContainer/HBoxContainer/ApproveButton");
			_rejectButton ??= GetNodeOrNull<Button>("VBoxContainer/HBoxContainer/RejectButton");
			_patienceProgressBar ??= GetNodeOrNull<ProgressBar>("VBoxContainer/CallerInfoScroll/InfoVBox/PatienceHBox/PatienceProgressBar");
			_statSummaryContainer ??= GetNodeOrNull<Control>("VBoxContainer/StatSummaryContainer");
		}

		public override void _Process(double delta)
		{
			if (_controller.CurrentCaller == null)
			{
				// Update UI for no caller state
				UpdateForNoCaller();
			}
			else
			{
				// Update UI for caller screening
				UpdateForCaller(delta);
			}
		}

        public async void SetPendingCaller(Caller caller)
        {
            if (caller == null)
            {
                Log.Error("ScreeningPanel.SetPendingCaller: caller is null");
                return;
            }

            // If we have a pending caller that's different, clear current state and stat summary
            if (_pendingCaller != null && _pendingCaller != caller)
            {
                ClearCurrentState();
                // Clear stat summary to show "No caller data" briefly when switching callers
                if (_statSummaryPanel != null && IsInstanceValid(_statSummaryPanel))
                {
                    _statSummaryPanel.SetProperties(null);
                }
            }

            // Set the new pending caller
            _pendingCaller = caller;

            // Get properties for this caller (will create rows if needed)
            var properties = caller.ScreenableProperties ?? Array.Empty<ScreenableProperty>();
            _pendingProperties = properties;

            // Clear existing rows and create new ones
            ClearPropertyRows();
            foreach (var property in properties)
            {
                var row = CreatePropertyRow(property);
                _propertiesContainer.AddChild(row);
                _propertyRows.Add(row);
            }

            // Add stat summary panel and set properties after a brief delay to show the clear state
            EnsureStatSummaryPanel();
            if (_statSummaryPanel != null && IsInstanceValid(_statSummaryPanel))
            {
                CallDeferred(nameof(SetPendingStatSummary));
            }
        }

		/// <summary>
		/// Set the current caller for screening (alias for SetPendingCaller for compatibility).
		/// </summary>
		public void SetCaller(Caller? caller)
		{
			if (caller != null)
			{
				SetPendingCaller(caller);
			}
			else
			{
				ClearCurrentState();
			}
		}

		/// <summary>
		/// Update the UI when there is no active caller.
		/// </summary>
		private void UpdateForNoCaller()
		{
			_headerRow.Text = "Waiting for callers...";
			_approveButton.Disabled = true;
			_rejectButton.Disabled = true;

			if (_patienceProgressBar != null)
			{
				_patienceProgressBar.Value = 0f;
			}

			// Clear property rows when no caller
			ClearPropertyRows();

			// Clear stat summary when no caller
			if (_statSummaryPanel != null && IsInstanceValid(_statSummaryPanel))
			{
				_statSummaryPanel.SetProperties(null);
			}
		}

		/// <summary>
		/// Update the UI for the current caller.
		/// </summary>
		private void UpdateForCaller(double delta)
		{
			if (_controller.CurrentCaller == null) return;

			var caller = _controller.CurrentCaller!;
			var progress = _controller.Progress;
 
			// Update header
			_headerRow.Text = $"Screening: {caller.Name}";

			// Enable/disable buttons based on screening phase
			// Enable buttons during Gathering and Deciding phases
			bool canInteract = _controller.Phase == ScreeningPhase.Gathering || 
			                   _controller.Phase == ScreeningPhase.Deciding;
			_approveButton.Disabled = !canInteract;
			_rejectButton.Disabled = !canInteract;

			// Update patience progress bar if available
			if (_patienceProgressBar != null && caller.ScreeningPatience > 0)
			{
				_patienceProgressBar.MaxValue = caller.ScreeningPatience;
				_patienceProgressBar.Value = caller.ScreeningPatience - progress.ElapsedTime;
			}

			// Only update stat summary panel if properties have changed (performance optimization)
			var properties = caller.ScreenableProperties ?? Array.Empty<ScreenableProperty>();
			if (_statSummaryPanel != null && IsInstanceValid(_statSummaryPanel))
			{
				_statSummaryPanel.UpdateDisplay();
			}

			// Update typewriter animations for each property row (lightweight, per-frame)
			float deltaF = (float)delta;
			foreach (var row in _propertyRows)
			{
				if (GodotObject.IsInstanceValid(row))
				{
					try
					{
						row.UpdateAnimation(deltaF);
					}
					catch
					{
						// Animation update failed, skip
					}
				}
			}

			_previousProgressPercent = progress.RevelationPercent;
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
        /// Ensure the stat summary panel exists in the summary container.
        /// </summary>
        private void EnsureStatSummaryPanel()
        {
            // Only create if doesn't exist
            if (_statSummaryPanel == null || !IsInstanceValid(_statSummaryPanel))
            {
                if (_statSummaryContainer == null)
                {
                    Log.Error("ScreeningPanel: _statSummaryContainer is null");
                    return;
                }
                
                _statSummaryPanel = new StatSummaryPanel();
                _statSummaryPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                _statSummaryPanel.CustomMinimumSize = new Vector2(0, 120);
                
                _statSummaryContainer.AddChild(_statSummaryPanel);
                
                // CRITICAL: Manually trigger OnResolved() for dynamic IDependent nodes
                if (_statSummaryPanel is IDependent dependent)
                {
                    dependent.OnResolved();
                }
            }
        }

		/// <summary>
		/// Clear all property rows from the container.
		/// </summary>
		private void ClearPropertyRows()
		{
			_propertyRows.Clear();
			
			// Only clear direct ScreenablePropertyRow children from properties container
			if (_propertiesContainer != null)
			{
				for (int i = _propertiesContainer.GetChildCount() - 1; i >= 0; i--)
				{
					var child = _propertiesContainer.GetChild(i);
					if (child is ScreenablePropertyRow)
					{
						child.QueueFree();
					}
				}
			}
		}

		/// <summary>
		/// Clear the current caller state.
		/// </summary>
		private void ClearCurrentState()
		{
			_pendingCaller = null;
			_previousCaller = null;
			_previousProgressPercent = -1f;
			ClearPropertyRows();
			// DON'T dispose stat summary panel - it persists!
		}

		/// <summary>
		/// Handle approve button press.
		/// </summary>
		private void OnApprovePressed()
		{
			if (_controller.CurrentCaller == null)
			{
				return;
			}

			var result = _controller.Approve();
			if (!result.IsSuccess)
			{
				Log.Error($"ScreeningPanel: Approve failed - {result.ErrorCode}: {result.ErrorMessage}");
			}
		}

        /// <summary>
        /// Handle reject button press.
        /// </summary>
        private void OnRejectPressed()
        {
            if (_controller.CurrentCaller == null)
            {
                return;
            }

            var result = _controller.Reject();
            if (!result.IsSuccess)
            {
                Log.Error($"ScreeningPanel: Reject failed - {result.ErrorCode}: {result.ErrorMessage}");
            }
        }

        /// <summary>
        /// Set the pending stat summary properties (called via CallDeferred).
        /// </summary>
        private void SetPendingStatSummary()
        {
            if (_statSummaryPanel != null && IsInstanceValid(_statSummaryPanel) && _pendingProperties != null)
            {
                _statSummaryPanel.SetProperties(_pendingProperties);
                _pendingProperties = null; // Clear after use
            }
        }
    }
}