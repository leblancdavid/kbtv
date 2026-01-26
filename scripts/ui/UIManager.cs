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
        private CanvasLayer _postShowLayer;
        private bool _gameStateConnected;
        private GameStateManager _gameState;
        private bool _isTransitioning;

        public void RegisterPreShowLayer(CanvasLayer layer)
        {
            _preShowLayer = layer;
            TryUpdateVisibility();
        }

        public void RegisterLiveShowLayer(CanvasLayer layer)
        {
            _liveShowLayer = layer;
            TryUpdateVisibility();
        }

        public void RegisterPostShowLayer(CanvasLayer layer)
        {
            _postShowLayer = layer;
        }

        public override void _Ready()
        {
            ServiceRegistry.Instance.RegisterSelf<UIManager>(this);
            CompleteInitialization();
        }

        private void CompleteInitialization()
        {
            _gameState = ServiceRegistry.Instance.GameStateManager;
            if (_gameState != null)
            {
                _gameState.Connect("PhaseChanged", Callable.From<int, int>(OnPhaseChanged));
                _gameStateConnected = true;
                TryUpdateVisibility();
            }
            else
            {
                GD.PrintErr("UIManager: GameStateManager not available - check autoload order");
            }
        }

        private void TryUpdateVisibility()
        {
            if (_preShowLayer == null)
            {
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

            if (newPhase == GamePhase.PreShow && _preShowLayer == null)
            {
                return; // Layer will be registered when available
            }

            if (newPhase == GamePhase.LiveShow && _liveShowLayer == null)
            {
                GD.PrintErr("UIManager: Cannot switch to LiveShow - LiveShow layer not registered");
                return;
            }

            if (_isTransitioning)
            {
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
            switch (newPhase)
            {
                case GamePhase.PreShow:
                    _preShowLayer.Show();
                    _liveShowLayer.Hide();
                    if (_postShowLayer != null) _postShowLayer.Hide();
                    break;

                case GamePhase.LiveShow:
                    _preShowLayer.Hide();
                    _liveShowLayer.Show();
                    if (_postShowLayer != null) _postShowLayer.Hide();
                    break;

                case GamePhase.PostShow:
                    _preShowLayer.Hide();
                    _liveShowLayer.Hide();
                    if (_postShowLayer != null) _postShowLayer.Show();
                    break;

                default:
                    GD.PrintErr($"UIManager: Unknown game phase {newPhase}");
                    break;
            }
        }

        private void PerformVisibilityUpdate(GamePhase newPhase)
        {
            PerformVisibilityUpdateInstant(newPhase);
        }

        public void HideAdBreakPanel()
        {
            // This method is called by EndShowPanel to hide the ad break panel
            // The actual hiding is done in LiveShowFooter.OnBreakEnded when breaks remaining == 0
        }

        public void ShowEndShowPanel()
        {
            // This method is called by EndShowPanel to show itself
            // The actual showing is done in LiveShowFooter.OnBreakEnded when breaks remaining == 0
        }

        public void ShowPostShowLayer()
        {
            if (_postShowLayer == null)
            {
                GD.PrintErr("UIManager: PostShow layer not registered");
                return;
            }

            _liveShowLayer.Hide();
            _postShowLayer.Show();
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