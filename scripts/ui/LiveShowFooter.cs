using Godot;
using KBTV.Ads;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Data;
using KBTV.Dialogue;
using KBTV.Economy;
using KBTV.Managers;
using KBTV.UI.Themes;

namespace KBTV.UI
{
    public partial class LiveShowFooter : Control, IDependent
    {
        private Label _callerNameLabel = null!;
        private Button _queueAdsButton = null!;
        private Button _dropCallerButton = null!;
        private Label _queueAdsTimerLabel = null!;
        private Label _dropTimerLabel = null!;
        private Label _adBreakTimerLabel = null!;
        private Control _adBreakPanel = null!;
        private Control _endShowPanel = null!;

        private ICallerRepository _repository = null!;
        private AdManager _adManager = null!;
        private AsyncBroadcastLoop _asyncBroadcastLoop = null!;
        private EventBus _eventBus = null!;
        private EconomyManager _economyManager = null!;

        // Cursing timer fields
        private bool _isCursingTimerActive = false;
        private float _cursingTimeRemaining = 0f;
        private const float CURSING_TIMER_DURATION = 20f;
        private bool _callerDroppedDueToCursing = false;

        private float _previousAdBreakSeconds = -1f;

        public override void _Notification(int what) => this.Notify(what);

        private string _previousOnAirCallerId = string.Empty;
        private bool _lastButtonEnabled = false;

        public override void _Ready()
        {
            _callerNameLabel = GetNode<Label>("HBoxContainer/OnAirPanel/OnAirVBox/OnAirTop/CallerNameLabel");
            _queueAdsButton = GetNode<Button>("HBoxContainer/AdBreakPanel/AdBreakVBox/AdBreakCenter/AdBreakControls/QueueAdsButton");
            _dropCallerButton = GetNode<Button>("HBoxContainer/OnAirPanel/OnAirVBox/OnAirCenter/DropControls/DropCallerButton");
            _queueAdsTimerLabel = GetNode<Label>("HBoxContainer/AdBreakPanel/AdBreakVBox/AdBreakCenter/AdBreakControls/QueueAdsTimerLabel");
            _dropTimerLabel = GetNode<Label>("HBoxContainer/OnAirPanel/OnAirVBox/OnAirCenter/DropControls/DropTimerLabel");
            _adBreakTimerLabel = GetNode<Label>("HBoxContainer/AdBreakPanel/AdBreakVBox/AdBreakTop/AdBreakTimerLabel");
            _adBreakPanel = GetNode<Control>("HBoxContainer/AdBreakPanel");
            _endShowPanel = GetNode<Control>("HBoxContainer/EndShowPanel");

            if (_queueAdsButton != null)
            {
                _queueAdsButton.Text = "QUEUE";
            }

            if (_dropCallerButton != null)
            {
                _dropCallerButton.Text = "DROP";
            }

            // Dependencies resolved in OnResolved()
        }

        /// <summary>
        /// Called when all dependencies are resolved.
        /// </summary>
        public void OnResolved()
        {
            // Get dependencies via DI
            _repository = DependencyInjection.Get<ICallerRepository>(this);
            _adManager = DependencyInjection.Get<AdManager>(this);
            _asyncBroadcastLoop = DependencyInjection.Get<AsyncBroadcastLoop>(this);
            _eventBus = DependencyInjection.Get<EventBus>(this);

            // Subscribe to broadcast interruption events for cursing
            _eventBus.Subscribe<BroadcastInterruptionEvent>(OnBroadcastInterruption);

            // Set up AdManager events
            SetupAdManagerEvents();

            // Connect button signal
            if (_queueAdsButton != null)
            {
                _queueAdsButton.Connect("pressed", Callable.From(OnQueueAdsPressed));
            }
            if (_dropCallerButton != null)
            {
                _dropCallerButton.Connect("pressed", Callable.From(OnDropCallerPressed));
            }
        }

        private void SetupAdManagerEvents()
        {
            if (_adManager != null)
            {
                if (_adManager.IsInitialized)
                {
                    // Handle immediately if already initialized
                    OnAdManagerInitialized();
                }
                else
                {
                    // Subscribe for future initialization
                    _adManager.OnInitialized += OnAdManagerInitialized;
                }
                _adManager.OnBreakWindowOpened += OnBreakWindowOpened;
                _adManager.OnBreakQueued += OnBreakQueued;
                _adManager.OnBreakStarted += OnBreakStarted;
                _adManager.OnBreakEnded += OnBreakEnded;
                _adManager.OnShowEnded += OnShowEnded;
            }

            TrackStateForRefresh();
            UpdateOnAirCaller();
            UpdateAdBreakControls();
        }


