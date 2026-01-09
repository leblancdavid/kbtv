using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KBTV.Data;
using KBTV.Managers;

namespace KBTV.UI
{
    /// <summary>
    /// UI panel displaying Vern's items with use buttons.
    /// Shows item name, quantity, hotkey, and cooldown status.
    /// </summary>
    public class ItemPanel : MonoBehaviour
    {
        private ItemManager _itemManager;
        private Dictionary<string, ItemButtonUI> _itemButtons = new Dictionary<string, ItemButtonUI>();

        /// <summary>
        /// Create and initialize an ItemPanel on the given parent.
        /// </summary>
        public static ItemPanel Create(Transform parent)
        {
            GameObject panelObj = UITheme.CreatePanel("ItemPanel", parent, UITheme.PanelBackground);

            RectTransform rect = panelObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            ItemPanel panel = panelObj.AddComponent<ItemPanel>();
            panel.BuildUI();

            return panel;
        }

        private void BuildUI()
        {
            UITheme.AddVerticalLayout(gameObject, padding: UITheme.PanelPadding, spacing: 8f);

            // Header
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(transform, false);
            TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
            headerText.text = "ITEMS";
            headerText.fontSize = UITheme.FontSizeLarge;
            headerText.color = UITheme.TextAmber;
            headerText.fontStyle = FontStyles.Bold;
            headerText.alignment = TextAlignmentOptions.Left;
            UITheme.AddLayoutElement(headerObj, preferredHeight: 24f);

            // Divider
            GameObject divider = new GameObject("Divider");
            divider.transform.SetParent(transform, false);
            Image dividerImg = divider.AddComponent<Image>();
            dividerImg.color = UITheme.PanelBorder;
            UITheme.AddLayoutElement(divider, preferredHeight: 2f);

            // Items will be added in Start when ItemManager is available
        }

        private void Start()
        {
            _itemManager = ItemManager.Instance;

            if (_itemManager != null)
            {
                _itemManager.OnInventoryChanged += RefreshUI;
                _itemManager.OnCooldownChanged += OnCooldownChanged;
                BuildItemButtons();
            }
        }

        private void OnDestroy()
        {
            if (_itemManager != null)
            {
                _itemManager.OnInventoryChanged -= RefreshUI;
                _itemManager.OnCooldownChanged -= OnCooldownChanged;
            }
        }

        private void Update()
        {
            // Handle keyboard shortcuts (1-5 for items by index)
            if (_itemManager != null)
            {
                for (int i = 0; i < 9; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                    {
                        _itemManager.UseItemByIndex(i);
                    }
                }
            }

            // Update cooldown displays
            UpdateCooldownDisplays();
        }

        private void BuildItemButtons()
        {
            if (_itemManager == null) return;

            int index = 1;
            foreach (var slot in _itemManager.ItemSlots)
            {
                CreateItemButton(slot, index);
                index++;
            }
        }

