# KBTV - Ad System

## Status: IMPLEMENTED (Phase 1 Complete)

This document outlines the advertisement system for KBTV. The ad system is the primary source of income, replacing the temporary per-show stipend.

## Overview

Players schedule ad breaks during shows to generate income. Better ads unlock as you reach listener milestones. Ad revenue is calculated per-listener, making audience growth directly tied to income.

## Time Budget (10-minute show)

| Component | Duration |
|-----------|----------|
| Show duration | 600 seconds (10 min real-time) |
| In-game time | 4 hours (10 PM - 2 AM) |
| Ad slot duration | 18 seconds (avg) |
| Break jingle | 5 seconds |
| Return jingle | 3 seconds |
| Max ad breaks | 4 per show |
| Max slots per break | 3 |

### Typical Configurations

| Config | Ad Time | Content Time |
|--------|---------|--------------|
| 3 breaks x 2 slots | ~2 min | ~8 min |
| 4 breaks x 3 slots | ~4 min | ~6 min |

## Core Concepts

| Concept | Description |
|---------|-------------|
| **Ad Break** | Scheduled pause in broadcast for advertisements |
| **Ad Slot** | A single ad within an ad break (18 seconds) |
| **Ad Type** | Tier of advertiser (Local to Premium) |
| **Per-Listener Rate** | Revenue per listener per ad slot |
| **Peak Listeners** | Highest listener count achieved (unlocks ads) |
| **Ad Shuffle** | Ads are shuffled at show start for variety |

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

## Ad Break Flow

### Queue-Based Break System

Breaks are **evenly spaced throughout the show** and occur at fixed times. The player's job is to **queue the break** before it happens, giving Vern a heads-up via transition music. If the player doesn't queue, Vern gets cut off and suffers a mood penalty.

#### Break Timing Calculation

If the player schedules **N breaks**, breaks occur at evenly distributed points:
- 1 break → at 50% of show
- 2 breaks → at 33% and 66%
- 3 breaks → at 25%, 50%, and 75%
- 4 breaks → at 20%, 40%, 60%, and 80%

Formula: `BreakTime[i] = ShowDuration * (i + 1) / (BreakCount + 1)`

#### Break Window Flow

Each break has a **20-second window** before the scheduled break time. The "Queue Ads" button is only enabled during this window.

```
Show Progress: 0%────[WINDOW OPENS]────[BREAK TIME]────33%
                           ↑                  ↑
                      20 sec before      Break starts
                      Button enabled     (always happens)
```

**Key concept:** The break **always happens** at the scheduled time. The button is a **warning system** for Vern, not a break trigger.

#### Player Actions

**If player clicks "Queue Ads" during window:**
1. Transition music starts playing (audio cue to Vern)
2. Button changes to "QUEUED" (disabled)
3. Vern hears the music and wraps up naturally
4. At break time: break starts smoothly, **no penalty**

