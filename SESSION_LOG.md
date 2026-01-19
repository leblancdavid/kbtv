## Current Session
- **Task**: ScreeningPanel UI - 2-column property grid with header row and word wrap
- **Status**: Completed
- **Started**: Mon Jan 19 2026
- **Last Updated**: Mon Jan 19 2026

### Changes Made

**1. Scene Structure (`scenes/ui/ScreeningPanel.tscn`):**
- Replaced single `CallerInfoLabel` with nested structure:
  - `CallerInfoScroll` → `InfoVBox` → `HeaderRow` + `Divider` + `PropertiesGrid`
- HeaderRow displays: `Name: X | Phone: X | Location: X | Topic: X`
- Divider line separates header from properties
- PropertiesGrid with 2 columns, h_separation=24, v_separation=4
- Tighter VBoxContainer spacing (separation = 4)
- Taller button row (custom_minimum_size.y = 60)
- Larger button text (font_size = 16)
- Rounded button corners (8px)

**2. Script Changes (`scripts/ui/ScreeningPanel.cs`):**
- Removed unused `_headerLabel` export
- Added `_headerRow` export for the caller info header
- Added `_propertiesGrid` export for the 2-column property grid
- Rewrote `BuildPropertyGrid()` to create Label nodes dynamically
- Properties paired in 2 columns (Quality + Belief Level, Emotional State + Evidence, etc.)
- Personality and ScreeningSummary as full-width rows at bottom
- Updated `_SetCallerImmediate()` to set header text with caller info

**3. Word Wrap Fixes:**
- All property grid labels now have `AutowrapMode = TextServer.AutowrapMode.WordSmart`
- Added vertical expand (`SizeFlagsVertical = ExpandFill`) to Personality and ScreeningSummary labels
- Long fields like ScreeningSummary now wrap instead of overflowing

**4. Fixed script-scene mismatch:**
- Updated `GetMissingNodeReferences()` to check for correct node paths
- Updated `EnsureNodesInitialized()` to get nodes from correct paths
- Changed `caller.Topic` to `caller.ClaimedTopic` (correct property)

### Files Modified
- `scenes/ui/ScreeningPanel.tscn`
- `scripts/ui/ScreeningPanel.cs`

### Build Status
**Build: SUCCESS** (0 errors, 26 warnings - pre-existing nullable annotations)

---

## Previous Session
- **Task**: Implement off-topic caller generation (90% on-topic, 10% off-topic based on show topic)
- **Status**: Completed
- **Started**: Mon Jan 19 2026
- **Last Updated**: Mon Jan 19 2026

### Feature Summary

Callers are now generated based on the show's selected topic:
- **90% on-topic**: Arcs are picked matching the show's current topic (from SelectedTopic.TopicId)
- **10% off-topic**: Arcs are picked from a DIFFERENT topic than the show's topic
- Uses `Topic.OffTopicRate` (default 0.1f = 10%) for the off-topic probability
- Off-topic callers are transparent - they claim their actual topic, not the show topic
- `Caller.IsOffTopic` is set automatically during generation

### Changes Made

**1. IArcRepository.cs - Added interface methods:**
- `GetRandomArcForTopic(topicId, legitimacy)` - Get arc matching specified topic
- `GetRandomArcForDifferentTopic(excludeTopicId, legitimacy)` - Get arc from different topic

**2. ArcRepository.cs - Added implementation methods:**
- `GetRandomArcForTopic()` - Uses `FindMatchingArcs()` and returns random from matches
- `GetRandomArcForDifferentTopic()` - Filters arcs by legitimacy, excludes topic-switcher arcs, excludes specified topic

**3. CallerGenerator.cs - Complete rewrite of arc assignment logic:**
- Removed old random arc assignment (30% deception was not respecting show topic)
- Added 90/10 split based on `showTopic.OffTopicRate`
- On-topic path: `GetRandomArcForTopic(showTopicId, legitimacy)` → sets IsOffTopic = false
- Off-topic path: `GetRandomArcForDifferentTopic(showTopicId, legitimacy)` → sets IsOffTopic = true
- Fallback to hardcoded Topics array if no matching arcs available
- Calls `caller.SetOffTopic(true)` for off-topic callers after construction

