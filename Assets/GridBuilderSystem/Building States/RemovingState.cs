using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridBuilder.Core
{
    public class RemovingState : IBuildingState
    {
        private int gameObjectIndex = -1;
        Grid grid;
        PreviewSystem previewSystem;
        GridData gridData;
        ObjectPlacer objectPlacer;
        SoundFeedback soundFeedback;
        private List<SplineGridContainer> splineGridContainers;
        private SplineGridContainer currentContainer;

        public RemovingState(Grid grid,
                            PreviewSystem previewSystem,
                            GridData gridData,
                            ObjectPlacer objectPlacer,
                            SoundFeedback soundFeedback,
                            List<SplineGridContainer> splineGridContainers = null)
        {
            this.grid = grid;
            this.previewSystem = previewSystem;
            this.gridData = gridData;
            this.objectPlacer = objectPlacer;
            this.soundFeedback = soundFeedback;
            this.splineGridContainers = splineGridContainers;
            this.currentContainer = splineGridContainers != null && splineGridContainers.Count > 0 ? splineGridContainers[0] : null;
            previewSystem.StartShowingRemovePreview(grid);
        }

        public void EndState()
        {
            previewSystem.StopShowingPreview();
        }

        // Interface-compliant overload
        public void OnAction(Vector3Int gridPosition)
        {
            // Use current container if available
            OnAction(gridPosition, currentContainer);
        }

        // Extended overload with container
        public void OnAction(Vector3Int gridPosition, SplineGridContainer container)
        {
            // Update current container
            currentContainer = container;
            if (currentContainer != null)
            {
                grid = currentContainer.Grid;
                gridData = currentContainer.GridData;
            }

            GridData selectedData = null;
            
            // Check across all containers if available
            if (splineGridContainers != null && splineGridContainers.Count > 0)
            {
                selectedData = FindGridDataAtPosition(gridPosition);
            }
            else
            {
                // Fallback to single container
                if (!gridData.CanPlaceObejctAt(gridPosition, new List<Vector3Int> { Vector3Int.zero }))
                {
                    selectedData = gridData;
                }
            }

            if (selectedData == null)
            {
                //sound
                soundFeedback.PlaySound(SoundType.WrongPlacement);
            }
            else
            {
                soundFeedback.PlaySound(SoundType.Remove);
                gameObjectIndex = selectedData.GetRepresentationIndex(gridPosition);
                if (gameObjectIndex == -1)
                    return;
                selectedData.RemoveObjectAt(gridPosition);
                objectPlacer.RemoveObjectAt(gameObjectIndex);
            }
            
            Vector3 cellPosition = grid.CellToWorld(gridPosition);
            previewSystem.UpdatePosition(cellPosition, CheckIfSelectionIsValid(gridPosition));
        }

        private GridData FindGridDataAtPosition(Vector3Int gridPosition)
        {
            if (currentContainer == null || currentContainer.Grid == null)
                return null;
            
            // Convert grid position to world position using current container's grid
            Vector3 worldPos = currentContainer.Grid.GetCellCenterWorld(gridPosition);
            
            // Check all containers to find which one has an object at this position
            foreach (var container in splineGridContainers)
            {
                if (container != null && container.IsPositionWithinBoundary(worldPos))
                {
                    Vector3Int containerCellPos = container.Grid.WorldToCell(worldPos);
                    if (!container.GridData.CanPlaceObejctAt(containerCellPos, new List<Vector3Int> { Vector3Int.zero }))
                    {
                        return container.GridData;
                    }
                }
            }
            
            return null;
        }

        private bool CheckIfSelectionIsValid(Vector3Int gridPosition)
        {
            // Check across all containers if available
            if (splineGridContainers != null && splineGridContainers.Count > 0)
            {
                return FindGridDataAtPosition(gridPosition) != null;
            }
            
            // Fallback to single container
            return !gridData.CanPlaceObejctAt(gridPosition, new List<Vector3Int> { Vector3Int.zero });
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
            
            bool validity = CheckIfSelectionIsValid(gridPosition);
            previewSystem.UpdatePosition(grid.GetCellCenterWorld(gridPosition), validity);
        }
    }
}