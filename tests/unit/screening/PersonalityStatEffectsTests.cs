using Chickensoft.GoDotTest;
using Godot;
using KBTV.Data;
using KBTV.Screening;
using System.Linq;

namespace KBTV.Tests.Unit.Screening
{
    /// <summary>
    /// Tests for PersonalityStatEffects - verifies that each personality type
    /// has correctly defined stat effects.
    /// </summary>
    public class PersonalityStatEffectsTests : KBTVTestClass
    {
        public PersonalityStatEffectsTests(Node testScene) : base(testScene) { }

        #region Positive Personalities

        [Test]
        public void GetEffects_MatterOfFactReporter_ReturnsPositiveEffects()
        {
            var effects = PersonalityStatEffects.GetEffects("Matter-of-fact reporter");

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == 2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 1f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == 3f));
        }

        [Test]
        public void GetEffects_AcademicResearcher_ReturnsEmotionalAndMentalBoost()
        {
            var effects = PersonalityStatEffects.GetEffects("Academic researcher");

            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == 3f));
        }

        [Test]
        public void GetEffects_TrueBeliever_ReturnsAllThreeStatsPositive()
        {
            var effects = PersonalityStatEffects.GetEffects("True believer");

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == 1f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == 2f));
        }

        #endregion

        #region Negative Personalities

        [Test]
        public void GetEffects_AttentionSeeker_ReturnsNegativeEffects()
        {
            var effects = PersonalityStatEffects.GetEffects("Attention seeker");

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == -2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == -1f));
        }

        [Test]
        public void GetEffects_ConspiracyTheorist_ReturnsNegativeEffects()
        {
            var effects = PersonalityStatEffects.GetEffects("Conspiracy theorist");

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == -1f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == -3f));
        }

        [Test]
        public void GetEffects_ChronicInterrupter_ReturnsHighNegativeEffects()
        {
            var effects = PersonalityStatEffects.GetEffects("Chronic interrupter");

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == -2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == -3f));
        }

        #endregion

        #region Neutral Personalities

        [Test]
        public void GetEffects_NervousButSincere_ReturnsMixedEffects()
        {
            var effects = PersonalityStatEffects.GetEffects("Nervous but sincere");

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == -1f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == 1f));
        }

        [Test]
        public void GetEffects_OverlyEnthusiastic_ReturnsMixedEffects()
        {
            var effects = PersonalityStatEffects.GetEffects("Overly enthusiastic");

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == 2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == -2f));
        }

        [Test]
        public void GetEffects_ReluctantWitness_ReturnsMixedEffects()
        {
            var effects = PersonalityStatEffects.GetEffects("Reluctant witness");

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == 2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -1f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == 2f));
        }

        #endregion

        #region Edge Cases

        [Test]
        public void GetEffects_UnknownPersonality_ReturnsEmptyList()
        {
            var effects = PersonalityStatEffects.GetEffects("Unknown personality name");

            AssertThat(effects != null);
            AssertThat(effects.Count == 0);
        }

        [Test]
        public void GetEffects_NullPersonality_ReturnsEmptyList()
        {
            var effects = PersonalityStatEffects.GetEffects(null!);

            AssertThat(effects != null);
            AssertThat(effects.Count == 0);
        }

        [Test]
        public void GetEffects_EmptyString_ReturnsEmptyList()
        {
            var effects = PersonalityStatEffects.GetEffects("");

            AssertThat(effects != null);
            AssertThat(effects.Count == 0);
        }

        #endregion

        #region HasEffects Tests

        [Test]
        public void HasEffects_KnownPersonality_ReturnsTrue()
        {
            AssertThat(PersonalityStatEffects.HasEffects("Matter-of-fact reporter"));
            AssertThat(PersonalityStatEffects.HasEffects("Attention seeker"));
            AssertThat(PersonalityStatEffects.HasEffects("Nervous but sincere"));
        }

        [Test]
        public void HasEffects_UnknownPersonality_ReturnsFalse()
        {
            AssertThat(!PersonalityStatEffects.HasEffects("Unknown"));
            AssertThat(!PersonalityStatEffects.HasEffects(""));
            AssertThat(!PersonalityStatEffects.HasEffects(null!));
        }

        #endregion

        #region GetAllPersonalityNames Tests

        [Test]
        public void GetAllPersonalityNames_Returns36Personalities()
        {
            var names = PersonalityStatEffects.GetAllPersonalityNames().ToList();

            AssertThat(names.Count == 36);
        }

        [Test]
        public void GetAllPersonalityNames_ContainsExpectedNames()
        {
            var names = PersonalityStatEffects.GetAllPersonalityNames().ToList();

            // Positive
            AssertThat(names.Contains("Matter-of-fact reporter"));
            AssertThat(names.Contains("Academic researcher"));
            AssertThat(names.Contains("True believer"));

            // Negative
            AssertThat(names.Contains("Attention seeker"));
            AssertThat(names.Contains("Conspiracy theorist"));
            AssertThat(names.Contains("Chronic interrupter"));

            // Neutral
            AssertThat(names.Contains("Nervous but sincere"));
            AssertThat(names.Contains("Overly enthusiastic"));
            AssertThat(names.Contains("Reluctant witness"));
        }

        #endregion

        #region Effect Totals Validation

        [Test]
        public void PositivePersonalities_HaveTotalEffectBetween5And6()
        {
            var positivePersonalities = new[]
            {
                "Matter-of-fact reporter", "Academic researcher", "Local history buff",
                "Frequent listener", "Genuinely frightened", "True believer",
                "Retired professional", "Careful observer", "Soft-spoken witness",
                "Articulate storyteller", "Patient explainer", "Earnest truth-seeker"
            };

            foreach (var name in positivePersonalities)
            {
                var effects = PersonalityStatEffects.GetEffects(name);
                var total = effects.Sum(e => e.Amount);
                AssertThat(total >= 5f && total <= 6f, $"{name} has total {total}, expected 5-6");
            }
        }

        [Test]
        public void NegativePersonalities_HaveTotalEffectBetweenMinus6AndMinus8()
        {
            var negativePersonalities = new[]
            {
                "Attention seeker", "Conspiracy theorist", "Rambling storyteller",
                "Joker type", "Monotone delivery", "Skeptical witness",
                "Know-it-all", "Chronic interrupter", "Drama queen",
                "Mumbling caller", "Easily distracted", "Defensive storyteller"
            };

            foreach (var name in negativePersonalities)
            {
                var effects = PersonalityStatEffects.GetEffects(name);
                var total = effects.Sum(e => e.Amount);
                AssertThat(total >= -8f && total <= -6f, $"{name} has total {total}, expected -8 to -6");
            }
        }

        [Test]
        public void NeutralPersonalities_HaveTotalEffectBetweenMinus2And4()
        {
            var neutralPersonalities = new[]
            {
                "Nervous but sincere", "Overly enthusiastic", "First-time caller",
                "Desperate for answers", "Reluctant witness", "Excitable narrator",
                "Quiet observer", "Chatty neighbor", "Late-night insomniac",
                "Curious skeptic", "Nostalgic elder", "Breathless reporter"
            };

            foreach (var name in neutralPersonalities)
            {
                var effects = PersonalityStatEffects.GetEffects(name);
                var total = effects.Sum(e => e.Amount);
                AssertThat(total >= -2f && total <= 4f, $"{name} has total {total}, expected -2 to +4");
            }
        }

        #endregion
    }
}
