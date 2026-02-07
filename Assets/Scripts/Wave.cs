using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GridBuilder.Core;

/// <summary>
/// Component for wave prefabs that apply force to boats and auto-despawn after a duration
/// </summary>
public class Wave : MonoBehaviour
{
    [Header("Wave Force Settings")]
    [SerializeField] private float forceStrength = 10f;
    [SerializeField] private float forceRadius = 5f;
    [SerializeField] private Vector3 forceDirection = Vector3.forward;
    [SerializeField] private bool useRadialForce = true;
    [SerializeField] private float forceFalloff = 1f;
    [SerializeField] private bool propagateToAdjacentCells = true;
    [SerializeField] private float adjacentCellForceMultiplier = 0.8f; // Force strength multiplier for adjacent cells
    
    [Header("Lifetime Settings")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private bool autoDespawn = true;
    
    [Header("Detection Settings")]
    [SerializeField] private LayerMask boatLayer = -1;
    [SerializeField] private float checkInterval = 0.1f;
    
    // Static reference to track the current wave instance
    private static Wave currentWave = null;
    
    private float spawnTime;
    private float lastCheckTime;
    private Collider waveCollider;
    
    // Track grid cells occupied by this wave
    private List<SplineGridContainer> occupiedContainers = new List<SplineGridContainer>();
    private Dictionary<SplineGridContainer, List<Vector3Int>> occupiedCells = new Dictionary<SplineGridContainer, List<Vector3Int>>();
    
    void Start()
    {
        // Destroy previous wave if one exists
        if (currentWave != null && currentWave != this)
        {
            currentWave.Despawn();
        }
        
        // Set this as the current wave
        currentWave = this;
        
        spawnTime = Time.time;
        lastCheckTime = Time.time;
        
        // Get or add a trigger collider for detection
        waveCollider = GetComponent<Collider>();
        if (waveCollider == null)
        {
            // Add a sphere collider as trigger if none exists
            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = forceRadius;
            waveCollider = sphereCollider;
        }
        else
        {
            waveCollider.isTrigger = true;
        }
        
        // Track occupied grid cells
        TrackOccupiedGridCells();
        
        // Start despawn coroutine if auto-despawn is enabled
        if (autoDespawn)
        {
            StartCoroutine(DespawnAfterLifetime());
        }
    }
    
    void FixedUpdate()
    {
        // Apply forces in FixedUpdate for proper physics timing
        // Check periodically based on checkInterval
        if (Time.time - lastCheckTime >= checkInterval)
        {
            ApplyForceToNearbyBoats();
            lastCheckTime = Time.time;
        }
    }
    
    /// <summary>
    /// Applies force to all boats within range
    /// </summary>
    private void ApplyForceToNearbyBoats()
    {
        // Find all ships in the scene first (more reliable than OverlapSphere)
        Ship[] allShips = FindObjectsByType<Ship>(FindObjectsSortMode.None);
        
        if (allShips != null && allShips.Length > 0)
        {
            foreach (Ship ship in allShips)
            {
                if (ship == null || ship.Rigidbody == null)
                    continue;
                
                // Calculate distance from wave center to ship
                float distance = Vector3.Distance(transform.position, ship.transform.position);
                
                // Apply force if within radius
                if (distance <= forceRadius)
                {
                    Vector3 force = CalculateForce(ship.transform.position);
                    
                    // Apply force directly to rigidbody (use FixedUpdate timing)
                    ship.Rigidbody.AddForce(force * Time.fixedDeltaTime * 50f, ForceMode.Force);
                    // Also accumulate for the ship's wave force system
                    ship.ApplyWaveForce(force);
                }
            }
        }
        
        // Also try OverlapSphere as backup (in case layer mask is important)
        Collider[] colliders = Physics.OverlapSphere(transform.position, forceRadius, boatLayer);
        foreach (Collider col in colliders)
        {
            Ship ship = col.GetComponent<Ship>();
            if (ship != null && ship.Rigidbody != null)
            {
                float distance = Vector3.Distance(transform.position, ship.transform.position);
                if (distance <= forceRadius)
                {
                    Vector3 force = CalculateForce(ship.transform.position);
                    ship.Rigidbody.AddForce(force * Time.fixedDeltaTime * 50f, ForceMode.Force);
                    ship.ApplyWaveForce(force);
                }
            }
        }
        
        // Also apply force to boats in adjacent grid cells
        if (propagateToAdjacentCells)
        {
            ApplyForceToBoatsInAdjacentCells();
        }
    }
    
    /// <summary>
    /// Applies force to boats in adjacent grid cells
    /// </summary>
    private void ApplyForceToBoatsInAdjacentCells()
    {
        // Find all ships in the scene
        Ship[] allShips = FindObjectsByType<Ship>(FindObjectsSortMode.None);
        if (allShips == null || allShips.Length == 0)
            return;
        
        // Check each container the wave occupies
        bool foundContainers = false;
        foreach (var container in occupiedContainers)
        {
            if (container == null || container.Grid == null || container.GridData == null)
                continue;
            
            if (!occupiedCells.ContainsKey(container) || occupiedCells[container].Count == 0)
                continue;
            
            foundContainers = true;
            
            // Get all adjacent cells (world positions)
            HashSet<Vector3> adjacentWorldPositions = GetAdjacentCellWorldPositions(container, occupiedCells[container]);
            
            if (adjacentWorldPositions.Count == 0)
                continue;
            
            // Check each ship to see if it's near any adjacent cell
            foreach (Ship boat in allShips)
            {
                if (boat == null || boat.Rigidbody == null)
                    continue;
                
                // Skip if boat is already being affected by the radius-based force
                float distanceToWave = Vector3.Distance(boat.transform.position, transform.position);
                if (distanceToWave <= forceRadius)
                    continue; // Already handled by radius-based method
                
                // Check if boat is near any adjacent cell
                bool boatNearAdjacentCell = false;
                float minDistanceToAdjacent = float.MaxValue;
                
                foreach (Vector3 adjacentWorldPos in adjacentWorldPositions)
                {
                    float distance = Vector3.Distance(boat.transform.position, adjacentWorldPos);
                    // Consider boat near if within 1.5x cell size (to account for boat size)
                    float cellSize = container.GridCellSize;
                    float threshold = cellSize * 1.5f;
                    
                    if (distance <= threshold)
                    {
                        boatNearAdjacentCell = true;
                        minDistanceToAdjacent = Mathf.Min(minDistanceToAdjacent, distance);
                        break;
                    }
                }
                
                // Apply force to boat if it's near an adjacent cell
                if (boatNearAdjacentCell)
                {
                    Vector3 force = CalculateForce(boat.transform.position);
                    // Reduce force for adjacent cells, but scale by distance
                    float distanceFactor = Mathf.Clamp01(1f - (minDistanceToAdjacent / (container.GridCellSize * 1.5f)));
                    force *= adjacentCellForceMultiplier * distanceFactor;
                    
                    // Apply force directly to rigidbody (use FixedUpdate timing)
                    boat.Rigidbody.AddForce(force * Time.fixedDeltaTime * 50f, ForceMode.Force);
                    // Also accumulate for the ship's wave force system
                    boat.ApplyWaveForce(force);
                }
            }
        }
        
        // Fallback: if no containers tracked or cells are empty, use wave position directly
        if (!foundContainers || occupiedContainers.Count == 0)
        {
            ApplyForceToBoatsInAdjacentCellsFallback(allShips);
        }
    }
    
    /// <summary>
    /// Fallback method when wave hasn't tracked its cells yet
    /// </summary>
    private void ApplyForceToBoatsInAdjacentCellsFallback(Ship[] allShips)
    {
        // Find all grid containers
        SplineGridContainer[] containers = FindObjectsByType<SplineGridContainer>(FindObjectsSortMode.None);
        if (containers == null || containers.Length == 0)
            return;
        
        foreach (var container in containers)
        {
            if (container == null || container.Grid == null)
                continue;
            
            // Check if wave is in this container
            if (!container.IsPositionWithinBoundary(transform.position))
                continue;
            
            // Get wave's grid cell
            Vector3Int waveCell = container.Grid.WorldToCell(transform.position);
            
            // Get adjacent cells
            List<Vector3Int> waveCells = new List<Vector3Int> { waveCell };
            HashSet<Vector3> adjacentWorldPositions = GetAdjacentCellWorldPositions(container, waveCells);
            
            if (adjacentWorldPositions.Count == 0)
                continue;
            
            // Check each ship
            foreach (Ship boat in allShips)
            {
                if (boat == null || boat.Rigidbody == null)
                    continue;
                
                // Skip if boat is already being affected by the radius-based force
                float distanceToWave = Vector3.Distance(boat.transform.position, transform.position);
                if (distanceToWave <= forceRadius)
                    continue;
                
                // Check if boat is near any adjacent cell
                bool boatNearAdjacentCell = false;
                float minDistanceToAdjacent = float.MaxValue;
                
                foreach (Vector3 adjacentWorldPos in adjacentWorldPositions)
                {
                    float distance = Vector3.Distance(boat.transform.position, adjacentWorldPos);
                    float cellSize = container.GridCellSize;
                    float threshold = cellSize * 1.5f;
                    
                    if (distance <= threshold)
                    {
                        boatNearAdjacentCell = true;
                        minDistanceToAdjacent = Mathf.Min(minDistanceToAdjacent, distance);
                        break;
                    }
                }
                
                // Apply force to boat if it's near an adjacent cell
                if (boatNearAdjacentCell)
                {
                    Vector3 force = CalculateForce(boat.transform.position);
                    float distanceFactor = Mathf.Clamp01(1f - (minDistanceToAdjacent / (container.GridCellSize * 1.5f)));
                    force *= adjacentCellForceMultiplier * distanceFactor;
                    
                    boat.Rigidbody.AddForce(force * Time.fixedDeltaTime * 50f, ForceMode.Force);
                    boat.ApplyWaveForce(force);
                }
            }
        }
    }
    
    /// <summary>
    /// Gets world positions of all adjacent cells
    /// </summary>
    private HashSet<Vector3> GetAdjacentCellWorldPositions(SplineGridContainer container, List<Vector3Int> occupiedCells)
    {
        HashSet<Vector3> worldPositions = new HashSet<Vector3>();
        
        if (container == null || container.Grid == null)
            return worldPositions;
        
        // Get adjacent cells
        HashSet<Vector3Int> adjacentCells = GetAdjacentCells(container, occupiedCells);
        
        // Convert to world positions
        foreach (var cell in adjacentCells)
        {
            Vector3 worldPos = container.Grid.GetCellCenterWorld(cell);
            worldPositions.Add(worldPos);
        }
        
        return worldPositions;
    }
    
    /// <summary>
    /// Gets all adjacent cells for the given occupied cells
    /// </summary>
    private HashSet<Vector3Int> GetAdjacentCells(SplineGridContainer container, List<Vector3Int> occupiedCells)
    {
        HashSet<Vector3Int> adjacentCells = new HashSet<Vector3Int>();
        
        if (container == null || container.Grid == null)
            return adjacentCells;
        
        // Define 8 directions (4 cardinal + 4 diagonal) for full propagation
        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(0, 0, 1),   // North
            new Vector3Int(0, 0, -1),  // South
            new Vector3Int(1, 0, 0),   // East
            new Vector3Int(-1, 0, 0),  // West
            new Vector3Int(1, 0, 1),    // Northeast
            new Vector3Int(-1, 0, 1),   // Northwest
            new Vector3Int(1, 0, -1),   // Southeast
            new Vector3Int(-1, 0, -1)   // Southwest
        };
        
        // For each occupied cell, check all adjacent cells
        foreach (var cell in occupiedCells)
        {
            foreach (var direction in directions)
            {
                Vector3Int adjacentCell = cell + direction;
                
                // Check if adjacent cell is within grid boundary
                Vector3 worldPos = container.Grid.GetCellCenterWorld(adjacentCell);
                if (container.IsPositionWithinBoundary(worldPos))
                {
                    adjacentCells.Add(adjacentCell);
                }
            }
        }
        
        return adjacentCells;
    }
    
