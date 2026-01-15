using Godot;
using KBTV.Callers;

namespace KBTV.UI
{
    /// <summary>
    /// Self-contained caller tab component.
    /// Manages its own lifecycle, connects to signals, and handles UI updates.
    /// Follows Godot's component-based architecture with no external dependencies.
    /// </summary>
    public partial class CallerTab : Control, ICallerActions
    {
        private CallerQueue _callerQueue;
        private CallerTabManager _tabManager;

        // UI references (set in scene)
        private Control _incomingPanel;
        private Control _screeningPanel;
        private Control _onHoldPanel;

        public override void _Ready()
        {
            GD.Print("CallerTab: Initializing self-contained caller tab");

            // Get UI references from scene
            _incomingPanel = GetNode<Control>("HBoxContainer/IncomingScroll/IncomingPanel");
            _screeningPanel = GetNode<Control>("HBoxContainer/ScreeningContainer");
            _onHoldPanel = GetNode<Control>("HBoxContainer/OnHoldScroll/OnHoldPanel");

            // Initialize dependencies
            _callerQueue = CallerQueue.Instance;
            if (_callerQueue == null)
            {
                GD.PrintErr("CallerTab: CallerQueue not found!");
                return;
            }

            // Create tab manager with self as actions handler
            _tabManager = new CallerTabManager(_callerQueue, this);

            // Connect to CallerQueue signals for real-time updates
            _callerQueue.Connect("CallerAdded", Callable.From<Caller>(OnCallerAdded));
            _callerQueue.Connect("CallerRemoved", Callable.From<Caller>(OnCallerRemoved));
            _callerQueue.Connect("CallerApproved", Callable.From<Caller>(OnCallerApproved));

            GD.Print("CallerTab: Connected to CallerQueue signals");

            // Initial population
            PopulateTabContent();

            GD.Print("CallerTab: Initialization complete");
        }

        private void PopulateTabContent()
        {
            GD.Print("CallerTab: Populating tab content");

            // Debug: Check caller queue status
            if (_callerQueue != null)
            {
                GD.Print($"CallerTab: Incoming callers: {_callerQueue.IncomingCallers.Count}, On-hold: {_callerQueue.OnHoldCallers.Count}, IsScreening: {_callerQueue.IsScreening}");
            }
            else
            {
                GD.PrintErr("CallerTab: CallerQueue is null!");
            }

            // Create incoming callers panel
            _tabManager.CreateIncomingPanel(_incomingPanel);

            // Create screening panel
            _tabManager.CreateScreeningPanel(_screeningPanel);

            // Create on-hold callers panel
            _tabManager.CreateOnHoldPanel(_onHoldPanel);
        }

        private void OnCallerAdded(Caller caller)
        {
            GD.Print($"CallerTab: Caller added - {caller.Name}");
            RefreshTabContent();
        }

        private void OnCallerRemoved(Caller caller)
        {
            GD.Print($"CallerTab: Caller removed - {caller.Name}");
            RefreshTabContent();
        }

        private void OnCallerApproved(Caller caller)
        {
            GD.Print($"CallerTab: Caller approved - {caller.Name}");
            RefreshTabContent();
        }

        private void RefreshTabContent()
        {
            // Clear existing content
            ClearPanel(_incomingPanel);
            ClearPanel(_screeningPanel);
            ClearPanel(_onHoldPanel);

            // Re-populate
            PopulateTabContent();
        }

        private void ClearPanel(Control panel)
        {
            foreach (var child in panel.GetChildren())
            {
                panel.RemoveChild(child);
                child.QueueFree();
            }
        }

        // ICallerActions implementation
        public void OnApproveCaller()
        {
            GD.Print("CallerTab: Approve button pressed");
            if (_callerQueue.ApproveCurrentCaller())
            {
                GD.Print("CallerTab: Caller approved successfully");
                RefreshTabContent();
            }
            else
            {
                GD.PrintErr("CallerTab: Failed to approve caller");
            }
        }

        public void OnRejectCaller()
        {
            GD.Print("CallerTab: Reject button pressed");
            if (_callerQueue.RejectCurrentCaller())
            {
                GD.Print("CallerTab: Caller rejected successfully");
                RefreshTabContent();
            }
            else
            {
                GD.PrintErr("CallerTab: Failed to reject caller");
            }
        }

        public override void _ExitTree()
        {
            // Clean up signal connections
            if (_callerQueue != null)
            {
                _callerQueue.Disconnect("CallerAdded", Callable.From<Caller>(OnCallerAdded));
                _callerQueue.Disconnect("CallerRemoved", Callable.From<Caller>(OnCallerRemoved));
                _callerQueue.Disconnect("CallerApproved", Callable.From<Caller>(OnCallerApproved));
            }

            GD.Print("CallerTab: Cleanup complete");
        }
    }
}