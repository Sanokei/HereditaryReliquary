# Dungeon Builder System

A comprehensive system for creating and connecting dungeon rooms with automatic wall generation, structural object placement, and doorway-based room connections.

## Overview

The Dungeon Builder System extends the Grid Builder System to support procedural dungeon creation with modular rooms that can be connected via doorways. Each room is a self-contained unit with walls, an internal grid for object placement, and configurable doorways for connections.

## Core Components

### 1. DungeonRoomBuilder
**Location:** `DungeonBuilderSystem/DungeonRoomBuilder.cs`

MonoBehaviour component that defines a dungeon room.

**Features:**
- Automatic wall generation around room boundaries
- Internal SplineGridContainer for playable area (room size - 2 for walls)
- Manages placed structural objects (walls, doors, pillars, decorations)
- Tracks doorway positions for room connections
- Can be saved as prefabs for reusability

**Key Properties:**
- `gridSize`: Total room dimensions (includes walls)
- `cellSize`: Size of each grid cell in world units
- `defaultWallPrefab`: Prefab used for auto-generated walls
- `placementLayerMask`: Layer for grid placement

**Key Methods:**
- `GenerateWalls()`: Creates walls around boundary
- `GetPlayableGridSize()`: Returns interior size (gridSize - 2)
- `GetDoorwayPositions()`: Extracts all doorway locations
- `CanConnectToRoom()`: Checks if rooms can connect via doorways

### 2. DungeonRoomEditor
**Location:** `DungeonBuilderSystem/Editor/DungeonRoomEditor.cs`

Advanced EditorWindow for visually editing dungeon rooms.

**Features:**
- 3D preview with camera controls (right-click rotate, scroll zoom)
- Left panel with thumbnail-based object browser
- Multiple editor modes: Place, Select, Move, Rotate, Delete
- Drag selection for multi-object manipulation
- Real-time visual feedback
- Save/load room configurations

**UI Layout:**
```
[Top Toolbar: Save | Cancel | Room Info | Object Count]
[Split View]
  Left Panel (220px):
    - Scrollable object list
    - Thumbnails with names
    - Click to select for placement
  
  Right Panel:
    [Action Toolbar: Place | Select | Move | Rotate | Delete]
    - 3D interactive preview
    - Selection box overlay
    - Camera orbit controls
```

**Controls:**
- **Right-click + Drag**: Rotate camera
- **Scroll Wheel**: Zoom in/out
- **Left-click** (Place mode): Place selected object
- **Left-click + Drag** (Select mode): Box select objects
- **Left-click** (Rotate mode): Rotate selected object 90°
- **Left-click** (Delete mode): Delete clicked object

### 3. DungeonObjectsDatabaseSO
**Location:** `DungeonBuilderSystem/DungeonObjectsDatabaseSO.cs`

ScriptableObject database for structural elements.

**Features:**
- References an ObjectsDatabaseSO for object definitions
- Stores thumbnail sprites for UI display
- Provides filtered access to structural objects
- Identifies doorway objects via validators

**Key Methods:**
- `GetAllObjects()`: Returns all available objects
- `GetThumbnail()`: Gets thumbnail for object ID
- `GetDoorwayObjects()`: Filters objects with doorway validators
- `IsDoorway()`: Checks if object is a doorway

### 4. DoorwayValidator
**Location:** `DungeonBuilderSystem/Validators/DoorwayValidator.cs`

Validator that marks objects as doorways with specific directions.

**Directions:**
- North (+Z)
- South (-Z)
- East (+X)
- West (-X)

**Features:**
- Marks cells as doorway connection points
- Validates doorway facing for connections
- Optional boundary placement validation

### 5. RoomConnectionValidator
**Location:** `DungeonBuilderSystem/Validators/RoomConnectionValidator.cs`

Validator for room-to-room placement.

**Features:**
- Validates doorway alignment between rooms
- Prevents room boundary overlaps
- Ensures proper room connections
- Allows first room placement without connections

**Configuration:**
- `requireConnection`: Require doorway connections
- `allowFirstRoom`: Allow placement without existing rooms
- `connectionDistance`: Max distance for valid connections

### 6. DungeonSystemManager
**Location:** `DungeonBuilderSystem/DungeonSystemManager.cs`

Manages all active dungeon rooms and their connections.

**Features:**
- Tracks all active rooms in the scene
- Validates room placement
- Finds connections between rooms
- Visualizes doorways and connections with Gizmos

**Key Methods:**
- `RegisterRoom()`: Add room to active list
- `CanPlaceRoom()`: Validate room placement
- `FindConnections()`: Get doorway connections between rooms
- `GetRoomsNearPosition()`: Find nearby connectable rooms

### 7. DungeonThumbnailGenerator
**Location:** `DungeonBuilderSystem/Editor/DungeonThumbnailGenerator.cs`

Utility for generating thumbnail images for objects.

**Features:**
- Automatic thumbnail generation for all objects
- Configurable camera and lighting
- Saves thumbnails next to database asset
- Batch processing support

**Usage:**
1. Open via `Tools > GridBuilder > Generate Dungeon Thumbnails`
2. Select DungeonObjectsDatabaseSO
3. Click "Generate All Thumbnails"

## Setup Guide

### Creating a Dungeon Room

1. **Create Room GameObject:**
   ```
   GameObject > Create Empty
   Add Component > DungeonRoomBuilder
   ```