    /// <summary>
    /// Calculates the force to apply to a boat at the given position
    /// </summary>
    private Vector3 CalculateForce(Vector3 boatPosition)
    {
        Vector3 direction;
        
        if (useRadialForce)
        {
            // Force direction is from wave center to boat
            direction = (boatPosition - transform.position).normalized;
            // Flatten to XZ plane
            direction.y = 0f;
            direction.Normalize();
        }
        else
        {
            // Use fixed direction (relative to wave's rotation)
            direction = transform.TransformDirection(forceDirection.normalized);
            direction.y = 0f;
            direction.Normalize();
        }
        
        // Calculate distance and apply falloff
        float distance = Vector3.Distance(transform.position, boatPosition);
        float distanceFactor = 1f;
        
        if (forceFalloff > 0f && distance > 0f)
        {
            // Inverse square falloff
            distanceFactor = Mathf.Pow(1f - (distance / forceRadius), forceFalloff);
            distanceFactor = Mathf.Clamp01(distanceFactor);
        }
        
        return direction * forceStrength * distanceFactor;
    }
    
    /// <summary>
    /// Coroutine to despawn the wave after its lifetime
    /// </summary>
    private IEnumerator DespawnAfterLifetime()
    {
        yield return new WaitForSeconds(lifetime);
        Despawn();
    }
    
