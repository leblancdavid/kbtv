using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KBTV.Callers;

namespace KBTV.UI
{
    /// <summary>
    /// Reusable component to display caller information.
    /// Used in screening panel, on-air display, and queue lists.
    /// </summary>
    public class CallerCardUI : MonoBehaviour
    {
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _phoneText;
        private TextMeshProUGUI _locationText;
        private TextMeshProUGUI _topicText;
        private TextMeshProUGUI _reasonText;
        private TextMeshProUGUI _legitimacyText;
        private Image _patienceBar;
        private TextMeshProUGUI _patienceText;

        private Caller _caller;
        private bool _showDetails;
        private bool _showPatience;

        /// <summary>
        /// Create a caller card with full details (for screening).
        /// </summary>
        public static CallerCardUI CreateDetailed(Transform parent)
        {
            return Create(parent, showDetails: true, showPatience: true);
        }

        /// <summary>
        /// Create a compact caller card (for queue lists).
        /// </summary>
        public static CallerCardUI CreateCompact(Transform parent)
        {
            return Create(parent, showDetails: false, showPatience: true);
        }

        /// <summary>
        /// Create a caller card for on-air display (shows legitimacy, no patience).
        /// </summary>
        public static CallerCardUI CreateOnAir(Transform parent)
        {
            return Create(parent, showDetails: true, showPatience: false);
        }

        private static CallerCardUI Create(Transform parent, bool showDetails, bool showPatience)
        {
            GameObject cardObj = UITheme.CreatePanel("CallerCard", parent, new Color(0.12f, 0.12f, 0.12f));

            UITheme.AddVerticalLayout(cardObj, padding: 8f, spacing: 4f);

            CallerCardUI card = cardObj.AddComponent<CallerCardUI>();
            card._showDetails = showDetails;
            card._showPatience = showPatience;
            card.BuildUI();

            return card;
        }

        private void BuildUI()
        {
            // Name (always shown)
            _nameText = UITheme.CreateText("Name", transform, "Unknown Caller", 
                UITheme.FontSizeLarge, UITheme.TextPrimary, TextAlignmentOptions.Left);
            _nameText.fontStyle = FontStyles.Bold;
            UITheme.AddLayoutElement(_nameText.gameObject, preferredHeight: 22f);

            if (_showDetails)
            {
                // Phone number
                _phoneText = UITheme.CreateText("Phone", transform, "Phone: ---", 
                    UITheme.FontSizeNormal, UITheme.TextWhite, TextAlignmentOptions.Left);
                UITheme.AddLayoutElement(_phoneText.gameObject, preferredHeight: 18f);

                // Location
                _locationText = UITheme.CreateText("Location", transform, "From: ---", 
                    UITheme.FontSizeNormal, UITheme.TextWhite, TextAlignmentOptions.Left);
                UITheme.AddLayoutElement(_locationText.gameObject, preferredHeight: 18f);

                // Topic
                _topicText = UITheme.CreateText("Topic", transform, "Topic: ---", 
                    UITheme.FontSizeNormal, UITheme.TextAmber, TextAlignmentOptions.Left);
                UITheme.AddLayoutElement(_topicText.gameObject, preferredHeight: 18f);

                // Call reason
                _reasonText = UITheme.CreateText("Reason", transform, "Reason: ---", 
                    UITheme.FontSizeSmall, UITheme.TextGray, TextAlignmentOptions.Left);
                _reasonText.textWrappingMode = TextWrappingModes.Normal;
                UITheme.AddLayoutElement(_reasonText.gameObject, preferredHeight: 32f);

                // Legitimacy (only shown for on-air, i.e., when patience is hidden)
                if (!_showPatience)
                {
                    _legitimacyText = UITheme.CreateText("Legitimacy", transform, "Legitimacy: ---", 
                        UITheme.FontSizeNormal, UITheme.TextWhite, TextAlignmentOptions.Left);
                    UITheme.AddLayoutElement(_legitimacyText.gameObject, preferredHeight: 18f);
                }
            }
            else
            {
                // Compact view - just topic
                _topicText = UITheme.CreateText("Topic", transform, "Topic: ---", 
                    UITheme.FontSizeSmall, UITheme.TextGray, TextAlignmentOptions.Left);
                UITheme.AddLayoutElement(_topicText.gameObject, preferredHeight: 16f);
            }

            // Patience bar (if shown)
            if (_showPatience)
            {
                CreatePatienceBar();
            }
        }

