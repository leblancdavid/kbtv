#nullable enable

using Godot;
using KBTV.Core;
using KBTV.Dialogue;

namespace KBTV.UI
{
    /// <summary>
    /// Main conversation display panel for the LiveShow phase.
    /// Displays current dialogue with typewriter effect.
    /// Polls BroadcastCoordinator for the current line to display.
    /// </summary>
    public partial class LiveShowPanel : Control
    {
        [Export] private Label? _speakerIcon;
        [Export] private Label? _speakerName;
        [Export] private Label? _phaseLabel;
        [Export] private RichTextLabel? _dialogueLabel;
        [Export] private ProgressBar? _progressBar;

        private BroadcastCoordinator _coordinator = null!;

        private string _displayedText = string.Empty;
        private float _typewriterSpeed = 50f;
        private int _typewriterIndex = 0;
        private float _typewriterAccumulator = 0f;
        private string _currentLineText = "";

        public override void _EnterTree()
        {
            // Subscribe to events as early as possible to avoid missing initial line
            if (ServiceRegistry.Instance?.EventBus != null)
            {
                ServiceRegistry.Instance.EventBus.Subscribe<LineAvailableEvent>(HandleLineAvailable);
            }
        }

        public override void _Ready()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (ServiceRegistry.Instance == null)
            {
                CallDeferred(nameof(Initialize));
                return;
            }

            _coordinator = ServiceRegistry.Instance.BroadcastCoordinator;
            GD.Print($"DEBUG: LiveShowPanel coordinator assigned: {_coordinator != null}");

            _speakerIcon = GetNode<Label>("%SpeakerIcon");
            _speakerName = GetNode<Label>("%SpeakerName");
            _phaseLabel = GetNode<Label>("%PhaseLabel");
            _dialogueLabel = GetNode<RichTextLabel>("%DialogueContainer/DialogueLabel");
            _progressBar = GetNode<ProgressBar>("%ProgressBar");
        }



        private void InitializeDeferred()
        {
            if (!ServiceRegistry.IsInitialized)
            {
                GD.PrintErr("LiveShowPanel: ServiceRegistry not initialized, retrying...");
                CallDeferred(nameof(InitializeDeferred));
                return;
            }

            _coordinator = ServiceRegistry.Instance.BroadcastCoordinator;

            _speakerIcon = GetNode<Label>("%SpeakerIcon");
            _speakerName = GetNode<Label>("%SpeakerName");
            _phaseLabel = GetNode<Label>("%PhaseLabel");
            _dialogueLabel = GetNode<RichTextLabel>("%DialogueContainer/DialogueLabel");
            _progressBar = GetNode<ProgressBar>("%ProgressBar");

            GD.Print("LiveShowPanel: Initialized");
        }

        // Event-driven line handling instead of polling
        private void HandleLineAvailable(LineAvailableEvent @event)
        {
            GD.Print($"DEBUG: LiveShowPanel.HandleLineAvailable: Type={@event.Line.Type}, Speaker={@event.Line.Speaker}, Text='{@event.Line.Text?.Substring(0, 50)}'");

            var line = @event.Line;
            GD.Print($"DEBUG: LiveShowPanel processing line - Id: {line.Id}, Phase: {line.Phase}");

            if (line.Type == BroadcastLineType.None)
            {
                if (string.IsNullOrEmpty(line.Text))
                {
                    UpdateWaitingDisplay();
                }
                return;
            }

            if (line.Text != _currentLineText)
            {
                _currentLineText = line.Text;
                UpdateLineDisplay(line);
            }
        }

        public override void _Process(double delta)
        {
            // Only handle typewriter effect, line availability is now event-driven
            if (!string.IsNullOrEmpty(_currentLineText))
            {
                UpdateTypewriter(delta);
            }
        }

        private void UpdateWaitingDisplay()
        {
            if (_speakerIcon == null || _speakerName == null || _phaseLabel == null)
            {
                return;
            }

            _speakerIcon.Text = string.Empty;
            _speakerName.Text = "Waiting for broadcast...";
            _phaseLabel.Text = string.Empty;
            _dialogueLabel?.Clear();
            _progressBar?.Hide();
        }

        private void UpdateLineDisplay(BroadcastLine line)
        {
            if (_speakerIcon == null || _speakerName == null || _phaseLabel == null)
            {
                return;
            }

            // Hide speaker name and phase label for minimal display
            _speakerName.Text = "";
            _phaseLabel.Text = "";

            // Set speaker icon based on content type
            if (line.Type == BroadcastLineType.AdBreak || line.Type == BroadcastLineType.Ad)
            {
                _speakerIcon.Text = "AD BREAK";
            }
            else if (line.Type == BroadcastLineType.Music)
            {
                _speakerIcon.Text = "MUSIC";
            }
            else if (line.Type == BroadcastLineType.CallerDialogue)
            {
                _speakerIcon.Text = "CALLER";
            }
            else if (line.SpeakerId == "VERN")
            {
                _speakerIcon.Text = "VERN";
            }
            else
            {
                _speakerIcon.Text = line.SpeakerId; // Fallback for system messages, etc.
            }

            if (_dialogueLabel != null)
            {
                _dialogueLabel.Clear();
            }

            if (_progressBar != null)
            {
                _progressBar.Show();
            }
        }

        private static string GetFlowStateDisplayName(BroadcastLineType type)
        {
            return type switch
            {
                BroadcastLineType.ShowOpening => "SHOW OPENING",
                BroadcastLineType.DeadAirFiller => "DEAD AIR",
                BroadcastLineType.BetweenCallers => "BETWEEN CALLERS",
                BroadcastLineType.ShowClosing => "SHOW CLOSING",
                BroadcastLineType.VernDialogue => "ON AIR",
                BroadcastLineType.CallerDialogue => "ON AIR",
                BroadcastLineType.Ad => "COMMERCIAL",
                _ => ""
            };
        }

        private void UpdateTypewriter(double delta)
        {
            if (_dialogueLabel == null)
            {
                return;
            }

            _typewriterAccumulator += (float)delta * _typewriterSpeed;

            while (_typewriterAccumulator >= 1f && _typewriterIndex < _displayedText.Length)
            {
                var ch = _displayedText[_typewriterIndex];
                _dialogueLabel.Text += ch;
                _typewriterAccumulator -= 1f;
                _typewriterIndex++;
            }
        }

        public override void _ExitTree()
        {
            GD.Print("LiveShowPanel: Cleanup complete");
        }
    }
}
