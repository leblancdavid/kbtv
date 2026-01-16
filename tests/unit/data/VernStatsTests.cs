using Chickensoft.GoDotTest;
using Godot;
using KBTV.Data;

namespace KBTV.Tests.Unit.Data
{
    public class VernStatsTests : KBTVTestClass
    {
        public VernStatsTests(Node testScene) : base(testScene) { }

        private VernStats _vernStats = null!;

        [Setup]
        public void Setup()
        {
            _vernStats = new VernStats();
            _vernStats.Initialize();
        }

        [Test]
        public void Initialize_CreatesAllStats()
        {
            AssertThat(_vernStats.Caffeine != null);
            AssertThat(_vernStats.Nicotine != null);
            AssertThat(_vernStats.Energy != null);
            AssertThat(_vernStats.Satiety != null);
            AssertThat(_vernStats.Spirit != null);
            AssertThat(_vernStats.Alertness != null);
            AssertThat(_vernStats.Discernment != null);
            AssertThat(_vernStats.Focus != null);
            AssertThat(_vernStats.Patience != null);
            AssertThat(_vernStats.Skepticism != null);
        }

        [Test]
        public void Initialize_SetsCorrectInitialCaffeine()
        {
            AssertThat(_vernStats.Caffeine.Value == 50f);
        }

        [Test]
        public void Initialize_SetsCorrectInitialEnergy()
        {
            AssertThat(_vernStats.Energy.Value == 100f);
        }

        [Test]
        public void Initialize_SetsCorrectInitialSpirit()
        {
            AssertThat(_vernStats.Spirit.Value == 0f);
        }

        [Test]
        public void CalculateVIBE_ReturnsValueBetweenNegative100And100()
        {
            float vibe = _vernStats.CalculateVIBE();

            AssertThat(vibe >= -100f);
            AssertThat(vibe <= 100f);
        }

        [Test]
        public void CalculateVIBE_ReturnsReasonableValue()
        {
            float vibe = _vernStats.CalculateVIBE();

            AssertThat(vibe > -50f);
            AssertThat(vibe < 50f);
        }

        [Test]
        public void CalculateEntertainment_ReturnsValueBetween0And100()
        {
            float entertainment = _vernStats.CalculateEntertainment();

            AssertThat(entertainment >= 0f);
            AssertThat(entertainment <= 100f);
        }

        [Test]
        public void CalculateCredibility_ReturnsValueBetween0And100()
        {
            float credibility = _vernStats.CalculateCredibility();

            AssertThat(credibility >= 0f);
            AssertThat(credibility <= 100f);
        }

        [Test]
        public void CalculateEngagement_ReturnsValueBetween0And100()
        {
            float engagement = _vernStats.CalculateEngagement();

            AssertThat(engagement >= 0f);
            AssertThat(engagement <= 100f);
        }

        [Test]
        public void CalculateSpiritModifier_ReturnsPositiveMultiplier()
        {
            float modifier = _vernStats.CalculateSpiritModifier();

            AssertThat(modifier > 0f);
        }

        [Test]
        public void CalculateMoodType_DefaultIsNeutral()
        {
            _vernStats.Initialize();

            AssertThat(_vernStats.CurrentMoodType == VernMoodType.Neutral);
        }

        [Test]
        public void CurrentMoodType_Initialized()
        {
            AssertThat(_vernStats.CurrentMoodType >= 0);
        }

        [Test]
        public void StatsChanged_EmitsOnValueChange()
        {
            bool called = false;
            _vernStats.StatsChanged += () => called = true;

            _vernStats.Energy.Modify(10f);

            AssertThat(called);
        }

        [Test]
        public void VibeChanged_EmitsWhenVibeChanges()
        {
            float? newVibe = null;
            _vernStats.VibeChanged += (vibe) => newVibe = vibe;

            _vernStats.Energy.Modify(-50f);

            AssertThat(newVibe != null);
        }
    }
}
