using System.Collections.Generic;
using UnityEngine;
using GridBuilder.Core;

namespace DungeonBuilderSystem.WFC.Validators
{
    [CreateAssetMenu(menuName = "Dungeon/Validators/Spacing")]
    public class SpacingValidator : DungeonValidator
    {
        public List<int> TargetObjectIDs; // Objects that enforce spacing (e.g. Doors, Windows)
        public float MinDistance = 3f; // In cells

        public override bool ValidatePlacement(DungeonRoom room, Vector3Int pos, int objectID, List<PlacementData> currentPlacements)
        {
            // If the object being placed is one of the targets
            if (TargetObjectIDs.Contains(objectID))
            {
                foreach (var placed in currentPlacements)
                {
                    // Check against other similar objects
                    if (TargetObjectIDs.Contains(placed.ID))
                    {
                        // We need the position of the placed object.
                        // PlacementData has a list of occupied positions.
                        // We check distance to ANY of them.
                        foreach (var occupied in placed.occupiedPositions)
                        {
                            if (Vector3Int.Distance(pos, occupied) < MinDistance)
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }
    }
}

