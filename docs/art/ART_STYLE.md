# KBTV - Art Style Guide

## Visual Direction

**Pixel Art Style** - Retro 2D aesthetic with a **late-night noir** overlay. The radio station should feel like a 3 AM broadcast: dim, desaturated, lit by screens and the warm red of an on-air sign.

### Project Settings
- **Resolution**: 1280x720 (16:9), stretched from 640x360 internal
- **Render Mode**: Forward Plus with pixel-perfect texture filtering
- **Tile Size**: 16x16 pixels (base) — walls render as 16x64 strips
- **Texture Filter**: Nearest (no interpolation)

### Aesthetic Goals
- Late-night radio station atmosphere — 3 AM broadcast feel
- **High contrast, low saturation** — the world reads cool and dark, screens and the on-air sign are the only warm punctuation
- Single dominant light source per room (ceiling light + monitor glow); everything else is in shadow
- Noir post-processing pass (desat 55%, vignette, animated grain, faint scanlines) handled by `shaders/noir_post.gdshader` and `scenes/NoirPost.tscn`
- "Light comes from screens" rule: warm rim light on characters/surfaces only when a CRT or LED is nearby

## Color Palette (noir-restricted)

Cap each asset at **8–10 colors**. No fully saturated primaries — everything gets pulled toward charcoal/oxblood/cool phosphor.

### Primary (cool, dark)
- **Void**: `#0d0d12` (near-black, deepest shadow)
- **Charcoal**: `#1f1f26` (room ambient)
- **Cool Shadow**: `#2a2d36` (unlit walls)
- **Slate**: `#3a3d48` (lit wall surface)

### Secondary (warm muted)
- **Warm Shadow**: `#2a221b` (wood shadow side)
- **Wood Mid**: `#5a5340` (desaturated khaki/tan, wood/desk surfaces)
- **Wood Highlight**: `#7a6f55` (wood lit side)

### Accents (rare, high contrast)
- **Cool Phosphor Green**: `#3a8a78` (CRT screen glow — NOT `#00ff44`)
- **Noir Red**: `#a23a3a` (on-air sign, alert LED — NOT `#ff4444`)
- **Cool Cyan**: `#5fa8a8` (occasional UI accent)
- **Warm Rim**: `#c89a5a` (faint warm rim light from CRT on nearby surfaces)

### UI Colors
- **Panel Background**: `#1a1a22` (matches room ambient)
- **Panel Border**: `#3a3d48`
- **Text Primary**: `#e8e8e8` (slightly off-white, never pure)
- **Text Secondary**: `#8a8a90`
- **Button Normal**: `#2a2d36`
- **Button Hover**: `#3a3d48`
- **Button Pressed**: `#1a1a22`
- **CRT Accent**: `#3a8a78`

## Character Design

### Vern (Radio Host)
- **Style**: Pixel portrait with limited animation frames
- **Palette**: Warm tones (browns, oranges) to contrast cool environment
- **Expression Range**: 4-6 mood variants per conversation

### Caller Silhouettes
- Simple placeholder shapes until voice audio is implemented

## Environment Art

### Control Room (Starting Location)
- **Floor**: Dark carpet tiles with subtle variation
- **Walls**: Top/mid/bottom wall segments with corner variants
- **Furniture**:
  - Monitor console with 3+ screens (animated glow)
  - Phone bank for screening callers
  - Filing cabinets for evidence
  - Wall clock
  - Coffee station
- **Lighting**: Dim ambient with monitor glow as primary light source

### Studio (Locked during broadcasts)
- Where Vern broadcasts from
- Player interacts via door drop-offs
- Microphone, soundboard, Dead Air indicator

### Kitchen/Dining Area
- Coffee machine, refrigerator
- Snack supplies for Vern

### Other Locations (Future)
- Parking Lot
- Equipment Room
- Main Office

---

## Prop Visual Style — Complete Specification

This section is the **single source of truth** for prop art. Follow it exactly when generating, reviewing, or rejecting KBTV props.

### Style Anchor Assets (the reference set)

Every prop must visually match these three existing assets:
- `assets/tiles/props/cabinet_tall.png` (32×64, tall metal cabinet)
- `assets/tiles/props/storage_shelf.png` (64×64, shelf with storage boxes)
- `assets/tiles/props/filing_cabinet.png` (32×32, short filing cabinet)

