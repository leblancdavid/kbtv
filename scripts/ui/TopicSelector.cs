using System.Collections.Generic;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.UI;

namespace KBTV.UI
{
    /// <summary>
    /// UI component for selecting show topics.
    /// Extracted from PreShowUIManager to improve maintainability.
    /// </summary>
    public partial class TopicSelector : Control
    {
        private OptionButton _topicSelector;
        private Label _topicDescription;
        private List<Topic> _availableTopics;
        private Topic _selectedTopic;

        public TopicSelector(List<Topic> availableTopics)
        {
            _availableTopics = availableTopics;

            // Set proper size flags for container layout
            this.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            this.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

            CreateUI();
        }

        private void CreateUI()
        {
            var container = new VBoxContainer();
            container.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            container.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            container.CustomMinimumSize = new Vector2(0, 200);
            AddChild(container);

            var label = new Label();
            label.Text = "SELECT TOPIC FOR TONIGHT'S SHOW";
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            label.AddThemeColorOverride("font_color", UITheme.TEXT_PRIMARY);
            container.AddChild(label);

            _topicSelector = new OptionButton();
            _topicSelector.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            _topicSelector.CustomMinimumSize = new Vector2(300, 40);
            _topicSelector.ItemSelected += OnTopicSelected;
            UITheme.ApplyButtonStyle(_topicSelector);
            container.AddChild(_topicSelector);

            foreach (var topic in _availableTopics)
            {
                _topicSelector.AddItem(topic.DisplayName);
            }

            // Description area
            _topicDescription = new Label();
            _topicDescription.HorizontalAlignment = HorizontalAlignment.Center;
            _topicDescription.VerticalAlignment = VerticalAlignment.Center;
            _topicDescription.AutowrapMode = TextServer.AutowrapMode.Word;
            _topicDescription.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _topicDescription.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _topicDescription.AddThemeColorOverride("font_color", UITheme.TEXT_SECONDARY);
            _topicDescription.CustomMinimumSize = new Vector2(0, 80);
            container.AddChild(_topicDescription);

            // Initialize with first topic
            if (_availableTopics.Count > 0)
            {
                OnTopicSelected(0);
            }
        }

        private void OnTopicSelected(long index)
        {
            if (index >= 0 && index < _availableTopics.Count)
            {
                _selectedTopic = _availableTopics[(int)index];
                _topicDescription.Text = _selectedTopic.Description;
            }
        }

        // Public accessors
        public Topic SelectedTopic => _selectedTopic;
        public OptionButton SelectorButton => _topicSelector;
        public Label TopicDescription => _topicDescription;
    }
}