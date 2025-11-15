using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.VisualScripting.Member;

namespace GridBuilder.Core
{
    public class PlacementState : IBuildingState
    {
        private int selectedObjectIndex = -1;
        int ID;
        Grid grid;
        PreviewSystem previewSystem;
        ObjectsDatabaseSO database;
        GridData gridData;
        ObjectPlacer objectPlacer;
        SoundFeedback soundFeedback;
        private struct PlacementGeometry
        {
            public Vector3Int Origin;
            public Vector3Int Size;
            public Vector3 PreviewCenter;
        }

        private List<SplineGridContainer> splineGridContainers;
        private SplineGridContainer currentContainer;

        public PlacementState(int iD,
                            Grid grid,
                            PreviewSystem previewSystem,
                            ObjectsDatabaseSO database,
                            GridData gridData,
                            ObjectPlacer objectPlacer,
                            SoundFeedback soundFeedback,
                            List<SplineGridContainer> splineGridContainers = null)
        {
            ID = iD;
            this.grid = grid;
            this.previewSystem = previewSystem;
            this.database = database;
            this.gridData = gridData;
            this.objectPlacer = objectPlacer;
            this.soundFeedback = soundFeedback;
            this.splineGridContainers = splineGridContainers;
            this.currentContainer = splineGridContainers != null && splineGridContainers.Count > 0 ? splineGridContainers[0] : null;

            selectedObjectIndex = database.objectsData.FindIndex(data => data.ID == ID);
            if (selectedObjectIndex > -1)
            {
                previewSystem.StartShowingPlacementPreview(
                    database.objectsData[selectedObjectIndex].Prefab,
                    database.objectsData[selectedObjectIndex].Size,
                    grid);
            }
            else
                throw new System.Exception($"No object with ID {iD}");

        }

        public void EndState()
        {
            previewSystem.StopShowingPreview();
        }

        public void OnAction(Vector3Int gridPosition, SplineGridContainer container)
        {
            // Update current container
            currentContainer = container;
            if (currentContainer != null)
            {
                grid = currentContainer.Grid;
                gridData = currentContainer.GridData;
            }

            PlacementGeometry geometry;
            bool placementValidity = CheckPlacementValidity(gridPosition, selectedObjectIndex, out geometry);
            if (placementValidity == false)
            {
                soundFeedback.PlaySound(SoundType.wrongPlacement);
                return;
            }
            soundFeedback.PlaySound(SoundType.Place);
            
            // Calculate placement position to match preview exactly
            // Preview calculation in UpdateState: center + centerOffset
            // Preview calculation in MovePreview: position + pivotOffset
            // Final preview position: center + centerOffset + pivotOffset
            // Since centerOffset = (size-1) * cellSize * 0.5 and pivotOffset = -(size-1) * cellSize * 0.5
            // They cancel out, so preview ends up at: grid.GetCellCenterWorld(gridPosition)
            Vector3Int objectSize = geometry.Size;
            Vector3 cellSize = grid.cellSize;
            Vector3 pivotOffset = new Vector3(
                -(objectSize.x - 1) * cellSize.x * 0.5f,
                -(objectSize.y - 1) * cellSize.y * 0.5f,
                -(objectSize.z - 1) * cellSize.z * 0.5f);
            
            // Match preview calculation exactly: previewCenter + pivotOffset
            Vector3 placementPosition = geometry.PreviewCenter + pivotOffset;
            
            int index = objectPlacer.PlaceObject(database.objectsData[selectedObjectIndex].Prefab,
                placementPosition);

            // Add object to all containers that contain parts of it
            if (splineGridContainers != null && splineGridContainers.Count > 0)
            {
                AddObjectToRelevantContainers(geometry.Origin, objectSize, index);
            }
            else
            {
                // Fallback to single container
                gridData.AddObjectAt(geometry.Origin,
                    database.objectsData[selectedObjectIndex].Size,
                    database.objectsData[selectedObjectIndex].ID,
                    index);
            }

            // Update preview position using the same calculation as UpdateState
            // This ensures the preview is correctly positioned immediately after placement
            UpdateState(gridPosition, container);
        }

