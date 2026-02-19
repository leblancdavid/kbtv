# Isometric Scene Building Pattern - Godot 4 Standards

## Overview

This document summarizes research on the latest standards for programmatically building isometric scenes in Godot 4 using C#. Based on Godot documentation, tutorials, and community discussions, TileMapLayer is the recommended approach for isometric grids (TileMap is deprecated).

## Key Findings

### TileMap vs TileMapLayer
- **TileMap**: Deprecated in favor of TileMapLayer. Shows deprecation warnings in Godot 4.
- **TileMapLayer**: Modern node for tile-based maps. Create multiple TileMapLayer children for different layers (terrain, objects, walls).
- Use Node2D as root, add TileMapLayer children for programmatic building.
- Both have full C# bindings with Vector2I support.

### Isometric Support
- Godot 4 supports isometric maps through TileSet configuration.
- Set Tile Shape to **Isometric** and Tile Layout to **Diamond Right** to get straight-edge parallelogram footprints for rectangular grids.
- Offset Axis should remain **Horizontal Off** for Diamond Right layouts.
- No custom shaders or math required for basic isometric views when layout is correct.
 - Avoid **Stairs Right** layouts for floors; they create stepped outlines instead of straight edges.

### C# Compatibility
- TileMapLayer APIs are identical in C# and GDScript.
- Use `TileMapLayer.SetCell(coords, sourceId, atlasCoords)` for programmatic tile placement.
- Vector2I works correctly in C#.

### Procedural Generation
- Ideal for dynamic scene building, terrain generation, or algorithmic level creation.
- Set tiles programmatically via C# scripts attached to parent node.

### Alternatives
If TileMapLayer is unsuitable (e.g., performance needs or custom rendering):
- Use Node2D with manually positioned Sprite2D/TextureRect nodes.
- Calculate positions using isometric formulas: `isoX = (x - y) * width/2`, `isoY = (x + y) * height/2`.
- Manual depth management with YSort or Z-indexing.

## Recommended Implementation

### Primary Approach: Node2D with TileMapLayer Children
1. Create Node2D as root node in scene.
2. Add TileMapLayer children for layers (floor, walls, etc.).
3. Assign isometric TileSet to each TileMapLayer (Tile Shape: Isometric, Tile Layout: Diamond Right).
4. Use C# to place tiles via SetCell().

**Benefits:**
- Follows current Godot 4 standards (no deprecation warnings)
- Native isometric rendering
- Efficient for large grids
- Multi-layer depth support

### Code Example
```csharp
public partial class ControlRoom : Node2D
{
    private TileMapLayer _floorLayer;

    public override void _Ready()
    {
        _floorLayer = GetNode<TileMapLayer>("FloorLayer");
        
        // Generate 8x5 floor grid
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                Vector2I coords = new Vector2I(x, y);
                _floorLayer.SetCell(coords, FLOOR_SOURCE_ID, ATLAS_COORDS);
            }
        }
        
        // Add walls as Sprite2Ds for occlusion
        CreateWallsWithOcclusion();
    }
    
    private void CreateWallsWithOcclusion()
    {
        // Create Sprite2Ds positioned using MapToLocal
        var sprite = new Sprite2D();
        sprite.Position = _floorLayer.MapToLocal(new Vector2I(x, y));
        sprite.AddChild(new Occluder()); // For fading effect
    }
}
```

## Best Practices
- Use TileSet editor for isometric configuration (tile size, collision) and confirm Tile Layout is Diamond Right.
- Separate layers for different elements (floors, walls, decorations).
- For occlusion, use Sprite2D overlays with custom scripts since TileMapLayer tiles can't have scripts.
- Test in editor for visual adjustments.
- For performance, generate in chunks if maps are very large.

## References
- Godot Docs: https://docs.godotengine.org/en/stable/tutorials/2d/using_tilemaps.html
- Isometric Tutorial: https://docs.godotengine.org/en/stable/tutorials/2d/isometric_tiles.html
