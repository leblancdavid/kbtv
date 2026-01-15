using Godot;
using System;
using KBTV.Callers;

namespace KBTV.UI
{
    /// <summary>
    /// Interface for caller action callbacks used by CallerTabManager
    /// </summary>
    public interface ICallerActions
    {
        void OnApproveCaller();
        void OnRejectCaller();
    }

    /// <summary>
    /// Interface for caller tab management
    /// </summary>
    public interface ICallerTabManager
    {
        void PopulateContent(Control contentArea);
        void RefreshContent();
        void Cleanup();
    }

    /// <summary>
    /// Manages the caller tab UI, independent of UIManagerBootstrap
    /// </summary>
    public partial class CallerTabManager : ICallerTabManager
    {
        private readonly CallerQueue _callerQueue;
        private readonly ICallerActions _callerActions;
        private readonly PanelFactory _panelFactory;

        // UI state
        private Control _currentContentArea;
        private Control _mainContainer;

        public CallerTabManager(CallerQueue callerQueue, ICallerActions callerActions)
        {
            _callerQueue = callerQueue ?? throw new ArgumentNullException(nameof(callerQueue));
            _callerActions = callerActions ?? throw new ArgumentNullException(nameof(callerActions));
            _panelFactory = new PanelFactory(callerQueue, callerActions);
        }

        /// <summary>
        /// Populate the caller tab content area
        /// </summary>
		public void PopulateContent(Control contentArea)
		{
			GD.Print($"CallerTabManager.PopulateContent called: contentArea null={contentArea == null}");

			try
			{
				_currentContentArea = contentArea ?? throw new ArgumentNullException(nameof(contentArea));

				GD.Print("CallerTabManager: Creating MarginContainer...");

                if (_callerQueue == null)
                {
                    ShowErrorMessage(contentArea, "Caller system not available");
                    return;
                }

                // Clear any existing content
                ClearContentArea(contentArea);

				// Create main container
				GD.Print("CallerTabManager: Calling CreateMainContainer...");
				_mainContainer = CreateMainContainer();
				GD.Print($"CallerTabManager: CreateMainContainer returned: {_mainContainer != null}");

				if (_mainContainer != null)
				{
					GD.Print("CallerTabManager: Adding main container to contentArea...");
					contentArea.AddChild(_mainContainer);
					GD.Print($"CallerTabManager: Created main container with {_mainContainer.GetChildCount()} children");
				}
				else
				{
					GD.PrintErr("CallerTabManager: CreateMainContainer returned null!");
				}
            }
            catch (Exception ex)
            {
                GD.PrintErr($"CallerTabManager.PopulateContent failed: {ex.Message}");
                ShowErrorMessage(contentArea, "Failed to load caller interface");
            }
        }

