using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GridBuilder.Core
{
    public class OccupiedCellsEditor : EditorWindow
    {
        private ObjectData objectData;
        private ObjectsDatabaseSO parentDatabase;
        private GameObject previewInstance;
        private HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
        private float cellSize = 1f;
        private Bounds prefabBounds;
        private Vector3Int gridDimensions;
        private Vector3 gridOrigin;
        
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
            cellSize = 1f; // Default cell size, matches grid cell size
            
            // Load existing occupied cells
            occupiedCells.Clear();
            if (data.OccupiedCells != null)
            {
                foreach (var cell in data.OccupiedCells)
                {
                    occupiedCells.Add(cell);
                }
            }
            
            // Create preview instance
            if (data.Prefab != null)
            {
                CreatePreviewInstance();
                CalculatePrefabBounds();
                CalculateGridDimensions();
            }
            
            // Initialize camera
            cameraTarget = prefabBounds.center;
            UpdateCameraPosition();
            
            // Create materials
            CreateMaterials();
        }
        
        private void CreatePreviewInstance()
        {
            if (previewInstance != null)
                DestroyImmediate(previewInstance);
                
            previewInstance = Instantiate(objectData.Prefab);
            previewInstance.hideFlags = HideFlags.HideAndDontSave;
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
            
            // Set grid origin at minimum corner (0,0,0 in grid space)
            gridOrigin = prefabBounds.min;
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
                SaveChanges();
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
                "Left Click: Toggle cell occupancy\n" +
                "Right Click + Drag: Rotate camera\n" +
                "Scroll Wheel: Zoom in/out",
                MessageType.Info);
            
            // Preview area
            Rect previewRect = GUILayoutUtility.GetRect(position.width, position.height - 70);
            
            if (Event.current.type == EventType.Repaint)
            {
                RenderPreview(previewRect);
            }
            
            HandleInput(previewRect);
        }
        
        private void RenderPreview(Rect rect)
        {
            // This is a simplified preview rendering
            // In a full implementation, you'd use Camera.Render or PreviewRenderUtility
            
            GUI.Box(rect, "");
            
            // Draw a simple representation
            GUI.Label(new Rect(rect.x + 10, rect.y + 10, rect.width - 20, 20), 
                $"Preview: {objectData.Name}");
            GUI.Label(new Rect(rect.x + 10, rect.y + 30, rect.width - 20, 20), 
                $"Grid Size: {gridDimensions.x}x{gridDimensions.z} cells");
            GUI.Label(new Rect(rect.x + 10, rect.y + 50, rect.width - 20, 20), 
                $"Cell Size: {cellSize}");
            
            // Draw grid representation in 2D (top-down view)
            DrawGridRepresentation(rect);
        }
        
        private void DrawGridRepresentation(Rect rect)
        {
            float cellDisplaySize = Mathf.Min(
                (rect.width - 100) / gridDimensions.x,
                (rect.height - 150) / gridDimensions.z
            );
            cellDisplaySize = Mathf.Max(cellDisplaySize, 10f);
            
            Vector2 gridStartPos = new Vector2(
                rect.x + (rect.width - gridDimensions.x * cellDisplaySize) / 2f,
                rect.y + 100
            );
            
            // Draw grid cells (XZ plane, top-down view)
            for (int x = 0; x < gridDimensions.x; x++)
            {
                for (int z = 0; z < gridDimensions.z; z++)
                {
                    Vector3Int cellPos = new Vector3Int(x, 0, z);
                    Rect cellRect = new Rect(
                        gridStartPos.x + x * cellDisplaySize,
                        gridStartPos.y + z * cellDisplaySize,
                        cellDisplaySize - 1,
                        cellDisplaySize - 1
                    );
                    
                    Color cellColor = occupiedCells.Contains(cellPos) 
                        ? new Color(0f, 1f, 0f, 0.7f) 
                        : new Color(0.3f, 0.3f, 0.3f, 0.3f);
                    
                    EditorGUI.DrawRect(cellRect, cellColor);
                    
                    // Draw border
                    Handles.BeginGUI();
                    Handles.color = Color.black;
                    Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, Color.black);
                    Handles.EndGUI();
                }
            }
        }
        
        private void HandleInput(Rect rect)
        {
            Event e = Event.current;
            
            if (!rect.Contains(e.mousePosition))
                return;
            
            // Left click to toggle cells
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Vector3Int? cell = GetCellAtMousePosition(e.mousePosition, rect);
                if (cell.HasValue)
                {
                    ToggleCell(cell.Value);
                    e.Use();
                    Repaint();
                }
            }
            
            // Right click drag for camera rotation
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                isDraggingCamera = true;
                lastMousePosition = e.mousePosition;
                e.Use();
            }
            
            if (e.type == EventType.MouseDrag && e.button == 1 && isDraggingCamera)
            {
                Vector2 delta = e.mousePosition - lastMousePosition;
                cameraAngleY += delta.x * 0.5f;
                cameraAngleX -= delta.y * 0.5f;
                cameraAngleX = Mathf.Clamp(cameraAngleX, 5f, 85f);
                UpdateCameraPosition();
                lastMousePosition = e.mousePosition;
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
        }
        
        private Vector3Int? GetCellAtMousePosition(Vector2 mousePos, Rect rect)
        {
            float cellDisplaySize = Mathf.Min(
                (rect.width - 100) / gridDimensions.x,
                (rect.height - 150) / gridDimensions.z
            );
            cellDisplaySize = Mathf.Max(cellDisplaySize, 10f);
            
            Vector2 gridStartPos = new Vector2(
                rect.x + (rect.width - gridDimensions.x * cellDisplaySize) / 2f,
                rect.y + 100
            );
            
            Vector2 localPos = mousePos - gridStartPos;
            int x = Mathf.FloorToInt(localPos.x / cellDisplaySize);
            int z = Mathf.FloorToInt(localPos.y / cellDisplaySize);
            
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
        
        private void SaveChanges()
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
        
        private void OnDestroy()
        {
            if (previewInstance != null)
            {
                DestroyImmediate(previewInstance);
            }
            
            if (gridMaterial != null)
            {
                DestroyImmediate(gridMaterial);
            }
            if (occupiedCellMaterial != null)
            {
                DestroyImmediate(occupiedCellMaterial);
            }
            if (emptyCellMaterial != null)
            {
                DestroyImmediate(emptyCellMaterial);
            }
        }
    }
}