When in doubt, **look at these three sprites first**. They define the visual language.

### Style: Oblique / Cabinet Projection

The defining style for KBTV furniture props is **cabinet projection** (also called **oblique projection** or **2.5D / Stardew Valley-style**). The core rule:

> **The front face stays perfectly flat** (only horizontal and vertical edges — no perspective distortion). Depth is implied by diagonal lines receding BACK from the front face corners at **45°**, foreshortened to **1/2 scale**.

This is **different from isometric** (where all 3 axes are angled 30° equally). Oblique keeps the front face honest; isometric rotates everything.

```
  ISOMETRIC (wrong)            OBLIQUE/CABINET (correct)
  ─ all axes 30°               ─ front face flat (H+V only)
  ─ 3 equal angled faces       ─ depth lines 45° going back at 1/2 scale
                               ─ top face = skewed rectangle (parallelogram)

       ╱╲                          ┌─────────┐  ← front face
      ╱  ╲                         │         │     (flat H+V)
     ╱    ╲                        │         │
    ╱      ╲                       │         │
   ╱        ╲                      └─────────┘
   ╲        ╱                          ╱   ╱  ← 45° depth
    ╲      ╱                          ╱   ╱
     ╲    �                          ╱   ╱
      ╲  ╱                          ╱   ╱
       ╲╱
   (cube rotates                  (cube has flat front
    in all 3 dims)                 + depth going back)
```

### Style Rules (the 13 commandments)

Every KBTV prop must satisfy ALL of these:

1. **Front face MUST be flat** — only horizontal/vertical edges on the face that points at the camera. No diagonals.
2. **All depth lines at 45°** — no random angles, no isometric 30° depth.
3. **Depth at 1/2 scale** — receding edges are about half the height of the front face.
4. **No flat top-down plan view** — tall props show both a top face and a side face.
5. **No head-on portrait view** — props have visible depth, never just a flat front.
6. **No smooth gradients** — pixel art, hard edges, dithered shadows only if absolutely needed.
7. **One outline color, silhouette only** — charcoal `#1f1f26` outlines on the silhouette, no outlines inside outlines.
8. **Same lighting direction across the set** — light comes from above-front-left (top face brightest, right side shadowed).
9. **Same scale across the set** — same outline weight, same shading style whether the prop is 16×16 or 64×64.
10. **Fill the canvas** — subject occupies 90-100% of the sprite box. The silhouette touches (or comes within 1-2 pixels of) the edges. No tiny subject floating in transparent padding.
11. **NO cast shadows in the sprite** — no ground shadow, no drop shadow, no contact shadow, no oval blob, no ambient occlusion underneath. The KBTV render pipeline adds its own dynamic shadows via `CastShadowSystem`; baked-in shadows double-render and look wrong. The sprite shows ONLY the prop on a transparent background.
12. **NO baked-in highlights/gradients below the prop** — the bottom edge of the prop is the bottom edge of the prop, not a shadow fade.
13. **Same lighting + same palette across the set** — every prop draws from the KBTV noir palette below; mismatched palettes break the cohesive look.

### Wall-Mounted & Tabletop Exceptions

Some prop categories are exempt from the oblique rule:

| Category | View | Examples |
|----------|------|----------|
| **Tall / standing furniture** | OBLIQUE (flat front + 45° depth) | cabinet, shelf, desk, table, audio rack, computer station |
| **Wall-mounted items** | Flat face-on is fine (they hang on walls) | poster, wall clock, on-air sign |
| **Small tabletop items** | Flat face-on or slight oblique | phone, coffee mug, papers, ashtray |
| **Free-standing silhouette props** | Side or 3/4 view OK | boom mic on stand |

### Pixel Grid Standards

- All props snap to a 4-pixel grid minimum (no sub-4px details)
- Lines are 1-2 pixels wide
- Highlights/shadows are 1-3 pixel bands
- Outline weight = 1 pixel for small props (≤32px), 1-2 pixels for larger props
- Depth lines: 4-8 pixels long on a 32px-tall prop (1/2 scale)

### Rejection Criteria (regenerate if any apply)

