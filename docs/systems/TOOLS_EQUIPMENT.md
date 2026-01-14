# KBTV - Investigation Tools

## Overview

Investigation tools are equipment that players can purchase and upgrade to improve their ability to investigate and document paranormal events. Better tools lead to higher quality evidence drops during special events.

## Core Concepts

| Concept | Description |
|---------|-------------|
| **Tool Type** | Category of investigation equipment |
| **Tool Level** | Current quality tier (1-4) |
| **Tool Bonus** | Improvements to evidence quality/drop rate |

## Tool Types

| Tool | Function | Evidence Affected |
|------|----------|-------------------|
| `Camera` | Take photos of phenomena | Photos |
| `Flashlight` | Illuminate dark areas | Photos, Samples |
| `EMF Reader` | Detect electromagnetic fields | All (detection) |
| `Audio Recorder` | Capture audio evidence | Audio |
| `Night Vision` | See in darkness | Photos, Video |
| `Motion Sensor` | Detect movement | All (detection) |

## Level Progression

Each tool has 4 levels:

| Level | Name | Quality |
|-------|------|---------|
| 1 | Basic | Low quality, grainy, unreliable |
| 2 | Standard | Decent quality, moderate reliability |
| 3 | Professional | High quality, reliable detection |
| 4 | Research Grade | Excellent quality, precise readings |

## Tool Details

### Camera

Affects photo and video evidence quality.

| Level | Name | Cost | Photo Quality | Zoom | Stabilization |
|-------|------|------|---------------|------|---------------|
| 1 | Basic Digital | $200 | 480p, grainy | 2x | None |
| 2 | Semi-Pro | $500 | 720p, clear | 4x | Digital |
| 3 | Professional | $1,200 | 1080p, sharp | 8x | Optical |
| 4 | Research Grade | $3,000 | 4K, crystal | 16x | Gyroscopic |

### Flashlight

Improves visibility for photos and sample collection.

| Level | Name | Cost | Brightness | Battery Life | Special |
|-------|------|------|------------|--------------|---------|
| 1 | Standard Maglite | $50 | Low | 2 hours | — |
| 2 | LED Flood | $150 | Medium | 4 hours | Wide beam |
| 3 | High-Powered | $400 | High | 6 hours | Adjustable |
| 4 | UV/IR Combo | $1,000 | Ultra | 8 hours | UV mode |

### EMF Reader

Detects electromagnetic anomalies, improves all evidence.

| Level | Name | Cost | Sensitivity | Range | Display |
|-------|------|------|-------------|-------|---------|
| 1 | Basic Meter | $100 | Low | 2m | LEDs |
| 2 | TriField | $300 | Medium | 5m | Analog |
| 3 | K-II Meter | $700 | High | 10m | Digital |
| 4 | Full Spectrum | $2,000 | Ultra | 20m | Graph |

### Audio Recorder

Affects audio evidence quality.

| Level | Name | Cost | Sample Rate | Filter | Battery |
|-------|------|------|-------------|--------|---------|
| 1 | Voice Recorder | $80 | 22kHz | None | 4 hours |
| 2 | Stereo Recorder | $250 | 44kHz | Basic | 8 hours |
| 3 | Parabolic Mic | $600 | 96kHz | Advanced | 12 hours |
| 4 | Studio Package | $1,500 | 192kHz | Pro | 24 hours |

### Night Vision

Improves low-light photography and video.

| Level | Name | Cost | Generation | Battery | FOV |
|-------|------|------|------------|---------|-----|
| 1 | PVS-14 Clone | $400 | Gen 1 | 4 hours | 40° |
| 2 | PVS-7 Clone | $900 | Gen 2 | 8 hours | 50° |
| 3 | PVS-15 Clone | $1,800 | Gen 3 | 12 hours | 60° |
| 4 | Thermal/IR | $4,000 | Gen 4 | 16 hours | 90° |

### Motion Sensor

Detects movement, improves detection during events.

