using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridBuilder.Core
{
    public enum CellSizePlacementMode
    {
        ConvertCells,  // Convert occupied cells from database cell size to container cell size (default)
        ScaleObject    // Scale the object prefab to match cell size difference, no cell conversion
    }
    
    [CreateAssetMenu(menuName = "GridBuilder/Objects Database", fileName = "ObjectsDatabaseSO")]
    public class ObjectsDatabaseSO : ScriptableObject
    {
        public LayerMask placementLayermask;
        [SerializeField, Min(1)] private int cellSize = 1;
        public List<ObjectData> objectsData;
        
        // Track previous cell size for conversion (editor only)
        #if UNITY_EDITOR
        [SerializeField, HideInInspector] private int previousCellSize = 1;
        #endif
        
        public int CellSize => cellSize;
        
        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Convert occupied cells when cellSize changes
            if (cellSize != previousCellSize && previousCellSize > 0 && objectsData != null)
            {
                ConvertOccupiedCellsForNewCellSize(previousCellSize, cellSize);
                previousCellSize = cellSize;
            }
            else if (previousCellSize == 0)
            {
                // Initialize previous cell size
                previousCellSize = cellSize;
            }
        }
        
        /// <summary>
        /// Converts all occupied cells in all objects when cellSize changes
        /// </summary>
        private void ConvertOccupiedCellsForNewCellSize(int oldCellSize, int newCellSize)
        {
            if (oldCellSize == newCellSize || objectsData == null)
                return;
            
            foreach (var objectData in objectsData)
            {
                if (objectData == null || objectData.OccupiedCells == null || objectData.OccupiedCells.Count == 0)
                    continue;
                
                // Convert cells from old cell size to new cell size
                // Formula: newCell = (oldCell * oldCellSize) / newCellSize
                List<Vector3Int> convertedCells = new List<Vector3Int>();
                HashSet<Vector3Int> uniqueCells = new HashSet<Vector3Int>();
                
                foreach (var cell in objectData.OccupiedCells)
                {
                    // Convert each cell coordinate
                    int newX = Mathf.RoundToInt((cell.x * oldCellSize) / (float)newCellSize);
                    int newY = Mathf.RoundToInt((cell.y * oldCellSize) / (float)newCellSize);
                    int newZ = Mathf.RoundToInt((cell.z * oldCellSize) / (float)newCellSize);
                    
                    Vector3Int convertedCell = new Vector3Int(newX, newY, newZ);
                    
                    // Only add unique cells
                    if (uniqueCells.Add(convertedCell))
                    {
                        convertedCells.Add(convertedCell);
                    }
                }
                
                // Ensure at least one cell
                if (convertedCells.Count == 0)
                {
                    convertedCells.Add(Vector3Int.zero);
                }
                
                // Update the occupied cells using reflection
                var occupiedCellsProperty = typeof(ObjectData).GetProperty("OccupiedCells");
                if (occupiedCellsProperty != null)
                {
                    occupiedCellsProperty.SetValue(objectData, convertedCells);
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        #endif
    }

    [Serializable]
    public class ObjectData
    {
        [field: SerializeField]
        public string Name { get; private set; }
        [field: SerializeField]
        public int ID { get; private set; }
        [field: SerializeField]
        public List<Vector3Int> OccupiedCells { get; private set; } = new List<Vector3Int> { Vector3Int.zero };
        [field: SerializeField]
        public GameObject Prefab { get; private set; }
        
        [Tooltip("Custom placement validators. All validators must pass for placement to be valid.")]
        [SerializeField]
        public List<PlacementValidatorSO> placementValidators = new List<PlacementValidatorSO>();
        
        #if UNITY_EDITOR
        /// <summary>
        /// Sets the ID (editor only, called by custom editor)
        /// </summary>
        public void SetID(int id)
        {
            var idField = typeof(ObjectData).GetField("<ID>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (idField != null)
            {
                idField.SetValue(this, id);
            }
        }
        
        /// <summary>
        /// Sets the prefab and auto-calculates occupied cells (editor only)
        /// </summary>
        public void SetPrefab(GameObject prefab, int cellSize)
        {
            var prefabField = typeof(ObjectData).GetField("<Prefab>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (prefabField != null)
            {
                prefabField.SetValue(this, prefab);
            }
            
            if (prefab != null)
            {
                CalculateOccupiedCellsFromPrefab(cellSize);
            }
        }
        
        /// <summary>
        /// Calculates occupied cells based on prefab bounds
        /// Uses the same logic as OccupiedCellsEditor for consistency
        /// </summary>
        private void CalculateOccupiedCellsFromPrefab(int cellSize)
        {
            if (Prefab == null || cellSize <= 0)
                return;
            
            // Get the reflection field info once
            var occupiedCellsFieldInfo = typeof(ObjectData).GetField("<OccupiedCells>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (occupiedCellsFieldInfo == null)
                return;
            
            // Temporarily instantiate prefab to calculate bounds (same as editor)
            GameObject tempInstance = null;
            try
            {
                tempInstance = UnityEngine.Object.Instantiate(Prefab);
                tempInstance.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInHierarchy;
                
                // Position at origin for bounds calculation
                // Don't reset scale - use the prefab's actual scale to match editor behavior
                tempInstance.transform.position = Vector3.zero;
                tempInstance.transform.rotation = Quaternion.identity;
                // Keep the prefab's original scale - don't reset to Vector3.one
                
                // Use Renderer bounds which are already calculated in world space
                // This is more reliable than calculating from MeshFilter bounds
                Renderer[] renderers = tempInstance.GetComponentsInChildren<Renderer>();
                Bounds prefabBounds;
                bool usingRenderers = renderers.Length > 0;
                
                if (!usingRenderers)
                {
                    // Fallback to MeshFilter if no renderers
                    MeshFilter[] meshFilters = tempInstance.GetComponentsInChildren<MeshFilter>();
                    if (meshFilters.Length == 0)
                    {
                        // Default to single cell if no meshes
                        occupiedCellsFieldInfo.SetValue(this, new List<Vector3Int> { Vector3Int.zero });
                        return;
                    }
                    
                    // Calculate bounds from MeshFilters (same as editor)
                    prefabBounds = new Bounds();
                    bool boundsInitialized = false;
                    
                    foreach (MeshFilter meshFilter in meshFilters)
                    {
                        if (meshFilter.sharedMesh != null)
                        {
                            Bounds meshBounds = meshFilter.sharedMesh.bounds;
                            Bounds worldBounds = new Bounds(
                                meshFilter.transform.TransformPoint(meshBounds.center),
                                meshBounds.size
                            );
                            
                            worldBounds.size = Vector3.Scale(worldBounds.size, meshFilter.transform.lossyScale);
                            
                            if (!boundsInitialized)
                            {
                                prefabBounds = worldBounds;
                                boundsInitialized = true;
                            }
                            else
                            {
                                prefabBounds.Encapsulate(worldBounds);
                            }
                        }
                    }
                }
                else
                {
                    // Use Renderer bounds (more accurate, already in world space)
                    prefabBounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        prefabBounds.Encapsulate(renderers[i].bounds);
                    }
                }
                
                // Ensure renderers are updated (force bounds recalculation)
                if (usingRenderers)
                {
                    foreach (Renderer renderer in renderers)
                    {
                        renderer.enabled = true;
                    }
                    
                    // Recalculate bounds after ensuring renderers are enabled
                    prefabBounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        prefabBounds.Encapsulate(renderers[i].bounds);
                    }
                }
                
                // Calculate grid dimensions based on bounds extent
                // Cell (0,0,0) starts at bounds.min
                // We need to calculate how many cells the bounds span from bounds.min to bounds.max
                Vector3 boundsMin = prefabBounds.min;
                Vector3 boundsMax = prefabBounds.max;
                Vector3 boundsSize = prefabBounds.size;
                
                // Calculate grid dimensions using the exact same math as the editor's CalculateGridDimensions
                Vector3 size = prefabBounds.size;
                Vector3Int gridDimensions = new Vector3Int(
                    Mathf.CeilToInt(size.x / cellSize),
                    Mathf.CeilToInt(size.y / cellSize),
                    Mathf.CeilToInt(size.z / cellSize)
                );
                
                // Calculate which cells are occupied using the same logic as CreateGridFloorMesh
                // The grid floor creates quads from (0,0) to (gridDimensions.x-1, gridDimensions.z-1) with Y=0
                // So we fill all cells from (0,0,0) to (gridDimensions.x-1, 0, gridDimensions.z-1)
                List<Vector3Int> occupiedCells = new List<Vector3Int>();
                
                for (int x = 0; x < gridDimensions.x; x++)
                {
                    for (int z = 0; z < gridDimensions.z; z++)
                    {
                        occupiedCells.Add(new Vector3Int(x, 0, z));
                    }
                }
                
                // Ensure at least one cell
                if (occupiedCells.Count == 0)
                {
                    occupiedCells.Add(Vector3Int.zero);
                }
                
                // Set occupied cells using reflection
                occupiedCellsFieldInfo.SetValue(this, occupiedCells);
            }
            finally
            {
                // Clean up temporary instance
                if (tempInstance != null)
                {
                    UnityEngine.Object.DestroyImmediate(tempInstance);
                }
            }
        }
        #endif
    }
}