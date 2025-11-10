using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridBuilder.Core
{
    public class GridData
    {
        Dictionary<Vector3Int, PlacementData> placedObjects = new();
        private BoundsInt? gridBounds = null;

        public void SetGridBounds(BoundsInt bounds)
        {
            gridBounds = bounds;
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
            
            // Check if all positions are within grid bounds (if bounds are set)
            if (gridBounds.HasValue)
            {
                foreach (var pos in positionToOccupy)
                {
                    if (!gridBounds.Value.Contains(pos))
                        return false;
                }
            }
            
            // Check if any positions are already occupied
            foreach (var pos in positionToOccupy)
            {
                if (placedObjects.ContainsKey(pos))
                    return false;
            }
            return true;
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