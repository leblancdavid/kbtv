# KBTV - Evidence System

## Overview

The evidence system allows players to collect, catalog, and display supernatural evidence discovered during broadcasts. Evidence serves as collectible loot that rewards exploration and provides stat bonuses to specific topics.

## Core Concepts

| Concept | Description |
|---------|-------------|
| **Evidence Type** | Category of evidence (Photo, Audio, Sample, Document, Video) |
| **Evidence Tier** | Rarity level (Common → Legendary), replaces lower tiers |
| **Evidence Cabinet** | Display storage with upgradeable slots |
| **Set Bonus** | Bonus granted when collecting all 5 types at a tier |

## Evidence Types

| Type | Icon | Description |
|------|------|-------------|
| `Photo` | 📷 | Photographic evidence of paranormal activity |
| `Audio` | 🎙️ | Recordings of voices, EVP, or unexplained sounds |
| `Sample` | 🧪 | Physical traces (dust, residue, biological samples) |
| `Document` | 📄 | Papers, journals, photographs, or records |
| `Video` | 🎥 | Video footage of paranormal events |

## Tier Progression

Each evidence type has 5 tiers. Higher tiers replace lower tiers (no duplicates stored):

| Tier | Name | Color | Rarity | Display Effect |
|------|------|-------|--------|----------------|
| 1 | Common | Gray | ~40% | Basic frame, no glow |
| 2 | Uncommon | Green | ~30% | Simple frame, faint glow |
| 3 | Rare | Blue | ~18% | Ornate frame, soft glow |
| 4 | Epic | Purple | ~9% | Glowing frame, particles |
| 5 | Legendary | Gold | ~3% | Radiant frame, intense effects |

## Evidence Sources

### Event Drops

Special events are the primary source of evidence. See [SPECIAL_EVENTS.md](SPECIAL_EVENTS.md) for details.

### Tool Bonus

Investigation tools increase evidence quality and drop rate. See [TOOLS_EQUIPMENT.md](TOOLS_EQUIPMENT.md).

### Topic Bonuses

Certain evidence provides bonuses to specific topics:

| Evidence | Topic Bonus |
|----------|-------------|
| UFO Photos | +15% Listener Growth (UFOs) |
| Government Documents | +20% Screening Info (Government) |
| Ghost Audio | +10% Discernment (Supernatural) |
| Bigfoot Hair Sample | +15% Caller Quality (Cryptids) |

## Evidence Cabinet

The cabinet stores collected evidence. Players start with limited slots and can upgrade.

### Cabinet Slots

| Upgrade | Slots | Cost |
|---------|-------|------|
| Basic Cabinet | 15 (3 per type) | — |
| Extended Cabinet | 25 (5 per type) | $500 |
| Professional Display | 40 (8 per type) | $1,500 |
| Museum Quality | 60 (12 per type) | $4,000 |

### Organization

- Evidence is automatically organized by type
- Within each type, higher tiers appear first
- Players can favorites evidence for quick access

## Set Bonuses

Collecting all 5 evidence types at a tier grants a bonus:

| Complete Set | Bonus |
|--------------|-------|
| Common Set | +5% All Topic XP |
| Uncommon Set | +10% All Topic XP |
| Rare Set | +15% All Topic XP, +5% Show Quality |
| Epic Set | +20% All Topic XP, +10% Show Quality, +$50/night |
| Legendary Set | +25% All Topic XP, +15% Show Quality, +$150/night |

## Event Interactions

Special events can affect evidence:

### Theft Events

Some events may steal evidence from the cabinet:
- Random chance during "Suspicious Van" (Government)
- Evidence of specific type is removed
- Legendary evidence cannot be stolen
- Insurance can be purchased ($200/night) to prevent theft

### Breakage Events

Physical events can damage evidence:
- "Poltergeist" events may break framed evidence
- Broken evidence is lost permanently
- Display cases provide protection (Epic+)

## Architecture

### Components

| Component | Description |
|-----------|-------------|
| `EvidenceType` | Enum of evidence categories |
| `EvidenceTier` | Enum of rarity levels |
| `EvidenceData` | Data class for single evidence item |
| `EvidenceCabinet` | Singleton managing storage and display |
| `EvidenceGenerator` | Handles evidence generation from events |
| `EvidenceSetManager` | Tracks set collection and bonuses |

### EvidenceData Class

```csharp
[Serializable]
public class EvidenceData
{
    public EvidenceType Type;
    public EvidenceTier Tier;
    public string Title;           // "UFO Photo #42"
    public string Description;     // Fluff text describing the evidence
    public DateTime CollectedDate;
    public TopicType? TopicBonus;  // Null if generic
}
```

