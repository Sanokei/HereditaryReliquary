using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.SceneManagement;

namespace GridBuilder.Core
{
    public class OccupiedCellsEditor : EditorWindow
    {
        private ObjectData objectData;
        private ObjectsDatabaseSO parentDatabase;
        private GameObject previewInstance;
        private GameObject gridFloor;
        private HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
        private float cellSize = 1f;
        private Bounds prefabBounds;
        private Vector3Int gridDimensions;
        private Vector3 gridOrigin;
        
        // 3D Preview
        private PreviewRenderUtility previewRenderUtility;
        private bool previewRenderUtilityInitialized = false;
        
        // Camera controls
        private Vector3 cameraPosition;
        private Vector3 cameraTarget;
        private float cameraDistance = 10f;
        private float cameraAngleX = 45f;
        private float cameraAngleY = 45f;
        
        // Grid visualization
        private Material gridMaterial;
        private Material occupiedCellMaterial;
        private Material emptyCellMaterial;
        private Material previewMaterial;
        
        // Mouse interaction
        private Vector2 lastMousePosition;
        private bool isDraggingCamera = false;
        
        public static void OpenWindow(ObjectData data, ObjectsDatabaseSO database)
        {
            OccupiedCellsEditor window = GetWindow<OccupiedCellsEditor>("Occupied Cells Editor");
            window.Initialize(data, database);
            window.Show();
        }
        
        private void Initialize(ObjectData data, ObjectsDatabaseSO database)
        {
            objectData = data;
            parentDatabase = database;
            cellSize = database != null ? database.cellSize : 1f; // Use database cell size
            
            // Load existing occupied cells
            occupiedCells.Clear();
            if (data.OccupiedCells != null)
            {
                foreach (var cell in data.OccupiedCells)
                {
                    occupiedCells.Add(cell);
                }
            }
            
            // Create materials first (needed for preview instance)
            CreateMaterials();
            
            // Create preview instance
            if (data.Prefab != null)
            {
                CreatePreviewInstance(); // This calculates bounds and positions the prefab
                CalculateGridDimensions();
                CreateGridFloor(); // Create grid floor after calculating dimensions
            }
            
            // Initialize camera
            cameraTarget = prefabBounds.center;
            UpdateCameraPosition();
        }
        
        private void CreatePreviewInstance()
        {
            if (previewInstance != null)
                DestroyImmediate(previewInstance);
                
            previewInstance = Instantiate(objectData.Prefab);
            // Hide from scene view and hierarchy
            previewInstance.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInHierarchy;
            
            // Make all children also hidden
            foreach (Transform child in previewInstance.GetComponentsInChildren<Transform>())
            {
                if (child != previewInstance.transform)
                {
                    child.gameObject.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInHierarchy;
                }
            }
            
            // Apply see-through material to all renderers
            ApplyPreviewMaterial();
            
            // Calculate bounds first to determine offset
            CalculatePrefabBounds();
            
            // Position prefab so its minimum corner aligns with grid origin (0,0,0)
            // This ensures the grid and prefab are aligned
            Vector3 offset = -prefabBounds.min;
            previewInstance.transform.position = offset;
            
            // Recalculate bounds after positioning
            CalculatePrefabBounds();
        }
        
        private void InitializePreviewRenderUtility()
        {
            if (previewRenderUtility == null)
            {
                previewRenderUtility = new PreviewRenderUtility();
            }
            
            if (previewInstance != null && !previewRenderUtilityInitialized)
            {
                // Add preview instance to the preview scene
                // Using deprecated method as newer API is not available
                #pragma warning disable CS0618
                previewRenderUtility.AddSingleGO(previewInstance, false);
                #pragma warning restore CS0618
                
                // Create and add grid floor
                CreateGridFloor();
                if (gridFloor != null)
                {
                    #pragma warning disable CS0618
                    previewRenderUtility.AddSingleGO(gridFloor, false);
                    #pragma warning restore CS0618
                }
                
                previewRenderUtilityInitialized = true;
            }
        }
        
