using UnityEngine;
using UnityEngine.Splines;
using GridBuilder.Core;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilderSystem
{
    [ExecuteInEditMode]
    public class DungeonRoom : SplineGridContainer
    {
        public enum RoomLayer { None, Wall, Corner, Floor, Perimeter, Door, Prop }
        public enum ShapeType { Rectangle, L_Shape, U_Shape, T_Shape, I_Shape }

        [Header("Dungeon Settings")]
        [SerializeField] private bool visualizeLayers = true;
        [SerializeField] private Color wallColor = new Color(1, 0, 0, 0.3f);
        [SerializeField] private Color floorColor = new Color(0, 1, 0, 0.1f);
        [SerializeField] private Color cornerColor = new Color(1, 1, 0, 0.3f); // Yellow
        [SerializeField] private Color doorColor = new Color(0, 0, 1, 0.3f); // Blue
        [SerializeField] private Color propColor = new Color(0.5f, 0, 0.5f, 0.3f); // Purple

        private Dictionary<Vector3Int, RoomLayer> cellLayers = new Dictionary<Vector3Int, RoomLayer>();
        private bool layersDirty = true;
        
        // Serialized list to persist door selections
        [HideInInspector] public List<Vector3Int> doors = new List<Vector3Int>();

        public DungeonGridData DungeonData => (DungeonGridData)GridData;

        protected override GridData CreateGridData()
        {
            return new DungeonGridData();
        }

        protected override void UpdateSplineFromGridSize()
        {
            if (SplineContainer != null && SplineContainer.Spline != null && SplineContainer.Spline.Count == 0)
            {
                base.UpdateSplineFromGridSize();
            }
        }

        protected override void InitializeGrid()
        {
            base.InitializeGrid();
            layersDirty = true;
        }

        private void Update()
        {
            if (transform.hasChanged)
            {
                layersDirty = true;
                transform.hasChanged = false;
            }
        }
        
        // Use base OnDisable/OnEnable if needed, but we can rely on SplineGridContainer's OnDisable for cleanup.
        // However, we want to ensure our specific visualization is cleared if needed.
        // SplineGridContainer.OnDisable calls HideGrid().

        private void OnDrawGizmos()
        {
            if (!visualizeLayers || Grid == null) return;

            if (layersDirty)
            {
                CalculateLayers();
            }

            // Draw Cubes - Only for Wall and Corner, outside the grid area
            // User requested "visual cubes should be outside of the grid size area"
            
            float yOffset = Grid.cellSize.y * 0.5f; // Slight offset to be visible
            Vector3 offsetVec = Vector3.up * yOffset;

            foreach (var kvp in cellLayers)
            {
                if (kvp.Value == RoomLayer.Floor) continue; // Don't draw cubes for inner grid

                Vector3 worldPos = Grid.GetCellCenterWorld(kvp.Key);
                Vector3 size = Grid.cellSize * 0.9f; 
                Vector3 drawPos = worldPos; // + offsetVec; // Keep on grid plane or slightly up? User said "outside of grid size area" likely referring to XZ plane.

                Color drawColor = GetLayerColor(kvp.Value);
                
                Gizmos.color = drawColor;
                Gizmos.DrawCube(drawPos, size);
                
                if (kvp.Value == RoomLayer.Corner)
                {
                     Gizmos.color = Color.white;
                     Gizmos.DrawWireCube(drawPos, size * 1.1f);
                }

                if (kvp.Value == RoomLayer.Door)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawCube(drawPos, size); // Solid cube
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireCube(drawPos, size * 1.1f);
                }
                
                if (kvp.Value == RoomLayer.Prop)
                {
                    Gizmos.color = propColor;
                    Gizmos.DrawCube(drawPos, size);
                }
            }

            // Draw Splines (Lines) for visualization
            DrawLayerSplines();
        }
        
        private void DrawLayerSplines()
        {
            // Draw a spline loop for the Walls
            var wallCells = cellLayers.Where(x => x.Value == RoomLayer.Wall || x.Value == RoomLayer.Corner || x.Value == RoomLayer.Door).Select(x => x.Key).ToHashSet();
            if (wallCells.Count == 0) return;

            Gizmos.color = wallColor;
            foreach (var cell in wallCells)
            {
                Vector3 center = Grid.GetCellCenterWorld(cell);
                Vector3Int[] neighbors = { new Vector3Int(1,0,0), new Vector3Int(0,0,1) };
                foreach (var n in neighbors)
                {
                    if (wallCells.Contains(cell + n))
                    {
                        Vector3 neighborPos = Grid.GetCellCenterWorld(cell + n);
                        Gizmos.DrawLine(center, neighborPos);
                    }
                }
            }
        }
        
        private Color GetLayerColor(RoomLayer layer)
        {
            switch (layer)
            {
                case RoomLayer.Wall: return wallColor;
                case RoomLayer.Corner: return cornerColor;
                case RoomLayer.Floor: return floorColor;
                case RoomLayer.Door: return doorColor;
                default: return Color.white;
            }
        }

        public void CalculateLayers()
        {
            cellLayers.Clear();
            if (Grid == null || SplineContainer == null || SplineContainer.Spline == null) return;

            // 1. Find all valid internal cells (Floor)
            // We scan a bounding box around the spline
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool first = true;
            foreach (var knot in SplineContainer.Spline)
            {
                Vector3 worldPos = SplineContainer.transform.TransformPoint(knot.Position);
                if (first) { bounds = new Bounds(worldPos, Vector3.zero); first = false; }
                else { bounds.Encapsulate(worldPos); }
            }
            bounds.Expand(Grid.cellSize * 2); // Expand to catch walls

            Vector3Int min = Grid.WorldToCell(bounds.min);
            Vector3Int max = Grid.WorldToCell(bounds.max);

            HashSet<Vector3Int> insideCells = new HashSet<Vector3Int>();
            HashSet<Vector3Int> potentialWallCells = new HashSet<Vector3Int>();

            for (int x = min.x; x <= max.x; x++)
            {
                for (int z = min.z; z <= max.z; z++)
                {
                     Vector3Int cell = new Vector3Int(x, 0, z);
                     Vector3 worldPos = Grid.GetCellCenterWorld(cell);
                     
                     if (IsPositionWithinBoundary(worldPos))
                     {
                         insideCells.Add(cell);
                         cellLayers[cell] = RoomLayer.Floor;
                     }
                     else
                     {
                         potentialWallCells.Add(cell);
                     }
                }
            }

            // 2. Find Walls: Neighbors of Floor cells that are NOT Floor cells
            foreach (var cell in insideCells)
            {
                Vector3Int[] neighbors = {
                    new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
                    new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1),
                    // Diagonals to ensure outer corners are captured
                    new Vector3Int(1, 0, 1), new Vector3Int(1, 0, -1),
                    new Vector3Int(-1, 0, 1), new Vector3Int(-1, 0, -1)
                };

                foreach (var n in neighbors)
                {
                    Vector3Int neighbor = cell + n;
                    if (!insideCells.Contains(neighbor))
                    {
                        // This is a wall candidate
                        // Let's first classify as Wall
                        cellLayers[neighbor] = RoomLayer.Wall;
                    }
                }
            }
            
            // 2b. Apply persistent Doors to override Wall
            foreach (var doorPos in doors)
            {
                if (cellLayers.ContainsKey(doorPos))
                {
                    cellLayers[doorPos] = RoomLayer.Door;
                }
            }

            // 3. Identify Corners within Walls
            // A wall cell is a corner if it has neighbors in different axes (e.g. North and East) that are also walls? 
            // OR simply checking geometric corners of the shape.
            // Using the user's "SplineKnot" logic or just checking adjacency.
            // Let's iterate modified keys to avoid concurrent modification
            var wallKeys = cellLayers.Where(x => x.Value == RoomLayer.Wall).Select(x => x.Key).ToList();
            foreach (var cell in wallKeys)
            {
                if (IsCorner(cell, insideCells))
                {
                    cellLayers[cell] = RoomLayer.Corner;
                }
            }

            // 4. Identify Prop Layer
            // Iterate Floor cells. If adjacent to Wall or Corner, it is a Prop.
            // If adjacent to Door, it stays Floor (clearance).
            
            // We need to iterate a copy of keys or just insideCells
            foreach (var cell in insideCells)
            {
                bool isProp = false;
                bool isClearance = false;
                
                Vector3Int[] neighbors = {
                    new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
                    new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
                };
                
                foreach (var n in neighbors)
                {
                    Vector3Int neighbor = cell + n;
                    if (cellLayers.ContainsKey(neighbor))
                    {
                        var layer = cellLayers[neighbor];
                        if (layer == RoomLayer.Door)
                        {
                            isClearance = true;
                            break; // Priority to clearance
                        }
                        if (layer == RoomLayer.Wall || layer == RoomLayer.Corner)
                        {
                            isProp = true;
                        }
                    }
                }
                
                if (isClearance)
                {
                    cellLayers[cell] = RoomLayer.Floor;
                }
                else if (isProp)
                {
                    cellLayers[cell] = RoomLayer.Prop;
                }
            }

            layersDirty = false;
        }

        private bool IsCorner(Vector3Int cell, HashSet<Vector3Int> floorCells)
        {
            // A generic way to detect outer corners on a grid:
            // Count how many direct neighbors are floors.
            // 1 neighbor = Edge wall
            // 2 neighbors = Inner corner (concave)
            // 0 neighbors = Outer corner (convex)? No, must be adjacent to floor.
            
            // But we also have "outer corners" like the tip of an L shape.
            // Let's look at the neighbors of this Wall cell.
            
            int floorNeighbors = 0;
            Vector3Int[] neighbors = {
                new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
            };
            
            foreach (var n in neighbors)
            {
                if (floorCells.Contains(cell + n)) floorNeighbors++;
            }

            // If it touches 2 floor cells (e.g. an inner corner of the room), it's a corner.
            if (floorNeighbors >= 2) return true;
            
            // For outer corners (convex), the wall cell touches only 1 floor cell.
            // But it must also be part of a "bend" in the wall.
            // Check diagonal neighbors?
            
            // Alternative: Use the Spline Knots as the definitive "Corners" if they align.
            // The user requested "SplineKnot" logic previously. Let's reuse that if possible, 
            // but mapped to the *outside* layer.
            // The knots define the *inner* boundary. 
            // The outer corner is diagonally adjacent to the knot?
            
            // Let's use neighbor logic:
            // If a wall cell has wall neighbors in perpendicular directions, it's a corner?
            // e.g. North is Wall, East is Wall -> Corner.
            
            int wallNeighbors = 0;
            bool hasX = false;
            bool hasZ = false;
            
             foreach (var n in neighbors)
            {
                if (cellLayers.ContainsKey(cell + n) && cellLayers[cell + n] != RoomLayer.Floor)
                {
                    wallNeighbors++;
                    if (n.x != 0) hasX = true;
                    if (n.z != 0) hasZ = true;
                }
            }
             
            // If we have walls on both X and Z axis, we are likely a corner.
            if (hasX && hasZ) return true;
            
            // End of a line (1 wall neighbor)
            if (wallNeighbors == 1) return true;

            return false;
        }

        public void SetCellLayer(Vector3Int cell, RoomLayer layer)
        {
            if (cellLayers.ContainsKey(cell))
            {
                cellLayers[cell] = layer;
                // No need to set layersDirty unless we want to recalculate everything
                // But setting it manually means we might be overriding calculation
            }
        }
        public List<Vector3Int> GetCellsByLayer(RoomLayer layer)
        {
            if (layersDirty) CalculateLayers();
            return cellLayers.Where(x => x.Value == layer).Select(x => x.Key).ToList();
        }

        public RoomLayer GetCellLayer(Vector3Int cell)
        {
             if (layersDirty) CalculateLayers();
             return cellLayers.ContainsKey(cell) ? cellLayers[cell] : RoomLayer.None;
        }
        
        public override bool CanPlaceObjectAt(Vector3Int gridPosition, List<Vector3Int> occupiedCells)
        {
            // Allow placement if ALL cells are in Wall or Corner layers
            // Or fallback to base (Inside)
            
            if (layersDirty) CalculateLayers();

            bool allInWall = true;
            foreach(var offset in occupiedCells)
            {
                Vector3Int pos = gridPosition + offset;
                if (!cellLayers.ContainsKey(pos) || cellLayers[pos] == RoomLayer.Floor)
                {
                    allInWall = false; 
                    break; 
                }
            }
            
            if (allInWall) return true;

            return base.CanPlaceObjectAt(gridPosition, occupiedCells);
        }

        public void InitializeForGeneration()
        {
            if (DungeonData != null)
            {
                DungeonData.Clear();
            }
            
            var children = new List<GameObject>();
            foreach (Transform child in transform)
            {
                if (child.name == "Grid" || child.name == "GridVisualization") continue;
                children.Add(child.gameObject);
            }

            foreach (var child in children)
            {
                if (Application.isPlaying) 
                {
                    Destroy(child);
                }
                else 
                {
                    #if UNITY_EDITOR
                    UnityEditor.Undo.DestroyObjectImmediate(child);
                    #else
                    DestroyImmediate(child);
                    #endif
                }
            }
        }

        public void PlaceObjectRaw(Vector3Int cell, int id, int rotation)
        {
            if (DungeonData == null) return;
            
            List<Vector3Int> occupied = new List<Vector3Int> { cell };
            DungeonData.AddObjectAt(cell, occupied, id, rotation);
        }

        public void RefreshGrid()
        {
            // Optional: Update visualization or base grid state
        }
        
        public Vector3Int GetPerimeterNormal(Vector3Int cell)
        {
            // Find direction towards Floor
            Vector3Int[] directions = {
                new Vector3Int(0, 0, 1),  // North
                new Vector3Int(1, 0, 0),  // East
                new Vector3Int(0, 0, -1), // South
                new Vector3Int(-1, 0, 0)  // West
            };
            
            // If this is a wall cell, the normal points OUTWARD from the room (away from Floor)
            // So we look for the neighbor that is Floor, and the normal is opposite to that.
            
            foreach (var dir in directions)
            {
                Vector3Int neighbor = cell + dir;
                if (cellLayers.ContainsKey(neighbor) && cellLayers[neighbor] == RoomLayer.Floor)
                {
                    return -dir; // Point away from floor
                }
            }
            
            return Vector3Int.forward;
        }

        public void SetShape(ShapeType shape)
        {
            if (SplineContainer == null) return;
            var spline = SplineContainer.Spline;
            spline.Clear();

            // Base unit size
            float s = GridCellSize * 5f; // 5x5 blocks per segment approximately

            // Define shapes centered or starting at 0,0
            // We use BezierKnot with Linear interpolation
            
            List<Vector3> points = new List<Vector3>();

            switch (shape)
            {
                case ShapeType.Rectangle:
                    points.Add(new Vector3(-s, 0, -s));
                    points.Add(new Vector3(s, 0, -s));
                    points.Add(new Vector3(s, 0, s));
                    points.Add(new Vector3(-s, 0, s));
                    break;

                case ShapeType.L_Shape:
                    points.Add(new Vector3(-s, 0, -s));
                    points.Add(new Vector3(s, 0, -s));
                    points.Add(new Vector3(s, 0, 0)); // Inner corner
                    points.Add(new Vector3(0, 0, 0));
                    points.Add(new Vector3(0, 0, s));
                    points.Add(new Vector3(-s, 0, s));
                    break;

                case ShapeType.U_Shape:
                    points.Add(new Vector3(-s, 0, -s));
                    points.Add(new Vector3(s, 0, -s));
                    points.Add(new Vector3(s, 0, s));
                    points.Add(new Vector3(0, 0, s));
                    points.Add(new Vector3(0, 0, 0)); // Inner notch
                    points.Add(new Vector3(-s/2, 0, 0)); // Just a small notch? or full U?
                    // Let's do a proper U
                    points.Clear();
                    points.Add(new Vector3(-s, 0, -s)); // BL
                    points.Add(new Vector3(s, 0, -s));  // BR
                    points.Add(new Vector3(s, 0, s));   // TR
                    points.Add(new Vector3(0, 0, s));   // T-Mid-Right
                    points.Add(new Vector3(0, 0, 0));   // Inner Bottom
                    points.Add(new Vector3(-s/2, 0, 0)); // Inner Bottom Left?? 
                    // Actually simpler coordinates:
                    // 0,0 is center.
                    // [-1,-1], [1,-1], [1,1], [0.5, 1], [0.5, 0], [-0.5, 0], [-0.5, 1], [-1, 1]
                    // Scale by s
                    points.Clear();
                    points.Add(new Vector3(-s, 0, -s));
                    points.Add(new Vector3(s, 0, -s));
                    points.Add(new Vector3(s, 0, s));
                    points.Add(new Vector3(s/2, 0, s));
                    points.Add(new Vector3(s/2, 0, 0));
                    points.Add(new Vector3(-s/2, 0, 0));
                    points.Add(new Vector3(-s/2, 0, s));
                    points.Add(new Vector3(-s, 0, s));
                    break;
                
                case ShapeType.T_Shape:
                    // Top bar
                    points.Add(new Vector3(-s, 0, s));
                    points.Add(new Vector3(s, 0, s));
                    points.Add(new Vector3(s, 0, 0));
                    points.Add(new Vector3(s/2, 0, 0));
                    points.Add(new Vector3(s/2, 0, -s));
                    points.Add(new Vector3(-s/2, 0, -s));
                    points.Add(new Vector3(-s/2, 0, 0));
                    points.Add(new Vector3(-s, 0, 0));
                    break;

                case ShapeType.I_Shape:
                     // Long rectangle
                    points.Add(new Vector3(-s/2, 0, -s*1.5f));
                    points.Add(new Vector3(s/2, 0, -s*1.5f));
                    points.Add(new Vector3(s/2, 0, s*1.5f));
                    points.Add(new Vector3(-s/2, 0, s*1.5f));
                    break;
            }

            foreach (var p in points)
            {
                spline.Add(new BezierKnot(p));
            }
            spline.Closed = true;
            
            // Force recalculation
            layersDirty = true;
            CalculateLayers();
        }

        public bool IsSplineKnot(Vector3Int cell)
        {
            if (SplineContainer == null) return false;
            
            Vector3 cellCenter = Grid.GetCellCenterWorld(cell);
            foreach (var knot in SplineContainer.Spline)
            {
                 Vector3 knotWorld = SplineContainer.transform.TransformPoint(knot.Position);
                 // Check if close enough (e.g. within half a cell size)
                 if (Vector3.Distance(cellCenter, knotWorld) < Grid.cellSize.x * 0.6f)
                 {
                     return true;
                 }
            }
            return false;
        }
    }
}
