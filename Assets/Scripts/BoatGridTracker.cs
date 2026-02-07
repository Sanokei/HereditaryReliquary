using System.Collections.Generic;
using UnityEngine;
using GridBuilder.Core;

/// <summary>
/// Tracks the boat's position on the grid and updates GridData dynamically
/// to prevent placing objects (like waves) on top of the boat
/// </summary>
public class BoatGridTracker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Ship boat;
    [SerializeField] private BuildingSystemManager gridManager;
    
    [Header("Tracking Settings")]
    [SerializeField] private float updateInterval = 0.1f;
    [SerializeField] private int boatObjectID = -1; // Special ID for boat in grid data
    [SerializeField] private int boatPlacedObjectIndex = -1; // Index for boat in ObjectPlacer
    
    private List<SplineGridContainer> gridContainers = new List<SplineGridContainer>();
    private Dictionary<SplineGridContainer, List<Vector3Int>> lastBoatCells = new Dictionary<SplineGridContainer, List<Vector3Int>>();
    private float lastUpdateTime;
    private bool isTracking = false;
    
    void Awake()
    {
        if (boat == null)
        {
            boat = FindFirstObjectByType<Ship>();
        }
        
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<BuildingSystemManager>();
        }
    }
    
    void Start()
    {
        // Delay initialization slightly to ensure grid containers are created
        Invoke(nameof(InitializeTracking), 0.1f);
    }
    
    void Update()
    {
        // Refresh grid containers periodically in case new ones are created
        if (gridManager != null && Time.frameCount % 60 == 0) // Every 60 frames (~1 second at 60fps)
        {
            RefreshGridContainers();
        }
        
        if (isTracking && Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateBoatPosition();
            lastUpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// Refreshes the list of grid containers in case new ones were created
    /// </summary>
    private void RefreshGridContainers()
    {
        if (gridManager == null)
            return;
        
        List<SplineGridContainer> currentContainers = gridManager.SplineGridContainers;
        
        // Check if new containers were added
        bool containersChanged = false;
        foreach (var container in currentContainers)
        {
            if (!gridContainers.Contains(container))
            {
                containersChanged = true;
                break;
            }
        }
        
        // Also check if containers were removed
        if (!containersChanged)
        {
            foreach (var container in gridContainers)
            {
                if (!currentContainers.Contains(container))
                {
                    containersChanged = true;
                    break;
                }
            }
        }
        
        if (containersChanged)
        {
            // Update containers list
            gridContainers = currentContainers;
            
            // Re-initialize tracking if not already tracking
            if (!isTracking && boat != null)
            {
                InitializeTracking();
            }
        }
    }
    
    /// <summary>
    /// Initializes boat tracking on all grid containers
    /// </summary>
    private void InitializeTracking()
    {
        if (boat == null)
        {
            Debug.LogError("BoatGridTracker: Boat reference is null. Cannot track boat position.");
            return;
        }
        
        if (gridManager == null)
        {
            Debug.LogError("BoatGridTracker: BuildingSystemManager not found. Cannot track boat position.");
            return;
        }
        
        gridContainers = gridManager.SplineGridContainers;
        
        if (gridContainers.Count == 0)
        {
            Debug.LogWarning("BoatGridTracker: No grid containers found. Boat tracking will not work.");
            return;
        }
        
        // Initialize boat position on all containers
        foreach (var container in gridContainers)
        {
            if (container != null && container.Grid != null)
            {
                List<Vector3Int> boatCells = boat.GetOccupiedGridCells(container.Grid);
                
                // Add boat to grid data if it's within this container's boundary
                if (IsBoatInContainer(container))
                {
                    AddBoatToGrid(container, boatCells);
                }
                
                lastBoatCells[container] = new List<Vector3Int>(boatCells);
            }
        }
        
        isTracking = true;
        lastUpdateTime = Time.time;
    }
    
    /// <summary>
    /// Updates the boat's position on all grid containers
    /// </summary>
    private void UpdateBoatPosition()
    {
        if (boat == null || gridContainers == null)
            return;
        
        foreach (var container in gridContainers)
        {
            if (container == null || container.Grid == null)
                continue;
            
            List<Vector3Int> currentBoatCells = boat.GetOccupiedGridCells(container.Grid);
            
            // Check if boat is in this container
            bool boatInContainer = IsBoatInContainer(container);
            
            // Get previous cells for this container
            if (!lastBoatCells.ContainsKey(container))
            {
                lastBoatCells[container] = new List<Vector3Int>();
            }
            
            List<Vector3Int> previousBoatCells = lastBoatCells[container];
            
            // Check if boat cells have changed
            if (!AreCellListsEqual(currentBoatCells, previousBoatCells))
            {
                // Remove boat from old cells
                RemoveBoatFromGrid(container, previousBoatCells);
                
                // Add boat to new cells if boat is in container
                if (boatInContainer)
                {
                    AddBoatToGrid(container, currentBoatCells);
                }
                
                // Update last known cells
                lastBoatCells[container] = new List<Vector3Int>(currentBoatCells);
            }
            else if (!boatInContainer && previousBoatCells.Count > 0)
            {
                // Boat moved out of container, remove from grid
                RemoveBoatFromGrid(container, previousBoatCells);
                lastBoatCells[container].Clear();
            }
        }
    }
    
    /// <summary>
    /// Adds the boat to the grid data at the specified cells
    /// </summary>
    private void AddBoatToGrid(SplineGridContainer container, List<Vector3Int> cells)
    {
        if (container == null || container.Grid == null || container.GridData == null)
            return;
        
        if (cells == null || cells.Count == 0)
            return;
        
        // Use the first cell as the origin position
        Vector3Int originCell = cells[0];
        
        // Calculate relative cells from origin
        List<Vector3Int> relativeCells = new List<Vector3Int>();
        foreach (var cell in cells)
        {
            relativeCells.Add(cell - originCell);
        }
        
        try
        {
            // Add boat to grid data
            container.GridData.AddObjectAt(originCell, relativeCells, boatObjectID, boatPlacedObjectIndex);
        }
        catch (System.Exception e)
        {
            // Boat might already be in some cells, try to update instead
            Debug.LogWarning($"BoatGridTracker: Could not add boat to grid: {e.Message}. Attempting to update existing cells.");
            UpdateBoatInGrid(container, cells);
        }
    }
    
    /// <summary>
    /// Removes the boat from the grid data at the specified cells
    /// </summary>
    private void RemoveBoatFromGrid(SplineGridContainer container, List<Vector3Int> cells)
    {
        if (container == null || container.Grid == null || container.GridData == null)
            return;
        
        if (cells == null || cells.Count == 0)
            return;
        
        // Remove boat from each cell
        foreach (var cell in cells)
        {
            // Check if this cell has the boat
            if (container.GridData.GetObjectIDAt(cell) == boatObjectID)
            {
                container.GridData.TryRemoveObjectAt(cell);
            }
        }
    }
    
    /// <summary>
    /// Updates boat position in grid by removing old and adding new
    /// </summary>
    private void UpdateBoatInGrid(SplineGridContainer container, List<Vector3Int> newCells)
    {
        // This is a fallback method - remove all boat cells and re-add
        if (lastBoatCells.ContainsKey(container))
        {
            RemoveBoatFromGrid(container, lastBoatCells[container]);
        }
        AddBoatToGrid(container, newCells);
    }
    
    /// <summary>
    /// Checks if the boat is within the container's boundary
    /// </summary>
    private bool IsBoatInContainer(SplineGridContainer container)
    {
        if (boat == null || container == null)
            return false;
        
        return container.IsPositionWithinBoundary(boat.transform.position);
    }
    
    /// <summary>
    /// Gets the boat's currently occupied grid cells for a specific container
    /// </summary>
    public List<Vector3Int> GetBoatCells(SplineGridContainer container)
    {
        if (boat == null || container == null || container.Grid == null)
            return new List<Vector3Int>();
        
        if (lastBoatCells.ContainsKey(container))
        {
            return new List<Vector3Int>(lastBoatCells[container]);
        }
        
        return boat.GetOccupiedGridCells(container.Grid);
    }
    
    /// <summary>
    /// Gets all boat cells across all containers
    /// </summary>
    public Dictionary<SplineGridContainer, List<Vector3Int>> GetAllBoatCells()
    {
        return new Dictionary<SplineGridContainer, List<Vector3Int>>(lastBoatCells);
    }
    
    /// <summary>
    /// Checks if a list of cells would overlap with the boat's current position
    /// </summary>
    public bool WouldOverlapWithBoat(SplineGridContainer container, Vector3Int gridPosition, List<Vector3Int> occupiedCells)
    {
        if (container == null || !lastBoatCells.ContainsKey(container))
            return false;
        
        List<Vector3Int> boatCells = lastBoatCells[container];
        if (boatCells.Count == 0)
            return false;
        
        // Calculate the cells the object would occupy
        HashSet<Vector3Int> objectCells = new HashSet<Vector3Int>();
        foreach (var cell in occupiedCells)
        {
            objectCells.Add(gridPosition + cell);
        }
        
        // Check for overlap
        foreach (var boatCell in boatCells)
        {
            if (objectCells.Contains(boatCell))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Compares two lists of Vector3Int for equality
    /// </summary>
    private bool AreCellListsEqual(List<Vector3Int> list1, List<Vector3Int> list2)
    {
        if (list1 == null && list2 == null)
            return true;
        if (list1 == null || list2 == null)
            return false;
        if (list1.Count != list2.Count)
            return false;
        
        HashSet<Vector3Int> set1 = new HashSet<Vector3Int>(list1);
        HashSet<Vector3Int> set2 = new HashSet<Vector3Int>(list2);
        
        return set1.SetEquals(set2);
    }
    
    void OnDestroy()
    {
        // Clean up: remove boat from all grids
        if (isTracking)
        {
            foreach (var container in gridContainers)
            {
                if (container != null && lastBoatCells.ContainsKey(container))
                {
                    RemoveBoatFromGrid(container, lastBoatCells[container]);
                }
            }
        }
    }
    
    void OnValidate()
    {
        updateInterval = Mathf.Max(0.01f, updateInterval);
    }
}

