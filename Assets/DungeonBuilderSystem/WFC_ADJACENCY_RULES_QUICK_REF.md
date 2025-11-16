# WFC Adjacency Rules Quick Reference

Quick lookup table for setting up adjacency rules in your Wall Piece Database.

## Rule of Thumb

**Empty list = Allow ALL types**  
**Populated list = Allow ONLY these types**

## Common Patterns

### 1. Basic Straight Wall (Can be next to anything)

```
Allowed North: Leave EMPTY (or add all types)
Allowed East: Leave EMPTY
Allowed South: Leave EMPTY
Allowed West: Leave EMPTY
```

**Use**: Your most common, flexible wall piece

---

### 2. Window (Never next to other windows or doors)

```
Allowed North: Straight, CornerOuter
Allowed East: Straight, CornerOuter
Allowed South: Straight, CornerOuter
Allowed West: Straight, CornerOuter
```

**Result**: Windows automatically space out, never adjacent to doors

---

### 3. Door Frame (Must have walls on both sides)

```
Valid Positions: South (or whichever wall you want the door)
Allowed North: Straight, Torch
Allowed East: EMPTY (nothing - door can't be at edge)
Allowed South: Straight, Torch  
Allowed West: EMPTY (nothing - door can't be at edge)
```

**Result**: Doors only in middle of walls, flanked by straight walls

---

### 4. Torch/Decoration (Never next to other decorations)

```
Allowed North: Straight, CornerOuter (exclude Torch, Window, DoorFrame)
Allowed East: Straight, CornerOuter
Allowed South: Straight, CornerOuter
Allowed West: Straight, CornerOuter
```

**Result**: Decorations spread out along walls

---

### 5. Corner Piece (Connects edges properly)

```
Valid Positions: NorthEast, NorthWest, SouthEast, SouthWest
Allowed North: Straight, Window, Torch, DoorFrame
Allowed East: Straight, Window, Torch, DoorFrame
Allowed South: Leave EMPTY (or omit)
Allowed West: Leave EMPTY (or omit)
```

**Result**: Corners connect to edge pieces properly

---

## Direction Cheat Sheet (Rectangular Room)

Imagine walking **clockwise** around the room perimeter:

### South Wall (Bottom)
- **North** = Next piece to the **East** →

### East Wall (Right)
- **North** = Next piece going **North** ↑

### North Wall (Top)
- **North** = Next piece to the **West** ←

### West Wall (Left)
- **North** = Next piece going **South** ↓

### Corners
Corners connect two edges, so:
- **SouthWest**: North = East along south wall, East = North along west wall
- **SouthEast**: North = North along east wall, East = West along south wall
- **NorthEast**: North = West along north wall, East = South along east wall
- **NorthWest**: North = South along west wall, East = East along north wall

## Troubleshooting Matrix

| Problem | Solution |
|---------|----------|
| Windows too close | Remove `Window` from Window's Allowed lists |
| Doors too close | Remove `DoorFrame` from Door's Allowed lists |
| Decorations clustered | Remove decoration types from their own Allowed lists |
| Corner gaps | Add `Straight, Window, etc.` to Corner's Allowed North/East |
| "No valid possibilities" | Relax rules - add more types to Allowed lists or leave empty |
| All same wall | Check weights, add more variants, check rules aren't excluding them |
| Piece never appears | Check Valid Positions, check if rules exclude it everywhere |

## Weight Guidelines

Use these as starting points:

```
Plain walls:     5.0 - 10.0  (very common)
Variant walls:   2.0 - 3.0   (common)
Windows:         0.5 - 1.5   (uncommon)
Decorations:     1.0 - 2.0   (uncommon to common)
Doors:           0.1 - 0.5   (rare)
Special pieces:  0.1 - 0.3   (very rare)
Corners:         1.0         (normal, always needed)
```

## Bidirectional Rule Check

Remember: **Both pieces must allow each other!**

Example check for Window next to Straight:
- ✅ Window has `Straight` in Allowed North?
- ✅ Straight has `Window` in Allowed South?
- **Result**: ✅ Can be adjacent

Example check for Window next to Window:
- ❌ Window has `Window` in Allowed North? NO
- **Result**: ❌ Cannot be adjacent (windows won't touch!)

## Testing Your Rules

### Step 1: Start Minimal
```
1. Basic Straight wall (empty allowed lists)
2. Corner piece (allows Straight only)
```
Generate walls. Should work!

### Step 2: Add Windows
```
3. Window piece (allows Straight and Corner only)
```
Generate. Windows should appear occasionally.

### Step 3: Add Doors
```
4. Door piece (allows Straight only, on one side only)
```
Generate. Rare door should appear on specified wall.

### Step 4: Add Decorations
```
5. Torch piece (allows Straight and Corner, not other Torches)
```
Generate. Torches should be spread out.

### Step 5: Add Variants
```
6-10. More Straight variants with different weights
```
Generate. Should see variety!

## Copy-Paste Templates

### Template: Basic Straight Wall
```
Piece Name: Wall_[Theme]_Straight_[Number]
Prefab: [Your prefab]
Piece Type: Straight
Weight: 5.0
Valid Positions: [Leave empty or North, South, East, West]
Allowed North: [Empty]
Allowed East: [Empty]
Allowed South: [Empty]
Allowed West: [Empty]
```

### Template: Window Wall
```
Piece Name: Wall_[Theme]_Window_[Number]
Prefab: [Your prefab]
Piece Type: Window
Weight: 1.0
Valid Positions: North, South, East, West
Allowed North: Straight, CornerOuter
Allowed East: Straight, CornerOuter
Allowed South: Straight, CornerOuter
Allowed West: Straight, CornerOuter
```

### Template: Door Frame
```
Piece Name: Wall_[Theme]_Door_[Side]
Prefab: [Your prefab]
Piece Type: DoorFrame
Weight: 0.3
Valid Positions: [South] (or North, East, West - pick one)
Allowed North: Straight
Allowed East: [Empty = none]
Allowed South: Straight
Allowed West: [Empty = none]
```

### Template: Corner
```
Piece Name: Wall_[Theme]_Corner
Prefab: [Your prefab]
Piece Type: CornerOuter
Weight: 1.0
Valid Positions: NorthEast, NorthWest, SouthEast, SouthWest
Allowed North: Straight, Window
Allowed East: Straight, Window
Allowed South: [Empty]
Allowed West: [Empty]
```

### Template: Decoration (Torch, Shelf, etc.)
```
Piece Name: Wall_[Theme]_[Decoration]_[Number]
Prefab: [Your prefab]
Piece Type: [Torch/Shelf/Pillar]
Weight: 1.5
Valid Positions: North, South, East, West
Allowed North: Straight, CornerOuter
Allowed East: Straight, CornerOuter
Allowed South: Straight, CornerOuter
Allowed West: Straight, CornerOuter
```

## See Also

- **WFC_WALL_GUIDE.md** - Complete WFC documentation
- **WALL_GENERATION_GUIDE.md** - All generation modes
- **QUICKSTART.md** - Getting started

---

**Remember**: Start simple, test often, add constraints gradually! 🎯

