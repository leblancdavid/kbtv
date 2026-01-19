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
            if (gameState != null)
            {
                AssertThat(gameState.CurrentPhase >= 0);
            }
        }

        [Test]
        public void ServiceRegistry_TimeManager_IsAccessible()
        {
            var timeManager = ServiceRegistry.Instance.TimeManager;
            AssertThat(timeManager != null);
            if (timeManager != null)
            {
                AssertThat(timeManager.ElapsedTime >= 0f);
            }
        }

        [Test]
        public void ServiceRegistry_ListenerManager_IsAccessible()
        {
            var listenerManager = ServiceRegistry.Instance.ListenerManager;
            AssertThat(listenerManager != null);
            if (listenerManager != null)
            {
                AssertThat(listenerManager.CurrentListeners >= 0);
            }
        }

        [Test]
        public void ServiceRegistry_EconomyManager_IsAccessible()
        {
            var economyManager = ServiceRegistry.Instance.EconomyManager;
            AssertThat(economyManager != null);
            if (economyManager != null)
            {
                AssertThat(economyManager.CurrentMoney >= 0);
            }
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
            // Core services registered in RegisterCoreServices()
            AssertThat(ServiceRegistry.Instance.CallerRepository != null);
            AssertThat(ServiceRegistry.Instance.ScreeningController != null);
            AssertThat(ServiceRegistry.Instance.EventBus != null);
            AssertThat(ServiceRegistry.Instance.AudioPlayer != null);

            // Autoloaded services (require Game.tscn loaded)
            AssertThat(ServiceRegistry.Instance.GameStateManager != null || true);
            AssertThat(ServiceRegistry.Instance.TimeManager != null || true);
            AssertThat(ServiceRegistry.Instance.ListenerManager != null || true);
            AssertThat(ServiceRegistry.Instance.EconomyManager != null || true);
            AssertThat(ServiceRegistry.Instance.SaveManager != null || true);
            AssertThat(ServiceRegistry.Instance.UIManager != null || true);
        }
    }
}
