#nullable enable

using Godot;
using KBTV.Core;
using KBTV.Dialogue;

namespace KBTV.UI
{
    public partial class TranscriptPanel : Control
    {
        [Export] private Label _currentLineLabel = null!;
        [Export] private VBoxContainer _historyContainer = null!;
        [Export] private PanelContainer _currentLinePanel = null!;

        private BroadcastCoordinator _coordinator = null!;
        private int _maxHistoryLines = 20;
        private string _currentLineText = "";

        public override void _Ready()
        {
            if (!ServiceRegistry.IsInitialized)
            {
                CallDeferred(nameof(RetryInitialization));
                return;
            }

            InitializeWithServices();
        }

        private void RetryInitialization()
        {
            if (ServiceRegistry.IsInitialized)
            {
                InitializeWithServices();
            }
            else
            {
                CallDeferred(nameof(RetryInitialization));
            }
        }

        private void InitializeWithServices()
        {
            _coordinator = ServiceRegistry.Instance.BroadcastCoordinator;

            if (_currentLineLabel == null)
            {
                _currentLineLabel = GetNode<Label>("CurrentLineLabel");
            }
            if (_historyContainer == null)
            {
                _historyContainer = GetNode<VBoxContainer>("HistoryContainer");
            }
            if (_currentLinePanel == null)
            {
                _currentLinePanel = GetNode<PanelContainer>("CurrentLinePanel");
            }

            GD.Print("TranscriptPanel: Initialized");
        }

        public override void _Process(double delta)
        {
            if (_coordinator == null)
            {
                return;
            }

            var line = _coordinator.GetNextLine();

            if (line.Type == BroadcastLineType.None)
            {
                return;
            }

            if (line.Text != _currentLineText)
            {
                _currentLineText = line.Text;
                DisplayCurrentLine(line);
                AddToHistory(line);
            }
        }

        private void DisplayCurrentLine(BroadcastLine line)
        {
            if (_currentLineLabel != null)
            {
                var speakerIcon = line.Speaker == "Vern" ? "VERN" : "CALLER";
                _currentLineLabel.Text = $"{speakerIcon}: {line.Text}";
            }

            var progress = line.Type == BroadcastLineType.DeadAirFiller
                ? ConversationPhase.Intro
                : line.Phase;

            UpdatePanelStyle(progress);
        }

        private void UpdatePanelStyle(ConversationPhase phase)
        {
            if (_currentLinePanel == null)
            {
                return;
            }

            var theme = _currentLinePanel.GetThemeStylebox("panel");
            if (theme != null)
            {
                return;
            }
        }

        private void AddToHistory(BroadcastLine line)
        {
            if (_historyContainer == null)
            {
                return;
            }

            var historyLabel = new Label();
            historyLabel.Text = $"{line.Speaker}: {line.Text}";
            historyLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            historyLabel.CustomMinimumSize = new Vector2(0, 40);
            _historyContainer.AddChild(historyLabel);

            while (_historyContainer.GetChildCount() > _maxHistoryLines)
            {
                var child = _historyContainer.GetChild(0);
                child.QueueFree();
                _historyContainer.RemoveChild(child);
            }
        }

        public void ClearTranscript()
        {
            _currentLineText = "";
            if (_currentLineLabel != null)
            {
                _currentLineLabel.Text = "";
            }

            if (_historyContainer != null)
            {
                foreach (var child in _historyContainer.GetChildren())
                {
                    child.QueueFree();
                }
            }
        }
    }
}
