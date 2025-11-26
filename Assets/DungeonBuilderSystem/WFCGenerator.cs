using UnityEngine;
using GridBuilder.Core;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilderSystem
{
    public class WFCGenerator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DungeonRoom room;
        [SerializeField] private ObjectsDatabaseSO database;

        [Header("Asset Names (Partial Match)")]
        [SerializeField] private string wallKeyword = "Wall";
        [SerializeField] private string doorKeyword = "Door";
        [SerializeField] private string windowKeyword = "Window";

        [Header("Generation Settings")]
        [SerializeField] private int minDoors = 1;
        [SerializeField] private int maxDoors = 2;
        [SerializeField] private bool preferCenteredDoors = true;
        [Range(0f, 1f)] [SerializeField] private float windowChance = 0.1f;

        private List<Vector3Int> doorPositions = new List<Vector3Int>();
        private List<Vector3Int> windowPositions = new List<Vector3Int>();

        public void Generate()
        {
            if (room == null || database == null)
            {
                Debug.LogError("WFCGenerator: Missing references.");
                return;
            }

            room.InitializeForGeneration();
            room.CalculateLayers();

            doorPositions.Clear();
            windowPositions.Clear();

            // Pass 0: Pre-calculate Doors
            PreCalculateDoors();

            Pass1_Walls();
            
            room.RefreshGrid();
        }

        [ContextMenu("Pre-Calculate Doors")]
        public void PreCalculateDoors()
        {
            if (room == null) return;

            // Reset doors in room
            room.doors.Clear();
            
            // Ensure layers are calculated
            room.CalculateLayers();

            List<Vector3Int> wallCells = room.GetCellsByLayer(DungeonRoom.RoomLayer.Wall);
            doorPositions.Clear();
            
            if (wallCells.Count == 0) return;

            // Group wall cells by continuous segments (walls)
            List<List<Vector3Int>> wallSegments = GroupWallSegments(wallCells);
            
            int doorsPlaced = 0;
            
            // Sort segments by length descending to place doors on longer walls first?
            // Or just iterate through them.
            
            foreach (var segment in wallSegments)
            {
                if (doorsPlaced >= maxDoors) break;
                
                // Skip corners implicitly because we only got Wall layer cells
                if (segment.Count < 3) continue; // Too short for a door with spacing?

                // Decide if this wall gets a door
                // Logic: Try to place 1 door per wall segment, up to maxDoors total for the room
                // Or distribute maxDoors across available walls.
                
                // Simplified: Try to place 1 or 2 doors on this wall
                int doorsForThisWall = (doorsPlaced == 0) ? 1 : (Random.value > 0.5f ? 1 : 0);
                if (doorsPlaced + doorsForThisWall > maxDoors) doorsForThisWall = maxDoors - doorsPlaced;
                
                if (doorsForThisWall == 0) continue;

                if (doorsForThisWall == 1)
                {
                    // Center placement
                    Vector3Int candidate;
                    if (preferCenteredDoors)
                    {
                         candidate = segment[segment.Count / 2];
                    }
                    else
                    {
                        candidate = segment[Random.Range(1, segment.Count - 1)];
                    }

                    if (WFCValidator.ValidateDoor(room, candidate, doorPositions))
                    {
                        doorPositions.Add(candidate);
                        // Persist door to DungeonRoom so it survives recalculation
                        if (!room.doors.Contains(candidate))
                            room.doors.Add(candidate);
                            
                        // Set layer to Door for visualization (and trigger recalculation if needed)
                        room.SetCellLayer(candidate, DungeonRoom.RoomLayer.Door);
                        doorsPlaced++;
                    }
                }
                else if (doorsForThisWall == 2)
                {
                    // TODO: account for wall size divided has a remaninder, and looks off.
                    // Equally spaced
                    // 1/3 and 2/3 points
                    int index1 = segment.Count / 3;
                    int index2 = (segment.Count * 2) / 3;
                    
                    Vector3Int c1 = segment[index1];
                    Vector3Int c2 = segment[index2];
                    
                    if (WFCValidator.ValidateDoor(room, c1, doorPositions))
                    {
                        doorPositions.Add(c1);
                        if (!room.doors.Contains(c1)) room.doors.Add(c1);
                        room.SetCellLayer(c1, DungeonRoom.RoomLayer.Door);
                        doorsPlaced++;
                    }
                    
                    if (doorsPlaced < maxDoors && WFCValidator.ValidateDoor(room, c2, doorPositions))
                    {
                        doorPositions.Add(c2);
                        if (!room.doors.Contains(c2)) room.doors.Add(c2);
                        room.SetCellLayer(c2, DungeonRoom.RoomLayer.Door);
                        doorsPlaced++;
                    }
                }
            }
            
            // Fallback if no doors placed (e.g. all walls too short or validation failed)
            if (doorsPlaced < minDoors)
            {
                 // Try random placement on any valid wall cell
                 int attempts = 0;
                 while (doorsPlaced < minDoors && attempts < 20)
                 {
                     Vector3Int candidate = wallCells[Random.Range(0, wallCells.Count)];
                    if (WFCValidator.ValidateDoor(room, candidate, doorPositions))
                    {
                        doorPositions.Add(candidate);
                        if (!room.doors.Contains(candidate)) room.doors.Add(candidate);
                        room.SetCellLayer(candidate, DungeonRoom.RoomLayer.Door);
                        doorsPlaced++;
                    }
                     attempts++;
                 }
            }
            
            // Force a refresh of layers to apply Props logic correctly around doors
            room.CalculateLayers();
        }

        private List<List<Vector3Int>> GroupWallSegments(List<Vector3Int> wallCells)
        {
            // Simple flood fill or linear walk to group contiguous wall cells
            // Note: Wall cells in cellLayers might not be contiguous in list order.
            // And corner cells break continuity in terms of "straight wall segments".
            
            List<List<Vector3Int>> segments = new List<List<Vector3Int>>();
            HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
            
            // We need to distinguish between different walls (e.g. North Wall, East Wall)
            // Wall segments are separated by Corners.
            
            foreach (var cell in wallCells)
            {
                if (visited.Contains(cell)) continue;
                
                List<Vector3Int> currentSegment = new List<Vector3Int>();
                
                // Start exploring
                Queue<Vector3Int> queue = new Queue<Vector3Int>();
                queue.Enqueue(cell);
                visited.Add(cell);
                
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    currentSegment.Add(current);
                    
                    // Check neighbors
                    Vector3Int[] neighbors = {
                        new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
                        new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
                    };
                    
                    foreach (var n in neighbors)
                    {
                        Vector3Int neighbor = current + n;
                        // Must be a Wall (not Corner, not Floor)
                        if (wallCells.Contains(neighbor) && !visited.Contains(neighbor))
                        {
                            // Also check if it is "aligned" - walls are straight lines usually
                            // But for now, just collecting contiguous blocks separated by corners
                            // Since corners are excluded from wallCells, this should separate the walls.
                            
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
                
                // Sort the segment linearly so we can find center/spacing
                // Determine orientation
                if (currentSegment.Count > 1)
                {
                    bool isHorizontal = currentSegment[0].z == currentSegment[1].z;
                    if (isHorizontal)
                        currentSegment = currentSegment.OrderBy(v => v.x).ToList();
                    else
                        currentSegment = currentSegment.OrderBy(v => v.z).ToList();
                }
                
                segments.Add(currentSegment);
            }
            
            return segments;
        }

        private void Pass1_Walls()
        {
            List<Vector3Int> wallCells = room.GetCellsByLayer(DungeonRoom.RoomLayer.Wall);
            List<Vector3Int> cornerCells = room.GetCellsByLayer(DungeonRoom.RoomLayer.Corner);

            // Process Corners (always procedurally generated)
            foreach(var cell in cornerCells)
            {
                PlaceDynamicCorner(cell);
            }

            // Process Walls
            foreach (var cell in wallCells)
            {
                // Check if pre-calculated as door
                if (doorPositions.Contains(cell))
                {
                    PlaceObject(cell, doorKeyword, true);
                    continue;
                }

                // Try Window
                bool placedWindow = false;
                if (Random.value < windowChance)
                {
                    if (WFCValidator.CheckSpacing(cell, doorPositions, 3) && 
                        WFCValidator.CheckSpacing(cell, windowPositions, 3))
                    {
                        if (PlaceObject(cell, windowKeyword, true))
                        {
                            windowPositions.Add(cell);
                            placedWindow = true;
                        }
                    }
                }
                
                // Default to Wall
                if (!placedWindow)
                {
                    PlaceObject(cell, wallKeyword, true);
                }
            }
        }

        private void PlaceDynamicCorner(Vector3Int cell)
        {
            HashSet<Vector3Int> directions = new HashSet<Vector3Int>();
            
            // Check all 4 neighbors
            Vector3Int[] neighbors = {
                new Vector3Int(0, 0, 1),  // North
                new Vector3Int(1, 0, 0),  // East
                new Vector3Int(0, 0, -1), // South
                new Vector3Int(-1, 0, 0)  // West
            };
            
            foreach (var dir in neighbors)
            {
                Vector3Int neighborPos = cell - dir; // Cell in direction -dir relative to current? No.
                // We want to check the neighbor at 'cell + dir' or 'cell - dir'?
                // logic: directions list stores the rotation/facing of the wall we want to place.
                // Wall facing North (0,0,1) is placed at (0,0) if Floor is at (0,-1)?
                // No, Wall facing North usually blocks the North side.
                // If Floor is South (0,-1), we want a wall facing North (0,1) or South (0,-1)?
                // Standard walls usually face "Out".
                // If Floor is South, the wall is on the North boundary of that floor.
                // It faces North (away from floor).
                // So direction = (Cell - FloorCell).
                
                Vector3Int nPos = cell + dir;
                var layer = room.GetCellLayer(nPos);
                
                if (layer == DungeonRoom.RoomLayer.Floor)
                {
                    // Case 1: Direct Floor neighbor (Inner Corner)
                    // Wall should face away from floor: (Cell - nPos) = -dir
                    directions.Add(-dir);
                }
                else if (layer == DungeonRoom.RoomLayer.Wall || layer == DungeonRoom.RoomLayer.Door)
                {
                    // Case 2: Wall neighbor (Outer Corner candidate)
                    // Check if this Wall neighbor has a Floor neighbor
                    foreach (var fDir in neighbors)
                    {
                        Vector3Int fPos = nPos + fDir;
                        if (room.GetCellLayer(fPos) == DungeonRoom.RoomLayer.Floor)
                        {
                            // Neighbor nPos is adjacent to Floor fPos.
                            // The wall at nPos faces away from fPos: (nPos - fPos) = -fDir
                            // We want to extend that wall to current cell 'cell'.
                            // So we add that same direction.
                            directions.Add(-fDir);
                        }
                    }
                }
            }
            
            // If we found directions, place walls
            if (directions.Count > 0)
            {
                var wallData = FindObjectInDB(wallKeyword);
                if (wallData == null) return;

                foreach (var dir in directions)
                {
                    // Calculate rotation for this direction
                    int rotationIndex = 0;
                    if (dir == new Vector3Int(0, 0, 1)) rotationIndex = 0;
                    else if (dir == new Vector3Int(1, 0, 0)) rotationIndex = 1;
                    else if (dir == new Vector3Int(0, 0, -1)) rotationIndex = 2;
                    else if (dir == new Vector3Int(-1, 0, 0)) rotationIndex = 3;
                    
                    Quaternion rotation = Quaternion.Euler(0, rotationIndex * 90f, 0);

                    // Instantiate Wall
                    if (wallData.Prefab != null)
                    {
                         GameObject instance;
                        if (Application.isPlaying)
                        {
                            instance = Instantiate(wallData.Prefab, room.transform);
                        }
                        else
                        {
                            #if UNITY_EDITOR
                            instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(wallData.Prefab, room.transform);
                            #else
                            instance = Instantiate(wallData.Prefab, room.transform);
                            #endif
                        }
                        
                        instance.transform.position = room.Grid.GetCellCenterWorld(cell);
                        instance.transform.rotation = rotation;
                        instance.name = $"{wallData.Name}_DynamicCornerPart";
                    }
                    
                    // Register in GridData?
                    if (directions.ToList().IndexOf(dir) == 0)
                    {
                        room.PlaceObjectRaw(cell, wallData.ID, rotationIndex);
                    }
                }
            }
            // Note: If no directions found, corner cell is left empty (shouldn't happen in normal cases)
        }


        private bool PlaceObject(Vector3Int cell, string keyword, bool rotateToNormal)
        {
            var objData = FindObjectInDB(keyword);
            if (objData == null) return false;

            int rotationIndex = 0;
            Quaternion rotation = Quaternion.identity;
            
            if (rotateToNormal)
            {
                rotationIndex = CalculateRotationIndex(cell);
                rotation = Quaternion.Euler(0, rotationIndex * 90f, 0);
            }

            room.PlaceObjectRaw(cell, objData.ID, rotationIndex);
            
            if (objData.Prefab != null)
            {
                GameObject instance;
                if (Application.isPlaying)
                {
                    instance = Instantiate(objData.Prefab, room.transform);
                }
                else
                {
                    #if UNITY_EDITOR
                    instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(objData.Prefab, room.transform);
                    #else
                    instance = Instantiate(objData.Prefab, room.transform);
                    #endif
                }
                
                instance.transform.position = room.Grid.GetCellCenterWorld(cell);
                instance.transform.rotation = rotation;
            }
            
            return true;
        }

        private ObjectData FindObjectInDB(string keyword)
        {
            if (database == null || database.objectsData == null) return null;

            // Get all matches
            var candidates = database.objectsData.Where(x => x.Name.Contains(keyword));

            // If searching for "Wall", filter out objects that are likely decorations or attachments
            // but happen to have "Wall" in the name (e.g., "Wall_Shelf", "Wall_Torch")
            if (keyword.Contains("Wall") || keyword.Contains("wall")) 
            {
                candidates = candidates.Where(x => !IsFalsePositiveWall(x.Name));
            }

            return candidates.FirstOrDefault();
        }

        private bool IsFalsePositiveWall(string name)
        {
            string lower = name.ToLower();
            // Add more exclusions here as needed
            if (lower.Contains("shelf") || 
                lower.Contains("torch") || 
                lower.Contains("decor") ||
                lower.Contains("sconce") ||
                lower.Contains("pillar") && !lower.Contains("wall_pillar")) // Example logic
            {
                return true;
            }
            return false;
        }

        private int CalculateRotationIndex(Vector3Int cell)
        {
            Vector3Int normal = room.GetPerimeterNormal(cell);
            if (normal == new Vector3Int(0, 0, 1)) return 0;
            if (normal == new Vector3Int(1, 0, 0)) return 1;
            if (normal == new Vector3Int(0, 0, -1)) return 2;
            if (normal == new Vector3Int(-1, 0, 0)) return 3;
            return 0;
        }

        private void OnDrawGizmos()
        {
            if (room == null || doorPositions == null) return;
            
            Gizmos.color = Color.cyan;
            foreach (var doorPos in doorPositions)
            {
                if (room.Grid == null) continue;

                Vector3 center = room.Grid.GetCellCenterWorld(doorPos);
                // Draw sphere inside the cube
                Vector3 drawPos = center; 
                
                Gizmos.DrawSphere(drawPos, 0.3f);
                
                // Draw arrow pointing outwards
                Vector3Int normal = room.GetPerimeterNormal(doorPos);
                Vector3 dir = new Vector3(normal.x, 0, normal.z);
                Vector3 start = drawPos;
                Vector3 end = start + dir * 2.0f; // Longer arrow
                
                Gizmos.DrawLine(start, end);
                
                // Arrow head
                Vector3 right = Quaternion.Euler(0, 150, 0) * dir;
                Vector3 left = Quaternion.Euler(0, -150, 0) * dir;
                Gizmos.DrawLine(end, end + right * 0.5f);
                Gizmos.DrawLine(end, end + left * 0.5f);
            }
        }
    }
}