| Level | Name | Cost | Sensitivity | Alert Types | Range |
|-------|------|------|-------------|-------------|-------|
| 1 | Basic PIR | $75 | Low | Movement | 5m |
| 2 | Dual Sensor | $200 | Medium | Heat + Motion | 10m |
| 3 | Tri-Sensor | $500 | High | Heat + Motion + Sound | 15m |
| 4 | Full Array | $1,200 | Ultra | All + Direction | 25m |

## Tool Bonuses

Each tool level provides bonuses:

### Evidence Quality Bonus

| Tool Level | Drop Quality | Tier Bonus Chance |
|------------|--------------|-------------------|
| 1 | 1.0x | +0% |
| 2 | 1.2x | +10% |
| 3 | 1.5x | +25% |
| 4 | 2.0x | +50% |

### Detection Bonus (EMF/Motion)

| Tool Level | Detection Range | Warning Time |
|------------|-----------------|--------------|
| 1 | 100% | -2s |
| 2 | 125% | Standard |
| 3 | 150% | +2s |
| 4 | 200% | +5s |

### Specific Bonuses

| Tool | Level 4 Bonus |
|------|---------------|
| Camera | Auto-capture on motion |
| Flashlight | UV reveals hidden messages |
| EMF Reader | Type identification |
| Audio Recorder | EVP filtering |
| Night Vision | Thermal imaging |
| Motion Sensor | Predictive tracking |

## Purchase Flow

Tools are purchased in PostShow phase:

```
1. Player opens Tools panel
2. Select tool category (Camera, etc.)
3. View current level and next upgrade
4. Click "Upgrade" if affordable
5. Tool level increases
6. EvidenceGenerator uses new level
```

## Synergy Bonuses

Having multiple tools at high levels provides synergy:

| Combination | Bonus |
|-------------|-------|
| Camera + Flashlight (L3+) | +20% Photo quality |
| EMF Reader + Motion Sensor (L3+) | +30% Detection |
| Audio Recorder + Night Vision (L3+) | +25% Video audio |
| All 6 tools at L4 | +100% All evidence quality |

## Architecture

### Components

| Component | Description |
|-----------|-------------|
| `ToolType` | Enum of tool categories |
| `ToolLevel` | Enum of upgrade levels |
| `InvestigationTool` | Data class for single tool |
| `ToolConfig` | ScriptableObject with all tools |
| `ToolManager` | Singleton managing all tools |
| `EvidenceGenerator` | Uses tool levels for drops |

### ToolType Enum

```csharp
public enum ToolType
{
    Camera,
    Flashlight,
    EMFReader,
    AudioRecorder,
    NightVision,
    MotionSensor
}
```

### InvestigationTool Class

```csharp
[Serializable]
public class InvestigationTool
{
    public ToolType Type;
    public int Level;  // 1-4
    public string Name;
    public string Description;
    public int Cost;  // Next upgrade cost
}
```

### ToolConfig ScriptableObject

```csharp
[CreateAssetMenu(fileName = "ToolConfig", menuName = "KBTV/Tool Config")]
public class ToolConfig : ScriptableObject
{
    [Header("Camera Upgrades")]
    public List<InvestigationTool> CameraUpgrades;
    
    [Header("Flashlight Upgrades")]
    public List<InvestigationTool> FlashlightUpgrades;
    
    // ... other tools
    
    public InvestigationTool GetUpgrade(ToolType type, int currentLevel);
    public int GetMaxLevel(ToolType type);
}
```

### ToolManager API

```csharp
public class ToolManager : MonoBehaviour
{
    public int GetToolLevel(ToolType type);
    public bool IsMaxLevel(ToolType type);
    public bool CanAffordNext(ToolType type);
    public bool PurchaseUpgrade(ToolType type);
    public float GetQualityBonus(ToolType type);
    public float GetTotalSynergyBonus();
}
```

## UI Design

### Tools Panel

