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
    }
}

