using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GridBuilder.Core
{
    public abstract class BaseBuildingSystem : MonoBehaviour
    {        
        protected PreviewSystem preview;

        protected Vector3Int lastDetectedPosition = Vector3Int.zero;

        protected ObjectPlacer objectPlacer;

        protected IBuildingState buildingState;

        protected SoundFeedback soundFeedback;

        protected Camera sceneCamera;

        protected Vector3 lastPosition;

        protected List<SplineGridContainer> activeGridContainers = new List<SplineGridContainer>();

        public event Action OnClicked, OnExit;

        private bool isInitialized = false;
        protected BuildingSystemManager manager;

        /// <summary>
        /// Initialize the system with required dependencies from the manager
        /// </summary>
        public void Initialize(PreviewSystem preview, ObjectPlacer objectPlacer, SoundFeedback soundFeedback, Camera sceneCamera, BuildingSystemManager manager = null)
        {
            if (preview == null || objectPlacer == null || soundFeedback == null || sceneCamera == null)
            {
                Debug.LogError($"{GetType().Name} initialization failed: One or more dependencies are null.");
                return;
            }
            
            this.preview = preview;
            this.objectPlacer = objectPlacer;
            this.soundFeedback = soundFeedback;
            this.sceneCamera = sceneCamera;
            this.manager = manager;
            isInitialized = true;
        }

        protected virtual void Awake()
        {
            // If not initialized yet, try to find the manager and initialize
            if (!isInitialized)
            {
                BuildingSystemManager foundManager = FindFirstObjectByType<BuildingSystemManager>();
                if (foundManager != null)
                {
                    Initialize(foundManager.PreviewSystem, foundManager.ObjectPlacer, foundManager.SoundFeedback, foundManager.SceneCamera, foundManager);
                }
            }
        }

        protected virtual void OnEnable()
        {
            // Ensure system is initialized before enabling
            if (!isInitialized)
            {
                // Try one more time to find the manager (in case Awake order was different)
                BuildingSystemManager foundManager = FindFirstObjectByType<BuildingSystemManager>();
                if (foundManager != null)
                {
                    Initialize(foundManager.PreviewSystem, foundManager.ObjectPlacer, foundManager.SoundFeedback, foundManager.SceneCamera, foundManager);
                }
                else
                {
                    Debug.LogWarning($"{GetType().Name} is not initialized. Make sure BuildingSystemManager is set up correctly.");
                    enabled = false;
                    return;
                }
            }
            
            // Initialize mouse position tracking
            lastMouseScreenPosition = Input.mousePosition;
        }

        /// <summary>
        /// Request to show specific grids through the manager's event system
        /// </summary>
        protected void RequestShowGrids(List<SplineGridContainer> containers)
        {
            if (manager != null)
            {
                manager.RequestShowGrids(containers);
            }
        }

        /// <summary>
        /// Request to hide specific grids through the manager's event system
        /// </summary>
        protected void RequestHideGrids(List<SplineGridContainer> containers)
        {
            if (manager != null)
            {
                manager.RequestHideGrids(containers);
            }
        }

        /// <summary>
        /// Request to hide all grids through the manager's event system
        /// </summary>
        protected void RequestHideAllGrids()
        {
            if (manager != null)
            {
                manager.RequestHideAllGrids();
            }
        }

        protected virtual void OnDisable()
        {
            // Clean up when system is disabled
            if (buildingState != null)
            {
                // Unsubscribe from events (child classes handle their own unsubscription in Stop methods)
                OnClicked -= OnAction;
                
                // End the current state
                buildingState.EndState();
                buildingState = null;
            }
            
            activeGridContainers.Clear();
            lastDetectedPosition = Vector3Int.zero;
        }
        
        protected Vector3 lastMouseScreenPosition;
        
        protected virtual void Update()
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
            
            // Handle system-specific input
            HandleSystemSpecificInput();
            
            // Check if mouse moved (only update when mouse actually moves)
            Vector3 currentMousePos = Input.mousePosition;
            if (Vector3.Distance(currentMousePos, lastMouseScreenPosition) > 0.1f)
            {
                lastMouseScreenPosition = currentMousePos;
                UpdateMousePosition();
            }
        }
        
        /// <summary>
        /// Override this method to handle system-specific input (e.g., rotation for placement)
        /// </summary>
        protected virtual void HandleSystemSpecificInput() { }
        
        protected virtual void UpdateMousePosition()
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
                UpdateStateForContainer(gridPosition, currentContainer);
                lastDetectedPosition = gridPosition;
            }
        }
        
        /// <summary>
        /// Override this method to handle state updates with container information
        /// </summary>
        protected virtual void UpdateStateForContainer(Vector3Int gridPosition, SplineGridContainer container)
        {
            buildingState.UpdateState(gridPosition);
        }
        
        /// <summary>
        /// Finds which active grid container contains the given world position
        /// </summary>
        protected SplineGridContainer GetContainerAtPosition(Vector3 worldPosition)
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

        protected virtual void StopBuilding()
        {
            soundFeedback.PlaySound(SoundType.Click);
            if (buildingState == null)
                return;
            
            // Hide all grids when stopping
            RequestHideAllGrids();
            
            buildingState.EndState();
            lastDetectedPosition = Vector3Int.zero;
            buildingState = null;
            activeGridContainers.Clear();
        }

        /// <summary>
        /// Override this method to handle the action when clicking (placement or removal)
        /// </summary>
        protected abstract void OnAction();

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

