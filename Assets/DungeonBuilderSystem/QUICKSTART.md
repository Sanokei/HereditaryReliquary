# Dungeon Builder System - Quick Start Guide

Get started with the Dungeon Builder System in 5 minutes!

## Step 0: Setup Dungeon Assets (1 minute) - OPTIONAL

If you have OBJ files that need to be converted to prefabs:

1. **Open Asset Setup Tool:**
   - `Tools > GridBuilder > Setup Dungeon Assets`

2. **Configure Paths:**
   - Asset Folder: `Assets/Art/Dungeon` (or your folder)
   - Prefab Output: `Assets/Art/Dungeon/Prefabs`
   - Texture Path: `Assets/Art/Dungeon/texture.png`

3. **Adjust Scale:**
   - Grid Cell Size: 1
   - Wall Scale: (1, 1, 1) or adjust to fit your grid
   - Door Scale: (1, 1, 1) or adjust to fit your grid
   - Pillar Scale: (1, 1, 1) or adjust to fit your grid

4. **Setup Assets:**
   - Click "Setup Everything (All Steps)"
   - This will:
     - Create material with texture
     - Convert walls to prefabs
     - Convert doors to prefabs
     - Convert pillars to prefabs
     - Fix pivot points and rotations
     - Add colliders

5. **Result:**
   - Prefabs created in `Assets/Art/Dungeon/Prefabs/`
   - Walls folder with all wall prefabs
   - Doors folder with door prefabs
   - Pillars folder with pillar prefabs
   - Material created in `Materials/` folder

**Skip this step if you already have prefabs ready!**

## Step 1: Create Structural Object Database (2 minutes)

1. **Create ObjectsDatabaseSO:**
   - Right-click in Project window
   - `Create > GridBuilder > Objects Database`
   - Name it "StructuralObjects"
   - Set cell size to 1
   - Set placement layer mask

2. **Add Wall Prefab:**
   - Create a simple wall cube (1x3x1 units)
   - Save as prefab "Wall_01"
   - Add to StructuralObjects database
   - Set occupied cells via editor button

3. **Create Door Prefab:**
   - Create door model
   - Save as prefab "Door_01"
   - Add to StructuralObjects database
   - Create DoorwayValidator asset:
     - `Create > GridBuilder > Validators > Doorway Validator`
     - Set direction to North
   - Assign validator to Door_01 in database

4. **Create DungeonObjectsDatabaseSO:**
   - `Create > GridBuilder > Dungeon Objects Database`
   - Name it "DungeonStructuralDB"
   - Reference StructuralObjects database

5. **Generate Thumbnails (Optional):**
   - `Tools > GridBuilder > Generate Dungeon Thumbnails`
   - Select DungeonStructuralDB
   - Click "Generate All Thumbnails"

## Step 2: Create Your First Room (2 minutes)

1. **Create Room GameObject:**
   - Create empty GameObject, name it "Room_Start"
   - Add component `DungeonRoomBuilder`

2. **Configure Room:**
   - Grid Size: 10x10
   - Cell Size: 1
   - Default Wall Prefab: Wall_01
   - Placement Layer Mask: (select your layer)
   - Enable "Auto Generate Walls"

3. **Open Room Editor:**
   - In inspector, assign DungeonStructuralDB
   - Click "Open Room Editor"

4. **Place Doors:**
   - Click on Door_01 in left panel
   - Mode should be "Place"
   - Click on wall locations to place doors
   - Place at least 1 door
   - Click "Save"

5. **Save as Prefab:**
   - Drag Room_Start to Project window
   - Name it "Room_Start_Prefab"

## Step 3: Setup Dungeon System (1 minute)

1. **Create System Manager:**
   - Create empty GameObject "DungeonSystem"
   - Add component `DungeonSystemManager`
   - Assign DungeonStructuralDB

2. **Create Room Database:**
   - `Create > GridBuilder > Objects Database`
   - Name it "DungeonRooms"
   - Set cell size to 1
   - Set placement layer mask
   - Add Room_Start_Prefab to database

3. **Add Connection Validator:**
   - `Create > GridBuilder > Validators > Room Connection Validator`
   - Name it "RoomConnector"
   - Assign DungeonStructuralDB
   - Enable "Allow First Room"
   - Set connection distance to 2
   - Assign validator to Room_Start_Prefab in database

