## Current Session
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