2. **Configure Room:**
   - Set `gridSize` (e.g., 10x10)
   - Set `cellSize` (default: 1)
   - Assign `defaultWallPrefab`
   - Set `placementLayerMask`

3. **Generate Walls:**
   - Click "Generate Walls" in inspector
   - Or enable `autoGenerateWalls`

4. **Open Room Editor:**
   - Assign `DungeonObjectsDatabaseSO`
   - Click "Open Room Editor"
   - Place structural objects

5. **Save as Prefab:**
   - Drag room to Project window
   - Add to ObjectsDatabaseSO for placement

### Creating Structural Objects

1. **Create Object Prefab:**
   - Model the object (wall, door, pillar, etc.)
   - Save as prefab

2. **Add to Database:**
   - Create/open ObjectsDatabaseSO
   - Add prefab to `objectsData`

3. **Configure Validators:**
   - For doors: Add DoorwayValidator
   - Set direction (North/South/East/West)

4. **Generate Thumbnails:**
   - Create DungeonObjectsDatabaseSO
   - Reference ObjectsDatabaseSO
   - Run thumbnail generator

### Placing Rooms in Grid System

1. **Setup BuildingSystemManager:**
   - Add DungeonSystemManager component
   - Reference BuildingSystemManager
   - Assign DungeonObjectsDatabaseSO

2. **Add Room to Database:**
   - Room prefab goes in ObjectsDatabaseSO
   - Add RoomConnectionValidator
   - Configure connection requirements

3. **Place in Scene:**
   - Use placement system
   - Validator checks doorway alignment
   - Prevents invalid placements

## Best Practices

### Room Design

1. **Minimum Size:** Use at least 3x3 grid (allows 1x1 interior)
2. **Wall Thickness:** Always 1 cell thick boundary
3. **Doorway Placement:** Place doors on walls, facing outward
4. **Cell Size Consistency:** Use same cell size across all rooms

### Doorway Configuration

1. **Facing Direction:** Always face outward from room
2. **Placement:** Center of wall segments
3. **Spacing:** Allow enough space for connections
4. **Validation:** Test connections before finalization

### Performance

1. **Object Pooling:** Reuse room prefabs
2. **LOD:** Use LOD groups for complex rooms
3. **Occlusion:** Configure occlusion culling
4. **Batching:** Keep similar materials together

## Integration with Grid Builder System

The dungeon system integrates seamlessly with the existing Grid Builder System:

- **SplineGridContainer:** Each room contains internal grid
- **BuildingSystemManager:** Manages room placement
- **PlacementValidators:** Room connection validation
- **ObjectPlacer:** Handles room instantiation
- **GridData:** Tracks placed rooms and objects

## API Reference

### DungeonRoomBuilder

```csharp
// Get playable area size
Vector2Int playableSize = roomBuilder.GetPlayableGridSize();

// Get all doorways
List<DoorwayInfo> doorways = roomBuilder.GetDoorwayPositions(database);

// Check room connection
bool canConnect = roomBuilder.CanConnectToRoom(otherRoom, database);

// Generate/clear walls
roomBuilder.GenerateWalls();
roomBuilder.ClearWalls();
```

### DungeonSystemManager

```csharp
// Check room placement
bool canPlace = manager.CanPlaceRoom(room, worldPosition);

// Find connections
var connections = manager.FindConnections(room1, room2);

// Find nearby rooms
List<DungeonRoomBuilder> nearby = manager.GetRoomsNearPosition(position, radius);
```

### DoorwayValidator

```csharp
// Get direction vector
Vector3Int dirVector = doorwayValidator.GetDirectionVector();

// Check connection compatibility
bool canConnect = doorway1.CanConnectWith(doorway2);

// Get opposite direction
DoorwayDirection opposite = doorwayValidator.GetOppositeDirection();
```

## Troubleshooting

### Walls Not Generating
- Check `defaultWallPrefab` is assigned
- Verify `gridSize` is at least 3x3
- Call `GenerateWalls()` manually if needed

### Room Editor Not Opening
- Assign `DungeonObjectsDatabaseSO` in inspector
- Ensure database references valid `ObjectsDatabaseSO`
- Check for editor script errors in console

### Doorways Not Connecting
- Verify doorways face opposite directions
- Check connection distance threshold
- Ensure doorways are within range (< 2 cells)
- Validate placement layer masks match

### Thumbnails Not Showing
- Run thumbnail generator utility
- Check thumbnail folder exists
- Verify sprite import settings
- Reassign thumbnails if needed

## File Structure

```
DungeonBuilderSystem/
├── DungeonRoomBuilder.cs
├── DungeonObjectsDatabaseSO.cs
├── DungeonSystemManager.cs
├── README.md
├── Editor/
│   ├── DungeonRoomEditor.cs
│   ├── DungeonRoomBuilderEditor.cs
│   └── DungeonThumbnailGenerator.cs
└── Validators/
    ├── DoorwayValidator.cs
    └── RoomConnectionValidator.cs
```

## Future Enhancements

- Procedural room generation
- Room templates library
- Minimap generation
- Pathfinding integration
- Runtime room modifications
- Multi-floor support
- Room themes and variations
- Advanced connection types (stairs, elevators)

## Credits

Built on top of the Grid Builder System.
Integrates with Unity Splines package.

## License

Same as parent Grid Builder System.

