# KBTV - Ad System

## Status: IMPLEMENTED (Core System Complete)

This document outlines the advertisement system for KBTV. The ad system is the primary source of income, replacing the temporary per-show stipend.

**Last Updated:** January 2026 - Added transcript integration, queue button countdown, and revenue calculation improvements.

## Overview

Players configure and schedule ad breaks during shows to generate income. Better ads unlock as you reach listener milestones. Ad revenue is calculated per-listener, making audience growth directly tied to income.

## Time Budget (10-minute show)

| Component | Duration |
|-----------|----------|
| Show duration | 600 seconds (10 min real-time) |
| In-game time | 4 hours (10 PM - 2 AM) |
| Ad slot duration | 18 seconds (avg) |
| Break jingle | 5 seconds |
| Return jingle | 3 seconds |
| Max ad breaks | 10 per show |
| Max slots per break | 3 |

### Typical Configurations

| Config | Ad Time | Content Time |
|--------|---------|--------------|
| 2 breaks x 2 slots | ~1:24 | ~8:36 |
| 3 breaks x 2 slots | ~2:06 | ~7:54 |
| 4 breaks x 3 slots | ~4:12 | ~5:48 |

## Core Concepts

| Concept | Description |
|---------|-------------|
| **Ad Break** | Scheduled pause in broadcast for advertisements |
| **Ad Slot** | A single ad within an ad break (18 seconds) |
| **Ad Type** | Tier of advertiser (Local to Premium) |
| **Per-Listener Rate** | Revenue per listener per ad slot |
| **Peak Listeners** | Highest listener count achieved (unlocks ads) |
| **Break Window** | 20-second window before scheduled break when player can queue |

## Ad Types and Revenue

| Type | Rate ($/listener/slot) | Unlock Threshold |
|------|------------------------|------------------|
| LocalBusiness | $0.02 | Starting |
| RegionalBrand | $0.04 | 200 peak listeners |
| NationalSponsor | $0.06 | 500 peak listeners |
| PremiumSponsor | $0.10 | 1000 peak listeners |

### Revenue Formula

```
Slot Revenue = Listeners * Rate
Break Revenue = Sum of all slot revenues
Show Revenue = Sum of all break revenues
```

Example: 150 listeners, 2 breaks x 2 slots (LocalBusiness @ $0.02)
- Per slot: 150 * $0.02 = $3.00
- Per break: 2 * $3.00 = $6.00
- Show total: 2 * $6.00 = $12.00

## Transcript Integration

Ad breaks are fully integrated into the broadcast transcript for player visibility:

### Transcript Entries
- **"=== AD BREAK ==="** - System message when break starts
- **"AD BREAK (1)", "AD BREAK (2)", etc.** - Individual ad slots with sequential numbering
- **"=== END AD BREAK ==="** - System message when break ends

### Speaker System
- New `Speaker.System` enum for system messages (breaks, transitions)
- Sponsor names displayed: "Ad sponsored by Local Business", "Ad sponsored by Premium Sponsor"
- `AdData.GetAdTypeDisplayName()` provides formatted sponsor names

### Queue Button Countdown
The queue button shows dynamic countdowns based on real-time calculations:

| State | Button Text | Description |
|-------|-------------|-------------|
| **Waiting** | "BREAK IN 1:32" | Countdown to break window using `GetNextBreakTime()` - current time |
| **Ready** | "QUEUE AD-BREAK" | In 20-second break window, button enabled |
| **Queued** | "QUEUED 0:15" | Player queued, shows time until break starts |
| **Playing** | "ON BREAK" | Ads playing, shows current break status |
| **Done** | "NO BREAKS" | All breaks completed |

### Revenue Calculation Details
Revenue is calculated per-listener with temporary listener dip during breaks:

- **Base Calculation**: `Listeners * AdType.Rate` per slot
- **Listener Dip**: ~5% temporary listener loss during ads (`LISTENER_DIP_PERCENTAGE = 0.05`)
- **Break Total**: Sum of all slot revenues in the break
- **Show Total**: Sum of all break revenues
- **Mood Penalty**: -15 Patience if player doesn't queue break before window ends

## Pre-Show Configuration

Players configure their ad schedule before the show starts using the Ad Configuration panel in PreShow.

