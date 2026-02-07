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

    private BroadcastItem? _currentBroadcastItem;
    private GameStateManager? _gameStateManager;

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
            // Dependencies resolved in OnResolved()
        }

        /// <summary>
        /// Called when all dependencies are resolved.
        /// </summary>
        public void OnResolved()
        {
            // Get dependencies via DI
            var eventBus = DependencyInjection.Get<EventBus>(this);
            _gameStateManager = DependencyInjection.Get<GameStateManager>(this);

            // Subscribe to events
            eventBus.Subscribe<BroadcastEvent>(HandleBroadcastEvent);
            eventBus.Subscribe<BroadcastItemStartedEvent>(HandleBroadcastItemStarted);
            eventBus.Subscribe<BroadcastStateChangedEvent>(HandleBroadcastStateChanged);
            eventBus.Subscribe<BroadcastInterruptionEvent>(HandleBroadcastInterruption);

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
            GD.Print($"LiveShowPanel: Received BroadcastItemStartedEvent - {@event.Item.Id} ({@event.Item.Type}) - Text: {@event.Item.Text}");

            _currentBroadcastItem = @event.Item;
            var item = @event.Item;

            // Skip displaying internal state transition operations
            if (item.Type == BroadcastItemType.PutOnAir)
            {
                return;
            }

            // Skip displaying ad sequence container items (only show individual ads)
            if (item.Type == BroadcastItemType.Ad && item.Text?.StartsWith("Playing") == true)
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

            GD.Print($"LiveShowPanel: Starting typewriter for '{item.Text}' with duration {_currentLineDuration}s");

            DeferredResetTypewriterState();
            DeferredUpdateItemDisplay(item);
        }



        // Handle broadcast interruptions (breaks, show ending, cursing)
        private void HandleBroadcastInterruption(BroadcastInterruptionEvent @event)
        {
            if (@event.Reason == BroadcastInterruptionReason.CallerCursed)
            {
                // For cursing, just reset typewriter state - cursing delay executable will handle UI
                CallDeferred("DeferredResetTypewriterState");
                GD.Print("LiveShowPanel: Cursing interruption - resetting typewriter state");
            }
            else
            {
                // For other interruptions (breaks, show ending), show interrupted display
                CallDeferred("DeferredHandleBroadcastInterruption");
            }
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
            if (@event.NewState == AsyncBroadcastState.AdBreak)
            {
                CallDeferred("DeferredHandleStateChangedToAdBreak");
            }
        }

        // Handle individual ad events during ad break sequence
        private void HandleAdItemStarted(BroadcastItemStartedEvent @event)
        {
            if (@event.Item.Type == BroadcastItemType.Ad)
            {
                CallDeferred("DeferredHandleAdStarted");
            }
        }

        private void DeferredHandleStateChangedToAdBreak()
        {
            if (_speakerIcon == null || _speakerName == null || _phaseLabel == null)
            {
                return;
            }

            // Reset typewriter state for clean transition
            DeferredResetTypewriterState();
            
            // Update display to show ad break state (not interrupted)
            _speakerIcon.Text = "AD BREAK";
            _speakerName.Text = "Commercial Break";
            _phaseLabel.Text = string.Empty;
            _dialogueLabel?.Clear();
            _progressBar?.Hide();
        }

        private void DeferredHandleAdStarted()
        {
            if (_speakerIcon == null || _speakerName == null || _phaseLabel == null || _dialogueLabel == null)
            {
                return;
            }

            // Use the broadcast item text directly - it now contains sponsor information
            string adText = _currentBroadcastItem?.Text ?? "Commercial Break";

            _speakerIcon.Text = "AD";
            _speakerName.Text = adText;
            _phaseLabel.Text = string.Empty;
            _dialogueLabel?.Clear();
            _progressBar?.Hide();
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

        private string GetVernDisplayName()
        {
            var vernStats = _gameStateManager?.VernStats;
            string mood = vernStats?.CurrentMoodType.ToString().ToUpper() ?? "NEUTRAL";
            return $"VERN ({mood})";
        }

        private void DeferredUpdateItemDisplay(BroadcastItem item)
        {
            if (_speakerIcon == null || _speakerName == null || _phaseLabel == null)
            {
                return;
            }

            // Hide speaker name and phase label for minimal display (except for ads which need sponsor info)
            _speakerName.Text = "";
            _phaseLabel.Text = "";

            // Set speaker icon based on content type
            if (item.Type == BroadcastItemType.Ad)
            {
                _speakerIcon.Text = "AD BREAK";
                // For ads, show the sponsor information in speaker name
                _speakerName.Text = item.Text;
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
                _speakerIcon.Text = GetVernDisplayName();
            }
            else if (item.Type == BroadcastItemType.CallerLine)
            {
                _speakerIcon.Text = "CALLER";
            }
            else if (item.Type == BroadcastItemType.DeadAir)
            {
                _speakerIcon.Text = GetVernDisplayName();
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

        public override void _Process(double delta)
        {
            // Update typewriter effect for active lines
            if (!string.IsNullOrEmpty(_currentLineText) && _currentLineDuration > 0)
            {
                UpdateTypewriter(delta);
                UpdateProgressBar();
                _elapsedTime += (float)delta;
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
        }
    }
}
