using Godot;
using KBTV.Ads;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Dialogue;
using KBTV.Managers;

namespace KBTV.UI
{
    public partial class LiveShowFooter : Control, IDependent
    {
        private Label _callerNameLabel = null!;
        private Button _queueAdsButton = null!;
        private Label _adBreakStatusLabel = null!;
        private Label _breaksRemainingLabel = null!;
        private Control _adBreakPanel = null!;
        private Control _endShowPanel = null!;

        private ICallerRepository _repository = null!;
        private AdManager _adManager = null!;
        private BroadcastCoordinator _coordinator = null!;

        public override void _Notification(int what) => this.Notify(what);

        private string _previousOnAirCallerId = string.Empty;

        public override void _Ready()
        {
            _callerNameLabel = GetNode<Label>("HBoxContainer/OnAirPanel/OnAirVBox/CallerNameLabel");
            _queueAdsButton = GetNode<Button>("HBoxContainer/AdBreakPanel/AdBreakVBox/AdBreakControls/QueueAdsButton");
            _adBreakStatusLabel = GetNode<Label>("HBoxContainer/AdBreakPanel/AdBreakVBox/AdBreakStatusLabel");
            _breaksRemainingLabel = GetNode<Label>("HBoxContainer/AdBreakPanel/AdBreakVBox/BreaksRemainingLabel");
            _adBreakPanel = GetNode<Control>("HBoxContainer/AdBreakPanel");
            _endShowPanel = GetNode<Control>("HBoxContainer/EndShowPanel");

            GD.Print("LiveShowFooter: Ready, waiting for dependencies...");
            // Dependencies resolved in OnResolved()
        }

        /// <summary>
        /// Called when all dependencies are resolved.
        /// </summary>
        public void OnResolved()
        {
            GD.Print("LiveShowFooter: Dependencies resolved, initializing...");

            // Get dependencies via DI
            _repository = DependencyInjection.Get<ICallerRepository>(this);
            _adManager = DependencyInjection.Get<AdManager>(this);
            _coordinator = DependencyInjection.Get<BroadcastCoordinator>(this);

            // Set up AdManager events
            SetupAdManagerEvents();
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
        }

        private void UpdateOnAirCaller()
        {
            if (_callerNameLabel != null)
            {
                var caller = _repository.OnAirCaller;
                _callerNameLabel.Text = caller != null ? caller.Name : "No caller";
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



        private string GetQueueButtonText()
        {
            if (_adManager == null || !_adManager.IsInitialized)
            {
                return "N/A";
            }

            if (!_adManager.IsActive) return "NO BREAKS";
            if (_adManager.IsAdBreakActive) return "ON BREAK";

            if (_adManager.IsQueued)
            {
                float nextBreakTime = _adManager.GetNextBreakTime();
                if (nextBreakTime > 0)
                {
                    float currentTime = DependencyInjection.Get<TimeManager>(this)?.ElapsedTime ?? 0f;
                    float countdown = Mathf.Max(0, nextBreakTime - currentTime);
                    return $"QUEUED {countdown:F1}";
                }
            }

            if (_adManager.IsInBreakWindow) return "QUEUE AD-BREAK";

            float timeUntilWindow = _adManager.TimeUntilBreakWindow;
            if (timeUntilWindow > 0)
            {
                int seconds = (int)timeUntilWindow;
                return $"BREAK IN {seconds / 60}:{seconds % 60:D2}";
            }

            return "BREAK SOON";
        }

        private void UpdateAdBreakControls()
        {
            if (_adManager == null || !_adManager.IsInitialized)
            {
                if (_queueAdsButton != null)
                {
                    _queueAdsButton.Disabled = true;
                    _queueAdsButton.Text = "N/A";
                }
                if (_adBreakStatusLabel != null)
                {
                    _adBreakStatusLabel.Text = "NOT READY";
                    _adBreakStatusLabel.AddThemeColorOverride("font_color", UITheme.TEXT_SECONDARY);
                }
                if (_breaksRemainingLabel != null)
                {
                    _breaksRemainingLabel.Text = "Breaks: 0";
                }
                return;
            }

            string buttonText = GetQueueButtonText();
            bool buttonEnabled = _adManager.IsQueueButtonEnabled();

            if (_queueAdsButton != null)
            {
                _queueAdsButton.Text = buttonText;
                _queueAdsButton.Disabled = !buttonEnabled;
                _queueAdsButton.Visible = _adManager.IsInBreakWindow || _adManager.IsAdBreakActive;
            }

            if (_adBreakStatusLabel != null)
            {
                if (_adManager.IsAdBreakActive)
                {
                    var sponsor = _coordinator?.CurrentAdSponsor;
                    if (!string.IsNullOrEmpty(sponsor))
                    {
                        _adBreakStatusLabel.Text = $"ON BREAK: {sponsor}";
                    }
                    else
                    {
                        _adBreakStatusLabel.Text = "ON BREAK";
                    }
                    _adBreakStatusLabel.AddThemeColorOverride("font_color", UITheme.ACCENT_RED);
                }
                else if (_adManager.IsInBreakWindow)
                {
                    _adBreakStatusLabel.Text = "WINDOW OPEN";
                    _adBreakStatusLabel.AddThemeColorOverride("font_color", UITheme.ACCENT_GREEN);
                }
                else if (_adManager.IsActive)
                {
                    // Calculate time until break window opens (UI concern, not system state)
                    float nextBreakTime = _adManager.GetNextBreakTime();
                    if (nextBreakTime > 0)
                    {
                        float currentTime = DependencyInjection.Get<TimeManager>(this)?.ElapsedTime ?? 0f;
                        float timeUntilWindow = Mathf.Max(0, nextBreakTime - AdConstants.BREAK_WINDOW_DURATION - currentTime);

                        int seconds = (int)timeUntilWindow;
                        if (seconds > 0)
                        {
                            _adBreakStatusLabel.Text = $"IN {seconds / 60}:{seconds % 60:D2}";
                        }
                        else
                        {
                            _adBreakStatusLabel.Text = "WINDOW OPEN";
                        }
                        _adBreakStatusLabel.AddThemeColorOverride("font_color", UITheme.TEXT_SECONDARY);
                    }
                    else
                    {
                        _adBreakStatusLabel.Text = "BREAK SOON";
                        _adBreakStatusLabel.AddThemeColorOverride("font_color", UITheme.TEXT_SECONDARY);
                    }
                }
                else
                {
                    _adBreakStatusLabel.Text = "NO MORE";
                    _adBreakStatusLabel.AddThemeColorOverride("font_color", UITheme.TEXT_SECONDARY);
                }
            }

            if (_breaksRemainingLabel != null)
            {
                int remaining = _adManager.BreaksRemaining;
                _breaksRemainingLabel.Text = $"Breaks: {remaining}";
            }
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

            if (_queueAdsButton != null)
            {
                _queueAdsButton.Disconnect("pressed", Callable.From(OnQueueAdsPressed));
            }
        }
    }
}