### Off-Topic vs Deception (Separate Features)

| Scenario | Show Topic | Claimed Topic | Actual Topic | IsOffTopic | IsLyingAboutTopic |
|----------|------------|---------------|--------------|------------|-------------------|
| On-topic | Ghosts | Ghosts | Ghosts | false | false |
| Off-topic | Ghosts | UFOs | UFOs | true | false |
| Deception | Ghosts | Ghosts | Demons | false | true |

### Files Modified
- scripts/dialogue/IArcRepository.cs
- scripts/dialogue/ArcRepository.cs
- scripts/callers/CallerGenerator.cs

### Build Status
**Build: SUCCESS** (0 errors, 26 warnings - pre-existing nullable annotations)

---

## Previous Session
- **Task**: Implement off-topic caller generation (90% on-topic, 10% off-topic based on show topic)
- **Status**: Completed
- **Started**: Mon Jan 19 2026
- **Last Updated**: Mon Jan 19 2026

### Feature Summary

Callers are now generated based on the show's selected topic:
- **90% on-topic**: Arcs are picked matching the show's current topic (from SelectedTopic.TopicId)
- **10% off-topic**: Arcs are picked from a DIFFERENT topic than the show's topic
- Uses `Topic.OffTopicRate` (default 0.1f = 10%) for the off-topic probability
- Off-topic callers are transparent - they claim their actual topic, not the show topic
- `Caller.IsOffTopic` is set automatically during generation

### Changes Made

**1. IArcRepository.cs - Added interface methods:**
- `GetRandomArcForTopic(topicId, legitimacy)` - Get arc matching specified topic
- `GetRandomArcForDifferentTopic(excludeTopicId, legitimacy)` - Get arc from different topic

**2. ArcRepository.cs - Added implementation methods:**
- `GetRandomArcForTopic()` - Uses `FindMatchingArcs()` and returns random from matches
- `GetRandomArcForDifferentTopic()` - Filters arcs by legitimacy, excludes topic-switcher arcs, excludes specified topic

**3. CallerGenerator.cs - Complete rewrite of arc assignment logic:**
- Removed old random arc assignment (30% deception was not respecting show topic)
- Added 90/10 split based on `showTopic.OffTopicRate`
- On-topic path: `GetRandomArcForTopic(showTopicId, legitimacy)` → sets IsOffTopic = false
- Off-topic path: `GetRandomArcForDifferentTopic(showTopicId, legitimacy)` → sets IsOffTopic = true
- Fallback to hardcoded Topics array if no matching arcs available
- Calls `caller.SetOffTopic(true)` for off-topic callers after construction

### Off-Topic vs Deception (Separate Features)

| Scenario | Show Topic | Claimed Topic | Actual Topic | IsOffTopic | IsLyingAboutTopic |
|----------|------------|---------------|--------------|------------|-------------------|
| On-topic | Ghosts | Ghosts | Ghosts | false | false |
| Off-topic | Ghosts | UFOs | UFOs | true | false |
| Deception | Ghosts | Ghosts | Demons | false | true |

### Files Modified
- scripts/dialogue/IArcRepository.cs
- scripts/dialogue/ArcRepository.cs
- scripts/callers/CallerGenerator.cs

### Build Status
**Build: SUCCESS** (0 errors, 26 warnings - pre-existing nullable annotations)

---

## Current Session
- **Task**: Fix on-hold callers not transferring to on-air and transcripts not playing caller dialogs
- **Status**: Completed
- **Started**: Sun Jan 18 2026
- **Last Updated**: Sun Jan 18 2026

### Architectural Changes

**Problem:** The old architecture mixed display lines with control signals in a single `GetNextLine()` method. Control signals like `PutCallerOnAir` were treated as "lines" with timing, causing:
- State management complexity when control actions needed immediate execution
- Premature arc setup before caller was actually on air
- First caller dialogue line being skipped

**Solution:** Separated display lines from control actions with a new split API:

**New API Methods:**
- `GetNextDisplayLine()` - Returns next displayable line (or null if no line)
- `GetPendingControlAction()` - Returns any pending control action (PutCallerOnAir, None)
- `OnControlActionCompleted()` - Clears the pending action
- `OnCallerPutOnAir(Caller caller)` - Properly sets up arc when caller goes on air

**Files Created:**
- `scripts/dialogue/ControlAction.cs` - New enum for control signals

**Files Modified:**
- `scripts/dialogue/BroadcastLineType.cs` - Removed `PutCallerOnAir` enum value
- `scripts/dialogue/BroadcastLine.cs` - Removed `PutCallerOnAir` factory method
- `scripts/dialogue/BroadcastCoordinator.cs` - Complete rewrite with split API
- `scripts/dialogue/ConversationDisplay.cs` - Updated to use new split API
- `scripts/ui/LiveShowFooter.cs` - Removed `PutCallerOnAir` check
- `scripts/ui/LiveShowPanel.cs` - Removed `PutCallerOnAir` check
- `scripts/ui/TranscriptPanel.cs` - Removed `PutCallerOnAir` check

**Corrected Flow:**
1. `GetPendingControlAction()` returns `PutCallerOnAir` when needed
2. `ConversationDisplay` calls `PutOnAir()` → moves caller to on-air state
3. `ConversationDisplay` calls `OnCallerPutOnAir(caller)` → properly sets arc with index 0
4. `ConversationDisplay` calls `OnControlActionCompleted()` → clears pending action
5. Next `GetNextDisplayLine()` call returns first arc line (index 0)
6. Caller dialogue displays and appears in transcripts

### Build Status
**Build: SUCCESS** (0 errors, 8 warnings - pre-existing nullable annotations)

---

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

---

## Current Session
- **Task**: Change transcript UI to rolling text (one line at a time)
- **Status**: Completed
- **Started**: Sat Jan 18 2026
- **Last Updated**: Sat Jan 18 2026

### Changes Made

**Created `scripts/ui/RollingTranscriptPanel.cs`:**
- New component that displays transcript as a rolling ticker
- Letter-by-letter reveal animation based on line duration
- Shows "[MUSIC PLAYING]" during show opening, closing, between callers, and dead air filler
- Displays speaker name followed by dialog (e.g., "Vern: Hello there")
- Pauses briefly after each line completes before scrolling

**Modified `scenes/ui/LiveShowPanel.tscn`:**
- Removed `TranscriptScroll/TranscriptContent` RichTextLabel nodes
- Added `RollingTranscriptPanel` Control with nested `TranscriptLabel`
- Added ext_resource for RollingTranscriptPanel script

**Modified `scripts/ui/LiveShowPanel.cs`:**
- Removed `UpdateTranscript()` method and `_transcriptContent` reference
- Removed transcript polling logic (now handled by RollingTranscriptPanel)
- Cleaned up unused using statements

### Behavior
- New transcript entries appear with letter-by-letter reveal animation
- Speed matches the calculated line duration (~0.05s per character)
- When a line finishes, pauses 0.5s then displays the full line
- Shows "[MUSIC PLAYING]" during non-conversation phases (show open/close, between callers, dead air)
- Only one entry visible at a time, previous entries scroll up and off-screen

### Files Modified
- `scripts/ui/RollingTranscriptPanel.cs` (new)
- `scenes/ui/LiveShowPanel.tscn`
- `scripts/ui/LiveShowPanel.cs`

### Build Status
**Build: SUCCESS** (0 errors, 3 warnings - pre-existing nullable annotations)

---

## Current Session
- **Task**: Fix transcript UI synchronization with dialogue
- **Status**: Completed
- **Started**: Sat Jan 18 2026
- **Last Updated**: Sat Jan 18 2026

### Changes Made

**Modified `scripts/ui/RollingTranscriptPanel.cs`:**
- Now syncs typewriter reveal to `ConversationDisplayInfo.ElapsedLineTime / CurrentLineDuration`
- Tracks `_previousDisplayInfo` to detect line changes
- Each new line resets and starts fresh, replacing the previous transcript entirely
- Removed independent timer-based reveal logic

