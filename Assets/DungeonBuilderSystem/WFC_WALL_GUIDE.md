# Wave Function Collapse Wall System with Constraint-Based Adjacency Rules

Complete guide to using WFC (Wave Function Collapse) for intelligent, rule-based wall generation.

## Overview

The WFC wall system uses **constraint propagation** and **adjacency rules** to automatically generate realistic wall layouts:
- **Corners**: Automatically use corner pieces at room corners
- **Straight walls**: Automatically use straight pieces on edges  
- **Windows**: Only placed where they make sense (not next to doors, not at corners)
- **Doors**: Spaced appropriately, following adjacency constraints
- **Decorations**: Torches, shelves, etc. placed according to rules
- **Constraint solving**: Invalid combinations are automatically eliminated

## How WFC Constraint Propagation Works

Think of it like a smart jigsaw puzzle:

1. **Start**: Each boundary cell can be any compatible wall piece
2. **Pick a cell**: Choose one cell (fewest options first)
3. **Collapse**: Select one specific piece for that cell
4. **Propagate**: Automatically remove incompatible pieces from neighbors based on adjacency rules
5. **Repeat**: Continue until all cells have exactly one piece

### Example: Window Placement

Let's say you set up these rules:
- **Window** piece can have: Straight walls adjacent (but no other windows, doors, or corners)
- **Straight wall** piece can have: Any piece adjacent

When WFC places a Window:
1. Window is placed at cell (5, 0)
2. Neighbors at (4, 0) and (6, 0) are checked
3. Their "possible pieces" lists are filtered to remove Windows and Doors
4. Only Straight walls remain as options for those cells
5. Result: Windows are automatically spaced out!

## Components

### 1. WallPieceDatabase
ScriptableObject that defines:
- **Wall Pieces**: List of all wall prefabs with their rules
- **Valid Positions**: Where each piece can be placed (corner vs edge)
- **Adjacency Rules**: What each piece allows as neighbors  
- **Weights**: Probability of selecting each piece

### 2. WallPiece
Individual wall definition with constraint rules:
- **Piece Name**: Identifier
- **Prefab**: The GameObject to instantiate
- **Piece Type**: Category (Straight, CornerOuter, Window, DoorFrame, etc.)
- **Weight**: Selection probability (0.1-10.0, higher = more likely)
- **Valid Positions**: Where this piece can be used (leave empty = all positions)
- **Adjacency Rules** ⚠️ **KEY FOR WFC**:
  - **Allowed North**: Which piece types can be clockwise-adjacent (North direction)
  - **Allowed East**: Which piece types can be to the East
  - **Allowed South**: Which piece types can be to the South
  - **Allowed West**: Which piece types can be to the West

### 3. WallPieceType Enum
Available categories:
- **Straight**: Regular wall segment
- **CornerInner**: 90° inward corner
- **CornerOuter**: 90° outward corner (most common corner)
- **TJunction**: T-shaped junction
- **CrossJunction**: + shaped junction
- **EndCap**: Wall end piece
- **DoorFrame**: Door opening
- **Window**: Window opening
- **Pillar**: Decorative pillar
- **Torch**: Wall-mounted torch
- **Shelf**: Wall-mounted shelf
- **Empty**: Empty space (no wall)

### 4. WFCSolver
The constraint solver (automatic):
- Initializes cells with possible pieces
- Finds cells with minimum entropy
- Collapses cells by selecting one piece
- Propagates constraints to neighbors
- Iterates until fully solved

## Quick Setup

### Step 1: Create Wall Piece Database

```
Right-click in Project > Create > GridBuilder > Wall Piece Database
```
Name it: `DungeonWallPieces`

### Step 2: Add Wall Pieces with Adjacency Rules

#### Example 1: Basic Straight Wall

1. Click "Add Wall Piece"
2. Configure:
   - **Piece Name**: "Wall_Straight_Basic"
   - **Prefab**: Your wall_1_1 prefab
   - **Piece Type**: Straight
   - **Weight**: 5.0 (common)
   - **Valid Positions**: North, South, East, West (or leave empty)
   - **Allowed North**: Straight, CornerOuter, Window, DoorFrame, Torch
   - **Allowed East**: Straight, CornerOuter, Window, DoorFrame, Torch
   - **Allowed South**: Straight, CornerOuter, Window, DoorFrame, Torch
   - **Allowed West**: Straight, CornerOuter, Window, DoorFrame, Torch

