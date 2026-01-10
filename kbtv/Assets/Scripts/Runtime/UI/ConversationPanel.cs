using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KBTV.Dialogue;

namespace KBTV.UI
{
    /// <summary>
    /// Panel that displays the conversation between Vern and the on-air caller.
    /// Shows the current speaker, dialogue text, and conversation progress.
    /// </summary>
    public class ConversationPanel : BasePanel
    {
        private GameObject _conversationContainer;
        private GameObject _emptyState;
        private TextMeshProUGUI _speakerLabel;
        private TextMeshProUGUI _dialogueText;
        private Image _speakerIcon;
        private Image _progressFill;
        private TextMeshProUGUI _phaseLabel;

        private ConversationManager _conversationManager;

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
            TextMeshProUGUI headerText = UITheme.CreateText("Header", headerRow.transform, "CONVERSATION",
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

            // Conversation container (shown when conversation is active)
            _conversationContainer = new GameObject("ConversationContainer");
            _conversationContainer.transform.SetParent(transform, false);
            UITheme.AddVerticalLayout(_conversationContainer, padding: 5f, spacing: 10f);
            UITheme.AddLayoutElement(_conversationContainer, flexibleHeight: 1f);

            // Speaker row
            GameObject speakerRow = new GameObject("SpeakerRow");
            speakerRow.transform.SetParent(_conversationContainer.transform, false);
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

            // Dialogue text container with padding
            GameObject dialogueContainer = UITheme.CreatePanel("DialogueContainer", _conversationContainer.transform, 
                new Color(0.12f, 0.12f, 0.12f));
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
            progressRow.transform.SetParent(_conversationContainer.transform, false);
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
            _conversationContainer.SetActive(false);
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
            }
        }

        protected override void Update()
        {
            base.Update(); // Handles subscription retry

            // Update progress bar
            if (_conversationManager != null && _conversationManager.IsPlaying)
            {
                _progressFill.fillAmount = _conversationManager.LineProgress;
            }
        }

        protected override void UpdateDisplay()
        {
            if (_conversationManager == null) return;

            bool hasConversation = _conversationManager.HasActiveConversation;

            _conversationContainer.SetActive(hasConversation);
            _emptyState.SetActive(!hasConversation);

            if (hasConversation && _conversationManager.CurrentLine != null)
            {
                DisplayLine(_conversationManager.CurrentLine);
                UpdatePhaseLabel(_conversationManager.CurrentConversation.CurrentPhase);
            }
        }

        private void OnConversationStarted(Conversation conversation)
        {
            UpdateDisplay();
        }

        private void OnConversationEnded(Conversation conversation)
        {
            UpdateDisplay();
        }

        private void OnLineDisplayed(DialogueLine line)
        {
            DisplayLine(line);
        }

        private void OnPhaseChanged(ConversationPhase phase)
        {
            UpdatePhaseLabel(phase);
        }

        private void DisplayLine(DialogueLine line)
        {
            if (line == null) return;

            // Update speaker display
            bool isVern = line.Speaker == Speaker.Vern;
            string speakerName = isVern ? "VERN" : GetCallerName();

            _speakerLabel.text = speakerName;
            _speakerLabel.color = isVern ? VernSpeakerColor : CallerSpeakerColor;
            _speakerIcon.color = isVern ? VernIconColor : CallerIconColor;

            // Update dialogue text with tone-based styling
            _dialogueText.text = line.Text;
            _dialogueText.color = GetTextColorForTone(line.Tone);
            _dialogueText.fontStyle = GetFontStyleForTone(line.Tone);

            // Reset progress
            _progressFill.fillAmount = 0f;

            // Ensure container is visible
            _conversationContainer.SetActive(true);
            _emptyState.SetActive(false);
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
    }
}
