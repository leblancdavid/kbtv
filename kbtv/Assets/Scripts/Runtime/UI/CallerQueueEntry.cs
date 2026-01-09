using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KBTV.Callers;

namespace KBTV.UI
{
    /// <summary>
    /// Single entry in the caller queue list.
    /// </summary>
    public class CallerQueueEntry : MonoBehaviour
    {
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _topicText;
        private Image _patienceBar;
        private Caller _caller;

        public static CallerQueueEntry Create(Transform parent, Caller caller, bool showPatience)
        {
            GameObject entryObj = new GameObject($"Entry_{caller.Name}");
            entryObj.transform.SetParent(parent, false);

            Image bg = entryObj.AddComponent<Image>();
            bg.color = UITheme.QueueEntryBackground;

            UITheme.AddHorizontalLayout(entryObj, padding: 6f, spacing: 8f);
            UITheme.AddLayoutElement(entryObj, preferredHeight: 28f);

            CallerQueueEntry entry = entryObj.AddComponent<CallerQueueEntry>();
            entry._caller = caller;
            entry.BuildUI(showPatience);

            return entry;
        }

        private void BuildUI(bool showPatience)
        {
            // Name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(transform, false);
            _nameText = nameObj.AddComponent<TextMeshProUGUI>();
            _nameText.text = _caller.Name;
            _nameText.fontSize = UITheme.FontSizeSmall;
            _nameText.color = UITheme.TextWhite;
            _nameText.alignment = TextAlignmentOptions.Left;
            _nameText.textWrappingMode = TextWrappingModes.NoWrap;
            _nameText.overflowMode = TextOverflowModes.Ellipsis;
            UITheme.AddLayoutElement(nameObj, minWidth: 80f, flexibleWidth: 0.5f);

            // Topic
            GameObject topicObj = new GameObject("Topic");
            topicObj.transform.SetParent(transform, false);
            _topicText = topicObj.AddComponent<TextMeshProUGUI>();
            _topicText.text = _caller.ClaimedTopic;
            _topicText.fontSize = UITheme.FontSizeSmall;
            _topicText.color = UITheme.TextGray;
            _topicText.alignment = TextAlignmentOptions.Left;
            _topicText.textWrappingMode = TextWrappingModes.NoWrap;
            _topicText.overflowMode = TextOverflowModes.Ellipsis;
            UITheme.AddLayoutElement(topicObj, flexibleWidth: 1f);

            // Patience bar
            if (showPatience)
            {
                GameObject barBg = new GameObject("PatienceBar");
                barBg.transform.SetParent(transform, false);
                Image bgImage = barBg.AddComponent<Image>();
                bgImage.color = new Color(0.1f, 0.1f, 0.1f);
                UITheme.AddLayoutElement(barBg, minWidth: 40f, preferredHeight: 8f);

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
            }
        }

        private void Update()
        {
            if (_caller == null || _patienceBar == null) return;

            float remaining = Mathf.Max(0f, _caller.Patience - _caller.WaitTime);
            float normalized = remaining / _caller.Patience;

            _patienceBar.fillAmount = normalized;
            _patienceBar.color = UITheme.GetPatienceColor(normalized);
        }
    }
}
