using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilderSystem
{
    public class DungeonConnectionManager : MonoBehaviour
    {
        public void ConnectRooms(List<DungeonRoom> rooms)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    CheckAndConnect(rooms[i], rooms[j]);
                }
            }
        }

        private void CheckAndConnect(DungeonRoom r1, DungeonRoom r2)
        {
            if (r1 == null || r2 == null) return;

            // Get Perimeters (Now Wall)
            var p1 = r1.GetCellsByLayer(DungeonRoom.RoomLayer.Wall);
            
            float cellSize = r1.Grid.cellSize.x;
            Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

            foreach (var c1 in p1)
            {
                Vector3 w1 = r1.Grid.GetCellCenterWorld(c1);

                foreach (var dir in directions)
                {
                    Vector3 neighborWorldPos = w1 + dir * cellSize;
                    Vector3Int c2 = r2.Grid.WorldToCell(neighborWorldPos);

                    // Check if r2 has an object at c2
                    if (r2.DungeonData.HasObjectAt(c2))
                    {
                        // We found adjacent perimeter cells.
                        // Now check if they are Doors.
                        // Since we don't have direct access to "Type" in GridData, 
                        // we'll check the visual objects if they exist.
                        
                        GameObject obj1 = FindVisualObject(r1, c1);
                        GameObject obj2 = FindVisualObject(r2, c2);

                        if (obj1 != null && obj2 != null)
                        {
                            bool isDoor1 = obj1.name.Contains("Door");
                            bool isDoor2 = obj2.name.Contains("Door");

                            if (isDoor1 && isDoor2)
                            {
                                // Visual Merge: Hide one door to prevent clipping / double doors
                                // Or swap to Open Passage.
                                // For now, disable the second door.
                                obj2.SetActive(false);
                                Debug.Log($"Merged Doors between {r1.name} and {r2.name}");
                            }
                        }
                    }
                }
            }
        }

        private GameObject FindVisualObject(DungeonRoom room, Vector3Int cell)
        {
            Vector3 targetPos = room.Grid.GetCellCenterWorld(cell);
            // Simple proximity check among children
            foreach (Transform child in room.transform)
            {
                if (Vector3.Distance(child.position, targetPos) < room.Grid.cellSize.x * 0.1f)
                {
                    return child.gameObject;
                }
            }
            return null;
        }
    }
}

