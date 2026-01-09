using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KBTV.Callers;

namespace KBTV.UI
{
    /// <summary>
    /// Panel displaying incoming and on-hold caller queues.
    /// </summary>
    public class CallerQueuePanel : BasePanel
    {
        private TextMeshProUGUI _incomingHeader;
        private Transform _incomingContainer;
        private TextMeshProUGUI _onHoldHeader;
        private Transform _onHoldContainer;

        private CallerQueue _callerQueue;
        private List<CallerQueueEntry> _incomingEntries = new List<CallerQueueEntry>();
        private List<CallerQueueEntry> _onHoldEntries = new List<CallerQueueEntry>();

        private const int MaxVisibleEntries = 5;

        /// <summary>
        /// Create and initialize a CallerQueuePanel on the given parent.
        /// </summary>
        public static CallerQueuePanel Create(Transform parent)
        {
            GameObject panelObj = UITheme.CreatePanel("CallerQueuePanel", parent, UITheme.PanelBackground);

            UITheme.AddVerticalLayout(panelObj, padding: UITheme.PanelPadding, spacing: 8f);

            CallerQueuePanel panel = panelObj.AddComponent<CallerQueuePanel>();
            panel.BuildUI();

            return panel;
        }

        private void BuildUI()
        {
            // Incoming section
            _incomingHeader = UITheme.CreateText("IncomingHeader", transform, "INCOMING (0)",
                UITheme.FontSizeNormal, UITheme.TextAmber, TextAlignmentOptions.Left);
            _incomingHeader.fontStyle = FontStyles.Bold;
            UITheme.AddLayoutElement(_incomingHeader.gameObject, preferredHeight: 20f);

            GameObject incomingScrollContainer = new GameObject("IncomingScroll");
            incomingScrollContainer.transform.SetParent(transform, false);
            UITheme.AddLayoutElement(incomingScrollContainer, flexibleHeight: 1f, minHeight: 80f);

            // Create scroll view for incoming
            CreateScrollView(incomingScrollContainer, out _incomingContainer);

            // Divider
            CreateDivider();

            // On-hold section
            _onHoldHeader = UITheme.CreateText("OnHoldHeader", transform, "ON HOLD (0/3)",
                UITheme.FontSizeNormal, UITheme.AccentGreen, TextAlignmentOptions.Left);
            _onHoldHeader.fontStyle = FontStyles.Bold;
            UITheme.AddLayoutElement(_onHoldHeader.gameObject, preferredHeight: 20f);

            GameObject onHoldScrollContainer = new GameObject("OnHoldScroll");
            onHoldScrollContainer.transform.SetParent(transform, false);
            UITheme.AddLayoutElement(onHoldScrollContainer, flexibleHeight: 1f, minHeight: 60f);

            // Create scroll view for on-hold
            CreateScrollView(onHoldScrollContainer, out _onHoldContainer);
        }

        private void CreateScrollView(GameObject container, out Transform contentTransform)
        {
            // Simple vertical list (no actual scroll for now - just overflow)
            Image bg = container.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

            GameObject content = new GameObject("Content");
            content.transform.SetParent(container.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();
            UITheme.FillParent(contentRect, 4f);

            UITheme.AddVerticalLayout(content, padding: 4f, spacing: 4f);

            // Add ContentSizeFitter for dynamic height
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            contentTransform = content.transform;
        }

        private void CreateDivider()
        {
            UITheme.CreateDivider(transform);
        }

        protected override bool DoSubscribe()
        {
            _callerQueue = CallerQueue.Instance;
            if (_callerQueue == null) return false;

            _callerQueue.OnCallerAdded += OnCallerChanged;
            _callerQueue.OnCallerRemoved += OnCallerChanged;
            _callerQueue.OnCallerDisconnected += OnCallerChanged;
            _callerQueue.OnCallerOnAir += OnCallerChanged;
            _callerQueue.OnCallerCompleted += OnCallerChanged;
            Debug.Log("CallerQueuePanel: Subscribed to CallerQueue events");
            return true;
        }

        protected override void DoUnsubscribe()
        {
            if (_callerQueue != null)
            {
                _callerQueue.OnCallerAdded -= OnCallerChanged;
                _callerQueue.OnCallerRemoved -= OnCallerChanged;
                _callerQueue.OnCallerDisconnected -= OnCallerChanged;
                _callerQueue.OnCallerOnAir -= OnCallerChanged;
                _callerQueue.OnCallerCompleted -= OnCallerChanged;
            }
        }

        private void OnCallerChanged(Caller caller)
        {
            RefreshLists();
        }

        protected override void Update()
        {
            base.Update();  // Handles subscription retry

            // Periodically check for queue changes (backup for missed events)
            if (_callerQueue != null)
            {
                int incomingCount = _callerQueue.IncomingCallers.Count;
                int onHoldCount = _callerQueue.OnHoldCallers.Count;

                if (incomingCount != _incomingEntries.Count || onHoldCount != _onHoldEntries.Count)
                {
                    RefreshLists();
                }
            }
        }

        protected override void UpdateDisplay()
        {
            RefreshLists();
        }

        private void RefreshLists()
        {
            if (_callerQueue == null) return;

            // Update headers
            _incomingHeader.text = $"INCOMING ({_callerQueue.IncomingCallers.Count})";
            _onHoldHeader.text = $"ON HOLD ({_callerQueue.OnHoldCallers.Count}/3)";

            // Clear existing entries
            ClearEntries(_incomingEntries, _incomingContainer);
            ClearEntries(_onHoldEntries, _onHoldContainer);

            // Populate incoming
            foreach (var caller in _callerQueue.IncomingCallers)
            {
                CallerQueueEntry entry = CallerQueueEntry.Create(_incomingContainer, caller, showPatience: true);
                _incomingEntries.Add(entry);
            }

            // Populate on-hold
            foreach (var caller in _callerQueue.OnHoldCallers)
            {
                CallerQueueEntry entry = CallerQueueEntry.Create(_onHoldContainer, caller, showPatience: true);
                _onHoldEntries.Add(entry);
            }

            // Show empty message if needed
            if (_callerQueue.IncomingCallers.Count == 0)
            {
                CreateEmptyMessage(_incomingContainer, "No incoming calls");
            }

            if (_callerQueue.OnHoldCallers.Count == 0)
            {
                CreateEmptyMessage(_onHoldContainer, "No callers on hold");
            }
        }

        private void ClearEntries(List<CallerQueueEntry> entries, Transform container)
        {
            foreach (var entry in entries)
            {
                if (entry != null && entry.gameObject != null)
                {
                    Destroy(entry.gameObject);
                }
            }
            entries.Clear();

            // Also destroy any empty message
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        private void CreateEmptyMessage(Transform container, string message)
        {
            TextMeshProUGUI emptyText = UITheme.CreateText("Empty", container, message,
                UITheme.FontSizeSmall, UITheme.TextDim, TextAlignmentOptions.Center);
            UITheme.AddLayoutElement(emptyText.gameObject, preferredHeight: 20f);
        }
    }
}
