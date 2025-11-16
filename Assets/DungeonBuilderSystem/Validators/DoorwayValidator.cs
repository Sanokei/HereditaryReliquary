using UnityEngine;

namespace GridBuilder.Core
{
    /// <summary>
    /// Direction a doorway is facing (outward from the room)
    /// </summary>
    public enum DoorwayDirection
    {
        North,  // +Z
        South,  // -Z
        East,   // +X
        West    // -X
    }

    /// <summary>
    /// Validator that marks an object as a doorway with a specific direction.
    /// Doorways must be placed on room boundaries and face outward for proper room connections.
    /// </summary>
    [CreateAssetMenu(fileName = "DoorwayValidator", menuName = "GridBuilder/Validators/Doorway Validator")]
    public class DoorwayValidator : PlacementValidatorSO
    {
        [Tooltip("Direction the doorway faces (outward from the room)")]
        [SerializeField] private DoorwayDirection direction = DoorwayDirection.North;

        [Tooltip("If true, validates that doorway is placed on the appropriate room boundary")]
        [SerializeField] private bool validateBoundaryPlacement = true;

        [Tooltip("Minimum room size required for doorway placement (0 = no requirement)")]
        [SerializeField] private int minimumRoomSize = 0;

        public DoorwayDirection Direction => direction;

        public override bool ValidatePlacement(PlacementValidationContext context)
        {
            // Doorways should always be allowed for placement in the editor
            // The validation here is primarily for marking the doorway type
            // and optionally checking boundary placement

            if (!validateBoundaryPlacement)
            {
                return true;
            }

            // Optional: Validate that doorway is on the appropriate boundary
            // This would require checking against the room bounds from DungeonRoomBuilder
            // For now, we'll allow placement and let the room builder handle validation

            return true;
        }

        /// <summary>
        /// Gets the grid direction vector for this doorway
        /// </summary>
        public Vector3Int GetDirectionVector()
        {
            switch (direction)
            {
                case DoorwayDirection.North:
                    return new Vector3Int(0, 0, 1);
                case DoorwayDirection.South:
                    return new Vector3Int(0, 0, -1);
                case DoorwayDirection.East:
                    return new Vector3Int(1, 0, 0);
                case DoorwayDirection.West:
                    return new Vector3Int(-1, 0, 0);
                default:
                    return Vector3Int.zero;
            }
        }

        /// <summary>
        /// Checks if two doorways can connect (they must face each other)
        /// </summary>
        public bool CanConnectWith(DoorwayValidator other)
        {
            if (other == null)
                return false;

            // Doorways must face opposite directions to connect
            return GetOppositeDirection() == other.Direction;
        }

        /// <summary>
        /// Gets the opposite direction of this doorway
        /// </summary>
        public DoorwayDirection GetOppositeDirection()
        {
            switch (direction)
            {
                case DoorwayDirection.North:
                    return DoorwayDirection.South;
                case DoorwayDirection.South:
                    return DoorwayDirection.North;
                case DoorwayDirection.East:
                    return DoorwayDirection.West;
                case DoorwayDirection.West:
                    return DoorwayDirection.East;
                default:
                    return direction;
            }
        }
    }
}

