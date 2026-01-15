using Godot;
using KBTV.Callers;

namespace KBTV.UI
{
    /// <summary>
    /// Footer component for the liveshow UI.
    /// Contains three panels: On-Air caller, Transcript, and Ad Break controls.
    /// </summary>
    public partial class LiveShowFooter : Control
    {
        // On-Air panel
        private Label _callerNameLabel;

        // Transcript panel
        private Label _transcriptText;

        // Ad Break panel
        private Button _startAdBreakButton;
        private Button _endAdBreakButton;
        private Label _adBreakTimerLabel;

        private CallerQueue _callerQueue;

        // Ad break state
        private bool _adBreakActive = false;
        private float _adBreakTime = 0f;

        public override void _Ready()
        {
            // Get UI references
            _callerNameLabel = GetNode<Label>("HBoxContainer/OnAirPanel/OnAirVBox/CallerNameLabel");
            _transcriptText = GetNode<Label>("HBoxContainer/TranscriptPanel/TranscriptVBox/TranscriptScroll/TranscriptText");
            _startAdBreakButton = GetNode<Button>("HBoxContainer/AdBreakPanel/AdBreakVBox/AdBreakControls/StartAdBreakButton");
            _endAdBreakButton = GetNode<Button>("HBoxContainer/AdBreakPanel/AdBreakVBox/AdBreakControls/EndAdBreakButton");
            _adBreakTimerLabel = GetNode<Label>("HBoxContainer/AdBreakPanel/AdBreakVBox/AdBreakControls/AdBreakTimerLabel");

            // Get manager references
            _callerQueue = CallerQueue.Instance;

            // Connect signals
            if (_callerQueue != null)
            {
                _callerQueue.Connect("CallerOnAir", Callable.From<Caller>(OnCallerOnAir));
                _callerQueue.Connect("CallerCompleted", Callable.From<Caller>(OnCallerCompleted));
            }

            // Connect button signals
            _startAdBreakButton.Connect("pressed", Callable.From(OnStartAdBreakPressed));
            _endAdBreakButton.Connect("pressed", Callable.From(OnEndAdBreakPressed));

            // Initial updates
            UpdateOnAirCaller();
            UpdateTranscript();
            UpdateAdBreakControls();
        }

        private void OnCallerOnAir(Caller caller)
        {
            UpdateOnAirCaller();
        }

        private void OnCallerCompleted(Caller caller)
        {
            UpdateOnAirCaller();
        }

        private void UpdateOnAirCaller()
        {
            if (_callerQueue != null && _callerNameLabel != null)
            {
                var caller = _callerQueue.OnAirCaller;
                _callerNameLabel.Text = caller != null ? caller.Name : "No caller";
            }
        }

        private void UpdateTranscript()
        {
            // TODO: Connect to actual conversation system
            // For now, just show placeholder
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

        public override void _Process(double delta)
        {
            if (_adBreakActive)
            {
                _adBreakTime += (float)delta;
                UpdateAdBreakTimer();
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
            // Clean up signal connections
            if (_callerQueue != null)
            {
                _callerQueue.Disconnect("CallerOnAir", Callable.From<Caller>(OnCallerOnAir));
                _callerQueue.Disconnect("CallerCompleted", Callable.From<Caller>(OnCallerCompleted));
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