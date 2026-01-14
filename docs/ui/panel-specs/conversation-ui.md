# Conversation UI Specification

## Overview
The conversation UI displays dialogue during live broadcasts, showing caller speech with mood-based visual styling and discernment mechanics. It provides the core interactive experience of the radio show.

## Purpose & Functionality
- **Dialogue Display**: Shows caller speech and host responses
- **Mood Visualization**: Visual representation of conversation emotional state
- **Discernment System**: Reveals caller truthfulness and hidden traits
- **Real-time Interaction**: Live conversation flow with timing elements

## Layout & Positioning
- **Position**: Central screen area, overlay during conversations
- **Size**: Large readable text area (80% screen width, variable height)
- **Modal Behavior**: Appears during active conversations, semi-transparent background
- **Responsive**: Scales text size with screen resolution

### Layout Structure
```
[Speaker Indicator]
[Dialogue Text Area - Scrollable]
[Mood Indicators & Discernment Cues]
[Response Options (when applicable)]
```

## Content Requirements

### Speaker Identification
- **Current Speaker**: Name/label of who's speaking (Caller Name, "You", "Host")
- **Visual Distinction**: Different styling for caller vs. host speech
- **Audio Integration**: Syncs with voice audio playback

### Dialogue Text
- **Real-time Display**: Text appears character-by-character or word-by-word
- **Formatting**: Speaker names in bold, dialogue in regular text
- **Length Handling**: Automatic text wrapping, scrolling for long passages
- **Special Effects**: Emphasis formatting for important lines

### Mood Visualization
- **Mood States**: 5 variants based on `CONVERSATION_ARCS.md`
  - Excited/Positive
  - Neutral/Cooperative
  - Skeptical/Questioning
  - Angry/Confrontational
  - Revelation/Breakthrough
- **Visual Indicators**: Background colors, border effects, icon representations
- **Transition Effects**: Smooth animations between mood states

### Discernment System
- **Truth Indicators**: Visual cues for caller honesty levels
- **Hidden Traits**: Subtle reveals of caller characteristics
- **Belief Tracking**: Shows how beliefs evolve during conversation
- **Lie Detection**: Optional mechanic for advanced players

## Behavior & Interactions

### Dialogue Flow
- **Automatic Progression**: Text displays at natural reading pace
- **Player Control**: Optional speed-up controls (click to skip, hold to fast-forward)
- **Pause/Resume**: Integration with game pause states

### Response System
- **Branching Options**: Multiple response choices when available
- **Timing Pressure**: Some responses have time limits
- **Consequence Preview**: Optional hints about response outcomes

### Mood Dynamics
- **Real-time Updates**: Mood changes based on dialogue content
- **Player Influence**: Responses affect mood progression
- **Visual Feedback**: Immediate reaction to player choices

## Technical Requirements
- **Text Rendering**: High-quality font with good readability
- **Animation System**: Smooth text appearance and mood transitions
- **Audio Sync**: Timing aligned with voice audio
- **Performance**: Efficient text processing, no frame drops during dialogue

## Visual Design References
- See `ART_STYLE.md` for typography and color guidelines
- Mood-based color schemes (warm for positive, cool for negative)
- Clear readability for live broadcast text
- Immersive conversation experience aesthetic

## Implementation Notes
- Integrated with conversation arc system from `CONVERSATION_ARCS.md`
- Uses mood variant data from `CONVERSATION_ARC_SCHEMA.md`
- Part of broadcast flow integration
- See `CONVERSATION_DESIGN.md` for dialogue mechanics

## Dialogue Display Specifications

### Text Styling
- **Font**: Clear, broadcast-appropriate typeface
- **Size**: 24-32pt for main text, smaller for speaker labels
- **Colors**: White text on semi-transparent backgrounds
- **Effects**: Optional glow or outline for emphasis

### Mood Visualization
- **Color Palette**: 
  - Positive: Warm yellows/golds
  - Neutral: Cool blues/grays
  - Negative: Reds/oranges for tension
- **Animation**: Subtle pulsing or gradient effects
- **Icons**: Mood representative symbols

### Response Options
- **Button Layout**: 2-4 options in a row or grid
- **Visual States**: Normal, hover, selected, disabled
- **Consequence Hints**: Optional preview text or icons

## Testing Requirements
- Verify text display timing matches audio
- Test all mood state transitions and visuals
- Validate response option functionality
- Confirm readability across different screen sizes
- Performance test with long dialogue sequences</content>
<parameter name="filePath">D:\Dev\Games\kbtv\docs\ui\panel-specs\conversation-ui.md