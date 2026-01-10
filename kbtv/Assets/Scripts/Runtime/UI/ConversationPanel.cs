using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KBTV.Dialogue;
using KBTV.Audio;

namespace KBTV.UI
{
    /// <summary>
    /// Panel that displays the conversation between Vern and the on-air caller.
    /// Shows the current speaker, dialogue text, and conversation progress.
    /// </summary>
    public class ConversationPanel : BasePanel
    {
        private GameObject _transcriptContainer;
        private GameObject _emptyState;
        private TextMeshProUGUI _speakerLabel;
        private TextMeshProUGUI _dialogueText;
        private Image _speakerIcon;
        private Image _progressFill;
        private TextMeshProUGUI _phaseLabel;

        private ConversationManager _conversationManager;

        // Typewriter effect state
        private string _fullText = "";
        private int _visibleCharCount = 0;
        private bool _isTyping = false;
        private float _typewriterTimer = 0f;
        private const float CHARS_PER_SECOND = 40f; // Typing speed

        // Dialogue history
        private TextMeshProUGUI _historyText;
        private const int MAX_HISTORY_LINES = 3;
        private System.Collections.Generic.List<string> _historyLines = new System.Collections.Generic.List<string>();
        private DialogueLine _previousLine = null; // Track previous line for history

        // Audio settings
        private int _lastBlipCharCount = 0;
        private const int CHARS_PER_BLIP = 3; // Play blip every N characters

        // Thinking indicator
        private TextMeshProUGUI _thinkingIndicator;
        private float _thinkingTimer = 0f;
        private bool _isShowingThinking = false;
        private const float THINKING_DURATION = 0.6f; // How long to show thinking dots
        private const float DOT_ANIMATION_SPEED = 4f; // Dots per second

        // Colors for different speakers
        private static readonly Color VernSpeakerColor = UITheme.TextPrimary;      // Green for Vern
        private static readonly Color CallerSpeakerColor = UITheme.TextAmber;       // Amber for callers
        private static readonly Color VernIconColor = new Color(0.2f, 0.6f, 0.2f);  // Dark green
        private static readonly Color CallerIconColor = new Color(0.6f, 0.4f, 0.1f); // Dark amber

        /// <summary>
        /// Create and initialize a ConversationPanel on the given parent.
        /// </summary>
        public static ConversationPanel Create(Transform parent)
        {
            GameObject panelObj = UITheme.CreatePanel("ConversationPanel", parent, UITheme.PanelBackground);

            UITheme.AddVerticalLayout(panelObj, padding: UITheme.PanelPadding, spacing: 8f);

            ConversationPanel panel = panelObj.AddComponent<ConversationPanel>();
            panel.BuildUI();

            return panel;
        }

