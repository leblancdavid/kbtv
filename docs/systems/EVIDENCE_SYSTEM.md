# KBTV - Evidence System

## Overview

The evidence system allows players to collect raw evidence during broadcasts, identify it to discover its properties, and then either sell it for money or store it for passive bonuses.

## Core Concepts

| Concept | Description |
|---------|-------------|
| **Raw Evidence** | Unidentified evidence collected from callers |
| **Identified Evidence** | Evidence with discovered properties and bonuses |
| **Evidence Bonus Types** | Categories of bonuses (stats, income, listeners, etc.) |
| **Evidence Cabinet** | Storage for passive stat bonuses |
| **Evidence Website** | Public posting for passive income |
| **Evidence Tier** | Rarity level (Common → One of a Kind) |

## Evidence Lifecycle

```
1. COLLECT → Raw evidence from screening (Wordle minigame)
2. PROCESS → Identify evidence (time-based on quality)
3. DECIDE → Sell for money OR Store in Cabinet/Website
```

## Evidence Tiers

| Tier | Color | Analysis Time | Sell Price |
|------|-------|----------------|------------|
| Common | Gray | 5 seconds | $25 |
| Uncommon | Green | 10 seconds | $50 |
| Rare | Blue | 15 seconds | $100 |
| Very Rare | Purple | 25 seconds | $200 |
| One of a Kind | Gold | 45 seconds | $500 |

## Bonus Types

| Bonus Type | Description | Cabinet | Website |
|------------|-------------|---------|---------|
| **Vern Physical** | Passive Physical stat bonus | ✓ | ✗ |
| **Vern Emotional** | Passive Emotional stat bonus | ✓ | ✗ |
| **Vern Mental** | Passive Mental stat bonus | ✓ | ✗ |
| **Listener Growth** | Faster listener growth rate | ✓ | ✗ |
| **Show Quality** | Base show quality bonus | ✓ | ✗ |
| **Topic XP** | XP gain for specific topic | ✓ | ✗ |
| **Income/Show** | Passive income per broadcast | ✗ | ✓ |
| **Screening Info** | More info during screening | ✓ | ✗ |

## Bonus Values by Tier

| Bonus Type | Common | Uncommon | Rare | Very Rare | One of a Kind |
|------------|--------|----------|------|-----------|---------------|
| Vern Physical | +2 | +4 | +7 | +11 | +16 |
| Vern Emotional | +2 | +4 | +7 | +11 | +16 |
| Vern Mental | +2 | +4 | +7 | +11 | +16 |
| Listener Growth | +3% | +6% | +10% | +15% | +22% |
| Show Quality | +1% | +2% | +4% | +7% | +12% |
| Topic XP | +5% | +10% | +15% | +22% | +35% |
| Income/Show | $2 | $5 | $10 | $18 | $35 |
| Screening Info | +1 | +2 | +3 | +5 | +8 |

## Storage Systems

### Evidence Cabinet

Stores identified evidence for passive stat bonuses.

| Upgrade | Slots | Cost |
|---------|-------|------|
| Basic Cabinet | 5 | — |
| Extended Cabinet | 10 | $300 |
| Professional Display | 15 | $600 |
| Museum Quality | 20 | $900 |

**Active Bonuses** (sum of all evidence in cabinet):
- Stat bonuses (Physical, Emotional, Mental)
- Listener growth bonus
- Show quality bonus
- Topic XP bonus
- Screening info bonus

### Evidence Website

Posts evidence publicly for passive income based on listeners.

| Upgrade | Slots | Cost |
|---------|-------|------|
| Basic Website | 5 | — |
| Extended Website | 10 | $400 |
| Professional Site | 15 | $800 |
| News Empire | 20 | $1,200 |

**Income Formula:**
```
Base Income × (Listeners / 1000)
```

Example: Very Rare evidence ($18 base) with 5,000 listeners = $90/show

## Evidence Analysis

### Process

1. Collect raw evidence from screening (Wordle minigame)
2. Click "Identify All" in Evidence tab
3. Wait for analysis (time based on tier)
4. Notification when ready

### Analysis Time (Testing)

| Tier | Time |
|------|------|
| Common | 5 seconds |
| Uncommon | 10 seconds |
| Rare | 15 seconds |
| Very Rare | 25 seconds |
| One of a Kind | 45 seconds |

**Future Enhancement:** Equipment upgrades can reduce analysis time and enable parallel processing.

## Evidence Tab UI