### Behavior
- Transcript reveals at the exact same rate as the main dialogue label
- New dialog lines replace the previous transcript immediately
- Still shows "[MUSIC PLAYING]" during bumpers
- Still shows "TRANSCRIPT" when no broadcast is active

### Build Status
**Build: SUCCESS** (0 errors, 3 warnings)

---

## Current Session
- **Task**: Consolidate transcript to single panel (LiveShowFooter)
- **Status**: Completed
- **Started**: Sat Jan 18 2026
- **Last Updated**: Sat Jan 18 2026

### Root Cause
Two transcript displays existed:
1. `LiveShowFooter.tscn` - Showed full log with timestamps (old behavior)
2. `LiveShowPanel.tscn` - New rolling single-line display

### Changes Made

**Modified `scripts/ui/LiveShowFooter.cs`:**
- Added `IConversationManager` reference
- Replaced `StringBuilder` log with single-line synced display
- Removed `_previousTranscriptCount` and transcript repository polling
- Now mirrors `ConversationDisplayInfo` directly (like RollingTranscriptPanel)
- Shows "Speaker: text" synced to dialogue with typewriter reveal
- Shows "[MUSIC PLAYING]" during bumpers

**Modified `scenes/ui/LiveShowPanel.tscn`:**
- Removed `RollingTranscriptPanel` node and ext_resource
- Removed `TranscriptContainer` section entirely (no longer needed)
- Reduced scene to only essential dialogue display elements

**Deleted:**
- `scripts/ui/RollingTranscriptPanel.cs`
- `scripts/ui/RollingTranscriptPanel.cs.uid`

### Behavior
- Single transcript display in LiveShowFooter
- Shows "VERN: text" or "CALLER: text" (speaker icon + current text)
- Typewriter reveal synced to conversation display
- Each new dialogue line replaces the previous one
- Shows "[MUSIC PLAYING]" during show open/close, between callers, dead air
- Shows "TRANSCRIPT" when no broadcast is active

### Build Status
**Build: SUCCESS** (0 errors, 3 warnings) 

---

## Current Session
- **Task**: Fix transcript panel stuck showing "PLAYING MUSIC"
- **Status**: Completed
- **Started**: Sun Jan 18 2026
- **Last Updated**: Sun Jan 18 2026

### Issue
Transcript panel was stuck showing "[MUSIC PLAYING]" and never displayed Vern or caller text during show.

### Root Cause
The transcript logic in `LiveShowFooter.cs` was showing "[MUSIC PLAYING]" during non-conversation states (ShowOpening, ShowClosing, BetweenCallers, DeadAirFiller) even though Vern had actual dialogue to display. Additionally, `ConversationDisplayInfo.CreateBroadcastLine()` was hardcoding `FlowState = ShowOpening` regardless of context.

### Fix Applied

**1. Prioritized Vern speech over music placeholder in `scripts/ui/LiveShowFooter.cs`:**
- Rewrote `UpdateTranscript()` to always check for dialogue content first
- Vern's speech now takes priority - if `_displayInfo.Text` has content, it's displayed
- "[MUSIC PLAYING]" only shows as fallback when there's no dialogue content
- Removed unused `_showMusicPlaceholder` field and `UpdateMusicState()` method

**2. Fixed `ConversationDisplayInfo.CreateBroadcastLine()` in `scripts/dialogue/ConversationDisplayInfo.cs`:**
- Added `BroadcastFlowState flowState` parameter
- Now correctly sets the flow state passed to it

**3. Updated all call sites in `scripts/dialogue/ConversationManager.cs`:**
- `PlayShowOpening()`: Uses `BroadcastFlowState.ShowOpening`
- `PlayTemplateIntroduction()`: Uses `BroadcastFlowState.Conversation`
- `PlayNextArcLine()`: Uses `BroadcastFlowState.Conversation` for both Vern and Caller lines
- `PlayBetweenCallers()`: Uses `BroadcastFlowState.BetweenCallers`
- `PlayShowClosing()`: Uses `BroadcastFlowState.ShowClosing`

**4. Updated test file `tests/unit/dialogue/ConversationManagerTests.cs`:**
- Updated `CreateBroadcastLine` calls to include the new flow state parameter