        private void TrackStateForRefresh()
        {
            _previousOnAirCallerId = _repository.OnAirCaller?.Id;
        }

        public override void _Process(double delta)
        {
            var currentOnAirCallerId = _repository.OnAirCaller?.Id;
            if (currentOnAirCallerId != _previousOnAirCallerId)
            {
                UpdateOnAirCaller();
                _previousOnAirCallerId = currentOnAirCallerId;
            }

            // Update ad break controls (handles countdown display and button states)
            UpdateAdBreakControls();
            UpdateAdBreakTimer();
            
            // Update cursing timer if active
            UpdateCursingTimer(delta);
            
            // Update drop caller button styling
            UpdateDropCallerButton();
        }

        private void UpdateOnAirCaller()
        {
            if (_callerNameLabel != null)
            {
                var caller = _repository.OnAirCaller;
                _callerNameLabel.Text = caller != null ? caller.Name : "No caller";
            }

            // Update drop button state
            if (_dropCallerButton != null)
            {
                _dropCallerButton.Disabled = _repository?.OnAirCaller == null;
            }

            if (_dropTimerLabel != null && _repository?.OnAirCaller == null)
            {
                _dropTimerLabel.Text = "--:--";
            }
        }

        private void OnDropCallerPressed()
        {
            if (_repository?.OnAirCaller != null && _asyncBroadcastLoop != null)
            {
                var callerId = _repository.OnAirCaller.Id;
                Log.Debug($"LiveShowFooter: Dropping caller {_repository.OnAirCaller.Name}");
                _asyncBroadcastLoop.InterruptBroadcast(BroadcastInterruptionReason.CallerDropped, callerId);

                // If cursing timer was active, stop it (successful drop)
                if (_isCursingTimerActive)
                {
                    StopCursingTimer();
                }
            }
        }

        private void OnQueueAdsPressed()
        {
            if (_adManager != null)
            {
                _adManager.QueueBreak();
            }
        }

        private void OnBreakWindowOpened(float timeUntilBreak)
        {
            UpdateAdBreakControls();
        }

        private void OnBreakQueued()
        {
            UpdateAdBreakControls();
        }

        private void OnBreakStarted()
        {
            UpdateAdBreakControls();
        }

        private void OnBreakEnded(float revenue)
        {
            UpdateAdBreakControls();

            // Check if no more breaks remaining, hide ad break panel and show end show panel
            if (_adManager != null && _adManager.BreaksRemaining == 0)
            {
                if (_adBreakPanel != null) _adBreakPanel.Visible = false;
                if (_endShowPanel != null) _endShowPanel.Visible = true;
            }
        }

        private void OnShowEnded()
        {
            UpdateAdBreakControls();
        }

        private void OnBroadcastInterruption(BroadcastInterruptionEvent interruptionEvent)
        {
            if (interruptionEvent.Reason == BroadcastInterruptionReason.CallerCursed)
            {
                _callerDroppedDueToCursing = true;
                StartCursingTimer();
            }
        }

        private void UpdateCursingTimer(double delta)
        {
            if (!_isCursingTimerActive) return;

            _cursingTimeRemaining -= (float)delta;

            if (_cursingTimeRemaining <= 0)
            {
                // Timer expired - apply penalties
                OnCursingTimerExpired();
                return;
            }

            // Update button text with countdown
            if (_dropTimerLabel != null)
            {
                int seconds = Mathf.CeilToInt(_cursingTimeRemaining);
                _dropTimerLabel.Text = $"DROP {seconds}";
            }
        }

