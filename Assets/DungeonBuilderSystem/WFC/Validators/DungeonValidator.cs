using System.Collections.Generic;
using UnityEngine;
using GridBuilder.Core;

namespace DungeonBuilderSystem.WFC
{
    public abstract class DungeonValidator : ScriptableObject
    {
        /// <summary>
        /// Checks if an object can be placed at a specific position.
        /// </summary>
        public virtual bool ValidatePlacement(DungeonRoom room, Vector3Int pos, int objectID, List<PlacementData> currentPlacements)
        {
            return true;
        }

        /// <summary>
        /// Checks if the entire generated set is valid (e.g. min counts).
        /// </summary>
        public virtual bool ValidateGlobal(DungeonRoom room, List<PlacementData> allPlacements)
        {
            return true;
        }
    }
}

