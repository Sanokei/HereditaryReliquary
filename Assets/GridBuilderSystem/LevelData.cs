using System.Collections.Generic;
using UnityEngine;

namespace GridBuilder.Core
{
    /// <summary>
    /// Represents a hazard placed in the level
    /// </summary>
    [System.Serializable]
    public class HazardData
    {
        public enum HazardType
        {
            Rock,
            Sandtrap,
            Whirlpool,
            Current
        }
        
        [Tooltip("Type of hazard")]
        public HazardType type = HazardType.Rock;
        
        [Tooltip("World position (XZ plane, Y is ignored)")]
        public Vector2 position = Vector2.zero;
        
        [Tooltip("Rotation in degrees (for Current direction)")]
        public float rotation = 0f;
        
        [Tooltip("Scale of the hazard")]
        public float scale = 1f;
        
        [Tooltip("Custom properties (JSON string for type-specific settings)")]
        public string customProperties = "";
    }
    
    /// <summary>
    /// ScriptableObject that stores level data including spline boundary, player spawn, and goal positions
    /// </summary>
    [CreateAssetMenu(fileName = "New Level", menuName = "Grid Builder/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Level Information")]
        [Tooltip("Display name for this level")]
        public string levelName = "New Level";
        
        [Tooltip("Par score for this level (target number of wave placements)")]
        [Min(0)]
        public int par = 0;
        
        [Header("Grid Settings")]
        [Tooltip("Cell size for the grid")]
        [Min(1)]
        public int gridCellSize = 1;
        
        [Tooltip("Grid size in cells (X and Z dimensions)")]
        [Min(1)]
        public Vector2 gridSize = new Vector2(10f, 10f);
        
        [Header("Spline Boundary")]
        [Tooltip("World space coordinates for the spline boundary (XZ plane, Y is ignored)")]
        public List<Vector2> splineBoundaryPoints = new List<Vector2>();
        
        [Header("Level Markers")]
        [Tooltip("Grid cell position where the player should spawn")]
        public Vector3Int playerSpawnCell = Vector3Int.zero;
        
        [Tooltip("Grid cell position where the goal/exit should be placed")]
        public Vector3Int goalCell = Vector3Int.zero;
        
        [Header("Optional Settings")]
        [Tooltip("Material for the grid visualization")]
        public Material gridMaterial;
        
        [Tooltip("Layer mask for placement")]
        public LayerMask placementLayerMask;
        
        [Tooltip("Prefab to use for the outside ring (optional)")]
        public GameObject ringPrefab;
        
        [Tooltip("Whether to auto-create the ring when level is built")]
        public bool autoCreateRing = false;
        
        [Header("Hazards")]
        [Tooltip("List of hazards placed in this level")]
        public List<HazardData> hazards = new List<HazardData>();
        
        [Header("Hazard Prefabs")]
        [Tooltip("Prefab for rock obstacles")]
        public GameObject rockPrefab;
        
        [Tooltip("Prefab for sandtraps")]
        public GameObject sandtrapPrefab;
        
        [Tooltip("Prefab for whirlpools")]
        public GameObject whirlpoolPrefab;
        
        [Tooltip("Prefab for currents")]
        public GameObject currentPrefab;
        
        /// <summary>
        /// Validates that the level data has minimum required information
        /// </summary>
        public bool IsValid()
        {
            // Need at least 3 points for a valid spline boundary
            if (splineBoundaryPoints == null || splineBoundaryPoints.Count < 3)
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Gets the spline boundary points as Vector3 (with Y = 0)
        /// </summary>
        public List<Vector3> GetSplineBoundaryPoints3D()
        {
            List<Vector3> points = new List<Vector3>();
            foreach (Vector2 point in splineBoundaryPoints)
            {
                points.Add(new Vector3(point.x, 0f, point.y));
            }
            return points;
        }
    }
}

