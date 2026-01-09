using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KBTV.Core;
using KBTV.Managers;

namespace KBTV.UI
{
    /// <summary>
    /// Header bar displaying night number, current phase, in-game time, and remaining time.
    /// </summary>
    public class HeaderBarUI : MonoBehaviour
    {
        private TextMeshProUGUI _nightText;
        private TextMeshProUGUI _phaseText;
        private TextMeshProUGUI _clockText;
        private TextMeshProUGUI _remainingText;
        private TextMeshProUGUI _listenerText;
        private TextMeshProUGUI _listenerChangeText;
        private Image _liveIndicator;

        private GameStateManager _gameState;
        private TimeManager _timeManager;
        private ListenerManager _listenerManager;

        /// <summary>
        /// Create and initialize a HeaderBarUI on the given parent.
        /// </summary>
        public static HeaderBarUI Create(Transform parent)
        {
            GameObject headerObj = UITheme.CreatePanel("HeaderBar", parent, UITheme.PanelBackground);
            
            // Don't set anchors - let parent LayoutGroup control positioning
            UITheme.AddLayoutElement(headerObj, preferredHeight: UITheme.HeaderHeight, minHeight: UITheme.HeaderHeight);

            HeaderBarUI header = headerObj.AddComponent<HeaderBarUI>();
            header.BuildUI();

            return header;
        }

