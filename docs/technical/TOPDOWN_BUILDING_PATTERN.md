# Topdown Scene Building Pattern - Godot 4

## Overview

KBTV uses a square topdown tile layout with faux perspective walls. This document defines the grid, wall, and layering standards for building rooms programmatically in Godot 4 using C#.

## Tile Specs

- **Floor tiles**: 32x32
- **North/South walls**: 32x64 wall faces (top band + mid + base)
- **East/West walls**: 16px wide vertical strips inside a 32x64 tile
- **Top band height (walls)**: 32px
- **Bottom band height (walls)**: 16px

## Tileset Configuration

- **Tile Shape**: Square
- **Tile Layout**: Square
- **Tile Size**: 32x32
- **Wall origins**: `texture_origin = Vector2i(0, -64)` for 32x64 wall tiles

## Layering Pattern

Use multiple TileMapLayer children for clarity and occlusion behavior:

1. `FloorLayer` (floor tiles)
2. `WallLayer` (north + east + west walls)
3. `SouthWallLayer` (south wall faces)
4. `SouthWallStripLayer` (thin strip when south wall hides)
5. `DoorLayer` (door tile drawn above walls)

## Wall Placement Rules

- **North wall**: row `y = -1`
- **South wall**: row `y = _gridHeight - 1`
- **East/West walls**: rows `y = -1` through `y = _gridHeight - 1`
- **Door**: placed on `DoorLayer` at the east wall coordinate

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
