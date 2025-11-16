# Wall Generation Guide

Complete guide to the three wall generation modes for dungeon rooms.

## Overview

The DungeonRoomBuilder supports four methods for generating walls around room boundaries. Each has pros/cons depending on your wall assets.

## Wall Generation Modes

### 1. AutoScale (RECOMMENDED - DEFAULT)

**Best for:** Any wall model - automatically scales to fit cell size perfectly

**How it works:**
- Places **one wall per cell** on boundary
- Automatically scales wall **width** to fit exactly 1 cell
- Positions walls at **outer edge** of their boundary cells
- South/North walls: aligned to Z edges
- West/East walls: aligned to X edges
- Calculates scale based on prefab bounds
- Perfect boundary alignment every time

**Advantages:**
- ✅ Works with ANY wall size (automatic scaling)
- ✅ Each wall fits exactly 1 cell
- ✅ No manual sizing needed
- ✅ Preserves individual wall separation
- ✅ Can replace individual walls later
- ✅ Perfect for irregular wall models

**Disadvantages:**
- ⚠️ More GameObjects than StretchToFit
- ⚠️ May stretch mesh slightly (but keeps 1:1 cell ratio)

**Setup:**
1. Set **Wall Mode:** AutoScale (default)
2. Assign any wall prefab to **Default Wall Prefab**
3. Click "Generate Walls"

**Result:**
- For 10x10 room: 36 walls total
  - South: 10 walls (each 1 cell wide, aligned to southern edge)
  - North: 10 walls (each 1 cell wide, aligned to northern edge)
  - West: 8 walls (each 1 cell long, aligned to western edge)
  - East: 8 walls (each 1 cell long, aligned to eastern edge)
- Each wall perfectly scaled and positioned at room boundary

### 2. StretchToFit

**Best for:** Irregular-sized wall models that don't fit exactly 1 cell

**How it works:**
- Creates **4 walls total** (one per room side)
- Stretches each wall to fit the entire side length
- Automatically calculates scale based on prefab bounds
- Maintains proper proportions

**Advantages:**
- ✅ Works with any wall size
- ✅ No gaps or overlaps
- ✅ Only 4 GameObjects total (performant)
- ✅ Smooth, continuous walls
- ✅ Perfect texture tiling

**Disadvantages:**
- ⚠️ Stretches the mesh (may distort details)
- ⚠️ Not ideal for walls with specific detail patterns

**Setup:**
1. Set **Wall Mode:** StretchToFit
2. Assign any wall prefab to **Default Wall Prefab**
3. Click "Generate Walls"

**Result:**
- South wall: Stretched along X axis (gridSize.x * cellSize)
- North wall: Stretched along X axis
- West wall: Stretched along Z axis (gridSize.y - 2 * cellSize)
- East wall: Stretched along Z axis

### 2. Procedural (BEST PERFORMANCE)

**Best for:** Simple textured walls without complex geometry

**How it works:**
- Generates **4 flat plane meshes** programmatically
- No prefab required
- Uses assigned material with texture
- Perfect UV mapping for tiling

**Advantages:**
- ✅ No wall prefab needed
- ✅ Perfect fit every time
- ✅ Best performance (simple geometry)
- ✅ Excellent texture tiling
- ✅ Only 4 GameObjects
- ✅ Automatic UVs based on cell size

**Disadvantages:**
- ⚠️ No 3D geometry (flat planes only)
- ⚠️ Requires material with texture
- ⚠️ Less visual detail

**Setup:**
1. Set **Wall Mode:** Procedural
   - OR check **Use Procedural Walls**
2. Assign material to **Wall Material**
3. Set **Wall Height** (e.g., 3 units)
4. Click "Generate Walls"

**Result:**
- Four double-sided wall planes
- UVs automatically tiled per cell
- Colliders included

### 4. Individual (LEGACY)

**Best for:** Wall models designed to be exactly 1 cell wide

**How it works:**
- Places **one wall prefab per grid cell** on boundary
- Total: (gridSize.x * 2) + ((gridSize.y - 2) * 2) walls
- No stretching or scaling

