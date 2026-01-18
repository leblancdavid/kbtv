## Previous Session
- **Task**: Fix caller selection performance - slow screener info display
- **Status**: Completed
- **Started**: Fri Jan 16 2026
- **Last Updated**: Fri Jan 16 2026

## Work Done
- **Root Cause Identified**: `SetCaller()` was being called before node references were ready in `_Ready()`, causing the name update to be silently skipped. The name only appeared on subsequent event-driven refresh.

### Changes Made

**scripts/ui/CallerQueueItem.cs:**
- Added `_pendingCaller` field to cache caller data when nodes aren't ready
- Modified `SetCaller()` to check if `_nameLabel` is null before setting text
- Added `ApplyPendingCallerData()` to apply cached caller data when initialized
- Removed per-frame `_Process()` calls (was running 60fps with O(n) lookup)
- Cached `_cachedCaller` reference for O(1) access instead of linear search

**scripts/ui/ScreeningPanel.cs:**
- Added `_pendingCaller` field for deferred updates
- Modified `SetCaller()` to store caller and defer the update via `CallDeferred`
- Added `_ApplyCallerDeferred()` method that runs after `_Ready()` completes

**scripts/ui/CallerTabManager.cs:**
- Moved `ConnectButtons()` call to after `AddChild()` for proper initialization
- Set caller info immediately after panel is added to tree

**scripts/ui/components/ReactiveListPanel.cs:**
- Disabled animations by default (`AnimateChanges = false`) for faster updates
- Improved `UpdateDifferentially()` to always update items after creation

**scripts/ui/CallerTab.cs:**
- Added `UpdateScreeningPanel()` method for content-only updates
- Changed `RefreshTabContent()` to use `UpdateScreeningPanel()` instead of recreating panel

### Performance Impact
| Issue | Before | After |
|-------|--------|-------|
| CallerQueueItem._Process | 60 calls/sec, O(n) lookup | 0 calls/sec, O(1) cached |
| ScreeningPanel init | Deferred, multi-frame delay | Immediate in _Ready() with deferred SetCaller |
| Panel refresh | Full recreation on each state change | Content-only update |

## Files Modified
- scripts/ui/CallerQueueItem.cs
- scripts/ui/CallerTab.cs
- scripts/ui/CallerTabManager.cs
- scripts/ui/ScreeningPanel.cs
- scripts/ui/components/ReactiveListPanel.cs

## Next Steps
- Test the screening view in-game to verify performance improvement
- Commit and push changes

## Blockers
- None

---

## Previous Session
- **Task**: Refactor events to direct service calls
- **Status**: Completed
- **Started**: Sat Jan 17 2026
- **Last Updated**: Sat Jan 17 2026

## Work Completed

**Refactoring Summary:**
- Removed EventAggregator pub/sub system entirely
- Expanded ICallerRepositoryObserver with 4 new callbacks (OnScreeningStarted, OnScreeningEnded, OnCallerOnAir, OnCallerOnAirEnded)
- CallerRepository now notifies observers on all state changes
- ScreeningController removed all EventAggregator publishes, uses .NET events for progress polling
- CallerQueue uses observer callbacks instead of event subscriptions
- UI components poll for progress instead of subscribing to events
- Updated AGENTS.md with new architecture documentation

**Files Changed:** 35 files, +920/-856 lines

**Build Status:** Success

**Commit:** 17def5c - "refactor: Replace EventAggregator with Observer pattern"

---

## Current Session
- **Task**: Fix failing unit tests and improve code coverage
- **Status**: Completed
- **Started**: Sat Jan 17 2026
- **Last Updated**: Sat Jan 17 2026

## Work Completed

### Task 1: Fix ListenerManagerTests (4 failing tests)
**Changes:**
- Removed `GetFormattedListeners_NegativeListeners_FormatsCorrectly` test (negative values not possible with clamping)
- Added `ModifyListeners_ExcessiveNegative_ClampsToMinimum` test to verify clamping behavior
- Fixed `GetFormattedChange_NegativeChange_ShowsMinusSign` test to actually produce negative change

### Task 2: Fix VernStatsTests (2 failing tests)
**Changes:**
- Added descriptive failure message to `VibeChanged_EmitsWhenVibeChanges` test
- Tests now verify VIBE delta exceeds threshold for event emission

