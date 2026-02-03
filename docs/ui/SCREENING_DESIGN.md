# KBTV - Screening System Design

## Overview

The screening system transforms the caller approval process from a binary approve/reject choice into an information-gathering mini-game. Players must gather information about incoming callers while managing time pressure, then decide whether to put them on air or risk dead air by waiting for more information.

## Core Concept

**Passive Revelation**: Information about callers is revealed automatically over time through visual cues in the screening panel. Players cannot ask questions - they must observe and decide.

**Strategic Tension**: The most valuable information (topic match, legitimacy) appears late in the reveal sequence. Players must balance waiting for critical info against:
- Caller patience running out
- Dead air penalty if no callers are available
- The temptation to put compelling (but possibly risky) callers on air quickly

## Properties

Each caller has 11 properties revealed during screening:

| Tier | Property | Values | Base Time | Scramble Effect |
|------|----------|--------|-----------|-----------------|
| 1 | Audio Quality | Excellent/Good/Average/Poor/Terrible | 3s | Static/clarity indicator |
| 1 | Emotional State | Calm/Anxious/Excited/Scared/Angry | 4s | Emoji cycling |
| 1 | Curse Risk | Low/Medium/High | 4s | Text cycling (LOW→MED→HIGH) |
| 2 | Summary | (arcNotes) | 5s | Line-by-line reveal |
| 2 | Personality | (from arc) | 5s | Text reveal |
| 2 | Belief Level | Curious/Partial/Committed/Certain/Zealot | 6s | Text cycling |
| 2 | Evidence | None/Low/Medium/High/Irrefutable | 6s | Tier progression |
| 2 | Urgency | Low/Medium/High/Critical | 5s | Tier progression |
| 3 | Topic | [Topic Name] | 6s | Topic name reveal |
| 3 | Legitimacy | Credible/Questionable/Fake | 8s | CREDIBLE→QUESTIONABLE→FAKE |
| 3 | Coherence | Coherent/Questionable/Incoherent | 8s | Text cycling |

### Property Descriptions

**Audio Quality**: How clear the caller's phone connection is. Affects listener experience and VIBE.

**Emotional State**: Caller's current emotional condition. Affects engagement level and conversation dynamics.

**Curse Risk**: Likelihood of the caller using profanity. High risk may trigger bleeps or listener complaints.

**Belief Level**: How convinced the caller is about their experience. Ranges from curious skeptic to true believer.

**Evidence**: Quality of proof the caller claims to have. None = testimony only, Irrefutable = undeniable proof.

**Coherence**: How well the caller can communicate their story. Incoherent callers are hard to follow.

**Urgency**: How time-sensitive the caller's situation is. Critical urgency = immediate stakes.

