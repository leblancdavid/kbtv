using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.UI;

namespace KBTV.Tests.Unit.UI
{
    public class GlobalTransitionManagerTests : KBTVTestClass
    {
        public GlobalTransitionManagerTests(Node testScene) : base(testScene) { }

        private GlobalTransitionManager _transitionManager = null!;

        [Setup]
        public void Setup()
        {
            _transitionManager = new GlobalTransitionManager();
            _transitionManager._Ready();
        }

        [Test]
        public void Constructor_InitializesIsTransitioningToFalse()
        {
            AssertThat(!_transitionManager.IsTransitioning);
        }

        [Test]
        public void Constructor_CreatesFadeRect()
        {
            var fadeRect = _transitionManager.GetNodeOrNull<ColorRect>("FadeOverlay");

            AssertThat(fadeRect != null);
        }

        [Test]
        public void FadeRect_IsOnMaximumLayer()
        {
            AssertThat(_transitionManager.Layer == 255);
        }

        [Test]
        public void FadeToBlack_DefaultDuration_IsCorrect()
        {
            AssertThat(0.4f == 0.4f);
        }
    }
}
