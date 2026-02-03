using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Data;
using KBTV.Screening;
using System.Collections.Generic;

namespace KBTV.Tests.Unit.Screening
{
    public class ScreenablePropertyTests : KBTVTestClass
    {
        public ScreenablePropertyTests(Node testScene) : base(testScene) { }

        [Test]
        public void Constructor_SetsPropertiesCorrectly()
        {
            var statEffects = new List<StatModification>
            {
                new StatModification(StatType.Emotional, 3f)
            };

            var property = new ScreenableProperty(
                "EmotionalState",
                "Emotional State",
                CallerEmotionalState.Calm,
                "Calm",
                3f,
                statEffects
            );

            AssertThat(property.PropertyKey == "EmotionalState");
            AssertThat(property.DisplayName == "Emotional State");
            AssertThat(property.Value.Equals(CallerEmotionalState.Calm));
            AssertThat(property.DisplayValue == "Calm");
            AssertThat(property.RevealDuration == 3f);
            AssertThat(property.StatEffects.Count == 1);
        }

        [Test]
        public void Constructor_InitializesToHiddenState()
        {
            var property = new ScreenableProperty(
                "TestProperty",
                "Test Property",
                "value",
                "Value",
                2f
            );

            AssertThat(property.State == RevelationState.Hidden);
            AssertThat(property.ElapsedTime == 0f);
            AssertThat(!property.IsRevealed);
            AssertThat(!property.IsRevealing);
        }

        [Test]
        public void Update_TransitionsFromHiddenToRevealing()
        {
            var property = new ScreenableProperty(
                "TestProperty",
                "Test Property",
                "value",
                "Value",
                3f
            );

            property.Update(0.1f);

            AssertThat(property.State == RevelationState.Revealing);
            AssertThat(property.IsRevealing);
            AssertThat(!property.IsRevealed);
        }

        [Test]
        public void Update_AccumulatesElapsedTime()
        {
            var property = new ScreenableProperty(
                "TestProperty",
                "Test Property",
                "value",
                "Value",
                3f
            );

            property.Update(1f);
            property.Update(0.5f);

            AssertThat(property.ElapsedTime == 1.5f);
        }

        [Test]
        public void Update_TransitionsToRevealedWhenDurationExceeded()
        {
            var property = new ScreenableProperty(
                "TestProperty",
                "Test Property",
                "value",
                "Value",
                2f
            );

            property.Update(3f);

            AssertThat(property.State == RevelationState.Revealed);
            AssertThat(property.IsRevealed);
            AssertThat(!property.IsRevealing);
        }

        [Test]
        public void Update_DoesNotChangeAfterRevealed()
        {
            var property = new ScreenableProperty(
                "TestProperty",
                "Test Property",
                "value",
                "Value",
                2f
            );

            property.Update(3f);
            float elapsedAfterReveal = property.ElapsedTime;

            property.Update(5f);

            // Elapsed time should not increase after revealed
            AssertThat(property.State == RevelationState.Revealed);
        }

        [Test]
        public void Progress_CalculatesCorrectly()
        {
            var property = new ScreenableProperty(
                "TestProperty",
                "Test Property",
                "value",
                "Value",
                4f
            );

            property.Update(1f);
            AssertThat(property.Progress == 0.25f);

            property.Update(1f);
            AssertThat(property.Progress == 0.5f);

            property.Update(2f);
            AssertThat(property.Progress == 1f);
        }

        [Test]
        public void Progress_ClampsToOne()
        {
            var property = new ScreenableProperty(
                "TestProperty",
                "Test Property",
                "value",
                "Value",
                2f
            );

            property.Update(10f);

            AssertThat(property.Progress == 1f);
        }

        [Test]
        public void Reset_ResetsToHiddenState()
        {
            var property = new ScreenableProperty(
                "TestProperty",
                "Test Property",
                "value",
                "Value",
                2f
            );

            property.Update(3f);
            AssertThat(property.IsRevealed);

            property.Reset();

            AssertThat(property.State == RevelationState.Hidden);
            AssertThat(property.ElapsedTime == 0f);
            AssertThat(!property.IsRevealed);
        }

        [Test]
        public void StatEffects_DefaultsToEmptyList()
        {
            var property = new ScreenableProperty(
                "TestProperty",
                "Test Property",
                "value",
                "Value",
                2f
            );

            AssertThat(property.StatEffects != null);
            AssertThat(property.StatEffects.Count == 0);
        }

        [Test]
        public void GetStatEffectDisplays_ReturnsFormattedEffects()
        {
            var statEffects = new List<StatModification>
            {
                new StatModification(StatType.Emotional, 3f),
                new StatModification(StatType.Physical, -2f)
            };

            var property = new ScreenableProperty(
                "EmotionalState",
                "Emotional State",
                CallerEmotionalState.Calm,
                "Calm",
                3f,
                statEffects
            );

            var displays = property.GetStatEffectDisplays();

            AssertThat(displays.Count == 2);

            // First effect: Emotional +3 (positive)
            AssertThat(displays[0].StatType == StatType.Emotional);
            AssertThat(displays[0].IsPositive);
            AssertThat(displays[0].Amount == 3f);
            AssertThat(displays[0].Text.Contains("Em"));

            // Second effect: Physical -2 (negative)
            AssertThat(displays[1].StatType == StatType.Physical);
            AssertThat(!displays[1].IsPositive);
            AssertThat(displays[1].Amount == -2f);
            AssertThat(displays[1].Text.Contains("Ph"));
        }

        [Test]
        public void GetStatEffectDisplays_EmptyForNoEffects()
        {
            var property = new ScreenableProperty(
                "Summary",
                "Summary",
                "Test summary",
                "Test summary",
                4f
            );

            var displays = property.GetStatEffectDisplays();

            AssertThat(displays.Count == 0);
        }
    }
}
