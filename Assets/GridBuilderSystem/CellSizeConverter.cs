using System.Collections.Generic;
using UnityEngine;

namespace GridBuilder.Core
{
    /// <summary>
    /// Utility class for converting occupied cells between different cell sizes
    /// </summary>
    public static class CellSizeConverter
    {
        /// <summary>
        /// Converts occupied cells from source cell size to target cell size
        /// Formula: targetCell = (sourceCell * sourceCellSize) / targetCellSize
        /// </summary>
        /// <param name="cells">List of cells in source cell size space</param>
        /// <param name="sourceCellSize">The cell size the cells are currently in</param>
        /// <param name="targetCellSize">The cell size to convert to</param>
        /// <returns>List of cells in target cell size space</returns>
        public static List<Vector3Int> ConvertOccupiedCells(List<Vector3Int> cells, int sourceCellSize, int targetCellSize)
        {
            if (cells == null || cells.Count == 0)
                return new List<Vector3Int>(cells);
            
            if (sourceCellSize == targetCellSize)
                return new List<Vector3Int>(cells);
            
            List<Vector3Int> convertedCells = new List<Vector3Int>();
            HashSet<Vector3Int> uniqueCells = new HashSet<Vector3Int>();
            
            foreach (var cell in cells)
            {
                // Convert each cell coordinate: targetCell = (sourceCell * sourceCellSize) / targetCellSize
                // Use rounding to handle fractional results
                int convertedX = Mathf.RoundToInt((cell.x * sourceCellSize) / (float)targetCellSize);
                int convertedY = Mathf.RoundToInt((cell.y * sourceCellSize) / (float)targetCellSize);
                int convertedZ = Mathf.RoundToInt((cell.z * sourceCellSize) / (float)targetCellSize);
                
                Vector3Int convertedCell = new Vector3Int(convertedX, convertedY, convertedZ);
                
                // Only add unique cells
                if (uniqueCells.Add(convertedCell))
                {
                    convertedCells.Add(convertedCell);
                }
            }
            
            return convertedCells;
        }
    }
}

