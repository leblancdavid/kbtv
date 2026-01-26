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

            protected override void InitializeWithServices()
            {
                InitializeWithServicesCalled = true;
                _isInitialized = true;
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
        public void InitializesImmediately_WhenServiceRegistryReady()
        {
            // Ensure ServiceRegistry is initialized (it should be by default in tests)
            var component = new TestServiceAwareComponent();

            // Component should have initialized immediately since ServiceRegistry is ready
            AssertThat(component.InitializeWithServicesCalled);
        }
    }
}
