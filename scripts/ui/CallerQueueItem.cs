using Godot;
using KBTV.Callers;

namespace KBTV.UI
{
    public partial class CallerQueueItem : Panel
    {
        private Caller _caller;
        private Label _nameLabel;
        private ProgressBar _statusIndicator;

        public override void _Ready()
        {
            _nameLabel = GetNode<Label>("HBoxContainer/NameLabel");
            _statusIndicator = GetNode<ProgressBar>("HBoxContainer/StatusIndicator");

            GuiInput += OnGuiInput;

            if (CallerQueue.Instance != null)
            {
                CallerQueue.Instance.ScreeningChanged += OnScreeningChanged;
            }
        }

        private void OnScreeningChanged()
        {
            UpdateVisualSelection();
        }

        public override void _ExitTree()
        {
            if (CallerQueue.Instance != null)
            {
                CallerQueue.Instance.ScreeningChanged -= OnScreeningChanged;
            }
        }

        public override void _Process(double delta)
        {
            if (_caller != null)
            {
                UpdateStatusIndicator();
            }
        }

        private void OnGuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                OnItemClicked();
            }
        }

        private void OnItemClicked()
        {
            if (_caller != null && CallerQueue.Instance != null)
            {
                bool success = CallerQueue.Instance.ReplaceScreeningCaller(_caller);
                if (success)
                {
                    GD.Print($"Replaced screening with caller {_caller.Name}");
                }
                else
                {
                    GD.PrintErr($"Failed to replace screening caller {_caller.Name}");
                }
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
            UpdateVisualSelection();
        }

        private void UpdateVisualSelection()
        {
            if (_caller == null || _statusIndicator == null) return;

            bool isScreening = CallerQueue.Instance?.CurrentScreening == _caller;

            var style = GetThemeStylebox("panel") as StyleBoxFlat;
            if (style == null)
            {
                style = new StyleBoxFlat();
                style.CornerRadiusTopLeft = 4;
                style.CornerRadiusTopRight = 4;
                style.CornerRadiusBottomRight = 4;
                style.CornerRadiusBottomLeft = 4;
                style.BgColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            }

            if (isScreening)
            {
                style.BgColor = new Color(0.2f, 0.5f, 0.2f);
            }
            else
            {
                style.BgColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            }

            AddThemeStyleboxOverride("panel", style);
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
    }
}