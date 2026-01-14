# Pre-Show UI Specification

## Overview
The pre-show UI handles topic selection and show preparation before live broadcast begins. It provides the setup phase where players choose broadcast topics and prepare for the show.

## Purpose & Functionality
- **Topic Selection**: Choose from available topics for the upcoming show
- **Show Preparation**: Final setup before going live
- **Topic Management**: View topic freshness, popularity, and rewards
- **Transition Interface**: Smooth handoff to live broadcast UI

## Layout & Positioning
- **Position**: Full screen during pre-show phase
- **Size**: Adaptive to screen resolution
- **Phase-Based**: Only visible during PreShow game phase
- **Clean Design**: Focus on topic selection without distractions

### Layout Structure
```
[Header: Show Preparation Title]
[Topic Selection Grid/List]
[Show Settings Panel]
[Start Show Button]
```

## Content Requirements

### Topic Selection
- **Available Topics**: Grid or list of selectable broadcast topics
- **Topic Details**: Name, description, estimated duration, potential rewards
- **Freshness Indicators**: Visual cues for topic repetition penalties
- **Popularity Metrics**: Expected listener interest levels

### Show Settings
- **Duration Selection**: Choose show length (short/medium/long)
- **Difficulty Options**: Optional challenge modifiers
- **Equipment Preview**: Show active upgrades and tools
- **Budget Display**: Available funds for show preparation

### Start Show Button
- **Prominent CTA**: Large, clearly labeled start button
- **Validation Checks**: Ensures topic selected and requirements met
- **Transition Effect**: Smooth fade to live show UI

## Behavior & Interactions

### Topic Selection
- **Single/Multiple Choice**: Select one primary topic, optional secondary
- **Preview Details**: Hover/click to see full topic descriptions
- **Validation**: Prevent invalid topic combinations
- **Persistence**: Save selections between sessions

### Settings Configuration
- **Real-time Updates**: Preview effects of settings changes
- **Budget Integration**: Show costs for premium options
- **Equipment Effects**: Display how upgrades affect show parameters

### Show Start
- **Confirmation Dialog**: Optional confirmation for important decisions
- **Loading Transition**: Brief loading state during UI switch
- **State Transfer**: Pass topic and settings to live show systems

## Technical Requirements
- **Phase Management**: Automatic activation during PreShow phase
- **Data Integration**: Pulls from topic database and player progression
- **Validation Logic**: Ensures all requirements met before show start
- **Performance**: Fast loading, smooth transitions

## Visual Design References
- See `ART_STYLE.md` for pre-show aesthetic guidelines
- Professional studio preparation feel
- Clear information hierarchy
- Motivational, exciting pre-broadcast atmosphere

## Implementation Notes
- Managed by `PreShowUIManager.cs`
- Integrates with topic experience system from `TOPIC_EXPERIENCE.md`
- Prepares data for live show initialization
- See `UI_IMPLEMENTATION.md` for manager patterns

## Topic Display Specifications

### Topic Cards
- **Layout**: Card-based design with image/icon, title, stats
- **Information**: Difficulty, duration, reward preview
- **States**: Available, Locked, Selected, Cooldown

### Freshness System
- **Visual Indicators**: Color coding for freshness levels
- **Penalty Warnings**: Clear display of XP/reward reductions
- **Cooldown Timers**: When topics become available again

### Selection Feedback
- **Highlighting**: Clear indication of selected topics
- **Combination Warnings**: Alerts for incompatible topic pairs
- **Reward Preview**: Dynamic calculation of expected earnings

## Testing Requirements
- Verify all topic selection options work correctly
- Test settings validation and budget calculations
- Confirm smooth transition to live show
- Validate topic data loading and display
- Performance test with large topic databases</content>
<parameter name="filePath">D:\Dev\Games\kbtv\docs\ui\panel-specs\pre-show-ui.md