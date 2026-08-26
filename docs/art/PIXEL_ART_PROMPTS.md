# KBTV - Pixel Art Prompt Library

This document contains comprehensive AI prompts for generating pixel art assets for KBTV. All prompts follow the project's dark noir retro aesthetic with precise specifications.

## Table of Contents

1. [Core Style Guidelines](#core-style-guidelines)
2. [Environment Tiles](#environment-tiles)
3. [Furniture & Props](#furniture--props)
4. [Character Assets](#character-assets)
5. [UI Components](#ui-components)
6. [Caller Silhouettes](#caller-silhouettes)
7. [Equipment & Technology](#equipment--technology)
8. [Evidence & Items](#evidence--items)
9. [Atmospheric Effects](#atmospheric-effects)
10. [Sprite Animations](#sprite-animations)
11. [Batch Generation Workflow](#batch-generation-workflow)
12. [Post-Processing Guide](#post-processing-guide)

---

## Core Style Guidelines

### Mandatory Keywords (Include in Every Prompt)

```
pixel art, [dimension], [view], limited palette, [lighting], clean edges, game asset, transparent background, 8-bit retro style, crisp pixels, no anti-aliasing, high contrast
```

### KBTV Color Palette Reference (Noir Edition)

The palette was restricted to support the late-night noir aesthetic. Use these hex codes in prompts — avoid fully saturated primaries.

**Primary (cool, dark):**
- Void: `#0d0d12`
- Charcoal: `#1f1f26`
- Cool Shadow: `#2a2d36`
- Slate: `#3a3d48`

**Secondary (warm muted):**
- Warm Shadow: `#2a221b`
- Wood Mid: `#5a5340`
- Wood Highlight: `#7a6f55`

**Accents (rare, high contrast):**
- Cool Phosphor Green: `#3a8a78` (CRT glow — NOT `#00ff44`)
- Noir Red: `#a23a3a` (on-air sign, alert LED — NOT `#ff4444`)
- Cool Cyan: `#5fa8a8`
- Warm Rim: `#c89a5a`

**UI Colors:**
- Panel Background: `#1a1a22`
- Text Primary: `#e8e8e8` (off-white)
- Button Normal: `#2a2d36`
- Button Hover: `#3a3d48`
- CRT Accent: `#3a8a78`

**Rule:** Cap each asset at 8–10 colors. Light comes from screens. Saturation stays under 30%.

### Dimension Standards

| Asset Type | Size | Notes |
|------------|------|-------|
| Floor tiles | 16x16 | Seamless tiling required |
| Wall tiles | 16x64 | Vertical strips |
| Small props | 16x16 to 32x32 | Coffee mug, phone, etc. |
| Furniture | 32x32 to 64x64 | Desks, cabinets, monitors |
| Character portraits | 64x64 | UI conversation display |
| In-game sprites | 32x48 | Isometric character |
| UI icons | 16x16 to 24x24 | Buttons, indicators |
| Effects | 32x32 to 64x64 | Glows, lighting, overlays |

### View/Perspective Keywords

- **Top-down isometric**: `isometric top-down view, pseudo-3D depth`
- **Front-facing portrait**: `portrait, close-up, face-focused`
- **Side/elevation**: `side view, elevation, isometric side`
- **Pixel depth**: `depth sprite, front face + top face` (for occluders)

### Lighting Keywords

- **Noir atmosphere**: `dramatic noir lighting, high contrast, shadows, moody`
- **Monitor glow**: `glowing screen, CRT illumination, backlit`
- **Dim ambient**: `dimly lit, dark room, single light source`
- **Retro tech**: `retro computer glow, phosphor green/red`

---

## Environment Tiles

### Floor Tiles

**Base Prompt:**
```
dark carpet tile pattern, pixel art, 16x16, seamless tiling, texture with subtle variation, muted green #2d4a2d, top-down view, office flooring, clean edges, no anti-aliasing
```

**Variations:**
```
floor tile, wear and tear, small scuff marks, pixel art, 16x16, dark green carpet texture, seamless
```
```
floor tile, stained, coffee drip mark, pixel art, 16x16, darker patch variation, realistic detail
```
```
floor tile, clean, uniform texture, pixel art, 16x16, fresh carpet, minimal detail
```

### Wall Textures

**North/South Wall (Full Height 64px):**
```
wall texture, retro pixel art, 16x64, concrete/plaster surface, brown tones #5a4a3a, isometric side view, minimal detail, vertical strip
```

**Wall Top Band (32px tall):**
```
wall top band, pixel art, 16x32, dark brown wood/panel, ceiling line, top-down perspective shadow
```

**Wall Bottom Band (16px tall):**
```
wall baseboard, pixel art, 16x16, dark trim, floor transition, simple detail
```

**Corner Piece:**
```
wall corner bracket, pixel art, 16x64, joining two walls, isometric angle, dark wood texture, clean edges
```

**Wall with Window:**
```
wall with window opening, pixel art, 16x64, window frame #2a2a3a, glass dark #1a1a2a, side view
```

---

## Furniture & Props

### Desk & Console Systems

**Monitor Console (Main Desk):**
```
retro computer console, monitor + keyboard + phone, pixel art, 64x64, dark wood/metal, top-down isometric, glowing CRT screen #00ff44, office equipment, 80s aesthetic
```

**Monitor Only:**
```
CRT monitor, pixel art, 32x32, glowing green/red screen, scanlines, retro display, dark bezel, isometric angle
```
```
monitor bezel, pixel art, 16x16, dark frame, screen inset, simple detail
```

**Keyboard:**
```
computer keyboard, retro style, pixel art, 32x16, dark keys with legends, top-down view
```

**Desk (Empty):**
```
office desk, dark wood finish, pixel art, 64x32, top-down, rectangular shape, legs visible
```

**Desk (Depth Shadow):**
```
desk depth sprite, pixel art, 48x64, front face darker #3a3a3a, top face lighter #4a4a4a, isometric occlusion, vertical shadow
```

### Storage Furniture

**Filing Cabinet:**
```
vertical filing cabinet, 2-3 drawers, pixel art, 32x48, steel grey #6a6a6a, office storage, labeled drawers
```

**Filing Cabinet (Depth):**
```
filing cabinet depth, pixel art, 48x64, tall rectangular, front face dark, top face lit, industrial design
```

**Bookcase:**
```
wooden bookcase, filled with books, pixel art, 48x64, dark stain #5a4a3a, organized clutter, isometric angle
```
```
bookcase (depth), pixel art, 64x64, tall shelves visible, shadow depth, library furniture
```

**Storage Shelf:**
```
open storage shelf, office supplies, pixel art, 32x48, metal frame, bins and boxes
```

### Seating

**Office Chair:**
```
office chair, swivel, dark fabric, pixel art, 32x32, top-down, wheel base visible
```
```
office chair (depth), pixel art, 32x48, backrest visible, cylindrical base, chair height
```

**Guest Chair:**
```
guest chair, visitor seat, pixel art, 24x24, simple design, placed at desk front
```

### Tables

**Round Table:**
```
round conference table, pixel art, 48x48, dark wood, oval shape, top-down isometric
```

**Studio Table:**
```
broadcast studio table, large mixing desk, pixel art, 80x48, dark surface, equipment mounts
```

**Table (Depth):**
```
table depth sprite, pixel art, 64x48, conference table, tall leg, front/top face shadow
```

---

## Character Assets

### Vern - Portrait Series

**Base Portrait Prompt (Noir / Art Bell inspired):**
```
late-night radio host portrait, pixel art, 64x64, silver swept-back hair, wire-frame glasses, light grey stubble, deep-set tired eyes, dark suit jacket over black collared shirt, warm rim light from off-screen CRT, dim charcoal ambient, expressive, retro aesthetic, noir palette only
```

#### Mood Variants

**Neutral/Broadcast Standard:**
```
Vern neutral portrait, pixel art, 64x64, calm expression, slight smile, late-night radio host, dim cool phosphor #3a8a78 rim light on face, professional demeanor, noir palette
```

**Tired/Sleepy:**
```
Vern tired portrait, pixel art, 64x64, heavy eyelids, dark circles, exhausted, 3am broadcast, muted colors, more stubble, noir palette
```

**Focused/Intense:**
```
Vern focused portrait, pixel art, 64x64, sharp gaze, leaning forward, dramatic shadows, glasses catching screen glow, determined, serious investigative tone
```

**Confused/Curious:**
```
Vern confused portrait, pixel art, 64x64, raised eyebrows, puzzled expression, head tilt, subtle confusion, curious investigator
```

**Stressed/Panicked:**
```
Vern stressed portrait, pixel art, 64x64, wide eyes, furrowed brow, sweat on temple, panic situation, high contrast noir lighting
```

**Suspicious:**
```
Vern suspicious portrait, pixel art, 64x64, narrowed eyes, skeptical look, pursed lips, paranoid, conspiracy mindset
```

**Happy/Pleased:**
```
Vern happy portrait, pixel art, 64x64, genuine smile, warm glow from CRT on face, satisfied, breakthrough moment
```

**Angry/Frustrated:**
```
Vern angry portrait, pixel art, 64x64, clenched jaw, intense stare, faint noir red #a23a3a accent lighting, heated emotion
```

**Scared/Nervous:**
```
Vern scared portrait, pixel art, 64x64, fearful eyes, trembling, cold sweat, terror, deep noir shadows
```

**Eureka/Moment:**
```
Vern eureka portrait, pixel art, 64x64, eyes wide with realization behind glasses, bright accent #5fa8a8, epiphany expression
```

### Vern - In-Game Sprite

**Standing Pose (Art Bell inspired, current style):**
```
isometric character, late-night radio host Vern, standing behind a desk, pixel art, 80x80 canvas, silver swept-back hair, wire-frame glasses, dark suit jacket, top-down low angle, noir atmosphere, dim charcoal ambient, cool phosphor rim light, transparent background
```

**Idle animation frames (6):**
- Frame 0: neutral standing
- Frame 1: subtle head tilt
- Frame 2: eyes blink
- Frame 3: shoulders settle
- Frame 4: hand moves toward coffee mug
- Frame 5: weight shifts, looking slightly down

**Drinking animation frames (6):**
- Frame 0: reaches for coffee mug
- Frame 1: lifts mug
- Frame 2: mug to lips
- Frame 3: drinking
- Frame 4: lowers mug
- Frame 5: mug back on desk, slight exhale

**Walking animation frames (per direction, 6 each):**
*Art Bell's late-night host shouldn't walk much — but if needed:*
```
Vern walking south frame 1, pixel art, 80x80, silver hair visible from behind, suit jacket sways, slow deliberate step, noir palette
```

### Vern - Body Variations (for different moods in-game)

**Tired Body Language:**
```
Vern tired posture, pixel art, 80x80, slumped shoulders, head down, slow walk, drooping arms, glasses sliding down nose
```

**Energetic Body Language:**
```
Vern energetic pose, pixel art, 80x80, upright posture, confident stride, animated arm gesture toward microphone
```

---

## UI Components

### Panel Backgrounds

**Main Panel:**
```
dark UI panel background, pixel-perfect borders, retro interface, #2a2a3a fill, subtle gradient, 1px stroke #3a3a4a, tech-panel design, corner brackets
```

**Panel Header:**
```
UI panel header bar, pixel art, 200x28, dark background #2a2a3a, top border highlight, amber text area #ffb300, clean
```

**Content Area:**
```
content container background, pixel art, #2a2a3a, slightly darker than header, borderless, flexible sizing placeholder
```

### Buttons

**Standard Button States:**

*Default:*
```
round button, pixel art, 80x28, inactive state, dark grey #4a4a5a, retro UI design, subtle bevel, labeled "APPROVE"
```
```
round button, pixel art, 80x28, inactive state, dark grey #4a4a5a, retro UI design, labeled "DENY"
```

*Hover:*
```
button hover state, pixel art, 80x28, brighter #5a5a6a, cyan glow #00ffff edge, upward bevel, interactive highlight
```

*Pressed:*
```
button pressed state, pixel art, 80x28, darker #3a3a4a, inset shadow, clicked feedback
```

### Icons & Indicators

**On-Air Indicator:**
```
ON AIR sign, glowing red LED, pixel art, 32x16, illuminated text, retro electronics, #ff4444 bright, dark off-state variant also needed
```

**Waiting Caller:**
```
caller waiting icon, silhouette, pixel art, 16x16, amber color #ffaa00, antenna waves, simple bell shape
```

**Airborne Waves:**
```
radio waves, signal transmission, pixel art, 16x16, concentric arcs, green glow #00ff44, broadcasting symbol
```

**Progress Icon:**
```
progress indicator, pixel art, 16x16, filled semicircle, cyan #00aaaa, radial fill percentage, minimal
```

**Status Light:**
```
status LED, pixel art, 8x8, circular, color variants: green (success), red (error), amber (warning), dark (inactive)
```

### Progress Bars

**Horizontal Bar:**
```
horizontal progress bar, retro style, pixel art, 200x16, filled state cyan #00aaaa, empty dark grey #4a4a5a, 1px segment divisions, clean edges
```

**Vertical Bar:**
```
vertical progress bar, pixel art, 16x100, top-to-bottom fill, cyan accent, retro tick marks, dark background
```

### Tab Elements

**Tab Button (Inactive):**
```
tab button, inactive state, pixel art, 80x24, dark grey #4a4a5a, bottom border hidden, labeled "CALLERS"
```

**Tab Button (Active):**
```
tab button, active state, pixel art, 80x24, light background #5a5a6a, bottom border visible #00ffff, selected indicator
```

**Tab Content Background:**
```
tab content area, pixel art, full width/height, #2a2a3a, borderless container, holds dynamic content
```

### List Items

**Caller Queue Item:**
```
caller queue item, pixel art, 240x32, dark background, left border cyan #00aaaa when selected, name left, wait time right, monospace font area
```

**Evidence List Item:**
```
evidence item, pixel art, 240x24, dark background, icon left, text right, locked/unlocked states, evidence type color coding
```

### Text Input Fields

**Input Box:**
```
text input field, pixel art, 200x28, dark background #2a2a3a, light border #4a4a5a, cursor block, retro terminal style
```

---

## Caller Silhouettes

### Generic Silhouettes

**Base Male Silhouette:**
```
male person silhouette, front-facing, pixel art, 32x48, dark shadow #1a1a2a, speaking pose, hands near mouth, mysterious, clean edges
```

**Base Female Silhouette:**
```
female person silhouette, front-facing, pixel art, 32x48, dark shadow #1a1a2a, different body shape, shoulder-length hair, subtle curves
```

### Archetype Variations

**Conspiracy Theorist:**
```
conspiracy theorist caller, wild hair, frantic gestures, silhouette, pixel art, 32x48, dramatic angle, papers in hand, chaotic energy
```

**Nervous Caller:**
```
nervous caller silhouette, hunched shoulders, crouching posture, pixel art, 32x48, small stature, hesitant pose
```

**Confident Whistleblower:**
```
confident caller, hands on hips, strong stance, chest out, pixel art, 32x48, authoritative silhouette
```

**Elderly Caller:**
```
elderly caller, hunched, frail posture, pixel art, 32x48, small head, thin frame, wobbling pose
```

**Aggressive Caller:**
```
aggressive caller, pointing finger, angry stance, pixel art, 32x48, clenched fists, forward lean
```

**Shy/Quiet Caller:**
```
shy caller, turned slightly away, arms crossed, pixel art, 32x48, hiding face, small, closed body language
```

**Excited Caller:**
```
excited caller, arms raised, jumping pose, pixel art, 32x48, enthusiastic energy, dynamic shape
```

**Panicked Caller:**
```
panicked caller, hands on head, desperate pose, pixel art, 32x48, wild hair, terrified body language
```

**Mysterious Caller:**
```
mysterious caller, hooded figure, concealed face, pixel art, 32x48, cloak draped, ominous, unknown identity
```

**Authority Figure:**
```
authority figure caller, military/law enforcement, pixel art, 32x48, hat, badge, formal uniform, stern posture
```

**Sketchy Caller:**
```
sketchy caller, shady character, pixel art, 32x48, hat pulled low, trench coat, suspicious eyes
```

---

## Equipment & Technology

### Microphones

**Boom Microphone:**
```
vintage boom microphone, large capsule, pixel art, 24x24, retro radio studio, dark metal, broadcast quality, suspended on arm
```

**Desktop Microphone:**
```
desk microphone, standard broadcast, pixel art, 16x20, round head, dark metal grille, office phone
```

**Microphone (Depth):**
```
microphone depth sprite, pixel art, 24x32, cylindrical, front face dark, top face lit, shadows
```

### Audio Equipment

**Sound Board / Mixing Console:**
```
mixing console, retro broadcast gear, pixel art, 64x32, top-down view, faders and knobs, detailed, dark surface, warm accent lights
```

**Basic Mixer:**
```
audio mixer, simplified, pixel art, 48x24, channel strips, sliders, potentiometers, functional design
```

**Audio Rack:**
```
equipment rack, 19-inch studio gear, pixel art, 32x48, metal chassis, blinking LEDs, cable management
```

**Speaker Stand:**
```
studio monitor speaker, bookshelf size, pixel art, 32x32, dark wood veneer, grille cloth texture, two-way design
```

*Single Speaker:*
```
bookshelf speaker, pixel art, 32x32, rectangular, dark finish, grille pattern, front-facing
```

**Headphones:**
```
studio headphones, over-ear, pixel art, 24x24, black padded, professional gear, headband visible
```

### Communication Equipment

**Phone (Rotary):**
```
vintage rotary telephone, dark color, pixel art, 24x32, office phone, dial mechanism visible, retro 1970s style
```

**Phone (Modern):**
```
office telephone, business phone, pixel art, 20x24, dark plastic, multiple buttons, LED display
```

**Phone Bank (Multiple Phones):**
```
phone bank, multiple handsets, pixel art, 48x32, stacked phones, rotary or button, call center aesthetic
```

**Answering Machine:**
```
answering machine, cassette-based, pixel art, 32x24, tape deck, message counter, retro voicemail
```

**Phone Line Indicator:**
```
phone line status light, pixel art, 8x8, green/red LED, label "LINE 1", circuit board background
```

### Computing Equipment

**Computer Station:**
```
computer workstation, retro PC, pixel art, 48x32, CRT monitor + keyboard + tower, 80s office, beige box aesthetic
```

**Computer Tower:**
```
desktop computer tower, beige PC, pixel art, 24x40, 5.25" floppy drive, power button, vents
```

**Floppy Disk:**
```
floppy disk, 3.5" diskette, pixel art, 16x16, label area, metal shutter, iconic storage
```

**Datasette/Tape Drive:**
```
cassette tape drive, data storage, pixel art, 24x16, tape reels visible, data cassette inserted
```

### Visual Display Equipment

**Multiple Monitors:**
```
dual monitor setup, pixel art, 64x32, two CRTs side-by-side, desk arrangement, glowing screens
```

**Monitor Array:**
```
quad monitor wall, pixel art, 96x48, 2x2 grid, control room setup, all displaying different data
```

**Dashboard Display:**
```
dashboard screen, data visualization, pixel art, 32x32, graphs, numbers, cyan/green glowing UI, retro terminal
```

**Security Monitor:**
```
security monitor, CCTV feed, pixel art, 32x32, split-screen view, timestamp overlay, grain effect
```

---

## Evidence & Items

### Storage & Organization

**Evidence Cabinet (Locked):**
```
filing cabinet labeled EVIDENCE, locked, pixel art, 32x48, metal grey, padlock visible, ominous
```

**Evidence Cabinet (Open):**
```
evidence cabinet open, pixel art, 32x48, drawer pulled, files inside, labeled folders
```

**Evidence Locker:**
```
wall-mounted evidence locker, pixel art, 24x36, metal box, combination lock, keyhole
```

**File Cabinet:**
```
vertical file cabinet, 4-drawer, pixel art, 32x48, office grey, labeled handles
```

### Document Types

**Classified Document:**
```
classified document, pixel art, 24x32, torn edges, paper texture, red CONFIDENTIAL stamp, typewriter text
```

**Photograph (Polaroid):**
```
polaroid photo, pixel art, 24x24, white border, ghostly figure visible, blurry, vintage look, grainy
```

**Map:**
```
hand-drawn map, pixel art, 32x32, crumpled paper, X marks location, hand-drawn style
```

**Manuscript/Notes:**
```
handwritten notes, pixel art, 32x32, paper pad, scribbled text, coffee stains, investigator's notes
```

**Audio Tape:**
```
cassette tape, audio evidence, pixel art, 16x16, reel-to-reel or cassette, labeled "INTERVIEW #3"
```

**Photograph (Clear):**
```
photograph, pixel art, 24x24, clear image, surveillance screenshot, timestamp 1995-03-12
```

### Evidence Icons

**Photo Evidence Icon:**
```
photo evidence icon, pixel art, 16x16, small camera, polaroid shape
```

**Audio Evidence Icon:**
```
audio evidence icon, pixel art, 16x16, waveform, speaker, sound file
```

**Document Evidence Icon:**
```
document evidence icon, pixel art, 16x16, folded paper, lines of text, file type
```

**Video Evidence Icon:**
```
video evidence icon, pixel art, 16x16,胶片, clapperboard, film reel
```

---

## Atmospheric Effects

### Lighting Sprites

**Monitor Glow (Green):**
```
soft green glow, pixel art, 32x32, radial gradient, alpha transparency, CRT screen effect, #00ff44 at center fading to transparent
```

**Monitor Glow (Red):**
```
red alert glow, pixel art, 32x32, radial, intense center #ff4444, warning atmosphere
```

**Monitor Glow (Cyan):**
```
cyan accent light, pixel art, 32x32, neon #00ffff, soft edges, tech aesthetic
```

**Monitor Glow (Amber):**
```
amber warning light, pixel art, 32x32, #ffaa00, subtle pulsing preparation, vintage indicator
```

**Light Cone:**
```
light cone/spotlight, pixel art, 64x64, triangular gradient, transparency, monitor direction, noir lighting
```

**Vignette Overlay:**
```
vignette effect, pixel art, 128x128, dark edges fading to transparent center, atmospheric depth
```

**Point Light:**
```
point light sprite, pixel art, 32x32, circular gradient, warm white center, soft falloff, lightbulb icon
```

### CRT/VHS Effects

**Scanlines Overlay:**
```
scanlines overlay, pixel art, 64x64, horizontal lines, 2px spacing, 50% opacity, retro CRT monitor effect, black lines on transparent
```

**Scanlines Subtle:**
```
subtle scanlines, pixel art, 32x32, thin lines, 1px height, light grey #aaaaaa, 30% opacity, TV effect
```

**Static Noise:**
```
TV static noise, pixel art, 32x32, random white/black specks, grain texture, transparent background, broadcast interference
```

**Static Frame 1:**
```
static noise frame 1, pixel art, 32x32, different random pattern, animation sequence
```
```
static noise frame 2, pixel art, 32x32, second variation
```
```
static noise frame 3, pixel art, 32x32, third variation
```

**Horizontal Hold Sync:**
```
horizontal sync line, pixel art, 128x4, white bar, CRT rolling effect element
```

**Rolling Bar:**
```
rolling bar effect, pixel art, 128x128, horizontal gradient band, TV rolling artifact, dark to light
```

### VHS Specific

**VHS Tracking Lines:**
```
VHS tracking error, horizontal wavy lines, pixel art, 128x32, distortion effect, retro video
```

**VHS Color Bleed:**
```
color bleed artifact, pixel art, 64x64, RGB separation, chromatic aberration, VHS tape effect
```

**Timecode Burn-in:**
```
VHS timecode, pixel art, 64x16, white numbers on black, 00:00:00:00 format, lower third
```

### Flicker Animations

**Monitor Flicker Set (4 frames):**
```
monitor flicker frame 1, dimmed, pixel art, 32x32, 30% brightness, green glow #00ff44
```
```
monitor flicker frame 2, brightened, pixel art, 32x32, 100% brightness, full glow
```
```
monitor flicker frame 3, medium, pixel art, 32x32, 60% brightness
```
```
monitor flicker frame 4, very dim, pixel art, 32x32, 15% brightness, almost off
```

**Dead Air Indicator:**
```
dead air sign, blinking, pixel art, 32x32, red text "DEAD AIR", dark background, urgent
```
```
dead air sign, frame 2, pixel art, 32x32, brighter, active state
```

**On-Air Light Pulse:**
```
ON AIR pulsing, pixel art, 16x16, bright red #ff4444, glowing, light pulse frame 1
```
```
ON AIR pulsing, pixel art, 16x16, medium red, frame 2
```
```
ON AIR pulsing, pixel art, 16x16, dim red, frame 3
```

---

## Sprite Animations

### Character Animations

**Vern Walk Cycle (3 frames per direction):**

*Down direction:*
```
Vern walking down frame 1, pixel art, 32x48, left foot forward, right arm back, slight bob in vertical
```
```
Vern walking down frame 2, pixel art, 32x48, both feet together, arms neutral, midpoint pose
```
```
Vern walking down frame 3, pixel art, 32x48, right foot forward, left arm back, opposite swing
```

*Up direction:*
```
Vern walking up frame 1, pixel art, 32x48, back view, legs partially visible, shoulder sway
```
```
Vern walking up frame 2, pixel art, 32x48, back view, standing pose
```
```
Vern walking up frame 3, pixel art, 32x48, back view, opposite leg forward
```

*Left/Right directions:*
```
Vern walking left frame 1, pixel art, 32x48, side view profile, three-quarter turn, left leg forward
```
```
Vern walking left frame 2, pixel art, 32x48, side view, centered
```
```
Vern walking left frame 3, pixel art, 32x48, side view, right leg forward
```

**Idle Animation (Breathing):**
```
Vern idle frame 1, pixel art, 32x48, standing, slight body rise, inhale
```
```
Vern idle frame 2, pixel art, 32x48, standing, slight body lower, exhale
```

### Equipment Animations

**Phone Ringing (3 frames):**
```
phone ringing frame 1, pixel art, 24x24, slight shake 1px left, bell icon visible
```
```
phone ringing frame 2, pixel art, 24x24, centered, no shake
```
```
phone ringing frame 3, pixel art, 24x24, slight shake 1px right, bell icon
```

**Monitor Glow Pulse (4 frames):**
```
monitor glow pulse frame 1, pixel art, 32x32, 100% brightness, #00ff44 full saturation
```
```
monitor glow pulse frame 2, pixel art, 32x32, 75% brightness
```
```
monitor glow pulse frame 3, pixel art, 32x32, 50% brightness
```
```
monitor glow pulse frame 4, pixel art, 32x32, 25% brightness, dim
```

**Coffee Steam (4 frames):**
```
coffee steam frame 1, pixel art, 16x16, small wisp rising, light grey alpha
```
```
coffee steam frame 2, pixel art, 16x16, larger wisp, higher
```
```
coffee steam frame 3, pixel art, 16x16, medium wisp, mid-height
```
```
coffee steam frame 4, pixel art, 16x16, dissipating, faint
```

**Clock Tick (2 frames):**
```
clock hand tick position 1, pixel art, 24x24, minute hand slightly before, second hand ready
```
```
clock hand tick position 2, pixel art, 24x24, minute hand moved, second hand advanced
```

**Paper Shuffle (3 frames):**
```
paper shuffle frame 1, pixel art, 24x24, papers stacked, edges aligned
```
```
paper shuffle frame 2, pixel art, 24x24, top paper shifted right, slight offset
```
```
paper shuffle frame 3, pixel art, 24x24, top paper shifted left, different offset
```

### UI Animations

**Button Hover (2 frames):**
```
button hover frame 1, pixel art, 80x28, normal state #4a4a5a
```
```
button hover frame 2, pixel art, 80x28, hovered #5a5a6a, cyan edge glow
```

**Progress Bar Fill (5 frames):**
```
progress bar 0%, pixel art, 200x16, empty, dark background only
```
```
progress bar 25%, pixel art, 200x16, quarter filled cyan
```
```
progress bar 50%, pixel art, 200x16, half filled
```
```
progress bar 75%, pixel art, 200x16, three-quarters filled
```
```
progress bar 100%, pixel art, 200x16, full cyan fill
```

---

## Batch Generation Workflow

### Phase 1: Core Environment (Week 1)

**Day 1-2: Floor System**
- Generate floor tiles (3 variants each)
- Generate floor edge tiles
- Generate carpet texture variations

**Day 3-4: Wall System**
- North wall atlas (full, door, window)
- South wall atlas
- East/West wall strips
- Corners and transitions

**Day 5-6: Doors & Openings**
- Door tile (open/closed)
- Door frame
- Threshold transition

**Day 7: Review & Refinement**
- Test tile placement in Godot
- Adjust colors to match lighting
- Generate any missing variants

### Phase 2: Furniture & Props (Week 2)

**Day 1-2: Desk Ecosystem**
- Monitor console (multiple angles)
- Monitor screens (on/off states)
- Keyboard, mouse
- Desk depth shadow

**Day 3-4: Storage**
- Filing cabinet (full/depth)
- Bookcase (filled/empty)
- Storage shelf
- Audio cabinet

**Day 5: Seating & Tables**
- Office chair (normal/depth)
- Guest chair
- Round table
- Studio table

**Day 6-7: Small Props**
- Coffee station
- Coffee mug
- Ashtray
- Clock
- Papers, pen holders
- Posters

### Phase 3: Character Assets (Week 3)

**Day 1-2: Vern Portraits**
- Neutral base (reference)
- 9 mood variants (tired, focused, confused, stressed, suspicious, happy, angry, scared, eureka)
- Test consistency across moods

**Day 3-4: Vern Sprites**
- Standing base sprite
- Walk cycle frames (12 total: 3 frames × 4 directions)
- Idle animation (2 frames)

**Day 5-7: Caller Silhouettes**
- Male base silhouette
- Female base silhouette
- 10 archetype variations
- Speaking pose variants (optional)

### Phase 4: UI Components (Week 4)

**Day 1-2: Panels & Buttons**
- Panel backgrounds (normal, header)
- Buttons (default, hover, pressed, disabled)
- Tab elements (active, inactive)

**Day 3: Icons & Indicators**
- On-air indicator
- Status lights (4 colors)
- Progress bar states
- Tab icons

**Day 4-5: List Items**
- Caller queue item
- Evidence list item
- Topic tab item

**Day 6-7: Inputs & Misc**
- Text input fields
- Sliders/toggles
- Modal backgrounds

### Phase 5: Effects & Polish (Week 5)

**Day 1-2: Lighting Effects**
- Monitor glows (4 colors)
- Light cone
- Vignette
- Point light

**Day 3-4: CRT/VHS Effects**
- Scanlines (subtle/intense)
- Static noise (3 frames)
- Horizontal hold
- Rolling bar

**Day 5-7: Animations**
- Flicker sets (monitor, lights)
- Steam effects
- Bell/ringer animations
- Integration testing

---

## Post-Processing Guide

### Step 1: Initial Cleanup

After AI generation, or using Pixel It:

1. **Remove anti-aliasing** - Ensure all edges are crisp pixels
2. **Fix transparency** - Set alpha to 0 or 255 only (no partial)
3. **Align to grid** - Snap all sprites to pixel boundaries
4. **Check dimensions** - Exact target size (16x16, 32x32, etc.)

### Step 2: Palette Adjustment

1. **Load KBTV palette** - Create color index in Aseprite/Pyxel Edit
2. **Replace colors** - Map image colors to project palette
3. **Dither elimination** - Remove any gradient attempts
4. **Color count** - Verify limited palette (16-32 colors typical)

### Step 3: Detail Refinement

1. **Edge cleanup** - Remove stray pixels, fix jaggies
2. **Shadow consistency** - Ensure shadows align with noir lighting (top-left light source typically)
3. **Pixel clusters** - Group pixels of same color, avoid isolated single pixels
4. **Readability test** - Scale down to game size, verify shapes still clear

### Step 4: Tileset Preparation

For tiles (floor, walls):

1. **Seamless edges** - Ensure opposite edges match exactly
2. **No unique features** - Remove distinctive marks that break tiling
3. **Variation generation** - Create 3-5 variants of each tile type
4. **Metadata naming** - Follow `assets/tiles/[category]/[name].png`

### Step 5: Sprite Preparation

For character/item sprites:

1. **Anchor point** - Set bottom-center anchor (for ground placement)
2. **Shadow baking** - Add hard shadow underneath (unless depth sprite)
3. **Animation frames** - Organize as individual files or spritesheet
4. **Naming convention** - `[name]_[state]_[frame].png`

### Step 6: Import to Godot

1. **Import settings**:
   - Filter: **Nearest** (not Linear)
   - Repeat: **Disabled** (unless tileset)
   - Compress: **Lossless** or **Disabled** (pixel art must be sharp)

2. **Create `*.import` files** - Godot generates these automatically

3. **Test in scene** - Drop PNG into TileSet or Sprite2D, verify sharp rendering

---

## Recommended Tools

### AI Generators (Ranked)

1. **Leonardo.ai** - Best: dedicated pixel art model, style presets, 150 free tokens/day
2. **Midjourney v6** - Excellent artistic quality, understands "pixel art" prompts well, paid
3. **Clipdrop** (Stable Diffusion) - Good free tier, fast, decent pixel results
4. **DALL-E 3** - Accessible via API/ChatGPT, okay but less pixel-aware
5. **Stable Diffusion + LoRA** - Self-host, train on pixel art dataset, free but complex

### Post-Processing Software

1. **Aseprite** ($20) - Industry standard for pixel art, excellent tools, animation
2. **Pyxel Edit** ($9) - Tile-focused, great for tilesets, seamless tiling built-in
3. **GraphicsGale** (free) - Windows, decent animation, old but functional
4. **Piskel** (free online) - Browser-based, okay for simple edits
5. **Krita** (free) with pixel brush packs - Full painting app, pixel mode available

### Pixel Art Converters

- **Pixel It**: https://pixelit.irarezra.com - Quick downscaling with dithering control
- **PixAI.pixelator**: Online pixel art converters
- **Photoshop/GIMP**: Image Size → Nearest Neighbor, Indexed Color

---

## Quick Start Checklist

- [ ] Read core style guidelines (palette, dimensions, lighting)
- [ ] Choose AI tool (Leonardo.ai recommended)
- [ ] Start with Phase 1: Environment tiles
- [ ] Generate 3-5 variations per asset
- [ ] Clean up in Aseprite/Pyxel Edit
- [ ] Test tiles in Godot to verify placement
- [ ] Lock style after first batch
- [ ] Continue through phases systematically
- [ ] Maintain prompt library log (which prompts worked)
- [ ] Import to Godot with Nearest filter
- [ ] Create TileSet resources from tiles

---

## Notes

- **Consistency is key**: Use same seed or reference image when possible
- **Batch processing**: Generate all similar assets in one session to maintain style
- **Document decisions**: Keep notes on which prompts produced best results
- **Iterate**: Don't expect perfection on first try; refine prompts based on outputs
- **Optimize file size**: PNG-8 (indexed color) vs PNG-24; pixel art typically tiny (1-4KB)

---

## Appendix: Asset Priority List

**Critical Path** (needed for playable demo):
1. Floor tiles (at least 3 variants)
2. Wall tiles (north, south, east/west strips, corners)
3. Door tile
4. Monitor console + screen
5. Filing cabinet
6. Coffee station
7. Vern portrait (neutral)
8. Vern sprite (standing)
9. Caller silhouette (male base)
10. Button states (default, hover, pressed)
11. Panel backgrounds
12. On-air indicator
13. Progress bar
14. Scanlines overlay

**Secondary** (enhances experience):
- All Vern mood portraits
- Complete wall variants with windows
- Complete furniture set
- All caller archetypes
- Full UI component library
- Advanced effects (glows, flickers)

**Polish** (nice to have):
- Animation frames for equipment
- VHS effects stack
- Alternative room props
- Decorative items (plants, posters, etc.)

---

*Last updated: 2026-03-08*