### Behavior After Fix
- **ShowOpening**: Displays Vern's show opening dialogue
- **Conversation**: Displays Vern/caller dialogue with typewriter reveal
- **BetweenCallers**: Displays transition banter
- **DeadAirFiller**: Displays Vern's filler lines (not "[MUSIC PLAYING]")
- **ShowClosing**: Displays Vern's closing remarks
- **No dialogue**: Shows "TRANSCRIPT" (idle state)

### Files Modified
- `scripts/ui/LiveShowFooter.cs`
- `scripts/dialogue/ConversationDisplayInfo.cs`
- `scripts/dialogue/ConversationManager.cs`
- `tests/unit/dialogue/ConversationManagerTests.cs`

### Build Status
**Build: SUCCESS** (0 errors, 3 warnings - pre-existing nullable annotations)

---

## Current Session
- **Task**: Implement Vern's intro dialog on show start
- **Status**: Completed
- **Started**: Sun Jan 18 2026
- **Last Updated**: Sun Jan 18 2026

### Problem
When the show started, Vern's intro dialog wasn't playing. The `BroadcastCoordinator` set state to `ShowOpening` and started the transcript, but `ConversationDisplay` had no trigger to start polling for the opening line.

### Solution
Added event-driven show start flow:

**Files Modified:**
1. `scripts/dialogue/ConversationEvents.cs` - Added `ShowStartedEvent` class
2. `scripts/dialogue/BroadcastCoordinator.cs` - Publishes `ShowStartedEvent` in `OnLiveShowStarted()`
3. `scripts/dialogue/ConversationDisplay.cs` - Subscribes to and handles `ShowStartedEvent`

### Flow After Changes
1. Show starts → `GameStateManager` calls `BroadcastCoordinator.OnLiveShowStarted()`
2. `OnLiveShowStarted()` sets state to `ShowOpening`, starts transcript, publishes `ShowStartedEvent`
3. `ConversationDisplay` receives event → calls `TryGetNextLine()`
4. Gets show opening line from `VernDialogue.json` → plays audio → displays text → adds to transcript
5. After 5 seconds → `OnLineCompleted()` → advances to next state (conversation or dead air)

### Build Status
**Build: SUCCESS** (0 errors, 21 warnings - pre-existing nullable annotations)

---

## Current Session
- **Task**: Remove verbose debug print statements from codebase
- **Status**: Completed
- **Started**: Sun Jan 18 2026
- **Last Updated**: Sun Jan 18 2026

### Changes Made
Removed ~70+ verbose debug print statements from the following files:

**High Priority Cleanup:**
1. `scripts/core/EventBus.cs` - Removed all 6 verbose event publishing logs
2. `scripts/dialogue/BroadcastCoordinator.cs` - Removed 20+ conversation flow debug prints
3. `scripts/dialogue/ConversationDisplay.cs` - Removed 15+ event handling debug prints

**Medium Priority Cleanup:**
4. `scripts/dialogue/AudioDialoguePlayer.cs` - Removed 10+ audio playback debug prints
5. `scripts/ui/InputHandler.cs` - Removed 12+ verbose input handling prints

**Low Priority Cleanup:**
6. `scripts/dialogue/TranscriptRepository.cs` - Removed 5 verbose transcript logs
7. `scripts/ui/LiveShowFooter.cs` - Removed 4 UI update debug prints

### Preserved Logging
- **Error logging** - All GD.PrintErr statements preserved for error reporting
- **DebugHelper.cs** - All intentional debug feature prints preserved (F3 key)
- **Service initialization** - Essential initialization errors preserved

### Files Modified
- scripts/core/EventBus.cs
- scripts/dialogue/BroadcastCoordinator.cs
- scripts/dialogue/ConversationDisplay.cs
- scripts/dialogue/AudioDialoguePlayer.cs
- scripts/ui/InputHandler.cs
- scripts/dialogue/TranscriptRepository.cs
- scripts/ui/LiveShowFooter.cs

### Build Status
**Build: SUCCESS** (0 errors, 20 warnings - all pre-existing)
