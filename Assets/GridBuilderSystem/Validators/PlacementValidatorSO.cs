using UnityEngine;

namespace GridBuilder.Core
{
    /// <summary>
    /// Base class for placement validators as ScriptableObjects
    /// Allows validators to be created as assets and referenced in ObjectData
    /// </summary>
    public abstract class PlacementValidatorSO : ScriptableObject, IPlacementValidator
    {
        /// <summary>
        /// Validates if an object can be placed at the given position
        /// </summary>
        /// <param name="context">Context containing all placement information</param>
        /// <returns>True if placement is valid, false otherwise</returns>
        public abstract bool ValidatePlacement(PlacementValidationContext context);
    }
}

