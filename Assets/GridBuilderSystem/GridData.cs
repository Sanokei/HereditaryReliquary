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
                                Vector3Int objectSize,
                                int ID,
                                int placedObjectIndex)
        {
            List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, objectSize);
            PlacementData data = new PlacementData(positionToOccupy, ID, placedObjectIndex);
            foreach (var pos in positionToOccupy)
            {
                if (placedObjects.ContainsKey(pos))
                    throw new Exception($"Dictionary already contains this cell position {pos}");
                placedObjects[pos] = data;
            }
        }

        private List<Vector3Int> CalculatePositions(Vector3Int gridPosition, Vector3Int objectSize)
        {
            List<Vector3Int> returnVal = new();
            for (int x = 0; x < objectSize.x; x++)
            {
                for (int y = 0; y < objectSize.y; y++)
                {
                    for (int z = 0; z < objectSize.z; z++)
                    {
                        returnVal.Add(gridPosition + new Vector3Int(x, y, z));
                    }
                }
            }
            return returnVal;
        }

        public bool CanPlaceObejctAt(Vector3Int gridPosition, Vector3Int objectSize)
        {
            List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, objectSize);
            
            // Check if any positions are already occupied
            foreach (var pos in positionToOccupy)
            {
                if (placedObjects.ContainsKey(pos))
                    return false;
            }
            return true;
        }

        public bool IsWithinGridBounds(Vector3Int gridPosition, Vector3Int objectSize)
        {
            // If grid properties are not set, assume unbounded grid
            if (!gridSize.HasValue || !anchorPoint.HasValue)
                return true;

            List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, objectSize);
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

        public IEnumerable<Vector3Int> GetPositionsForObject(Vector3Int gridPosition, Vector3Int objectSize)
        {
            return CalculatePositions(gridPosition, objectSize);
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