# Topdown Scene Building Pattern - Godot 4

> **Note:** For new room development, see [Room Component Architecture](../AGENTS.md#room-component-architecture) in AGENTS.md. The shared components (`common/RoomBase`, `common/WallSystem`, `common/RoomLightingBuilder`, `common/CastShadowSystem`, plus the per-room builders and `props/` files) handle most of this automatically.

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

1. Create a new room folder `scripts/world/<room>/` containing a builder (`<Room>Builder.cs`), a layout (`<Room>Layout.cs`) and a `props/` directory
2. Implement `IRoomBuilder` interface
3. Add the builder to WorldRoom
4. Configure exports for grid, lighting, props

### Room Builder Pattern

KBTV uses the **Room Builder pattern** to encapsulate each room's logic. Each room has its own builder class that handles all setup, with shared infrastructure in `common/` and one file per prop:

```
scripts/world/
├── common/                          # Shared infrastructure
│   ├── IRoomBuilder.cs              # Interface all builders implement
│   ├── IRoomSection.cs
│   ├── RoomBase.cs
│   ├── RoomSection.cs
│   ├── WallSystem.cs
│   ├── CastShadowSystem.cs
│   ├── RoomLightingBuilder.cs
│   ├── RoomDebug.cs
│   ├── PropBuilder.cs
│   ├── layout/RoomLayoutTypes.cs    # GridPlacement, PropSpec, BoardSpec
│   └── props/OnAirSignProp.cs       # Shared prop (ON AIR sign)
├── control_room/
│   ├── ControlRoomBuilder.cs        # Control room specific logic
│   ├── ControlRoomLayout.cs         # Room-level facts (grid, ceiling light, sign tuning)
│   └── props/                       # One file per prop (desk, stands, cabinet, shelves, chair)
└── studio/
    ├── StudioBuilder.cs             # Studio specific logic
    ├── StudioLayout.cs
    ├── StudioSmoke.cs               # Ambient smoke effect
    └── props/                       # One file per prop (bookcases, round table, Vern's chair)
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

The builder side is always thin: `foreach (var spec in SpeakerStandsProp.Specs) CreateProp(spec, SpeakerStandsProp.TexturePath);`.

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
> `props/` (see the *Prop File Ownership Rule* in AGENTS.md), not inline in a builder. A prop's file
> exposes either a `Create(...)` method or a `Specs`/`Placements` array, and builders just call it.

## Grid Alignment

- **Place walls and props on grid coordinates first** (anchor cell in the prop file).
- **Small pixel offsets are fine** for fine-tuning a prop's landing spot (e.g. shelves raised off
  the floor), but that offset lives in the prop's file, never in the builder or layout.
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
├── common/                   # Shared infrastructure
│   ├── IRoomBuilder.cs       # Interface for room builders
│   ├── IRoomSection.cs
│   ├── RoomBase.cs
│   ├── RoomSection.cs        # Individual room grid manager
│   ├── WallSystem.cs         # Wall/door/window management
│   ├── CastShadowSystem.cs   # Shadow rendering
│   ├── RoomLightingBuilder.cs
│   ├── RoomDebug.cs
│   ├── PropBuilder.cs        # Static helper for props
│   ├── layout/RoomLayoutTypes.cs
│   └── props/OnAirSignProp.cs
├── control_room/
│   ├── ControlRoomBuilder.cs # Control room logic
│   ├── ControlRoomLayout.cs
│   └── props/                # Desk, speaker stands, audio cabinet, shelves, chair
└── studio/
    ├── StudioBuilder.cs      # Studio logic
    ├── StudioLayout.cs
    ├── StudioSmoke.cs
    └── props/                # Bookcases, round table, Vern's chair group
```

The WorldRoom uses the topdown tileset and the layering rules above for wall placement, door visibility, and south wall occlusion. Each room builder manages its own:
- TileMapLayers (floor, door, debug)
- WallSystem (walls, doors, windows)
- Lighting (ceiling, monitor, desk lamp)
- CastShadowSystem (shadow casting)
- Props (via PropBuilder)