        private void CreatePatienceBar()
        {
            GameObject patienceContainer = new GameObject("PatienceContainer");
            patienceContainer.transform.SetParent(transform, false);
            UITheme.AddHorizontalLayout(patienceContainer, spacing: 5f);
            UITheme.AddLayoutElement(patienceContainer, preferredHeight: 16f);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(patienceContainer.transform, false);
            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = "Patience:";
            label.fontSize = UITheme.FontSizeSmall;
            label.color = UITheme.TextGray;
            label.alignment = TextAlignmentOptions.Left;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            UITheme.AddLayoutElement(labelObj, minWidth: 60f);

            // Bar background
            GameObject barBg = new GameObject("BarBg");
            barBg.transform.SetParent(patienceContainer.transform, false);
            Image bgImage = barBg.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f);
            UITheme.AddLayoutElement(barBg, flexibleWidth: 1f, preferredHeight: 12f);

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(barBg.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(1f, 1f);
            fillRect.offsetMax = new Vector2(-1f, -1f);
            fillRect.pivot = new Vector2(0f, 0.5f);

            _patienceBar = fillObj.AddComponent<Image>();
            _patienceBar.color = UITheme.AccentGreen;
            _patienceBar.type = Image.Type.Filled;
            _patienceBar.fillMethod = Image.FillMethod.Horizontal;
            _patienceBar.fillOrigin = 0;
            _patienceBar.fillAmount = 1f;

            // Time text
            GameObject timeObj = new GameObject("Time");
            timeObj.transform.SetParent(patienceContainer.transform, false);
            _patienceText = timeObj.AddComponent<TextMeshProUGUI>();
            _patienceText.text = "30s";
            _patienceText.fontSize = UITheme.FontSizeSmall;
            _patienceText.color = UITheme.TextWhite;
            _patienceText.alignment = TextAlignmentOptions.Right;
            _patienceText.textWrappingMode = TextWrappingModes.NoWrap;
            UITheme.AddLayoutElement(timeObj, minWidth: 35f);
        }

        /// <summary>
        /// Set the caller to display.
        /// </summary>
        public void SetCaller(Caller caller)
        {
            _caller = caller;
            UpdateDisplay();
        }

        /// <summary>
        /// Clear the display (no caller).
        /// </summary>
        public void Clear()
        {
            _caller = null;
            
            if (_nameText != null) _nameText.text = "---";
            if (_phoneText != null) _phoneText.text = "Phone: ---";
            if (_locationText != null) _locationText.text = "From: ---";
            if (_topicText != null) _topicText.text = "Topic: ---";
            if (_reasonText != null) _reasonText.text = "";
            if (_legitimacyText != null) _legitimacyText.text = "";
            if (_patienceBar != null) _patienceBar.fillAmount = 0f;
            if (_patienceText != null) _patienceText.text = "";
        }

        private void Update()
        {
            // Update patience display in real-time
            if (_caller != null && _showPatience)
            {
                UpdatePatienceDisplay();
            }
        }

        private void UpdateDisplay()
        {
            if (_caller == null)
            {
                Clear();
                return;
            }

            if (_nameText != null)
            {
                _nameText.text = _caller.Name;
            }

            if (_phoneText != null)
            {
                _phoneText.text = $"Phone: {_caller.PhoneNumber}";
            }

            if (_locationText != null)
            {
                _locationText.text = $"From: {_caller.Location}";
            }

            if (_topicText != null)
            {
                _topicText.text = $"Topic: {_caller.ClaimedTopic}";
            }

            if (_reasonText != null)
            {
                _reasonText.text = $"\"{_caller.CallReason}\"";
            }

            if (_legitimacyText != null)
            {
                string legColor = _caller.Legitimacy switch
                {
                    CallerLegitimacy.Fake => "#FF3333",
                    CallerLegitimacy.Questionable => "#FFCC00",
                    CallerLegitimacy.Credible => "#FFFFFF",
                    CallerLegitimacy.Compelling => "#33FF33",
                    _ => "#FFFFFF"
                };
                _legitimacyText.text = $"Legitimacy: <color={legColor}>{_caller.Legitimacy}</color>";

                // Show if they were lying
                if (_caller.IsLyingAboutTopic)
                {
                    _legitimacyText.text += $"\n<color=#FF3333>LIED! Actual topic: {_caller.ActualTopic}</color>";
                }
            }

            UpdatePatienceDisplay();
        }

        private void UpdatePatienceDisplay()
        {
            if (_caller == null || !_showPatience) return;

            float remaining = Mathf.Max(0f, _caller.Patience - _caller.WaitTime);
            float normalized = remaining / _caller.Patience;

            if (_patienceBar != null)
            {
                _patienceBar.fillAmount = normalized;

                // Color based on patience
                if (normalized > 0.5f)
                    _patienceBar.color = UITheme.AccentGreen;
                else if (normalized > 0.25f)
                    _patienceBar.color = UITheme.AccentYellow;
                else
                    _patienceBar.color = UITheme.AccentRed;
            }

            if (_patienceText != null)
            {
                _patienceText.text = $"{remaining:F0}s";
            }
        }
    }
}
