using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KBTV.Callers;
using KBTV.Audio;

namespace KBTV.UI
{
    /// <summary>
    /// Panel displaying the current on-air caller.
    /// Shows caller info, legitimacy reveal, and end call button.
    /// </summary>
    public class OnAirPanel : MonoBehaviour
    {
        private TextMeshProUGUI _headerText;
        private GameObject _liveIndicator;
        private CallerCardUI _callerCard;
        private GameObject _callerContainer;
        private GameObject _emptyState;
        private Button _putOnAirButton;
        private Button _endCallButton;

        private CallerQueue _callerQueue;

        /// <summary>
        /// Create and initialize an OnAirPanel on the given parent.
        /// </summary>
        public static OnAirPanel Create(Transform parent)
        {
            GameObject panelObj = UITheme.CreatePanel("OnAirPanel", parent, UITheme.PanelBackground);

            UITheme.AddVerticalLayout(panelObj, padding: UITheme.PanelPadding, spacing: 8f);

            OnAirPanel panel = panelObj.AddComponent<OnAirPanel>();
            panel.BuildUI();

            return panel;
        }

        private void BuildUI()
        {
            // Header row with live indicator
            GameObject headerRow = new GameObject("HeaderRow");
            headerRow.transform.SetParent(transform, false);
            UITheme.AddHorizontalLayout(headerRow, spacing: 10f);
            UITheme.AddLayoutElement(headerRow, preferredHeight: 25f);

            // Header text
            _headerText = UITheme.CreateText("Header", headerRow.transform, "ON AIR",
                UITheme.FontSizeLarge, UITheme.AccentRed, TextAlignmentOptions.Left);
            _headerText.fontStyle = FontStyles.Bold;
            UITheme.AddLayoutElement(_headerText.gameObject, minWidth: 80f);

            // Live indicator dot
            _liveIndicator = new GameObject("LiveDot");
            _liveIndicator.transform.SetParent(headerRow.transform, false);
            Image dotImage = _liveIndicator.AddComponent<Image>();
            dotImage.color = UITheme.AccentRed;
            UITheme.AddLayoutElement(_liveIndicator, preferredWidth: 12f, preferredHeight: 12f);

            // Spacer
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(headerRow.transform, false);
            spacer.AddComponent<RectTransform>();
            UITheme.AddLayoutElement(spacer, flexibleWidth: 1f);

            // Divider
            CreateDivider();

            // Caller container (shown when someone is on air)
            _callerContainer = new GameObject("CallerContainer");
            _callerContainer.transform.SetParent(transform, false);
            UITheme.AddVerticalLayout(_callerContainer, padding: 0f, spacing: 10f);
            UITheme.AddLayoutElement(_callerContainer, flexibleHeight: 1f);

            // Caller card (on-air style - shows legitimacy)
            _callerCard = CallerCardUI.CreateOnAir(_callerContainer.transform);
            UITheme.AddLayoutElement(_callerCard.gameObject, flexibleHeight: 1f);

            // End call button
            _endCallButton = UITheme.CreateButton("EndCallBtn", _callerContainer.transform, "END CALL",
                UITheme.AccentRed, UITheme.TextWhite);
            UITheme.AddLayoutElement(_endCallButton.gameObject, preferredHeight: UITheme.ButtonHeight);
            _endCallButton.onClick.AddListener(OnEndCallClicked);

            // Empty state (shown when no one is on air)
            _emptyState = new GameObject("EmptyState");
            _emptyState.transform.SetParent(transform, false);
            UITheme.AddVerticalLayout(_emptyState, padding: 20f, spacing: 10f, childForceExpand: true);
            UITheme.AddLayoutElement(_emptyState, flexibleHeight: 1f);

            // Empty message
            TextMeshProUGUI emptyMsg = UITheme.CreateText("EmptyMsg", _emptyState.transform,
                "No caller on air",
                UITheme.FontSizeNormal, UITheme.TextGray, TextAlignmentOptions.Center);
            UITheme.AddLayoutElement(emptyMsg.gameObject, preferredHeight: 30f);

            // Put on air button
            _putOnAirButton = UITheme.CreateButton("PutOnAirBtn", _emptyState.transform, "PUT NEXT CALLER ON AIR",
                UITheme.PanelBorder, UITheme.TextPrimary);
            UITheme.AddLayoutElement(_putOnAirButton.gameObject, preferredHeight: UITheme.ButtonHeight);
            _putOnAirButton.onClick.AddListener(OnPutOnAirClicked);

            // Initially show empty state
            _callerContainer.SetActive(false);
            _emptyState.SetActive(true);
            _liveIndicator.SetActive(false);
        }