### EvidenceCabinet API

```csharp
public class EvidenceCabinet : MonoBehaviour
{
    public int TotalSlots;
    public int UsedSlots;
    public Dictionary<EvidenceType, List<EvidenceData>> StoredEvidence;
    
    public bool AddEvidence(EvidenceData evidence);
    public bool HasEvidence(EvidenceType type, EvidenceTier tier);
    public int GetCountByType(EvidenceType type);
    public int GetSetBonusLevel(EvidenceTier tier);
    public void TriggerSetBonuses();
}
```

### EvidenceGenerator Class

```csharp
public class EvidenceGenerator
{
    public EvidenceData GenerateEvidence(
        SpecialEventType eventType,
        int toolLevel,
        TopicType? relatedTopic = null);
    
    public float GetDropChance(SpecialEventType eventType, int toolLevel);
    public EvidenceTier RollTier();
}
```

## UI Design

### Cabinet View

```
+------------------------------------------+
|           EVIDENCE CABINET               |
|  Set Bonus: Rare Set Complete! (+15% XP) |
+------------------------------------------+
|  [Photo]  [Audio]  [Sample]  [Doc]  [Vid]|
+------------------------------------------+
|                                          |
|  📷 PHOTOS (4/8 slots)                   |
|  +----------------------------------+    |
|  | [🏆] UFO Photo - Mysterious      |    |
|  |     Lights Over Nevada           |    |
|  |     Tier: Epic | XP: +15% UFO    |    |
|  +----------------------------------+    |
|  | [⭐] EVP Recording Session       |    |
|  |     "They're here..."            |    |
|  |     Tier: Rare | XP: +10% All    |    |
|  +----------------------------------+    |
|                                          |
|  🎙️ AUDIO (2/8 slots)                   |
|  +----------------------------------+    |
|  | [🥇] Government Wiretap          |    |
|  |     Classified conversation      |    |
|  |     Tier: Legendary | XP: +20%   |    |
|  |     GOVT                       |    |
|  +----------------------------------+    |
|                                          |
+------------------------------------------+
|  [Upgrade Cabinet - $1,500]              |
+------------------------------------------+
```

### Evidence Detail View

```
+------------------------------------------+
|  📷 UFO Photo - Mysterious Lights        |
+------------------------------------------+
|          [ GOLDEN FRAME ]                |
|                                          |
|    ▓▓▓      ░░░                          |
|   ▓▓▓▓▓   ░░░░░                          |
|    ▓▓▓      ░░░    🔴 LIVE               |
|                                          |
|     Over Area 51, 3:47 AM                |
+------------------------------------------+
|  Tier: Legendary (5/5)                   |
|  Bonus: +20% Topic XP (UFO)              |
|  Collected: Night 23                     |
+------------------------------------------+
|  [Favorite]  [Rotate]  [Close]           |
+------------------------------------------+
```

## Persistence

Evidence is saved in `SaveData`:

```csharp
public class SaveData
{
    public List<EvidenceData> CollectedEvidence;
    public int CabinetSlots;
    public HashSet<string> FavoriteEvidenceIds;
    public int InsuranceActiveWeeks;  // 0 = inactive
}
```

## Balance Notes

### Drop Rates

| Event Tier | Base Drop Chance | With Max Tools |
|------------|------------------|----------------|
| Tier 1 (Minor) | 25% | 50% |
| Tier 2 (Major) | 50% | 80% |
| Tier 3 (Critical) | 75% | 100% |

### Tier Distribution

| Tier | Weight |
|------|--------|
| Common | 40% |
| Uncommon | 30% |
| Rare | 18% |
| Epic | 9% |
| Legendary | 3% |

### Expected Collection Rate

- One evidence every 2-3 events on average
- Complete Common set: ~10-15 events
- Complete Legendary set: ~200+ events

## Future Enhancements

- **Evidence trading**: Trade with other stations
- **Authentication**: Some evidence can be proven fake
- **Research**: Combine evidence for discoveries
- **Story unlocks**: Legendary sets unlock special arcs
- **Photo mode**: Zoom and rotate evidence in detail view

## References

- [SPECIAL_EVENTS.md](SPECIAL_EVENTS.md) - Event system that generates evidence
- [TOOLS_EQUIPMENT.md](TOOLS_EQUIPMENT.md) - Tools that improve evidence quality
- [TOPIC_EXPERIENCE.md](TOPIC_EXPERIENCE.md) - Topic bonuses
- [ECONOMY_SYSTEM.md](ECONOMY_SYSTEM.md) - Purchasing cabinet upgrades
