# Topdown Scene Building Pattern - Godot 4

> **Note:** For new room development, see [Room Component Architecture](../AGENTS.md#room-component-architecture) in AGENTS.md. The components (`RoomBase`, `WallSystem`, `RoomLighting`, `CastShadowSystem`) handle most of this automatically.

## Overview

KBTV uses a single **WorldRoom** scene that contains multiple room sections (Control Room, Studio, etc.) in one unified world. This document defines the grid, wall, and layering standards for building rooms programmatically in Godot 4 using C#.

## WorldRoom Architecture

KBTV uses a **single world scene** approach where multiple rooms exist in one `WorldRoom` scene. Each room is a `RoomSection` child that manages its own grid, walls, lighting, and props.

### Key Concepts

| Concept | Description |
|---------|-------------|
| **WorldRoom** | Single scene containing all rooms |
| **RoomSection** | Manages one room's grid, walls, lighting |
| **GridAnchor** | Offset position for each room's grid in world space |
| **TileMapLayer** | Floor, door, and debug layers per room |

### Grid Anchors

Rooms are positioned using grid anchors - world coordinates where each room's grid starts:

```csharp
[ExportGroup("Grid Settings")]
[Export] public Vector2 ControlRoomGridAnchor = new(0, 0);    // Origin
[Export] public Vector2 StudioGridAnchor = new(0, -160);      // 160px above (Y increases down)
```

### Creating a New Room

1. Create a new RoomBuilder class in `scripts/world/builders/`
2. Implement `IRoomBuilder` interface
3. Add the builder to WorldRoom
4. Configure exports for grid, lighting, props

### Room Builder Pattern

KBTV uses the **Room Builder pattern** to encapsulate each room's logic. Each room has its own builder class that handles all setup:

```
scripts/world/
├── WorldRoom.cs              # Orchestrator - delegates to builders
├── builders/
│   ├── IRoomBuilder.cs       # Interface all builders implement
│   ├── ControlRoomBuilder.cs # Control room specific logic
│   └── StudioBuilder.cs      # Studio specific logic
```

#### IRoomBuilder Interface

```csharp
public interface IRoomBuilder
{
    void Build(WorldRoom world);
    void SetPlayer(CharacterBody2D player);
    Vector2 GridToWorld(Vector2I gridPos);
    CastShadowSystem GetShadows();
}
```

#### Creating a New Room Builder

1. Create a new class extending the pattern
2. Add exports for all room configuration (grid, doors, lighting, props)
3. Implement Build(), SetPlayer(), GridToWorld(), GetShadows(), Update(), ToggleDebug()

```csharp
public partial class MyRoomBuilder : IRoomBuilder
{
    [ExportGroup("Grid Settings")]
    [Export] public Vector2 GridAnchor = new(0, -320);
    [Export] public int GridWidth = 14;
    [Export] public int GridHeight = 8;

    // Add exports for doors, windows, lighting, props...

    public void Build(WorldRoom world)
    {
        // 1. Create TileMapLayers
        // 2. Create RoomSection
        // 3. Create WallSystem
        // 4. Create CastShadowSystem
        // 5. Create lighting
        // 6. Create props
    }

    // Implement other interface members...
}
```

#### Adding a Room to WorldRoom

```csharp
public partial class WorldRoom : Node2D
{
    private MyRoomBuilder _myRoomBuilder = null!;

    public override void _Ready()
    {
        PropSort = new Node2D { Name = "PropSort" };
        PropSort.YSortEnabled = true;
        AddChild(PropSort);

        _myRoomBuilder = new MyRoomBuilder();
        _myRoomBuilder.Build(this);
    }

    public override void _Process(double delta)
    {
        _myRoomBuilder.Update(this, delta);
    }
}
```

#### Room Builder Responsibilities

Each builder handles:
- **TileMapLayers**: Floor, door, debug layers
- **WallSystem**: Walls, doors, windows setup
- **CastShadowSystem**: Shadow rendering
- **Lighting**: CanvasModulate, PointLights (room-specific)
- **Props**: All room props via PropBuilder
- **Debug**: RoomDebug initialization and toggling

### Room Section Setup

```csharp
private RoomSection CreateRoomSection(Vector2 gridAnchor, int width, int height)
{
    var section = new RoomSection
    {
        GridAnchor = gridAnchor,
        GridWidth = width,
        GridHeight = height
    };
    AddChild(section);
    return section;
}
```

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

**Usage:**

```csharp
// Default: auto-derive from bottom 16px band
PropBuilder.CreatePropAutoCollider(_propSort, "res://assets/tiles/props/speaker_stand.png",
    new Vector2I(2, 0), Vector2.Zero,
    _shadows, _shadows.DepthShadowMaterial, _section, LightMask,
    floorScanHeight: 12);

// Tables need a taller scan band to capture the full top surface
PropBuilder.CreatePropAutoCollider(_propSort, "res://assets/tiles/props/round_table.png",
    new Vector2I(6, 1), Vector2.Zero,
    _shadows, _shadows.DepthShadowMaterial, _section, LightMask,
    floorScanHeight: 24);

// Custom override (e.g. surface-only collider for a walk-behind table)
PropBuilder.CreatePropAutoCollider(_propSort, "res://assets/tiles/props/studio_table.png",
    new Vector2I(6, 1), Vector2.Zero,
    _shadows, _shadows.DepthShadowMaterial, _section, LightMask,
    colliderOverride: new Vector4(-62, -5, 124, 10));
```

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

## Grid Alignment

- **Place walls and props on grid coordinates only**.
- **Avoid per-prop pixel offsets** except for tabletop items that must sit on a surface.
- When in doubt, adjust grid coordinates, not pixel offsets.

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
- Toggle in `ControlRoom` using `ui_select`

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

## Resolution & Scaling

- Internal viewport: **640x360**
- UI scales with the game (no separate UI viewport)
- Texture filter: **Nearest**

## Example Setup

```
scripts/world/
├── WorldRoom.tscn            # Single scene with all rooms
├── WorldRoom.cs              # Main world coordinator (~65 lines)
├── RoomSection.cs            # Individual room grid manager
├── builders/
│   ├── IRoomBuilder.cs       # Interface for room builders
│   ├── ControlRoomBuilder.cs # Control room logic
│   └── StudioBuilder.cs      # Studio logic
├── WallSystem.cs            # Wall/door/window management
├── CastShadowSystem.cs      # Shadow rendering
├── PropBuilder.cs           # Static helper for props
└── ControlRoom.cs          # Legacy standalone room (kept for testing)
└── StudioRoom.cs           # Legacy standalone room (kept for testing)
```

The WorldRoom uses the topdown tileset and the layering rules above for wall placement, door visibility, and south wall occlusion. Each room builder manages its own:
- TileMapLayers (floor, door, debug)
- WallSystem (walls, doors, windows)
- Lighting (ceiling, monitor, desk lamp)
- CastShadowSystem (shadow casting)
- Props (via PropBuilder)