        private void CreateItemButton(ItemSlot slot, int displayIndex)
        {
            if (slot == null || slot.Modifier == null) return;

            GameObject buttonContainer = new GameObject($"Item_{slot.ItemId}");
            buttonContainer.transform.SetParent(transform, false);
            UITheme.AddLayoutElement(buttonContainer, preferredHeight: 45f);

            // Background
            Image bgImage = buttonContainer.AddComponent<Image>();
            bgImage.color = new Color(0.12f, 0.12f, 0.12f);

            // Horizontal layout
            UITheme.AddHorizontalLayout(buttonContainer, padding: 6f, spacing: 8f);

            // Hotkey indicator (use display index if no hotkey set)
            int hotkeyDisplay = slot.Hotkey > 0 ? slot.Hotkey : displayIndex;
            GameObject hotkeyObj = new GameObject("Hotkey");
            hotkeyObj.transform.SetParent(buttonContainer.transform, false);
            TextMeshProUGUI hotkeyText = hotkeyObj.AddComponent<TextMeshProUGUI>();
            hotkeyText.text = $"[{hotkeyDisplay}]";
            hotkeyText.fontSize = UITheme.FontSizeSmall;
            hotkeyText.color = UITheme.TextDim;
            hotkeyText.alignment = TextAlignmentOptions.Center;
            hotkeyText.textWrappingMode = TextWrappingModes.NoWrap;
            UITheme.AddLayoutElement(hotkeyObj, minWidth: 28f, preferredWidth: 28f);

            // Item name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(buttonContainer.transform, false);
            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = slot.ShortName;
            nameText.fontSize = UITheme.FontSizeNormal;
            nameText.color = UITheme.TextPrimary;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            UITheme.AddLayoutElement(nameObj, flexibleWidth: 1f);

            // Quantity
            GameObject qtyObj = new GameObject("Quantity");
            qtyObj.transform.SetParent(buttonContainer.transform, false);
            TextMeshProUGUI qtyText = qtyObj.AddComponent<TextMeshProUGUI>();
            qtyText.text = $"x{slot.Quantity}";
            qtyText.fontSize = UITheme.FontSizeNormal;
            qtyText.color = UITheme.TextAmber;
            qtyText.alignment = TextAlignmentOptions.Right;
            qtyText.textWrappingMode = TextWrappingModes.NoWrap;
            UITheme.AddLayoutElement(qtyObj, minWidth: 30f);

            // Cooldown text (hidden when not on cooldown)
            GameObject cooldownObj = new GameObject("Cooldown");
            cooldownObj.transform.SetParent(buttonContainer.transform, false);
            TextMeshProUGUI cooldownText = cooldownObj.AddComponent<TextMeshProUGUI>();
            cooldownText.text = "";
            cooldownText.fontSize = UITheme.FontSizeSmall;
            cooldownText.color = UITheme.AccentRed;
            cooldownText.alignment = TextAlignmentOptions.Right;
            cooldownText.textWrappingMode = TextWrappingModes.NoWrap;
            UITheme.AddLayoutElement(cooldownObj, minWidth: 35f);

            // Use button
            Button useButton = UITheme.CreateButton("UseBtn", buttonContainer.transform, "USE", UITheme.AccentGreen, UITheme.BackgroundDark);
            UITheme.AddLayoutElement(useButton.gameObject, minWidth: 50f, preferredWidth: 50f);

            // Button click handler
            string itemId = slot.ItemId;
            useButton.onClick.AddListener(() => OnUseClicked(itemId));

            // Store references for updates
            var buttonUI = new ItemButtonUI
            {
                Container = buttonContainer,
                NameText = nameText,
                QuantityText = qtyText,
                CooldownText = cooldownText,
                UseButton = useButton,
                Background = bgImage,
                Slot = slot
            };
            _itemButtons[slot.ItemId] = buttonUI;
        }

        private void OnUseClicked(string itemId)
        {
            _itemManager?.UseItem(itemId);
        }

        private void RefreshUI()
        {
            foreach (var kvp in _itemButtons)
            {
                var buttonUI = kvp.Value;
                if (buttonUI.Slot != null)
                {
                    UpdateButtonState(buttonUI);
                }
            }
        }

        private void OnCooldownChanged(StatModifier modifier)
        {
            string itemId = modifier.name;
            if (_itemButtons.TryGetValue(itemId, out var buttonUI))
            {
                UpdateButtonState(buttonUI);
            }
        }

        private void UpdateCooldownDisplays()
        {
            foreach (var kvp in _itemButtons)
            {
                var buttonUI = kvp.Value;
                if (buttonUI.Slot != null && buttonUI.Slot.IsOnCooldown)
                {
                    buttonUI.CooldownText.text = $"{buttonUI.Slot.CooldownRemaining:F0}s";
                }
            }
        }

        private void UpdateButtonState(ItemButtonUI buttonUI)
        {
            var slot = buttonUI.Slot;

            // Update quantity
            buttonUI.QuantityText.text = $"x{slot.Quantity}";
            buttonUI.QuantityText.color = slot.HasStock ? UITheme.TextAmber : UITheme.AccentRed;

            // Update cooldown
            if (slot.IsOnCooldown)
            {
                buttonUI.CooldownText.text = $"{slot.CooldownRemaining:F0}s";
                buttonUI.CooldownText.gameObject.SetActive(true);
            }
            else
            {
                buttonUI.CooldownText.text = "";
                buttonUI.CooldownText.gameObject.SetActive(false);
            }

            // Update button interactability
            buttonUI.UseButton.interactable = slot.CanUse;

            // Visual feedback for disabled state
            buttonUI.NameText.color = slot.CanUse ? UITheme.TextPrimary : UITheme.TextDim;
            buttonUI.Background.color = slot.CanUse 
                ? new Color(0.12f, 0.12f, 0.12f) 
                : new Color(0.08f, 0.08f, 0.08f);
        }

        private class ItemButtonUI
        {
            public GameObject Container;
            public TextMeshProUGUI NameText;
            public TextMeshProUGUI QuantityText;
            public TextMeshProUGUI CooldownText;
            public Button UseButton;
            public Image Background;
            public ItemSlot Slot;
        }
    }
}