        private void CreateDivider()
        {
            GameObject divider = new GameObject("Divider");
            divider.transform.SetParent(transform, false);
            Image dividerImage = divider.AddComponent<Image>();
            dividerImage.color = UITheme.PanelBorder;
            UITheme.AddLayoutElement(divider, preferredHeight: 1f);
        }

        private void Start()
        {
            _callerQueue = CallerQueue.Instance;

            if (_callerQueue != null)
            {
                _callerQueue.OnCallerOnAir += OnCallerOnAir;
                _callerQueue.OnCallerCompleted += OnCallerCompleted;
                Debug.Log("OnAirPanel: Subscribed to CallerQueue events");
            }
            else
            {
                Debug.LogWarning("OnAirPanel: CallerQueue.Instance is null - will retry in Update");
            }

            UpdateDisplay();
        }

        private void OnDestroy()
        {
            if (_callerQueue != null)
            {
                _callerQueue.OnCallerOnAir -= OnCallerOnAir;
                _callerQueue.OnCallerCompleted -= OnCallerCompleted;
            }
        }

        private void OnCallerOnAir(Caller caller)
        {
            Debug.Log($"OnAirPanel: Caller {caller.Name} went on air");
            UpdateDisplay();
        }

        private void OnCallerCompleted(Caller caller)
        {
            Debug.Log($"OnAirPanel: Caller {caller.Name} completed");
            UpdateDisplay();
        }

        private void Update()
        {
            // Retry subscription if we missed it in Start()
            if (_callerQueue == null)
            {
                _callerQueue = CallerQueue.Instance;
                if (_callerQueue != null)
                {
                    _callerQueue.OnCallerOnAir += OnCallerOnAir;
                    _callerQueue.OnCallerCompleted += OnCallerCompleted;
                    Debug.Log("OnAirPanel: Late-subscribed to CallerQueue events");
                }
            }

            // Check if on-air state changed
            bool hasOnAir = _callerQueue != null && _callerQueue.OnAirCaller != null;

            if (hasOnAir != _callerContainer.activeSelf)
            {
                UpdateDisplay();
            }

            // Blink the live indicator
            if (_liveIndicator.activeSelf)
            {
                Image dot = _liveIndicator.GetComponent<Image>();
                if (dot != null)
                {
                    float alpha = (Mathf.Sin(Time.time * 4f) + 1f) / 2f;
                    Color c = dot.color;
                    c.a = Mathf.Lerp(0.3f, 1f, alpha);
                    dot.color = c;
                }
            }
        }

        private void UpdateDisplay()
        {
            if (_callerQueue == null) return;

            Caller onAir = _callerQueue.OnAirCaller;
            bool hasOnAir = onAir != null;

            _callerContainer.SetActive(hasOnAir);
            _emptyState.SetActive(!hasOnAir);
            _liveIndicator.SetActive(hasOnAir);

            if (hasOnAir)
            {
                _callerCard.SetCaller(onAir);
            }
            else
            {
                _callerCard.Clear();
            }

            // Update put on air button state
            _putOnAirButton.interactable = _callerQueue.HasOnHoldCallers;
        }

        private void OnEndCallClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            if (_callerQueue != null)
            {
                _callerQueue.EndCurrentCall();
                UpdateDisplay();
            }
        }

        private void OnPutOnAirClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            
            if (_callerQueue == null)
            {
                Debug.LogWarning("OnAirPanel: CallerQueue is null!");
                return;
            }
            
            Debug.Log($"OnAirPanel: Put On Air clicked. OnHold count: {_callerQueue.OnHoldCallers.Count}, HasOnHoldCallers: {_callerQueue.HasOnHoldCallers}");
            
            if (_callerQueue.HasOnHoldCallers)
            {
                Caller caller = _callerQueue.PutNextCallerOnAir();
                Debug.Log($"OnAirPanel: Put {caller?.Name ?? "null"} on air");
                UpdateDisplay();
            }
            else
            {
                Debug.Log("OnAirPanel: No callers on hold to put on air");
            }
        }
    }
}
