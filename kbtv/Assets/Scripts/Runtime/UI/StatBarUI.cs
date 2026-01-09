using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KBTV.Data;

namespace KBTV.UI
{
    /// <summary>
    /// Reusable stat bar component displaying a label, progress bar, and value.
    /// Automatically updates when the bound Stat changes.
    /// </summary>
    public class StatBarUI : MonoBehaviour
    {
        private TextMeshProUGUI _labelText;
        private Image _fillImage;
        private RectTransform _fillRect;
        private RectTransform _barContainerRect;
        private TextMeshProUGUI _valueText;
        private Stat _stat;
        private Color _fillColor;

        /// <summary>
        /// Create and initialize a StatBarUI on the given parent.
        /// </summary>
        public static StatBarUI Create(Transform parent, string label, Color fillColor)
        {
            GameObject barObj = new GameObject($"StatBar_{label}");
            barObj.transform.SetParent(parent, false);

            barObj.AddComponent<RectTransform>();
            // Don't set sizeDelta - let LayoutElement control size

            // Horizontal layout for label | bar | value
            UITheme.AddHorizontalLayout(barObj, spacing: 8f);
            UITheme.AddLayoutElement(barObj, preferredHeight: UITheme.StatBarHeight, minHeight: UITheme.StatBarHeight);

            StatBarUI statBar = barObj.AddComponent<StatBarUI>();
            statBar._fillColor = fillColor;
            statBar.BuildUI(label);

            return statBar;
        }

        private void BuildUI(string label)
        {
            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(transform, false);
            _labelText = labelObj.AddComponent<TextMeshProUGUI>();
            _labelText.text = label;
            _labelText.fontSize = UITheme.FontSizeNormal;
            _labelText.color = UITheme.TextAmber;
            _labelText.alignment = TextAlignmentOptions.Left;
            _labelText.textWrappingMode = TextWrappingModes.NoWrap;
            UITheme.AddLayoutElement(labelObj, minWidth: 100f, preferredWidth: 100f);

            // Progress bar container
            GameObject barContainer = new GameObject("BarContainer");
            barContainer.transform.SetParent(transform, false);
            _barContainerRect = barContainer.AddComponent<RectTransform>();
            UITheme.AddLayoutElement(barContainer, flexibleWidth: 1f, preferredHeight: 20f);

            // Background
            Image bgImage = barContainer.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f);

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(barContainer.transform, false);
            _fillRect = fillObj.AddComponent<RectTransform>();
            // Anchor to left side, stretch vertically
            _fillRect.anchorMin = new Vector2(0f, 0f);
            _fillRect.anchorMax = new Vector2(0f, 1f);
            _fillRect.pivot = new Vector2(0f, 0.5f);
            _fillRect.offsetMin = new Vector2(2f, 2f);
            _fillRect.offsetMax = new Vector2(2f, -2f); // Will set width via sizeDelta.x

            _fillImage = fillObj.AddComponent<Image>();
            _fillImage.color = _fillColor;

            // Value text
            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(transform, false);
            _valueText = valueObj.AddComponent<TextMeshProUGUI>();
            _valueText.text = "50";
            _valueText.fontSize = UITheme.FontSizeNormal;
            _valueText.color = UITheme.TextWhite;
            _valueText.alignment = TextAlignmentOptions.Right;
            _valueText.textWrappingMode = TextWrappingModes.NoWrap;
            UITheme.AddLayoutElement(valueObj, minWidth: 35f, preferredWidth: 35f);
        }

        /// <summary>
        /// Bind this bar to a Stat and subscribe to changes.
        /// </summary>
        public void SetStat(Stat stat)
        {
            // Unsubscribe from previous stat
            if (_stat != null)
            {
                _stat.OnValueChanged -= OnStatChanged;
            }

            _stat = stat;

            if (_stat != null)
            {
                _stat.OnValueChanged += OnStatChanged;
                Debug.Log($"StatBarUI: Bound to stat '{_stat.Name}', value={_stat.Value}");
                UpdateDisplay();
            }
            else
            {
                Debug.LogWarning($"StatBarUI: SetStat called with null stat");
            }
        }

        /// <summary>
        /// Update the label text.
        /// </summary>
        public void SetLabel(string label)
        {
            if (_labelText != null)
            {
                _labelText.text = label;
            }
        }

        /// <summary>
        /// Manually set the bar value (0-1 normalized).
        /// </summary>
        public void SetValue(float normalized, float displayValue)
        {
            if (_fillImage != null)
            {
                _fillImage.fillAmount = Mathf.Clamp01(normalized);
            }

            if (_valueText != null)
            {
                _valueText.text = $"{displayValue:F0}";
            }
        }

        private void OnStatChanged(float oldValue, float newValue)
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_stat == null) return;

            if (_fillRect != null && _barContainerRect != null)
            {
                // Calculate fill width based on normalized value
                float containerWidth = _barContainerRect.rect.width - 4f; // Account for padding
                float fillWidth = containerWidth * _stat.Normalized;
                _fillRect.sizeDelta = new Vector2(fillWidth, _fillRect.sizeDelta.y);
            }

            if (_valueText != null)
            {
                _valueText.text = $"{_stat.Value:F0}";
            }
        }

        private void LateUpdate()
        {
            // Continuously update the fill bar to handle layout changes
            // and ensure proper sizing after layout is calculated
            if (_stat != null && _fillRect != null && _barContainerRect != null)
            {
                float containerWidth = _barContainerRect.rect.width - 4f;
                if (containerWidth > 0)
                {
                    float fillWidth = containerWidth * _stat.Normalized;
                    _fillRect.sizeDelta = new Vector2(fillWidth, _fillRect.sizeDelta.y);
                }
            }
        }

        private void OnDestroy()
        {
            if (_stat != null)
            {
                _stat.OnValueChanged -= OnStatChanged;
            }
        }
    }
}
