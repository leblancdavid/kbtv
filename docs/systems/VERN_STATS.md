# KBTV - Vern's Stats System v2

## Overview

Vern's stats system tracks his physical, emotional, and mental state during broadcasts. The system consists of:

- **Three Core Stats**: Physical, Emotional, Mental (range: -100 to +100)
- **Two Dependencies**: Caffeine, Nicotine (range: 0-100, decay over time)
- **Topic Belief**: Per-topic tiered XP system with level floors

These stats combine to form **VIBE**, which determines listener growth and show quality.

---

## Core Stats

### Three-Stat System

All core stats range from **-100 to +100**, starting at **0**.

| Stat | Purpose | High State (+50 to +100) | Low State (-100 to -50) |
|------|---------|--------------------------|-------------------------|
| **Physical** | Energy, stamina, reaction time | Fast reactions, engaged, can handle long shows | Sluggish, slow reactions, misses conversation cues, yawning |
| **Emotional** | Mood, morale, passion | Enthusiastic, invested, engaging dialogue | Defeated, bitter, snarky/dismissive, checked out |
| **Mental** | Discernment, patience, focus | Spots hoaxers, patient with callers, draws out good content | Fooled by fakes, cuts callers off, snaps at them, goes on tangents |

### Stat Ranges and States

| Range | State | Description |
|-------|-------|-------------|
| +50 to +100 | **Excellent** | Peak performance |
| +25 to +49 | **Good** | Above average |
| -24 to +24 | **Neutral** | Functional, standard |
| -49 to -25 | **Poor** | Noticeable decline |
| -100 to -50 | **Critical** | Severe impairment |

---

## Dependencies

Dependencies are consumables that decay over time. When depleted, core stats begin to decay.

| Dependency | Range | Start | Base Decay | Decay Modifier |
|------------|-------|-------|------------|----------------|
| **Caffeine** | 0-100 | 100 | -5/min | Higher Mental → slower decay |
| **Nicotine** | 0-100 | 100 | -4/min | Higher Emotional → slower decay |

### Decay Modifier Formula

Dependencies decay slower when their associated stat is high:

```
Caffeine decay rate = BaseRate × (1 - (Mental / 100))
Nicotine decay rate = BaseRate × (1 - (Emotional / 100))
```

**Examples:**
- Mental at +100 → Caffeine decays at 50% rate (-2.5/min)
- Mental at 0 → Caffeine decays at 100% rate (-5/min)
- Mental at -100 → Caffeine decays at 200% rate (-10/min)

### Withdrawal Effects

When a dependency is depleted (reaches 0), core stats begin to decay:

| Dependency Depleted | Stats Affected | Decay Rates |
|---------------------|----------------|-------------|
| **Caffeine = 0** | Physical, Mental | Physical: -6/min, Mental: -3/min |
| **Nicotine = 0** | Emotional, Mental | Emotional: -6/min, Mental: -3/min |
| **Both = 0** | All three | Physical: -6/min, Emotional: -6/min, Mental: -6/min |

**Note:** Core stats do NOT decay while dependencies are above 0.

---

## Stat Interactions

Stats affect each other's decay rates when in critical states:

| Condition | Effect | Rationale |
|-----------|--------|-----------|
| Physical < -25 | Mental decay rate +50% | Exhaustion impairs thinking |
| Emotional < -25 | Physical decay rate +50% | Demoralization is draining |
| Mental < -25 | Caffeine & Nicotine decay +25% | Poor self-regulation |

These accelerators stack with withdrawal effects.

---

## VIBE Calculation

VIBE (Vibrancy, Interest, Broadcast Entertainment) is the composite metric that drives listener behavior.

### Formula

```
VIBE = (Physical × 0.25) + (Emotional × 0.40) + (Mental × 0.35)
```

**Range:** -100 to +100

**Weighting rationale:** Emotional is weighted highest because listeners respond most to Vern's engagement and passion.

### VIBE Effects on Listeners

| VIBE Range | Effect | Listeners/Min |
|------------|--------|---------------|
| +50 to +100 | Viral growth | +30 to +50 |
| +25 to +49 | Steady growth | +10 to +25 |
| -24 to +24 | Stable | -5 to +5 |
| -49 to -25 | Steady decline | -10 to -25 |
| -100 to -50 | Rapid decline | -30 to -50 |

---

## Mood Types

Mood types determine which dialogue variant plays during conversations. They are derived from core stats.

### Priority Order (First Match Wins)

```
Tired → Irritated → Energized → Amused → Focused → Gruff → Neutral
```

Negative states take priority over positive states.

### Mood Triggers

| Mood Type | Trigger Conditions | Vern Sounds Like |
|-----------|-------------------|------------------|
| **Tired** | Physical < -25 | Slow, flat, yawning, misses cues |
| **Irritated** | Emotional < -25 | Snarky, dismissive, short |
| **Energized** | Physical > +50 | Quick, enthusiastic, energetic |
| **Amused** | Emotional > +50 | Laughing, playful, engaged |
| **Focused** | Mental > +50 | Analytical, probing, sharp |
| **Gruff** | Emotional < 0 AND Mental > 0 | Grumpy but competent |
| **Neutral** | Default (none of above) | Professional, balanced |

---

## Caller Effects

Callers affect Vern's stats based on their quality:

| Caller Quality | Physical | Emotional | Mental |
|----------------|----------|-----------|--------|
| **Good caller** | +5 | +15 | +5 |
| **Bad caller** | -3 | -15 | -10 |
| **Catching hoaxer** | 0 | +5 | +10 |
| **Fooled by hoaxer** | 0 | -10 | -15 |

