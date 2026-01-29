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
    public partial class LiveShowPanel : Control, IDependent
    {
        [Export] private Label? _speakerIcon;
        [Export] private Label? _speakerName;
        [Export] private Label? _phaseLabel;
        [Export] private RichTextLabel? _dialogueLabel;
        [Export] private ProgressBar? _progressBar;

        private BroadcastCoordinator _coordinator = null!;
        private BroadcastItemStartedEvent? _pendingBroadcastItemStartedEvent;

        public override void _Notification(int what) => this.Notify(what);

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
            // Dependencies will be resolved in OnResolved()
        }

        public override void _Ready()
        {
            GD.Print("LiveShowPanel: Ready, waiting for dependencies...");
            // Dependencies resolved in OnResolved()
        }

        /// <summary>
        /// Called when all dependencies are resolved.
        /// </summary>
        public void OnResolved()
        {
            GD.Print("LiveShowPanel: Dependencies resolved, initializing...");

            // Get dependencies via DI
            var eventBus = DependencyInjection.Get<EventBus>(this);
            _coordinator = DependencyInjection.Get<BroadcastCoordinator>(this);

            // Subscribe to events
            eventBus.Subscribe<BroadcastEvent>(HandleBroadcastEvent);
            eventBus.Subscribe<BroadcastItemStartedEvent>(HandleBroadcastItemStarted);
            eventBus.Subscribe<BroadcastStateChangedEvent>(HandleBroadcastStateChanged);

            GD.Print($"DEBUG: LiveShowPanel coordinator assigned: {_coordinator != null}");

            // Initialize UI nodes
            _speakerIcon = GetNode<Label>("%SpeakerIcon");
            _speakerName = GetNode<Label>("%SpeakerName");
            _phaseLabel = GetNode<Label>("%PhaseLabel");
            _dialogueLabel = GetNode<RichTextLabel>("%DialogueContainer/DialogueLabel");
            _progressBar = GetNode<ProgressBar>("%ProgressBar");
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
            GD.Print($"LiveShowPanel: Received BroadcastItemStartedEvent - Type: {@event.Item.Type}, Text: '{@event.Item.Text}', Duration: {@event.Duration}");
            _pendingBroadcastItemStartedEvent = @event;
            CallDeferred("DeferredHandleBroadcastItemStarted");
        }

        private void DeferredHandleBroadcastItemStarted()
        {
            if (_pendingBroadcastItemStartedEvent == null) return;
            
            var @event = _pendingBroadcastItemStartedEvent;
            _pendingBroadcastItemStartedEvent = null;
            
            var item = @event.Item;

            // Skip displaying internal state transition operations
            if (item.Type == BroadcastItemType.PutOnAir)
            {
                return;
            }

            if (string.IsNullOrEmpty(item.Text))
            {
                DeferredUpdateWaitingDisplay();
                return;
            }

            // Start new line with audio-synced typewriter
            _currentLineText = item.Text;
            float rawDuration = @event.AudioLength > 0 ? @event.AudioLength : @event.Duration;
            _currentLineDuration = Mathf.Max(rawDuration - 1.5f, 0.5f);
            _elapsedTime = 0f;
            
            DeferredResetTypewriterState();
            DeferredUpdateItemDisplay(item);
        }

        // Handle broadcast interruptions (breaks, show ending)
        private void HandleBroadcastInterruption(BroadcastInterruptionEvent @event)
        {
            GD.Print($"LiveShowPanel: Received BroadcastInterruptionEvent - Reason: {@event.Reason}");
            CallDeferred("DeferredHandleBroadcastInterruption");
        }

        private void DeferredHandleBroadcastInterruption()
        {
            // Reset typewriter state for clean interruption
            DeferredResetTypewriterState();
            
            // Update display to show interruption state
            DeferredUpdateInterruptedDisplay();
        }

        // Handle broadcast state changes for UI updates
        private void HandleBroadcastStateChanged(BroadcastStateChangedEvent @event)
        {
            GD.Print($"LiveShowPanel: Received BroadcastStateChangedEvent - {@event.PreviousState} -> {@event.NewState}");
            
            if (@event.NewState == AsyncBroadcastState.AdBreak)
            {
                CallDeferred("DeferredHandleStateChangedToAdBreak");
            }
            else if (@event.NewState == AsyncBroadcastState.WaitingForT0)
            {
                CallDeferred("DeferredHandleStateChangedToWaitingForT0");
            }
        }

        private void DeferredHandleStateChangedToAdBreak()
        {
            // Reset typewriter state for clean transition
            DeferredResetTypewriterState();
            
            // Update display to show break state
            DeferredUpdateInterruptedDisplay();
        }

        private void DeferredHandleStateChangedToWaitingForT0()
        {
            // Reset typewriter state for clean transition
            DeferredResetTypewriterState();
            
            // Update display to show waiting for T0 state
            DeferredUpdateWaitingForT0Display();
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

        private void DeferredUpdateWaitingDisplay()
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

        private void DeferredUpdateInterruptedDisplay()
        {
            if (_speakerIcon == null || _speakerName == null || _phaseLabel == null)
            {
                return;
            }

            _speakerIcon.Text = "INTERRUPTED";
            _speakerName.Text = "Broadcast interrupted...";
            _phaseLabel.Text = string.Empty;
            _dialogueLabel?.Clear();
            _progressBar?.Hide();
        }

        private void DeferredUpdateWaitingForT0Display()
        {
            if (_speakerIcon == null || _speakerName == null || _phaseLabel == null)
            {
                return;
            }

            _speakerIcon.Text = "BREAK";
            _speakerName.Text = "Waiting for break...";
            _phaseLabel.Text = string.Empty;
            _dialogueLabel?.Clear();
            _progressBar?.Hide();
        }

        private void DeferredUpdateItemDisplay(BroadcastItem item)
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
            else if (item.Type == BroadcastItemType.Conversation)
            {
                _speakerIcon.Text = "ON AIR";
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
            DeferredResetTypewriterState();
            
            if (_progressBar != null)
            {
                _progressBar.Show();
            }
        }

        private void DeferredResetTypewriterState()
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
                BroadcastItemType.Conversation => "ON AIR",
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