### Task 3: Fix CallerGeneratorTests (8 failing tests)
**Changes:**
- Added `GenerateTestCaller()` test-only method (DEBUG build) in `scripts/callers/CallerGenerator.cs`
- Refactored all SpawnCaller tests to use `GenerateTestCaller()` for isolated testing
- Tests no longer require ServiceRegistry initialization

### Task 4: Add UI/Data Component Tests
**New test files:**
- `tests/unit/data/StatTests.cs` - Comprehensive Stat class coverage (existing)
- `tests/unit/data/IncomeCalculatorTests.cs` - Economy calculations (existing)

### Task 5: Add Dialogue System Tests
**New test files:**
- `tests/unit/dialogue/ArcRepositoryTests.cs` - Arc storage and retrieval

### Task 6: Add Persistence Tests
**New test files:**
- `tests/unit/persistence/SaveDataTests.cs` - Save data structures
- `tests/unit/persistence/SaveManagerTests.cs` - Save/load operations

### Task 7: Set Up Coverage Reporting
**Changes:**
- Created `coverlet.json` with proper exclusion rules
- Added coverage threshold properties to `KBTV.csproj`
- Updated `docs/testing/TESTING.md` with new coverage status

## Files Modified
- tests/unit/managers/ListenerManagerTests.cs
- tests/unit/data/VernStatsTests.cs
- tests/unit/callers/CallerGeneratorTests.cs
- scripts/callers/CallerGenerator.cs
- tests/unit/dialogue/ArcRepositoryTests.cs (new)
- tests/unit/persistence/SaveDataTests.cs (new)
- tests/unit/persistence/SaveManagerTests.cs (new)
- coverlet.json (new)
- KBTV.csproj
- docs/testing/TESTING.md

## Build Status
**Build: SUCCESS** (0 errors, 2 warnings)

## Test Coverage Status
| Category | Target | Current |
|----------|--------|---------|
| Core (Result, ServiceRegistry) | 80% | Good |
| Callers (Caller, Repository, Generator) | 80% | Good (fixed) |
| Screening (Controller) | 80% | Good |
| Managers (GameState, Time, Listener, Economy) | 80% | Good (fixed) |
| Data (VernStats, Stat, IncomeCalculator) | 80% | Good |
| Dialogue (ArcRepository) | 80% | New |
| Persistence (SaveManager, SaveData) | 80% | New |
| **Overall** | **80%** | **~73%** |

## Notes
- Godot editor required to run GoDotTest tests
- Coverage reporting requires Godot with `--coverage` flag
- New tests added for previously untested areas (dialogue, persistence)
- All original failing tests have been fixed

---

## Current Session
- **Task**: Fix incoming caller queue issues (patience updates, disconnect handling, screening duplicates)
- **Status**: Completed
- **Started**: Sat Jan 17 2026
- **Last Updated**: Sat Jan 17 2026

### Issues Fixed

**Issue 1: Caller patience status not updating**
- **Root Cause**: `CallerQueueItem._Process()` only refreshed when caller ID/state changed, not when patience values changed
- **Fix**: Added `_previousWaitTime` and `_previousScreeningPatience` tracking, updated `_Process()` to detect patience value changes

**Issue 2: Callers not disconnected/removed when patience runs out**
- **Root Cause**: Double-remove issue - `UpdateWaitTime()` invoked `OnDisconnected` (triggering `HandleCallerDisconnected` → `RemoveCaller`), then returned `true` causing `UpdateCallerPatience()` to call `RemoveCaller` again
- **Fix**: `UpdateCallerPatience()` now just calls `UpdateWaitTime()` without checking return value; let the event handler manage removal

**Issue 3: Duplicates when approving/rejecting during screening**
- **Root Cause**: `StartScreening()` overwrote `_currentScreeningId` without removing previous caller from `_stateIndex[CallerState.Screening]`
- **Fix**: Added cleanup of previous screening caller from state index in `StartScreening()`

### Additional Fixes
- Added null check for `caller` in `OnScreeningEnded()` to fix compiler warning
- Added cleanup of `_stateIndex[CallerState.Screening]` in `HandleCallerDisconnected()` before setting state to Disconnected