        private void BuildUI()
        {
            // Header row
            GameObject headerRow = new GameObject("HeaderRow");
            headerRow.transform.SetParent(transform, false);
            UITheme.AddHorizontalLayout(headerRow, spacing: 10f);
            UITheme.AddLayoutElement(headerRow, preferredHeight: 25f);

            // Header text
            TextMeshProUGUI headerText = UITheme.CreateText("Header", headerRow.transform, "TRANSCRIPT",
                UITheme.FontSizeLarge, UITheme.TextPrimary, TextAlignmentOptions.Left);
            headerText.fontStyle = FontStyles.Bold;
            UITheme.AddLayoutElement(headerText.gameObject, minWidth: 120f);

            // Spacer
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(headerRow.transform, false);
            spacer.AddComponent<RectTransform>();
            UITheme.AddLayoutElement(spacer, flexibleWidth: 1f);

            // Phase label
            _phaseLabel = UITheme.CreateText("PhaseLabel", headerRow.transform, "",
                UITheme.FontSizeSmall, UITheme.TextGray, TextAlignmentOptions.Right);
            UITheme.AddLayoutElement(_phaseLabel.gameObject, minWidth: 80f);

            // Divider
            UITheme.CreateDivider(transform);

            // Transcript container (shown when conversation or filler is active)
            _transcriptContainer = new GameObject("TranscriptContainer");
            _transcriptContainer.transform.SetParent(transform, false);
            UITheme.AddVerticalLayout(_transcriptContainer, padding: 5f, spacing: 6f);
            UITheme.AddLayoutElement(_transcriptContainer, flexibleHeight: 1f);

            // History text (shows previous lines)
            _historyText = UITheme.CreateText("HistoryText", _transcriptContainer.transform,
                "", UITheme.FontSizeSmall, UITheme.TextDim, TextAlignmentOptions.Left);
            _historyText.textWrappingMode = TextWrappingModes.Normal;
            _historyText.overflowMode = TextOverflowModes.Truncate;
            _historyText.fontStyle = FontStyles.Italic;
            UITheme.AddLayoutElement(_historyText.gameObject, preferredHeight: 45f, flexibleHeight: 0f);

            // Speaker row
            GameObject speakerRow = new GameObject("SpeakerRow");
            speakerRow.transform.SetParent(_transcriptContainer.transform, false);
            UITheme.AddHorizontalLayout(speakerRow, spacing: 8f);
            UITheme.AddLayoutElement(speakerRow, preferredHeight: 30f);

            // Speaker icon (colored square)
            GameObject iconObj = new GameObject("SpeakerIcon");
            iconObj.transform.SetParent(speakerRow.transform, false);
            _speakerIcon = iconObj.AddComponent<Image>();
            _speakerIcon.color = VernIconColor;
            UITheme.AddLayoutElement(iconObj, preferredWidth: 24f, preferredHeight: 24f);

            // Speaker name
            _speakerLabel = UITheme.CreateText("SpeakerLabel", speakerRow.transform, "VERN",
                UITheme.FontSizeNormal, VernSpeakerColor, TextAlignmentOptions.Left);
            _speakerLabel.fontStyle = FontStyles.Bold;
            UITheme.AddLayoutElement(_speakerLabel.gameObject, flexibleWidth: 1f);
            
            // Thinking indicator (shows "..." animation between speakers)
            _thinkingIndicator = UITheme.CreateText("ThinkingIndicator", speakerRow.transform, "",
                UITheme.FontSizeNormal, UITheme.TextGray, TextAlignmentOptions.Right);
            _thinkingIndicator.fontStyle = FontStyles.Bold;
            UITheme.AddLayoutElement(_thinkingIndicator.gameObject, minWidth: 30f);
            _thinkingIndicator.gameObject.SetActive(false);

            // Dialogue text container with padding
            GameObject dialogueContainer = UITheme.CreatePanel("DialogueContainer", _transcriptContainer.transform, 
                UITheme.DialogueContainerBackground);
            UITheme.AddVerticalLayout(dialogueContainer, padding: 12f, spacing: 0f);
            UITheme.AddLayoutElement(dialogueContainer, flexibleHeight: 1f, minHeight: 60f);

            // Dialogue text
            _dialogueText = UITheme.CreateText("DialogueText", dialogueContainer.transform, 
                "Waiting for conversation...",
                UITheme.FontSizeNormal, UITheme.TextWhite, TextAlignmentOptions.Left);
            _dialogueText.textWrappingMode = TextWrappingModes.Normal;
            _dialogueText.overflowMode = TextOverflowModes.Ellipsis;
            UITheme.AddLayoutElement(_dialogueText.gameObject, flexibleHeight: 1f);

            // Progress bar
            GameObject progressRow = new GameObject("ProgressRow");
            progressRow.transform.SetParent(_transcriptContainer.transform, false);
            UITheme.AddHorizontalLayout(progressRow, spacing: 8f);
            UITheme.AddLayoutElement(progressRow, preferredHeight: 8f);

            var (progressBg, progressFill) = UITheme.CreateProgressBar("ProgressBar", progressRow.transform, UITheme.AccentCyan);
            _progressFill = progressFill;
            _progressFill.fillAmount = 0f;
            UITheme.AddLayoutElement(progressBg.gameObject, flexibleWidth: 1f, preferredHeight: 6f);

            // Empty state (shown when no conversation)
            _emptyState = new GameObject("EmptyState");
            _emptyState.transform.SetParent(transform, false);
            _emptyState.AddComponent<RectTransform>();
            UITheme.AddVerticalLayout(_emptyState, padding: 20f, spacing: 10f, childForceExpand: true);
            UITheme.AddLayoutElement(_emptyState, flexibleHeight: 1f);

            // Empty message
            TextMeshProUGUI emptyMsg = UITheme.CreateText("EmptyMsg", _emptyState.transform,
                "No active conversation\nPut a caller on air to start",
                UITheme.FontSizeNormal, UITheme.TextGray, TextAlignmentOptions.Center);
            emptyMsg.raycastTarget = false;
            UITheme.AddLayoutElement(emptyMsg.gameObject, flexibleHeight: 1f);

            // Initially show empty state
            _transcriptContainer.SetActive(false);
            _emptyState.SetActive(true);
        }

