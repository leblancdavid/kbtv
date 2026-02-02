using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Data;
using KBTV.Screening;
using System.Collections.Generic;
using System.Linq;

namespace KBTV.Tests.Unit.Screening
{
    public class CallerStatEffectsTests : KBTVTestClass
    {
        public CallerStatEffectsTests(Node testScene) : base(testScene) { }

        #region EmotionalState Tests

        [Test]
        public void GetStatEffects_EmotionalState_Calm_ReturnsPatiencePlus()
        {
            var effects = CallerStatEffects.GetStatEffects("EmotionalState", CallerEmotionalState.Calm);

            AssertThat(effects.Count == 1);
            AssertThat(effects[0].StatType == StatType.Patience);
            AssertThat(effects[0].Amount == 3f);
        }

        [Test]
        public void GetStatEffects_EmotionalState_Anxious_ReturnsPatienceAndSpiritMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("EmotionalState", CallerEmotionalState.Anxious);

            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Patience && e.Amount == -2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Spirit && e.Amount == -1f));
        }

        [Test]
        public void GetStatEffects_EmotionalState_Excited_ReturnsSpiritAndEnergyPlus()
        {
            var effects = CallerStatEffects.GetStatEffects("EmotionalState", CallerEmotionalState.Excited);

            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Spirit && e.Amount == 3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Energy && e.Amount == 2f));
        }

        [Test]
        public void GetStatEffects_EmotionalState_Scared_ReturnsPatienceAndSpiritMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("EmotionalState", CallerEmotionalState.Scared);

            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Patience && e.Amount == -3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Spirit && e.Amount == -2f));
        }

        [Test]
        public void GetStatEffects_EmotionalState_Angry_ReturnsHighPatienceAndSpiritMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("EmotionalState", CallerEmotionalState.Angry);

            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Patience && e.Amount == -5f));
            AssertThat(effects.Any(e => e.StatType == StatType.Spirit && e.Amount == -3f));
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
        public void GetStatEffects_CurseRisk_Medium_ReturnsPatienceMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("CurseRisk", CallerCurseRisk.Medium);

            AssertThat(effects.Count == 1);
            AssertThat(effects[0].StatType == StatType.Patience);
            AssertThat(effects[0].Amount == -2f);
        }

        [Test]
        public void GetStatEffects_CurseRisk_High_ReturnsPatienceAndSpiritMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("CurseRisk", CallerCurseRisk.High);

            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Patience && e.Amount == -4f));
            AssertThat(effects.Any(e => e.StatType == StatType.Spirit && e.Amount == -2f));
        }

        #endregion

        #region Coherence Tests

        [Test]
        public void GetStatEffects_Coherence_Coherent_ReturnsPatienceAndFocusPlus()
        {
            var effects = CallerStatEffects.GetStatEffects("Coherence", CallerCoherence.Coherent);

            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Patience && e.Amount == 2f));
            AssertThat(effects.Any(e => e.StatType == StatType.Focus && e.Amount == 1f));
        }

        [Test]
        public void GetStatEffects_Coherence_Questionable_ReturnsPatienceMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("Coherence", CallerCoherence.Questionable);

            AssertThat(effects.Count == 1);
            AssertThat(effects[0].StatType == StatType.Patience);
            AssertThat(effects[0].Amount == -2f);
        }

        [Test]
        public void GetStatEffects_Coherence_Incoherent_ReturnsPatienceAndFocusMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("Coherence", CallerCoherence.Incoherent);

            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Patience && e.Amount == -4f));
            AssertThat(effects.Any(e => e.StatType == StatType.Focus && e.Amount == -3f));
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
        public void GetStatEffects_Urgency_Critical_ReturnsSpiritPlusAndPatienceMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("Urgency", CallerUrgency.Critical);

            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Spirit && e.Amount == 3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Patience && e.Amount == -2f));
        }

        #endregion

        #region BeliefLevel Tests

        [Test]
        public void GetStatEffects_BeliefLevel_Curious_ReturnsDiscernmentPlus()
        {
            var effects = CallerStatEffects.GetStatEffects("BeliefLevel", CallerBeliefLevel.Curious);

            AssertThat(effects.Count == 1);
            AssertThat(effects[0].StatType == StatType.Discernment);
            AssertThat(effects[0].Amount == 1f);
        }

        [Test]
        public void GetStatEffects_BeliefLevel_Zealot_ReturnsPatienceAndSpiritMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("BeliefLevel", CallerBeliefLevel.Zealot);

            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Patience && e.Amount == -3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Spirit && e.Amount == -1f));
        }

        #endregion

        #region EvidenceLevel Tests

        [Test]
        public void GetStatEffects_Evidence_None_ReturnsBeliefMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("Evidence", CallerEvidenceLevel.None);

            AssertThat(effects.Count == 1);
            AssertThat(effects[0].StatType == StatType.Belief);
            AssertThat(effects[0].Amount == -2f);
        }

        [Test]
        public void GetStatEffects_Evidence_Irrefutable_ReturnsSpiritAndDiscernmentPlus()
        {
            var effects = CallerStatEffects.GetStatEffects("Evidence", CallerEvidenceLevel.Irrefutable);

            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Spirit && e.Amount == 5f));
            AssertThat(effects.Any(e => e.StatType == StatType.Discernment && e.Amount == 2f));
        }

        #endregion

        #region Legitimacy Tests

        [Test]
        public void GetStatEffects_Legitimacy_Fake_ReturnsSpiritAndPatienceMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("Legitimacy", CallerLegitimacy.Fake);

            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Spirit && e.Amount == -5f));
            AssertThat(effects.Any(e => e.StatType == StatType.Patience && e.Amount == -3f));
        }

        [Test]
        public void GetStatEffects_Legitimacy_Credible_ReturnsNoEffects()
        {
            var effects = CallerStatEffects.GetStatEffects("Legitimacy", CallerLegitimacy.Credible);

            AssertThat(effects.Count == 0);
        }

        [Test]
        public void GetStatEffects_Legitimacy_Compelling_ReturnsSpiritAndDiscernmentPlus()
        {
            var effects = CallerStatEffects.GetStatEffects("Legitimacy", CallerLegitimacy.Compelling);

            AssertThat(effects.Count == 2);
            AssertThat(effects.Any(e => e.StatType == StatType.Spirit && e.Amount == 3f));
            AssertThat(effects.Any(e => e.StatType == StatType.Discernment && e.Amount == 2f));
        }

        #endregion

        #region AudioQuality (PhoneQuality) Tests

        [Test]
        public void GetStatEffects_AudioQuality_Terrible_ReturnsPatienceMinus()
        {
            var effects = CallerStatEffects.GetStatEffects("AudioQuality", CallerPhoneQuality.Terrible);

            AssertThat(effects.Count == 1);
            AssertThat(effects[0].StatType == StatType.Patience);
            AssertThat(effects[0].Amount == -3f);
        }

        [Test]
        public void GetStatEffects_AudioQuality_Good_ReturnsPatiencePlus()
        {
            var effects = CallerStatEffects.GetStatEffects("AudioQuality", CallerPhoneQuality.Good);

            AssertThat(effects.Count == 1);
            AssertThat(effects[0].StatType == StatType.Patience);
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
                    new List<StatModification> { new StatModification(StatType.Patience, 3f) }),
                new ScreenableProperty("CurseRisk", "Curse Risk", CallerCurseRisk.High, "High", 3f,
                    new List<StatModification> { new StatModification(StatType.Patience, -4f) })
            };

            // Reveal only the first property
            properties[0].Update(5f);

            var totals = CallerStatEffects.AggregateStatEffects(properties, revealedOnly: true);

            AssertThat(totals.Count == 1);
            AssertThat(totals[StatType.Patience] == 3f);
        }

        [Test]
        public void AggregateStatEffects_AllProperties_IncludesAll()
        {
            var properties = new List<ScreenableProperty>
            {
                new ScreenableProperty("EmotionalState", "Emotional State", CallerEmotionalState.Calm, "Calm", 3f,
                    new List<StatModification> { new StatModification(StatType.Patience, 3f) }),
                new ScreenableProperty("CurseRisk", "Curse Risk", CallerCurseRisk.High, "High", 3f,
                    new List<StatModification> { new StatModification(StatType.Patience, -4f) })
            };

            var totals = CallerStatEffects.AggregateStatEffects(properties, revealedOnly: false);

            AssertThat(totals.Count == 1);
            AssertThat(totals[StatType.Patience] == -1f);  // 3 + (-4) = -1
        }

        [Test]
        public void AggregateStatEffects_MultipleStats_AggregatesCorrectly()
        {
            var properties = new List<ScreenableProperty>
            {
                new ScreenableProperty("EmotionalState", "Emotional State", CallerEmotionalState.Angry, "Angry", 3f,
                    new List<StatModification>
                    {
                        new StatModification(StatType.Patience, -5f),
                        new StatModification(StatType.Spirit, -3f)
                    }),
                new ScreenableProperty("Coherence", "Coherence", CallerCoherence.Coherent, "Coherent", 4f,
                    new List<StatModification>
                    {
                        new StatModification(StatType.Patience, 2f),
                        new StatModification(StatType.Focus, 1f)
                    })
            };

            // Reveal all
            foreach (var prop in properties)
            {
                prop.Update(5f);
            }

            var totals = CallerStatEffects.AggregateStatEffects(properties);

            AssertThat(totals.Count == 3);
            AssertThat(totals[StatType.Patience] == -3f);  // -5 + 2
            AssertThat(totals[StatType.Spirit] == -3f);
            AssertThat(totals[StatType.Focus] == 1f);
        }

        #endregion
    }
}
