# Screening UI Specification

## Overview
The screening UI transforms caller approval into an information-gathering mini-game. Players observe visual cues revealing caller properties over time while managing patience and dead air risk.

## Purpose & Functionality
- **Information Revelation**: Passive display of caller properties through visual cues
- **Time Pressure**: Balances information gathering against caller patience
- **Strategic Decisions**: Players choose when to approve based on revealed information
- **Risk Management**: Dead air penalties for waiting too long or rejecting good callers

## Layout & Positioning
- **Position**: Overlay or dedicated panel during caller screening
- **Size**: Medium panel (600x400 pixels approximate)
- **Modal Behavior**: Appears when new caller arrives, dismissible by decision
- **Responsive**: Scales appropriately, maintains readability

### Layout Structure
```
[Header: Caller Name & Patience Meter]
[Property Grid: 11 Reveal Slots]
[Action Buttons: Approve/Reject]
```

## Content Requirements

### Header Section
- **Caller Name**: Displayed prominently at top
- **Patience Meter**: Visual bar showing remaining patience time
- **Time Pressure**: Color changes as patience decreases (green → yellow → red)

### Property Revelation Grid
- **11 Properties**: Organized in a 3x4 grid or similar layout
- **Reveal States**: Hidden → Scrambling → Revealed
- **Visual Cues**: Icons, text, colors representing different property types
- **Timing**: Properties reveal at different intervals

### Property Types (11 total)
Based on `SCREENING_DESIGN.md`:
1. **Basic Info**: Name, location, age range
2. **Topic Match**: How well caller fits current topic
3. **Legitimacy**: Real caller vs. troll/prank
4. **Personality**: Mood, demeanor, speaking style
5. **Technical**: Call quality, background noise
6. **Relevance**: Connection to current broadcast
7. **Value**: Potential entertainment/controversy level
8. **Timing**: When caller is calling (peak/off hours)
9. **History**: Previous call attempts
10. **Social**: Influencer status, following size
11. **Special**: Unique traits or red flags

### Action Buttons
- **Approve Button**: Green, puts caller on air immediately
- **Reject Button**: Red, removes caller, may cause dead air
- **Wait Button**: Optional, allows more information revelation

## Behavior & Interactions

### Revelation Mechanics
- **Automatic Progression**: Properties reveal over time without player input
- **Scrambling Effect**: Visual distortion during reveal transition
- **Priority System**: Most important info (topic match, legitimacy) reveals last

### Time Management
- **Patience Decay**: Visual meter decreases over time
- **Dead Air Risk**: Penalty if no callers available when needed
- **Strategic Timing**: Players must decide before patience expires

### Decision Outcomes
- **Approve**: Immediate broadcast start, risk of bad caller
- **Reject**: Caller removed, potential dead air period
- **Wait**: More info revealed, but patience continues decaying

## Technical Requirements
- **Animation System**: Smooth transitions for property reveals
- **State Management**: Tracks revelation progress and timing
- **Event Integration**: Connects with caller queue and broadcast system
- **Performance**: Efficient animations, no blocking operations

## Visual Design References
- See `ART_STYLE.md` for color coding and visual effects
- Mystery/reveal aesthetic with scrambling effects
- Clear urgency indicators for time pressure
- Professional screening room appearance

## Implementation Notes
- Part of screening system integration
- Uses property data from caller generation system
- Timer-based updates for revelation progression
- See `SCREENING_DESIGN.md` for gameplay mechanics

## Property Display Specifications

### Reveal States
- **Hidden**: Empty slot with placeholder
- **Scrambling**: Animated distortion effect (3-5 seconds)
- **Revealed**: Clear display of property value and icon

### Visual Feedback
- **Tier Indicators**: Color coding for property importance
- **Icon System**: Unique icons for each property type
- **Text Formatting**: Clear, readable property descriptions

## Testing Requirements
- Verify revelation timing matches design specifications
- Test patience meter accuracy and visual feedback
- Validate all property types display correctly
- Confirm decision outcomes trigger appropriate game events
- Performance test with multiple rapid revelations</content>
<parameter name="filePath">D:\Dev\Games\kbtv\docs\ui\panel-specs\screening-ui.md