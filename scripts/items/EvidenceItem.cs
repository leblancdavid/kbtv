using System;
using Godot;

namespace KBTV.Items
{
    /// <summary>
    /// Represents a piece of evidence collected from callers during screening.
    /// Evidence is permanent and appears in the items tab.
    /// </summary>
    [Serializable]
    public class EvidenceItem
    {
        /// <summary>
        /// Unique identifier for this evidence item.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The 5-letter word that was successfully guessed to obtain this evidence.
        /// </summary>
        public string Word { get; set; }

        /// <summary>
        /// Name of the caller this evidence was collected from.
        /// </summary>
        public string SourceCallerName { get; set; }

        /// <summary>
        /// Evidence level of the caller (None, Low, Medium, High, Irrefutable).
        /// </summary>
        public string EvidenceLevel { get; set; }

        /// <summary>
        /// ISO 8601 timestamp of when this evidence was collected.
        /// </summary>
        public string CollectionDate { get; set; }

        /// <summary>
        /// Human-readable description of the evidence.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Evidence tier based on difficulty (Common, Uncommon, Rare, VeryRare, OneOfAKind).
        /// </summary>
        public EvidenceTier Tier { get; set; }

        /// <summary>
        /// Creates a new evidence item.
        /// </summary>
        public static EvidenceItem Create(string word, string callerName, string evidenceLevel, int topicLevel)
        {
            var tier = DetermineTier(topicLevel);

            return new EvidenceItem
            {
                Id = Guid.NewGuid().ToString(),
                Word = word.ToUpper(),
                SourceCallerName = callerName,
                EvidenceLevel = evidenceLevel,
                CollectionDate = DateTime.UtcNow.ToString("o"),
                Description = GenerateDescription(word, callerName, tier),
                Tier = tier
            };
        }

        /// <summary>
        /// Creates a new evidence item with a pre-determined tier (for loading from save).
        /// </summary>
        public static EvidenceItem Create(string word, string callerName, string evidenceLevel, EvidenceTier tier)
        {
            return new EvidenceItem
            {
                Id = Guid.NewGuid().ToString(),
                Word = word.ToUpper(),
                SourceCallerName = callerName,
                EvidenceLevel = evidenceLevel,
                CollectionDate = DateTime.UtcNow.ToString("o"),
                Description = GenerateDescription(word, callerName, tier),
                Tier = tier
            };
        }

        /// <summary>
        /// Determines the evidence tier based on topic level using loot table roll.
        /// </summary>
        private static EvidenceTier DetermineTier(int topicLevel)
        {
            return EvidenceLootTable.RollQuality(topicLevel);
        }

        /// <summary>
        /// Generates a description for the evidence item.
        /// </summary>
        private static string GenerateDescription(string word, string callerName, EvidenceTier tier)
        {
            var quality = tier switch
            {
                EvidenceTier.OneOfAKind => "one-of-a-kind evidence",
                EvidenceTier.VeryRare => "extremely rare evidence",
                EvidenceTier.Rare => "rare evidence",
                EvidenceTier.Uncommon => "uncommon evidence",
                EvidenceTier.Common => "common evidence",
                _ => "evidence"
            };

            return $"{quality} provided by {callerName} - keyword: {word}";
        }
    }
}