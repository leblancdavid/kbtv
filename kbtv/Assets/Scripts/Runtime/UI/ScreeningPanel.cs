using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KBTV.Callers;
using KBTV.Audio;

namespace KBTV.UI
{
    /// <summary>
    /// Panel for screening the current caller.
    /// Shows caller details and approve/reject buttons.
    /// </summary>
    public class ScreeningPanel : MonoBehaviour
    {
        private TextMeshProUGUI _headerText;
        private CallerCardUI _callerCard;
        private GameObject _callerContainer;
        private GameObject _emptyState;
        private Button _screenNextButton;
        private Button _approveButton;
        private Button _rejectButton;

        private CallerQueue _callerQueue;

        /// <summary>
        /// Create and initialize a ScreeningPanel on the given parent.
        /// </summary>
        public static ScreeningPanel Create(Transform parent)
        {
            GameObject panelObj = UITheme.CreatePanel("ScreeningPanel", parent, UITheme.PanelBackground);

            UITheme.AddVerticalLayout(panelObj, padding: UITheme.PanelPadding, spacing: 8f);

            ScreeningPanel panel = panelObj.AddComponent<ScreeningPanel>();
            panel.BuildUI();

            return panel;
        }

        private void BuildUI()
        {
            // Header
            _headerText = UITheme.CreateText("Header", transform, "CALLER SCREENING",
                UITheme.FontSizeLarge, UITheme.TextAmber, TextAlignmentOptions.Left);
            _headerText.fontStyle = FontStyles.Bold;
            UITheme.AddLayoutElement(_headerText.gameObject, preferredHeight: 25f);

            // Divider
            CreateDivider();

            // Caller container (shown when screening)
            _callerContainer = new GameObject("CallerContainer");
            _callerContainer.transform.SetParent(transform, false);
            RectTransform containerRect = _callerContainer.AddComponent<RectTransform>();
            UITheme.AddVerticalLayout(_callerContainer, padding: 0f, spacing: 10f);
            UITheme.AddLayoutElement(_callerContainer, flexibleHeight: 1f);

            // Caller card
            _callerCard = CallerCardUI.CreateDetailed(_callerContainer.transform);
            UITheme.AddLayoutElement(_callerCard.gameObject, flexibleHeight: 1f);

            // Button container
            GameObject buttonContainer = new GameObject("Buttons");
            buttonContainer.transform.SetParent(_callerContainer.transform, false);
            UITheme.AddHorizontalLayout(buttonContainer, spacing: 10f);
            UITheme.AddLayoutElement(buttonContainer, preferredHeight: UITheme.ButtonHeight + 10f);

            // Approve button
            _approveButton = UITheme.CreateButton("ApproveBtn", buttonContainer.transform, "APPROVE",
                UITheme.AccentGreen, UITheme.TextWhite);
            UITheme.AddLayoutElement(_approveButton.gameObject, flexibleWidth: 1f, preferredHeight: UITheme.ButtonHeight);
            _approveButton.onClick.AddListener(OnApproveClicked);

            // Reject button
            _rejectButton = UITheme.CreateButton("RejectBtn", buttonContainer.transform, "REJECT",
                UITheme.AccentRed, UITheme.TextWhite);
            UITheme.AddLayoutElement(_rejectButton.gameObject, flexibleWidth: 1f, preferredHeight: UITheme.ButtonHeight);
            _rejectButton.onClick.AddListener(OnRejectClicked);

            // Empty state (shown when no one is being screened)
            _emptyState = new GameObject("EmptyState");
            _emptyState.transform.SetParent(transform, false);
            UITheme.AddVerticalLayout(_emptyState, padding: 20f, spacing: 10f, childForceExpand: true);
            UITheme.AddLayoutElement(_emptyState, flexibleHeight: 1f);

            // Empty message
            TextMeshProUGUI emptyMsg = UITheme.CreateText("EmptyMsg", _emptyState.transform,
                "No caller being screened",
                UITheme.FontSizeNormal, UITheme.TextGray, TextAlignmentOptions.Center);
            UITheme.AddLayoutElement(emptyMsg.gameObject, preferredHeight: 30f);

            // Screen next button
            _screenNextButton = UITheme.CreateButton("ScreenNextBtn", _emptyState.transform, "SCREEN NEXT CALLER",
                UITheme.PanelBorder, UITheme.TextPrimary);
            UITheme.AddLayoutElement(_screenNextButton.gameObject, preferredHeight: UITheme.ButtonHeight);
            _screenNextButton.onClick.AddListener(OnScreenNextClicked);

            // Initially show empty state
            _callerContainer.SetActive(false);
            _emptyState.SetActive(true);
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
                _callerQueue.OnCallerAdded += OnCallerQueueChanged;
                _callerQueue.OnCallerRemoved += OnCallerQueueChanged;
                _callerQueue.OnCallerDisconnected += OnCallerQueueChanged;
            }

            UpdateDisplay();
        }

        private void OnDestroy()
        {
            if (_callerQueue != null)
            {
                _callerQueue.OnCallerAdded -= OnCallerQueueChanged;
                _callerQueue.OnCallerRemoved -= OnCallerQueueChanged;
                _callerQueue.OnCallerDisconnected -= OnCallerQueueChanged;
            }
        }

        private void Update()
        {
            // Check if screening state changed
            bool hasScreening = _callerQueue != null && _callerQueue.CurrentScreening != null;
            
            if (hasScreening != _callerContainer.activeSelf)
            {
                UpdateDisplay();
            }
        }

        private void OnCallerQueueChanged(Caller caller)
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_callerQueue == null) return;

            Caller screening = _callerQueue.CurrentScreening;
            bool hasScreening = screening != null;

            _callerContainer.SetActive(hasScreening);
            _emptyState.SetActive(!hasScreening);

            if (hasScreening)
            {
                _callerCard.SetCaller(screening);
            }
            else
            {
                _callerCard.Clear();
            }

            // Update screen next button state
            _screenNextButton.interactable = _callerQueue.HasIncomingCallers;
        }

        private void OnApproveClicked()
        {
            if (_callerQueue != null)
            {
                _callerQueue.ApproveCurrentCaller();
                AudioManager.Instance?.PlayCallerDecision(true);
                UpdateDisplay();
            }
        }

        private void OnRejectClicked()
        {
            if (_callerQueue != null)
            {
                _callerQueue.RejectCurrentCaller();
                AudioManager.Instance?.PlayCallerDecision(false);
                UpdateDisplay();
            }
        }

        private void OnScreenNextClicked()
        {
            if (_callerQueue != null && _callerQueue.HasIncomingCallers)
            {
                _callerQueue.StartScreeningNext();
                AudioManager.Instance?.PlayButtonClick();
                UpdateDisplay();
            }
        }
    }
}