```
+------------------------------------------+
|        INVESTIGATION TOOLS               |
+------------------------------------------+
|                                          |
|  📷 CAMERA                               |
|  Current: Professional (Level 3)         |
|  [========--] 3/4                        |
|  Next: Research Grade - $3,000           |
|  "4K, crystal clear, 16x zoom"           |
|  [UPGRADE]                               |
|                                          |
|  ────────────────────────────────────    |
|                                          |
|  🔦 FLASHLIGHT                           |
|  Current: High-Powered (Level 3)         |
|  [========--] 3/4                        |
|  Next: UV/IR Combo - $1,000              |
|  "Ultra bright, 6h battery, UV mode"     |
|  [UPGRADE]                               |
|                                          |
|  ────────────────────────────────────    |
|                                          |
|  📡 EMF READER                           |
|  Current: K-II Meter (Level 3)           |
|  [========--] 3/4                        |
|  Next: Full Spectrum - $2,000            |
|  "Ultra sensitivity, 20m range"          |
|  [UPGRADE]                               |
|                                          |
+------------------------------------------+
|  Synergy Bonus: 0%                       |
|  Balance: $4,500                         |
|  [Continue]                              |
+------------------------------------------+
```

### Tool Detail Popup

```
+------------------------------------------+
|              📷 CAMERA                   |
|        Research Grade (Level 4)          |
+------------------------------------------+
|                                          |
|          [4K SAMPLE PHOTO]               |
|                                          |
|  SPECIFICATIONS:                         |
|  • Resolution: 4K Ultra HD               |
|  • Zoom: 16x Optical                     |
|  • Stabilization: Gyroscopic             |
|  • Battery: 12 hours                     |
|                                          |
|  BONUSES:                                |
|  • Evidence Quality: +100%               |
|  • Tier Bonus Chance: +50%               |
|  • Special: Auto-capture on motion       |
|                                          |
|  "The best consumer-grade camera         |
|   available. Captures even faint         |
|   phenomena with clarity."               |
|                                          |
|         [UPGRADE - $3,000]               |
|         [Close]                          |
+------------------------------------------+
```

## Persistence

Tool levels are saved in `SaveData`:

```csharp
public class SaveData
{
    public SerializableDictionary<string, int> ToolLevels;
}
```

## Balance Notes

### Upgrade Pacing

| Level | Cost Range | Shows Needed (at $150/night) |
|-------|------------|------------------------------|
| 2 | $50-$200 | 1-2 shows |
| 3 | $300-$700 | 3-5 shows |
| 4 | $1,000-$4,000 | 7-27 shows |

### Tool Priority

Recommended purchase order:
1. EMF Reader (improves all evidence)
2. Camera (most evidence is photos)
3. Audio Recorder (second most common)
4. Motion Sensor (detection helps)
5. Night Vision (situationally useful)
6. Flashlight (least critical)

### Budget Build

Minimum viable setup (~2-3 nights):
- EMF Reader L2: $300
- Camera L2: $500
- Audio Recorder L2: $250
- **Total: $1,050**

### Endgame Build

All tools at max (~25-40 nights):
- All tools L4: ~$12,000
- Plus cabinet upgrades
- **Total: ~$17,000**

## Future Enhancements

- **Tool degradation**: Tools wear out, need repairs
- **Tool crafting**: Combine tools for better versions
- **Tool customization**: Add lenses, filters, attachments
- **Toolset bonuses**: Completing tool categories
- **Staff bonuses**: Technicians improve tool effectiveness

## References

- [EVIDENCE_SYSTEM.md](EVIDENCE_SYSTEM.md) - Evidence that tools improve
- [SPECIAL_EVENTS.md](SPECIAL_EVENTS.md) - Events where tools are used
- [ECONOMY_SYSTEM.md](ECONOMY_SYSTEM.md) - Purchasing
- [STATION_EQUIPMENT.md](STATION_EQUIPMENT.md) - Station equipment (separate system)
