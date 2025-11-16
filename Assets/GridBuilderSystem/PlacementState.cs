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
            public List<Vector3Int> OccupiedCells;
            public Vector3 PreviewCenter;
        }

        private List<SplineGridContainer> splineGridContainers;
        private SplineGridContainer currentContainer;
        private float currentRotation = 0f;
        private PreviewSystem previewSystemRef;

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
            this.previewSystemRef = previewSystem;

            selectedObjectIndex = database.objectsData.FindIndex(data => data.ID == ID);
            if (selectedObjectIndex > -1)
            {
                previewSystem.StartShowingPlacementPreview(
                    database.objectsData[selectedObjectIndex].Prefab,
                    database.objectsData[selectedObjectIndex].OccupiedCells,
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
            
            // Calculate placement position - use the grid position directly
            Vector3 placementPosition = grid.GetCellCenterWorld(geometry.Origin);
            
            Quaternion rotation = Quaternion.Euler(0, currentRotation, 0);
            int index = objectPlacer.PlaceObject(database.objectsData[selectedObjectIndex].Prefab,
                placementPosition, rotation);

            // Get rotated occupied cells
            List<Vector3Int> rotatedCells = RotateOccupiedCells(geometry.OccupiedCells, currentRotation);
            
            // Add object to all containers that contain parts of it
            if (splineGridContainers != null && splineGridContainers.Count > 0)
            {
                AddObjectToRelevantContainers(geometry.Origin, rotatedCells, index);
            }
            else
            {
                // Fallback to single container
                gridData.AddObjectAt(geometry.Origin,
                    rotatedCells,
                    database.objectsData[selectedObjectIndex].ID,
                    index);
            }

            // Update preview position using the same calculation as UpdateState
            // This ensures the preview is correctly positioned immediately after placement
            UpdateState(gridPosition, container);
        }

        private bool CheckPlacementValidity(Vector3Int gridPosition, int selectedObjectIndex, out PlacementGeometry geometry)
        {
            List<Vector3Int> occupiedCells = database.objectsData[selectedObjectIndex].OccupiedCells;
            geometry = CalculatePlacementGeometry(gridPosition, occupiedCells);
            
            // Get rotated cells for validity check
            List<Vector3Int> rotatedCells = RotateOccupiedCells(occupiedCells, currentRotation);
            
            // Check across all active containers if available
            if (splineGridContainers != null && splineGridContainers.Count > 0)
            {
                return CanPlaceObjectAcrossContainers(geometry.Origin, rotatedCells);
            }
            
            // Check if within spline boundary if current container is available
            if (currentContainer != null)
            {
                return currentContainer.CanPlaceObjectAt(geometry.Origin, rotatedCells);
            }
            
            // Fallback to grid data check only
            return gridData.CanPlaceObejctAt(geometry.Origin, rotatedCells);
        }
        
        /// <summary>
        /// Checks if an object can be placed across multiple containers.
        /// Each cell must be within at least one container, and no collisions across all containers.
        /// </summary>
        private bool CanPlaceObjectAcrossContainers(Vector3Int gridPosition, List<Vector3Int> occupiedCells)
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
            foreach (var cell in occupiedCells)
            {
                Vector3Int cellPos = gridPosition + cell;
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
        private void AddObjectToRelevantContainers(Vector3Int gridPosition, List<Vector3Int> occupiedCells, int objectIndex)
        {
            if (splineGridContainers == null || splineGridContainers.Count == 0)
                return;
            
            Grid referenceGrid = splineGridContainers[0].Grid;
            if (referenceGrid == null)
                return;
            
            // Track which containers need to have which cells added
            Dictionary<SplineGridContainer, List<Vector3Int>> containerCells = new Dictionary<SplineGridContainer, List<Vector3Int>>();
            
            // Determine which cells belong to which containers
            foreach (var cell in occupiedCells)
            {
                Vector3Int cellPos = gridPosition + cell;
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
                        // Store the cell relative to gridPosition for this container
                        containerCells[container].Add(cell);
                    }
                }
            }
            
            // Add the object to each relevant container's grid data
            foreach (var kvp in containerCells)
            {
                SplineGridContainer container = kvp.Key;
                List<Vector3Int> cells = kvp.Value;
                
                if (cells.Count > 0)
                {
                    // Convert grid position to container's grid space
                    Vector3 worldPos = referenceGrid.GetCellCenterWorld(gridPosition);
                    Vector3Int containerGridPos = container.Grid.WorldToCell(worldPos);
                    
                    container.GridData.AddObjectAt(containerGridPos,
                        cells,
                        database.objectsData[selectedObjectIndex].ID,
                        objectIndex);
                }
            }
        }
        
        public void SetRotation(float rotation)
        {
            currentRotation = rotation;
            if (previewSystemRef != null)
            {
                previewSystemRef.SetRotation(rotation);
            }
        }
        
        public float GetRotation()
        {
            return currentRotation;
        }
        
        private PlacementGeometry CalculatePlacementGeometry(Vector3Int gridPosition, List<Vector3Int> occupiedCells)
        {
            Vector3 pointerCellCenter = grid.GetCellCenterWorld(gridPosition);

            return new PlacementGeometry
            {
                Origin = gridPosition,
                OccupiedCells = occupiedCells,
                PreviewCenter = pointerCellCenter
            };
        }
        
        private List<Vector3Int> RotateOccupiedCells(List<Vector3Int> cells, float yRotation)
        {
            // Normalize rotation to 0, 90, 180, 270 degrees
            int rotationSteps = Mathf.RoundToInt(yRotation / 90f) % 4;
            if (rotationSteps < 0) rotationSteps += 4;
            
            if (rotationSteps == 0)
                return new List<Vector3Int>(cells);
            
            // Calculate center of occupied cells
            Vector3 center = Vector3.zero;
            foreach (var cell in cells)
            {
                center += new Vector3(cell.x, cell.y, cell.z);
            }
            center /= cells.Count;
            
            List<Vector3Int> rotatedCells = new List<Vector3Int>();
            
            foreach (var cell in cells)
            {
                // Translate to origin
                Vector3 relative = new Vector3(cell.x, cell.y, cell.z) - center;
                
                // Apply 90-degree rotations
                for (int i = 0; i < rotationSteps; i++)
                {
                    float temp = relative.x;
                    relative.x = -relative.z;
                    relative.z = temp;
                }
                
                // Translate back and round to int
                Vector3 rotated = relative + center;
                rotatedCells.Add(new Vector3Int(
                    Mathf.RoundToInt(rotated.x),
                    Mathf.RoundToInt(rotated.y),
                    Mathf.RoundToInt(rotated.z)
                ));
            }
            
            return rotatedCells;
        }
    }

}