using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace GridBuilder.Core
{
    /// <summary>
    /// Editor window for creating and editing levels
    /// Allows setting up spline boundaries, player spawn, and goal positions
    /// </summary>
    public class LevelEditor : EditorWindow
    {
        private LevelData currentLevelData;
        private SerializedObject serializedLevelData;
        
        // Scene references
        private SplineGridContainer sceneGridContainer;
        private SplineContainer sceneSplineContainer;
        private GameObject playerMarker;
        private GameObject goalMarker;
        private List<GameObject> hazardMarkers = new List<GameObject>();
        
        // Editing state
        private bool isEditingBoundary = false;
        private int selectedBoundaryPoint = -1;
        private Vector2? playerSpawnWorldPos;
        private Vector2? goalWorldPos;
        private bool isPlacingHazard = false;
        private HazardData.HazardType selectedHazardType = HazardData.HazardType.Rock;
        private int selectedHazardIndex = -1;
        
        // Settings
        private int gridCellSize = 1;
        private Vector2 gridSize = new Vector2(10f, 10f);
        private Material gridMaterial;
        private LayerMask placementLayerMask;
        
        // Scene view handles
        private const float HANDLE_SIZE = 0.5f;
        private const float SNAP_DISTANCE = 1f;
        
        [MenuItem("Window/Level Tools/Level Editor", false, 1)]
        public static void ShowWindow()
        {
            LevelEditor window = GetWindow<LevelEditor>("Level Editor");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }
        
        // Alternative menu item in case the main one doesn't show
        [MenuItem("Tools/Level Editor", false, 100)]
        public static void ShowWindowAlternative()
        {
            ShowWindow();
        }
        
        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosed += OnSceneClosed;
            FindSceneReferences();
            
            // Clean up any existing markers from previous scene
            CleanupMarkers();
            
            // Recreate markers if positions are set
            if (playerSpawnWorldPos.HasValue && sceneGridContainer != null && sceneGridContainer.Grid != null)
            {
                Vector3 pos = new Vector3(playerSpawnWorldPos.Value.x, 0f, playerSpawnWorldPos.Value.y);
                CreatePlayerMarker(pos);
            }
            
            if (goalWorldPos.HasValue && sceneGridContainer != null && sceneGridContainer.Grid != null)
            {
                Vector3 pos = new Vector3(goalWorldPos.Value.x, 0f, goalWorldPos.Value.y);
                CreateGoalMarker(pos);
            }
        }
        
        private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            // Clean up markers when opening a new scene
            CleanupMarkers();
            // Reset positions since they're scene-specific
            playerSpawnWorldPos = null;
            goalWorldPos = null;
            // Refresh references for new scene
            FindSceneReferences();
        }
        
        private void OnSceneClosed(UnityEngine.SceneManagement.Scene scene)
        {
            // Clean up markers when closing a scene
            CleanupMarkers();
        }
        
        /// <summary>
        /// Creates the player spawn marker at the specified position
        /// </summary>
        private void CreatePlayerMarker(Vector3 position)
        {
            if (playerMarker != null)
                return;
                
            playerMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            playerMarker.name = "PlayerSpawnMarker";
            playerMarker.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
            
            Collider col = playerMarker.GetComponent<Collider>();
            if (col != null)
            {
                DestroyImmediate(col);
            }
            
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.green;
            playerMarker.GetComponent<Renderer>().material = mat;
            playerMarker.transform.localScale = Vector3.one * 0.5f;
            playerMarker.transform.position = position;
        }
        
        /// <summary>
        /// Creates the goal marker at the specified position
        /// </summary>
        private void CreateGoalMarker(Vector3 position)
        {
            if (goalMarker != null)
                return;
                
            goalMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            goalMarker.name = "GoalMarker";
            goalMarker.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
            
            Collider col = goalMarker.GetComponent<Collider>();
            if (col != null)
            {
                DestroyImmediate(col);
            }
            
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.yellow;
            goalMarker.GetComponent<Renderer>().material = mat;
            goalMarker.transform.localScale = Vector3.one * 0.5f;
            goalMarker.transform.position = position;
        }
        
        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosed -= OnSceneClosed;
            CleanupMarkers();
        }
        
        private void OnDestroy()
        {
            CleanupMarkers();
        }
        
        /// <summary>
        /// Cleans up marker GameObjects when switching scenes or closing the window
        /// </summary>
        private void CleanupMarkers()
        {
            if (playerMarker != null)
            {
                DestroyImmediate(playerMarker);
                playerMarker = null;
            }
            
            if (goalMarker != null)
            {
                DestroyImmediate(goalMarker);
                goalMarker = null;
            }
            
            CleanupHazardMarkers();
        }
        
        /// <summary>
        /// Cleans up hazard marker GameObjects
        /// </summary>
        private void CleanupHazardMarkers()
        {
            foreach (var marker in hazardMarkers)
            {
                if (marker != null)
                {
                    DestroyImmediate(marker);
                }
            }
            hazardMarkers.Clear();
        }
        
        /// <summary>
        /// Refreshes hazard markers in the scene
        /// </summary>
        private void RefreshHazardMarkers()
        {
            CleanupHazardMarkers();
            
            if (currentLevelData == null || currentLevelData.hazards == null)
                return;
            
            foreach (var hazard in currentLevelData.hazards)
            {
                CreateHazardMarker(hazard);
            }
        }
        
        /// <summary>
        /// Creates a visual marker for a hazard
        /// </summary>
        private void CreateHazardMarker(HazardData hazard)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = $"HazardMarker_{hazard.type}";
            marker.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
            
            Collider col = marker.GetComponent<Collider>();
            if (col != null)
            {
                DestroyImmediate(col);
            }
            
            Material mat = new Material(Shader.Find("Standard"));
            switch (hazard.type)
            {
                case HazardData.HazardType.Rock:
                    mat.color = Color.gray;
                    break;
                case HazardData.HazardType.Sandtrap:
                    mat.color = new Color(0.8f, 0.7f, 0.4f); // Sandy color
                    break;
                case HazardData.HazardType.Whirlpool:
                    mat.color = Color.blue;
                    break;
                case HazardData.HazardType.Current:
                    mat.color = Color.cyan;
                    break;
            }
            marker.GetComponent<Renderer>().material = mat;
            marker.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
            marker.transform.position = new Vector3(hazard.position.x, 0f, hazard.position.y);
            marker.transform.rotation = Quaternion.Euler(0, hazard.rotation, 0);
            
            hazardMarkers.Add(marker);
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Level Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Level Data Selection
            EditorGUILayout.LabelField("Level Data", EditorStyles.boldLabel);
            LevelData newLevelData = (LevelData)EditorGUILayout.ObjectField(
                "Level Data",
                currentLevelData,
                typeof(LevelData),
                false
            );
            
            if (newLevelData != currentLevelData)
            {
                LoadLevelData(newLevelData);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            // Grid Settings
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
            gridCellSize = EditorGUILayout.IntField("Cell Size", gridCellSize);
            gridSize = EditorGUILayout.Vector2Field("Grid Size", gridSize);
            gridMaterial = (Material)EditorGUILayout.ObjectField("Grid Material", gridMaterial, typeof(Material), false);
            placementLayerMask = EditorGUILayout.LayerField("Placement Layer", placementLayerMask);
            
            EditorGUILayout.Space();
            
            // Refresh button to update scene references
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Scene References", GUILayout.Height(25)))
            {
                FindSceneReferences();
                if (sceneGridContainer != null)
                {
                    EditorUtility.DisplayDialog("Success", $"Found grid container: {sceneGridContainer.name}", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Not Found", "No SplineGridContainer found in scene. Create one first.", "OK");
                }
            }
            
            // Show current grid status
            if (sceneGridContainer != null)
            {
                EditorGUILayout.LabelField($"Grid: {sceneGridContainer.name}", EditorStyles.helpBox);
            }
            else
            {
                EditorGUILayout.LabelField("No grid found", EditorStyles.helpBox);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Create/Update Scene Grid Button
            if (GUILayout.Button("Create/Update Scene Grid", GUILayout.Height(30)))
            {
                CreateOrUpdateSceneGrid();
                // Refresh references after creating
                FindSceneReferences();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            // Boundary Editing
            EditorGUILayout.LabelField("Spline Boundary", EditorStyles.boldLabel);
            
            if (sceneSplineContainer != null && sceneSplineContainer.Spline != null)
            {
                var spline = sceneSplineContainer.Spline;
                EditorGUILayout.LabelField($"Boundary Points: {spline.Count}");
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Point"))
                {
                    AddBoundaryPoint();
                }
                if (GUILayout.Button("Clear All"))
                {
                    ClearBoundary();
                }
                EditorGUILayout.EndHorizontal();
                
                isEditingBoundary = EditorGUILayout.Toggle("Edit Boundary in Scene", isEditingBoundary);
            }
            else
            {
                EditorGUILayout.HelpBox("Create a scene grid first to edit the boundary.", MessageType.Info);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            // Player and Goal Markers
            EditorGUILayout.LabelField("Level Markers", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set Player Spawn"))
            {
                SetPlayerSpawn();
            }
            if (GUILayout.Button("Clear Player Spawn"))
            {
                ClearPlayerSpawn();
            }
            EditorGUILayout.EndHorizontal();
            
            if (playerSpawnWorldPos.HasValue)
            {
                EditorGUILayout.LabelField($"Player Spawn: {playerSpawnWorldPos.Value}");
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set Goal"))
            {
                SetGoal();
            }
            if (GUILayout.Button("Clear Goal"))
            {
                ClearGoal();
            }
            EditorGUILayout.EndHorizontal();
            
            if (goalWorldPos.HasValue)
            {
                EditorGUILayout.LabelField($"Goal: {goalWorldPos.Value}");
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            // Hazards Section
            EditorGUILayout.LabelField("Hazards", EditorStyles.boldLabel);
            
            if (currentLevelData == null)
            {
                EditorGUILayout.HelpBox("Load a LevelData asset to edit hazards.", MessageType.Info);
            }
            else
            {
                // Hazard type selection
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Hazard Type:", GUILayout.Width(100));
                selectedHazardType = (HazardData.HazardType)EditorGUILayout.EnumPopup(selectedHazardType);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Place Hazard"))
                {
                    isPlacingHazard = true;
                    // Don't show dialog during OnGUI - it causes layout issues
                    // User will see the instruction in the scene view
                }
                if (GUILayout.Button("Clear All Hazards"))
                {
                    if (EditorUtility.DisplayDialog("Clear All Hazards", 
                        "Are you sure you want to remove all hazards?", 
                        "Yes", "No"))
                    {
                        currentLevelData.hazards.Clear();
                        RefreshHazardMarkers();
                        EditorUtility.SetDirty(currentLevelData);
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                if (isPlacingHazard)
                {
                    EditorGUILayout.HelpBox("Click in the Scene View to place the hazard. Press Escape to cancel.", MessageType.Info);
                }
                
                // List existing hazards
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Hazards ({currentLevelData.hazards.Count}):");
                
                for (int i = 0; i < currentLevelData.hazards.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{i + 1}. {currentLevelData.hazards[i].type} at ({currentLevelData.hazards[i].position.x:F1}, {currentLevelData.hazards[i].position.y:F1})");
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        currentLevelData.hazards.RemoveAt(i);
                        RefreshHazardMarkers();
                        EditorUtility.SetDirty(currentLevelData);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            // Save Section
            EditorGUILayout.LabelField("Save Level", EditorStyles.boldLabel);
            
            if (currentLevelData == null)
            {
                EditorGUILayout.HelpBox("Create or load a LevelData asset to save.", MessageType.Info);
                
                if (GUILayout.Button("Create New Level Data", GUILayout.Height(30)))
                {
                    CreateNewLevelData();
                }
            }
            else
            {
                EditorGUILayout.LabelField($"Current: {currentLevelData.name}");
                
                if (GUILayout.Button("Save to Level Data", GUILayout.Height(30)))
                {
                    SaveToLevelData();
                }
            }
        }
        
        private void OnSceneGUI(SceneView sceneView)
        {
            if (sceneSplineContainer == null)
                return;
            
            var spline = sceneSplineContainer.Spline;
            if (spline == null)
                return;
            
            // Draw boundary points
            if (isEditingBoundary)
            {
                for (int i = 0; i < spline.Count; i++)
                {
                    var knot = spline[i];
                    Vector3 worldPos = sceneSplineContainer.transform.TransformPoint(knot.Position);
                    
                    // Draw handle
                    EditorGUI.BeginChangeCheck();
                    Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(sceneSplineContainer, "Move Boundary Point");
                        Vector3 localPos = sceneSplineContainer.transform.InverseTransformPoint(newWorldPos);
                        knot.Position = localPos;
                        spline[i] = knot;
                        EditorUtility.SetDirty(sceneSplineContainer);
                    }
                    
                    // Draw label
                    Handles.Label(worldPos + Vector3.up * 0.5f, $"Point {i}");
                }
            }
            
            // Draw player spawn marker with draggable handle
            if (playerSpawnWorldPos.HasValue)
            {
                Vector3 pos = new Vector3(playerSpawnWorldPos.Value.x, 0f, playerSpawnWorldPos.Value.y);
                
                Handles.color = Color.green;
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.PositionHandle(pos, Quaternion.identity);
                
                if (EditorGUI.EndChangeCheck())
                {
                    // Snap to grid if available
                    Grid grid = GetSceneGrid();
                    if (grid != null)
                    {
                        Vector3Int cellPos = grid.WorldToCell(newPos);
                        newPos = grid.GetCellCenterWorld(cellPos);
                        newPos.y = 0f;
                    }
                    
                    playerSpawnWorldPos = new Vector2(newPos.x, newPos.z);
                    if (playerMarker != null)
                    {
                        playerMarker.transform.position = newPos;
                    }
                }
                
                Handles.DrawWireCube(pos, Vector3.one * HANDLE_SIZE);
                Handles.Label(pos + Vector3.up, "Player Spawn");
            }
            
            // Draw goal marker with draggable handle
            if (goalWorldPos.HasValue)
            {
                Vector3 pos = new Vector3(goalWorldPos.Value.x, 0f, goalWorldPos.Value.y);
                
                Handles.color = Color.yellow;
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.PositionHandle(pos, Quaternion.identity);
                
                if (EditorGUI.EndChangeCheck())
                {
                    // Snap to grid if available
                    Grid grid = GetSceneGrid();
                    if (grid != null)
                    {
                        Vector3Int cellPos = grid.WorldToCell(newPos);
                        newPos = grid.GetCellCenterWorld(cellPos);
                        newPos.y = 0f;
                    }
                    
                    goalWorldPos = new Vector2(newPos.x, newPos.z);
                    if (goalMarker != null)
                    {
                        goalMarker.transform.position = newPos;
                    }
                }
                
                Handles.DrawWireCube(pos, Vector3.one * HANDLE_SIZE);
                Handles.Label(pos + Vector3.up, "Goal");
            }
            
            // Draw hazard markers
            if (currentLevelData != null && currentLevelData.hazards != null)
            {
                for (int i = 0; i < currentLevelData.hazards.Count; i++)
                {
                    var hazard = currentLevelData.hazards[i];
                    Vector3 pos = new Vector3(hazard.position.x, 0f, hazard.position.y);
                    
                    Color hazardColor = Color.gray;
                    switch (hazard.type)
                    {
                        case HazardData.HazardType.Rock:
                            hazardColor = Color.gray;
                            break;
                        case HazardData.HazardType.Sandtrap:
                            hazardColor = new Color(0.8f, 0.7f, 0.4f);
                            break;
                        case HazardData.HazardType.Whirlpool:
                            hazardColor = Color.blue;
                            break;
                        case HazardData.HazardType.Current:
                            hazardColor = Color.cyan;
                            break;
                    }
                    
                    Handles.color = hazardColor;
                    
                    EditorGUI.BeginChangeCheck();
                    Vector3 newPos = Handles.PositionHandle(pos, Quaternion.Euler(0, hazard.rotation, 0));
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        // Snap to grid if available
                        Grid grid = GetSceneGrid();
                        if (grid != null)
                        {
                            Vector3Int cellPos = grid.WorldToCell(newPos);
                            newPos = grid.GetCellCenterWorld(cellPos);
                            newPos.y = 0f;
                        }
                        
                        hazard.position = new Vector2(newPos.x, newPos.z);
                        RefreshHazardMarkers();
                        EditorUtility.SetDirty(currentLevelData);
                    }
                    
                    Handles.DrawWireCube(pos, Vector3.one * HANDLE_SIZE);
                    Handles.Label(pos + Vector3.up * 1.5f, $"{hazard.type} {i + 1}");
                }
            }
            
            // Handle hazard placement
            if (isPlacingHazard)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                
                Event e = Event.current;
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    // Place hazard at mouse position
                    Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                    Plane plane = new Plane(Vector3.up, Vector3.zero);
                    if (plane.Raycast(ray, out float distance))
                    {
                        Vector3 worldPos = ray.GetPoint(distance);
                        
                        // Snap to grid if available
                        Grid grid = GetSceneGrid();
                        if (grid != null)
                        {
                            Vector3Int cellPos = grid.WorldToCell(worldPos);
                            worldPos = grid.GetCellCenterWorld(cellPos);
                            worldPos.y = 0f;
                        }
                        
                        HazardData newHazard = new HazardData
                        {
                            type = selectedHazardType,
                            position = new Vector2(worldPos.x, worldPos.z),
                            rotation = 0f,
                            scale = 1f
                        };
                        
                        if (currentLevelData != null)
                        {
                            currentLevelData.hazards.Add(newHazard);
                            RefreshHazardMarkers();
                            EditorUtility.SetDirty(currentLevelData);
                        }
                        
                        isPlacingHazard = false;
                        e.Use();
                    }
                }
                else if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.RightArrow))
                {
                    isPlacingHazard = false;
                    e.Use();
                }
            }
        }
        
        private void FindSceneReferences()
        {
            sceneGridContainer = FindFirstObjectByType<SplineGridContainer>();
            if (sceneGridContainer != null)
            {
                sceneSplineContainer = sceneGridContainer.SplineContainer;
            }
            else
            {
                sceneSplineContainer = FindFirstObjectByType<SplineContainer>();
            }
        }
        
        /// <summary>
        /// Safely gets the Grid component from the scene grid container
        /// </summary>
        private Grid GetSceneGrid()
        {
            if (sceneGridContainer == null)
                return null;
            
            Grid grid = sceneGridContainer.Grid;
            if (grid == null)
            {
                // Try to find the Grid child object
                Transform gridTransform = sceneGridContainer.transform.Find("Grid");
                if (gridTransform != null)
                {
                    grid = gridTransform.GetComponent<Grid>();
                }
            }
            
            return grid;
        }
        
        private void CreateOrUpdateSceneGrid()
        {
            // Find or create grid container
            if (sceneGridContainer == null)
            {
                GameObject containerObject = new GameObject("LevelEditor_GridContainer");
                sceneSplineContainer = containerObject.AddComponent<SplineContainer>();
                sceneGridContainer = containerObject.AddComponent<SplineGridContainer>();
                
                // Force Awake to run by enabling the component
                sceneGridContainer.enabled = true;
            }
            
            // Ensure Grid is initialized - manually initialize if needed
            if (sceneGridContainer.Grid == null)
            {
                // The Grid should be created in Awake, but if it's not, we need to create it manually
                Transform gridTransform = sceneGridContainer.transform.Find("Grid");
                if (gridTransform == null)
                {
                    // Grid wasn't created, so Awake didn't run. Force initialization by calling the private method via reflection
                    var initializeMethod = typeof(SplineGridContainer).GetMethod("InitializeGrid", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (initializeMethod != null)
                    {
                        initializeMethod.Invoke(sceneGridContainer, null);
                    }
                }
            }
            
            var spline = sceneSplineContainer.Spline;
            if (spline == null)
                return;
            
            // Create default rectangular boundary if empty
            if (spline.Count == 0)
            {
                float worldWidth = gridSize.x * gridCellSize;
                float worldHeight = gridSize.y * gridCellSize;
                float halfWidth = worldWidth * 0.5f;
                float halfHeight = worldHeight * 0.5f;
                
                spline.Clear();
                spline.Add(new BezierKnot(new Vector3(-halfWidth, 0f, -halfHeight)));
                spline.Add(new BezierKnot(new Vector3(halfWidth, 0f, -halfHeight)));
                spline.Add(new BezierKnot(new Vector3(halfWidth, 0f, halfHeight)));
                spline.Add(new BezierKnot(new Vector3(-halfWidth, 0f, halfHeight)));
                spline.Closed = true;
            }
            
            EditorUtility.SetDirty(sceneSplineContainer);
            Selection.activeGameObject = sceneGridContainer.gameObject;
            
            // Refresh references after creation
            FindSceneReferences();
        }
        
        private void AddBoundaryPoint()
        {
            if (sceneSplineContainer == null)
                return;
            
            var spline = sceneSplineContainer.Spline;
            if (spline == null)
                return;
            
            Undo.RecordObject(sceneSplineContainer, "Add Boundary Point");
            
            // Add a new point at the end
            Vector3 newPos = Vector3.zero;
            if (spline.Count > 0)
            {
                var lastKnot = spline[spline.Count - 1];
                newPos = (Vector3)lastKnot.Position + new Vector3(2f, 0f, 2f);
            }
            
            spline.Add(new BezierKnot(newPos));
            EditorUtility.SetDirty(sceneSplineContainer);
        }
        
        private void ClearBoundary()
        {
            if (sceneSplineContainer == null)
                return;
            
            var spline = sceneSplineContainer.Spline;
            if (spline == null)
                return;
            
            Undo.RecordObject(sceneSplineContainer, "Clear Boundary");
            spline.Clear();
            EditorUtility.SetDirty(sceneSplineContainer);
        }
        
        private void SetPlayerSpawn()
        {
            // Refresh scene references before checking
            FindSceneReferences();
            
            if (sceneGridContainer == null)
            {
                EditorUtility.DisplayDialog("Error", "No SplineGridContainer found in scene. Create a scene grid first.", "OK");
                return;
            }
            
            // Get the grid reference, trying multiple methods
            Grid grid = sceneGridContainer.Grid;
            if (grid == null)
            {
                // Try to find the Grid child object
                Transform gridTransform = sceneGridContainer.transform.Find("Grid");
                if (gridTransform != null)
                {
                    grid = gridTransform.GetComponent<Grid>();
                }
            }
            
            // Final check - if still null, show error
            if (grid == null)
            {
                EditorUtility.DisplayDialog("Error", "Grid not initialized. The grid container may need to be recreated. Try clicking 'Create/Update Scene Grid' again.", "OK");
                return;
            }
            
            // Use the selected object's position, or scene view camera position, or default to origin
            Vector3 worldPos = Vector3.zero;
            if (Selection.activeGameObject != null)
            {
                worldPos = Selection.activeGameObject.transform.position;
            }
            else if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            {
                // Use scene view camera position projected to ground
                Vector3 camPos = SceneView.lastActiveSceneView.camera.transform.position;
                worldPos = new Vector3(camPos.x, 0f, camPos.z);
            }
            
            // Snap to grid cell center
            Vector3Int cellPos = grid.WorldToCell(worldPos);
            worldPos = grid.GetCellCenterWorld(cellPos);
            worldPos.y = 0f;
            
            playerSpawnWorldPos = new Vector2(worldPos.x, worldPos.z);
            
            // Update marker in scene
            if (playerMarker == null)
            {
                playerMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                playerMarker.name = "PlayerSpawnMarker";
                playerMarker.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy; // Don't save marker to scene, hide in hierarchy
                
                // Remove collider to avoid physics issues
                Collider col = playerMarker.GetComponent<Collider>();
                if (col != null)
                {
                    DestroyImmediate(col);
                }
                
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = Color.green;
                playerMarker.GetComponent<Renderer>().material = mat;
                
                // Make it slightly smaller
                playerMarker.transform.localScale = Vector3.one * 0.5f;
                
                // Mark as editor-only
                if (Application.isPlaying == false)
                {
                    playerMarker.hideFlags |= HideFlags.NotEditable;
                }
            }
            playerMarker.transform.position = worldPos;
            
            EditorUtility.DisplayDialog("Player Spawn Set", $"Player spawn set to cell {cellPos} at position {worldPos}", "OK");
        }
        
        private void ClearPlayerSpawn()
        {
            playerSpawnWorldPos = null;
            if (playerMarker != null)
            {
                DestroyImmediate(playerMarker);
                playerMarker = null;
            }
        }
        
        private void SetGoal()
        {
            // Refresh scene references before checking
            FindSceneReferences();
            
            if (sceneGridContainer == null)
            {
                EditorUtility.DisplayDialog("Error", "No SplineGridContainer found in scene. Create a scene grid first.", "OK");
                return;
            }
            
            // Get the grid reference
            Grid grid = GetSceneGrid();
            if (grid == null)
            {
                EditorUtility.DisplayDialog("Error", "Grid not initialized. The grid container may need to be recreated. Try clicking 'Create/Update Scene Grid' again.", "OK");
                return;
            }
            
            // Use the selected object's position, or scene view camera position, or default to origin
            Vector3 worldPos = Vector3.zero;
            if (Selection.activeGameObject != null)
            {
                worldPos = Selection.activeGameObject.transform.position;
            }
            else if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            {
                // Use scene view camera position projected to ground
                Vector3 camPos = SceneView.lastActiveSceneView.camera.transform.position;
                worldPos = new Vector3(camPos.x, 0f, camPos.z);
            }
            
            // Snap to grid cell center
            Vector3Int cellPos = grid.WorldToCell(worldPos);
            worldPos = grid.GetCellCenterWorld(cellPos);
            worldPos.y = 0f;
            
            goalWorldPos = new Vector2(worldPos.x, worldPos.z);
            
            // Update marker in scene
            if (goalMarker == null)
            {
                goalMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                goalMarker.name = "GoalMarker";
                goalMarker.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy; // Don't save marker to scene, hide in hierarchy
                
                // Remove collider to avoid physics issues
                Collider col = goalMarker.GetComponent<Collider>();
                if (col != null)
                {
                    DestroyImmediate(col);
                }
                
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = Color.yellow;
                goalMarker.GetComponent<Renderer>().material = mat;
                
                // Make it slightly smaller
                goalMarker.transform.localScale = Vector3.one * 0.5f;
                
                // Mark as editor-only
                if (Application.isPlaying == false)
                {
                    goalMarker.hideFlags |= HideFlags.NotEditable;
                }
            }
            goalMarker.transform.position = worldPos;
            
            EditorUtility.DisplayDialog("Goal Set", $"Goal set to cell {cellPos} at position {worldPos}", "OK");
        }
        
        private void ClearGoal()
        {
            goalWorldPos = null;
            if (goalMarker != null)
            {
                DestroyImmediate(goalMarker);
                goalMarker = null;
            }
        }
        
        private void LoadLevelData(LevelData levelData)
        {
            currentLevelData = levelData;
            
            if (levelData == null)
            {
                serializedLevelData = null;
                return;
            }
            
            serializedLevelData = new SerializedObject(levelData);
            
            // Load settings
            gridCellSize = levelData.gridCellSize;
            gridSize = levelData.gridSize;
            gridMaterial = levelData.gridMaterial;
            placementLayerMask = levelData.placementLayerMask;
            
            // Load boundary points
            if (sceneSplineContainer != null && levelData.splineBoundaryPoints != null && levelData.splineBoundaryPoints.Count > 0)
            {
                var spline = sceneSplineContainer.Spline;
                if (spline != null)
                {
                    spline.Clear();
                    foreach (Vector2 point in levelData.splineBoundaryPoints)
                    {
                        Vector3 worldPos = new Vector3(point.x, 0f, point.y);
                        Vector3 localPos = sceneSplineContainer.transform.InverseTransformPoint(worldPos);
                        spline.Add(new BezierKnot(localPos));
                    }
                    spline.Closed = true;
                    EditorUtility.SetDirty(sceneSplineContainer);
                }
            }
            
            // Load markers
            Grid grid = GetSceneGrid();
            if (grid != null)
            {
                Vector3 playerWorld = grid.GetCellCenterWorld(levelData.playerSpawnCell);
                playerSpawnWorldPos = new Vector2(playerWorld.x, playerWorld.z);
                
                Vector3 goalWorld = grid.GetCellCenterWorld(levelData.goalCell);
                goalWorldPos = new Vector2(goalWorld.x, goalWorld.z);
                
                // Update markers in scene
                if (playerSpawnWorldPos.HasValue)
                {
                    if (playerMarker == null)
                    {
                        playerMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        playerMarker.name = "PlayerSpawnMarker";
                        Material mat = new Material(Shader.Find("Standard"));
                        mat.color = Color.green;
                        playerMarker.GetComponent<Renderer>().material = mat;
                    }
                    playerMarker.transform.position = new Vector3(playerSpawnWorldPos.Value.x, 0f, playerSpawnWorldPos.Value.y);
                }
                
                if (goalWorldPos.HasValue)
                {
                    if (goalMarker == null)
                    {
                        goalMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        goalMarker.name = "GoalMarker";
                        Material mat = new Material(Shader.Find("Standard"));
                        mat.color = Color.yellow;
                        goalMarker.GetComponent<Renderer>().material = mat;
                    }
                    goalMarker.transform.position = new Vector3(goalWorldPos.Value.x, 0f, goalWorldPos.Value.y);
                }
            }
            
            // Load hazards
            RefreshHazardMarkers();
        }
        
        private void CreateNewLevelData()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New Level Data",
                "NewLevel",
                "asset",
                "Choose where to save the level data"
            );
            
            if (string.IsNullOrEmpty(path))
                return;
            
            LevelData newLevelData = CreateInstance<LevelData>();
            newLevelData.levelName = System.IO.Path.GetFileNameWithoutExtension(path);
            
            AssetDatabase.CreateAsset(newLevelData, path);
            AssetDatabase.SaveAssets();
            
            LoadLevelData(newLevelData);
            Selection.activeObject = newLevelData;
        }
        
        private void SaveToLevelData()
        {
            if (currentLevelData == null)
            {
                EditorUtility.DisplayDialog("Error", "No LevelData assigned. Create or load one first.", "OK");
                return;
            }
            
            if (sceneSplineContainer == null || sceneSplineContainer.Spline == null)
            {
                EditorUtility.DisplayDialog("Error", "No spline container found in scene. Create a scene grid first.", "OK");
                return;
            }
            
            Undo.RecordObject(currentLevelData, "Save Level Data");
            
            // Save grid settings
            currentLevelData.gridCellSize = gridCellSize;
            currentLevelData.gridSize = gridSize;
            currentLevelData.gridMaterial = gridMaterial;
            currentLevelData.placementLayerMask = placementLayerMask;
            
            // Save boundary points
            var spline = sceneSplineContainer.Spline;
            currentLevelData.splineBoundaryPoints.Clear();
            for (int i = 0; i < spline.Count; i++)
            {
                var knot = spline[i];
                Vector3 worldPos = sceneSplineContainer.transform.TransformPoint(knot.Position);
                currentLevelData.splineBoundaryPoints.Add(new Vector2(worldPos.x, worldPos.z));
            }
            
            // Save player and goal positions
            Grid grid = GetSceneGrid();
            if (grid != null)
            {
                if (playerSpawnWorldPos.HasValue)
                {
                    Vector3 worldPos = new Vector3(playerSpawnWorldPos.Value.x, 0f, playerSpawnWorldPos.Value.y);
                    currentLevelData.playerSpawnCell = grid.WorldToCell(worldPos);
                }
                
                if (goalWorldPos.HasValue)
                {
                    Vector3 worldPos = new Vector3(goalWorldPos.Value.x, 0f, goalWorldPos.Value.y);
                    currentLevelData.goalCell = grid.WorldToCell(worldPos);
                }
            }
            
            EditorUtility.SetDirty(currentLevelData);
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("Success", $"Level data saved to {currentLevelData.name}", "OK");
        }
    }
}

