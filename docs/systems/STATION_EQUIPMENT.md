# KBTV - Equipment System

## Overview

The equipment system allows players to upgrade their radio station's hardware, improving audio quality and unlocking new capabilities. Equipment upgrades are purchased during the PostShow phase using money earned from broadcasts.

## Core Concepts

| Concept | Description |
|---------|-------------|
| **Equipment Type** | Category of upgradeable equipment |
| **Equipment Level** | Current quality tier (1-4) |
| **Upgrade** | Purchasing the next level for an equipment type |

## Equipment Types

### Currently Implemented

| Type | Affects | Audio Parameters |
|------|---------|------------------|
| `PhoneLine` | Caller audio quality | Filters, distortion, static |
| `Broadcast` | Vern's broadcast quality | EQ, compression, distortion |

### Future Equipment Types

| Type | Planned Effect |
|------|----------------|
| `Transmitter` | Listener range, signal strength |
| `ScreeningTools` | Caller info visibility, legitimacy hints |
| `StudioAcoustics` | Reduce background noise, echo |
| `RecordingGear` | Enable call recording, playback |

## Level Progression

Each equipment type has 4 levels:

| Level | Name | Quality |
|-------|------|---------|
| 1 | Stock | Rough, lo-fi, noticeable artifacts |
| 2 | Basic | Improved, less distortion |
| 3 | Professional | Clean, broadcast-ready |
| 4 | Studio Quality | Crystal clear, warm presence |

## Phone Line Equipment

Affects caller audio through the CallerGroup mixer.

### Level Details

| Level | Name | Cost | Description |
|-------|------|------|-------------|
| 1 | Stock Phone Lines | — | Heavy static, narrow bandwidth, distortion |
| 2 | Basic Phone Lines | $300 | Reduced static, slightly wider band |
| 3 | Professional Lines | $800 | Minimal static, clear voice |
| 4 | Broadcast Quality | $2000 | Crystal clear, full bandwidth |

### Audio Parameter Mapping

| Parameter | Level 1 | Level 2 | Level 3 | Level 4 |
|-----------|---------|---------|---------|---------|
| `CallerLowPassCutoff` | 2200 Hz | 4800 Hz | 7400 Hz | 10000 Hz |
| `CallerHighPassCutoff` | 500 Hz | 367 Hz | 233 Hz | 100 Hz |
| `CallerLowPassResonance` | 2.5 | 2.0 | 1.5 | 1.0 |
| `CallerNasalBoost` | +4 dB | +2.7 dB | +1.3 dB | 0 dB |
| `CallerDistortion` | 0.12 | 0.08 | 0.04 | 0.00 |
| `CallerVolume` | +6 dB | +6 dB | +6 dB | +6 dB |
| Static Volume* | 0.06 | 0.04 | 0.025 | 0.01 |

*Static volume is controlled via `StaticNoiseController.SetBaseVolume()` (linear 0-1), not a mixer parameter.
*Static only plays while callers are speaking, not during the entire call.
*CallerNasalBoost is a ParamEQ effect centered at 1800 Hz to add old phone "honk" quality.

### Audio Effect (Perceptual)

- **Level 1**: Old landline phone - narrow bandwidth, nasal resonance, slight crackle
- **Level 2**: Better landline - wider band, less distortion
- **Level 3**: Clean phone call, professional
- **Level 4**: Sounds like they're in the studio

## Caller Phone Quality

In addition to the player's Phone Line equipment level, each caller has their own **phone quality** that affects how they sound. This creates variety and a risk/reward element during screening.

### Phone Quality Tiers

| Quality | Description | Audio Modifier | Example Callers |
|---------|-------------|----------------|-----------------|
| `Terrible` | Rotary phone, bad cell signal, payphone | -2 levels | Remote witness, trucker at rest stop |
| `Poor` | Old cordless, cheap prepaid | -1 level | Budget-conscious, rural caller |
| `Average` | Standard landline or decent cell | 0 (baseline) | Most callers |
| `Good` | Modern smartphone, clear VOIP | +1 level | Tech-savvy caller, professional |

### Effective Quality Calculation

```
Effective Level = Equipment Level + Phone Quality Modifier
Clamped to range [1, 4]
```

**Examples:**
- Player has Level 2 equipment, caller has `Poor` quality → Effective Level 1
- Player has Level 3 equipment, caller has `Good` quality → Effective Level 4
- Player has Level 1 equipment, caller has `Terrible` quality → Effective Level 1 (clamped)

### Screening Visibility

Phone quality provides a subtle hint during screening:

| Quality | Visible? | Screening Hint |
|---------|----------|----------------|
| Terrible | Yes | "Bad connection" indicator or static icon |
| Poor | With upgrades | Faint static visible at higher screening levels |
| Average | No | No indicator |
| Good | No | No indicator |

### Generation Distribution

| Quality | Probability | Notes |
|---------|-------------|-------|
| Terrible | 10% | Rare but memorable |
| Poor | 25% | Common enough to matter |
| Average | 50% | Default experience |
| Good | 15% | Slight bonus for lucky screening |

### Design Intent

