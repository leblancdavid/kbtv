## Ad Break Sequence Implementation - COMPLETED ✅

**Problem**: Ad breaks were only playing 1 ad instead of the full 6-ad sequence during commercial breaks.

**Solution**: Implemented AdBreakSequenceExecutable to coordinate sequential playback of all 6 ads.

#### Key Features Delivered

**1. AdBreakSequenceExecutable Class:**
- Created new executable that manages sequential ad playback
- Plays all 6 ads in order with proper timing
- Each ad uses actual audio duration or 4-second fallback
- Auto-advances between ads without user intervention
- Displays "AD (1)", "AD (2)", etc. in LiveShowPanel UI

**2. BroadcastStateManager Integration:**
- Added `GetNextExecutable()` method to return executables based on current state
- AdBreak state now returns `AdBreakSequenceExecutable` instead of single ad
- After sequence completes, transitions to BreakReturn state
- Clean separation between state management and executable creation

**3. UI Integration:**
- LiveShowPanel displays individual ad numbers during sequence
- `HandleAdItemStarted` event handler shows "Commercial Break X" for each ad
- Proper event-driven UI updates for each ad in the sequence

**4. Ad Selection Logic:**
- Uses `AdExecutable.CreateForListenerCount()` for each ad
- Ad types vary based on listener count (Local Business → Premium Sponsor)
- Audio paths: `res://assets/audio/ads/{adType}_{adIndex}.mp3`

#### Expected Ad Break Flow
```
T5 Interrupt → Vern Break Transition → T0 Wait → 
AdBreakSequenceExecutable → 
  Ad (1) - plays audio/duration → 
  Ad (2) - plays audio/duration → 
  ... → 
  Ad (6) - plays audio/duration → 
Break Return Music → Conversation Continues
```

#### Files Modified
- **`scripts/dialogue/executables/AdBreakSequenceExecutable.cs`** - New executable for ad sequence coordination
- **`scripts/dialogue/BroadcastStateManager.cs`** - Added GetNextExecutable method, integrated sequence executable
- **`scripts/ui/LiveShowPanel.cs`** - Enhanced ad display for individual ads in sequence

#### Result
✅ **Complete Ad Break Sequence** - All 6 ads now play sequentially during commercial breaks with proper UI display and auto-advancement.

**Latest Commit**: `feature/fix-dependency-injection` branch ready for testing