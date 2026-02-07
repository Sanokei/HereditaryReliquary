using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.Collections.Generic;

namespace GridRingTool
{
    public class GridRingToolWindow : EditorWindow
    {
        private Tilemap targetTilemap;
        private GameObject ringPrefab;
        private Transform parent;
        private Vector3 positionOffset = Vector3.zero;
        private bool clearExisting = true;
        private const string RING_OBJECT_NAME_PREFIX = "RingObject_";
        
        // Static reference to last used prefab for context menu
        private static GameObject lastUsedPrefab;
        
        [MenuItem("Window/Level Tools/Grid Ring Tool")]
        public static void ShowWindow()
        {
            GridRingToolWindow window = GetWindow<GridRingToolWindow>("Grid Ring Tool");
            window.minSize = new Vector2(300, 250);
            window.Show();
        }
        
        [MenuItem("CONTEXT/Tilemap/Generate Ring From Prefab")]
        public static void GenerateRingFromContext(MenuCommand command)
        {
            Tilemap tilemap = command.context as Tilemap;
            if (tilemap == null)
                return;
            
            // Open the window and set the tilemap
            GridRingToolWindow window = GetWindow<GridRingToolWindow>("Grid Ring Tool");
            window.targetTilemap = tilemap;
            window.Show();
            
            // If we have a last used prefab, set it
            if (lastUsedPrefab != null)
            {
                window.ringPrefab = lastUsedPrefab;
            }
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Grid Ring Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Tilemap field
            targetTilemap = (Tilemap)EditorGUILayout.ObjectField(
                "Target Tilemap",
                targetTilemap,
                typeof(Tilemap),
                true
            );
            
            // Prefab field
            ringPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Ring Prefab",
                ringPrefab,
                typeof(GameObject),
                false
            );
            
            // Parent field
            parent = (Transform)EditorGUILayout.ObjectField(
                "Parent Transform",
                parent,
                typeof(Transform),
                true
            );
            
            // Position offset
            positionOffset = EditorGUILayout.Vector3Field("Position Offset", positionOffset);
            
            // Clear existing toggle
            clearExisting = EditorGUILayout.Toggle("Clear Existing Ring", clearExisting);
            
            EditorGUILayout.Space();
            
            // Validation
            bool canGenerate = ValidateInputs();
            
            if (!canGenerate)
            {
                EditorGUILayout.HelpBox(
                    "Please assign a Tilemap and a Prefab to generate the ring.",
                    MessageType.Warning
                );
            }
            
            // Generate button
            EditorGUI.BeginDisabledGroup(!canGenerate);
            if (GUILayout.Button("Generate Ring", GUILayout.Height(30)))
            {
                GenerateRing();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space();
            
            // Info box
            if (targetTilemap != null)
            {
                BoundsInt bounds = targetTilemap.cellBounds;
                EditorGUILayout.HelpBox(
                    $"Tilemap Bounds: {bounds.min} to {bounds.max}\n" +
                    $"Size: {bounds.size.x} x {bounds.size.y} x {bounds.size.z}",
                    MessageType.Info
                );
            }
        }
        
        private bool ValidateInputs()
        {
            return targetTilemap != null && ringPrefab != null;
        }
        
        private void GenerateRing()
        {
            if (!ValidateInputs())
            {
                Debug.LogError("Grid Ring Tool: Cannot generate ring - missing required references.");
                return;
            }
            
            // Determine parent
            Transform targetParent = parent != null ? parent : targetTilemap.transform;
            
            // Clear existing ring objects if requested
            if (clearExisting)
            {
                ClearExistingRingObjects(targetParent);
            }
            
            // Calculate expanded bounds and get ring cells
            List<Vector3Int> ringCells = CalculateRingCells();
            
            if (ringCells.Count == 0)
            {
                Debug.LogWarning("Grid Ring Tool: No ring cells found. The tilemap may be empty or too small.");
                return;
            }
            
            // Store prefab for context menu use
            lastUsedPrefab = ringPrefab;
            
            // Instantiate prefabs at each ring cell
            int createdCount = 0;
            foreach (Vector3Int cellPosition in ringCells)
            {
                Vector3 worldPosition = targetTilemap.CellToWorld(cellPosition) + positionOffset;
                GameObject instance = PrefabUtility.InstantiatePrefab(ringPrefab, targetParent) as GameObject;
                
                if (instance != null)
                {
                    instance.transform.position = worldPosition;
                    instance.name = $"{RING_OBJECT_NAME_PREFIX}{cellPosition.x}_{cellPosition.y}_{cellPosition.z}";
                    
                    // Register undo
                    Undo.RegisterCreatedObjectUndo(instance, "Create Ring Object");
                    createdCount++;
                }
            }
            
            // Mark scene as dirty
            if (createdCount > 0)
            {
                Undo.FlushUndoRecordObjects();
                EditorUtility.SetDirty(targetParent.gameObject);
                Debug.Log($"Grid Ring Tool: Created {createdCount} ring objects around the tilemap.");
            }
        }
        
