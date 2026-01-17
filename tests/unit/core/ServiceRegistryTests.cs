using System;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Economy;
using KBTV.Managers;
using KBTV.Persistence;
using KBTV.Screening;
using KBTV.UI;

namespace KBTV.Tests.Unit.Core
{
    public class ServiceRegistryTests : KBTVTestClass
    {
        public ServiceRegistryTests(Node testScene) : base(testScene) { }

        private ServiceRegistry _registry = null!;

        [Setup]
        public void Setup()
        {
            _registry = new ServiceRegistry();
            _registry._Ready();
        }

        [Test]
        public void Register_Instance_StoresService()
        {
            var service = new TestService();
            _registry.Register<ITestService>(service);

            var retrieved = _registry.Get<ITestService>();
            AssertThat(retrieved == service);
        }

        [Test]
        public void Register_NullInstance_LogsError()
        {
            _registry.Register<ITestService>(null!);
            AssertThat(!_registry.HasService<ITestService>());
        }

        [Test]
        public void Register_DuplicateService_Overwrites()
        {
            var service1 = new TestService();
            var service2 = new TestService();

            _registry.Register<ITestService>(service1);
            _registry.Register<ITestService>(service2);

            var retrieved = _registry.Get<ITestService>();
            AssertThat(retrieved == service2);
        }

        [Test]
        public void RegisterFactory_RegistersFactory()
        {
            _registry.RegisterFactory<ITestService>(() => new TestService());
            AssertThat(_registry.HasService<ITestService>());

            var instance1 = _registry.Get<ITestService>();
            var instance2 = _registry.Get<ITestService>();
            AssertThat(instance1 == instance2);
        }

        [Test]
        public void RegisterFactory_NullFactory_DoesNotRegister()
        {
            _registry.RegisterFactory<ITestService>(null!);
            AssertThat(!_registry.HasService<ITestService>());
        }

        [Test]
        public void Get_UnregisteredService_ReturnsNull()
        {
            var service = _registry.Get<INonexistentService>();
            AssertThat(service == null);
        }

        [Test]
        public void Get_UnregisteredService_LogsError()
        {
            var service = _registry.Get<INonexistentService>();
            AssertThat(service == null);
        }

        [Test]
        public void HasService_Registered_ReturnsTrue()
        {
            var service = new TestService();
            _registry.Register<ITestService>(service);

            AssertThat(_registry.HasService<ITestService>());
        }

        [Test]
        public void HasService_NotRegistered_ReturnsFalse()
        {
            AssertThat(!_registry.HasService<INonexistentService>());
        }

        [Test]
        public void Unregister_RemovesService()
        {
            var service = new TestService();
            _registry.Register<ITestService>(service);
            AssertThat(_registry.HasService<ITestService>());

            _registry.Unregister<ITestService>();

            AssertThat(!_registry.HasService<ITestService>());
        }

        [Test]
        public void ClearAll_RemovesAllServices()
        {
            _registry.Register<ITestService>(new TestService());
            _registry.Register<ITestService2>(new TestService2());

            _registry.ClearAll();

            AssertThat(!_registry.HasService<ITestService>());
            AssertThat(!_registry.HasService<ITestService2>());
        }

        [Test]
        public void Require_ServiceExists_ReturnsService()
        {
            var service = new TestService();
            _registry.Register<ITestService>(service);

            var retrieved = _registry.Require<ITestService>();

            AssertThat(retrieved == service);
        }

        [Test]
        public void Require_ServiceMissing_ThrowsException()
        {
            AssertThat(_registry.HasService<IMissingService>() == false);
        }

        [Test]
        public void Instance_SingletonBehavior()
        {
            AssertThat(ServiceRegistry.Instance == _registry);
        }

        [Test]
        public void ShortcutProperties_ReturnValidServices()
        {
            AssertThat(_registry.GameStateManager != null);
            AssertThat(_registry.TimeManager != null);
            AssertThat(_registry.ListenerManager != null);
            AssertThat(_registry.EconomyManager != null);
            AssertThat(_registry.SaveManager != null);
            AssertThat(_registry.UIManager != null);
            AssertThat(_registry.CallerRepository != null);
            AssertThat(_registry.ScreeningController != null);
            AssertThat(_registry.CallerGenerator != null);
            AssertThat(_registry.GlobalTransitionManager != null);
        }

        [Test]
        public void InitializationProgress_StartsAtZero()
        {
            AssertThat(_registry.InitializationProgress >= 0f);
            AssertThat(_registry.InitializationProgress <= 1f);
        }

        [Test]
        public void IsInitialized_AfterReady_IsTrue()
        {
            AssertThat(ServiceRegistry.IsInitialized);
        }

        private interface ITestService { }
        private interface ITestService2 { }
        private interface INonexistentService { }
        private interface IMissingService { }

        private class TestService : ITestService { }
        private class TestService2 : ITestService2 { }
    }
}
