# Dungeon Asset Setup Guide

Quick guide for converting your OBJ dungeon assets into grid-compatible prefabs.

## Your Assets

Located in: `Assets/Art/Dungeon/`

**Walls:**
- wall_1_1.obj through wall_1_5.obj (5 variants)
- wall_2_1.obj through wall_2_5.obj (5 variants)
- wall_shelf.obj

**Doors:**
- door.obj

**Pillars:**
- pillar_1.obj
- pillar_2.obj

**Texture:**
- texture.png (shared texture for all models)

## Quick Setup (2 minutes)

### Step 1: Open the Tool

`Tools > GridBuilder > Setup Dungeon Assets`

### Step 2: Verify Paths

The tool should auto-detect these paths:
- **Asset Folder:** `Assets/Art/Dungeon`
- **Prefab Output:** `Assets/Art/Dungeon/Prefabs`
- **Texture Path:** `Assets/Art/Dungeon/texture.png`

If paths are wrong, correct them!

### Step 3: Configure Scale

Start with default settings:
- **Grid Cell Size:** 1
- **Wall Scale:** (1, 1, 1)
- **Door Scale:** (1, 1, 1)
- **Pillar Scale:** (1, 1, 1)
- **Auto-Fix Rotation:** ✓ Enabled
- **Auto-Fix Pivot:** ✓ Enabled

### Step 4: Run Setup

Click **"Setup Everything (All Steps)"**

This will:
1. Create `DungeonMaterial.mat` in `Materials/` folder
2. Apply texture.png to the material
3. Create wall prefabs in `Prefabs/Walls/`
4. Create door prefabs in `Prefabs/Doors/`
5. Create pillar prefabs in `Prefabs/Pillars/`
6. Fix pivot points (bottom-center for grid alignment)
7. Fix rotations (face forward)
8. Add mesh colliders

### Step 5: Test & Adjust

1. **Check a Wall Prefab:**
   - Open `Prefabs/Walls/wall_1_1.prefab`
   - Verify texture shows in Scene view
   - Check scale looks appropriate
   - Pivot should be at bottom-center

2. **If Scale is Wrong:**
   - Delete generated prefabs
   - Adjust scale settings in tool
   - Run setup again

3. **Common Scale Values:**
   - If walls are too big: Try (0.5, 0.5, 0.5)
   - If walls are too small: Try (2, 2, 2)
   - Goal: Wall should be ~1 unit wide for 1x1 grid cell

## Manual Adjustments (If Needed)

### If Texture Doesn't Show

1. **Check Material:**
   - Open `Materials/DungeonMaterial.mat`
   - Verify `texture.png` is in Albedo slot
   - Set Rendering Mode to Opaque

2. **Check Texture Import:**
   - Select `texture.png`
   - Inspector: Texture Type = Default
   - Check "Read/Write Enabled"
   - Apply and reimport

### If Pivot is Wrong

The tool tries to center pivot at bottom, but if it's still wrong:

1. **Manual Fix:**
   - Open prefab
   - Create empty parent GameObject
   - Move mesh children to offset position
   - Adjust until pivot is at bottom-center of wall

### If Rotation is Wrong

Walls should face forward (Z+) by default:

1. **Manual Fix:**
   - Open prefab
   - Select mesh
   - Rotate Y by 90° increments until correct
   - Save prefab

## Using the Prefabs

### Add to Database

1. **Create/Open ObjectsDatabaseSO:**
   - `Create > GridBuilder > Objects Database`

2. **Add Wall Prefabs:**
   - Drag prefabs from `Prefabs/Walls/` to database
   - Click "Edit Occupied Cells" for each
   - Mark appropriate cells (usually 1x1x3 for standard wall)

3. **Add Door Prefabs:**
   - Drag from `Prefabs/Doors/`
   - Create DoorwayValidator:
     - `Create > GridBuilder > Validators > Doorway Validator`
     - Set direction (North/South/East/West)
   - Assign validator to door in database
   - Set occupied cells

4. **Add Pillar Prefabs:**
   - Drag from `Prefabs/Pillars/`
   - Set occupied cells (usually 1x1x3)