        protected override bool DoSubscribe()
        {
            _conversationManager = ConversationManager.Instance;
            if (_conversationManager == null) return false;

            _conversationManager.OnConversationStarted += OnConversationStarted;
            _conversationManager.OnConversationEnded += OnConversationEnded;
            _conversationManager.OnLineDisplayed += OnLineDisplayed;
            _conversationManager.OnPhaseChanged += OnPhaseChanged;
            _conversationManager.OnFillerLineDisplayed += OnFillerLineDisplayed;
            _conversationManager.OnFillerStopped += OnFillerStopped;
            _conversationManager.OnBroadcastLineDisplayed += OnBroadcastLineDisplayed;
            _conversationManager.OnBroadcastLineCompleted += OnBroadcastLineCompleted;
            return true;
        }

        protected override void DoUnsubscribe()
        {
            if (_conversationManager != null)
            {
                _conversationManager.OnConversationStarted -= OnConversationStarted;
                _conversationManager.OnConversationEnded -= OnConversationEnded;
                _conversationManager.OnLineDisplayed -= OnLineDisplayed;
                _conversationManager.OnPhaseChanged -= OnPhaseChanged;
                _conversationManager.OnFillerLineDisplayed -= OnFillerLineDisplayed;
                _conversationManager.OnFillerStopped -= OnFillerStopped;
                _conversationManager.OnBroadcastLineDisplayed -= OnBroadcastLineDisplayed;
                _conversationManager.OnBroadcastLineCompleted -= OnBroadcastLineCompleted;
            }
        }

        protected override void Update()
        {
            base.Update(); // Handles subscription retry

            // Update progress bar
            if (_conversationManager != null)
            {
                if (_conversationManager.IsPlaying)
                {
                    _progressFill.fillAmount = _conversationManager.LineProgress;
                }
                else if (_conversationManager.IsPlayingBroadcastLine)
                {
                    _progressFill.fillAmount = _conversationManager.BroadcastLineProgress;
                }
                else if (_conversationManager.IsPlayingDeadAirFiller)
                {
                    _progressFill.fillAmount = _conversationManager.FillerLineProgress;
                }
            }

            // Update typewriter effect
            if (_isTyping && _visibleCharCount < _fullText.Length)
            {
                _typewriterTimer += Time.deltaTime * CHARS_PER_SECOND;
                int charsToShow = Mathf.FloorToInt(_typewriterTimer);
                
                if (charsToShow > _visibleCharCount)
                {
                    int oldCount = _visibleCharCount;
                    _visibleCharCount = Mathf.Min(charsToShow, _fullText.Length);
                    _dialogueText.maxVisibleCharacters = _visibleCharCount;
                    
                    // Play dialogue blip every CHARS_PER_BLIP characters
                    int blipsTrigger = _visibleCharCount / CHARS_PER_BLIP;
                    int lastBlipsTrigger = oldCount / CHARS_PER_BLIP;
                    if (blipsTrigger > lastBlipsTrigger)
                    {
                        AudioManager.Instance?.PlayDialogueBlip();
                    }
                    
                    if (_visibleCharCount >= _fullText.Length)
                    {
                        _isTyping = false;
                    }
                }
            }
            
            // Update thinking indicator animation
            if (_isShowingThinking)
            {
                _thinkingTimer += Time.deltaTime;
                
                // Animate dots (1 to 3 dots cycling)
                int dotCount = 1 + (int)((_thinkingTimer * DOT_ANIMATION_SPEED) % 3);
                _thinkingIndicator.text = new string('.', dotCount);
                
                // Hide after duration
                if (_thinkingTimer >= THINKING_DURATION)
                {
                    HideThinkingIndicator();
                }
            }
        }

