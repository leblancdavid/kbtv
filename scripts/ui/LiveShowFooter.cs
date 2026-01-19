using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Dialogue;

namespace KBTV.UI
{
    public partial class LiveShowFooter : Control
    {
        private Label _callerNameLabel = null!;
        private Label _transcriptText = null!;
        private Button _startAdBreakButton = null!;
        private Button _endAdBreakButton = null!;
        private Label _adBreakTimerLabel = null!;

        private ICallerRepository _repository = null!;
        private BroadcastCoordinator _coordinator = null!;
        private ITranscriptRepository _transcriptRepository = null!;

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
            _coordinator = Core.ServiceRegistry.Instance.BroadcastCoordinator;
            _transcriptRepository = Core.ServiceRegistry.Instance.TranscriptRepository;

            _startAdBreakButton.Connect("pressed", Callable.From(OnStartAdBreakPressed));
            _endAdBreakButton.Connect("pressed", Callable.From(OnEndAdBreakPressed));

            if (_transcriptRepository != null)
            {
                _transcriptRepository.EntryAdded += OnTranscriptEntryAdded;
            }

            TrackStateForRefresh();
            UpdateOnAirCaller();
            UpdateAdBreakControls();

            _transcriptText.Text = "TRANSCRIPT";
        }

        private void OnTranscriptEntryAdded(TranscriptEntry entry)
        {
            if (_adBreakActive)
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

        private void OnStartAdBreakPressed()
        {
            _adBreakActive = true;
            _adBreakTime = 0f;
            UpdateAdBreakControls();
        }

        private void OnEndAdBreakPressed()
        {
            _adBreakActive = false;
            _adBreakTime = 0f;
            UpdateAdBreakControls();
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
            if (_transcriptRepository != null)
            {
                _transcriptRepository.EntryAdded -= OnTranscriptEntryAdded;
            }

            if (_startAdBreakButton != null)
            {
                _startAdBreakButton.Disconnect("pressed", Callable.From(OnStartAdBreakPressed));
            }

            if (_endAdBreakButton != null)
            {
                _endAdBreakButton.Disconnect("pressed", Callable.From(OnEndAdBreakPressed));
            }
        }
    }
}
