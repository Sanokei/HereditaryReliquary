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
        GridData floorData;
        GridData furnitureData;
        ObjectPlacer objectPlacer;
        SoundFeedback soundFeedback;
        private struct PlacementGeometry
        {
            public Vector3Int Origin;
            public Vector3Int Size;
            public Vector3 PreviewCenter;
        }

        public PlacementState(int iD,
                            Grid grid,
                            PreviewSystem previewSystem,
                            ObjectsDatabaseSO database,
                            GridData floorData,
                            GridData furnitureData,
                            ObjectPlacer objectPlacer,
                            SoundFeedback soundFeedback)
        {
            ID = iD;
            this.grid = grid;
            this.previewSystem = previewSystem;
            this.database = database;
            this.floorData = floorData;
            this.furnitureData = furnitureData;
            this.objectPlacer = objectPlacer;
            this.soundFeedback = soundFeedback;

            selectedObjectIndex = database.objectsData.FindIndex(data => data.ID == ID);
            if (selectedObjectIndex > -1)
            {
                previewSystem.StartShowingPlacementPreview(
                    database.objectsData[selectedObjectIndex].Prefab,
                    database.objectsData[selectedObjectIndex].Size,
                    grid);
            }
            else
                throw new System.Exception($"No object with ID {iD}");

        }

        public void EndState()
        {
            previewSystem.StopShowingPreview();
        }

        public void OnAction(Vector3Int gridPosition)
        {

            PlacementGeometry geometry;
            bool placementValidity = CheckPlacementValidity(gridPosition, selectedObjectIndex, out geometry);
            if (placementValidity == false)
            {
                soundFeedback.PlaySound(SoundType.wrongPlacement);
                return;
            }
            soundFeedback.PlaySound(SoundType.Place);
            
            // Calculate placement position to match preview exactly
            // Preview calculation in UpdateState: center + centerOffset
            // Preview calculation in MovePreview: position + pivotOffset
            // Final preview position: center + centerOffset + pivotOffset
            // Since centerOffset = (size-1) * cellSize * 0.5 and pivotOffset = -(size-1) * cellSize * 0.5
            // They cancel out, so preview ends up at: grid.GetCellCenterWorld(gridPosition)
            Vector3Int objectSize = geometry.Size;
            Vector3 cellSize = grid.cellSize;
            Vector3 pivotOffset = new Vector3(
                -(objectSize.x - 1) * cellSize.x * 0.5f,
                -(objectSize.y - 1) * cellSize.y * 0.5f,
                -(objectSize.z - 1) * cellSize.z * 0.5f);
            
            // Match preview calculation exactly: previewCenter + pivotOffset
            Vector3 placementPosition = geometry.PreviewCenter + pivotOffset;
            
            int index = objectPlacer.PlaceObject(database.objectsData[selectedObjectIndex].Prefab,
                placementPosition);

            GridData selectedData = database.objectsData[selectedObjectIndex].ID == 0 ?
                floorData :
                furnitureData;
            selectedData.AddObjectAt(geometry.Origin,
                database.objectsData[selectedObjectIndex].Size,
                database.objectsData[selectedObjectIndex].ID,
                index);

            // Update preview position using the same calculation as UpdateState
            // This ensures the preview is correctly positioned immediately after placement
            UpdateState(gridPosition);
        }

        private bool CheckPlacementValidity(Vector3Int gridPosition, int selectedObjectIndex, out PlacementGeometry geometry)
        {
            Vector3Int objectSize = database.objectsData[selectedObjectIndex].Size;
            geometry = CalculatePlacementGeometry(gridPosition, objectSize);
            // TODO: Check if within grid 
            return floorData.CanPlaceObejctAt(geometry.Origin, objectSize) && furnitureData.CanPlaceObejctAt(geometry.Origin, objectSize);
        }

        public void UpdateState(Vector3Int gridPosition)
        {
            PlacementGeometry geometry;
            bool placementValidity = CheckPlacementValidity(gridPosition, selectedObjectIndex, out geometry);

            // Calculate position for preview - needs to account for multi-cell objects
            // The preview system will apply the same offset to both preview and indicator
            Vector3 previewPosition = geometry.PreviewCenter;
            
            previewSystem.UpdatePosition(previewPosition, placementValidity);
        }

        private PlacementGeometry CalculatePlacementGeometry(Vector3Int gridPosition, Vector3Int objectSize)
        {
            Vector3 cellSize = grid.cellSize;
            Vector3 pointerCellCenter = grid.GetCellCenterWorld(gridPosition);
            Vector3 centerOffset = new Vector3(
                (objectSize.x - 1) * cellSize.x * 0.5f,
                (objectSize.y - 1) * cellSize.y * 0.5f,
                (objectSize.z - 1) * cellSize.z * 0.5f);

            Vector3 previewCenter = pointerCellCenter + centerOffset;

            Vector3 halfSizeWorld = Vector3.Scale(new Vector3(objectSize.x, objectSize.y, objectSize.z), cellSize) * 0.5f;
            Vector3 minCellCenterWorld = previewCenter - halfSizeWorld + new Vector3(
                cellSize.x * 0.5f,
                cellSize.y * 0.5f,
                cellSize.z * 0.5f);

            Vector3Int origin = grid.WorldToCell(minCellCenterWorld);

            return new PlacementGeometry
            {
                Origin = origin,
                Size = objectSize,
                PreviewCenter = previewCenter
            };
        }
    }

}