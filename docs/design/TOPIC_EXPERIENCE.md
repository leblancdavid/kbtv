# KBTV Topic Experience System

## Status: PLANNING

This document outlines the topic experience and leveling system for KBTV, providing meta-progression that rewards specialization and encourages variety.

## Overview

Each topic has an **experience level** that increases as you do shows on that topic. Higher levels unlock bonuses, making you more effective at handling that topic over time.

## Core Concepts

| Concept | Description |
|---------|-------------|
| **Topic** | A show subject (UFOs, Government, etc.) |
| **XP** | Experience points gained from shows |
| **Level** | Current mastery tier (1-7) |
| **Freshness** | Modifier affecting XP gain (encourages variety) |

---

## Topics

### Starting Topics (4-6)

| Topic | Description |
|-------|-------------|
| UFOs & Aliens | Sightings, abductions, government cover-ups |
| Government Conspiracies | Secret programs, cover-ups, deep state |
| Supernatural | Ghosts, hauntings, paranormal activity |
| Cryptids | Bigfoot, Mothman, mysterious creatures |

### Future Expansion Topics

| Category | Examples |
|----------|----------|
| Expansion 1 | Time Travel, Ancient Aliens, Secret Societies |
| Expansion 2 | Psychic Phenomena, Alternative History, Underground Bases |
| Special | Crossover events, seasonal topics |

### Topic Data Structure

Topics are data-driven for easy expansion:

```csharp
[CreateAssetMenu(fileName = "Topic", menuName = "KBTV/Topic")]
public class TopicData : ScriptableObject
{
    public string ID;                    // "ufos"
    public string DisplayName;           // "UFOs & Aliens"
    public string Description;           // "Sightings, abductions..."
    public Sprite Icon;                  // Optional, placeholder OK
    public int[] XPThresholds;           // [0, 200, 500, 1000, 2000, 3500, 5000]
    public string[] LevelTitles;         // ["Novice", "Amateur", ...]
    public bool StartsUnlocked = true;   // For future locked topics
}
```

---

## Experience System

### XP Sources

| Source | Base XP | Notes |
|--------|---------|-------|
| Complete show | 100 | Primary source, × freshness multiplier |
| Show quality bonus | 0-50 | Based on final show quality % |
| Correct screening | 5-10 | Per correctly rejected bad caller |
| Great caller | 5-15 | Per high-quality caller aired |
| Special event | 25-50 | Topic-specific events |

### Level Thresholds

| Level | Total XP | Shows (approx) | Title |
|-------|----------|----------------|-------|
| 1 | 0 | 0 | Novice |
| 2 | 200 | ~2 | Amateur |
| 3 | 500 | ~5 | Familiar |
| 4 | 1,000 | ~10 | Experienced |
| 5 | 2,000 | ~18 | Expert |
| 6 | 3,500 | ~30 | Authority |
| 7 | 5,000 | ~45 | Master |

---

## Freshness System

Encourages topic variety without punishing specialization harshly.

### Freshness Levels

Each topic has a **freshness level** (1-5):

| Freshness | XP Multiplier |
|-----------|---------------|
| 5 (Fresh) | 100% |
| 4 | 80% |
| 3 | 60% |
| 2 | 40% |
| 1 (Stale) | 20% |

### After Each Show

- **Covered topic**: Freshness decreases by 1 (minimum 1)
- **All other topics**: Freshness increases by 1 (maximum 5)

### Example Progression

Starting state (all topics fresh at 5):
```
Night 1 - Cover UFOs:
  UFOs: 5→4 (80%)
  Others: stay at 5 (already max)

Night 2 - Cover UFOs again:
  UFOs: 4→3 (60%)
  Others: stay at 5

Night 3 - Cover Government:
  UFOs: 3→4 (80%)      ← recovered
  Government: 5→4 (80%)
  Others: stay at 5

Night 4 - Cover Supernatural:
  UFOs: 4→5 (100%)     ← fully recovered
  Government: 4→5 (100%)
  Supernatural: 5→4 (80%)
  Cryptids: stays at 5
```

### Design Benefits

- **Symmetric recovery**: Takes as many nights to recover as it took to deplete
- **Encourages rotation**: Optimal XP requires cycling through topics
- **No hard punishment**: Can still grind one topic, just less efficiently
- **Simple math**: +1 or -1 per night, clamped to 1-5

---

## Level Bonuses

### Vern's Mental Bonus

Higher topic level = Vern is better at detecting fake callers (Mental stat boost).

| Level | Mental Bonus |
|-------|--------------|
| 1 | +0% |
| 2 | +5% |
| 3 | +10% |
| 4 | +15% |
| 5 | +20% |
| 6 | +25% |
| 7 | +30% |

**Effect**: Higher Mental helps detect hoaxers during screening. See [VERN_STATS.md](../systems/VERN_STATS.md) for how Mental interacts with the three-stat system.