- Creates **variety** in how callers sound, even with maxed equipment
- Adds **risk/reward** to screening: that caller with terrible audio might have an amazing story
- Remote/rural callers with compelling paranormal stories often have bad connections
- Provides subtle **audio cues** to help skilled players screen better

## Broadcast Equipment

Affects Vern's audio through the VernGroup mixer.

### Level Details

| Level | Name | Cost | Description |
|-------|------|------|-------------|
| 1 | Stock Broadcast | — | Muffled, slight distortion, weak presence |
| 2 | Basic Broadcast | $300 | Clearer, less distortion |
| 3 | Professional | $800 | Clean radio sound |
| 4 | Studio Quality | $2000 | Warm, professional presence |

### Audio Parameter Mapping

| Parameter | Level 1 | Level 2 | Level 3 | Level 4 |
|-----------|---------|---------|---------|---------|
| `VernDistortion` | 0.01 | 0.007 | 0.003 | 0.00 |
| `VernMidBoost` | 1.0 dB | 1.3 dB | 1.7 dB | 2.0 dB |
| `VernVolume` | +12 dB | +12 dB | +12 dB | +12 dB |

Note: The +12 dB volume boost compensates for signal loss from the compressor and effect chain.

### Audio Effect (Perceptual)

- **Level 1**: Sounds like AM radio from the 70s
- **Level 2**: Cleaner, but still lo-fi
- **Level 3**: Modern FM radio quality
- **Level 4**: Podcast/studio quality, warm and present

## Architecture

### Components

| Component | Description |
|-----------|-------------|
| `EquipmentType` | Enum of equipment categories |
| `EquipmentUpgrade` | Data class for single upgrade |
| `EquipmentConfig` | ScriptableObject with all upgrades defined |
| `EquipmentManager` | Singleton managing levels and purchases |
| `AudioQualityController` | Translates levels to mixer parameters |

### EquipmentType Enum

```csharp
public enum EquipmentType
{
    PhoneLine,
    Broadcast
    // Future: Transmitter, ScreeningTools, etc.
}
```

### EquipmentUpgrade Class

```csharp
[Serializable]
public class EquipmentUpgrade
{
    public string Name;           // "Basic Phone Lines"
    public string Description;    // "Reduced static, wider bandwidth"
    public int Level;             // 2
    public int Cost;              // 300
}
```

### EquipmentConfig ScriptableObject

```csharp
[CreateAssetMenu(fileName = "EquipmentConfig", menuName = "KBTV/Equipment Config")]
public class EquipmentConfig : ScriptableObject
{
    [Header("Phone Line Upgrades")]
    public List<EquipmentUpgrade> PhoneLineUpgrades;
    
    [Header("Broadcast Upgrades")]
    public List<EquipmentUpgrade> BroadcastUpgrades;
    
    public EquipmentUpgrade GetUpgrade(EquipmentType type, int level);
    public EquipmentUpgrade GetNextUpgrade(EquipmentType type, int currentLevel);
    public int GetMaxLevel(EquipmentType type);
}
```

## EquipmentManager API

### Properties

```csharp
// Get current level for an equipment type
int GetLevel(EquipmentType type);

// Check if equipment is at max level
bool IsMaxLevel(EquipmentType type);

// Get the next available upgrade (null if maxed)
EquipmentUpgrade GetNextUpgrade(EquipmentType type);

// Check if player can afford the next upgrade
bool CanAffordNextUpgrade(EquipmentType type);
```

### Methods

```csharp
// Attempt to purchase the next upgrade
// Returns true if successful, false if can't afford or already maxed
bool PurchaseUpgrade(EquipmentType type);

// Set level directly (for save/load)
void SetLevel(EquipmentType type, int level);
```

### Events

```csharp
// Fired when equipment is upgraded
event Action<EquipmentType, int> OnEquipmentUpgraded;  // (type, newLevel)
```

## AudioQualityController

Translates equipment levels to audio mixer parameters.

### Initialization

```csharp
void Start()
{
    // Apply current levels on game start
    ApplyPhoneLineLevel(EquipmentManager.Instance.GetLevel(EquipmentType.PhoneLine));
    ApplyBroadcastLevel(EquipmentManager.Instance.GetLevel(EquipmentType.Broadcast));
    
    // Subscribe to upgrades
    EquipmentManager.Instance.OnEquipmentUpgraded += HandleEquipmentUpgraded;
}
```

### Level Application

```csharp
void ApplyPhoneLineLevel(int level)
{
    float t = (level - 1) / 3f;  // Normalize to 0-1
    
    _mixer.SetFloat("CallerLowPassCutoff", Mathf.Lerp(3400f, 12000f, t));
    _mixer.SetFloat("CallerHighPassCutoff", Mathf.Lerp(300f, 80f, t));
    _mixer.SetFloat("CallerDistortion", Mathf.Lerp(0.15f, 0f, t));
    
    // Static volume controlled via StaticNoiseController, not mixer
    _staticNoiseController?.SetBaseVolume(Mathf.Lerp(0.8f, 0.05f, t));
}

void ApplyBroadcastLevel(int level)
{
    float t = (level - 1) / 3f;
    
    _mixer.SetFloat("VernDistortion", Mathf.Lerp(0.08f, 0f, t));
    _mixer.SetFloat("VernMidBoost", Mathf.Lerp(2f, 4f, t));
}
```

