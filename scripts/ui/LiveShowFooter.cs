using Godot;
using KBTV.Ads;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Dialogue;
using KBTV.Managers;
using KBTV.UI.Themes;

namespace KBTV.UI
{
    public partial class LiveShowFooter : Control, IDependent
    {
        private Label _callerNameLabel = null!;
        private Button _queueAdsButton = null!;
        private Label _breaksRemainingLabel = null!;
        private Control _adBreakPanel = null!;
        private Control _endShowPanel = null!;

        private ICallerRepository _repository = null!;
        private AdManager _adManager = null!;

        public override void _Notification(int what) => this.Notify(what);

        private string _previousOnAirCallerId = string.Empty;
        private bool _lastButtonEnabled = false;

        public override void _Ready()
        {
            _callerNameLabel = GetNode<Label>("HBoxContainer/OnAirPanel/OnAirVBox/CallerNameLabel");
            _queueAdsButton = GetNode<Button>("HBoxContainer/AdBreakPanel/AdBreakVBox/AdBreakControls/QueueAdsButton");
            _breaksRemainingLabel = GetNode<Label>("HBoxContainer/AdBreakPanel/AdBreakVBox/BreaksRemainingLabel");
            _adBreakPanel = GetNode<Control>("HBoxContainer/AdBreakPanel");
            _endShowPanel = GetNode<Control>("HBoxContainer/EndShowPanel");

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

            // Set up AdManager events
            SetupAdManagerEvents();

            // Connect button signal
            if (_queueAdsButton != null)
            {
                _queueAdsButton.Connect("pressed", Callable.From(OnQueueAdsPressed));
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
                float nextBreakTimeQueued = _adManager.GetNextBreakTime();
                if (nextBreakTimeQueued > 0)
                {
                    float currentTime = DependencyInjection.Get<TimeManager>(this)?.ElapsedTime ?? 0f;
                    float countdown = Mathf.Max(0, nextBreakTimeQueued - currentTime);
                    int minutes = Mathf.FloorToInt(countdown / 60);
                    int seconds = Mathf.FloorToInt(countdown % 60);
                    return $"QUEUED {minutes}:{seconds:D2}";
                }
            }

            if (_adManager.IsInBreakWindow)
            {
                float nextBreakTimeWindow = _adManager.GetNextBreakTime();
                if (nextBreakTimeWindow > 0)
                {
                    float currentTime = DependencyInjection.Get<TimeManager>(this)?.ElapsedTime ?? 0f;
                    float countdown = Mathf.Max(0, nextBreakTimeWindow - currentTime);
                    int minutes = Mathf.FloorToInt(countdown / 60);
                    int seconds = Mathf.FloorToInt(countdown % 60);
                    return $"BREAK IN {minutes}:{seconds:D2}";
                }
                return "BREAK NOW";
            }

            // Outside window: show countdown to next break
            float nextBreakTimeOutside = _adManager.GetNextBreakTime();
            if (nextBreakTimeOutside > 0)
            {
                float currentTime = DependencyInjection.Get<TimeManager>(this)?.ElapsedTime ?? 0f;
                float countdown = Mathf.Max(0, nextBreakTimeOutside - currentTime);
                int minutes = Mathf.FloorToInt(countdown / 60);
                int seconds = Mathf.FloorToInt(countdown % 60);
                return $"BREAK IN {minutes}:{seconds:D2}";
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
                if (_breaksRemainingLabel != null)
                {
                    _breaksRemainingLabel.Text = "Breaks: 0";
                }
                return;
            }

            string buttonText = GetQueueButtonText();
            bool buttonEnabled = _adManager.IsQueueButtonEnabled();

            if (buttonEnabled != _lastButtonEnabled)
            {
                _lastButtonEnabled = buttonEnabled;
            }

            if (_queueAdsButton != null)
            {
                _queueAdsButton.Text = buttonText;
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
