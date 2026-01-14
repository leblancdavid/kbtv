# KBTV - Vern's Stats System

## Overview

Vern's stats system tracks his physical, emotional, and cognitive state during broadcasts. These stats drive the VIBE metric, which determines listener growth and show quality. The system uses sigmoid curves for smooth, natural-feeling transitions.

## Stat Categories

### Dependencies (Decay-Only)
Things Vern is addicted to that cause immediate withdrawal symptoms when low.

| Stat | Range | Purpose | Critical Effect |
|------|-------|---------|-----------------|
| **Caffeine** | 0-100 | Hours since last coffee | Below 20: Headache, irritability, slower reactions |
| **Nicotine** | 0-100 | Hours since last cigarette | Below 20: Anxiety, restlessness, shaky hands |

### Physical Capacity (Decay + Recovery)
How long/how well Vern can perform.

| Stat | Range | Purpose | Low State Effect |
|------|-------|---------|------------------|
| **Energy** | 0-100 | Mental stamina for hosting | Slurred speech, slow responses, lower quality |
| **Satiety** | 0-100 | Hunger + Thirst combined | Irritability, faster fatigue |

### Emotional State (Bidirectional)
How Vern is currently feeling.

| Stat | Range | Purpose | Effect on Show |
|------|-------|---------|----------------|
| **Spirit** | -50 to +50 | Baseline emotional state | Modulates all mood expressions |

#### Spirit Tiers and Effects

| Spirit Range | Vern's Behavior | VIBE Modifier |
|--------------|-----------------|---------------|
| +30 to +50 | Enthusiastic, fighting spirit | 1.4-1.6x boost |
| +10 to +29 | Balanced, natural expression | 1.0-1.2x mild boost |
| -9 to +9 | Flat, muted | 1.0x neutral |
| -25 to -10 | Pessimistic, draining | 0.6-0.8x penalty |
| -50 to -26 | Crisis state, checked out | 0.3-0.5x severe penalty |

### Cognitive Performance (Derived + Direct)
How well Vern processes information.

| Stat | Range | Purpose | Low State Effect |
|------|-------|---------|------------------|
| **Alertness** | 0-100 | Wakefulness, reaction time | Misses conversation cues |
| **Discernment** | 0-100 | Ability to spot fakes/liars | Gets fooled by compelling fakes |
| **Focus** | 0-100 | Ability to stay on topic | Rabbitholes, loses callers |

### Long-Term (Persistent Across Nights)
Vern's trajectory and worldview.

| Stat | Range | Purpose | What Moves It |
|------|-------|---------|---------------|
| **Skepticism** | 0-100 | How easily Vern accepts claims | Cumulative caller credibility |
| **Topic Affinity** | -50 to +50 per topic | Relative interest in topic | Repeated topic choice |

---

## Mood Types (7 Total)

Mood types determine which dialog variant plays during conversations. Priority order:

```
Tired → Energetized → Irritated → Amused → Gruff → Focused → Neutral
```

| Mood Type | Trigger Conditions | Vern Sounds Like |
|-----------|-------------------|------------------|
| **Tired** | Energy < 30 | Slow, flat, misses cues |
| **Energized** | Caffeine > 60 AND Energy > 60 | Enthusiastic, quick-witted |
| **Irritated** | Spirit < -10 OR Patience < 40 | Snarky, dismissive |
| **Amused** | Spirit > 20 AND LastCallerPositive | Laughing, playful |
| **Gruff** | RecentBadCaller OR Spirit < 0 | Grumpy, reluctant |
| **Focused** | Alertness > 60 AND Discernment > 50 | Analytical, digging into claims |
| **Neutral** | Default state | Professional, balanced |

### Spirit Modulation Example

Same mood type, different Spirit:

**Mood Type: TIRED (Energy < 30)**

| Spirit | Vern Sounds Like | VIBE Impact |
|--------|------------------|-------------|
| +40 | "Alright folks, I'm running on fumes but we're doing this!" | -5 |
| +10 | "Okay... next caller... I'm listening..." | -10 |
| -20 | "I can't... I physically cannot do this anymore..." | -20 |
| -40 | "Why am I here... what's the point..." | -30 |

---

## VIBE (Vibrancy, Interest, Broadcast Entertainment)

VIBE is the composite metric that drives listener behavior.

### Component Calculations

```
Entertainment = Spirit×0.4 + Energy×0.3 + Alertness×0.2 + TopicAffinity×0.1
Credibility   = Discernment×0.5 + Skepticism×0.3 + EvidenceBonus×0.2
Engagement    = Focus×0.4 + Patience×0.3 + Spirit×0.2 + CallerQuality×0.1
```

### VIBE Formula

```
VIBE = (Entertainment × 0.4) + (Credibility × 0.3) + (Engagement × 0.3)
```

**Range**: -100 to +100

### Listener Growth (Sigmoid)

