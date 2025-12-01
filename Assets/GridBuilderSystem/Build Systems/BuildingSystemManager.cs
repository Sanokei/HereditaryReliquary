using System;
using System.Collections.Generic;
using System.Linq;
using BuildingBlocks.DataTypes;
using UnityEngine;

namespace GridBuilder.Core
{
    [RequireComponent(typeof(PreviewSystem))]
    [RequireComponent(typeof(ObjectPlacer))]
    [RequireComponent(typeof(SoundFeedback))]
    public class BuildingSystemManager : MonoBehaviour
    {
        [Header("Required Components")]
        [SerializeField] private PreviewSystem previewSystem;
        [SerializeField] private ObjectPlacer objectPlacer;
        [SerializeField] private SoundFeedback soundFeedback;
        [SerializeField] private Camera sceneCamera;
        private List<SplineGridContainer> splineGridContainers = new();
        public List<ObjectsDatabaseSO> Databases = new();
        
        [Header("Out of Bounds Collision")]
        [SerializeField] private bool createOutofBounds = false;
        [SerializeField, Min(0.1f)] private float outOfBoundsHeight = 5f;
        private GameObject outOfBoundsCollider;
        
        public enum BuildingSystem
        {
            Placement,
            Removal,
            // Add more systems here as needed
        }

        [Header("Building Systems")]
        [SerializeField] private InspectableDictionary<BuildingSystem, BaseBuildingSystem> systems = new();
        private BaseBuildingSystem activeSystem;

        // Events for grid visibility management
        public event Action<List<SplineGridContainer>> OnRequestShowGrids;
        public event Action<List<SplineGridContainer>> OnRequestHideGrids;
        public event Action OnRequestHideAllGrids;

