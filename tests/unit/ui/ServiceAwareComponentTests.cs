using Chickensoft.GoDotTest;
using Godot;
using KBTV.Core;
using KBTV.UI;

namespace KBTV.Tests.Unit.UI
{
    public partial class ServiceAwareComponentTests : KBTVTestClass
    {
        public ServiceAwareComponentTests(Node testScene) : base(testScene) { }

        private TestServiceAwareComponent _component = null!;

        private partial class TestServiceAwareComponent : ServiceAwareComponent
        {
            public bool InitializeWithServicesCalled { get; private set; } = false;
            public bool RetryInitializationCalled { get; private set; } = false;

            protected override void InitializeWithServices()
            {
                InitializeWithServicesCalled = true;
                _isInitialized = true;
            }

            protected override void RetryInitialization()
            {
                RetryInitializationCalled = true;
                base.RetryInitialization();
            }
        }

        [Setup]
        public void Setup()
        {
            _component = new TestServiceAwareComponent();
        }

        [Test]
        public void Constructor_CreatesControlNode()
        {
            AssertThat(_component != null);
        }

        [Test]
        public void DefersInitialization_WhenServiceRegistryNotReady()
        {
            // Reset ServiceRegistry to test uninitialized behavior
            ServiceRegistry.ResetForTesting();

            // Create a new component after reset
            var component = new TestServiceAwareComponent();

            // Component should not have initialized since ServiceRegistry is not ready
            AssertThat(!component.InitializeWithServicesCalled);
        }
    }
}
