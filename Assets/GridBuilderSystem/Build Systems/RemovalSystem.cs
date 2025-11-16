using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GridBuilder.Core
{
    public class RemovalSystem : BaseBuildingSystem
    {
        protected override void UpdateStateForContainer(Vector3Int gridPosition, SplineGridContainer container)
        {
            (buildingState as RemovingState).UpdateState(gridPosition, container);
        }

        public void StartRemoving()
        {
            StopRemoving();
            
            // Get all spline grid containers from manager
            if (manager == null)
            {
                Debug.LogError("BuildingSystemManager not found. Cannot start removal.");
                return;
            }
            
            List<SplineGridContainer> allContainers = manager.SplineGridContainers;
            
            // Use all available grid containers for removal state
            if (allContainers.Count > 0)
            {
                activeGridContainers.Clear();
                activeGridContainers.AddRange(allContainers.Where(c => c != null));
                
                // Show all grids for removal using event system
                RequestShowGrids(activeGridContainers);
                
                // Use the first container's grid for initial setup
                SplineGridContainer firstContainer = activeGridContainers[0];
                buildingState = new RemovingState(firstContainer.Grid,
                                                preview,
                                                firstContainer.GridData,
                                                objectPlacer,
                                                soundFeedback,
                                                activeGridContainers);
            }
            else
            {
                Debug.LogError("No spline grid containers available for removal");
                return;
            }
            
            OnClicked += OnAction;
            OnExit += StopRemoving;
        }

        public void StopRemoving()
        {
            OnClicked -= OnAction;
            OnExit -= StopRemoving;
            StopBuilding();
        }

        protected override void OnAction()
        {
            if (IsPointerOverUI())
            {
                return;
            }
            
            if (activeGridContainers == null || activeGridContainers.Count == 0)
                return;
                
            Vector3 mousePosition = GetSelectedMapPosition();
            
            // Find which active grid container the mouse is over
            SplineGridContainer currentContainer = GetContainerAtPosition(mousePosition);
            if (currentContainer == null || currentContainer.Grid == null)
                return;
                
            Vector3Int gridPosition = currentContainer.Grid.WorldToCell(mousePosition);

            // If it's a RemovingState, use the extended method with container
            if (buildingState is RemovingState removingState)
            {
                removingState.OnAction(gridPosition, currentContainer);
            }
            else
            {
                buildingState.OnAction(gridPosition);
            }
        }
    }
}