        private void Awake()
        {
            // Find all SplineGridContainer components
            splineGridContainers = FindObjectsByType<SplineGridContainer>(FindObjectsSortMode.None).ToList();
            
            // Auto-assign required components if not set
            if (previewSystem == null)
                previewSystem = GetComponent<PreviewSystem>();
            
            if (objectPlacer == null)
                objectPlacer = GetComponent<ObjectPlacer>();
            
            if (soundFeedback == null)
                soundFeedback = GetComponent<SoundFeedback>();

            // Find camera if not assigned
            if (sceneCamera == null)
                sceneCamera = Camera.main;
                
            // Subscribe to grid visibility events
            OnRequestShowGrids += HandleShowGrids;
            OnRequestHideGrids += HandleHideGrids;
            OnRequestHideAllGrids += HandleHideAllGrids;
                
            // Initialize all systems with dependencies
            InitializeSystems();
            
            // Create out of bounds collider if enabled
            if (createOutofBounds)
            {
                CreateOutOfBoundsCollider();
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            OnRequestShowGrids -= HandleShowGrids;
            OnRequestHideGrids -= HandleHideGrids;
            OnRequestHideAllGrids -= HandleHideAllGrids;
        }

        private void HandleShowGrids(List<SplineGridContainer> containers)
        {
            foreach (var container in containers)
            {
                if (container != null)
                    container.ShowGrid();
            }
        }

        private void HandleHideGrids(List<SplineGridContainer> containers)
        {
            foreach (var container in containers)
            {
                if (container != null)
                    container.HideGrid();
            }
        }

        private void HandleHideAllGrids()
        {
            HideAllGrids();
        }

        private void InitializeSystems()
        {
            foreach (var system in systems.Values)
            {
                if (system != null)
                {
                    // Initialize with dependencies first (pass manager reference)
                    system.Initialize(previewSystem, objectPlacer, soundFeedback, sceneCamera, this);
                    
                    // Disable system after initialization (they should be disabled by default)
                    // This ensures they're only enabled when explicitly requested
                    system.enabled = false;
                }
            }
        }

        /// <summary>
        /// Gets the currently active system
        /// </summary>
        public BaseBuildingSystem GetActiveSystem()
        {
            return activeSystem;
        }

        /// <summary>
        /// Get System by type
        /// </summary>
        public BaseBuildingSystem GetSystemByType(BuildingSystem systemType)
        {
            if (systems.TryGetValue(systemType, out var system))
            {
                return system;
            }
            Debug.LogError($"Building system of type {systemType} not found.");
            return null;
        }

        protected void HideAllGrids()
        {
            foreach (var container in splineGridContainers)
            {
                if (container != null)
                    container.HideGrid();
            }
        }

        /// <summary>
        /// Public method to request showing specific grids (called by building systems)
        /// </summary>
        public void RequestShowGrids(List<SplineGridContainer> containers)
        {
            OnRequestShowGrids?.Invoke(containers);
        }

        /// <summary>
        /// Public method to request hiding specific grids (called by building systems)
        /// </summary>
        public void RequestHideGrids(List<SplineGridContainer> containers)
        {
            OnRequestHideGrids?.Invoke(containers);
        }

        /// <summary>
        /// Public method to request hiding all grids (called by building systems)
        /// </summary>
        public void RequestHideAllGrids()
        {
            OnRequestHideAllGrids?.Invoke();
        }

        /// <summary>
        /// Gets a system of the specified type
        /// </summary>
        public T GetSystem<T>() where T : BaseBuildingSystem
        {
            foreach (var system in systems)
            {
                if (system is T typedSystem)
                {
                    return typedSystem;
                }
            }
            return null;
        }

        // Public getters for dependencies
        public PreviewSystem PreviewSystem => previewSystem;
        public ObjectPlacer ObjectPlacer => objectPlacer;
        public SoundFeedback SoundFeedback => soundFeedback;
        public Camera SceneCamera => sceneCamera;
        public List<SplineGridContainer> SplineGridContainers => splineGridContainers;

        private void OnValidate()
        {
            // Auto-assign components in editor
            if (previewSystem == null)
                previewSystem = GetComponent<PreviewSystem>();
            
            if (objectPlacer == null)
                objectPlacer = GetComponent<ObjectPlacer>();
            
            if (soundFeedback == null)
                soundFeedback = GetComponent<SoundFeedback>();

            if (sceneCamera == null)
                sceneCamera = Camera.main;
            
            // Update out of bounds collider if enabled
            if (createOutofBounds && Application.isPlaying)
            {
                // Recreate collider to ensure it's up to date
                CreateOutOfBoundsCollider();
            }
            else if (!createOutofBounds && outOfBoundsCollider != null)
            {
                DestroyOutOfBoundsCollider();
            }
        }
        
        /// <summary>
        /// Creates a polygonal 3D collision box that surrounds all grids
        /// </summary>
        private void CreateOutOfBoundsCollider()
        {
            // Clean up existing collider and mesh if they exist
            if (outOfBoundsCollider != null)
            {
                MeshCollider oldCollider = outOfBoundsCollider.GetComponent<MeshCollider>();
                if (oldCollider != null && oldCollider.sharedMesh != null)
                {
                    Mesh oldMesh = oldCollider.sharedMesh;
                    if (Application.isPlaying)
                    {
                        Destroy(oldMesh);
                    }
                    else
                    {
#if UNITY_EDITOR
                        DestroyImmediate(oldMesh);
#else
                        Destroy(oldMesh);
#endif
                    }
                }
                DestroyOutOfBoundsCollider();
            }
            
            if (splineGridContainers == null || splineGridContainers.Count == 0)
            {
                Debug.LogWarning("No SplineGridContainers found. Cannot create out of bounds collider.");
                return;
            }
            
            // Create the collider GameObject
            outOfBoundsCollider = new GameObject("OutOfBoundsCollider");
            outOfBoundsCollider.transform.SetParent(transform);
            outOfBoundsCollider.transform.localPosition = Vector3.zero;
            
            // Create mesh for collision
            Mesh collisionMesh = CreatePolygonalCollisionMesh();
            
            if (collisionMesh != null)
            {
                // Add MeshCollider component
                MeshCollider meshCollider = outOfBoundsCollider.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = collisionMesh;
                meshCollider.convex = false; // Use non-convex for complex shapes
            }
            else
            {
                Debug.LogWarning("Failed to create collision mesh for out of bounds collider.");
                Destroy(outOfBoundsCollider);
                outOfBoundsCollider = null;
            }
        }
        
        /// <summary>
        /// Creates a mesh that forms walls around the grid boundaries
        /// </summary>
        private Mesh CreatePolygonalCollisionMesh()
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            
            // Process each grid container
            foreach (var container in splineGridContainers)
            {
                if (container == null)
                    continue;
                
                // Get boundary polygon in world space (XZ plane)
                List<Vector2> polygon = container.GetBoundaryPolygon();
                
                if (polygon == null || polygon.Count < 3)
                    continue;
                
                // Get the container's transform to determine Y position
                Transform containerTransform = container.transform;
                float baseY = containerTransform.position.y;
                
                // Create walls for each edge of the polygon
                for (int i = 0; i < polygon.Count; i++)
                {
                    Vector2 p1 = polygon[i];
                    Vector2 p2 = polygon[(i + 1) % polygon.Count];
                    
                    // Create a quad (wall) for this edge
                    // Bottom vertices
                    Vector3 v1 = new Vector3(p1.x, baseY, p1.y);
                    Vector3 v2 = new Vector3(p2.x, baseY, p2.y);
                    
                    // Top vertices
                    Vector3 v3 = new Vector3(p2.x, baseY + outOfBoundsHeight, p2.y);
                    Vector3 v4 = new Vector3(p1.x, baseY + outOfBoundsHeight, p1.y);
                    
                    // Add vertices
                    int baseIndex = vertices.Count;
                    vertices.Add(v1);
                    vertices.Add(v2);
                    vertices.Add(v3);
                    vertices.Add(v4);
                    
                    // Add triangles (two triangles per quad)
                    // First triangle: v1, v2, v3
                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 1);
                    triangles.Add(baseIndex + 2);
                    
                    // Second triangle: v1, v3, v4
                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 3);
                }
            }
            
            if (vertices.Count == 0)
                return null;
            
            // Create mesh
            Mesh mesh = new Mesh();
            mesh.name = "OutOfBoundsCollisionMesh";
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        /// <summary>
        /// Destroys the out of bounds collider GameObject
        /// </summary>
        private void DestroyOutOfBoundsCollider()
        {
            if (outOfBoundsCollider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(outOfBoundsCollider);
                }
                else
                {
#if UNITY_EDITOR
                    DestroyImmediate(outOfBoundsCollider);
#else
                    Destroy(outOfBoundsCollider);
#endif
                }
                outOfBoundsCollider = null;
            }
        }
    }
}

