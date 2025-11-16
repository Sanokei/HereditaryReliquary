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
        private SplineGridContainer previousContainer;
        private Grid previousGrid;
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
            this.previousContainer = null;
            this.previousGrid = null;
            this.previewSystemRef = previewSystem;

            selectedObjectIndex = database.objectsData.FindIndex(data => data.ID == ID);
            if (selectedObjectIndex > -1)
            {
                List<Vector3Int> databaseOccupiedCells = database.objectsData[selectedObjectIndex].OccupiedCells;
                int databaseCellSize = database.CellSize;
                int containerCellSize = currentContainer != null ? currentContainer.GridCellSize : 1;
                CellSizePlacementMode placementMode = currentContainer != null ? currentContainer.PlacementMode : CellSizePlacementMode.ConvertCells;
                float scaleFactor = 1f;
                List<Vector3Int> cellsForPreview = databaseOccupiedCells;
                
                // Handle placement mode (from container, not database)
                if (placementMode == CellSizePlacementMode.ScaleObject)
                {
                    // Scale mode: scale the object, use original cells
                    scaleFactor = (float)containerCellSize / databaseCellSize;
                    cellsForPreview = new List<Vector3Int>(databaseOccupiedCells);
                }
                else
                {
                    // Convert mode: convert cells, no scaling
                    cellsForPreview = CellSizeConverter.ConvertOccupiedCells(
                        databaseOccupiedCells,
                        databaseCellSize,
                        containerCellSize
                    );
                }
                
                previewSystem.StartShowingPlacementPreview(
                    database.objectsData[selectedObjectIndex].Prefab,
                    cellsForPreview,
                    grid,
                    scaleFactor);
                
                // Initialize previous references after first preview creation
                previousContainer = currentContainer;
                previousGrid = grid;
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
                soundFeedback.PlaySound(SoundType.WrongPlacement);
                return;
            }
            soundFeedback.PlaySound(SoundType.Place);
            
            // Calculate placement position - use the preview center which accounts for multi-cell objects and rotation
            Vector3 placementPosition = geometry.PreviewCenter;
            
            // Keep object on ground level
            float groundY = 0f;
            if (grid != null)
            {
                groundY = grid.transform.position.y;
            }
            
            Quaternion rotation = Quaternion.Euler(0, currentRotation, 0);
            
            // Apply scaling if in ScaleObject mode
            GameObject prefab = database.objectsData[selectedObjectIndex].Prefab;
            int databaseCellSize = database.CellSize;
            int containerCellSize = currentContainer != null ? currentContainer.GridCellSize : 1;
            CellSizePlacementMode placementMode = currentContainer != null ? currentContainer.PlacementMode : CellSizePlacementMode.ConvertCells;
            Vector3 scale = prefab.transform.localScale;
            
            if (placementMode == CellSizePlacementMode.ScaleObject)
            {
                float scaleFactor = (float)containerCellSize / databaseCellSize;
                scale = prefab.transform.localScale * scaleFactor;
            }
            
            // Calculate the object's bounds to ensure it doesn't clip through the floor
            // Get bounds from prefab's mesh filters without instantiating
            MeshFilter[] meshFilters = prefab.GetComponentsInChildren<MeshFilter>();
            float bottomOffset = 0f;
            
            if (meshFilters.Length > 0)
            {
                // Calculate combined bounds in local space (relative to prefab root)
                Bounds combinedLocalBounds = new Bounds();
                bool boundsInitialized = false;
                
                foreach (MeshFilter meshFilter in meshFilters)
                {
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        // Get mesh bounds in mesh local space
                        Bounds meshBounds = meshFilter.sharedMesh.bounds;
                        
                        // Transform mesh bounds to prefab root local space
                        // Account for meshFilter's local position, rotation, and scale
                        Transform meshTransform = meshFilter.transform;
                        Vector3 localPos = meshTransform.localPosition;
                        Quaternion localRot = meshTransform.localRotation;
                        Vector3 localScale = meshTransform.localScale;
                        
                        // Calculate the 8 corners of the mesh bounds
                        Vector3[] meshCorners = new Vector3[8];
                        Vector3 meshCenter = meshBounds.center;
                        Vector3 meshExtents = meshBounds.extents;
                        
                        meshCorners[0] = meshCenter + new Vector3(-meshExtents.x, -meshExtents.y, -meshExtents.z);
                        meshCorners[1] = meshCenter + new Vector3(meshExtents.x, -meshExtents.y, -meshExtents.z);
                        meshCorners[2] = meshCenter + new Vector3(-meshExtents.x, -meshExtents.y, meshExtents.z);
                        meshCorners[3] = meshCenter + new Vector3(meshExtents.x, -meshExtents.y, meshExtents.z);
                        meshCorners[4] = meshCenter + new Vector3(-meshExtents.x, meshExtents.y, -meshExtents.z);
                        meshCorners[5] = meshCenter + new Vector3(meshExtents.x, meshExtents.y, -meshExtents.z);
                        meshCorners[6] = meshCenter + new Vector3(-meshExtents.x, meshExtents.y, meshExtents.z);
                        meshCorners[7] = meshCenter + new Vector3(meshExtents.x, meshExtents.y, meshExtents.z);
                        
                        // Transform corners to prefab root local space
                        for (int i = 0; i < meshCorners.Length; i++)
                        {
                            // Scale, rotate, then translate
                            meshCorners[i] = Vector3.Scale(meshCorners[i], localScale);
                            meshCorners[i] = localRot * meshCorners[i];
                            meshCorners[i] += localPos;
                        }
                        
                        // Create bounds from transformed corners
                        if (!boundsInitialized)
                        {
                            combinedLocalBounds = new Bounds(meshCorners[0], Vector3.zero);
                            boundsInitialized = true;
                        }
                        
                        foreach (Vector3 corner in meshCorners)
                        {
                            combinedLocalBounds.Encapsulate(corner);
                        }
                    }
                }
                
                if (boundsInitialized)
                {
                    // Transform the local bounds by the rotation and scale that will be applied at root
                    // Apply root scale to bounds
                    Vector3 scaledCenter = Vector3.Scale(combinedLocalBounds.center, scale);
                    Vector3 scaledSize = Vector3.Scale(combinedLocalBounds.size, scale);
                    
                    // Calculate the 8 corners of the bounding box in local space
                    Vector3[] corners = new Vector3[8];
                    Vector3 extents = scaledSize * 0.5f;
                    
                    corners[0] = scaledCenter + new Vector3(-extents.x, -extents.y, -extents.z);
                    corners[1] = scaledCenter + new Vector3(extents.x, -extents.y, -extents.z);
                    corners[2] = scaledCenter + new Vector3(-extents.x, -extents.y, extents.z);
                    corners[3] = scaledCenter + new Vector3(extents.x, -extents.y, extents.z);
                    corners[4] = scaledCenter + new Vector3(-extents.x, extents.y, -extents.z);
                    corners[5] = scaledCenter + new Vector3(extents.x, extents.y, -extents.z);
                    corners[6] = scaledCenter + new Vector3(-extents.x, extents.y, extents.z);
                    corners[7] = scaledCenter + new Vector3(extents.x, extents.y, extents.z);
                    
                    // Rotate corners and center
                    Vector3 rotatedCenter = rotation * scaledCenter;
                    for (int i = 0; i < corners.Length; i++)
                    {
                        corners[i] = rotation * corners[i];
                    }
                    
                    // Find the minimum Y after rotation
                    float minY = float.MaxValue;
                    foreach (Vector3 corner in corners)
                    {
                        minY = Mathf.Min(minY, corner.y);
                    }
                    
                    // Adjust placement position so:
                    // 1. Bounds center aligns with target position (X, Z)
                    // 2. Bounds bottom aligns with ground (Y)
                    placementPosition = new Vector3(
                        placementPosition.x - rotatedCenter.x,
                        groundY - minY,
                        placementPosition.z - rotatedCenter.z);
                }
            }
            else
            {
                // Fallback if no mesh filters found - just align Y with ground
                placementPosition.y = groundY;
            }
            
            int index = objectPlacer.PlaceObject(prefab, placementPosition, rotation, scale);

            // Get rotated occupied cells (already converted to container cell size in CheckPlacementValidity)
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
            // Get occupied cells from database (in database cell size space)
            List<Vector3Int> databaseOccupiedCells = database.objectsData[selectedObjectIndex].OccupiedCells;
            
            int databaseCellSize = database.CellSize;
            int containerCellSize = currentContainer != null ? currentContainer.GridCellSize : 1;
            CellSizePlacementMode placementMode = currentContainer != null ? currentContainer.PlacementMode : CellSizePlacementMode.ConvertCells;
            List<Vector3Int> occupiedCells;
            
            // Handle placement mode (from container, not database)
            if (placementMode == CellSizePlacementMode.ScaleObject)
            {
                // Scale mode: use original cells without conversion
                occupiedCells = new List<Vector3Int>(databaseOccupiedCells);
            }
            else
            {
                // Convert mode: convert cells from database cell size to container cell size
                occupiedCells = CellSizeConverter.ConvertOccupiedCells(
                    databaseOccupiedCells, 
                    databaseCellSize, 
                    containerCellSize
                );
            }
            
            geometry = CalculatePlacementGeometry(gridPosition, occupiedCells);
            
            // Get rotated cells for validity check
            List<Vector3Int> rotatedCells = RotateOccupiedCells(occupiedCells, currentRotation);
            
            // First, do standard placement checks (collision, boundaries, etc.)
            bool standardCheckPassed = false;
            if (splineGridContainers != null && splineGridContainers.Count > 0)
            {
                standardCheckPassed = CanPlaceObjectAcrossContainers(geometry.Origin, rotatedCells);
            }
            else if (currentContainer != null)
            {
                standardCheckPassed = currentContainer.CanPlaceObjectAt(geometry.Origin, rotatedCells);
            }
            else
            {
                standardCheckPassed = gridData.CanPlaceObejctAt(geometry.Origin, rotatedCells);
            }

            // If standard checks fail, no need to check validators
            if (!standardCheckPassed)
                return false;

            // Now check custom validators if any are defined
            ObjectData objectData = database.objectsData[selectedObjectIndex];
            if (objectData.placementValidators != null && objectData.placementValidators.Count > 0)
            {
                // Create validation context
                PlacementValidationContext context = new PlacementValidationContext
                {
                    gridPosition = geometry.Origin,
                    occupiedCells = rotatedCells,
                    activeContainers = splineGridContainers != null && splineGridContainers.Count > 0 
                        ? splineGridContainers 
                        : (currentContainer != null ? new List<SplineGridContainer> { currentContainer } : new List<SplineGridContainer>()),
                    currentContainer = currentContainer,
                    database = database,
                    objectID = objectData.ID,
                    rotation = currentRotation,
                    referenceGrid = grid
                };

                // Check all validators - all must pass
                foreach (var validator in objectData.placementValidators)
                {
                    if (validator == null)
                        continue;

                    if (!validator.ValidatePlacement(context))
                    {
                        // Validator failed
                        return false;
                    }
                }
            }

            // All checks passed
            return true;
        }
        
        /// <summary>
        /// Checks if an object can be placed across multiple containers.
        /// Each cell must be within at least one container, and no collisions across all containers.
        /// Additionally, if an object spans multiple containers, those containers' boundaries must actually intersect.
        /// </summary>
        private bool CanPlaceObjectAcrossContainers(Vector3Int gridPosition, List<Vector3Int> occupiedCells)
        {
            if (splineGridContainers == null || splineGridContainers.Count == 0)
                return false;
            
            // Use the current container's grid for coordinate calculations (or first if none)
            Grid referenceGrid = currentContainer != null ? currentContainer.Grid : splineGridContainers[0].Grid;
            if (referenceGrid == null)
                return false;
            
            // Track which containers are involved
            HashSet<SplineGridContainer> involvedContainers = new HashSet<SplineGridContainer>();
            
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
                        involvedContainers.Add(container);
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
            
            // If the object spans multiple containers, verify their boundaries actually intersect
            if (involvedContainers.Count > 1)
            {
                List<SplineGridContainer> containerList = new List<SplineGridContainer>(involvedContainers);
                
                // Check that all involved containers have intersecting boundaries
                for (int i = 0; i < containerList.Count; i++)
                {
                    for (int j = i + 1; j < containerList.Count; j++)
                    {
                        if (!containerList[i].BoundariesIntersect(containerList[j]))
                        {
                            // Containers are not touching, block placement
                            return false;
                        }
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
                
                // Only recreate preview if container or grid actually changed
                bool containerChanged = currentContainer != previousContainer;
                bool gridChanged = grid != previousGrid;
                
                if (containerChanged || gridChanged)
                {
                    // Update preview with converted cells if container/grid changed
                    if (selectedObjectIndex >= 0 && selectedObjectIndex < database.objectsData.Count)
                    {
                        List<Vector3Int> databaseOccupiedCells = database.objectsData[selectedObjectIndex].OccupiedCells;
                        int databaseCellSize = database.CellSize;
                        int containerCellSize = currentContainer.GridCellSize;
                        CellSizePlacementMode placementMode = currentContainer.PlacementMode;
                        float scaleFactor = 1f;
                        List<Vector3Int> cellsForPreview;
                        
                        // Handle placement mode (from container, not database)
                        if (placementMode == CellSizePlacementMode.ScaleObject)
                        {
                            // Scale mode: scale the object, use original cells
                            scaleFactor = (float)containerCellSize / databaseCellSize;
                            cellsForPreview = new List<Vector3Int>(databaseOccupiedCells);
                        }
                        else
                        {
                            // Convert mode: convert cells, no scaling
                            cellsForPreview = CellSizeConverter.ConvertOccupiedCells(
                                databaseOccupiedCells,
                                databaseCellSize,
                                containerCellSize
                            );
                        }
                        
                        // Update preview system with new grid and converted cells
                        previewSystem.StartShowingPlacementPreview(
                            database.objectsData[selectedObjectIndex].Prefab,
                            cellsForPreview,
                            grid,
                            scaleFactor);
                        previewSystem.SetRotation(currentRotation);
                    }
                    
                    previousContainer = currentContainer;
                    previousGrid = grid;
                }
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
            
            // PreviewCenter should be the world position of the grid cell under the mouse cursor
            // This ensures the object's visual center aligns with the mouse cursor
            // The object's pivot will be adjusted to align its bounds center with this position
            Vector3 centerWorld = grid.GetCellCenterWorld(gridPosition);

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