```
+------------------------------------------+
|         AD BREAK CONFIGURATION           |
+------------------------------------------+
|  BREAKS PER SHOW:    [<] 2 [>]           |
|  SLOTS PER BREAK:    [<] 2 [>]           |
|                                          |
|  Est. Revenue: $12 - $24                 |
|  Ad Time: ~1:24                          |
+------------------------------------------+
```

### Default Values
- Breaks per show: 2 (configurable 0-10)
- Slots per break: 2 (configurable 1-3)

### Timing Calculation
Breaks are evenly spaced throughout the show:
- 1 break → at 50% of show
- 2 breaks → at 33% and 66%
- 3 breaks → at 25%, 50%, and 75%
- 4 breaks → at 20%, 40%, 60%, and 80%

Formula: `BreakTime[i] = ShowDuration * (i + 1) / (BreakCount + 1)`

## Live Show Controls

### Queue Button States

The Ad Break panel in the live show footer shows different states:

| State | Button Text | Description |
|-------|-------------|-------------|
| **Waiting** | "BREAK IN 1:32" | Countdown to break window |
| **Ready** | "QUEUE AD-BREAK" | In break window, button enabled |
| **Queued** | "QUEUED 0:15" | Player queued, countdown to break |
| **Playing** | "ON BREAK" | Ads are playing |
| **Done** | "NO BREAKS" | All breaks used |

### Break Window Flow

Each break has a **20-second window** before the scheduled break time. The "QUEUE AD-BREAK" button is only enabled during this window.

```
Show Progress: 0%────[WINDOW OPENS]────[GRACE STARTS]────[BREAK TIME]────33%
                           ↑                    ↑              ↑
                      20 sec before        10 sec before  Break starts
                      Button enabled       Transition prep   (always happens)
```

**Key concept:** The break **always happens** at the scheduled time. The button is a **warning system** for Vern, not a break trigger. When queued, Vern gets a natural transition instead of being cut off.

#### Detailed Timing Flow

**Break Window (T-20s to T-0s):**
- **T-20s:** Button becomes visible and enabled ("QUEUE AD-BREAK")
- **T-10s:** If queued, grace period begins (transition preparation)
- **T-9s to T-5s:** Current conversation wraps up, transition line interrupts
- **T-5s:** Imminent warning (fallback if transition fails)
- **T-0s:** Break starts (smoothly if queued, abruptly if not)

**Event-Driven Architecture:**
- Uses events to coordinate between `BroadcastCoordinator` and `AdManager`
- Transition lines interrupt current dialogue immediately when ready
- Audio and UI update simultaneously for seamless experience

### Player Actions

**If player clicks "QUEUE AD-BREAK" during window:**
1. Transition music starts playing (audio cue to Vern)
2. Button changes to "QUEUED" with countdown
3. **Grace period begins** (T-10s to T-5s):
   - Vern hears the music and wraps up current conversation
   - When current line finishes, transition line interrupts immediately
   - Vern speaks break transition dialogue ("Alright folks, time for a quick break...")
   - Transition line displays in UI and plays audio
4. At break time: break starts smoothly after transition completes, **no penalty**