        private void BuildUI()
        {
            // Horizontal layout
            UITheme.AddHorizontalLayout(gameObject, padding: UITheme.PanelPadding, spacing: 20f);

            // Night indicator
            GameObject nightObj = new GameObject("Night");
            nightObj.transform.SetParent(transform, false);
            _nightText = nightObj.AddComponent<TextMeshProUGUI>();
            _nightText.text = "NIGHT 1";
            _nightText.fontSize = UITheme.FontSizeLarge;
            _nightText.color = UITheme.TextAmber;
            _nightText.fontStyle = FontStyles.Bold;
            _nightText.alignment = TextAlignmentOptions.Left;
            _nightText.textWrappingMode = TextWrappingModes.NoWrap;
            UITheme.AddLayoutElement(nightObj, minWidth: 100f);

            // Phase indicator
            GameObject phaseObj = new GameObject("Phase");
            phaseObj.transform.SetParent(transform, false);
            _phaseText = phaseObj.AddComponent<TextMeshProUGUI>();
            _phaseText.text = "PRE-SHOW";
            _phaseText.fontSize = UITheme.FontSizeLarge;
            _phaseText.color = UITheme.AccentYellow;
            _phaseText.fontStyle = FontStyles.Bold;
            _phaseText.alignment = TextAlignmentOptions.Left;
            _phaseText.textWrappingMode = TextWrappingModes.NoWrap;
            UITheme.AddLayoutElement(phaseObj, minWidth: 120f);

            // Live indicator (blinking dot)
            GameObject liveContainer = new GameObject("LiveIndicator");
            liveContainer.transform.SetParent(transform, false);
            UITheme.AddLayoutElement(liveContainer, minWidth: 80f);

            GameObject dotObj = new GameObject("Dot");
            dotObj.transform.SetParent(liveContainer.transform, false);
            _liveIndicator = dotObj.AddComponent<Image>();
            _liveIndicator.color = UITheme.AccentRed;
            RectTransform dotRect = dotObj.GetComponent<RectTransform>();
            dotRect.sizeDelta = new Vector2(12f, 12f);
            dotRect.anchorMin = new Vector2(0f, 0.5f);
            dotRect.anchorMax = new Vector2(0f, 0.5f);
            dotRect.anchoredPosition = new Vector2(6f, 0f);

            GameObject liveTextObj = new GameObject("LiveText");
            liveTextObj.transform.SetParent(liveContainer.transform, false);
            TextMeshProUGUI liveText = liveTextObj.AddComponent<TextMeshProUGUI>();
            liveText.text = "LIVE";
            liveText.fontSize = UITheme.FontSizeLarge;
            liveText.color = UITheme.AccentRed;
            liveText.fontStyle = FontStyles.Bold;
            liveText.alignment = TextAlignmentOptions.Left;
            RectTransform liveTextRect = liveTextObj.GetComponent<RectTransform>();
            liveTextRect.anchorMin = Vector2.zero;
            liveTextRect.anchorMax = Vector2.one;
            liveTextRect.offsetMin = new Vector2(20f, 0f);
            liveTextRect.offsetMax = Vector2.zero;

            // Spacer
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(transform, false);
            spacer.AddComponent<RectTransform>();
            UITheme.AddLayoutElement(spacer, flexibleWidth: 1f);

            // Clock
            GameObject clockObj = new GameObject("Clock");
            clockObj.transform.SetParent(transform, false);
            _clockText = clockObj.AddComponent<TextMeshProUGUI>();
            _clockText.text = "10:00 PM";
            _clockText.fontSize = UITheme.FontSizeHeader;
            _clockText.color = UITheme.TextPrimary;
            _clockText.fontStyle = FontStyles.Bold;
            _clockText.alignment = TextAlignmentOptions.Right;
            _clockText.textWrappingMode = TextWrappingModes.NoWrap;
            UITheme.AddLayoutElement(clockObj, minWidth: 120f);

            // Remaining time
            GameObject remainingObj = new GameObject("Remaining");
            remainingObj.transform.SetParent(transform, false);
            _remainingText = remainingObj.AddComponent<TextMeshProUGUI>();
            _remainingText.text = "(5:00 left)";
            _remainingText.fontSize = UITheme.FontSizeNormal;
            _remainingText.color = UITheme.TextGray;
            _remainingText.alignment = TextAlignmentOptions.Right;
            _remainingText.textWrappingMode = TextWrappingModes.NoWrap;
            UITheme.AddLayoutElement(remainingObj, minWidth: 100f);

            // Listener count section
            GameObject listenerContainer = new GameObject("ListenerContainer");
            listenerContainer.transform.SetParent(transform, false);
            UITheme.AddHorizontalLayout(listenerContainer, spacing: 4f);
            UITheme.AddLayoutElement(listenerContainer, minWidth: 140f);

            // Listener icon (headphones emoji or text)
            GameObject listenerIcon = new GameObject("ListenerIcon");
            listenerIcon.transform.SetParent(listenerContainer.transform, false);
            TextMeshProUGUI iconText = listenerIcon.AddComponent<TextMeshProUGUI>();
            iconText.text = "LISTENERS:";
            iconText.fontSize = UITheme.FontSizeSmall;
            iconText.color = UITheme.TextGray;
            iconText.alignment = TextAlignmentOptions.Right;
            iconText.textWrappingMode = TextWrappingModes.NoWrap;

            // Listener count
            GameObject listenerObj = new GameObject("ListenerCount");
            listenerObj.transform.SetParent(listenerContainer.transform, false);
            _listenerText = listenerObj.AddComponent<TextMeshProUGUI>();
            _listenerText.text = "0";
            _listenerText.fontSize = UITheme.FontSizeLarge;
            _listenerText.color = UITheme.TextAmber;
            _listenerText.fontStyle = FontStyles.Bold;
            _listenerText.alignment = TextAlignmentOptions.Right;
            _listenerText.textWrappingMode = TextWrappingModes.NoWrap;

            // Listener change (+/-)
            GameObject changeObj = new GameObject("ListenerChange");
            changeObj.transform.SetParent(listenerContainer.transform, false);
            _listenerChangeText = changeObj.AddComponent<TextMeshProUGUI>();
            _listenerChangeText.text = "";
            _listenerChangeText.fontSize = UITheme.FontSizeSmall;
            _listenerChangeText.color = UITheme.AccentGreen;
            _listenerChangeText.alignment = TextAlignmentOptions.Left;
            _listenerChangeText.textWrappingMode = TextWrappingModes.NoWrap;
            UITheme.AddLayoutElement(changeObj, minWidth: 50f);

            // Hide live indicator initially
            _liveIndicator.transform.parent.gameObject.SetActive(false);
        }

