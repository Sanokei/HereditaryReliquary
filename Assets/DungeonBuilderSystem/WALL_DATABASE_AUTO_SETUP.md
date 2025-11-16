# Wall Database Auto-Setup Guide

**Automatically populate your WallPieceDatabase in 2 minutes!**

The Wall Database Auto-Populator scans your asset folder, categorizes wall pieces, and generates a complete WallPieceDatabase with proper adjacency rules—all automatically.

## Quick Start (3 Steps)

### 1. Open the Tool

```
Tools > Dungeon Builder > Auto-Populate Wall Database
```

### 2. Create Target Database

1. Right-click in Project window
2. `Create > GridBuilder > Wall Piece Database`
3. Name it "DungeonWallDatabase"

### 3. Auto-Populate

1. **Set Asset Folder**: `Assets/Art/Dungeon` (or your folder)
2. **Assign Target Database**: Drag your DungeonWallDatabase
3. **Click "🔍 Scan for Assets"**
4. **Review found assets** (see categorization below)
5. **Click "✨ Generate Database with Adjacency Rules"**

**Done!** Your database is now populated with proper adjacency rules. 🎉

---

## Detailed Walkthrough

### Step 1: Open the Tool

From Unity menu:
```
Tools > Dungeon Builder > Auto-Populate Wall Database
```

A window will open with these sections:
- **Settings**: Folder path and target database
- **Asset List**: Found assets with categories
- **Quick Categorization**: Buttons to auto-categorize
- **Generate**: Create the database

### Step 2: Configure Settings

#### Asset Folder
Default: `Assets/Art/Dungeon`

- Type the path directly, or
- Click **"Browse"** to select folder
- Must be inside Assets directory

#### Target Database
- Create new: `Create > GridBuilder > Wall Piece Database`
- Or select existing database
- Database will be modified (pieces added)

### Step 3: Scan for Assets

Click **"🔍 Scan for Assets"**

The tool will:
- Find all `.prefab` files in folder
- Find all `.obj` model files in folder
- Automatically categorize based on naming
- Suggest weights based on type
- Auto-exclude furniture/floors

### Step 4: Review & Categorize

Each found asset shows:
- **Asset Name**: e.g., "wall_1_1"
- **Category**: Dropdown (Straight, Window, Door, etc.)
- **Weight**: Number (higher = more common)
- **Include**: Checkbox (include in database?)

#### Automatic Categorization

The tool automatically categorizes based on name:

| Name Contains | Auto-Category | Auto-Weight | Auto-Include |
|---------------|---------------|-------------|--------------|
| "corner" | CornerOuter | 1.0 | ✅ Yes |
| "window" | Window | 0.8 | ✅ Yes |
| "door" | DoorFrame | 0.3 | ✅ Yes |
| "torch" | Torch | 1.5 | ✅ Yes |
| "shelf" | Shelf | 1.0 | ✅ Yes |
| "pillar" | Pillar | 1.0 | ✅ Yes |
| "wall_1_*" | Straight | 3.0 | ✅ Yes |
| "wall_2_*" | Straight | 3.0 | ✅ Yes |
| "damaged", "broken" | Straight | 0.5 | ✅ Yes |
| "floor", "table", "chair" | - | - | ❌ No |

#### Quick Categorization Buttons

If auto-categorization isn't perfect, use quick buttons:

- **"All → Straight"**: Mark all as Straight walls
- **"'window' → Window"**: Find names with "window", mark as Window
- **"'door' → Door"**: Find names with "door", mark as DoorFrame
- **"'corner' → Corner"**: Find names with "corner", mark as CornerOuter
- **"'torch' → Torch"**: Find names with "torch", mark as Torch
- **"'shelf' → Shelf"**: Find names with "shelf", mark as Shelf
- **"Include All"**: Check all Include boxes
- **"Exclude All"**: Uncheck all Include boxes

#### Manual Adjustments

Click dropdown to change category:
- **Straight**: Regular wall segment
- **CornerOuter**: 90° corner piece (most common)
- **CornerInner**: Inward corner (rare)
- **Window**: Window opening
- **DoorFrame**: Door opening
- **Torch**: Wall torch decoration
- **Shelf**: Wall shelf decoration
- **Pillar**: Decorative pillar
- **TJunction**: T-shaped junction
- **CrossJunction**: + junction
- **EndCap**: Wall end piece
- **Empty**: No wall