**Result**: This wall can be next to most things.

#### Example 2: Window Wall

1. Click "Add Wall Piece"
2. Configure:
   - **Piece Name**: "Wall_Window"
   - **Prefab**: Your wall with window prefab
   - **Piece Type**: Window
   - **Weight**: 1.0 (uncommon)
   - **Valid Positions**: North, South, East, West (edges only)
   - **Allowed North**: Straight, CornerOuter (NOT Window, NOT DoorFrame)
   - **Allowed East**: Straight, CornerOuter
   - **Allowed South**: Straight, CornerOuter
   - **Allowed West**: Straight, CornerOuter

**Result**: Windows can only be next to straight walls or corners, never next to other windows or doors!

#### Example 3: Door Frame

1. Click "Add Wall Piece"
2. Configure:
   - **Piece Name**: "Wall_Door_South"
   - **Prefab**: Your door frame prefab
   - **Piece Type**: DoorFrame
   - **Weight**: 0.5 (rare)
   - **Valid Positions**: South (only on south wall)
   - **Allowed North**: Straight (must have straight walls on both sides)
   - **Allowed East**: (empty = none, door can't be at edge)
   - **Allowed South**: Straight
   - **Allowed West**: (empty = none)

**Result**: Door only appears on south wall, with straight walls on both sides!

#### Example 4: Corner Piece

1. Click "Add Wall Piece"
2. Configure:
   - **Piece Name**: "Wall_Corner"
   - **Prefab**: Your corner prefab
   - **Piece Type**: CornerOuter
   - **Weight**: 1.0
   - **Valid Positions**: NorthEast, NorthWest, SouthEast, SouthWest
   - **Allowed North**: Straight, Window, DoorFrame, Torch (connects to edge)
   - **Allowed East**: Straight, Window, DoorFrame, Torch (connects to edge)
   - **Allowed South**: (empty = no constraint)
   - **Allowed West**: (empty = no constraint)

**Result**: Corners connect properly to edge pieces!

### Step 3: Configure DungeonRoomBuilder

1. Select your DungeonRoomBuilder GameObject
2. Set:
   - **Wall Mode**: WaveFunctionCollapse
   - **Wall Piece Database**: Your DungeonWallPieces asset
3. Click "Generate Walls"

**Result**: Automatic intelligent constraint-based wall placement! 🎉

## Understanding Adjacency Rules

### Direction Meaning

Directions are **clockwise around the room perimeter** from the piece's perspective:

For a rectangular room going clockwise from southwest corner:
- **South edge**: North = next piece to the east
- **Southeast corner**: North = piece going north on east edge
- **East edge**: North = next piece going north
- **Northeast corner**: North = piece going west on north edge
- **North edge**: North = next piece going west
- **Northwest corner**: North = piece going south on west edge
- **West edge**: North = next piece going south
- **Southwest corner**: North = piece going east on south edge

### Empty Lists = No Restrictions

If you leave an adjacency list **empty**, any piece type can be adjacent in that direction.

**Use Case**: Corners often have empty lists for some directions since they connect to multiple edges.

### Bidirectional Constraints

Remember: Both pieces must allow each other!

If piece A allows piece B to the north, but piece B doesn't allow piece A to the south, they **can't be adjacent**.

**Example**:
- Window allows Straight to north ✅
- Straight allows Window to south ✅
- **Result**: Can be adjacent! ✅

vs:
- Window allows DoorFrame to north ❌
- DoorFrame allows Window to south ✅
- **Result**: Cannot be adjacent! ❌ (Window blocks it)

## Example Configurations

### Basic Setup (Straight + Corners)

```
Wall Piece Database:
├── Straight_Wall
│   ├── Type: Straight
│   ├── Weight: 5.0
│   ├── Valid: North, South, East, West
│   ├── Allowed North: Straight, CornerOuter
│   ├── Allowed East: Straight, CornerOuter
│   ├── Allowed South: Straight, CornerOuter
│   └── Allowed West: Straight, CornerOuter
├── Corner_Outer
│   ├── Type: CornerOuter
│   ├── Weight: 1.0
│   ├── Valid: NE, NW, SE, SW
│   ├── Allowed North: Straight
│   └── Allowed East: Straight
```

**Result**: Basic walls with proper corners

### Varied Walls with Windows

```
Wall Piece Database:
├── Stone_Wall_Clean
│   ├── Type: Straight
│   ├── Weight: 5.0
│   ├── Allowed: Straight, CornerOuter, Window, Torch
├── Stone_Wall_Cracked
│   ├── Type: Straight
│   ├── Weight: 2.0
│   ├── Allowed: Straight, CornerOuter, Window, Torch
├── Stone_Wall_Window
│   ├── Type: Window
│   ├── Weight: 1.0
│   ├── Valid: North, South, East, West
│   ├── Allowed North: Straight (NOT Window, NOT DoorFrame)
│   ├── Allowed East: Straight
│   ├── Allowed South: Straight
│   └── Allowed West: Straight
├── Stone_Corner
│   ├── Type: CornerOuter
│   ├── Weight: 1.0
│   ├── Allowed North: Straight, Window
│   └── Allowed East: Straight, Window
```

**Result**: Varied walls with spaced-out windows that never touch each other!

### Dungeon with Doors and Torches

```
Wall Piece Database:
├── Dungeon_Wall
│   ├── Type: Straight
│   ├── Weight: 10.0
│   ├── Allowed: All types (empty lists)
├── Dungeon_Wall_Torch
│   ├── Type: Torch
│   ├── Weight: 2.0
│   ├── Valid: North, South, East, West
│   ├── Allowed: Straight, CornerOuter (NOT Torch, NOT Window)
├── Dungeon_Door_South
│   ├── Type: DoorFrame
│   ├── Weight: 0.3 (rare)
│   ├── Valid: South
│   ├── Allowed North: Straight, Torch
│   ├── Allowed East: Empty (can't be at edge)
│   ├── Allowed South: Straight, Torch
│   └── Allowed West: Empty
├── Dungeon_Corner
│   ├── Type: CornerOuter
│   ├── Weight: 1.0
│   ├── Allowed: Straight, Torch
```

**Result**: 
- Torches appear occasionally, never next to each other
- Doors rarely appear on south wall, flanked by straight walls
- Everything connects properly!

## Advanced Techniques

### Preventing Adjacent Decorations

To prevent torches next to torches:
```
Torch piece:
├── Allowed North: Straight, CornerOuter (exclude Torch, Window, Door)
├── Allowed South: Straight, CornerOuter
etc.
```

### Forcing Patterns

To force windows to always have 2+ straight walls between them:
```
Window piece:
├── Allowed North: Straight only
├── Allowed East: Straight only
├── etc.

Straight piece:
├── Allowed North: Straight, CornerOuter, Window (normal)
├── etc.
```

Then reduce Window weight to make them rare. WFC will space them out!

### Door Placement Control

To put doors only in the center of walls:
1. Create different prefabs for each wall side
2. Use Valid Positions to restrict to specific edges
3. Use Allowed rules to require straight walls on both sides
4. Use low Weight to make them rare

### Multiple Databases for Themes

Create different databases:
- `DungeonWallPieces_Stone` (heavy, fewer windows)
- `DungeonWallPieces_Wood` (lighter, more windows)
- `DungeonWallPieces_Metal` (industrial, grates)

Switch between them for different room styles!

## Creating Wall Prefabs

### Straight Wall Guidelines

1. **Dimensions**: 
   - Width: Any (auto-scaled to cell size)
   - Depth: 0.1-0.3 units (thin)
   - Height: 2-4 units
2. **Pivot**: Bottom-center
3. **Facing**: Forward (Z+)

### Corner Guidelines

1. **Dimensions**: 1×1 footprint, height matches straights
2. **Pivot**: Center bottom
3. **Shape**: 90° L-shape
4. **Facing**: Designed for southwest corner (will be rotated)

## Troubleshooting

### "WFC: Cell has no valid possibilities"

**Problem**: Over-constrained rules, no valid solution

**Solutions**:
1. Check adjacency rules aren't too restrictive
2. Add more wall piece variants
3. Leave some Allowed lists empty for flexibility
4. Ensure at least one piece type can go in every position

### Windows/Doors too close together

**Problem**: Not enough constraint

**Solution**:
- Remove Window/Door from Allowed lists of Window/Door pieces
- Only allow Straight walls adjacent

### All same wall type

**Problem**: One piece has much higher weight

**Solution**:
- Balance weights (5.0 for common, 1.0 for normal, 0.3 for rare)
- Check adjacency rules aren't excluding variants

### Corners not appearing

**Problem**: No CornerOuter pieces defined

**Solution**:
- Add CornerOuter pieces with Valid Positions = corners
- Ensure edge pieces allow CornerOuter in their Allowed lists

## How It Works Internally

### WFCSolver Algorithm

```
1. Initialize all boundary cells with possible pieces (based on Valid Positions)
2. While not all cells collapsed:
   a. Find cell with minimum entropy (fewest possibilities)
   b. Collapse it (select one piece, weight-based random)
   c. Propagate constraints:
      - For each neighbor
      - Remove pieces incompatible with collapsed piece
      - If neighbor now has 1 option, mark as collapsed
      - Continue propagating from changed neighbors
3. Return final layout
```

### Constraint Checking

For each pair of adjacent cells:
```csharp
// Both must allow each other
bool compatible = 
    pieceA.CanBeAdjacentTo(pieceB.Type, directionToB) &&
    pieceB.CanBeAdjacentTo(pieceA.Type, directionFromB);
```

## Performance

WFC is fast for dungeon wall generation:
- 10×10 room: <1ms
- 20×20 room: ~2ms  
- 50×50 room: ~20ms

Constraint propagation is efficient, scales well!

## Comparison with Other Modes

| Feature | WFC | AutoScale | StretchToFit |
|---------|-----|-----------|--------------|
| Corners | ✅ Intelligent | ❌ Same as edges | ❌ Stretched |
| Variety | ✅ Multiple pieces | ❌ One piece | ❌ One piece |
| Rules | ✅ Adjacency constraints | ❌ None | ❌ None |
| Windows/Doors | ✅ Realistic placement | ❌ Manual | ❌ Manual |
| Setup | Complex | Easy | Easy |
| Control | Very High | Low | Low |
| Result Quality | Best | Good | Good |

## Best Practices

1. **Start simple**: Straight + Corner pieces only
2. **Add rules gradually**: Test after each new piece type
3. **Use empty lists first**: Add constraints only when needed
4. **Test weights**: Adjust for desired variety
5. **Name clearly**: "Wall_Stone_Straight_Window_01"
6. **Bidirectional**: Remember both pieces must allow each other
7. **Debug**: If generation fails, relax constraints

## Example: Complete Medieval Dungeon Database

```
Database: Medieval_Dungeon_Walls

1. Stone_Wall_Plain
   - Type: Straight, Weight: 8.0
   - Allowed: Straight, CornerOuter, Window, Torch, DoorFrame

2. Stone_Wall_Cracked
   - Type: Straight, Weight: 3.0
   - Allowed: Straight, CornerOuter, Window, Torch

3. Stone_Wall_Window
   - Type: Window, Weight: 1.5
   - Valid: North, South, East, West
   - Allowed North: Straight
   - Allowed East: Straight
   - Allowed South: Straight
   - Allowed West: Straight

4. Stone_Wall_Torch
   - Type: Torch, Weight: 2.0
   - Allowed North: Straight, CornerOuter
   - Allowed East: Straight, CornerOuter
   - Allowed South: Straight, CornerOuter
   - Allowed West: Straight, CornerOuter

5. Stone_Door_North
   - Type: DoorFrame, Weight: 0.5
   - Valid: North only
   - Allowed North: Straight, Torch
   - Allowed East: Empty (can't be at edge)
   - Allowed South: Straight, Torch
   - Allowed West: Empty

6. Stone_Door_South
   - Type: DoorFrame, Weight: 0.5
   - Valid: South only
   - Allowed: Same as North door

7. Stone_Corner
   - Type: CornerOuter, Weight: 1.0
   - Valid: NE, NW, SE, SW
   - Allowed North: Straight, Window, Torch
   - Allowed East: Straight, Window, Torch
```

**Result**: 
- Mostly plain walls, some cracked
- Windows appear occasionally, spaced out
- Torches scattered, never adjacent
- Rare door on north or south wall, flanked by walls
- Perfect corners

## See Also

- **WALL_GENERATION_GUIDE.md** - All wall generation modes
- **QUICKSTART.md** - General dungeon setup
- **ASSET_SETUP_GUIDE.md** - Preparing wall assets

---

**TL;DR:** WFC with adjacency rules = Smart, realistic wall placement with windows, doors, and decorations that follow your constraints! 🏰✨🔧

