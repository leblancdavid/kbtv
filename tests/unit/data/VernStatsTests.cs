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
            // Dependencies
            AssertThat(_vernStats.Caffeine != null);
            AssertThat(_vernStats.Nicotine != null);

            // Core Stats
            AssertThat(_vernStats.Physical != null);
            AssertThat(_vernStats.Emotional != null);
            AssertThat(_vernStats.Mental != null);
        }

        [Test]
        public void Initialize_SetsCorrectInitialCaffeine()
        {
            // Dependencies start at 100 (full)
            AssertThat(_vernStats.Caffeine.Value == 100f);
        }

        [Test]
        public void Initialize_SetsCorrectInitialNicotine()
        {
            // Dependencies start at 100 (full)
            AssertThat(_vernStats.Nicotine.Value == 100f);
        }

        [Test]
        public void Initialize_CoreStatsStartAtZero()
        {
            // Core stats start at 0 (neutral)
            AssertThat(_vernStats.Physical.Value == 0f);
            AssertThat(_vernStats.Emotional.Value == 0f);
            AssertThat(_vernStats.Mental.Value == 0f);
        }

        [Test]
        public void Initialize_CoreStatsHaveCorrectRange()
        {
            // Core stats range from -100 to +100
            AssertThat(_vernStats.Physical.MinValue == -100f);
            AssertThat(_vernStats.Physical.MaxValue == 100f);
            AssertThat(_vernStats.Emotional.MinValue == -100f);
            AssertThat(_vernStats.Emotional.MaxValue == 100f);
            AssertThat(_vernStats.Mental.MinValue == -100f);
            AssertThat(_vernStats.Mental.MaxValue == 100f);
        }

        [Test]
        public void CalculateVIBE_ReturnsValueBetweenNegative100And100()
        {
            float vibe = _vernStats.CalculateVIBE();

            AssertThat(vibe >= -100f);
            AssertThat(vibe <= 100f);
        }

        [Test]
        public void CalculateVIBE_AtZeroStats_ReturnsZero()
        {
            // All stats at 0 should give VIBE of 0
            float vibe = _vernStats.CalculateVIBE();

            AssertThat(Mathf.IsEqualApprox(vibe, 0f), $"Expected VIBE near 0, got {vibe}");
        }

        [Test]
        public void CalculateVIBE_UsesCorrectWeights()
        {
            // Set known values to verify weights
            _vernStats.Physical.SetValue(100f);
            _vernStats.Emotional.SetValue(100f);
            _vernStats.Mental.SetValue(100f);

            float vibe = _vernStats.CalculateVIBE();

            // VIBE = (Physical × 0.25) + (Emotional × 0.40) + (Mental × 0.35)
            // = 25 + 40 + 35 = 100
            AssertThat(Mathf.IsEqualApprox(vibe, 100f), $"Expected VIBE = 100, got {vibe}");
        }

        [Test]
        public void CalculateMoodType_DefaultIsNeutral()
        {
            // All stats at 0 should give Neutral mood
            AssertThat(_vernStats.CurrentMoodType == VernMoodType.Neutral);
        }

        [Test]
        public void CalculateMoodType_Exhausted_ExtremeNegativePhysicalAndEmotional()
        {
            _vernStats.Physical.SetValue(-70f);  // Below -66
            _vernStats.Emotional.SetValue(-40f); // Below -33

            var mood = _vernStats.CalculateMoodType();

            AssertThat(mood == VernMoodType.Exhausted);
        }

        [Test]
        public void CalculateMoodType_Depressed_ExtremeNegativeEmotionalAndMental()
        {
            _vernStats.Emotional.SetValue(-70f); // Below -66
            _vernStats.Mental.SetValue(-40f);    // Below -33

            var mood = _vernStats.CalculateMoodType();

            AssertThat(mood == VernMoodType.Depressed);
        }

        [Test]
        public void CalculateMoodType_Angry_ExtremeNegativeEmotional()
        {
            _vernStats.Emotional.SetValue(-70f); // Below -66
            _vernStats.Physical.SetValue(-30f);  // Above -33

            var mood = _vernStats.CalculateMoodType();

            AssertThat(mood == VernMoodType.Angry);
        }

        [Test]
        public void CalculateMoodType_Frustrated_ModerateNegativeEmotional()
        {
            _vernStats.Emotional.SetValue(-50f); // -66 to -33
            _vernStats.Mental.SetValue(0f);      // -33 to +33

            var mood = _vernStats.CalculateMoodType();

            AssertThat(mood == VernMoodType.Frustrated);
        }

        [Test]
        public void CalculateMoodType_Tired_ModerateNegativePhysical()
        {
            _vernStats.Physical.SetValue(-50f);  // -66 to -33
            _vernStats.Emotional.SetValue(0f);   // -33 to +33

            var mood = _vernStats.CalculateMoodType();

            AssertThat(mood == VernMoodType.Tired);
        }

        [Test]
        public void CalculateMoodType_Irritated_LowMental()
        {
            _vernStats.Mental.SetValue(-50f);    // Below -33
            _vernStats.Emotional.SetValue(0f);   // -33 to +33
            _vernStats.Physical.SetValue(0f);    // -33 to +33

            var mood = _vernStats.CalculateMoodType();

            AssertThat(mood == VernMoodType.Irritated);
        }

        [Test]
        public void CalculateMoodType_Obsessive_ExtremePositiveMental()
        {
            _vernStats.Mental.SetValue(70f);     // Above +66
            _vernStats.Emotional.SetValue(0f);   // -33 to +66

            var mood = _vernStats.CalculateMoodType();

            AssertThat(mood == VernMoodType.Obsessive);
        }

        [Test]
        public void CalculateMoodType_Manic_ExtremePositivePhysicalAndEmotional()
        {
            _vernStats.Physical.SetValue(70f);   // Above +66
            _vernStats.Emotional.SetValue(40f);  // Above +33

            var mood = _vernStats.CalculateMoodType();

            AssertThat(mood == VernMoodType.Manic);
        }

        [Test]
        public void CalculateMoodType_Energized_ModeratePositivePhysicalAndEmotional()
        {
            _vernStats.Physical.SetValue(50f);   // +33 to +66
            _vernStats.Emotional.SetValue(50f);  // +33 to +66

            var mood = _vernStats.CalculateMoodType();

            AssertThat(mood == VernMoodType.Energized);
        }

        [Test]
        public void CalculateMoodType_Amused_ModeratePositiveEmotional()
        {
            _vernStats.Emotional.SetValue(50f);  // +33 to +66
            _vernStats.Mental.SetValue(0f);      // -33 to +66

            var mood = _vernStats.CalculateMoodType();

            AssertThat(mood == VernMoodType.Amused);
        }

        [Test]
        public void CalculateMoodType_Focused_ModeratePositiveMental()
        {
            _vernStats.Mental.SetValue(50f);     // +33 to +66
            _vernStats.Physical.SetValue(0f);    // -33 to +66

            var mood = _vernStats.CalculateMoodType();

            AssertThat(mood == VernMoodType.Focused);
        }

        [Test]
        public void CalculateMoodType_Gruff_SlightlyNegativeEmotionalAndPositiveMental()
        {
            _vernStats.Emotional.SetValue(-20f); // -33 to 0
            _vernStats.Mental.SetValue(50f);     // +33 to +66
            _vernStats.Physical.SetValue(0f);    // -33 to +66

            var mood = _vernStats.CalculateMoodType();

            AssertThat(mood == VernMoodType.Gruff);
        }

        [Test]
        public void StatsChanged_EmitsOnValueChange()
        {
            bool called = false;
            _vernStats.Connect("StatsChanged", Callable.From(() => called = true));

            _vernStats.Physical.Modify(-10f);

            AssertThat(called);
        }

        [Test]
        public void VibeChanged_EmitsWhenVibeChanges()
        {
            float initialVibe = _vernStats.CalculateVIBE();
            float? newVibe = null;
            _vernStats.Connect("VibeChanged", Callable.From<float>((vibe) => newVibe = vibe));

            _vernStats.Emotional.Modify(50f);  // Emotional has highest weight (0.40)

            AssertThat(newVibe != null, $"VibeChanged did not fire. Initial vibe: {initialVibe}");
        }

        [Test]
        public void GetCaffeineDecayModifier_HighMental_SlowsDecay()
        {
            _vernStats.Mental.SetValue(100f);

            float modifier = _vernStats.GetCaffeineDecayModifier();

            // At Mental +100: modifier = 1 - (100/100) = 0 (capped to 0.5)
            AssertThat(Mathf.IsEqualApprox(modifier, 0.5f), $"Expected 0.5, got {modifier}");
        }

        [Test]
        public void GetNicotineDecayModifier_HighEmotional_SlowsDecay()
        {
            _vernStats.Emotional.SetValue(100f);

            float modifier = _vernStats.GetNicotineDecayModifier();

            // At Emotional +100: modifier = 1 - (100/100) = 0 (capped to 0.5)
            AssertThat(Mathf.IsEqualApprox(modifier, 0.5f), $"Expected 0.5, got {modifier}");
        }

        [Test]
        public void GetCaffeineDecayModifier_LowMental_AcceleratesDecay()
        {
            _vernStats.Mental.SetValue(-100f);

            float modifier = _vernStats.GetCaffeineDecayModifier();

            // At Mental -100: modifier = 1 - (-100/100) = 2.0
            AssertThat(Mathf.IsEqualApprox(modifier, 2.0f), $"Expected 2.0, got {modifier}");
        }

        [Test]
        public void GetNicotineDecayModifier_LowEmotional_AcceleratesDecay()
        {
            _vernStats.Emotional.SetValue(-100f);

            float modifier = _vernStats.GetNicotineDecayModifier();

            // At Emotional -100: modifier = 1 - (-100/100) = 2.0
            AssertThat(Mathf.IsEqualApprox(modifier, 2.0f), $"Expected 2.0, got {modifier}");
        }

        [Test]
        public void ApplyGoodCallerEffects_IncreasesStats()
        {
            float initialPhysical = _vernStats.Physical.Value;
            float initialEmotional = _vernStats.Emotional.Value;
            float initialMental = _vernStats.Mental.Value;

            _vernStats.ApplyGoodCallerEffects();

            AssertThat(_vernStats.Physical.Value > initialPhysical);
            AssertThat(_vernStats.Emotional.Value > initialEmotional);
            AssertThat(_vernStats.Mental.Value > initialMental);
        }

        [Test]
        public void ApplyBadCallerEffects_DecreasesStats()
        {
            float initialEmotional = _vernStats.Emotional.Value;
            float initialMental = _vernStats.Mental.Value;

            _vernStats.ApplyBadCallerEffects();

            AssertThat(_vernStats.Emotional.Value < initialEmotional);
            AssertThat(_vernStats.Mental.Value < initialMental);
        }

        [Test]
        public void UseCoffee_RestoresCaffeineAndBoostsPhysical()
        {
            _vernStats.Caffeine.SetValue(50f);  // Partial caffeine
            float initialPhysical = _vernStats.Physical.Value;

            _vernStats.UseCoffee();

            AssertThat(_vernStats.Caffeine.Value == 100f);
            AssertThat(_vernStats.Physical.Value > initialPhysical);
        }

        [Test]
        public void UseCigarette_RestoresNicotineAndBoostsEmotional()
        {
            _vernStats.Nicotine.SetValue(50f);  // Partial nicotine
            float initialEmotional = _vernStats.Emotional.Value;

            _vernStats.UseCigarette();

            AssertThat(_vernStats.Nicotine.Value == 100f);
            AssertThat(_vernStats.Emotional.Value > initialEmotional);
        }
    }
}
