using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridBuilder.Core
{
    public class GridData
    {
        Dictionary<Vector3Int, PlacementData> placedObjects = new();
        private Vector3? gridSize = null;
        private Vector3? cellSize = null;
        private Vector3? anchorPoint = null;

        public void SetGridProperties(Vector3 gridSize, Vector3 cellSize, Vector3 anchorPoint)
        {
            this.gridSize = gridSize;
            this.cellSize = cellSize;
            this.anchorPoint = anchorPoint;
        }

        public void AddObjectAt(Vector3Int gridPosition,
                                List<Vector3Int> occupiedCells,
                                int ID,
                                int placedObjectIndex)
        {
            List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, occupiedCells);
            PlacementData data = new PlacementData(positionToOccupy, ID, placedObjectIndex);
            foreach (var pos in positionToOccupy)
            {
                if (placedObjects.ContainsKey(pos))
                    throw new Exception($"Dictionary already contains this cell position {pos}");
                placedObjects[pos] = data;
            }
        }

        private List<Vector3Int> CalculatePositions(Vector3Int gridPosition, List<Vector3Int> occupiedCells)
        {
            List<Vector3Int> returnVal = new();
            foreach (var cell in occupiedCells)
            {
                returnVal.Add(gridPosition + cell);
            }
            return returnVal;
        }

        public bool CanPlaceObejctAt(Vector3Int gridPosition, List<Vector3Int> occupiedCells)
        {
            List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, occupiedCells);
            
            // Check if any positions are already occupied
            foreach (var pos in positionToOccupy)
            {
                if (placedObjects.ContainsKey(pos))
                    return false;
            }
            return true;
        }

        public bool IsWithinGridBounds(Vector3Int gridPosition, List<Vector3Int> occupiedCells)
        {
            // If grid properties are not set, assume unbounded grid
            if (!gridSize.HasValue || !anchorPoint.HasValue)
                return true;

            List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, occupiedCells);
            Vector3 gridSizeValue = gridSize.Value;
            Vector3 anchorPointValue = anchorPoint.Value;

            // Calculate grid bounds in grid coordinates
            // anchorPoint is typically the center or corner of the grid
            // gridSize defines the size in world units, but we need to convert to grid cells
            // For simplicity, assuming anchorPoint is the minimum corner and gridSize is in grid cells
            Vector3Int minBound = new Vector3Int(
                Mathf.RoundToInt(anchorPointValue.x),
                Mathf.RoundToInt(anchorPointValue.y),
                Mathf.RoundToInt(anchorPointValue.z));
            Vector3Int maxBound = minBound + new Vector3Int(
                Mathf.RoundToInt(gridSizeValue.x),
                Mathf.RoundToInt(gridSizeValue.y),
                Mathf.RoundToInt(gridSizeValue.z));

            // Check if all positions are within bounds
            foreach (var pos in positionToOccupy)
            {
                if (pos.x < minBound.x || pos.x >= maxBound.x ||
                    pos.y < minBound.y || pos.y >= maxBound.y ||
                    pos.z < minBound.z || pos.z >= maxBound.z)
                {
                    return false;
                }
            }
            return true;
        }

        public IEnumerable<Vector3Int> GetPositionsForObject(Vector3Int gridPosition, List<Vector3Int> occupiedCells)
        {
            return CalculatePositions(gridPosition, occupiedCells);
        }

        public bool HasObjectAt(Vector3Int gridPosition)
        {
            return placedObjects.ContainsKey(gridPosition);
        }

        public bool HasObjectAtXZ(Vector3Int gridPosition)
        {
            foreach (var pos in placedObjects.Keys)
            {
                if (pos.x == gridPosition.x && pos.z == gridPosition.z)
                {
                    return true;
                }
            }

            return false;
        }

        internal int GetRepresentationIndex(Vector3Int gridPosition)
        {
            if (placedObjects.ContainsKey(gridPosition) == false)
                return -1;
            return placedObjects[gridPosition].PlacedObjectIndex;
        }

        internal void RemoveObjectAt(Vector3Int gridPosition)
        {
            foreach (var pos in placedObjects[gridPosition].occupiedPositions)
            {
                placedObjects.Remove(pos);
            }
        }

        /// <summary>
        /// Gets the object ID at a specific grid position
        /// </summary>
        public int GetObjectIDAt(Vector3Int gridPosition)
        {
            if (placedObjects.ContainsKey(gridPosition))
            {
                return placedObjects[gridPosition].ID;
            }
            return -1;
        }

        /// <summary>
        /// Gets object IDs from adjacent cells (4 directions: N, S, E, W in X/Z plane)
        /// </summary>
        public List<int> GetAdjacentObjectIDs(Vector3Int gridPosition, List<Vector3Int> occupiedCells)
        {
            List<int> adjacentIDs = new List<int>();
            HashSet<int> foundIDs = new HashSet<int>();

            // Define 4 directions in X/Z plane (N, S, E, W)
            Vector3Int[] directions = new Vector3Int[]
            {
                new Vector3Int(0, 0, 1),  // North
                new Vector3Int(0, 0, -1), // South
                new Vector3Int(1, 0, 0),  // East
                new Vector3Int(-1, 0, 0)  // West
            };

            List<Vector3Int> positionsToCheck = CalculatePositions(gridPosition, occupiedCells);

            foreach (var pos in positionsToCheck)
            {
                foreach (var direction in directions)
                {
                    Vector3Int adjacentPos = pos + direction;
                    if (placedObjects.ContainsKey(adjacentPos))
                    {
                        int objectID = placedObjects[adjacentPos].ID;
                        if (!foundIDs.Contains(objectID))
                        {
                            foundIDs.Add(objectID);
                            adjacentIDs.Add(objectID);
                        }
                    }
                }
            }

            return adjacentIDs;
        }

        /// <summary>
        /// Counts all objects with the given ID
        /// </summary>
        public int CountObjectsByID(int objectID)
        {
            HashSet<int> countedObjects = new HashSet<int>();
            int count = 0;

            foreach (var kvp in placedObjects)
            {
                if (kvp.Value.ID == objectID)
                {
                    // Count unique objects (by checking if we've seen this object's index before)
                    if (!countedObjects.Contains(kvp.Value.PlacedObjectIndex))
                    {
                        countedObjects.Add(kvp.Value.PlacedObjectIndex);
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Counts all objects from the given database
        /// Note: This requires checking object IDs against the database, so it's a helper that works with database reference
        /// </summary>
        public int CountObjectsByDatabase(ObjectsDatabaseSO database)
        {
            if (database == null || database.objectsData == null)
                return 0;

            HashSet<int> databaseObjectIDs = new HashSet<int>();
            foreach (var objData in database.objectsData)
            {
                databaseObjectIDs.Add(objData.ID);
            }

            HashSet<int> countedObjects = new HashSet<int>();
            int count = 0;

            foreach (var kvp in placedObjects)
            {
                if (databaseObjectIDs.Contains(kvp.Value.ID))
                {
                    // Count unique objects
                    if (!countedObjects.Contains(kvp.Value.PlacedObjectIndex))
                    {
                        countedObjects.Add(kvp.Value.PlacedObjectIndex);
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Gets the total count of all unique objects in the grid
        /// </summary>
        public int GetAllObjectCount()
        {
            HashSet<int> uniqueObjects = new HashSet<int>();
            foreach (var kvp in placedObjects)
            {
                uniqueObjects.Add(kvp.Value.PlacedObjectIndex);
            }
            return uniqueObjects.Count;
        }

        /// <summary>
        /// Gets all unique object IDs in the grid
        /// </summary>
        public List<int> GetAllObjectIDs()
        {
            HashSet<int> uniqueIDs = new HashSet<int>();
            foreach (var kvp in placedObjects)
            {
                uniqueIDs.Add(kvp.Value.ID);
            }
            return new List<int>(uniqueIDs);
        }
        
        /// <summary>
        /// Gets all unique PlacementData objects in the grid
        /// </summary>
        public List<PlacementData> GetAllPlacementData()
        {
            HashSet<int> seenIndices = new HashSet<int>();
            List<PlacementData> result = new List<PlacementData>();
            
            foreach (var kvp in placedObjects)
            {
                PlacementData data = kvp.Value;
                if (!seenIndices.Contains(data.PlacedObjectIndex))
                {
                    seenIndices.Add(data.PlacedObjectIndex);
                    result.Add(data);
                }
            }
            
            return result;
        }
    }

    public class PlacementData
    {
        public List<Vector3Int> occupiedPositions;
        public int ID { get; private set; }
        public int PlacedObjectIndex { get; private set; }

        public PlacementData(List<Vector3Int> occupiedPositions, int iD, int placedObjectIndex)
        {
            this.occupiedPositions = occupiedPositions;
            ID = iD;
            PlacedObjectIndex = placedObjectIndex;
        }
    }
}