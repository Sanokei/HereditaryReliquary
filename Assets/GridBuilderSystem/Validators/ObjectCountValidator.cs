using UnityEngine;

namespace GridBuilder.Core
{
    /// <summary>
    /// Validator that enforces minimum/maximum count limits for objects from a database
    /// </summary>
    [CreateAssetMenu(fileName = "ObjectCountValidator", menuName = "GridBuilder/Validators/Object Count Validator")]
    public class ObjectCountValidator : PlacementValidatorSO
    {
        [Tooltip("Database to count objects from")]
        [SerializeField] private ObjectsDatabaseSO targetDatabase;

        [Tooltip("Specific object ID to count (-1 for any object from the database)")]
        [SerializeField] private int targetObjectID = -1;

        [Tooltip("Maximum allowed count (0 = unlimited)")]
        [SerializeField] private int maxCount = 0;

        [Tooltip("Minimum required count (0 = no requirement)")]
        [SerializeField] private int minCount = 0;

        [Tooltip("If true, count across all containers. If false, only count in current container.")]
        [SerializeField] private bool countAcrossAllContainers = true;

        public override bool ValidatePlacement(PlacementValidationContext context)
        {
            if (targetDatabase == null)
            {
                // No target database specified, validation passes
                return true;
            }

            // Count existing objects matching the criteria
            int currentCount = context.CountObjects(targetObjectID, targetDatabase, countAcrossAllContainers);

            // Check minimum requirement
            if (minCount > 0 && currentCount < minCount)
            {
                return false;
            }

            // Check maximum limit (if maxCount is 0, it means unlimited)
            if (maxCount > 0 && currentCount >= maxCount)
            {
                return false;
            }

            return true;
        }
    }
}