**Note**: This is separate from Topic Belief (see VERN_STATS.md), which tracks Vern's conviction in each topic and provides its own tier-based Mental bonuses. Topic Experience (XP/levels) tracks player skill progression, while Topic Belief tracks Vern's in-world belief.

### Screening Information

Higher topic level = more caller info visible during screening.

| Level | Information Visible |
|-------|---------------------|
| 1 | Base info (name, location, topic) |
| 2 | + Caller mood indicator |
| 3 | + Legitimacy hint (vague) |
| 4 | + Phone quality indicator |
| 5 | + Legitimacy tier (Low/Med/High) |
| 6 | + Caller patience level |
| 7 | Full caller profile |

### Caller Quality Pool

Higher topic level = better callers attracted to your show.

| Level | Effect |
|-------|--------|
| 1-2 | Normal distribution |
| 3-4 | +5% chance of high-legitimacy callers |
| 5-6 | +10% chance, fewer "terrible" phone quality |
| 7 | +15% chance, occasional "superfan" callers |

### Vern's Starting Mood

Higher topic level = Vern is more comfortable on familiar topics.

| Level | Effect |
|-------|--------|
| 1-2 | No bonus |
| 3-4 | +5 starting mood |
| 5-6 | +10 starting mood |
| 7 | +15 starting mood, slower mood decay |

### Off-Topic Callers

The caller generation system creates **10% off-topic callers** to encourage topic variety and prevent meta-gaming:

- **Generation**: `Topic.OffTopicRate` controls probability (default 0.1f = 10%)
- **XP Impact**: Off-topic callers grant reduced XP (50% of normal) to discourage grinding one topic
- **Screening**: Off-topic callers are transparent about their actual topic (no deception)
- **Freshness**: Off-topic callers count as "fresh" for their actual topic, helping recover freshness

**Example**: Show on UFOs, caller discusses Government Conspiracies:
- XP gained: 50% of normal (if show was on-topic, would get full XP)
- Government topic freshness increases (helps variety)
- UFO topic still gets reduced XP for hosting the show

### Bonus Summary Table

| Level | Mental | Screening | Callers | Mood |
|-------|--------|-----------|---------|------|
| 1 | +0% | Base | Normal | +0 |
| 2 | +5% | +Mood | Normal | +0 |
| 3 | +10% | +Hint | +5% good | +5 |
| 4 | +15% | +Phone | +5% good | +5 |
| 5 | +20% | +Tier | +10% good | +10 |
| 6 | +25% | +Patience | +10% good | +10 |
| 7 | +30% | Full | +15% good | +15 |

---

## Special Unlocks

### By Level

| Level | Unlock |
|-------|--------|
| 3 | Recurring caller (flavor), topic-specific sponsor available |
| 4 | Expert guest booking unlocked, new conversation arcs |
| 5 | Breaking news events, premium sponsors |
| 6 | Insider contact, cross-topic content hints |
| 7 | Documentary offer, exclusive sponsorship deals |

### Recurring Callers

- Unlock at level 3
- Familiar callers who call back with follow-up stories
- Simple flavor (no persistent state tracking)
- Add variety and world-building

### Expert Guests

- Unlock at level 4+
- Separate booking system (not regular callers)
- See Guest Booking section below

---

## Guest Booking System

Guests are special segments, separate from the caller queue.

### Overview

| Aspect | Description |
|--------|-------------|
| Unlock | Topic level 4+ |
| Booking | PreShow phase |
| Cost | Money or "contact" resource |
| Duration | Dedicated segment during show |
| Effect | Listener boost, Vern mood boost, special dialogue |

### Guest Tiers

| Type | Unlock | Listener Boost | Notes |
|------|--------|----------------|-------|
| Amateur Expert | Level 4 | +10% | Enthusiast, basic dialogue |
| Professional | Level 5 | +20% | Author, researcher |
| Celebrity | Level 6 | +30% | Famous figure, special events possible |
| Insider | Level 7 | +40% | Government source, exclusive content |

### Guest Booking UI (PreShow)

```
+------------------------------------------+
|           BOOK A GUEST                   |
+------------------------------------------+
|                                          |
|  Topic: UFOs & Aliens (Level 5)          |
|                                          |
|  Available Guests:                       |
|  ┌────────────────────────────────────┐  |
|  │ [BOOK] Dr. Marcus Webb             │  |
|  │ Professional - UFO Researcher      │  |
|  │ Cost: $150 | Listeners: +20%       │  |
|  └────────────────────────────────────┘  |
|  ┌────────────────────────────────────┐  |
|  │ [BOOK] "Skywatch Steve"            │  |
|  │ Amateur Expert - Enthusiast        │  |
|  │ Cost: $50 | Listeners: +10%        │  |
|  └────────────────────────────────────┘  |
|                                          |
|  [Skip Guest Tonight]                    |
+------------------------------------------+
```