**If player does NOT queue before break time:**
1. Break starts immediately (interrupts everything)
2. **Mood penalty applied** (Vern wasn't warned)
3. Vern gets cut off mid-broadcast

#### Full Break Sequence

1. **Window opens (T-20 sec)** - "QUEUE ADS" button becomes enabled
2. **Player clicks Queue** (optional) - Transition music plays, button shows "QUEUED"
3. **Grace period (T-10 sec)** - If queued, let current dialogue line finish naturally
4. **Transition line** - Once line finishes, Vern speaks break transition (if time permits)
5. **Break imminent (T-5 sec)** - Fallback interrupt if grace period didn't trigger
6. **Break time reached (T-0)** - Break starts (penalty if not queued), transition music stops
7. **Break jingle plays** (5 seconds)
8. **Ad slots play sequentially** (18 seconds each)
9. **Return jingle plays** (3 seconds)
10. **Show resumes** - Next caller can be taken

```
Timeline:
T-20s ─────────── T-10s ──────── T-5s ────── T-0s ──────────────── T+break
  │                  │              │           │                      │
  ▼                  ▼              ▼           ▼                      ▼
Window opens    Grace period    Imminent    Break starts           Break ends
Button enabled  Let line finish (fallback)  Music stops            Resume show
Music starts    Then transition             Ads play
```

#### Grace Period System

When a break is queued, the system uses a **grace period** to ensure dialogue doesn't get cut off mid-sentence:

1. **At T-10 seconds**: `OnBreakGracePeriod` fires
   - If voice audio is playing: wait for the current line to finish naturally
   - If nothing playing: proceed immediately to transition

2. **When the line finishes**: Check remaining time
   - If >= 5 seconds remain: Play Vern's transition line ("We'll be right back...")
   - If < 5 seconds remain: Skip the transition line, let music continue

3. **Transition music behavior**:
   - Starts when player clicks "Queue Ads"
   - Plays continuously under dialogue (Vern's audio cue)
   - Only stops when break actually starts (T-0)

4. **At T-5 seconds**: Fallback only
   - If grace period already handled transition: do nothing
   - Otherwise: interrupt and play transition line

This creates a smooth, professional radio flow where Vern naturally wraps up before going to break.

### Timing Constants

| Constant | Value | Description |
|----------|-------|-------------|
| `BREAK_WINDOW_DURATION` | 20 sec | How long before break time the button enables |
| `BREAK_GRACE_TIME` | 10 sec | When grace period starts (let current line finish) |
| `BREAK_IMMINENT_TIME` | 5 sec | Fallback interrupt point if grace period didn't trigger |
| `ESTIMATED_TRANSITION_LINE_DURATION` | 4 sec | Expected length of Vern's transition line |
| `TRANSITION_TIME_BUFFER` | 1 sec | Safety buffer when checking if transition line fits |
| `UNQUEUED_MOOD_PENALTY` | 15 | Mood penalty if player didn't queue before break |

### Button States

| State | Condition | Button |
|-------|-----------|--------|
| **Waiting** | Before break window | Disabled, shows time until window |
| **Ready** | In break window, not queued | Enabled, "QUEUE ADS" |
| **Queued** | Player clicked queue | Disabled, "QUEUED" (music playing) |
| **Playing** | Break is playing | Disabled, "ON BREAK" |
| **Done** | All breaks used | Disabled, "NO BREAKS" |

### Mood Penalty

If the player **doesn't click "Queue Ads"** before the break time, Vern suffers a mood penalty. This represents him being caught off-guard and cut off mid-sentence.

- Penalty: **15 mood points**
- The penalty applies even if Vern wasn't actively speaking
- Queuing the break = no penalty (Vern had warning)

### Transition Music

When the player clicks "Queue Ads":
- Transition music starts playing in the background
- This is Vern's audio cue that a break is coming
- Gives him time to wrap up his current thought naturally
- Music continues until break starts

## Listener Impact

- Ads cause temporary listener dip (~5-10%)
- Listeners gradually return after break ends
- Too many breaks = cumulative listener fatigue
- Well-timed breaks (between callers) minimize loss

## Implementation

### Files Created

| File | Description |
|------|-------------|
| `Runtime/Ads/AdType.cs` | Enum for ad tiers |
| `Runtime/Ads/AdData.cs` | ScriptableObject defining an ad |
| `Runtime/Ads/AdBreak.cs` | Data class for a scheduled break |
| `Runtime/Ads/AdSchedule.cs` | Player's break configuration for a show |
| `Runtime/Ads/AdManager.cs` | Singleton managing ads |
| `Runtime/Ads/AdConstants.cs` | Timing and config constants |
| `Runtime/UI/AdSchedulePanel.cs` | PreShow break configuration |
| `Runtime/UI/AdBreakPanel.cs` | LiveShow break controls |
| `Editor/AdAssetCreator.cs` | Editor script to create sample ads |

### Files Modified

| File | Changes |
|------|---------|
| `TimeManager.cs` | Default duration 300 -> 600 |
| `PreShowUIManager.cs` | Add AdSchedulePanel |
| `HeaderBarUI.cs` | Add AdBreakPanel |
| `ConversationManager.cs` | Add InterruptForBreak() for break transitions |
| `DialogueJsonData.cs` | Add breakTransitionLines field |
| `VernDialogueTemplate.cs` | Add BreakTransitionLines + GetBreakTransition() |
| `DialogueLoader.cs` | Populate BreakTransitionLines from JSON |
| `AudioManager.cs` | Add SFX types for break jingles |
| `VernDialogue.json` | Add breakTransitionLines array |

### Key Constants

| Constant | Value |
|----------|-------|
| `SHOW_DURATION_SECONDS` | 600 |
| `AD_SLOT_DURATION` | 18 |
| `MAX_BREAKS_PER_SHOW` | 10 |
| `MAX_SLOTS_PER_BREAK` | 3 |
| `BREAK_JINGLE_DURATION` | 5 |
| `RETURN_JINGLE_DURATION` | 3 |
| `BREAK_WINDOW_DURATION` | 20 |
| `BREAK_GRACE_TIME` | 10 |
| `BREAK_IMMINENT_TIME` | 5 |
| `ESTIMATED_TRANSITION_LINE_DURATION` | 4 |
| `TRANSITION_TIME_BUFFER` | 1 |
| `UNQUEUED_MOOD_PENALTY` | 15 |
| `LISTENER_DIP_PERCENTAGE` | 0.05 |

## UI Design

### PreShow: Ad Schedule Panel

Players configure their ad schedule before the show starts. They select:
- Number of breaks (1-4)
- Slots per break (1-3)
- Which ads to include (must select at least breaks × slots)

```
+------------------------------------------+
|         AD BREAK CONFIGURATION           |
+------------------------------------------+
|  Breaks: [<] 3 [>]    Slots: [<] 2 [>]   |
|                                          |
|  Select Ads (min 6):                     |
|  [✓] Big Earl's Auto         Local       |
|  [✓] Pizza Palace            Local       |
|  [ ] TinFoil Plus      (200 listeners)   |
|  [ ] Bigfoot Repellent (200 listeners)   |
|  ...                                     |
|                                          |
|  Est. Revenue: $12 - $18                 |
|  Ad Time: ~2:12                          |
+------------------------------------------+
```

### LiveShow: Ad Break Panel (Header Bar)

Compact controls in the header bar with multiple states:

```
Waiting:   [ BREAK IN 1:32 ]  Breaks: 0/3     (disabled, countdown to window)
Ready:     [ QUEUE ADS ]      Breaks: 0/3     (enabled, in window)
Queued:    [ QUEUED 0:12 ]    Breaks: 0/3     (disabled, music playing)
On Break:  [ ON BREAK ]       Breaks: 1/3     (ads playing)
Done:      [ NO BREAKS ]      Breaks: 3/3     (all used)
```

- **Waiting**: Shows countdown to next break window
- **Ready**: In break window, player can click to queue the break
- **Queued**: Player queued, transition music playing, countdown to break
- **On Break**: Ads are playing
- **Done**: All breaks used

## Thematic Ads (Flavor)

Fun paranormal-themed advertisers for the late-night conspiracy radio theme:

| Name | Type | Tagline |
|------|------|---------|
| Big Earl's Auto | LocalBusiness | "We'll fix your car, no questions asked. Especially about those dents." |
| Pizza Palace | LocalBusiness | "Open 'til 3 AM. We've seen things too." |
| TinFoil Plus | RegionalBrand | "Premium signal-blocking headwear. Now in camo." |
| Bigfoot Repellent | RegionalBrand | "Keep your campsite safe! 97% effective.*" |
| Night Vision Warehouse | RegionalBrand | "See what they don't want you to see. In the dark." |
| Area 51 Tours | NationalSponsor | "See what they DON'T want you to see. Bus leaves at midnight." |
| Ghost-B-Gone | NationalSponsor | "Spectral removal since 1987. Satisfaction mostly guaranteed." |

## Editor Menu Commands

| Command | Description |
|---------|-------------|
| KBTV > Create Sample Ads | Creates 7 sample AdData assets in Assets/Data/Ads/ |
| KBTV > Assign Ads to AdManager | Assigns all ads in Assets/Data/Ads/ to the scene's AdManager |

## Implementation Phases

### Phase 1: Core System (Complete)

- [x] Show duration updated to 10 minutes
- [x] Data models (AdType, AdData, AdBreak, AdSchedule)
- [x] AdManager singleton with break execution
- [x] AdConstants with timing values
- [x] Sample ad assets (10 thematic advertisers via Editor script)
- [x] Scheduled break windows (breaks evenly distributed through show)
- [x] Queue-based break system (player queues to warn Vern)
- [x] Break window system (button enables 20 sec before scheduled time)
- [x] Auto-start breaks at scheduled time
- [x] Mood penalty for unqueued breaks
- [x] Transition music on queue (OnBreakQueued event)
- [x] AdSchedulePanel (PreShow UI with break/slot config)
- [x] AdBreakPanel (LiveShow header with queue button)
- [x] Revenue calculation
- [x] Listener dip during breaks (5%)
- [x] Unlock system (peak listeners)
- [x] Persistence (PeakListenersAllTime in SaveData)
- [x] Ad shuffle (randomize ad order each show for variety)

### Phase 2: Audio (In Progress)

- [x] Ad audio playback in AdManager (PlayBreakCoroutine)
- [x] AudioManager.PlayAdClip() method
- [x] AdData.AudioVariations and GetRandomVariation()
- [x] AdAudioSetup.cs editor script (auto-assign clips)
- [x] Ad script JSON files (7 ads in Tools/AdGeneration/ads/)
- [x] generate_ads.py (Suno jingle workflow)
- [ ] Generate actual audio files (Suno)

### Phase 3: Advanced (Future)

- [ ] Dynamic ad pricing
- [ ] Relationship building with sponsors
- [ ] Exclusive deals
- [ ] Ad quality affects listener retention

## Integration Points

| System | Integration |
|--------|-------------|
| Economy | Ad revenue -> EconomyManager.AddMoney() |
| Time | Breaks consume show time (included in 10 min) |
| Listeners | Ads affect listener count temporarily (5% dip) |
| Conversations | InterruptForBreak() plays transition line |
| VernStats | Mood penalty if Vern cut off mid-sentence |
| Save | Track PeakListenersAllTime for unlock system |
| Audio | Break approaching music, jingles, ad playback |
| Dialogue | Break transition lines in VernDialogueTemplate |

### ConversationManager Integration

The `ConversationManager` subscribes to `AdManager` events to coordinate broadcast state:

**On Break Queued (`OnBreakQueued`):**
- Starts playing transition music
- Vern hears the cue and can wrap up naturally
- Music continues playing until break starts

**On Break Grace Period (`OnBreakGracePeriod`):**
- Fires 10 seconds before break starts (only if break was queued)
- If voice audio is playing: waits for current line to finish naturally
- When line finishes: checks if there's time for Vern's transition line
- If enough time (>= 5s): plays transition line while music continues
- If not enough time: skips transition line, lets music play until break

**On Break Imminent (`OnBreakImminent`):**
- Fires 5 seconds before break starts (only if break was queued)
- **Now a fallback only** - if grace period already handled transition, does nothing
- Otherwise: interrupts current content and plays transition line

**On Break Started (`OnBreakStarted`):**
- Stops transition music
- Stops any active conversation or broadcast line
- Ends current caller if still on air
- Applies mood penalty if break wasn't queued

**On Break Ended (`OnBreakEnded`):**
- Resumes broadcast flow
- If there's a caller waiting, puts them on air
- Otherwise, starts dead air filler with Vern's "back from break" line

```csharp
// ConversationManager.cs
AdManager.Instance.OnBreakQueued += HandleBreakQueued;
AdManager.Instance.OnBreakGracePeriod += HandleBreakGracePeriod;
AdManager.Instance.OnBreakImminent += HandleBreakImminent;
AdManager.Instance.OnBreakStarted += HandleBreakStarted;
AdManager.Instance.OnBreakEnded += HandleBreakEnded;

private void HandleBreakQueued()
{
    PlayTransitionMusic();  // Audio cue for Vern - plays until break starts
}

private void HandleBreakGracePeriod()
{
    _inBreakGracePeriod = true;
    // If voice playing: wait for line to finish, then TryPlayBreakTransition()
    // If not playing: call TryPlayBreakTransition() immediately
}

private void TryPlayBreakTransition()
{
    float remainingTime = AdManager.Instance.QueueCountdown;
    float requiredTime = ESTIMATED_TRANSITION_LINE_DURATION + TRANSITION_TIME_BUFFER;
    
    if (remainingTime >= requiredTime)
    {
        PlayBreakTransitionLine();  // Vern says "We'll be right back..."
    }
    // else: skip transition line, music continues until break
}

private void HandleBreakImminent()
{
    if (_gracePeriodTransitionStarted) return;  // Already handled
    InterruptForBreak();  // Fallback only
}

private void HandleBreakStarted(bool wasQueued)
{
    StopTransitionMusic();  // Music stops here, not before
    StopAllDialogue();
    EndCurrentCaller();
    if (!wasQueued) ApplyMoodPenalty();
}

private void HandleBreakEnded(float revenue)
{
    ResumeNextCaller();
}
```

## Dependencies

- [ECONOMY_SYSTEM.md](ECONOMY_SYSTEM.md) - Revenue integration
- [GAME_DESIGN.md](GAME_DESIGN.md) - Show structure
- [EQUIPMENT_SYSTEM.md](EQUIPMENT_SYSTEM.md) - Equipment doesn't affect ads (yet)

---

*Last updated: Added ad shuffle for variety across shows*
