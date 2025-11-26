using System.Collections.Generic;
using UnityEngine;
using GridBuilder.Core;
using System.Linq;

namespace DungeonBuilderSystem
{
    public class DungeonGridData : GridData
    {
        // Parallel structure to support multiple items per cell
        // We sync the 'primary' item to the base 'placedObjects' to keep compatibility with systems that use base GridData
        // but for Dungeon logic we check this structure.
        private Dictionary<Vector3Int, List<PlacementData>> multiLayerObjects = new Dictionary<Vector3Int, List<PlacementData>>();

        public void Clear()
        {
            multiLayerObjects.Clear();
            placedObjects.Clear();
        }

        public override void AddObjectAt(Vector3Int gridPosition, List<Vector3Int> occupiedCells, int ID, int placedObjectIndex)
        {
            List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, occupiedCells);
            PlacementData data = new PlacementData(positionToOccupy, ID, placedObjectIndex);

            foreach (var pos in positionToOccupy)
            {
                if (!multiLayerObjects.ContainsKey(pos))
                {
                    multiLayerObjects[pos] = new List<PlacementData>();
                }
                
                // Allow multiple objects.
                multiLayerObjects[pos].Add(data);

                // Update base class for compatibility (store the first one or the most "solid" one?)
                try 
                {
                    if (!placedObjects.ContainsKey(pos))
                    {
                        placedObjects[pos] = data;
                    }
                    else
                    {
                        // Keep the first one placed as primary (Perimeter)
                    }
                }
                catch
                {
                    // Ignore base collisions as we handle multi-layer
                }
            }
        }

        public override bool CanPlaceObejctAt(Vector3Int gridPosition, List<Vector3Int> occupiedCells)
        {
            // Override to allow placement if layers are different.
            // For now, let's return true if we haven't reached a hard cap (e.g. 3 items).
            
            List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, occupiedCells);
            foreach (var pos in positionToOccupy)
            {
                if (multiLayerObjects.ContainsKey(pos))
                {
                     if (multiLayerObjects[pos].Count >= 3) // Arbitrary limit for now: Floor, Prop, Item
                        return false;
                }
            }
            return true;
        }

        public override bool HasObjectAt(Vector3Int gridPosition)
        {
            return multiLayerObjects.ContainsKey(gridPosition) && multiLayerObjects[gridPosition].Count > 0;
        }

        public override int GetObjectIDAt(Vector3Int gridPosition)
        {
            // Return the ID of the first object (Perimeter/Structure usually placed first)
            if (multiLayerObjects.ContainsKey(gridPosition) && multiLayerObjects[gridPosition].Count > 0)
            {
                return multiLayerObjects[gridPosition][0].ID;
            }
            return -1;
        }

        public List<PlacementData> GetObjectsAt(Vector3Int gridPosition)
        {
            if (multiLayerObjects.ContainsKey(gridPosition))
                return multiLayerObjects[gridPosition];
            return new List<PlacementData>();
        }
        
        internal override void RemoveObjectAt(Vector3Int gridPosition)
        {
            if (multiLayerObjects.ContainsKey(gridPosition))
            {
                // Get all objects at this position
                var list = multiLayerObjects[gridPosition];
                // We need to remove these objects from ALL their occupied cells.
                
                // Make a copy to iterate
                var toRemove = new List<PlacementData>(list);
                
                foreach (var data in toRemove)
                {
                     foreach (var pos in data.occupiedPositions)
                     {
                         if (multiLayerObjects.ContainsKey(pos))
                         {
                             multiLayerObjects[pos].Remove(data);
                             if (multiLayerObjects[pos].Count == 0)
                                 multiLayerObjects.Remove(pos);
                             
                             // Sync base
                             if (placedObjects.ContainsKey(pos) && placedObjects[pos] == data)
                             {
                                 placedObjects.Remove(pos);
                                 // Promote next item to primary if exists
                                 if (multiLayerObjects.ContainsKey(pos) && multiLayerObjects[pos].Count > 0)
                                 {
                                     placedObjects[pos] = multiLayerObjects[pos][0];
                                 }
                             }
                         }
                     }
                }
            }
        }
        
        // Helper to get specific placement data
        public PlacementData GetPlacementData(Vector3Int gridPos, int objectIndex)
        {
             if (multiLayerObjects.ContainsKey(gridPos))
             {
                 return multiLayerObjects[gridPos].FirstOrDefault(p => p.PlacedObjectIndex == objectIndex);
             }
             return null;
        }
    }
}