**Advantages:**
- ✅ Preserves original model detail
- ✅ Can mix different wall types
- ✅ Each wall is separate (can replace individually)

**Disadvantages:**
- ⚠️ Many GameObjects (performance impact)
- ⚠️ Gaps if wall < 1 cell wide
- ⚠️ Overlaps if wall > 1 cell wide
- ⚠️ Requires properly sized wall prefabs

**Setup:**
1. Set **Wall Mode:** Individual
2. Ensure wall prefab is exactly 1 cell wide
3. Click "Generate Walls"

## Quick Comparison

| Feature | AutoScale | StretchToFit | Procedural | Individual |
|---------|-----------|--------------|------------|------------|
| GameObjects | Many (20-40) | 4 | 4 | Many (20-40) |
| Performance | Good | Excellent | Best | Good |
| Works with any size | ✅ | ✅ | ✅ | ❌ |
| Perfect cell fit | ✅ | ⚠️ | ✅ | ❌ |
| Individual walls | ✅ | ❌ | ❌ | ✅ |
| Auto-scaling | ✅ | ✅ | N/A | ❌ |
| Texture quality | Good | Good | Excellent | Varies |
| Setup complexity | Easy | Easy | Easy | Hard |

## Choosing the Right Mode

### Use **AutoScale** when:
- You want each wall to fit exactly 1 cell
- Your wall models are irregular sizes
- You want to be able to replace individual walls
- You want automatic scaling (set it and forget it)
- **This is the default and recommended mode for most cases**

### Use **StretchToFit** when:
- You want the best performance (only 4 GameObjects)
- You're okay with stretched wall textures
- You need minimal object count

### Use **Procedural** when:
- You have a nice dungeon texture
- You don't need 3D wall geometry
- You want the best performance
- You want perfect texture tiling
- You're making many rooms

### Use **Individual** when:
- Your wall models are EXACTLY 1 cell wide
- You need to preserve fine details
- You want to mix different wall types per cell
- Performance isn't critical

## Configuration Options

### DungeonRoomBuilder Inspector

```
[Wall Settings]
- Default Wall Prefab: Your wall model (for StretchToFit/Individual)
- Auto Generate Walls: Generate on Awake if checked
- Wall Height: Height for procedural walls (e.g., 3)
- Wall Mode: StretchToFit / Procedural / Individual
- Use Procedural Walls: Override to force procedural
- Wall Material: Material for procedural walls
```

## Examples

### Example 1: AutoScale (Your Dungeon Assets - RECOMMENDED)

```
Room Setup:
- Grid Size: 10x10
- Cell Size: 1
- Wall Mode: AutoScale (default)
- Default Wall Prefab: wall_1_1.prefab (any size)

Result:
- 36 walls total
- South: 10 walls, each scaled to 1 cell wide, aligned to Z=0 edge
- North: 10 walls, each scaled to 1 cell wide, aligned to Z=max edge
- West: 8 walls, each scaled to 1 cell wide, aligned to X=0 edge
- East: 8 walls, each scaled to 1 cell wide, aligned to X=max edge
- Each wall positioned at outer boundary edge
- Perfect boundary alignment regardless of original wall size!
```

### Example 2: Stretch Fit (Performance Mode)

```
Room Setup:
- Grid Size: 10x10
- Cell Size: 1
- Wall Mode: StretchToFit
- Default Wall Prefab: wall_1_1.prefab (any size)

Result:
- 4 walls, each stretched to fit
- South: 10 units long
- North: 10 units long
- West: 8 units long (excludes corners)
- East: 8 units long
```

### Example 3: Procedural Walls

```
Room Setup:
- Grid Size: 12x8
- Cell Size: 1
- Wall Mode: Procedural
- Wall Material: DungeonMaterial (with texture.png)
- Wall Height: 3

Result:
- 4 procedural plane walls
- Perfect texture tiling
- Automatic UVs (texture repeats every cell)
- Very performant
```

### Example 4: Individual Cells (Legacy)