        private void OnCursingTimerExpired()
        {
            _isCursingTimerActive = false;
            _cursingTimeRemaining = 0;

            Log.Debug("LiveShowFooter: Cursing timer expired - applying penalties");

            // Apply penalties: Vern stat penalties and FCC fine
            var gameStateManager = DependencyInjection.Get<GameStateManager>(this);
            var vernStats = gameStateManager?.VernStats;
            var economyManager = DependencyInjection.Get<EconomyManager>(this);

            if (vernStats != null)
            {
                vernStats.ApplyCursingPenalty();
                Log.Debug("LiveShowFooter: Applied Vern cursing penalties");
            }
            else
            {
                Log.Error("LiveShowFooter: Could not access VernStats for cursing penalties");
            }

            if (economyManager != null)
            {
                economyManager.ApplyFCCFine(100); // $100 fine
                Log.Debug("LiveShowFooter: Applied $100 FCC fine");
            }

            // Automatically drop the caller - but only if not already dropped due to cursing
            // (the cursing flow already handles dropping the caller)
            if (_callerDroppedDueToCursing)
            {
                Log.Debug("LiveShowFooter: Caller already dropped due to cursing, skipping extra drop");
                _callerDroppedDueToCursing = false; // Reset flag
            }
            else if (_repository?.OnAirCaller != null && _asyncBroadcastLoop != null)
            {
                var callerId = _repository.OnAirCaller.Id;
                Log.Debug($"LiveShowFooter: Automatically dropping cursing caller {_repository.OnAirCaller.Name}");
                _asyncBroadcastLoop.InterruptBroadcast(BroadcastInterruptionReason.CallerDropped, callerId);
            }

            if (_dropTimerLabel != null)
            {
                _dropTimerLabel.Text = "--:--";
            }

            // Publish timer completion event (unsuccessful - penalties applied)
            var eventBus = DependencyInjection.Get<EventBus>(this);
            eventBus?.Publish(new CursingTimerCompletedEvent(wasSuccessful: false));
        }

        private void StartCursingTimer()
        {
            _isCursingTimerActive = true;
            _cursingTimeRemaining = CURSING_TIMER_DURATION;
        }

        private void StopCursingTimer()
        {
            _isCursingTimerActive = false;
            _cursingTimeRemaining = 0;

            if (_dropTimerLabel != null)
            {
                _dropTimerLabel.Text = "--:--";
            }

            // Publish timer completion event (successful - no penalties)
            var eventBus = DependencyInjection.Get<EventBus>(this);
            eventBus?.Publish(new CursingTimerCompletedEvent(wasSuccessful: true));
        }

        private void UpdateDropCallerButton()
        {
            if (_dropCallerButton == null) return;

            bool hasCaller = _repository?.OnAirCaller != null;
            
            if (_dropCallerButton != null)
            {
                // Apply dynamic styling based on caller state and cursing timer
                var styleBoxNormal = new StyleBoxFlat();
                var styleBoxDisabled = new StyleBoxFlat();
                var styleBoxPressed = new StyleBoxFlat();

                Color bgColor, borderColor;
                if (_isCursingTimerActive)
                {
                    // Flashing red during cursing timer (every 0.5 seconds)
                    float flashTime = Time.GetTicksMsec() / 1000.0f;
                    bool isRedPhase = Mathf.Floor(flashTime * 2) % 2 == 0; // Flash every 0.5 seconds
                    
                    bgColor = isRedPhase ? UIColors.Accent.Red : UIColors.BG_PANEL;
                    borderColor = UIColors.Accent.Red;
                }
                else if (hasCaller)
                {
                    // Red color for drop action when caller is available
                    bgColor = UIColors.Accent.Red;
                    borderColor = UIColors.Accent.Red;
                }
                else
                {
                    // Reset to default style when no caller
                    _dropCallerButton.RemoveThemeStyleboxOverride("normal");
                    _dropCallerButton.RemoveThemeStyleboxOverride("disabled");
                    _dropCallerButton.RemoveThemeStyleboxOverride("pressed");
                    return;
                }

                // Normal state: full brightness
                styleBoxNormal.BgColor = bgColor;
                styleBoxNormal.BorderColor = borderColor;
                styleBoxNormal.BorderWidthTop = 2;
                styleBoxNormal.BorderWidthBottom = 2;
                styleBoxNormal.BorderWidthLeft = 2;
                styleBoxNormal.BorderWidthRight = 2;

                // Disabled state: dimmed
                styleBoxDisabled.BgColor = new Color(bgColor.R, bgColor.G, bgColor.B, 0.6f);
                styleBoxDisabled.BorderColor = new Color(borderColor.R, borderColor.G, borderColor.B, 0.6f);
                styleBoxDisabled.BorderWidthTop = 2;
                styleBoxDisabled.BorderWidthBottom = 2;
                styleBoxDisabled.BorderWidthLeft = 2;
                styleBoxDisabled.BorderWidthRight = 2;

                // Pressed state: full brightness
                styleBoxPressed.BgColor = bgColor;
                styleBoxPressed.BorderColor = borderColor;
                styleBoxPressed.BorderWidthTop = 2;
                styleBoxPressed.BorderWidthBottom = 2;
                styleBoxPressed.BorderWidthLeft = 2;
                styleBoxPressed.BorderWidthRight = 2;

                _dropCallerButton.AddThemeStyleboxOverride("normal", styleBoxNormal);
                _dropCallerButton.AddThemeStyleboxOverride("disabled", styleBoxDisabled);
                _dropCallerButton.AddThemeStyleboxOverride("pressed", styleBoxPressed);
            }
        }