        private void Start()
        {
            _gameState = GameStateManager.Instance;
            _timeManager = TimeManager.Instance;
            _listenerManager = ListenerManager.Instance;

            if (_gameState != null)
            {
                _gameState.OnPhaseChanged += OnPhaseChanged;
                _gameState.OnNightStarted += OnNightStarted;
                UpdatePhaseDisplay();
                UpdateNightDisplay();
            }

            if (_timeManager != null)
            {
                _timeManager.OnTick += OnTick;
                UpdateTimeDisplay();
            }

            if (_listenerManager != null)
            {
                _listenerManager.OnListenersChanged += OnListenersChanged;
                UpdateListenerDisplay();
            }
        }

        private void OnDestroy()
        {
            if (_gameState != null)
            {
                _gameState.OnPhaseChanged -= OnPhaseChanged;
                _gameState.OnNightStarted -= OnNightStarted;
            }

            if (_timeManager != null)
            {
                _timeManager.OnTick -= OnTick;
            }

            if (_listenerManager != null)
            {
                _listenerManager.OnListenersChanged -= OnListenersChanged;
            }
        }

        private void OnPhaseChanged(GamePhase oldPhase, GamePhase newPhase)
        {
            UpdatePhaseDisplay();
        }

        private void OnNightStarted(int nightNumber)
        {
            UpdateNightDisplay();
        }

        private void OnTick(float deltaTime)
        {
            UpdateTimeDisplay();
        }

        private void UpdatePhaseDisplay()
        {
            if (_gameState == null) return;

            GamePhase phase = _gameState.CurrentPhase;
            bool isLive = phase == GamePhase.LiveShow;

            _phaseText.text = phase switch
            {
                GamePhase.PreShow => "PRE-SHOW",
                GamePhase.LiveShow => "ON AIR",
                GamePhase.PostShow => "POST-SHOW",
                _ => "UNKNOWN"
            };

            _phaseText.color = phase switch
            {
                GamePhase.PreShow => UITheme.AccentYellow,
                GamePhase.LiveShow => UITheme.AccentRed,
                GamePhase.PostShow => UITheme.AccentGreen,
                _ => UITheme.TextWhite
            };

            // Show/hide live indicator
            _liveIndicator.transform.parent.gameObject.SetActive(isLive);
        }

        private void UpdateNightDisplay()
        {
            if (_gameState == null) return;
            _nightText.text = $"NIGHT {_gameState.CurrentNight}";
        }

        private void UpdateTimeDisplay()
        {
            if (_timeManager == null) return;

            _clockText.text = _timeManager.CurrentTimeFormatted;
            _remainingText.text = $"({_timeManager.RemainingTimeFormatted} left)";
        }

        private void OnListenersChanged(int oldCount, int newCount)
        {
            UpdateListenerDisplay();
        }

        private void UpdateListenerDisplay()
        {
            if (_listenerManager == null || _listenerText == null) return;

            _listenerText.text = _listenerManager.GetFormattedListeners();

            int change = _listenerManager.ListenerChange;
            if (change != 0)
            {
                _listenerChangeText.text = _listenerManager.GetFormattedChange();
                _listenerChangeText.color = change >= 0 ? UITheme.AccentGreen : UITheme.AccentRed;
            }
            else
            {
                _listenerChangeText.text = "";
            }
        }

        private float _blinkTimer;

        private void Update()
        {
            // Blink the live indicator
            if (_liveIndicator != null && _liveIndicator.transform.parent.gameObject.activeSelf)
            {
                _blinkTimer += Time.deltaTime;
                float alpha = (Mathf.Sin(_blinkTimer * 4f) + 1f) / 2f;
                Color c = _liveIndicator.color;
                c.a = Mathf.Lerp(0.3f, 1f, alpha);
                _liveIndicator.color = c;
            }
        }
    }
}
