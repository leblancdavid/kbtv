using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KBTV.Callers;

namespace KBTV.UI
{
    /// <summary>
    /// Panel for selecting tonight's show topic during PreShow phase.
    /// Displays a grid of available topics with selection and description.
    /// </summary>
    public class TopicSelectionPanel : BasePanel
    {
        private Topic[] _topics;
        private Topic _selectedTopic;
        private Button[] _topicButtons;
        private Image[] _topicButtonImages;
        
        private TextMeshProUGUI _selectedTopicName;
        private TextMeshProUGUI _selectedTopicDescription;
        
        private const int COLUMNS = 3;
        private static readonly Color SelectedColor = UITheme.AccentGreen;
        private static readonly Color UnselectedColor = UITheme.PanelBorder;

        /// <summary>
        /// Event fired when a topic is selected. Parameter is the selected topic (or null if deselected).
        /// </summary>
        public event Action<Topic> OnTopicSelected;

        /// <summary>
        /// The currently selected topic.
        /// </summary>
        public Topic SelectedTopic => _selectedTopic;

        /// <summary>
        /// Create a new TopicSelectionPanel as a child of the given parent.
        /// </summary>
        public static TopicSelectionPanel Create(Transform parent)
        {
            GameObject panelObj = UITheme.CreatePanel("TopicSelectionPanel", parent, UITheme.PanelBackground);
            UITheme.AddVerticalLayout(panelObj, padding: UITheme.PanelPadding, spacing: UITheme.ElementSpacing);
            
            TopicSelectionPanel panel = panelObj.AddComponent<TopicSelectionPanel>();
            panel.CreateContent();
            
            return panel;
        }

        private void CreateContent()
        {
            // Title
            TextMeshProUGUI title = UITheme.CreateText("Title", transform, "SELECT TONIGHT'S TOPIC",
                UITheme.FontSizeLarge, UITheme.TextAmber, TextAlignmentOptions.Center);
            UITheme.AddLayoutElement(title.gameObject, preferredHeight: 30f);

            // Spacer
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(transform, false);
            spacer.AddComponent<RectTransform>();
            UITheme.AddLayoutElement(spacer, preferredHeight: 10f);

            // Grid container
            GameObject gridContainer = new GameObject("TopicGrid");
            gridContainer.transform.SetParent(transform, false);
            gridContainer.AddComponent<RectTransform>();
            
            GridLayoutGroup grid = gridContainer.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(180f, 50f);
            grid.spacing = new Vector2(10f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = COLUMNS;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(10, 10, 5, 5);
            
            UITheme.AddLayoutElement(gridContainer, preferredHeight: 180f, flexibleHeight: 0f);

            // Divider
            UITheme.CreateDivider(transform, 2f);

            // Selected topic info area
            GameObject infoArea = UITheme.CreatePanel("SelectedTopicInfo", transform, UITheme.BackgroundDark);
            UITheme.AddVerticalLayout(infoArea, padding: UITheme.PanelPadding, spacing: 5f);
            UITheme.AddLayoutElement(infoArea, preferredHeight: 100f, flexibleHeight: 1f);

            _selectedTopicName = UITheme.CreateText("SelectedName", infoArea.transform, "No topic selected",
                UITheme.FontSizeLarge, UITheme.TextPrimary, TextAlignmentOptions.Center);
            UITheme.AddLayoutElement(_selectedTopicName.gameObject, preferredHeight: 30f);

            _selectedTopicDescription = UITheme.CreateText("SelectedDescription", infoArea.transform, 
                "Select a topic above to see its description.",
                UITheme.FontSizeNormal, UITheme.TextGray, TextAlignmentOptions.Center);
            UITheme.AddLayoutElement(_selectedTopicDescription.gameObject, flexibleHeight: 1f);
        }

        protected override bool DoSubscribe()
        {
            // Get topics from GameBootstrap
            _topics = GameBootstrap.GetAvailableTopics();
            
            if (_topics == null || _topics.Length == 0)
            {
                Debug.LogWarning("TopicSelectionPanel: No topics available from GameBootstrap");
                return false;
            }

            CreateTopicButtons();
            return true;
        }

        protected override void DoUnsubscribe()
        {
            // No events to unsubscribe from
        }

        protected override void UpdateDisplay()
        {
            UpdateSelectedTopicDisplay();
        }

        private void CreateTopicButtons()
        {
            if (_topics == null) return;

            // Find the grid container
            Transform gridContainer = transform.Find("TopicGrid");
            if (gridContainer == null)
            {
                Debug.LogError("TopicSelectionPanel: Could not find TopicGrid");
                return;
            }

            _topicButtons = new Button[_topics.Length];
            _topicButtonImages = new Image[_topics.Length];

            for (int i = 0; i < _topics.Length; i++)
            {
                Topic topic = _topics[i];
                if (topic == null) continue;

                Button button = UITheme.CreateButton($"Topic_{topic.TopicId}", gridContainer, 
                    topic.DisplayName, UnselectedColor, UITheme.TextWhite);
                
                _topicButtons[i] = button;
                _topicButtonImages[i] = button.GetComponent<Image>();

                // Capture index for closure
                int index = i;
                button.onClick.AddListener(() => SelectTopic(index));
            }
        }

        private void SelectTopic(int index)
        {
            if (index < 0 || index >= _topics.Length) return;

            Topic topic = _topics[index];
            if (topic == null) return;

            _selectedTopic = topic;

            // Update button visuals
            for (int i = 0; i < _topicButtonImages.Length; i++)
            {
                if (_topicButtonImages[i] != null)
                {
                    _topicButtonImages[i].color = (i == index) ? SelectedColor : UnselectedColor;
                }
            }

            // Set topic on CallerScreeningManager
            if (CallerScreeningManager.Instance != null)
            {
                CallerScreeningManager.Instance.SetTopic(topic);
            }

            UpdateSelectedTopicDisplay();

            // Fire event
            OnTopicSelected?.Invoke(topic);

            // Play button sound
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlayButtonClick();
            }
        }

        private void UpdateSelectedTopicDisplay()
        {
            if (_selectedTopic != null)
            {
                _selectedTopicName.text = _selectedTopic.DisplayName;
                _selectedTopicName.color = UITheme.TextPrimary;
                _selectedTopicDescription.text = _selectedTopic.Description;
                _selectedTopicDescription.color = UITheme.TextWhite;
            }
            else
            {
                _selectedTopicName.text = "No topic selected";
                _selectedTopicName.color = UITheme.TextGray;
                _selectedTopicDescription.text = "Select a topic above to see its description.";
                _selectedTopicDescription.color = UITheme.TextGray;
            }
        }
    }
}
