using Godot;
using KBTV.Ads;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Dialogue;

namespace KBTV.UI
{
    public partial class LiveShowFooter : Control
    {
        private Label _callerNameLabel = null!;
        private Label _transcriptText = null!;
        private Button _queueAdsButton = null!;
        private Label _adBreakStatusLabel = null!;
        private Label _breaksRemainingLabel = null!;

        private ICallerRepository _repository = null!;
        private ITranscriptRepository _transcriptRepository = null!;
        private AdManager _adManager = null!;

        private string _previousOnAirCallerId = string.Empty;

        public override void _Ready()
        {
            CallDeferred(nameof(InitializeDeferred));
        }

        private void InitializeDeferred()
        {
            if (!Core.ServiceRegistry.IsInitialized)
            {
                GD.PrintErr("LiveShowFooter: ServiceRegistry not initialized, retrying...");
                CallDeferred(nameof(InitializeDeferred));
                return;
            }

            _callerNameLabel = GetNode<Label>("HBoxContainer/OnAirPanel/OnAirVBox/CallerNameLabel");
            _transcriptText = GetNode<Label>("HBoxContainer/TranscriptPanel/TranscriptVBox/TranscriptScroll/TranscriptText");
            _queueAdsButton = GetNode<Button>("HBoxContainer/AdBreakPanel/AdBreakVBox/AdBreakControls/QueueAdsButton");
            _adBreakStatusLabel = GetNode<Label>("HBoxContainer/AdBreakPanel/AdBreakVBox/AdBreakStatusLabel");
            _breaksRemainingLabel = GetNode<Label>("HBoxContainer/AdBreakPanel/AdBreakVBox/BreaksRemainingLabel");

            _repository = Core.ServiceRegistry.Instance.CallerRepository;
            _transcriptRepository = Core.ServiceRegistry.Instance.TranscriptRepository;
            _adManager = Core.ServiceRegistry.Instance.AdManager;

            _queueAdsButton.Connect("pressed", Callable.From(OnQueueAdsPressed));

            if (_transcriptRepository != null)
            {
                _transcriptRepository.EntryAdded += OnTranscriptEntryAdded;
            }

            if (_adManager != null)
            {
                _adManager.OnBreakWindowOpened += OnBreakWindowOpened;
                _adManager.OnBreakQueued += OnBreakQueued;
                _adManager.OnBreakStarted += OnBreakStarted;
                _adManager.OnBreakEnded += OnBreakEnded;
                _adManager.OnShowEnded += OnShowEnded;
            }

            TrackStateForRefresh();
            UpdateOnAirCaller();
            UpdateAdBreakControls();

            _transcriptText.Text = "TRANSCRIPT";
        }

        private void OnTranscriptEntryAdded(TranscriptEntry entry)
        {
            if (_adManager != null && _adManager.IsAdBreakActive)
            {
                return;
            }

            if (_transcriptText == null)
            {
                return;
            }

            var displayText = entry.GetDisplayText();
            _transcriptText.Text = displayText;
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

            if (_adManager != null && _adManager.IsActive)
            {
                UpdateAdBreakControls();
            }
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
        }

        private void OnShowEnded()
        {
            UpdateAdBreakControls();
        }

        private void UpdateAdBreakControls()
        {
            if (_adManager == null)
            {
                if (_queueAdsButton != null)
                {
                    _queueAdsButton.Disabled = true;
                    _queueAdsButton.Text = "N/A";
                }
                return;
            }

            string buttonText = _adManager.GetQueueButtonText();
            bool buttonEnabled = _adManager.IsQueueButtonEnabled();

            if (_queueAdsButton != null)
            {
                _queueAdsButton.Text = buttonText;
                _queueAdsButton.Disabled = !buttonEnabled;
                _queueAdsButton.Visible = _adManager.IsInBreakWindow;
            }

            if (_adBreakStatusLabel != null)
            {
                if (_adManager.IsAdBreakActive)
                {
                    _adBreakStatusLabel.Text = "ON BREAK";
                    _adBreakStatusLabel.AddThemeColorOverride("font_color", UITheme.ACCENT_RED);
                }
                else if (_adManager.IsInBreakWindow)
                {
                    _adBreakStatusLabel.Text = "WINDOW OPEN";
                    _adBreakStatusLabel.AddThemeColorOverride("font_color", UITheme.ACCENT_GREEN);
                }
                else if (_adManager.IsActive)
                {
                    int seconds = (int)_adManager.TimeUntilBreakWindow;
                    if (seconds > 0)
                    {
                        _adBreakStatusLabel.Text = $"IN {seconds / 60}:{seconds % 60:D2}";
                    }
                    else
                    {
                        _adBreakStatusLabel.Text = "BREAK SOON";
                    }
                    _adBreakStatusLabel.AddThemeColorOverride("font_color", UITheme.TEXT_SECONDARY);
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

        public override void _ExitTree()
        {
            if (_transcriptRepository != null)
            {
                _transcriptRepository.EntryAdded -= OnTranscriptEntryAdded;
            }

            if (_adManager != null)
            {
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
