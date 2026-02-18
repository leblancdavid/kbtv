# KBTV - Art Style Guide

## Visual Direction

**Pixel Art Style** - Retro 2D aesthetic inspired by classic adventure games

### Project Settings
- **Resolution**: 1280x720 (16:9)
- **Render Mode**: Forward Plus with pixel-perfect texture filtering
- **Tile Size**: 32x32 pixels (base)
- **Texture Filter**: Nearest (no interpolation)

### Aesthetic Goals
- Retro radio station atmosphere
- Dark, moody control room with glowing monitors
- Noir-inspired lighting with neon accents
- VHS/CRT screen effects for monitors

## Color Palette

### Primary Colors
- **Dark Background**: #1a1a2a (deep blue-black)
- **Floor Dark**: #3a3a3a (charcoal)
- **Floor Carpet**: #2d4a2d (dark green)
- **Walls**: #5a4a3a to #6a5a4a (browns)

### Accent Colors
- **Monitor Glow Green**: #00ff44 (bright green)
- **Monitor Glow Red**: #ff4444 (alert red)
- **Neon Accent**: #00ffff (cyan)
- **Warning**: #ffaa00 (amber)

### UI Colors
- **Panel Background**: #2a2a3a
- **Text Primary**: #ffffff
- **Text Secondary**: #aaaaaa
- **Button Normal**: #4a4a5a
- **Button Hover**: #5a5a6a
- **Button Pressed**: #3a3a4a

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

## Next Steps for Asset Generation

1. **Generate pixel art** using AI (Midjourney/DALL-E) with "pixel art" prompt
2. **Convert to pixel art** using Pixel It (pixelit.irarezra.com)
3. **Import to Godot** with nearest-neighbor filtering
4. **Replace placeholders** in ControlRoom.tscn

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
└─────────────┘
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
