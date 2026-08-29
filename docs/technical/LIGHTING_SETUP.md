# KBTV Lighting Specification

## Overview

2D lighting implementation for KBTV radio station, creating a dark nighttime atmosphere while maintaining the pixel art aesthetic. Uses a **hybrid approach**: sprite-based glows for visual effect + one dim light for shadow casting.

## Hybrid Approach

| Component | Type | Purpose |
|-----------|------|---------|
| **Glow Sprites** | Sprite2D with additive blending | Visual "light" effect - colored glows that look like pixel art |
| **Shadow Light** | PointLight2D (very dim) | Casts shadows against walls only |

### Why Hybrid?
- **Pure lights**: Tint everything in the scene color - green light makes everything greenish
- **Pure sprites**: Can't cast shadows
- **Hybrid**: Best of both - nice colored glows + shadow effect

## Global Atmosphere

### CanvasModulate
- **Color**: `#262638` (RGB: 38, 38, 56) - slightly brighter for visibility
- **Purpose**: Darkens entire scene to simulate nighttime
- **Implementation**: Added per-room in WorldRoom (ControlAmbientColor, StudioAmbientColor)

### Multi-Room Lighting

WorldRoom manages lighting separately for each room section:

| Room | Ambient Color | Ceiling Light | Monitor Light | Desk Lamp |
|------|---------------|---------------|---------------|-----------|
| Control Room | #262638 | Yes | Yes | Yes |
| Studio | #333344 | Yes | Yes | Yes |

Each room has its own:
- CanvasModulate for ambient darkness
- PointLight2D ceiling light with shadows
- PointLight2D monitor/desk lamp accent lights
- CastShadowSystem for shadow rendering

## Light Sources

### Phase 1: Core Lights (Implemented - Hybrid)

