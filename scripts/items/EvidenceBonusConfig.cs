using System.Collections.Generic;

namespace KBTV.Items
{
    public static class EvidenceBonusConfig
    {
        private static List<string> _itemNames = new();
        private static bool _itemsLoaded = false;
        private const string ITEMS_PATH = "res://assets/config/evidence_items.json";

        private static void LoadItemNames()
        {
            if (_itemsLoaded)
            {
                return;
            }

            _itemsLoaded = true;

            try
            {
                if (!Godot.FileAccess.FileExists(ITEMS_PATH))
                {
                    Godot.GD.PrintErr($"EvidenceBonusConfig: Item names file not found at {ITEMS_PATH}");
                    return;
                }

                var file = Godot.FileAccess.Open(ITEMS_PATH, Godot.FileAccess.ModeFlags.Read);
                if (file == null)
                {
                    Godot.GD.PrintErr($"EvidenceBonusConfig: Failed to open item names file");
                    return;
                }

                string json = file.GetAsText();
                file.Close();

                var jsonParse = Godot.Json.ParseString(json);
                if (jsonParse.VariantType == Godot.Variant.Type.Nil)
                {
                    Godot.GD.PrintErr("EvidenceBonusConfig: Failed to parse item names JSON");
                    return;
                }

                var dict = (Godot.Collections.Dictionary)jsonParse;
                if (!dict.ContainsKey("items"))
                {
                    Godot.GD.PrintErr("EvidenceBonusConfig: Item names JSON missing 'items' key");
                    return;
                }

                var itemsArray = (Godot.Collections.Array)dict["items"];
                _itemNames.Clear();

                foreach (var itemVariant in itemsArray)
                {
                    string itemName = itemVariant.ToString();
                    if (!string.IsNullOrEmpty(itemName))
                    {
                        _itemNames.Add(itemName);
                    }
                }

                Godot.GD.Print($"EvidenceBonusConfig: Successfully loaded {_itemNames.Count} item names from {ITEMS_PATH}");
            }
            catch (System.Exception ex)
            {
                Godot.GD.PrintErr($"EvidenceBonusConfig: Exception loading item names: {ex.Message}");
            }
        }

        public static string GetRandomItemName()
        {
            LoadItemNames();
            
            if (_itemNames.Count == 0)
            {
                return "Unknown Evidence"; // Fallback
            }

            return _itemNames[(int)(Godot.GD.Randi() % (uint)_itemNames.Count)];
        }
        public static float GetBonusAmount(EvidenceTier tier, EvidenceBonusType type)
        {
            return type switch
            {
                EvidenceBonusType.VernPhysical => GetTierValue(tier, 1f, 2f, 3f, 4f, 5f),
                EvidenceBonusType.VernEmotional => GetTierValue(tier, 1f, 2f, 3f, 4f, 5f),
                EvidenceBonusType.VernMental => GetTierValue(tier, 1f, 2f, 3f, 4f, 5f),
                EvidenceBonusType.ListenerGrowth => GetTierValue(tier, 0.03f, 0.06f, 0.10f, 0.15f, 0.22f),
                EvidenceBonusType.ShowQuality => GetTierValue(tier, 0.01f, 0.02f, 0.04f, 0.07f, 0.12f),
                EvidenceBonusType.TopicXP => GetTierValue(tier, 0.05f, 0.10f, 0.15f, 0.20f, 0.25f),
                EvidenceBonusType.IncomePerShow => GetTierValue(tier, 2f, 5f, 10f, 18f, 35f),
                EvidenceBonusType.ScreeningInfo => GetTierValue(tier, 1f, 2f, 3f, 5f, 8f),
                _ => 0f
            };
        }

        public static float GetAnalysisTimeSeconds(EvidenceTier tier)
        {
            return 5f; // Fast analysis for testing
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

        public static EvidenceBonusType GetRandomBonusType()
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
                EvidenceBonusType.ListenerGrowth => $"Listener Growth +{amount * 100:F0}%",
                EvidenceBonusType.ShowQuality => $"Show Quality +{amount * 100:F0}%",
                EvidenceBonusType.TopicXP => $"Topic XP +{amount * 100:F0}%",
                EvidenceBonusType.IncomePerShow => $"${amount:F0}/show",
                EvidenceBonusType.ScreeningInfo => $"+{amount:F0} Screening Info",
                _ => "Unknown Bonus"
            };
        }
    }
}
