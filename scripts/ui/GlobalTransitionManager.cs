using System.Threading.Tasks;
using Godot;
using KBTV.Core;

namespace KBTV.UI
{
    public partial class GlobalTransitionManager : CanvasLayer
    {
        private ColorRect _fadeRect;
        private bool _isTransitioning;
        private const float DEFAULT_FADE_DURATION = 0.4f;

        public bool IsTransitioning => _isTransitioning;

        public override void _Ready()
        {
            ServiceRegistry.Instance.RegisterSelf<GlobalTransitionManager>(this);
            CreateFadeOverlay();
            GD.Print("GlobalTransitionManager: Initialized");
        }

        private void CreateFadeOverlay()
        {
            Layer = 255;
            _fadeRect = new ColorRect();
            _fadeRect.Name = "FadeOverlay";
            _fadeRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _fadeRect.Color = Colors.Black;
            _fadeRect.Modulate = new Color(0f, 0f, 0f, 0f);
            _fadeRect.Visible = false;
            AddChild(_fadeRect);
        }

        public async Task FadeToBlack(float duration = DEFAULT_FADE_DURATION)
        {
            if (_isTransitioning)
            {
                GD.Print("GlobalTransitionManager: Already transitioning, skipping");
                return;
            }

            _isTransitioning = true;
            _fadeRect.Modulate = new Color(0f, 0f, 0f, 0f);
            _fadeRect.Visible = true;

            var tween = CreateTween();
            tween.SetEase(Tween.EaseType.InOut);
            tween.SetTrans(Tween.TransitionType.Linear);
            tween.TweenProperty(_fadeRect, "modulate:a", 1f, duration);

            await ToSignal(tween, "finished");
            _isTransitioning = false;
        }

        public async Task FadeFromBlack(float duration = DEFAULT_FADE_DURATION)
        {
            if (_isTransitioning)
            {
                GD.Print("GlobalTransitionManager: Already transitioning, skipping");
                return;
            }

            _isTransitioning = true;
            _fadeRect.Modulate = new Color(0f, 0f, 0f, 1f);

            var tween = CreateTween();
            tween.SetEase(Tween.EaseType.InOut);
            tween.SetTrans(Tween.TransitionType.Linear);
            tween.TweenProperty(_fadeRect, "modulate:a", 0f, duration);

            await ToSignal(tween, "finished");

            _fadeRect.Visible = false;
            _isTransitioning = false;
        }

        public async Task TransitionToScene(string scenePath, float fadeOutDuration = DEFAULT_FADE_DURATION, float fadeInDuration = DEFAULT_FADE_DURATION)
        {
            var tree = GetTree();
            if (tree == null)
            {
                GD.PrintErr("GlobalTransitionManager: GetTree() is null, cannot transition");
                return;
            }

            await FadeToBlack(fadeOutDuration);
            tree.ChangeSceneToFile(scenePath);
            await ToSignal(tree, "process_frame");
            await FadeFromBlack(fadeInDuration);
        }
    }
}
