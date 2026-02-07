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
        
        // BBCode handling for red text
        private string _fullTextWithBBCode = string.Empty;
        private string _plainText = string.Empty;
        private bool _isUsingBBCode = false;
        
        // Audio-synced typewriter state
        private float _currentLineDuration = 0f;
        private float _elapsedTime = 0f;
        
        // Separate typewriter duration for quick reveals
        private float _typewriterDuration = 0f;

        public override void _EnterTree()
        {
            // Dependencies will be resolved in OnResolved()
        }

        public override void _Ready()
        {
            // Dependencies resolved in OnResolved()
            
            // Add notification timer for temporary system messages
            var notificationTimer = new Timer { OneShot = true, WaitTime = 2.0f };
            AddChild(notificationTimer);
            notificationTimer.Timeout += () => DeferredResetToWaitingDisplay();
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



            DeferredResetTypewriterState();
            DeferredUpdateItemDisplay(item);
        }



        // Handle broadcast interruptions (breaks, show ending, cursing)
        private void HandleBroadcastInterruption(BroadcastInterruptionEvent @event)
        {
            // No longer handling cursing interruptions here - moved to CursingDelayExecutable
            if (@event.Reason != BroadcastInterruptionReason.CallerCursed)
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

        private void DeferredResetToWaitingDisplay()
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

        private void DeferredDisplaySystemNotification(string message)
        {
            if (_speakerIcon == null || _speakerName == null || _phaseLabel == null || _dialogueLabel == null)
            {
                return;
            }

            // Clear any ongoing typewriter effect
            DeferredResetTypewriterState();
            
            // Display system notification
            _speakerIcon.Text = "SYSTEM";
            _speakerName.Text = "";
            _phaseLabel.Text = "";
            _dialogueLabel.Text = message;
            _progressBar?.Hide();
            
            // Start auto-hide timer
            var timer = GetNode<Timer>("Timer");
            timer.Start();
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

            // Check for FCC violation flag
            bool isFccViolation = item.Metadata != null && 
                                item.Metadata.GetType().GetProperty("IsFccViolation")?.GetValue(item.Metadata) as bool? == true;

            // Set durations - FCC violations reveal quickly but stay visible for full penalty
            if (isFccViolation)
            {
                _typewriterDuration = 2.0f; // Quick 2-second reveal
                _currentLineDuration = 20.0f; // Full 20-second penalty duration
            }
            else
            {
                _typewriterDuration = _currentLineDuration; // Normal behavior
            }

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
            else if (item.Type == BroadcastItemType.CursingDelay)
            {
                _speakerIcon.Text = isFccViolation ? "SYSTEM" : "PENALTY";
            }
            else
            {
                _speakerIcon.Text = "SYSTEM"; // Fallback for transitions, etc.
            }

            // Apply red styling for FCC violations
            if (_dialogueLabel is RichTextLabel richLabel)
            {
                richLabel.BbcodeEnabled = true;
                if (isFccViolation)
                {
                    _fullTextWithBBCode = $"[color=red]{item.Text}[/color]";
                    _plainText = item.Text;
                    _isUsingBBCode = true;
                }
                else
                {
                    _fullTextWithBBCode = item.Text;
                    _plainText = item.Text;
                    _isUsingBBCode = false;
                }
            }
            else
            {
                _fullTextWithBBCode = item.Text;
                _plainText = item.Text;
                _isUsingBBCode = false;
            }

            // Set the text to display (will be overridden by typewriter if BBCode)
            _currentLineText = _plainText;

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
            if (_dialogueLabel == null)
            {
                return;
            }

            // Handle BBCode text with alpha transparency reveal
            if (_isUsingBBCode && _dialogueLabel is RichTextLabel richLabel)
            {
                // Use typewriter duration for reveal speed (defaults to current line duration)
                float typewriterProgress = Mathf.Min(_elapsedTime / _typewriterDuration, 1.0f);
                int targetIndex = (int)(typewriterProgress * _plainText.Length);
                
                // Create text with visible part in red, hidden part transparent
                string visibleText = _plainText.Substring(0, Mathf.Min(targetIndex, _plainText.Length));
                string hiddenText = _plainText.Substring(Mathf.Min(targetIndex, _plainText.Length));
                
                string formattedText = $"[color=red]{visibleText}[/color]";
                if (!string.IsNullOrEmpty(hiddenText))
                {
                    formattedText += $"[color=#ff000000]{hiddenText}[/color]"; // Transparent red for hidden text
                }
                
                richLabel.Text = formattedText;
            }
            else
            {
                // Original logic for non-BBCode text
                if (string.IsNullOrEmpty(_displayedText))
                {
                    return;
                }

                // Use typewriter duration for reveal speed
                float typewriterProgress = Mathf.Min(_elapsedTime / _typewriterDuration, 1.0f);
                int targetIndex = (int)(typewriterProgress * _displayedText.Length);
                
                // Reveal characters up to target position
                string revealedText = _displayedText.Substring(0, Mathf.Min(targetIndex, _displayedText.Length));
                _dialogueLabel.Text = revealedText;
            }
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
