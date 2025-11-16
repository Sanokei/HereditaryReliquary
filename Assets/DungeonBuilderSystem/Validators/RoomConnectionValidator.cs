using System.Collections.Generic;
using UnityEngine;

namespace GridBuilder.Core
{
    /// <summary>
    /// Validator for placing dungeon rooms that checks doorway alignment and prevents overlaps.
    /// Ensures rooms connect properly via matching doorways.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomConnectionValidator", menuName = "GridBuilder/Validators/Room Connection Validator")]
    public class RoomConnectionValidator : PlacementValidatorSO
    {
        [Tooltip("Dungeon objects database used to identify doorways")]
        [SerializeField] private DungeonObjectsDatabaseSO dungeonDatabase;

        [Tooltip("If true, requires at least one doorway connection to existing rooms")]
        [SerializeField] private bool requireConnection = true;

        [Tooltip("If true, allows placement even if no existing rooms (for first room)")]
        [SerializeField] private bool allowFirstRoom = true;

        [Tooltip("Maximum distance between doorways to consider them connected (in world units)")]
        [SerializeField] private float connectionDistance = 2f;

        public override bool ValidatePlacement(PlacementValidationContext context)
        {
            if (dungeonDatabase == null)
            {
                Debug.LogWarning("RoomConnectionValidator: No dungeon database assigned");
                return false;
            }

            // Get the dungeon room being placed
            ObjectData roomData = context.database != null ? 
                context.database.objectsData.Find(obj => obj.ID == context.objectID) : null;

            if (roomData == null || roomData.Prefab == null)
            {
                return false;
            }

            // Check if the prefab has a DungeonRoomBuilder component
            DungeonRoomBuilder roomBuilder = roomData.Prefab.GetComponent<DungeonRoomBuilder>();
            if (roomBuilder == null)
            {
                Debug.LogWarning($"RoomConnectionValidator: Prefab {roomData.Name} does not have DungeonRoomBuilder component");
                return false;
            }

            // Get all existing dungeon rooms in the scene
            List<DungeonRoomBuilder> existingRooms = FindExistingRooms(context);

            // If this is the first room and we allow it, placement is valid
            if (existingRooms.Count == 0 && allowFirstRoom)
            {
                return true;
            }

            // If we require connection and there are existing rooms, check for valid connections
            if (requireConnection && existingRooms.Count > 0)
            {
                bool hasValidConnection = CheckForValidConnections(
                    roomBuilder, 
                    existingRooms, 
                    context.gridPosition, 
                    context.rotation,
                    context.referenceGrid
                );

                if (!hasValidConnection)
                {
                    return false;
                }
            }

            // Check for boundary overlaps with existing rooms
            if (!CheckForOverlaps(roomBuilder, existingRooms, context.gridPosition, context.referenceGrid))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Finds all existing dungeon rooms in active containers
        /// </summary>
        private List<DungeonRoomBuilder> FindExistingRooms(PlacementValidationContext context)
        {
            List<DungeonRoomBuilder> rooms = new List<DungeonRoomBuilder>();

            if (context.activeContainers == null)
                return rooms;

            foreach (var container in context.activeContainers)
            {
                if (container == null)
                    continue;

                // Find all DungeonRoomBuilder components in the container's hierarchy
                DungeonRoomBuilder[] foundRooms = container.GetComponentsInChildren<DungeonRoomBuilder>();
                rooms.AddRange(foundRooms);
            }

            return rooms;
        }

        /// <summary>
        /// Checks if the room has valid doorway connections to existing rooms
        /// </summary>
        private bool CheckForValidConnections(
            DungeonRoomBuilder newRoom,
            List<DungeonRoomBuilder> existingRooms,
            Vector3Int gridPosition,
            float rotation,
            Grid referenceGrid)
        {
            if (dungeonDatabase == null || newRoom == null)
                return false;

            // Get doorways from the new room
            List<DoorwayInfo> newRoomDoorways = newRoom.GetDoorwayPositions(dungeonDatabase);

            // Transform doorways to world space based on placement position
            List<DoorwayInfo> transformedDoorways = TransformDoorways(
                newRoomDoorways,
                gridPosition,
                rotation,
                referenceGrid,
                newRoom.CellSize
            );

            // Check each existing room for matching doorways
            foreach (var existingRoom in existingRooms)
            {
                if (existingRoom == newRoom)
                    continue;

                List<DoorwayInfo> existingDoorways = existingRoom.GetDoorwayPositions(dungeonDatabase);

                // Check if any doorways align and face each other
                foreach (var newDoorway in transformedDoorways)
                {
                    foreach (var existingDoorway in existingDoorways)
                    {
                        float distance = Vector3.Distance(newDoorway.worldPosition, existingDoorway.worldPosition);
                        
                        if (distance <= connectionDistance)
                        {
                            // Check if doorways face each other
                            if (AreDoorwaysFacing(newDoorway.direction, existingDoorway.direction))
                            {
                                return true; // Found a valid connection
                            }
                        }
                    }
                }
            }

            return false; // No valid connections found
        }

        /// <summary>
        /// Transforms doorway positions from local to world space
        /// </summary>
        private List<DoorwayInfo> TransformDoorways(
            List<DoorwayInfo> doorways,
            Vector3Int gridPosition,
            float rotation,
            Grid grid,
            int cellSize)
        {
            List<DoorwayInfo> transformed = new List<DoorwayInfo>();

            if (grid == null)
                return transformed;

            Vector3 worldBase = grid.GetCellCenterWorld(gridPosition);
            Quaternion rotQuat = Quaternion.Euler(0, rotation, 0);

            foreach (var doorway in doorways)
            {
                Vector3 localPos = new Vector3(
                    doorway.gridPosition.x * cellSize,
                    doorway.gridPosition.y * cellSize,
                    doorway.gridPosition.z * cellSize
                );

                Vector3 rotatedPos = rotQuat * localPos;
                Vector3 worldPos = worldBase + rotatedPos;

                // Also rotate the direction
                DoorwayDirection rotatedDirection = RotateDirection(doorway.direction, rotation);

                transformed.Add(new DoorwayInfo
                {
                    gridPosition = doorway.gridPosition,
                    direction = rotatedDirection,
                    worldPosition = worldPos
                });
            }

            return transformed;
        }

        /// <summary>
        /// Rotates a doorway direction by the given angle (in degrees)
        /// </summary>
        private DoorwayDirection RotateDirection(DoorwayDirection direction, float angleDegrees)
        {
            // Normalize angle to 0-360
            int angle = Mathf.RoundToInt(angleDegrees) % 360;
            if (angle < 0) angle += 360;

            // Round to nearest 90 degrees
            int rotations = Mathf.RoundToInt(angle / 90f);

            for (int i = 0; i < rotations; i++)
            {
                direction = RotateDirection90(direction);
            }

            return direction;
        }

        /// <summary>
        /// Rotates a direction 90 degrees clockwise
        /// </summary>
        private DoorwayDirection RotateDirection90(DoorwayDirection direction)
        {
            switch (direction)
            {
                case DoorwayDirection.North:
                    return DoorwayDirection.East;
                case DoorwayDirection.East:
                    return DoorwayDirection.South;
                case DoorwayDirection.South:
                    return DoorwayDirection.West;
                case DoorwayDirection.West:
                    return DoorwayDirection.North;
                default:
                    return direction;
            }
        }

        /// <summary>
        /// Checks if two doorways face each other (opposite directions)
        /// </summary>
        private bool AreDoorwaysFacing(DoorwayDirection dir1, DoorwayDirection dir2)
        {
            return (dir1 == DoorwayDirection.North && dir2 == DoorwayDirection.South) ||
                   (dir1 == DoorwayDirection.South && dir2 == DoorwayDirection.North) ||
                   (dir1 == DoorwayDirection.East && dir2 == DoorwayDirection.West) ||
                   (dir1 == DoorwayDirection.West && dir2 == DoorwayDirection.East);
        }

        /// <summary>
        /// Checks if the new room would overlap with any existing rooms
        /// </summary>
        private bool CheckForOverlaps(
            DungeonRoomBuilder newRoom,
            List<DungeonRoomBuilder> existingRooms,
            Vector3Int gridPosition,
            Grid referenceGrid)
        {
            if (newRoom == null || referenceGrid == null)
                return false;

            Vector3 worldBase = referenceGrid.GetCellCenterWorld(gridPosition);
            Vector2Int newRoomSize = newRoom.GridSize;
            int cellSize = newRoom.CellSize;

            // Calculate new room bounds in world space
            Bounds newRoomBounds = new Bounds(
                worldBase + new Vector3(
                    newRoomSize.x * cellSize * 0.5f,
                    0,
                    newRoomSize.y * cellSize * 0.5f
                ),
                new Vector3(
                    newRoomSize.x * cellSize,
                    10f, // Height doesn't matter much for 2D overlap
                    newRoomSize.y * cellSize
                )
            );

            // Check against all existing rooms
            foreach (var existingRoom in existingRooms)
            {
                if (existingRoom == newRoom)
                    continue;

                Vector2Int existingSize = existingRoom.GridSize;
                int existingCellSize = existingRoom.CellSize;

                Bounds existingBounds = new Bounds(
                    existingRoom.transform.position + new Vector3(
                        existingSize.x * existingCellSize * 0.5f,
                        0,
                        existingSize.y * existingCellSize * 0.5f
                    ),
                    new Vector3(
                        existingSize.x * existingCellSize,
                        10f,
                        existingSize.y * existingCellSize
                    )
                );

                // Check for intersection
                if (newRoomBounds.Intersects(existingBounds))
                {
                    // Rooms overlap - this is invalid unless doorways connect them
                    return false;
                }
            }

            return true; // No overlaps
        }
    }
}

