using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KBTV.UI
{
    /// <summary>
    /// Static class containing UI theme colors and helper methods for runtime UI generation.
    /// Dark/retro radio station aesthetic.
    /// </summary>
    public static class UITheme
    {
        // Background colors
        public static readonly Color BackgroundDark = new Color(0.1f, 0.1f, 0.1f);       // #1a1a1a
        public static readonly Color PanelBackground = new Color(0.16f, 0.16f, 0.16f);   // #2a2a2a
        public static readonly Color PanelBorder = new Color(0.3f, 0.3f, 0.3f);          // #4d4d4d

        // Text colors
        public static readonly Color TextPrimary = new Color(0.2f, 1f, 0.2f);            // #33ff33 (green terminal)
        public static readonly Color TextAmber = new Color(1f, 0.67f, 0f);               // #ffaa00
        public static readonly Color TextWhite = Color.white;
        public static readonly Color TextGray = new Color(0.6f, 0.6f, 0.6f);             // #999999
        public static readonly Color TextDim = new Color(0.4f, 0.4f, 0.4f);              // #666666

        // Accent colors
        public static readonly Color AccentRed = new Color(1f, 0.2f, 0.2f);              // Live/reject/danger
        public static readonly Color AccentGreen = new Color(0.2f, 0.8f, 0.2f);          // Approve/good
        public static readonly Color AccentYellow = new Color(1f, 0.9f, 0.2f);           // Warning
        public static readonly Color AccentCyan = new Color(0.2f, 0.8f, 0.8f);           // Info

        // Item button colors
        public static readonly Color ItemButtonEnabled = new Color(0.12f, 0.12f, 0.12f);
        public static readonly Color ItemButtonDisabled = new Color(0.08f, 0.08f, 0.08f);
        
        // Queue entry colors
        public static readonly Color QueueEntryBackground = new Color(0.15f, 0.15f, 0.15f);

        // Stat bar colors
        public static readonly Color StatMood = new Color(1f, 0.4f, 0.7f);               // Pink
        public static readonly Color StatEnergy = new Color(1f, 0.9f, 0.2f);             // Yellow
        public static readonly Color StatHunger = new Color(1f, 0.5f, 0f);               // Orange
        public static readonly Color StatThirst = new Color(0.2f, 0.8f, 1f);             // Cyan
        public static readonly Color StatPatience = new Color(0.6f, 0.6f, 0.6f);         // Gray
        public static readonly Color StatSusceptibility = new Color(0.6f, 0.2f, 0.8f);   // Purple
        public static readonly Color StatBelief = new Color(0.2f, 1f, 0.2f);             // Green

        // Layout constants
        public const float PanelPadding = 10f;
        public const float ElementSpacing = 5f;
        public const float HeaderHeight = 50f;
        public const float StatBarHeight = 25f;
        public const float ButtonHeight = 35f;

        // Font sizes
        public const float FontSizeSmall = 12f;
        public const float FontSizeNormal = 14f;
        public const float FontSizeLarge = 18f;
        public const float FontSizeHeader = 24f;

        /// <summary>
        /// Create a panel GameObject with standard styling.
        /// Note: Does not set anchors - parent LayoutGroups will control positioning.
        /// Use FillParent() explicitly if you need the panel to stretch to fill its parent.
        /// </summary>
        public static GameObject CreatePanel(string name, Transform parent, Color? backgroundColor = null)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            // Don't set anchors here - let LayoutGroups control positioning
            // Panels that need to fill parent should call FillParent() explicitly

            Image image = panel.AddComponent<Image>();
            image.color = backgroundColor ?? PanelBackground;

            return panel;
        }

        /// <summary>
        /// Create a TextMeshPro text element.
        /// </summary>
        public static TextMeshProUGUI CreateText(string name, Transform parent, string text = "", 
            float fontSize = FontSizeNormal, Color? color = null, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color ?? TextPrimary;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            return tmp;
        }

        /// <summary>
        /// Create a button with standard styling.
        /// </summary>
        public static Button CreateButton(string name, Transform parent, string label, 
            Color? backgroundColor = null, Color? textColor = null)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, ButtonHeight);

            Image image = buttonObj.AddComponent<Image>();
            image.color = backgroundColor ?? PanelBorder;

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;

            // Button colors
            ColorBlock colors = button.colors;
            colors.normalColor = backgroundColor ?? PanelBorder;
            colors.highlightedColor = (backgroundColor ?? PanelBorder) * 1.2f;
            colors.pressedColor = (backgroundColor ?? PanelBorder) * 0.8f;
            colors.selectedColor = backgroundColor ?? PanelBorder;
            button.colors = colors;

            // Button text
            TextMeshProUGUI buttonText = CreateText("Text", buttonObj.transform, label, 
                FontSizeNormal, textColor ?? TextWhite, TextAlignmentOptions.Center);
            buttonText.raycastTarget = false; // Don't block button clicks

            return button;
        }

        /// <summary>
        /// Create a progress/stat bar with background and fill.
        /// </summary>
        public static (Image background, Image fill) CreateProgressBar(string name, Transform parent, Color fillColor)
        {
            GameObject barObj = new GameObject(name);
            barObj.transform.SetParent(parent, false);

            RectTransform barRect = barObj.AddComponent<RectTransform>();
            barRect.sizeDelta = new Vector2(150f, 20f);

            // Background
            Image background = barObj.AddComponent<Image>();
            background.color = new Color(0.1f, 0.1f, 0.1f);

            // Fill container (for masking)
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(barObj.transform, false);

            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            fillRect.pivot = new Vector2(0f, 0.5f);

            Image fill = fillObj.AddComponent<Image>();
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0.5f;

            return (background, fill);
        }

        /// <summary>
        /// Create a vertical layout group with standard settings.
        /// </summary>
        public static VerticalLayoutGroup AddVerticalLayout(GameObject obj, float padding = PanelPadding, 
            float spacing = ElementSpacing, bool childForceExpand = false)
        {
            VerticalLayoutGroup layout = obj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;  // Let layout control child heights via LayoutElement
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = childForceExpand;
            layout.childAlignment = TextAnchor.UpperLeft;
            return layout;
        }

        /// <summary>
        /// Create a horizontal layout group with standard settings.
        /// </summary>
        public static HorizontalLayoutGroup AddHorizontalLayout(GameObject obj, float padding = 0f, 
            float spacing = ElementSpacing, bool childForceExpand = false)
        {
            HorizontalLayoutGroup layout = obj.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = childForceExpand;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;
            return layout;
        }

        /// <summary>
        /// Add a layout element for size control.
        /// </summary>
        public static LayoutElement AddLayoutElement(GameObject obj, float preferredWidth = -1f, 
            float preferredHeight = -1f, float minWidth = -1f, float minHeight = -1f, 
            float flexibleWidth = -1f, float flexibleHeight = -1f)
        {
            LayoutElement element = obj.AddComponent<LayoutElement>();
            if (preferredWidth >= 0) element.preferredWidth = preferredWidth;
            if (preferredHeight >= 0) element.preferredHeight = preferredHeight;
            if (minWidth >= 0) element.minWidth = minWidth;
            if (minHeight >= 0) element.minHeight = minHeight;
            if (flexibleWidth >= 0) element.flexibleWidth = flexibleWidth;
            if (flexibleHeight >= 0) element.flexibleHeight = flexibleHeight;
            return element;
        }

        /// <summary>
        /// Set RectTransform to fill parent with optional margins.
        /// </summary>
        public static void FillParent(RectTransform rect, float margin = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }

        /// <summary>
        /// Set RectTransform anchors and position for top-anchored element.
        /// </summary>
        public static void AnchorTop(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Set RectTransform anchors for stretch below a top element.
        /// </summary>
        public static void AnchorFillBelowTop(RectTransform rect, float topOffset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(0f, -topOffset);
        }

        /// <summary>
        /// Create a horizontal divider line.
        /// </summary>
        public static GameObject CreateDivider(Transform parent, float height = 1f)
        {
            GameObject divider = new GameObject("Divider");
            divider.transform.SetParent(parent, false);
            Image dividerImage = divider.AddComponent<Image>();
            dividerImage.color = PanelBorder;
            AddLayoutElement(divider, preferredHeight: height);
            return divider;
        }

        /// <summary>
        /// Get color for patience/progress bar based on normalized value (0-1).
        /// Green when high, yellow when medium, red when low.
        /// </summary>
        public static Color GetPatienceColor(float normalized)
        {
            if (normalized > 0.5f)
                return AccentGreen;
            else if (normalized > 0.25f)
                return AccentYellow;
            else
                return AccentRed;
        }

        /// <summary>
        /// Get alpha value for blinking effect based on time.
        /// Returns value between minAlpha and 1.0.
        /// </summary>
        public static float GetBlinkAlpha(float time, float speed = 4f, float minAlpha = 0.3f)
        {
            float alpha = (Mathf.Sin(time * speed) + 1f) / 2f;
            return Mathf.Lerp(minAlpha, 1f, alpha);
        }
    }
}
