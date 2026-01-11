using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using KBTV.Core;
using KBTV.Callers;

namespace KBTV.UI
{
    /// <summary>
    /// Main controller for the PreShow UI.
    /// Displays topic selection and Start Show button.
    /// Shows only during PreShow phase.
    /// </summary>
    public class PreShowUIManager : MonoBehaviour
    {
        public static PreShowUIManager Instance { get; private set; }

        private Canvas _canvas;
        private CanvasScaler _scaler;
        private GameObject _rootPanel;

        private TextMeshProUGUI _headerText;
        private TopicSelectionPanel _topicPanel;
        private Button _startShowButton;
        private TextMeshProUGUI _startShowButtonText;

        private GameStateManager _gameState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateUI();
        }

        private void Start()
        {
            _gameState = GameStateManager.Instance;

            if (_gameState != null)
            {
                _gameState.OnPhaseChanged += OnPhaseChanged;
                _gameState.OnNightStarted += OnNightStarted;
                UpdateVisibility();
                UpdateHeader();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (_gameState != null)
            {
                _gameState.OnPhaseChanged -= OnPhaseChanged;
                _gameState.OnNightStarted -= OnNightStarted;
            }

            if (_topicPanel != null)
            {
                _topicPanel.OnTopicSelected -= OnTopicSelected;
            }
        }

        private void CreateUI()
        {
            // Create Canvas
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            // Add CanvasScaler for resolution independence
            _scaler = gameObject.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920f, 1080f);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _scaler.matchWidthOrHeight = 0.5f;

            // Add GraphicRaycaster for button interactions
            gameObject.AddComponent<GraphicRaycaster>();

            // Ensure EventSystem exists
            UITheme.EnsureEventSystem();

            // Create root panel (dark background)
            _rootPanel = UITheme.CreatePanel("RootPanel", transform, UITheme.BackgroundDark);
            RectTransform rootRect = _rootPanel.GetComponent<RectTransform>();
            UITheme.FillParent(rootRect);

            // Main layout - vertical with center alignment
            VerticalLayoutGroup rootLayout = UITheme.AddVerticalLayout(_rootPanel, padding: 50f, spacing: 20f);
            rootLayout.childAlignment = TextAnchor.UpperCenter;

            // Header
            _headerText = UITheme.CreateText("Header", _rootPanel.transform, "NIGHT 1 - PRE-SHOW",
                UITheme.FontSizeHeader, UITheme.TextAmber, TextAlignmentOptions.Center);
            UITheme.AddLayoutElement(_headerText.gameObject, preferredHeight: 50f);

            // Spacer
            CreateSpacer(_rootPanel.transform, 20f);

            // Center content container (constrained width)
            GameObject centerContainer = new GameObject("CenterContainer");
            centerContainer.transform.SetParent(_rootPanel.transform, false);
            centerContainer.AddComponent<RectTransform>();
            UITheme.AddLayoutElement(centerContainer, preferredWidth: 600f, flexibleHeight: 1f);
            UITheme.AddVerticalLayout(centerContainer, padding: 0f, spacing: 20f);

            // Topic selection panel
            _topicPanel = TopicSelectionPanel.Create(centerContainer.transform);
            UITheme.AddLayoutElement(_topicPanel.gameObject, preferredHeight: 350f, flexibleHeight: 0f);
            _topicPanel.OnTopicSelected += OnTopicSelected;

            // Spacer
            CreateSpacer(centerContainer.transform, 20f);

            // Start Show button
            _startShowButton = UITheme.CreateButton("StartShowButton", centerContainer.transform, 
                "START SHOW", UITheme.AccentGreen, UITheme.TextWhite);
            _startShowButton.onClick.AddListener(OnStartShowClicked);
            
            RectTransform buttonRect = _startShowButton.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(250f, 60f);
            UITheme.AddLayoutElement(_startShowButton.gameObject, preferredWidth: 250f, preferredHeight: 60f);

            // Get button text reference for updating
            _startShowButtonText = _startShowButton.GetComponentInChildren<TextMeshProUGUI>();
            if (_startShowButtonText != null)
            {
                _startShowButtonText.fontSize = UITheme.FontSizeLarge;
            }

            // Initially disable until topic selected
            UpdateStartShowButton();

            // Bottom spacer (flexible to push content up)
            GameObject bottomSpacer = new GameObject("BottomSpacer");
            bottomSpacer.transform.SetParent(_rootPanel.transform, false);
            bottomSpacer.AddComponent<RectTransform>();
            UITheme.AddLayoutElement(bottomSpacer, flexibleHeight: 1f);
        }

        private void CreateSpacer(Transform parent, float height)
        {
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(parent, false);
            spacer.AddComponent<RectTransform>();
            UITheme.AddLayoutElement(spacer, preferredHeight: height);
        }

        private void OnPhaseChanged(GamePhase oldPhase, GamePhase newPhase)
        {
            UpdateVisibility();
        }

        private void OnNightStarted(int nightNumber)
        {
            UpdateHeader();
        }

        private void OnTopicSelected(Topic topic)
        {
            UpdateStartShowButton();
        }

        private void OnStartShowClicked()
        {
            if (_topicPanel.SelectedTopic == null)
            {
                Debug.LogWarning("PreShowUIManager: Cannot start show without selecting a topic");
                return;
            }

            // Play button sound
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlayButtonClick();
            }

            // Advance to LiveShow
            if (_gameState != null)
            {
                _gameState.AdvancePhase();
            }
        }

        private void UpdateVisibility()
        {
            if (_gameState == null) return;

            bool showUI = _gameState.CurrentPhase == GamePhase.PreShow;
            
            if (_rootPanel != null)
            {
                _rootPanel.SetActive(showUI);
            }
        }

        private void UpdateHeader()
        {
            if (_headerText != null && _gameState != null)
            {
                _headerText.text = $"NIGHT {_gameState.CurrentNight} - PRE-SHOW";
            }
        }

        private void UpdateStartShowButton()
        {
            if (_startShowButton == null) return;

            bool hasTopicSelected = _topicPanel != null && _topicPanel.SelectedTopic != null;
            _startShowButton.interactable = hasTopicSelected;

            // Update button appearance
            Image buttonImage = _startShowButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = hasTopicSelected ? UITheme.AccentGreen : UITheme.PanelBorder;
            }

            if (_startShowButtonText != null)
            {
                _startShowButtonText.color = hasTopicSelected ? UITheme.TextWhite : UITheme.TextGray;
            }
        }

        /// <summary>
        /// Show the PreShow UI.
        /// </summary>
        public void Show()
        {
            if (_rootPanel != null)
            {
                _rootPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Hide the PreShow UI.
        /// </summary>
        public void Hide()
        {
            if (_rootPanel != null)
            {
                _rootPanel.SetActive(false);
            }
        }
    }
}
