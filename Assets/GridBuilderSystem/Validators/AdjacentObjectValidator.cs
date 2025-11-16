using System.Collections.Generic;
using UnityEngine;

namespace GridBuilder.Core
{
    /// <summary>
    /// Validator that requires the object to be placed adjacent to objects from specific databases
    /// </summary>
    [CreateAssetMenu(fileName = "AdjacentObjectValidator", menuName = "GridBuilder/Validators/Adjacent Object Validator")]
    public class AdjacentObjectValidator : PlacementValidatorSO
    {
        [Tooltip("Objects must be adjacent to objects from these databases")]
        [SerializeField] private List<ObjectsDatabaseSO> requiredDatabases = new List<ObjectsDatabaseSO>();

        [Tooltip("If true, object must be adjacent to objects from ALL databases. If false, any one database is sufficient.")]
        [SerializeField] private bool requireAllDatabases = false;

        public override bool ValidatePlacement(PlacementValidationContext context)
        {
            if (requiredDatabases == null || requiredDatabases.Count == 0)
            {
                // No requirements, so validation passes
                return true;
            }

            // Get adjacent object IDs from required databases
            List<int> adjacentIDs = context.GetAdjacentObjectIDsFromDatabases(requiredDatabases);

            if (adjacentIDs.Count == 0)
            {
                // No adjacent objects from required databases
                return false;
            }

            if (requireAllDatabases)
            {
                // Need to check if we have adjacent objects from ALL required databases
                // We need to verify that for each database, there's at least one adjacent object
                foreach (var database in requiredDatabases)
                {
                    bool foundFromThisDatabase = false;

                    // Check if any adjacent object belongs to this database
                    foreach (var container in context.activeContainers)
                    {
                        if (container == null || container.ObjectsDatabase != database)
                            continue;

                        // Check if any of the adjacent IDs are in this database
                        foreach (var objData in database.objectsData)
                        {
                            if (adjacentIDs.Contains(objData.ID))
                            {
                                foundFromThisDatabase = true;
                                break;
                            }
                        }

                        if (foundFromThisDatabase)
                            break;
                    }

                    if (!foundFromThisDatabase)
                    {
                        // Missing adjacency to objects from this database
                        return false;
                    }
                }

                return true;
            }
            else
            {
                // Any one database is sufficient - we already have adjacent objects
                return true;
            }
        }
    }
}

