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
        
        // Audio-synced typewriter state
        private float _currentLineDuration = 0f;
        private float _elapsedTime = 0f;

        public override void _EnterTree()
        {
            // Subscribe to events as early as possible to avoid missing initial line
            if (ServiceRegistry.Instance?.EventBus != null)
            {
                ServiceRegistry.Instance.EventBus.Subscribe<BroadcastEvent>(HandleBroadcastEvent);
                ServiceRegistry.Instance.EventBus.Subscribe<BroadcastItemStartedEvent>(HandleBroadcastItemStarted);
            }
        }

        public override void _Ready()
        {
            GD.Print("LiveShowPanel: Initializing with services...");
            InitializeWithServices();
        }

        private void InitializeWithServices()
        {
            _coordinator = ServiceRegistry.Instance.BroadcastCoordinator;
            GD.Print($"DEBUG: LiveShowPanel coordinator assigned: {_coordinator != null}");

            _speakerIcon = GetNode<Label>("%SpeakerIcon");
            _speakerName = GetNode<Label>("%SpeakerName");
            _phaseLabel = GetNode<Label>("%PhaseLabel");
            _dialogueLabel = GetNode<RichTextLabel>("%DialogueContainer/DialogueLabel");
            _progressBar = GetNode<ProgressBar>("%ProgressBar");

            GD.Print("LiveShowPanel: Initialization complete");
        }

        // Event-driven line handling using BroadcastEvent system
        private void HandleBroadcastEvent(BroadcastEvent @event)
        {
            // BroadcastEvent is for system-level coordination, we don't need to handle it directly
            // BroadcastItemStartedEvent handles the UI display with duration info
        }

        // Handle new broadcast item with duration information for audio-synced typewriter
        private void HandleBroadcastItemStarted(BroadcastItemStartedEvent @event)
        {
            var item = @event.Item;

            if (string.IsNullOrEmpty(item.Text))
            {
                UpdateWaitingDisplay();
                return;
            }

            // Start new line with audio-synced typewriter
            _currentLineText = item.Text;
            _currentLineDuration = @event.Duration;
            _elapsedTime = 0f;
            
            ResetTypewriterState();
            UpdateItemDisplay(item);
        }

public override void _Process(double delta)
        {
            // Only handle typewriter effect for active lines
            if (!string.IsNullOrEmpty(_currentLineText) && _currentLineDuration > 0)
            {
                UpdateTypewriter(delta);
                _elapsedTime += (float)delta;
                
                // Update progress bar based on elapsed time
                UpdateProgressBar();
            }
        }

        private void UpdateTypewriter(double delta)
        {
            if (_dialogueLabel == null || string.IsNullOrEmpty(_displayedText))
            {
                return;
            }

            // Calculate how much text should be revealed based on elapsed time
            float progress = Mathf.Min(_elapsedTime / _currentLineDuration, 1.0f);
            int targetIndex = (int)(progress * _displayedText.Length);
            
            // Reveal characters up to target position
            string revealedText = _displayedText.Substring(0, Mathf.Min(targetIndex, _displayedText.Length));
            _dialogueLabel.Text = revealedText;
        }

        private void UpdateProgressBar()
        {
            if (_progressBar != null && _currentLineDuration > 0)
            {
                float progress = Mathf.Min(_elapsedTime / _currentLineDuration, 1.0f);
                _progressBar.Value = progress * 100; // Convert to 0-100 range
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

private void UpdateItemDisplay(BroadcastItem item)
        {
            if (_speakerIcon == null || _speakerName == null || _phaseLabel == null)
            {
                return;
            }

            // Hide speaker name and phase label for minimal display
            _speakerName.Text = "";
            _phaseLabel.Text = "";

            // Set speaker icon based on content type
            if (item.Type == BroadcastItemType.Ad)
            {
                _speakerIcon.Text = "AD BREAK";
            }
            else if (item.Type == BroadcastItemType.Music)
            {
                _speakerIcon.Text = "MUSIC";
            }
            else if (item.Type == BroadcastItemType.CallerLine)
            {
                _speakerIcon.Text = "CALLER";
            }
            else if (item.Type == BroadcastItemType.VernLine)
            {
                _speakerIcon.Text = "VERN";
            }
            else if (item.Type == BroadcastItemType.DeadAir)
            {
                _speakerIcon.Text = "VERN";
            }
            else
            {
                _speakerIcon.Text = "SYSTEM"; // Fallback for transitions, etc.
            }

            // Reset typewriter state for new line
            ResetTypewriterState();
            
            if (_progressBar != null)
            {
                _progressBar.Show();
            }
        }

        private void ResetTypewriterState()
        {
            _displayedText = _currentLineText;
            _typewriterIndex = 0;
            _typewriterAccumulator = 0f;
            
            if (_dialogueLabel != null)
            {
                _dialogueLabel.Clear();
            }
        }

private static string GetFlowStateDisplayName(BroadcastItemType type)
        {
            return type switch
            {
                BroadcastItemType.Music => "MUSIC",
                BroadcastItemType.VernLine => "ON AIR",
                BroadcastItemType.CallerLine => "ON AIR",
                BroadcastItemType.Ad => "COMMERCIAL",
                BroadcastItemType.DeadAir => "DEAD AIR",
                BroadcastItemType.Transition => "TRANSITION",
                _ => ""
            };
}

        public override void _ExitTree()
        {
            GD.Print("LiveShowPanel: Cleanup complete");
        }
    }
}
