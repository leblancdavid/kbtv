# Tab System Specification

## Overview
The tab system provides navigation between different information views during live broadcasts. It allows players to switch between caller queue, inventory items, and broadcast statistics without leaving the main UI.

## Purpose & Functionality
- **Content Organization**: Groups related information into logical categories
- **Space Efficiency**: Allows multiple data sets in limited screen space
- **User Control**: Gives players choice over what information to prioritize
- **Visual Feedback**: Clear indication of active tab and available content

## Layout & Positioning
- **Position**: Below header panel, full screen width
- **Height**: 40-50 pixels
- **Horizontal Distribution**: Equal-width buttons filling full width
- **Responsive**: Scales with screen size, maintains button proportions

### Layout Structure
```
[CALLERS Button (33%)] [ITEMS Button (33%)] [STATS Button (33%)]
```

## Content Requirements

### Tab Buttons (3 total)
- **Type**: Unity Button components
- **Labels**: "CALLERS", "ITEMS", "STATS"
- **Styling**: Consistent button appearance, active state highlighting
- **States**: Normal, Hover, Active, Disabled (if applicable)

### Tab Content Areas
- **Position**: Below tab buttons, fills remaining screen height minus footer
- **Dynamic Content**: Different content per tab (caller list, inventory, statistics)
- **Scroll Support**: Full ScrollRect with vertical scrollbar, auto-hide when not needed
- **Minimum Height**: 100px minimum to ensure usability on small screens
- **Empty States**: Appropriate messages when no content available

## Behavior & Interactions

### Tab Switching
- **Click Handling**: Left-click to switch active tab
- **Visual Feedback**: Active tab highlighted (different color/background)
- **Content Visibility**: Only active tab's content visible
- **State Persistence**: Remembers last active tab between sessions

### Content Updates
- **Real-time Updates**: Content refreshes as game state changes
- **Event-Driven**: Responds to relevant game events (new caller, item changes, stat updates)
- **Performance**: Efficient updates, avoid full rebuilds on minor changes

### Navigation
- **Keyboard Support**: Tab key navigation between buttons
- **Accessibility**: Screen reader announcements for tab changes

## Technical Requirements
- **Implementation**: Generic TabController.cs with TabDefinition configuration
- **Configuration**: List<TabDefinition> with content providers and callbacks
- **State Management**: Tracks active tab index, content visibility
- **Event Integration**: Event-driven content updates via RefreshTabContent()
- **Scrolling**: ScrollRect with ContentSizeFitter for dynamic content
- **Layout**: LayoutElement components for proper height distribution
- **Memory Management**: Efficient content population with automatic cleanup

## Visual Design References
- See `ART_STYLE.md` for button styling and color scheme
- Consistent with overall UI aesthetic
- Clear visual hierarchy between active/inactive tabs
- Professional broadcast interface appearance

## Implementation Notes
- **Generic Architecture**: Configured with `List<TabDefinition>` containing content providers
- **Dynamic Creation**: Tabs and content areas created based on configuration
- **Event-Driven Updates**: Content refreshed via `RefreshTabContent(int tabIndex)` calls
- **Content Providers**: `PopulateCallersContent()`, `PopulateItemsContent()`, `PopulateStatsContent()` in UIManagerBootstrap
- **Layout**: `LayoutElement` components enable flexible height distribution
- **Reusable**: Can be configured for any tabbed interface with different content

## Tab Content Specifications

### CALLERS Tab
- Displays caller queue with screening information
- Real-time caller status updates
- Integration with screening system

### ITEMS Tab
- Shows inventory/evidence items
- Item organization and tooltips
- Equipment status and upgrades

### STATS Tab
- Broadcast statistics (listeners, revenue, etc.)
- Performance metrics
- Historical data and trends

## Testing Requirements
- Verify all tab switching works smoothly
- Test content updates in each tab
- Validate layout on different resolutions
- Confirm keyboard navigation and accessibility
- Performance test with large content sets</content>
<parameter name="filePath">D:\Dev\Games\kbtv\docs\ui\panel-specs\tab-system.md