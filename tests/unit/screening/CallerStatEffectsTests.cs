using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Data;
using KBTV.Screening;
using System.Collections.Generic;
using System.Linq;

namespace KBTV.Tests.Unit.Screening
{
    /// <summary>
    /// Tests for CallerStatEffects using the v2 Three-Stat System:
    /// - Physical: Energy, stamina, reaction time
    /// - Emotional: Mood, morale, patience, passion
    /// - Mental: Discernment, focus, patience (cognitive)
    /// </summary>
    public class CallerStatEffectsTests : KBTVTestClass
    {
        public CallerStatEffectsTests(Node testScene) : base(testScene) { }

        #region EmotionalState Tests

        [Test]
        public void GetStatEffects_EmotionalState_Calm_ReturnsAllThreeStats()
        {
            var effects = CallerStatEffects.GetStatEffects("EmotionalState", CallerEmotionalState.Calm);

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == 2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == 2f));
        }

        [Test]
        public void GetStatEffects_EmotionalState_Anxious_ReturnsAllThreeStatsMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("EmotionalState", CallerEmotionalState.Anxious);

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == -2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == -1f));
        }

        [Test]
        public void GetStatEffects_EmotionalState_Excited_ReturnsAllThreeStats()
        {
            var effects = CallerStatEffects.GetStatEffects("EmotionalState", CallerEmotionalState.Excited);

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == 3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 4f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == -2f));
        }

        [Test]
        public void GetStatEffects_EmotionalState_Scared_ReturnsAllThreeStats()
        {
            var effects = CallerStatEffects.GetStatEffects("EmotionalState", CallerEmotionalState.Scared);

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == -3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == 2f));
        }

        [Test]
        public void GetStatEffects_EmotionalState_Angry_ReturnsAllThreeStatsMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("EmotionalState", CallerEmotionalState.Angry);

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == -3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -5f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == -3f));
        }

        #endregion

        #region CurseRisk Tests

        [Test]
        public void GetStatEffects_CurseRisk_Low_ReturnsAllThreeStatsPlus()
        {
            var effects = CallerStatEffects.GetStatEffects("CurseRisk", CallerCurseRisk.Low);

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == 1f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == 1f));
        }

        [Test]
        public void GetStatEffects_CurseRisk_Medium_ReturnsEmotionalAndMentalMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("CurseRisk", CallerCurseRisk.Medium);

            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -1f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == -2f));
        }

        [Test]
        public void GetStatEffects_CurseRisk_High_ReturnsAllThreeStatsMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("CurseRisk", CallerCurseRisk.High);

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == -2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == -3f));
        }

        #endregion

        #region Coherence Tests

        [Test]
        public void GetStatEffects_Coherence_Coherent_ReturnsAllThreeStatsPlus()
        {
            var effects = CallerStatEffects.GetStatEffects("Coherence", CallerCoherence.Coherent);

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == 2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == 4f));
        }

        [Test]
        public void GetStatEffects_Coherence_Questionable_ReturnsEmotionalAndMentalMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("Coherence", CallerCoherence.Questionable);

            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -1f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == -2f));
        }

        [Test]
        public void GetStatEffects_Coherence_Incoherent_ReturnsAllThreeStatsMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("Coherence", CallerCoherence.Incoherent);

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == -2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == -5f));
        }

        #endregion

        #region Urgency Tests

        [Test]
        public void GetStatEffects_Urgency_Low_ReturnsAllThreeStatsMixed()
        {
            var effects = CallerStatEffects.GetStatEffects("Urgency", CallerUrgency.Low);

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == 2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -1f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == 1f));
        }

        [Test]
        public void GetStatEffects_Urgency_Critical_ReturnsAllThreeStatsMixed()
        {
            var effects = CallerStatEffects.GetStatEffects("Urgency", CallerUrgency.Critical);

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == -3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == -2f));
        }

        #endregion

        #region BeliefLevel Tests

        [Test]
        public void GetStatEffects_BeliefLevel_Curious_ReturnsAllThreeStatsPlus()
        {
            var effects = CallerStatEffects.GetStatEffects("BeliefLevel", CallerBeliefLevel.Curious);

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == 2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == 2f));
        }

        [Test]
        public void GetStatEffects_BeliefLevel_Zealot_ReturnsAllThreeStatsMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("BeliefLevel", CallerBeliefLevel.Zealot);

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == -3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -4f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == -4f));
        }

        #endregion

        #region EvidenceLevel Tests

        [Test]
        public void GetStatEffects_Evidence_None_ReturnsAllThreeStatsMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("Evidence", CallerEvidenceLevel.None);

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == -2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == -1f));
        }

        [Test]
        public void GetStatEffects_Evidence_Irrefutable_ReturnsAllThreeStatsPlus()
        {
            var effects = CallerStatEffects.GetStatEffects("Evidence", CallerEvidenceLevel.Irrefutable);

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == 3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 5f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == 3f));
        }

        #endregion

        #region Legitimacy Tests

        [Test]
        public void GetStatEffects_Legitimacy_Fake_ReturnsAllThreeStatsMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("Legitimacy", CallerLegitimacy.Fake);

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == -2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -5f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == -4f));
        }

        [Test]
        public void GetStatEffects_Legitimacy_Credible_ReturnsEmotionalAndMentalPlus()
        {
            var effects = CallerStatEffects.GetStatEffects("Legitimacy", CallerLegitimacy.Credible);

            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 1f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == 1f));
        }

        [Test]
        public void GetStatEffects_Legitimacy_Compelling_ReturnsAllThreeStatsPlus()
        {
            var effects = CallerStatEffects.GetStatEffects("Legitimacy", CallerLegitimacy.Compelling);

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == 2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 4f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == 3f));
        }

        #endregion

        #region AudioQuality (PhoneQuality) Tests

        [Test]
        public void GetStatEffects_AudioQuality_Terrible_ReturnsAllThreeStatsMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("AudioQuality", CallerPhoneQuality.Terrible);

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == -2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == -3f));
        }

        [Test]
        public void GetStatEffects_AudioQuality_Good_ReturnsAllThreeStatsPlus()
        {
            var effects = CallerStatEffects.GetStatEffects("AudioQuality", CallerPhoneQuality.Good);

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == 2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == 2f));
        }

        [Test]
        public void GetStatEffects_AudioQuality_Average_ReturnsNoEffects()
        {
            var effects = CallerStatEffects.GetStatEffects("AudioQuality", CallerPhoneQuality.Average);

            AssertThat(effects.Count == 0);
        }

        #endregion

        #region No-Effect Properties Tests

        [Test]
        public void GetStatEffects_Summary_ReturnsNoEffects()
        {
            var effects = CallerStatEffects.GetStatEffects("Summary", "This is a test summary");

            AssertThat(effects.Count == 0);
        }

        [Test]
        public void GetStatEffects_Topic_ReturnsNoEffects()
        {
            var effects = CallerStatEffects.GetStatEffects("Topic", "Ghosts");

            AssertThat(effects.Count == 0);
        }

        [Test]
        public void GetStatEffects_Personality_KnownPersonality_ReturnsEffects()
        {
            var effects = CallerStatEffects.GetStatEffects("Personality", "Matter-of-fact reporter");

            AssertThat(effects.Count == 3);
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == 2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 1f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == 3f));
        }

        [Test]
        public void GetStatEffects_Personality_UnknownPersonality_ReturnsNoEffects()
        {
            var effects = CallerStatEffects.GetStatEffects("Personality", "unknown_personality");

            AssertThat(effects.Count == 0);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void GetStatEffects_NullValue_ReturnsEmptyList()
        {
            var effects = CallerStatEffects.GetStatEffects("EmotionalState", null!);

            AssertThat(effects != null);
            AssertThat(effects.Count == 0);
        }

        [Test]
        public void GetStatEffects_UnknownProperty_ReturnsEmptyList()
        {
            var effects = CallerStatEffects.GetStatEffects("UnknownProperty", "some value");

            AssertThat(effects != null);
            AssertThat(effects.Count == 0);
        }

        #endregion

        #region Aggregation Tests

        [Test]
        public void AggregateStatEffects_EmptyProperties_ReturnsEmptyDictionary()
        {
            var properties = new List<ScreenableProperty>();
            var totals = CallerStatEffects.AggregateStatEffects(properties);

            AssertThat(totals.Count == 0);
        }

        [Test]
        public void AggregateStatEffects_RevealedOnly_OnlyIncludesRevealed()
        {
            // Use explicit StatModification lists to test aggregation behavior
            var properties = new List<ScreenableProperty>
            {
                new ScreenableProperty("EmotionalState", "Emotional State", CallerEmotionalState.Calm, "Calm", 3f,
                    new List<StatModification>
                    {
                        new StatModification(StatType.Physical, 2f),
                        new StatModification(StatType.Emotional, 3f),
                        new StatModification(StatType.Mental, 2f)
                    }),
                new ScreenableProperty("CurseRisk", "Curse Risk", CallerCurseRisk.High, "High", 3f,
                    new List<StatModification>
                    {
                        new StatModification(StatType.Physical, -2f),
                        new StatModification(StatType.Emotional, -3f),
                        new StatModification(StatType.Mental, -3f)
                    })
            };

            // Reveal only the first property
            properties[0].Update(5f);

            var totals = CallerStatEffects.AggregateStatEffects(properties, revealedOnly: true);

            AssertThat(totals.Count == 3);
            AssertThat(totals[StatType.Physical] == 2f);
            AssertThat(totals[StatType.Emotional] == 3f);
            AssertThat(totals[StatType.Mental] == 2f);
        }

        [Test]
        public void AggregateStatEffects_AllProperties_IncludesAll()
        {
            // Use explicit StatModification lists to test aggregation behavior
            var properties = new List<ScreenableProperty>
            {
                new ScreenableProperty("EmotionalState", "Emotional State", CallerEmotionalState.Calm, "Calm", 3f,
                    new List<StatModification>
                    {
                        new StatModification(StatType.Physical, 2f),
                        new StatModification(StatType.Emotional, 3f),
                        new StatModification(StatType.Mental, 2f)
                    }),
                new ScreenableProperty("CurseRisk", "Curse Risk", CallerCurseRisk.High, "High", 3f,
                    new List<StatModification>
                    {
                        new StatModification(StatType.Physical, -2f),
                        new StatModification(StatType.Emotional, -3f),
                        new StatModification(StatType.Mental, -3f)
                    })
            };

            var totals = CallerStatEffects.AggregateStatEffects(properties, revealedOnly: false);

            AssertThat(totals.Count == 3);
            // Calm: Ph +2, Em +3, Me +2
            // High CurseRisk: Ph -2, Em -3, Me -3
            // Totals: Ph 0, Em 0, Me -1
            AssertThat(totals[StatType.Physical] == 0f);
            AssertThat(totals[StatType.Emotional] == 0f);
            AssertThat(totals[StatType.Mental] == -1f);
        }

        [Test]
        public void AggregateStatEffects_MultipleStats_AggregatesCorrectly()
        {
            // Use explicit StatModification lists matching the new stat effects
            var properties = new List<ScreenableProperty>
            {
                new ScreenableProperty("EmotionalState", "Emotional State", CallerEmotionalState.Angry, "Angry", 3f,
                    new List<StatModification>
                    {
                        new StatModification(StatType.Physical, -3f),
                        new StatModification(StatType.Emotional, -5f),
                        new StatModification(StatType.Mental, -3f)
                    }),
                new ScreenableProperty("Coherence", "Coherence", CallerCoherence.Coherent, "Coherent", 4f,
                    new List<StatModification>
                    {
                        new StatModification(StatType.Physical, 2f),
                        new StatModification(StatType.Emotional, 2f),
                        new StatModification(StatType.Mental, 4f)
                    })
            };

            // Reveal all
            foreach (var prop in properties)
            {
                prop.Update(5f);
            }

            var totals = CallerStatEffects.AggregateStatEffects(properties);

            AssertThat(totals.Count == 3);
            // Angry: Ph -3, Em -5, Me -3
            // Coherent: Ph +2, Em +2, Me +4
            // Totals: Ph -1, Em -3, Me +1
            AssertThat(totals[StatType.Physical] == -1f);
            AssertThat(totals[StatType.Emotional] == -3f);
            AssertThat(totals[StatType.Mental] == 1f);
        }

        #endregion
    }
}