### Future Considerations

- Guest availability (some guests busy certain nights)
- Guest relationships (repeat bookings improve quality)
- Exclusive guests (one-time special events)

---

## UI Design

### Topic Selection (PreShow)

```
+------------------------------------------+
|           TONIGHT'S TOPIC                |
+------------------------------------------+
|                                          |
|  [UFOs & ALIENS]        Level 4 ████░░░  |
|  "Experienced"          1250/2000 XP     |
|  Freshness: ●●●●○ (80%)                  |
|  Bonuses: +15% Mental, Mood +5           |
|                                          |
|  [GOVERNMENT]           Level 2 ██░░░░░  |
|  "Amateur"              350/500 XP       |
|  Freshness: ●●●●● (100%)                 |
|  Bonuses: +5% Mental                     |
|                                          |
|  [SUPERNATURAL]         Level 1 █░░░░░░  |
|  "Novice"               75/200 XP        |
|  Freshness: ●●●●● (100%)                 |
|  Bonuses: None                           |
|                                          |
|  [CRYPTIDS]             Level 3 ███░░░░  |
|  "Familiar"             780/1000 XP      |
|  Freshness: ●●●○○ (60%)                  |
|  Bonuses: +10% Mental, +5 Mood           |
|                                          |
+------------------------------------------+
```

### Post-Show XP Summary

```
+------------------------------------------+
|         TOPIC: UFOs & ALIENS             |
+------------------------------------------+
|                                          |
|  Freshness: ●●●●○ (80% XP)               |
|                                          |
|  Show Completed:      100 × 80% = 80 XP  |
|  Quality Bonus (78%):          +22 XP    |
|  Good Callers (2):             +20 XP    |
|  Bad Caller Caught:            +10 XP    |
|  ─────────────────────────────────       |
|  TOTAL:                       +132 XP    |
|                                          |
|  Level 4 → 5:  1382/2000 XP  ███████░░░  |
|                                          |
|  [Continue]                              |
+------------------------------------------+
```

### Level Up Notification (Post-Show)

```
+------------------------------------------+
|            ★ LEVEL UP! ★                 |
+------------------------------------------+
|                                          |
|  UFOs & ALIENS                           |
|  Level 4 → Level 5                       |
|  "Experienced" → "Expert"                |
|                                          |
|  New Bonuses:                            |
|  • Mental: +15% → +20%                   |
|  • Screening: Now shows Legitimacy Tier  |
|  • Caller Quality: +10% good callers     |
|  • Starting Mood: +5 → +10               |
|                                          |
|  New Unlock:                             |
|  • Breaking News events now possible!    |
|  • Premium sponsors available            |
|                                          |
|  [Awesome!]                              |
+------------------------------------------+
```

---

## Persistence

### Save Data

```csharp
[Serializable]
public class TopicProgressData
{
    public string TopicID;
    public int CurrentXP;
    public int CurrentLevel;
    public int Freshness;  // 1-5
}

// In SaveData
public List<TopicProgressData> TopicProgress;
```

---

## Implementation Phases

### Phase 1: Core XP System
- [ ] TopicData ScriptableObject
- [ ] TopicProgressData runtime class
- [ ] TopicProgressManager singleton
- [ ] XP gain on show completion
- [ ] Level calculation from XP
- [ ] Persistence (save/load)

### Phase 2: Freshness System
- [ ] Freshness tracking per topic
- [ ] Freshness adjustment after each show
- [ ] XP multiplier from freshness
- [ ] UI indicator for freshness

### Phase 3: Level Bonuses
- [ ] Mental bonus integration with three-stat system
- [ ] Screening info visibility by level
- [ ] Caller quality pool adjustment
- [ ] Starting mood bonus

### Phase 4: UI
- [ ] Topic selection shows level/XP/freshness/bonuses
- [ ] Post-show XP breakdown screen
- [ ] Level-up notification popup

### Phase 5: Special Unlocks
- [ ] Recurring caller system
- [ ] Topic-specific sponsors
- [ ] Topic-specific events

### Phase 6: Guest System
- [ ] Guest data structure
- [ ] Guest booking UI (PreShow)
- [ ] Guest segment during show
- [ ] Guest effects (listeners, mood, dialogue)

### Phase 7: Expansion Support
- [ ] Topic unlock system
- [ ] New topic content pipeline
- [ ] Locked topic UI

---

## References

- [GAME_DESIGN.md](GAME_DESIGN.md) - Core game design
- [ECONOMY_PLAN.md](ECONOMY_PLAN.md) - Economy system (sponsor unlocks tie in)
- [CONVERSATION_DESIGN.md](CONVERSATION_DESIGN.md) - Dialogue system
- [CONVERSATION_ARCS.md](CONVERSATION_ARCS.md) - Arc content per topic