**If player does NOT queue before break time:**
1. Break starts immediately (interrupts everything)
2. **Mood penalty applied** (Vern wasn't warned)
3. Vern gets cut off mid-broadcast

### Full Break Sequence

1. **Window opens (T-20 sec)** - "QUEUE AD-BREAK" button becomes visible and enabled
2. **Player clicks Queue** (optional) - Button shows "QUEUED" with countdown, transition music plays
3. **Grace period (T-10 sec)** - BroadcastCoordinator enters BreakGracePeriod state
4. **Transition preparation (T-9 sec)** - Transition line created, OnTransitionLineAvailable event fired
5. **Immediate interruption** - ConversationDisplay receives event, interrupts current line, displays transition
6. **Transition line plays** - Vern speaks break transition ("Alright folks, time for a quick break...")
7. **Break imminent (T-5 sec)** - Fallback interrupt if transition fails (rare)
8. **Break time reached (T-0)** - Break starts smoothly after transition completes (no penalty)
9. **Break jingle plays** (5 seconds)
10. **Ad slots play sequentially** (18 seconds each)
11. **Return jingle plays** (3 seconds)
12. **Show resumes** - Next caller can be taken

**Unqueued Break (penalty path):**
1. **Window opens (T-20 sec)** - Button enabled but player doesn't click
2. **Break imminent (T-5 sec)** - No transition, current line continues
3. **Break time reached (T-0)** - Break starts immediately, interrupts everything
4. **Mood penalty applied** (-15 Patience) for not warning Vern

### Mood Penalty

If the player **doesn't click "QUEUE AD-BREAK"** before the break time, Vern suffers a mood penalty:
- Penalty: **-15 Patience** (affects VIBE calculation)
- The penalty applies even if Vern wasn't actively speaking
- Queuing the break = no penalty (Vern gets natural transition instead of interruption)

## Listener Impact

- Ads cause temporary listener dip (~5% during break)
- Listeners gradually return after break ends
- Too many breaks = cumulative listener fatigue
- Well-timed breaks (between callers) minimize loss

## Files Created

| File | Description |
|------|-------------|
| `scripts/ads/AdBreakConfig.cs` | Single break configuration with timing |
| `scripts/ads/AdSchedule.cs` | Player's ad configuration for show |
| `scripts/ads/AdConstants.cs` | Timing and configuration constants |
| `scripts/ads/AdManager.cs` | Autoload service for break management |
| `scripts/ads/AdData.cs` | Ad resource with revenue data |
| `scripts/ads/AdType.cs` | Enum for ad tiers |
| `scripts/dialogue/IBroadcastCoordinator.cs` | Interface for broadcast coordination |
| `assets/audio/music/break_jingle.wav` | 5s placeholder break jingle |
| `assets/audio/music/return_jingle.wav` | 3s placeholder return jingle |

## Files Modified

| File | Changes |
|------|---------|
| `scripts/core/GameStateManager.cs` | Added AdSchedule property, Initialize AdManager |
| `scripts/core/ServiceRegistry.cs` | Added AdManager service property |
| `scripts/ui/PreShowUIManager.cs` | Added ad configuration UI panel |
| `scripts/ui/LiveShowFooter.cs` | Integrated AdManager queue button states |
| `scenes/ui/LiveShowFooter.tscn` | Updated scene with new ad controls |
| `scripts/dialogue/BroadcastCoordinator.cs` | Added AdBreak state, OnAdBreakStarted/Ended |

## Implementation Phases

### Phase 1: Core System (Complete)

- [x] AdSchedule data class with break/slot configuration
- [x] PreShow UI for ad configuration
- [x] AdManager autoload service with break scheduling
- [x] Evenly spaced break timing calculation
- [x] Queue button with states (Waiting, Ready, Queued, Playing, Done)
- [x] AdBreak state in BroadcastCoordinator
- [x] Mood penalty for unqueued breaks
- [x] Listener dip during breaks
- [x] Revenue calculation at break end

### Phase 2: Audio (Complete)

- [x] Break jingle placeholder (5s silent)
- [x] Return jingle placeholder (3s silent)
- [x] AdManager events for audio triggering
- [x] BroadcastCoordinator integration

### Phase 3: Future Enhancements

- [ ] Ad selection/management screen
- [ ] Real audio content for jingles
- [ ] Ad script generation/display
- [ ] Dynamic ad pricing
- [ ] Relationship building with sponsors
- [ ] Exclusive deals
- [ ] Ad quality affects listener retention

## Key Constants

| Constant | Value |
|----------|-------|
| `SHOW_DURATION_SECONDS` | 600 |
| `AD_SLOT_DURATION` | 18 |
| `MAX_BREAKS_PER_SHOW` | 10 |
| `MAX_SLOTS_PER_BREAK` | 3 |
| `BREAK_JINGLE_DURATION` | 5 |
| `RETURN_JINGLE_DURATION` | 3 |
| `BREAK_WINDOW_DURATION` | 20 |
| `UNQUEUED_MOOD_PENALTY` | 15 |
| `LISTENER_DIP_PERCENTAGE` | 0.05 |

## Integration Points

| System | Integration |
|--------|-------------|
| Economy | Ad revenue -> EconomyManager.AddMoney() |
| Time | Breaks consume show time (included in 10 min) |
| Listeners | Ads affect listener count temporarily (5% dip) |
| Conversations | AdBreak state interrupts/returns to conversation |
| VernStats | Mood penalty if Vern cut off mid-sentence |
| Save | Track PeakListenersAllTime for unlock system |
| Audio | Break/return jingles, transition music |