        /// <summary>
        /// Refresh the caller tab content
        /// </summary>
        public void RefreshContent()
        {
            try
            {
                if (_currentContentArea == null)
                {
                    GD.PrintErr("CallerTabManager.RefreshContent: No content area set");
                    return;
                }

                GD.Print("CallerTabManager: Refreshing caller content");

                // Re-populate content
                PopulateContent(_currentContentArea);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"CallerTabManager.RefreshContent failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Cleanup()
        {
            try
            {
                _currentContentArea = null;
                _mainContainer = null;
                GD.Print("CallerTabManager: Cleanup completed");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"CallerTabManager.Cleanup failed: {ex.Message}");
            }
        }

		private Control CreateMainContainer()
		{
			GD.Print("CallerTabManager.CreateMainContainer: Starting...");

			try
			{
				// MarginContainer provides padding and ensures full size usage
				var marginContainer = new MarginContainer();
				marginContainer.Name = "CallerTabMarginContainer";
				GD.Print("CallerTabManager: Created MarginContainer");

				marginContainer.AddThemeConstantOverride("margin_left", 10);
				marginContainer.AddThemeConstantOverride("margin_top", 10);
				marginContainer.AddThemeConstantOverride("margin_right", 10);
				marginContainer.AddThemeConstantOverride("margin_bottom", 10);
				GD.Print("CallerTabManager: Set margin constants");

				// HBoxContainer distributes panels horizontally
				var mainContainer = new HBoxContainer();
				mainContainer.Name = "CallersMainContainer";
				// Removed minimum size constraint to allow full expansion
				mainContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
				mainContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
				GD.Print("CallerTabManager: Created HBoxContainer");

				// Left panel: Incoming callers with vertical scrolling (25% width)
				GD.Print("CallerTabManager: Creating incoming callers panel...");
				var incomingScroll = CreateScrollableCallerPanel("INCOMING CALLERS", _callerQueue.IncomingCallers, new Color(1f, 0.7f, 0f), new Color(0.8f, 0.8f, 0.8f));
				if (incomingScroll != null)
				{
					incomingScroll.SizeFlagsStretchRatio = 1; // 25%
					mainContainer.AddChild(incomingScroll);
					GD.Print("CallerTabManager: Added incoming callers panel");
				}
				else
				{
					GD.PrintErr("CallerTabManager: Incoming callers panel creation failed!");
				}

				// Middle panel: Screening controls (50% width)
				GD.Print("CallerTabManager: Creating screening panel...");
				var screeningPanel = CreateScreeningPanelScene();
				if (screeningPanel != null)
				{
					screeningPanel.SizeFlagsStretchRatio = 2; // 50%
					mainContainer.AddChild(screeningPanel);
					GD.Print("CallerTabManager: Added screening panel");
				}
				else
				{
					GD.PrintErr("CallerTabManager: Screening panel creation failed!");
				}

				// Right panel: On-hold callers with vertical scrolling (25% width)
				GD.Print("CallerTabManager: Creating on-hold callers panel...");
				var onHoldScroll = CreateScrollableCallerPanel("ON HOLD", _callerQueue.OnHoldCallers, new Color(0f, 0.7f, 1f), new Color(0.6f, 0.6f, 0.6f));
				if (onHoldScroll != null)
				{
					onHoldScroll.SizeFlagsStretchRatio = 1; // 25%
					mainContainer.AddChild(onHoldScroll);
					GD.Print("CallerTabManager: Added on-hold callers panel");
				}
				else
				{
					GD.PrintErr("CallerTabManager: On-hold callers panel creation failed!");
				}

				marginContainer.AddChild(mainContainer);
				GD.Print($"CallerTabManager: CreateMainContainer completed successfully with {mainContainer.GetChildCount()} panels");
				return marginContainer;
			}
			catch (Exception ex)
			{
				GD.PrintErr($"CallerTabManager.CreateMainContainer failed: {ex.Message}\n{ex.StackTrace}");
				return null;
			}
    }

		private Control CreateScrollableCallerPanel(string headerText, System.Collections.Generic.IReadOnlyList<Caller> callers, Color headerColor, Color itemColor)
		{
			GD.Print($"CallerTabManager.CreateScrollableCallerPanel: Creating '{headerText}' panel");

			try
			{
				var scrollContainer = new ScrollContainer();
				scrollContainer.Name = $"{headerText.Replace(" ", "")}ScrollContainer";
				scrollContainer.ScrollHorizontal = (int)ScrollContainer.ScrollMode.Disabled; // No horizontal scrolling
				scrollContainer.ScrollVertical = (int)ScrollContainer.ScrollMode.Auto; // Auto vertical scrolling
				scrollContainer.CustomMinimumSize = new Vector2(200, 300); // Minimum size for caller panels
				GD.Print("CallerTabManager: Created ScrollContainer");

				var callerPanel = CreateCallerPanelScene(headerText, callers, headerColor, itemColor);
				if (callerPanel != null)
				{
					scrollContainer.AddChild(callerPanel);
					GD.Print($"CallerTabManager: Added caller panel to ScrollContainer");
				}
				else
				{
					GD.PrintErr("CallerTabManager: CreateCallerPanelScene returned null!");
				}

				return scrollContainer;
			}
			catch (Exception ex)
			{
				GD.PrintErr($"CallerTabManager.CreateScrollableCallerPanel failed: {ex.Message}");
				return null;
			}
		}

		private Control CreateCallerPanelScene(string headerText, System.Collections.Generic.IReadOnlyList<Caller> callers, Color headerColor, Color itemColor)
		{
			GD.Print($"CallerTabManager.CreateCallerPanelScene: Loading scene for '{headerText}'");

			try
			{
				string scenePath = "res://scenes/ui/CallerPanel.tscn";
				var scene = ResourceLoader.Load<PackedScene>(scenePath);
				GD.Print($"CallerTabManager: Scene loaded: {scene != null}, path exists: {ResourceLoader.Exists(scenePath)}");

				if (scene != null)
				{
					var panel = scene.Instantiate<CallerPanel>();
					GD.Print($"CallerTabManager: Panel instantiated: {panel != null}");

					if (panel != null)
					{
						GD.Print($"CallerTabManager: Calling SetHeader with '{headerText}'");
						panel.SetHeader(headerText, headerColor);
						GD.Print("CallerTabManager: Calling SetCallers");
						panel.SetCallers(callers, itemColor);
						GD.Print("CallerTabManager: Panel creation successful");
						return panel;
					}
				}

                // Fallback to programmatic creation
                GD.Print("CallerTabManager: Using fallback panel creation");
                return CreateCallerPanelFallback(headerText, callers, headerColor, itemColor);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"CallerTabManager.CreateCallerPanelScene failed: {ex.Message}");
                return CreateErrorPanel($"Failed to create {headerText.ToLower()} panel");
            }
        }