**Summary**: A brief preview of what the caller wants to discuss (from arc's screeningSummary field).

**Topic**: The paranormal topic the caller wants to discuss. Off-topic callers trigger Vern's annoyance.

**Legitimacy**: How truthful the caller is likely to be. Core metric for screening decisions.

**Personality**: The caller's communication style (nervous_hiker, cold_factual, etc.). Informational only.

## Weight Tiers

Properties are assigned to weight tiers that determine their position in the reveal order:

- **Tier 1 (Easy - 11s total)**: Revealed early - Audio Quality (3s), Emotional State (4s), Curse Risk (4s)
- **Tier 2 (Medium - 27s total)**: Revealed mid - Summary (5s), Personality (5s), Belief Level (6s), Evidence (6s), Urgency (5s)
- **Tier 3 (Hard - 22s total)**: Revealed late - Topic (6s), Legitimacy (8s), Coherence (8s)

**Total baseline screening time: 60 seconds**

**Within each tier**, properties appear in random order. This creates variation between callers while maintaining the weighted tendency for important information to appear later.

## Screening Speed Configuration

Screening timing is centralized in `ScreeningConfig.cs` for easy tuning and equipment integration.

### Speed Multiplier

The `SpeedMultiplier` affects all property reveal times:
- **1.0** = Baseline (60s total)
- **>1.0** = Faster (e.g., 1.5 = 40s total)
- **<1.0** = Slower (e.g., 0.5 = 120s total)

Equipment upgrades increase the multiplier to speed up screening.

### Multiplier Examples

| Scenario | Multiplier | Total Time | Notes |
|----------|------------|------------|-------|
| Baseline (no upgrades) | 1.0 | 60s | Default starting point |
| Basic Phone Upgrade | 1.25 | 48s | 25% faster |
| Advanced Equipment | 1.5 | 40s | 50% faster |
| Max Upgrades | 2.0 | 30s | Double speed |
| Debuff (e.g., tired Vern) | 0.75 | 80s | 25% slower |

### Usage

```csharp
// Get effective reveal duration for a property
float duration = ScreeningConfig.GetRevealDuration("Legitimacy");

// Modify speed globally (e.g., from equipment system)
ScreeningConfig.SpeedMultiplier = 1.5f; // 50% faster
```

## Scramble Effect

When a property is being revealed, its value cycles through scrambled characters before settling on the actual value:

```
Audio Quality: ????? → ????? → ????? → EXCELLENT
```

The scramble duration equals the reveal time for that property. This creates anticipation and adds to the tension of waiting for information.

## Patience System

### Base Patience
Each caller has random patience between 20-40 seconds when entering screening.

### Screening Drain Rate
During screening, patience drains at 50% of the normal rate. This gives players more time to gather information.

### Disconnect Behavior
When patience reaches zero, the caller disconnects:
- Caller is removed from the queue
- No stat impact (caller never went on air)
- Player must wait for next caller

## Dead Air System

### Trigger
When the caller queue is empty (no incoming callers), dead air mode activates.

### Penalty
Continuous drain to Vern's stats while filler content is playing:
- **VIBE**: -0.5 per second (adjustable)
- **Energy**: -0.2 per second (adjustable)

### Mitigation
Players can avoid dead air by:
- Putting callers on air with incomplete information
- Approving callers faster to keep queue moving
- Managing the balance between information gathering and call throughput

## Off-Topic Callers

### Definition
A caller is "off-topic" when their claimed topic doesn't match the current show topic (10% chance).

### Vern's Reaction
When an off-topic caller goes on air, Vern makes a remark acknowledging the mismatch. This has flavor value and applies a small stat penalty (VIBE + Energy).

### Impact on Gameplay
Off-topic callers:
- Provide variety and comedy
- Challenge players to handle tangential situations
- Affect show quality negatively
- May occasionally lead to interesting cross-topic conversations

## Stat Impact Summary

Properties affect Vern's stats when the call completes (not when approved):

| Property | VIBE | Energy | Skepticism | Notes |
|----------|------|--------|------------|-------|
| Audio Quality | ✓ | ✓ | - | Terrible = penalty |
| Emotional State | ✓ | ✓ | - | Excited/Scared = bonus |
| Belief Level | - | ✓ | ✓ | Zealot = skepticism impact |
| Evidence | ✓ | - | ✓ | Higher evidence = bonus |
| Coherence | ✓ | ✓ | ✓ | Incoherent = penalty |
| Urgency | ✓ | ✓ | - | Critical = bonus but risky |
| Topic Match | ✓ | ✓ | - | Off-topic = penalty |
| Legitimacy | ✓ | - | ✓ | Fake = penalty |
| Curse Risk | ✓ | - | - | High = penalty |

## UI Design

### Layout Structure

```
┌─────────────────────────────────────────────────┐
│ HeaderBar                                       │
├────────────────────┬────────────────────────────┤
│                    │                            │
│   [CALLERS]        │   ON AIR PANEL             │
│   [  ITEMS  ]      │                            │
│   [  STATS  ]      ├────────────────────────────┤
│                    │                            │
│                    │   CONVERSATION PANEL       │
│                    │   (Transcript - always vis)│
│                    │                            │
└────────────────────┴────────────────────────────┘
      350px fixed              Flexible
```

### Left Column (Tabbed)
- **Width**: 350px fixed
- **Tab bar**: CALLERS | ITEMS | STATS
- **Default tab**: Callers (active on show start)

### Right Column (Always Visible)
- **Width**: Flexible
- **OnAirPanel**: ~90px fixed - Shows current on-air caller with live indicator
- **ConversationPanel**: Flexible height - Shows transcript, always visible
- **Callers tab content**: ScreeningPanel + CallerQueuePanel stacked vertically

### Visual Elements

**Property Slots**: 11 slots showing current reveal state:
- Hidden: Shows "????" or scrambled text
- Revealing: Characters cycling
- Revealed: Shows actual value with label

**Patience Bar**: Progress bar showing remaining patience, color-coded (green → yellow → red)

**Approve Button**: Green button to put caller on hold

**Reject Button**: Red button to disconnect caller

**Queue Status**: Indicator showing how many callers are waiting

### Scramble Animation
- Characters cycle randomly for the reveal duration
- Original characters from the actual value appear with decreasing frequency as time passes
- Final reveal "snaps" into place

## Implementation Notes

### File References

| File | Purpose |
|------|---------|
| `Caller.cs` | Property storage, revelation state |
| `CallerGenerator.cs` | Property generation, arc selection, order assignment |
| `CallerScreeningManager.cs` | Revelation timing, patience drain |
| `ScreeningPanel.cs` | UI rendering, scramble effects |
| `TabContainer.cs` | Tab management for left column panels |
| `DeadAirManager.cs` | Dead air detection and penalty |
| `ConversationManager.cs` | Off-topic remark trigger |
| `ArcJsonData.cs` | screeningSummary field |
| `VernStats.cs` | Stat modification on call completion |
| `LiveShowUIManager.cs` | Main UI layout and tab coordination |

### Arc Selection

Caller arc is selected during generation based on:
1. Caller's topic (claimed or actual)
2. Caller's legitimacy
3. Random variation within matching arcs

The arc ID is stored on the caller for later conversation lookup.

### Future Extensions

**Equipment Modifiers**:
- `ScreeningConfig.SpeedMultiplier`: Reveal properties faster (already implemented)
- `PropertiesPerCycle`: Reveal multiple properties simultaneously
- `PatienceMultiplier`: Slower patience drain during screening

**Advanced Features**:
- Topic-specific screening bonuses
- Caller history tracking
- Related call detection (callers referencing previous calls)