## Step 4: Place Rooms (30 seconds)

1. **Setup Building System:**
   - Create/find BuildingSystemManager in scene
   - Add DungeonRooms database to databases list

2. **Place First Room:**
   - Use placement system to place Room_Start_Prefab
   - It should place successfully (first room)

3. **Place Second Room:**
   - Place another Room_Start_Prefab nearby
   - Validator checks doorway alignment
   - If doorways align and face each other, placement succeeds

4. **View Connections:**
   - Select DungeonSystem GameObject
   - Gizmos show doorway connections (cyan lines)

## That's It!

You now have a working dungeon room system with:
- ✓ Modular rooms with walls
- ✓ Doorway-based connections
- ✓ Visual room editor
- ✓ Placement validation
- ✓ Connection visualization

## Next Steps

### Create More Rooms

1. Duplicate Room_Start
2. Change grid size (8x8, 12x12, etc.)
3. Edit in Room Editor
4. Place different door positions
5. Add to DungeonRooms database

### Add More Structural Objects

1. Create pillar prefabs
2. Create floor decoration prefabs
3. Add to StructuralObjects database
4. Use in Room Editor

### Advanced Features

1. **Multiple Door Types:**
   - Create DoorwayValidators for each direction
   - Create separate door prefabs per direction
   - Place strategically in rooms

2. **Special Rooms:**
   - Create entrance room (1-2 doors)
   - Create goal room (1 door, special objects)
   - Create hallway rooms (2+ doors)
   - Set validators accordingly

3. **Room Templates:**
   - Design common room layouts
   - Save as prefabs
   - Build library of reusable rooms

4. **Runtime Generation:**
   - Use ObjectPlacer to place rooms at runtime
   - Check connections programmatically
   - Build procedural dungeons

## Common Issues

**Walls not appearing?**
- Click "Generate Walls" in inspector
- Check defaultWallPrefab is assigned

**Can't place second room?**
- Check doorways are facing each other
- Verify connection distance (< 2 cells)
- Ensure RoomConnectionValidator is assigned

**Room Editor blank?**
- Assign DungeonStructuralDB in inspector
- Click "Initialize Editor"
- Check database has objects

**Doorways not connecting?**
- Verify DoorwayValidator direction is correct
- Check doorways are on wall boundaries
- Use Gizmos to visualize (select DungeonSystem)

**Texture not showing on prefabs?**
- Run Step 1 of Asset Setup tool to create material
- Check material has texture assigned
- Verify texture import settings (read/write enabled)
- Reimport texture if needed

**Walls/Objects wrong size?**
- Adjust scale settings in Asset Setup tool
- Test with one wall first before batch processing
- Recommended: Wall scale (1, 1, 1) for 1-unit grid
- Check "Auto-Fix Pivot" is enabled

**OBJ files not converting?**
- Verify folder paths are correct
- Check OBJ files are in correct location
- Run each step individually to debug
- Check Unity console for specific errors

## Example Room Configurations

### Small Combat Room (8x8)
- 2-3 doors (different walls)
- 2-4 pillars inside
- Good for encounters

### Long Hallway (4x12)
- 2 doors (opposite ends)
- No interior objects
- Connects large rooms

### Boss Room (15x15)
- 1-2 doors (entrance)
- Multiple pillars
- Large interior space

### Starting Room (10x10)
- 1-2 doors (exits)
- Safe area, no enemies
- Tutorial space

## Tips

1. **Keep cell size consistent** (all objects use same size)
2. **Place doors on walls** (not interior cells)
3. **Face doors outward** (away from room center)
4. **Test connections** before finalizing rooms
5. **Use Gizmos** to visualize doorways and connections
6. **Save frequently** when editing rooms
7. **Name rooms clearly** (Room_Combat_8x8, Room_Hall_4x12)
8. **Document door locations** for later reference

## Resources

- Full documentation: See README.md
- Grid Builder System docs: See parent system
- Unity Splines: https://docs.unity3d.com/Packages/com.unity.splines@latest

## Support

For issues or questions:
1. Check README.md troubleshooting section
2. Verify all components are assigned
3. Check Unity console for errors
4. Review linter warnings

Happy dungeon building!

