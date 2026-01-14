# Footer Panels Specification

## Overview
The footer panels display dynamic broadcast controls and status information at the bottom of the screen. They provide critical gameplay interactions and real-time feedback during live shows.

## Purpose & Functionality
- **Broadcast Control**: On Air button for putting callers live
- **Content Display**: Transcript of current broadcast segment
- **Ad Management**: Ad break controls and revenue tracking
- **Financial Overview**: Money display with income/expense tracking
- **Action Center**: Primary player interaction area for broadcast decisions

## Layout & Positioning
- **Position**: Bottom edge of screen, full width
- **Height**: 120-160 pixels (accommodates text content)
- **Horizontal Distribution**: Evenly spaced panels with flexible spacers
- **Responsive**: Scales with screen size, maintains readable text sizes

### Layout Structure
```
[Flexible Spacer] [On Air Panel] [Flexible Spacer] [Transcript Panel] [Flexible Spacer] [Ad Break Panel] [Flexible Spacer] [Money Panel] [Flexible Spacer]
```

## Content Requirements

### On Air Panel
- **Primary Button**: Large "ON AIR" button for activating callers
- **Caller Preview**: Shows next caller information when available
- **State Feedback**: Visual indication of broadcast status
- **Error Handling**: Disabled state when no valid caller available

### Transcript Panel
- **Content**: Live broadcast transcript text
- **Scrolling**: Vertical scroll for long transcripts
- **Real-time Updates**: Text appears as broadcast progresses
- **Formatting**: Speaker identification, dialogue formatting

### Ad Break Panel
- **Ad Button**: Trigger commercial breaks
- **Revenue Display**: Shows ad revenue earned
- **Cooldown Indicator**: Shows when ads are available again
- **Ad Queue**: Preview of upcoming sponsored content

### Money Panel
- **Current Balance**: Primary money display ($XXX format)
- **Income Tracking**: Recent transactions and earnings
- **Expense Alerts**: Warnings for upcoming costs
- **Visual Feedback**: Color coding for positive/negative changes

## Behavior & Interactions

### User Actions
- **On Air Button**: Click to start broadcast with selected caller
- **Ad Break Button**: Click to insert commercial break
- **Panel Interactions**: Hover effects, tooltips for additional info
- **Keyboard Shortcuts**: Hotkeys for common actions (O for On Air, etc.)

### Dynamic Updates
- **Real-time Sync**: All panels update with game state changes
- **Event Responses**: React to broadcast events, money changes, ad availability
- **State Transitions**: Visual changes during broadcast phases

### Visual Feedback
- **Active States**: Highlighting during active operations
- **Status Colors**: Green for positive, red for alerts/warnings
- **Animations**: Subtle transitions for state changes

## Technical Requirements
- **Canvas Integration**: Part of main UIManagerBootstrap canvas
- **Performance**: Efficient text updates, minimal layout recalculations
- **Memory**: Text pooling for transcript content
- **Accessibility**: Keyboard navigation, screen reader support

## Visual Design References
- See `ART_STYLE.md` for panel styling, color palette
- Consistent with header/footer aesthetic
- Professional broadcast control room appearance
- Clear visual hierarchy and readability

## Implementation Notes
- Created in `UIManagerBootstrap.CreateFooterPanels()`
- Individual panel creation methods for each component
- Event subscriptions for dynamic updates
- Uses `UIPanelBuilder` for consistent panel construction

## Panel-Specific Requirements

### On Air Panel Details
- **Button States**: Normal, Pressed, Disabled
- **Caller Validation**: Checks screening completion and compatibility
- **Broadcast Integration**: Triggers dialogue system and audio

### Transcript Panel Details
- **Text Formatting**: Speaker names in bold, dialogue in regular
- **Scroll Behavior**: Auto-scroll to bottom for new content
- **History Limit**: Configurable maximum transcript length

### Ad Break Panel Details
- **Ad Types**: Support for different sponsor tiers
- **Revenue Calculation**: Based on listener count and ad quality
- **Cooldown System**: Prevents ad spam, configurable duration

### Money Panel Details
- **Currency Format**: $XXX.XX with appropriate separators
- **Transaction Log**: Recent income/expenses with timestamps
- **Budget Alerts**: Warnings for low funds or high expenses

## Testing Requirements
- Verify all button interactions work correctly
- Test real-time updates across all panels
- Validate layout scaling on different resolutions
- Confirm transcript scrolling and formatting
- Performance test with long broadcast sessions</content>
<parameter name="filePath">D:\Dev\Games\kbtv\docs\ui\panel-specs\footer-panels.md