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
            
            BoundsInt calculatedBounds = CalculateGridBounds();
            floorData.SetGridBounds(calculatedBounds);
            furnitureData.SetGridBounds(calculatedBounds);
            
            // Set gridVisualization back to inactive if it wasn't active before
            if (gridVisualization != null && !wasActive)
            {
                gridVisualization.SetActive(false);
            }
        }

        private BoundsInt CalculateGridBounds()
        {
            if (gridVisualization == null || grid == null)
            {
                // Fallback to default bounds if gridVisualization or grid is not set
                return new BoundsInt(new Vector3Int(-10, 0, -10), new Vector3Int(20, 1, 20));
            }

            // Get the bounds of the gridVisualization GameObject
            Bounds worldBounds = GetGameObjectBounds(gridVisualization);
            
            // Convert world bounds to grid cell coordinates
            Vector3 minWorld = worldBounds.min;
            Vector3 maxWorld = worldBounds.max;
            
            Vector3Int minCell = grid.WorldToCell(minWorld);
            Vector3Int maxCell = grid.WorldToCell(maxWorld);
            
            // Ensure min is actually minimum and max is maximum (in case of negative coordinates)
            Vector3Int boundsMin = new Vector3Int(
                Mathf.Min(minCell.x, maxCell.x),
                0,
                Mathf.Min(minCell.z, maxCell.z)
            );
            
            Vector3Int boundsSize = new Vector3Int(
                Mathf.Abs(maxCell.x - minCell.x) + 1,
                1,
                Mathf.Abs(maxCell.z - minCell.z) + 1
            );
            
            return new BoundsInt(boundsMin, boundsSize);
        }

        private Bounds GetGameObjectBounds(GameObject gameObject)
        {
            Bounds bounds = new Bounds();
            bool boundsInitialized = false;

            // Try to get bounds from Renderer
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                bounds = renderer.bounds;
                boundsInitialized = true;
            }

            // Try to get bounds from Collider
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                if (boundsInitialized)
                {
                    bounds.Encapsulate(collider.bounds);
                }
                else
                {
                    bounds = collider.bounds;
                    boundsInitialized = true;
                }
            }

            // If no Renderer or Collider, check children
            if (!boundsInitialized)
            {
                Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        bounds.Encapsulate(renderers[i].bounds);
                    }
                    boundsInitialized = true;
                }
            }

            // If still no bounds found, use transform scale as fallback
            if (!boundsInitialized)
            {
                Vector3 scale = gameObject.transform.localScale;
                Vector3 position = gameObject.transform.position;
                bounds = new Bounds(position, scale);
            }

            return bounds;
        }

        void Start()
        {
            StartPlacement(0);
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
            buildingState = new RemovingState(grid, preview, floorData, furnitureData, objectPlacer, soundFeedback);
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