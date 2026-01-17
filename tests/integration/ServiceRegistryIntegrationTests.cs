using Chickensoft.GoDotTest;
using Godot;
using KBTV.Core;

namespace KBTV.Tests.Integration
{
    public class ServiceRegistryIntegrationTests : KBTVTestClass
    {
        public ServiceRegistryIntegrationTests(Node testScene) : base(testScene) { }

        [Test]
        public void ServiceRegistry_IsInitialized_ReturnsTrue()
        {
            AssertThat(ServiceRegistry.IsInitialized);
            AssertThat(ServiceRegistry.Instance != null);
        }

        [Test]
        public void ServiceRegistry_GameStateManager_IsAccessible()
        {
            var gameState = ServiceRegistry.Instance.GameStateManager;
            AssertThat(gameState != null);
            AssertThat(gameState.CurrentPhase >= 0);
        }

        [Test]
        public void ServiceRegistry_TimeManager_IsAccessible()
        {
            var timeManager = ServiceRegistry.Instance.TimeManager;
            AssertThat(timeManager != null);
            AssertThat(timeManager.ElapsedTime >= 0f);
        }

        [Test]
        public void ServiceRegistry_ListenerManager_IsAccessible()
        {
            var listenerManager = ServiceRegistry.Instance.ListenerManager;
            AssertThat(listenerManager != null);
            AssertThat(listenerManager.CurrentListeners >= 0);
        }

        [Test]
        public void ServiceRegistry_EconomyManager_IsAccessible()
        {
            var economyManager = ServiceRegistry.Instance.EconomyManager;
            AssertThat(economyManager != null);
            AssertThat(economyManager.CurrentMoney >= 0);
        }

        [Test]
        public void ServiceRegistry_SaveManager_IsAccessible()
        {
            var saveManager = ServiceRegistry.Instance.SaveManager;
            AssertThat(saveManager != null);
        }

        [Test]
        public void ServiceRegistry_UIManager_IsAccessible()
        {
            var uiManager = ServiceRegistry.Instance.UIManager;
            AssertThat(uiManager != null);
        }

        [Test]
        public void ServiceRegistry_CallerRepository_IsAccessible()
        {
            var repository = ServiceRegistry.Instance.CallerRepository;
            AssertThat(repository != null);
        }

        [Test]
        public void ServiceRegistry_ScreeningController_IsAccessible()
        {
            var controller = ServiceRegistry.Instance.ScreeningController;
            AssertThat(controller != null);
            AssertThat(!controller.IsActive);
        }

        [Test]
        public void ServiceRegistry_AllShortcuts_ReturnValidServices()
        {
            AssertThat(ServiceRegistry.Instance.GameStateManager != null);
            AssertThat(ServiceRegistry.Instance.TimeManager != null);
            AssertThat(ServiceRegistry.Instance.ListenerManager != null);
            AssertThat(ServiceRegistry.Instance.EconomyManager != null);
            AssertThat(ServiceRegistry.Instance.SaveManager != null);
            AssertThat(ServiceRegistry.Instance.UIManager != null);
            AssertThat(ServiceRegistry.Instance.CallerRepository != null);
            AssertThat(ServiceRegistry.Instance.ScreeningController != null);
        }
    }
}
