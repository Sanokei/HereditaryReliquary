using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace GridBuilder.Core
{
    /// <summary>
    /// Runtime script that builds a level from LevelData ScriptableObject
    /// Creates the SplineGridContainer, places player and goal markers
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        [Header("Level Data")]
        [Tooltip("The level data to build")]
        public LevelData levelData;
        
        [Header("Prefabs")]
        [Tooltip("Prefab for the player spawn marker (optional)")]
        [SerializeField] private GameObject playerMarkerPrefab;
        
        [Tooltip("Prefab for the goal/exit marker (optional)")]
        [SerializeField] private GameObject goalMarkerPrefab;
        
        [Header("Parent Objects")]
        [Tooltip("Parent transform for the grid container (optional)")]
        [SerializeField] private Transform gridParent;
        
        [Tooltip("Parent transform for markers (optional)")]
        [SerializeField] private Transform markersParent;
        
        private SplineGridContainer createdGridContainer;
        private GameObject playerMarker;
        private GameObject goalMarker;
        private List<GameObject> placedHazards = new List<GameObject>();
        
        /// <summary>
        /// The created SplineGridContainer instance
        /// </summary>
        public SplineGridContainer GridContainer => createdGridContainer;
        
        /// <summary>
        /// Builds the level from the assigned LevelData
        /// </summary>
        public void BuildLevel()
        {
            if (levelData == null)
            {
                Debug.LogError("LevelBuilder: Cannot build level - LevelData is not assigned.", this);
                return;
            }
            
            if (!levelData.IsValid())
            {
                Debug.LogError("LevelBuilder: Cannot build level - LevelData is invalid (needs at least 3 spline boundary points).", this);
                return;
            }
            
            // Create the grid container
            CreateGridContainer();
            
            // Place markers
            PlaceMarkers();
            
            // Place hazards
            PlaceHazards();
            
            // Register any existing islands that might already be in the scene
            RegisterExistingIslands();
            
            Debug.Log($"LevelBuilder: Successfully built level '{levelData.levelName}'", this);
        }
        
        /// <summary>
        /// Clears the built level
        /// </summary>
        public void ClearLevel()
        {
            if (createdGridContainer != null)
            {
                Destroy(createdGridContainer.gameObject);
                createdGridContainer = null;
            }
            
            if (playerMarker != null)
            {
                Destroy(playerMarker);
                playerMarker = null;
            }
            
            if (goalMarker != null)
            {
                Destroy(goalMarker);
                goalMarker = null;
            }
            
            // Clear hazards
            foreach (var hazard in placedHazards)
            {
                if (hazard != null)
                {
                    Destroy(hazard);
                }
            }
            placedHazards.Clear();
        }
        
        private void CreateGridContainer()
        {
            // Determine parent
            Transform parent = gridParent != null ? gridParent : transform;
            
            // Create GameObject for the grid container
            GameObject containerObject = new GameObject($"Level_{levelData.levelName}_Grid");
            containerObject.transform.SetParent(parent);
            containerObject.transform.position = Vector3.zero;
            
            // Add SplineContainer component
            SplineContainer splineContainer = containerObject.AddComponent<SplineContainer>();
            
            // Set up the spline from level data
            SetupSpline(splineContainer);
            
            // Configure settings BEFORE adding SplineGridContainer (so Awake uses correct values)
            // We'll set them via a temporary component or use reflection after creation
            // Actually, we need to add the component first, then configure it
            
            // Add SplineGridContainer component
            createdGridContainer = containerObject.AddComponent<SplineGridContainer>();
            
            // Configure the grid container with level data settings using reflection
            var gridCellSizeField = typeof(SplineGridContainer).GetField("gridCellSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var gridSizeField = typeof(SplineGridContainer).GetField("gridSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var gridMaterialField = typeof(SplineGridContainer).GetField("gridMaterial", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var placementLayerMaskField = typeof(SplineGridContainer).GetField("placementLayerMask", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var ringPrefabField = typeof(SplineGridContainer).GetField("ringPrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var autoCreateRingField = typeof(SplineGridContainer).GetField("autoCreateRingOnStart", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (gridCellSizeField != null) gridCellSizeField.SetValue(createdGridContainer, levelData.gridCellSize);
            if (gridSizeField != null) gridSizeField.SetValue(createdGridContainer, levelData.gridSize);
            if (gridMaterialField != null) gridMaterialField.SetValue(createdGridContainer, levelData.gridMaterial);
            if (placementLayerMaskField != null) placementLayerMaskField.SetValue(createdGridContainer, levelData.placementLayerMask);
            if (ringPrefabField != null) ringPrefabField.SetValue(createdGridContainer, levelData.ringPrefab);
            if (autoCreateRingField != null) autoCreateRingField.SetValue(createdGridContainer, levelData.autoCreateRing);
            
            // Re-setup the spline after Awake (since UpdateSplineFromGridSize may have overwritten it)
            SetupSpline(splineContainer);
            
            // Update grid cell size
            if (createdGridContainer.Grid != null)
            {
                createdGridContainer.Grid.cellSize = new Vector3(levelData.gridCellSize, levelData.gridCellSize, levelData.gridCellSize);
            }
        }
        
        private void SetupSpline(SplineContainer splineContainer)
        {
            if (splineContainer == null || levelData.splineBoundaryPoints == null)
                return;
            
            var spline = splineContainer.Spline;
            if (spline == null)
                return;
            
            // Clear existing knots
            spline.Clear();
            
            // Add knots from level data
            // Convert Vector2 points to BezierKnots (in local space of splineContainer)
            foreach (Vector2 point in levelData.splineBoundaryPoints)
            {
                // Convert to Vector3 (XZ plane, Y = 0)
                Vector3 worldPos = new Vector3(point.x, 0f, point.y);
                // Convert to local space of splineContainer
                Vector3 localPos = splineContainer.transform.InverseTransformPoint(worldPos);
                
                BezierKnot knot = new BezierKnot(localPos);
                spline.Add(knot);
            }
            
            // Close the spline to form a loop
            spline.Closed = true;
        }
        
        private void PlaceMarkers()
        {
            if (createdGridContainer == null || createdGridContainer.Grid == null)
            {
                Debug.LogWarning("LevelBuilder: Cannot place markers - grid container is not initialized.", this);
                return;
            }
            
            // Determine parent for markers
            Transform parent = markersParent != null ? markersParent : transform;
            
            // Place player marker
            if (playerMarkerPrefab != null)
            {
                Vector3 playerWorldPos = createdGridContainer.Grid.GetCellCenterWorld(levelData.playerSpawnCell);
                playerWorldPos.y = 0f; // Place on ground
                
                playerMarker = Instantiate(playerMarkerPrefab, playerWorldPos, Quaternion.identity, parent);
                playerMarker.name = "PlayerSpawnMarker";
            }
            
            // Place goal marker
            if (goalMarkerPrefab != null)
            {
                Vector3 goalWorldPos = createdGridContainer.Grid.GetCellCenterWorld(levelData.goalCell);
                
                // Calculate the bottom of the island prefab to position it correctly
                // Instantiate temporarily to get accurate bounds
                GameObject tempInstance = Instantiate(goalMarkerPrefab, Vector3.zero, Quaternion.identity);
                Bounds instanceBounds = GetInstanceBounds(tempInstance);
                
                // Get the bottom Y of the instance (relative to its position at origin)
                float instanceBottom = instanceBounds.min.y;
                
                // Island should be at y=0.15
                // Position island so its bottom is at the target height
                // If instanceBottom is -2 (pivot is 2 units above bottom), we need y=2.15 to place bottom at 0.15
                float targetHeight = -0.25f;
                goalWorldPos.y = targetHeight - instanceBottom;
                
                // Destroy temp instance
                Destroy(tempInstance);
                
                // Now instantiate at correct position
                goalMarker = Instantiate(goalMarkerPrefab, goalWorldPos, Quaternion.identity, parent);
                goalMarker.name = "GoalMarker";
                
                // Add island to grid data so objects can't be placed on it
                AddIslandToGridData(goalMarker, createdGridContainer);
            }
        }
        
        /// <summary>
        /// Gets the bounds of an instantiated GameObject in world space
        /// </summary>
        private Bounds GetInstanceBounds(GameObject instance)
        {
            if (instance == null)
                return new Bounds(Vector3.zero, Vector3.zero);
            
            // Get all renderers for accurate bounds
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            
            if (renderers.Length == 0)
            {
                // Fallback: try colliders
                Collider[] colliders = instance.GetComponentsInChildren<Collider>();
                if (colliders.Length > 0)
                {
                    Bounds combinedBounds = colliders[0].bounds;
                    for (int i = 1; i < colliders.Length; i++)
                    {
                        combinedBounds.Encapsulate(colliders[i].bounds);
                    }
                    return combinedBounds;
                }
                
                // Final fallback: assume 1 unit tall centered at origin
                return new Bounds(Vector3.zero, new Vector3(1f, 1f, 1f));
            }
            
            // Combine all renderer bounds
            Bounds combinedRendererBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedRendererBounds.Encapsulate(renderers[i].bounds);
            }
            
            return combinedRendererBounds;
        }
        
        /// <summary>
        /// Calculates occupied grid cells from a GameObject's bounds
        /// </summary>
        private List<Vector3Int> CalculateOccupiedCellsFromBounds(GameObject obj, Grid grid)
        {
            if (obj == null || grid == null)
                return new List<Vector3Int>();
            
            Bounds bounds = GetInstanceBounds(obj);
            List<Vector3Int> occupiedCells = new List<Vector3Int>();
            
            // Get the bounds corners and find all cells within
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            
            // Convert world bounds to grid cells
            Vector3Int minCell = grid.WorldToCell(min);
            Vector3Int maxCell = grid.WorldToCell(max);
            
            // Add all cells within the bounds
            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int z = minCell.z; z <= maxCell.z; z++)
                {
                    Vector3Int cellPos = new Vector3Int(x, 0, z);
                    Vector3 cellCenter = grid.GetCellCenterWorld(cellPos);
                    
                    // Check if cell center is within bounds (XZ plane)
                    if (cellCenter.x >= min.x && cellCenter.x <= max.x &&
                        cellCenter.z >= min.z && cellCenter.z <= max.z)
                    {
                        occupiedCells.Add(cellPos);
                    }
                }
            }
            
            // If no cells found, add at least the object's position cell
            if (occupiedCells.Count == 0)
            {
                Vector3Int cellPos = grid.WorldToCell(obj.transform.position);
                occupiedCells.Add(new Vector3Int(cellPos.x, 0, cellPos.z));
            }
            
            return occupiedCells;
        }
        
        /// <summary>
        /// Adds the island to grid data so objects can't be placed on it
        /// </summary>
        private void AddIslandToGridData(GameObject island, SplineGridContainer container)
        {
            if (island == null || container == null)
                return;
            
            // Use the public method from SplineGridContainer
            container.RegisterIsland(island);
        }
        
        /// <summary>
        /// Registers any existing islands in the scene to the grid data
        /// This is useful if islands are manually placed in the scene rather than via LevelBuilder
        /// </summary>
        public void RegisterExistingIslands()
        {
            if (createdGridContainer == null)
            {
                Debug.LogWarning("LevelBuilder: Cannot register islands - grid container is not initialized.", this);
                return;
            }
            
            // Find all GameObjects named "GoalMarker" or containing "island" in their name
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            List<GameObject> islands = new List<GameObject>();
            
            foreach (GameObject obj in allObjects)
            {
                string objName = obj.name.ToLower();
                if (objName.Contains("goalmarker") || objName.Contains("island"))
                {
                    // Check if it's within the grid container's boundary
                    if (createdGridContainer.IsPositionWithinBoundary(obj.transform.position))
                    {
                        islands.Add(obj);
                    }
                }
            }
            
            // Register each found island
            foreach (GameObject island in islands)
            {
                AddIslandToGridData(island, createdGridContainer);
            }
            
            if (islands.Count > 0)
            {
                Debug.Log($"LevelBuilder: Registered {islands.Count} existing island(s) to grid data", this);
            }
        }
        
        /// <summary>
        /// Places hazards from level data
        /// </summary>
        private void PlaceHazards()
        {
            if (levelData == null || levelData.hazards == null || levelData.hazards.Count == 0)
                return;
            
            // Determine parent for hazards
            Transform parent = markersParent != null ? markersParent : transform;
            
            foreach (var hazardData in levelData.hazards)
            {
                GameObject prefab = GetHazardPrefab(hazardData.type);
                if (prefab == null)
                {
                    Debug.LogWarning($"LevelBuilder: No prefab assigned for hazard type {hazardData.type}. Skipping.", this);
                    continue;
                }
                
                Vector3 position = new Vector3(hazardData.position.x, 0f, hazardData.position.y);
                Quaternion rotation = Quaternion.Euler(0, hazardData.rotation, 0);
                Vector3 scale = Vector3.one * hazardData.scale;
                
                GameObject hazard = Instantiate(prefab, position, rotation, parent);
                hazard.transform.localScale = scale;
                hazard.name = $"Hazard_{hazardData.type}_{placedHazards.Count}";
                
                placedHazards.Add(hazard);
            }
            
            if (placedHazards.Count > 0)
            {
                Debug.Log($"LevelBuilder: Placed {placedHazards.Count} hazard(s)", this);
            }
        }
        
        /// <summary>
        /// Gets the prefab for a hazard type from level data
        /// </summary>
        private GameObject GetHazardPrefab(HazardData.HazardType type)
        {
            if (levelData == null)
                return null;
            
            switch (type)
            {
                case HazardData.HazardType.Rock:
                    return levelData.rockPrefab;
                case HazardData.HazardType.Sandtrap:
                    return levelData.sandtrapPrefab;
                case HazardData.HazardType.Whirlpool:
                    return levelData.whirlpoolPrefab;
                case HazardData.HazardType.Current:
                    return levelData.currentPrefab;
                default:
                    return null;
            }
        }
        
        // Auto-build on Start if levelData is assigned
        private void Start()
        {
            if (levelData != null)
            {
                BuildLevel();
            }
        }
        
        // Cleanup on destroy
        private void OnDestroy()
        {
            ClearLevel();
        }
    }
}