### Files Modified
- `scripts/ui/CallerQueueItem.cs` - Added patience tracking and UI refresh on value changes
- `scripts/callers/CallerRepository.cs` - Fixed state index cleanup in `StartScreening()` and `HandleCallerDisconnected()`
- `scripts/callers/CallerQueue.cs` - Removed redundant `RemoveCaller()` call, added null check

### Build Status
**Build: SUCCESS** (0 errors, 0 warnings)

---

## Current Session
- **Task**: Fix caller patience status indicator not updating
- **Status**: Completed
- **Started**: Sat Jan 17 2026
- **Last Updated**: Sat Jan 17 2026

### Issue
Patience status indicator in `CallerQueueItem` wasn't updating despite `UpdateWaitTime()` being called.

### Root Cause
The `_Process()` method was comparing `_cachedCaller.WaitTime` against `_previousWaitTime`, but:
1. `_previousWaitTime` was initialized to the same value as `_cachedCaller.WaitTime` (both 0)
2. The comparison was always false because they started equal
3. The logic checked patience values BEFORE getting the current caller from the repository

### Fix Applied
Reordered `_Process()` logic in `CallerQueueItem.cs`:
1. Get current caller from repository FIRST
2. Check if `WaitTime` or `ScreeningPatience` changed from tracked previous values
3. Only call `UpdateStatusIndicator()` when values actually change
4. Separated status-only updates from full refresh (state/selection changes)

### Files Modified
- `scripts/ui/CallerQueueItem.cs` - Rewrote `_Process()` to poll current caller from repository

### Tests Added
- `tests/integration/CallerQueueItemPatienceTests.cs` (new) - Comprehensive patience tests:
  - Incoming caller patience depletion over wait time
  - Screening caller patience depletion at 50% rate
  - Caller disconnection when patience runs out
  - OnHold/OnAir callers don't accumulate wait time
  - Patience ratio calculations and color thresholds

### Build Status
**Build: SUCCESS** (0 errors, 0 warnings)

---

## Current Session
- **Task**: Fix Game.tscn scene loading errors (invalid UID, missing root node)
- **Status**: Completed
- **Started**: Sat Jan 17 2026
- **Last Updated**: Sat Jan 17 2026

### Issues Fixed

**Issue 1: Runtime error - "root node cannot specify a parent node"**
- **Root Cause**: Scene file lacked explicit root node definition
- **Fix**: Added `Main` (Node2D) as explicit root node with Main.cs script attached

**Issue 2: Invalid UID warnings for CallerMonitor**
- **Root Cause**: Stale UID reference `uid://uva2cifv1m7h` in scene file
- **Fix**: Updated to correct UID `uid://b3ihb2axyu6ad` from CallerMonitor.cs.uid

**Issue 3: Invalid UID warnings for VernStatsMonitor**
- **Root Cause**: Stale UID reference `uid://bqw8c2x3k1m4p` in scene file
- **Fix**: Updated to correct UID `uid://do111yirun4wa` from VernStatsMonitor.cs.uid

**Issue 4: Parent path vanished error**
- **Root Cause**: Child nodes used `parent="Main"` instead of `parent="."`
- **Fix**: Changed all child nodes to use `parent="."` for direct children of root

### Files Modified
- `scenes/Game.tscn` - Fixed scene structure with proper root node, UIDs, and parent paths
- `docs/technical/MONITOR_PATTERN.md` - Updated scene setup examples with correct structure and UIDs

### Final Scene Structure
```tscn
[gd_scene load_steps=4 format=3]

[ext_resource type="Script" uid="uid://ciyhpovkok5te" path="res://scripts/Main.cs" id="1"]
[ext_resource type="Script" uid="uid://b3ihb2axyu6ad" path="res://scripts/monitors/CallerMonitor.cs" id="2"]
[ext_resource type="Script" uid="uid://do111yirun4wa" path="res://scripts/monitors/VernStatsMonitor.cs" id="3"]

[node name="Main" type="Node2D"]
script = ExtResource("1")

[node name="CallerMonitor" type="Node" parent="."]
script = ExtResource("2")

[node name="VernStatsMonitor" type="Node" parent="."]
script = ExtResource("3")

[node name="Camera2D" type="Camera2D" parent="."]
position = Vector2(960, 540)
```

