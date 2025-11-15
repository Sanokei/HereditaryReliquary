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
        [SerializeField] private float gridCellSize = 1f;
        [SerializeField] private LayerMask placementLayerMask;
        [SerializeField] private ObjectsDatabaseSO objectsDatabase;
        
        private Grid grid;
        private GameObject gridVisualization;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private GridData gridData;
        
        public SplineContainer SplineContainer => splineContainer;
        public Grid Grid => grid;
        public GridData GridData => gridData;
        public LayerMask PlacementLayerMask => placementLayerMask;
        public ObjectsDatabaseSO ObjectsDatabase => objectsDatabase;
        public float GridCellSize => gridCellSize;
        
        private void Awake()
        {
            // Get SplineContainer component from the same GameObject
            splineContainer = GetComponent<SplineContainer>();
            
            gridData = new GridData();
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
            List<int> triangles = new List<int>();
            for (int i = 1; i < projectedPoints.Count - 1; i++)
            {
                triangles.Add(0);
                triangles.Add(i);
                triangles.Add(i + 1);
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
        /// Checks if all grid positions for an object are within the boundary
        /// </summary>
        public bool CanPlaceObjectAt(Vector3Int gridPosition, Vector3Int objectSize)
        {
            if (grid == null)
                return false;
                
            // Check all cells the object would occupy
            for (int x = 0; x < objectSize.x; x++)
            {
                for (int y = 0; y < objectSize.y; y++)
                {
                    for (int z = 0; z < objectSize.z; z++)
                    {
                        Vector3Int cellPos = gridPosition + new Vector3Int(x, y, z);
                        Vector3 worldPos = grid.GetCellCenterWorld(cellPos);
                        
                        if (!IsPositionWithinBoundary(worldPos))
                        {
                            return false;
                        }
                    }
                }
            }
            
            // Also check grid data for collisions
            return gridData.CanPlaceObejctAt(gridPosition, objectSize);
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
        
        private void OnValidate()
        {
            // Get SplineContainer if not already set
            if (splineContainer == null)
            {
                splineContainer = GetComponent<SplineContainer>();
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
    }
}

