using Chickensoft.GoDotTest;
using Godot;
using KBTV.Ads;

namespace KBTV.Tests.Unit.Ads
{
    public class BreakLogicTests : KBTVTestClass
    {
        public BreakLogicTests(Node testScene) : base(testScene) { }

        private BreakLogic _logic = null!;

        [Setup]
        public void Setup()
        {
            _logic = new BreakLogic();
        }

        [Test]
        public void Constructor_CreatesInstance()
        {
            AssertThat(_logic != null);
        }

        [Test]
        public void ApplyUnqueuedPenalty_DoesNotThrow()
        {
            // This method accesses ServiceRegistry.Instance.GameStateManager
            // In test environment, it may or may not be available
            // We just verify it doesn't throw an exception
            try
            {
                _logic.ApplyUnqueuedPenalty();
                AssertThat(true); // If we reach here, no exception was thrown
            }
            catch
            {
                AssertThat(false, "ApplyUnqueuedPenalty should not throw exceptions");
            }
        }

        [Test]
        public void ApplyListenerDip_DoesNotThrow()
        {
            try
            {
                _logic.ApplyListenerDip();
                AssertThat(true);
            }
            catch
            {
                AssertThat(false, "ApplyListenerDip should not throw exceptions");
            }
        }

        [Test]
        public void RestoreListeners_DoesNotThrow()
        {
            try
            {
                _logic.RestoreListeners();
                AssertThat(true);
            }
            catch
            {
                AssertThat(false, "RestoreListeners should not throw exceptions");
            }
        }
    }
}