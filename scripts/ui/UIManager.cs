using System.Threading.Tasks;
using Godot;
using KBTV.Core;

namespace KBTV.UI
{
    /// <summary>
    /// Manages UI layer visibility based on game phases.
    /// Handles the CanvasLayer-based UI architecture for proper draw ordering.
    /// Converted to AutoInject Dependent pattern.
    /// </summary>
    public partial class UIManager : Node, IUIManager,
        IProvide<UIManager>,
        IDependent
    {
        public override void _Notification(int what) => this.Notify(what);

        private GameStateManager GameStateManager => DependencyInjection.Get<GameStateManager>(this);

        private GlobalTransitionManager GlobalTransitionManager => DependencyInjection.Get<GlobalTransitionManager>(this);

        private CanvasLayer _preShowLayer;
        private CanvasLayer _liveShowLayer;
        private CanvasLayer _postShowLayer;
        private bool _isTransitioning;

        // Provider interface implementation
        UIManager IProvide<UIManager>.Value() => this;

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

        /// <summary>
        /// Called when all dependencies are resolved.
        /// </summary>
        public void OnResolved()
        {
            GameStateManager.Connect("PhaseChanged", Callable.From<int, int>(OnPhaseChanged));
        }

        /// <summary>
        /// Called when node enters the scene tree and is ready.
        /// </summary>
        public void OnReady()
        {
            this.Provide();
        }

        private void TryUpdateVisibility()
        {
            if (_preShowLayer == null)
            {
                return;
            }

            PerformVisibilityUpdate(GameStateManager.CurrentPhase);
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
                Log.Error("UIManager: Cannot switch to LiveShow - LiveShow layer not registered");
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

            var transitionManager = GlobalTransitionManager;
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
                case GamePhase.Loading:
                    // Hide all UI layers during loading
                    _preShowLayer?.Hide();
                    _liveShowLayer?.Hide();
                    if (_postShowLayer != null) _postShowLayer.Hide();
                    break;

                case GamePhase.PreShow:
                    _preShowLayer?.Show();
                    _liveShowLayer?.Hide();
                    if (_postShowLayer != null) _postShowLayer.Hide();
                    break;

                case GamePhase.LiveShow:
                    _preShowLayer?.Hide();
                    _liveShowLayer?.Show();
                    if (_postShowLayer != null) _postShowLayer.Hide();
                    break;

                case GamePhase.PostShow:
                    _preShowLayer?.Hide();
                    _liveShowLayer?.Hide();
                    if (_postShowLayer != null) _postShowLayer.Show();
                    break;

                default:
                    Log.Error($"UIManager: Unknown game phase {newPhase}");
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
                Log.Error("UIManager: PostShow layer not registered");
                return;
            }

            _liveShowLayer.Hide();
            _postShowLayer.Show();
        }

        public override void _ExitTree()
        {
            GameStateManager.Disconnect("PhaseChanged", Callable.From<int, int>(OnPhaseChanged));
        }
    }
}