        private void CreateGridFloor()
        {
            if (gridFloor != null)
                DestroyImmediate(gridFloor);
            
            if (gridDimensions.x == 0 || gridDimensions.z == 0)
                return;
            
            gridFloor = new GameObject("GridFloor");
            gridFloor.hideFlags = HideFlags.HideAndDontSave;
            
            MeshFilter meshFilter = gridFloor.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gridFloor.AddComponent<MeshRenderer>();
            MeshCollider meshCollider = gridFloor.AddComponent<MeshCollider>();
            
            // Create grid floor mesh
            Mesh gridMesh = CreateGridFloorMesh();
            meshFilter.mesh = gridMesh;
            meshCollider.sharedMesh = gridMesh;
            
            // Create material for grid floor - full bright, opaque
            Material floorMaterial = new Material(Shader.Find("Unlit (Vertex Color)"));
            floorMaterial.SetFloat("_Mode", 0); // Opaque mode
            floorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            floorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            floorMaterial.SetInt("_ZWrite", 1);
            floorMaterial.DisableKeyword("_ALPHATEST_ON");
            floorMaterial.DisableKeyword("_ALPHABLEND_ON");
            floorMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            floorMaterial.renderQueue = -1;
            floorMaterial.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            meshRenderer.material = floorMaterial;
            
            // Position grid floor at the bottom of the prefab bounds
            // Grid floor's local (0,0,0) aligns with prefab's minimum corner (which is now at world origin)
            gridFloor.transform.position = new Vector3(
                0,
                prefabBounds.min.y - 0.01f,
                0
            );
        }
        
        private Mesh CreateGridFloorMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "GridFloorMesh";
            
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Color> colors = new List<Color>();
            
            // Create a quad for each grid cell
            // Grid cells should align with the prefab's minimum corner
            // Cell (0,0,0) should start at (0,0,0) in local space and extend to (cellSize, 0, cellSize)
            for (int x = 0; x < gridDimensions.x; x++)
            {
                for (int z = 0; z < gridDimensions.z; z++)
                {
                    Vector3Int cellPos = new Vector3Int(x, 0, z);
                    
                    // Position cell corners at grid coordinates
                    // Cell (0,0,0) extends from (0,0,0) to (cellSize, 0, cellSize)
                    Vector3 cellMinCorner = new Vector3(
                        x * cellSize,
                        0,
                        z * cellSize
                    );
                    
                    // Determine cell color based on occupancy
                    // Light grey for empty, blue for occupied - full bright
                    Color cellColor = occupiedCells.Contains(cellPos) 
                        ? new Color(0f, 0.5f, 1f, 1f) // Bright blue
                        : new Color(0.8f, 0.8f, 0.8f, 1f); // Light grey
                    
                    int vertexOffset = vertices.Count;
                    
                    // Create quad vertices (cell corners)
                    vertices.Add(cellMinCorner + new Vector3(0, 0, 0));
                    vertices.Add(cellMinCorner + new Vector3(cellSize, 0, 0));
                    vertices.Add(cellMinCorner + new Vector3(cellSize, 0, cellSize));
                    vertices.Add(cellMinCorner + new Vector3(0, 0, cellSize));
                    
                    // Add colors for each vertex
                    colors.Add(cellColor);
                    colors.Add(cellColor);
                    colors.Add(cellColor);
                    colors.Add(cellColor);
                    
                    // Create triangles
                    triangles.Add(vertexOffset + 0);
                    triangles.Add(vertexOffset + 2);
                    triangles.Add(vertexOffset + 1);
                    
                    triangles.Add(vertexOffset + 0);
                    triangles.Add(vertexOffset + 3);
                    triangles.Add(vertexOffset + 2);
                }
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.colors = colors.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        private void UpdateGridFloorMesh()
        {
            if (gridFloor == null)
                return;
                
            MeshFilter meshFilter = gridFloor.GetComponent<MeshFilter>();
            MeshCollider meshCollider = gridFloor.GetComponent<MeshCollider>();
            
            if (meshFilter != null)
            {
                Mesh gridMesh = CreateGridFloorMesh();
                if (meshFilter.sharedMesh != null && meshFilter.sharedMesh.name == "GridFloorMesh")
                {
                    DestroyImmediate(meshFilter.sharedMesh);
                }
                meshFilter.mesh = gridMesh;
                
                // Update collider mesh as well
                if (meshCollider != null)
                {
                    meshCollider.sharedMesh = gridMesh;
                }
            }
        }
        
        private void ApplyPreviewMaterial()
        {
            if (previewInstance == null || previewMaterial == null)
                return;
                
            Renderer[] renderers = previewInstance.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = previewMaterial;
                }
                renderer.sharedMaterials = materials;
            }
        }
        