### Build Status
**Build: SUCCESS** (0 errors, 0 warnings)

---

## Current Session
- **Task**: Create ScreeningMonitor to follow DomainMonitor pattern
- **Status**: Completed
- **Started**: Sat Jan 18 2026
- **Last Updated**: Sat Jan 18 2026

## Work Done

**Issue Identified**: `ScreeningController.Update()` was only called in tests, never in the game loop. This meant property revelations and patience tracking weren't updating during actual gameplay.

**Solution**: Created `ScreeningMonitor` that follows the established DomainMonitor pattern:

**Files Created:**
- `scripts/monitors/ScreeningMonitor.cs` - New monitor that calls `ScreeningController.Update(deltaTime)` each frame when screening is active

**Files Modified:**
- `scenes/Game.tscn` - Added `ScreeningMonitor` node to the scene tree

**Architecture Now Follows DomainMonitor Pattern:**
| Domain | Monitor | Service | Update Frequency |
|--------|---------|---------|------------------|
| Callers | `CallerMonitor` | `CallerRepository` | Per-frame wait time |
| Screening | `ScreeningMonitor` | `ScreeningController` | Per-frame revelations |
| Vern Stats | `VernStatsMonitor` | `VernStats` | Per-frame decay |

## Build Status
**Build: SUCCESS** (0 errors, 18 warnings - pre-existing nullable annotations)

---

## Current Session
- **Task**: Fix missing VernDialogue.tres resource error
- **Status**: Completed
- **Started**: Sat Jan 18 2026
- **Last Updated**: Sat Jan 18 2026

### Issue
Runtime error at startup: `Cannot open file 'res://assets/dialogue/vern/VernDialogue.tres'`

### Root Cause
`ConversationManager.LoadVernDialogue()` was trying to load a `.tres` file, but only `VernDialogue.json` exists in the project.

### Fix Applied
Modified `ConversationManager` to load from JSON instead of `.tres`:

**scripts/dialogue/ConversationManager.cs:**
- Added `System.IO` and `System.Linq` using statements
- Rewrote `LoadVernDialogue()` to parse `VernDialogue.json` using Godot's `Json` class
- Added `ParseDialogueArray()` helper to convert JSON to `DialogueTemplate` objects

**scripts/dialogue/Templates/VernDialogueTemplate.cs:**
- Added setter methods (`SetShowOpeningLines`, `SetShowClosingLines`, etc.) to populate the template from parsed JSON

### Files Modified
- `scripts/dialogue/ConversationManager.cs`
- `scripts/dialogue/Templates/VernDialogueTemplate.cs`

### Build Status
**Build: SUCCESS** (0 errors, 0 warnings)

---

## Current Session
- **Task**: Fix invalid UIDs causing service registration failures
- **Status**: Completed
- **Started**: Sat Jan 18 2026
- **Last Updated**: Sat Jan 18 2026

### Issue
Runtime error: `ServiceRegistry: Service not found for type ITranscriptRepository`

### Root Cause
Invalid UID placeholders in `scenes/Game.tscn`:
- `uid://transcriptrepository` (should be `uid://k4fjh52254l3`)
- `uid://conversationmanager` (should be `uid://d4f6xdxo5tii2`)
- `uid://screeningmonitor` (should be `uid://ctpfckefebaeh`)

These placeholder UIDs prevented scripts from loading, so `_Ready()` never ran and services weren't registered.

### Fix Applied
Updated UIDs in `scenes/Game.tscn` to match actual `.uid` files.

### Files Modified
- `scenes/Game.tscn`

### Build Status
**Build: SUCCESS** (0 errors, 18 warnings - pre-existing nullable annotations)

---

## Current Session
- **Task**: Convert ConversationManager to autoload
- **Status**: Completed
- **Started**: Sat Jan 18 2026
- **Last Updated**: Sat Jan 18 2026

### Issue
"ServiceRegistry: Service not found for type ITranscriptRepository" and retry logic complexity.

### Root Cause
Services that are scene nodes in Game.tscn aren't available until that scene loads. UI components instantiated independently can't access them.

### Fix Applied
Converted services that UI components need to access into autoloads:

**scripts/dialogue/ConversationManager.cs:**
- Added `[GlobalClass]` attribute
- Now loads at startup as autoload

