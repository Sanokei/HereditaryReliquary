using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GridBuilder.Core
{
    public class PlacementSystem : BaseBuildingSystem
    {
        [SerializeField] AudioClip correctPlacementClip;

        private ObjectsDatabaseSO activeDatabase;
        
        protected override void HandleSystemSpecificInput()
        {
            // Handle rotation with R key
            if (Input.GetKeyDown(KeyCode.R))
            {
                RotateObject(buildingState as PlacementState);
            }
        }
        
        protected override void UpdateStateForContainer(Vector3Int gridPosition, SplineGridContainer container)
        {
            (buildingState as PlacementState).UpdateState(gridPosition, container);
        }
        
        private void RotateObject(PlacementState placementState)
        {
            // Rotate 90 degrees on Y-axis
            float currentRotation = preview.GetRotation();
            float newRotation = (currentRotation + 90f) % 360f;
            preview.SetRotation(newRotation);
            placementState.SetRotation(newRotation);
            
            // Update preview position to reflect rotation
            if (activeGridContainers != null && activeGridContainers.Count > 0)
            {
                Vector3 mousePosition = GetSelectedMapPosition();
                SplineGridContainer currentContainer = GetContainerAtPosition(mousePosition);
                if (currentContainer != null && currentContainer.Grid != null)
                {
                    Vector3Int gridPosition = currentContainer.Grid.WorldToCell(mousePosition);
                    placementState.UpdateState(gridPosition, currentContainer);
                }
            }
        }

        public void StartPlacement(ObjectsDatabaseSO targetDatabase, int ID)
        {
            StopPlacement();

            activeDatabase = targetDatabase;
            activeGridContainers.Clear();
            
            // Get all spline grid containers from manager
            if (manager == null)
            {
                Debug.LogError("BuildingSystemManager not found. Cannot start placement.");
                return;
            }
            
            List<SplineGridContainer> allContainers = manager.SplineGridContainers;
            List<SplineGridContainer> containersToHide = new List<SplineGridContainer>();
            
            // Find all spline grid containers that match the database's layer mask
            foreach (var container in allContainers)
            {
                if (container != null)
                {
                    // Check if layer masks match
                    if ((container.PlacementLayerMask.value & targetDatabase.placementLayermask.value) != 0)
                    {
                        activeGridContainers.Add(container);
                    }
                    else
                    {
                        containersToHide.Add(container);
                    }
                }
            }
            
            if (activeGridContainers.Count == 0)
            {
                Debug.LogError($"No spline grid container found for database with layer mask \"{LayerMask.LayerToName(targetDatabase.placementLayermask.value)}\".\n Does the database have a layer mask set?");
                return;
            }
            
            // Show active grids, hide others using event system
            RequestShowGrids(activeGridContainers);
            RequestHideGrids(containersToHide);
            
            // Use the first active container's grid for initial setup
            // The actual container used will be determined by mouse position
            SplineGridContainer firstContainer = activeGridContainers[0];
            buildingState = new PlacementState(ID,
                                            firstContainer.Grid,
                                            preview,
                                            activeDatabase,
                                            firstContainer.GridData,
                                            objectPlacer,
                                            soundFeedback,
                                            activeGridContainers);
            OnClicked += OnAction;
            OnExit += StopPlacement;
        }

        public void StopPlacement()
        {
            OnClicked -= OnAction;
            OnExit -= StopPlacement;
            StopBuilding();
            activeDatabase = null;
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

            // If it's a PlacementState, use the extended method with container
            if (buildingState is PlacementState placementState)
            {
                placementState.OnAction(gridPosition, currentContainer);
            }
            else
            {
                buildingState.OnAction(gridPosition);
            }
        }
    }
}