```csharp
float listenerRate = BaseRate × Sigmoid(VIBE / 100f);
```

| VIBE Range | Listeners/Min | Effect |
|------------|---------------|--------|
| -75 to -50 | -50 | Rapid decline |
| -49 to -25 | -15 | Steady decline |
| -24 to +24 | 0 | Stable |
| +25 to +50 | +10 | Steady growth |
| +51 to +75 | +25 | Good growth |
| +76 to +100 | +50 | Viral growth |

---

## Stat Decay Rules

| Stat | Base Decay | Accelerators |
|------|------------|--------------|
| Caffeine | -5/min | Low Energy (-2x), Stress (-1.5x) |
| Nicotine | -4/min | High Stress (-2x) |
| Energy | -2/min | Low Caffeine (-2x), Low Satiety (-1.5x), Low Spirit (-1.3x) |
| Satiety | -3/min | Talking (-1.5x) |
| Spirit | 0/min baseline | Bad caller (-8), Good caller (+8), Momentum (+1/min) |
| Patience | -3/min | Bad caller (-10), Time pressure (-2x) |

---

## Sigmoid Functions

### Spirit Modifier (For VIBE)

```csharp
float normalizedSpirit = spirit / 100f;  // -0.5 to +0.5
float modifier = 1.0f + (normalizedSpirit * 0.8f) + (normalizedSpirit * normalizedSpirit * 0.4f);

// Results:
// Spirit +50 → modifier = 1.58
// Spirit 0    → modifier = 1.0
// Spirit -50  → modifier = 0.58
```

### Other Sigmoid Applications

| Use | Purpose |
|-----|---------|
| Listener Growth | Smooth listener rate, no sharp cliffs |
| Decay Acceleration | Gradual degradation when stats approach 0 |
| Item Effectiveness | Diminishing returns on repeated use |
| Discernment | Sweet spot curve, low = worse than linear |

---

## Items and Effects

| Item | Primary | Secondary | Cooldown |
|------|---------|-----------|----------|
| **Coffee** | +Caffeine (+40) | +Energy (+15), +Alertness (+10) | 30s |
| **Cigarette** | +Nicotine (+35) | +Spirit (+10), -Energy (-5) | 30s |
| **Water** | +Satiety (+30) | +Spirit (+5) | 30s |
| **Sandwich** | +Satiety (+60) | +Energy (+20), -Spirit (-5) | 30s |
| **Whiskey** | +Spirit (+35) | +Energy (+10), -Discernment (-20), -Alertness (-15) | 60s |

### Item Diminishing Returns (Sigmoid)

```csharp
float effectiveness = Sigmoid(consecutiveUses * -0.5f);

// 1st use → 1.0 (100%)
// 2nd use → 0.73 (27% less)
// 3rd use → 0.45 (55% less)
// 4th use → 0.26 (74% less)
```

---

## Starting Values

| Stat | Starting Value |
|------|----------------|
| Caffeine | 50 |
| Nicotine | 50 |
| Energy | 100 |
| Satiety | 50 |
| Spirit | 0 |
| Patience | 50 |
| Alertness | 75 |
| Discernment | 50 |
| Focus | 50 |
| Skepticism | 50 |
| Topic Affinity | 0 (all topics) |

---

## UI Layout

```
[ VIBE: ████████░░ ] +25/min  (gradient: green to red)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[CAFFEINE] ████████░░  [NICOTINE] ████░░░░░
[ENERGY]   ████████░░  [SATIETY]  ████░░░░░
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[SPIRIT]   ████░░░░░  [-50   -25   0   +25   +50]
[PATIENCE] ██████░░░░
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[ALERTNESS]███████░░░ [DISCERNMENT]█████░░░░
[FOCUS]    ██████░░░░
```

### Crisis Warnings

- Stat < 25: Bar turns red, icon pulses
- VIBE < -25: Warning indicator, "LISTENERS LEAVING"
- Caffeine < 20: "VERN HAS HEADACHE"

---

## Implementation Classes

| Class | Purpose |
|-------|---------|
| `VernStats` | Main stat container, initialization, decay logic |
| `Stat` | Individual stat with clamping and events |
| `VernStateCalculator` | VIBE calculation, mood categorization, sigmoid functions |
| `VernMoodType` | Enum of mood types (7 values) |
| `ItemEffect` | Handles item application with diminishing returns |

---

## References

- [CONVERSATION_DESIGN.md](CONVERSATION_DESIGN.md) - Dialog mood variants
- [TOPIC_EXPERIENCE.md](TOPIC_EXPERIENCE.md) - Topic affinity system
- [EVIDENCE_SYSTEM.md](EVIDENCE_SYSTEM.md) - Evidence bonuses to credibility
- [ECONOMY_SYSTEM.md](ECONOMY_SYSTEM.md) - VIBE affects ad revenue
