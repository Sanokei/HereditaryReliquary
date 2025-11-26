using UnityEngine;
using System.Collections.Generic;

namespace DungeonBuilderSystem
{
    public static class WFCValidator
    {
        // Configuration
        private const int MIN_DOOR_SPACING = 3;

        public static bool ValidatePlacement(DungeonRoom room, Vector3Int cell, string objectTag, DungeonGridData data)
        {
            // 1. Location: Corners must only appear at Corner layers.
            if (objectTag.Contains("Corner"))
            {
                if (room.GetCellLayer(cell) != DungeonRoom.RoomLayer.Corner)
                {
                    return false;
                }
            }
            
            return true;
        }

        public static bool CheckSpacing(Vector3Int center, List<Vector3Int> existingRestrictedPositions, int radius)
        {
            if (existingRestrictedPositions == null) return true;
            foreach (var pos in existingRestrictedPositions)
            {
                if (Vector3Int.Distance(center, pos) < radius)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool ValidateDoor(DungeonRoom room, Vector3Int cell, List<Vector3Int> existingDoors)
        {
            // Rule 1: Must NOT be a Corner
            if (room.GetCellLayer(cell) == DungeonRoom.RoomLayer.Corner)
            {
                return false;
            }

            // Rule 2: Spacing from other doors
            if (!CheckSpacing(cell, existingDoors, MIN_DOOR_SPACING))
            {
                return false;
            }

            // Rule 3: Must have a valid "outward" normal (not ambiguous)
            Vector3Int normal = room.GetPerimeterNormal(cell);
            if (normal == Vector3Int.zero) return false; // Trapped?
            
            return true;
        }
    }
}
