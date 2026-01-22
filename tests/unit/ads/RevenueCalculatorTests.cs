using Chickensoft.GoDotTest;
using Godot;
using KBTV.Ads;

namespace KBTV.Tests.Unit.Ads
{
    public class RevenueCalculatorTests : KBTVTestClass
    {
        public RevenueCalculatorTests(Node testScene) : base(testScene) { }

        private RevenueCalculator _calculator = null!;

        [Setup]
        public void Setup()
        {
            _calculator = new RevenueCalculator();
        }

        [Test]
        public void Constructor_CreatesInstance()
        {
            AssertThat(_calculator != null);
        }

        [Test]
        public void CalculateBreakRevenue_WithZeroListeners_ReturnsZero()
        {
            var config = new AdBreakConfig();
            config.SlotsPerBreak = 1;

            var revenue = _calculator.CalculateBreakRevenue(0, config);

            AssertThat(revenue == 0f);
        }

        [Test]
        public void CalculateBreakRevenue_WithListeners_ReturnsPositiveRevenue()
        {
            var config = new AdBreakConfig();
            config.SlotsPerBreak = 1;

            var revenue = _calculator.CalculateBreakRevenue(100, config);

            AssertThat(revenue > 0f);
        }

        [Test]
        public void CalculateBreakRevenue_WithMultipleSlots_IncreasesRevenue()
        {
            var config1 = new AdBreakConfig();
            config1.SlotsPerBreak = 1;

            var config2 = new AdBreakConfig();
            config2.SlotsPerBreak = 2;

            var revenue1 = _calculator.CalculateBreakRevenue(100, config1);
            var revenue2 = _calculator.CalculateBreakRevenue(100, config2);

            AssertThat(revenue2 > revenue1);
        }

        [Test]
        public void AwardRevenue_DoesNotThrow()
        {
            try
            {
                _calculator.AwardRevenue(100f);
                AssertThat(true);
            }
            catch
            {
                AssertThat(false, "AwardRevenue should not throw exceptions");
            }
        }
    }
}