```
┌─ EVIDENCE ────────────────────────────────┐
│                                            │
│  ┌─ RAW EVIDENCE (3) ──────────────────┐  │
│  │ [Evidence Name]                    │  │
│  │ [Evidence Name]                    │  │
│  │ [Evidence Name]                    │  │
│  │                                     │  │
│  │ [ IDENTIFY ALL (10s each) ]       │  │
│  └────────────────────────────────────┘  │
│                                            │
│  ┌─ PROCESSING (1) ───────────────────┐  │
│  │ Analyzing: "UFO Sighting Report"   │  │
│  │ [██████████░░░░░░░] 50% - 12s left │  │
│  └────────────────────────────────────┘  │
│                                            │
│  ┌─ IDENTIFIED (5) ────────────────────┐  │
│  │ [✓] UFO Sighting (Mental +7)       │  │
│  │   [CABINET] [WEBSITE] [SELL $100] │  │
│  │                                      │  │
│  │ [✓] Ghost Photo (Show Quality +4%) │  │
│  │   [CABINET] [WEBSITE] [SELL $100] │  │
│  └────────────────────────────────────┘  │
│                                            │
│  ┌─ FILE CABINET (3/5) ────────────────┐ │
│  │ Physical +11  |  Quality +7%        │ │
│  │ [Ghost Photo] [UFO Sighting]        │ │
│  │ [Mothman Footage]                    │ │
│  │ [+ UPGRADE CABINET]                  │ │
│  └────────────────────────────────────┘ │
│                                            │
│  ┌─ WEBSITE (2/5) ──────────────────────┐ │
│  │ Income/Show: $24 (+12% listeners)   │ │
│  │ [Bigfoot Hair Sample]                 │ │
│  │ [Roswell Debris]                     │ │
│  │ [+ UPGRADE WEBSITE]                  │ │
│  └────────────────────────────────────┘ │
│                                            │
└────────────────────────────────────────────┘
```

## Architecture

### Core Classes

| Class | Description |
|-------|-------------|
| `EvidenceBonusType` | Enum of bonus categories |
| `EvidenceBonusConfig` | Static config for bonus values |
| `IdentifiedEvidence` | Evidence with bonus properties |
| `EvidenceAnalyzer` | Service for identification and management |
| `EvidenceCabinet` | Passive stat bonus storage |
| `EvidenceWebsite` | Passive income system |

### EvidenceAnalyzer API

```csharp
public interface IEvidenceAnalyzer
{
    List<IdentifiedEvidence> GetRawEvidence();
    List<IdentifiedEvidence> GetProcessingEvidence();
    List<IdentifiedEvidence> GetIdentifiedEvidence();
    List<IdentifiedEvidence> GetCabinetEvidence();
    List<IdentifiedEvidence> GetWebsiteEvidence();

    void StartAnalysis(IdentifiedEvidence evidence);
    void StartAnalysisAll();
    void MoveToCabinet(IdentifiedEvidence evidence);
    void MoveToWebsite(IdentifiedEvidence evidence);
    bool SellEvidence(IdentifiedEvidence evidence);

    float GetTotalCabinetBonus(EvidenceBonusType type);
    float CalculatePassiveIncome(int currentListeners);
}
```

### IdentifiedEvidence Class

```csharp
public class IdentifiedEvidence : EvidenceItem
{
    public EvidenceBonusType BonusType;
    public float BonusAmount;
    public EvidenceStatus Status;  // Raw, Processing, Identified, InCabinet, OnWebsite, Sold

    public string BonusDisplayName => EvidenceBonusConfig.GetBonusDisplayName(BonusType);
    public string BonusDescription => EvidenceBonusConfig.GetBonusDescription(BonusType, BonusAmount);
    public int SellPrice => EvidenceBonusConfig.GetSellPrice(Tier);
    public float AnalysisTimeSeconds => EvidenceBonusConfig.GetAnalysisTimeSeconds(Tier);
}
```

## Persistence

Evidence data is saved in `SaveData`:

```csharp
public class EvidenceSystemData
{
    public List<IdentifiedEvidenceData> RawEvidence;
    public List<IdentifiedEvidenceData> ProcessingEvidence;
    public List<IdentifiedEvidenceData> IdentifiedEvidence;
    public EvidenceCabinetData Cabinet;
    public EvidenceWebsiteData Website;
}

public class SaveData
{
    public EvidenceSystemData EvidenceSystem;
    // ... other fields
}
```

## Future Enhancements

- **Parallel Processing**: Equipment upgrades for simultaneous analysis
- **Faster Analysis**: Upgradeable lab equipment
- **Set Bonuses**: Collect all bonus types at a tier for extra bonuses
- **Evidence Trading**: Trade with other stations
- **Authentication**: Some evidence can be proven fake
- **Research**: Combine evidence for discoveries

## References

- [TOOLS_EQUIPMENT.md](TOOLS_EQUIPMENT.md) - Investigation tools
- [ECONOMY_SYSTEM.md](ECONOMY_SYSTEM.md) - Economy and income
- [GAME_DESIGN.md](../design/GAME_DESIGN.md) - Overall game design