        private bool CheckPlacementValidity(Vector3Int gridPosition, int selectedObjectIndex, out PlacementGeometry geometry)
        {
            Vector3Int objectSize = database.objectsData[selectedObjectIndex].Size;
            geometry = CalculatePlacementGeometry(gridPosition, objectSize);
            
            // Check across all active containers if available
            if (splineGridContainers != null && splineGridContainers.Count > 0)
            {
                return CanPlaceObjectAcrossContainers(geometry.Origin, objectSize);
            }
            
            // Check if within spline boundary if current container is available
            if (currentContainer != null)
            {
                return currentContainer.CanPlaceObjectAt(geometry.Origin, objectSize);
            }
            
            // Fallback to grid data check only
            return gridData.CanPlaceObejctAt(geometry.Origin, objectSize);
        }
        
        /// <summary>
        /// Checks if an object can be placed across multiple containers.
        /// Each cell must be within at least one container, and no collisions across all containers.
        /// </summary>
        private bool CanPlaceObjectAcrossContainers(Vector3Int gridPosition, Vector3Int objectSize)
        {
            if (splineGridContainers == null || splineGridContainers.Count == 0)
                return false;
            
            // Use the first container's grid for coordinate calculations
            Grid referenceGrid = splineGridContainers[0].Grid;
            if (referenceGrid == null)
                return false;
            
            // Track which containers contain each cell and check for collisions
            Dictionary<Vector3Int, List<SplineGridContainer>> cellContainers = new Dictionary<Vector3Int, List<SplineGridContainer>>();
            
            // Check all cells the object would occupy
            for (int x = 0; x < objectSize.x; x++)
            {
                for (int y = 0; y < objectSize.y; y++)
                {
                    for (int z = 0; z < objectSize.z; z++)
                    {
                        Vector3Int cellPos = gridPosition + new Vector3Int(x, y, z);
                        Vector3 worldPos = referenceGrid.GetCellCenterWorld(cellPos);
                        
                        // Find which containers contain this cell
                        List<SplineGridContainer> containingContainers = new List<SplineGridContainer>();
                        foreach (var container in splineGridContainers)
                        {
                            if (container != null && container.IsPositionWithinBoundary(worldPos))
                            {
                                containingContainers.Add(container);
                            }
                        }
                        
                        // If no container contains this cell, placement is invalid
                        if (containingContainers.Count == 0)
                        {
                            return false;
                        }
                        
                        cellContainers[cellPos] = containingContainers;
                    }
                }
            }
            
            // Check for collisions across all relevant containers
            // We need to check each cell against all containers that contain it
            foreach (var kvp in cellContainers)
            {
                Vector3Int cellPos = kvp.Key;
                Vector3 worldPos = referenceGrid.GetCellCenterWorld(cellPos);
                List<SplineGridContainer> containers = kvp.Value;
                
                // Check if any of the containers that contain this cell already have an object here
                foreach (var container in containers)
                {
                    // Convert to container's grid space
                    Vector3Int containerCellPos = container.Grid.WorldToCell(worldPos);
                    if (container.GridData.HasObjectAt(containerCellPos))
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }

        // Interface-compliant overload
        public void UpdateState(Vector3Int gridPosition)
        {
            // Use current container if available
            UpdateState(gridPosition, currentContainer);
        }
        
        // Extended overload with container
        public void UpdateState(Vector3Int gridPosition, SplineGridContainer container)
        {
            // Update current container
            currentContainer = container;
            if (currentContainer != null)
            {
                grid = currentContainer.Grid;
                gridData = currentContainer.GridData;
            }
            
            PlacementGeometry geometry;
            bool placementValidity = CheckPlacementValidity(gridPosition, selectedObjectIndex, out geometry);

            // Calculate position for preview - needs to account for multi-cell objects
            // The preview system will apply the same offset to both preview and indicator
            Vector3 previewPosition = geometry.PreviewCenter;
            
            previewSystem.UpdatePosition(previewPosition, placementValidity);
        }
        
        // Interface-compliant overload
        public void OnAction(Vector3Int gridPosition)
        {
            // Use current container if available
            OnAction(gridPosition, currentContainer);
        }

        /// <summary>
        /// Adds an object to all containers that contain parts of it
        /// </summary>
        private void AddObjectToRelevantContainers(Vector3Int gridPosition, Vector3Int objectSize, int objectIndex)
        {
            if (splineGridContainers == null || splineGridContainers.Count == 0)
                return;
            
            Grid referenceGrid = splineGridContainers[0].Grid;
            if (referenceGrid == null)
                return;
            
            // Track which containers need to have which cells added
            Dictionary<SplineGridContainer, List<Vector3Int>> containerCells = new Dictionary<SplineGridContainer, List<Vector3Int>>();
            
            // Determine which cells belong to which containers
            for (int x = 0; x < objectSize.x; x++)
            {
                for (int y = 0; y < objectSize.y; y++)
                {
                    for (int z = 0; z < objectSize.z; z++)
                    {
                        Vector3Int cellPos = gridPosition + new Vector3Int(x, y, z);
                        Vector3 worldPos = referenceGrid.GetCellCenterWorld(cellPos);
                        
                        // Find which containers contain this cell
                        foreach (var container in splineGridContainers)
                        {
                            if (container != null && container.IsPositionWithinBoundary(worldPos))
                            {
                                if (!containerCells.ContainsKey(container))
                                {
                                    containerCells[container] = new List<Vector3Int>();
                                }
                                containerCells[container].Add(cellPos);
                            }
                        }
                    }
                }
            }
            
            // Add the object to each relevant container's grid data
            foreach (var kvp in containerCells)
            {
                SplineGridContainer container = kvp.Key;
                List<Vector3Int> cells = kvp.Value;
                
                // For each container, we need to add the object at the minimum cell position
                // that's within that container's grid space
                if (cells.Count > 0)
                {
                    // Find the minimum cell position for this container
                    Vector3Int minCell = cells[0];
                    foreach (var cell in cells)
                    {
                        if (cell.x < minCell.x || (cell.x == minCell.x && cell.z < minCell.z) || 
                            (cell.x == minCell.x && cell.z == minCell.z && cell.y < minCell.y))
                        {
                            minCell = cell;
                        }
                    }
                    
                    // Calculate the size relative to this container's grid
                    // We need to find the bounding box of cells in this container
                    Vector3Int maxCell = cells[0];
                    foreach (var cell in cells)
                    {
                        if (cell.x > maxCell.x || (cell.x == maxCell.x && cell.z > maxCell.z) || 
                            (cell.x == maxCell.x && cell.z == maxCell.z && cell.y > maxCell.y))
                        {
                            maxCell = cell;
                        }
                    }
                    
                    Vector3Int containerObjectSize = maxCell - minCell + Vector3Int.one;
                    
                    // Convert world cell position to container's grid space
                    Vector3 worldMinPos = referenceGrid.GetCellCenterWorld(minCell);
                    Vector3Int containerGridPos = container.Grid.WorldToCell(worldMinPos);
                    
                    container.GridData.AddObjectAt(containerGridPos,
                        containerObjectSize,
                        database.objectsData[selectedObjectIndex].ID,
                        objectIndex);
                }
            }
        }
        
        private PlacementGeometry CalculatePlacementGeometry(Vector3Int gridPosition, Vector3Int objectSize)
        {
            Vector3 cellSize = grid.cellSize;
            Vector3 pointerCellCenter = grid.GetCellCenterWorld(gridPosition);
            Vector3 centerOffset = new Vector3(
                (objectSize.x - 1) * cellSize.x * 0.5f,
                (objectSize.y - 1) * cellSize.y * 0.5f,
                (objectSize.z - 1) * cellSize.z * 0.5f);

            Vector3 previewCenter = pointerCellCenter + centerOffset;

            Vector3 halfSizeWorld = Vector3.Scale(new Vector3(objectSize.x, objectSize.y, objectSize.z), cellSize) * 0.5f;
            Vector3 minCellCenterWorld = previewCenter - halfSizeWorld + new Vector3(
                cellSize.x * 0.5f,
                cellSize.y * 0.5f,
                cellSize.z * 0.5f);

            Vector3Int origin = grid.WorldToCell(minCellCenterWorld);

            return new PlacementGeometry
            {
                Origin = origin,
                Size = objectSize,
                PreviewCenter = previewCenter
            };
        }
    }

}