# ZIndex-Based Y-Sort Pattern

## Problem

In Godot 4, the built-in `YSortEnabled` has limitations for top-down 2D games:
- YSort compares node positions, not sprite bounds or collision shapes
- Tall sprites (tables, cabinets, speakers) incorrectly cover the player when standing in front of them
- `YSortOrigin` only exists for TileMap tiles, not for regular Node2D/CharacterBody2D nodes
- Moving sprite offsets doesn't help because y-sort uses the root node's position

## Solution

Instead of using Godot's built-in YSort, use manual ZIndex assignment based on entity Y position.

In Godot 2D, higher ZIndex draws on top of lower ZIndex. Since screen Y increases downward, entities "lower" on screen (higher Y) should have higher ZIndex to appear in front.

## Implementation

### Player (or any moving entity)

Disable YSort and update ZIndex each frame based on GlobalPosition.Y:

```csharp
public partial class Player : CharacterBody2D
{
    public override void _Ready()
    {
        YSortEnabled = false;
    }

    public override void _Process(double delta)
    {
        ZIndex = (int)GlobalPosition.Y;
    }
}
```

### Static Props

Set ZIndex once when creating the prop, based on its root position:

```csharp
var root = new StaticBody2D();
root.Position = basePosition;

// Sprite offset doesn't affect sorting - root position matters
var sprite = new Sprite2D { 
    Texture = texture, 
    Position = new Vector2(0, -texture.GetSize().Y * 0.5f) 
};
root.AddChild(sprite);

// Key: Set ZIndex based on root position
root.ZIndex = (int)root.GlobalPosition.Y;

parent.AddChild(root);
```

### Walls

Same approach - set ZIndex based on sprite position:

```csharp
var sprite = new Sprite2D
{
    Texture = texture,
    Position = position,
    ZIndex = (int)position.Y  // Sort by Y position
};
```

## Key Insights

1. **Root position matters, not sprite position**: YSort compares node positions, not sprite bounds. Offsetting sprites doesn't change sorting.

2. **ZIndex = GlobalPosition.Y**: Higher Y (lower on screen) = higher ZIndex = draws in front.

3. **Disable YSortEnabled**: Don't mix built-in y-sort with manual ZIndex.

4. **Update every frame for moving entities**: Static props set ZIndex once; moving entities update in `_Process()`.

5. **All entities must participate**: Walls, props, and characters all need ZIndex set for correct sorting.

## Tradeoffs

| Aspect | Built-in YSort | Manual ZIndex |
|--------|---------------|---------------|
| Setup | Easy | Requires code |
| Moving entities | Automatic | Must update each frame |
| Tall sprites | Problematic | Works correctly |
| Performance | Slightly faster | Minimal overhead |
| Control | Limited | Full control |

## Files Modified

- `scripts/player/Player.cs` - Added manual ZIndex in _Process()
- `scripts/world/ControlRoom.cs` - Added ZIndex to props and walls
