using System;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Screening;
using KBTV.UI.Themes;

namespace KBTV.UI
{
    public interface ICallerActions
    {
        void OnApproveCaller();
        void OnRejectCaller();
    }

    public partial class CallerTabManager
    {
        private readonly ICallerRepository _repository;
        private readonly IScreeningController _screeningController;
        private readonly ICallerActions _callerActions;
        private ScreeningPanel _screeningPanel;
        private Control _screeningPanelContainer;

        public CallerTabManager(
            ICallerRepository repository,
            IScreeningController screeningController,
            ICallerActions callerActions)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _screeningController = screeningController ?? throw new ArgumentNullException(nameof(screeningController));
            _callerActions = callerActions ?? throw new ArgumentNullException(nameof(callerActions));
        }

        public void CreateScreeningPanel(Control panel)
        {
            GD.Print("CallerTabManager: Setting up screening panel");

            _screeningPanelContainer = panel;

            if (_screeningPanel == null)
            {
                CreateScreeningPanelInstance(panel);
            }
            else
            {
                UpdateScreeningPanelContent();
            }
        }

        private void CreateScreeningPanelInstance(Control panel)
        {
            try
            {
                var scenePath = "res://scenes/ui/ScreeningPanel.tscn";
                var scene = ResourceLoader.Load<PackedScene>(scenePath);
                GD.Print($"CallerTabManager: Screening scene loaded: {scene != null}");

                if (scene != null)
                {
                    _screeningPanel = scene.Instantiate<ScreeningPanel>();
                    GD.Print($"CallerTabManager: Screening panel instantiated: {_screeningPanel != null}");

                    if (_screeningPanel != null)
                    {
                        _screeningPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                        _screeningPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                        _screeningPanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                        _screeningPanel.CustomMinimumSize = Vector2.Zero;
                        GD.Print("CallerTabManager: Configured screening panel for full rect containment");

                        _screeningPanel.ConnectButtons(
                            Callable.From(() => _callerActions.OnApproveCaller()),
                            Callable.From(() => _callerActions.OnRejectCaller())
                        );

                        panel.AddChild(_screeningPanel);
                        UpdateScreeningPanelContent();
                        GD.Print("CallerTabManager: Screening panel creation successful");
                    }
                }
                else
                {
                    GD.PrintErr("CallerTabManager: Failed to load screening panel scene");
                    panel.AddChild(CreateErrorPanel("Failed to load screening panel"));
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"CallerTabManager.CreateScreeningPanel failed: {ex.Message}");
                panel.AddChild(CreateErrorPanel("Failed to create screening panel"));
            }
        }

        public void UpdateScreeningPanelContent()
        {
            if (_screeningPanel == null) return;

            _screeningPanel.SetCaller(_repository.CurrentScreening);
        }

        public void OnScreeningPanelCleared()
        {
            _screeningPanel = null;
        }

        public void Cleanup()
        {
            if (_screeningPanel != null)
            {
                _screeningPanel.QueueFree();
                _screeningPanel = null;
            }
            _screeningPanelContainer = null;
            GD.Print("CallerTabManager: Cleanup completed");
        }

        private Control CreateErrorPanel(string message)
        {
            var panel = new Panel();
            panel.Name = "ErrorPanel";

            var label = new Label
            {
                Text = message,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            panel.AddChild(label);

            UITheme.ApplyPanelStyle(panel);

            return panel;
        }
    }
}
