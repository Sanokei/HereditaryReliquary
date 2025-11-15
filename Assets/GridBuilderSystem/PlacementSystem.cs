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

        private SplineGridContainer activeGridContainer;
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
        }
        
        void Update()
        {
            if (buildingState == null)
                return;
                
            if (activeGridContainer == null || activeGridContainer.Grid == null)
                return;
                
            Vector3 mousePosition = GetSelectedMapPosition();
            Vector3Int gridPosition = activeGridContainer.Grid.WorldToCell(mousePosition);
            if (lastDetectedPosition != gridPosition)
            {
                buildingState.UpdateState(gridPosition);
                lastDetectedPosition = gridPosition;
            }
            if (Input.GetMouseButtonDown(0))
                OnClicked?.Invoke();
            if (Input.GetKeyDown(KeyCode.Escape))
                OnExit?.Invoke();
        }

        public void StartPlacement(ObjectsDatabaseSO targetDatabase, int ID)
        {
            StopPlacement();

            activeDatabase = targetDatabase;
            
            // Find spline grid container that matches the database's layer mask
            SplineGridContainer targetContainer = null;
            foreach (var container in splineGridContainers)
            {
                if (container != null && container.ObjectsDatabase == targetDatabase)
                {
                    // Check if layer masks match
                    if ((container.PlacementLayerMask.value & targetDatabase.placementLayermask.value) != 0)
                    {
                        targetContainer = container;
                        break;
                    }
                }
            }
            
            if (targetContainer == null)
            {
                Debug.LogError($"No spline grid container found for database with layer mask \"{LayerMask.LayerToName(targetDatabase.placementLayermask.value)}\".\n Does the database have a layer mask set?");
                return;
            }
            
            activeGridContainer = targetContainer;
            
            // Show the active grid, hide others
            foreach (var container in splineGridContainers)
            {
                if (container != null)
                {
                    if (container == activeGridContainer)
                        container.ShowGrid();
                    else
                        container.HideGrid();
                }
            }
            
            buildingState = new PlacementState(ID,
                                            activeGridContainer.Grid,
                                            preview,
                                            activeDatabase,
                                            activeGridContainer.GridData,
                                            objectPlacer,
                                            soundFeedback,
                                            activeGridContainer);
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
                activeGridContainer = splineGridContainers[0];
                buildingState = new RemovingState(activeGridContainer.Grid,
                                                preview,
                                                activeGridContainer.GridData,
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
            
            if (activeGridContainer == null || activeGridContainer.Grid == null)
                return;
                
            Vector3 mousePosition = GetSelectedMapPosition();
            Vector3Int gridPosition = activeGridContainer.Grid.WorldToCell(mousePosition);

            buildingState.OnAction(gridPosition);
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
            activeGridContainer = null;
            activeDatabase = null;
        }

        public Vector3 GetSelectedMapPosition()
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = sceneCamera.nearClipPlane;
            Ray ray = sceneCamera.ScreenPointToRay(mousePos);
            RaycastHit hit;
            
            // Use active grid container's layer mask if available
            LayerMask layerMask = activeGridContainer != null ? activeGridContainer.PlacementLayerMask : ~0;
            
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
            {
                lastPosition = hit.point;
            }
            return lastPosition;
        }

        public bool IsPointerOverUI()
            => EventSystem.current.IsPointerOverGameObject();
    }
}