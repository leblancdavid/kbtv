using KBTV.Callers;

namespace KBTV.Items
{
    public static class EvidenceBonusConfig
    {
        public static float GetBonusAmount(EvidenceTier tier, EvidenceBonusType type)
        {
            return type switch
            {
                EvidenceBonusType.VernPhysical => GetTierValue(tier, 2f, 4f, 7f, 11f, 16f),
                EvidenceBonusType.VernEmotional => GetTierValue(tier, 2f, 4f, 7f, 11f, 16f),
                EvidenceBonusType.VernMental => GetTierValue(tier, 2f, 4f, 7f, 11f, 16f),
                EvidenceBonusType.ListenerGrowth => GetTierValue(tier, 0.03f, 0.06f, 0.10f, 0.15f, 0.22f),
                EvidenceBonusType.ShowQuality => GetTierValue(tier, 0.01f, 0.02f, 0.04f, 0.07f, 0.12f),
                EvidenceBonusType.TopicXP => GetTierValue(tier, 0.05f, 0.10f, 0.15f, 0.22f, 0.35f),
                EvidenceBonusType.IncomePerShow => GetTierValue(tier, 2f, 5f, 10f, 18f, 35f),
                EvidenceBonusType.ScreeningInfo => GetTierValue(tier, 1f, 2f, 3f, 5f, 8f),
                _ => 0f
            };
        }

        public static float GetAnalysisTimeSeconds(EvidenceTier tier)
        {
            return tier switch
            {
                EvidenceTier.Common => 5f,
                EvidenceTier.Uncommon => 10f,
                EvidenceTier.Rare => 15f,
                EvidenceTier.VeryRare => 25f,
                EvidenceTier.OneOfAKind => 45f,
                _ => 5f
            };
        }

        public static int GetSellPrice(EvidenceTier tier)
        {
            return tier switch
            {
                EvidenceTier.Common => 25,
                EvidenceTier.Uncommon => 50,
                EvidenceTier.Rare => 100,
                EvidenceTier.VeryRare => 200,
                EvidenceTier.OneOfAKind => 500,
                _ => 25
            };
        }

        public static EvidenceBonusType GetRandomBonusType(ShowTopic? topic = null)
        {
            var values = System.Enum.GetValues<EvidenceBonusType>();
            return values[Godot.GD.RandRange(0, values.Length - 1)];
        }

        private static float GetTierValue(EvidenceTier tier, float common, float uncommon, float rare, float veryRare, float oneOfAKind)
        {
            return tier switch
            {
                EvidenceTier.Common => common,
                EvidenceTier.Uncommon => uncommon,
                EvidenceTier.Rare => rare,
                EvidenceTier.VeryRare => veryRare,
                EvidenceTier.OneOfAKind => oneOfAKind,
                _ => common
            };
        }

        public static string GetBonusDisplayName(EvidenceBonusType type)
        {
            return type switch
            {
                EvidenceBonusType.VernPhysical => "Physical +",
                EvidenceBonusType.VernEmotional => "Emotional +",
                EvidenceBonusType.VernMental => "Mental +",
                EvidenceBonusType.ListenerGrowth => "Listener Growth +",
                EvidenceBonusType.ShowQuality => "Show Quality +",
                EvidenceBonusType.TopicXP => "Topic XP +",
                EvidenceBonusType.IncomePerShow => "Income/Show +",
                EvidenceBonusType.ScreeningInfo => "Screening Info +",
                _ => "Unknown"
            };
        }

        public static string GetBonusDescription(EvidenceBonusType type, float amount)
        {
            return type switch
            {
                EvidenceBonusType.VernPhysical => $"Physical Stat +{amount:F0}",
                EvidenceBonusType.VernEmotional => $"Emotional Stat +{amount:F0}",
                EvidenceBonusType.VernMental => $"Mental Stat +{amount:F0}",
                EvidenceBonusType.ListenerGrowth => $"Listener Growth +{amount:P0}",
                EvidenceBonusType.ShowQuality => $"Show Quality +{amount:P0}",
                EvidenceBonusType.TopicXP => $"Topic XP +{amount:P0}",
                EvidenceBonusType.IncomePerShow => $"${amount:F0}/show",
                EvidenceBonusType.ScreeningInfo => $"+{amount:F0} Screening Info",
                _ => "Unknown Bonus"
            };
        }
    }
}