		private Control CreateScreeningPanelScene()
		{
			GD.Print("CallerTabManager.CreateScreeningPanelScene: Loading screening panel");

			try
			{
				string scenePath = "res://scenes/ui/ScreeningPanel.tscn";
				var scene = ResourceLoader.Load<PackedScene>(scenePath);
				GD.Print($"CallerTabManager: Screening scene loaded: {scene != null}, path exists: {ResourceLoader.Exists(scenePath)}");

				if (scene != null)
				{
					var panel = scene.Instantiate<ScreeningPanel>();
					GD.Print($"CallerTabManager: Screening panel instantiated: {panel != null}");

					if (panel != null)
					{
						// Ensure proper sizing for the panel
						panel.CustomMinimumSize = new Vector2(300, 300); // Minimum screening panel size
						GD.Print("CallerTabManager: Set screening panel size");

						// Set caller info
						if (_callerQueue.IsScreening)
						{
							GD.Print("CallerTabManager: Setting current screening caller");
							panel.SetCaller(_callerQueue.CurrentScreening);
						}
						else
						{
							GD.Print("CallerTabManager: No caller screening, setting null");
							panel.SetCaller(null);
						}

						// Connect buttons
						GD.Print("CallerTabManager: Connecting screening panel buttons");
						panel.ConnectButtons(
							Callable.From(() => OnApprovePressed()),
							Callable.From(() => OnRejectPressed())
						);

						GD.Print("CallerTabManager: Screening panel creation successful");
						return panel;
					}
				}

                // Fallback would go here if needed
                GD.PrintErr("CallerTabManager: Failed to create screening panel");
                return CreateErrorPanel("Failed to create screening panel");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"CallerTabManager.CreateScreeningPanelScene failed: {ex.Message}");
                return CreateErrorPanel("Failed to create screening panel");
            }
        }

        private Control CreateCallerPanelFallback(string headerText, System.Collections.Generic.IReadOnlyList<Caller> callers, Color headerColor, Color itemColor)
        {
            try
            {
                return CreateCallerPanel("FallbackPanel", headerText, callers, headerColor, itemColor);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"CallerTabManager.CreateCallerPanelFallback failed: {ex.Message}");
                return CreateErrorPanel($"Failed to create {headerText.ToLower()} panel");
            }
        }

        private Control CreateCallerPanel(string name, string headerText, System.Collections.Generic.IReadOnlyList<Caller> callers, Color headerColor, Color itemColor)
        {
            GD.Print($"CallerTabManager: Creating caller panel {name} with {callers?.Count ?? 0} callers");

            var panel = new Panel();
            panel.Name = name;
            UITheme.ApplyPanelStyle(panel);
            panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

            var layout = new VBoxContainer();
            layout.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            layout.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            panel.AddChild(layout);

            // Header
            var header = new Label();
            header.Text = headerText;
            header.AddThemeColorOverride("font_color", headerColor);
            header.HorizontalAlignment = HorizontalAlignment.Center;
            layout.AddChild(header);

            // Scrollable list
            var scroll = new ScrollContainer();
            scroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            layout.AddChild(scroll);

            var listContainer = new VBoxContainer();
            listContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            listContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            scroll.AddChild(listContainer);

            // Add caller items
            if (callers != null && callers.Count > 0)
            {
                foreach (var caller in callers)
                {
                    var callerLabel = new Label();
                    callerLabel.Text = $"{caller.Name} - {caller.Location}";
                    callerLabel.AddThemeColorOverride("font_color", itemColor);
                    listContainer.AddChild(callerLabel);
                }
            }
            else
            {
                var emptyLabel = new Label();
                emptyLabel.Text = "None";
                emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
                emptyLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
                listContainer.AddChild(emptyLabel);
            }

            return panel;
        }

        private Control CreateErrorPanel(string message)
        {
            var panel = new Panel();
            panel.Name = "ErrorPanel";
            UITheme.ApplyPanelStyle(panel);

            var label = new Label();
            label.Text = message;
            label.HorizontalAlignment = HorizontalAlignment.Center;
            panel.AddChild(label);

            return panel;
        }

        private void ClearContentArea(Control contentArea)
        {
            foreach (var child in contentArea.GetChildren())
            {
                contentArea.RemoveChild(child);
                child.QueueFree();
            }
        }

        private void ShowErrorMessage(Control parent, string message)
        {
            ClearContentArea(parent);
            var errorLabel = new Label { Text = message };
            errorLabel.HorizontalAlignment = HorizontalAlignment.Center;
            parent.AddChild(errorLabel);
        }

        private void OnApprovePressed()
        {
            try
            {
                _callerActions.OnApproveCaller();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"CallerTabManager.OnApprovePressed failed: {ex.Message}");
            }
        }

        private void OnRejectPressed()
        {
            try
            {
                _callerActions.OnRejectCaller();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"CallerTabManager.OnRejectPressed failed: {ex.Message}");
            }
        }
    }
}