        private string GetQueueButtonText()
        {
            return "A";
        }

        private void UpdateAdBreakControls()
        {
            if (_adManager == null || !_adManager.IsInitialized)
            {
                if (_queueAdsButton != null)
                {
                    _queueAdsButton.Disabled = true;
                    _queueAdsButton.Text = "QUEUE";
                }
                if (_queueAdsTimerLabel != null)
                {
                    _queueAdsTimerLabel.Text = "--:--";
                }
                if (_adBreakTimerLabel != null)
                {
                    _adBreakTimerLabel.Text = "Next break: --:--";
                }
                return;
            }

            bool buttonEnabled = _adManager.IsQueueButtonEnabled();

            if (buttonEnabled != _lastButtonEnabled)
            {
                _lastButtonEnabled = buttonEnabled;
            }

                if (_queueAdsButton != null)
                {
                    _queueAdsButton.Text = "QUEUE";
                    _queueAdsButton.Disabled = !buttonEnabled;
                    _queueAdsButton.Visible = true;

                // Apply dynamic styling based on ad break state
                var styleBoxNormal = new StyleBoxFlat();
                var styleBoxDisabled = new StyleBoxFlat();
                var styleBoxPressed = new StyleBoxFlat();

                Color bgColor, borderColor;
                if (_adManager.IsAdBreakActive)
                {
                    bgColor = UIColors.Accent.Red;
                    borderColor = UIColors.Accent.Red;
                }
                else if (_adManager.IsQueued)
                {
                    bgColor = UIColors.Accent.Gold;
                    borderColor = UIColors.Accent.Gold;
                }
                else if (_adManager.IsInBreakWindow)
                {
                    bgColor = UIColors.Accent.Green;
                    borderColor = UIColors.Accent.Green;
                }
                else
                {
                    // Reset to default style
                    _queueAdsButton.RemoveThemeStyleboxOverride("normal");
                    _queueAdsButton.RemoveThemeStyleboxOverride("disabled");
                    _queueAdsButton.RemoveThemeStyleboxOverride("pressed");
                    return;
                }

                // Normal state: full brightness
                styleBoxNormal.BgColor = bgColor;
                styleBoxNormal.BorderColor = borderColor;
                styleBoxNormal.BorderWidthTop = 2;
                styleBoxNormal.BorderWidthBottom = 2;
                styleBoxNormal.BorderWidthLeft = 2;
                styleBoxNormal.BorderWidthRight = 2;

                // Disabled state: dimmed
                styleBoxDisabled.BgColor = new Color(bgColor.R, bgColor.G, bgColor.B, 0.6f);
                styleBoxDisabled.BorderColor = new Color(borderColor.R, borderColor.G, borderColor.B, 0.6f);
                styleBoxDisabled.BorderWidthTop = 2;
                styleBoxDisabled.BorderWidthBottom = 2;
                styleBoxDisabled.BorderWidthLeft = 2;
                styleBoxDisabled.BorderWidthRight = 2;

                // Pressed state: full brightness
                styleBoxPressed.BgColor = bgColor;
                styleBoxPressed.BorderColor = borderColor;
                styleBoxPressed.BorderWidthTop = 2;
                styleBoxPressed.BorderWidthBottom = 2;
                styleBoxPressed.BorderWidthLeft = 2;
                styleBoxPressed.BorderWidthRight = 2;

                _queueAdsButton.AddThemeStyleboxOverride("normal", styleBoxNormal);
                _queueAdsButton.AddThemeStyleboxOverride("disabled", styleBoxDisabled);
                _queueAdsButton.AddThemeStyleboxOverride("pressed", styleBoxPressed);
                if (_queueAdsTimerLabel != null)
                {
                    if (_adManager.IsQueued && _adManager.QueuedCountdown > 0f)
                    {
                        int seconds = Mathf.CeilToInt(_adManager.QueuedCountdown);
                        _queueAdsTimerLabel.Text = $"QUEUED {FormatSeconds(seconds)}";
                    }
                    else if (_adManager.IsInBreakWindow && _adManager.TimeUntilNextBreak > 0f)
                    {
                        int seconds = Mathf.CeilToInt(_adManager.TimeUntilNextBreak);
                        _queueAdsTimerLabel.Text = $"BREAK IN {FormatSeconds(seconds)}";
                    }
                    else if (_adManager.TimeUntilNextBreak > 0f)
                    {
                        int seconds = Mathf.CeilToInt(_adManager.TimeUntilNextBreak);
                        _queueAdsTimerLabel.Text = $"BREAK IN {FormatSeconds(seconds)}";
                    }
                    else
                    {
                        _queueAdsTimerLabel.Text = "--:--";
                    }
                }
            }

        }

