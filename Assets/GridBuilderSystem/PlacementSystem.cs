using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GridBuilder.Core
{
    public class PlacementSystem : MonoBehaviour
    {
        [SerializeField] List<SplineGridContainer> splineGridContainers = new List<SplineGridContainer>();
        
        [SerializeField] List<ObjectsDatabaseSO> databases = new List<ObjectsDatabaseSO>();

        [SerializeField] AudioClip correctPlacementClip, wrongPlacementClip;
        [SerializeField] AudioSource source;

        [SerializeField] PreviewSystem preview;

        Vector3Int lastDetectedPosition = Vector3Int.zero;

        [SerializeField] ObjectPlacer objectPlacer;

        IBuildingState buildingState;

        [SerializeField] SoundFeedback soundFeedback;

        [SerializeField] Camera sceneCamera;

        Vector3 lastPosition;

        private List<SplineGridContainer> activeGridContainers = new List<SplineGridContainer>();
        private ObjectsDatabaseSO activeDatabase;

        public event Action OnClicked, OnExit;

        void Awake()
        {
            // Hide all grids initially
            foreach (var container in splineGridContainers)
            {
                if (container != null)
                    container.HideGrid();
            }
        }

        void Start()
        {
            // debug
            StartPlacement(databases[0],0);
            lastMouseScreenPosition = Input.mousePosition;
        }
        
        private Vector3 lastMouseScreenPosition;
        
        void Update()
        {
            if (buildingState == null)
                return;
                
            if (activeGridContainers == null || activeGridContainers.Count == 0)
                return;
            
            // Only check for input events - position updates handled separately
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
                OnClicked?.Invoke();
            
            if (Input.GetKeyDown(KeyCode.Escape))
                OnExit?.Invoke();
            
            // Handle rotation with R key
            if (Input.GetKeyDown(KeyCode.R) && buildingState is PlacementState placementState)
            {
                RotateObject(placementState);
            }
            
            // Check if mouse moved (only update when mouse actually moves)
            Vector3 currentMousePos = Input.mousePosition;
            if (Vector3.Distance(currentMousePos, lastMouseScreenPosition) > 0.1f)
            {
                lastMouseScreenPosition = currentMousePos;
                UpdateMousePosition();
            }
        }
        
        private void UpdateMousePosition()
        {
            if (buildingState == null || activeGridContainers == null || activeGridContainers.Count == 0)
                return;
                
            Vector3 mousePosition = GetSelectedMapPosition();
            
            // Find which active grid container the mouse is over
            SplineGridContainer currentContainer = GetContainerAtPosition(mousePosition);
            if (currentContainer == null || currentContainer.Grid == null)
                return;
                
            Vector3Int gridPosition = currentContainer.Grid.WorldToCell(mousePosition);
            if (lastDetectedPosition != gridPosition)
            {
                // If it's a PlacementState, use the extended method with container
                if (buildingState is PlacementState placementState)
                {
                    placementState.UpdateState(gridPosition, currentContainer);
                }
                else
                {
                    buildingState.UpdateState(gridPosition);
                }
                lastDetectedPosition = gridPosition;
            }
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
        
        /// <summary>
        /// Finds which active grid container contains the given world position
        /// </summary>
        private SplineGridContainer GetContainerAtPosition(Vector3 worldPosition)
        {
            foreach (var container in activeGridContainers)
            {
                if (container != null && container.IsPositionWithinBoundary(worldPosition))
                {
                    return container;
                }
            }
            // If no container contains the position, return the first one as fallback
            return activeGridContainers.Count > 0 ? activeGridContainers[0] : null;
        }

        public void StartPlacement(ObjectsDatabaseSO targetDatabase, int ID)
        {
            StopPlacement();

            activeDatabase = targetDatabase;
            activeGridContainers.Clear();
            
            // Find all spline grid containers that match the database's layer mask
            foreach (var container in splineGridContainers)
            {
                if (container != null && container.ObjectsDatabase == targetDatabase)
                {
                    // Check if layer masks match
                    if ((container.PlacementLayerMask.value & targetDatabase.placementLayermask.value) != 0)
                    {
                        activeGridContainers.Add(container);
                    }
                }
            }
            
            if (activeGridContainers.Count == 0)
            {
                Debug.LogError($"No spline grid container found for database with layer mask \"{LayerMask.LayerToName(targetDatabase.placementLayermask.value)}\".\n Does the database have a layer mask set?");
                return;
            }
            
            // Show all active grids, hide others
            foreach (var container in splineGridContainers)
            {
                if (container != null)
                {
                    if (activeGridContainers.Contains(container))
                        container.ShowGrid();
                    else
                        container.HideGrid();
                }
            }
            
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
            OnClicked += PlaceStructure;
            OnExit += StopPlacement;
        }

        public void StartRemoving()
        {
            StopPlacement();
            
            // Show all grids for removal
            foreach (var container in splineGridContainers)
            {
                if (container != null)
                    container.ShowGrid();
            }
            
            // Use first available grid for removal state
            if (splineGridContainers.Count > 0 && splineGridContainers[0] != null)
            {
                activeGridContainers.Clear();
                activeGridContainers.AddRange(splineGridContainers.Where(c => c != null));
                
                buildingState = new RemovingState(splineGridContainers[0].Grid,
                                                preview,
                                                splineGridContainers[0].GridData,
                                                objectPlacer,
                                                soundFeedback);
            }
            else
            {
                Debug.LogError("No spline grid containers available for removal");
                return;
            }
            
            OnClicked += PlaceStructure;
            OnExit += StopPlacement;
        }

        void PlaceStructure()
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

        void StopPlacement()
        {
            soundFeedback.PlaySound(SoundType.Click);
            if (buildingState == null)
                return;
                
            // Hide all grids
            foreach (var container in splineGridContainers)
            {
                if (container != null)
                    container.HideGrid();
            }
            
            buildingState.EndState();
            OnClicked -= PlaceStructure;
            OnExit -= StopPlacement;
            lastDetectedPosition = Vector3Int.zero;
            buildingState = null;
            activeGridContainers.Clear();
            activeDatabase = null;
        }

        public Vector3 GetSelectedMapPosition()
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = sceneCamera.nearClipPlane;
            Ray ray = sceneCamera.ScreenPointToRay(mousePos);
            RaycastHit hit;
            
            // Combine layer masks from all active grid containers
            LayerMask combinedLayerMask = 0;
            if (activeGridContainers != null && activeGridContainers.Count > 0)
            {
                foreach (var container in activeGridContainers)
                {
                    if (container != null)
                    {
                        combinedLayerMask |= container.PlacementLayerMask;
                    }
                }
            }
            else
            {
                combinedLayerMask = ~0; // Use all layers if no active containers
            }
            
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, combinedLayerMask))
            {
                lastPosition = hit.point;
            }
            return lastPosition;
        }

        public bool IsPointerOverUI()
            => EventSystem.current.IsPointerOverGameObject();
    }
}