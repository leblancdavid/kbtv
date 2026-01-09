using NUnit.Framework;
using UnityEngine;
using KBTV.Data;

namespace KBTV.Tests
{
    /// <summary>
    /// Unit tests for the VernStats ScriptableObject.
    /// </summary>
    public class VernStatsTests
    {
        private VernStats _stats;

        [SetUp]
        public void SetUp()
        {
            _stats = ScriptableObject.CreateInstance<VernStats>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_stats);
        }

        [Test]
        public void Initialize_CreatesAllStatObjects()
        {
            _stats.Initialize();

            Assert.IsNotNull(_stats.Mood);
            Assert.IsNotNull(_stats.Energy);
            Assert.IsNotNull(_stats.Hunger);
            Assert.IsNotNull(_stats.Thirst);
            Assert.IsNotNull(_stats.Patience);
            Assert.IsNotNull(_stats.Susceptibility);
            Assert.IsNotNull(_stats.Belief);
        }

        [Test]
        public void Initialize_SetsDefaultValues()
        {
            _stats.Initialize();

            // Default initial values from VernStats serialized fields
            Assert.AreEqual(50f, _stats.Mood.Value);      // _initialMood = 50
            Assert.AreEqual(75f, _stats.Energy.Value);    // _initialEnergy = 75
            Assert.AreEqual(30f, _stats.Hunger.Value);    // _initialHunger = 30
            Assert.AreEqual(30f, _stats.Thirst.Value);    // _initialThirst = 30
            Assert.AreEqual(50f, _stats.Patience.Value);  // _initialPatience = 50
            Assert.AreEqual(50f, _stats.Susceptibility.Value); // _initialSusceptibility = 50
            Assert.AreEqual(25f, _stats.Belief.Value);    // _initialBelief = 25
        }

        [Test]
        public void Initialize_FiresOnStatsChanged()
        {
            int callCount = 0;
            _stats.OnStatsChanged += () => callCount++;

            _stats.Initialize();

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void CalculateShowQuality_ReturnsBetweenZeroAndOne()
        {
            _stats.Initialize();

            float quality = _stats.CalculateShowQuality();

            Assert.GreaterOrEqual(quality, 0f);
            Assert.LessOrEqual(quality, 1f);
        }

        [Test]
        public void CalculateShowQuality_HigherWithHighBelief()
        {
            _stats.Initialize();
            float lowBeliefQuality = _stats.CalculateShowQuality();

            _stats.Belief.SetValue(100f);
            float highBeliefQuality = _stats.CalculateShowQuality();

            Assert.Greater(highBeliefQuality, lowBeliefQuality);
        }

        [Test]
        public void CalculateShowQuality_BeliefHasHighestWeight()
        {
            _stats.Initialize();

            // Set all stats to 0
            _stats.Mood.SetValue(0f);
            _stats.Energy.SetValue(0f);
            _stats.Hunger.SetValue(0f);
            _stats.Thirst.SetValue(0f);
            _stats.Patience.SetValue(0f);
            _stats.Belief.SetValue(0f);

            float baseQuality = _stats.CalculateShowQuality();

            // Now only set Belief to max
            _stats.Belief.SetValue(100f);
            float beliefOnlyQuality = _stats.CalculateShowQuality();

            // Reset and only set Mood to max
            _stats.Belief.SetValue(0f);
            _stats.Mood.SetValue(100f);
            float moodOnlyQuality = _stats.CalculateShowQuality();

            // Belief should have more impact (40% weight vs 25% for mood)
            float beliefImpact = beliefOnlyQuality - baseQuality;
            float moodImpact = moodOnlyQuality - baseQuality;

            Assert.Greater(beliefImpact, moodImpact);
        }

        [Test]
        public void CalculateShowQuality_HighHungerReducesQuality()
        {
            _stats.Initialize();

            _stats.Hunger.SetValue(0f);
            float lowHungerQuality = _stats.CalculateShowQuality();

            _stats.Hunger.SetValue(100f);
            float highHungerQuality = _stats.CalculateShowQuality();

            Assert.Less(highHungerQuality, lowHungerQuality);
        }

        [Test]
        public void ApplyDecay_ReducesEnergy()
        {
            _stats.Initialize();
            float initialEnergy = _stats.Energy.Value;

            _stats.ApplyDecay(1f);

            Assert.Less(_stats.Energy.Value, initialEnergy);
        }

        [Test]
        public void ApplyDecay_IncreasesHunger()
        {
            _stats.Initialize();
            float initialHunger = _stats.Hunger.Value;

            _stats.ApplyDecay(1f);

            Assert.Greater(_stats.Hunger.Value, initialHunger);
        }

        [Test]
        public void ApplyDecay_IncreasesThirst()
        {
            _stats.Initialize();
            float initialThirst = _stats.Thirst.Value;

            _stats.ApplyDecay(1f);

            Assert.Greater(_stats.Thirst.Value, initialThirst);
        }

        [Test]
        public void ApplyDecay_HighHungerReducesMood()
        {
            _stats.Initialize();

            // Set hunger above threshold (70)
            _stats.Hunger.SetValue(80f);
            float initialMood = _stats.Mood.Value;

            _stats.ApplyDecay(1f);

            Assert.Less(_stats.Mood.Value, initialMood);
        }

        [Test]
        public void ApplyDecay_HighThirstReducesMood()
        {
            _stats.Initialize();

            // Set thirst above threshold (70)
            _stats.Thirst.SetValue(80f);
            float initialMood = _stats.Mood.Value;

            _stats.ApplyDecay(1f);

            Assert.Less(_stats.Mood.Value, initialMood);
        }

        [Test]
        public void ApplyDecay_LowNeedsDoNotReduceMood()
        {
            _stats.Initialize();

            // Set both hunger and thirst below threshold
            _stats.Hunger.SetValue(30f);
            _stats.Thirst.SetValue(30f);
            float initialMood = _stats.Mood.Value;

            _stats.ApplyDecay(1f);

            // Mood should be unchanged (no penalty from needs)
            Assert.AreEqual(initialMood, _stats.Mood.Value, 0.001f);
        }

        [Test]
        public void ApplyDecay_RespectsMultiplier()
        {
            _stats.Initialize();
            float initialEnergy = _stats.Energy.Value;

            // Apply with 2x multiplier
            _stats.ApplyDecay(1f, 2f);
            float energyAfter2x = initialEnergy - _stats.Energy.Value;

            // Reset and apply with 1x
            _stats.Initialize();
            _stats.ApplyDecay(1f, 1f);
            float energyAfter1x = initialEnergy - _stats.Energy.Value;

            // 2x should drain twice as fast (allowing for floating point tolerance)
            Assert.AreEqual(energyAfter2x, energyAfter1x * 2f, 0.01f);
        }

        [Test]
        public void ApplyDecay_ReducesPatience()
        {
            _stats.Initialize();
            float initialPatience = _stats.Patience.Value;

            _stats.ApplyDecay(1f);

            Assert.Less(_stats.Patience.Value, initialPatience);
        }

        [Test]
        public void OnStatsChanged_FiresWhenStatChanges()
        {
            _stats.Initialize();
            int callCount = 0;
            _stats.OnStatsChanged += () => callCount++;

            _stats.Mood.SetValue(75f);

            Assert.AreEqual(1, callCount);
        }
    }
}