## Purchase Flow

```
1. Player opens PostShow upgrade screen
2. UI displays current levels and next upgrade for each type
3. Player clicks "Upgrade Phone Lines" button
4. EquipmentManager.PurchaseUpgrade(EquipmentType.PhoneLine)
   a. Check CanAfford via EconomyManager
   b. If false: return false, UI shows "Insufficient Funds"
   c. EconomyManager.SpendMoney(cost)
   d. Increment level
   e. Fire OnEquipmentUpgraded event
   f. SaveManager.MarkDirty()
5. AudioQualityController handles event, applies new mixer params
6. UI updates to show new level
```

## Persistence

Equipment levels are saved in `SaveData`:

```csharp
public class SaveData
{
    public SerializableDictionary<string, int> EquipmentLevels;
}
```

`EquipmentManager` implements `ISaveable`:

```csharp
public void OnBeforeSave(SaveData data)
{
    data.EquipmentLevels = new SerializableDictionary<string, int>();
    foreach (EquipmentType type in Enum.GetValues(typeof(EquipmentType)))
    {
        data.EquipmentLevels[type.ToString()] = GetLevel(type);
    }
}

public void OnAfterLoad(SaveData data)
{
    if (data.EquipmentLevels != null)
    {
        foreach (var kvp in data.EquipmentLevels)
        {
            if (Enum.TryParse<EquipmentType>(kvp.Key, out var type))
            {
                SetLevel(type, kvp.Value);
            }
        }
    }
}
```

## UI Design (PostShow Upgrade Screen)

```
+------------------------------------------+
|           STATION UPGRADES               |
+------------------------------------------+
|                                          |
|  PHONE LINES                             |
|  Current: Basic Phone Lines (Level 2)    |
|  [====----] 2/4                          |
|                                          |
|  Next: Professional Lines - $800         |
|  "Minimal static, clear voice"           |
|  [UPGRADE]                               |
|                                          |
|  ────────────────────────────────────    |
|                                          |
|  BROADCAST EQUIPMENT                     |
|  Current: Stock Broadcast (Level 1)      |
|  [==------] 1/4                          |
|                                          |
|  Next: Basic Broadcast - $300            |
|  "Clearer signal, less distortion"       |
|  [UPGRADE]                               |
|                                          |
+------------------------------------------+
|  Balance: $700                           |
|  [Continue to Next Night]                |
+------------------------------------------+
```

## Audio Mixer Setup

The Audio Mixer must have these exposed parameters:

| Group | Parameter | Exposed Name |
|-------|-----------|--------------|
| CallerGroup | Volume | `CallerVolume` |
| CallerGroup | Lowpass Cutoff | `CallerLowPassCutoff` |
| CallerGroup | Lowpass Resonance | `CallerLowPassResonance` |
| CallerGroup | Highpass Cutoff | `CallerHighPassCutoff` |
| CallerGroup | Distortion | `CallerDistortion` |
| CallerGroup | ParamEQ Gain (1800Hz) | `CallerNasalBoost` |
| VernGroup | Volume | `VernVolume` |
| VernGroup | Distortion | `VernDistortion` |
| VernGroup | ParamEQ Gain | `VernMidBoost` |

**Note**: Static volume is controlled via `StaticNoiseController.SetBaseVolume()`, not the mixer. Static only plays while callers are speaking.

See [VOICE_AUDIO.md](VOICE_AUDIO.md) for mixer configuration instructions.

## Debug Tools

| Command | Effect |
|---------|--------|
| `EquipmentManager.Instance.SetLevel(type, 4)` | Max out equipment |
| `EquipmentManager.Instance.PurchaseUpgrade(type)` | Force upgrade |

## Balance Notes

### Upgrade Pacing

- **Night 1-3**: Save up, maybe 1 upgrade
- **Night 4-6**: Second upgrade
- **Night 7-10**: Third upgrades
- **Night 11+**: Max level achievable

### Cost Justification

| Level | Cost | Shows Needed (at $100/show) |
|-------|------|-----------------------------|
| 2 | $300 | 3 shows |
| 3 | $800 | 8 shows (cumulative) |
| 4 | $2000 | 20 shows (cumulative) |

Total to max both tracks: $6,200 = ~62 shows (will be faster with ads)

## Future Enhancements

- **Visual feedback**: Equipment sprites change with level
- **Audio preview**: Play sample with new equipment before purchase
- **Bundle deals**: Discount for upgrading multiple at once
- **Maintenance**: Equipment degrades without upkeep (late game)

## References

- [ECONOMY_SYSTEM.md](ECONOMY_SYSTEM.md) - Purchasing
- [SAVE_SYSTEM.md](SAVE_SYSTEM.md) - Persistence
- [VOICE_AUDIO.md](VOICE_AUDIO.md) - Audio mixer details
- [AUDIO_DESIGN.md](AUDIO_DESIGN.md) - Overall audio direction
