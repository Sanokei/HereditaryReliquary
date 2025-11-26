using System.Collections.Generic;
using UnityEngine;
using GridBuilder.Core;
using System.Linq;

namespace DungeonBuilderSystem.WFC.Validators
{
    [CreateAssetMenu(menuName = "Dungeon/Validators/Min Quantity")]
    public class MinQuantityValidator : DungeonValidator
    {
        public int TargetObjectID; // Using ID for simplicity, ideally use a flexible matching system
        public int MinCount = 2;

        public override bool ValidateGlobal(DungeonRoom room, List<PlacementData> allPlacements)
        {
            int count = allPlacements.Count(p => p.ID == TargetObjectID);
            return count >= MinCount;
        }
    }
}

