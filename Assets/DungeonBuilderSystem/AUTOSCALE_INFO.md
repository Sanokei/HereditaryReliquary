# AutoScale Wall Mode - Quick Info

## What is AutoScale?

The **default and recommended** wall generation mode that automatically scales each wall to fit exactly one cell.

## Why AutoScale?

✅ **Works with any wall size** - No need to manually resize your walls
✅ **Perfect fit** - Each wall fits exactly 1 cell, no gaps or overlaps
✅ **Individual walls** - Can replace specific walls later
✅ **Automatic** - Just assign prefab and click "Generate Walls"

## How It Works

```
Your wall_1_1.obj (any size)
    ↓
Auto-calculates bounds (width × depth × height)
    ↓
Scales width to fit 1 cell:
  - Width (X) = cell size
  - Depth (Z) = unchanged (keeps original thickness)
  - Height (Y) = unchanged
    ↓
Positions at outer edge of boundary cells
```

### Perfect Edge Alignment!

AutoScale now positions walls at the **outermost edge** of boundary cells:
- ✅ **South walls**: Aligned to southern edge (Z = 0)
- ✅ **North walls**: Aligned to northern edge (Z = max)
- ✅ **West walls**: Aligned to western edge (X = 0)
- ✅ **East walls**: Aligned to eastern edge (X = max)
- ✅ Each wall centered in its cell along width
- ✅ Perfect boundary alignment!

### Example: 10x10 Room

- **South side:** 10 walls (each 1 cell wide)
- **North side:** 10 walls (each 1 cell wide)
- **West side:** 8 walls (each 1 cell long, excluding corners)
- **East side:** 8 walls (each 1 cell long, excluding corners)
- **Total:** 36 walls, all perfectly fitted

## Quick Setup

1. **DungeonRoomBuilder Inspector:**
   - Wall Mode: `AutoScale` (already default!)
   - Default Wall Prefab: `wall_1_1.prefab`
   - Click "Generate Walls"

2. **Result:**
   - Perfect walls, every time!
   - Each wall centered in its cell
   - Auto-scaled to cell size

## Comparison with Other Modes

| Mode | Walls for 10x10 | Works with any size? | Individual walls? |
|------|-----------------|----------------------|-------------------|
| **AutoScale** | 36 | ✅ Yes | ✅ Yes |
| StretchToFit | 4 | ✅ Yes | ❌ No |
| Procedural | 4 | ✅ Yes | ❌ No |
| Individual | 36 | ❌ No (must be exact) | ✅ Yes |

## When to Use Each Mode

### Use AutoScale (Default):
- ✅ You want individual walls per cell
- ✅ Walls need to fit exactly 1 cell
- ✅ Your wall models are any size
- ✅ You want "set it and forget it"

### Use StretchToFit:
- Only if you need absolute minimum GameObjects (4 vs 36)
- Okay with stretched textures

### Use Procedural:
- Don't have wall prefabs yet
- Want perfect texture tiling
- Flat walls are acceptable

### Use Individual:
- Your walls are ALREADY exactly 1 cell size
- Don't want any scaling at all

## Your Dungeon Assets

Your `wall_1_1.obj` through `wall_2_5.obj`:
- Any size is fine!
- AutoScale handles it automatically
- No need to resize in Blender
- No need to manually scale prefabs

## Performance

AutoScale creates more GameObjects than StretchToFit (36 vs 4 for a 10x10 room), but:
- Still very performant for typical dungeon sizes
- Batching helps reduce draw calls
- Benefits of individual walls outweigh cost
- Can optimize later if needed

For rooms < 20x20: **AutoScale is recommended**
For rooms > 20x20: **Consider StretchToFit or Procedural**

## FAQ

**Q: Will AutoScale distort my walls?**
A: Slightly - it scales to fit 1 cell. But since all walls are scaled the same, it looks uniform.

**Q: Can I use different wall types?**
A: Yes! Generate with one type, then manually replace specific walls.

**Q: What if my wall is 0.5 units wide and cells are 1 unit?**
A: AutoScale will scale it 2x in width to fit the cell perfectly. Depth stays the same.

**Q: What if my wall is 3 units wide and cells are 1 unit?**
A: AutoScale will scale it 0.33x in width to fit the cell perfectly. Depth stays the same.

**Q: Why edge alignment instead of centering?**
A: Edge alignment ensures walls form the exact outer boundary of the room, creating a perfect perimeter at grid edges.

**Q: Does it work with doors?**
A: Yes! Doors are placed separately as structural objects, not part of walls.

## Tips

1. **Start with AutoScale** - It's the default for a reason
2. **Test with one wall variant** before committing
3. **Use consistent cell size** across all rooms (e.g., 1 unit)
4. **Mix wall types** by manually replacing after generation
5. **Bake lighting** after generating walls for best visuals

## See Also

- **WALL_GENERATION_GUIDE.md** - Complete guide to all modes
- **QUICKSTART.md** - Full dungeon setup tutorial
- **ASSET_SETUP_GUIDE.md** - Converting OBJ files to prefabs

---

**TL;DR:** AutoScale = Perfect walls with zero effort! 🎉

