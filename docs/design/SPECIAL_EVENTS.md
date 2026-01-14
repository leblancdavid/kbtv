# KBTV - Special Events System

## Overview

Special events are突发事件 that occur during live broadcasts, adding urgency and stakes to gameplay. These events create dramatic moments, offer rewards (evidence), and can penalize the player if mishandled.

## Core Concepts

| Concept | Description |
|---------|-------------|
| **Event Type** | Category of supernatural/strange occurrence |
| **Event Tier** | Difficulty/reward level (1-3) |
| **Trigger Condition** | What causes the event to fire |
| **Urgency** | Time limit to resolve (during broadcast) |

## Event Timing

Events occur **during live broadcasts**, creating genuine urgency:

- Events interrupt the normal broadcast flow
- A countdown timer appears (30-120 seconds depending on severity)
- Player must handle the situation while managing Vern's stats
- Vern stays in studio; player handles events alone
- Resolution quality affects show quality and evidence drops

## Topic-Based Events

Events unlock based on topic levels. Each topic has 3 events:

| Topic | Level 3 Event | Level 5 Event | Level 7 Event |
|-------|---------------|---------------|---------------|
| **UFOs** | Strange Lights | Alien in Station | Close Encounter |
| **Government** | Suspicious Van | Phone Tap | Government Raid |
| **Supernatural** | Cold Spot | Poltergeist | Full Manifestation |
| **Cryptids** | Strange Noises | Bigfoot Break-in | Creature Cornered |

### Event Descriptions

#### UFO Events

| Event | Description | Difficulty | Evidence Drop |
|-------|-------------|------------|---------------|
| Strange Lights | Multiple callers report aerial phenomena | Easy | UFO Photo (Common) |
| Alien in Station | Someone claiming to be alien enters studio | Medium | UFO Photo (Rare), Audio (Uncommon) |
| Close Encounter | UFO lands, aliens attempt contact | Hard | UFO Photo (Legendary), Video (Epic) |

#### Government Events

| Event | Description | Difficulty | Evidence Drop |
|-------|-------------|------------|---------------|
| Suspicious Van | Government surveillance van parked outside | Easy | Document (Common) |
| Phone Tap | Discover calls are being recorded | Medium | Audio (Rare), Document (Uncommon) |
| Government Raid | Agents attempt to shut down broadcast | Hard | Document (Legendary), Video (Epic) |

#### Supernatural Events

| Event | Description | Difficulty | Evidence Drop |
|-------|-------------|------------|---------------|
| Cold Spot | Studio temperature drops, EVP detected | Easy | Audio (Common), Sample (Common) |
| Poltergeist | Objects move, equipment malfunctions | Medium | Sample (Rare), Video (Uncommon) |
| Full Manifestation | Ghost fully appears on camera | Hard | Photo (Legendary), Video (Epic) |

#### Cryptid Events

| Event | Description | Difficulty | Evidence Drop |
|-------|-------------|------------|---------------|
| Strange Noises | Outside, animal-like sounds reported | Easy | Audio (Common) |
| Bigfoot Break-in | Creature enters studio, trashes place | Medium | Sample (Rare), Photo (Uncommon) |
| Creature Cornered | Cornered cryptid threatens callers | Hard | Sample (Legendary), Photo (Epic) |

## Cross-Topic Events

When **2 or more topics reach level 7**, cross-topic events become available:

| Event | Requirement | Difficulty | Evidence Drop |
|-------|-------------|------------|---------------|
| Government Cover-up | UFO + Government both at 7 | Hard | All 3 types (Epic) |
| Alien Ghost | UFO + Supernatural both at 7 | Hard | All 3 types (Epic) |
| Cryptid Conspiracy | Cryptid + Government both at 7 | Hard | All 3 types (Epic) |
| The Convergence | 3+ topics at 7 | Very Hard | All 3 types (Legendary) |

## Event Structure

### Event Lifecycle

```
1. Trigger Check
   - Topic level requirements met
   - Random roll (base 5% per broadcast, increases with topic level)
   
2. Event Announcement
   - Visual alert on screen
   - Sound effect
   - Event name revealed
   
3. Urgency Phase
   - Countdown timer (30-120 seconds)
   - Player must take actions:
     a) Continue show with reduced quality
     b) Address event directly
     c) Call authorities (may end broadcast)
   
4. Resolution
   - Quality based on actions taken
   - Evidence drops if successful
   - Stat impacts applied
   
5. Recovery
   - Show continues if not ended
   - Listeners affected by resolution quality
   - Vern's mood adjusted
```

### Event Resolution Quality

| Resolution | Show Quality | Listeners | Evidence |
|------------|--------------|-----------|----------|
| Perfect | +20% | +50 | +Tier bonus |
| Good | +10% | +25 | Standard |
| Adequate | +5% | +0 | Reduced chance |
| Poor | -10% | -25 | No drop |
| Failed | -25% | -50 | Possible theft |

## Event Mechanics

### Urgency Actions

During an event, player can:

| Action | Time Cost | Effect |
|--------|-----------|--------|
| **Address on Air** | 0 (instant) | Discuss event, affects mood, may resolve |
| **Investigate** | 30s | Gather evidence, unlocks better resolution |
| **Call Authorities** | 60s | Ends event, may end broadcast |
| **Ignore** | 0 | Show continues, event escalates |
| **Evacuate** | 90s | Safest option, broadcast ends |

### Difficulty Scaling

Event difficulty increases with:

| Factor | Effect |
|--------|--------|
| Topic level | Higher level = harder event |
| Topic freshness | Fresh topics = harder |
| Time of night | Later = slightly harder |
| Previous success | Recent successes = harder |

### Punishments

| Event Type | Failure Punishment |
|------------|-------------------|
| Minor | -10% Show Quality, -25 Listeners |
| Major | -25% Show Quality, -50 Listeners, Equipment Damage |
| Critical | Show Ends, -100 Listeners, Possible Evidence Theft |

## Evidence Drop System

Events are the primary source of evidence. Drop mechanics:

### Drop Chance

```
Base Chance = 25%
Tool Bonus = +(toolLevel * 10)%
Resolution Multiplier:
  Perfect = 1.5x
  Good = 1.0x
  Adequate = 0.5x
  Poor = 0.25x
  Failed = 0%
```

### Tier Roll

If drop occurs, tier is determined by:

| Factor | Weight |
|--------|--------|
| Event difficulty | Major = +1 tier weight |
| Tool level | Higher tools = better tiers |
| Luck factor | Random roll |

### No Duplicates

Higher tier evidence replaces lower tier of same type:
- Player keeps only 1 of each type at each tier
- New evidence upgrades existing if duplicate type+tier
- Cabinet storage tracks what's owned

## UI Design

### Event Alert

```
+------------------------------------------+
|                                          |
|            ⚠️  ALERT  ⚠️                 |
|                                          |
|         STRANGE LIGHTS                   |
|     Multiple reports of aerial           |
|     phenomena over Nevada.               |
|                                          |
|          ⏱️ 02:45                        |
|                                          |
|  [Discuss on Air]  [Investigate]         |
|  [Call Authorities]  [Ignore]            |
|                                          |
+------------------------------------------+
```

### Event Resolution

```
+------------------------------------------+
|                                          |
|          EVENT RESOLVED                  |
|                                          |
|    UFO Photo - Mysterious Lights         |
|    Epic Tier (+15% XP)                   |
|                                          |
|  Show Quality: +20%  Listeners: +50      |
|                                          |
|         [Continue Show]                  |
|                                          |
+------------------------------------------+
```

## Architecture

### Components

| Component | Description |
|-----------|-------------|
| `SpecialEventType` | Enum of event categories |
| `SpecialEvent` | Data class for event instance |
| `EventManager` | Singleton managing event triggers |
| `EventUI` | Handles event display and input |
| `EvidenceGenerator` | Generates evidence from events |

### SpecialEvent Class

```csharp
[Serializable]
public class SpecialEvent
{
    public SpecialEventType Type;
    public int Tier;
    public string Title;
    public string Description;
    public float TimeLimit;  // seconds
    public EventResolution[] PossibleResolutions;
    public EvidenceDrop[] PossibleDrops;
}
```

### EventManager API

```csharp
public class EventManager : MonoBehaviour
{
    public float BaseTriggerChance;
    public float CurrentThreatLevel;
    
    public void CheckForEvent();
    public void TriggerEvent(SpecialEventType type);
    public void HandleEventAction(EventAction action);
    public void ResolveEvent(EventResolution resolution);
}
```

### EventAction Enum

```csharp
public enum EventAction
{
    DiscussOnAir,
    Investigate,
    CallAuthorities,
    Ignore,
    Evacuate
}
```

## Balance Notes

### Trigger Rates

| Topic Level | Per-Broadcast Chance |
|-------------|---------------------|
| 3 | 10% |
| 4 | 15% |
| 5 | 20% |
| 6 | 25% |
| 7 | 30% |

Max 1 event per broadcast (or 2 at level 7).

### Expected Frequency

- First event: ~Night 8-12
- Regular events: ~1 per 5 broadcasts
- Critical events: ~1 per 20 broadcasts

### Player Response Distribution

| Action | % of Players |
|--------|--------------|
| Investigate | 60% |
| Discuss on Air | 25% |
| Call Authorities | 10% |
| Ignore | 5% |

## Future Enhancements

- **Event chains**: Events that evolve across multiple nights
- **Recurring antagonists**: Named villains that appear repeatedly
- **Player choices matter**: Event outcomes affect future content
- **Secret events**: Hidden events requiring specific conditions
- **Multi-broadcaster events**: Events affecting the station as a whole

## References

- [EVIDENCE_SYSTEM.md](EVIDENCE_SYSTEM.md) - Evidence drops from events
- [TOOLS_EQUIPMENT.md](TOOLS_EQUIPMENT.md) - Tools for investigation
- [TOPIC_EXPERIENCE.md](TOPIC_EXPERIENCE.md) - Topic level requirements
- [ECONOMY_PLAN.md](ECONOMY_PLAN.md) - Penalties and fines
