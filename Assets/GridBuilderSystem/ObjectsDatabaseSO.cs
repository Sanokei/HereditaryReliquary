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
    
    [CreateAssetMenu]
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
    }
}