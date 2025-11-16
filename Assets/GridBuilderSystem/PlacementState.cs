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
            
            // Calculate placement position - use the preview center which accounts for multi-cell objects and rotation
            Vector3 placementPosition = geometry.PreviewCenter;
            
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
            
            // Use the current container's grid for coordinate calculations (or first if none)
            Grid referenceGrid = currentContainer != null ? currentContainer.Grid : splineGridContainers[0].Grid;
            if (referenceGrid == null)
                return false;
            
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
                
                // Check for collisions in each container that contains this cell
                foreach (var container in containingContainers)
                {
                    // Convert world position to this container's grid space
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
            
            // Use the current container's grid for coordinate calculations (or first if none)
            Grid referenceGrid = currentContainer != null ? currentContainer.Grid : splineGridContainers[0].Grid;
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
                        
                        // Convert the cell position to this container's grid space
                        Vector3Int containerCellPos = container.Grid.WorldToCell(worldPos);
                        // Calculate the relative offset from the container's grid position
                        Vector3 gridPosWorld = referenceGrid.GetCellCenterWorld(gridPosition);
                        Vector3Int containerGridPos = container.Grid.WorldToCell(gridPosWorld);
                        Vector3Int relativeCell = containerCellPos - containerGridPos;
                        
                        containerCells[container].Add(relativeCell);
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
            // Get rotated cells for this calculation
            List<Vector3Int> rotatedCells = RotateOccupiedCells(occupiedCells, currentRotation);
            
            // Calculate the center of all occupied cells (after rotation)
            // This ensures multi-cell objects are properly centered
            Vector3 centerWorld = Vector3.zero;
            if (rotatedCells.Count > 0)
            {
                foreach (var cell in rotatedCells)
                {
                    Vector3Int worldCellPos = gridPosition + cell;
                    centerWorld += grid.GetCellCenterWorld(worldCellPos);
                }
                centerWorld /= rotatedCells.Count;
            }
            else
            {
                // Fallback to origin cell center if no cells
                centerWorld = grid.GetCellCenterWorld(gridPosition);
            }

            return new PlacementGeometry
            {
                Origin = gridPosition,
                OccupiedCells = occupiedCells,
                PreviewCenter = centerWorld
            };
        }
        
        private List<Vector3Int> RotateOccupiedCells(List<Vector3Int> cells, float yRotation)
        {
            // Normalize rotation to 0, 90, 180, 270 degrees
            int rotationSteps = Mathf.RoundToInt(yRotation / 90f) % 4;
            if (rotationSteps < 0) rotationSteps += 4;
            
            if (rotationSteps == 0)
                return new List<Vector3Int>(cells);
            
            if (cells == null || cells.Count == 0)
                return new List<Vector3Int>(cells);
            
            // Find bounding box to calculate center
            int minX = int.MaxValue, minY = int.MaxValue, minZ = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue, maxZ = int.MinValue;
            
            foreach (var cell in cells)
            {
                minX = Mathf.Min(minX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                minZ = Mathf.Min(minZ, cell.z);
                maxX = Mathf.Max(maxX, cell.x);
                maxY = Mathf.Max(maxY, cell.y);
                maxZ = Mathf.Max(maxZ, cell.z);
            }
            
            // Calculate center of bounding box (as float for accuracy)
            // Then round to nearest integer to use as pivot point
            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;
            float centerZ = (minZ + maxZ) * 0.5f;
            
            Vector3Int pivot = new Vector3Int(
                Mathf.RoundToInt(centerX),
                Mathf.RoundToInt(centerY),
                Mathf.RoundToInt(centerZ)
            );
            
            List<Vector3Int> rotatedCells = new List<Vector3Int>();
            
            foreach (var cell in cells)
            {
                // Translate to origin relative to pivot
                Vector3Int relative = cell - pivot;
                
                // Apply 90-degree rotations counter-clockwise: (x, z) -> (-z, x)
                int rotatedX = relative.x;
                int rotatedZ = relative.z;
                
                for (int i = 0; i < rotationSteps; i++)
                {
                    int temp = rotatedX;
                    rotatedX = -rotatedZ;
                    rotatedZ = temp;
                }
                
                // Translate back relative to pivot
                Vector3Int rotated = new Vector3Int(rotatedX, relative.y, rotatedZ) + pivot;
                rotatedCells.Add(rotated);
            }
            
            return rotatedCells;
        }
    }

}