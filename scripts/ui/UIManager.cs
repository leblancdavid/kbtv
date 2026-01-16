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
        private int _visibilityUpdateAttempts;
        private const int MAX_UPDATE_ATTEMPTS = 60;

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

            var gameState = ServiceRegistry.Instance?.GameStateManager;
            if (gameState != null)
            {
                gameState.Connect("PhaseChanged", Callable.From<int, int>(OnPhaseChanged));
                _gameStateConnected = true;
                TryUpdateVisibility();
            }
            else
            {
                GD.Print("UIManager: GameStateManager not available yet, deferring initialization");
                CallDeferred(nameof(DeferredInit));
            }
        }

        private void DeferredInit()
        {
            var gameState = ServiceRegistry.Instance?.GameStateManager;
            if (gameState != null)
            {
                gameState.Connect("PhaseChanged", Callable.From<int, int>(OnPhaseChanged));
                _gameStateConnected = true;
                TryUpdateVisibility();
            }
            else
            {
                GD.Print("UIManager: GameStateManager still not available after deferral, will retry");
                CallDeferred(nameof(DeferredInit));
            }
        }

        private void TryUpdateVisibility()
        {
            if (_preShowLayer == null || _liveShowLayer == null)
            {
                _visibilityUpdateAttempts++;
                if (_visibilityUpdateAttempts >= MAX_UPDATE_ATTEMPTS)
                {
                    GD.PrintErr("UIManager: Max visibility update attempts reached, layers not registered");
                    return;
                }
                CallDeferred(nameof(TryUpdateVisibility));
                return;
            }

            var gameState = ServiceRegistry.Instance?.GameStateManager;
            if (gameState != null)
            {
                PerformVisibilityUpdate(gameState.CurrentPhase);
            }
        }

        private void OnPhaseChanged(int oldPhaseInt, int newPhaseInt)
        {
            var newPhase = (GamePhase)newPhaseInt;
            GD.Print($"UIManager: Phase changed to {newPhase}");

            if (_preShowLayer == null || _liveShowLayer == null)
            {
                CallDeferred(nameof(TryUpdateVisibility));
                return;
            }

            PerformVisibilityUpdate(newPhase);
        }

        private void PerformVisibilityUpdate(GamePhase newPhase)
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

        public override void _ExitTree()
        {
            if (_gameStateConnected)
            {
                var gameState = ServiceRegistry.Instance?.GameStateManager;
                if (gameState != null)
                {
                    gameState.Disconnect("PhaseChanged", Callable.From<int, int>(OnPhaseChanged));
                }
            }
        }
    }
}