    /// <summary>
    /// Manually despawn the wave
    /// </summary>
    public void Despawn()
    {
        // Free occupied grid cells
        FreeOccupiedGridCells();
        
        // Clear static reference if this is the current wave
        if (currentWave == this)
        {
            currentWave = null;
        }
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Tracks which grid cells this wave occupies
    /// </summary>
    private void TrackOccupiedGridCells()
    {
        // Find ObjectPlacer to get this wave's index
        ObjectPlacer objectPlacer = FindFirstObjectByType<ObjectPlacer>();
        if (objectPlacer == null)
            return;
        
        // Find this wave's index in the ObjectPlacer's list using reflection
        int waveIndex = -1;
        var placedObjectsField = typeof(ObjectPlacer).GetField("placedGameObjects", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (placedObjectsField != null)
        {
            var placedObjects = placedObjectsField.GetValue(objectPlacer) as System.Collections.IList;
            if (placedObjects != null)
            {
                for (int i = 0; i < placedObjects.Count; i++)
                {
                    if (placedObjects[i] == gameObject)
                    {
                        waveIndex = i;
                        break;
                    }
                }
            }
        }
        
        if (waveIndex < 0)
            return;
        
        // Find all SplineGridContainers in the scene
        SplineGridContainer[] containers = FindObjectsByType<SplineGridContainer>(FindObjectsSortMode.None);
        
        if (containers == null || containers.Length == 0)
            return;
        
        Vector3 wavePosition = transform.position;
        
        foreach (var container in containers)
        {
            if (container == null || container.Grid == null || container.GridData == null)
                continue;
            
            // Check if wave position is within this container's boundary
            if (!container.IsPositionWithinBoundary(wavePosition))
                continue;
            
            // Convert wave position to grid cell
            Vector3Int gridCell = container.Grid.WorldToCell(wavePosition);
            
            // Check if this cell is occupied and get the PlacementData
            if (container.GridData.HasObjectAt(gridCell))
            {
                // Get the representation index at this cell
                int representationIndex = container.GridData.GetRepresentationIndex(gridCell);
                
                // If it matches our wave index, get all cells for this object
                if (representationIndex == waveIndex)
                {
                    // Get all cells occupied by this object
                    List<Vector3Int> cells = container.GridData.GetCellsForObjectIndex(waveIndex);
                    
                    if (cells != null && cells.Count > 0)
                    {
                        occupiedContainers.Add(container);
                        occupiedCells[container] = cells;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Frees the grid cells occupied by this wave
    /// </summary>
    private void FreeOccupiedGridCells()
    {
        foreach (var container in occupiedContainers)
        {
            if (container == null || container.GridData == null)
                continue;
            
            if (occupiedCells.ContainsKey(container))
            {
                foreach (var cell in occupiedCells[container])
                {
                    container.GridData.TryRemoveObjectAt(cell);
                }
            }
        }
        
        occupiedContainers.Clear();
        occupiedCells.Clear();
    }
    
    /// <summary>
    /// Gets the remaining lifetime of the wave
    /// </summary>
    public float GetRemainingLifetime()
    {
        return Mathf.Max(0f, lifetime - (Time.time - spawnTime));
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Apply immediate force when boat enters trigger
        Ship ship = other.GetComponent<Ship>();
        if (ship != null && ship.Rigidbody != null)
        {
            Vector3 force = CalculateForce(ship.transform.position);
            // Apply immediate impulse force
            ship.Rigidbody.AddForce(force * 0.1f, ForceMode.Impulse);
            // Also accumulate for the ship's wave force system
            ship.ApplyWaveForce(force);
        }
    }
    
    void OnTriggerStay(Collider other)
    {
        // Apply continuous force while boat is in trigger
        Ship ship = other.GetComponent<Ship>();
        if (ship != null && ship.Rigidbody != null)
        {
            Vector3 force = CalculateForce(ship.transform.position);
            // Apply force directly to rigidbody for more immediate effect
            ship.Rigidbody.AddForce(force * Time.fixedDeltaTime * 50f, ForceMode.Force);
            // Also accumulate for the ship's wave force system
            ship.ApplyWaveForce(force);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw force radius in editor
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, forceRadius);
        
        // Draw force direction
        if (!useRadialForce)
        {
            Gizmos.color = Color.yellow;
            Vector3 direction = transform.TransformDirection(forceDirection.normalized);
            direction.y = 0f;
            Gizmos.DrawRay(transform.position, direction * forceRadius);
        }
    }
    
    void OnDestroy()
    {
        // Ensure we free grid cells when destroyed (in case Despawn wasn't called)
        FreeOccupiedGridCells();
        
        // Clear static reference if this is the current wave
        if (currentWave == this)
        {
            currentWave = null;
        }
    }
    
    void OnValidate()
    {
        forceStrength = Mathf.Max(0f, forceStrength);
        forceRadius = Mathf.Max(0.1f, forceRadius);
        lifetime = Mathf.Max(0f, lifetime);
        forceFalloff = Mathf.Max(0f, forceFalloff);
        checkInterval = Mathf.Max(0.01f, checkInterval);
    }
}

