#nullable enable

using System;
using System.Text;
using Godot;
using KBTV.Core;
using KBTV.Dialogue;

namespace KBTV.UI
{
    /// <summary>
    /// Main conversation display panel for the LiveShow phase.
    /// Displays current dialogue with typewriter effect and transcript history.
    /// Uses direct property access + polling pattern for UI updates.
    /// </summary>
    public partial class LiveShowPanel : Control
    {
        [Export] private Label? _speakerIcon;
        [Export] private Label? _speakerName;
        [Export] private Label? _phaseLabel;
        [Export] private RichTextLabel? _dialogueLabel;
        [Export] private ProgressBar? _progressBar;
        [Export] private RichTextLabel? _transcriptContent;

        private IConversationManager _conversationManager = null!;
        private ITranscriptRepository _transcriptRepository = null!;

        private ConversationDisplayInfo _previousDisplay = new();
        private int _previousTranscriptCount = -1;

        private string _displayedText = string.Empty;
        private float _typewriterSpeed = 50f;
        private int _typewriterIndex = 0;
        private float _typewriterAccumulator = 0f;

        public override void _Ready()
        {
            CallDeferred(nameof(InitializeDeferred));
        }

        private void InitializeDeferred()
        {
            if (!ServiceRegistry.IsInitialized)
            {
                GD.PrintErr("LiveShowPanel: ServiceRegistry not initialized, retrying...");
                CallDeferred(nameof(InitializeDeferred));
                return;
            }

            _conversationManager = ServiceRegistry.Instance.ConversationManager;
            _transcriptRepository = ServiceRegistry.Instance.TranscriptRepository;

            _speakerIcon = GetNode<Label>("%SpeakerIcon");
            _speakerName = GetNode<Label>("%SpeakerName");
            _phaseLabel = GetNode<Label>("%PhaseLabel");
            _dialogueLabel = GetNode<RichTextLabel>("%DialogueContainer/DialogueLabel");
            _progressBar = GetNode<ProgressBar>("%ProgressBar");
            _transcriptContent = GetNode<RichTextLabel>("%TranscriptScroll/TranscriptContent");

            UpdateDisplay(_conversationManager.DisplayInfo);
            UpdateTranscript();

            GD.Print("LiveShowPanel: Initialized");
        }

        public override void _Process(double delta)
        {
            if (_conversationManager == null || _transcriptRepository == null)
            {
                return;
            }

            var display = _conversationManager.DisplayInfo;

            if (display.HasChanged(_previousDisplay))
            {
                UpdateDisplay(display);
                _previousDisplay = display.Copy();
            }

            if (display.IsTyping)
            {
                UpdateTypewriter(delta, display);
            }

            if (_transcriptRepository.EntryCount != _previousTranscriptCount)
            {
                UpdateTranscript();
                _previousTranscriptCount = _transcriptRepository.EntryCount;
            }
        }

        private void UpdateDisplay(ConversationDisplayInfo display)
        {
            if (_speakerIcon == null || _speakerName == null || _phaseLabel == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(display.SpeakerName))
            {
                _speakerIcon.Text = string.Empty;
                _speakerName.Text = "Waiting for broadcast...";
                _phaseLabel.Text = string.Empty;
                _dialogueLabel?.Clear();
                _progressBar?.Hide();
                return;
            }

            _speakerIcon.Text = display.SpeakerIcon;
            _speakerName.Text = display.SpeakerName;

            _phaseLabel.Text = display.FlowState.GetDisplayName();

            switch (display.FlowState)
            {
                case BroadcastFlowState.DeadAirFiller:
                    _phaseLabel.Modulate = new Color(0.5f, 0.5f, 0.5f);
                    break;
                case BroadcastFlowState.Conversation:
                    _phaseLabel.Modulate = new Color(0.5f, 0.8f, 0.5f);
                    break;
                case BroadcastFlowState.ShowOpening:
                case BroadcastFlowState.BetweenCallers:
                case BroadcastFlowState.ShowClosing:
                    _phaseLabel.Modulate = new Color(0.9f, 0.7f, 0.3f);
                    break;
                default:
                    _phaseLabel.Modulate = new Color(1, 1, 1);
                    break;
            }

            _displayedText = display.Text;
            _typewriterIndex = 0;
            _typewriterAccumulator = 0f;

            if (_dialogueLabel != null)
            {
                _dialogueLabel.Clear();
            }

            if (_progressBar != null)
            {
                _progressBar.Show();
                _progressBar.MaxValue = display.CurrentLineDuration > 0 ? display.CurrentLineDuration : 1f;
                _progressBar.Value = display.ElapsedLineTime;
            }
        }

        private void UpdateTypewriter(double delta, ConversationDisplayInfo display)
        {
            if (_dialogueLabel == null || !display.IsTyping)
            {
                return;
            }

            _typewriterAccumulator += (float)delta * _typewriterSpeed;

            while (_typewriterAccumulator >= 1f && _typewriterIndex < _displayedText.Length)
            {
                var ch = _displayedText[_typewriterIndex];
                _dialogueLabel.Text += ch;
                _typewriterAccumulator -= 1f;
                _typewriterIndex++;
            }

            if (_progressBar != null)
            {
                _progressBar.Value = display.ElapsedLineTime + (float)delta;
            }
        }

        private void UpdateTranscript()
        {
            if (_transcriptContent == null || _transcriptRepository == null)
            {
                return;
            }

            var transcript = _transcriptRepository.GetCurrentShowTranscript();
            if (transcript.Count == 0)
            {
                _transcriptContent.Text = "[Transcript will appear here during the broadcast]";
                return;
            }

            var sb = new StringBuilder();
            const int maxEntries = 50;

            int startIndex = Math.Max(0, transcript.Count - maxEntries);
            for (int i = startIndex; i < transcript.Count; i++)
            {
                var entry = transcript[i];
                var speakerLabel = entry.SpeakerName ?? (entry.Speaker == Dialogue.Speaker.Vern ? "Vern" : "Caller");
                sb.AppendLine($"[{entry.Timestamp:F1}s] [b]{speakerLabel}[/b]: {entry.Text}");
            }

            _transcriptContent.Text = sb.ToString();
            var vscroll = _transcriptContent.GetVScrollBar();
            if (vscroll != null)
            {
                vscroll.Value = vscroll.MaxValue;
            }
        }

        public override void _ExitTree()
        {
            GD.Print("LiveShowPanel: Cleanup complete");
        }
    }
}
