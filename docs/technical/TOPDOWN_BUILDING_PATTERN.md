# Topdown Scene Building Pattern - Godot 4

## Overview

KBTV uses a square topdown tile layout with faux perspective walls. This document defines the grid, wall, and layering standards for building rooms programmatically in Godot 4 using C#.

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

- `scenes/world/ControlRoom.tscn`
- `scripts/world/ControlRoom.cs`

The ControlRoom uses the topdown tileset and the layering rules above for wall placement, door visibility, and south wall occlusion.
