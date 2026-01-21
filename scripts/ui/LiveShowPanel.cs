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

        public override void _Ready()
        {
            CallDeferred(nameof(InitializeDeferred));
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

        public override void _Process(double delta)
        {
            if (_coordinator == null)
            {
                return;
            }

            var line = _coordinator.GetNextLine();

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
                _displayedText = line.Text;
                _typewriterIndex = 0;
                _typewriterAccumulator = 0f;

                // Clear previous text to show only the current line
                _dialogueLabel?.Clear();

                UpdateLineDisplay(line);
            }

            if (_typewriterIndex < _displayedText.Length)
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

            _speakerIcon.Text = line.SpeakerId;
            _speakerName.Text = line.Speaker;

            _phaseLabel.Text = GetFlowStateDisplayName(line.Type);

            switch (line.Type)
            {
                case BroadcastLineType.DeadAirFiller:
                    _phaseLabel.Modulate = new Color(0.5f, 0.5f, 0.5f);
                    break;
                case BroadcastLineType.VernDialogue:
                case BroadcastLineType.CallerDialogue:
                    _phaseLabel.Modulate = new Color(0.5f, 0.8f, 0.5f);
                    break;
                case BroadcastLineType.ShowOpening:
                case BroadcastLineType.BetweenCallers:
                case BroadcastLineType.ShowClosing:
                    _phaseLabel.Modulate = new Color(0.9f, 0.7f, 0.3f);
                    break;
                case BroadcastLineType.Ad:
                    _phaseLabel.Modulate = new Color(1f, 0.8f, 0f); // Gold/yellow for commercials
                    break;
                default:
                    _phaseLabel.Modulate = new Color(1, 1, 1);
                    break;
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
