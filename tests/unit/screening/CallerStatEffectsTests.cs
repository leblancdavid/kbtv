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
        public void GetStatEffects_EmotionalState_Calm_ReturnsEmotionalPlus()
        {
            var effects = CallerStatEffects.GetStatEffects("EmotionalState", CallerEmotionalState.Calm);

            AssertThat(effects.Count == 1);
            AssertThat(effects[0].StatType == StatType.Emotional);
            AssertThat(effects[0].Amount == 3f);
        }

        [Test]
        public void GetStatEffects_EmotionalState_Anxious_ReturnsEmotionalMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("EmotionalState", CallerEmotionalState.Anxious);

            AssertThat(effects.Count == 1);
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -3f));
        }

        [Test]
        public void GetStatEffects_EmotionalState_Excited_ReturnsEmotionalAndPhysicalPlus()
        {
            var effects = CallerStatEffects.GetStatEffects("EmotionalState", CallerEmotionalState.Excited);

            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Physical && e.Amount == 2f));
        }

        [Test]
        public void GetStatEffects_EmotionalState_Scared_ReturnsEmotionalMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("EmotionalState", CallerEmotionalState.Scared);

            AssertThat(effects.Count == 1);
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -5f));
        }

        [Test]
        public void GetStatEffects_EmotionalState_Angry_ReturnsHighEmotionalMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("EmotionalState", CallerEmotionalState.Angry);

            AssertThat(effects.Count == 1);
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -8f));
        }

        #endregion

        #region CurseRisk Tests

        [Test]
        public void GetStatEffects_CurseRisk_Low_ReturnsNoEffects()
        {
            var effects = CallerStatEffects.GetStatEffects("CurseRisk", CallerCurseRisk.Low);

            AssertThat(effects.Count == 0);
        }

        [Test]
        public void GetStatEffects_CurseRisk_Medium_ReturnsEmotionalMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("CurseRisk", CallerCurseRisk.Medium);

            AssertThat(effects.Count == 1);
            AssertThat(effects[0].StatType == StatType.Emotional);
            AssertThat(effects[0].Amount == -2f);
        }

        [Test]
        public void GetStatEffects_CurseRisk_High_ReturnsEmotionalMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("CurseRisk", CallerCurseRisk.High);

            AssertThat(effects.Count == 1);
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -6f));
        }

        #endregion

        #region Coherence Tests

        [Test]
        public void GetStatEffects_Coherence_Coherent_ReturnsMentalPlus()
        {
            var effects = CallerStatEffects.GetStatEffects("Coherence", CallerCoherence.Coherent);

            AssertThat(effects.Count == 1);
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == 3f));
        }

        [Test]
        public void GetStatEffects_Coherence_Questionable_ReturnsMentalMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("Coherence", CallerCoherence.Questionable);

            AssertThat(effects.Count == 1);
            AssertThat(effects[0].StatType == StatType.Mental);
            AssertThat(effects[0].Amount == -2f);
        }

        [Test]
        public void GetStatEffects_Coherence_Incoherent_ReturnsMentalMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("Coherence", CallerCoherence.Incoherent);

            AssertThat(effects.Count == 1);
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == -7f));
        }

        #endregion

        #region Urgency Tests

        [Test]
        public void GetStatEffects_Urgency_Low_ReturnsNoEffects()
        {
            var effects = CallerStatEffects.GetStatEffects("Urgency", CallerUrgency.Low);

            AssertThat(effects.Count == 0);
        }

        [Test]
        public void GetStatEffects_Urgency_Critical_ReturnsEmotionalEffects()
        {
            var effects = CallerStatEffects.GetStatEffects("Urgency", CallerUrgency.Critical);

            // Critical has two Emotional effects (+3 and -2 = net +1)
            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -2f));
        }

        #endregion

        #region BeliefLevel Tests

        [Test]
        public void GetStatEffects_BeliefLevel_Curious_ReturnsMentalPlus()
        {
            var effects = CallerStatEffects.GetStatEffects("BeliefLevel", CallerBeliefLevel.Curious);

            AssertThat(effects.Count == 1);
            AssertThat(effects[0].StatType == StatType.Mental);
            AssertThat(effects[0].Amount == 1f);
        }

        [Test]
        public void GetStatEffects_BeliefLevel_Zealot_ReturnsEmotionalMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("BeliefLevel", CallerBeliefLevel.Zealot);

            AssertThat(effects.Count == 1);
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -4f));
        }

        #endregion

        #region EvidenceLevel Tests

        [Test]
        public void GetStatEffects_Evidence_None_ReturnsEmotionalMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("Evidence", CallerEvidenceLevel.None);

            AssertThat(effects.Count == 1);
            AssertThat(effects[0].StatType == StatType.Emotional);
            AssertThat(effects[0].Amount == -2f);
        }

        [Test]
        public void GetStatEffects_Evidence_Irrefutable_ReturnsEmotionalAndMentalPlus()
        {
            var effects = CallerStatEffects.GetStatEffects("Evidence", CallerEvidenceLevel.Irrefutable);

            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 5f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == 2f));
        }

        #endregion

        #region Legitimacy Tests

        [Test]
        public void GetStatEffects_Legitimacy_Fake_ReturnsEmotionalMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("Legitimacy", CallerLegitimacy.Fake);

            AssertThat(effects.Count == 1);
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == -8f));
        }

        [Test]
        public void GetStatEffects_Legitimacy_Credible_ReturnsNoEffects()
        {
            var effects = CallerStatEffects.GetStatEffects("Legitimacy", CallerLegitimacy.Credible);

            AssertThat(effects.Count == 0);
        }

        [Test]
        public void GetStatEffects_Legitimacy_Compelling_ReturnsEmotionalAndMentalPlus()
        {
            var effects = CallerStatEffects.GetStatEffects("Legitimacy", CallerLegitimacy.Compelling);

            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Emotional && e.Amount == 3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Mental && e.Amount == 2f));
        }

        #endregion

        #region AudioQuality (PhoneQuality) Tests

        [Test]
        public void GetStatEffects_AudioQuality_Terrible_ReturnsEmotionalMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("AudioQuality", CallerPhoneQuality.Terrible);

            AssertThat(effects.Count == 1);
            AssertThat(effects[0].StatType == StatType.Emotional);
            AssertThat(effects[0].Amount == -3f);
        }

        [Test]
        public void GetStatEffects_AudioQuality_Good_ReturnsEmotionalPlus()
        {
            var effects = CallerStatEffects.GetStatEffects("AudioQuality", CallerPhoneQuality.Good);

            AssertThat(effects.Count == 1);
            AssertThat(effects[0].StatType == StatType.Emotional);
            AssertThat(effects[0].Amount == 1f);
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
        public void GetStatEffects_Personality_ReturnsNoEffects()
        {
            var effects = CallerStatEffects.GetStatEffects("Personality", "nervous_hiker");

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
            var properties = new List<ScreenableProperty>
            {
                new ScreenableProperty("EmotionalState", "Emotional State", CallerEmotionalState.Calm, "Calm", 3f,
                    new List<StatModification> { new StatModification(StatType.Emotional, 3f) }),
                new ScreenableProperty("CurseRisk", "Curse Risk", CallerCurseRisk.High, "High", 3f,
                    new List<StatModification> { new StatModification(StatType.Emotional, -6f) })
            };

            // Reveal only the first property
            properties[0].Update(5f);

            var totals = CallerStatEffects.AggregateStatEffects(properties, revealedOnly: true);

            AssertThat(totals.Count == 1);
            AssertThat(totals[StatType.Emotional] == 3f);
        }

        [Test]
        public void AggregateStatEffects_AllProperties_IncludesAll()
        {
            var properties = new List<ScreenableProperty>
            {
                new ScreenableProperty("EmotionalState", "Emotional State", CallerEmotionalState.Calm, "Calm", 3f,
                    new List<StatModification> { new StatModification(StatType.Emotional, 3f) }),
                new ScreenableProperty("CurseRisk", "Curse Risk", CallerCurseRisk.High, "High", 3f,
                    new List<StatModification> { new StatModification(StatType.Emotional, -6f) })
            };

            var totals = CallerStatEffects.AggregateStatEffects(properties, revealedOnly: false);

            AssertThat(totals.Count == 1);
            AssertThat(totals[StatType.Emotional] == -3f);  // 3 + (-6) = -3
        }

        [Test]
        public void AggregateStatEffects_MultipleStats_AggregatesCorrectly()
        {
            var properties = new List<ScreenableProperty>
            {
                new ScreenableProperty("EmotionalState", "Emotional State", CallerEmotionalState.Angry, "Angry", 3f,
                    new List<StatModification>
                    {
                        new StatModification(StatType.Emotional, -8f)
                    }),
                new ScreenableProperty("Coherence", "Coherence", CallerCoherence.Coherent, "Coherent", 4f,
                    new List<StatModification>
                    {
                        new StatModification(StatType.Mental, 3f)
                    })
            };

            // Reveal all
            foreach (var prop in properties)
            {
                prop.Update(5f);
            }

            var totals = CallerStatEffects.AggregateStatEffects(properties);

            AssertThat(totals.Count == 2);
            AssertThat(totals[StatType.Emotional] == -8f);
            AssertThat(totals[StatType.Mental] == 3f);
        }

        #endregion
    }
}
