using Chickensoft.GoDotTest;
using Godot;
using KBTV.Economy;

namespace KBTV.Tests.Unit.Data
{
    public class IncomeCalculatorTests : KBTVTestClass
    {
        public IncomeCalculatorTests(Node testScene) : base(testScene) { }

        [Test]
        public void CalculateShowIncome_ReturnsBaseStipend()
        {
            var income = IncomeCalculator.CalculateShowIncome(1000, 50f);

            AssertThat(income == 100);
        }

        [Test]
        public void CalculateShowIncome_WithZeroListeners_ReturnsBaseStipend()
        {
            var income = IncomeCalculator.CalculateShowIncome(0, 50f);

            AssertThat(income == 100);
        }

        [Test]
        public void CalculateShowIncome_WithHighListeners_ReturnsBaseStipend()
        {
            var income = IncomeCalculator.CalculateShowIncome(100000, 100f);

            AssertThat(income == 100);
        }

        [Test]
        public void CalculateShowIncome_WithLowQuality_ReturnsBaseStipend()
        {
            var income = IncomeCalculator.CalculateShowIncome(1000, 0f);

            AssertThat(income == 100);
        }

        [Test]
        public void CalculateShowIncome_WithHighQuality_ReturnsBaseStipend()
        {
            var income = IncomeCalculator.CalculateShowIncome(1000, 100f);

            AssertThat(income == 100);
        }

        [Test]
        public void GetIncomeBreakdown_ReturnsCorrectStructure()
        {
            var breakdown = IncomeCalculator.GetIncomeBreakdown(1000, 50f);

            AssertThat(breakdown.BaseStipend == 100);
            AssertThat(breakdown.AdRevenue == 0);
            AssertThat(breakdown.BonusIncome == 0);
            AssertThat(breakdown.Total == 100);
        }

        [Test]
        public void GetIncomeBreakdown_AdRevenueAlwaysZero()
        {
            var breakdown = IncomeCalculator.GetIncomeBreakdown(100000, 100f);

            AssertThat(breakdown.AdRevenue == 0);
        }

        [Test]
        public void GetIncomeBreakdown_BonusIncomeAlwaysZero()
        {
            var breakdown = IncomeCalculator.GetIncomeBreakdown(100000, 100f);

            AssertThat(breakdown.BonusIncome == 0);
        }

        [Test]
        public void IncomeBreakdown_TotalEqualsSum()
        {
            var breakdown = IncomeCalculator.GetIncomeBreakdown(1000, 50f);

            AssertThat(breakdown.Total == breakdown.BaseStipend + breakdown.AdRevenue + breakdown.BonusIncome);
        }
    }
}