| Light | Type | Position | Color | Size | Animation |
|-------|------|----------|-------|------|-----------|
| Ceiling Glow | Sprite2D (additive) | Room center | White (#f2e6d9) | 120px | Flicker |
| Monitor Glow | Sprite2D (additive) | Table, center | Green (#00ff44) | 70px | Pulse |
| Desk Lamp Glow | Sprite2D (additive) | Table, corner | Orange (#ffaa44) | 50px | Shimmer |

**Shadow Light** (single dim PointLight2D):
- Energy: 0.15 (very dim)
- Casts shadows from all walls
- Does NOT tint the scene (too dim to matter)

### Light Configuration Details

**Ceiling Glow (Sprite2D)**
- Position: Center of room (grid 7, 5)
- Animation: Subtle flicker (sin wave at 8Hz + 23Hz)
- Uses CanvasItemMaterial with Blend Mode: Add
- Modulates alpha for flicker effect

**Monitor Glow (Sprite2D)**
- Position: On table, above computer station
- Animation: Subtle pulse (sin wave at 2Hz)
- Green color stays green - doesn't tint floor/walls
- Uses CanvasItemMaterial with Blend Mode: Add

**Desk Lamp Glow (Sprite2D)**
- Position: Corner of table
- Animation: Shimmer (sin wave at 3Hz)
- Uses CanvasItemMaterial with Blend Mode: Add

**Shadow Light (PointLight2D)**
- Very dim (energy 0.15) - just enough for shadow calculation
- Shadow only - no visible lighting effect
- Positioned at ceiling

### Phase 2: Extended Lights (Planned)

| Light | Type | Color | Energy | Radius | Shadows |
|-------|------|-------|--------|--------|---------|
| Window Moonlight | DirectionalLight2D | Blue (#4466aa) | 0.3 | N/A | YES |
| ON AIR Sign | PointLight2D | Red (#ff4444) | 0.6 | 40 | NO |
| Phone Indicator | PointLight2D | Cyan (#00ffff) | 0.3 | 30 | NO |
| Equipment LEDs | Sprite2D (additive) | Various | N/A | N/A | NO |

## Shadows

### Implementation

**Wall Occluders**
- Automatically generated in `CreateWallOccluders()`
- Rectangle polygons matching wall tile positions
- Excludes door gap and window columns
- All walls cast shadows

**Shadow Settings**
- Shadow Color: Black at 70% opacity
- Shadow Filter: None (hard pixel-art edges)
- Only ceiling light and monitor cast shadows

### Light Occluder vs Depth Occlusion

| System | Purpose | Node | Used For |
|--------|---------|------|----------|
| Light Occluder (LightOccluder2D) | Block light rays | Shadow casting | Lighting realism |
| Depth Occlusion (Occluder.cs + shader) | Hide objects player walks behind | Sprite visibility | Depth perception |

Both systems coexist - they serve different rendering purposes.

## Color Palette

### Light Colors

| Name | Hex | RGB | Use Case |
|------|-----|-----|----------|
| Monitor Green | #00ff44 | 0, 255, 68 | Primary monitor glow |
| Monitor Red | #ff4444 | 255, 68, 68 | ON AIR indicator |
| Desk Lamp Warm | #ffaa44 | 255, 170, 68 | Table lamp |
| Moonlight Blue | #4466aa | 68, 102, 170 | Windows |
| Phone Cyan | #00ffff | 0, 255, 255 | Caller indicator |
| Ambient White | #e6d9cc | 230, 217, 204 | Ceiling light |
| Dark Base | #1a1a2e | 26, 26, 46 | CanvasModulate |

## Performance Considerations

### Layer Strategy
- All lights use default layer (0)
- Shadow-casting lights: 2 max (ceiling + monitor)
- Non-shadow lights: Unlimited (desk lamp, LEDs)

### Rendering Cost

| Feature | Cost | Recommendation |
|---------|------|----------------|
| CanvasModulate | Free | Always on |
| PointLight2D (no shadow) | Low | Use freely |
| PointLight2D (shadow) | Medium | 2-3 max |
| DirectionalLight2D | Medium | 1-2 max |
| LightOccluder2D | Per-instance | Walls + tall furniture only |

## Implementation Notes

### Creating Glow Sprites (Code - Hybrid Approach)

```csharp
private Sprite2D CreateGlowSprite(Vector2 position, Color color, float size)
{
    // Create gradient texture for the glow
    var gradientTexture = new GradientTexture2D();
    gradientTexture.Set("fill", (int)0); // Radial
    gradientTexture.Set("fill_from", new Vector2(0.5f, 0.5f));
    gradientTexture.Set("fill_to", new Vector2(1f, 1f));
    gradientTexture.Gradient = new Gradient
    {
        Colors = new Color[] { 
            new Color(color.R, color.G, color.B, color.A), 
            new Color(color.R, color.G, color.B, 0) 
        },
        Offsets = new float[] { 0f, 1f }
    };
    gradientTexture.Width = (int)size;
    gradientTexture.Height = (int)size;

    // Create sprite with the gradient texture
    var sprite = new Sprite2D
    {
        Position = position,
        Texture = gradientTexture
    };

    // Create material with additive blending
    var material = new CanvasItemMaterial();
    material.Set("blend_mode", (int)1); // Add mode
    sprite.Material = material;

    return sprite;
}
```

### Animating Glow Sprites

```csharp
// In _Process(double delta)
_flickerTime += (float)delta;

// Flicker animation
var flicker = 0.4f + Mathf.Sin(_flickerTime * 8f) * 0.03f;
var baseColor = new Color(0.95f, 0.9f, 0.85f, flicker);
_ceilingGlow.Modulate = baseColor;

// Pulse animation
var pulse = 0.5f + Mathf.Sin(_flickerTime * 2f) * 0.08f;
var greenColor = new Color(0f, 1f, 0.27f, pulse);
_monitorGlow.Modulate = greenColor;
```

### Creating Light Occluders (Code)

```csharp
var occluder = new LightOccluder2D { Position = wallPosition };
var polygon = new OccluderPolygon2D
{
    Polygon = new Vector2[]
    {
        new Vector2(-width * 0.5f, -height * 0.5f),
        new Vector2(width * 0.5f, -height * 0.5f),
        new Vector2(width * 0.5f, height * 0.5f),
        new Vector2(-width * 0.5f, height * 0.5f)
    },
    CullMode = OccluderPolygon2D.CullModeEnum.Disabled
};
occluder.Occluder = polygon;
parent.AddChild(occluder);
```

## Critical: Light2D Z-Range vs the Manual Y-Sort

**Symptom:** A `PointLight2D` sits correctly in the middle of a room (its position is right, and the
debug overlay draws the dot there) yet the **player is not illuminated**, while the *same* player
lights up normally when entering a neighboring room. Reads like "the light is in a different room"
or "the light's z is under the floor."

### Root Cause

KBTV uses a **manual y-sort**: `Player._Process()` sets `ZIndex = (int)GlobalPosition.Y` every
frame (see `ZINDEX_Y_SORT_PATTERN.md`), and static props do the same once. In a multi-room world
the player's `ZIndex` is therefore large in absolute terms (e.g. ~1240 in the control room, whose
grid anchor sits at world Y 1000).

A `Light2D` only illuminates canvas items whose z falls inside its `range_z_min` / `range_z_max`.
**These are relative to the light's own z** (including the parent chain). The default range is
`±1024`, so a light parked on a low-z node (e.g. a `WorldRoom` at z 0) with `ZIndex = 10` has an
effective ceiling around z **1034** — *below* the y-sorted player at z ~1240. The player is sorted
into a z-band the light never reaches, so the light "misses" them even though they're physically
underneath it.

It looked room-dependent because the two rooms set z differently:
- **Studio** (`RoomBase`-based): `RoomBase._Ready()` sets the room `ZIndex = 1001` with
  `ZAsRelative = false` on every layer. The studio ceiling light (ZIndex 10, relative) lands at
  effective z ~1011, so the default `±1024` range happens to cover the player (~800-900). It worked
  by luck of the z offset.
- **Control room** (`WorldRoom` + `IRoomSection`): built into a plain Node2D at z 0, so the light
  sits at effective z 10 and the default range misses the high-z player.

### Fix

Give room lights a wide z-range in the shared factory so they illuminate the y-sorted player/props
regardless of how high `ZIndex` climbs. In `RoomLightingBuilder.MakeLight` (used by ceiling,
monitor, desk, and on-air lights):

```csharp
var light = new PointLight2D
{
    /* ... */
    RangeZMin = -LightZRange,
    RangeZMax = LightZRange,   // LightZRange = 4096
};
```

`RangeZMin` must also be negative (not 0): the floor can sit *below* the light's z (e.g. studio
floor at 1001 vs light at 1011), so a min of 0 would stop the light reaching the floor.

Rooms still don't leak into each other because each room's light has its own `light_mask`
(control = 1, studio = 2) which culls items by mask in addition to z-range.

### Lesson / Gotcha

- **Reach ≠ brightness.** When a `PointLight2D.texture` is set, the `range` property is ignored;
  reach is `texture_size × texture_scale`. Don't inflate `texture_scale` to make a light "reach"
  a dark player — the real culprit may be z-range culling, and inflating scale just over-brightens
  the whole room. (This is exactly what happened: a `2.4` texture scale was added chasing a "reach"
  bug that was actually a z-range bug, and it made the room too bright once the z-range was fixed.)
- A dim player under a visible, well-positioned light is a **z-range** problem, not a position or
  reach problem.

### Files

- `scripts/world/common/RoomLightingBuilder.cs` — `MakeLight` sets `RangeZMin`/`RangeZMax`
  (`LightZRange = 4096`); `OvalGradient` produces the light texture.
- `scripts/player/Player.cs:227` — `ZIndex = (int)GlobalPosition.Y` (the high-z source).
- `scripts/world/common/RoomBase.cs:111` — room `ZIndex = 1001`, `ZAsRelative = false` (why the
  studio z-band was higher).

## Critical: Lighting Initialization Order

**IMPORTANT**: When setting up rooms programmatically, lights MUST be created before shadows are initialized. This is handled automatically by WorldRoom's `_Ready()` method:

```csharp
public override void _Ready()
{
    CreateTileMapLayers();
    CreateLighting();    // MUST be before CreateSystems()
    CreateSystems();     // Shadows initialized with valid light refs
    InitializeDebug();
    CreateProps();
}
```

If shadows are initialized with null light references:
- Shadow system silently skips all updates
- Props appear "too bright" (no shadow modulation)
- Shadow angle calculations fail

The fix (commit `b96d6c4d`) ensures lights exist before `CastShadowSystem.Initialize()` is called.

## Future Enhancements

### Animations
- Phone glow pulse when callers waiting
- ON AIR sign flicker on show start
- Equipment LED patterns (scanning, recording)

### Additional Lights (Phase 3)
- Kitchen area light (coffee maker glow)
- Storage room lights
- Parking lot exterior lights

### Advanced Effects
- Light intensity changes based on show events
- Emergency lighting during "events"
- Caller disconnection "phone line dead" effect

## File Changes

### Modified
- `scripts/world/ControlRoom.cs` - Added CreateLighting(), CreateGlowSprite(), CreateWallOccluders()
- `scripts/world/WorldRoom.cs` - Multi-room lighting with per-room CanvasModulate and PointLight2D
- `scripts/world/CastShadowSystem.cs` - Shadow rendering tied to ceiling light

### Created
- `docs/technical/LIGHTING_SETUP.md` - This specification

## Current Implementation Summary

- **WorldRoom**: Single scene with multiple rooms (Control Room, Studio)
- **CanvasModulate**: Per-room ambient darkening (#262638 control, #333344 studio)
- **Ceiling Lights**: PointLight2D with shadows enabled, positioned at room center
- **Monitor/Desk Lamp Lights**: Accent PointLight2D for local illumination
- **CastShadowSystem**: Dynamic shadows based on ceiling light position
- **Wall Occluders**: LightOccluder2D nodes on all walls
- **Result**: Multi-room dark atmosphere with room-specific lighting + shadow effects

## Available Glow Textures

Located in `assets/lighting/`:

| Texture | Shape | Size | Use Case |
|---------|-------|------|----------|
| `glow_soft_*.png` | Circular soft | 32-256px | Ambient/ceiling lights |
| `glow_tight_*.png` | Circular tight | 32-64px | Focused lights (monitors) |
| `glow_oval_*.png` | Oval | various | Perspective lights (desk lamps) |
| `glow_cone_*.png` | Cone/dome | 96-144px | Overhead fixtures |
| `glow_tiny_*.png` | Tiny | 16-24px | LED indicators |

## Generating More Glow Textures

Run the Python script to generate additional glow textures:

```bash
python generate_glow_textures.py
```

Edit `generate_glow_textures.py` to add more sizes/shapes as needed.