### Hoaxer Detection

Hoaxers are detected during **screening** (Option A):
- **Caught:** Player rejects a low-legitimacy caller during screening
- **Fooled:** Player approves a low-legitimacy caller who goes on-air

Higher Mental (via Topic Belief bonuses) provides screening hints to help identify low-legitimacy callers.

---

## Items

| Item | Primary Effect | Secondary Effect | Cooldown |
|------|----------------|------------------|----------|
| **Coffee** | Caffeine → 100 | Physical +10 | 30s |
| **Cigarette** | Nicotine → 100 | Emotional +5 | 30s |

---

## Topic Belief System

Belief is a **per-topic tiered XP system** that tracks Vern's growing conviction in each topic over time.

### Key Mechanics

- Belief can go up and down based on caller quality
- Once you reach a tier, you cannot drop below that tier's floor
- Higher tiers provide Mental bonuses for that topic

### Tier Structure

| Tier | Name | Belief Required | Mental Bonus | Other Bonuses |
|------|------|-----------------|--------------|---------------|
| 1 | Skeptic | 0 | +0% | None |
| 2 | Curious | 100 | +5% | - |
| 3 | Interested | 300 | +10% | Screening hints |
| 4 | Believer | 600 | +15% | Better caller pool |
| 5 | True Believer | 1000 | +20% | Expert guests available |

### Belief Changes

| Event | Belief Change |
|-------|---------------|
| Good caller (on-topic) | +10 to +20 |
| Bad/hoax caller (on-topic) | -5 to -15 (cannot drop below tier floor) |
| Show completed (on-topic) | +25 |

### Tier Floor Example

```
Current: Tier 3 (Interested), Belief = 350

Bad caller: -15 belief → Belief = 335 (still Tier 3)
Bad caller: -15 belief → Belief = 320 (still Tier 3)
Bad caller: -15 belief → Belief = 305 (still Tier 3)
Bad caller: -15 belief → Belief = 300 (floor reached, stays at 300)

Cannot drop to Tier 2 once Tier 3 is reached.
```

---

## Starting Values

| Stat/Dependency | Starting Value |
|-----------------|----------------|
| Physical | 0 |
| Emotional | 0 |
| Mental | 0 |
| Caffeine | 100 (fresh cup of coffee) |
| Nicotine | 100 (new pack of smokes) |
| Topic Belief | 0 (Tier 1: Skeptic) |

---

## UI Layout

### VERN Tab

```
┌─────────────────────────────────────────────────────────────┐
│  VIBE  [░░░░░░░████████████░░░░]  +25   FOCUSED             │
│                                                             │
│  ─── DEPENDENCIES ──────────────────────────────────────    │
│  CAFFEINE   [████████░░░░░░░░░░]  80/100                    │
│  NICOTINE   [████░░░░░░░░░░░░░░]  40/100                    │
│                                                             │
│  ─── CORE STATS ────────────────────────────────────────    │
│  PHYSICAL   [░░░░░░░░░░|██████░░]  +30                      │
│  EMOTIONAL  [░░░░░░████|░░░░░░░░]  -20                      │
│  MENTAL     [░░░░░░░░░░|████░░░░]  +15                      │
│                                                             │
│  ─── TOPIC BELIEF ──────────────────────────────────────    │
│  UFOs & Aliens                                              │
│  Tier 3: INTERESTED       [████████░░]  245/300             │
│  Bonus: +10% Mental                                         │
└─────────────────────────────────────────────────────────────┘
```

### Crisis Warnings

| Condition | Warning |
|-----------|---------|
| Caffeine = 0 | "CAFFEINE CRASH" |
| Nicotine = 0 | "NICOTINE WITHDRAWAL" |
| Physical < -50 | "EXHAUSTED" |
| Emotional < -50 | "DEMORALIZED" |
| Mental < -50 | "UNFOCUSED" |
| VIBE < -25 | "LISTENERS LEAVING" |

---

## Implementation Classes

| Class | Purpose |
|-------|---------|
| `Stat` | Individual stat with min/max, clamping, and change events |
| `VernStats` | Main stat container, initialization, VIBE/mood calculation |
| `VernStatsMonitor` | Handles decay logic in game loop |
| `VernMoodType` | Enum of 7 mood types |
| `TopicXP` | Per-topic belief tracking with tiers |

---

## Design Rationale

### Why Three Stats?

Consolidating from 9+ stats to 3 provides:
- **Clarity:** Each stat has a clear, distinct purpose
- **Readability:** Easy to understand at a glance
- **Balance:** Easier to tune interactions
- **UI simplicity:** Clean display without clutter

### Why Dependencies Separate?

Caffeine and Nicotine are:
- **Consumables** (can be replenished with items)
- **Decay buffers** (protect core stats from decay)
- **Strategic choices** (when to use coffee/cigarettes)

### Why Tiered Belief?

- **Progression feel:** Clear milestones and level-ups
- **Safety net:** Can't lose all progress from a bad night
- **Meaningful bonuses:** Each tier unlocks tangible benefits

---

## References

- [CONVERSATION_DESIGN.md](../ui/CONVERSATION_DESIGN.md) - Dialogue mood variants
- [TOPIC_EXPERIENCE.md](../design/TOPIC_EXPERIENCE.md) - Topic system overview
- [ECONOMY_SYSTEM.md](ECONOMY_SYSTEM.md) - VIBE affects ad revenue