Adjust **Weight**:
- Higher = more common (e.g., 5.0 for basic walls)
- Lower = rarer (e.g., 0.3 for doors)
- Default suggestions are usually good!

Uncheck **Include** to skip an asset

### Step 5: Generate Database

Click **"✨ Generate Database with Adjacency Rules"**

The tool will:
1. ✅ Add each included asset to database
2. ✅ Set piece name, prefab, type, weight
3. ✅ Configure Valid Positions (corners vs edges)
4. ✅ **Setup Adjacency Rules** (this is the magic!)
5. ✅ Save database

**Result**: Database ready for WFC generation!

---

## Adjacency Rules (Automatic)

The tool automatically configures adjacency rules based on category:

### Straight Walls
```
Allowed North/East/South/West: Straight, CornerOuter, Window, DoorFrame, Torch, Shelf, Pillar
```
**Result**: Flexible, can be next to most things

### Window Walls
```
Allowed North/East/South/West: Straight, CornerOuter
```
**Result**: Windows never adjacent to other windows or doors

### Door Frames
```
Allowed North/South: Straight
Allowed East/West: (empty - none allowed)
```
**Result**: Doors must have straight walls on both sides, can't be at edges

### Torches/Shelves/Decorations
```
Allowed North/East/South/West: Straight, CornerOuter
```
**Result**: Decorations spread out, never adjacent to each other

### Corners
```
Allowed North/East: Straight, Window, Torch, Shelf
Allowed South/West: (empty)
```
**Result**: Corners connect properly to edge pieces

---

## Example: Your Dungeon Folder

Let's say your `Assets/Art/Dungeon` contains:

```
wall_1_1.obj
wall_1_2.obj
wall_1_3.obj
wall_1_4.obj
wall_1_5.obj
wall_2_1.obj (window)
wall_2_2.obj (window)
door.obj
Corner.prefab
torch.obj
wall_shelf.obj
```

### After Scanning:

| Asset | Category | Weight | Include |
|-------|----------|--------|---------|
| wall_1_1 | Straight | 3.0 | ✅ |
| wall_1_2 | Straight | 3.0 | ✅ |
| wall_1_3 | Straight | 3.0 | ✅ |
| wall_1_4 | Straight | 3.0 | ✅ |
| wall_1_5 | Straight | 3.0 | ✅ |
| wall_2_1 | Straight | 3.0 | ✅ |
| wall_2_2 | Straight | 3.0 | ✅ |
| door | DoorFrame | 0.3 | ✅ |
| Corner | CornerOuter | 1.0 | ✅ |
| torch | Torch | 1.5 | ✅ |
| wall_shelf | Shelf | 1.0 | ✅ |

### Manual Adjustments:

If `wall_2_1` and `wall_2_2` have windows:
1. Click dropdown for wall_2_1 → **Window**
2. Click dropdown for wall_2_2 → **Window**
3. Adjust weight to 0.8 (less common)

### After Generate:

Database now has:
- **5 Straight walls** (wall_1_1 through wall_1_5)
  - Can be next to anything
  - Weight 3.0 (common)
- **2 Window walls** (wall_2_1, wall_2_2)
  - Can only be next to Straight or Corner
  - Weight 0.8 (uncommon)
  - Will automatically space out!
- **1 Door** (door)
  - Must have Straight walls on sides
  - Weight 0.3 (rare)
- **1 Corner** (Corner)
  - Connects to edges properly
  - Weight 1.0 (normal)
- **1 Torch** (torch)
  - Can only be next to Straight or Corner
  - Weight 1.5 (uncommon)
  - Will spread out, not cluster
- **1 Shelf** (wall_shelf)
  - Can only be next to Straight or Corner
  - Weight 1.0 (normal)

---

## Using the Generated Database

### 1. Assign to DungeonRoomBuilder

1. Select your DungeonRoomBuilder GameObject
2. Set **Wall Mode** = `WaveFunctionCollapse`
3. Assign **Wall Piece Database** = DungeonWallDatabase
4. Click **"Generate Walls"**

### 2. See the Magic

