using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using KBTV.Core;

namespace KBTV.UI
{
    /// <summary>
    /// Main controller for the Live Show UI.
    /// Creates and manages the entire UI hierarchy at runtime.
    /// Shows/hides based on game phase.
    /// </summary>
    public class LiveShowUIManager : MonoBehaviour
    {
        public static LiveShowUIManager Instance { get; private set; }

        private Canvas _canvas;
        private CanvasScaler _scaler;
        private GameObject _rootPanel;

        private HeaderBarUI _headerBar;
        private VernStatsPanel _statsPanel;
        private ItemPanel _itemPanel;
        private ScreeningPanel _screeningPanel;
        private OnAirPanel _onAirPanel;
        private CallerQueuePanel _callerQueuePanel;

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
                UpdateVisibility();
            }
        }

        private void OnDestroy()
        {
            if (_gameState != null)
            {
                _gameState.OnPhaseChanged -= OnPhaseChanged;
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

            // Ensure EventSystem exists for UI interactions
            EnsureEventSystem();

            // Create root panel (dark background)
            _rootPanel = UITheme.CreatePanel("RootPanel", transform, UITheme.BackgroundDark);
            RectTransform rootRect = _rootPanel.GetComponent<RectTransform>();
            UITheme.FillParent(rootRect);

            // Main layout - vertical: header + content
            UITheme.AddVerticalLayout(_rootPanel, padding: 0f, spacing: 0f);

            // Header bar (top)
            _headerBar = HeaderBarUI.Create(_rootPanel.transform);

            // Content area (below header)
            GameObject contentArea = new GameObject("ContentArea");
            contentArea.transform.SetParent(_rootPanel.transform, false);
            RectTransform contentRect = contentArea.AddComponent<RectTransform>();
            UITheme.AddLayoutElement(contentArea, flexibleHeight: 1f);

            // Two-column layout for content
            UITheme.AddHorizontalLayout(contentArea, padding: UITheme.PanelPadding, spacing: UITheme.PanelPadding);

            // Left column (stats + items)
            GameObject leftColumn = CreateColumn(contentArea.transform, "LeftColumn", 350f);
            _statsPanel = VernStatsPanel.Create(leftColumn.transform);
            UITheme.AddLayoutElement(_statsPanel.gameObject, flexibleHeight: 1f, minHeight: 300f);

            // Item panel below stats
            _itemPanel = ItemPanel.Create(leftColumn.transform);
            UITheme.AddLayoutElement(_itemPanel.gameObject, preferredHeight: 280f, minHeight: 200f);

            // Right column (caller management)
            GameObject rightColumn = CreateColumn(contentArea.transform, "RightColumn", -1f); // Flexible width
            UITheme.AddLayoutElement(rightColumn, flexibleWidth: 1f, minWidth: 400f);

            // Note: CreateColumn already adds a VerticalLayoutGroup

            // Top section: Screening + On Air side by side
            GameObject topSection = new GameObject("TopSection");
            topSection.transform.SetParent(rightColumn.transform, false);
            topSection.AddComponent<RectTransform>();
            UITheme.AddHorizontalLayout(topSection, spacing: UITheme.PanelPadding);
            UITheme.AddLayoutElement(topSection, flexibleHeight: 1f, minHeight: 250f);

            // Screening panel
            _screeningPanel = ScreeningPanel.Create(topSection.transform);
            UITheme.AddLayoutElement(_screeningPanel.gameObject, flexibleWidth: 1f, minWidth: 200f);

            // On-Air panel
            _onAirPanel = OnAirPanel.Create(topSection.transform);
            UITheme.AddLayoutElement(_onAirPanel.gameObject, flexibleWidth: 1f, minWidth: 200f);

            // Bottom section: Caller queues
            _callerQueuePanel = CallerQueuePanel.Create(rightColumn.transform);
            UITheme.AddLayoutElement(_callerQueuePanel.gameObject, preferredHeight: 200f, minHeight: 150f, flexibleHeight: 0.5f);

            Debug.Log("LiveShowUIManager: UI created successfully");
        }

        private GameObject CreateColumn(Transform parent, string name, float width)
        {
            GameObject column = new GameObject(name);
            column.transform.SetParent(parent, false);

            RectTransform rect = column.AddComponent<RectTransform>();

            if (width > 0)
            {
                UITheme.AddLayoutElement(column, preferredWidth: width, minWidth: width);
            }

            UITheme.AddVerticalLayout(column, padding: 0f, spacing: UITheme.PanelPadding);

            return column;
        }

        private void OnPhaseChanged(GamePhase oldPhase, GamePhase newPhase)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (_gameState == null) return;

            // Show UI during all phases for now (can restrict to LiveShow only later)
            bool showUI = true; // _gameState.CurrentPhase == GamePhase.LiveShow;
            
            if (_rootPanel != null)
            {
                _rootPanel.SetActive(showUI);
            }
        }

        /// <summary>
        /// Show the Live Show UI.
        /// </summary>
        public void Show()
        {
            if (_rootPanel != null)
            {
                _rootPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Hide the Live Show UI.
        /// </summary>
        public void Hide()
        {
            if (_rootPanel != null)
            {
                _rootPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Ensure an EventSystem exists in the scene for UI interactions.
        /// </summary>
        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
                Debug.Log("LiveShowUIManager: Created EventSystem");
            }
        }
    }
}
