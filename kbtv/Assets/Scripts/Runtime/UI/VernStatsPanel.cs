using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KBTV.Core;
using KBTV.Data;

namespace KBTV.UI
{
    /// <summary>
    /// Panel displaying all of Vern's stats and the current show quality.
    /// </summary>
    public class VernStatsPanel : MonoBehaviour
    {
        private StatBarUI _moodBar;
        private StatBarUI _energyBar;
        private StatBarUI _hungerBar;
        private StatBarUI _thirstBar;
        private StatBarUI _patienceBar;
        private StatBarUI _susceptibilityBar;
        private StatBarUI _beliefBar;

        private TextMeshProUGUI _showQualityLabel;
        private Image _showQualityFill;
        private TextMeshProUGUI _showQualityValue;

        private VernStats _stats;

        /// <summary>
        /// Create and initialize a VernStatsPanel on the given parent.
        /// </summary>
        public static VernStatsPanel Create(Transform parent)
        {
            GameObject panelObj = UITheme.CreatePanel("VernStatsPanel", parent, UITheme.PanelBackground);
            
            UITheme.AddVerticalLayout(panelObj, padding: UITheme.PanelPadding, spacing: 8f);

            VernStatsPanel panel = panelObj.AddComponent<VernStatsPanel>();
            panel.BuildUI();

            return panel;
        }

        private void BuildUI()
        {
            // Header
            TextMeshProUGUI header = UITheme.CreateText("Header", transform, "VERN'S STATUS", 
                UITheme.FontSizeLarge, UITheme.TextAmber, TextAlignmentOptions.Left);
            header.fontStyle = FontStyles.Bold;
            UITheme.AddLayoutElement(header.gameObject, preferredHeight: 25f);

            // Divider
            CreateDivider();

            // Stat bars
            _moodBar = StatBarUI.Create(transform, "Mood", UITheme.StatMood);
            _energyBar = StatBarUI.Create(transform, "Energy", UITheme.StatEnergy);
            _hungerBar = StatBarUI.Create(transform, "Hunger", UITheme.StatHunger);
            _thirstBar = StatBarUI.Create(transform, "Thirst", UITheme.StatThirst);
            _patienceBar = StatBarUI.Create(transform, "Patience", UITheme.StatPatience);
            _susceptibilityBar = StatBarUI.Create(transform, "Susceptibility", UITheme.StatSusceptibility);

            // Divider before Belief
            CreateDivider();

            // Belief bar (emphasized)
            _beliefBar = StatBarUI.Create(transform, "BELIEF", UITheme.StatBelief);

            // Divider before show quality
            CreateDivider();

            // Show Quality display
            CreateShowQualityDisplay();
        }

        private void CreateDivider()
        {
            GameObject divider = new GameObject("Divider");
            divider.transform.SetParent(transform, false);
            
            Image dividerImage = divider.AddComponent<Image>();
            dividerImage.color = UITheme.PanelBorder;
            
            UITheme.AddLayoutElement(divider, preferredHeight: 1f);
        }

        private void CreateShowQualityDisplay()
        {
            // Container
            GameObject container = new GameObject("ShowQuality");
            container.transform.SetParent(transform, false);
            UITheme.AddHorizontalLayout(container, spacing: 8f);
            UITheme.AddLayoutElement(container, preferredHeight: 30f);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(container.transform, false);
            _showQualityLabel = labelObj.AddComponent<TextMeshProUGUI>();
            _showQualityLabel.text = "SHOW QUALITY";
            _showQualityLabel.fontSize = UITheme.FontSizeNormal;
            _showQualityLabel.color = UITheme.TextPrimary;
            _showQualityLabel.fontStyle = FontStyles.Bold;
            _showQualityLabel.alignment = TextAlignmentOptions.Left;
            _showQualityLabel.textWrappingMode = TextWrappingModes.NoWrap;
            UITheme.AddLayoutElement(labelObj, minWidth: 120f);

            // Progress bar container
            GameObject barContainer = new GameObject("BarContainer");
            barContainer.transform.SetParent(container.transform, false);
            UITheme.AddLayoutElement(barContainer, flexibleWidth: 1f, preferredHeight: 24f);

            // Background
            Image bgImage = barContainer.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f);

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(barContainer.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            fillRect.pivot = new Vector2(0f, 0.5f);

            _showQualityFill = fillObj.AddComponent<Image>();
            _showQualityFill.color = UITheme.AccentGreen;
            _showQualityFill.type = Image.Type.Filled;
            _showQualityFill.fillMethod = Image.FillMethod.Horizontal;
            _showQualityFill.fillOrigin = 0;
            _showQualityFill.fillAmount = 0.5f;

            // Value text
            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(container.transform, false);
            _showQualityValue = valueObj.AddComponent<TextMeshProUGUI>();
            _showQualityValue.text = "50%";
            _showQualityValue.fontSize = UITheme.FontSizeLarge;
            _showQualityValue.color = UITheme.TextWhite;
            _showQualityValue.fontStyle = FontStyles.Bold;
            _showQualityValue.alignment = TextAlignmentOptions.Right;
            _showQualityValue.textWrappingMode = TextWrappingModes.NoWrap;
            UITheme.AddLayoutElement(valueObj, minWidth: 50f);
        }

        private void Start()
        {
            TryBindStats();
        }

        private void Update()
        {
            // Retry binding if we missed it in Start()
            if (_stats == null)
            {
                TryBindStats();
            }
        }

        private void TryBindStats()
        {
            if (GameStateManager.Instance == null) return;
            if (GameStateManager.Instance.VernStats == null) return;
            
            // Check if stats are actually initialized (Mood will be null until Initialize() is called)
            if (GameStateManager.Instance.VernStats.Mood == null)
            {
                Debug.Log("VernStatsPanel: VernStats exists but Mood is null - waiting for Initialize()");
                return;
            }

            _stats = GameStateManager.Instance.VernStats;
            BindStats();
            Debug.Log("VernStatsPanel: Bound to VernStats successfully");
        }

        /// <summary>
        /// Bind to VernStats and subscribe to changes.
        /// </summary>
        public void BindStats()
        {
            if (_stats == null) return;

            _moodBar.SetStat(_stats.Mood);
            _energyBar.SetStat(_stats.Energy);
            _hungerBar.SetStat(_stats.Hunger);
            _thirstBar.SetStat(_stats.Thirst);
            _patienceBar.SetStat(_stats.Patience);
            _susceptibilityBar.SetStat(_stats.Susceptibility);
            _beliefBar.SetStat(_stats.Belief);

            _stats.OnStatsChanged += UpdateShowQuality;
            UpdateShowQuality();
        }

        private void OnDestroy()
        {
            if (_stats != null)
            {
                _stats.OnStatsChanged -= UpdateShowQuality;
            }
        }

        private void UpdateShowQuality()
        {
            if (_stats == null) return;

            float quality = _stats.CalculateShowQuality();

            if (_showQualityFill != null)
            {
                _showQualityFill.fillAmount = quality;

                // Color based on quality
                if (quality < 0.3f)
                    _showQualityFill.color = UITheme.AccentRed;
                else if (quality < 0.6f)
                    _showQualityFill.color = UITheme.AccentYellow;
                else
                    _showQualityFill.color = UITheme.AccentGreen;
            }

            if (_showQualityValue != null)
            {
                _showQualityValue.text = $"{(quality * 100f):F0}%";
            }
        }
    }
}