The WFC system will:
- ✅ Place corners at room corners
- ✅ Fill edges with straight walls (variety from 5 variants!)
- ✅ Occasionally place windows (spaced out, never adjacent)
- ✅ Rarely place door (on any wall, with clearance)
- ✅ Scatter torches/shelves (never clustered)
- ✅ All pieces follow adjacency rules

### 3. Iterate

If you want different results:
- Adjust weights in database (higher = more common)
- Add more wall variants
- Change adjacency rules manually
- Regenerate to see new layouts

---

## Tips & Tricks

### Naming Conventions

For best auto-categorization, name your assets:
- Walls: `wall_1_1`, `wall_straight_01`, `stone_wall_clean`
- Windows: `wall_window`, `wall_2_1_window`, `window_wall`
- Doors: `door`, `door_frame`, `doorway`
- Corners: `corner`, `wall_corner`, `corner_outer`
- Decorations: `torch`, `wall_shelf`, `wall_torch`

### Weight Guidelines

- **5.0+**: Very common (basic walls)
- **2.0-3.0**: Common (variant walls)
- **1.0-1.5**: Uncommon (decorations)
- **0.5-0.8**: Uncommon (windows)
- **0.1-0.3**: Rare (doors, special pieces)

### Multiple Databases

Create different databases for different themes:
- `DungeonWallDatabase_Stone` (your current one)
- `DungeonWallDatabase_Wood` (run tool again with wood assets)
- `DungeonWallDatabase_Metal` (run tool again with metal assets)

Switch between them on DungeonRoomBuilder for variety!

### Re-Running the Tool

You can:
- Scan different folders
- Add to existing database (it appends)
- Or create new database for fresh start

### Excluding Assets

Uncheck **Include** for:
- Non-wall assets that slipped through
- Duplicate variants you don't want
- Assets not yet ready for use

---

## Troubleshooting

### "No wall assets found"

**Check**:
- Folder path is correct
- Folder contains .prefab or .obj files
- Folder is inside Assets directory

### All assets excluded automatically

**Cause**: Names contain "floor", "table", "chair", etc.

**Fix**: 
- Click "Include All" button
- Or rename assets to include "wall", "door", "corner"

### Wrong categories

**Fix**:
- Use quick categorization buttons
- Or manually change dropdown for each asset
- The tool's guesses are just suggestions!

### Database not saved

**Check**:
- Target Database is assigned
- Database asset exists in Project

**Fix**:
- Create new database: `Create > GridBuilder > Wall Piece Database`
- Assign it before clicking Generate

### "Database is empty" warning in DungeonRoomBuilder

**Check**:
- You clicked "Generate Database" (not just "Scan")
- Database has pieces (open it to verify)

**Fix**:
- Open database in Inspector
- Should show list of Wall Pieces
- If empty, run Auto-Populator again

---

## Advanced: Manual Editing After Auto-Population

After auto-population, you can manually refine:

1. **Open Database** in Inspector
2. **Expand Wall Pieces** list
3. **Adjust individual pieces**:
   - Change prefab reference
   - Modify weight
   - Add/remove Valid Positions
   - **Fine-tune Adjacency Rules**:
     - Expand Allowed North/East/South/West
     - Add/remove allowed types
     - Leave empty for "allow all"

See `WFC_ADJACENCY_RULES_QUICK_REF.md` for advanced patterns.

---

## Summary

The Auto-Populator:
1. ✅ Scans your asset folder
2. ✅ Finds walls, doors, corners, decorations
3. ✅ Auto-categorizes based on naming
4. ✅ Suggests appropriate weights
5. ✅ Configures Valid Positions
6. ✅ **Sets up proper adjacency rules**
7. ✅ Generates complete database

**Result**: Ready-to-use WallPieceDatabase in 2 minutes instead of 30 minutes of manual setup!

---

## See Also

- **WFC_WALL_GUIDE.md** - Complete WFC documentation
- **WFC_ADJACENCY_RULES_QUICK_REF.md** - Rule patterns
- **WFC_CONSTRAINT_SYSTEM_SUMMARY.md** - How it works
- **WALL_GENERATION_GUIDE.md** - All generation modes
- **QUICKSTART.md** - General setup

---

**Start generating intelligent dungeon walls now!** 🏰✨🤖