        protected override void UpdateDisplay()
        {
            if (_conversationManager == null) return;

            bool hasConversation = _conversationManager.HasActiveConversation;
            bool hasFillerPlaying = _conversationManager.IsPlayingDeadAirFiller;
            bool hasBroadcastLine = _conversationManager.IsPlayingBroadcastLine;

            _transcriptContainer.SetActive(hasConversation || hasFillerPlaying || hasBroadcastLine);
            _emptyState.SetActive(!hasConversation && !hasFillerPlaying && !hasBroadcastLine);

            if (hasConversation && _conversationManager.CurrentLine != null)
            {
                DisplayLine(_conversationManager.CurrentLine);
                UpdatePhaseLabel(_conversationManager.CurrentConversation.CurrentPhase);
            }
            else if (hasBroadcastLine && _conversationManager.CurrentBroadcastLine != null)
            {
                DisplayLine(_conversationManager.CurrentBroadcastLine);
                _phaseLabel.text = "ON AIR";
            }
            else if (hasFillerPlaying && _conversationManager.CurrentFillerLine != null)
            {
                DisplayLine(_conversationManager.CurrentFillerLine);
                _phaseLabel.text = "ON AIR";
            }
        }

        private void OnConversationStarted(Conversation conversation)
        {
            ClearHistory();
            UpdateDisplay();
        }

        private void OnConversationEnded(Conversation conversation)
        {
            ResetTypewriter();
            ClearHistory();
            HideThinkingIndicator();
            UpdateDisplay();
        }

        private void OnLineDisplayed(DialogueLine line)
        {
            DisplayLine(line);
        }

        /// <summary>
        /// Skip the typewriter effect and show full text immediately.
        /// </summary>
        public void SkipTypewriter()
        {
            if (_isTyping)
            {
                _isTyping = false;
                _visibleCharCount = _fullText.Length;
                _dialogueText.maxVisibleCharacters = _visibleCharCount;
            }
        }

        private void OnPhaseChanged(ConversationPhase phase)
        {
            UpdatePhaseLabel(phase);
        }

        private void OnFillerLineDisplayed(DialogueLine line)
        {
            DisplayFillerOrBroadcastLine(line);
        }

        private void OnFillerStopped()
        {
            UpdateDisplay();
        }

        private void OnBroadcastLineDisplayed(DialogueLine line)
        {
            DisplayFillerOrBroadcastLine(line);
        }

        private void OnBroadcastLineCompleted()
        {
            UpdateDisplay();
        }

        private void DisplayLine(DialogueLine line)
        {
            if (line == null) return;

            // Check if speaker changed for audio cue
            bool speakerChanged = _previousLine != null && _previousLine.Speaker != line.Speaker;

            // Add previous line to history before showing new one
            if (_previousLine != null)
            {
                string speakerName = _previousLine.Speaker == Speaker.Vern ? "VERN" : GetCallerName();
                string historyEntry = $"{speakerName}: {TruncateForHistory(_previousLine.Text)}";
                _historyLines.Add(historyEntry);
                
                // Keep only the last MAX_HISTORY_LINES
                while (_historyLines.Count > MAX_HISTORY_LINES)
                {
                    _historyLines.RemoveAt(0);
                }
                
                // Update history display
                if (_historyText != null)
                {
                    _historyText.text = string.Join("\n", _historyLines);
                }
            }
            _previousLine = line;
            
            // Play speaker change sound and show thinking indicator
            if (speakerChanged)
            {
                AudioManager.Instance?.PlaySpeakerChange();
                ShowThinkingIndicator();
            }

            // Update speaker display
            bool isVern = line.Speaker == Speaker.Vern;
            string speakerNameCurrent = isVern ? "VERN" : GetCallerName();

            _speakerLabel.text = speakerNameCurrent;
            _speakerLabel.color = isVern ? VernSpeakerColor : CallerSpeakerColor;
            _speakerIcon.color = isVern ? VernIconColor : CallerIconColor;

            // Update dialogue text with tone-based styling
            _fullText = line.Text;
            _dialogueText.text = _fullText;
            _dialogueText.color = GetTextColorForTone(line.Tone);
            _dialogueText.fontStyle = GetFontStyleForTone(line.Tone);

            // Start typewriter effect
            _visibleCharCount = 0;
            _typewriterTimer = 0f;
            _isTyping = true;
            _dialogueText.maxVisibleCharacters = 0;

            // Reset progress
            _progressFill.fillAmount = 0f;

            // Ensure transcript container is visible
            _transcriptContainer.SetActive(true);
            _emptyState.SetActive(false);
        }

