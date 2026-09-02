# Topdown Scene Building Pattern - Godot 4

> **Note:** For new room development, see [Room Component Architecture](../AGENTS.md#room-component-architecture) in AGENTS.md. Each room is a self-contained `RoomBase` node (`ControlRoom`, `StudioRoom`) that builds and owns its subtree; the shared components (`common/RoomBase`, `common/WallSystem`, `common/RoomLightingBuilder`, `common/CastShadowSystem`, plus the `props/` files) handle most of this automatically. `WorldRoom` is a thin host that adds the rooms and forwards a small cross-room API.

## Overview

KBTV uses a single **WorldRoom** node that contains multiple room sections (Control Room, Studio, etc.) in one unified world. This document defines the grid, wall, and layering standards for building rooms programmatically in Godot 4 using C#.

## WorldRoom Architecture

KBTV uses a **single world scene** approach where multiple rooms exist under one `WorldRoom` node. Each room is a `RoomBase` child that builds and manages its own grid, walls, lighting, and props.

### Key Concepts

| Concept | Description |
|---------|-------------|
| **WorldRoom** | Thin host node containing all rooms; forwards player assignment, grid translation, shadow access |
| **RoomBase** | Abstract `Node2D` each room extends; creates the floor/door/grid layers and `PropSort`, exposes lifecycle hooks |
| **GridAnchor** | Offset position for each room's grid in world space (`GridOffset`) |
| **TileMapLayer** | Floor, door, and debug layers per room |

### Grid Anchors

Rooms are positioned using grid anchors - world coordinates where each room's grid starts:

```csharp
[ExportGroup("Grid Settings")]
[Export] public Vector2 GridAnchor = new(640, 360);   // World-space top-left of this room's grid
```

Current rooms override this in `ConfigureRoom()`: ControlRoom anchors at `(0, 1000)` (14×10, light mask 1) and StudioRoom at `(0, 776)` (14×6, light mask 2).

### Creating a New Room

1. Create a new room folder `scripts/world/<room>/` containing a `RoomBase` subclass (`<Room>Room.cs`) and a `props/` directory (one file per prop)
2. Override `ConfigureRoom()` (grid anchor, width/height, light mask) and `OnRoomReady()` (walls, shadows, lighting, props, bounds)
3. Add the room as a child of `WorldRoom` in its `_Ready()`
4. Configure exports for grid, doors, lighting, props

### RoomBase Lifecycle Hooks

```csharp
protected override void ConfigureRoom()    // grid facts + light mask (called first)
protected override void OnRoomReady()      // build walls/shadows/lights/props (layers + floor exist)
protected override void OnRoomProcess(double delta)  // per-frame updates (wall visibility, flicker)
```

`RoomBase._Ready()` calls `ConfigureRoom()`, creates and parents `FloorLayer` (z=0, `LightMask`), `DoorLayer` (z=1000), `GridDebugLayer` (hidden) and `PropSort` (YSortEnabled), sets `GridOffset = GridAnchor`, paints the floor, then calls `OnRoomReady()`. `_Process()` forwards to `OnRoomProcess(delta)`.

#### Room Responsibilities

Each room handles:
- **TileMapLayers**: Floor, door, debug layers (created by RoomBase)
- **WallSystem**: Walls, doors, windows setup
- **CastShadowSystem**: Shadow rendering (assign the room's `Shadows` property)
- **Lighting**: CanvasModulate, PointLights (room-specific)
- **Props**: All room props via PropBuilder
- **Debug**: RoomDebug initialization (assign to `DebugNode`) and `ToggleDebug()`
- **Bounds**: self-register with `RoomStateManager` in `OnRoomReady()`

### Adding a Room to WorldRoom

```csharp
public partial class WorldRoom : Node2D
{
    private ControlRoom _controlRoom = null!;

    public override void _Ready()
    {
        _controlRoom = new ControlRoom { Name = "ControlRoom" };
        AddChild(_controlRoom);

        // ...studio, and any future rooms
    }
}
```

### WorldRoom Cross-Room API

WorldRoom keeps the small public surface the rest of the game uses:

```csharp
public CastShadowSystem ControlShadows => _controlRoom.Shadows;
public CastShadowSystem StudioShadows => _studioRoom.Shadows;
public void SetPlayer(CharacterBody2D player);
public Vector2 ControlRoomGridToWorld(Vector2I gridPos);
public Vector2 StudioGridToWorld(Vector2I gridPos);
public Rect2 GetStudioBounds();
```

`CallerScreenerManager` walks `Main/World/WorldRoom` and uses `StudioGridToWorld`/`GetStudioBounds`; `World` uses `SetPlayer`, `ControlShadows` and `StudioShadows`; `WorldRoom._Input` toggles each room's debug overlay on `ui_select`.

## Tile Specs

- **Floor tiles**: 16x16
- **North/South walls**: 16x64 wall faces (4 tiles tall)
- **East/West walls**: 16px wide vertical strips inside a 16x64 tile
- **Top band height (walls)**: 32px
- **Bottom band height (walls)**: 16px

## Bottom-Anchor Rule

- **All sprites use a bottom anchor** (the bottom of the image sits on the tile line).
- **Any sprite taller than 32px extends upward only**.
- **Collisions are placed at the bottom of the sprite** (not centered).

### Tight Base Collider (Auto-Derived)

For oblique/cabinet-projection props, the **visible floor footprint** is the bottom band of the sprite (the base/feet where the prop touches the floor). KBTV uses an alpha-scan helper (`PropBuilder.GetBaseFootprint`) that reads the bottom `floorScanHeight` rows of the sprite's alpha channel and returns a tight bounding box of non-transparent pixels.

The collider is positioned so its bottom edge sits at the sprite's bottom-anchor (the floor). For a pixel at image coords `(px, py)` on a bottom-anchored sprite (`sprite.Position = (0, -textureHeight/2)`), the root-relative offset is `(px - W/2, py - H)`. The collider CENTER is therefore placed at the corresponding center of the footprint:

```
centerX = footprint.minX + footprint.width / 2  - textureWidth / 2
centerY = footprint.minY + footprint.height / 2 - textureHeight
```

See `PropBuilder.FootprintToCollisionCenter` for the reference implementation. The result is that the collider's bottom edge aligns with the prop's visible base — if the sprite has a transparent margin at the bottom, the collider ends slightly above the floor (which matches the topdown 2.5D convention where you can see "under" objects).

**Why tight colliders matter:**

- Player can walk right up to the prop base instead of being blocked by an arbitrary 24×16 box that doesn't match the visual
- The collider matches the visible sprite footprint rather than a hand-tuned guess
- Per-prop tuning is still possible via the `colliderOverride` Vector4 parameter (sprite-local x, y, w, h)

**Usage** (settings are authored in the prop's file, which calls `PropBuilder`):

```csharp
// SpeakerStandsProp.cs — specs own the cell, offset, scan height and collider override
public static PropSpec[] Specs { get; } =
{
    new(new Vector2I(2, 0), Vector2.Zero, FloorScanHeight: 24, ColliderOverride: new Vector4(0, -4, 36, 16)),
    new(new Vector2I(10, 0), Vector2.Zero, FloorScanHeight: 24, ColliderOverride: new Vector4(0, -4, 36, 16)),
};

// RoundTableProp.cs — a custom Create joins the group to PropBuilder
var group = PropBuilder.CreatePropAutoCollider(
    parent, TexturePath, Placement.Cell, Placement.Offset,
    shadowSystem, depthShadowMaterial, roomSection, lightMask,
    createCastShadow: false, floorScanHeight: FloorScanHeight);

// ControlTableGroupProp.cs — desk collider authored inline (size + lift) in CreateTableGroup
var tableShape = new RectangleShape2D { Size = ColliderSize };
```

The room side is always thin: `foreach (var spec in SpeakerStandsProp.Specs) CreateProp(spec, SpeakerStandsProp.TexturePath);`.

**`floorScanHeight` guidelines per prop category:**

| Category | Recommended scan height | Rationale |
|---|---|---|
| Speaker stand, audio cabinet, storage shelf, bookcase | 8–12 px | Captures just the feet/base strip so the player can walk close to the prop |
| Round table, studio table | 20–32 px | Captures the full visible footprint of the horizontal surface |
| Tall standing furniture (filing cabinet, cabinet_tall) | 8–12 px | Same as speaker stand |
| Wall-mounted items (clock, poster) | N/A | Use `CreateProp` with `collidable=false` |

**When to use the `colliderOverride`:**

- Surface-only colliders (e.g. the studio_table's 124×10 surface strip that lets the player walk behind the table)
- Asymmetric collision shapes
- When the alpha scan produces unexpected results (rare — usually the prop sprite is missing a clear base band)

> **Prop-file ownership:** `floorScanHeight`, `colliderOverride`, `createCastShadow` and the anchor
> cell / offset are all **per-prop settings** and must be authored in the prop's own file under
> `props/` (see the *Prop File Ownership Rule* in AGENTS.md), not inline in a room class. A prop's file
> exposes either a `Create(...)` method or a `Specs`/`Placements` array, and rooms just call it.

## Grid Alignment

- **Place walls and props on grid coordinates first** (anchor cell in the prop file).
- **Small pixel offsets are fine** for fine-tuning a prop's landing spot (e.g. shelves raised off
  the floor), but that offset lives in the prop's file, never in the room class.
- When in doubt, adjust the anchor cell or the offset in the prop file.

## Layering Strategy (Player vs Props)

KBTV uses explicit layers instead of full Y-sort for predictable occlusion.

- `PropsBack`: large furniture (tables, speakers, shelves, cabinets)
- `PropsFront`: small tabletop items and chairs
- `Player` is moved between these layers based on a per-object threshold

**Pattern (ControlRoom):**

```csharp
private void UpdatePlayerLayering()
{
    var tableSortY = _tableSortY;
    if (_player.GlobalPosition.Y < tableSortY)
        _player.Reparent(_propsBackRoot);
    else
        _player.Reparent(_propsFrontRoot);
}
```

## Debug Grid Overlay

Use a dedicated debug TileMapLayer with a grid tile for layout verification:

- Tile: `assets/tiles/topdown/grid_debug.png` (16x16)
- Layer: `GridDebugLayer` (set `visible=false` by default)
- Toggle via `WorldRoom._Input` on `ui_select` (calls each room's `ToggleDebug()`)

## Tabletop Offsets

The only approved pixel offsets are for tabletop items to sit on the desk surface:

- `phone_line.png`
- `sound_board.png`
- `computer_station.png`

## Tileset Configuration

- **Tile Shape**: Square
- **Tile Layout**: Square
- **Tile Size**: 16x16
- **Wall origins**: `texture_origin = Vector2i(0, -64)` for 16x64 wall tiles

## Layering Pattern

Use multiple TileMapLayer children for clarity and occlusion behavior:

1. `FloorLayer` (floor tiles)
2. `WallLayer` (north + east + west walls)
3. `SouthWallLayer` (south wall faces)
4. `SouthWallStripLayer` (thin strip when south wall hides)
5. `DoorLayer` (door tile drawn above walls)

## Z-Index Bands

Use 100-point bands to keep ordering predictable and extensible:

- Floor: **0**
- Wall strips: **100**
- Full walls: **150**
- PropsBack: **200**
- Player: **250**
- PropsFront: **300**
- DoorLayer: **400**
- UI/Overlays: **1000**

## Wall Placement Rules

- **North wall**: row `y = -1`
- **South wall**: row `y = _gridHeight - 1`
- **East/West walls**: rows `y = -1` through `y = _gridHeight - 1`
- **Door**: placed on `DoorLayer` at the east wall coordinate (2 tiles high)

## South Wall Hide Behavior

When the player walks behind the south wall, hide the full wall and show the strip:

```csharp
private void UpdateSouthWallVisibility()
{
    var southWallLocal = _floorLayer.MapToLocal(new Vector2I(0, _gridHeight - 1));
    var southWallGlobalY = _floorLayer.ToGlobal(southWallLocal).Y;
    var shouldHide = _player.GlobalPosition.Y < southWallGlobalY - _southWallHideOffset;

    _southWallLayer.Visible = !shouldHide;
    _southWallStripLayer.Visible = shouldHide;
}
```

## Assets & Paths

- **Tileset**: `assets/tiles/topdown/topdown_tileset.tres`
- **Wall atlases**: `assets/tiles/topdown/wall_*_atlas.png`
- **South strip**: `assets/tiles/topdown/wall_south_strip.png`

## Wall Texture System

### Texture Atlas Structure

KBTV uses horizontal atlas textures with 4 frames (left, mid-left, mid-right, right corners):

| Texture | Size | Frames | Purpose |
|---------|------|--------|---------|
| `studio_north_atlas.png` | 128×128 | 4 × 32×32 | Standard wall texture (north/south/east/west) |
| `wall_south_atlas.png` | 128×128 | 4 × 32×32 | South-facing brick wall |
| `wall_west_atlas.png` | 32×32 | 1 | West wall (exterior brick) |
| `wall_east_atlas.png` | 32×32 | 1 | East wall (interior wall) |
| `wall_east_door_atlas.png` | 64×32 | 2 × 32×32 | East door frame |
| `wall_south_strip.png` | 32×32 | 1 | South wall strip (when player hides wall) |

### Standard Wall Pattern

All rooms use the same texture for consistency:

| Wall Direction | Texture | Transform | Notes |
|---------------|---------|----------|-------|
| North | `studio_north_atlas.png` | Default | Standard wall |
| South | `wall_south_atlas.png` | Default | Brick-facing exterior |
| West | `studio_north_atlas.png` | Rotate 90°, Scale Y=0.25 | Brick exterior |
| East | `studio_north_atlas.png` | Rotate 90°, Scale Y=0.25 | Interior wall |

### Setting Wall Textures in Room Setup

```csharp
_wallSystem = new WallSystem
{
    CustomSouthWallTexture = GD.Load<Texture2D>("res://assets/tiles/topdown/wall_south_atlas.png"),
    CustomEastWallTexture = GD.Load<Texture2D>("res://assets/tiles/topdown/studio_north_atlas.png")
};
```

### Door Textures

East doors use sprite-based textures:
- `CustomEastDoorTexture`: Wood door frame (2-frame horizontal atlas)

### Sprite Creation: `CreateRotatedWallSprite`

WallSystem provides a `CreateRotatedWallSprite` helper method for creating properly aligned rotated wall sprites:

```csharp
private Sprite2D CreateRotatedWallSprite(
    Texture2D texture,
    int hFrames,
    Vector2I atlasCoords,
    Vector2I gridCoords,
    WallDirection direction,
    float rotationDegrees)
```

**Key implementation details:**

1. **Offset = (0, 0)**: The sprite's center is used as the transformation origin
2. **Position = grid-to-world coordinate**: The sprite's position matches the tile grid
3. **Scale applied first, then rotation**: Godot's transformation pipeline applies scale before rotation
4. **No manual position adjustments**: Using center offset (0,0) means no arbitrary pixel offsets are needed

**Usage:**
```csharp
// West wall: rotate 90°, scale Y by 0.25 to compress 128px to 32px
var sprite = CreateRotatedWallSprite(southTexture, 4, atlasY, new Vector2I(-1, y), WallDirection.West, 90);

// East wall: same transformation
var sprite = CreateRotatedWallSprite(eastTexture, 4, Vector2I.Zero, gridPos, WallDirection.East, 90);
```

### Why Rotate & Scale?

The studio wall atlas is 128×128 (4 frames × 32px). For east/west walls:
1. **Scale Y by 0.25** → compresses 128px height to 32px (one tile)
2. **Rotate 90°** → reorients the texture for vertical wall visibility

This creates a consistent vertical brick strip that matches the horizontal wall texture style.

**Why offset matters:** When you scale then rotate a sprite, the visual center shifts. Using `Offset = (0, 0)` (sprite center as origin) and positioning via `Position` keeps the sprite visually aligned with grid coordinates without arbitrary adjustments.

## Resolution & Scaling

- Internal viewport: **640x360**
- UI scales with the game (no separate UI viewport)
- Texture filter: **Nearest**

## Example Setup

```
scripts/world/
├── WorldRoom.cs              # Thin host: adds rooms (+ ~65 lines of cross-room API)
├── common/                   # Shared infrastructure
│   ├── IRoomSection.cs
│   ├── RoomBase.cs           # Abstract self-building room: layers, grid, floor, lifecycle hooks
│   ├── WallSystem.cs         # Wall/door/window management
│   ├── CastShadowSystem.cs   # Shadow rendering
│   ├── RoomLightingBuilder.cs
│   ├── RoomDebug.cs
│   ├── PropBuilder.cs        # Static helper for props
│   ├── layout/RoomLayoutTypes.cs
│   └── props/OnAirSignProp.cs
├── control_room/
│   ├── ControlRoom.cs        # Self-contained control room: walls, lighting, props, screening trigger
│   └── props/                # Desk, speaker stands, audio cabinet, shelves, chair
└── studio/
    ├── StudioRoom.cs         # Self-contained studio: walls, lighting, smoke, props
    ├── StudioSmoke.cs
    └── props/                # Bookcases, round table, Vern's chair group
```

The WorldRoom uses the topdown tileset and the layering rules above for wall placement, door visibility, and south wall occlusion. Each `RoomBase` subclass manages its own:
- TileMapLayers (floor, door, debug — created by RoomBase)
- WallSystem (walls, doors, windows)
- Lighting (ceiling, monitor, desk lamp)
- CastShadowSystem (shadow casting, assigned to the room's `Shadows`)
- Props (via PropBuilder)
- Per-frame updates (wall visibility, light flicker) via `OnRoomProcess(delta)`