### Create Dungeon Database

1. **Create DungeonObjectsDatabaseSO:**
   - `Create > GridBuilder > Dungeon Objects Database`
   - Reference your ObjectsDatabaseSO

2. **Generate Thumbnails:**
   - `Tools > GridBuilder > Generate Dungeon Thumbnails`
   - Select your DungeonObjectsDatabaseSO
   - Click "Generate All Thumbnails"

## Testing

### Test in Room Editor

1. **Create Test Room:**
   - Create empty GameObject
   - Add DungeonRoomBuilder component
   - Set grid size (10x10)
   - Assign wall prefab

2. **Open Room Editor:**
   - Assign DungeonObjectsDatabaseSO
   - Click "Open Room Editor"

3. **Place Objects:**
   - Select wall from left panel
   - Click to place in 3D view
   - Verify scale, rotation, and appearance

## Troubleshooting

### Walls Too Big/Small

**Problem:** Walls don't fit 1x1 grid cell

**Solution:**
1. Note current scale (e.g., walls are 2x too big)
2. Delete generated prefabs
3. Adjust Wall Scale in tool (e.g., 0.5, 0.5, 0.5)
4. Run "Setup Wall Prefabs" again
5. Test until correct

### Texture Not Visible

**Problem:** Prefabs appear gray/white in scene

**Solution 1 - Material:**
- Check `Materials/DungeonMaterial.mat` has texture assigned
- If not, manually assign `texture.png` to Albedo
- Try different shader (Standard, URP/Lit, etc.)

**Solution 2 - Texture Settings:**
- Select `texture.png`
- Set Max Size to 2048 or higher
- Enable "Read/Write Enabled"
- Click Apply

**Solution 3 - Lighting:**
- Check scene has proper lighting
- Add Directional Light if needed
- Check material isn't in Transparent mode

### Wrong Orientation

**Problem:** Walls face wrong direction

**Solution:**
1. Disable "Auto-Fix Rotation" in tool
2. Or manually rotate prefabs after creation
3. Walls should face forward (local Z+)
4. Use rotation handles in Scene view

### Colliders Missing

**Problem:** Can't select objects in scene

**Solution:**
- Tool should auto-add MeshColliders
- If missing, manually add MeshCollider to prefabs
- Set Convex = false for walls

## Advanced Options

### Individual Setup Steps

Instead of "Setup Everything", you can run steps individually:

1. **"1. Create Material with Texture"**
   - Creates material only
   - Good for testing texture first

2. **"2. Setup Wall Prefabs"**
   - Converts only wall_*.obj files

3. **"3. Setup Door Prefabs"**
   - Converts only door.obj file

4. **"4. Setup Pillar Prefabs"**
   - Converts only pillar_*.obj files

Use individual steps when:
- Testing scale adjustments
- Only need specific object types
- Debugging issues

### Custom Scales Per Type

Set different scales for each type:
- **Walls:** (1, 1, 1) - Standard grid size
- **Doors:** (1, 1.2, 1) - Slightly taller
- **Pillars:** (0.8, 1, 0.8) - Slightly thinner

## Next Steps

After setup complete:

1. ✓ Prefabs created with materials
2. ✓ Added to ObjectsDatabaseSO
3. ✓ Occupied cells configured
4. ✓ Doorway validators assigned
5. ✓ Thumbnails generated

**Now you're ready to build dungeons!**

See QUICKSTART.md for full dungeon creation workflow.

## Additional Objects

Your dungeon folder also has:
- **Floors:** floor_1 through floor_6
- **Furniture:** tables, chairs, beds, bookshelves
- **Containers:** barrels, chests, crates
- **Decorations:** torches, candles, food, etc.

These can be setup the same way:
1. Add pattern to tool (or create manually)
2. Generate prefabs
3. Add to database
4. Use in Room Editor

## Support

If you encounter issues:
1. Check Unity Console for errors
2. Verify file paths are correct
3. Test with single object first
4. Check texture.png is valid image
5. Try different scale values
6. See QUICKSTART.md troubleshooting section

