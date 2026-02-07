using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace GridBuilder.Core
{
    /// <summary>
    /// Container for a spline-based grid. Generates a grid from a spline boundary
    /// and provides grid visualization with a material.
    /// </summary>
    [RequireComponent(typeof(SplineContainer))]
    public class SplineGridContainer : MonoBehaviour
    {
        private SplineContainer splineContainer;
        [SerializeField] private Material gridMaterial;
        [SerializeField, Min(1)] private int gridCellSize = 1;
        [SerializeField, Min(1)] private Vector2 gridSize = new(10f,10f);
        [SerializeField] private LayerMask placementLayerMask;
        [SerializeField] private CellSizePlacementMode placementMode = CellSizePlacementMode.ConvertCells;
        
        [Header("Outside Ring Settings")]
        [SerializeField] private GameObject ringPrefab;
        [SerializeField] private Transform ringParent;
        [SerializeField] private Vector3 ringPositionOffset = Vector3.zero;
        [SerializeField] private bool autoCreateRingOnStart = false;
        [SerializeField] private bool clearExistingRingOnCreate = true;
        
        private Grid grid;
        private GameObject gridVisualization;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private GridData gridData;
        private int previousCellSize;
        
        public SplineContainer SplineContainer => splineContainer;
        public Grid Grid => grid;
        public GridData GridData => gridData;
        public LayerMask PlacementLayerMask => placementLayerMask;
        public int GridCellSize => gridCellSize;
        public CellSizePlacementMode PlacementMode => placementMode;
        
        /// <summary>
        /// Initializes the SplineGridContainer with custom settings (call this before Awake or after component creation)
        /// </summary>
        public void InitializeSettings(int cellSize, Vector2 size, Material material, LayerMask layerMask, GameObject ringPrefab = null, bool autoCreateRing = false)
        {
            gridCellSize = cellSize;
            gridSize = size;
            gridMaterial = material;
            placementLayerMask = layerMask;
            this.ringPrefab = ringPrefab;
            autoCreateRingOnStart = autoCreateRing;
        }
        
        private void Awake()
        {
            // Get SplineContainer component from the same GameObject
            splineContainer = GetComponent<SplineContainer>();
            
            gridData = new GridData();
            previousCellSize = gridCellSize;
            UpdateSplineFromGridSize();
            InitializeGrid();
            GenerateGridVisualization();
        }
        
        private void Start()
        {
            // Auto-create ring if enabled
            if (autoCreateRingOnStart && ringPrefab != null)
            {
                CreateOutsideRing(ringPrefab, ringParent, ringPositionOffset, clearExistingRingOnCreate);
            }
        }
        
        private void InitializeGrid()
        {
            if (grid == null)
            {
                GameObject gridObject = new GameObject("Grid");
                gridObject.transform.SetParent(transform);
                gridObject.transform.localPosition = Vector3.zero;
                grid = gridObject.AddComponent<Grid>();
                grid.cellSize = new Vector3(gridCellSize, gridCellSize, gridCellSize);
            }
            else
            {
                // Update existing grid cell size
                UpdateGridCellSize();
            }
        }
        
        /// <summary>
        /// Updates the grid's cell size
        /// </summary>
        private void UpdateGridCellSize()
        {
            if (grid != null)
            {
                grid.cellSize = new Vector3(gridCellSize, gridCellSize, gridCellSize);
            }
        }
        
        private void GenerateGridVisualization()
        {
            if (splineContainer == null || splineContainer.Spline == null)
                return;
                
            if (gridVisualization == null)
            {
                gridVisualization = new GameObject("GridVisualization");
                gridVisualization.transform.SetParent(transform);
                gridVisualization.transform.localPosition = Vector3.zero;
                meshFilter = gridVisualization.AddComponent<MeshFilter>();
                meshRenderer = gridVisualization.AddComponent<MeshRenderer>();
                MeshCollider meshCollider = gridVisualization.AddComponent<MeshCollider>();
                gridVisualization.SetActive(false);
                
                if (gridMaterial != null)
                {
                    meshRenderer.material = gridMaterial;
                }
                
                // Set layer based on placement layer mask
                int layer = GetLayerFromLayerMask(placementLayerMask);
                if (layer >= 0)
                {
                    gridVisualization.layer = layer;
                }
            }
            
            CreatePolygonMesh();
        }
        
        private void CreatePolygonMesh()
        {
            if (splineContainer == null || splineContainer.Spline == null)
                return;
                
            var spline = splineContainer.Spline;
            if (spline.Count < 3)
                return;
                
            // Extract points from spline (knots are in local space of splineContainer)
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < spline.Count; i++)
            {
                var knot = spline[i];
                // Transform from spline container's local space to world space
                Vector3 worldPos = splineContainer.transform.TransformPoint(knot.Position);
                // Then transform to this object's local space for the mesh
                Vector3 localPos = transform.InverseTransformPoint(worldPos);
                points.Add(localPos);
            }
            
            // Create mesh from polygon
            Mesh mesh = CreatePolygonMeshFromPoints(points);
            meshFilter.mesh = mesh;
            
            // Update mesh collider if it exists
            MeshCollider meshCollider = gridVisualization.GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                meshCollider.sharedMesh = mesh;
            }
        }
        
        private Mesh CreatePolygonMeshFromPoints(List<Vector3> points)
        {
            Mesh mesh = new Mesh();
            mesh.name = "SplineGridMesh";
            
            // Project points to XZ plane (Y = 0)
            List<Vector3> projectedPoints = new List<Vector3>();
            foreach (var point in points)
            {
                projectedPoints.Add(new Vector3(point.x, 0.01f, point.z));
            }
            
            // Triangulate polygon (simple fan triangulation for convex polygons)
            // Reverse winding order so normals point upward (counter-clockwise when viewed from above)
            List<int> triangles = new List<int>();
            for (int i = 1; i < projectedPoints.Count - 1; i++)
            {
                triangles.Add(0);
                triangles.Add(i + 1);
                triangles.Add(i);
            }
            
            mesh.vertices = projectedPoints.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        /// <summary>
        /// Checks if a world position is within the spline boundary
        /// </summary>
        public bool IsPositionWithinBoundary(Vector3 worldPosition)
        {
            if (splineContainer == null || splineContainer.Spline == null)
                return false;
                
            // Project to XZ plane
            Vector2 point = new Vector2(worldPosition.x, worldPosition.z);
            
            // Get spline points (transform from spline container's local space to world space)
            List<Vector2> polygon = new List<Vector2>();
            var spline = splineContainer.Spline;
            for (int i = 0; i < spline.Count; i++)
            {
                var knot = spline[i];
                Vector3 worldPos = splineContainer.transform.TransformPoint(knot.Position);
                polygon.Add(new Vector2(worldPos.x, worldPos.z));
            }
            
            return IsPointInPolygon(point, polygon);
        }
        
        /// <summary>
        /// Ray casting algorithm to check if point is inside polygon
        /// </summary>
        private bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
        {
            int intersections = 0;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 p1 = polygon[i];
                Vector2 p2 = polygon[(i + 1) % polygon.Count];
                
                if (((p1.y > point.y) != (p2.y > point.y)) &&
                    (point.x < (p2.x - p1.x) * (point.y - p1.y) / (p2.y - p1.y) + p1.x))
                {
                    intersections++;
                }
            }
            return (intersections % 2) == 1;
        }
        
        /// <summary>
        /// Gets the boundary polygon points in world space (XZ plane)
        /// </summary>
        public List<Vector2> GetBoundaryPolygon()
        {
            List<Vector2> polygon = new List<Vector2>();
            if (splineContainer == null || splineContainer.Spline == null)
                return polygon;
                
            var spline = splineContainer.Spline;
            for (int i = 0; i < spline.Count; i++)
            {
                var knot = spline[i];
                Vector3 worldPos = splineContainer.transform.TransformPoint(knot.Position);
                polygon.Add(new Vector2(worldPos.x, worldPos.z));
            }
            return polygon;
        }
        
        /// <summary>
        /// Checks if this container's boundary intersects with another container's boundary
        /// </summary>
        public bool BoundariesIntersect(SplineGridContainer other)
        {
            if (other == null || other == this)
                return false;
                
            List<Vector2> polygon1 = GetBoundaryPolygon();
            List<Vector2> polygon2 = other.GetBoundaryPolygon();
            
            if (polygon1.Count < 3 || polygon2.Count < 3)
                return false;
            
            // Check if any edge of polygon1 intersects with any edge of polygon2
            for (int i = 0; i < polygon1.Count; i++)
            {
                Vector2 p1a = polygon1[i];
                Vector2 p1b = polygon1[(i + 1) % polygon1.Count];
                
                for (int j = 0; j < polygon2.Count; j++)
                {
                    Vector2 p2a = polygon2[j];
                    Vector2 p2b = polygon2[(j + 1) % polygon2.Count];
                    
                    if (DoLineSegmentsIntersect(p1a, p1b, p2a, p2b))
                    {
                        return true;
                    }
                }
            }
            
            // Also check if one polygon is completely inside the other (they intersect)
            // Check if any vertex of polygon1 is inside polygon2
            foreach (var vertex in polygon1)
            {
                if (IsPointInPolygon(vertex, polygon2))
                {
                    return true;
                }
            }
            
            // Check if any vertex of polygon2 is inside polygon1
            foreach (var vertex in polygon2)
            {
                if (IsPointInPolygon(vertex, polygon1))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Checks if two line segments intersect
        /// </summary>
        private bool DoLineSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            float denominator = (p4.y - p3.y) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.y - p1.y);
            
            // Lines are parallel
            if (Mathf.Abs(denominator) < 0.0001f)
                return false;
            
            float ua = ((p4.x - p3.x) * (p1.y - p3.y) - (p4.y - p3.y) * (p1.x - p3.x)) / denominator;
            float ub = ((p2.x - p1.x) * (p1.y - p3.y) - (p2.y - p1.y) * (p1.x - p3.x)) / denominator;
            
            // Check if intersection point is within both line segments
            return ua >= 0 && ua <= 1 && ub >= 0 && ub <= 1;
        }
        
        /// <summary>
        /// Checks if all grid positions for an object are within the boundary
        /// </summary>
        public bool CanPlaceObjectAt(Vector3Int gridPosition, List<Vector3Int> occupiedCells)
        {
            if (grid == null)
                return false;
                
            // Check all cells the object would occupy
            foreach (var cell in occupiedCells)
            {
                Vector3Int cellPos = gridPosition + cell;
                Vector3 worldPos = grid.GetCellCenterWorld(cellPos);
                
                if (!IsPositionWithinBoundary(worldPos))
                {
                    return false;
                }
            }
            
            // Also check grid data for collisions
            return gridData.CanPlaceObejctAt(gridPosition, occupiedCells);
        }
        
        public void ShowGrid()
        {
            if (gridVisualization != null)
                gridVisualization.SetActive(true);
        }
        
        public void HideGrid()
        {
            if (gridVisualization != null)
                gridVisualization.SetActive(false);
        }
        
        /// <summary>
        /// Registers an island GameObject to occupy grid cells so objects can't be placed on it
        /// </summary>
        /// <param name="island">The island GameObject to register</param>
        /// <returns>True if the island was successfully registered, false otherwise</returns>
        public bool RegisterIsland(GameObject island)
        {
            if (island == null || grid == null || gridData == null)
                return false;
            
            // Check if island is within this container's boundary
            if (!IsPositionWithinBoundary(island.transform.position))
            {
                Debug.LogWarning($"SplineGridContainer: Island at {island.transform.position} is not within grid boundary", this);
                return false;
            }
            
            // Calculate occupied cells from island bounds
            List<Vector3Int> occupiedCells = CalculateOccupiedCellsFromBounds(island);
            
            if (occupiedCells.Count == 0)
            {
                // Fallback: at least register the cell the island is positioned at
                Vector3Int islandCell = grid.WorldToCell(island.transform.position);
                occupiedCells.Add(new Vector3Int(islandCell.x, 0, islandCell.z));
            }
            
            // Use the first cell as the origin position
            Vector3Int originCell = occupiedCells[0];
            
            // Calculate relative cells from origin
            List<Vector3Int> relativeCells = new List<Vector3Int>();
            foreach (var cell in occupiedCells)
            {
                relativeCells.Add(cell - originCell);
            }
            
            // Use special ID for island (-2) to distinguish it from other objects
            // Use -1 as placedObjectIndex since it's not in ObjectPlacer
            const int islandObjectID = -2;
            const int islandPlacedObjectIndex = -1;
            
            try
            {
                gridData.AddObjectAt(originCell, relativeCells, islandObjectID, islandPlacedObjectIndex);
                Debug.Log($"SplineGridContainer: Registered island '{island.name}' to grid data at {occupiedCells.Count} cells (origin: {originCell})", this);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"SplineGridContainer: Could not register island to grid data: {e.Message}", this);
                return false;
            }
        }
        
        /// <summary>
        /// Calculates occupied grid cells from a GameObject's bounds
        /// </summary>
        private List<Vector3Int> CalculateOccupiedCellsFromBounds(GameObject obj)
        {
            if (obj == null || grid == null)
                return new List<Vector3Int>();
            
            // Get all renderers for accurate bounds
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            Bounds bounds;
            
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }
            else
            {
                // Fallback: try colliders
                Collider[] colliders = obj.GetComponentsInChildren<Collider>();
                if (colliders.Length > 0)
                {
                    bounds = colliders[0].bounds;
                    for (int i = 1; i < colliders.Length; i++)
                    {
                        bounds.Encapsulate(colliders[i].bounds);
                    }
                }
                else
                {
                    // Final fallback: use position with default size
                    bounds = new Bounds(obj.transform.position, Vector3.one);
                }
            }
            
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
            
            return occupiedCells;
        }
        
        /// <summary>
        /// Gets the layer index from a LayerMask (returns the first set bit)
        /// </summary>
        private int GetLayerFromLayerMask(LayerMask layerMask)
        {
            int layerValue = layerMask.value;
            if (layerValue == 0)
                return -1; // No layer set
                
            // Find the first set bit
            for (int i = 0; i < 32; i++)
            {
                if ((layerValue & (1 << i)) != 0)
                {
                    return i;
                }
            }
            return -1;
        }
        
        /// <summary>
        /// Updates the spline container to match the gridSize, creating a rectangular boundary
        /// The actual world size is gridSize * gridCellSize, so the spline scales with cell size
        /// </summary>
        private void UpdateSplineFromGridSize()
        {
            if (splineContainer == null)
                return;
                
            var spline = splineContainer.Spline;
            if (spline == null)
                return;
            
            // Clear existing knots
            spline.Clear();
            
            // Calculate actual world size: gridSize represents number of cells, multiply by cell size for world units
            // gridSize.x maps to X axis, gridSize.y maps to Z axis
            float worldWidth = gridSize.x * gridCellSize;
            float worldHeight = gridSize.y * gridCellSize;
            float halfWidth = worldWidth * 0.5f;
            float halfHeight = worldHeight * 0.5f;
            
            // Create 4 corners of the rectangle (in local space of splineContainer)
            // Order: bottom-left, bottom-right, top-right, top-left (to form a closed loop)
            BezierKnot knot1 = new BezierKnot(new Vector3(-halfWidth, 0f, -halfHeight));
            BezierKnot knot2 = new BezierKnot(new Vector3(halfWidth, 0f, -halfHeight));
            BezierKnot knot3 = new BezierKnot(new Vector3(halfWidth, 0f, halfHeight));
            BezierKnot knot4 = new BezierKnot(new Vector3(-halfWidth, 0f, halfHeight));
            
            // Add knots to spline
            spline.Add(knot1);
            spline.Add(knot2);
            spline.Add(knot3);
            spline.Add(knot4);
            
            // Close the spline to form a loop
            spline.Closed = true;
        }
        
        private void OnValidate()
        {
            // Get SplineContainer if not already set
            if (splineContainer == null)
            {
                splineContainer = GetComponent<SplineContainer>();
            }
            
            // Update spline when gridSize changes
            if (splineContainer != null)
            {
                UpdateSplineFromGridSize();
            }
            
            // Update grid cell size if it changed
            if (grid != null && gridCellSize != previousCellSize)
            {
                UpdateGridCellSize();
                
                // Update spline when cell size changes (spline size scales with cell size)
                if (splineContainer != null)
                {
                    UpdateSplineFromGridSize();
                }
                
                // If playing and objects are placed, snap them to new grid
                if (Application.isPlaying && gridData != null)
                {
                    SnapExistingObjectsToGrid(previousCellSize, gridCellSize);
                }
                
                previousCellSize = gridCellSize;
            }
            
            if (Application.isPlaying && gridVisualization != null)
            {
                GenerateGridVisualization();
                
                // Update layer if placement layer mask changed
                int layer = GetLayerFromLayerMask(placementLayerMask);
                if (layer >= 0)
                {
                    gridVisualization.layer = layer;
                }
            }
        }
        
        /// <summary>
        /// Snaps existing placed objects to the new grid cell size
        /// Note: This requires ObjectPlacer access which should be provided via BuildingSystemManager
        /// </summary>
        private void SnapExistingObjectsToGrid(int oldCellSize, int newCellSize)
        {
            // Get all unique placed objects from GridData
            HashSet<int> uniqueObjectIndices = new HashSet<int>();
            Dictionary<int, PlacementData> objectDataMap = new Dictionary<int, PlacementData>();
            
            // Collect all unique objects and their data
            // Note: This is a simplified approach - in practice, you'd need access to ObjectPlacer
            // to actually move the GameObjects. This method prepares the data for snapping.
            var allPlacementData = GetAllPlacementData();
            
            if (allPlacementData.Count == 0)
                return;
            
            // For each unique object, calculate new grid position based on world position
            // Since we don't have direct access to ObjectPlacer here, we'll update GridData
            // The actual GameObject positions would need to be updated by the system that has ObjectPlacer access
            foreach (var placementData in allPlacementData)
            {
                if (uniqueObjectIndices.Contains(placementData.PlacedObjectIndex))
                    continue;
                
                uniqueObjectIndices.Add(placementData.PlacedObjectIndex);
                objectDataMap[placementData.PlacedObjectIndex] = placementData;
            }
            
            // Note: Actual GameObject snapping would require ObjectPlacer reference
            // This is a placeholder that updates GridData structure
            // The calling system should handle GameObject position updates
        }
        
        /// <summary>
        /// Gets all unique PlacementData from GridData
        /// </summary>
        private List<PlacementData> GetAllPlacementData()
        {
            if (gridData == null)
                return new List<PlacementData>();
            
            return gridData.GetAllPlacementData();
        }
        
        /// <summary>
        /// Creates a ring of prefabs around the outside of the grid boundary (one cell outside).
        /// The ring consists of cells that are exactly one cell outside the grid's boundary.
        /// </summary>
        /// <param name="ringPrefab">The prefab to instantiate at each ring cell position</param>
        /// <param name="parent">Optional parent transform for the ring objects. If null, uses this transform.</param>
        /// <param name="positionOffset">Optional offset to apply to each ring object's position</param>
        /// <param name="clearExisting">If true, clears existing ring objects with the naming prefix before creating new ones</param>
        /// <returns>List of created GameObjects, or empty list if creation failed</returns>
        public List<GameObject> CreateOutsideRing(GameObject ringPrefab, Transform parent = null, Vector3 positionOffset = default, bool clearExisting = true)
        {
            List<GameObject> createdObjects = new List<GameObject>();
            
            if (ringPrefab == null)
            {
                Debug.LogError("SplineGridContainer: Cannot create ring - ringPrefab is null.", this);
                return createdObjects;
            }
            
            if (grid == null)
            {
                Debug.LogError("SplineGridContainer: Cannot create ring - grid is not initialized.", this);
                return createdObjects;
            }
            
            // Determine parent
            Transform targetParent = parent != null ? parent : transform;
            
            // Clear existing ring objects if requested
            if (clearExisting)
            {
                ClearExistingRingObjects(targetParent);
            }
            
            // Calculate ring cells
            List<Vector3Int> ringCells = CalculateRingCells();
            
            if (ringCells.Count == 0)
            {
                Debug.LogWarning("SplineGridContainer: No ring cells found. The grid may be too small.", this);
                return createdObjects;
            }
            
            // Instantiate prefabs at each ring cell
            foreach (Vector3Int cellPosition in ringCells)
            {
                Vector3 worldPosition = grid.GetCellCenterWorld(cellPosition);
                // Set Y to ground level (0) so objects sit on the ground, then apply offset
                worldPosition.y = 0f;
                worldPosition += positionOffset;
                
                GameObject instance;
                #if UNITY_EDITOR
                // In edit mode, use PrefabUtility to maintain prefab connections
                if (!Application.isPlaying)
                {
                    instance = UnityEditor.PrefabUtility.InstantiatePrefab(ringPrefab, targetParent) as GameObject;
                    if (instance != null)
                    {
                        instance.transform.position = worldPosition;
                        instance.transform.rotation = Quaternion.identity;
                    }
                }
                else
                {
                    instance = Instantiate(ringPrefab, worldPosition, Quaternion.identity, targetParent);
                }
                #else
                instance = Instantiate(ringPrefab, worldPosition, Quaternion.identity, targetParent);
                #endif
                
                if (instance != null)
                {
                    instance.name = $"RingObject_{cellPosition.x}_{cellPosition.y}_{cellPosition.z}";
                    createdObjects.Add(instance);
                }
            }
            
            if (createdObjects.Count > 0)
            {
                Debug.Log($"SplineGridContainer: Created {createdObjects.Count} ring objects around the grid.", this);
            }
            
            return createdObjects;
        }
        
        /// <summary>
        /// Calculates the cells that form a ring one cell outside the grid boundary.
        /// For a 2D grid (XZ plane), this creates a perimeter ring.
        /// The method samples cells around the expanded boundary and filters to only include
        /// cells that are outside the spline boundary.
        /// </summary>
        private List<Vector3Int> CalculateRingCells()
        {
            HashSet<Vector3Int> ringCellsSet = new HashSet<Vector3Int>();
            
            if (grid == null)
                return new List<Vector3Int>();
            
            // Get the boundary polygon to determine the approximate grid bounds
            List<Vector2> boundaryPolygon = GetBoundaryPolygon();
            if (boundaryPolygon.Count < 3)
            {
                // Fallback: use gridSize to calculate bounds
                return CalculateRingCellsFromGridSize();
            }
            
            // Find the bounding box of the boundary polygon
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            
            foreach (Vector2 point in boundaryPolygon)
            {
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minZ = Mathf.Min(minZ, point.y);
                maxZ = Mathf.Max(maxZ, point.y);
            }
            
            // Convert world bounds to grid cell coordinates
            // We need to find cells that are exactly one cell outside the boundary
            // First, find the actual boundary edge by sampling cells
            Vector3Int minCell = new Vector3Int(int.MaxValue, 0, int.MaxValue);
            Vector3Int maxCell = new Vector3Int(int.MinValue, 0, int.MinValue);
            
            // Sample points along the boundary to find the actual cell bounds
            for (int i = 0; i < boundaryPolygon.Count; i++)
            {
                Vector2 point = boundaryPolygon[i];
                Vector3 worldPos = new Vector3(point.x, transform.position.y, point.y);
                Vector3Int cell = grid.WorldToCell(worldPos);
                
                minCell.x = Mathf.Min(minCell.x, cell.x);
                minCell.z = Mathf.Min(minCell.z, cell.z);
                maxCell.x = Mathf.Max(maxCell.x, cell.x);
                maxCell.z = Mathf.Max(maxCell.z, cell.z);
            }
            
            // The ring should be exactly one cell outside the boundary edge
            // minCell and maxCell already represent the boundary edge cells
            // So we go one cell outside on the negative side, but maxCell is already at the edge
            int ringMinX = minCell.x - 1;
            int ringMaxX = maxCell.x;  // Don't add 1, maxCell is already at the outer edge
            int ringMinZ = minCell.z - 1;
            int ringMaxZ = maxCell.z;  // Don't add 1, maxCell is already at the outer edge
            
            // For a 2D grid (XZ plane), we create a ring on the perimeter
            // Top and bottom edges (constant Z) - these are one cell outside
            for (int x = ringMinX; x <= ringMaxX; x++)
            {
                // Bottom edge (zMin) - one cell below the boundary
                Vector3Int bottomCell = new Vector3Int(x, 0, ringMinZ);
                if (IsCellOutsideBoundary(bottomCell))
                {
                    ringCellsSet.Add(bottomCell);
                }
                
                // Top edge (zMax) - one cell above the boundary
                Vector3Int topCell = new Vector3Int(x, 0, ringMaxZ);
                if (IsCellOutsideBoundary(topCell))
                {
                    ringCellsSet.Add(topCell);
                }
            }
            
            // Left and right edges (constant X) - skip corners to avoid duplicates
            for (int z = ringMinZ + 1; z < ringMaxZ; z++)
            {
                // Left edge (xMin) - one cell to the left of the boundary
                Vector3Int leftCell = new Vector3Int(ringMinX, 0, z);
                if (IsCellOutsideBoundary(leftCell))
                {
                    ringCellsSet.Add(leftCell);
                }
                
                // Right edge (xMax) - one cell to the right of the boundary
                Vector3Int rightCell = new Vector3Int(ringMaxX, 0, z);
                if (IsCellOutsideBoundary(rightCell))
                {
                    ringCellsSet.Add(rightCell);
                }
            }
            
            return new List<Vector3Int>(ringCellsSet);
        }
        
        /// <summary>
        /// Fallback method to calculate ring cells using gridSize when boundary polygon is not available.
        /// </summary>
        private List<Vector3Int> CalculateRingCellsFromGridSize()
        {
            HashSet<Vector3Int> ringCellsSet = new HashSet<Vector3Int>();
            
            if (grid == null)
                return new List<Vector3Int>();
            
            // Calculate grid bounds in cell coordinates
            // gridSize represents the number of cells in X and Z dimensions
            int gridWidth = Mathf.RoundToInt(gridSize.x);
            int gridHeight = Mathf.RoundToInt(gridSize.y);
            
            // Calculate the bounds of the grid (centered at origin in grid space)
            int halfWidth = gridWidth / 2;
            int halfHeight = gridHeight / 2;
            
            // The ring should be exactly one cell outside the grid boundary
            // Grid goes from -halfWidth to +halfWidth (approximately), so ring is at -halfWidth-1 and +halfWidth
            int ringMinX = -halfWidth - 1;
            int ringMaxX = halfWidth;
            int ringMinZ = -halfHeight - 1;
            int ringMaxZ = halfHeight;
            
            // For a 2D grid (XZ plane), we create a ring on the perimeter
            // Top and bottom edges (constant Z)
            for (int x = ringMinX; x <= ringMaxX; x++)
            {
                // Bottom edge (zMin) - one cell below the grid
                Vector3Int bottomCell = new Vector3Int(x, 0, ringMinZ);
                if (IsCellOutsideBoundary(bottomCell))
                {
                    ringCellsSet.Add(bottomCell);
                }
                
                // Top edge (zMax) - one cell above the grid
                Vector3Int topCell = new Vector3Int(x, 0, ringMaxZ);
                if (IsCellOutsideBoundary(topCell))
                {
                    ringCellsSet.Add(topCell);
                }
            }
            
            // Left and right edges (constant X) - skip corners to avoid duplicates
            for (int z = ringMinZ + 1; z < ringMaxZ; z++)
            {
                // Left edge (xMin) - one cell to the left of the grid
                Vector3Int leftCell = new Vector3Int(ringMinX, 0, z);
                if (IsCellOutsideBoundary(leftCell))
                {
                    ringCellsSet.Add(leftCell);
                }
                
                // Right edge (xMax) - one cell to the right of the grid
                Vector3Int rightCell = new Vector3Int(ringMaxX, 0, z);
                if (IsCellOutsideBoundary(rightCell))
                {
                    ringCellsSet.Add(rightCell);
                }
            }
            
            return new List<Vector3Int>(ringCellsSet);
        }
        
        /// <summary>
        /// Checks if a cell position is outside the spline boundary.
        /// For the ring, we want cells that are outside the boundary.
        /// </summary>
        private bool IsCellOutsideBoundary(Vector3Int cellPosition)
        {
            if (grid == null)
                return false;
            
            Vector3 worldPosition = grid.GetCellCenterWorld(cellPosition);
            return !IsPositionWithinBoundary(worldPosition);
        }
        
        /// <summary>
        /// Clears existing ring objects that were created by CreateOutsideRing.
        /// </summary>
        private void ClearExistingRingObjects(Transform parent)
        {
            List<GameObject> toDestroy = new List<GameObject>();
            const string RING_OBJECT_NAME_PREFIX = "RingObject_";
            
            // Find all children with the ring object name prefix
            foreach (Transform child in parent)
            {
                if (child.name.StartsWith(RING_OBJECT_NAME_PREFIX))
                {
                    toDestroy.Add(child.gameObject);
                }
            }
            
            // Destroy found objects (use DestroyImmediate in edit mode)
            foreach (GameObject obj in toDestroy)
            {
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(obj);
                }
                else
                {
                    Destroy(obj);
                }
                #else
                Destroy(obj);
                #endif
            }
            
            if (toDestroy.Count > 0)
            {
                Debug.Log($"SplineGridContainer: Cleared {toDestroy.Count} existing ring objects.", this);
            }
        }
    }
}