        private List<Vector3Int> CalculateRingCells()
        {
            HashSet<Vector3Int> ringCellsSet = new HashSet<Vector3Int>();
            
            if (targetTilemap == null)
                return new List<Vector3Int>();
            
            // Get the tilemap's cell bounds
            BoundsInt originalBounds = targetTilemap.cellBounds;
            
            // Expand bounds by 1 cell in all directions
            BoundsInt expandedBounds = new BoundsInt(
                originalBounds.xMin - 1,
                originalBounds.yMin - 1,
                originalBounds.zMin - 1,
                originalBounds.size.x + 2,
                originalBounds.size.y + 2,
                originalBounds.size.z + 2
            );
            
            // For 3D, we need to handle all faces of the expanded bounds
            // The ring consists of cells on the outer surface of the expanded bounds
            // Using HashSet to avoid duplicates at edges and corners
            
            // Top and bottom faces (constant Y)
            for (int x = expandedBounds.xMin; x < expandedBounds.xMax; x++)
            {
                for (int z = expandedBounds.zMin; z < expandedBounds.zMax; z++)
                {
                    // Bottom face (yMin)
                    ringCellsSet.Add(new Vector3Int(x, expandedBounds.yMin, z));
                    
                    // Top face (yMax - 1, since bounds are exclusive)
                    if (expandedBounds.size.y > 1)
                    {
                        ringCellsSet.Add(new Vector3Int(x, expandedBounds.yMax - 1, z));
                    }
                }
            }
            
            // Front and back faces (constant Z)
            for (int x = expandedBounds.xMin; x < expandedBounds.xMax; x++)
            {
                for (int y = expandedBounds.yMin + 1; y < expandedBounds.yMax - 1; y++)
                {
                    // Front face (zMin)
                    ringCellsSet.Add(new Vector3Int(x, y, expandedBounds.zMin));
                    
                    // Back face (zMax - 1)
                    if (expandedBounds.size.z > 1)
                    {
                        ringCellsSet.Add(new Vector3Int(x, y, expandedBounds.zMax - 1));
                    }
                }
            }
            
            // Left and right faces (constant X)
            for (int y = expandedBounds.yMin + 1; y < expandedBounds.yMax - 1; y++)
            {
                for (int z = expandedBounds.zMin + 1; z < expandedBounds.zMax - 1; z++)
                {
                    // Left face (xMin)
                    ringCellsSet.Add(new Vector3Int(expandedBounds.xMin, y, z));
                    
                    // Right face (xMax - 1)
                    if (expandedBounds.size.x > 1)
                    {
                        ringCellsSet.Add(new Vector3Int(expandedBounds.xMax - 1, y, z));
                    }
                }
            }
            
            return new List<Vector3Int>(ringCellsSet);
        }
        
        private void ClearExistingRingObjects(Transform parent)
        {
            List<GameObject> toDestroy = new List<GameObject>();
            
            // Find all children with the ring object name prefix
            foreach (Transform child in parent)
            {
                if (child.name.StartsWith(RING_OBJECT_NAME_PREFIX))
                {
                    toDestroy.Add(child.gameObject);
                }
            }
            
            // Destroy found objects
            foreach (GameObject obj in toDestroy)
            {
                Undo.DestroyObjectImmediate(obj);
            }
            
            if (toDestroy.Count > 0)
            {
                Debug.Log($"Grid Ring Tool: Cleared {toDestroy.Count} existing ring objects.");
            }
        }
    }
}

