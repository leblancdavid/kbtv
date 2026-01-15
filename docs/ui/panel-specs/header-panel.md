# Header Panel Specification

## Overview
The header panel displays critical broadcast status information at the top of the screen during live shows. It provides real-time feedback on broadcast state, timing, and audience engagement.

## Purpose & Functionality
- **Live Status Indicator**: Shows when the broadcast is active ("LIVE" text or visual indicator)
- **Time Display**: Shows remaining show time in MM:SS format
- **Listener Count**: Displays current listener/viewer count
- **Visual Hierarchy**: Highest priority information, always visible during live broadcast

## Layout & Positioning
- **Position**: Top edge of screen, full width
- **Height**: 40-60 pixels (responsive to screen size)
- **Horizontal Distribution**: Evenly spaced elements with flexible spacers
- **Responsive**: Scales with reference resolution (1920x1080), maintains proportions

### Layout Structure
```
[Flexible Spacer] [Live Indicator] [Flexible Spacer] [Time Display] [Flexible Spacer] [Listener Count] [Flexible Spacer]
```

## Content Requirements

### Live Indicator
- **Type**: Label node
- **Content**: "LIVE" when broadcast active, "OFF AIR" when inactive
- **Styling**: Bold, gold/orange color (#FFA500), 16-20pt font
- **Animation**: Optional pulsing effect when live

### Time Display
- **Type**: TextMeshProUGUI
- **Format**: "MM:SS" (e.g., "12:34")
- **Data Source**: ShowDuration - ElapsedTime
- **Update Frequency**: Every frame or every second
- **Styling**: Monospace font, white color, 18-24pt font
- **Edge Cases**: Shows "00:00" when time expires

### Listener Count
- **Type**: TextMeshProUGUI
- **Format**: "###" (e.g., "247")
- **Data Source**: GameState.Listeners or similar
- **Update Frequency**: Real-time when listeners change
- **Styling**: Regular font, green color (#00FF00), 16-20pt font
- **Icon**: Optional listener icon (👥 or similar)

## Behavior & Interactions
- **Visibility**: Only visible during LiveShow phase
- **Updates**: Automatic via event subscriptions (OnTimeUpdate, OnListenerUpdate)
- **No User Interaction**: Read-only display, no click handlers
- **Performance**: Efficient updates, no expensive operations per frame

## Technical Requirements
- **Integration**: Part of main UIManagerBootstrap Control hierarchy
- **Font**: Uses UIHelpers.DefaultFont with fallback system
- **Memory**: Minimal allocations, reuse text components
- **Accessibility**: Screen reader support for time and listener values
- **Platform**: Works on all Godot supported platforms

## Visual Design References
- See `ART_STYLE.md` for color palette and typography guidelines
- Gold accent color for live indicator
- Consistent spacing with other UI panels
- Clean, broadcast-studio aesthetic

## Implementation Notes
- Created in `UIManagerBootstrap._Ready()` method
- Uses `HBoxContainer` for distribution
- Event subscriptions in `SubscribeToEvents()`
- See `UI_IMPLEMENTATION.md` for code patterns and canvas setup

## Testing Requirements
- Verify time counts down accurately
- Confirm listener count updates in real-time
- Test layout on different screen resolutions
- Validate font loading and fallbacks</content>
<parameter name="filePath">D:\Dev\Games\kbtv\docs\ui\panel-specs\header-panel.md