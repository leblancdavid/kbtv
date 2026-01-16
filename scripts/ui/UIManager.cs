using System.Threading.Tasks;
using Godot;
using KBTV.Core;

namespace KBTV.UI
{
    /// <summary>
    /// Manages UI layer visibility based on game phases.
    /// Handles the CanvasLayer-based UI architecture for proper draw ordering.
    /// </summary>
    public partial class UIManager : Node, IUIManager
    {
        private CanvasLayer _preShowLayer;
        private CanvasLayer _liveShowLayer;
        private bool _gameStateConnected;
        private GameStateManager _gameState;
        private bool _isTransitioning;

        public void RegisterPreShowLayer(CanvasLayer layer)
        {
            _preShowLayer = layer;
            GD.Print($"UIManager: PreShow layer registered: {layer != null}");
            TryUpdateVisibility();
        }

        public void RegisterLiveShowLayer(CanvasLayer layer)
        {
            _liveShowLayer = layer;
            GD.Print($"UIManager: LiveShow layer registered: {layer != null}");
            TryUpdateVisibility();
        }

        public override void _Ready()
        {
            ServiceRegistry.Instance.RegisterSelf<UIManager>(this);
            CallDeferred(nameof(CompleteInitialization));
        }

        private void CompleteInitialization()
        {
            _gameState = ServiceRegistry.Instance.GameStateManager;
            if (_gameState != null)
            {
                _gameState.Connect("PhaseChanged", Callable.From<int, int>(OnPhaseChanged));
                _gameStateConnected = true;
                TryUpdateVisibility();
                GD.Print("UIManager: Initialization complete");
            }
            else
            {
                GD.PrintErr("UIManager: GameStateManager not available after all services ready");
            }
        }

        private void TryUpdateVisibility()
        {
            if (_preShowLayer == null)
            {
                GD.Print("UIManager: PreShow layer not yet registered, visibility update deferred");
                return;
            }

            if (_gameState != null)
            {
                PerformVisibilityUpdate(_gameState.CurrentPhase);
            }
        }

        private void OnPhaseChanged(int oldPhaseInt, int newPhaseInt)
        {
            var newPhase = (GamePhase)newPhaseInt;
            GD.Print($"UIManager: Phase changed to {newPhase}");

            if (newPhase == GamePhase.PreShow && _preShowLayer == null)
            {
                CallDeferred(nameof(TryUpdateVisibility));
                return;
            }

            if (newPhase == GamePhase.LiveShow && _liveShowLayer == null)
            {
                GD.PrintErr("UIManager: Cannot switch to LiveShow - LiveShow layer not registered");
                CallDeferred(nameof(TryUpdateVisibility));
                return;
            }

            if (_isTransitioning)
            {
                GD.Print("UIManager: Already transitioning, skipping");
                return;
            }

            CallDeferred(nameof(PerformTransitionAsync), (int)newPhase);
        }

        private async void PerformTransitionAsync(int newPhaseInt)
        {
            var newPhase = (GamePhase)newPhaseInt;
            _isTransitioning = true;

            var transitionManager = ServiceRegistry.Instance?.GlobalTransitionManager;
            if (transitionManager != null)
            {
                await transitionManager.FadeToBlack(0.3f);
            }

            PerformVisibilityUpdateInstant(newPhase);

            if (transitionManager != null)
            {
                await transitionManager.FadeFromBlack(0.3f);
            }

            _isTransitioning = false;
        }

        private void PerformVisibilityUpdateInstant(GamePhase newPhase)
        {
            GD.Print($"UIManager: Before update - PreShow visible: {_preShowLayer.Visible}, LiveShow visible: {_liveShowLayer.Visible}");

            switch (newPhase)
            {
                case GamePhase.PreShow:
                    _preShowLayer.Show();
                    _liveShowLayer.Hide();
                    GD.Print("UIManager: PreShow UI visible, LiveShow UI hidden");
                    break;

                case GamePhase.LiveShow:
                    _preShowLayer.Hide();
                    _liveShowLayer.Show();
                    GD.Print("UIManager: PreShow UI hidden, LiveShow UI visible");
                    break;

                default:
                    GD.PrintErr($"UIManager: Unknown game phase {newPhase}");
                    break;
            }

            GD.Print($"UIManager: After update - PreShow visible: {_preShowLayer.Visible}, LiveShow visible: {_liveShowLayer.Visible}");
        }

        private void PerformVisibilityUpdate(GamePhase newPhase)
        {
            PerformVisibilityUpdateInstant(newPhase);
        }

        public override void _ExitTree()
        {
            if (_gameStateConnected && _gameState != null)
            {
                _gameState.Disconnect("PhaseChanged", Callable.From<int, int>(OnPhaseChanged));
            }
        }
    }
}