using Chickensoft.GoDotTest;
using Godot;
using KBTV.Core;
using KBTV.Economy;

namespace KBTV.Tests.Unit.Managers
{
    public class EconomyManagerTests : KBTVTestClass
    {
        public EconomyManagerTests(Node testScene) : base(testScene) { }

        private EconomyManager _economyManager = null!;

        [Setup]
        public void Setup()
        {
            _economyManager = new EconomyManager();
            _economyManager._Ready();
        }

        [Test]
        public void Constructor_InitializesToStartingMoney()
        {
            AssertThat(_economyManager.CurrentMoney == 500);
        }

        [Test]
        public void CanAfford_EnoughMoney_ReturnsTrue()
        {
            AssertThat(_economyManager.CanAfford(100));
        }

        [Test]
        public void CanAfford_ExactAmount_ReturnsTrue()
        {
            AssertThat(_economyManager.CanAfford(500));
        }

        [Test]
        public void CanAfford_TooMuch_ReturnsFalse()
        {
            AssertThat(!_economyManager.CanAfford(1000));
        }

        [Test]
        public void AddMoney_PositiveAmount_IncreasesBalance()
        {
            int initial = _economyManager.CurrentMoney;

            _economyManager.AddMoney(100);

            AssertThat(_economyManager.CurrentMoney == initial + 100);
        }

        [Test]
        public void AddMoney_ZeroAmount_DoesNotChange()
        {
            int initial = _economyManager.CurrentMoney;

            _economyManager.AddMoney(0);

            AssertThat(_economyManager.CurrentMoney == initial);
        }

        [Test]
        public void AddMoney_NegativeAmount_DoesNotChange()
        {
            int initial = _economyManager.CurrentMoney;

            _economyManager.AddMoney(-50);

            AssertThat(_economyManager.CurrentMoney == initial);
        }

        [Test]
        public void AddMoney_EmitsMoneyChangedEvent()
        {
            int oldAmount = _economyManager.CurrentMoney;
            int? newAmount = null;

            _economyManager.MoneyChanged += (_, newVal) => newAmount = newVal;

            _economyManager.AddMoney(100);

            AssertThat(newAmount == oldAmount + 100);
        }

        [Test]
        public void SpendMoney_SufficientFunds_ReturnsTrue()
        {
            int initial = _economyManager.CurrentMoney;

            var result = _economyManager.SpendMoney(100);

            AssertThat(result);
            AssertThat(_economyManager.CurrentMoney == initial - 100);
        }

        [Test]
        public void SpendMoney_ExactAmount_ReturnsTrue()
        {
            var result = _economyManager.SpendMoney(500);

            AssertThat(result);
            AssertThat(_economyManager.CurrentMoney == 0);
        }

        [Test]
        public void SpendMoney_InsufficientFunds_ReturnsFalse()
        {
            int initial = _economyManager.CurrentMoney;

            var result = _economyManager.SpendMoney(1000);

            AssertThat(!result);
            AssertThat(_economyManager.CurrentMoney == initial);
        }

        [Test]
        public void SpendMoney_ZeroAmount_ReturnsFalse()
        {
            var result = _economyManager.SpendMoney(0);

            AssertThat(!result);
        }

        [Test]
        public void SpendMoney_NegativeAmount_ReturnsFalse()
        {
            var result = _economyManager.SpendMoney(-50);

            AssertThat(!result);
        }

        [Test]
        public void SpendMoney_EmitsMoneyChangedEvent()
        {
            int oldAmount = _economyManager.CurrentMoney;
            int? newAmount = null;

            _economyManager.MoneyChanged += (_, newVal) => newAmount = newVal;

            _economyManager.SpendMoney(100);

            AssertThat(newAmount == oldAmount - 100);
        }

        [Test]
        public void SpendMoney_SufficientFunds_EmitsPurchaseEvent()
        {
            int amount = 0;
            string? reason = null;

            _economyManager.Purchase += (amt, rsn) =>
            {
                amount = amt;
                reason = rsn;
            };

            _economyManager.SpendMoney(100, "Test Purchase");

            AssertThat(amount == 100);
            AssertThat(reason == "Test Purchase");
        }

        [Test]
        public void SpendMoney_InsufficientFunds_EmitsPurchaseFailedEvent()
        {
            bool failedCalled = false;

            _economyManager.PurchaseFailed += (_) => failedCalled = true;

            _economyManager.SpendMoney(1000);

            AssertThat(failedCalled);
        }

        [Test]
        public void SetMoney_PositiveValue_SetsCorrectly()
        {
            _economyManager.SetMoney(1000);

            AssertThat(_economyManager.CurrentMoney == 1000);
        }

        [Test]
        public void SetMoney_NegativeValue_ClampsToZero()
        {
            _economyManager.SetMoney(-100);

            AssertThat(_economyManager.CurrentMoney == 0);
        }

        [Test]
        public void SetMoney_ZeroValue_SetsToZero()
        {
            _economyManager.SetMoney(0);

            AssertThat(_economyManager.CurrentMoney == 0);
        }
    }
}