```
Room Setup:
- Grid Size: 8x8
- Cell Size: 1
- Wall Mode: Individual
- Default Wall Prefab: wall_perfect_1x1.prefab

Result:
- 28 individual wall instances
  (8 + 8 + 6 + 6 = 28)
- Each wall is 1x1 cell
- Can replace individual walls
```

## Texture Tiling

### StretchToFit
- Texture stretches with mesh
- May appear elongated
- Good for uniform textures

### Procedural
- Perfect tiling per cell
- UV = length / cellSize
- Best for brick/stone patterns
- Texture repeats naturally

### Individual
- Depends on prefab UVs
- May tile per wall or stretch

## Performance Tips

1. **Use StretchToFit or Procedural** for large rooms
2. **Avoid Individual mode** for rooms > 15x15
3. **Use Procedural** if you have many rooms
4. **Bake lighting** after generating walls
5. **Use texture atlases** for better batching

## Troubleshooting

### Walls are stretched/distorted (StretchToFit)
**Solution:** This is expected. If unacceptable:
- Switch to Procedural mode
- Or use properly sized wall prefabs with Individual mode

### Gaps between walls (AutoScale/Individual)
**Solution for AutoScale:**
- AutoScale positions walls at outer edges of boundary cells
- Gaps may appear if walls are too thin - this is expected
- Walls are aligned to room boundary, not to each other
- Use StretchToFit or Procedural modes for gap-free walls

**Solution for Individual mode:**
- Wall prefab is too small
- Switch to AutoScale or StretchToFit mode
- Or scale up wall prefab in Asset Setup tool

### Walls overlap (Individual)
**Solution:**
- Wall prefab is too large
- Switch to StretchToFit mode
- Or scale down wall prefab

### Texture doesn't show (Procedural)
**Solution:**
- Assign material to Wall Material field
- Check material has texture in Albedo slot
- Verify texture import settings

### Walls are the wrong height
**Solution:**
- Adjust Wall Height value
- Default: 3 units
- Typical range: 2-5 units

### Too many GameObjects (Individual)
**Solution:**
- Switch to StretchToFit or Procedural
- Both create only 4 walls total

## Advanced: Custom Wall Generation

You can create your own wall generation logic:

```csharp
public class CustomWallGenerator : MonoBehaviour
{
    public DungeonRoomBuilder roomBuilder;
    
    void GenerateCustomWalls()
    {
        roomBuilder.ClearWalls();
        
        // Your custom logic here
        // Access: roomBuilder.GridSize, roomBuilder.CellSize
        // Create wall GameObjects as children of roomBuilder.transform
    }
}
```

## Best Practices

1. **Start with StretchToFit** - It works for most cases
2. **Test with one room** before generating many
3. **Use Procedural** for prototype/placeholder walls
4. **Bake lighting** after wall generation
5. **Consider LODs** for complex wall meshes
6. **Use occlusion culling** for large dungeons

## Integration with Room Editor

The Room Editor works with all wall generation modes:
- Walls are treated as boundary
- Interior grid is gridSize - 2
- Place structural objects inside walls
- Doorways are placed as objects, not in walls

## FAQ

**Q: Can I mix wall modes?**
A: No, one mode per room. But you can have different modes in different rooms.

**Q: Can I change walls after generation?**
A: Yes, click "Clear Walls" then "Generate Walls" again.

**Q: Do walls affect object placement?**
A: No, walls are decoration. Objects place on interior grid (gridSize - 2).

**Q: Can I manually edit walls?**
A: Yes! Generated walls are just GameObjects. Edit as needed.

**Q: Which mode is fastest?**
A: Procedural > StretchToFit > Individual

**Q: Can I use different wall prefabs per side?**
A: Not automatically. Use Individual mode, then manually replace walls.

## Next Steps

1. Choose your wall mode
2. Generate test room
3. Verify walls look correct
4. Open Room Editor
5. Place structural objects (doors, pillars, etc.)
6. Save as prefab
7. Use in dungeon layout

See QUICKSTART.md for full dungeon creation workflow!