        private void UpdateAdBreakTimer()
        {
            if (_adBreakTimerLabel == null)
            {
                return;
            }

            if (_adManager == null || !_adManager.IsInitialized)
            {
                _adBreakTimerLabel.Text = "Next break: --:--";
                _previousAdBreakSeconds = -1f;
                return;
            }

            float secondsUntilBreak = 0f;

            if (_adManager.IsAdBreakActive)
            {
                if (_adManager.QueuedCountdown > 0f)
                {
                    int seconds = Mathf.CeilToInt(_adManager.QueuedCountdown);
                    _adBreakTimerLabel.Text = $"Break: {FormatSeconds(seconds)}";
                }
                else
                {
                    _adBreakTimerLabel.Text = "Break: LIVE";
                }
                _previousAdBreakSeconds = -1f;
                return;
            }

            if (_adManager.BreaksRemaining <= 0)
            {
                _adBreakTimerLabel.Text = "Next break: --:--";
                _previousAdBreakSeconds = -1f;
                return;
            }

            if (_adManager.IsQueued && _adManager.QueuedCountdown > 0f)
            {
                secondsUntilBreak = _adManager.QueuedCountdown;
            }
            else if (_adManager.TimeUntilNextBreak > 0f)
            {
                secondsUntilBreak = _adManager.TimeUntilNextBreak;
            }

            int roundedSeconds = Mathf.CeilToInt(secondsUntilBreak);
            if (roundedSeconds == Mathf.CeilToInt(_previousAdBreakSeconds))
            {
                return;
            }

            _previousAdBreakSeconds = secondsUntilBreak;
            _adBreakTimerLabel.Text = $"Next break: {FormatSeconds(roundedSeconds)}";
        }

        private static string FormatSeconds(int totalSeconds)
        {
            if (totalSeconds < 0)
            {
                return "--:--";
            }

            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes}:{seconds:D2}";
        }

        private void OnAdManagerInitialized()
        {
            if (_adManager != null && _adManager.BreaksRemaining == 0)
            {
                if (_adBreakPanel != null) _adBreakPanel.Visible = false;
                if (_endShowPanel != null) _endShowPanel.Visible = true;
            }
        }

        public override void _ExitTree()
        {
            if (_adManager != null)
            {
                _adManager.OnInitialized -= OnAdManagerInitialized;
                _adManager.OnBreakWindowOpened -= OnBreakWindowOpened;
                _adManager.OnBreakQueued -= OnBreakQueued;
                _adManager.OnBreakStarted -= OnBreakStarted;
                _adManager.OnBreakEnded -= OnBreakEnded;
                _adManager.OnShowEnded -= OnShowEnded;
            }

            // Unsubscribe from broadcast events
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<BroadcastInterruptionEvent>(OnBroadcastInterruption);
            }

            if (_queueAdsButton != null)
            {
                _queueAdsButton.Disconnect("pressed", Callable.From(OnQueueAdsPressed));
            }
            if (_dropCallerButton != null)
            {
                _dropCallerButton.Disconnect("pressed", Callable.From(OnDropCallerPressed));
            }
        }
    }
}
