using System;
using Godot;
using KBTV.Core;

namespace KBTV.Tests.Integration
{
    /// <summary>
    /// Integration tests for ServiceRegistry-based service access.
    /// These tests verify that all managers are properly registered and accessible.
    /// Run manually from Godot editor or via test runner.
    /// </summary>
    [GlobalClass]
    public partial class ServiceRegistryIntegrationTests : Node
    {
        private int _passedTests = 0;
        private int _failedTests = 0;

        public override void _Ready()
        {
            GD.Print("=== ServiceRegistry Integration Tests ===");
            RunTests();
            GD.Print($"=== Results: {_passedTests} passed, {_failedTests} failed ===");
        }

        private void RunTests()
        {
            TestServiceRegistryInitialized();
            TestGameStateManagerAccess();
            TestTimeManagerAccess();
            TestListenerManagerAccess();
            TestEconomyManagerAccess();
            TestSaveManagerAccess();
            TestUIManagerAccess();
            TestCallerRepositoryAccess();
            TestScreeningControllerAccess();
            TestEventAggregatorAccess();
            TestAllShortcutsAvailable();
        }

        private void TestServiceRegistryInitialized()
        {
            try
            {
                Assert(ServiceRegistry.IsInitialized, "ServiceRegistry should be initialized");
                Assert(ServiceRegistry.Instance != null, "ServiceRegistry.Instance should not be null");
                Pass("ServiceRegistry initialized correctly");
            }
            catch (Exception ex)
            {
                Fail($"ServiceRegistry initialization: {ex.Message}");
            }
        }

        private void TestGameStateManagerAccess()
        {
            try
            {
                var gameState = ServiceRegistry.Instance.GameStateManager;
                Assert(gameState != null, "GameStateManager should not be null");
                Assert(gameState.CurrentPhase >= 0, "GameStateManager should have valid phase");
                Pass("GameStateManager accessible via ServiceRegistry");
            }
            catch (Exception ex)
            {
                Fail($"GameStateManager access: {ex.Message}");
            }
        }

        private void TestTimeManagerAccess()
        {
            try
            {
                var timeManager = ServiceRegistry.Instance.TimeManager;
                Assert(timeManager != null, "TimeManager should not be null");
                Assert(timeManager.ElapsedTime >= 0f, "TimeManager should have valid elapsed time");
                Assert(!timeManager.IsRunning, "TimeManager should not be running initially");
                Pass("TimeManager accessible via ServiceRegistry");
            }
            catch (Exception ex)
            {
                Fail($"TimeManager access: {ex.Message}");
            }
        }

        private void TestListenerManagerAccess()
        {
            try
            {
                var listenerManager = ServiceRegistry.Instance.ListenerManager;
                Assert(listenerManager != null, "ListenerManager should not be null");
                Assert(listenerManager.CurrentListeners >= 0, "ListenerManager should have valid listener count");
                Pass("ListenerManager accessible via ServiceRegistry");
            }
            catch (Exception ex)
            {
                Fail($"ListenerManager access: {ex.Message}");
            }
        }

        private void TestEconomyManagerAccess()
        {
            try
            {
                var economyManager = ServiceRegistry.Instance.EconomyManager;
                Assert(economyManager != null, "EconomyManager should not be null");
                Assert(economyManager.CurrentMoney >= 0, "EconomyManager should have valid money");
                Pass("EconomyManager accessible via ServiceRegistry");
            }
            catch (Exception ex)
            {
                Fail($"EconomyManager access: {ex.Message}");
            }
        }

        private void TestSaveManagerAccess()
        {
            try
            {
                var saveManager = ServiceRegistry.Instance.SaveManager;
                Assert(saveManager != null, "SaveManager should not be null");
                Assert(saveManager.CurrentSave != null, "SaveManager should have save data");
                Pass("SaveManager accessible via ServiceRegistry");
            }
            catch (Exception ex)
            {
                Fail($"SaveManager access: {ex.Message}");
            }
        }

        private void TestUIManagerAccess()
        {
            try
            {
                var uiManager = ServiceRegistry.Instance.UIManager;
                Assert(uiManager != null, "UIManager should not be null");
                Pass("UIManager accessible via ServiceRegistry");
            }
            catch (Exception ex)
            {
                Fail($"UIManager access: {ex.Message}");
            }
        }

        private void TestCallerRepositoryAccess()
        {
            try
            {
                var repository = ServiceRegistry.Instance.CallerRepository;
                Assert(repository != null, "CallerRepository should not be null");
                Pass("CallerRepository accessible via ServiceRegistry");
            }
            catch (Exception ex)
            {
                Fail($"CallerRepository access: {ex.Message}");
            }
        }

        private void TestScreeningControllerAccess()
        {
            try
            {
                var controller = ServiceRegistry.Instance.ScreeningController;
                Assert(controller != null, "ScreeningController should not be null");
                Assert(!controller.IsActive, "ScreeningController should not be active initially");
                Pass("ScreeningController accessible via ServiceRegistry");
            }
            catch (Exception ex)
            {
                Fail($"ScreeningController access: {ex.Message}");
            }
        }

        private void TestEventAggregatorAccess()
        {
            try
            {
                var events = ServiceRegistry.Instance.EventAggregator;
                Assert(events != null, "EventAggregator should not be null");
                Pass("EventAggregator accessible via ServiceRegistry");
            }
            catch (Exception ex)
            {
                Fail($"EventAggregator access: {ex.Message}");
            }
        }

        private void TestAllShortcutsAvailable()
        {
            try
            {
                Assert(ServiceRegistry.Instance.GameStateManager != null, "GameStateManager shortcut");
                Assert(ServiceRegistry.Instance.TimeManager != null, "TimeManager shortcut");
                Assert(ServiceRegistry.Instance.ListenerManager != null, "ListenerManager shortcut");
                Assert(ServiceRegistry.Instance.EconomyManager != null, "EconomyManager shortcut");
                Assert(ServiceRegistry.Instance.SaveManager != null, "SaveManager shortcut");
                Assert(ServiceRegistry.Instance.UIManager != null, "UIManager shortcut");
                Assert(ServiceRegistry.Instance.CallerRepository != null, "CallerRepository shortcut");
                Assert(ServiceRegistry.Instance.ScreeningController != null, "ScreeningController shortcut");
                Assert(ServiceRegistry.Instance.EventAggregator != null, "EventAggregator shortcut");
                Pass("All shortcut properties available");
            }
            catch (Exception ex)
            {
                Fail($"Shortcut properties: {ex.Message}");
            }
        }

        private void Assert(bool condition, string testName)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {testName}");
            }
        }

        private void Pass(string message)
        {
            _passedTests++;
            GD.Print($"[PASS] {message}");
        }

        private void Fail(string message)
        {
            _failedTests++;
            GD.PrintErr($"[FAIL] {message}");
        }
    }
}
