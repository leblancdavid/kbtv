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
            this.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            this.CustomMinimumSize = new Vector2(600, 200);

            CreateUI();
        }

    private void CreateUI()
    {
        // Set minimum size for visibility
        CustomMinimumSize = new Vector2(600, 200);
        SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;

        // Create vertical layout
        var vbox = new VBoxContainer();
        vbox.Name = "TopicSelectorVBox";
        vbox.AddThemeConstantOverride("separation", 10);
        AddChild(vbox);

        // Topic selector label
        var label = new Label();
        label.Name = "TopicLabel";
        label.Text = "Select Broadcast Topic";
        label.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(label);

        // Topic dropdown
        _topicSelector = new OptionButton();
        _topicSelector.Name = "TopicOptionButton";
        _topicSelector.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _topicSelector.ItemSelected += OnTopicSelected;
        vbox.AddChild(_topicSelector);

        // Description label
        _topicDescription = new Label();
        _topicDescription.Name = "DescriptionLabel";
        _topicDescription.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        _topicDescription.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _topicDescription.CustomMinimumSize = new Vector2(0, 60);
        vbox.AddChild(_topicDescription);

        // Populate topics
        PopulateTopics();

        GD.Print($"TopicSelector: Created with {_availableTopics.Count} topics, added {_topicSelector.GetItemCount()} items to OptionButton");
    }

    private void PopulateTopics()
    {
        if (_topicSelector == null || _availableTopics == null)
            return;

        _topicSelector.Clear();
        foreach (var topic in _availableTopics)
        {
            _topicSelector.AddItem(topic.DisplayName);
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