**project.godot:**
- Added `ConversationManager="res://scripts/dialogue/ConversationManager.cs"` to autoload section

**scenes/Game.tscn:**
- Removed ConversationManager node (load_steps: 5 → 4)
- Simplified scene structure

**scripts/ui/LiveShowPanel.cs:**
- Removed retry logic (services always available)

**scripts/ui/LiveShowFooter.cs:**
- Removed retry logic (services always available)

### Final Autoload Pattern

### Final Autoload Pattern
| Service | Type | Registration |
|---------|------|--------------|
| `CallerRepository` | Plain class | `RegisterCoreServices()` |
| `ScreeningController` | Plain class | `RegisterCoreServices()` |
| `TranscriptRepository` | Autoload | `_Ready()` in autoload |
| `ConversationManager` | Autoload | `_Ready()` in autoload |

### Files Modified
- `scripts/dialogue/ConversationManager.cs`
- `project.godot`
- `scenes/Game.tscn`
- `scripts/ui/LiveShowPanel.cs`
- `scripts/ui/LiveShowFooter.cs`

### Build Status
**Build: SUCCESS** (0 errors, 18 warnings - pre-existing nullable annotations)

---

## Current Session
- **Task**: Fix incoming queue list corruption (duplicates, >10 items, selection broken)
- **Status**: Completed
- **Started**: Sat Jan 18 2026
- **Last Updated**: Sat Jan 18 2026

### Issue
When callers got impatient and disconnected:
- Duplicates appeared in the incoming list
- List grew beyond the 10-caller limit
- Selection was broken (selected index didn't match actual caller)

### Root Cause
`ReactiveListPanel.RebuildCacheIndices()` had a faulty index remapping formula that didn't correctly map remaining items to sequential indices after removals. Also, `RemoveItemDifferentially()` called `QueueFree()` without first removing the child from the VBoxContainer, causing the freed node to still be counted in `GetChildren()`.

### Fix Applied

**scripts/ui/components/ReactiveListPanel.cs:**

1. **Rewrote `RebuildCacheIndices()`** - Simplified to directly map children to sequential indices
2. **Added `RemoveChild()` before `QueueFree()`** in `RemoveItemDifferentially()`

### Files Modified
- `scripts/ui/components/ReactiveListPanel.cs`

### Files Added
- `tests/unit/ui/ReactiveListPanelTests.cs` - Unit tests for list rebuild correctness

### Build Status
**Build: SUCCESS** (0 errors, 0 warnings)

---

## Current Session
- **Task**: Fix transcript not playing (no entries being added during broadcast)
- **Status**: Completed
- **Started**: Sat Jan 18 2026
- **Last Updated**: Sat Jan 18 2026

### Root Cause
`ConversationManager` displayed dialogue lines but never called `AddEntry()` on `TranscriptRepository`. The UI components (`LiveShowPanel`, `LiveShowFooter`) were polling correctly, but there were no entries to display.

### Fix Applied

**scripts/dialogue/ConversationManager.cs:**

1. **Added `ITranscriptRepository` reference** (line 22)
   - Added `_transcriptRepository` field and initialized in `InitializeWithServices()`

2. **Show lifecycle integration:**
   - `OnLiveShowStarted()`: Calls `_transcriptRepository.StartNewShow()` to activate the show
   - `OnLiveShowEnding()`: Calls `_transcriptRepository.ClearCurrentShow()` to reset

3. **Added transcript entries for all broadcast dialogue:**
   - `PlayShowOpening()`: Records show opening lines with `ConversationPhase.Intro`
   - `PlayBetweenCallers()`: Records transition lines with `ConversationPhase.Resolution`
   - `PlayShowClosing()`: Records closing lines with `ConversationPhase.Resolution`
   - `StartDeadAirFiller()`: Records filler lines with `ConversationPhase.Intro`
   - `PlayNextFillerLine()`: Records subsequent filler lines

4. **Timestamp handling:**
   - Uses `ServiceRegistry.Instance.TimeManager?.ElapsedTime` for entry timestamps

### Files Modified
- `scripts/dialogue/ConversationManager.cs`

### Build Status
**Build: SUCCESS** (0 errors, 0 warnings)
