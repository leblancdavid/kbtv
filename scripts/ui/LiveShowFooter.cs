using Godot;
using KBTV.Callers;
using KBTV.Core;

namespace KBTV.UI
{
    /// <summary>
    /// Footer component for the liveshow UI.
    /// Contains three panels: On-Air caller, Transcript, and Ad Break controls.
    /// </summary>
    public partial class LiveShowFooter : Control
    {
        private Label _callerNameLabel = null!;
        private Label _transcriptText = null!;
        private Button _startAdBreakButton = null!;
        private Button _endAdBreakButton = null!;
        private Label _adBreakTimerLabel = null!;

        private ICallerRepository _repository = null!;

        private bool _adBreakActive = false;
        private float _adBreakTime = 0f;

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
            _startAdBreakButton = GetNode<Button>("HBoxContainer/AdBreakPanel/AdBreakVBox/AdBreakControls/StartAdBreakButton");
            _endAdBreakButton = GetNode<Button>("HBoxContainer/AdBreakPanel/AdBreakVBox/AdBreakControls/EndAdBreakButton");
            _adBreakTimerLabel = GetNode<Label>("HBoxContainer/AdBreakPanel/AdBreakVBox/AdBreakControls/AdBreakTimerLabel");

            _repository = Core.ServiceRegistry.Instance.CallerRepository;

            _startAdBreakButton.Connect("pressed", Callable.From(OnStartAdBreakPressed));
            _endAdBreakButton.Connect("pressed", Callable.From(OnEndAdBreakPressed));

            TrackStateForRefresh();
            UpdateOnAirCaller();
            UpdateTranscript();
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

            if (_adBreakActive)
            {
                _adBreakTime += (float)delta;
                UpdateAdBreakTimer();
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

        private void UpdateTranscript()
        {
            if (_transcriptText != null)
            {
                _transcriptText.Text = "[Transcript will appear here when conversation system is active]";
            }
        }

        private void OnStartAdBreakPressed()
        {
            _adBreakActive = true;
            _adBreakTime = 0f;
            UpdateAdBreakControls();
            GD.Print("Ad break started");
        }

        private void OnEndAdBreakPressed()
        {
            _adBreakActive = false;
            _adBreakTime = 0f;
            UpdateAdBreakControls();
            GD.Print("Ad break ended");
        }

        private void UpdateAdBreakControls()
        {
            if (_startAdBreakButton != null)
            {
                _startAdBreakButton.Disabled = _adBreakActive;
            }

            if (_endAdBreakButton != null)
            {
                _endAdBreakButton.Disabled = !_adBreakActive;
            }
        }

        private void UpdateAdBreakTimer()
        {
            if (_adBreakTimerLabel != null)
            {
                int minutes = (int)(_adBreakTime / 60f);
                int seconds = (int)(_adBreakTime % 60f);
                _adBreakTimerLabel.Text = $"{minutes:00}:{seconds:00}";
            }
        }

        public override void _ExitTree()
        {
            if (_startAdBreakButton != null)
            {
                _startAdBreakButton.Disconnect("pressed", Callable.From(OnStartAdBreakPressed));
            }

            if (_endAdBreakButton != null)
            {
                _endAdBreakButton.Disconnect("pressed", Callable.From(OnEndAdBreakPressed));
            }

            GD.Print("LiveShowFooter: Cleanup complete");
        }
    }
}