Reject and regenerate if the result has:
- **Front face has diagonals** — the face pointing at the camera must be flat
- **Depth lines are not at ~45°** — reject 30° (isometric) or random angles
- **Prop is rendered flat top-down** (no front face visible)
- **Prop is rendered as a head-on portrait** (no depth visible)
- **Prop is rotated/lying on its side**
- **Aspect ratio looks squashed or stretched** (subject doesn't fill canvas)
- **Colors include any saturated primary** (red `#ff0000`, green `#00ff00`, blue `#0000ff`)
- **Prop has blurry or anti-aliased edges**
- **Prop doesn't show the noir palette** (warm wood + cool charcoal + phosphor green accent)
- **Prop has more than 3 distinguishing colors competing for attention**
- **Any baked-in shadow** (ground shadow, drop shadow, contact shadow, oval blob, ambient occlusion below)

---

## Prop Category-Specific Guidelines

Different prop categories need slightly different prompts and treatment. Use the right category template from `PIXELLAB_MCP_GUIDE.md` §"KBTV Prompt Templates by Prop Type".

### Cabinets & Shelves (oblique — flat front + 45° depth)

- Use `view: "side"` in `pixellab_create_image_pixflux`
- Standard oblique prompt template
- Front face dominates; depth on right side
- Examples: `cabinet_tall.png`, `storage_shelf.png`, `filing_cabinet.png`, `audio_cabinet.png`

### Tables (different from cabinets!)

Tables are **horizontally-oriented** — they need a visible **TOP SURFACE** where items can sit, not just a vertical front face. This is the single most common mistake when generating KBTV tables.

**Required treatment:**
- **Use `view: "high top-down"` in `pixellab_create_image_pixflux`** (NOT `"side"`)
- The TOP SURFACE occupies the upper portion of the sprite as a clearly visible flat plane
- The FRONT FACE (table apron/bulk) is below the top surface — flat with only H+V edges
- The LEGS are short stubby verticals at the corners — about **20-24px tall** (roughly half the player's 48px height)
- **Short legs are critical** — long legs make tables look like they're floating high above the floor
- Material: dark charcoal monochrome (NOT wood for studio tables; wood is OK for homey props)

**Prompt keywords for tables:**
```
{subject}, view from slightly above showing both TOP SURFACE and FRONT FACE clearly, 
the wide flat top takes up the top portion as a flat plane, narrow front face below, 
four charcoal metal legs at corners, NO wood texture for studio/professional tables, 
dark charcoal monochrome metal, cabinet projection, oblique projection, 2.5D pixel art, 
flat front face, 45 degree depth lines, dark noir palette, transparent background, 
NO shadows, NO ground shadow, NO drop shadow, NO contact shadow, charcoal black outlines, 
crisp pixels, no anti-aliasing, pixel art
```

### Computer Stations & CRT Items

- Show the CRT monitor with phosphor green screen prominently
- Include keyboard, mouse, and desktop tower as visible elements
- The computer_station at 32×32 is small — every pixel counts
- Avoid wood textures for these (dark gray/charcoal preferred)

### Audio Equipment (tall racks)

- Show TWO COLUMNS of audio devices side by side (mixer, equalizer, tape deck, amplifier)
- Phosphor green LEDs visible on the equipment
- Rack chassis with vent slots at top, rack ears at corners
- The 32×56 aspect ratio is correct for a tall rack

### Wall-Mounted Items

- Can be flat face-on (don't need oblique depth)
- Must still obey the "no shadows" and "fill canvas" rules
- Poster: dark frame with noir eye/conspiracy art
- Clock: wall clock face with roman numerals

### Tabletop Item Positioning (for code, not art)

When placing items on a table sprite, the math:
- Sprite2D position is the sprite's CENTER (Godot default)
- For a sprite with size W×H, position `(x, y)` puts the sprite's center at world `(x, y)`, extending from `(x-W/2, y-H/2)` to `(x+W/2, y+H/2)`
- The table sprite is bottom-anchored: `Position = (0, -textureHeight/2)` — the bottom of the sprite sits at the group origin (grid cell)
- Items drawn AFTER the table in the same group render ON TOP of the table
- For items to APPEAR to sit on the visible top surface, their visual bottoms must align with the bottom edge of the visible top surface in the sprite

**Item size awareness** (KBTV's actual current sizes):
| Item | Dimensions |
|------|-----------|
| `phone_line.png` | 24×16 |
| `sound_board.png` | 28×16 |
| `computer_station.png` | 32×32 |
| `boom_mic.png` | 24×24 |

Items are NOT all 32×32! Compute offsets based on each item's actual height:
- 32-tall item: `offset.y = tableTopSurfaceY - 16` (so visual bottom at tableTopSurfaceY)
- 24-tall item: `offset.y = tableTopSurfaceY - 12`
- 16-tall item: `offset.y = tableTopSurfaceY - 8`

**Why items on the same table need different Y offsets** — they have different heights but should all visually "sit" on the same surface plane.

---

## Asset Specifications

### File Format
- **Images**: PNG with alpha channel
- **Spritesheets**: For animated elements

### Naming Conventions
```
assets/
├── tiles/
│   ├── floor/
│   │   ├── floor_dark.png
│   │   ├── floor_light.png
│   │   └── floor_carpet.png
│   ├── walls/
│   │   ├── wall_top.png
│   │   ├── wall_mid.png
│   │   ├── wall_bottom.png
│   │   └── wall_corner.png
│   └── props/
│       ├── desk.png
│       ├── monitor.png
│       └── ...
├── sprites/
│   ├── characters/
│   │   └── player_placeholder.png
│   └── furniture/
└── backgrounds/
```

### TileSet Structure
- **Layer 0**: Floor (walkable)
- **Layer 1**: Walls (collision)
- **Layer 2**: Details (decorative, no collision)

## Animation Guidelines

- **Frame Rate**: 8-12 FPS for retro feel
- **Monitor Flicker**: Subtle brightness oscillation
- **Player Movement**: 4-directional with smooth interpolation
- **UI Feedback**: Instant response (no long animations)

## Godot 4.x Implementation Notes

### project.godot Settings
```toml
[display]
window/size/viewport_width=1280
window/size/viewport_height=720
window/size/mode=2
window/stretch/mode="viewport"

[rendering]
textures/canvas_textures/default_texture_filter=0
```

### TileMapLayers (Godot 4.3+)
- Use `TileMapLayer` nodes instead of single `TileMap` with layers
- Each TileMapLayer is a separate node with its own TileSet
- Layers: FloorLayer (walkable), WallsLayer (collision), DetailsLayer (decorative)
- Enable `y_sort_enabled` for proper sprite rendering order

### Camera2D
- No "Make Current" checkbox in Godot 4.x
- First Camera2D in scene tree is automatically the active camera

### Scene Structure
```
ControlRoom (Node2D)
├── FloorLayer (TileMapLayer)
├── WallsLayer (TileMapLayer)  
├── DetailsLayer (TileMapLayer)
├── Player (CharacterBody2D)
│   ├── Sprite2D
│   ├── CollisionShape2D
│   └── Camera2D
└── [Props as Sprite2D nodes]
```

### Import Files
- PNG files need `.import` metadata files in the same folder
- Delete `.godot` folder to force clean re-import
- Godot auto-generates UIDs for resources

## Pseudo-Isometric Implementation

### Overview
The game uses a **top-down with depth** visual style that achieves pseudo-isometric occlusion:
- Objects have height drawn as "front face" + "top face"
- When player walks behind tall objects, they fade out smoothly
- This creates the illusion of 3D space in 2D

### Depth Sprite Format
Each sprite has two visual components:
- **Top face** (lighter color): Shows the surface facing up
- **Front face** (darker color): Shows the wall/object facing the viewer

```
┌─────────────┐  ← Top (lighter)
│             │
│             │
├─────────────┤  ← Front (darker)
│             │
└─────────────�
```

### Occlusion System

**Shader** (`shaders/occlusion.gdshader`):
- Calculates alpha based on player position vs object height
- Smooth fade when player Y < object Y + height

**Occluder Component** (`scripts/components/Occluder.cs`):
- Attaches to Sprite2D nodes
- Configurable `Height` property (pixels)
- Configurable `FadeRange` for transition smoothness

**Usage**:
1. Attach Occluder script to any Sprite2D
2. Set `Height` based on object tallness:
   - Walls: 48px
   - Tall furniture: 40-64px
   - Small items: 0px (never occlude)

### Wall Sprite Naming
- `wall_depth_top.png` - Cap piece (16px tall)
- `wall_depth_mid.png` - Tall section (48px tall)  
- `wall_depth_bottom.png` - Base section (32px tall)
- `wall_depth_corner.png` - Corner piece (48px tall)

### Prop Sprite Naming
- `*_depth.png` - Sprites with depth for occluding objects
- No `_depth` suffix - Sprites that don't occlude (small items)
