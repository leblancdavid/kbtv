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

            _startAdBreakButton.Connect("pressed", Callable.From(OnStartAdBreakPressed));
            _endAdBreakButton.Connect("pressed", Callable.From(OnEndAdBreakPressed));

            TrackStateForRefresh();
            UpdateOnAirCaller();
            UpdateAdBreakControls();

            _transcriptText.Text = "TRANSCRIPT";
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

            UpdateTranscript();

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
            if (_transcriptText == null)
            {
                return;
            }

            var transcriptRepository = Core.ServiceRegistry.Instance.TranscriptRepository;
            if (transcriptRepository == null)
            {
                _transcriptText.Text = "TRANSCRIPT";
                return;
            }

            var entries = transcriptRepository.GetCurrentShowTranscript();

            if (entries.Count == 0)
            {
                _transcriptText.Text = "TRANSCRIPT";
                return;
            }

            var transcriptLines = new System.Collections.Generic.List<string>();
            foreach (var entry in entries)
            {
                var speaker = entry.Speaker == KBTV.Dialogue.Speaker.Vern ? "VERN" : entry.SpeakerName ?? "CALLER";
                transcriptLines.Add($"{speaker}: {entry.Text}");
            }

            _transcriptText.Text = string.Join("\n", transcriptLines);
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
