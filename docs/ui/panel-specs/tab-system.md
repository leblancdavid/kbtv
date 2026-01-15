# Tab System Specification

## Overview
The tab system provides navigation between different information views during live broadcasts using Godot's TabContainer. It allows players to switch between caller queue, inventory items, and broadcast statistics without leaving the main UI.

## Purpose & Functionality
- **Content Organization**: Groups related information into logical categories
- **Space Efficiency**: Allows multiple data sets in limited screen space
- **User Control**: Gives players choice over what information to prioritize
- **Visual Feedback**: Clear indication of active tab and available content

## Layout & Positioning
- **Position**: Below header panel, full screen width
- **Height**: Dynamic based on content, fills available space
- **Horizontal Distribution**: Tab buttons fill available width proportionally
- **Responsive**: Scales with screen size using Godot's layout system

### Layout Structure
```
┌─ TabContainer ──────────────────────────────┐
│  ┌─────┬─────┬─────┐                        │
│  │CALLERS│ITEMS│STATS│                       │
│  └─────┴─────┴─────┘                        │
│                                             │
│  [Tab Content Area - fills remaining space] │
└─────────────────────────────────────────────┘
```

## Content Requirements

### Tab Container
- **Type**: Godot TabContainer node
- **Scene**: `TabContainerUI.tscn`
- **Tabs**: 3 predefined tabs (Callers, Items, Stats)
- **Styling**: Theme overrides for consistent appearance

### Tab Content Areas
- **Position**: Within TabContainer, fills tab content area
- **Dynamic Content**: Different panels per tab instantiated from scenes
- **Scroll Support**: ScrollContainer with vertical scrollbar when needed
- **Minimum Height**: Adapts to available space
- **Empty States**: Appropriate messages when no content available

## Behavior & Interactions

### Tab Switching
- **Click Handling**: Left-click tab headers to switch
- **Visual Feedback**: Godot's built-in tab highlighting
- **Content Visibility**: TabContainer automatically shows/hides tab contents
- **State Persistence**: Remembers active tab between refreshes

### Content Updates
- **Real-time Updates**: Content refreshes when tab becomes active
- **Event-Driven**: Responds to game events via signal connections
- **Performance**: Efficient instantiation of scene-based panels

### Navigation
- **Keyboard Support**: Tab key navigation between tab headers
- **Mouse**: Click to switch tabs
- **Accessibility**: Godot's built-in accessibility features

## Technical Requirements
- **Implementation**: Godot TabContainer with scene-instantiated content
- **Configuration**: Tabs populated programmatically in UIManagerBootstrap
- **State Management**: TabContainer.CurrentTab tracks active tab
- **Event Integration**: Content updates via RefreshTabContent()
- **Layout**: SizeFlagsExpandFill for responsive sizing
- **Memory Management**: Scene instances cleaned up on tab changes

## Visual Design References
- See `ART_STYLE.md` for color scheme and styling
- Consistent with overall Godot theme
- Clear visual hierarchy between active/inactive tabs
- Professional broadcast interface appearance

## Implementation Notes
- **Scene-Based**: Each tab content is a PackedScene instance
- **Factory Pattern**: PanelFactory creates appropriate panels for each tab
- **Event-Driven Updates**: Content refreshed via signal connections
- **Content Methods**: PopulateCallersContent(), PopulateItemsContent(), PopulateStatsContent()
- **Layout**: VBoxContainer/HBoxContainer for responsive layouts
- **Reusable**: TabContainer can be extended with additional tabs

## Tab Content Specifications

### CALLERS Tab
- **Layout**: Three-panel horizontal layout (Incoming/Screening/On Hold)
- **Content**: CallerPanel scenes showing caller lists
- **Updates**: Real-time caller queue changes
- **Interaction**: Screening panel with approve/reject buttons

### ITEMS Tab
- **Layout**: Grid or list of inventory items
- **Content**: Evidence items and equipment status
- **Updates**: Inventory changes and tool upgrades
- **Interaction**: Item tooltips and selection

### STATS Tab
- **Layout**: Vertical list of statistics
- **Content**: Listener count, revenue, show metrics
- **Updates**: Real-time statistic changes
- **Interaction**: Historical data display

## Testing Requirements
- Verify TabContainer switches tabs correctly
- Test scene instantiation for each tab content
- Validate responsive layout on different resolutions
- Confirm keyboard navigation works
- Performance test with large content sets
- Verify content updates on tab switches</content>
<parameter name="filePath">D:\Dev\Games\kbtv\docs\ui\panel-specs\tab-system.md