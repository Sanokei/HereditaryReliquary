using System.Collections;
using System.Collections.Generic;
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

            bool placementValidity = CheckPlacementValidity(gridPosition, selectedObjectIndex);
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
            Vector3Int objectSize = database.objectsData[selectedObjectIndex].Size;
            Vector3 cellSize = grid.cellSize;
            
            Vector3 centerPosition = grid.GetCellCenterWorld(gridPosition);
            Vector3 centerOffset = new Vector3(
                (objectSize.x - 1) * cellSize.x * 0.5f,
                (objectSize.y - 1) * cellSize.y * 0.5f,
                (objectSize.z - 1) * cellSize.z * 0.5f);
            Vector3 pivotOffset = new Vector3(
                -(objectSize.x - 1) * cellSize.x * 0.5f,
                -(objectSize.y - 1) * cellSize.y * 0.5f,
                -(objectSize.z - 1) * cellSize.z * 0.5f);
            
            // Match preview calculation exactly: center + centerOffset + pivotOffset
            Vector3 placementPosition = centerPosition + centerOffset + pivotOffset;
            
            int index = objectPlacer.PlaceObject(database.objectsData[selectedObjectIndex].Prefab,
                placementPosition);

            GridData selectedData = database.objectsData[selectedObjectIndex].ID == 0 ?
                floorData :
                furnitureData;
            selectedData.AddObjectAt(gridPosition,
                database.objectsData[selectedObjectIndex].Size,
                database.objectsData[selectedObjectIndex].ID,
                index);

            // Update preview position using the same calculation as UpdateState
            // This ensures the preview is correctly positioned immediately after placement
            UpdateState(gridPosition);
        }

        private bool CheckPlacementValidity(Vector3Int gridPosition, int selectedObjectIndex)
        {
            GridData selectedData = database.objectsData[selectedObjectIndex].ID == 0 ?
                floorData :
                furnitureData;

            return selectedData.CanPlaceObejctAt(gridPosition, database.objectsData[selectedObjectIndex].Size);
        }

        public void UpdateState(Vector3Int gridPosition)
        {
            bool placementValidity = CheckPlacementValidity(gridPosition, selectedObjectIndex);

            // Calculate position for preview - needs to account for multi-cell objects
            // The preview system will apply the same offset to both preview and indicator
            Vector3Int objectSize = database.objectsData[selectedObjectIndex].Size;
            Vector3 cellSize = grid.cellSize;
            Vector3 offset = new Vector3(
                (objectSize.x - 1) * cellSize.x * 0.5f,
                (objectSize.y - 1) * cellSize.y * 0.5f,
                (objectSize.z - 1) * cellSize.z * 0.5f);
            Vector3 previewPosition = grid.GetCellCenterWorld(gridPosition) + offset;
            
            previewSystem.UpdatePosition(previewPosition, placementValidity);
        }
    }

}