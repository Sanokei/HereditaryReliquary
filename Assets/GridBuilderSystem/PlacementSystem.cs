using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GridBuilder.Core
{
    public class PlacementSystem : MonoBehaviour
    {
        [SerializeField] Grid grid;

        [SerializeField] ObjectsDatabaseSO database;

        [SerializeField] GameObject gridVisualization;

        [SerializeField] AudioClip correctPlacementClip, wrongPlacementClip;
        [SerializeField] AudioSource source;

        GridData floorData, furnitureData;

        [SerializeField] PreviewSystem preview;

        Vector3Int lastDetectedPosition = Vector3Int.zero;

        [SerializeField] ObjectPlacer objectPlacer;

        IBuildingState buildingState;

        [SerializeField] SoundFeedback soundFeedback;

        [SerializeField] Camera sceneCamera;

        Vector3 lastPosition;

        [SerializeField] LayerMask placementLayermask;

        public event Action OnClicked, OnExit;

        void Awake()
        {
            floorData = new();
            furnitureData = new();

            // Calculate grid bounds automatically from gridVisualization and grid cell size
            // Note: gridVisualization needs to be active to get accurate bounds
            bool wasActive = gridVisualization != null ? gridVisualization.activeSelf : false;
            if (gridVisualization != null && !wasActive)
            {
                gridVisualization.SetActive(true);
            }

            // Set gridVisualization back to inactive if it wasn't active before
            if (gridVisualization != null && !wasActive)
            {
                gridVisualization.SetActive(false);
            }
        }

        void Start()
        {
            // debug
            StartPlacement(1);
        }
        
        void Update()
        {
            if (buildingState == null)
                return;
            Vector3 mousePosition = GetSelectedMapPosition();
            Vector3Int gridPosition = grid.WorldToCell(mousePosition);
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

        public void StartPlacement(int ID)
        {
            StopPlacement();
            gridVisualization.SetActive(true);
            buildingState = new PlacementState(ID,
                                            grid,
                                            preview,
                                            database,
                                            floorData,
                                            furnitureData,
                                            objectPlacer,
                                            soundFeedback);
            OnClicked += PlaceStructure;
            OnExit += StopPlacement;
        }

        public void StartRemoving()
        {
            StopPlacement();
            gridVisualization.SetActive(true);
            buildingState = new RemovingState(grid,
                                            preview,
                                            floorData,
                                            furnitureData,
                                            objectPlacer,
                                            soundFeedback);
            OnClicked += PlaceStructure;
            OnExit += StopPlacement;
        }

        void PlaceStructure()
        {
            if (IsPointerOverUI())
            {
                return;
            }
            Vector3 mousePosition = GetSelectedMapPosition();
            Vector3Int gridPosition = grid.WorldToCell(mousePosition);

            buildingState.OnAction(gridPosition);

        }

        //bool CheckPlacementValidity(Vector3Int gridPosition, int selectedObjectIndex)
        //{
        //    GridData selectedData = database.objectsData[selectedObjectIndex].ID == 0 ? 
        //        floorData : 
        //        furnitureData;

        //    return selectedData.CanPlaceObejctAt(gridPosition, database.objectsData[selectedObjectIndex].Size);
        //}

        void StopPlacement()
        {
            soundFeedback.PlaySound(SoundType.Click);
            if (buildingState == null)
                return;
            gridVisualization.SetActive(false);
            buildingState.EndState();
            OnClicked -= PlaceStructure;
            OnExit -= StopPlacement;
            lastDetectedPosition = Vector3Int.zero;
            buildingState = null;
        }

        public Vector3 GetSelectedMapPosition()
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = sceneCamera.nearClipPlane;
            Ray ray = sceneCamera.ScreenPointToRay(mousePos);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, placementLayermask))
            {
                lastPosition = hit.point;
            }
            return lastPosition;
        }

        public bool IsPointerOverUI()
            => EventSystem.current.IsPointerOverGameObject();
    }
}