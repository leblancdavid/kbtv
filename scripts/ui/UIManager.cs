using Godot;
using KBTV.Core;

namespace KBTV.UI
{
    /// <summary>
    /// Manages UI layer visibility based on game phases.
    /// Handles the CanvasLayer-based UI architecture for proper draw ordering.
    /// </summary>
    public partial class UIManager : Node
    {
        public static UIManager Instance
        {
            get
            {
                var root = ((SceneTree)Engine.GetMainLoop()).Root;
                var uiManager = root.GetNode("/root/Main/UIManager") as UIManager;
                if (uiManager == null)
                {
                    GD.PrintErr("UIManager.Instance: UIManager node not found at /root/Main/UIManager");
                }
                return uiManager;
            }
        }

        private CanvasLayer _preShowLayer;
        private CanvasLayer _liveShowLayer;

        // Public methods for UI managers to register their layers
        public void RegisterPreShowLayer(CanvasLayer layer)
        {
            _preShowLayer = layer;
            GD.Print($"UIManager: PreShow layer registered: {layer != null}");
        }

        public void RegisterLiveShowLayer(CanvasLayer layer)
        {
            _liveShowLayer = layer;
            GD.Print($"UIManager: LiveShow layer registered: {layer != null}");
        }

        public override void _Ready()
        {
            // GD.Print("UIManager: _Ready called");

            // CanvasLayers will be registered by UI managers when they create them
            // Try to connect to GameStateManager immediately
            TryConnectToGameStateManager();

            // If still not connected, set up deferred retry
            if (GameStateManager.Instance == null)
            {
                // GD.Print("UIManager: GameStateManager not ready, deferring connection");
                CallDeferred(nameof(TryConnectToGameStateManager));
            }
        }

        private void TryConnectToGameStateManager()
        {
            var gameState = GameStateManager.Instance;
            if (gameState != null)
            {
                // Connect to phase changes
                gameState.Connect("PhaseChanged", Callable.From<int, int>(UpdateUIVisibility));

                // Set initial visibility based on current phase - defer to ensure layers are registered
                CallDeferred(nameof(UpdateInitialVisibility));
                // GD.Print("UIManager: Successfully connected to GameStateManager");
            }
            else
            {
                // Still not available - show PreShow as default - defer
                CallDeferred(nameof(UpdateInitialVisibility));
                // GD.Print("UIManager: GameStateManager still not available, using PreShow default");

                // Try again in next frame
                CallDeferred(nameof(TryConnectToGameStateManager));
            }
        }

        private void UpdateInitialVisibility()
        {
            var gameState = GameStateManager.Instance;
            if (gameState != null)
            {
                UpdateUIVisibility((int)GamePhase.PreShow, (int)gameState.CurrentPhase);
            }
            else
            {
                UpdateUIVisibility((int)GamePhase.PreShow, (int)GamePhase.PreShow);
            }
        }

        private void UpdateUIVisibility(int oldPhaseInt, int newPhaseInt)
        {
            GamePhase oldPhase = (GamePhase)oldPhaseInt;
            GamePhase newPhase = (GamePhase)newPhaseInt;

            GD.Print($"UIManager: Updating UI visibility for phase {newPhase}");

            if (_preShowLayer == null || _liveShowLayer == null)
            {
                GD.PrintErr("UIManager: CanvasLayers not found!");
                return;
            }

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
            // Unsubscribe from events
            var gameState = GameStateManager.Instance;
            if (gameState != null)
            {
                gameState.Disconnect("PhaseChanged", Callable.From<int, int>(UpdateUIVisibility));
                // GD.Print("UIManager: Disconnected from GameStateManager");
            }
        }
    }
}