using System.Collections.Generic;
using UnityEngine;

namespace GridBuilder.Core
{
    /// <summary>
    /// Interface for custom placement validation rules
    /// </summary>
    public interface IPlacementValidator
    {
        /// <summary>
        /// Validates if an object can be placed at the given position
        /// </summary>
        /// <param name="context">Context containing all placement information</param>
        /// <returns>True if placement is valid, false otherwise</returns>
        bool ValidatePlacement(PlacementValidationContext context);
    }
}