        /// <summary>
        /// Truncate text for history display (max ~50 chars with ellipsis).
        /// </summary>
        private string TruncateForHistory(string text)
        {
            const int MAX_LENGTH = 50;
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= MAX_LENGTH) return text;
            return text.Substring(0, MAX_LENGTH - 3) + "...";
        }

        private void UpdatePhaseLabel(ConversationPhase phase)
        {
            string phaseText = phase switch
            {
                ConversationPhase.Intro => "INTRO",
                ConversationPhase.Probe => "DETAILS",
                ConversationPhase.Challenge => "CHALLENGE",
                ConversationPhase.Resolution => "WRAP-UP",
                _ => ""
            };

            _phaseLabel.text = phaseText;
        }

        private string GetCallerName()
        {
            // During filler, there's no caller
            if (_conversationManager?.IsPlayingDeadAirFiller == true)
            {
                return ""; // Not used during filler since Vern is only speaker
            }

            if (_conversationManager?.CurrentConversation?.Caller != null)
            {
                return _conversationManager.CurrentConversation.Caller.Name.ToUpperInvariant();
            }
            return "CALLER";
        }

        private Color GetTextColorForTone(DialogueTone tone)
        {
            return tone switch
            {
                DialogueTone.Excited => UITheme.AccentYellow,
                DialogueTone.Scared => UITheme.AccentRed,
                DialogueTone.Skeptical => UITheme.TextGray,
                DialogueTone.Dismissive => UITheme.TextDim,
                DialogueTone.Believing => UITheme.AccentGreen,
                DialogueTone.Conspiratorial => UITheme.AccentCyan,
                DialogueTone.Dramatic => UITheme.AccentYellow,
                DialogueTone.Annoyed => UITheme.AccentRed,
                DialogueTone.Nervous => UITheme.TextAmber,
                _ => UITheme.TextWhite
            };
        }

        private FontStyles GetFontStyleForTone(DialogueTone tone)
        {
            return tone switch
            {
                DialogueTone.Excited => FontStyles.Bold,
                DialogueTone.Dramatic => FontStyles.Bold | FontStyles.Italic,
                DialogueTone.Scared => FontStyles.Italic,
                DialogueTone.Conspiratorial => FontStyles.Italic,
                _ => FontStyles.Normal
            };
        }

        /// <summary>
        /// Show the thinking indicator (animated dots).
        /// </summary>
        private void ShowThinkingIndicator()
        {
            if (_thinkingIndicator == null) return;
            
            _isShowingThinking = true;
            _thinkingTimer = 0f;
            _thinkingIndicator.text = ".";
            _thinkingIndicator.gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide the thinking indicator.
        /// </summary>
        private void HideThinkingIndicator()
        {
            _isShowingThinking = false;
            _thinkingTimer = 0f;
            if (_thinkingIndicator != null)
            {
                _thinkingIndicator.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Clear the dialogue history display.
        /// </summary>
        private void ClearHistory()
        {
            _historyLines.Clear();
            _previousLine = null;
            if (_historyText != null)
            {
                _historyText.text = "";
            }
        }

        /// <summary>
        /// Reset the typewriter effect state.
        /// </summary>
        private void ResetTypewriter()
        {
            _isTyping = false;
            _fullText = "";
            _visibleCharCount = 0;
            _typewriterTimer = 0f;
        }

        /// <summary>
        /// Display a filler or broadcast line (shared logic).
        /// </summary>
        private void DisplayFillerOrBroadcastLine(DialogueLine line)
        {
            ClearHistory();
            _phaseLabel.text = "ON AIR";
            DisplayLine(line);
        }
    }
}