        private void CalculatePrefabBounds()
        {
            // Get all mesh renderers in the hierarchy
            MeshRenderer[] renderers = previewInstance.GetComponentsInChildren<MeshRenderer>();
            
            if (renderers.Length == 0)
            {
                // Fallback to a default bounds
                prefabBounds = new Bounds(Vector3.zero, Vector3.one);
                return;
            }
            
            // Calculate combined bounds
            prefabBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                prefabBounds.Encapsulate(renderers[i].bounds);
            }
        }
        
        private void CalculateGridDimensions()
        {
            // Calculate grid dimensions based on bounds
            Vector3 size = prefabBounds.size;
            gridDimensions = new Vector3Int(
                Mathf.CeilToInt(size.x / cellSize),
                Mathf.CeilToInt(size.y / cellSize),
                Mathf.CeilToInt(size.z / cellSize)
            );
            
            // Grid origin is at (0,0,0) since prefab is positioned so its min corner is at origin
            gridOrigin = Vector3.zero;
        }
        
        private void CreateMaterials()
        {
            // Grid line material
            gridMaterial = new Material(Shader.Find("Sprites/Default"));
            gridMaterial.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            
            // Occupied cell material (green)
            occupiedCellMaterial = new Material(Shader.Find("Sprites/Default"));
            occupiedCellMaterial.color = new Color(0f, 1f, 0f, 0.5f);
            
            // Empty cell material (transparent)
            emptyCellMaterial = new Material(Shader.Find("Sprites/Default"));
            emptyCellMaterial.color = new Color(1f, 1f, 1f, 0.2f);
            
            // Preview material (see-through)
            previewMaterial = new Material(Shader.Find("Standard"));
            previewMaterial.SetFloat("_Mode", 3); // Transparent mode
            previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            previewMaterial.SetInt("_ZWrite", 0);
            previewMaterial.DisableKeyword("_ALPHATEST_ON");
            previewMaterial.EnableKeyword("_ALPHABLEND_ON");
            previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            previewMaterial.renderQueue = 3000;
            previewMaterial.color = new Color(1f, 1f, 1f, 0.3f);
        }
        
        private void OnGUI()
        {
            if (objectData == null || objectData.Prefab == null)
            {
                EditorGUILayout.HelpBox("No object data or prefab selected.", MessageType.Warning);
                return;
            }
            
            // Top toolbar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                SaveOccupiedCells();
            }
            
            if (GUILayout.Button("Cancel", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                Close();
            }
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.LabelField($"Occupied Cells: {occupiedCells.Count}", EditorStyles.toolbarButton);
            
            EditorGUILayout.EndHorizontal();
            
            // Instructions
            EditorGUILayout.HelpBox(
                "Left Click (Grid): Toggle cell occupancy\n" +
                "Right Click + Drag (3D View): Rotate camera\n" +
                "Scroll Wheel (3D View): Zoom in/out",
                MessageType.Info);
            
            // Preview area - full 3D preview with grid floor
            Rect preview3DRect = GUILayoutUtility.GetRect(position.width, position.height - 120);
            
            if (Event.current.type == EventType.Repaint)
            {
                Render3DPreview(preview3DRect);
            }
            
            HandleInput(preview3DRect);
        }
        
        
        private void Render3DPreview(Rect rect)
        {
            if (previewInstance == null)
                return;
                
            InitializePreviewRenderUtility();
            
            if (previewRenderUtility == null || previewRenderUtility.camera == null)
                return;
            
            // Update camera position
            UpdateCameraPosition();
            previewRenderUtility.camera.transform.position = cameraPosition;
            previewRenderUtility.camera.transform.LookAt(cameraTarget);
            
            // Set up camera
            previewRenderUtility.camera.nearClipPlane = 0.1f;
            previewRenderUtility.camera.farClipPlane = 100f;
            previewRenderUtility.camera.fieldOfView = 30f;
            
            // Render the preview
            previewRenderUtility.BeginPreview(rect, GUIStyle.none);
            previewRenderUtility.Render();
            previewRenderUtility.EndAndDrawPreview(rect);
        }
        
        
        private void HandleInput(Rect preview3DRect)
        {
            Event e = Event.current;
            Vector2 mousePos = e.mousePosition;
            
            if (!preview3DRect.Contains(mousePos))
                return;
            
            // Handle 3D preview camera controls
            // Right click drag for camera rotation
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                isDraggingCamera = true;
                lastMousePosition = mousePos;
                e.Use();
            }
            
            if (e.type == EventType.MouseDrag && e.button == 1 && isDraggingCamera)
            {
                Vector2 delta = mousePos - lastMousePosition;
                cameraAngleY += delta.x * 0.5f;
                cameraAngleX += delta.y * 0.5f;
                cameraAngleX = Mathf.Clamp(cameraAngleX, 5f, 85f);
                UpdateCameraPosition();
                lastMousePosition = mousePos;
                e.Use();
                Repaint();
            }
            
            if (e.type == EventType.MouseUp && e.button == 1)
            {
                isDraggingCamera = false;
                e.Use();
            }
            
            // Scroll wheel for zoom
            if (e.type == EventType.ScrollWheel)
            {
                cameraDistance += e.delta.y * 0.5f;
                cameraDistance = Mathf.Clamp(cameraDistance, 2f, 50f);
                UpdateCameraPosition();
                e.Use();
                Repaint();
            }
            
            // Handle grid cell selection via raycast
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Vector3Int? cell = GetCellAtMousePosition3D(mousePos, preview3DRect);
                if (cell.HasValue)
                {
                    ToggleCell(cell.Value);
                    UpdateGridFloorMesh(); // Update the 3D grid visualization
                    e.Use();
                    Repaint();
                }
            }
        }
        
        private Vector3Int? GetCellAtMousePosition3D(Vector2 mousePos, Rect rect)
        {
            if (previewRenderUtility == null || previewRenderUtility.camera == null || gridFloor == null)
                return null;
            
            Camera cam = previewRenderUtility.camera;
            
            // Convert mouse position to normalized viewport coordinates (0-1)
            Vector2 normalizedMousePos = new Vector2(
                (mousePos.x - rect.x) / rect.width,
                1f - ((mousePos.y - rect.y) / rect.height) // Invert Y
            );
            
            // Create a ray from the camera through the mouse position
            Ray ray = cam.ViewportPointToRay(normalizedMousePos);
            
            // Intersect ray with the grid floor plane (Y = gridFloor.transform.position.y)
            float planeY = gridFloor.transform.position.y;
            Vector3 planeNormal = Vector3.up;
            Vector3 planePoint = new Vector3(0, planeY, 0);
            
            // Calculate intersection point
            float denominator = Vector3.Dot(ray.direction, planeNormal);
            if (Mathf.Abs(denominator) < 0.0001f)
                return null; // Ray is parallel to plane
            
            float t = Vector3.Dot(planePoint - ray.origin, planeNormal) / denominator;
            if (t < 0)
                return null; // Intersection is behind the camera
            
            Vector3 worldHitPoint = ray.origin + ray.direction * t;
            
            // Convert hit point to local space (relative to grid floor)
            Vector3 localHitPoint = gridFloor.transform.InverseTransformPoint(worldHitPoint);
            
            // Grid cells start at (0,0,0) and extend by cellSize
            // Cell (0,0,0) extends from (0,0,0) to (cellSize, 0, cellSize)
            int x = Mathf.FloorToInt(localHitPoint.x / cellSize);
            int z = Mathf.FloorToInt(localHitPoint.z / cellSize);
            
            if (x >= 0 && x < gridDimensions.x && z >= 0 && z < gridDimensions.z)
            {
                return new Vector3Int(x, 0, z);
            }
            
            return null;
        }
        
        
        private void ToggleCell(Vector3Int cell)
        {
            if (occupiedCells.Contains(cell))
            {
                occupiedCells.Remove(cell);
            }
            else
            {
                occupiedCells.Add(cell);
            }
        }
        
        private void UpdateCameraPosition()
        {
            float radX = cameraAngleX * Mathf.Deg2Rad;
            float radY = cameraAngleY * Mathf.Deg2Rad;
            
            cameraPosition = cameraTarget + new Vector3(
                Mathf.Sin(radY) * Mathf.Cos(radX),
                Mathf.Sin(radX),
                Mathf.Cos(radY) * Mathf.Cos(radX)
            ) * cameraDistance;
        }
        
        private void SaveOccupiedCells()
        {
            if (objectData == null)
                return;
            
            // Use reflection to set the private property
            var occupiedCellsProperty = typeof(ObjectData).GetProperty("OccupiedCells");
            if (occupiedCellsProperty != null)
            {
                List<Vector3Int> cellList = new List<Vector3Int>(occupiedCells);
                if (cellList.Count == 0)
                {
                    cellList.Add(Vector3Int.zero); // Ensure at least one cell
                }
                occupiedCellsProperty.SetValue(objectData, cellList);
            }
            
            // Mark the parent ScriptableObject as dirty
            if (parentDatabase != null)
            {
                EditorUtility.SetDirty(parentDatabase);
                AssetDatabase.SaveAssets();
            }
            
            Close();
        }
        
        private void OnDisable()
        {
            Cleanup();
        }
        
        private void OnDestroy()
        {
            Cleanup();
        }
        
        private void Cleanup()
        {
            if (previewInstance != null)
            {
                DestroyImmediate(previewInstance);
                previewInstance = null;
            }
            
            if (gridFloor != null)
            {
                DestroyImmediate(gridFloor);
                gridFloor = null;
            }
            
            if (previewRenderUtility != null)
            {
                previewRenderUtility.Cleanup();
                previewRenderUtility = null;
                previewRenderUtilityInitialized = false;
            }
            
            if (gridMaterial != null)
            {
                DestroyImmediate(gridMaterial);
                gridMaterial = null;
            }
            if (occupiedCellMaterial != null)
            {
                DestroyImmediate(occupiedCellMaterial);
                occupiedCellMaterial = null;
            }
            if (emptyCellMaterial != null)
            {
                DestroyImmediate(emptyCellMaterial);
                emptyCellMaterial = null;
            }
            if (previewMaterial != null)
            {
                DestroyImmediate(previewMaterial);
                previewMaterial = null;
            }
        }
    }
}

