using System.Collections.Generic;
using UnityEngine;

namespace GridBuilder.Core
{
    /// <summary>
    /// Context structure containing all information needed for placement validation
    /// </summary>
    public struct PlacementValidationContext
    {
        public Vector3Int gridPosition;
        public List<Vector3Int> occupiedCells;
        public List<SplineGridContainer> activeContainers;
        public SplineGridContainer currentContainer;
        public ObjectsDatabaseSO database;
        public int objectID;
        public float rotation;
        public Grid referenceGrid;

        /// <summary>
        /// Gets object IDs from adjacent cells (4 directions: N, S, E, W in X/Z plane)
        /// </summary>
        public List<int> GetAdjacentObjectIDs()
        {
            List<int> adjacentIDs = new List<int>();
            
            if (activeContainers == null || activeContainers.Count == 0)
                return adjacentIDs;

            // Define 4 directions in X/Z plane (N, S, E, W)
            Vector3Int[] directions = new Vector3Int[]
            {
                new Vector3Int(0, 0, 1),  // North
                new Vector3Int(0, 0, -1), // South
                new Vector3Int(1, 0, 0),  // East
                new Vector3Int(-1, 0, 0)  // West
            };

            HashSet<int> foundIDs = new HashSet<int>();

            // Check each occupied cell for adjacent objects
            foreach (var cell in occupiedCells)
            {
                Vector3Int cellPos = gridPosition + cell;
                
                foreach (var direction in directions)
                {
                    Vector3Int adjacentPos = cellPos + direction;
                    
                    // Check all containers for object at this position
                    foreach (var container in activeContainers)
                    {
                        if (container == null || container.Grid == null || container.GridData == null)
                            continue;

                        // Convert to container's grid space if needed
                        Vector3Int containerPos = adjacentPos;
                        if (referenceGrid != null && container.Grid != referenceGrid)
                        {
                            Vector3 worldPos = referenceGrid.GetCellCenterWorld(adjacentPos);
                            containerPos = container.Grid.WorldToCell(worldPos);
                        }

                        int objectID = container.GridData.GetObjectIDAt(containerPos);
                        if (objectID != -1 && !foundIDs.Contains(objectID))
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
        /// Gets object IDs from adjacent cells that belong to specific databases
        /// Only checks containers whose layer mask matches the database's layer mask
        /// </summary>
        public List<int> GetAdjacentObjectIDsFromDatabases(List<ObjectsDatabaseSO> databases)
        {
            List<int> filteredIDs = new List<int>();

            if (databases == null || databases.Count == 0 || activeContainers == null || activeContainers.Count == 0)
                return filteredIDs;

            // Define 4 directions in X/Z plane (N, S, E, W)
            Vector3Int[] directions = new Vector3Int[]
            {
                new Vector3Int(0, 0, 1),  // North
                new Vector3Int(0, 0, -1), // South
                new Vector3Int(1, 0, 0),  // East
                new Vector3Int(-1, 0, 0)  // West
            };

            HashSet<int> foundIDs = new HashSet<int>();

            // For each required database, check adjacent cells in containers that match its layer mask
            foreach (var requiredDatabase in databases)
            {
                if (requiredDatabase == null || requiredDatabase.objectsData == null)
                    continue;

                // Filter containers by layer mask matching the database's layer mask
                foreach (var container in activeContainers)
                {
                    if (container == null || container.Grid == null || container.GridData == null)
                        continue;

                    // Only check containers whose layer mask matches the database's layer mask
                    if ((container.PlacementLayerMask.value & requiredDatabase.placementLayermask.value) == 0)
                        continue;

                    // Check each occupied cell for adjacent objects
                    foreach (var cell in occupiedCells)
                    {
                        Vector3Int cellPos = gridPosition + cell;
                        
                        foreach (var direction in directions)
                        {
                            Vector3Int adjacentPos = cellPos + direction;
                            
                            // Convert to container's grid space if needed
                            Vector3Int containerPos = adjacentPos;
                            if (referenceGrid != null && container.Grid != referenceGrid)
                            {
                                Vector3 worldPos = referenceGrid.GetCellCenterWorld(adjacentPos);
                                containerPos = container.Grid.WorldToCell(worldPos);
                            }

                            int objectID = container.GridData.GetObjectIDAt(containerPos);
                            if (objectID != -1 && !foundIDs.Contains(objectID))
                            {
                                // Check if this object ID exists in the required database
                                foreach (var objData in requiredDatabase.objectsData)
                                {
                                    if (objData.ID == objectID)
                                    {
                                        foundIDs.Add(objectID);
                                        filteredIDs.Add(objectID);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return filteredIDs;
        }

        /// <summary>
        /// Counts objects matching the criteria across all active containers
        /// Note: When counting across containers, objects that span multiple containers may be counted in each container.
        /// For accurate counts, use countAcrossAllContainers=false to count only in the current container.
        /// </summary>
        /// <param name="targetObjectID">Object ID to count (-1 for any object from database)</param>
        /// <param name="targetDatabase">Database to count objects from (null for any database)</param>
        /// <param name="countAcrossAllContainers">If true, count across all containers; if false, only current container</param>
        public int CountObjects(int targetObjectID, ObjectsDatabaseSO targetDatabase, bool countAcrossAllContainers = true)
        {
            List<SplineGridContainer> containersToCheck = countAcrossAllContainers ? activeContainers : 
                (currentContainer != null ? new List<SplineGridContainer> { currentContainer } : new List<SplineGridContainer>());

            if (containersToCheck == null || containersToCheck.Count == 0)
                return 0;

            int count = 0;

            foreach (var container in containersToCheck)
            {
                if (container == null || container.GridData == null)
                    continue;

                if (targetDatabase != null)
                {
                    // Only count from containers whose layer mask matches the database's layer mask
                    if ((container.PlacementLayerMask.value & targetDatabase.placementLayermask.value) == 0)
                        continue;

                    // Count objects from specific database
                    if (targetObjectID == -1)
                    {
                        // Count all objects from this database
                        count += container.GridData.CountObjectsByDatabase(targetDatabase);
                    }
                    else
                    {
                        // Verify the object ID exists in the database before counting
                        bool objectExistsInDatabase = false;
                        if (targetDatabase.objectsData != null)
                        {
                            foreach (var objData in targetDatabase.objectsData)
                            {
                                if (objData.ID == targetObjectID)
                                {
                                    objectExistsInDatabase = true;
                                    break;
                                }
                            }
                        }

                        if (objectExistsInDatabase)
                        {
                            count += container.GridData.CountObjectsByID(targetObjectID);
                        }
                    }
                }
                else
                {
                    // Count objects regardless of database
                    if (targetObjectID == -1)
                    {
                        // Count all objects
                        count += container.GridData.GetAllObjectCount();
                    }
                    else
                    {
                        // Count specific object ID
                        count += container.GridData.CountObjectsByID(targetObjectID);
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Checks if there is an object at the given position
        /// </summary>
        public bool HasObjectAt(Vector3Int position)
        {
            if (activeContainers == null || activeContainers.Count == 0)
                return false;

            foreach (var container in activeContainers)
            {
                if (container == null || container.Grid == null || container.GridData == null)
                    continue;

                Vector3Int containerPos = position;
                if (referenceGrid != null && container.Grid != referenceGrid)
                {
                    Vector3 worldPos = referenceGrid.GetCellCenterWorld(position);
                    containerPos = container.Grid.WorldToCell(worldPos);
                }

                if (container.GridData.HasObjectAt(containerPos))
                    return true;
            }

            return false;
        }
    }
}

