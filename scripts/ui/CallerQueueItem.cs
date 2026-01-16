using Godot;
using KBTV.Callers;

namespace KBTV.UI
{
    public partial class CallerQueueItem : Panel
    {
        private Caller _caller;
        private Label _nameLabel;
        private ProgressBar _statusIndicator;
        private Button _selectButton;

        public override void _Ready()
        {
            _nameLabel = GetNode<Label>("HBoxContainer/NameLabel");
            _statusIndicator = GetNode<ProgressBar>("HBoxContainer/StatusIndicator");
            _selectButton = GetNode<Button>("HBoxContainer/SelectButton");

            _selectButton.Pressed += OnSelectPressed;
        }

        public override void _Process(double delta)
        {
            if (_caller != null)
            {
                UpdateStatusIndicator();
            }
        }

        public void SetCaller(Caller caller)
        {
            _caller = caller;
            CallDeferred(nameof(_ApplyCallerName), caller?.Name ?? "");
        }

        private void _ApplyCallerName(string name)
        {
            if (_nameLabel != null)
            {
                _nameLabel.Text = name;
            }
            UpdateStatusIndicator();
        }

        private void UpdateStatusIndicator()
        {
            if (_caller == null || _statusIndicator == null) return;

            // Calculate remaining patience
            float remainingPatience = _caller.Patience - _caller.WaitTime;
            float patienceRatio = Mathf.Clamp(remainingPatience / _caller.Patience, 0f, 1f);

            _statusIndicator.Value = patienceRatio;

            // Color coding based on patience level
            Color color;
            if (patienceRatio > 0.66f)
            {
                color = new Color(0f, 1f, 0f); // Green
            }
            else if (patienceRatio > 0.33f)
            {
                color = new Color(1f, 1f, 0f); // Yellow
            }
            else
            {
                color = new Color(1f, 0f, 0f); // Red
            }

            _statusIndicator.AddThemeColorOverride("fill", color);
        }

        private void OnSelectPressed()
        {
            if (_caller != null && CallerQueue.Instance != null)
            {
                bool success = CallerQueue.Instance.StartScreeningCaller(_caller);
                if (success)
                {
                    GD.Print($"Selected caller {_caller.Name} for screening");
                    // UI refresh will be handled by the tab manager
                }
                else
                {
                    GD.PrintErr($"Failed to select caller {_caller.Name} for screening");
                }
            }
        }
    }
}